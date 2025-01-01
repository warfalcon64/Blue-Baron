using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Random = UnityEngine.Random;

public class AttackState : State.IState
{
    [SerializeField]
    private float duration;

    private float attackTime;

    public AttackState(float duration = 15f)
    {
        this.duration = duration;
    }

    public void OnEnter(AIControllerBase c)
    {
        attackTime = Random.Range(4, duration);
    }

    public void UpdateState(AIControllerBase c)
    {
        if (attackTime <= 0)
        {
            c.ChangeState(c.maneuverState);
        }
        else
        {
            attackTime -= Time.deltaTime;
        }
    }

    public void OnHurt(AIControllerBase c)
    {
        if (c.target != null && c.attacker != null)
        {
            Vector2 aPos = c.attacker.GetComponent<Rigidbody2D>().position;
            Vector2 tPos = c.target.GetComponent<Rigidbody2D>().position;
            float aDistance = (aPos - c.rb.position).sqrMagnitude;
            float tDistance = (tPos - c.rb.position).sqrMagnitude;

            //if (c.attacker.GetComponent<ShipBase>())
            //{
                c.SetTarget(c.attacker);
            //}

            //if (aDistance < tDistance)
            //{
            //    c.SetTarget(c.attacker);
            //}
        }
    }

    public void FixedUpdateState(AIControllerBase c)
    {
        if (c.target != null)
        {
            float angle = c.GetAngleToTarget();
            Rigidbody2D targetRb = c.target.GetComponent<Rigidbody2D>();

            Vector2 tDirection = targetRb.position - c.rb.position;
            float distance = Mathf.Sqrt(tDirection.sqrMagnitude);
            Vector2 inaccuracy = (new Vector2(Random.Range(-c.plasmaInaccuracy, c.plasmaInaccuracy),
            Random.Range(-c.plasmaInaccuracy, c.plasmaInaccuracy)) * (1 / distance));
            Vector2 targetAcceleration = c.CalculateTargetAcceleration() + inaccuracy;
            c.MoveToEngage(angle);
            c.AttackTarget(targetAcceleration, Math.Abs(angle));
        }
        else
        {
            c.ChangeState(c.searchState);
        }
    }

    public void OnExit(AIControllerBase c)
    {

    }
}
