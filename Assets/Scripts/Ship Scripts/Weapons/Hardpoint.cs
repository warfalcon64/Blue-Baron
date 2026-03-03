using UnityEngine;

public class Hardpoint : MonoBehaviour
{
    [SerializeField] private WeaponsBase weaponPrefab;
    [SerializeField] private float cooldownOverride;
    [SerializeField] private float rangeOverride;
    [SerializeField] private float fireArc = 360f;

    private float nextFireTime;

    public WeaponsBase Fire(Vector2 aimPos, Vector2 shipVelocity, ShipBase source)
    {
        Vector2 spawnPos = transform.position;
        Vector2 toAim = aimPos - (Vector2)source.transform.position;
        float sqrMag = toAim.sqrMagnitude;
        Vector2 shootDirection;
        if (float.IsNaN(sqrMag) || float.IsInfinity(sqrMag) || sqrMag < 0.001f)
            shootDirection = source.transform.up;
        else
            shootDirection = toAim.normalized;
        float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        WeaponsBase projectile = Instantiate(weaponPrefab, spawnPos, rotation);
        projectile.Setup(shootDirection, shipVelocity, source);
        nextFireTime = Time.time + GetCooldown();
        return projectile;
    }

    public float GetCooldown()
    {
        return cooldownOverride > 0 ? cooldownOverride : weaponPrefab.GetCoolDown();
    }

    public float GetRange()
    {
        return rangeOverride > 0 ? rangeOverride : weaponPrefab.GetRange();
    }

    public float GetFireArc()
    {
        return fireArc;
    }

    public WeaponUsage GetUsage()
    {
        return weaponPrefab.GetUsage();
    }

    public WeaponsBase GetWeaponPrefab()
    {
        return weaponPrefab;
    }

    public bool IsReady()
    {
        return Time.time >= nextFireTime;
    }

    public bool IsInFireArc(Vector2 aimPos, Transform shipTransform)
    {
        if (fireArc >= 360f) return true;

        Vector2 toTarget = (aimPos - (Vector2)shipTransform.position).normalized;
        float angle = Vector2.Angle(shipTransform.up, toTarget);
        return angle <= fireArc;
    }

    public void ResetCooldown()
    {
        nextFireTime = Time.time;
    }

    public void SetNextFireTime(float time)
    {
        nextFireTime = time;
    }
}
