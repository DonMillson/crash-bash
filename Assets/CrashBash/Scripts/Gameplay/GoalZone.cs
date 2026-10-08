using UnityEngine;

namespace CrashBashRemake
{
    public class GoalZone : MonoBehaviour
    {
        public ArenaSide side;
        public MatchManager match;

        void OnTriggerEnter(Collider other)
        {
            ArenaBall ball = other.GetComponent<ArenaBall>();
            if (ball) match.GoalConceded(side, ball);
        }
    }
}
