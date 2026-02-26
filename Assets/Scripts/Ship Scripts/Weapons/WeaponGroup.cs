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
    public List<Hardpoint> hardpoints;

    [NonSerialized] private int nextHardpointIndex;

    public List<WeaponsBase> Fire(Vector2 aimPos, Vector2 shipVelocity, ShipBase source)
    {
        List<WeaponsBase> fired = new List<WeaponsBase>();

        if (hardpoints == null || hardpoints.Count == 0) return fired;

        if (fireMode == FireMode.Unison)
        {
            foreach (Hardpoint hp in hardpoints)
            {
                if (hp.IsReady() && hp.IsInFireArc(aimPos, source.transform))
                {
                    fired.Add(hp.Fire(aimPos, shipVelocity, source));
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
                    fired.Add(hp.Fire(aimPos, shipVelocity, source));
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

        return fired;
    }

    public bool HasUsage(WeaponUsage usage)
    {
        if (hardpoints == null) return false;

        foreach (Hardpoint hp in hardpoints)
        {
            if ((hp.GetUsage() & usage) != 0) return true;
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
