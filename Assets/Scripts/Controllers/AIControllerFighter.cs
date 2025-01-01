using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class AIControllerFighter : AIControllerBase
{
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    public override void AttackTarget(Vector2 targetAcceleration, float angle) => base.AttackTarget(targetAcceleration, angle);

    public override GameObject FindTarget() => base.FindTarget();

    public override void MoveToEngage(float angle) => base.MoveToEngage(angle);

    protected override void UpdateTimers() => base.UpdateTimers();

    public override float GetAngleToTarget() => base.GetAngleToTarget();

    public override Vector2 CalculateTargetAcceleration() => base.CalculateTargetAcceleration();

    protected override Vector2 GetTargetLeadingPosition(UnityEngine.Vector2 targetAcceleration, int iterations, WeaponsBase weapon) => base.GetTargetLeadingPosition(targetAcceleration, iterations, weapon);

    protected override float SolveQuarticNewton(float guess, int iterations, float a, float b, float c, float d, float e) => base.SolveQuarticNewton(guess, iterations, a, b, c, d, e);

    protected override float EvalQuartic(float t, float a, float b, float c, float d, float e) => base.EvalQuartic(t, a, b, c, d, e);

    protected override float EvalQuarticDerivative(float t, float a, float b, float c, float d) => base.EvalQuarticDerivative(t, a, b, c, d);
}
