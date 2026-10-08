namespace CrashBashRemake
{
    // Mirrors the state families exposed by the original PS1 Arkenoid header.
    public enum ArkenoidHeroState
    {
        Idle,
        Move,
        Kick,
        RedKick,
        Grab,
        Taunt,
        Winner,
        Lose,
        Die,
        Dead
    }

    public enum ArkenoidObjectType
    {
        Ball = 0x1501,
        Pickup,
        Repulse,
        Flash,
        DeadWall,
        NGDeadWall,
        NGin,
        SeaWeed,
        LaserWall
    }
}
