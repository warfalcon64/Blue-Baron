using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float turnSpeed = 2f;
    [SerializeField] private float fieldOfFire; // This is the angle between the line bisecting the craft vertically and the line limiting the field of fire
    [SerializeField] private float primaryCoolDown;
    [SerializeField] private Transform pfBlueLaser; // Laser gameobject for the ship to shoot
    [SerializeField] private Transform leftGun; // Positions to shoot lasers from
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
   
    private float turn; // Input axis for turning (-1 turns left, 1 turns right)
    private float minTurn; // Speeds for turning and forward movement
    private float maxTurn;
    private float minSpeed;
    private float maxSpeed;
    private float acceleration; // Input axis for forward movement (-1 slows down, 1 speeds up)
    
    private float nextFire; // In game time for next moment lasers can fire
    private bool leftFire;
   
    private Vector2 worldMousePosition;
    private Vector3 mousePosition;


    Rigidbody2D rb;
    // Start is called before the first frame update
    private void Awake()
    {
        minSpeed = speed / 2;
        maxSpeed = speed;
        minTurn = turnSpeed;
        maxTurn = turnSpeed + 1;
        shootMode = shootType.None;
        leftFire = false;
        nextFire = 0f;
        ship = shipType.Fighter;
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    private void Update()
    {
        // Horizontal axis = A, D  Vertical Axis = W, S
        turn = -Input.GetAxisRaw("Horizontal");
        acceleration = Input.GetAxisRaw("Vertical");

        if (Input.GetMouseButton(0))
        {
            shootMode = shootType.Primary;
        }
        else
        {
            shootMode = shootType.None;
        }

        // Calculate mouse position on screen
        mousePosition = Input.mousePosition;
        mousePosition.z = Camera.main.nearClipPlane;
        worldMousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
    }

    private void FixedUpdate()
    {
        switch (ship)
        {
            case shipType.Fighter:
                fighterControl();
                break;
        }

    }

    private void fighterControl()
    {

        if (acceleration < 0f && speed > minSpeed)
        {
            speed -= 0.2f;

            if (turnSpeed < maxTurn)
            {
                turnSpeed += 0.1f;
            }
        } else if (acceleration > 0f && speed < maxSpeed)
        {
            speed += 0.2f;

            if (turnSpeed > minTurn)
            {
                turnSpeed -= 0.1f;
            }
        }

        rb.velocity = transform.up * speed;
        rb.MoveRotation(rb.rotation + (turnSpeed * turn));
        shootProjectiles();
    }

    private void shootProjectiles()
    {
        Vector2 shootDirection = (worldMousePosition - rb.position).normalized;
        float angle = Vector2.Angle((Vector2)transform.up, shootDirection);
        //print(angle);

        if (shootMode == shootType.Primary && angle <= fieldOfFire && nextFire <= Time.time)
        {
            shootLaser();
            nextFire = Time.time + primaryCoolDown;
        }
    }

    private void shootLaser()
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

        // Create laser and call its setup function from the script attached to it
        Vector2 shootDirection = (worldMousePosition - laserSpawn).normalized;
        Transform bulletClone = Instantiate(pfBlueLaser, laserSpawn, leftGun.rotation);
        bulletClone.GetComponent<Laser>().setup(shootDirection, rb.velocity);
    }
}
