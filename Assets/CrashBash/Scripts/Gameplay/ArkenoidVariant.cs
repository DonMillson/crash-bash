namespace CrashBashRemake
{
    // Internal variant IDs preserved from the original PS1 Arkenoid subsystem.
    // Do not rename these to guessed level names until the BIN mapping is proven.
    public enum ArkenoidVariant { BA, SE, NG, PI }

    public interface IArkenoidRules
    {
        ArkenoidVariant Variant { get; }
        int StartingScore { get; }
        bool HasCustomBounds { get; }
        bool HasCustomLaunchBounds { get; }
    }

    public sealed class BAArkenoidRules : IArkenoidRules
    {
        public ArkenoidVariant Variant => ArkenoidVariant.BA;
        public int StartingScore => 15;
        public bool HasCustomBounds => true;
        public bool HasCustomLaunchBounds => true;
    }

    public sealed class SEArkenoidRules : IArkenoidRules
    {
        public ArkenoidVariant Variant => ArkenoidVariant.SE;
        public int StartingScore => 15;
        public bool HasCustomBounds => true;
        public bool HasCustomLaunchBounds => false;
    }

    public sealed class NGArkenoidRules : IArkenoidRules
    {
        public ArkenoidVariant Variant => ArkenoidVariant.NG;
        public int StartingScore => 15;
        public bool HasCustomBounds => true;
        public bool HasCustomLaunchBounds => false;
    }

    public sealed class PIArkenoidRules : IArkenoidRules
    {
        public ArkenoidVariant Variant => ArkenoidVariant.PI;
        public int StartingScore => 15;
        public bool HasCustomBounds => true;
        public bool HasCustomLaunchBounds => true;
    }
}
