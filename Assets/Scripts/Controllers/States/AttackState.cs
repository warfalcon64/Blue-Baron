using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : State.IState
{
    private Action attack;

    [SerializeField]
    private float duration;

    public AttackState(Action attack, float duration = 5f)
    {
        this.attack = attack;
        this.duration = duration;
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

    public void OnHurt(AIControllerBase c, WeaponsBase weapon, ShipBase attacker)
    {

    }

    public void FixedUpdateState(AIControllerBase c)
    {
        attack.Invoke();
    }

    public void OnExit(AIControllerBase c)
    {

    }
}
