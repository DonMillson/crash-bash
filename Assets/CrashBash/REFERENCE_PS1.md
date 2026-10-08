# Crashball / Ballistix – PS1 gameplay reference

Reference sources:
- https://gamefaqs.gamespot.com/ps/914119-crash-bash/faqs/54749
- https://www.psxdatacenter.com/games/P/C/SCES-02834.html
- https://crashbandicoot.fandom.com/wiki/Ballistix
- https://crashbash.app/minigames/crashball (modern independent recreation, not primary original source)

## Confirmed baseline
- 4 hovercraft defending 4 goals, restricted to one axis each.
- 15 points per player in Crashball, one point lost per ball conceded.
- No round timer; last surviving player wins.
- Multiple balls enter play as the round develops.
- L1/R1 held to move faster; Square triggers timed extra kick / pulse.
- Eliminated goal becomes an active deflection barrier.
- Trophy in Adventure requires three round victories.
- Character choice does not determine arena side.

## Implemented approximation (requires side-by-side PS1 tuning)
- Arena dimensions, paddle widths, ball speeds, pulse cooldown and AI reaction.
- Periodic ball spawning up to 5 concurrent balls (exact PS1 cadence not measured).
- Keyboard prototype controls.

## Known missing
- Real corner launchers and warning lamps, exact PS1 geometry.
- PS1-accurate contact-angle and impulse calibration.
- Character select UI and real hovercraft/character models.
- Controller support and original visual effects.
- Unity editor compile/playmode verification (not available in this environment).

No extracted PS1 assets or ROM data are included.
