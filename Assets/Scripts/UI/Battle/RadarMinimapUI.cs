using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RadarMinimapUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] float alphaThreshold = 0.1f;
    [SerializeField] private Radar radar;
    [SerializeField] private LayerMask pingSelectionLayer;

    private bool isRadarSelectionEnabled;
    private float radarRange;
    private Image radarImage;
    private RectTransform radarRect;
    private Camera minimapCam;

    public EventHandler<ShipBase> OnRadarPingSelect;

    // Start is called before the first frame update
    void Start()
    {
        isRadarSelectionEnabled = false;
        radarImage = GetComponentInChildren<Image>();
        radarRect = radarImage.GetComponent<RectTransform>();
        radarImage.alphaHitTestMinimumThreshold = alphaThreshold;
        minimapCam = SceneManager.Instance.minimapCam;
        radarRange = radar.GetRadarRange();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        Vector2 localClick;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(radarRect, pointerEventData.position, Camera.main, out localClick);
        localClick.y = (radarImage.sprite.textureRect.yMin * -1) - (localClick.y * -1);
        Vector2 normalizedClick = new Vector2(localClick.x / (radarRect.rect.width / 2), localClick.y / (radarRect.rect.height / 2));
        Vector2 radarCamPos = minimapCam.transform.position;
        Vector2 worldClick = radarCamPos + new Vector2(normalizedClick.x * radarRange, normalizedClick.y * radarRange);

        if (isRadarSelectionEnabled)
        {
            Collider2D[] radarPings = Physics2D.OverlapPointAll(worldClick, pingSelectionLayer);

            if (radarPings.Length > 0)
            {
                RadarPing ping = radarPings[0].gameObject.GetComponent<RadarPing>();
                ShipBase selectedShip = ping.GetShip();
                OnRadarPingSelect?.Invoke(this, selectedShip);
            }
        }
    }

    // * Can merge these two funcions into one that subscribes based on boolean parameter
    public void SubscribeToPlayerController(PlayerController pc)
    {
        pc.OnRadarSelectionEnabled += HandleRadarSelectionEnabled;
        pc.OnRadarSelectionDisabled += HandleRadarSelectionDisabled;
    }

    public void UnsubscribeToPlayerController(PlayerController pc)
    {
        pc.OnRadarSelectionEnabled -= HandleRadarSelectionEnabled;
        pc.OnRadarSelectionDisabled -= HandleRadarSelectionDisabled;
    }

    private void HandleRadarSelectionEnabled(object sender, EventArgs e)
    {
        if (!isRadarSelectionEnabled) { isRadarSelectionEnabled = true; }
    }

    private void HandleRadarSelectionDisabled(object sender, EventArgs e)
    {
        if (isRadarSelectionEnabled) { isRadarSelectionEnabled= false; }
    }

}
