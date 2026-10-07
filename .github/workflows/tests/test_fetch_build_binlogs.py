import os
from pathlib import Path
import shutil
import stat
import subprocess
import tempfile
import unittest
import zipfile


SCRIPT = Path(__file__).resolve().parents[1] / "scripts" / "fetch-build-binlogs.cs"


def write_archive(path, entries):
    with zipfile.ZipFile(path, "w") as archive:
        for name, mode, content in entries:
            info = zipfile.ZipInfo(name)
            info.create_system = 3
            info.external_attr = mode << 16
            archive.writestr(info, content)


class FetchBuildBinlogsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.dotnet = shutil.which("dotnet")
        if not cls.dotnet:
            raise RuntimeError("dotnet is required.")

    def extract(self, entries, budget=1024 * 1024, label="../../Leg"):
        with tempfile.TemporaryDirectory(prefix="roslyn-binlog-extract-") as directory:
            root = Path(directory)
            archive = root / "artifact.zip"
            destination = root / "output"
            write_archive(archive, entries)
            result = subprocess.run(
                [
                    self.dotnet,
                    "run",
                    "--file",
                    str(SCRIPT),
                    "-p:ImportDirectoryBuildProps=false",
                    "-p:ImportDirectoryBuildTargets=false",
                    "-p:ImportDirectoryPackagesProps=false",
                    "--",
                    "--extract",
                    str(archive),
                    str(destination),
                    "7",
                    str(budget),
                    label,
                ],
                capture_output=True,
                text=True,
                cwd=root,
                env=os.environ,
                timeout=120,
            )
            files = {
                path.name: path.read_bytes()
                for path in destination.glob("*")
                if path.is_file()
            } if destination.exists() else {}
            return result, files

    def test_extracts_regular_binlogs_to_generated_names(self):
        result, files = self.extract(
            [
                ("nested/build.binlog", stat.S_IFREG | 0o644, b"binlog"),
                ("nested/readme.txt", stat.S_IFREG | 0o644, b"text"),
            ]
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.strip(), "1 6")
        self.assertEqual(files, {"7_0_Leg.binlog": b"binlog"})

    def test_rejects_unsafe_paths_and_entry_types(self):
        cases = (
            ("traversal", "../escape.binlog", stat.S_IFREG | 0o644),
            ("absolute", "/escape.binlog", stat.S_IFREG | 0o644),
            ("drive", r"C:\escape.binlog", stat.S_IFREG | 0o644),
            ("symlink", "link.binlog", stat.S_IFLNK | 0o777),
            ("device", "device.binlog", stat.S_IFCHR | 0o600),
        )
        for name, entry, mode in cases:
            with self.subTest(name=name):
                result, files = self.extract([(entry, mode, b"target")])
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual(files, {})

    def test_budget_failure_removes_partial_outputs(self):
        result, files = self.extract(
            [("build.binlog", stat.S_IFREG | 0o644, b"too-large")],
            budget=4,
        )
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(files, {})


if __name__ == "__main__":
    unittest.main()
