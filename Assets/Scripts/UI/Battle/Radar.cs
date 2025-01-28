using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Radar : MonoBehaviour
{
    public RectTransform radarSweep;

    [Header("Radar Settings")]
    public float rotateSpeed = 100f;

    private float radarDistance;
    private List<Collider2D> colliders;

    private void Awake()
    {
        colliders = new List<Collider2D>();
        //radarDistance = radarSweep.sizeDelta.x;
    }

    // Start is called before the first frame update
    void Start()
    {
        radarDistance = radarSweep.sizeDelta.x;
        // *** FOR LARGER SHIP CLASSES IN FUTURE, MAY HAVE TO SCALE UP RADAR!
        print(radarDistance);
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
            if (!colliders.Contains(raycastHit.collider))
            {
                colliders.Add(raycastHit.collider);
                print(raycastHit.collider.gameObject);
            }
        }
    }

    private Vector3 GetVectorFromAngle(float angle)
    {
        float angleRad = (angle + 90) * (Mathf.PI / 180f);
        return new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }
}
