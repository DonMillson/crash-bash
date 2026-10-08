using UnityEngine;

namespace CrashBashRemake
{
    [RequireComponent(typeof(Rigidbody))]
    public class ArenaPaddle : MonoBehaviour
    {
        public ArenaSide side;
        public bool isHuman;
        public float speed = 8f;
        public Transform ballTarget;

        Rigidbody body;
        Vector3 home;
        float extent = 4.5f;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            home = transform.position;
        }

        void FixedUpdate()
        {
            float input = isHuman ? Input.GetAxisRaw("Horizontal") : BotInput();
            Vector3 p = body.position;

            if (side == ArenaSide.Bottom || side == ArenaSide.Top)
                p.x = Mathf.Clamp(p.x + input * speed * Time.fixedDeltaTime, -extent, extent);
            else
                p.z = Mathf.Clamp(p.z + input * speed * Time.fixedDeltaTime, -extent, extent);

            body.MovePosition(p);
        }

        float BotInput()
        {
            if (!ballTarget) return 0f;
            float delta = (side == ArenaSide.Bottom || side == ArenaSide.Top)
                ? ballTarget.position.x - transform.position.x
                : ballTarget.position.z - transform.position.z;
            return Mathf.Abs(delta) < .2f ? 0f : Mathf.Sign(delta);
        }

        public void ResetPaddle()
        {
            if (body) body.position = home;
            else transform.position = home;
        }
    }
}
