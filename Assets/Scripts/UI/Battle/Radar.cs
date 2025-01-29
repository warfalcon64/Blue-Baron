using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Radar : MonoBehaviour
{
    public RectTransform radarSweep;
    public RadarPing ping;

    [Header("Radar Settings")]
    public float rotateSpeed = 100f;

    private RadarPing tmpPing;
    private float radarDistance;
    private Dictionary<ShipBase, RadarPing> shipPingPairs;


    private void Awake()
    {
        shipPingPairs = new Dictionary<ShipBase, RadarPing>();
        //radarDistance = radarSweep.sizeDelta.x;
    }

    // Start is called before the first frame update
    void Start()
    {
        radarDistance = radarSweep.sizeDelta.x;
        // *** FOR LARGER SHIP CLASSES IN FUTURE, MAY HAVE TO SCALE UP RADAR!
    }

    // Update is called once per frame
    void Update()
    {
        radarSweep.eulerAngles -= new Vector3(0, 0, rotateSpeed * Time.deltaTime);

        RaycastHit2D raycastHit = Physics2D.Raycast(transform.position, GetVectorFromAngle(radarSweep.eulerAngles.z), radarDistance);
        Debug.DrawRay(transform.position, GetVectorFromAngle(radarSweep.eulerAngles.z) * radarDistance, Color.red);
        
        // * Hardcoded tag comparison, may need to remove in future
        if (raycastHit.collider != null && !raycastHit.collider.CompareTag("Blue") && raycastHit.collider.GetComponent<ShipBase>())
        {
            ShipBase ship = raycastHit.collider.GetComponent<ShipBase>();

            if (shipPingPairs.ContainsKey(ship) && shipPingPairs[ship] != null)
            {
                tmpPing = shipPingPairs[ship];
                tmpPing.blip.position = raycastHit.point;
                tmpPing.SetFadeTimer(0);
            }
            else
            {
                shipPingPairs[ship] = Instantiate(ping, raycastHit.point, Quaternion.identity);
            }
            
        }
    }

    private Vector3 GetVectorFromAngle(float angle)
    {
        float angleRad = (angle + 90) * (Mathf.PI / 180f);
        return new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }
}
