using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.SceneManagement;

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
        if (duration <= 0 || !c.stopSearch)
        {
            // change state
        }
        else if (c.stopSearch)
        {
            // change state
        }
        else
        {
            duration -= Time.deltaTime;
        }
    }

    public void OnHurt(AIControllerBase c)
    {
    }

    public void FixedUpdateState(AIControllerBase c)
    {
        if (c.enemyTeam.Count > 0)
        {
            c.SetTarget(c.FindTarget());
            c.target.GetComponent<ShipBase>().OnShipDeath += c.HandleTargetDeath;

            if (c.target != null)
            {
                c.ChangeState(c.attackState);
            }
        }
    }

    public void OnExit(AIControllerBase c)
    {

    }
}
