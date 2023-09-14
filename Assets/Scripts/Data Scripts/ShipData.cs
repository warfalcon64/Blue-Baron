using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ShipData : ScriptableObject
{
    [Header("Blue Team")]
    public string blueTag = "Blue";
    public GameObject bluePlasma;

    [Header("Red Team")]
    public string redTag = "Red";
    public GameObject redPlasma;
    

}
