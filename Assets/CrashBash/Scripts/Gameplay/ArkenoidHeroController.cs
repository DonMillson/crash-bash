using UnityEngine;

namespace CrashBashRemake
{
    [RequireComponent(typeof(ArkenoidPlayerMotor))]
    public sealed class ArkenoidHeroController : MonoBehaviour
    {
        public bool human;
        public Transform ballTarget; // Legacy reference; bot targeting now examines every simulation ball.
        public int inputIndex;
        public ArkenoidHeroState State => Model == null ? ArkenoidHeroState.Idle : Model.State;
        public ArkHeroModel Model { get; private set; }
        public ArkenoidPlayerMotor Motor { get; private set; }
        public ArkenoidSimulation Simulation { get; private set; }
        ArkInput pending;
        bool acceptInput;

        void Awake() { Motor = GetComponent<ArkenoidPlayerMotor>(); }
        public void Configure(ArkenoidSimulation simulation, ArkHeroModel model, int device)
        {
            Simulation = simulation; Model = model; human = model.Human; inputIndex = device;
            Motor.Configure(simulation, model); acceptInput = true; enabled = true;
        }
        public void AllowInput(bool allow) { acceptInput = allow; if (!allow) pending = new ArkInput(); }
        void Update()
        {
            if (!human || Model == null || !acceptInput || Model.IsEliminated) return;
            // Keyboard layouts are independent of slot, side and selected character.
            if (inputIndex == 0)
            {
                pending.Axis = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0)
                    - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
                pending.Boost = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E);
                pending.KickPressed |= Input.GetKeyDown(KeyCode.Space);
                pending.RepulsePressed |= Input.GetKeyDown(KeyCode.X);
                pending.AttractHeld = Input.GetKey(KeyCode.LeftControl);
                pending.TauntPressed |= Input.GetKeyDown(KeyCode.T);
            }
            else if (inputIndex == 1)
            {
                pending.Axis = (Input.GetKey(KeyCode.L) ? 1 : 0) - (Input.GetKey(KeyCode.J) ? 1 : 0);
                pending.Boost = Input.GetKey(KeyCode.RightShift);
                pending.KickPressed |= Input.GetKeyDown(KeyCode.I);
                pending.RepulsePressed |= Input.GetKeyDown(KeyCode.O);
                pending.AttractHeld = Input.GetKey(KeyCode.K);
                pending.TauntPressed |= Input.GetKeyDown(KeyCode.P);
            }
        }
        public void SubmitInput()
        {
            if (Model == null || !human) return;
            Simulation.SetInput(Model.SlotId, acceptInput ? pending : new ArkInput());
            pending.KickPressed = pending.RepulsePressed = pending.TauntPressed = false;
        }
        public void SetDead() { acceptInput = false; pending = new ArkInput(); }
        public void ResetController() { acceptInput = true; pending = new ArkInput(); enabled = true; }
    }
}
