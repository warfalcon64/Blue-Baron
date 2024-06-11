using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;


public class WeaponMap : ScriptableObject
{
    public Dictionary<ShootType, WeaponsBase> weapons;

    public WeaponMap ()
    {
        weapons = new Dictionary<ShootType, WeaponsBase>();
    }

    public void Init(List<WeaponMapEntry> entries)
    {
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

