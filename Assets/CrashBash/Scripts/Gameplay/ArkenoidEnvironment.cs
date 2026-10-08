using UnityEngine;

namespace CrashBashRemake
{
    public abstract class ArkenoidEnvironment : MonoBehaviour
    {
        public abstract ArkenoidVariant Variant { get; }
        public virtual void Build() { }
        public virtual void TickEnvironment() { }
        protected virtual void Update() => TickEnvironment();

        protected GameObject MakeTrigger(string name, Vector3 position, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = scale;
            return go;
        }
    }

    // Original PS1 header exposes separate BA/SE/NG/PI bounds handlers.
    // These modules are intentionally separate even before every numeric constant is decoded.
    public sealed class BAEnvironment : ArkenoidEnvironment
    {
        public override ArkenoidVariant Variant => ArkenoidVariant.BA;
    }

    public sealed class SEEnvironment : ArkenoidEnvironment
    {
        public override ArkenoidVariant Variant => ArkenoidVariant.SE;
        public override void Build()
        {
            // Seaweed is a real object family in the PS1 Arkenoid subsystem.
            // Visual/physics values stay disabled until decoded rather than invented.
        }
    }

    public sealed class NGEnvironment : ArkenoidEnvironment
    {
        public override ArkenoidVariant Variant => ArkenoidVariant.NG;
        public override void Build()
        {
            // Reserved for N.Gin and NGDeadWall behavior recovered from the original subsystem.
        }
    }

    public sealed class PIEnvironment : ArkenoidEnvironment
    {
        public override ArkenoidVariant Variant => ArkenoidVariant.PI;
        public override void Build()
        {
            // Reserved for PI-specific launch bounds and laser-wall behavior.
        }
    }
}
