using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Has Target", story: "[Target] is null", category: "Conditions", id: "131c5c3e8df0c6a2748f8e057191c19c")]
public partial class HasTargetCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    public override bool IsTrue()
    {
        if (Target.Value == null || !Target.Value.activeInHierarchy)
            return true;

        return false;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
