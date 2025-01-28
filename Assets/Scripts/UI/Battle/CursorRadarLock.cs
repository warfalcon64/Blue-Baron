using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorRadarLock : MonoBehaviour
{
    public RectTransform radarRangeIndicator;

    private float lockRadius;
    private SceneManager sceneManager;

    private void Start()
    {
        sceneManager = SceneManager.Instance;
        lockRadius = sceneManager.playerManager.GetComponent<PlayerLockOnSystem>().GetLockRadius();
        float pixelsPerUnit = Screen.height / (Camera.main.orthographicSize * 2);
        float circleDiameterInPixels = lockRadius * 2 * pixelsPerUnit; // ** REPLACE CONSTANT WITH VARIABLE THAT ADJUSTS TO SCREEN RESOLUTION IN FUTURE
        radarRangeIndicator.sizeDelta = new Vector2(circleDiameterInPixels, circleDiameterInPixels);
    }

    // Update is called once per frame
    void Update()
    {
        radarRangeIndicator.position = Input.mousePosition;
    }
}
