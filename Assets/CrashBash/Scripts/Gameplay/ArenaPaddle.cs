using UnityEngine;

namespace CrashBashRemake
{
    /// <summary>Compatibility adapter for existing scenes. Arkenoid owns gameplay.</summary>
    [RequireComponent(typeof(ArkenoidHeroController))]
    public class ArenaPaddle : MonoBehaviour
    {
        public ArenaSide side;
        public bool isHuman;
        public Transform ballTarget;
        public bool eliminated;
        public void Eliminate()
        {
            eliminated = true;
            GetComponent<ArkenoidHeroController>().SetDead();
            var shape = GetComponent<Collider>(); if (shape) shape.enabled = false;
        }
        public void ResetPaddle()
        {
            eliminated = false;
            GetComponent<ArkenoidHeroController>().ResetController();
            GetComponent<ArkenoidPlayerMotor>().SyncView();
        }
    }
}
