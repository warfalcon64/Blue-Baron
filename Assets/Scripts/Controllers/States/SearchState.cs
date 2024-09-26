using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SearchState : State.IState
{
    private Action search;

    [SerializeField]
    private float duration;

    public SearchState(Action search)
    {
        this.search = search;
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

    public void OnHurt(AIControllerBase c, WeaponsBase weapon, ShipBase attacker)
    {

    }

    public void FixedUpdateState(AIControllerBase c)
    {
        search.Invoke();
    }

    public void OnExit(AIControllerBase c)
    {

    }
}
