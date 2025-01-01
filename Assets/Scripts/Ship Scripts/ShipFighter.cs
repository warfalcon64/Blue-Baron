
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static UnityEngine.GraphicsBuffer;
using Random = UnityEngine.Random;

public class ShipFighter : ShipBase
{
    protected override void Awake()
    {
       base.Awake();
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    protected override void Init()
    {
        base.Init();
    }

    protected override void OnTriggerEnter2D(Collider2D collider)
    {
        base.OnTriggerEnter2D(collider);
    }

    protected override void UpdateSmoke()
    {
        base.UpdateSmoke();
    }

    protected override void PlayHitVFX(string type)
    {
        base.PlayHitVFX(type);
    }

    // Moves the ship to face the specified target gameobject
    protected override void Move()
    {
        base.Move();
    }
}
