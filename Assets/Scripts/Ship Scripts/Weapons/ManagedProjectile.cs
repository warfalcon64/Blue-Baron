using UnityEngine;

public abstract class ManagedProjectile : WeaponsBase, IManagedProjectile
{
    public int ManagerIndex { get; set; } = -1;

    // Advance one frame. Return false when finished (hit or expired).
    public abstract bool Step(float deltaTime);

    public virtual void Despawn()
    {
        Destroy(gameObject);
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
