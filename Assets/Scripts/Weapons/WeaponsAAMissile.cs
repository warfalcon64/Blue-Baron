using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponsAAMissile : WeaponsBase
{
    private float seekerFieldOfView = 1;
    private Vector2 aimPos;
    private Vector2 velocity;
    private Vector2 relativeVelocity;

    private VFXManager vfxManager;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        vfxManager = SceneManager.Instance.vfxManager.GetComponent<VFXManager>();
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
        float distance = Vector2.Distance(rb.position, targetRb.position);
        float travelTime = distance / speed;
        aimPos = targetRb.position + targetRb.velocity * travelTime;
    }

    private void TurnMissile()
    {
        Vector2 heading = aimPos - rb.position;
        heading.Normalize();
        float rotateAmount = Vector3.Cross(heading, transform.up).z;
        rb.angularVelocity = -rotateAmount * turnSpeed;
    }

    public override void Setup(Vector2 shootDirection, Vector2 shipVelocity, ShipBase source)
    {
        this.source = source;
        relativeVelocity = shipVelocity;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Rigidbody2D>() != null && !collision.CompareTag(tag))
        {
            Vector3 hitPos = transform.position;
            vfxManager.PlayVFX(VFXManager.VFXType.Explosion, hitPos);
            Destroy(gameObject);
        }
    }
}
