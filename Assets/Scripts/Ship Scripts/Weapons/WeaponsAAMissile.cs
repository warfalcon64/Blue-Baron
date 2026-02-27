using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponsAAMissile : WeaponsBase
{
    [Header("Radar Seeker")]
    [SerializeField] private float seekerHalfAngle = 30f;
    [SerializeField] private float seekerSweepRate = 20f;
    [SerializeField] private float targetRadarSignature = 5f;
    [SerializeField] private float lockHoldTime = 0.5f;
    [SerializeField] private LayerMask seekerLayerMask;

    [Header("One Turn")]
    [SerializeField] private float oneTurnDelay = 1f;
    [SerializeField] private float oneTurnAngleThreshold = 10f;
    [SerializeField] private float turnAcceleration = 50f;

    [Header("Proportional Navigation")]
    [SerializeField] private float navigationGain = 4f;
    [SerializeField] private float boostAcceleration = 30f;

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

    private bool oneTurnComplete;
    private float oneTurnTimer;
    private float currentTurnSpeed;
    private Vector2 missileVelocity;

    private GameObject previousTarget;

    private float seekerCurrentAngle;
    private bool seekerSweepRight = true;
    private float lockHoldTimer;
    private bool hadTargetLastFrame;

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

        if (source == null && !oneTurnComplete)
        {
            oneTurnComplete = true;
            hasPreviousLOS = false;
        }

        if (fuelRemaining > 0f)
        {
            float fuelCost = Mathf.Min(thrustFuelRate * dt, fuelRemaining);

            currentSpeed = Mathf.MoveTowards(currentSpeed, speed, boostAcceleration * dt);

            fuelRemaining -= fuelCost;
        }

        if (fuelRemaining <= 0f && engineTrail != null && engineTrail.emitting)
        {
            engineTrail.emitting = false;
        }

        if (oneTurnComplete && fuelRemaining > 0f)
        {
            UpdateSeekerSweep(dt);
        }

        // Guidance
        if (target != null)
        {
            Vector2 los = (Vector2)target.transform.position - rb.position;
            float losAngle = Mathf.Atan2(los.y, los.x);

            if (!oneTurnComplete)
                UpdateOneTurn(dt, los, losAngle);
            else if (hasPreviousLOS)
                UpdateProNav(dt, los, losAngle);

            previousLOSAngle = losAngle;
            hasPreviousLOS = true;
            hadTargetLastFrame = true;
        }
        else
        {
            if (hadTargetLastFrame)
            {
                Debug.Log($"[Missile] Lost target, coasting ballistically (fuel: {fuelRemaining:F1})");
                hadTargetLastFrame = false;
            }
            missileVelocity = missileVelocity.normalized * currentSpeed;
        }

        // Update rotation to face velocity direction
        if (missileVelocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(missileVelocity.y, missileVelocity.x) * Mathf.Rad2Deg - 90f;
            rb.rotation = angle;
        }

        rb.linearVelocity = missileVelocity;
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
        hasPreviousLOS = false;
        oneTurnComplete = false;
        oneTurnTimer = 0f;
        currentTurnSpeed = 0f;
        Destroy(gameObject, lifetime);
    }


    private void UpdateOneTurn(float dt, Vector2 los, float losAngle)
    {
        oneTurnTimer += dt;
        if (oneTurnTimer < oneTurnDelay)
        {
            missileVelocity = missileVelocity.normalized * currentSpeed;
            return;
        }

        // Check if one-turn phase is complete
        float velAngle = Mathf.Atan2(missileVelocity.y, missileVelocity.x) * Mathf.Rad2Deg;
        float losAngleDeg = losAngle * Mathf.Rad2Deg;
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(velAngle, losAngleDeg));

        if (angleDiff <= oneTurnAngleThreshold)
        {
            Debug.Log($"[Missile] One-turn complete, switching to seeker/PN phase");
            oneTurnComplete = true;
            return;
        }

        // Direct pursuit, ramp up turn rate
        currentTurnSpeed = Mathf.MoveTowards(currentTurnSpeed, turnSpeed, turnAcceleration * dt);

        Vector2 velDir = missileVelocity.normalized;
        Vector2 losDir = los.normalized;
        Vector2 perpDir = new Vector2(-velDir.y, velDir.x);

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

    private void UpdateProNav(float dt, Vector2 los, float losAngle)
    {
        float losRate = Mathf.DeltaAngle(previousLOSAngle * Mathf.Rad2Deg, losAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad / dt;

        // Closing speed (positive when closing)
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        Vector2 targetVel = targetRb != null ? targetRb.linearVelocity : Vector2.zero;
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

    private void UpdateSeekerSweep(float dt)
    {
        if (lockHoldTimer > 0f)
            lockHoldTimer -= dt;

        float sweepDelta = seekerSweepRate * dt;
        if (seekerSweepRight)
        {
            seekerCurrentAngle += sweepDelta;
            if (seekerCurrentAngle >= seekerHalfAngle)
            {
                seekerCurrentAngle = seekerHalfAngle;
                seekerSweepRight = false;
            }
        }
        else
        {
            seekerCurrentAngle -= sweepDelta;
            if (seekerCurrentAngle <= -seekerHalfAngle)
            {
                seekerCurrentAngle = -seekerHalfAngle;
                seekerSweepRight = true;
            }
        }

        // Cast ray in swept direction
        Vector2 velDir = missileVelocity.normalized;
        Vector2 rayDir = (Vector2)(Quaternion.Euler(0f, 0f, seekerCurrentAngle) * velDir);

        float rayLength;
        if (target != null)
            rayLength = Vector2.Distance(rb.position, (Vector2)target.transform.position);
        else
            rayLength = speed * lifetime;

        RaycastHit2D hit = Physics2D.Raycast(rb.position, rayDir, rayLength, seekerLayerMask);
        if (hit.collider == null)
            return;

        if (target != null && hit.collider.gameObject == target)
            return;

        float detectedSignal;
        Flare flare = hit.collider.GetComponent<Flare>();
        if (flare != null)
            detectedSignal = flare.GetChaffStrength();
        else if (hit.collider.GetComponent<ShipBase>() != null)
            detectedSignal = targetRadarSignature;
        else
            return;

        float currentSignal = 0f;
        if (target != null)
        {
            Flare currentFlare = target.GetComponent<Flare>();
            if (currentFlare != null)
                currentSignal = currentFlare.GetChaffStrength();
            else if (target.GetComponent<ShipBase>() != null)
                currentSignal = targetRadarSignature;
        }

        if (detectedSignal > currentSignal && lockHoldTimer <= 0f)
        {
            if (previousTarget != null)
            {
                AIControllerBase prevAI = previousTarget.GetComponent<AIControllerBase>();
                if (prevAI != null)
                    prevAI.RemoveIncomingMissile(this);
            }

            bool wasFlare = flare != null;
            target = hit.collider.gameObject;
            previousTarget = target;

            if (wasFlare)
                Debug.Log($"[Missile] Locked onto flare (chaff strength: {detectedSignal:F1}) over previous target (signal: {currentSignal:F1})");
            else
                Debug.Log($"[Missile] Locked onto ship {target.name} (signal: {detectedSignal:F1}) over previous target (signal: {currentSignal:F1})");

            if (target.TryGetComponent<AIControllerBase>(out AIControllerBase newAI))
                newAI.AddIncomingMissile(this);

            // Reset PN state so guidance restarts fresh toward new target
            hasPreviousLOS = false;

            lockHoldTimer = lockHoldTime;
        }
    }

    public override float GetSpeed()
    {
        return currentSpeed;
    }

    public override void SetTarget(GameObject newTarget)
    {
        if (previousTarget != null)
        {
            AIControllerBase prevAI = previousTarget.GetComponent<AIControllerBase>();
            if (prevAI != null)
                prevAI.RemoveIncomingMissile(this);
        }

        base.SetTarget(newTarget);
        previousTarget = newTarget;

        if (target != null)
        {
            AIControllerBase targetAI = target.GetComponent<AIControllerBase>();
            if (targetAI != null)
                targetAI.AddIncomingMissile(this);
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

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !oneTurnComplete)
            return;

        Vector2 velDir = missileVelocity.normalized;
        float rayLength = target != null
            ? Vector2.Distance(rb.position, (Vector2)target.transform.position)
            : speed * lifetime;

        // Draw cone edges
        Vector2 leftDir = (Vector2)(Quaternion.Euler(0f, 0f, seekerHalfAngle) * velDir);
        Vector2 rightDir = (Vector2)(Quaternion.Euler(0f, 0f, -seekerHalfAngle) * velDir);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(rb.position, rb.position + leftDir * rayLength);
        Gizmos.DrawLine(rb.position, rb.position + rightDir * rayLength);

        // Draw current sweep ray
        Vector2 sweepDir = (Vector2)(Quaternion.Euler(0f, 0f, seekerCurrentAngle) * velDir);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(rb.position, rb.position + sweepDir * rayLength);
    }

    private void OnDestroy()
    {
        if (previousTarget != null)
        {
            AIControllerBase prevAI = previousTarget.GetComponent<AIControllerBase>();
            if (prevAI != null)
                prevAI.RemoveIncomingMissile(this);
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
