using UnityEngine;
using UnityEngine.VFX;

public class Flare : MonoBehaviour
{
    [Header("Attributes")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float strength = 10f;
    [SerializeField] private float deceleration = 3f;

    [Header("VFX")]
    [SerializeField] private Transform FlareVFX;

    private VisualEffect flareTrail;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private int smokeVelocityID;
    private int smokeSizeID;

    private Color baseColor;
    private float totalLifetime;
    private float spawnTime;
    private float initialSmokeSize;
    private bool stopped;

    private void Awake()
    {
        flareTrail = FlareVFX.GetComponent<VisualEffect>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        smokeVelocityID = Shader.PropertyToID("SmokeVelocity");
        smokeSizeID = Shader.PropertyToID("SmokeSize");
    }

    private void FixedUpdate()
    {
        float currentSpeed = rb.linearVelocity.magnitude;

        if (currentSpeed > 0f)
        {
            float newSpeed = Mathf.Max(0f, currentSpeed - deceleration * Time.fixedDeltaTime);
            rb.linearVelocity = rb.linearVelocity.normalized * newSpeed;
        }

        if (currentSpeed <= 0f && !stopped)
        {
            flareTrail.Stop();
            stopped = true;
        }

        float elapsed = Time.time - spawnTime;
        float t = Mathf.Clamp01(elapsed / totalLifetime);

        // Fade sprite opacity over the total lifetime
        baseColor.a = 1f - t;
        spriteRenderer.color = baseColor;

        // Shrink smoke trail size linearly over the total lifetime
        flareTrail.SetFloat(smokeSizeID, initialSmokeSize * (1f - t));

        // Update smoke trail to trail behind flare movement
        if (currentSpeed > 0.1f)
        {
            Vector2 trailDir = -rb.linearVelocity.normalized;
            flareTrail.SetVector3(smokeVelocityID, new Vector3(trailDir.x, trailDir.y, 0f));
        }
    }

    public void Setup(Vector2 direction, Vector2 shipVelocity, ShipBase source)
    {
        rb.linearVelocity = direction.normalized * speed;
        baseColor = spriteRenderer.color;
        spawnTime = Time.time;

        float initialSpeed = rb.linearVelocity.magnitude;
        float timeToStop = initialSpeed / deceleration;
        float smokeLifetime = flareTrail.GetFloat(Shader.PropertyToID("SmokeLifetime"));
        totalLifetime = timeToStop + smokeLifetime;

        initialSmokeSize = flareTrail.GetFloat(smokeSizeID);
        flareTrail.SendEvent("OnDamage");
        Destroy(gameObject, totalLifetime);
    }

    public float GetStrength() => strength;
}
