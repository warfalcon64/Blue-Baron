using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public abstract class AIControllerBase : MonoBehaviour
{
    protected bool stopSearch;

    protected float nextTurn;
    protected float nextAdjust;
    protected float maxSpeed;
    protected float minSpeed;
    protected float fieldOfFire;

    protected List<ShipBase> enemyTeam;
    protected GameObject target;
    protected Rigidbody2D targetRb;
    Vector2 targetAcceleration;
    protected Vector2 lastVelocity;

    [Header("Behaviors")]
    [SerializeField] protected float faceEnemyAngle = 1;
    [SerializeField] protected float plasmaInaccuracy;

    public ShipBase ship;

    public event EventHandler OnShipDeath;

    private Rigidbody2D rb;

    protected virtual void Awake()
    {
        enemyTeam = SceneManager.Instance.GetLiveEnemies(tag);

        maxSpeed = ship.GetShipMaxSpeed();
        minSpeed = ship.GetShipMinSpeed();
        fieldOfFire = ship.GetFieldOfFire();
        nextTurn = Random.Range(0, 2f);
        nextAdjust = Random.Range(0, 2f);
        stopSearch = false;

        ship.PrimaryReady += AttackTarget;

        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        
    }

    protected virtual void FixedUpdate()
    {
        if (target != null && !stopSearch)
        {
            float angle = GetAngleToTarget();
            targetRb = target.GetComponent<Rigidbody2D>();

            if (Mathf.Abs(angle) <= fieldOfFire)
            {
                Vector2 posDiff = targetRb.position - rb.position;
                float distance = Mathf.Sqrt(posDiff.sqrMagnitude);

                Vector2 inaccuracy = (new Vector2(Random.Range(-plasmaInaccuracy, plasmaInaccuracy), 
                    Random.Range(-plasmaInaccuracy, plasmaInaccuracy)) * (1 / distance));

                targetAcceleration = CalculateTargetAcceleration() + inaccuracy;
            }
        }
        else if (!stopSearch)
        {
            target = FindTarget();
        }

        Move();
    }

    protected virtual void AttackTarget(object sender, ShipBase.ShootArgs e)
    {
        ShipBase ship = (ShipBase)sender;

        Vector2 aimPos = GetTargetLeadingPosition(targetAcceleration, 0, e.primary);
        Vector2 shootDirection = (aimPos - e.projectileSpawnPoint).normalized;

        float angle = Vector2.Angle((Vector2)transform.up, shootDirection);

        if (angle <= fieldOfFire)
        {
            switch(e.type)
            {
                case ShootType.Primary:
                    ship.ShootPrimary(shootDirection);
                    break;
                
                default:
                    print("Error in AIController : AttackTarget");
                    break;
            }
        }
    }

    protected virtual GameObject FindTarget()
    {
        float distance;
        float lowestDistance = Mathf.Infinity;

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
                if (Math.Abs(angle) < 90 && speed < maxSpeed)
                {
                    speed += 0.2f;
                }
                if (Math.Abs(angle) >= 90 && speed > minSpeed)
                {
                    speed -= 0.2f;
                }
            }
        }
        else
        {
            turn = 0f;
        }

        ship.SetShipSpeed(speed);
        ship.SetShipTurn(turn);
    }

    protected virtual void UpdateBehaviors()
    {
        if (nextTurn > 0) nextTurn -= Time.deltaTime;

        if (nextAdjust > 0) nextAdjust -= Time.deltaTime;
    }

    protected virtual float GetAngleToTarget()
    {
        Vector2 targetDirection = targetRb.position - rb.position;

        return Vector2.SignedAngle((Vector2)transform.up, targetDirection);
    }

    protected virtual Vector2 CalculateTargetAcceleration()
    {
        Vector2 targetAcceleration = (targetRb.velocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = targetRb.velocity;

        return targetAcceleration;
    }

    // Imma be real idk how any of the math stuff works, stole it from the internet
    protected virtual Vector2 GetTargetLeadingPosition(Vector2 targetAcceleration, int iterations, WeaponsBase weapon)
    {
        float s = weapon.getSpeed(); // * maybe add ship speed somehow? <-- why would I do this? can't remember...
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
