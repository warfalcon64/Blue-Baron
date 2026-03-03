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
        // Replaced by FindTargetAction in behavior graph
    }

    public void OnExit(AIControllerBase c)
    {

    }
}
