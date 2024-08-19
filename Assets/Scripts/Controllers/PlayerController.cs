using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static UnityEngine.Rendering.DebugUI;

using Common;

public class PlayerController : MonoBehaviour
{
    private ShipType shipType;
    private ShootType shootMode;

    private float primaryFieldOfFire;
    private float turn; // Input axis for turning (-1 turns left, 1 turns right)
    private float acceleration; // Input axis for forward movement (-1 slows down, 1 speeds up)
    
    private float primaryCoolDown;
    private float primaryNextFire;
   
    private Vector2 worldMousePosition;
    private Vector3 mousePosition;


    private WeaponMap weaponMap;
    private ShipBase ship;
    Rigidbody2D rb;

    public event EventHandler SwapShip;

    // Start is called before the first frame update
    private void Awake()
    {
        shootMode = ShootType.None;
        primaryNextFire = 0f;

        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        ship = GetComponent<ShipBase>();
        ship.isPlayer = true;
        weaponMap = ship.GetWeaponMap();
        shipType = ship.GetShipType();

        // Get ship variables
        primaryCoolDown = ship.GetPrimaryCoolDown();
        primaryFieldOfFire = ship.GetPrimaryFieldOfFire();

        //switch (shipType)
        //{
        //    case ShipType.Fighter:
        //        lowHealth = health * 0.25f;
        //        break;

        //    default:
        //        print("Error: Player ship type is undefined!");
        //        break;
        //}
    }

    // Update is called once per frame
    private void Update()
    {
        // Horizontal axis = A, D  Vertical Axis = W, S
        turn = -Input.GetAxisRaw("Horizontal");
        acceleration = Input.GetAxisRaw("Vertical");

        if (shipType == ShipType.Spectator)
        {
            SpectatorMode();
        }
        else
        {

            if (Input.GetMouseButton(0))
            {
                shootMode = ShootType.Primary;

            }
            else
            {
                shootMode = ShootType.None;
            }

            UpdateTimers();

            // Calculate mouse position on screen
            mousePosition = Input.mousePosition;
            mousePosition.z = Camera.main.nearClipPlane;
            worldMousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
        }
    }

    private void FixedUpdate()
    {
        switch (shipType)
        {
            case ShipType.Fighter:
                fighterControl();
                break;
            case ShipType.Spectator:
                break;
        }

    }

    private void fighterControl()
    {

        if (acceleration < 0f)
        {
            ship.Decelerate(0.2f);
        } 
        else if (acceleration > 0f)
        {
            ship.Accelerate(0.2f);
        }

        ship.SetShipTurn(turn);
        //rb.velocity = transform.up * speed;
        //rb.MoveRotation(rb.rotation + (turnSpeed * turn));
        shootProjectiles();
    }

    // *** THIS DOESN'T ACTUALLY WORK, SEND EVENT TO SCENE MANAGER TO HANDLE SHIP SWAPPING INPUT
    private void SpectatorMode()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            SwapShip?.Invoke(this, new KeyPressedEventArgs(KeyCode.A));
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            SwapShip?.Invoke(this, new KeyPressedEventArgs(KeyCode.D));
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            SwapShip?.Invoke(this, new KeyPressedEventArgs(KeyCode.Space));
        }
    }

    private void shootProjectiles()
    {
        Vector2 shootDirection = (worldMousePosition - rb.position).normalized;
        float angle = Vector2.Angle((Vector2)transform.up, shootDirection);
        //print(angle);

        if (shootMode == ShootType.Primary && angle <= primaryFieldOfFire && primaryNextFire <= 0)
        {
            //shootLaser();
            ship.ShootPrimary(worldMousePosition);
            primaryNextFire = primaryCoolDown;
        }
    }

    public void TransferShipValues(ShipBase newShip)
    {
        ship = newShip;
    }

    public void OnPlayerDeath()
    {
        SwapShip?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateTimers()
    {
        if (primaryNextFire > 0)
        {
            primaryNextFire -= Time.deltaTime;
        }
    }
}
