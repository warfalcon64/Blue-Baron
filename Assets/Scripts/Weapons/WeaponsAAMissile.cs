using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponsAAMissile : WeaponsBase
{
    [Header("Missile Attributes")]
    public float leadTimePercentage = 0.8f;
    public  float maxTimePrediction = 2.0f;

    private float seekerFieldOfView = 1;
    private Vector2 aimPos;
    private Vector2 velocity;
    private Vector2 relativeVelocity;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (target != null)
        {
            PredictMovement();
        }
        TurnMissile();
        rb.velocity = (Vector2)transform.up * (speed);
    }

    private void PredictMovement()
    {
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        float predictionTime = Mathf.Lerp(0, maxTimePrediction, leadTimePercentage);
        aimPos = targetRb.position + targetRb.velocity * predictionTime;
    }

    private void TurnMissile()
    {
        Vector2 heading = aimPos - rb.position;

        float angle = Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg;
        float currentAngle = rb.rotation;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, angle, turnSpeed * Time.deltaTime);
        Debug.DrawLine(rb.position, aimPos);
        
        rb.MoveRotation(newAngle);
    }

    public override void Setup(Vector2 shootDirection, Vector2 shipVelocity, ShipBase source)
    {
        relativeVelocity = shipVelocity;
        Destroy(gameObject, lifetime);
    }

    //private Vector2 GetTargetLeadingPosition(Vector2 targetAcceleration, int iterations)
    //{

    //}
}
