using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using static State;

public abstract class AIControllerBase : MonoBehaviour
{
    public bool debug;

    [HideInInspector]
    public bool stopSearch { get; private set; }

    protected float nextTurn;
    protected float nextAdjust;
    protected float maxSpeed;
    protected float minSpeed;

    protected SceneManager sceneManager;
    protected List<ShipBase> attackingEnemies;
    protected IState currentState;
    protected Rigidbody2D targetRb;
    protected Vector2 targetAcceleration;
    protected Vector2 lastVelocity;

    private List<int> controllableGroupIndices;

    public List<ShipBase> enemyTeam { get; private set; }
    public GameObject attacker { get; private set; }
    public GameObject target { get; private set; }
    public List<WeaponsAAMissile> incomingMissiles { get; private set; }

    public AttackState attackState;
    public SearchState searchState;
    public ManeuverState maneuverState;

    [Header("Behaviors")]
    [SerializeField] public bool canHover = false;
    [SerializeField] public float faceEnemyAngle = 1; // * May have to change access of this in future
    [SerializeField] public float angularError = 5f;
    public float plasmaInaccuracy { get; private set; } // *** in the future make this solely for ship script, and instead of changing calculations try changing rotation of bullet as it is instantiated

    public ShipBase ship;
    public Rigidbody2D rb { get; private set; }

    // * Can avoid infinite circling by tracking last time you fired a shot while going after a target, if above certain threshold, then do a random maneuver
    protected virtual void Awake()
    {
        target = null;
        nextTurn = Random.Range(0, 2f);
        nextAdjust = Random.Range(0, 2f);
        stopSearch = false;
        attackingEnemies = new List<ShipBase>();
        incomingMissiles = new List<WeaponsAAMissile>();

        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Start()
    {
        sceneManager = SceneManager.Instance;
        enemyTeam = sceneManager.GetLiveEnemies(tag);

        // Initialize states
        attackState = new AttackState(7);
        searchState = new SearchState();
        maneuverState = new ManeuverState(2);

        maxSpeed = ship.GetShipMaxSpeed();
        minSpeed = ship.GetShipMinSpeed();
        ship.OnShipDamage += HandleDamageEvent;
        ship.OnSeekerFired += HandleSeekerFired;

        // Cache non-autonomous weapon group indices
        controllableGroupIndices = new List<int>();
        List<WeaponGroup> groups = ship.GetWeaponGroups();
        for (int i = 0; i < groups.Count; i++)
        {
            if (!groups[i].autonomous) controllableGroupIndices.Add(i);
        }

        currentState = searchState;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        UpdateTimers();

        // Transition to ManeuverState when missiles are incoming
        if (incomingMissiles.Count > 0 && currentState != maneuverState)
        {
            ChangeState(maneuverState);
        }

        currentState.UpdateState(this);
    }

    protected virtual void FixedUpdate()
    {
        enemyTeam = sceneManager.GetLiveEnemies(tag);
        currentState.FixedUpdateState(this);
    }

    /// <summary>
    /// Changes the current state of AI by calling the current state's exit function and calling the new state's enter function.
    /// </summary>
    /// <param name="newState">The new state to transition to.</param>
    public virtual void ChangeState(IState newState)
    {
        if (newState != null)
        {
            currentState.OnExit(this);
        }

        currentState = newState;
        if (debug) print(currentState);
        currentState.OnEnter(this);
    }

    /// <summary>
    /// Fires the AI's weapons at a given target by calculating the proper trajectory necessary for each weapon it attacks with.
    /// </summary>
    /// <param name="targetAcceleration">The acceleration vector of the target to be fired at.</param>
    /// <param name="angle">Kept for MoveToEngage compatibility but no longer used for arc check.</param>
    public virtual void AttackTarget(Vector2 targetAcceleration, float angle)
    {
        foreach (int groupIndex in controllableGroupIndices)
        {
            WeaponGroup group = ship.GetWeaponGroup(groupIndex);
            WeaponsBase repWeapon = group.GetRepresentativeWeapon();
            if (repWeapon == null) continue;

            List<Hardpoint> hps = group.GetHardpoints();
            Vector2 avgPos = Vector2.zero;
            foreach (Hardpoint hp in hps)
            {
                avgPos += (Vector2)hp.transform.position;
            }
            avgPos /= hps.Count;

            Vector2 aimPos = WorseTargetLeadingPosition(repWeapon, avgPos);
            ship.FireGroup(groupIndex, aimPos);
        }
    }

    // ** Change this to target enemies in specified collider, otherwise go towards radar signature once radar is added
    /// <summary>
    /// Attempts to find a valid target. A valid target consists of the ship closest to the AI on a different team.
    /// </summary>
    /// <returns>The target, if a valid target exists. Otherwise, it returns null.</returns>
    public virtual GameObject FindTarget()
    {
        float distance;
        float lowestDistance = Mathf.Infinity;
        enemyTeam = SceneManager.Instance.GetLiveEnemies(tag);

        if (enemyTeam.Count == 0)
        {
            stopSearch = true;
            return null;
        }

        foreach (ShipBase enemy in enemyTeam)
        {
            Vector2 posDiff = enemy.GetComponent<Rigidbody2D>().position - rb.position;
            distance = posDiff.sqrMagnitude;

            if (distance < lowestDistance)
            {
                lowestDistance = distance;
                target = enemy.gameObject;
            }
        }

        return target;
    }

    protected virtual void HandleDamageEvent(object sender, ShipBase attacker)
    {
        this.attacker = attacker.gameObject;
        currentState.OnHurt(this);
    }

    protected virtual void HandleSeekerFired(object sender, WeaponsBase seeker)
    {
        seeker.SetTarget(target);
    }

    /// <summary>
    /// Attempts to find another target when the current target is destroyed and unsubscribes from the destroyed target.
    /// </summary>
    /// <param name="sender">The ship sending the death event.</param>
    /// <param name="e"></param>
    public virtual void HandleTargetDeath(object sender, EventArgs e)
    {
        ShipBase shipBase = (ShipBase)sender;
        target = FindTarget();
        if (target != null)
        {
            target.GetComponent<ShipBase>().OnShipDeath += HandleTargetDeath;
        }

        shipBase.OnShipDeath -= HandleTargetDeath;
    }

    /// <summary>
    /// Makes the AI turn and accelerate/decelerate towards the current target to place it within the ship's field of fire. (Old method)
    /// </summary>
    public virtual void MoveToEngage(float angle)
    {
        float turn = 0f;
        float speed = ship.GetShipSpeed();

        if (target != null && !stopSearch) // ** Not sure if target check is needed here anymore
        {
            // Turning logic
            if (Mathf.Abs(angle) > faceEnemyAngle && nextTurn <= 0)
            {
                if (angle > 0) turn = 1f;

                if (angle < 0) turn = -1f;

            }
            else if (Mathf.Abs(angle) < faceEnemyAngle && nextTurn <= 0)
            {
                nextTurn = Random.Range(0, 2f);
            }

            // Acceleration logic
            if (nextAdjust <= 0)
            {

                if (Math.Abs(angle) < 70)
                {
                    ship.Accelerate(0.2f);
                }
                else if (Math.Abs(angle) >= 90)
                {
                    ship.Decelerate(0.2f);
                }
                else
                {
                    nextAdjust = Random.Range(0, 0.2f);
                }
            }
        }

        ship.SetShipTurn(turn);
    }

    public virtual void MoveToEvade(float angle)
    {
        float turn = 0f;

        if (Math.Abs(angle) > 5f)
        {
            if (angle > 0) turn = 1f;

            if (angle < 0) turn = -1f;
        }

        ship.Accelerate(0.2f);
        ship.SetShipTurn(turn);
    }

    public void AddIncomingMissile(WeaponsAAMissile missile)
    {
        if (!incomingMissiles.Contains(missile))
        {
            incomingMissiles.Add(missile);
        }
    }

    public void RemoveIncomingMissile(WeaponsAAMissile missile)
    {
        incomingMissiles.Remove(missile);
    }

    /// <summary>
    /// Updates all the timers.
    /// </summary>
    protected virtual void UpdateTimers()
    {
        if (nextTurn > 0) nextTurn -= Time.deltaTime;

        if (nextAdjust > 0) nextAdjust -= Time.deltaTime;
    }

    private void OnDisable()
    {
        ship.OnShipDamage -= HandleDamageEvent;
        ship.OnSeekerFired -= HandleSeekerFired;
    }

    public void SetTarget(GameObject newTarget)
    {
        if (newTarget != null && newTarget != target)
        {
            target = newTarget;
        }
    }

    // ===== Leading target calculations =====
    public virtual Vector2 WorseTargetLeadingPosition(WeaponsBase weapon, Vector2 gunPosition)
    {
        targetRb = target.GetComponent<Rigidbody2D>();
        float speed = weapon.GetSpeed();
        float distance = Vector2.Distance(gunPosition, targetRb.position);
        float travelTime = distance / speed;
        return targetRb.position + targetRb.linearVelocity * travelTime;
    }


    /// <summary>
    /// Gets the angle between the current ship's forward direction and the direction to the target's ship.
    /// </summary>
    /// <returns>A signed angle.</returns>
    public virtual float GetAngleToTarget()
    {
        targetRb = target.GetComponent<Rigidbody2D>();
        Vector2 targetDirection = targetRb.position - rb.position;

        return Vector2.SignedAngle((Vector2)transform.up, targetDirection);
    }

    /// <summary>
    /// Gets the angle between the ship's forward direction and the direction to the specified destination.
    /// </summary>
    /// <param name="dest"></param>
    /// <returns>A signed angle.</returns>
    public virtual float GetAngleToDestination(Vector2 dest)
    {
        return Vector2.SignedAngle((Vector2)transform.up, dest);
    }

    /// <summary>
    /// Calculates the acceleration of the target.
    /// </summary>
    /// <returns>A vector signifying the target's acceleration.</returns>
    public virtual Vector2 CalculateTargetAcceleration()
    {
        targetRb = target.GetComponent<Rigidbody2D>();
        Vector2 targetAcceleration = (targetRb.linearVelocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = targetRb.linearVelocity;

        return targetAcceleration;
    }

    /// <summary>
    /// Calculates the proper lead for the current target with the current target's acceleration and the specified weapon's projectile speed.
    /// </summary>
    /// <param name="targetAcceleration">The acceleration of the current target</param>
    /// <param name="iterations">The number of iterations to run the calculations. More iterations increase accuracy and use more resources.</param>
    /// <param name="weapon">The weapon to shoot the current target with.</param>
    /// <returns>A vector describing the required lead to hit the target.</returns>
    protected virtual Vector2 GetTargetLeadingPosition(Vector2 targetAcceleration, int iterations, WeaponsBase weapon)
    {
        targetRb = target.GetComponent<Rigidbody2D>();

        float s = weapon.GetSpeed();
        float distance = Vector2.Distance(targetRb.position, rb.position);

        // setup formula constants
        Vector2 pT = targetRb.position - rb.position;
        Vector2 vT = targetRb.linearVelocity - rb.linearVelocity;
        Vector2 aT = targetAcceleration;
        Vector2 aP = Vector2.zero;

        Vector2 accel = aT - aP;

        // Guess the time to target
        float guess = distance / s;

        if (iterations > 0)
        {
            // Quartic coefficients
            float a = Vector2.Dot(accel, accel) * 0.25f;
            float b = Vector2.Dot(accel, vT);
            float c = Vector2.Dot(accel, pT) + Vector2.Dot(vT, vT) - s * s;
            float d = 2f * Vector2.Dot(vT, vT);
            float e = Vector3.Dot(pT, pT);

            // Solve with Newton's equation
            float finalGuess = SolveQuarticNewton(guess, iterations, a, b, c, d, e);

            // Use the first guess if negative or zero
            if (finalGuess > 0f)
            {
                guess = finalGuess;
            }
        }

        Vector2 travel = pT + vT * guess + 0.5f * aT * guess * guess;
        return rb.position + travel;
    }

    protected virtual float SolveQuarticNewton(float guess, int iterations, float a, float b, float c, float d, float e)
    {
        for (int i = 0; i < iterations; i++)
        {
            guess = guess - EvalQuartic(guess, a, b, c, d, e) / EvalQuarticDerivative(guess, a, b, c, d);
        }
        return guess;
    }

    protected virtual float EvalQuartic(float t, float a, float b, float c, float d, float e)
    {
        return a * t * t * t * t + b * t * t * t + c * t * t + d * t + e;
    }

    protected virtual float EvalQuarticDerivative(float t, float a, float b, float c, float d)
    {
        return 4f * a * t * t * t + 3f * b * t * t + 2f * c * t + d;
    }
}
