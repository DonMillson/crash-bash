using UnityEngine;
namespace CrashBashRemake
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class ArkenoidPlayerMotor : MonoBehaviour
    {
        public ArenaSide side;
        public float moveSpeed = 7.5f;
        public float boostMultiplier = 1.5f;
        public float extent = 2.15f;
        Rigidbody body;
        Vector3 home;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            home = body.position;
        }

        public void Move(float axis, bool boost)
        {
            Vector3 p = body.position;
            float delta = axis * moveSpeed * (boost ? boostMultiplier : 1f) * Time.fixedDeltaTime;
            if (side == ArenaSide.Bottom || side == ArenaSide.Top)
                p.x = Mathf.Clamp(p.x + delta, -extent, extent);
            else
                p.z = Mathf.Clamp(p.z + delta, -extent, extent);
            body.MovePosition(p);
        }

        public void ResetMotor() => body.position = home;
    }
}