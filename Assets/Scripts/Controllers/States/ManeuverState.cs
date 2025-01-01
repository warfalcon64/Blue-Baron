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

    public ManeuverState(float duration = 15f)
    {
        this.duration = duration;
    }

    public void OnEnter(AIControllerBase c)
    {
        maneuverTime = Random.Range(1, duration);
    }

    public void UpdateState(AIControllerBase c)
    {
        if (maneuverTime <= 0)
        {
            // change state
            c.ChangeState(c.searchState);
        }
        else
        {
            maneuverTime -= Time.deltaTime;
        }
    }

    public void OnHurt(AIControllerBase c) // <-- keep lists inside aicontroller instead, state use aicontroller methods to acess list
    {
        // if missile, add to missile queue. if other projectile, add shipbase to attacker queue
    }

    public void FixedUpdateState(AIControllerBase c)
    {
        if (c.target != null)
        {
            Vector2 tPos = c.target.GetComponent<Rigidbody2D>().position;
            Vector2 myPos = c.rb.position;
            Vector2 moveDirection = -(tPos - myPos).normalized;

            float angle = c.GetAngleToDestination(moveDirection);
            c.MoveToEvade(angle);

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
