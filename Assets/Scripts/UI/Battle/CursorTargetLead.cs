using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorTargetLead : MonoBehaviour
{
    public RectTransform leadIndicator;
    public float size;

    private SceneManager sceneManager;
    private Vector2 lead;
    // Start is called before the first frame update
    void Start()
    {
        sceneManager = SceneManager.Instance;
        lead = Vector2.zero;
        leadIndicator.sizeDelta = new Vector2(size, size);
    }

    // Update is called once per frame
    void Update()
    {
        lead = sceneManager.playerManager.GetComponent<PlayerLockOnSystem>().GetLead();

        if (lead != Vector2.zero && lead != null)
        {
            leadIndicator.position = lead;
        }
    }
}
