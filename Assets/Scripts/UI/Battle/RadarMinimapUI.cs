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

    private bool isRadarSelectionEnabled;
    private Image radarImage;

    // Start is called before the first frame update
    void Start()
    {
        isRadarSelectionEnabled = false;
        radarImage = GetComponentInChildren<Image>();
        radarImage.alphaHitTestMinimumThreshold = alphaThreshold;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        print("Works");
        
    }

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
