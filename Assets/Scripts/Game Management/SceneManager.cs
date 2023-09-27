using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    // Make sure that anything can read variables of this class, but variables can only be set internally
    public static SceneManager Instance { get; private set; }

    [Header("Blue Team")]
    public List<ShipBase> blueShips = new List<ShipBase>();
    public List<ShipBase> deadBlueShips;

    [Header("Red Team")]
    public List<ShipBase> redShips = new List<ShipBase>();
    public List<ShipBase> deadRedShips;

    [Header("Managers")]
    public GameObject vfxManager;

    [Header("Data")]
    public ShipData shipData;

    void Awake()
    {
        // Make sure there is only 1 copy of scene manager at any given time
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // Add scene manager as a listener to ship death events
        foreach (ShipBase ship in blueShips)
        {
            ship.OnShipDeath += RecordDeadShip;
        }

        foreach (ShipBase ship in redShips)
        {
            ship.OnShipDeath += RecordDeadShip;
        }

        deadBlueShips = new List<ShipBase>();
        
        deadRedShips = new List<ShipBase>();
    }

    // Respond to ship death event
    private void RecordDeadShip(object sender, EventArgs e)
    {
        ShipBase ship = (ShipBase)sender;

        if (ship.CompareTag(shipData.blueTag))
        {
            blueShips.Remove(ship);
            deadBlueShips.Add(ship);
        }
        else
        {
            redShips.Remove(ship);
            deadRedShips.Add(ship);
        }

        // Remove scene manager as listener to ship's death event
        ship.OnShipDeath -= RecordDeadShip;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public List<ShipBase> GetLiveEnemies(string team)
    {
        if (team.Equals(shipData.blueTag))
        {
            return redShips;
        }
        else
        {
            return blueShips;
        }
    }

    public ShipData GetShipData()
    {
        return shipData;
    }

    public GameObject GetVFXManager()
    {
        return vfxManager;
    }

    public void EnterSpectateMode()
    {

    }

    public void SwapShip()
    {

    }
}
