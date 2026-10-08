#!/usr/bin/env python3
"""Produce structural evidence only; never export PS1 models, textures or code.

Offsets are relative to CRASHBSH.DAT, NOT to the raw BIN. Surviving declarations
prove entry points and object IDs, not function bodies or gameplay constants.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import pathlib
import re
from inspect_ps1_disc import sector, directory, extent


def analyze(data: bytes) -> dict:
    anchor = data.find(b"enum\tARKENOID_AST_types")
    if anchor < 0:
        raise ValueError("Arkenoid header not found in this disc revision")
    end = data.find(b'GameEng\\\\Polar', anchor)
    if end < 0:
        raise ValueError("Cannot delimit the Arkenoid header safely")
    header = data[anchor:end]
    enum_body = re.search(rb"enum\s+ARKENOID_AST_types\s*\{([^}]+)\}", header).group(1)
    names = re.findall(rb"\bAST_[A-Za-z0-9_]+\b", enum_body)
    base = int(re.search(rb"ARKENOID_AST_zero\s*=\s*(0x[0-9a-fA-F]+)", enum_body).group(1), 16)
    declaration = re.compile(rb"(?:void|bool|NAS\*|signed\s+(?:short\s+)?int)\s+([A-Za-z_][A-Za-z_0-9]*)\s*\([^;{}]*\)\s*;")
    functions = [{"name": m.group(1).decode(), "datOffset": f"0x{anchor + m.start():08X}"}
                 for m in declaration.finditer(header)]
    states = [name for name in ["Idle", "Move", "Kick", "RedKick", "Grab", "Taunt", "Winner", "Lose", "Die", "Dead"]
              if f"SetArkHero{name}".encode() in header and f"ArkHero{name}".encode() in header]
    tutorial_signals = {
        "goalDeflectionAndLastSurvivor": b"USE YOUR SHIP TO DEFLECT THE",
        "holdForShipSpeed": b"PRESS AND HOLD TO INCREASE",
        "extraKick": b"PRESS TO GIVE THE BALLS",
        "holdAttract": b"TO ATTRACT BALLS TO",
        "releaseToFire": b"RELEASE TO FIRE BALLS",
        "nginAttack": b"BEWARE OF N.GIN'S ATTACKS",
        "forceFieldFromCornerPost": b"COLLECT THE FORCE FIELD",
        "failingEngines": b"BEWARE OF THE FAILING ENGINES",
        "challengeBallsDoNotScore": b"THESE BALLS DO NOT SCORE",
    }
    signals = {name: [f"0x{m.start():08X}" for m in re.finditer(re.escape(term), data)]
               for name, term in tutorial_signals.items()}
    return {
        "schemaVersion": 1,
        "archive": {"path": "/CRASHBSH/CRASHBSH.DAT", "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()},
        "header": {"enumOffset": f"0x{anchor:08X}", "kind": "preprocessed declarations; no Arkenoid function bodies recovered"},
        "objectIds": {name.decode(): f"0x{base + i + 1:04X}" for i, name in enumerate(names)},
        "statePairs": states,
        "functions": functions,
        "resourceGroups": [name.decode() for name in re.findall(rb"extern\s+file_group_load_info\s+(file_group_[A-Z]+)", header)],
        "tutorialSignals": signals,
        "verifiedNumericGameplayValues": {},
        "unresolved": ["BA/SE/NG/PI to level/resource mapping", "bounds and movement units", "launch cadence and speed",
                       "kick/RedKick semantics and cooldowns", "bot decision logic", "variant ownership of special objects",
                       "animation timing and geometry records"],
    }


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("bin", type=pathlib.Path)
    ap.add_argument("--cue", type=pathlib.Path)
    ap.add_argument("--output", type=pathlib.Path)
    args = ap.parse_args()
    if args.cue:
        cue = args.cue.read_text(encoding="utf-8-sig")
        if "MODE2/2352" not in cue or "INDEX 01 00:00:00" not in cue or cue.count("TRACK ") != 1:
            raise ValueError("Only single-track MODE2/2352, zero-index CUE is currently supported")
    with args.bin.open("rb") as fp:
        pvd = sector(fp, 16)
        if pvd[:7] != b"\x01CD001\x01":
            raise ValueError("Invalid ISO9660 primary volume descriptor")
        root = pvd[156:190]
        entries = list(directory(fp, int.from_bytes(root[2:6], "little"), int.from_bytes(root[10:14], "little")))
        archive = next(row for row in entries if row["path"] == "/CRASHBSH/CRASHBSH.DAT")
        report = analyze(extent(fp, archive["lba"], archive["size"]))
    report["archive"]["lba"] = archive["lba"]
    serialized = json.dumps(report, indent=2) + "\n"
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(serialized, encoding="utf-8")
        print(f"Verified {len(report['functions'])} declarations, {len(report['statePairs'])} states, {len(report['objectIds'])} object IDs. No gameplay numbers inferred.")
    else:
        print(serialized, end="")


if __name__ == "__main__":
    main()
