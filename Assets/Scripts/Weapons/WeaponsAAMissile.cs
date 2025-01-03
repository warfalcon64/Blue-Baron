using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponsAAMissile : WeaponsBase
{
    Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    

}
