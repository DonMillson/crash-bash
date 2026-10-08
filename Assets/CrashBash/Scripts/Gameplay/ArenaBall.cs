using UnityEngine;

namespace CrashBashRemake
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class ArenaBall : MonoBehaviour
    {
        public float launchSpeed = 8f;
        public float maxSpeed = 15f;
        Rigidbody body;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        void Start() => Launch();

        void FixedUpdate()
        {
            Vector3 v = body.linearVelocity;
            v.y = 0f;
            if (v.magnitude > maxSpeed) v = v.normalized * maxSpeed;
            if (v.magnitude < launchSpeed * .55f && v.sqrMagnitude > .01f) v = v.normalized * launchSpeed * .55f;
            body.linearVelocity = v;
        }

        public void ResetBall()
        {
            body.position = new Vector3(0f, .55f, 0f);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            Launch();
        }

        void Launch()
        {
            Vector2 d = Random.insideUnitCircle.normalized;
            if (Mathf.Abs(d.x) < .25f) d.x = Mathf.Sign(d.x == 0 ? 1 : d.x) * .35f;
            if (Mathf.Abs(d.y) < .25f) d.y = Mathf.Sign(d.y == 0 ? 1 : d.y) * .35f;
            body.linearVelocity = new Vector3(d.x, 0, d.y).normalized * launchSpeed;
        }
    }
}
