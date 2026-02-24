using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponsAAMissile : WeaponsBase
{
    [Header("Proportional Navigation")]
    [SerializeField] private float navigationGain = 4f;
    [SerializeField] private float boostAcceleration = 30f;

    [Header("One Turn")]
    [SerializeField] private float oneTurnDelay = 1f;
    [SerializeField] private float oneTurnAngleThreshold = 10f;
    [SerializeField] private float turnAcceleration = 50f;

    [Header("Fuel")]
    [SerializeField] private float totalFuel = 100f;
    [SerializeField] private float thrustFuelRate = 20f;
    [SerializeField] private float rcsFuelRate = 15f;
    
    [Header("Team Trail Colors")]
    [SerializeField] private Color blueTeamTrailStart = new Color(0.1f, 0.73f, 1f, 1f);
    [SerializeField] private Color blueTeamTrailEnd = new Color(0.35f, 0.68f, 0.82f, 1f);
    [SerializeField] private Color redTeamTrailStart = new Color(1f, 0.5f, 0.1f, 1f);
    [SerializeField] private Color redTeamTrailEnd = new Color(0.82f, 0.45f, 0.2f, 1f);

    private float previousLOSAngle;
    private bool hasPreviousLOS;
    private float currentSpeed;
    private float fuelRemaining;
    private bool hasLock;
    private bool oneTurnComplete;
    private float oneTurnTimer;
    private float currentTurnSpeed;
    private Vector2 missileVelocity;

    private GameObject previousTarget;

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
        if (source == null && hasLock)
        {
            hasLock = false;
            if (previousTarget != null)
            {
                previousTarget.GetComponent<AIControllerBase>()?.RemoveIncomingMissile(this);
                previousTarget = null;
            }
        }

        // Motor thrust — burn fuel while motor has fuel
        if (fuelRemaining > 0f)
        {
            float fuelCost = Mathf.Min(thrustFuelRate * dt, fuelRemaining);

            // Accelerate toward max speed (don't clamp down inherited velocity)
            if (currentSpeed < speed)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, speed, boostAcceleration * dt);
            }
            else
            {
                // Inherited velocity above max speed — let it decay toward max
                currentSpeed = Mathf.MoveTowards(currentSpeed, speed, boostAcceleration * dt);
            }

            fuelRemaining -= fuelCost;
        }

        // Kill engine trail when fuel is depleted
        if (fuelRemaining <= 0f && engineTrail != null && engineTrail.emitting)
        {
            engineTrail.emitting = false;
        }

        // Guidance
        if (hasLock && target != null)
        {
            Vector2 targetPos = target.transform.position;
            Vector2 missilePos = rb.position;
            Vector2 los = targetPos - missilePos;
            float losAngle = Mathf.Atan2(los.y, los.x);

            // Initiate One Turn phase after specified delay
            if (!oneTurnComplete)
            {
                oneTurnTimer += dt;
                if (oneTurnTimer < oneTurnDelay)
                {
                    // Coast during delay before one-turn begins
                    missileVelocity = missileVelocity.normalized * currentSpeed;
                }
                else
                {
                    // Check if one-turn phase is complete
                    float velAngle = Mathf.Atan2(missileVelocity.y, missileVelocity.x) * Mathf.Rad2Deg;
                    float losAngleDeg = losAngle * Mathf.Rad2Deg;
                    float angleDiff = Mathf.Abs(Mathf.DeltaAngle(velAngle, losAngleDeg));

                    if (angleDiff <= oneTurnAngleThreshold)
                    {
                        oneTurnComplete = true;
                    }
                    else
                    {
                        // One-turn phase: direct pursuit — ramp up turn rate
                        currentTurnSpeed = Mathf.MoveTowards(currentTurnSpeed, turnSpeed, turnAcceleration * dt);

                        Vector2 velDir = missileVelocity.normalized;
                        Vector2 losDir = los.normalized;
                        Vector2 perpDir = new Vector2(-velDir.y, velDir.x);

                        // Project LOS onto perpendicular to get turn direction
                        // Scale lateral acceleration by speed ratio so angular turn rate is consistent
                        float turnDirection = Vector2.Dot(losDir, perpDir);
                        float speedRatio = speed > 0f ? currentSpeed / speed : 0f;
                        float clampedAccel = Mathf.Sign(turnDirection) * currentTurnSpeed * speedRatio;

                        // RCS fuel cost
                        float absAccel = Mathf.Abs(clampedAccel);
                        float rcsCost = rcsFuelRate * absAccel * dt;
                        if (rcsCost > fuelRemaining)
                        {
                            float scaleFactor = fuelRemaining / rcsCost;
                            clampedAccel *= scaleFactor;
                            rcsCost = fuelRemaining;
                        }

                        fuelRemaining -= rcsCost;

                        missileVelocity += perpDir * clampedAccel * dt;
                        missileVelocity = missileVelocity.normalized * currentSpeed;

                        // Reset PN state so it starts fresh after one-turn completes
                        hasPreviousLOS = false;
                    }
                }
            }
            else if (hasPreviousLOS)
            {
                // PN guidance
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
        gameObject.tag = source.tag;

        // Set trail color based on team
        if (engineTrail != null)
        {
            Gradient gradient = new Gradient();
            Color startColor = source.CompareTag("Blue") ? blueTeamTrailStart : redTeamTrailStart;
            Color endColor = source.CompareTag("Blue") ? blueTeamTrailEnd : redTeamTrailEnd;
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(startColor, 0f), new GradientColorKey(endColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
            engineTrail.colorGradient = gradient;
        }

        missileVelocity = shipVelocity;
        currentSpeed = missileVelocity.magnitude;
        fuelRemaining = totalFuel;
        hasLock = true;
        hasPreviousLOS = false;
        oneTurnComplete = false;
        oneTurnTimer = 0f;
        currentTurnSpeed = 0f;
        Destroy(gameObject, lifetime);
    }

    public override float GetSpeed()
    {
        return currentSpeed;
    }

    public override void SetTarget(GameObject newTarget)
    {
        // Remove self from previous target's incoming list
        if (previousTarget != null)
        {
            previousTarget.GetComponent<AIControllerBase>()?.RemoveIncomingMissile(this);
        }

        base.SetTarget(newTarget);
        previousTarget = newTarget;

        // Notify new target's AI that a missile is tracking it
        if (target != null)
        {
            target.GetComponent<AIControllerBase>()?.AddIncomingMissile(this);
        }
    }

    public GameObject GetTarget()
    {
        return target;
    }

    public float GetFuelFraction()
    {
        return totalFuel > 0f ? fuelRemaining / totalFuel : 0f;
    }

    private void OnDestroy()
    {
        if (previousTarget != null)
        {
            previousTarget.GetComponent<AIControllerBase>()?.RemoveIncomingMissile(this);
        }
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
