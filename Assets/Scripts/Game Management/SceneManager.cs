using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Common;
using Unity.VisualScripting;
using Random = UnityEngine.Random;

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

    [Header("Cameras")]
    public FollowPlayer mainCam;

    private int playerIndex;

    public event EventHandler<NewShipArgs> PlayerSwapped;

    public class NewShipArgs : EventArgs
    {
        public ShipBase ship;
    }

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

    // Swap player to different ship within their own team
    // * Note that when player dies they are randomly assigned to a ship to spectate
    public void SwapPlayerShip(object sender, KeyPressedEventArgs k)
    {
        bool enablePlayerControl = false;
        // * Might not need this
        for (int i = 0; i < blueShips.Count; i++)
        {
            if (blueShips[i].isPlayer)
            {
                playerIndex = i;
            }
        }

        switch (k.KeyCode)
        {
            case KeyCode.A:
                if (playerIndex - 1 >= 0)
                {
                    playerIndex--;
                }
                else
                {
                    playerIndex = blueShips.Count - 1;
                }
                break;

            case KeyCode.D:
                if (playerIndex + 1 <= (blueShips.Count - 1))
                {
                    playerIndex++;
                }
                else
                {
                    playerIndex = 0;
                }
                break;

            case KeyCode.Space:
                enablePlayerControl = true;
                break;

            default:
                playerIndex = Random.Range(0, blueShips.Count);
                break;
        }

        ShipBase destShip = blueShips[playerIndex];
        mainCam.player = destShip;

        if (enablePlayerControl)
        {
            destShip.GetComponent<AIControllerBase>().enabled = false;
            PlayerController pc = destShip.AddComponent<PlayerController>();
            pc.TransferShipValues(destShip);
        }
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
