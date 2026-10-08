# Crash Bash · Ballistix / Crashball

Development continues in this existing Unity 6 project. Only the Ballistix family
is in scope. The user's PS1 image is private reference material; no original
PS1 graphical assets, ROM bytes or original function bodies are distributed.

Open with **Unity 6000.0.60f1**, open an empty scene and press Play. The runtime
bootstrap creates the Crashball slice. Alternatively use **CrashBash → Create
Crashball scene**, then open `Assets/CrashBash/Scenes/Crashball.unity`.

- A/D or arrows: move; Shift/Q/E: sprint; Space: kick; T: taunt.
- Tab: choose any of the eight characters, a side, and human/CPU control.
- Second keyboard: J/L move, Right Shift sprint, I kick, P taunt.
- R: new match; Escape: pause. Match results persist until restart.
- Hold Ctrl to attract and release to fire; X to spend a repulse charge when the
  corresponding **opt-in calibration settings** are enabled in the Inspector.

Gameplay uses one tested fixed-step Arkenoid simulation, independent of visuals
and invisible Unity sensors. BA/SE/NG/PI have separate profiles and dispatch;
variant-specific original geometry and hazards still require calibration.
The working Crashball profile reproduces measured Dingodile inertia, ball launch
heights and growing kick influence. Goal triggers, collision angles, other
characters' stats and variant hazards remain provisional.

See [PS1 analysis](Docs/PS1_Ballistix_Analysis.md),
[reproducible evidence](Docs/PS1_Arkenoid_Evidence.json),
[runtime PS1 measurements](Docs/PS1_Arkenoid_Runtime_Measurements.json) and
[implementation / verification limits](Docs/Arkenoid_Implementation.md).

```sh
python -m unittest discover -s tools -p 'test_*.py'
python tools/run_headless_tests.py --dotnet /path/to/dotnet
python tools/compile_unity_references.py --dotnet /path/to/dotnet --managed /path/to/Unity/Editor/Data/Managed
python tools/analyze_arkenoid.py '/private/Crash Bash.bin' --cue '/private/Crash Bash.cue' --output Docs/PS1_Arkenoid_Evidence.json
```

**C# simulation and ISO regressions run in GitHub Actions. A previous runtime
revision compiled against genuine Unity 6 references. Unity editor import,
shader rendering, EditMode tests and a Windows executable build have not been
run in this session.** See the implementation notes for the scope of each check.
Use **CrashBash → Build Windows player** in a licensed Unity installation, or
`Unity -batchmode -quit -projectPath <this-repo> -executeMethod CrashBashRemake.Editor.ArkenoidBuildTools.BuildWindows -logFile build.log`.
