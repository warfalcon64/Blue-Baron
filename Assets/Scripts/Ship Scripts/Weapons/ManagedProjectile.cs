using UnityEngine;

public abstract class ManagedProjectile : WeaponsBase, IManagedProjectile
{
    public int ManagerIndex { get; set; } = -1;

    // Which pool bucket this projectile is spawned from and returned to. No default, every
    // managed projectile must declare it so a new type can't silently file into the wrong pool.
    public abstract ObjectPoolManager.PoolType PoolType { get; }

    // Advance one frame. Return false when finished (hit or expired).
    public abstract bool Step(float deltaTime);

    public virtual void Despawn()
    {
        ObjectPoolManager.ReturnObjectToPool(gameObject, PoolType);
    }

    protected void EnterManager()
    {
        ProjectileManager.GetOrCreate().Register(this);
    }

    protected virtual void OnDestroy()
    {
        if (ManagerIndex >= 0 && ProjectileManager.Instance != null)
            ProjectileManager.Instance.Unregister(this);
    }
}
