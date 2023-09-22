using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;


public class WeaponMap : ScriptableObject
{
    public Dictionary<ShootType, WeaponsBase> weapons;

    public WeaponMap (List<WeaponMapEntry> entries)
    {
        this.weapons = new Dictionary<ShootType, WeaponsBase>();

        foreach (WeaponMapEntry entry in entries)
        {
            weapons.Add(entry.type, entry.weapon);
        }
    }

    [Serializable]
    public class WeaponMapEntry
    {
        public ShootType type;
        public WeaponsBase weapon;
    }
}


