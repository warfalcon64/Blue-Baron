using System;
using System.Collections.Generic;
using UnityEngine;

public enum FireMode
{
    Unison,
    Alternating
}

[Serializable]
public class WeaponGroup
{
    public string groupName;
    public FireMode fireMode;
    public bool autonomous;
    public bool enabled = true;
    public List<Hardpoint> hardpoints;

    [NonSerialized] private int nextHardpointIndex;
    [NonSerialized] private readonly List<WeaponsBase> firedCache = new List<WeaponsBase>();

    public List<WeaponsBase> Fire(Vector2 aimPos, Vector2 shipVelocity, ShipBase source)
    {
        firedCache.Clear();

        if (!enabled || hardpoints == null || hardpoints.Count == 0) return firedCache;

        if (fireMode == FireMode.Unison)
        {
            for (int i = 0; i < hardpoints.Count; i++)
            {
                Hardpoint hp = hardpoints[i];
                if (hp.IsReady() && hp.IsInFireArc(aimPos, source.transform))
                {
                    firedCache.Add(hp.Fire(aimPos, shipVelocity, source));
                }
            }
        }
        else // Alternating
        {
            int count = hardpoints.Count;
            for (int i = 0; i < count; i++)
            {
                int index = (nextHardpointIndex + i) % count;
                Hardpoint hp = hardpoints[index];

                if (hp.IsReady() && hp.IsInFireArc(aimPos, source.transform))
                {
                    firedCache.Add(hp.Fire(aimPos, shipVelocity, source));
                    nextHardpointIndex = (index + 1) % count;

                    // Stagger the remaining hardpoints evenly across the cooldown
                    float cooldown = hp.GetCooldown();
                    float interval = cooldown / count;
                    for (int j = 1; j < count; j++)
                    {
                        int staggerIndex = (index + j) % count;
                        Hardpoint other = hardpoints[staggerIndex];
                        if (other.IsReady())
                        {
                            other.SetNextFireTime(Time.time + interval * j);
                        }
                    }

                    break;
                }
            }
        }

        return firedCache;
    }

    public bool HasUsage(WeaponUsage usage)
    {
        if (hardpoints == null) return false;

        for (int i = 0; i < hardpoints.Count; i++)
        {
            if ((hardpoints[i].GetUsage() & usage) != 0) return true;
        }
        return false;
    }

    public List<Hardpoint> GetHardpoints()
    {
        return hardpoints;
    }

    public WeaponsBase GetRepresentativeWeapon()
    {
        if (hardpoints != null && hardpoints.Count > 0)
        {
            return hardpoints[0].GetWeaponPrefab();
        }
        return null;
    }
}
