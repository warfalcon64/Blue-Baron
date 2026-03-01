using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] protected float turnSpeed = 100f;

    [Header("Weapon Groups")]
    [SerializeField] private List<WeaponGroup> weaponGroups;

    [Header("Combat System")]
    [SerializeField] private CombatSystemBase combatSystem;

    [Header("VFX")]
    [SerializeField] protected float lowHealth = 25;
    [SerializeField] protected Transform smoke;

    public bool godMode;

    [HideInInspector]
    public bool isPlayer;

    protected bool isSmoking;
    protected float maxSpeed;
    protected float minSpeed;
    protected float turn;
    protected float maxTurnSpeed;
    protected float minTurnSpeed;

    protected VisualEffect mainEffects;
    protected VisualEffect shipEffects;
    protected Rigidbody2D rb;
    protected VFXManager vfxManager;

    protected ShipType type;

    // Event parameter for OnShipDamage is the ShipBase that fired the WeaponBase which hit this ship, the "attacker"
    public event EventHandler<ShipBase> OnShipDamage;
    public event EventHandler OnShipDeath;
    public event EventHandler<WeaponsBase> OnSeekerFired;

    private Hardpoint[] hardpoints;

    // Start is called before the first frame update
    protected virtual void Awake()
    {
        Init();
    }

    protected virtual void Start()
    {
        mainEffects = SceneManager.Instance.GetVFXManager().GetComponentInChildren<VisualEffect>();
        vfxManager = SceneManager.Instance.vfxManager.GetComponent<VFXManager>();
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
        maxTurnSpeed = turnSpeed + 60f;
        minTurnSpeed = turnSpeed;
        isSmoking = false;

        hardpoints = GetComponentsInChildren<Hardpoint>();

        rb = GetComponent<Rigidbody2D>();

        type = ShipType.Fighter;
        shipEffects = smoke.GetComponent<VisualEffect>();

        if (combatSystem != null)
            combatSystem.Init(this);
    }

    protected virtual void OnDeath()
    {
        vfxManager.PlayVFX(VFXManager.VFXType.Explosion, transform.position);

        if (isPlayer)
        {
            PlayerController pc = GetComponent<PlayerController>();
            pc.OnPlayerDeath();
        }

        // Detach smoke trail so it can fade out after the ship is deactivated
        if (isSmoking)
        {
            smoke.SetParent(null);
            shipEffects.Stop();
            float maxLifetime = shipEffects.GetFloat(Shader.PropertyToID("SmokeLifetime"));
            Destroy(smoke.gameObject, maxLifetime);
        }

        gameObject.SetActive(false);
    }

    // move vfx code from here and playvfx to vfx manager
    protected virtual void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.GetComponent<Rigidbody2D>() != null && !collider.CompareTag(tag))
        {
            if (godMode) { return; }
            WeaponsBase weapon = collider.gameObject.GetComponent<WeaponsBase>();
            string type = weapon.damageType;
            float damage = weapon.GetDamage();


            // Determine the type of damage weapon deals, apply modifiers accordingly
            // * Move damage code into the respective projectiles: they calculate damage with modifiers then tell ship the total damage, ship then applies damage to itself
            switch (type)
            {
                case "Plasma":
                    if (shield <= 0) damage *= 2;
                    break;

                case "High Explosive":
                    damage *= 2;
                    break;

                default:
                    print("MISC DAMAGE APPLIED:" + type);
                    break;
            }

            health -= damage;
            OnShipDamage?.Invoke(this, weapon.GetSource());


            if (health <= lowHealth && !isSmoking)
            {
                isSmoking = true;
                shipEffects.SendEvent("OnDamage");
            }
        }
    }

    public List<WeaponsBase> FireGroup(int groupIndex, Vector2 aimPos)
    {
        if (groupIndex < 0 || groupIndex >= weaponGroups.Count)
            return new List<WeaponsBase>();

        List<WeaponsBase> projectiles = weaponGroups[groupIndex].Fire(aimPos, rb.linearVelocity, this);

        foreach (WeaponsBase projectile in projectiles)
        {
            if (projectile.IsSeeker())
            {
                OnSeekerFired?.Invoke(this, projectile);
            }
        }

        return projectiles;
    }

    // Moves the ship to keep the specified target gameobject within the given parameters
    protected virtual void Move()
    {
        rb.linearVelocity = transform.up * speed;
        rb.MoveRotation(rb.rotation + (turnSpeed * turn * Time.fixedDeltaTime));
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
            turnSpeed -= 2f;
        }
    }

    public virtual void Decelerate(float decelAmount)
    {
        if ((speed - decelAmount) >= minSpeed)
        {
            speed -= decelAmount;
            turnSpeed += 2f;
        }
    }

    // ====== Utility ======
    public float GetAngleToTarget(GameObject target)
    {
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        Vector2 targetDirection = targetRb.position - rb.position;
        return Vector2.SignedAngle((Vector2)transform.up, targetDirection);
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
        return maxSpeed;
    }

    public virtual float GetShipMinSpeed()
    {
        return minSpeed;
    }

    public List<WeaponGroup> GetWeaponGroups()
    {
        return weaponGroups;
    }

    public WeaponGroup GetWeaponGroup(int index)
    {
        if (index >= 0 && index < weaponGroups.Count)
            return weaponGroups[index];
        return null;
    }

    public List<WeaponGroup> GetWeaponGroupsByUsage(WeaponUsage usage)
    {
        List<WeaponGroup> result = new List<WeaponGroup>();
        foreach (WeaponGroup group in weaponGroups)
        {
            if (group.HasUsage(usage))
                result.Add(group);
        }
        return result;
    }

    public virtual PlayerController GetPlayerController()
    {
        return GetComponent<PlayerController>();
    }

    public Rigidbody2D GetRigidBody()
    {
        return rb;
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

    public CombatSystemBase GetCombatSystem() => combatSystem;

    public void ActivateCombatSystem()
    {
        if (combatSystem != null && combatSystem.IsReady())
            combatSystem.Activate();
    }

}
