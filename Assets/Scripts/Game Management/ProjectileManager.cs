using System.Collections.Generic;
using UnityEngine;

// Central driver for projectiles without rigidbodies and colliders.
// Replaces per-projectile MonoBehaviour ticking with a single Update loop.

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }

    private readonly List<IManagedProjectile> projectiles = new List<IManagedProjectile>(8192);

    public static ProjectileManager GetOrCreate()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("ProjectileManager");
            Instance = go.AddComponent<ProjectileManager>();
        }
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Register(IManagedProjectile p)
    {
        p.ManagerIndex = projectiles.Count;
        projectiles.Add(p);
    }

    public void Unregister(IManagedProjectile p)
    {
        int i = p.ManagerIndex;
        if (i < 0 || i >= projectiles.Count || projectiles[i] != p) return;
        RemoveAt(i);
        p.ManagerIndex = -1;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        // Iterate backwards so a finished projectile's swap-removal (tail moves into slot i) never
        // skips or double-processes a replacement.
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            IManagedProjectile p = projectiles[i];
            if (!p.Step(dt))
            {
                RemoveAt(i);
                p.ManagerIndex = -1;
                p.Despawn();
            }
        }
    }

    // The moved tail element takes slot i.
    private void RemoveAt(int i)
    {
        int last = projectiles.Count - 1;
        IManagedProjectile moved = projectiles[last];
        projectiles[i] = moved;
        moved.ManagerIndex = i;
        projectiles.RemoveAt(last);
    }
}
