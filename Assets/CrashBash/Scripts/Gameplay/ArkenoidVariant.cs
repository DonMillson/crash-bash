using System;

namespace CrashBashRemake
{
    // Internal IDs are verified; correspondence to named levels is still unresolved.
    public enum ArkenoidVariant { BA, SE, NG, PI }

    public interface IArkenoidRules
    {
        ArkenoidVariant Variant { get; }
        int StartingScore { get; }
        bool HasCustomBounds { get; }
        bool HasCustomLaunchBounds { get; }
        string BoundsEntryPoint { get; }
        string OutOfBoundsEntryPoint { get; }
        string LaunchEntryPoint { get; }
        ArkenoidObjectType DeadWallType { get; }
        float ClampHero(float lateral, ArkenoidArenaGeometry geometry);
        float ClampHero(ArenaSide side, float lateral, ArkenoidArenaGeometry geometry);
        bool SweepBoundary(ArkVector point, ArkVector motion, ArkWallSegment wall,
            ArkenoidArenaGeometry geometry, out float time, out ArkVector normal);
        bool TestOutOfBounds(ArkVector point, ArkenoidArenaGeometry geometry);
        bool TestLaunchBounds(ArkVector point, ArkenoidArenaGeometry geometry);
        ArkVector LaunchPosition(int corner, ArkenoidArenaGeometry geometry);
    }

    /// <summary>
    /// Conservative shared implementation pending PS1 code/geometry measurements.
    /// Each variant has its own selected profile and explicit dispatch entry points.
    /// This fallback is not recovered BA/SE/NG/PI behavior.
    /// </summary>
    public abstract class ArkenoidRulesBase : IArkenoidRules
    {
        readonly int score;
        protected ArkenoidRulesBase(int startingScore = 15) { score = startingScore; }
        public abstract ArkenoidVariant Variant { get; }
        public int StartingScore => score;
        public bool HasCustomBounds => true;
        public abstract bool HasCustomLaunchBounds { get; }
        public abstract string BoundsEntryPoint { get; }
        public abstract string OutOfBoundsEntryPoint { get; }
        public abstract string LaunchEntryPoint { get; }
        public virtual ArkenoidObjectType DeadWallType => ArkenoidObjectType.DeadWall;
        public virtual float ClampHero(float lateral, ArkenoidArenaGeometry geometry)
            => ArkMath.Clamp(lateral, -geometry.Tuning.defenderTravel, geometry.Tuning.defenderTravel);
        public virtual float ClampHero(ArenaSide side, float lateral, ArkenoidArenaGeometry geometry)
        {
            geometry.DefenderLimits(side, out float minimum, out float maximum);
            return ArkMath.Clamp(lateral, minimum, maximum);
        }
        public virtual bool SweepBoundary(ArkVector point, ArkVector motion, ArkWallSegment wall,
            ArkenoidArenaGeometry geometry, out float time, out ArkVector normal)
            => ArkenoidArenaGeometry.SweepSegment(point, motion, geometry.Tuning.ballRadius, wall, out time, out normal);
        public virtual bool TestOutOfBounds(ArkVector point, ArkenoidArenaGeometry geometry)
            => geometry.OutsideRecoveryBounds(point);
        public virtual bool TestLaunchBounds(ArkVector point, ArkenoidArenaGeometry geometry)
            => geometry.InsideLaunchBounds(point);
        public virtual ArkVector LaunchPosition(int corner, ArkenoidArenaGeometry geometry)
            => geometry.CornerPosition(corner);
    }

    public sealed class BAArkenoidRules : ArkenoidRulesBase
    {
        public BAArkenoidRules(int score = 15) : base(score) { }
        public override ArkenoidVariant Variant => ArkenoidVariant.BA;
        public override bool HasCustomLaunchBounds => true;
        public override string BoundsEntryPoint => "AR_BA_BounceOnBounds";
        public override string OutOfBoundsEntryPoint => "AR_BA_TestOutOfBounds";
        public override string LaunchEntryPoint => "AR_BA_TestLaunchBounds";
    }
    public sealed class SEArkenoidRules : ArkenoidRulesBase
    {
        public SEArkenoidRules(int score = 15) : base(score) { }
        public override ArkenoidVariant Variant => ArkenoidVariant.SE;
        public override bool HasCustomLaunchBounds => false;
        public override string BoundsEntryPoint => "AR_SE_BounceOnBounds";
        public override string OutOfBoundsEntryPoint => "AR_SE_TestOutOfBounds";
        public override string LaunchEntryPoint => "unresolved; conservative fallback";
    }
    public sealed class NGArkenoidRules : ArkenoidRulesBase
    {
        public NGArkenoidRules(int score = 15) : base(score) { }
        public override ArkenoidVariant Variant => ArkenoidVariant.NG;
        public override bool HasCustomLaunchBounds => false;
        public override string BoundsEntryPoint => "unresolved; no NG bounce declaration";
        public override string OutOfBoundsEntryPoint => "AR_NG_TestOutOfBounds";
        public override string LaunchEntryPoint => "unresolved; conservative fallback";
        public override ArkenoidObjectType DeadWallType => ArkenoidObjectType.NGDeadWall;
    }
    public sealed class PIArkenoidRules : ArkenoidRulesBase
    {
        public PIArkenoidRules(int score = 15) : base(score) { }
        public override ArkenoidVariant Variant => ArkenoidVariant.PI;
        public override bool HasCustomLaunchBounds => true;
        public override string BoundsEntryPoint => "AR_PI_BounceOnBounds";
        public override string OutOfBoundsEntryPoint => "AR_PI_TestOutOfBounds";
        public override string LaunchEntryPoint => "AR_PI_TestLaunchBounds";
    }
    public static class ArkenoidRulesFactory
    {
        public static IArkenoidRules Create(ArkenoidVariant variant, int score)
        {
            switch (variant)
            {
                case ArkenoidVariant.BA: return new BAArkenoidRules(score);
                case ArkenoidVariant.SE: return new SEArkenoidRules(score);
                case ArkenoidVariant.NG: return new NGArkenoidRules(score);
                case ArkenoidVariant.PI: return new PIArkenoidRules(score);
                default: throw new ArgumentOutOfRangeException(nameof(variant));
            }
        }
    }
}
