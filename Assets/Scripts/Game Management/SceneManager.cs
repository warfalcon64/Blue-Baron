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
    private bool inSwapMode = false;

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

    private void Start()
    {
        for (int i = 0; i < blueShips.Count; i++)
        {
            if (blueShips[i].isPlayer)
            {
                playerIndex = i;
            }
        }
        
        ShipBase playerShip = blueShips[playerIndex];
        PlayerController pc = playerShip.GetPlayerController();
        pc.SwapShip += EnableShipSwapping;
    }

    private void Update()
    {
        // When player dies, scenemanager handles input for game to swap player to another ship
        if (inSwapMode)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                SwapPlayerShip(KeyCode.A);
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                SwapPlayerShip(KeyCode.D);
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                SwapPlayerShip(KeyCode.Space);
            }
        }
    }

    // Swap player to different ship within their own team
    private void SwapPlayerShip(KeyCode k)
    {
        switch (k)
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
                inSwapMode = false;
                break;

            default:
                playerIndex = Random.Range(0, blueShips.Count);
                break;
        }

        // Make sure that camera and vfx controller follow the ship player wants to spectate
        ShipBase destShip = blueShips[playerIndex];
        mainCam.player = destShip;
        vfxManager.GetComponent<FollowPlayer>().player = destShip;

        // Once player presses space, disable AI controller and create a player controller on the ship they are spectating
        if (!inSwapMode)
        {
            destShip.GetComponent<AIControllerBase>().enabled = false;
            PlayerController pc = destShip.AddComponent<PlayerController>();
            pc.TransferShipValues(destShip);
            pc.SwapShip += EnableShipSwapping;
        }
    }

    // Event called when player dies, changes to "swap mode" where scene manager handles input and spectator camera/vfx until a new player controller is created
    private void EnableShipSwapping(object sender, EventArgs e)
    {
        if (blueShips.Count > 0)
        {
            inSwapMode = true;
        }

        PlayerController pc = (PlayerController)sender;
        pc.SwapShip -= EnableShipSwapping;
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
}
