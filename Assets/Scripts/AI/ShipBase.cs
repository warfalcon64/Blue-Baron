
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class ShipBase : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] protected float health = 100f;

    [Header("Movement")]
    [SerializeField] protected float speed = 10f;
    [SerializeField] protected float turnSpeed = 2f;
    [SerializeField] protected float faceEnemyAngle = 1f;

    [Header("Attack")]
    [SerializeField] protected float primaryCoolDown = 0.2f;
    [SerializeField] protected float fieldOfFire = 45f; // This is the angle between the line bisecting the craft vertically and the line limiting the field of fire
    [SerializeField] protected float laserInaccuracy = 0.0f;
    [SerializeField] protected Transform leftGun;
    [SerializeField] protected Transform rightGun;
    [SerializeField] protected Transform laser;

    protected bool leftFire;
    protected float maxSpeed;
    protected float minSpeed;
    protected float maxTurn;
    protected float minTurn;
    protected float nextFire;
    protected float nextTurn;
    protected float nextAdjust;
    protected GameObject target;
    protected Rigidbody2D rb;
    protected shipType ship;
    protected Vector2 lastVelocity;

    protected enum shipType
    {
        Fighter,
        Bomber
    }

    private void Awake()
    {
        Init();
    }

    private void FixedUpdate()
    {
        if (!target)
        {
            target = FindTarget();
        }

        if (target != null)
        {
            float angle = GetAngleToTarget();

            if (Mathf.Abs(angle) <= fieldOfFire)
            {
                Vector2 posDiff = target.GetComponent<Rigidbody2D>().position - rb.position;
                float distance = Mathf.Sqrt(posDiff.sqrMagnitude);

                Vector2 targetAcceleration = GetTargetAcceleration() 
                    + (new Vector2(Random.Range(-laserInaccuracy, laserInaccuracy), Random.Range(-laserInaccuracy, laserInaccuracy)) * (1 / distance)); // Adding inaccuracy to prevent player skill diff
                ShootProjectiles(targetAcceleration);
            }
        }

        Move();
    }

    protected virtual void Init()
    {
        maxSpeed = speed;
        minSpeed = speed / 2;
        maxTurn = turnSpeed + 1;
        minTurn = turnSpeed;
        rb = GetComponent<Rigidbody2D>();
        target = null;
        nextFire = 0f;
        nextTurn = Time.time + Random.value;
        nextAdjust = Time.time + Random.value;
        leftFire = false;
    }

    protected virtual void ShootProjectiles(Vector2 targetAcceleration)
    {
        if (nextFire <= Time.time)
        {
            ShootLaser(targetAcceleration);
            nextFire = Time.time + primaryCoolDown;
        }
    }

    protected virtual void ShootLaser(Vector2 targetAcceleration)
    {
        Vector2 laserSpawn = leftGun.position;

        if (!leftFire)
        {
            leftFire = true;
            laserSpawn = leftGun.position;
        }
        else
        {
            leftFire = false;
            laserSpawn = rightGun.position;
        }

        Vector2 aimPos = GetTargetLeadingPosition(targetAcceleration, 0);
        Vector2 shootDirection = (aimPos - laserSpawn).normalized;

        float angle = Vector2.Angle((Vector2)transform.up, shootDirection);

        if (angle <= fieldOfFire)
        {
            Transform bulletClone = Instantiate(laser, laserSpawn, leftGun.rotation);
            bulletClone.GetComponent<Laser>().setup(shootDirection, rb.velocity);
        }
        
    }

    // Moves the ship to face the specified target gameobject
    protected virtual void Move()
    {
        float turn = 0f;

        if (target != null)
        {
            float angle = GetAngleToTarget();
            //print(angle);

            // Turning logic
            if (Mathf.Abs(angle) > faceEnemyAngle && nextTurn <= Time.time)
            {
                if (angle > 0)
                {
                    turn = 1f;
                }
                if (angle < 0)
                {
                    turn = -1f;
                }
            } 
            else if (Mathf.Abs(angle) < faceEnemyAngle && nextTurn <= Time.time)
            {
                nextTurn = Time.time + Random.value;
            }

            // Acceleration logic
            if (nextAdjust <= Time.time)
            {
                if (Mathf.Abs(angle) < 90 && speed < maxSpeed)
                {
                    speed += 0.2f;
                }
                if (Mathf.Abs(angle) >= 90 && speed > minSpeed)
                {
                    speed -= 0.2f;
                }
            } else
            {
                nextAdjust = Time.time + Random.value;
            }
        }

        rb.velocity = transform.up * speed;
        rb.MoveRotation(rb.rotation + (turnSpeed * turn));
    }

    protected virtual GameObject FindTarget()
    {
        GameObject enemyTeam;
        GameObject target = null;
        string enemyTag;
        float distance;
        float lowestDistance = Mathf.Infinity;

        if (CompareTag("Blue"))
        {
            enemyTag = "Red Team";
        }
        else
        {
            enemyTag = "Blue Team";
        }

        enemyTeam = GameObject.FindGameObjectWithTag(enemyTag);

        if (enemyTeam.transform.childCount == 0) return target;

        foreach (Transform enemy in enemyTeam.transform)
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

    protected virtual float GetAngleToTarget()
    {
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        Vector2 targetDirection = targetRb.position - rb.position;

        return Vector2.SignedAngle((Vector2)transform.up, targetDirection);
    }

    protected virtual Vector2 GetTargetAcceleration()
    {
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        Vector2 targetAcceleration = (targetRb.velocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = targetRb.velocity;

        return targetAcceleration;
    }

    // Imma be real idk how any of the stuff below works, I found it on the internet
    protected virtual Vector2 GetTargetLeadingPosition(Vector2 targetAcceleration, int iterations)
    {
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();

        float s = laser.GetComponent<Laser>().getSpeed();
        float distance = Vector2.Distance(targetRb.position, rb.position);

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
