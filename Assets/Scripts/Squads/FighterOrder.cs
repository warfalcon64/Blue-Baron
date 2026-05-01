using UnityEngine;

public struct FighterOrder
{
    public FighterMode mode;
    public Vector2 slotPosition;
    public Vector2 squadVelocity;
    public Vector2 squadHeading;
    public ShipBase target;
    public bool authorizeMissile;
    public float orderTimestamp;
}
