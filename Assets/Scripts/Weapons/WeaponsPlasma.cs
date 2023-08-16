using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.VFX;

public class WeaponsPlasma : WeaponsBase
{
    Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void setup(Vector2 shootDirection, Vector2 shipVelocity)
    {
        shootDirection = shootDirection.normalized;
        float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;

        rb.rotation = angle;
        rb.velocity = (shootDirection * speed) + shipVelocity;

        Destroy(gameObject, 10f);
    }

    public override float getSpeed() => base.getSpeed();

    public override float getDamage() => base.getDamage();

    public override float getRange() => base.getRange();
}
