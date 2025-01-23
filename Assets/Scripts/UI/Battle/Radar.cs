using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Radar : MonoBehaviour
{
    public RectTransform radarSweep;

    [Header("Radar Settings")]
    public float rotateSpeed = 100f;
    public float radarDistance = 1776f;

    private float currentAngle;
    // Start is called before the first frame update
    void Start()
    {
        // *** FOR LARGER SHIP CLASSES IN FUTURE, MAY HAVE TO SCALE UP RADAR!
    }

    // Update is called once per frame
    void Update()
    {
        radarSweep.eulerAngles -= new Vector3(0, 0, rotateSpeed * Time.deltaTime);

        RaycastHit2D raycastHit = Physics2D.Raycast(transform.position, GetVectorFromAngle(radarSweep.eulerAngles.z), radarDistance);

        if (raycastHit.collider != null && !raycastHit.collider.CompareTag("Blue") && raycastHit.collider.GetComponent<ShipBase>())
        {
            print(raycastHit.collider.gameObject);
        }
    }

    private Vector3 GetVectorFromAngle(float angle)
    {
        float angleRad = angle * (Mathf.PI / 180f);
        return new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }
}
