using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.VFX;

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

    [Header("Lifetime")]
    [SerializeField] protected float lifetime = 5f;

    protected ShipBase source;

    public virtual void setup(Vector2 shootDirection, Vector2 shipVelocity, ShipBase source)
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

    public virtual void SetCoolDown(float newCoolDown)
    {
        coolDown = newCoolDown;
    }
}
