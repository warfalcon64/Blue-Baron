using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Common;

public class PlayerController : MonoBehaviour
{
    private ShipType shipType;

    private float turn; // Input axis for turning (-1 turns left, 1 turns right)
    private float acceleration; // Input axis for forward movement (-1 slows down, 1 speeds up)

    private Vector2 worldMousePosition;
    private Vector3 mousePosition;

    private SceneManager sceneManager;
    private PlayerLockOnSystem playerLockOnSystem;
    private ShipBase ship;
    Rigidbody2D rb;

    public event EventHandler OnRadarSelectionEnabled;
    public event EventHandler OnRadarSelectionDisabled;
    public event EventHandler SwapShip;

    // Start is called before the first frame update
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        ship = GetComponent<ShipBase>();
        ship.isPlayer = true;
        shipType = ship.GetShipType();

        sceneManager = SceneManager.Instance;
        playerLockOnSystem = sceneManager.playerManager.GetPlayerLockOnSystem();
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
            playerLockOnSystem.DisableLocking();
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            OnRadarSelectionDisabled?.Invoke(this, EventArgs.Empty);
            playerLockOnSystem.EnableLocking();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            playerLockOnSystem.LockClosestEnemy();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            ship.ActivateCombatSystem();
        }

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
        if (Input.GetKey(KeyCode.LeftShift)) return;

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

        if (Input.GetMouseButton(0))
        {
            ship.FireGroup(0, aimPos);
        }

        if (Input.GetMouseButton(1))
        {
            ship.FireGroup(1, aimPos);
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
}
