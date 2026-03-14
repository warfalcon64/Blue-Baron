using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    private float current;
    private float lowest = float.MaxValue;
    private float highest;

    private float sampleTimer;
    private int frameCount;
    private float frameTimeAccum;

    private const float sampleInterval = 0.5f;

    private GUIStyle style;

    private void Update()
    {
        frameCount++;
        frameTimeAccum += Time.unscaledDeltaTime;

        sampleTimer += Time.unscaledDeltaTime;
        if (sampleTimer >= sampleInterval)
        {
            current = frameCount / frameTimeAccum;
            frameCount = 0;
            frameTimeAccum = 0f;
            sampleTimer = 0f;

            if (current < lowest) lowest = current;
            if (current > highest) highest = current;
        }
    }

    private void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.fontSize = 18;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
        }

        float x = 10f;
        float y = 10f;
        float w = 260f;
        float h = 70f;

        GUI.Box(new Rect(x, y, w, h), GUIContent.none);
        GUI.Label(new Rect(x + 6, y + 4, w, 22), $"FPS: {current:F0}", style);
        GUI.Label(new Rect(x + 6, y + 24, w, 22), $"Low: {(lowest < float.MaxValue ? lowest : 0f):F0}", style);
        GUI.Label(new Rect(x + 6, y + 44, w, 22), $"High: {highest:F0}", style);
    }

    public void ResetMinMax()
    {
        lowest = float.MaxValue;
        highest = 0f;
    }
}
