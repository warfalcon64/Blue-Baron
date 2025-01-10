using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponsAAMissile : WeaponsBase
{
    private float seekerFieldOfView = 1;

    private Rigidbody2D rb;

    

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        
    }

    public override void Setup(Vector2 shootDirection, Vector2 shipVelocity, ShipBase source)
    {
        
    }

    //private Vector2 GetTargetLeadingPosition(Vector2 targetAcceleration, int iterations)
    //{

    //}
}
