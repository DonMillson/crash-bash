# Arkenoid implementation and calibration

The existing Unity 6 project is retained. No new Unity project or PS1 graphical
assets have been added. The game now uses one shared fixed-step simulation,
with Unity components supplying input, invisible collision sensors, and views.
`MatchManager` replaces the earlier two competing paddle/controller loops.

## Gameplay implemented

- Independent slot ID, arena side, character ID and input device index.
- Four defenders, one-axis movement and boost, bounded using the selected profile.
- Swept ball contacts with finite corner walls, goal posts and moving defenders.
- Timed kick, passive deflection with movement/offset influence, multiple corner
  launches, duplicate-safe scoring and non-scoring ball support.
- Lose -> Die -> Dead progression; eliminated players stop moving and their goal
  becomes a reflecting wall immediately. Countdown, round result, three-win
  match result, and explicit restart. Match wins no longer disappear automatically.
- Bot threat selection scans all balls. Prediction/reaction is a temporary
  heuristic, not a claim to have recovered `Ark_HeroBot` or `DangerBalls` bodies.
- Hold-to-attract, aligned grabbed ball and release-to-fire are functional in
  opt-in calibration mode. No invented automatic .8-second release.
- Repulse requires a collected charge; opt-in corner pickup placement is provisional.
  Unlimited repulse is disabled. The RedKick/repulse animation binding is provisional.

## Variant ownership

BA, SE, NG, PI have separately selectable tuning profiles, rule objects and
live environment dispatch. Bounds/out-of-bounds/launch entry points are traced
by name. The currently shared conservative geometry fallback is not advertised
as decoded original variant geometry. SeaWeed, N_Gin, Flash and LaserWall must
not be assigned to variants just from their names.

The baseline uses BA as the working profile. Its association with the visible
Crashball arena is provisional until resource/overlay mapping establishes it.
`startingScore=15` and `winsNeeded=3` retain the previous reference settings;
the header extraction does not prove either number.

## Numerical provenance

**Every** setting in `ArkenoidTuning` is a temporary calibration value unless a
later measurement is documented here. Visual builders consume geometry without
modifying it. In particular, `wallHalfExtent=5.81` is derived from the previous
Unity wall centre (6) minus half its thickness (.38 / 2), not a PS1 coordinate.
The earlier collision gaps between wall pieces and goal rails are removed by
using contiguous finite segments. Contact angle, ball speeds, launch interval,
maximum population, cooldown, score delay and animation durations are unmeasured.
Ball-to-ball collision is not enabled without a reference measurement.

## Verification available in this session

`Tests/Headless/Program.cs` compiles and executes the exact simulation source
files used by Unity. Run with the .NET 8 SDK:

```sh
python tools/run_headless_tests.py --dotnet /path/to/dotnet
```

Coverage includes permuted identities, bounded travel, duplicate goals,
non-scoring balls, dead-wall rebound, elimination, persistent match result,
restart, fast/moving swept contacts, gated repulse, hold/release, and seeded bot
soaks for all four variant dispatches. These are real C# simulation tests, not
Python replicas or Unity API stubs.

The Unity editor is unavailable here. Unity import, editor compilation, rendering,
PlayMode and Windows executable builds have **not** been run. A C# syntax parse
is useful but is not represented as a Unity build. Full editor verification is
still required before calling the vertical slice PS1-accurate or release-ready.
