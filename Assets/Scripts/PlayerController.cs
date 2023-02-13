using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float turnSpeed;
    [SerializeField] private float fieldOfFire; // This is the angle between the line bisecting the craft vertically and the line limiting the field of fire
    [SerializeField] private float primaryCoolDown;
    [SerializeField] private Transform pfBlueLaser;
    [SerializeField] private Transform leftGun;
    [SerializeField] private Transform rightGun;


    private enum shipType
    {
        Fighter,
        Bomber
    }

    private enum shootType
    {
        Primary,
        Secondary,
        None
    }

    private shipType ship;
    private shootType shootMode;
    private float turn;
    private float nextFire;
    private bool shootLMB;
    private bool leftFire;
    private Vector2 worldMousePosition;


    Rigidbody2D rb;
    // Start is called before the first frame update
    private void Awake()
    {
        shootMode = shootType.None;
        leftFire = false;
        nextFire = 0f;
        ship = shipType.Fighter;
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    private void Update()
    {
        turn = -Input.GetAxisRaw("Horizontal");

        if (Input.GetMouseButton(0))
        {
            shootMode = shootType.Primary;
        }
        else
        {
            shootMode = shootType.None;
        }

        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Camera.main.nearClipPlane;
        worldMousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

    }

    private void FixedUpdate()
    {
        // Maybe use switch here
        if (ship == shipType.Fighter)
        {
            rb.velocity = transform.up * speed * Time.deltaTime;
            rb.MoveRotation(rb.rotation + (turnSpeed * turn));

            shootProjectiles(worldMousePosition);
        }
    }

    private void shootProjectiles(Vector2 mousePosition)
    {
        Vector2 shootDirection = (mousePosition - rb.position).normalized;
        float angle = Vector2.Angle((Vector2)transform.up, shootDirection);

        if (shootMode == shootType.Primary && angle <= fieldOfFire && nextFire <= Time.time)
        {
            shootLaser(worldMousePosition);
            nextFire = Time.time + primaryCoolDown;
        }
    }

    private void shootLaser(Vector2 mousePosition)
    {
        Vector2 laserSpawn;
        laserSpawn = leftGun.position;

        if (!leftFire)
        {
            leftFire = true;
            laserSpawn = leftGun.position;
        }
        else
        {
            laserSpawn = rightGun.position;
            leftFire = false;
        }

        Vector2 shootDirection = (mousePosition - laserSpawn).normalized;
        Transform bulletClone = Instantiate(pfBlueLaser, laserSpawn, leftGun.rotation);
        bulletClone.GetComponent<Laser>().setup(shootDirection, rb.velocity);

    }
}
