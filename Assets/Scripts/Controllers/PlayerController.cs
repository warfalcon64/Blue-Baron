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
    private float secondaryCoolDown;
    private float secondaryNextFire;
   
    private Vector2 worldMousePosition;
    private Vector3 mousePosition;

    private SceneManager sceneManager;
    private PlayerLockOnSystem playerLockOnSystem;
    private WeaponMap weaponMap;
    private ShipBase ship;
    Rigidbody2D rb;

    public event EventHandler OnRadarSelectionEnabled;
    public event EventHandler OnRadarSelectionDisabled;
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
        secondaryCoolDown = ship.GetSecondaryCoolDown();

        sceneManager = SceneManager.Instance;
        playerLockOnSystem = sceneManager.playerManager.GetComponent<PlayerLockOnSystem>();
    }

    // Update is called once per frame
    private void Update()
    {
        // Horizontal axis = A, D  Vertical Axis = W, S
        turn = -Input.GetAxisRaw("Horizontal");
        acceleration = Input.GetAxisRaw("Vertical");


        if (Input.GetKey(KeyCode.LeftShift))
        {
            OnRadarSelectionEnabled?.Invoke(this, EventArgs.Empty);
            shootMode = ShootType.None;
            playerLockOnSystem.DisableLocking();
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            OnRadarSelectionDisabled?.Invoke(this, EventArgs.Empty);
            playerLockOnSystem.EnableLocking();
        }
        else if (Input.GetMouseButton(0))
        {
            shootMode = ShootType.Primary;

        }
        else if (Input.GetMouseButton(1)) // * Maybe make it possible to shoot both primary and secondary weapons at same time in the future
        {
            shootMode = ShootType.Secondary;
        }
        else
        {
            shootMode = ShootType.None;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            playerLockOnSystem.ToggleRadarLock(); // * Switch to event instead of direct reference
        }

        UpdateTimers();

        // Calculate mouse position on screen
        mousePosition = Input.mousePosition;
        mousePosition.z = Camera.main.nearClipPlane;
        worldMousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
        
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
        shootProjectiles();
    }

    private void shootProjectiles()
    {
        GameObject lockedEnemy = playerLockOnSystem.GetLockedEnemy();
        Vector2 aimPos;

        if (lockedEnemy != null && lockedEnemy.activeSelf)
        {
            aimPos = playerLockOnSystem.GetLead();
        }
        else
        {
            aimPos = worldMousePosition;
        }
        
        Vector2 shootDirection = (aimPos - rb.position).normalized;
        float angle = Vector2.Angle((Vector2)transform.up, shootDirection);
        //print(angle);

        if (shootMode == ShootType.Primary && angle <= primaryFieldOfFire && primaryNextFire <= 0)
        {
            //shootLaser();
            ship.ShootPrimary(aimPos);
            primaryNextFire = primaryCoolDown;
        }
        else if (shootMode == ShootType.Secondary && secondaryNextFire <= 0)
        {
            ship.ShootSecondary(aimPos);
            secondaryNextFire = secondaryCoolDown;
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

        if (secondaryNextFire > 0)
        {
            secondaryNextFire -= Time.deltaTime;
        }
    }
}
