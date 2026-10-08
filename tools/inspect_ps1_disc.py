#!/usr/bin/env python3
"""Read-only PlayStation BIN/CUE MODE2/2352 ISO9660 inspector.

Does not export copyrighted assets or modify the source image.
Usage: python3 tools/inspect_ps1_disc.py "Crash Bash.bin"
"""
from __future__ import annotations
import argparse
import json
import pathlib

SECTOR = 2352
PAYLOAD = 2048
OFFSET = 24  # MODE2 Form 1 user data starts at byte 24

def sector(fp, lba: int) -> bytes:
    fp.seek(lba * SECTOR)
    raw = fp.read(SECTOR)
    if len(raw) != SECTOR:
        raise ValueError(f"Missing sector {lba}")
    if raw[15] != 2:
        raise ValueError(f"Sector {lba} is not MODE2")
    if raw[18] & 0x20:
        raise ValueError(f"Sector {lba} is MODE2 Form 2 (not ISO9660 Form 1)")
    return raw[OFFSET:OFFSET + PAYLOAD]

def extent(fp, lba: int, length: int) -> bytes:
    count = (length + PAYLOAD - 1) // PAYLOAD
    return b"".join(sector(fp, lba + n) for n in range(count))[:length]

def directory(fp, lba: int, size: int, base: str = "", depth: int = 0, seen=None):
    if depth > 10:
        raise ValueError("ISO9660 directory nesting exceeds safety limit")
    seen = set() if seen is None else seen
    if lba in seen:
        raise ValueError(f"Cyclic ISO9660 directory extent at LBA {lba}")
    seen = seen | {lba}
    data = extent(fp, lba, size)
    pos = 0
    while pos < len(data):
        n = data[pos]
        if n == 0:
            pos = ((pos // PAYLOAD) + 1) * PAYLOAD
            continue
        record = data[pos:pos+n]
        if n < 34 or len(record) != n or (pos % PAYLOAD) + n > PAYLOAD:
            raise ValueError(f"Malformed ISO9660 directory record at {base}:{pos}")
        location = int.from_bytes(record[2:6], "little")
        length = int.from_bytes(record[10:14], "little")
        flags = record[25]
        namelen = record[32]
        identifier = record[33:33+namelen]
        if len(identifier) != namelen:
            raise ValueError("Truncated ISO9660 file identifier")
        pos += n
        if identifier in (b"\x00", b"\x01"):
            continue
        name = identifier.decode("ascii", "replace").split(";")[0]
        full = base + "/" + name
        yield {"path": full, "lba": location, "size": length, "directory": bool(flags & 2)}
        if flags & 2:
            yield from directory(fp, location, length, full, depth + 1, seen)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("bin", type=pathlib.Path)
    ap.add_argument("--json", action="store_true")
    args = ap.parse_args()
    with args.bin.open("rb") as fp:
        pvd = sector(fp, 16)
        if pvd[1:6] != b"CD001":
            raise ValueError("ISO9660 primary volume descriptor not found at sector 16")
        root = pvd[156:190]
        lba = int.from_bytes(root[2:6], "little")
        size = int.from_bytes(root[10:14], "little")
        rows = list(directory(fp, lba, size))
    if args.json:
        print(json.dumps(rows, indent=2))
    else:
        for row in rows:
            print(f"{row['lba']:7d} {row['size']:10d} {row['path']}")

if __name__ == "__main__":
    main()
