using System;
using System.Collections.Generic;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Dogfight Target", story: "[Self] dogfights [Target]", category: "Action/Attack", id: "90f43ceeb8beca586cf559d050f8a66f")]
public partial class DogfightTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    private float leadFactor;
    private float leadFactorTimer;
    private float leadFactorMin = 0.7f;
    private float leadFactorMax = 1.3f;
    private float leadFactorRerollInterval = 3f;

    private float faceEnemyAngle = 1f;
    private float nextAdjust;

    // Disengage detection
    private float lastSampledDistance;
    private float disengageBuildup;
    private float disengageSampleTimer;
    private float disengageThreshold = 4f;
    private float disengageTimeLimit;
    private float disengageTimeLimitMin = 0.5f;
    private float disengageTimeLimitMax = 2f;
    private float disengageSampleInterval = 0.5f;

    // Break turn
    private bool isDisengaging;
    private float disengageTimer;
    private float disengageDuration = 0.75f;

    private List<WeaponGroup> groups;

    private ShipBase ship;
    private Rigidbody2D rb;
    private Rigidbody2D targetRb;

    protected override Status OnStart()
    {
        ship = Self.Value.GetComponent<ShipBase>();
        rb = Self.Value.GetComponent<Rigidbody2D>();
        targetRb = Target.Value.GetComponent<Rigidbody2D>();

        groups = ship.GetWeaponGroups();
        
        leadFactor = UnityEngine.Random.Range(leadFactorMin, leadFactorMax);
        leadFactorTimer = leadFactorRerollInterval;

        lastSampledDistance = 0f;
        disengageBuildup = 0f;
        disengageSampleTimer = 0f;
        disengageTimeLimit = UnityEngine.Random.Range(disengageTimeLimitMin, disengageTimeLimitMax);
        isDisengaging = false;
        disengageTimer = 0f;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target.Value == null || !Target.Value.activeInHierarchy)
            return Status.Failure;

        float dt = Time.deltaTime;
        if (nextAdjust > 0) nextAdjust -= dt;

        float angle = GetAngleToLeadTarget();
        float distance = (targetRb.position - rb.position).magnitude;

        if (isDisengaging)
        {
            BreakTurn(angle, distance, dt);
        }
        else
        {
            UpdateDisengageTimer(distance, dt);
            FollowTarget(angle);
            AttackTarget();
        }

        return Status.Running;
    }

    private void BreakTurn(float angle, float distance, float dt)
    {
        float reverseTurn = angle > 0 ? -1f : 1f;
        ship.SetShipTurn(reverseTurn);
        ship.Accelerate(0.2f);

        AttackTarget();

        disengageTimer -= dt;
        if (disengageTimer <= 0f)
        {
            isDisengaging = false;
            disengageBuildup = 0f;
            disengageSampleTimer = 0f;
            disengageTimeLimit = UnityEngine.Random.Range(disengageTimeLimitMin, disengageTimeLimitMax);
            lastSampledDistance = distance;
        }
    }

    private void UpdateDisengageTimer(float distance, float dt)
    {
        disengageSampleTimer -= dt;
        if (disengageSampleTimer <= 0f)
        {
            if (lastSampledDistance > 0f)
            {
                if (Mathf.Abs(distance - lastSampledDistance) < disengageThreshold)
                    disengageBuildup += disengageSampleInterval;
                else
                    disengageBuildup = 0f;
            }

            lastSampledDistance = distance;
            disengageSampleTimer = disengageSampleInterval;

            if (disengageBuildup >= disengageTimeLimit)
            {
                isDisengaging = true;
                disengageTimer = disengageDuration;
            }
        }
    }

    protected override void OnEnd()
    {
    }

    private void AttackTarget()
    {
        for (int i = 0; i < groups.Count; i++)
        {
            WeaponGroup group = groups[i];
            if (group.autonomous) continue;

            WeaponsBase repWeapon = group.GetRepresentativeWeapon();
            if (repWeapon == null) continue;

            if ((repWeapon.GetUsage() & WeaponUsage.Missile) != 0)
            {
                float angleToTarget = Mathf.Abs(ship.GetAngleToTarget(Target.Value));
                if (angleToTarget > 45f)
                    continue;
            }

            List<Hardpoint> hps = group.GetHardpoints();
            Vector2 avgPos = Vector2.zero;
            foreach (Hardpoint hp in hps)
            {
                avgPos += (Vector2)hp.transform.position;
            }
            avgPos /= hps.Count;

            Vector2 aimPos = GetLeadPosition(repWeapon, avgPos);
            ship.FireGroup(i, aimPos);
        }
    }

    private void FollowTarget(float angle)
    {
        float turn = 0f;

        // Turning logic
        if (Mathf.Abs(angle) > faceEnemyAngle)
        {
            if (angle > 0) turn = 1f;
            if (angle < 0) turn = -1f;
        }

        // Acceleration logic
        if (nextAdjust <= 0)
        {
            if (Mathf.Abs(angle) < 70)
            {
                ship.Accelerate(0.2f);
            }
            else if (Mathf.Abs(angle) >= 90)
            {
                ship.Decelerate(0.2f);
            }
            else
            {
                nextAdjust = UnityEngine.Random.Range(0f, 0.2f);
            }
        }

        ship.SetShipTurn(turn);
    }

    private Vector2 GetLeadPosition(WeaponsBase weapon, Vector2 gunPosition)
    {
        float speed = weapon.GetSpeed();
        float distance = Vector2.Distance(gunPosition, targetRb.position);
        float travelTime = distance / speed;
        return targetRb.position + targetRb.linearVelocity * travelTime;
    }

    private float GetAngleToLeadTarget()
    {
        leadFactorTimer -= Time.deltaTime;
        if (leadFactorTimer <= 0f)
        {
            leadFactor = UnityEngine.Random.Range(leadFactorMin, leadFactorMax);
            leadFactorTimer = leadFactorRerollInterval;
        }

        WeaponGroup primaryGroup = ship.GetWeaponGroup(0);
        WeaponsBase repWeapon = primaryGroup.GetRepresentativeWeapon();
        if (repWeapon == null)
            return ship.GetAngleToTarget(Target.Value);

        float projectileSpeed = repWeapon.GetSpeed();
        float distance = Vector2.Distance(rb.position, targetRb.position);
        float travelTime = distance / projectileSpeed * leadFactor;
        Vector2 leadPos = targetRb.position + targetRb.linearVelocity * travelTime;
        Vector2 leadDirection = leadPos - rb.position;

        return Vector2.SignedAngle((Vector2)Self.Value.transform.up, leadDirection);
    }
}

