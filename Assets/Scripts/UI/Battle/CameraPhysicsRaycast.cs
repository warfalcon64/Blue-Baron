using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPhysicsRaycast : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D raycastHit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            //if (Physics2D.Raycast(ray, out raycastHit))
            //{

            //}
        }
    }
}
