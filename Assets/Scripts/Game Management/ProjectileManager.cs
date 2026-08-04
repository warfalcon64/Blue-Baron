using System.Collections.Generic;
using UnityEngine;

// Central driver for projectiles without rigidbodies and colliders.
// Replaces per-projectile MonoBehaviour ticking with a single Update loop.

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }

    // Viewport LOD: bolts inside the expanded camera AABB step every frame at dt; bolts outside
    // accumulate dt and step at ~1/OffscreenInterval Hz with the accumulated time. Trajectories,
    // hit detection and expiry stay exact — a low-rate step sweeps one long CircleCast capsule
    // instead of several short ones, covering the same corridor with a tenth of the casts.
    private const float OffscreenInterval = 0.1f;

    // The AABB is expanded so bolts about to enter the view are already stepping at full rate
    // (no visible pop at the screen edge): margin >= max bolt speed (plasma 25 + ship ~10) *
    // OffscreenInterval, with headroom for the camera tracking the player.
    private const float ViewMargin = 5f;

    // Verification aids, usable in builds. InvertLodTiers is a positive control: on-screen bolts
    // get the low rate, so a visible 10 Hz stutter proves the tier machinery works. The overlay
    // prints per-frame tier counts.
    public static bool InvertLodTiers = false;
    public static bool ShowDebugOverlay = false;
    public int FullRateCount { get; private set; }
    public int LowRateCount { get; private set; }

    private readonly List<IManagedProjectile> projectiles = new List<IManagedProjectile>(8192);

    private Camera viewCamera;
    private float viewXMin, viewXMax, viewYMin, viewYMax;
    private bool hasViewBounds;

    public static ProjectileManager GetOrCreate()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("ProjectileManager");
            Instance = go.AddComponent<ProjectileManager>();
        }
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Register(IManagedProjectile p)
    {
        p.ManagerIndex = projectiles.Count;
        // Pooled projectiles can carry a stale accumulator from their previous flight.
        p.LodAccumulator = 0f;
        projectiles.Add(p);
    }

    public void Unregister(IManagedProjectile p)
    {
        int i = p.ManagerIndex;
        if (i < 0 || i >= projectiles.Count || projectiles[i] != p) return;
        RemoveAt(i);
        p.ManagerIndex = -1;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        UpdateViewBounds();

        int fullCount = 0, lowCount = 0;

        // Iterate backwards so a finished projectile's swap-removal (tail moves into slot i) never
        // skips or double-processes a replacement.
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            IManagedProjectile p = projectiles[i];
            float acc = p.LodAccumulator + dt;

            bool fullRate = true;
            if (hasViewBounds)
            {
                Vector2 pos = p.Position;
                fullRate = pos.x >= viewXMin && pos.x <= viewXMax
                        && pos.y >= viewYMin && pos.y <= viewYMax;
                if (InvertLodTiers) fullRate = !fullRate;
            }

            if (fullRate) fullCount++;
            else lowCount++;

            if (!fullRate && acc < OffscreenInterval)
            {
                p.LodAccumulator = acc;
                continue;
            }

            // Full-rate bolts step with plain dt (their accumulator is always 0); a bolt that just
            // crossed into view catches up with its accumulated time in this same swept step.
            p.LodAccumulator = 0f;
            if (!p.Step(acc))
            {
                RemoveAt(i);
                p.ManagerIndex = -1;
                p.Despawn();
            }
        }

        FullRateCount = fullCount;
        LowRateCount = lowCount;
    }

    private void UpdateViewBounds()
    {
        if (viewCamera == null)
            viewCamera = Camera.main;

        // No usable camera: step everything at full rate rather than guessing at bounds.
        if (viewCamera == null || !viewCamera.orthographic)
        {
            hasViewBounds = false;
            return;
        }

        Vector2 center = viewCamera.transform.position;
        float halfH = viewCamera.orthographicSize + ViewMargin;
        float halfW = viewCamera.orthographicSize * viewCamera.aspect + ViewMargin;
        viewXMin = center.x - halfW;
        viewXMax = center.x + halfW;
        viewYMin = center.y - halfH;
        viewYMax = center.y + halfH;
        hasViewBounds = true;
    }

    private void OnGUI()
    {
        if (!ShowDebugOverlay) return;
        GUI.Label(new Rect(10f, 10f, 460f, 22f),
            $"Plasma LOD  full-rate: {FullRateCount}  low-rate: {LowRateCount}" +
            $"{(hasViewBounds ? "" : "  [no camera - all full]")}{(InvertLodTiers ? "  [INVERTED]" : "")}");
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!hasViewBounds) return;
        Gizmos.color = InvertLodTiers ? Color.red : Color.yellow;
        Vector3 a = new Vector3(viewXMin, viewYMin);
        Vector3 b = new Vector3(viewXMax, viewYMin);
        Vector3 c = new Vector3(viewXMax, viewYMax);
        Vector3 d = new Vector3(viewXMin, viewYMax);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }
#endif

    // The moved tail element takes slot i.
    private void RemoveAt(int i)
    {
        int last = projectiles.Count - 1;
        IManagedProjectile moved = projectiles[last];
        projectiles[i] = moved;
        moved.ManagerIndex = i;
        projectiles.RemoveAt(last);
    }
}
