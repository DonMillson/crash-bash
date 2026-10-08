using UnityEngine;

namespace CrashBashRemake
{
    [RequireComponent(typeof(ArkenoidPlayerMotor))]
    public sealed class ArkenoidHeroController : MonoBehaviour
    {
        public bool human;
        public Transform ballTarget;
        public ArkenoidHeroState State { get; private set; } = ArkenoidHeroState.Idle;

        public float kickRadius = 1.8f;
        public float grabRadius = 1.55f;
        public float kickCooldown = .55f;
        public float grabHoldSeconds = .8f;

        ArkenoidPlayerMotor motor;
        ArenaBall grabbedBall;
        float nextAction;
        float grabReleaseAt;

        void Awake() => motor = GetComponent<ArkenoidPlayerMotor>();

        void Update()
        {
            if (State == ArkenoidHeroState.Dead || State == ArkenoidHeroState.Die) return;

            if (grabbedBall)
            {
                State = ArkenoidHeroState.Grab;
                grabbedBall.transform.position = transform.position + Facing() * .9f + Vector3.up * .1f;
                if (Time.time >= grabReleaseAt || (human && Input.GetKeyUp(KeyCode.Space)))
                    ReleaseGrab();
                return;
            }

            bool kick = human ? Input.GetKeyDown(KeyCode.Space) : BotAction(kickRadius);
            bool grab = human ? Input.GetKeyDown(KeyCode.LeftControl) : false;
            bool repel = human ? Input.GetKeyDown(KeyCode.X) : BotAction(kickRadius * 1.15f);

            if (Time.time >= nextAction)
            {
                if (grab) TryGrab();
                else if (repel) Repulse();
                else if (kick) Kick();
            }
        }

        void FixedUpdate()
        {
            if (State == ArkenoidHeroState.Dead || State == ArkenoidHeroState.Die || grabbedBall) return;
            float axis = human ? Input.GetAxisRaw("Horizontal") : BotAxis();
            bool boost = human ? Input.GetKey(KeyCode.LeftShift) : Mathf.Abs(axis) > .5f;
            motor.Move(axis, boost);
            if (Time.time >= nextAction)
                State = Mathf.Abs(axis) > .05f ? ArkenoidHeroState.Move : ArkenoidHeroState.Idle;
        }

        float BotAxis()
        {
            if (!ballTarget) return 0f;
            float d = (motor.side == ArenaSide.Bottom || motor.side == ArenaSide.Top)
                ? ballTarget.position.x - transform.position.x
                : ballTarget.position.z - transform.position.z;
            return Mathf.Abs(d) < .12f ? 0f : Mathf.Sign(d);
        }

        bool BotAction(float radius) => ballTarget && Vector3.Distance(ballTarget.position, transform.position) <= radius && Random.value < .08f;

        Vector3 Facing() => motor.side switch
        {
            ArenaSide.Bottom => Vector3.forward,
            ArenaSide.Top => Vector3.back,
            ArenaSide.Left => Vector3.right,
            _ => Vector3.left
        };

        void Kick()
        {
            State = ArkenoidHeroState.Kick;
            foreach (Collider hit in Physics.OverlapSphere(transform.position + Facing() * .55f, kickRadius))
            {
                ArenaBall b = hit.GetComponent<ArenaBall>();
                if (b) b.Kick(Facing(), 1.35f);
            }
            nextAction = Time.time + kickCooldown;
        }

        void Repulse()
        {
            State = ArkenoidHeroState.RedKick;
            foreach (Collider hit in Physics.OverlapSphere(transform.position, kickRadius * 1.15f))
            {
                ArenaBall b = hit.GetComponent<ArenaBall>();
                if (!b) continue;
                Vector3 away = b.transform.position - transform.position;
                away.y = 0f;
                b.Kick(away.sqrMagnitude > .01f ? away : Facing(), 1.55f);
            }
            nextAction = Time.time + kickCooldown * 1.35f;
        }

        void TryGrab()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position + Facing() * .45f, grabRadius);
            float best = float.MaxValue;
            foreach (Collider hit in hits)
            {
                ArenaBall b = hit.GetComponent<ArenaBall>();
                if (!b) continue;
                float d = (b.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; grabbedBall = b; }
            }
            if (!grabbedBall) return;
            grabbedBall.Freeze(true);
            State = ArkenoidHeroState.Grab;
            grabReleaseAt = Time.time + grabHoldSeconds;
            nextAction = grabReleaseAt + .15f;
        }

        void ReleaseGrab()
        {
            ArenaBall b = grabbedBall;
            grabbedBall = null;
            b.Freeze(false);
            b.LaunchFrom(transform.position + Facing(), Facing(), b.maxSpeed * .8f);
            State = ArkenoidHeroState.Kick;
        }

        public void SetDead()
        {
            State = ArkenoidHeroState.Dead;
            enabled = false;
        }
    }
}
