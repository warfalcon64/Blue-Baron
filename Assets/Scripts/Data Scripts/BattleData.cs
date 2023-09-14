using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class BattleData : ScriptableObject
{
    [Header("Blue Team Stats")]
    public int blueShipsLost;
    public int totalBlueDamage;
    public int bluePlasmaDamage;

    [Header("Red Team Stats")]
    public int redShipsLost;
    public int totalRedDamage;
    public int redPlasmaDamage;
}
