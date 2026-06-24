// Contract for projectiles driven by ProjectileManager's single per-frame loop instead of their
// own Update/FixedUpdate. The manager knows nothing about weapons or GameObjects.
public interface IManagedProjectile
{
    // Slot index owned by the manager.
    int ManagerIndex { get; set; }

    // Advance one frame by deltaTime. Return false when finished (hit or expired).
    bool Step(float deltaTime);

    void Despawn();
}
