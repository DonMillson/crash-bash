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
    static void Main()
    {
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
        sim.SetInput(2, new ArkInput { RepulsePressed = true }); sim.Step(.02f);
        Check(sim.Hero(2).State != ArkenoidHeroState.RedKick, "unlimited unearned repulse was enabled");

        sim = Start(new ArkenoidTuning { allowAttractForCalibration = true });
        var captured = sim.AddBall(new ArkVector(0, -4.5f), new ArkVector(0, -.1f));
        sim.SetInput(2, new ArkInput { AttractHeld = true }); sim.Step(.02f);
        Check(captured.GrabOwnerSlot == 2 && sim.Hero(2).State == ArkenoidHeroState.Grab, "attract did not capture");
        Step(sim, 100);
        Check(captured.GrabOwnerSlot == 2 && captured.Velocity.LengthSquared == 0, "grab auto-released or changed velocity");
        sim.SetInput(2, new ArkInput()); sim.Step(.02f);
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
        Console.WriteLine($"PASS: {assertions} assertions; identity, goals, death walls, reset, swept contacts, grab, four variant dispatches, seeded bot soak.");
    }
}
