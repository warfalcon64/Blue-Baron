using UnityEngine;
using UnityEngine.VFX;

public class Flare : MonoBehaviour
{
    [Header("Attributes")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float strength = 10f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float deceleration = 3f;

    [Header("VFX")]
    [SerializeField] private Transform FlareVFX;

    private VisualEffect flareTrail;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private int smokeVelocityID;

    private void Awake()
    {
        flareTrail = FlareVFX.GetComponent<VisualEffect>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        smokeVelocityID = Shader.PropertyToID("SmokeVelocity");
    }

    private void FixedUpdate()
    {
        // Decelerate over time
        float currentSpeed = rb.linearVelocity.magnitude;
        if (currentSpeed > 0f)
        {
            float newSpeed = Mathf.Max(0f, currentSpeed - deceleration * Time.fixedDeltaTime);
            rb.linearVelocity = rb.linearVelocity.normalized * newSpeed;
        }

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

        flareTrail.SendEvent("OnDamage");
        Destroy(gameObject, lifetime);
    }

    public float GetStrength() => strength;
}
