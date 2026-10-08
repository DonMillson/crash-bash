using System;

namespace CrashBashRemake
{
    /// <summary>Gameplay plane (Unity X/Z). No rendering or physics dependency.</summary>
    [Serializable]
    public struct ArkVector
    {
        public float X, Y;
        public ArkVector(float x, float y) { X = x; Y = y; }
        public float LengthSquared => X * X + Y * Y;
        public float Length => (float)Math.Sqrt(LengthSquared);
        public ArkVector Normalized => LengthSquared > .000001f ? this / Length : new ArkVector(0, 1);
        public static ArkVector operator +(ArkVector a, ArkVector b) => new ArkVector(a.X + b.X, a.Y + b.Y);
        public static ArkVector operator -(ArkVector a, ArkVector b) => new ArkVector(a.X - b.X, a.Y - b.Y);
        public static ArkVector operator -(ArkVector a) => new ArkVector(-a.X, -a.Y);
        public static ArkVector operator *(ArkVector a, float b) => new ArkVector(a.X * b, a.Y * b);
        public static ArkVector operator /(ArkVector a, float b) => new ArkVector(a.X / b, a.Y / b);
        public static float Dot(ArkVector a, ArkVector b) => a.X * b.X + a.Y * b.Y;
        public static ArkVector Reflect(ArkVector velocity, ArkVector normal) => velocity - normal * (2 * Dot(velocity, normal));
        public override string ToString() => $"({X:0.000}, {Y:0.000})";
    }

    internal static class ArkMath
    {
        public static float Clamp(float x, float min, float max) => Math.Max(min, Math.Min(max, x));
        public static float MoveTowards(float x, float target, float distance) => x + Clamp(target - x, -distance, distance);
        public static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
    }
}
