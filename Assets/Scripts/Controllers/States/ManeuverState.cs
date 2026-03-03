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
        // Replaced by behavior graph flow
    }

    public void OnHurt(AIControllerBase c)
    {
    }

    public void FixedUpdateState(AIControllerBase c)
    {
        // Replaced by FleeFromMissileAction in behavior graph
    }

    public bool HasCloseIncomingMissile(AIControllerBase c)
    {
        return c.HasCloseIncomingMissile(missileEvadeRange);
    }

    // Replaced by FleeFromMissileAction in behavior graph
    // private void EvadeMissiles(AIControllerBase c) { ... }

    private void RollJink()
    {
        jinkOffset = Random.Range(-1f, 1f) * 60f;
        jinkTimer = Random.Range(0.3f, 0.8f);
    }

    public void OnExit(AIControllerBase c)
    {
    }
}
