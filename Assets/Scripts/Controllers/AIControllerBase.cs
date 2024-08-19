using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public abstract class AIControllerBase : MonoBehaviour
{
    public bool debug;

    protected bool stopSearch;

    protected float nextTurn;
    protected float nextAdjust;
    protected float maxSpeed;
    protected float minSpeed;
    protected float primaryFieldofFire;
    protected float primaryCooldown;

    protected SceneManager sceneManager;
    protected List<ShipBase> enemyTeam;
    protected GameObject target;
    protected Rigidbody2D targetRb;
    protected WeaponMap weaponMap;
    protected WeaponsBase primary;
    protected Vector2 targetAcceleration;
    protected Vector2 lastVelocity;

    [Header("Behaviors")]
    [SerializeField] protected float faceEnemyAngle = 1;
    [SerializeField] protected float plasmaInaccuracy; // *** in the future make this solely for ship script, and instead of changing calculations try changing rotation of bullet as it is instantiated

    public ShipBase ship;

    private Rigidbody2D rb;

    // * Can avoid infinite circling by tracking last time you fired a shot while going after a target, if above certain threshold, then do a random maneuver

    protected virtual void Start()
    {
        sceneManager = SceneManager.Instance;
        enemyTeam = sceneManager.GetLiveEnemies(tag);
        
        maxSpeed = ship.GetShipMaxSpeed();
        minSpeed = ship.GetShipMinSpeed();

        weaponMap = ship.GetWeaponMap();
        primary = weaponMap.GetWeapon(ShootType.Primary);
        primaryCooldown = primary.GetCoolDown();
    }


    protected virtual void Awake()
    {
        target = null;
        primaryFieldofFire = ship.GetPrimaryFieldOfFire();
        nextTurn = Random.Range(0, 2f);
        nextAdjust = Random.Range(0, 2f);
        stopSearch = false;

        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        UpdateTimers();
    }

    protected virtual void FixedUpdate()
    {
        if (target != null && !stopSearch)
        {
            float angle = Math.Abs(GetAngleToTarget());
            targetRb = target.GetComponent<Rigidbody2D>();

            Vector2 posDiff = targetRb.position - rb.position;
            float distance = Mathf.Sqrt(posDiff.sqrMagnitude);

            // Adding inaccuracy because the calculations are too accurate and precise for players to dodge ** <-- change this to adding randomness to the bullets created instead
            Vector2 inaccuracy = (new Vector2(Random.Range(-plasmaInaccuracy, plasmaInaccuracy), 
            Random.Range(-plasmaInaccuracy, plasmaInaccuracy)) * (1 / distance));

            targetAcceleration = CalculateTargetAcceleration() + inaccuracy;
            AttackTarget(targetAcceleration, angle);
            
        }
        else if (!stopSearch)
        {
            target = FindTarget();
            target.GetComponent<ShipBase>().OnShipDeath += HandleTargetDeath;
        }

        Move();
    }

    protected virtual void AttackTarget(Vector2 targetAcceleration, float angle)
    {
        if (primaryCooldown <= 0 && angle <= primaryFieldofFire)
        {
            Vector2 aimPos = GetTargetLeadingPosition(targetAcceleration, 0, primary);
            ship.ShootPrimary(aimPos);
            primaryCooldown = primary.GetCoolDown();
        }
        // * ADD CODE FOR SECONDARY STUFF HERE
        else
        {
            return;
        }

        
    }

    // ** Change this to target enemies in specified collider, otherwise go towards radar signature once radar is added
    protected virtual GameObject FindTarget()
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

    protected virtual void HandleTargetDeath(object sender, EventArgs e)
    {
        ShipBase shipBase = (ShipBase)sender;
        target = FindTarget();
        if (target != null)
        {
            target.GetComponent<ShipBase>().OnShipDeath += HandleTargetDeath;
        }

        shipBase.OnShipDeath -= HandleTargetDeath;
    }

    protected virtual void Move()
    {
        float turn = 0f;
        float speed = ship.GetShipSpeed();

        if (target != null && !stopSearch)
        {
            float angle = GetAngleToTarget();

            // Turning logic
            if (Mathf.Abs(angle) > faceEnemyAngle && nextTurn <= 0)
            {
                //print("NEXTTURN IS: " + nextTurn);
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
                if (debug)
                {
                    print(Math.Abs(angle));
                }

                if (Math.Abs(angle) < 70)
                {
                    //print("INCREASING SPEED");
                    //speed += 0.2f;
                    ship.Accelerate(0.2f);
                }
                else if (Math.Abs(angle) >= 90)
                {
                    //speed -= 0.2f;
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

    protected virtual void UpdateTimers()
    {
        if (nextTurn > 0) nextTurn -= Time.deltaTime;

        if (nextAdjust > 0) nextAdjust -= Time.deltaTime;

        if (primaryCooldown > 0) primaryCooldown -= Time.deltaTime;
    }

    // ===== Leading target calculations =====
    protected virtual float GetAngleToTarget()
    {
        targetRb = target.GetComponent<Rigidbody2D>();
        Vector2 targetDirection = targetRb.position - rb.position;

        return Vector2.SignedAngle((Vector2)transform.up, targetDirection);
    }

    protected virtual Vector2 CalculateTargetAcceleration()
    {
        targetRb = target.GetComponent<Rigidbody2D>();
        Vector2 targetAcceleration = (targetRb.velocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = targetRb.velocity;

        return targetAcceleration;
    }

    protected virtual Vector2 GetTargetLeadingPosition(Vector2 targetAcceleration, int iterations, WeaponsBase weapon)
    {
        targetRb = target.GetComponent<Rigidbody2D>();

        float s = weapon.GetSpeed(); // * maybe add ship speed somehow? <-- why does this matter?
        float distance = Vector2.Distance(targetRb.position, rb.position);

        // setup formula constants
        Vector2 pT = targetRb.position - rb.position;
        Vector2 vT = targetRb.velocity - rb.velocity;
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
            float e = Vector3.Dot(pT, pT); // ** Check if this is supposed to be Vector3, kinda sus

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
