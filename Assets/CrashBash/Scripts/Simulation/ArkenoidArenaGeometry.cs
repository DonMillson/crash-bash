using System;
using System.Collections.Generic;

namespace CrashBashRemake
{
    public struct ArkWallSegment
    {
        public ArkVector A, B;
        public ArenaSide Side;
        public bool DeadWall;
        public ArkWallSegment(ArkVector a, ArkVector b, ArenaSide side, bool deadWall = false)
        { A = a; B = b; Side = side; DeadWall = deadWall; }
    }

    public sealed class ArkenoidArenaGeometry
    {
        public readonly ArkenoidTuning Tuning;
        public ArkenoidArenaGeometry(ArkenoidTuning tuning) { tuning.Validate(); Tuning = tuning; }
        public static ArkVector Inward(ArenaSide side)
        {
            switch (side)
            {
                case ArenaSide.Bottom: return new ArkVector(0, 1);
                case ArenaSide.Right: return new ArkVector(-1, 0);
                case ArenaSide.Top: return new ArkVector(0, -1);
                default: return new ArkVector(1, 0);
            }
        }
        public static ArkVector Tangent(ArenaSide side)
        {
            ArkVector n = Inward(side);
            return new ArkVector(n.Y, -n.X);
        }
        public ArkVector SidePoint(ArenaSide side, float lateral, float distance)
            => -Inward(side) * distance + Tangent(side) * lateral;
        public ArkVector HeroPosition(ArenaSide side, float lateral)
            => SidePoint(side, lateral, Tuning.defenderLine);
        public float Lateral(ArenaSide side, ArkVector point) => ArkVector.Dot(point, Tangent(side));
        public float OutwardDistance(ArenaSide side, ArkVector point) => ArkVector.Dot(point, -Inward(side));

        public IEnumerable<ArkWallSegment> Walls(IReadOnlyList<ArkHeroModel> heroes)
        {
            float h = Tuning.wallHalfExtent, g = Tuning.goalHalfWidth;
            for (int i = 0; i < 4; i++)
            {
                ArenaSide side = (ArenaSide)i;
                yield return new ArkWallSegment(SidePoint(side, -h, h), SidePoint(side, -g, h), side);
                yield return new ArkWallSegment(SidePoint(side, g, h), SidePoint(side, h, h), side);
                for (int j = 0; j < heroes.Count; j++)
                    if (heroes[j].Side == side && heroes[j].IsEliminated)
                        yield return new ArkWallSegment(SidePoint(side, -g, h), SidePoint(side, g, h), side, true);
            }
        }

        public ArkVector CornerPosition(int corner)
        {
            float d = Tuning.wallHalfExtent - Tuning.ballRadius - .65f;
            return new ArkVector((corner == 0 || corner == 3) ? -d : d, corner < 2 ? -d : d);
        }
        public bool InsideLaunchBounds(ArkVector point) =>
            Math.Abs(point.X) <= Tuning.wallHalfExtent - Tuning.ballRadius &&
            Math.Abs(point.Y) <= Tuning.wallHalfExtent - Tuning.ballRadius;
        public bool OutsideRecoveryBounds(ArkVector point) =>
            Math.Abs(point.X) > Tuning.goalPlane + 2 || Math.Abs(point.Y) > Tuning.goalPlane + 2;

        /// <summary>Continuous circle vs finite wall segment, including the rounded endpoints.</summary>
        public static bool SweepSegment(ArkVector point, ArkVector delta, float radius,
            ArkWallSegment wall, out float time, out ArkVector normal)
        {
            time = 2; normal = new ArkVector();
            ArkVector edge = wall.B - wall.A;
            float length = edge.Length;
            if (length < .0001f) return false;
            ArkVector tangent = edge / length;
            ArkVector n = new ArkVector(-tangent.Y, tangent.X);
            float d = ArkVector.Dot(point - wall.A, n), v = ArkVector.Dot(delta, n);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                if (v * sign >= -.000001f) continue;
                float t = (sign * radius - d) / v;
                float along = ArkVector.Dot(point + delta * t - wall.A, tangent);
                if (t >= -.00001f && t <= 1 && along >= 0 && along <= length && t < time)
                { time = Math.Max(0, t); normal = n * sign; }
            }
            SweepEndpoint(point, delta, radius, wall.A, ref time, ref normal);
            SweepEndpoint(point, delta, radius, wall.B, ref time, ref normal);
            return time <= 1;
        }

        static void SweepEndpoint(ArkVector p, ArkVector delta, float radius, ArkVector centre,
            ref float best, ref ArkVector normal)
        {
            ArkVector offset = p - centre;
            float a = delta.LengthSquared, b = 2 * ArkVector.Dot(offset, delta), c = offset.LengthSquared - radius * radius;
            if (a < .000001f || b >= 0) return;
            float disc = b * b - 4 * a * c;
            if (disc < 0) return;
            float t = (-b - (float)Math.Sqrt(disc)) / (2 * a);
            if (t >= -.00001f && t <= 1 && t < best)
            { best = Math.Max(0, t); normal = (p + delta * best - centre).Normalized; }
        }

        public static bool SweepBox(ArkVector point, ArkVector delta, float halfX, float halfY,
            out float time, out ArkVector normal)
        {
            float enter = 0, leave = 1;
            normal = new ArkVector(); time = 2;
            for (int axis = 0; axis < 2; axis++)
            {
                float p = axis == 0 ? point.X : point.Y, d = axis == 0 ? delta.X : delta.Y;
                float extent = axis == 0 ? halfX : halfY;
                if (Math.Abs(d) < .000001f) { if (Math.Abs(p) > extent) return false; continue; }
                float a = (-extent - p) / d, b = (extent - p) / d;
                ArkVector n = axis == 0 ? new ArkVector(-Math.Sign(d), 0) : new ArkVector(0, -Math.Sign(d));
                if (a > b) { float swap = a; a = b; b = swap; }
                if (a > enter) { enter = a; normal = n; }
                leave = Math.Min(leave, b);
                if (enter > leave) return false;
            }
            if (enter < 0 || enter > 1 || normal.LengthSquared < .5f) return false;
            time = enter;
            return true;
        }
    }
}
