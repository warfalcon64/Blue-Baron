using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Random = UnityEngine.Random;

public class AttackState : State.IState
{
    [SerializeField]
    private float duration;

    private float attackTime;

    // Stale engagement detection
    private float lastSampledDistance;
    private float staleTimer;
    private float sampleTimer;
    private float staleThreshold = 4f;
    private float staleTimeLimit;
    private float staleTimeLimitMin = 0.5f;
    private float staleTimeLimitMax = 2f;
    private float staleSampleInterval = 0.5f;

    // Break turn
    private bool isDisengaging;
    private float disengageTimer;
    private float disengageDuration = 0.75f;

    public AttackState(float duration = 15f)
    {
        this.duration = duration;
    }

    public void OnEnter(AIControllerBase c)
    {
        attackTime = Random.Range(4, duration);
        lastSampledDistance = 0f;
        staleTimer = 0f;
        sampleTimer = 0f;
        staleTimeLimit = Random.Range(staleTimeLimitMin, staleTimeLimitMax);
        isDisengaging = false;
        disengageTimer = 0f;
    }

    public void UpdateState(AIControllerBase c)
    {
        // if (attackTime <= 0)
        // {
        //     c.ChangeState(c.maneuverState);
        // }
        // else
        // {
        //     attackTime -= Time.deltaTime;
        // }
    }

    public void OnHurt(AIControllerBase c)
    {
        if (c.target != null && c.attacker != null)
        {
            Vector2 aPos = c.attacker.GetComponent<Rigidbody2D>().position;
            Vector2 tPos = c.target.GetComponent<Rigidbody2D>().position;
            float aDistance = (aPos - c.rb.position).sqrMagnitude;
            float tDistance = (tPos - c.rb.position).sqrMagnitude;

            c.SetTarget(c.attacker); // * may have to later add condition instead of blind setting to avoid dumb AI
        }
    }

    public void FixedUpdateState(AIControllerBase c)
    {
        // Replaced by DogfightTargetAction in behavior graph
    }

    public void OnExit(AIControllerBase c)
    {

    }
}
