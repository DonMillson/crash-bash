# Arkenoid implementation and calibration

The existing Unity 6 project is retained. No new Unity project or PS1 graphical
assets have been added. The game now uses one shared fixed-step simulation,
with Unity components supplying input, invisible collision sensors, and views.
The shared clock now accumulates host updates into 30Hz logic ticks, preserving
button edges until consumption and interpolating only the replaceable visuals.
`MatchManager` replaces the earlier two competing paddle/controller loops.

## Gameplay implemented

- Independent slot ID, arena side, character ID and input device index.
- Four defenders, one-axis acceleration, sprint and release inertia, bounded
  using the selected profile. Character motion overrides are independent of side.
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
The original running Crashball VS game confirms 15 starting points, D-pad movement,
L1/R1 sprint and Square kick. The game options default to three cups; match length
is configurable. Crashball's rule page does not advertise universal attract or
repulse, so those mechanics remain gated behind calibration settings.

## Numerical provenance

Settings in `ArkenoidTuning` are temporary unless a measurement is documented
in `PS1_Arkenoid_Runtime_Measurements.json`. Visual builders consume geometry without
modifying it. In particular, `wallHalfExtent=5.81` is derived from the previous
Unity wall centre (6) minus half its thickness (.38 / 2), not a PS1 coordinate.
The earlier collision gaps between wall pieces and goal rails are removed by
using contiguous finite segments. Contact angle, ball speeds, launch interval,
maximum population, cooldown, score delay and animation durations are unmeasured.
Ball-to-ball collision is not enabled without a reference measurement.

The supplied NTSC-U image now runs locally in the official PCSX-ReARMed libretro
core (interpreter, HLE BIOS). A controlled P1 Dingodile VS run supplies actual
coordinate traces, sampled after each video frame. Position advances every other
60Hz callback. Normal displacements ramp 34, 68, 102, then 104 original units per
logic tick. Sprint ramps 56, 112, then 160. Released input reduces displacement
by 18 each tick; it does not stop immediately. The measured override uses 7.8/12
Unity units/s, acceleration 76.5/126 and deceleration 40.5 at 30Hz, with the chosen
conversion of 400 original coordinate units per Unity unit. The conversion is an
authoring choice, not a recovered PS1 constant. Other seven characters still use
explicitly provisional fallback motion; these observations do not prove their stats.

All four observed defender lines have coordinate magnitude 2176, now 5.44 at the
chosen scale. Human Dingodile clamps at -1200 and +1201; the current symmetric
travel of 3 is an approximation that leaves the one-unit asymmetry unresolved.
The goal aperture is temporarily 4.1 to contain the measured lane and vehicle
collision width. It is **not** a decoded PS1 goal bound. Collision radius, exact
contour and goal plane still require independent measurements. Modern visuals
have not changed any of these bounds.

## Verification available in this session

`Tests/Headless/Program.cs` compiles and executes the exact simulation source
files used by Unity. Run with the .NET 8 SDK:

```sh
python tools/run_headless_tests.py --dotnet /path/to/dotnet
```

Coverage includes five PS1 movement traces (start, sprint, coast and bound),
sub-tick input edges, update-rate independence, permuted identities, bounded travel, duplicate goals,
non-scoring balls, dead-wall rebound, elimination, persistent match result,
restart, fast/moving swept contacts, gated repulse, hold/release, and seeded bot
soaks for all four variant dispatches. These are real C# simulation tests, not
Python replicas or Unity API stubs.

All runtime C# files also compile using Roslyn against genuine UnityEngine
managed assemblies from the official Unity 6000.0.60f1 Linux distribution. This
checks API signatures and dependencies without fake Unity API stubs:

```sh
python tools/compile_unity_references.py --dotnet /path/to/dotnet --managed /path/to/Unity/Editor/Data/Managed
```

The Unity editor itself is unavailable here. Unity import, editor assembly
compilation, shader rendering, PlayMode and Windows executable builds have **not**
been run. C# reference compilation is not represented as a Unity build. Full editor verification is
still required before calling the vertical slice PS1-accurate or release-ready.

## Vehicle, arena and animation integration

The collider root has unit scale, no Renderer and one invisible BoxCollider.
The replaceable hovercraft visual uses 27 authored mesh parts: a lofted pressure
hull, outriggers, armour, front deflector, rubber skirt, seat, console, headlamps,
turbine collars, hover nozzles, jets and levitation coil. It includes a distinct
modular pilot rig for every character. These are original procedural art, not
finished replica character models or extracted PS1 meshes.

All ten gameplay states reach the visual layer. Breathing, movement bank, kick
recoil, magnet pose, taunt, victory, loss and death use authored procedural motion.
Their timing/poses have not been measured from original animation records. Pilots
are selected independently in the Players menu; changing side swaps the two side
assignments while preserving their slot and character. Two keyboard players and
CPU control are selectable; gamepad support is still pending.
Selections are now a draft until Start New Match: Back/Tab/Escape discard changes.
The setup menu locks pause/restart hotkeys and restores the prior pause state.

The arena renderer consumes the selected simulation geometry. Goal thresholds,
posts, contiguous corner walls, movement lane markings, corner launch nozzles,
warning lamps, score readouts and eliminated-goal gates are live. Runtime PBR
materials, a generated brushed-panel texture, shadows, emissive lamps, ball seams,
trails, pooled event-driven rings and authored synthetic audio replace the greybox.
No art component creates a gameplay collider or changes a movement/spawn bound.
An offscreen mesh inspection rendered the actual C#-exported craft recipe; this
was **not a Unity game screenshot**. Mesh indices and outward winding pass the
same C# test harness.

Unity EditMode integration tests and Windows build menu/CLI entry points are
included. They remain **unexecuted** until the Unity editor is available. The
Windows builder creates a scene in the existing project, retains runtime shaders,
and checks the real BuildReport before reporting success.
