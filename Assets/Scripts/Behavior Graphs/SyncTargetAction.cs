using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Sync Target", story: "[Self] syncs [Target] to controller", category: "Controller", id: "9ab1bddd17c3b577869ef28d7fd5bec3")]
public partial class SyncTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    private AIControllerBase ai;

    protected override Status OnStart()
    {
        if (ai == null)
            ai = Self.Value.GetComponent<AIControllerBase>();

        ai.SetTarget(Target.Value);
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

