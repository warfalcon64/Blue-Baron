using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public abstract class WeaponsBase : MonoBehaviour
{
    [Header("Damage Type")]
    [SerializeField] public string damageType = "none";

    [Header("Attributes")]
    [SerializeField] protected float speed = 25;
    [SerializeField] protected float turnSpeed = 0;
    [SerializeField] protected float health = 0;
    [SerializeField] protected float damage = 10;
    [SerializeField] protected float range = 100;
    [SerializeField] protected float coolDown = 1;
    [SerializeField] protected bool isDamageable = false;
    [SerializeField] protected bool isSeeker = false;
    [SerializeField] protected WeaponUsage usage = WeaponUsage.None;

    [Header("Lifetime")]
    [SerializeField] protected float lifetime = 5f;

    [Header("Impact VFX")]
    [Tooltip("Event fired on the central VFXManager graph when this projectile hits.")]
    [SerializeField] protected string impactVFXEvent = "OnImpact";

    protected GameObject target;
    protected ShipBase source;

    // Pooled weapons are spawned via ObjectPoolManager and must return themselves to PoolType when
    // finished instead of calling Destroy. Unpooled weapons keep the Instantiate/Destroy lifecycle.
    public virtual bool IsPooled => false;
    public virtual ObjectPoolManager.PoolType PoolType => ObjectPoolManager.PoolType.GameObjects;

    public virtual void Setup(Vector2 shootDirection, Vector2 shipVelocity, ShipBase source)
    {
        throw new NotImplementedException();
    }

    public virtual float GetSpeed()
    {
        return speed;
    }

    public virtual float GetTurnSpeed()
    {
        return turnSpeed;
    }

    public virtual float GetHealth()
    {
        return health;
    }

    public virtual float GetDamage()
    {
        return damage;
    }

    public virtual float GetRange()
    {
        return range;
    }

    public virtual float GetCoolDown()
    {
        return coolDown;
    }

    public virtual ShipBase GetSource()
    {
        return source;
    }

    public virtual bool IsSeeker()
    {
        return isSeeker;
    }

    public virtual WeaponUsage GetUsage()
    {
        return usage;
    }

    public virtual void SetCoolDown(float newCoolDown)
    {
        coolDown = newCoolDown;
    }

    public virtual void SetTarget(GameObject newTarget)
    {
        target = newTarget;
    }

    // impactVelocity (the projectile's velocity at the moment of impact) is passed so the spark can
    // inherit a fraction of the projectile's momentum, like real spall. Defaults to zero for callers
    // (e.g. missiles) that don't supply it.
    protected void SpawnImpactVFX(Vector2 impactVelocity = default)
    {
        if (VFXManager.Instance != null)
            VFXManager.Instance.PlayImpact(transform.position, impactVelocity, impactVFXEvent);
    }
}
