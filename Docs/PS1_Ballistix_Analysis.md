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
