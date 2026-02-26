using UnityEngine;

public abstract class CombatSystemBase : MonoBehaviour
{
    [SerializeField] protected float cooldown = 5f;
    [SerializeField] protected int maxCharges = -1; // -1 = unlimited

    protected float nextReadyTime;
    protected int currentCharges;
    protected ShipBase ship;

    public virtual void Init(ShipBase ship)
    {
        this.ship = ship;
        currentCharges = maxCharges;
    }

    public abstract void Activate();

    public bool IsReady()
    {
        return Time.time >= nextReadyTime && (maxCharges < 0 || currentCharges > 0);
    }

    public float GetCooldown() => cooldown;
    public int GetCurrentCharges() => currentCharges;
    public int GetMaxCharges() => maxCharges;
}
