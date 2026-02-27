using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SearchState : State.IState
{

    [SerializeField]
    private float duration;

    public SearchState(float duration = 15f)
    {
        this.duration = duration;
    }


    public void OnEnter(AIControllerBase c)
    {

    }

    public void UpdateState(AIControllerBase c)
    {
    }

    public void OnHurt(AIControllerBase c)
    {
    }

    public void FixedUpdateState(AIControllerBase c)
    {
        if (c.enemyTeam.Count > 0)
        {
            GameObject found = c.FindTarget();
            if (found != null)
            {
                c.SetTarget(found);
                c.ChangeState(c.attackState);
            }
        }
    }

    public void OnExit(AIControllerBase c)
    {

    }
}
