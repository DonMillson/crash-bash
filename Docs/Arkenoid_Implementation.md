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
- Timed kick with a growing influence window and one impulse per ball/action,
  passive deflection with movement/offset influence, multiple corner launches,
  duplicate-safe scoring and non-scoring ball support.
- Lose -> Die -> Dead progression; eliminated players stop moving and their goal
  becomes a reflecting wall immediately. Countdown, round result, three-win
  match result, and explicit restart. Match wins no longer disappear automatically.
  Immediate dead-wall activation is a provisional timing choice pending a PS1 death trace.
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
using contiguous finite segments. Contact angle, exact collision contour, launch
interval, maximum population, cooldown, score delay and animation durations are
unmeasured. Scoped current/cruise speed and launch-height observations are described below.
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
chosen scale. Human Dingodile clamps at -1200 and +1201; the Crashball reference profile now includes
that positive-coordinate overrun. Side-aware limits preserve the coordinate sign
when translating to a side's lateral axis. Other characters and the unmeasured
sides still use this provisional bound fallback.
The goal aperture is temporarily 4.1 to contain the measured lane and vehicle
collision width. It is **not** a decoded PS1 goal bound. Collision radius, exact
contour and goal plane still require independent measurements. Modern visuals
have not changed any of these bounds.

## Ball and kick calibration from the running PS1 reference

The BA working profile opts into `CrashballReference()`; SE, NG and PI retain
independent provisional profiles. The first observed corner launcher is at
(2048, -256, -2048), converted to (5.12, .64, -5.12). Its 31 measured height
samples are reproduced by the 30Hz model: initial upward speed 48 units/tick,
gravity 6 units/tick squared, resting centre height 96 and rebound ratio .5.
Horizontal current speed starts at 32 and ramps by 4 towards cruise target 80.
After the launch ramp, the original trajectory changes direction. The remake's
horizontal aim/RNG throughout the launch is still provisional; only height and
speed behavior are calibrated, not the full XYZ trajectory. The remaining three
launch corners currently mirror the one measured position provisionally.
Height is separate from the 2D rules and interpolated only by the ball view.

Controlled incoming-ball probes distinguish current speed from cruise target.
With cruise target 144, incoming speeds 80/100/144/160/200/240 all produce 207
on the first affected update. A zero-current/zero-target ball produces 68,
then 72/76/80. The fitted explanation sets kick speed to old target +64,
switches cruise target to 144, then applies the ordinary +4/-1 approach.
This is a behavior reconstruction, not recovered `CalcArkInfluence` code.
Passive-contact angles and exact original collision shape remain unmeasured.
The current passive-contact cruise-target reset is also provisional: controlled
confirmation of target 144 applies to kick, not every passive collision.

Dingodile's stationary-ball probes fit a wave starting at radius 384, expanding
by 192 per logic update, with an effective 24-unit hit padding. It first affects
balls on action updates 2 through 5. Fine probes separate distances 600/601 and
792/793; 1177 and above remain unaffected. These are fitted parameters, not
decoded constants. The wave also separates the ball to its current radius before
ordinary movement. A ball receives one impulse per action; pushed separation is
swept against corner/dead walls to avoid tunnelling. Other character wave profiles
use this explicitly provisional fallback. The VFX follows the moving craft and
shows the same fitted growth instead of an unrelated easing curve.

One natural score occurs between observed coordinates -2962 and -3101.
3072 is only a candidate; both stationary and moving RAM teleport experiments
fail to score across it. Original trajectory flags/goal triggers remain unresolved.
The working goal plane 7.68 and collision radius .24 are provisional. Radius .24
is inferred from resting centre height, not decoded collision data.
The numeric fixture preserves selected real observations and failed experiments;
no missing coordinates, animation frames or original binary material are included.

## Verification available in this session

`Tests/Headless/Program.cs` compiles and executes the exact simulation source
files used by Unity. Run with the .NET 8 SDK:

```sh
python tools/run_headless_tests.py --dotnet /path/to/dotnet
```

Coverage includes five PS1 movement traces (start, sprint, coast and bound),
sub-tick input edges, update-rate independence, permuted identities, bounded travel, duplicate goals,
non-scoring balls, dead-wall rebound, elimination, persistent match result,
three-round win accumulation, restart, fast/moving swept contacts, gated repulse,
hold/release, 31 launch-height samples, six kick speed probes, coarse/fine kick
reach probes, wave/corner containment and seeded bot soaks for all four dispatches.
The motion regression now selects the Crashball reference profile and checks the
measured -1200/+1201 bounds within .025 original units. Additional functional
regressions exercise all ten hero states, taunt movement
lock/resume, earned repulse charge consumption and grabbed-ball cleanup on elimination.
Those regressions check the provisional action bindings; they do not confirm PS1
RedKick semantics, animation timings or pickup ownership. These are real C# simulation tests, not
Python replicas or Unity API stubs.

All runtime C# files also compile using Roslyn against genuine UnityEngine
managed assemblies from the official Unity 6000.0.60f1 Linux distribution. This
checks API signatures and dependencies without fake Unity API stubs:

```sh
python tools/compile_unity_references.py --dotnet /path/to/dotnet --managed /path/to/Unity/Editor/Data/Managed
```

The final source revision `0a49da3` was checked by two inspected CI runs:

- [Arkenoid simulation](https://github.com/DonMillson/crash-bash/actions/runs/37818894813):
  **195,332 assertions passed**, with the measured motion/ball/kick fixtures,
  exact -1200/+1201 bound checks, all ten hero states and the three-round match.
  All three ISO traversal regressions passed.
- [Unity C# API compilation](https://github.com/DonMillson/crash-bash/actions/runs/37818894841):
  **all 30 runtime C# files compiled against 73 genuine UnityEngine assemblies**,
  and the editor C# assembly compiled against genuine UnityEditor references.
  No synthetic Unity API substitutes were used.
- The 240-second reference-profile CPU game again produced **107 goals,
  four eliminations, one round win and a five-ball peak**.

The reference workflow downloads the pinned official Linux editor archive in
validated HTTP ranges and extracts managed DLLs only into runner temporary
storage. It records archive/assembly hashes there; no third-party DLL is committed
or uploaded as an artifact. Modular API selection excludes the legacy monolithic
reference facades that otherwise cause duplicate type definitions.

To repeat API compilation without an installed editor:

```sh
python tools/fetch_unity_managed_references.py --output /tmp/UnityManaged
python tools/compile_unity_references.py --dotnet /path/to/dotnet --managed /tmp/UnityManaged --editor
```

The download link and changeset are verified against the
[official Unity release page](https://unity.com/releases/editor/whats-new/6000.0.60f1).
An existing Unity installation's managed directory can instead be passed directly.

The Unity editor itself could not be run in this session; the local environment
later disconnected and the final changes were recovered and committed through
GitHub. Unity project import/assembly reload, shader rendering, EditMode/PlayMode
execution and Windows executable builds have **not** been run.
Reference compilation is **not** a Unity build or visual/playtesting approval.
The latest documentation-only commit does not alter the validated C# sources.

## Vehicle, arena and animation integration

The collider root has unit scale, Y=0, no Renderer and one invisible BoxCollider.
The visual model owns its hover height; bob and bank never translate the rules root.
Kinematic Rigidbody creation is safe in both runtime and editor configuration paths.
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
warning lamps, score readouts and eliminated-goal gates are live. The reference
launcher's barrel/cradle uses the measured launch height. Restart clears pooled
rings and ongoing audio; respawn resets launch, height and contact state. Runtime PBR
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

## Next reference and editor work

1. Measure scoring flags, actual goal aperture/collision contour and dead-wall
   activation timing before calling Crashball mechanics 1:1. Failed goal teleports
   and the unresolved passive speed reset must remain visible in the evidence.
2. Measure the other seven characters' movement, kick influence and button
   hold/repeat/cooldown behavior; replace only the corresponding provisional fields.
3. Establish BA/SE/NG/PI resource/overlay mapping and original bounds/launch handlers
   before assigning SeaWeed, N_Gin, Flash or LaserWall gameplay to a variant.
4. Import and play the existing Unity project, run its EditMode/PlayMode checks,
   inspect arena/craft lighting and poses, then build Windows with the real BuildReport.
   Match animation timing and craft silhouette against the private PS1 reference.
