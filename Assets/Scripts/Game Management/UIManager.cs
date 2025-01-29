using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Player UI")]
    [SerializeField] private RectTransform playerRadar;


    [SerializeField] private ShipBase playerShip;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (playerShip != null)
        {
            playerRadar.position = playerShip.transform.position;
        }
    }

    public void SetPlayerShip(ShipBase playerShip)
    {
        this.playerShip = playerShip;
    }
}
