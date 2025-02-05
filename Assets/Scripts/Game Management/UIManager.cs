using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Player UI")]
    [SerializeField] private RadarMinimapUI radarMinimapUI;

    [Header("Scene UI Objects")]
    [SerializeField] private RectTransform playerSceneRadar;


    private ShipBase playerShip;
    private PlayerController pc;

    // Start is called before the first frame update
    void Start()
    {
        SceneManager.Instance.PlayerDeath += HandlePlayerDeath;
        SceneManager.Instance.PlayerRebirth += HandlePlayerRebirth;

        radarMinimapUI.SubscribeToPlayerController(pc);
    }

    // Update is called once per frame
    void Update()
    {
        if (playerShip != null)
        {
            playerSceneRadar.position = playerShip.transform.position;
        }
    }

    private void HandlePlayerDeath(object sender, ShipBase ship)
    {
        radarMinimapUI.UnsubscribeToPlayerController(ship.GetPlayerController());
        playerShip = null;
    }

    private void HandlePlayerRebirth(object sender, ShipBase ship)
    {
        playerShip = ship;
        radarMinimapUI.SubscribeToPlayerController(ship.GetPlayerController());
    }

    public void SetPlayerShip(ShipBase playerShip)
    {
        this.playerShip = playerShip;
    }

    public void SetPlayerController(PlayerController pc)
    {
        this.pc = pc;
    }
}
