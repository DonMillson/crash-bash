using UnityEngine;

namespace CrashBashRemake
{
    /// <summary>A view and invisible sensor for one shared Arkenoid ball model.</summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class ArenaBall : MonoBehaviour
    {
        public float launchSpeed = 7, maxSpeed = 16;
        public ArkBallModel Model { get; private set; }
        public ArkenoidSimulation Simulation { get; private set; }
        Rigidbody body;
        SphereCollider shape;
        Transform visual;
        TrailRenderer trail;
        bool externalFreeze;
        MatchManager host;
        bool lastActive;
        void Awake()
        {
            body = GetComponent<Rigidbody>(); shape = GetComponent<SphereCollider>();
            body.isKinematic = true; body.useGravity = false; shape.isTrigger = true;
            transform.localScale = Vector3.one;
        }
        public void Configure(ArkenoidSimulation simulation, ArkBallModel model, Transform visualModel, MatchManager viewHost = null)
        {
            Simulation = simulation; Model = model; visual = visualModel; host = viewHost;
            launchSpeed = simulation.Tuning.launchSpeed; maxSpeed = simulation.Tuning.maxBallSpeed;
            shape.radius = simulation.Tuning.ballRadius;
            trail = GetComponentInChildren<TrailRenderer>();
            if (trail) trail.Clear();
            SyncView(0);
        }
        void LateUpdate()
        {
            if (Model == null || !visual) return;
            float alpha = host ? host.PresentationAlpha : Simulation.InterpolationAlpha;
            ArkVector position = Model.PreviousPosition + (Model.Position - Model.PreviousPosition) * alpha;
            visual.localPosition = ArkenoidPlayerMotor.ToWorld(position - Model.Position, 0);
        }
        public void SyncView(float seconds)
        {
            if (Model == null) return;
            body.position = ArkenoidPlayerMotor.ToWorld(Model.Position, Simulation.Tuning.ballHeight);
            if (trail && Model.Active && !lastActive) trail.Clear();
            lastActive = Model.Active;
            shape.enabled = Model.Active && Model.GrabOwnerSlot == -1;
            if (visual) visual.gameObject.SetActive(Model.Active);
            if (trail) trail.emitting = Model.Active && Model.GrabOwnerSlot == -1 && Model.Velocity.LengthSquared > 1;
            if (visual && Model.Active && !externalFreeze)
                visual.Rotate(new Vector3(Model.Velocity.Y, 0, -Model.Velocity.X), seconds * 110, Space.World);
        }
        public void Kick(Vector3 direction, float multiplier)
            => Simulation?.KickBall(Model, ArkenoidPlayerMotor.ToPlane(direction), multiplier);
        public void Freeze(bool frozen)
        {
            if (Model == null) return;
            externalFreeze = frozen;
            Model.GrabOwnerSlot = frozen ? -2 : -1;
            if (frozen) Model.Velocity = new ArkVector();
        }
        public void LaunchFrom(Vector3 origin, Vector3 direction, float speed)
        {
            if (Model == null) return;
            Model.GrabOwnerSlot = -1; Model.Active = true;
            Model.Position = Model.PreviousPosition = ArkenoidPlayerMotor.ToPlane(origin);
            Model.Velocity = ArkenoidPlayerMotor.ToPlane(direction).Normalized * Mathf.Min(speed, maxSpeed);
            if (trail) trail.Clear();
            SyncView(0);
        }
        public void Launch() { LaunchFrom(transform.position, new Vector3(1, 0, 1), launchSpeed); }
        public void ResetBall()
        {
            if (Model == null) return;
            Model.Active = false; Model.RespawnTime = Simulation.Tuning.scoredBallDelay;
        }
    }
}
