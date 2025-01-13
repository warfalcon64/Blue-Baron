using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
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
    [SerializeField] protected Transform missileSpawn;
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

    // Event parameter for OnShipDamage is the ShipBase that fired the WeaponBase which hit this ship, the "attacker"
    public event EventHandler<ShipBase> OnShipDamage;
    public event EventHandler OnShipDeath;
    public event EventHandler<WeaponsBase> OnSeekerFired;
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
    }

    protected virtual void Update()
    {
        if (health <= 0)
        {
            OnShipDeath?.Invoke(this, EventArgs.Empty);
            OnDeath();
        }

        UpdateSmoke();
    }

    // Update is called once per frame
    protected virtual void FixedUpdate()
    {
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

    protected virtual void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.GetComponent<Rigidbody2D>() != null && !collider.CompareTag(tag))
        {
            if (godMode) { return; }
            WeaponsBase weapon = collider.GetComponent<WeaponsBase>();
            string type = weapon.damageType;
            float damage = weapon.GetDamage();

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
            OnShipDamage?.Invoke(this, weapon.GetSource());

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

    // *** IMPORTANT: DEATH VFX MUST BE SEPARATED FROM SHIP GAMEOBJECT WHEN IT DIES, OTHERWISE THEY ARE CUT OFF
    protected virtual void PlayDeathVFX()
    {
        VFXEventAttribute eventAttribute = mainEffects.CreateVFXEventAttribute();

        int vfxPosition = Shader.PropertyToID("Position");

        mainEffects.SetVector3(vfxPosition, transform.position);
        mainEffects.SendEvent("OnDeath", eventAttribute);
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
        temp.Setup(shootDirection, rb.velocity, this);
    }

    public virtual void ShootSecondary(Vector2 aimPos)
    {
        WeaponsBase projectile = weaponMap.GetWeapon(ShootType.Secondary);
        Vector2 projectileSpawn = missileSpawn.position;

        Vector2 shootDirection = (aimPos - projectileSpawn).normalized;
        WeaponsBase temp = Instantiate(projectile, projectileSpawn, missileSpawn.rotation);

        if (projectile.IsSeeker())
        {
            OnSeekerFired?.Invoke(this, temp);
        }

        temp.Setup(shootDirection, rb.velocity, this);
    }

    // Moves the ship to keep the specified target gameobject within the given parameters
    protected virtual void Move()
    {
        rb.velocity = transform.up * speed;
        rb.MoveRotation(rb.rotation + (turnSpeed * turn));
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

    public virtual WeaponsBase GetPrimaryWeapon()
    {
        return primary;
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

    public virtual Tuple<Transform, Transform> GetPrimaryFirePositions()
    {
        return new Tuple<Transform, Transform>(leftGun, rightGun);
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
