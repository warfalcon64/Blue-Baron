using UnityEngine;

public class WeaponsPlasma : ManagedProjectile
{
    [Header("Non-physical hit detection")]
    [Tooltip("Layers tested for hits (ships). If left empty, defaults to the \"Ships\" layer at runtime.")]
    [SerializeField] private LayerMask hitLayers;
    [Tooltip("Cast radius approximating the bolt's half-width (prefab capsule was ~0.08 wide).")]
    [SerializeField] private float castRadius = 0.08f;

    private Vector2 velocity;
    private float expireTime;

    public override ObjectPoolManager.PoolType PoolType => ObjectPoolManager.PoolType.Plasma;

    private void Awake()
    {
        if (hitLayers == 0)
            hitLayers = LayerMask.GetMask("Ships");
    }

    public override void Setup(Vector2 shootDirection, Vector2 shipVelocity, ShipBase source)
    {
        shootDirection = shootDirection.normalized;
        velocity = (shootDirection * speed) + shipVelocity;
        this.source = source;
        expireTime = Time.time + lifetime;

        EnterManager();
    }

    public override bool Step(float deltaTime)
    {
        if (Time.time >= expireTime)
            return false;

        Vector2 pos = transform.position;
        Vector2 step = velocity * deltaTime;
        float dist = step.magnitude;

        if (dist > 0f)
        {
            Vector2 dir = step / dist;
            // Single nearest hit along the travel segment; struct return, no allocation.
            RaycastHit2D hit = Physics2D.CircleCast(pos, castRadius, dir, dist, hitLayers);
            Rigidbody2D hitRb = hit.rigidbody;
            if (hitRb != null)
            {
                ShipBase ship = hitRb.GetComponent<ShipBase>();
                if (ship != null && !ship.CompareTag(tag))
                {
                    ship.ApplyWeaponDamage(this);
                    SpawnImpactVFX();
                    return false;
                }
            }
        }

        transform.position = pos + step;
        return true;
    }

    public override float GetSpeed() => base.GetSpeed();

    public override float GetDamage() => base.GetDamage();

    public override float GetRange() => base.GetRange();
}
