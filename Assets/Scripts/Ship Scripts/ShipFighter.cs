
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

    private void Start()
    {
        //StartCoroutine(Turn());
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

    protected override void ShootProjectiles(Vector2 targetAcceleration)
    {
        base.ShootProjectiles(targetAcceleration);
    }

    protected override void ShootPlasma(Vector2 targetAcceleration)
    {
        base.ShootPlasma(targetAcceleration);
    }

    // Moves the ship to face the specified target gameobject
    protected override void Move()
    {
        base.Move();
    }

    // This is garbage for now
    IEnumerator Turn()
    {
        while (true)
        {
            if (!target) yield return new WaitForFixedUpdate();

            float turn = 0f;
            float angle = GetAngleToTarget();

            // Turning logic
            if (Mathf.Abs(angle) > faceEnemyAngle)
            {
                if (angle > 0)
                {
                    turn = 1f;
                }
                if (angle < 0)
                {
                    turn = -1f;
                }
            }
            
            if (Mathf.Abs(angle) < faceEnemyAngle)
            {
                yield return new WaitForSeconds(Random.Range(0, 1.5f));
            }

            rb.MoveRotation(rb.rotation + (turnSpeed * turn));
        }
    }

    protected override GameObject FindTarget()
    {
        return base.FindTarget();
    }

    protected override float GetAngleToTarget()
    {
        return base.GetAngleToTarget();
    }

    protected override Vector2 GetTargetAcceleration()
    {
        return base.GetTargetAcceleration();
    }

    // Imma be real idk how any of the math stuff below works, I found it on the internet
    protected override Vector2 GetTargetLeadingPosition(Vector2 targetAcceleration, int iterations, float weaponSpeed)
    {
        return base.GetTargetLeadingPosition(targetAcceleration, iterations, weaponSpeed);
    }

    protected override float SolveQuarticNewton(float guess, int iterations, float a, float b, float c, float d, float e)
    {
        return base.SolveQuarticNewton(guess, iterations, a, b, c, d, e);
    }

    protected override float EvalQuartic(float t, float a, float b, float c, float d, float e)
    {
        return base.EvalQuartic(t, a, b, c, d, e);
    }

    protected override float EvalQuarticDerivative(float t, float a, float b, float c, float d)
    {
        return base.EvalQuarticDerivative(t, a, b, c, d);
    }
}
