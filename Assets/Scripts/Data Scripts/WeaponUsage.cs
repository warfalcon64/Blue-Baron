using System;

[Flags]
public enum WeaponUsage
{
    None = 0,
    AntiShip = 1,
    AntiShield = 2,
    AntiMissile = 4,
    Multipurpose = 8,
    Missile = 16
}
