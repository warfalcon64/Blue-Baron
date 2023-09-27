using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Unity.VisualScripting;
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

    public WeaponsBase GetWeapon(ShootType type)
    {
        return weapons[type];
    }

    [Serializable]
    public class WeaponMapEntry
    {
        public ShootType type;
        public WeaponsBase weapon;
    }

    
}

