"""Extract *.binlog entries from one Azure DevOps build-log artifact.

The archive is produced by a PR-triggered build, so its entry paths, metadata
and contents are untrusted. Two properties keep extraction safe:

  * destination names are generated here, never taken from the archive, so a
    traversal or absolute path cannot choose where bytes land;
  * writing stops as soon as the caller's remaining byte budget is exceeded,
    so a zip bomb cannot fill the runner disk.

Entry paths and types are still validated up front — an archive containing a
traversal path or a link/device entry is hostile rather than merely odd, so
the whole artifact is rejected instead of partially extracted.

Usage: extract-binlogs.py <archive> <destination> <prefix> <budget-bytes> [label]
Prints "<extracted-count> <written-bytes>".
"""

import os
import stat
import sys
import zipfile
from pathlib import PurePosixPath

CHUNK_SIZE = 1024 * 1024
ALLOWED_TYPES = (0, stat.S_IFREG, stat.S_IFDIR)


def has_unsafe_path(name):
    normalized = name.replace("\\", "/")
    path = PurePosixPath(normalized)
    first = path.parts[0] if path.parts else ""
    return (
        "\0" in name
        or path.is_absolute()
        or ".." in path.parts
        # A Windows drive spec such as "c:/foo" or "c:foo" is not absolute to
        # PurePosixPath but is still an attempt to escape the destination.
        or (len(first) >= 2 and first[0].isalpha() and first[1] == ":")
    )


def has_unsupported_type(entry):
    mode = (entry.external_attr >> 16) & 0xFFFF
    return stat.S_IFMT(mode) not in ALLOWED_TYPES


def safe_label(value):
    """Artifact names are untrusted build metadata, so re-sanitize here rather
    than trusting the caller: only the destination name generated in this
    process may decide where bytes land."""
    cleaned = "".join(c if (c.isalnum() or c in "._-") else "_" for c in value)
    return cleaned.strip("._-")[:80]


def main():
    archive, destination, prefix, budget = sys.argv[1:5]
    label = safe_label(sys.argv[5]) if len(sys.argv) > 5 else ""
    budget = int(budget)

    with zipfile.ZipFile(archive) as zip_file:
        entries = zip_file.infolist()

        # Validate every entry before reading any payload.
        for index, entry in enumerate(entries):
            if has_unsafe_path(entry.filename):
                raise ValueError(f"archive entry {index} has an unsafe path")
            if has_unsupported_type(entry):
                raise ValueError(f"archive entry {index} has an unsupported type")

        selected = [
            entry
            for entry in entries
            if not entry.is_dir() and entry.filename.lower().endswith(".binlog")
        ]

        os.makedirs(destination, exist_ok=True)
        written = 0
        for index, entry in enumerate(selected):
            stem = f"{prefix}_{index}_{label}" if label else f"{prefix}_{index}"
            target = os.path.join(destination, f"{stem}.binlog")
            with zip_file.open(entry) as source, open(target, "xb") as output:
                while chunk := source.read(CHUNK_SIZE):
                    written += len(chunk)
                    if written > budget:
                        raise ValueError("extracted binlogs exceed the remaining budget")
                    output.write(chunk)

    print(f"{len(selected)} {written}")


main()
