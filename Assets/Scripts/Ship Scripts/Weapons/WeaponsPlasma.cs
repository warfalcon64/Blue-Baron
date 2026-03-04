using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponsPlasma : WeaponsBase
{
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
    }

    public override void Setup(Vector2 shootDirection, Vector2 shipVelocity, ShipBase source)
    {
        shootDirection = shootDirection.normalized;
        rb.linearVelocity = (shootDirection * speed) + shipVelocity;
        this.source = source;

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.GetComponent<Rigidbody2D>() != null && !collider.CompareTag(tag))
        {
            SpawnImpactVFX();
            Destroy(gameObject);
        }
    }

    public override float GetSpeed() => base.GetSpeed();

    public override float GetDamage() => base.GetDamage();

    public override float GetRange() => base.GetRange();
}
