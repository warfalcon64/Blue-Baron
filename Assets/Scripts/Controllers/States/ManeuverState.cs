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

    // Flare deployment thresholds
    private float flareRearAngle = 60f;    // Max angle between rear and threat to deploy flares
    private float flareEmergencyRange = 5f; // Deploy flares regardless of angle if missile is this close

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
        if (c.target == null || !c.target.activeInHierarchy)
        {
            GameObject found = c.FindTarget();
            if (found != null)
            {
                c.SetTarget(found);
            }
        }

        if (HasCloseIncomingMissile(c))
        {
            EvadeMissiles(c);
        }

        // Fire weapons at target regardless of evasion — urgent fire to distract attacker
        if (c.target != null && c.target.activeInHierarchy)
        {
            c.AttackTarget(urgentFire: true);
        }
    }

    public bool HasCloseIncomingMissile(AIControllerBase c)
    {
        return c.HasCloseIncomingMissile(missileEvadeRange);
    }

    private void EvadeMissiles(AIControllerBase c)
    {
        float dt = Time.fixedDeltaTime;

        jinkTimer -= dt;
        if (jinkTimer <= 0f)
        {
            RollJink();
        }

        // Compute weighted threat direction (closer missiles = heavier weight)
        Vector2 myPos = c.rb.position;
        Vector2 evadeDir = Vector2.zero;
        float closestDistSqr = float.MaxValue;

        for (int i = c.incomingMissiles.Count - 1; i >= 0; i--)
        {
            WeaponsAAMissile missile = c.incomingMissiles[i];
            if (missile == null || !missile.gameObject.activeInHierarchy)
            {
                c.incomingMissiles.RemoveAt(i);
                continue;
            }

            Vector2 toShip = myPos - (Vector2)missile.transform.position;
            float distSqr = toShip.sqrMagnitude;
            float dist = Mathf.Sqrt(distSqr);
            if (dist < 0.01f) continue;

            if (distSqr < closestDistSqr)
                closestDistSqr = distSqr;

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

        // Deploy flares when rear faces the threat, or immediately if missile is dangerously close
        Vector2 rear = -(Vector2)c.transform.up;
        Vector2 threatDir = -evadeDir; // Direction toward missiles
        float rearAngle = Vector2.Angle(rear, threatDir);
        bool rearFacingThreat = rearAngle <= flareRearAngle;
        bool missileEmergency = closestDistSqr <= flareEmergencyRange * flareEmergencyRange;

        if (rearFacingThreat || missileEmergency)
        {
            c.ship.ActivateCombatSystem(); // * may have to later check for specific combat system instead of blind activation
        }
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
