using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CursorTargetLead : MonoBehaviour
{
    public RectTransform leadIndicator;
    public float size;

    private bool leadEnabled;
    private SceneManager sceneManager;
    private PlayerManager playerManager;
    private PlayerLockOnSystem lockOnSystem;
    private GameObject lockedEnemy;
    private Vector2 lead;

    // Start is called before the first frame update
    void Start()
    {
        sceneManager = SceneManager.Instance;
        playerManager = sceneManager.playerManager;
        lockOnSystem = sceneManager.playerManager.GetPlayerLockOnSystem();
        lead = Vector2.zero;
        leadIndicator.sizeDelta = new Vector2(size, size);
        leadEnabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        lead = lockOnSystem.GetLead();
        leadEnabled = lockOnSystem.IsLockingEnabled();
        lockedEnemy = playerManager.GetLockedEnemy();

        if (lead != null)
        {
            if (lockedEnemy != null && lockedEnemy.activeSelf && leadEnabled)
            {
                if (!leadIndicator.gameObject.activeSelf)
                {
                    leadIndicator.gameObject.SetActive(true);
                }

                leadIndicator.position = lead;
            }
            else
            {
                leadIndicator.gameObject.SetActive(false);
            }
        }
    }
}
