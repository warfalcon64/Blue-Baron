using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    // Get rid of offsets if never used
    public float xOffset;
    public float yOffset;
    public ShipBase player;

    [Header("Debug Camera")]
    [SerializeField] private bool debugEnabled = true;
    [SerializeField] private KeyCode toggleMissileFollowKey = KeyCode.M;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 2f;
    [SerializeField] private float maxZoom = 200f;

    private Camera cam;
    private float defaultOrthoSize;
    private Transform missileTarget;
    private WeaponsAAMissile trackedMissile;
    private bool followingMissile;

    // GUI resources
    private Texture2D whiteTex;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
            defaultOrthoSize = cam.orthographicSize;

        whiteTex = new Texture2D(1, 1);
        whiteTex.SetPixel(0, 0, Color.white);
        whiteTex.Apply();
    }

    private void Update()
    {
        if (!debugEnabled) return;

        // Toggle missile follow with key
        if (Input.GetKeyDown(toggleMissileFollowKey))
        {
            if (followingMissile)
            {
                // Return to player
                followingMissile = false;
                missileTarget = null;
                trackedMissile = null;
                if (cam != null)
                    cam.orthographicSize = defaultOrthoSize;
            }
            else
            {
                // Find the nearest active missile
                WeaponsAAMissile missile = FindClosestMissile();
                if (missile != null)
                {
                    missileTarget = missile.transform;
                    trackedMissile = missile;
                    followingMissile = true;
                }
            }
        }

        // Scroll wheel zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f && cam != null)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }

        // If missile was destroyed, return to player
        if (followingMissile && missileTarget == null)
        {
            followingMissile = false;
            trackedMissile = null;
            if (cam != null)
                cam.orthographicSize = defaultOrthoSize;
        }
    }

    // Update is called once per frame
    private void LateUpdate()
    {
        if (followingMissile && missileTarget != null)
        {
            transform.position = missileTarget.position + new Vector3(0, 0, -10);
        }
        else
        {
            // The vector 3 sets the camera above the player so you can actually see stuff
            transform.position = player.transform.position + new Vector3(xOffset, yOffset, -10);
        }
    }

    private void OnGUI()
    {
        if (!followingMissile || trackedMissile == null || cam == null) return;

        GameObject tgt = trackedMissile.GetTarget();

        // Draw target direction arrow
        if (tgt != null)
        {
            Vector2 missileScreenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector3 targetWorldPos = tgt.transform.position;
            Vector3 targetScreenPos3 = cam.WorldToScreenPoint(targetWorldPos);
            // Flip Y for GUI coordinates
            Vector2 targetScreenPos = new Vector2(targetScreenPos3.x, Screen.height - targetScreenPos3.y);

            Vector2 dir = (targetScreenPos - missileScreenPos).normalized;
            float dist = Vector2.Distance(targetScreenPos, missileScreenPos);

            // Arrow sits at edge of screen or at target, whichever is closer
            float arrowDist = Mathf.Min(dist, Mathf.Min(Screen.width, Screen.height) * 0.4f);
            Vector2 arrowTip = missileScreenPos + dir * arrowDist;

            // Draw line from center toward target
            Color arrowColor = Color.red;
            DrawLine(missileScreenPos + dir * 30f, arrowTip, arrowColor, 2f);

            // Draw arrowhead
            Vector2 perp = new Vector2(-dir.y, dir.x);
            Vector2 back = arrowTip - dir * 12f;
            DrawLine(arrowTip, back + perp * 6f, arrowColor, 2f);
            DrawLine(arrowTip, back - perp * 6f, arrowColor, 2f);

            // Distance label
            float worldDist = Vector2.Distance(trackedMissile.transform.position, tgt.transform.position);
            GUI.color = arrowColor;
            GUI.Label(new Rect(arrowTip.x + 10, arrowTip.y - 10, 100, 20), $"{worldDist:F0}m");
        }

        // Draw fuel bar
        float fuel = trackedMissile.GetFuelFraction();
        float barWidth = 120f;
        float barHeight = 10f;
        float barX = 10f;
        float barY = Screen.height - 30f;

        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(barX - 1, barY - 1, barWidth + 2, barHeight + 2), whiteTex);
        GUI.color = Color.Lerp(Color.red, Color.green, fuel);
        GUI.DrawTexture(new Rect(barX, barY, barWidth * fuel, barHeight), whiteTex);
        GUI.color = Color.white;
        GUI.Label(new Rect(barX, barY - 18, barWidth, 20), $"Fuel: {fuel * 100f:F0}%");

        // Speed label
        GUI.Label(new Rect(barX, barY - 36, barWidth, 20), $"Speed: {trackedMissile.GetSpeed():F1}");
    }

    private void DrawLine(Vector2 a, Vector2 b, Color color, float width)
    {
        Matrix4x4 matrixBackup = GUI.matrix;
        GUI.color = color;

        Vector2 delta = b - a;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        float length = delta.magnitude;

        GUIUtility.RotateAroundPivot(angle, a);
        GUI.DrawTexture(new Rect(a.x, a.y - width * 0.5f, length, width), whiteTex);
        GUI.matrix = matrixBackup;
    }

    private WeaponsAAMissile FindClosestMissile()
    {
        WeaponsAAMissile[] missiles = FindObjectsOfType<WeaponsAAMissile>();
        WeaponsAAMissile closest = null;
        float closestDist = float.MaxValue;

        foreach (var m in missiles)
        {
            float dist = Vector2.Distance(player.transform.position, m.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = m;
            }
        }
        return closest;
    }
}
