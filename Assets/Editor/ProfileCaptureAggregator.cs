using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

public static class ProfileCaptureAggregator
{
    private const string CaptureDir = "ProfilerCaptures";
    private const int SkipLeadingFrames = 0;
    private const string FramePrefKey = "ProfileAggregator_DumpFrameIndex";

    [MenuItem("Tools/Profile Aggregate/Aggregate Latest Capture")]
    public static void AggregateLatest()
    {
        string path = FindLatestCapture();
        if (path == null) return;
        AggregateFile(path);
    }

    [MenuItem("Tools/Profile Aggregate/Dump Median-Cost Frame From Latest")]
    public static void DumpMedianFrameFromLatest()
    {
        string path = FindLatestCapture();
        if (path == null) return;
        DumpFrame(path, frameIndex: -1);
    }

    [MenuItem("Tools/Profile Aggregate/Dump Specific Frame From Latest")]
    public static void DumpSpecificFrameFromLatest()
    {
        string path = FindLatestCapture();
        if (path == null) return;
        int frame = EditorPrefs.GetInt(FramePrefKey, 136);
        DumpFrame(path, frame);
    }

    [MenuItem("Tools/Profile Aggregate/Set Dump Frame Index")]
    public static void SetDumpFrameIndex()
    {
        int current = EditorPrefs.GetInt(FramePrefKey, 136);
        string s = EditorUtility.DisplayDialogComplex(
            "Set Dump Frame Index",
            $"Current: {current}. Edit Assets/Editor/ProfileCaptureAggregator.cs constant or use EditorPrefs.SetInt(\"{FramePrefKey}\", N).",
            "OK", "", "") == 0 ? "" : "";
    }

    private static string FindLatestCapture()
    {
        if (!Directory.Exists(CaptureDir))
        {
            Debug.LogError($"[ProfileAggregator] Directory not found: {CaptureDir}");
            return null;
        }
        var files = Directory.GetFiles(CaptureDir, "*.data");
        if (files.Length == 0)
        {
            Debug.LogError("[ProfileAggregator] No .data files in ProfilerCaptures/");
            return null;
        }
        return files.OrderByDescending(f => File.GetLastWriteTime(f)).First();
    }

    public static void AggregateFile(string path)
    {
        Debug.Log($"[ProfileAggregator] Loading: {path}");
        ProfilerDriver.LoadProfile(path, false);

        int firstFrame = ProfilerDriver.firstFrameIndex;
        int lastFrame = ProfilerDriver.lastFrameIndex;
        int totalFrames = lastFrame - firstFrame + 1;
        int analyzedFirst = firstFrame + Mathf.Min(SkipLeadingFrames, Mathf.Max(0, totalFrames - 1));
        Debug.Log($"[ProfileAggregator] Frames: {firstFrame}..{lastFrame}, analyzing {analyzedFirst}..{lastFrame}");

        var perFrameSamples = new Dictionary<string, MarkerSamples>(2048);
        var children = new List<int>(64);
        var stack = new Stack<int>(256);
        var frameTotalMs = new List<float>(totalFrames);
        int analyzedCount = 0;

        for (int f = analyzedFirst; f <= lastFrame; f++)
        {
            using (var view = ProfilerDriver.GetHierarchyFrameDataView(
                f, 0,
                HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                HierarchyFrameDataView.columnTotalTime, false))
            {
                if (view == null || !view.valid) continue;

                var thisFrame = new Dictionary<string, FrameMarker>(256);

                int rootId = view.GetRootItemID();
                stack.Clear();
                children.Clear();
                view.GetItemChildren(rootId, children);
                for (int i = 0; i < children.Count; i++) stack.Push(children[i]);

                while (stack.Count > 0)
                {
                    int id = stack.Pop();
                    string name = view.GetItemName(id);
                    if (!string.IsNullOrEmpty(name))
                    {
                        float totalMs = view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnTotalTime);
                        float selfMs = view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnSelfTime);
                        float calls = view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnCalls);

                        if (thisFrame.TryGetValue(name, out var s))
                        {
                            s.totalMs += totalMs;
                            s.selfMs += selfMs;
                            s.calls += (long)calls;
                            thisFrame[name] = s;
                        }
                        else
                        {
                            thisFrame[name] = new FrameMarker { totalMs = totalMs, selfMs = selfMs, calls = (long)calls };
                        }
                    }

                    children.Clear();
                    view.GetItemChildren(id, children);
                    for (int i = 0; i < children.Count; i++) stack.Push(children[i]);
                }

                foreach (var kv in thisFrame)
                {
                    if (!perFrameSamples.TryGetValue(kv.Key, out var samples))
                    {
                        samples = new MarkerSamples();
                        perFrameSamples[kv.Key] = samples;
                    }
                    samples.totalMs.Add(kv.Value.totalMs);
                    samples.selfMs.Add(kv.Value.selfMs);
                    samples.calls.Add(kv.Value.calls);
                }

                if (thisFrame.TryGetValue("PlayerLoop", out var pl))
                    frameTotalMs.Add(pl.totalMs);
            }
            analyzedCount++;
        }

        var stats = new List<MarkerStat>(perFrameSamples.Count);
        foreach (var kv in perFrameSamples)
            stats.Add(ComputeStats(kv.Key, kv.Value));
        stats.Sort((a, b) => b.medianTotal.CompareTo(a.medianTotal));
        if (stats.Count > 250) stats.RemoveRange(250, stats.Count - 250);

        var frameTotalSorted = new List<float>(frameTotalMs);
        frameTotalSorted.Sort();
        float frameMedian = Pct(frameTotalSorted, 0.5f);
        float frameP95 = Pct(frameTotalSorted, 0.95f);
        float frameMean = frameTotalSorted.Count > 0 ? frameTotalSorted.Sum() / frameTotalSorted.Count : 0;
        float frameMin = frameTotalSorted.Count > 0 ? frameTotalSorted[0] : 0;
        float frameMax = frameTotalSorted.Count > 0 ? frameTotalSorted[frameTotalSorted.Count - 1] : 0;

        var sb = new StringBuilder();
        sb.AppendLine($"Capture: {path}");
        sb.AppendLine($"Total frames in capture: {totalFrames} ({firstFrame}..{lastFrame})");
        sb.AppendLine($"Analyzed: {analyzedCount} frames ({analyzedFirst}..{lastFrame}); skipped first {SkipLeadingFrames} to dodge spawn-spike");
        sb.AppendLine();
        sb.AppendLine("FRAME TOTAL (PlayerLoop) DISTRIBUTION:");
        sb.AppendLine($"  Min: {frameMin:F2}  Median: {frameMedian:F2}  Mean: {frameMean:F2}  P95: {frameP95:F2}  Max: {frameMax:F2}  ms");
        sb.AppendLine();
        sb.AppendLine("PER-MARKER STATS — values are PER-FRAME totals (the marker's combined time within a frame across all its calls).");
        sb.AppendLine("Sorted by MedianTotal descending. MedianTotal is the headline number — matches what you see in the profiler hierarchy for a typical frame.");
        sb.AppendLine();
        sb.AppendLine($"{"Marker",-90} {"MedianTot",10} {"MeanTot",10} {"P95Tot",10} {"MaxTot",10} {"MedianSelf",10} {"MeanCalls",10}");
        sb.AppendLine(new string('-', 160));
        foreach (var s in stats)
        {
            sb.AppendLine($"{Trunc(s.name, 88),-90} {s.medianTotal,10:F3} {s.meanTotal,10:F3} {s.p95Total,10:F3} {s.maxTotal,10:F3} {s.medianSelf,10:F3} {s.meanCalls,10:F0}");
        }

        string outPath = Path.Combine(CaptureDir, "aggregate.txt");
        File.WriteAllText(outPath, sb.ToString());
        Debug.Log($"[ProfileAggregator] Wrote {stats.Count} markers to {outPath}");
    }

    public static void DumpFrame(string path, int frameIndex)
    {
        Debug.Log($"[ProfileAggregator] Loading: {path}");
        ProfilerDriver.LoadProfile(path, false);

        int firstFrame = ProfilerDriver.firstFrameIndex;
        int lastFrame = ProfilerDriver.lastFrameIndex;

        if (frameIndex < 0)
        {
            int totalFrames = lastFrame - firstFrame + 1;
            int analyzedFirst = firstFrame + Mathf.Min(SkipLeadingFrames, Mathf.Max(0, totalFrames - 1));
            var totals = new List<(int idx, float total)>(totalFrames);
            for (int f = analyzedFirst; f <= lastFrame; f++)
            {
                using (var view = ProfilerDriver.GetHierarchyFrameDataView(
                    f, 0, HierarchyFrameDataView.ViewModes.Default,
                    HierarchyFrameDataView.columnTotalTime, false))
                {
                    if (view == null || !view.valid) continue;
                    int rootId = view.GetRootItemID();
                    var rootChildren = new List<int>();
                    view.GetItemChildren(rootId, rootChildren);
                    float t = 0;
                    foreach (var c in rootChildren)
                    {
                        if (view.GetItemName(c) == "PlayerLoop")
                            t = view.GetItemColumnDataAsFloat(c, HierarchyFrameDataView.columnTotalTime);
                    }
                    totals.Add((f, t));
                }
            }
            totals.Sort((a, b) => a.total.CompareTo(b.total));
            frameIndex = totals[totals.Count / 2].idx;
            Debug.Log($"[ProfileAggregator] Median-cost frame is index {frameIndex} ({totals[totals.Count / 2].total:F2} ms)");
        }

        if (frameIndex < firstFrame || frameIndex > lastFrame)
        {
            Debug.LogError($"[ProfileAggregator] Frame {frameIndex} out of range [{firstFrame}..{lastFrame}]");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Capture: {path}");
        sb.AppendLine($"Frame index: {frameIndex} (capture range {firstFrame}..{lastFrame})");
        sb.AppendLine("Hierarchy view of main thread, sorted by total time desc, depth-limited.");
        sb.AppendLine();
        sb.AppendLine($"{"Marker (indented)",-90} {"TotalMs",10} {"SelfMs",10} {"Calls",10}");
        sb.AppendLine(new string('-', 130));

        using (var view = ProfilerDriver.GetHierarchyFrameDataView(
            frameIndex, 0,
            HierarchyFrameDataView.ViewModes.Default,
            HierarchyFrameDataView.columnTotalTime, false))
        {
            if (view == null || !view.valid)
            {
                Debug.LogError($"[ProfileAggregator] Invalid view for frame {frameIndex}");
                return;
            }
            int rootId = view.GetRootItemID();
            DumpRecursive(view, rootId, 0, sb);
        }

        string outPath = Path.Combine(CaptureDir, $"frame_{frameIndex}.txt");
        File.WriteAllText(outPath, sb.ToString());
        Debug.Log($"[ProfileAggregator] Wrote frame {frameIndex} to {outPath}");
    }

    private static void DumpRecursive(HierarchyFrameDataView view, int id, int depth, StringBuilder sb)
    {
        var children = new List<int>();
        view.GetItemChildren(id, children);
        children.Sort((a, b) =>
            view.GetItemColumnDataAsFloat(b, HierarchyFrameDataView.columnTotalTime)
                .CompareTo(view.GetItemColumnDataAsFloat(a, HierarchyFrameDataView.columnTotalTime)));

        foreach (int childId in children)
        {
            string name = view.GetItemName(childId);
            float totalMs = view.GetItemColumnDataAsFloat(childId, HierarchyFrameDataView.columnTotalTime);
            float selfMs = view.GetItemColumnDataAsFloat(childId, HierarchyFrameDataView.columnSelfTime);
            float calls = view.GetItemColumnDataAsFloat(childId, HierarchyFrameDataView.columnCalls);

            string indent = new string(' ', depth * 2);
            string label = Trunc(indent + name, 88);
            sb.AppendLine($"{label,-90} {totalMs,10:F3} {selfMs,10:F3} {(long)calls,10}");

            if (totalMs > 0.05f && depth < 8)
                DumpRecursive(view, childId, depth + 1, sb);
        }
    }

    private static MarkerStat ComputeStats(string name, MarkerSamples samples)
    {
        var s = new MarkerStat { name = name };
        var sortedTotal = new List<float>(samples.totalMs);
        sortedTotal.Sort();
        var sortedSelf = new List<float>(samples.selfMs);
        sortedSelf.Sort();

        int n = sortedTotal.Count;
        if (n == 0) return s;

        s.medianTotal = Pct(sortedTotal, 0.5f);
        s.medianSelf = Pct(sortedSelf, 0.5f);
        s.p95Total = Pct(sortedTotal, 0.95f);
        s.maxTotal = sortedTotal[n - 1];
        s.meanTotal = sortedTotal.Sum() / n;
        s.meanCalls = (float)samples.calls.Sum() / n;
        return s;
    }

    private static float Pct(List<float> sorted, float p)
    {
        if (sorted.Count == 0) return 0;
        int idx = Mathf.Clamp((int)(sorted.Count * p), 0, sorted.Count - 1);
        return sorted[idx];
    }

    private static string Trunc(string s, int max) => s.Length <= max ? s : s.Substring(0, max);

    private struct MarkerStat
    {
        public string name;
        public float medianTotal;
        public float meanTotal;
        public float p95Total;
        public float maxTotal;
        public float medianSelf;
        public float meanCalls;
    }

    private struct FrameMarker
    {
        public float totalMs;
        public float selfMs;
        public long calls;
    }

    private class MarkerSamples
    {
        public List<float> totalMs = new List<float>(64);
        public List<float> selfMs = new List<float>(64);
        public List<long> calls = new List<long>(64);
    }
}
