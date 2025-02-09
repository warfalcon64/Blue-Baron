using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Radar : MonoBehaviour
{
    [SerializeField] private RectTransform radarSweep;
    [SerializeField] private RadarPing ping;
    [SerializeField] private LayerMask radarLayerMask;

    [Header("Radar Settings")]
    [SerializeField] private float rotateSpeed = 100f;

    private float radarDistance;
    private Dictionary<ShipBase, RadarPing> shipToPing;
    private PlayerManager playerManager;

    private void Awake()
    {
        shipToPing = new Dictionary<ShipBase, RadarPing>();
    }

    // Start is called before the first frame update
    void Start()
    {
        radarDistance = radarSweep.sizeDelta.x;

        playerManager = SceneManager.Instance.playerManager;
    }

    // Update is called once per frame
    void Update()
    {
        Scan();
    }

    private void Scan()
    {
        radarSweep.eulerAngles -= new Vector3(0, 0, rotateSpeed * Time.deltaTime);

        RaycastHit2D[] raycastHitArray = Physics2D.CircleCastAll(transform.position, 3f, GetVectorFromAngle(radarSweep.eulerAngles.z), radarDistance, radarLayerMask);
        Debug.DrawRay(transform.position, GetVectorFromAngle(radarSweep.eulerAngles.z) * radarDistance, Color.red);
        

        foreach (RaycastHit2D raycastHit in raycastHitArray)
        {
            // * Hardcoded tag comparison, may need to remove in future
            if (raycastHit.collider != null && raycastHit.collider.CompareTag("Red"))
            {
                ShipBase ship = raycastHit.collider.GetComponent<ShipBase>();

                if (shipToPing.ContainsKey(ship) && shipToPing[ship] != null)
                {
                    RadarPing tmpPing = shipToPing[ship];
                    tmpPing.blip.position = raycastHit.point;
                    tmpPing.SetFadeTimer(0);
                }
                else
                {
                    shipToPing[ship] = Instantiate(ping, raycastHit.point, Quaternion.identity, this.transform);
                    RadarPing tmpPing = shipToPing[ship];
                    tmpPing.SetFadeTime(360f / rotateSpeed);
                    tmpPing.SetShip(ship);

                    if (playerManager.GetLockedEnemy() == ship.gameObject)
                    {
                        tmpPing.SetColor(new Color(1f, 0, 0));
                    }
                    else
                    {
                        tmpPing.SetColor(new Color(0, 1f, 0));
                    }
                }
            }
        }
    }

    private Vector3 GetVectorFromAngle(float angle)
    {
        float angleRad = (angle + 90) * (Mathf.PI / 180f);
        return new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }

    public float GetRadarRange()
    {
        return radarDistance;
    }
}
