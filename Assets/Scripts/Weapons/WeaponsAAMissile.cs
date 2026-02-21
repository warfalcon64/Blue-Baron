using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponsAAMissile : WeaponsBase
{
    [Header("Proportional Navigation")]
    [SerializeField] private float navigationGain = 4f;
    [SerializeField] private float boostAcceleration = 30f;

    [Header("Fuel")]
    [SerializeField] private float totalFuel = 100f;
    [SerializeField] private float thrustFuelRate = 20f;
    [SerializeField] private float rcsFuelRate = 15f;

    private float previousLOSAngle;
    private bool hasPreviousLOS;
    private float currentSpeed;
    private float fuelRemaining;
    private bool hasLock;
    private Vector2 missileVelocity;

    private VFXManager vfxManager;
    private Rigidbody2D rb;
    private TrailRenderer engineTrail;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        vfxManager = SceneManager.Instance.vfxManager.GetComponent<VFXManager>();
        engineTrail = GetComponentInChildren<TrailRenderer>();
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // Source ship check — lose lock permanently if source is destroyed
        if (source == null)
        {
            hasLock = false;
        }

        // Motor thrust — burn fuel while motor has fuel
        if (fuelRemaining > 0f)
        {
            float fuelCost = thrustFuelRate * dt;
            if (fuelCost > fuelRemaining)
                fuelCost = fuelRemaining;

            // Only accelerate if below max speed
            if (currentSpeed < speed)
            {
                float fuelFraction = fuelCost / (thrustFuelRate * dt);
                currentSpeed += boostAcceleration * fuelFraction * dt;
                if (currentSpeed > speed)
                    currentSpeed = speed;
            }

            fuelRemaining -= fuelCost;
        }

        // Kill engine trail when fuel is depleted
        if (fuelRemaining <= 0f && engineTrail != null && engineTrail.emitting)
        {
            engineTrail.emitting = false;
        }

        // PN guidance
        if (hasLock && target != null)
        {
            Vector2 targetPos = target.transform.position;
            Vector2 missilePos = rb.position;
            Vector2 los = targetPos - missilePos;
            float losAngle = Mathf.Atan2(los.y, los.x);

            if (hasPreviousLOS)
            {
                float losRate = Mathf.DeltaAngle(previousLOSAngle * Mathf.Rad2Deg, losAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad / dt;

                // Closing speed (positive when closing)
                Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
                Vector2 targetVel = targetRb != null ? targetRb.velocity : Vector2.zero;
                Vector2 relVel = missileVelocity - targetVel;
                Vector2 losDir = los.normalized;
                float closingSpeed = Mathf.Abs(Vector2.Dot(relVel, losDir));

                // PN commanded lateral acceleration: a = N * Vc * losRate
                float lateralAccelMag = navigationGain * closingSpeed * losRate;

                // Perpendicular to missile velocity (left-hand normal in 2D)
                Vector2 velDir = missileVelocity.normalized;
                Vector2 perpDir = new Vector2(-velDir.y, velDir.x);

                // Clamp to max lateral accel (turnSpeed from WeaponsBase)
                float clampedAccel = Mathf.Clamp(lateralAccelMag, -turnSpeed, turnSpeed);

                // RCS fuel cost
                float absAccel = Mathf.Abs(clampedAccel);
                float rcsCost = rcsFuelRate * absAccel * dt;
                if (rcsCost > fuelRemaining)
                {
                    // Scale down proportionally
                    float scaleFactor = fuelRemaining / rcsCost;
                    clampedAccel *= scaleFactor;
                    absAccel *= scaleFactor;
                    rcsCost = fuelRemaining;
                }

                fuelRemaining -= rcsCost;

                // Apply lateral acceleration to velocity
                missileVelocity += perpDir * clampedAccel * dt;

                // Re-normalize to currentSpeed (PN changes direction, not speed)
                missileVelocity = missileVelocity.normalized * currentSpeed;
            }

            previousLOSAngle = losAngle;
            hasPreviousLOS = true;
        }
        else
        {
            // No lock — coast ballistically, keep velocity direction
            missileVelocity = missileVelocity.normalized * currentSpeed;
        }

        // Update rotation to face velocity direction
        if (missileVelocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(missileVelocity.y, missileVelocity.x) * Mathf.Rad2Deg - 90f;
            rb.rotation = angle;
        }

        rb.velocity = missileVelocity;
    }

    public override void Setup(Vector2 shootDirection, Vector2 shipVelocity, ShipBase source)
    {
        this.source = source;
        missileVelocity = shipVelocity;
        currentSpeed = missileVelocity.magnitude;
        fuelRemaining = totalFuel;
        hasLock = true;
        hasPreviousLOS = false;
        Destroy(gameObject, lifetime);
    }

    public override float GetSpeed()
    {
        return currentSpeed;
    }

    public GameObject GetTarget()
    {
        return target;
    }

    public float GetFuelFraction()
    {
        return totalFuel > 0f ? fuelRemaining / totalFuel : 0f;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Rigidbody2D>() != null && !collision.CompareTag(tag))
        {
            Vector3 hitPos = transform.position;
            vfxManager.PlayVFX(VFXManager.VFXType.Explosion, hitPos);
            Destroy(gameObject);
        }
    }
}
