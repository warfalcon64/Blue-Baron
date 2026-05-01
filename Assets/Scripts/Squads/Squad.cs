using System.Collections.Generic;
using UnityEngine;

public class Squad : MonoBehaviour
{
    private readonly List<FighterController> members = new List<FighterController>();

    public Vector2 anchorPos;
    public Vector2 anchorVelocity;
    public Vector2 anchorHeading;

    public void RegisterMember(FighterController brain)
    {
        if (brain != null && !members.Contains(brain))
            members.Add(brain);
    }

    public IReadOnlyList<FighterController> GetMembers() => members;
}
