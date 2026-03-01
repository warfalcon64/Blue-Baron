using System;
using System.Collections.Generic;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Find Target", story: "[Agent] finds [Target]", category: "Action", id: "c6038e1683d162433dcefb77967f7392")]
public partial class FindTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    
    private Rigidbody2D rb;

    private List<ShipBase> enemyTeam;

    protected override Status OnStart()
    {
        rb = Agent.Value.GetComponent<Rigidbody2D>();

        if (enemyTeam.Count == 0)
            return Status.Failure;

        float lowestDistance = Mathf.Infinity;
        GameObject closest = null;

        foreach (ShipBase enemy in enemyTeam)
        {
            if (!enemy.gameObject.activeInHierarchy) continue;

            float distance = (enemy.GetComponent<Rigidbody2D>().position - rb.position).sqrMagnitude;
            if (distance < lowestDistance)
            {
                lowestDistance = distance;
                closest = enemy.gameObject;
            }
        }

        if (closest == null)
            return Status.Failure;

        Target.Value = closest;
        return Status.Success;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

