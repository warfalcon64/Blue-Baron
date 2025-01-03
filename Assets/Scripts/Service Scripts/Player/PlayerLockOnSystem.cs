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

    private Vector2 lead;
    private GameObject lockedEnemy;
    private SceneManager sceneManager;
    private PlayerController pc;
    private Dictionary<GameObject, float> enemyLockTimers = new Dictionary<GameObject, float>();

    private void Start()
    {
        lead = Vector2.zero;
        lockedEnemy = null;
        sceneManager = SceneManager.Instance;
        pc = sceneManager.GetPlayerController();
    }

    private void Update()
    {
        if (lockedEnemy == null)
        {
            UpdateHoverState();
        }

        if (pc == null)
        {
            pc = sceneManager.GetPlayerController();
        }
    }

    private void FixedUpdate()
    {
        if (lockedEnemy != null)
        {
            CalculateLead();
        }
    }

    private void UpdateHoverState()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] detectedShips = Physics2D.OverlapCircleAll(mousePos, lockRadius);
        //print(detectedShips.Length);

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
                print($"Locked onto {lockedEnemy.name}");
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

    public void ToggleRadarLock(object sender, EventArgs e)
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
}
