using UnityEngine;

namespace CrashBashRemake
{
    /// <summary>Invisible gameplay body; the simulation owns movement bounds.</summary>
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class ArkenoidPlayerMotor : MonoBehaviour
    {
        public ArenaSide side;
        Rigidbody body;
        BoxCollider shape;
        ArkenoidSimulation simulation;
        ArkHeroModel model;
        public ArkHeroModel Model => model;
        public float LateralVelocity => model == null ? 0 : model.Velocity;
        public Vector3 Facing => ToWorld(ArkenoidArenaGeometry.Inward(side), 0);
        public static Vector3 ToWorld(ArkVector v, float height) => new Vector3(v.X, height, v.Y);
        public static ArkVector ToPlane(Vector3 v) => new ArkVector(v.x, v.z);

        void Awake() { EnsureBody(); }
        void EnsureBody()
        {
            body = GetComponent<Rigidbody>(); shape = GetComponent<BoxCollider>();
            body.isKinematic = true; body.useGravity = false;
            shape.isTrigger = true;
            // Root scale is always one: visual proportions cannot affect collision dimensions.
            transform.localScale = Vector3.one;
        }
        public void Configure(ArkenoidSimulation sim, ArkHeroModel hero)
        {
            EnsureBody();
            simulation = sim; model = hero; side = hero.Side;
            var t = sim.Tuning;
            bool horizontal = side == ArenaSide.Bottom || side == ArenaSide.Top;
            shape.size = horizontal ? new Vector3(t.defenderHalfWidth * 2, 1.05f, t.defenderHalfDepth * 2)
                : new Vector3(t.defenderHalfDepth * 2, 1.05f, t.defenderHalfWidth * 2);
            body.position = ToWorld(sim.Geometry.HeroPosition(side, hero.Lateral), 0);
        }
        public void SyncView()
        {
            if (model == null) return;
            shape.enabled = !model.IsEliminated;
            Vector3 position = ToWorld(simulation.Geometry.HeroPosition(side, model.Lateral), 0);
            body.position = position;
        }
        // Compatibility entry points forward into the authoritative model; no second movement loop.
        public void Move(float axis, bool boost)
        {
            if (model == null) return;
            simulation.SetInput(model.SlotId, new ArkInput { Axis = axis, Boost = boost });
        }
        public void ResetMotor() { SyncView(); }
    }
}
