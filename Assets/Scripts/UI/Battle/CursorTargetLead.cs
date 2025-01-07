using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CursorTargetLead : MonoBehaviour
{
    public RectTransform leadIndicator;
    public float size;

    private SceneManager sceneManager;
    private PlayerLockOnSystem lockOnSystem;
    private GameObject lockedEnemy;
    private Vector2 lead;

    // Start is called before the first frame update
    void Start()
    {
        sceneManager = SceneManager.Instance;
        lockOnSystem = sceneManager.playerManager.GetComponent<PlayerLockOnSystem>();
        lead = Vector2.zero;
        leadIndicator.sizeDelta = new Vector2(size, size);
    }

    // Update is called once per frame
    void Update()
    {
        lead = lockOnSystem.GetLead();
        lockedEnemy = lockOnSystem.GetLockedEnemy();

        if (lead != null)
        {
            if (lockedEnemy != null && lockedEnemy.activeSelf)
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
