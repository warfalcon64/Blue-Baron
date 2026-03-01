using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Has Incoming Missile", story: "[Self] has close incoming missile", category: "Conditions", id: "18f897378bc3674f6366ea2d21e0993b")]
public partial class HasIncomingMissileCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<float> missileEvadeRange = new(15f);

    private AIControllerBase ai;

    public override bool IsTrue()
    {
        if (ai == null)
            ai = Self.Value.GetComponent<AIControllerBase>();

        return ai.HasCloseIncomingMissile(missileEvadeRange);
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
