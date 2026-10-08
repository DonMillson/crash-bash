using System;

namespace CrashBashRemake
{
    public enum ArkenoidMatchPhase { Countdown, Playing, RoundResult, MatchResult }
    public enum ArkEventKind { LaunchWarning, BallLaunched, Deflect, Kick, Repulse, Attract, Grab, Release, Goal, Eliminated, RoundWon, MatchWon, Pickup }

    public struct ArkInput
    {
        public float Axis;
        public bool Boost, KickPressed, RepulsePressed, AttractHeld, TauntPressed;
    }
    public struct ArkEvent
    {
        public ArkEventKind Kind;
        public int SlotId, BallId, Corner;
        public ArkVector Position;
        public ArkEvent(ArkEventKind kind, ArkVector position, int slot = -1, int ball = -1, int corner = -1)
        { Kind = kind; Position = position; SlotId = slot; BallId = ball; Corner = corner; }
    }

    public sealed class ArkHeroModel
    {
        public int SlotId { get; internal set; }
        public ArenaSide Side { get; internal set; }
        public CharacterId Character { get; internal set; }
        public bool Human { get; internal set; }
        public int Lives { get; internal set; }
        public int Wins { get; internal set; }
        public float Lateral { get; internal set; }
        public float PreviousLateral { get; internal set; }
        public float Velocity { get; internal set; }
        internal float MotorVelocity;
        public ArkenoidHeroState State { get; internal set; }
        public float StateTime { get; internal set; }
        public bool IsEliminated => Lives <= 0;
        public int RepulseCharges { get; internal set; }
        internal float ActionCooldown, ActionTime, BotThinkTime, BotTarget;
        internal int GrabbedBallId = -1;
        internal ArkInput Input;
    }

    public sealed class ArkBallModel
    {
        public int Id { get; internal set; }
        public ArkVector Position { get; internal set; }
        public ArkVector PreviousPosition { get; internal set; }
        public ArkVector Velocity { get; internal set; }
        public int GrabOwnerSlot { get; internal set; } = -1;
        public int LastTouchSlot { get; internal set; } = -1;
        public bool Scores { get; internal set; } = true;
        public bool Active { get; internal set; } = true;
        public float RespawnTime { get; internal set; }
        internal int LastCollisionSlot = -1;
        internal float ContactImmunity;
    }

    public struct ArkPlayerSetup
    {
        public int SlotId;
        public ArenaSide Side;
        public CharacterId Character;
        public bool Human;
        public ArkPlayerSetup(int slot, ArenaSide side, CharacterId character, bool human)
        { SlotId = slot; Side = side; Character = character; Human = human; }
    }
}
