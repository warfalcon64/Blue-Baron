using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.VFX;

public class WeaponsPlasma : WeaponsBase
{
    Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void setup(Vector2 shootDirection, Vector2 shipVelocity, ShipBase source)
    {
        shootDirection = shootDirection.normalized;
        float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;

        rb.rotation = angle;
        rb.velocity = (shootDirection * speed) + shipVelocity;
        this.source = source;

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.GetComponent<Rigidbody2D>() != null && !collider.CompareTag(tag))
        {
            Destroy(gameObject);
        }
    }

    public override float GetSpeed() => base.GetSpeed();

    public override float GetDamage() => base.GetDamage();

    public override float GetRange() => base.GetRange();
}
