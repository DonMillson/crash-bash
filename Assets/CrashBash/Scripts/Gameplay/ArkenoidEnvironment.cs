using UnityEngine;

namespace CrashBashRemake
{
    public abstract class ArkenoidEnvironment : MonoBehaviour
    {
        protected ArkenoidSimulation Simulation;
        public abstract ArkenoidVariant Variant { get; }
        public string BoundsHandler => Simulation == null ? "" : Simulation.Rules.BoundsEntryPoint;
        public string OutOfBoundsHandler => Simulation == null ? "" : Simulation.Rules.OutOfBoundsEntryPoint;
        public string LaunchHandler => Simulation == null ? "" : Simulation.Rules.LaunchEntryPoint;
        public void Configure(ArkenoidSimulation simulation)
        {
            if (simulation.Rules.Variant != Variant) throw new System.ArgumentException("Environment/rules variant mismatch.");
            Simulation = simulation;
        }
        public virtual void Build() { ValidateLaunchers(); }
        public virtual void Restart() { ValidateLaunchers(); }
        public virtual void TickEnvironment(float seconds) { }
        protected void ValidateLaunchers()
        {
            for (int corner = 0; corner < 4; corner++)
                if (!Simulation.Rules.TestLaunchBounds(Simulation.Rules.LaunchPosition(corner, Simulation.Geometry), Simulation.Geometry))
                    Debug.LogError(Variant + " launch position is outside its selected calibration profile.");
        }
        public static ArkenoidEnvironment AddTo(GameObject root, ArkenoidVariant variant)
        {
            switch (variant)
            {
                case ArkenoidVariant.BA: return root.AddComponent<BAEnvironment>();
                case ArkenoidVariant.SE: return root.AddComponent<SEEnvironment>();
                case ArkenoidVariant.NG: return root.AddComponent<NGEnvironment>();
                default: return root.AddComponent<PIEnvironment>();
            }
        }
    }
}
