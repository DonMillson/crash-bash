using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrashBashRemake
{
    public class MatchManager : MonoBehaviour
    {
        public readonly List<PlayerSlot> slots = new();
        public ArenaBall ball;
        bool resolving;

        public void Configure(List<PlayerSlot> configuredSlots, ArenaBall arenaBall)
        {
            slots.Clear();
            slots.AddRange(configuredSlots);
            ball = arenaBall;
        }

        public void GoalConceded(ArenaSide side, ArenaBall scoredBall)
        {
            if (resolving) return;
            PlayerSlot slot = slots.Find(s => s.side == side);
            if (slot == null || slot.lives <= 0) return;
            slot.lives--;
            Debug.Log($"{slot.character} conceded! Lives: {slot.lives}");
            StartCoroutine(ResetAfterGoal(scoredBall));
        }

        IEnumerator ResetAfterGoal(ArenaBall scoredBall)
        {
            resolving = true;
            scoredBall.gameObject.SetActive(false);
            yield return new WaitForSeconds(.7f);
            foreach (var s in slots) s.paddle?.ResetPaddle();

            int alive = 0;
            PlayerSlot winner = null;
            foreach (var s in slots)
                if (s.lives > 0) { alive++; winner = s; }

            if (alive <= 1)
            {
                Debug.Log(winner != null ? $"WINNER: {winner.character}" : "DRAW");
                yield return new WaitForSeconds(1.2f);
                foreach (var s in slots) s.lives = 5;
            }

            scoredBall.gameObject.SetActive(true);
            scoredBall.ResetBall();
            resolving = false;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                StopAllCoroutines();
                resolving = false;
                foreach (var s in slots) { s.lives = 5; s.paddle?.ResetPaddle(); }
                if (ball) { ball.gameObject.SetActive(true); ball.ResetBall(); }
            }
        }

        void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, 80));
            GUILayout.BeginHorizontal();
            foreach (var s in slots)
                GUILayout.Label($"{s.character}: {s.lives}", style, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
