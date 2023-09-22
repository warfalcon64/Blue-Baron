using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static UnityEngine.Rendering.DebugUI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] public float health = 100f;
    [SerializeField] public float shield = 0f;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float turnSpeed = 2f;
    [SerializeField] private float fieldOfFire; // This is the angle between the line bisecting the craft vertically and the line limiting the field of fire
    [SerializeField] private float primaryCoolDown;
    [SerializeField] private Transform pfBlueLaser; // Laser gameobject for the ship to shoot
    [SerializeField] private Transform leftGun; // Positions to shoot lasers from
    [SerializeField] private Transform rightGun;

    [SerializeField] private Transform vfxManager;
    [SerializeField] private Transform smoke;


    private enum shipType
    {
        Fighter,
        Bomber
    }

    private shipType ship;
    private ShootType shootMode;

    private float lowHealth;
    private float turn; // Input axis for turning (-1 turns left, 1 turns right)
    private float minTurn; // Speeds for turning and forward movement
    private float maxTurn;
    private float minSpeed;
    private float maxSpeed;
    private float acceleration; // Input axis for forward movement (-1 slows down, 1 speeds up)
    
    private float nextFire;
    private bool leftFire;
    private bool isSmoking;
   
    private Vector2 worldMousePosition;
    private Vector3 mousePosition;

    private VisualEffect mainEffects;
    private VisualEffect shipEffects;

    SectorObjectPool objectPool;


    Rigidbody2D rb;
    // Start is called before the first frame update
    private void Awake()
    {
        minSpeed = speed / 2;
        maxSpeed = speed;
        minTurn = turnSpeed;
        maxTurn = turnSpeed + 1;
        shootMode = ShootType.None;
        leftFire = false;
        isSmoking = false;
        nextFire = 0f;
        ship = shipType.Fighter;

        rb = GetComponent<Rigidbody2D>();
        mainEffects = vfxManager.GetComponent<VisualEffect>();
        shipEffects = smoke.GetComponent<VisualEffect>();

        switch (ship)
        {
            case shipType.Fighter:
                lowHealth = health * 0.25f;
                break;

            default:
                print("Error: Player ship type is undefined!");
                break;
        }
    }

    // Update is called once per frame
    private void Update()
    {
        // Horizontal axis = A, D  Vertical Axis = W, S
        turn = -Input.GetAxisRaw("Horizontal");
        acceleration = Input.GetAxisRaw("Vertical");

        if (Input.GetMouseButton(0))
        {
            shootMode = ShootType.Primary;
        }
        else
        {
            shootMode = ShootType.None;
        }

        UpdateSmoke();
        UpdateTimers();

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

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.GetComponent<Rigidbody2D>() != null && !collider.CompareTag(tag))
        {
            // Grab the vfx attached to whatever weapon just hit the ship, and create an attribute to send to vfx
            VFXEventAttribute eventAttribute = mainEffects.CreateVFXEventAttribute();

            // Get the ID of the property we want to modify
            int vfxPosition = Shader.PropertyToID("Position");
            // Set the property, and send event with the attribute carrying the info to the vfx graph
            mainEffects.SetVector3(vfxPosition, transform.position);
            mainEffects.SendEvent("LaserHit", eventAttribute);

            string type = collider.GetComponent<WeaponsBase>().damageType;
            float damage = collider.GetComponent<WeaponsBase>().getDamage();

            // Determine the type of damage weapon deals, apply modifiers accordingly
            switch (type)
            {
                case "Plasma":
                    if (shield <= 0) damage *= 2;
                    break;

                default:
                    print("DID NOT APPLY DAMAGE CORRECTLY");
                    break;
            }

            health -= damage;

            if (health <= lowHealth && !isSmoking)
            {
                isSmoking = true;
                shipEffects.SendEvent("OnDamage");
            }
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

        if (shootMode == ShootType.Primary && angle <= fieldOfFire && nextFire <= 0)
        {
            shootLaser();
            nextFire = primaryCoolDown;
        }
    }

    private void shootLaser()
    {
        Vector2 plasmaSpawn = leftGun.position;
        leftFire = !leftFire;

        if (!leftFire) plasmaSpawn = rightGun.position;

        // Create laser and call its setup function from the script attached to it
        Vector2 shootDirection = (worldMousePosition - plasmaSpawn).normalized;
        Transform bulletClone = Instantiate(pfBlueLaser, plasmaSpawn, leftGun.rotation);
        bulletClone.transform.GetComponent<WeaponsPlasma>().setup(shootDirection, rb.velocity);
    }

    private void UpdateSmoke()
    {
        if (!isSmoking) return;

        int lifetime = Shader.PropertyToID("SmokeLifetime");

        if (speed < 6.1f)
        {
            shipEffects.SetFloat(lifetime, 1.2f);
        }
        else if (speed < 7.4f)
        {
            shipEffects.SetFloat(lifetime, 1f);
        }
        else if (speed < 10f)
        {
            shipEffects.SetFloat(lifetime, 0.9f);
        }
        else if (speed == 10f)
        {
            shipEffects.SetFloat(lifetime, 0.85f);
        }
    }

    private void UpdateTimers()
    {
        if (nextFire > 0)
        {
            nextFire -= Time.deltaTime;
        }
    }
}
