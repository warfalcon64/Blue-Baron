using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class DebugBattleSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject blueFighterPrefab;
    [SerializeField] private GameObject redFighterPrefab;

    [Header("Team Sizes")]
    [SerializeField, Range(1, 100)] private int blueTeamSize = 3;
    [SerializeField, Range(1, 100)] private int redTeamSize = 3;

    [Header("Formation")]
    [SerializeField] private float shipSpacing = 5f;
    [SerializeField] private float teamSeparation = 40f;
    [SerializeField, Range(1, 10)] private int maxColumns = 5;

    [Header("Player")]
    [SerializeField] private bool assignPlayer = true;

    [Header("References")]
    [SerializeField] private SceneManager sceneManager;

    private void Awake()
    {
        // Destroy any manually placed ships before spawning
        DestroyExistingShips(sceneManager.blueShips);
        DestroyExistingShips(sceneManager.redShips);

        // Blue team: centered at -X, facing right (rotation -90)
        List<ShipBase> blueShips = SpawnTeam(
            blueFighterPrefab,
            blueTeamSize,
            new Vector2(-teamSeparation / 2f, 0f),
            -90f,
            "Blue"
        );

        // Red team: centered at +X, facing left (rotation +90)
        List<ShipBase> redShips = SpawnTeam(
            redFighterPrefab,
            redTeamSize,
            new Vector2(teamSeparation / 2f, 0f),
            90f,
            "Red"
        );

        // Assign player to first blue ship
        if (assignPlayer && blueShips.Count > 0)
        {
            ShipBase playerShip = blueShips[0];

            AIControllerBase aiController = playerShip.GetComponent<AIControllerBase>();
            if (aiController != null)
                aiController.enabled = false;

            BehaviorGraphAgent graphAgent = playerShip.GetComponent<BehaviorGraphAgent>();
            if (graphAgent != null)
                graphAgent.enabled = false;

            PlayerController pc = playerShip.gameObject.AddComponent<PlayerController>();
            playerShip.isPlayer = true;
        }

        // Override SceneManager lists (when spawner is disabled, Inspector lists work as-is)
        sceneManager.blueShips = blueShips;
        sceneManager.redShips = redShips;
    }

    private List<ShipBase> SpawnTeam(GameObject prefab, int count, Vector2 center, float rotationZ, string teamLabel)
    {
        List<ShipBase> ships = new List<ShipBase>(count);

        // Create a parent object for organization
        Transform parent = new GameObject($"{teamLabel} Team").transform;

        for (int i = 0; i < count; i++)
        {
            int col = i % maxColumns;
            int row = i / maxColumns;

            // Number of ships in this row
            int colsInRow = Mathf.Min(maxColumns, count - row * maxColumns);

            // Center each row: offset so the row is symmetric around Y=0
            float yOffset = (col - (colsInRow - 1) / 2f) * shipSpacing;

            // Rows stack along the facing direction (X axis away from enemy)
            // Negative sign so later rows are further from the enemy
            float xOffset = -row * shipSpacing * Mathf.Sign(center.x);

            Vector2 pos = center + new Vector2(xOffset, yOffset);
            Quaternion rot = Quaternion.Euler(0f, 0f, rotationZ);

            GameObject shipObj = Instantiate(prefab, pos, rot, parent);
            shipObj.name = $"{teamLabel} {prefab.name} {i + 1}";

            ShipBase ship = shipObj.GetComponent<ShipBase>();
            ships.Add(ship);
        }

        return ships;
    }

    private void DestroyExistingShips(List<ShipBase> ships)
    {
        foreach (ShipBase ship in ships)
        {
            if (ship != null)
                Destroy(ship.gameObject);
        }
    }
}
