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
        if (attackTime <= 0)
        {
            c.ChangeState(c.maneuverState);
        }
        else
        {
            attackTime -= Time.deltaTime;
        }
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
        if (c.target == null || !c.target.activeInHierarchy)
        {
            c.ChangeState(c.searchState);
            return;
        }

        float angle = c.GetAngleToLeadTarget();
        Rigidbody2D targetRb = c.target.GetComponent<Rigidbody2D>();

        Vector2 tDirection = targetRb.position - c.rb.position;
        float distance = tDirection.magnitude;

        if (isDisengaging)
        {
            // Break turn: reverse turn direction and accelerate to max
            float reverseTurn = angle > 0 ? -1f : 1f;
            c.ship.SetShipTurn(reverseTurn);
            c.ship.Accelerate(0.2f);

            // Still fire if target is in arc
            c.AttackTarget();

            disengageTimer -= Time.fixedDeltaTime;
            if (disengageTimer <= 0f)
            {
                isDisengaging = false;
                staleTimer = 0f;
                sampleTimer = 0f;
                staleTimeLimit = Random.Range(staleTimeLimitMin, staleTimeLimitMax);
                lastSampledDistance = distance;
            }
        }
        else
        {
            // Sample distance periodically for stale detection
            sampleTimer -= Time.fixedDeltaTime;
            if (sampleTimer <= 0f)
            {
                if (lastSampledDistance > 0f)
                {
                    if (Mathf.Abs(distance - lastSampledDistance) < staleThreshold)
                    {
                        staleTimer += staleSampleInterval;
                    }
                    else
                    {
                        staleTimer = 0f;
                    }
                }

                lastSampledDistance = distance;
                sampleTimer = staleSampleInterval;

                // Trigger break turn
                if (staleTimer >= staleTimeLimit)
                {
                    isDisengaging = true;
                    disengageTimer = disengageDuration;
                    return;
                }
            }

            c.MoveToEngage(angle);
            c.AttackTarget();
        }
    }

    public void OnExit(AIControllerBase c)
    {

    }
}
