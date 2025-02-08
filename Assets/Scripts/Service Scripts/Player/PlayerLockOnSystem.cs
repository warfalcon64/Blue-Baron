using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLockOnSystem : MonoBehaviour
{
    [SerializeField] private float lockOnTime = 2.0f;
    [SerializeField] private float lockRadius = 1.5f;
    [SerializeField] private float lockDecay = 0.1f; // maybe remove this
    public LayerMask enemyLayer;
    public Color gizmoColor = Color.green;

    private bool lockingEnabled;
    private Vector2 lead;
    private GameObject lockedEnemy;
    private SceneManager sceneManager;
    private PlayerController pc;
    private ShipBase playerShip;
    private Dictionary<GameObject, float> enemyLockTimers = new Dictionary<GameObject, float>();

    // *** THIS SCRIPT NEEDS TO SUBSCRIBE TO PLAYER DEATH EVENT: NULL LOCKED ENEMY UPON PLAYER DEATH

    private void Start()
    {
        lead = Vector2.zero;
        lockingEnabled = true;
        lockedEnemy = null;
        sceneManager = SceneManager.Instance;
        sceneManager.PlayerDeath += DisableLocking;
        sceneManager.PlayerRebirth += EnableLocking;
    }

    private void Update()
    {
        if ((lockedEnemy == null || !lockedEnemy.activeSelf) && lockingEnabled)
        {
            UpdateHoverState();
        }

        if (pc != null)
        {
            playerShip = pc.gameObject.GetComponent<ShipBase>();
            playerShip.OnSeekerFired += HandleSeekerFired;
        }
    }

    private void FixedUpdate()
    {
        if (lockedEnemy != null && lockedEnemy.activeSelf && lockingEnabled)
        {
            CalculateLead();
        }
    }

    public void HandleSeekerFired(object source, WeaponsBase seeker)
    {
        seeker.SetTarget(lockedEnemy);
    }

    private void UpdateHoverState()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] detectedShips = Physics2D.OverlapCircleAll(mousePos, lockRadius, enemyLayer);

        foreach (Collider2D ship in detectedShips)
        {
            if (ship.gameObject.CompareTag(sceneManager.shipData.blueTag))
            {
                continue;
            }

            GameObject enemy = ship.gameObject;

            if (!enemyLockTimers.ContainsKey(enemy))
            {
                enemyLockTimers[enemy] = 0f;
            }

            enemyLockTimers[enemy] += Time.deltaTime;

            if (enemyLockTimers[enemy] >= lockOnTime)
            {
                lockedEnemy = enemy;
                enemyLockTimers.Clear();
            }
        }
    }

    private void CalculateLead()
    {
        GameObject player = pc.gameObject;
        ShipBase playerShip = player.GetComponent<ShipBase>();
        Tuple<Transform, Transform> gunPositions = playerShip.GetPrimaryFirePositions();
        Rigidbody2D targetRb = lockedEnemy.GetComponent<Rigidbody2D>();
        float primarySpeed = playerShip.GetPrimaryWeapon().GetSpeed();
        Vector2 avgGunPos = (gunPositions.Item1.position + gunPositions.Item2.position) / 2;
        float distance = Vector2.Distance(avgGunPos, targetRb.position);
        float travelTime = distance / primarySpeed;
        lead = targetRb.position + targetRb.velocity * travelTime;
    }

    public void HandleRadarPingSelect(object sender, ShipBase ship)
    {
        lockedEnemy = ship.gameObject;
    }

    private void DisableLocking(object sender, ShipBase ship)
    {
        lockingEnabled = false;
    }

    private void EnableLocking(object sender, ShipBase ship)
    {
        lockingEnabled = true;
        pc = sceneManager.GetPlayerController();
    }
    public void ToggleRadarLock()
    {
        lockedEnemy = null;
        lead = Vector2.zero;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Gizmos.DrawWireSphere(mousePosition, lockRadius);
    }

    public GameObject GetLockedEnemy()
    {
        return lockedEnemy;
    }

    public float GetLockRadius()
    {
        return lockRadius;
    }

    public Vector2 GetLead()
    {
        return lead;
    }

    public bool IsLockingEnabled()
    {
        return lockingEnabled;
    }

    public void SetPlayerController(PlayerController newPc)
    {
        pc = newPc;
    }

    public void DisableLocking()
    {
        lockingEnabled = false;
    }

    public void EnableLocking()
    {
        lockingEnabled = true;
    }
}
