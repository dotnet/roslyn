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

    def validate_url(self, url):
        with tempfile.TemporaryDirectory(prefix="roslyn-binlog-url-") as directory:
            return subprocess.run(
                [
                    self.dotnet,
                    "run",
                    "--file",
                    str(SCRIPT),
                    "-p:ImportDirectoryBuildProps=false",
                    "-p:ImportDirectoryBuildTargets=false",
                    "-p:ImportDirectoryPackagesProps=false",
                    "--",
                    "--validate-url",
                    url,
                ],
                capture_output=True,
                text=True,
                cwd=directory,
                env=os.environ,
                timeout=120,
            )

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

    def test_accepts_documented_artifact_hosts_only(self):
        artifact_path = (
            "/A6fcc92e5-73a7-4f88-8d13-d9045b45fb27/"
            "cbb18261-c48f-4abb-8651-8cdcb5474649/artifact"
        )
        cases = (
            ("compact-region", f"https://artprodeus21.artifacts.visualstudio.com{artifact_path}", True),
            ("dotted-region", f"https://artprod.eus21.artifacts.visualstudio.com{artifact_path}", True),
            ("hyphenated-region", f"https://artprod-weu.artifacts.visualstudio.com{artifact_path}", True),
            ("wrong-project", "https://artprod.eus21.artifacts.visualstudio.com/A6fcc92e5-73a7-4f88-8d13-d9045b45fb27/00000000-0000-0000-0000-000000000000/artifact", False),
            ("lookalike-host", f"https://artprod.eus21.artifacts.visualstudio.com.evil.example{artifact_path}", False),
            ("wrong-prefix", f"https://evilartprod.eus21.artifacts.visualstudio.com{artifact_path}", False),
        )
        for name, url, accepted in cases:
            with self.subTest(name=name):
                result = self.validate_url(url)
                self.assertEqual(result.returncode == 0, accepted, result.stderr)


if __name__ == "__main__":
    unittest.main()
