using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Player Scripts")]
    [SerializeField] private PlayerLockOnSystem playerLockOnSystem;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public PlayerLockOnSystem GetPlayerLockOnSystem()
    {
        return playerLockOnSystem;
    }

    public GameObject GetLockedEnemy()
    {
        return playerLockOnSystem.GetLockedEnemy();
    }
}
