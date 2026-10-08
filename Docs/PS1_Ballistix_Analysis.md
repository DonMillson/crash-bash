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
