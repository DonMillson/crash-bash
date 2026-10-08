"""Regression coverage for directory traversal, without distributing a disc."""
import io
import unittest
from inspect_ps1_disc import directory, SECTOR, OFFSET


def record(identifier, lba, directory_flag=False):
    size = 33 + len(identifier) + (len(identifier) % 2 == 0)
    r = bytearray(size)
    r[0] = size
    r[2:6] = lba.to_bytes(4, "little")
    r[10:14] = (2048 if directory_flag else 100).to_bytes(4, "little")
    r[25] = 2 if directory_flag else 0
    r[32] = len(identifier)
    r[33:33+len(identifier)] = identifier
    return r


def image(payload):
    raw = bytearray(SECTOR)
    raw[15] = 2
    raw[OFFSET:OFFSET+len(payload)] = payload
    return io.BytesIO(raw)


class DirectoryTests(unittest.TestCase):
    def test_self_parent_are_skipped_without_recursion(self):
        data = record(b"\x00", 0, True) + record(b"\x01", 0, True) + record(b"CRASHBSH.DAT;1", 236)
        rows = list(directory(image(data), 0, 2048))
        self.assertEqual([r["path"] for r in rows], ["/CRASHBSH.DAT"])

    def test_malformed_record_rejected(self):
        with self.assertRaises(ValueError):
            list(directory(image(b"\x07garbage"), 0, 2048))

    def test_cycle_rejected(self):
        with self.assertRaisesRegex(ValueError, "Cyclic"):
            list(directory(image(record(b"LOOP", 0, True)), 0, 2048))


if __name__ == "__main__":
    unittest.main()
