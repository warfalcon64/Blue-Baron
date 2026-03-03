using System;
using System.Collections.Generic;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Flee From Missile", story: "[Self] flees from missile", category: "Action", id: "d68cab7d2440fa5f1389e02830585c59")]
public partial class FleeFromMissileAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> MissileEvadeRange = new(15f);

    // Jink fields
    private float jinkOffset;
    private float jinkTimer;

    // Flare deployment thresholds
    private float flareRearAngle = 60f;
    private float flareEmergencyRange = 5f;

    private ShipBase ship;
    private Rigidbody2D rb;
    private AIControllerBase ai;

    protected override Status OnStart()
    {
        ship = Self.Value.GetComponent<ShipBase>();
        rb = Self.Value.GetComponent<Rigidbody2D>();
        ai = Self.Value.GetComponent<AIControllerBase>();

        RollJink();

        return Status.Running;
    }

    protected override void OnEnd()
    {
    }

    protected override Status OnUpdate()
    {
        if (!ai.HasCloseIncomingMissile(MissileEvadeRange))
            return Status.Success;

        float dt = Time.deltaTime;

        jinkTimer -= dt;
        if (jinkTimer <= 0f)
            RollJink();

        EvadeMissiles(dt);
        AttackTarget();

        return Status.Running;
    }

    private void EvadeMissiles(float dt)
    {
        Vector2 myPos = rb.position;
        Vector2 evadeDir = Vector2.zero;
        float closestDistSqr = float.MaxValue;
        List<WeaponsAAMissile> missiles = ai.incomingMissiles;

        for (int i = missiles.Count - 1; i >= 0; i--)
        {
            WeaponsAAMissile missile = missiles[i];
            if (missile == null || !missile.gameObject.activeInHierarchy)
            {
                missiles.RemoveAt(i);
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

        // Turn toward jinked direction and accelerate
        float angle = Vector2.SignedAngle((Vector2)Self.Value.transform.up, jinkedDir);
        float turn = 0f;
        if (Mathf.Abs(angle) > 5f)
        {
            if (angle > 0) turn = 1f;
            if (angle < 0) turn = -1f;
        }
        ship.Accelerate(0.2f);
        ship.SetShipTurn(turn);

        // Deploy flares when rear faces the threat, or immediately if missile is dangerously close
        Vector2 rear = -(Vector2)Self.Value.transform.up;
        Vector2 threatDir = -evadeDir;
        float rearAngle = Vector2.Angle(rear, threatDir);
        bool rearFacingThreat = rearAngle <= flareRearAngle;
        bool missileEmergency = closestDistSqr <= flareEmergencyRange * flareEmergencyRange;

        if (rearFacingThreat || missileEmergency)
            ship.ActivateCombatSystem();
    }

    private void AttackTarget()
    {
        if (Target.Value == null || !Target.Value.activeInHierarchy)
            return;

        Rigidbody2D targetRb = Target.Value.GetComponent<Rigidbody2D>();
        List<WeaponGroup> groups = ship.GetWeaponGroups();
        for (int i = 0; i < groups.Count; i++)
        {
            WeaponGroup group = groups[i];
            if (group.autonomous) continue;

            WeaponsBase repWeapon = group.GetRepresentativeWeapon();
            if (repWeapon == null) continue;

            // Urgent fire — skip missile angle gate during evasion
            List<Hardpoint> hps = group.GetHardpoints();
            Vector2 avgPos = Vector2.zero;
            foreach (Hardpoint hp in hps)
            {
                avgPos += (Vector2)hp.transform.position;
            }
            avgPos /= hps.Count;

            float speed = repWeapon.GetSpeed();
            float distance = Vector2.Distance(avgPos, targetRb.position);
            float travelTime = distance / speed;
            Vector2 aimPos = targetRb.position + targetRb.linearVelocity * travelTime;

            ship.FireGroup(i, aimPos);
        }
    }

    private void RollJink()
    {
        jinkOffset = UnityEngine.Random.Range(-1f, 1f) * 60f;
        jinkTimer = UnityEngine.Random.Range(0.3f, 0.8f);
    }
}
