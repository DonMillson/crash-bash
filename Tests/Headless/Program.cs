using System;
using CrashBashRemake;

static class Program
{
    static int assertions;
    static void Check(bool pass, string description)
    {
        assertions++;
        if (!pass) throw new Exception(description);
    }
    static ArkPlayerSetup[] Roster(bool bots = false) => new[] {
        new ArkPlayerSetup(2, ArenaSide.Bottom, CharacterId.RillaRoo, !bots),
        new ArkPlayerSetup(0, ArenaSide.Right, CharacterId.Coco, !bots),
        new ArkPlayerSetup(3, ArenaSide.Top, CharacterId.NBrio, !bots),
        new ArkPlayerSetup(1, ArenaSide.Left, CharacterId.Tiny, !bots),
    };
    static ArkenoidSimulation Start(ArkenoidTuning tuning = null, bool bots = false, ArkenoidVariant variant = ArkenoidVariant.BA)
    {
        tuning = tuning ?? new ArkenoidTuning();
        var sim = new ArkenoidSimulation(tuning, ArkenoidRulesFactory.Create(variant, tuning.startingScore), Roster(bots));
        for (int i = 0; i < 110; i++) sim.Step(.02f);
        return sim;
    }
    static void Step(ArkenoidSimulation sim, int count) { for (int i = 0; i < count; i++) sim.Step(.02f); }
    static void MeasuredMotion()
    {
        // Numeric observations from the actual user-supplied PS1 image, not values
        // generated from the implementation under test. Only Dingodile is measured.
        using var evidence = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText("Docs/PS1_Arkenoid_Runtime_Measurements.json"));
        foreach (var trace in evidence.RootElement.GetProperty("motion_traces").EnumerateArray())
        {
            var tuning = new ArkenoidTuning { countdownSeconds = 0 };
            var roster = Roster(); roster[0].Character = CharacterId.Dingodile;
            var sim = new ArkenoidSimulation(tuning, ArkenoidRulesFactory.Create(ArkenoidVariant.BA, 15), roster);
            sim.Step(tuning.simulationTickSeconds);
            foreach (var segment in trace.GetProperty("segments").EnumerateArray())
            {
                sim.SetInput(2, new ArkInput { Axis = segment.GetProperty("axis").GetSingle(), Boost = segment.GetProperty("sprint").GetBoolean() });
                foreach (var originalX in segment.GetProperty("x_per_logic_tick").EnumerateArray())
                {
                    sim.Step(tuning.simulationTickSeconds);
                    Check(Math.Abs(sim.Hero(2).Lateral * 400 - originalX.GetInt32()) < 1.1f,
                        "PS1 motion trace mismatch: " + trace.GetProperty("name").GetString());
                }
            }
        }
        var clock = Start(); int before = clock.TickNumber;
        clock.SetInput(2, new ArkInput { KickPressed = true }); clock.Step(.01f);
        Check(clock.TickNumber == before, "sub-tick advanced original simulation clock");
        clock.SetInput(2, new ArkInput()); clock.Step(.024f);
        Check(clock.Hero(2).State == ArkenoidHeroState.Kick, "button edge was lost between 50Hz host and 30Hz simulation");
        var schedule = Start(new ArkenoidTuning { kickCooldown = 0 });
        schedule.SetInput(2, new ArkInput { KickPressed = true }); schedule.Step(.2f);
        int kicks = 0; foreach (ArkEvent ev in schedule.Events) if(ev.Kind == ArkEventKind.Kick && ev.SlotId == 2) kicks++;
        Check(kicks == 1, "button edge executed more than once in accumulated ticks");
        var a = Start(); var b = Start();
        a.SetInput(2, new ArkInput { Axis = 1 }); b.SetInput(2, new ArkInput { Axis = 1 });
        for(int i=0;i<20;i++)a.Step(.01f);
        for(int i=0;i<6;i++)b.Step(b.Tuning.simulationTickSeconds);
        Check(a.TickNumber == b.TickNumber && Math.Abs(a.Hero(2).Lateral-b.Hero(2).Lateral)<.0001f,
            "movement depended on host update frequency");
    }
    static ArkenoidSimulation ReferenceScenario()
    {
        var tuning = ArkenoidTuning.CrashballReference();
        tuning.countdownSeconds = 0; tuning.maxBallSpeed = 30;
        var roster = Roster(); roster[0].Character = CharacterId.Dingodile;
        var sim = new ArkenoidSimulation(tuning, new BAArkenoidRules(), roster);
        sim.Step(tuning.simulationTickSeconds);
        return sim;
    }
    static void MeasuredBalls()
    {
        using var evidence = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText("Docs/PS1_Arkenoid_Runtime_Measurements.json"));
        var reference = evidence.RootElement.GetProperty("ball_reference");
        var arc = ReferenceScenario(); var launched = arc.Balls[0];
        Check(Math.Abs(launched.Position.X * 400) == 2048 && Math.Abs(launched.Position.Y * 400) == 2048,
            "PS1 corner launcher coordinates");
        int sample = 0;
        foreach (var point in reference.GetProperty("launch_arc_trace").GetProperty("xyz_per_logic_tick_after_fire").EnumerateArray())
        {
            arc.Step(arc.Tuning.simulationTickSeconds);
            Check(Math.Abs(launched.Height * 400 + point[1].GetInt32()) < .01f,
                "PS1 launch/bounce height mismatch at sample " + sample++);
        }
        foreach (var probe in reference.GetProperty("kick_current_speed_probes").EnumerateArray())
        {
            var sim = ReferenceScenario(); sim.Balls[0].Active = false; sim.Balls[0].RespawnTime = 100;
            var hero = sim.Hero(2); hero.Lateral = hero.PreviousLateral = 1201 / 400f;
            float originalSpeed = probe.GetProperty("forced_incoming_speed").GetSingle();
            var ball = sim.AddBall(new ArkVector(440 / 400f, -1560 / 400f),
                new ArkVector(118, -83).Normalized * (originalSpeed * 30 / 400));
            ball.TargetSpeed = 144 * 30 / 400f;
            sim.SetInput(2, new ArkInput { KickPressed = true });
            // The committed fixture contains the selected odd video callbacks.
            // Each adjacent row is one measured 30Hz game update, not two updates.
            foreach (var point in probe.GetProperty("samples").EnumerateArray())
            {
                sim.Step(sim.Tuning.simulationTickSeconds);
                Check(Math.Abs(ball.Velocity.Length * 400 / 30 - point[1].GetInt32()) < .05f,
                    "PS1 kick current/cruise speed mismatch: incoming " + originalSpeed + ", callback " + point[0].GetInt32());
            }
        }
        foreach (var probe in reference.GetProperty("kick_static_radius_probes").EnumerateArray())
        {
            float distance = probe.GetProperty("forced_static_distance").GetSingle();
            if (distance == 300) continue; // Original passive-overlap geometry is unresolved.
            var sim = ReferenceScenario(); sim.Balls[0].Active = false; sim.Balls[0].RespawnTime = 100;
            var hero = sim.Hero(2); hero.Lateral = hero.PreviousLateral = 1201 / 400f;
            var ball = sim.AddBall(new ArkVector(1201 / 400f, (-2176 + distance) / 400f), new ArkVector());
            ball.TargetSpeed = 0;
            sim.SetInput(2, new ArkInput { KickPressed = true });
            foreach (var point in probe.GetProperty("samples").EnumerateArray())
            {
                sim.Step(sim.Tuning.simulationTickSeconds);
                Check(Math.Abs(ball.Velocity.Length * 400 / 30 - point[1].GetInt32()) < .05f,
                    "PS1 kick influence window mismatch: distance " + distance + ", callback " + point[0].GetInt32());
            }
        }
        foreach (var probe in reference.GetProperty("kick_fine_radius_probes").EnumerateArray())
        {
            float distance = probe.GetProperty("distance").GetSingle();
            var sim = ReferenceScenario(); sim.Balls[0].Active = false; sim.Balls[0].RespawnTime = 100;
            var hero = sim.Hero(2); hero.Lateral = hero.PreviousLateral = 1201 / 400f;
            var ball = sim.AddBall(new ArkVector(1201 / 400f, (-2176 + distance) / 400f), new ArkVector());
            ball.TargetSpeed = 0; sim.SetInput(2, new ArkInput { KickPressed = true });
            int firstHit = 0;
            for (int tick = 1; tick <= 6; tick++)
            {
                sim.Step(sim.Tuning.simulationTickSeconds);
                if (ball.LastTouchSlot == 2 && firstHit == 0) firstHit = tick;
            }
            var expected = probe.GetProperty("first_hit");
            int expectedTick = expected.ValueKind == System.Text.Json.JsonValueKind.Null ? 0 : (expected[0].GetInt32() + 1) / 2;
            Check(firstHit == expectedTick, "PS1 kick fine radius boundary: " + distance);
        }
        var containment = ReferenceScenario(); containment.Balls[0].Active = false; containment.Balls[0].RespawnTime = 100;
        containment.Hero(2).Lateral = containment.Hero(2).PreviousLateral = 1201 / 400f;
        var corner = containment.AddBall(new ArkVector(5.5f, -4.7f), new ArkVector());
        corner.TargetSpeed = 0; containment.SetInput(2, new ArkInput { KickPressed = true });
        for (int i = 0; i < 5; i++) containment.Step(containment.Tuning.simulationTickSeconds);
        Check(corner.Position.X <= containment.Tuning.wallHalfExtent - containment.Tuning.ballRadius + .002f && corner.Velocity.X < 0,
            "kick wave separation tunneled through corner wall");

        var delayed = Start(); var incoming = delayed.AddBall(new ArkVector(0, -3.1f), new ArkVector(0, -8));
        delayed.SetInput(2, new ArkInput { KickPressed = true }); delayed.Step(delayed.Tuning.simulationTickSeconds);
        Check(incoming.Velocity.Y < 0, "incoming ball was already in action range");
        for (int i = 0; i < 5; i++) delayed.Step(delayed.Tuning.simulationTickSeconds);
        Check(incoming.LastTouchSlot == 2 && incoming.Velocity.Y > 0, "kick action window ignored a later arriving ball");
        Check(incoming.Velocity.Length < 11, "one kick applied multiple impulses to the same ball");

        var soak = Start(ArkenoidTuning.CrashballReference(), true);
        int goals = 0, eliminations = 0, rounds = 0, peak = 0;
        for (int i = 0; i < 12000; i++)
        {
            soak.Step(.02f); peak = Math.Max(peak, soak.Balls.Count);
            foreach (ArkEvent ev in soak.Events)
            {
                if (ev.Kind == ArkEventKind.Goal) goals++;
                if (ev.Kind == ArkEventKind.Eliminated) eliminations++;
                if (ev.Kind == ArkEventKind.RoundWon) rounds++;
            }
            foreach (ArkBallModel ball in soak.Balls)
                Check(!float.IsNaN(ball.Position.X) && !float.IsNaN(ball.Position.Y) && !float.IsNaN(ball.Height) &&
                    ball.Height >= soak.Tuning.ballHeight - .0001f && ball.Velocity.Length <= soak.Tuning.maxBallSpeed + .01f,
                    "reference ball became invalid during bot game");
        }
        Check(goals > 20 && eliminations > 0 && rounds > 0 && peak > 1, "reference bot game did not exercise scoring, elimination and multiple balls");
        Console.WriteLine($"Reference bot game: {goals} goals, {eliminations} eliminations, {rounds} round wins, peak {peak} balls in 240 simulated seconds.");
    }
    static void Main(string[] args)
    {
        MeasuredMotion();
        MeasuredBalls();
        foreach (ArkCraftPart part in ArkenoidCraftRecipe.Create())
        {
            double volume = 0;
            var v = part.Mesh.Vertices; var triangles = part.Mesh.Triangles;
            for (int i=0;i<triangles.Count;i+=3)
            {
                Check(triangles[i]>=0 && triangles[i]<v.Count && triangles[i+1]>=0 && triangles[i+1]<v.Count && triangles[i+2]>=0 && triangles[i+2]<v.Count,"invalid craft mesh indices");
                ArkVertex a=v[triangles[i]], b=v[triangles[i+1]], c=v[triangles[i+2]];
                volume += (a.X*(b.Y*c.Z-b.Z*c.Y)+a.Y*(b.Z*c.X-b.X*c.Z)+a.Z*(b.X*c.Y-b.Y*c.X))/6.0;
            }
            Check(volume>0,"craft mesh has inward winding or no volume: "+part.Name);
        }
        if (args.Length==2 && args[0]=="--export")
            System.IO.File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(ArkenoidCraftRecipe.Create(),new System.Text.Json.JsonSerializerOptions{IncludeFields=true}));
        var sim = Start();
        Check(sim.Hero(2).Side == ArenaSide.Bottom && sim.Hero(2).Character == CharacterId.RillaRoo, "slot/side/character were coupled");
        sim.SetInput(2, new ArkInput { Axis = 1 }); Step(sim, 50);
        Check(Math.Abs(sim.Hero(2).Lateral - sim.Tuning.defenderTravel) < .001f, "hero travel bounds");
        var score = sim.AddBall(new ArkVector(0, -sim.Tuning.goalPlane), new ArkVector(0, -7));
        Check(sim.TryScore(ArenaSide.Bottom, score), "goal was not scored");
        Check(!sim.TryScore(ArenaSide.Bottom, score) && sim.Hero(2).Lives == 14, "duplicate goal deducted twice");
        var noScore = sim.AddBall(new ArkVector(0, -sim.Tuning.goalPlane), new ArkVector(0, -7), false);
        sim.TryScore(ArenaSide.Bottom, noScore);
        Check(sim.Hero(2).Lives == 14, "challenge ball scored");

        sim = Start(new ArkenoidTuning { startingScore = 1, winsNeeded = 1 });
        foreach (ArenaSide side in new[] { ArenaSide.Bottom, ArenaSide.Right, ArenaSide.Top })
        {
            var b = sim.AddBall(sim.Geometry.SidePoint(side, 0, sim.Tuning.goalPlane), -ArkenoidArenaGeometry.Inward(side) * 8);
            sim.TryScore(side, b);
        }
        Check(sim.Phase == ArkenoidMatchPhase.MatchResult && sim.MatchWinnerSlot == 1 && sim.Hero(1).Wins == 1, "match winner incorrect");
        Step(sim, 500);
        Check(sim.Phase == ArkenoidMatchPhase.MatchResult && sim.Hero(1).Wins == 1, "match result was erased automatically");
        Check(sim.Hero(2).State == ArkenoidHeroState.Dead && sim.Hero(1).State == ArkenoidHeroState.Winner, "terminal states not advanced");
        sim.ResetMatch();
        Check(sim.Balls.Count == 0 && sim.Hero(2).Lives == 1 && sim.Hero(1).Wins == 0 && sim.Hero(2).State == ArkenoidHeroState.Idle, "reset leaked round state");
        sim = Start(new ArkenoidTuning { startingScore = 1, winsNeeded = 3, countdownSeconds = 0, roundResultSeconds = .05f });
        for (int round = 1; round <= 3; round++)
        {
            foreach (ArenaSide side in new[] { ArenaSide.Bottom, ArenaSide.Right, ArenaSide.Top })
            {
                var b = sim.AddBall(sim.Geometry.SidePoint(side, 0, sim.Tuning.goalPlane), -ArkenoidArenaGeometry.Inward(side) * 8);
                sim.TryScore(side, b);
            }
            Check(sim.Hero(1).Wins == round, "round win was not accumulated");
            if (round < 3)
            {
                sim.Step(.11f);
                Check(sim.Phase == ArkenoidMatchPhase.Playing && sim.RoundNumber == round + 1 && sim.Hero(2).Lives == 1,
                    "round transition did not reset life or resume play");
            }
        }
        Check(sim.Phase == ArkenoidMatchPhase.MatchResult && sim.MatchWinnerSlot == 1, "three-round match did not finish");
        sim.ResetMatch();
        Check(sim.TickNumber == 0 && sim.ElapsedTime == 0 && sim.Hero(1).Wins == 0, "match restart did not reset clock or wins");


        sim = Start(new ArkenoidTuning { startingScore = 1 });
        var deadGoal = sim.AddBall(sim.Geometry.SidePoint(ArenaSide.Bottom, 0, sim.Tuning.goalPlane), new ArkVector(0, -8));
        sim.TryScore(ArenaSide.Bottom, deadGoal);
        sim.SetInput(2, new ArkInput { Axis = 1, KickPressed = true }); Step(sim, 20);
        Check(sim.Hero(2).Lateral == 0, "eliminated hero kept moving");
        var rebound = sim.AddBall(new ArkVector(0, -4.5f), new ArkVector(0, -16)); Step(sim, 7);
        Check(rebound.Active && rebound.Velocity.Y > 0, "eliminated goal did not become a solid wall");

        sim = Start();
        var fast = sim.AddBall(new ArkVector(0, -3), new ArkVector(0, -16));
        sim.Step(.2f);
        Check(fast.Active && fast.Velocity.Y > 0, "fast ball tunneled through defender");
        sim = Start();
        var movingContact = sim.AddBall(new ArkVector(0, -3), new ArkVector(0, -16));
        sim.SetInput(2, new ArkInput { Axis = 1 }); sim.Step(.2f);
        Check(movingContact.Active && movingContact.Velocity.Y > 0, "sweep used defender's end position instead of movement across tick");
        sim.SetInput(2, new ArkInput { RepulsePressed = true }); sim.Step(.04f);
        Check(sim.Hero(2).State != ArkenoidHeroState.RedKick, "unlimited unearned repulse was enabled");

        sim = Start(new ArkenoidTuning { allowAttractForCalibration = true });
        var captured = sim.AddBall(new ArkVector(0, -4.5f), new ArkVector(0, -.1f));
        sim.SetInput(2, new ArkInput { AttractHeld = true }); sim.Step(.04f);
        Check(captured.GrabOwnerSlot == 2 && sim.Hero(2).State == ArkenoidHeroState.Grab, "attract did not capture");
        Step(sim, 100);
        Check(captured.GrabOwnerSlot == 2 && captured.Velocity.LengthSquared == 0, "grab auto-released or changed velocity");
        sim.SetInput(2, new ArkInput()); sim.Step(.04f);
        Check(captured.GrabOwnerSlot == -1 && captured.Velocity.Y > 0 && sim.Hero(2).State == ArkenoidHeroState.Kick, "hold/release failed");

        foreach (ArkenoidVariant variant in Enum.GetValues(typeof(ArkenoidVariant)))
        {
            sim = Start(new ArkenoidTuning { seed = 37, maxBalls = 5, spawnInterval = .5f }, true, variant);
            for (int i = 0; i < 6000; i++)
            {
                sim.Step(.02f);
                foreach (ArkBallModel b in sim.Balls)
                    Check(!float.IsNaN(b.Position.X) && !float.IsNaN(b.Position.Y) && b.Velocity.Length <= sim.Tuning.maxBallSpeed + .01f, "ball became invalid");
                Check(sim.Balls.Count <= 5, "ball population exceeded cap");
            }
        }
        Console.WriteLine($"PASS: {assertions} assertions; measured PS1 Dingodile motion/ball/wave traces, three-round match, 30Hz input/clock, identity, goals, death walls, reset, swept contacts, grab, four variant dispatches, seeded bot soak.");
    }
}
