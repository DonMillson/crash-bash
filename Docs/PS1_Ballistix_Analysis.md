# PS1 source-image analysis — Ballistix / Arkenoid

This project uses the user's own Crash Bash PS1 BIN/CUE as a behavioral and structural reference. Original copyrighted assets are not committed.

## Disc facts verified from the image
- PlayStation CD, MODE2/2352.
- ISO9660 volume contains:
  - `SCUS_945.70` — PS-X EXE
  - `CRASHBSH/CRASHBSH.DAT` — main Crash Bash data archive
  - `BASHY.`
  - `SYSTEM.CNF`
  - Spyro 3 demo data

## Ballistix implementation names recovered from CRASHBSH.DAT
The original engine calls this game family **Arkenoid** internally.

Recovered symbols/strings include:
- `ARKENOID_PREMAP`
- `ARKENOIDSpecifics`
- `arkenoid_shell_info`
- `file_group_BALLS`
- `file_group_BEACHBALL`
- `file_group_NGINBALL`
- `file_group_SKYBALL`
- `DangerBalls(unsigned int)`
- `Init_RepulseRing` / `Plot_RepulseRing`
- `Init_MagnaRing` / `Plot_MagnaRing`
- `ArkHero2ObjectCollision`
- `ArkHeroRad2ObjectRepel`
- `ArkHeroRad2ObjectAttract`
- `TestArkObjectObstacles`

Character animation sequences exist for Crash, Coco, Tiny, Cortex, Brio, Dingodile, Koala Kong and Rilla Roo, including:
- breathe
- idle
- taunt
- move
- kick
- win
- victory
- lose

## Instruction strings recovered
The data contains original tutorial text referring to:
- moving to deflect balls,
- giving balls an extra kick,
- attracting balls and releasing to fire,
- repelling balls,
- special balls that do not score.

This is stronger evidence than our earlier generic Pong-like placeholder assumptions.

## Reconstruction rule
Unity implementation should reproduce the PS1 game behavior first, then upgrade rendering. Do not substitute generic paddles, Pong rules or invented mechanics where the PS1 data can be analyzed.

Next reverse-engineering targets:
1. locate the Arkenoid level/config records referenced by the recovered symbols;
2. map BALLS / BEACHBALL / NGINBALL / SKYBALL resource groups;
3. derive arena dimensions and object spawn positions;
4. derive movement, kick/repulse/attract timing and ball parameters;
5. recreate visible assets with new meshes/materials while preserving gameplay dimensions.

No ROM/ISO assets are redistributed in this repository.


## Direct BIN/CUE inspection — verified
The supplied disc image was parsed as MODE2/2352 and the ISO9660 filesystem was read directly.

Relevant files:
- /CRASHBSH/CRASHBSH.DAT — 73,220,096 bytes, LBA 236
- /SCUS_945.70 — 432,128 bytes
- /BASHY. — 31,752,000 bytes

A readable preprocessed/debug header block survives near the end of CRASHBSH.DAT. This exposes substantially more of the original Arkenoid subsystem than web references alone.

### Original Arkenoid functions recovered
- AR_SetUp / AR_PreInitLevel / AR_InitLevel / AR_RestartLevel / AR_UpdateLevel
- AR_InitHero / AR_PreMoveHero / AR_MoveHero / Ark_HeroBot
- AR_KeepInBounds and per-variant BA/SE/NG/PI bounds handlers
- AR_BA_BounceOnBounds / AR_SE_BounceOnBounds / AR_PI_BounceOnBounds
- CreateBall / InitBall / PlotBall / CalcArkInfluence
- FreeGrabbedObject / AlignGrabBall
- Create_ArkPickup / Plot_Ark_Pickup
- Init_ArkLaserWall / Plot_ArkLaserWall
- Init_RepulseRing / Plot_RepulseRing
- Init_MagnaRing / Plot_MagnaRing
- ArkHero2ObjectCollision / ArkHeroRad2ObjectCollision
- ArkHeroRad2ObjectRepel / ArkHeroRad2ObjectAttract

### Variant evidence
Separate handlers exist for BA, SE, NG and PI, proving that the four Ballistix-family arenas are not just cosmetic reskins. They have variant-specific bounds/out-of-bounds behavior.

### Object-type evidence
ARKENOID object IDs include Ball, AR_PU, ArkRepulse, ArkFlash, ArkDeadWall, NGDeadWall, N_Gin, SeaWeed and LaserWall.

### Animation evidence
All eight playable characters have dedicated Arkenoid animation sets for breathe, idle, taunt, move, kick, win, victory and lose.

## Engineering consequence
The temporary Unity implementation must now be treated only as scaffolding. Gameplay code should be split into a common Arkenoid core plus per-variant rule modules rather than one generic Pong ruleset. Visual hovercraft/character rigs should be independent from invisible collision volumes.


## Raw symbol pass from the supplied CRASHBSH.DAT

The read-only extractor now confirms these original functions directly in the data archive:

```
AR_SetUp
AR_PreInitLevel
AR_InitLevel
AR_RestartLevel
AR_UpdateLevel
AR_InitHero
AR_PreMoveHero
AR_MoveHero
Ark_HeroBot
DangerBalls
AR_KeepInBounds
AR_PI_KeepInBounds
AR_BA_BounceOnBounds
AR_SE_BounceOnBounds
AR_PI_BounceOnBounds
AR_BA_TestOutOfBounds
AR_SE_TestOutOfBounds
AR_NG_TestOutOfBounds
AR_PI_TestOutOfBounds
AR_BA_TestLaunchBounds
AR_PI_TestLaunchBounds
CreateBall
InitBall
PlotBall
CalcArkInfluence
FreeGrabbedObject
AlignGrabBall
Init_ArkDeadWall
Init_NGArkDeadWall
SetCornerFailure
HandleCornerFailure
GetArkContour
GetArkPIContour
Create_ArkPickup
Init_ArkLaserWall
Init_RepulseRing
Init_MagnaRing
ArkHero2ObjectCollision
ArkHeroRad2ObjectCollision
ArkHeroRad2ObjectRepel
ArkHeroRad2ObjectAttract
```

This confirms that BA/SE/NG/PI have distinct boundary and out-of-bounds paths in the original engine. Until their exact mapping is proven from the disc, the remake code must preserve these internal variant IDs instead of guessing descriptive names.

### Immediate implementation rule
Do not hard-code one generic rectangular arena. Build an Arkenoid base controller with pluggable boundary, launch, scoring and special-object behavior for BA, SE, NG and PI. Rendering remains separate from collision/gameplay geometry.


## Deeper header recovery

A contiguous preprocessed C/C++ header section was located at approximately `0x045CE8E3` inside `CRASHBSH.DAT`. It identifies the original source path as:

`c:\\Crash4\\GameEng\\Arkenoid\\arkenoid.h`

Additional behavior entry points recovered:
- `AR_Triggers`, `AR_EarliestTriggers`, `AR_InitIcons`
- `AR_InitShadow`, `AR_UpdateShadow`
- `TestArkenoidStop`, `TestForMove`
- state pairs for Idle, Move, Kick, RedKick, Grab, Taunt, Winner, Lose, Die, Dead
- `PickArkPlayer`, `FinishArkLevel`
- `Init_N_Gin`, `Plot_N_Gin`
- `InitARKfloor`, `UpdateArkFloor`, `ArkCrateMaster`
- seaweed, camera flash, dead-wall, pickup and laser-wall systems

The original object enum begins at `0x1500` and includes:
`AST_Ball, AST_AR_PU, AST_ArkRepulse, AST_ArkFlash, AST_ArkDeadWall, AST_NGDeadWall, AST_N_Gin, AST_SeaWeed, AST_LaserWall`.

### Reconstruction consequence
The visible player object must support explicit PS1-style animation/gameplay states rather than being a generic paddle:
Idle -> Move -> Kick/RedKick/Grab -> Taunt/Win/Lose/Die/Dead.

The four variants also need separate environment modules because the original engine contains N.Gin, seaweed and laser-wall systems alongside variant-specific bounds.

## Reproducible evidence pass, 2026-10-08

`tools/analyze_arkenoid.py` now generates `Docs/PS1_Arkenoid_Evidence.json`
directly from the supplied BIN/CUE. The JSON contains offsets, identifiers and
an archive hash, **no original graphical assets or original function bodies**.

Verified: 91 declarations, ten set/update state pairs and sequential object IDs
`0x1501` through `0x1509` (the `0x1500` value is the zero sentinel). Tutorial
signals at `0x00E140xx`–`0x00E143xx` independently establish hold-to-attract,
release-to-fire, a force field collected at a corner post, failing engines,
N.Gin attacks and non-scoring challenge balls.

Limits: the surviving header contains declarations, not Arkenoid implementations.
It does **not** establish dimensions, velocities, frame timings, bot logic, the
meaning of RedKick, or a one-to-one mapping of BA/SE/NG/PI to resource groups.
In particular, assigning seaweed to SE or LaserWall to PI from their names alone
is an unverified inference. No numeric gameplay setting is declared PS1-verified
by this pass. The earlier reconstruction arrows describe implementation intent,
not a recovered PS1 transition graph.

The ISO inspector had escaped textual `\\x00`/`\\x01` instead of byte identifiers
for the self/parent entries. It now skips them correctly, rejects cyclic and
malformed directories, and normalizes ISO version suffixes. Three synthetic
regression tests pass; the real disc produces nine entries without recursion.

The literal `\\n` between two methods in `ArenaBall.cs` was also removed. This
fixes an obvious C# syntax error; Unity compilation is not claimed by that fix.
