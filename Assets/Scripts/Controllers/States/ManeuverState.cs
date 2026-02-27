using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ManeuverState : State.IState
{
    [SerializeField]
    private float duration;
    private float maneuverTime;

    // Jink fields for missile evasion
    private float jinkOffset;
    private float jinkTimer;

    // Range at which incoming missiles trigger evasion
    private float missileEvadeRange;

    public ManeuverState(float duration = 5f, float missileEvadeRange = 15f)
    {
        this.duration = duration;
        this.missileEvadeRange = missileEvadeRange;
    }

    public void OnEnter(AIControllerBase c)
    {
        maneuverTime = Random.Range(1, duration);
        RollJink();
    }

    public void UpdateState(AIControllerBase c)
    {
        // Return to attack as soon as no missiles are close
        if (!HasCloseIncomingMissile(c))
        {
            c.ChangeState(c.attackState);
            return;
        }

        maneuverTime -= Time.deltaTime;
    }

    public void OnHurt(AIControllerBase c)
    {
    }

    public void FixedUpdateState(AIControllerBase c)
    {
        // Try to acquire a target if we don't have one
        if (c.target == null || !c.target.activeInHierarchy)
        {
            GameObject found = c.FindTarget();
            if (found != null)
            {
                c.SetTarget(found);
            }
        }

        // Evade close missiles while still firing at target
        if (HasCloseIncomingMissile(c))
        {
            EvadeMissiles(c);
        }

        // Fire weapons at target regardless of evasion
        if (c.target != null && c.target.activeInHierarchy)
        {
            Vector2 targetAcceleration = c.CalculateTargetAcceleration();
            float angle = c.GetAngleToTarget();
            c.AttackTarget(targetAcceleration, Math.Abs(angle));
        }
    }

    public bool HasCloseIncomingMissile(AIControllerBase c)
    {
        float rangeSqr = missileEvadeRange * missileEvadeRange;
        Vector2 myPos = c.rb.position;

        for (int i = c.incomingMissiles.Count - 1; i >= 0; i--)
        {
            WeaponsAAMissile missile = c.incomingMissiles[i];
            if (missile == null || !missile.gameObject.activeInHierarchy)
            {
                c.incomingMissiles.RemoveAt(i);
                continue;
            }

            float distSqr = ((Vector2)missile.transform.position - myPos).sqrMagnitude;
            if (distSqr <= rangeSqr)
                return true;
        }

        return false;
    }

    private void EvadeMissiles(AIControllerBase c)
    {
        float dt = Time.fixedDeltaTime;

        // Update jink timer
        jinkTimer -= dt;
        if (jinkTimer <= 0f)
        {
            RollJink();
        }

        // Compute weighted threat direction (closer missiles = heavier weight)
        Vector2 myPos = c.rb.position;
        Vector2 evadeDir = Vector2.zero;

        for (int i = c.incomingMissiles.Count - 1; i >= 0; i--)
        {
            WeaponsAAMissile missile = c.incomingMissiles[i];
            if (missile == null || !missile.gameObject.activeInHierarchy)
            {
                c.incomingMissiles.RemoveAt(i);
                continue;
            }

            Vector2 toShip = myPos - (Vector2)missile.transform.position;
            float dist = toShip.magnitude;
            if (dist < 0.01f) continue;

            float weight = 1f / dist;
            evadeDir += toShip.normalized * weight;
        }

        if (evadeDir.sqrMagnitude < 0.0001f)
            return;

        evadeDir.Normalize();

        // Apply jink offset to evade direction
        float evadeAngle = Mathf.Atan2(evadeDir.y, evadeDir.x) * Mathf.Rad2Deg;
        float jinkedAngle = evadeAngle + jinkOffset;
        float jinkedRad = jinkedAngle * Mathf.Deg2Rad;
        Vector2 jinkedDir = new Vector2(Mathf.Cos(jinkedRad), Mathf.Sin(jinkedRad));

        // Turn toward jinked direction and accelerate to max
        float angle = c.GetAngleToDestination(jinkedDir);
        c.MoveToEvade(angle);
    }

    private void RollJink()
    {
        jinkOffset = Random.Range(-1f, 1f) * 60f;
        jinkTimer = Random.Range(0.3f, 0.8f);
    }

    public void OnExit(AIControllerBase c)
    {
    }
}
