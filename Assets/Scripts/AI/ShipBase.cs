using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public abstract class ShipBase : MonoBehaviour
{
    [Header("Health/Shields")]
    [SerializeField] protected float health = 100;
    [SerializeField] protected float shield = 0;

    [Header("Movement")]
    [SerializeField] protected float speed = 10;
    [SerializeField] protected float turnSpeed = 2f;
    [SerializeField] protected float faceEnemyAngle = 1; // The min FOV to keep enemy in

    [Header("Attack")]
    [SerializeField] protected float primaryCoolDown = 0.2f;
    [SerializeField] protected float fieldOfFire = 45; // Angle between the vertical line bisecting the craft and the line representing the edge of field
    [SerializeField] protected float plasmaInaccuracy = 0;
    [SerializeField] protected Transform leftGun;
    [SerializeField] protected Transform rightGun;
    [SerializeField] protected Transform plasma;

    [Header("VFX")]
    [SerializeField] protected Transform smoke;

    protected bool isSmoking;
    protected bool leftFire;
    protected float maxSpeed;
    protected float minSpeed;
    protected float maxTurn;
    protected float minTurn;
    protected float nextFire;
    protected float nextTurn;
    protected float nextAdjust;

    protected GameObject vfxManager; //*** MAKE VFX MANAGER A SCRIPTABLE OBJECT TO AVOID FIND GAMEOBJECT CALLS
    protected VisualEffect effects;
    protected GameObject target;
    protected Rigidbody2D rb;
    protected Rigidbody2D targetRb;
    protected shipType ship;
    protected Vector2 lastVelocity;

    protected enum shipType
    {
        Fighter,
        Bomber
    }

    // Start is called before the first frame update
    protected virtual void Awake()
    {
        Init();
    }

    protected virtual void Update()
    {
        if (health <= (health * 0.25f) && !isSmoking)
        {
            VFXEventAttribute eventAttribute = effects.CreateVFXEventAttribute();
        }

        if (health <= 0)
        {
            OnDeath();
        }
    }

    // Update is called once per frame
    protected virtual void FixedUpdate()
    {
        if (target != null)
        {
            float angle = GetAngleToTarget();

            if (Mathf.Abs(angle) <= fieldOfFire)
            {
                Vector2 posDiff = target.GetComponent<Rigidbody2D>().position - rb.position;
                float distance = Mathf.Sqrt(posDiff.sqrMagnitude);

                Vector2 targetAcceleration = GetTargetAcceleration()
                    + (new Vector2(Random.Range(-plasmaInaccuracy, plasmaInaccuracy), Random.Range(-plasmaInaccuracy, plasmaInaccuracy)) * (1 / distance)); // Adding inaccuracy to prevent player skill diff
                ShootProjectiles(targetAcceleration);
            }
        }
        else
        {
            target = FindTarget();
        }

        Move();
    }

    protected virtual void Init()
    {
        maxSpeed = speed;
        minSpeed = speed / 2;
        maxTurn = turnSpeed + 1;
        minTurn = turnSpeed;
        target = null;
        targetRb = null;
        nextFire = 0f;
        nextTurn = Time.time + Random.value;
        nextAdjust = Time.time + Random.value;
        leftFire = false;
        isSmoking = false;

        vfxManager = GameObject.FindGameObjectWithTag("VFX Manager");
        rb = GetComponent<Rigidbody2D>();
        effects = vfxManager.GetComponent<VisualEffect>();
    }

    protected virtual void OnDeath()
    {
        
    }

    protected virtual void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.GetComponent<Rigidbody2D>() != null && !collider.CompareTag(tag))
        {
            string type = collider.GetComponent<WeaponsBase>().damageType;
            float damage = collider.GetComponent<WeaponsBase>().getDamage();

            switch (type)
            {
                case "Plasma":
                    if (shield <= 0) damage *= 2;
                    break;

                default:
                    print("DID NOT APPLY DAMAGE CORRECTLY");
                    break;
            }

            health -= damage;

            PlayHitVFX(type);
        }
    }

    protected virtual void PlayHitVFX(string type)
    {
        string effectEvent = "null";

        switch (type)
        {
            case "Plasma":
                effectEvent = "LaserHit"; // ***Change the name of "LaserHit" to something more generic and not have laser in it
                break;

            default:
                print("COULD NOT GET DAMAGE TYPE OF WEAPON");
                break;

        }

        VFXEventAttribute eventAttribute = effects.CreateVFXEventAttribute();

        // Get the ID of the property we want to modify
        int vfxPosition = Shader.PropertyToID("Position");

        // Set the property, and send event with the attribute carrying the info to the vfx graph
        effects.SetVector3(vfxPosition, transform.position);
        effects.SendEvent(effectEvent, eventAttribute);
    }

    protected virtual void ShootProjectiles(Vector2 targetAcceleration)
    {
        if (nextFire <= Time.time)
        {
            ShootPlasma(targetAcceleration);
            nextFire = Time.time + primaryCoolDown;
        }
    }

    protected virtual void ShootPlasma(Vector2 targetAcceleration)
    {
        Vector2 plasmaSpawn = leftGun.position;

        if (!leftFire)
        {
            leftFire = true;
            plasmaSpawn = leftGun.position;
        }
        else
        {
            leftFire = false;
            plasmaSpawn = rightGun.position;
        }

        Vector2 aimPos = GetTargetLeadingPosition(targetAcceleration, 0, plasma);
        Vector2 shootDirection = (aimPos - plasmaSpawn).normalized;

        float angle = Vector2.Angle((Vector2)transform.up, shootDirection);

        if (angle <= fieldOfFire)
        {
            Transform plasmaClone = Instantiate(plasma, plasmaSpawn, leftGun.rotation);
            plasmaClone.GetComponent<WeaponsPlasma>().setup(shootDirection, rb.velocity);
        }
    }

    // Moves the ship to keep the specified target gameobject within the given parameters
    protected virtual void Move()
    {
        float turn = 0f;

        if (target != null)
        {
            float angle = GetAngleToTarget();

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
                if (Math.Abs(angle) < 90 && speed < maxSpeed)
                {
                    speed += 0.2f;
                }
                if (Math.Abs(angle) >= 90 && speed > minSpeed)
                {
                    speed -= 0.2f;
                }
            }
            else
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
        targetRb = target.GetComponent<Rigidbody2D>();
        Vector2 targetDirection = targetRb.position - rb.position;

        return Vector2.SignedAngle((Vector2)transform.up, targetDirection);
    }

    protected virtual Vector2 GetTargetAcceleration()
    {
        targetRb = target.GetComponent<Rigidbody2D>();
        Vector2 targetAccelearation = (targetRb.velocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = targetRb.velocity;

        return targetAccelearation;
    }

    // Imma be real idk how any of the math stuff works, stole it from the internet
    protected virtual Vector2 GetTargetLeadingPosition(Vector2 targetAcceleration, int iterations, Transform weapon)
    {
        targetRb = target.GetComponent<Rigidbody2D>();

        float s = weapon.GetComponent<WeaponsBase>().getSpeed(); // *maybe add ship speed somehow?
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
