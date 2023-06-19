using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class Laser : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] float damage;

    VisualEffect spark;

    Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spark = GetComponent<VisualEffect>();
    }

    public void setup(Vector2 shootDirection, Vector2 shipVelocity) 
    {
        // convert shootDirection to an angle
        shootDirection = shootDirection.normalized;
        float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;

        rb.rotation = angle;
        rb.velocity = (shootDirection * speed) + shipVelocity;

        Destroy(gameObject, 10f);
    }

    private void OnTriggerEnter2D(Collider2D collider) 
    {
        // Checks if target is valid and not an ally
        if (collider.gameObject.GetComponent<Rigidbody2D>() != null && !collider.gameObject.CompareTag(tag))
        {
            spark.SendEvent("LaserHit");
            
            // Damage collider here
            //Destroy(gameObject);
            
        }
    }

    public float getSpeed()
    {
        return speed;
    }

}
