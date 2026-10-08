using UnityEngine;

namespace CrashBashRemake
{
    // Invisible diagnostic sensor. Authoritative swept goal-plane scoring runs in
    // ArkenoidSimulation, so a PhysX enter event cannot deduct a second point.
    public class GoalZone : MonoBehaviour
    {
        public ArenaSide side;
        public MatchManager match;
        void OnTriggerEnter(Collider other)
        {
            ArenaBall ball = other.GetComponent<ArenaBall>();
            if (ball && match) match.GoalConceded(side, ball);
        }
    }
}
