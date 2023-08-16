using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    public virtual void setup(Vector2 shootDirection, Vector2 shipVelocity)
    {
        throw new NotImplementedException();
    }

    public virtual float getSpeed()
    {
        return speed;
    }

    public virtual float getTurnSpeed()
    {
        return turnSpeed;
    }

    public virtual float getHealth()
    {
        return health;
    }

    public virtual float getDamage()
    {
        return damage;
    }

    public virtual float getRange()
    {
        return range;
    }
}
