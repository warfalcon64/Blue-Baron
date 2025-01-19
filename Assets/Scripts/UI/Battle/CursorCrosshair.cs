using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CursorCrosshair : MonoBehaviour
{
    public RectTransform outerCrosshair;
    public RectTransform innerCrosshair;
    public RectTransform dot;

    [Header("Rotation Settings")]
    public float speed = 100f;

    private SceneManager sceneManager;
    private float lockRadius;
    private Vector2 pos;
    private Vector3 outerPos = Vector2.zero;
    private Vector3 innerPos = Vector2.zero;
    private Vector3 dotPos = Vector3.zero;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        sceneManager = SceneManager.Instance;
        lockRadius = sceneManager.playerManager.GetComponent<PlayerLockOnSystem>().GetLockRadius();
        float pixelsPerUnit = Screen.height / (Camera.main.orthographicSize * 2);
        float circleDiameterInPixels = lockRadius * 2 * pixelsPerUnit;
        outerCrosshair.sizeDelta = new Vector2(circleDiameterInPixels, circleDiameterInPixels);
        innerCrosshair.sizeDelta = new Vector2(circleDiameterInPixels, circleDiameterInPixels);
    }

    // Update is called once per frame
    void Update()
    {
        pos = Input.mousePosition;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(outerCrosshair, pos, Camera.main, out outerPos);
        outerCrosshair.position = outerPos;

        RectTransformUtility.ScreenPointToWorldPointInRectangle(innerCrosshair, pos, Camera.main, out innerPos);
        innerCrosshair.position = innerPos;
        innerCrosshair.Rotate(0, 0, speed * Time.deltaTime);

        RectTransformUtility.ScreenPointToWorldPointInRectangle(dot, pos, Camera.main, out dotPos);
        dot.position = dotPos;
    }
}
