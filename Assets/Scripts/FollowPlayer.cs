using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    // Get rid of offsets if never used
    public float xOffset;
    public float yOffset;
    public ShipBase player;

    // Update is called once per frame
    private void LateUpdate()
    { // The vector 3 sets the camera above the player so you can actually see stuff
        transform.position = player.transform.position + new Vector3(xOffset, yOffset, -10);
    }
}
