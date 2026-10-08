using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrashBashRemake
{
    /// <summary>Unity host for AR_InitLevel / AR_UpdateLevel / AR_RestartLevel.</summary>
    public class MatchManager : MonoBehaviour
    {
        public readonly List<PlayerSlot> slots = new List<PlayerSlot>();
        public ArenaBall ball;
        public ArkenoidSimulation Simulation { get; private set; }
        public bool Paused { get; private set; }
        public bool MenuOpen { get; private set; }
        bool pausedBeforeMenu;
        int menuChangedFrame = -1;
        float presentationTime;
        public float PresentationAlpha => Simulation == null ? 1 : Paused ? 1 : Mathf.Clamp01(
            Simulation.InterpolationAlpha + (Time.time - presentationTime) / Simulation.Tuning.simulationTickSeconds);
        public event Action<ArkEvent> GameplayEvent;
        public event Action MatchRestarted;
        readonly Dictionary<int, ArenaBall> views = new Dictionary<int, ArenaBall>();
        Func<ArkBallModel, ArenaBall> ballFactory;
        ArkenoidEnvironment environment;
        public IReadOnlyDictionary<int, ArenaBall> BallViews => views;

        public void Configure(List<PlayerSlot> configuredSlots, ArkenoidSimulation simulation,
            Func<ArkBallModel, ArenaBall> createBall, ArkenoidEnvironment arenaEnvironment)
        {
            slots.Clear(); slots.AddRange(configuredSlots);
            Simulation = simulation; ballFactory = createBall; environment = arenaEnvironment;
            presentationTime = Time.time;
            foreach (PlayerSlot slot in slots)
            {
                if (!slot.hero && slot.paddle) slot.hero = slot.paddle.GetComponent<ArkenoidHeroController>();
                if (!slot.hero) throw new InvalidOperationException("Missing Arkenoid defender for slot " + slot.slotId);
                slot.hero.Configure(simulation, simulation.Hero(slot.slotId), slot.inputIndex, this);
            }
            environment.Configure(simulation); environment.Build();
            SyncViews(0);
        }
        public void GoalConceded(ArenaSide side, ArenaBall scoredBall)
        {
            if (Simulation == null || Paused || !scoredBall) return;
            // The simulation rejects duplicate, held and prematurely reported goals.
            Simulation.TryScore(side, scoredBall.Model);
        }
        void FixedUpdate()
        {
            if (Simulation == null || Paused) return;
            foreach (PlayerSlot slot in slots) slot.hero.SubmitInput();
            Simulation.Step(Time.fixedDeltaTime);
            presentationTime = Time.time;
            SyncViews(Time.fixedDeltaTime);
            environment.TickEnvironment(Time.fixedDeltaTime);
            foreach (ArkEvent ev in Simulation.Events) GameplayEvent?.Invoke(ev);
        }
        void SyncViews(float seconds)
        {
            foreach (PlayerSlot slot in slots)
            {
                slot.lives = Simulation.Hero(slot.slotId).Lives;
                slot.hero.Motor.SyncView();
                if (slot.paddle) slot.paddle.eliminated = slot.lives <= 0;
            }
            var expired = new List<int>();
            foreach (var item in views) if (Simulation.Ball(item.Key) == null) expired.Add(item.Key);
            foreach (int id in expired) { if (views[id]) Destroy(views[id].gameObject); views.Remove(id); }
            foreach (ArkBallModel model in Simulation.Balls)
            {
                if (!views.TryGetValue(model.Id, out ArenaBall view))
                { view = ballFactory(model); views.Add(model.Id, view); if (!ball) ball = view; }
                view.SyncView(seconds);
            }
        }
        public void SetPaused(bool value)
        {
            Paused = value;
            if (value) Simulation?.ClearInputs();
            foreach (PlayerSlot slot in slots) slot.hero.AllowInput(!value);
        }
        public void SetMenuOpen(bool value)
        {
            if (value == MenuOpen) return;
            MenuOpen = value; menuChangedFrame = Time.frameCount;
            if (value) { pausedBeforeMenu = Paused; SetPaused(true); }
            else SetPaused(pausedBeforeMenu);
        }
        public void RestartMatch()
        {
            if (Simulation == null) return;
            Simulation.ResetMatch(); environment.Restart();
            MatchRestarted?.Invoke(); presentationTime = Time.time;
            foreach (PlayerSlot slot in slots) slot.hero.ResetController();
            SetPaused(false); SyncViews(0);
        }
        void Update()
        {
            if (Simulation == null || MenuOpen || menuChangedFrame == Time.frameCount) return;
            if (Input.GetKeyDown(KeyCode.R)) RestartMatch();
            if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(!Paused);
        }
    }
}
