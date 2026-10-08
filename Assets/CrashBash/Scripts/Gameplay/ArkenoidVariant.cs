namespace CrashBashRemake
{
    public enum ArkenoidVariant { CrashBall, PolarPanicBall, NGinBall, SkyBall }

    public interface IArkenoidRules
    {
        ArkenoidVariant Variant { get; }
        int StartingScore { get; }
        int BallsLimit { get; }
        bool SupportsGrab { get; }
        bool SupportsRepulse { get; }
        bool SupportsAttract { get; }
    }

    public sealed class CrashBallRules : IArkenoidRules
    {
        public ArkenoidVariant Variant => ArkenoidVariant.CrashBall;
        public int StartingScore => 15;
        public int BallsLimit => 5;
        public bool SupportsGrab => false;
        public bool SupportsRepulse => true;
        public bool SupportsAttract => false;
    }
}