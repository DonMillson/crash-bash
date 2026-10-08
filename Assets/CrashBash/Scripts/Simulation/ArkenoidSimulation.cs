using System;
using System.Collections.Generic;

namespace CrashBashRemake
{
    /// <summary>
    /// Shared Arkenoid gameplay. Unity supplies inputs and renders models; it does
    /// not run a second PhysX ruleset. A 30Hz tick and scoped Dingodile motion are
    /// measured; other numerical behavior remains provisional.
    /// </summary>
    public sealed class ArkenoidSimulation
    {
        readonly List<ArkHeroModel> heroes = new List<ArkHeroModel>();
        readonly List<ArkBallModel> balls = new List<ArkBallModel>();
        readonly List<ArkEvent> events = new List<ArkEvent>();
        readonly float[] pickupCooldown = new float[4];
        Random random;
        int nextBallId, nextCorner;
        float spawnTime, warningTime;
        double accumulatedSeconds;
        public readonly ArkenoidTuning Tuning;
        public readonly ArkenoidArenaGeometry Geometry;
        public readonly IArkenoidRules Rules;
        public IReadOnlyList<ArkHeroModel> Heroes => heroes;
        public IReadOnlyList<ArkBallModel> Balls => balls;
        public IReadOnlyList<ArkEvent> Events => events;
        public ArkenoidMatchPhase Phase { get; private set; }
        public float PhaseTime { get; private set; }
        public float ElapsedTime { get; private set; }
        public int RoundWinnerSlot { get; private set; } = -1;
        public int MatchWinnerSlot { get; private set; } = -1;
        public int LaunchWarningCorner { get; private set; } = -1;
        public int RoundNumber { get; private set; }
        public int TickNumber { get; private set; }
        public float InterpolationAlpha => (float)(accumulatedSeconds / Tuning.simulationTickSeconds);

        public ArkenoidSimulation(ArkenoidTuning tuning, IArkenoidRules rules, IEnumerable<ArkPlayerSetup> players)
        {
            Tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Geometry = new ArkenoidArenaGeometry(tuning);
            var ids = new HashSet<int>();
            var sides = new HashSet<ArenaSide>();
            foreach (ArkPlayerSetup player in players)
            {
                if (player.SlotId < 0 || player.SlotId >= 4 || !ids.Add(player.SlotId) ||
                    (int)player.Side < 0 || (int)player.Side >= 4 || !sides.Add(player.Side) ||
                    (int)player.Character < 0 || (int)player.Character >= 8)
                    throw new ArgumentException("Four unique slots and sides, with valid independent character IDs, are required.");
                heroes.Add(new ArkHeroModel { SlotId = player.SlotId, Side = player.Side,
                    Character = player.Character, Human = player.Human });
            }
            if (heroes.Count != 4) throw new ArgumentException("Ballistix requires four defenders.");
            ResetMatch();
        }

        public ArkHeroModel Hero(int slot) => heroes.Find(h => h.SlotId == slot);
        public ArkBallModel Ball(int id) => balls.Find(b => b.Id == id);
        public void SetInput(int slot, ArkInput input)
        {
            ArkHeroModel hero = Hero(slot);
            if (!ArkMath.Finite(input.Axis)) throw new ArgumentException("Input axis must be finite.");
            if (hero != null)
            {
                input.Axis = ArkMath.Clamp(input.Axis, -1, 1);
                // Host FixedUpdate can run more frequently than the original 30Hz tick.
                // Preserve button edges until a simulation tick actually consumes them.
                input.KickPressed |= hero.Input.KickPressed;
                input.RepulsePressed |= hero.Input.RepulsePressed;
                input.TauntPressed |= hero.Input.TauntPressed;
                hero.Input = input;
            }
        }
        public void ClearInputs() { foreach (ArkHeroModel hero in heroes) hero.Input = new ArkInput(); }
        public void ResetMatch()
        {
            random = new Random(Tuning.seed);
            nextCorner = random.Next(4);
            events.Clear(); RoundNumber = 0; MatchWinnerSlot = -1; ElapsedTime = 0;
            accumulatedSeconds = 0; TickNumber = 0;
            foreach (ArkHeroModel hero in heroes) hero.Wins = 0;
            RestartRound();
        }
        void RestartRound()
        {
            balls.Clear();
            RoundNumber++;
            foreach (ArkHeroModel hero in heroes)
            {
                hero.Lives = Rules.StartingScore;
                hero.Lateral = hero.PreviousLateral = hero.Velocity = hero.MotorVelocity = 0;
                hero.ActionCooldown = hero.ActionTime = hero.BotThinkTime = hero.BotTarget = 0;
                hero.RepulseCharges = 0; hero.GrabbedBallId = -1; hero.Input = new ArkInput();
                hero.State = ArkenoidHeroState.Idle; hero.StateTime = 0;
            }
            Array.Clear(pickupCooldown, 0, pickupCooldown.Length);
            Phase = ArkenoidMatchPhase.Countdown; PhaseTime = Tuning.countdownSeconds;
            RoundWinnerSlot = -1; spawnTime = Tuning.spawnInterval; warningTime = 0;
            LaunchWarningCorner = nextCorner;
        }

        public void Step(float seconds)
        {
            if (!ArkMath.Finite(seconds) || seconds <= 0 || seconds > .25f)
                throw new ArgumentOutOfRangeException(nameof(seconds), "Use a positive fixed step of at most .25 seconds.");
            events.Clear(); accumulatedSeconds += seconds;
            double tick = Tuning.simulationTickSeconds;
            while (accumulatedSeconds + .0000001 >= tick)
            {
                accumulatedSeconds = Math.Max(0, accumulatedSeconds - tick);
                Tick(Tuning.simulationTickSeconds);
            }
        }
        void Tick(float seconds)
        {
            TickNumber++; ElapsedTime += seconds;
            UpdatePresentationStates(seconds);
            if (Phase != ArkenoidMatchPhase.Playing)
            {
                PhaseTime = Math.Max(0, PhaseTime - seconds);
                if (Phase == ArkenoidMatchPhase.Countdown && PhaseTime <= 0)
                {
                    Phase = ArkenoidMatchPhase.Playing;
                    Launch(AddBall(new ArkVector(), new ArkVector(), true), nextCorner);
                    LaunchWarningCorner = -1; nextCorner = (nextCorner + 1) % 4;
                }
                else if (Phase == ArkenoidMatchPhase.RoundResult && PhaseTime <= 0) RestartRound();
                ClearPresses();
                return;
            }

            foreach (ArkBallModel ball in balls) ball.PreviousPosition = ball.Position;
            foreach (ArkHeroModel hero in heroes) MoveHero(hero, seconds);
            foreach (ArkHeroModel hero in heroes) HandleActions(hero, seconds);
            for (int i = 0; i < balls.Count && Phase == ArkenoidMatchPhase.Playing; i++) UpdateBall(balls[i], seconds);
            if (Phase == ArkenoidMatchPhase.Playing) UpdateLaunchers(seconds);
            ClearPresses();
        }

        void ClearPresses()
        {
            foreach (ArkHeroModel hero in heroes)
            { hero.Input.KickPressed = hero.Input.RepulsePressed = hero.Input.TauntPressed = false; }
        }
        void UpdatePresentationStates(float dt)
        {
            foreach (ArkHeroModel hero in heroes)
            {
                hero.StateTime += dt;
                hero.ActionCooldown = Math.Max(0, hero.ActionCooldown - dt);
                hero.ActionTime = Math.Max(0, hero.ActionTime - dt);
                if (hero.IsEliminated)
                {
                    if (hero.State == ArkenoidHeroState.Lose && hero.StateTime >= .2f) SetState(hero, ArkenoidHeroState.Die);
                    if (hero.State == ArkenoidHeroState.Die && hero.StateTime >= Tuning.deathAnimationSeconds) SetState(hero, ArkenoidHeroState.Dead);
                }
            }
        }
        void SetState(ArkHeroModel hero, ArkenoidHeroState state)
        {
            if (hero.State == state) return;
            hero.State = state; hero.StateTime = 0;
        }
        void MoveHero(ArkHeroModel hero, float dt)
        {
            hero.PreviousLateral = hero.Lateral;
            if (hero.IsEliminated) { hero.Velocity = hero.MotorVelocity = 0; return; }
            if (!hero.Human) hero.Input = ThinkBot(hero, dt);
            float axis = hero.Input.Axis;
            if (hero.GrabbedBallId >= 0 || (hero.State == ArkenoidHeroState.Taunt && hero.ActionTime > 0)) axis = 0;
            ArkMotionSettings motion = Tuning.MotionFor(hero.Character);
            float targetSpeed = axis * (hero.Input.Boost ? motion.sprintSpeed : motion.speed);
            float ramp = axis == 0 ? motion.deceleration : hero.Input.Boost ? motion.sprintAcceleration : motion.acceleration;
            hero.MotorVelocity = ArkMath.MoveTowards(hero.MotorVelocity, targetSpeed, ramp * dt);
            float desired = hero.Lateral + hero.MotorVelocity * dt;
            hero.Lateral = Rules.ClampHero(desired, Geometry);
            hero.Velocity = (hero.Lateral - hero.PreviousLateral) / dt;
            if (hero.Lateral != desired) hero.MotorVelocity = 0;
            if (hero.ActionTime <= 0 && hero.GrabbedBallId < 0)
                SetState(hero, Math.Abs(hero.Velocity) > .05f ? ArkenoidHeroState.Move : ArkenoidHeroState.Idle);
            if (Tuning.allowCornerPickupsForCalibration)
            {
                int side = (int)hero.Side;
                pickupCooldown[side] = Math.Max(0, pickupCooldown[side] - dt);
                if (pickupCooldown[side] <= 0 && hero.RepulseCharges == 0 && Math.Abs(hero.Lateral) >= Tuning.defenderTravel - .12f)
                { hero.RepulseCharges = 1; pickupCooldown[side] = 8; events.Add(new ArkEvent(ArkEventKind.Pickup, HeroPosition(hero), hero.SlotId)); }
            }
        }
        ArkVector HeroPosition(ArkHeroModel hero) => Geometry.HeroPosition(hero.Side, hero.Lateral);
        bool Influencable(ArkHeroModel hero, ArkBallModel ball, float radius)
        {
            ArkVector relative = ball.Position - HeroPosition(hero);
            return ball.Active && ball.GrabOwnerSlot == -1 && relative.LengthSquared <= radius * radius &&
                   ArkVector.Dot(relative, ArkenoidArenaGeometry.Inward(hero.Side)) >= -.05f;
        }
        void HandleActions(ArkHeroModel hero, float dt)
        {
            if (hero.IsEliminated) return;
            ArkVector inward = ArkenoidArenaGeometry.Inward(hero.Side);
            ArkVector anchor = HeroPosition(hero) + inward * Tuning.grabDistance;
            if (hero.GrabbedBallId >= 0)
            {
                ArkBallModel held = Ball(hero.GrabbedBallId);
                if (held == null || held.GrabOwnerSlot != hero.SlotId) { hero.GrabbedBallId = -1; return; }
                held.Position = anchor;
                if (!hero.Input.AttractHeld) FreeGrabbedObject(hero, true);
                return;
            }
            if (Tuning.allowAttractForCalibration && hero.Input.AttractHeld && hero.ActionCooldown <= 0)
            {
                if (hero.State != ArkenoidHeroState.Grab) events.Add(new ArkEvent(ArkEventKind.Attract, HeroPosition(hero), hero.SlotId));
                SetState(hero, ArkenoidHeroState.Grab); hero.ActionTime = dt * 2;
                foreach (ArkBallModel ball in balls)
                {
                    if (!Influencable(hero, ball, Tuning.attractionRadius)) continue;
                    ArkVector pull = anchor - ball.Position;
                    if (pull.Length < Tuning.ballRadius + .16f)
                    {
                        ball.GrabOwnerSlot = hero.SlotId; ball.LastTouchSlot = hero.SlotId;
                        hero.GrabbedBallId = ball.Id; ball.Velocity = new ArkVector(); ball.Position = ball.PreviousPosition = anchor;
                        events.Add(new ArkEvent(ArkEventKind.Grab, anchor, hero.SlotId, ball.Id)); break;
                    }
                    ball.Velocity = Limit(ball.Velocity + pull.Normalized * Tuning.attractionAcceleration * dt);
                }
                return;
            }
            if (hero.ActionCooldown > 0) return;
            if (hero.Input.RepulsePressed && hero.RepulseCharges > 0)
            {
                hero.RepulseCharges--;
                // Provisional animation binding; RedKick semantics are not established by declarations.
                SetState(hero, ArkenoidHeroState.RedKick);
                hero.ActionTime = Tuning.kickAnimationSeconds; hero.ActionCooldown = Tuning.kickCooldown;
                foreach (ArkBallModel ball in balls)
                    if (Influencable(hero, ball, Tuning.repulseRadius))
                        KickBall(ball, (ball.Position - HeroPosition(hero)).Normalized, Tuning.repulseMultiplier, hero.SlotId);
                events.Add(new ArkEvent(ArkEventKind.Repulse, HeroPosition(hero), hero.SlotId));
            }
            else if (hero.Input.KickPressed)
            {
                SetState(hero, ArkenoidHeroState.Kick);
                hero.ActionTime = Tuning.kickAnimationSeconds; hero.ActionCooldown = Tuning.kickCooldown;
                foreach (ArkBallModel ball in balls)
                    if (Influencable(hero, ball, Tuning.kickRadius))
                    {
                        float offset = Geometry.Lateral(hero.Side, ball.Position - HeroPosition(hero));
                        ArkVector shot = inward + ArkenoidArenaGeometry.Tangent(hero.Side) * (offset / Tuning.kickRadius * .5f);
                        KickBall(ball, shot, Tuning.kickMultiplier, hero.SlotId);
                    }
                events.Add(new ArkEvent(ArkEventKind.Kick, HeroPosition(hero), hero.SlotId));
            }
            else if (hero.Input.TauntPressed)
            { SetState(hero, ArkenoidHeroState.Taunt); hero.ActionTime = Tuning.tauntSeconds; }
        }

        public void KickBall(ArkBallModel ball, ArkVector direction, float multiplier, int slot = -1)
        {
            if (ball == null || !ball.Active || ball.GrabOwnerSlot != -1) return;
            float speed = Math.Min(Tuning.maxBallSpeed, Math.Max(Tuning.launchSpeed, ball.Velocity.Length) * multiplier);
            ball.Velocity = direction.Normalized * speed; ball.LastTouchSlot = slot;
        }
        void FreeGrabbedObject(ArkHeroModel hero, bool fire)
        {
            ArkBallModel ball = Ball(hero.GrabbedBallId); hero.GrabbedBallId = -1;
            if (ball == null) return;
            ball.GrabOwnerSlot = -1;
            ArkVector direction = ArkenoidArenaGeometry.Inward(hero.Side);
            ArkVector launch = HeroPosition(hero) + direction * Tuning.grabDistance;
            ball.Position = ball.PreviousPosition = Rules.TestLaunchBounds(launch, Geometry) ? launch : new ArkVector();
            ball.Velocity = direction * (fire ? Tuning.grabReleaseSpeed : Tuning.launchSpeed);
            ball.ContactImmunity = .08f; ball.LastCollisionSlot = hero.SlotId;
            if (fire)
            {
                SetState(hero, ArkenoidHeroState.Kick); hero.ActionTime = Tuning.kickAnimationSeconds;
                hero.ActionCooldown = Tuning.kickCooldown;
                events.Add(new ArkEvent(ArkEventKind.Release, ball.Position, hero.SlotId, ball.Id));
            }
        }

        public ArkBallModel AddBall(ArkVector position, ArkVector velocity, bool scores = true)
        {
            if (!ArkMath.Finite(position.X) || !ArkMath.Finite(position.Y) || !ArkMath.Finite(velocity.X) || !ArkMath.Finite(velocity.Y))
                throw new ArgumentException("Ball data must be finite.");
            var ball = new ArkBallModel { Id = nextBallId++, Position = position, PreviousPosition = position, Velocity = Limit(velocity), Scores = scores };
            balls.Add(ball); return ball;
        }
        ArkVector Limit(ArkVector velocity) => velocity.LengthSquared > Tuning.maxBallSpeed * Tuning.maxBallSpeed
            ? velocity.Normalized * Tuning.maxBallSpeed : velocity;
        void Launch(ArkBallModel ball, int corner)
        {
            ArkVector origin = Rules.LaunchPosition(corner, Geometry);
            if (!Rules.TestLaunchBounds(origin, Geometry)) origin = new ArkVector();
            ArkVector target = new ArkVector((float)random.NextDouble() * 3 - 1.5f, (float)random.NextDouble() * 3 - 1.5f);
            ball.Position = ball.PreviousPosition = origin; ball.Velocity = (target - origin).Normalized * Tuning.launchSpeed;
            ball.Active = true; ball.GrabOwnerSlot = -1; ball.LastTouchSlot = -1;
            ball.RespawnTime = ball.ContactImmunity = 0; ball.LastCollisionSlot = -1;
            events.Add(new ArkEvent(ArkEventKind.BallLaunched, origin, ball: ball.Id, corner: corner));
        }
        void UpdateLaunchers(float dt)
        {
            if (warningTime > 0)
            {
                warningTime -= dt;
                if (warningTime <= 0)
                {
                    Launch(AddBall(new ArkVector(), new ArkVector()), LaunchWarningCorner);
                    LaunchWarningCorner = -1; nextCorner = (nextCorner + 1) % 4;
                    spawnTime = Tuning.spawnInterval;
                }
            }
            else if (balls.Count < Tuning.maxBalls)
            {
                spawnTime -= dt;
                if (spawnTime <= 0)
                {
                    LaunchWarningCorner = nextCorner; warningTime = Math.Max(.0001f, Tuning.launchWarningSeconds);
                    events.Add(new ArkEvent(ArkEventKind.LaunchWarning, Geometry.CornerPosition(nextCorner), corner: nextCorner));
                }
            }
        }

        void UpdateBall(ArkBallModel ball, float dt)
        {
            if (!ball.Active)
            {
                ball.RespawnTime -= dt;
                if (ball.RespawnTime <= 0) { Launch(ball, nextCorner); nextCorner = (nextCorner + 1) % 4; }
                return;
            }
            if (ball.GrabOwnerSlot != -1) return;
            ball.ContactImmunity = Math.Max(0, ball.ContactImmunity - dt);
            float remaining = dt;
            // Swept contacts avoid tunnelling at kick speed and handle more than one bounce in a tick.
            for (int bounce = 0; bounce < 8 && remaining > .00001f; bounce++)
            {
                ArkVector delta = ball.Velocity * remaining;
                float best = 1.0001f; ArkVector hitNormal = new ArkVector();
                ArkHeroModel hitHero = null;
                foreach (ArkWallSegment wall in Geometry.Walls(heroes))
                    if (Rules.SweepBoundary(ball.Position, delta, wall, Geometry, out float t, out ArkVector n) && t < best)
                    { best = t; hitNormal = n; hitHero = null; }
                foreach (ArkHeroModel hero in heroes)
                {
                    if (hero.IsEliminated || (ball.ContactImmunity > 0 && ball.LastCollisionSlot == hero.SlotId)) continue;
                    ArkVector tangent = ArkenoidArenaGeometry.Tangent(hero.Side), inward = ArkenoidArenaGeometry.Inward(hero.Side);
                    ArkVector heroMotion = tangent * hero.Velocity * remaining;
                    ArkVector relative = ball.Position - (HeroPosition(hero) - heroMotion);
                    ArkVector local = new ArkVector(ArkVector.Dot(relative, tangent), ArkVector.Dot(relative, inward));
                    ArkVector motion = new ArkVector(ArkVector.Dot(delta - heroMotion, tangent), ArkVector.Dot(delta - heroMotion, inward));
                    if (ArkenoidArenaGeometry.SweepBox(local, motion, Tuning.defenderHalfWidth + Tuning.ballRadius,
                        Tuning.defenderHalfDepth + Tuning.ballRadius, out float t, out ArkVector n) && t < best)
                    { best = t; hitNormal = tangent * n.X + inward * n.Y; hitHero = hero; }
                }
                // Goals are planes beyond the wall gap, rather than frame-dependent trigger events.
                ArenaSide? goal = null;
                foreach (ArkHeroModel hero in heroes)
                {
                    if (hero.IsEliminated) continue;
                    float outward = Geometry.OutwardDistance(hero.Side, ball.Position);
                    float travel = ArkVector.Dot(delta, -ArkenoidArenaGeometry.Inward(hero.Side));
                    if (travel <= .000001f) continue;
                    float t = (Tuning.goalPlane - outward) / travel;
                    if (t >= 0 && t <= 1 && t < best && Math.Abs(Geometry.Lateral(hero.Side, ball.Position + delta * t)) < Tuning.goalHalfWidth)
                    { best = t; goal = hero.Side; }
                }
                if (best > 1) { ball.Position += delta; break; }
                ball.Position += delta * best;
                if (goal.HasValue) { HandleGoal(goal.Value, ball); return; }
                if (hitHero != null)
                {
                    ArkVector inward = ArkenoidArenaGeometry.Inward(hitHero.Side);
                    float front = ArkVector.Dot(hitNormal, inward);
                    if (front > .5f)
                    {
                        ArkVector contactCentre = HeroPosition(hitHero) - ArkenoidArenaGeometry.Tangent(hitHero.Side) * (hitHero.Velocity * remaining * (1 - best));
                        float offset = Geometry.Lateral(hitHero.Side, ball.Position - contactCentre) / (Tuning.defenderHalfWidth + Tuning.ballRadius);
                        float influence = offset * Tuning.contactOffsetInfluence + hitHero.Velocity * Tuning.contactMovementInfluence;
                        ball.Velocity = (inward + ArkenoidArenaGeometry.Tangent(hitHero.Side) * influence).Normalized * ball.Velocity.Length;
                    }
                    else ball.Velocity = ArkVector.Reflect(ball.Velocity, hitNormal);
                    ball.LastTouchSlot = ball.LastCollisionSlot = hitHero.SlotId; ball.ContactImmunity = .04f;
                    events.Add(new ArkEvent(ArkEventKind.Deflect, ball.Position, hitHero.SlotId, ball.Id));
                }
                else ball.Velocity = ArkVector.Reflect(ball.Velocity, hitNormal);
                ball.Position += hitNormal * .001f;
                remaining *= 1 - best;
            }
            if (Rules.TestOutOfBounds(ball.Position, Geometry))
            { ball.Active = false; ball.RespawnTime = Tuning.scoredBallDelay; }
        }

        public bool TryScore(ArenaSide side, ArkBallModel ball)
        {
            if (ball == null || Geometry.OutwardDistance(side, ball.Position) < Tuning.goalPlane - .001f) return false;
            return HandleGoal(side, ball);
        }
        bool HandleGoal(ArenaSide side, ArkBallModel ball)
        {
            ArkHeroModel hero = heroes.Find(h => h.Side == side);
            if (Phase != ArkenoidMatchPhase.Playing || hero == null || hero.IsEliminated || !ball.Active || ball.GrabOwnerSlot != -1) return false;
            ball.Active = false; ball.Velocity = new ArkVector(); ball.RespawnTime = Tuning.scoredBallDelay;
            if (!ball.Scores) return true;
            hero.Lives--;
            events.Add(new ArkEvent(ArkEventKind.Goal, ball.Position, hero.SlotId, ball.Id));
            if (hero.IsEliminated)
            {
                if (hero.GrabbedBallId >= 0) FreeGrabbedObject(hero, false);
                hero.Velocity = hero.MotorVelocity = 0; SetState(hero, ArkenoidHeroState.Lose);
                events.Add(new ArkEvent(ArkEventKind.Eliminated, HeroPosition(hero), hero.SlotId));
            }
            int alive = 0; ArkHeroModel winner = null;
            foreach (ArkHeroModel h in heroes) if (!h.IsEliminated) { alive++; winner = h; }
            if (alive == 1) FinishRound(winner);
            return true;
        }
        void FinishRound(ArkHeroModel winner)
        {
            winner.Wins++; RoundWinnerSlot = winner.SlotId;
            SetState(winner, ArkenoidHeroState.Winner);
            foreach (ArkHeroModel hero in heroes)
            {
                hero.Velocity = hero.MotorVelocity = 0; hero.PreviousLateral = hero.Lateral;
                if (hero.GrabbedBallId >= 0) FreeGrabbedObject(hero, false);
            }
            Phase = ArkenoidMatchPhase.RoundResult; PhaseTime = Tuning.roundResultSeconds;
            LaunchWarningCorner = -1; warningTime = 0;
            events.Add(new ArkEvent(ArkEventKind.RoundWon, HeroPosition(winner), winner.SlotId));
            if (winner.Wins >= Tuning.winsNeeded)
            {
                Phase = ArkenoidMatchPhase.MatchResult; MatchWinnerSlot = winner.SlotId;
                events.Add(new ArkEvent(ArkEventKind.MatchWon, HeroPosition(winner), winner.SlotId));
            }
        }

        /// <summary>Temporary DangerBalls-style heuristic. It is not recovered Ark_HeroBot code.</summary>
        ArkInput ThinkBot(ArkHeroModel hero, float dt)
        {
            hero.BotThinkTime -= dt;
            float dangerTime = float.MaxValue; ArkBallModel danger = null;
            if (hero.BotThinkTime <= 0)
            {
                hero.BotTarget = 0;
                foreach (ArkBallModel ball in balls)
                    if (PredictDanger(hero, ball, out float time, out float target) && time < dangerTime)
                    { dangerTime = time; hero.BotTarget = Rules.ClampHero(target, Geometry); }
                hero.BotThinkTime = Tuning.botReactionSeconds + hero.SlotId * .015f;
            }
            foreach (ArkBallModel ball in balls)
                if (PredictDanger(hero, ball, out float time, out float _) && time < dangerTime)
                { dangerTime = time; danger = ball; }
            float difference = hero.BotTarget - hero.Lateral;
            ArkMotionSettings motion = Tuning.MotionFor(hero.Character);
            bool boost = dangerTime < Math.Abs(difference) / motion.speed + .15f;
            float speed = boost ? motion.sprintSpeed : motion.speed;
            // Brake before the target instead of oscillating around it with inertia.
            float stoppingDistance = hero.MotorVelocity * hero.MotorVelocity / (2 * motion.deceleration);
            float axis = Math.Abs(difference) < .035f ||
                (Math.Sign(difference) == Math.Sign(hero.MotorVelocity) && Math.Abs(difference) <= stoppingDistance) ? 0
                : ArkMath.Clamp(difference / (speed * Math.Max(dt, .0001f)), -1, 1);
            return new ArkInput {
                Axis = axis,
                Boost = boost,
                KickPressed = danger != null && dangerTime < .19f && Influencable(hero, danger, Tuning.kickRadius),
                RepulsePressed = hero.RepulseCharges > 0 && danger != null && dangerTime < .12f,
            };
        }
        bool PredictDanger(ArkHeroModel hero, ArkBallModel ball, out float time, out float target)
        {
            time = 0; target = 0;
            if (!ball.Active || ball.GrabOwnerSlot != -1) return false;
            float speed = ArkVector.Dot(ball.Velocity, -ArkenoidArenaGeometry.Inward(hero.Side));
            if (speed <= .001f) return false;
            float contactLine = Tuning.defenderLine - Tuning.defenderHalfDepth - Tuning.ballRadius;
            time = (contactLine - Geometry.OutwardDistance(hero.Side, ball.Position)) / speed;
            if (time < 0 || time > 3) return false;
            ArkVector prediction = ball.Position + ball.Velocity * time;
            target = Geometry.Lateral(hero.Side, prediction);
            float edge = Tuning.wallHalfExtent - Tuning.ballRadius;
            // Fold through solid wall bounces; other-player contacts remain unpredictable.
            for (int i = 0; i < 8 && Math.Abs(target) > edge; i++)
                target = target > edge ? 2 * edge - target : -2 * edge - target;
            return Math.Abs(target) <= Tuning.goalHalfWidth + Tuning.defenderHalfWidth;
        }
    }
}
