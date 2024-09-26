using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManeuverState : State.IState
{
    private Action maneuver;

    [SerializeField]
    private float duration;

    public ManeuverState(Action maneuver)
    {
        this.maneuver = maneuver;
    }

    public void OnEnter(AIControllerBase c)
    {

    }

    public void UpdateState(AIControllerBase c)
    {
        if (duration <= 0)
        {
            // change state
        }
        else
        {
            duration -= Time.deltaTime;
        }
    }

    public void OnHurt(AIControllerBase c, WeaponsBase weapon, ShipBase attacker) // <-- keep lists inside aicontroller instead, state use aicontroller methods to acess list
    {
        // if missile, add to missile queue. if other projectile, add shipbase to attacker queue
    }

    public void FixedUpdateState(AIControllerBase c)
    {
        maneuver.Invoke();
    }

    public void OnExit(AIControllerBase c)
    {

    }
}
