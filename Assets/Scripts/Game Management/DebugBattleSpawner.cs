using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class DebugBattleSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject blueFighterPrefab;
    [SerializeField] private GameObject redFighterPrefab;

    [Header("Team Sizes")]
    [SerializeField, Range(1, 500)] private int blueTeamSize = 3;
    [SerializeField, Range(1, 500)] private int redTeamSize = 3;

    [Header("Squads")]
    [SerializeField, Range(1, 50)] private int squadSize = 15;
    // A 15-ship vee at formationSpacing=5 is ~70 wide x ~35 deep — separation must clear that.
    [SerializeField] private float squadSeparation = 80f;
    [SerializeField] private float formationSpacing = 5f;

    [Header("Layout")]
    [SerializeField] private float teamSeparation = 120f;

    [Header("Player")]
    [SerializeField] private bool assignPlayer = true;

    [Header("References")]
    [SerializeField] private SceneManager sceneManager;

    private void Awake()
    {
        DestroyExistingShips(sceneManager.blueShips);
        DestroyExistingShips(sceneManager.redShips);

        // Blue team: centered at -X, facing +X (rotation -90 in this Y-up convention).
        List<ShipBase> blueShips = SpawnTeam(
            blueFighterPrefab,
            blueTeamSize,
            new Vector2(-teamSeparation / 2f, 0f),
            -90f,
            "Blue",
            sceneManager.shipData.blueTag
        );

        // Red team: centered at +X, facing -X (rotation +90).
        List<ShipBase> redShips = SpawnTeam(
            redFighterPrefab,
            redTeamSize,
            new Vector2(teamSeparation / 2f, 0f),
            90f,
            "Red",
            sceneManager.shipData.redTag
        );

        // Player takes the first blue ship and is excluded from squad membership for now —
        // squad/player integration (anchor adaptation, ship-swap) is a later step.
        if (assignPlayer && blueShips.Count > 0)
        {
            ShipBase playerShip = blueShips[0];

            FighterController fc = playerShip.GetComponent<FighterController>();
            if (fc != null)
            {
                if (fc.squad != null)
                    fc.squad.UnregisterMember(fc);
                fc.enabled = false;
            }

            playerShip.gameObject.AddComponent<PlayerController>();
            playerShip.isPlayer = true;
        }

        sceneManager.blueShips = blueShips;
        sceneManager.redShips = redShips;
    }

    private List<ShipBase> SpawnTeam(GameObject prefab, int count, Vector2 center, float rotationZ, string teamLabel, string teamTag)
    {
        List<ShipBase> ships = new List<ShipBase>(count);
        Transform teamParent = new GameObject($"{teamLabel} Team").transform;

        // Heading vector (+Y rotated by rotationZ). Used both for ship facing and for vee orientation.
        float headingRad = (rotationZ + 90f) * Mathf.Deg2Rad;
        Vector2 heading = new Vector2(Mathf.Cos(headingRad), Mathf.Sin(headingRad));
        Quaternion rot = Quaternion.Euler(0f, 0f, rotationZ);

        // Lateral axis along which squad centers are spaced (perpendicular to heading).
        Vector2 lateral = new Vector2(-heading.y, heading.x);

        int numSquads = Mathf.CeilToInt((float)count / squadSize);
        int spawnedSoFar = 0;

        for (int s = 0; s < numSquads; s++)
        {
            int squadShipCount = Mathf.Min(squadSize, count - spawnedSoFar);

            float squadLateralOffset = (s - (numSquads - 1) / 2f) * squadSeparation;
            Vector2 squadCenter = center + lateral * squadLateralOffset;

            GameObject squadObj = new GameObject($"{teamLabel} Squad {s + 1}");
            squadObj.transform.SetParent(teamParent);
            squadObj.transform.position = squadCenter;
            Squad squad = squadObj.AddComponent<Squad>();
            squad.teamTag = teamTag;
            sceneManager.RegisterSquad(squad);

            for (int i = 0; i < squadShipCount; i++)
            {
                Vector2 slotLocal = Squad.ComputeVeeSlotCentered(i, squadShipCount, formationSpacing);
                Vector2 slotWorld = squadCenter + RotateLocalToWorld(slotLocal, heading);

                GameObject shipObj = Instantiate(prefab, slotWorld, rot, squadObj.transform);
                shipObj.name = $"{teamLabel} {prefab.name} {spawnedSoFar + 1}";

                ShipBase ship = shipObj.GetComponent<ShipBase>();
                ships.Add(ship);

                FighterController fc = ship.GetComponent<FighterController>();
                if (fc != null)
                    squad.RegisterMember(fc);

                spawnedSoFar++;
            }
        }

        return ships;
    }

    private static Vector2 RotateLocalToWorld(Vector2 local, Vector2 heading)
    {
        Vector2 right = new Vector2(heading.y, -heading.x);
        return right * local.x + heading * local.y;
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
