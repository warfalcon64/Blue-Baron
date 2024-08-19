using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
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
    [SerializeField] protected float primaryCoolDown = 0.2f; // ** primaryCoolDown IN THE SHIP CLASS IS DEPRECATED, USE WEAPONMAP coolDown INSTEAD!!
    [SerializeField] protected float primaryFieldofFire = 45; // Angle between the vertical line bisecting the craft and the line representing the edge of field
    [SerializeField] protected float plasmaInaccuracy = 0;
    [SerializeField] protected Transform leftGun;
    [SerializeField] protected Transform rightGun;
    //[SerializeField] protected WeaponsBase plasma;

    [Header("VFX")]
    [SerializeField] protected float lowHealth = 25;
    [SerializeField] protected Transform smoke;

    public bool godMode;

    [HideInInspector]
    public bool isPlayer;

    protected bool stopSearch;
    protected bool isSmoking;
    protected bool leftFire;
    protected float maxSpeed;
    protected float minSpeed;
    protected float turn;
    protected float maxTurnSpeed;
    protected float minTurnSpeed;
    protected float nextFire;
    protected float nextTurn;
    protected float nextAdjust;

    protected List<ShipBase> enemyTeam;
    protected VisualEffect mainEffects;
    protected VisualEffect shipEffects;
    protected GameObject target;
    protected Rigidbody2D rb;
    protected Rigidbody2D targetRb;
    protected Vector2 lastVelocity;
    protected Vector2 targetAcceleration;

    protected ShipType type;
    protected ShootType shootMode;

    public event EventHandler OnShipDeath;
    public event EventHandler<ShootArgs> PrimaryReady;

    [Header("Weapon Slots")]
    [SerializeField] private List<WeaponMap.WeaponMapEntry> weapons;
    private WeaponMap weaponMap;
    private WeaponsBase primary;

    public class ShootArgs : EventArgs
    {
        public ShootType type;
        public WeaponsBase primary;
        public Vector2 projectileSpawnPoint;
    }

    // Start is called before the first frame update
    protected virtual void Awake()
    {
        Init();

    }

    protected virtual void Start()
    {
        mainEffects = SceneManager.Instance.GetVFXManager().GetComponentInChildren<VisualEffect>();
        enemyTeam = SceneManager.Instance.GetLiveEnemies(tag);
        // *** MAKE SHIP LISTEN TO TARGET'S DEATH EVENTS IN THE AI CONTROLLER INSTEAD
        // Make ship listen to enemy ships for their death events
        //foreach (ShipBase ship in enemyTeam)
        //{
        //    ship.OnShipDeath += OnEnemyDeath;
        //}
    }

    protected virtual void Update()
    {
        if (health <= 0)
        {
            OnShipDeath?.Invoke(this, EventArgs.Empty);
            OnDeath();
        }

        UpdateSmoke();
        UpdateTimers();
    }

    // Update is called once per frame
    protected virtual void FixedUpdate()
    {
        //if (target != null && !stopSearch)
        //{
        //    float angle = GetAngleToTarget();

        //    if (Mathf.Abs(angle) <= primaryFieldofFire)
        //    {
        //        Vector2 posDiff = target.GetComponent<Rigidbody2D>().position - rb.position;
        //        float distance = Mathf.Sqrt(posDiff.sqrMagnitude);

        //        targetAcceleration = GetTargetAcceleration()
        //            + (new Vector2(Random.Range(-plasmaInaccuracy, plasmaInaccuracy), Random.Range(-plasmaInaccuracy, plasmaInaccuracy)) * (1 / distance)); // Adding inaccuracy to prevent player skill diff
        //        ShootProjectiles(targetAcceleration);
        //    }
        //}
        //else if (!stopSearch)
        //{
        //    target = FindTarget();
        //}

        Move();
    }

    protected virtual void Init()
    {
        isPlayer = false;
        maxSpeed = speed;
        minSpeed = speed / 2;
        turn = 0f;
        maxTurnSpeed = turnSpeed + 1.2f;
        minTurnSpeed = turnSpeed;
        target = null;
        targetRb = null;
        nextFire = 0f;
        nextTurn = Random.Range(0, 2f);
        nextAdjust = Random.Range(0, 2f);
        stopSearch = false;
        leftFire = false;
        isSmoking = false;
        shootMode = ShootType.None;

        weaponMap = ScriptableObject.CreateInstance<WeaponMap>();
        weaponMap.Init(weapons);

        primary = weaponMap.GetWeapon(ShootType.Primary);

        rb = GetComponent<Rigidbody2D>();

        type = ShipType.Fighter;
        shipEffects = smoke.GetComponent<VisualEffect>();
    }

    protected virtual void OnDeath() // *** UNSUBSCRIBE FROM ONSHIPDEATH EVENT!!!
    {
        PlayDeathVFX();
        //Destroy(gameObject);

        if (isPlayer)
        {
            PlayerController pc = GetComponent<PlayerController>();
            pc.OnPlayerDeath();
        }

        gameObject.SetActive(false);
    }

    private void OnEnemyDeath(object sender, EventArgs e)
    {
        target = FindTarget();
    }

    protected virtual void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.GetComponent<Rigidbody2D>() != null && !collider.CompareTag(tag))
        {
            if (godMode) { return; }
            string type = collider.GetComponent<WeaponsBase>().damageType;
            float damage = collider.GetComponent<WeaponsBase>().GetDamage();

            // Determine the type of damage weapon deals, apply modifiers accordingly
            // * Move damage code into the respective projectiles: they calculate damage with modifiers then tell ship the total damage, ship then applies damage to itself
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

            if (health <= lowHealth && !isSmoking)
            {
                isSmoking = true;
                shipEffects.SendEvent("OnDamage");
            }
        }
    }

    protected virtual void PlayHitVFX(string type)
    {
        string effectEvent = "null";

        switch (type)
        {
            case "Plasma":
                effectEvent = "LaserHit"; // * Change the name of "LaserHit" to something more generic and not have laser in it
                break;

            default:
                print("COULD NOT GET DAMAGE TYPE OF WEAPON");
                break;

        }

        VFXEventAttribute eventAttribute = mainEffects.CreateVFXEventAttribute();

        // Get the ID of the property we want to modify
        int vfxPosition = Shader.PropertyToID("Position");

        // Set the property, and send event with the attribute carrying the info to the vfx graph
        mainEffects.SetVector3(vfxPosition, transform.position);
        mainEffects.SendEvent(effectEvent, eventAttribute);
    }

    protected virtual void PlayDeathVFX()
    {
        VFXEventAttribute eventAttribute = mainEffects.CreateVFXEventAttribute();

        int vfxPosition = Shader.PropertyToID("Position");

        mainEffects.SetVector3(vfxPosition, transform.position);
        mainEffects.SendEvent("OnDeath", eventAttribute);
    }


    // Switch this to activate Primary/Secondary ready events
    protected virtual void ShootProjectiles(Vector2 targetAcceleration)
    {
        if (nextFire <= 0)
        {
            //ShootPlasma(targetAcceleration);
            PrimaryReady?.Invoke(this, 
                new ShootArgs() { 
                    type = ShootType.Primary, 
                    primary = weaponMap.GetWeapon(ShootType.Primary), 
                    projectileSpawnPoint = leftGun.position 
                });
            // *** rename nextFire before adding secondary weapons!!!
            nextFire = primaryCoolDown;
        }
    }

    public virtual void ShootPrimary(Vector2 aimPos)
    {
        WeaponsBase projectile = weaponMap.GetWeapon(ShootType.Primary);
        Vector2 projectileSpawn = leftGun.position;

        leftFire = !leftFire;

        if (!leftFire) projectileSpawn = rightGun.position;

        //Vector2 aimPos = GetTargetLeadingPosition(targetAcceleration, 0, primary.GetSpeed());
        Vector2 shootDirection = (aimPos - projectileSpawn).normalized;
        WeaponsBase temp = Instantiate(projectile, projectileSpawn, leftGun.rotation);
        temp.setup(shootDirection, rb.velocity);
    }

    // Come up with a generic method to shoot whatever weapon has been put into primary or secondary slots
    protected virtual void ShootPlasma(Vector2 targetAcceleration)
    {
        Vector2 plasmaSpawn = leftGun.position;
        leftFire = !leftFire;

        if (!leftFire) plasmaSpawn = rightGun.position;

        Vector2 aimPos = GetTargetLeadingPosition(targetAcceleration, 0, weaponMap.GetWeapon(ShootType.Primary).GetSpeed());
        Vector2 shootDirection = (aimPos - plasmaSpawn).normalized;

        float angle = Vector2.Angle((Vector2)transform.up, shootDirection);

        if (angle <= primaryFieldofFire)
        {
            WeaponsBase plasmaClone = Instantiate(weaponMap.GetWeapon(ShootType.Primary), plasmaSpawn, leftGun.rotation);
            plasmaClone.GetComponent<WeaponsPlasma>().setup(shootDirection, rb.velocity);
        }
    }

    // Moves the ship to keep the specified target gameobject within the given parameters
    protected virtual void Move()
    {

        //if (target != null && !stopSearch)
        //{
        //    float angle = GetAngleToTarget();

        //    // Turning logic
        //    if (Mathf.Abs(angle) > faceEnemyAngle && nextTurn <= 0)
        //    {
        //        if (angle > 0)
        //        {
        //            turn = 1f;
        //        }
        //        if (angle < 0)
        //        {
        //            turn = -1f;
        //        }
        //    }
        //    else if (Mathf.Abs(angle) < faceEnemyAngle && nextTurn <= 0)
        //    {
        //        Random.Range(0, 0.5f);
        //    }

        //    // Acceleration logic
        //    if (nextAdjust <= 0)
        //    {
        //        if (Math.Abs(angle) < 90 && speed < maxSpeed)
        //        {
        //            speed += 0.2f;
        //        }
        //        if (Math.Abs(angle) >= 90 && speed > minSpeed)
        //        {
        //            speed -= 0.2f;
        //        }
        //    }
        //    else
        //    {
        //        nextAdjust = Random.Range(0, 0.5f);
        //    }
        //}
        //else
        //{
        //    turn = 0f;
        //}

        // keep this
        rb.velocity = transform.up * speed;
        rb.MoveRotation(rb.rotation + (turnSpeed * turn));
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
            if (enemy == null)
            {
                //print("ENEMY IS NULL");
                continue;
            }

            //print("ENEMY IS NOT NULL");
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

    protected virtual void UpdateSmoke()
    {
        if (!isSmoking) return;

        int lifetime = Shader.PropertyToID("SmokeLifetime");

        if (speed < 6.1f)
        {
            shipEffects.SetFloat(lifetime, 1.2f);
        }
        else if (speed < 7.4f)
        {
            shipEffects.SetFloat(lifetime, 1f);
        }
        else if (speed < 10f)
        {
            shipEffects.SetFloat(lifetime, 0.9f);
        }
        else if (speed == 10f)
        {
            shipEffects.SetFloat(lifetime, 0.85f);
        }
    }

    protected virtual void UpdateTimers()
    {
        if (nextFire > 0) nextFire -= Time.deltaTime;

        if (nextTurn > 0) nextTurn -= Time.deltaTime;

        if (nextAdjust > 0) nextAdjust -= Time.deltaTime;
    }

    public virtual void Accelerate(float accelAmount)
    {
        if ((speed + accelAmount) <= maxSpeed)
        {
            speed += accelAmount;
            turnSpeed -= .04f;
        }
    }

    public virtual void Decelerate(float decelAmount)
    {
        if ((speed - decelAmount) >= minSpeed)
        {
            speed -= decelAmount;
            turnSpeed += .04f;
        }
    }

    // ===== Leading target calculations =====
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

    protected virtual Vector2 GetTargetLeadingPosition(Vector2 targetAcceleration, int iterations, float weaponSpeed)
    {
        targetRb = target.GetComponent<Rigidbody2D>();

        float s = weaponSpeed; // *maybe add ship speed somehow?
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

    // ====== Getters and Setters ======
    public virtual float GetShipSpeed()
    {
        return speed;
    }

    public virtual void SetShipSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public virtual float GetShipMaxSpeed()
    {
        //print("MAX SPEED IS: " + maxSpeed);
        return maxSpeed;
    }

    public virtual float GetShipMinSpeed()
    {
        return minSpeed;
    }

    public virtual float GetPrimaryFieldOfFire()
    {
        return primaryFieldofFire;
    }

    public virtual float GetPrimaryCoolDown()
    {
        return primaryCoolDown;
    }

    public virtual WeaponMap GetWeaponMap()
    {
        return weaponMap;
    }

    public virtual PlayerController GetPlayerController()
    {
        return GetComponent<PlayerController>();
    }

    public virtual ShipType GetShipType()
    {
        return type;
    }

    public virtual void SetShipTurn(float newTurn)
    {
        turn = newTurn;
    }

    public virtual void SetShipTurnSpeed(float newTurnSpeed)
    {
        turnSpeed = newTurnSpeed;
    }

    public virtual void SetTargetAcceleration(Vector2 acceleration)
    {
        targetAcceleration = acceleration;
    }

    public virtual void SetShootType(ShootType type)
    {
        shootMode = type;
    }
}
