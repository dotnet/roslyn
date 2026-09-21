#!/usr/bin/env python3
"""Extract failing tests from a VSTest .trx file as a Markdown list.

Usage:
    trx_failures.py <file.trx> [more.trx ...] [-o OUT.md]

Output format (one bullet per failing test):
    - `Fully.Qualified.Test.Name` — path/to/File.cs:123
"""

import argparse
import re
import sys
import xml.etree.ElementTree as ET

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}

# " at Ns.Type.Method() in C:\src\File.cs:line 42"  (also matches localized-free ":line N")
STACK_LOCATION = re.compile(r"\sin\s(?P<file>.+?):line\s(?P<line>\d+)", re.IGNORECASE)


def _findall(root, tag):
    """Find elements by tag with or without the TRX namespace."""
    found = root.findall(f".//t:{tag}", NS)
    return found if found else root.findall(f".//{tag}")


def _find(elem, tag):
    found = elem.find(f"t:{tag}", NS)
    return found if found is not None else elem.find(tag)


def _text(elem, tag):
    child = _find(elem, tag) if elem is not None else None
    return child.text if child is not None and child.text else ""


def location_from_stack(stack):
    """Return (file, line) of the deepest test-owned frame, or (None, None)."""
    matches = list(STACK_LOCATION.finditer(stack or ""))
    if not matches:
        return None, None
    m = matches[0]
    return m.group("file").strip(), int(m.group("line"))


def collect(trx_path):
    root = ET.parse(trx_path).getroot()

    # testId -> (className, codeBase) from the test definitions
    definitions = {}
    for unit_test in _findall(root, "UnitTest"):
        method = _find(unit_test, "TestMethod")
        definitions[unit_test.get("id")] = (
            method.get("className") if method is not None else None,
            unit_test.get("storage") or (method.get("codeBase") if method is not None else None),
        )

    failures = []
    for result in _findall(root, "UnitTestResult"):
        if (result.get("outcome") or "").lower() != "failed":
            continue

        test_id = result.get("testId")
        class_name, storage = definitions.get(test_id, (None, None))
        name = result.get("testName") or ""

        output = _find(result, "Output")
        error_info = _find(output, "ErrorInfo") if output is not None else None
        stack = _text(error_info, "StackTrace")
        message = " ".join((_text(error_info, "Message") or "").split())

        file_path, line = location_from_stack(stack)
        failures.append(
            {
                "name": name,
                "class": class_name,
                "storage": storage,
                "file": file_path,
                "line": line,
                "message": message,
            }
        )
    return failures


def format_markdown(failures):
    lines = []
    for f in failures:
        if f["file"]:
            location = f"{f['file']}:{f['line']}"
        elif f["class"]:
            location = f["class"]
        elif f["storage"]:
            location = f["storage"]
        else:
            location = "unknown location"
        lines.append(f"- `{f['name']}` - {location}")
    return "\n".join(lines)


def main(argv=None):
    parser = argparse.ArgumentParser(description="Extract failing tests from .trx files as Markdown.")
    parser.add_argument("trx", nargs="+", help="Path(s) to .trx file(s)")
    parser.add_argument("-o", "--output", help="Write Markdown here instead of stdout")
    args = parser.parse_args(argv)

    failures = []
    for path in args.trx:
        failures.extend(collect(path))

    # Stable order, de-duplicated by (name, file, line).
    seen = set()
    unique = []
    for f in sorted(failures, key=lambda x: (x["name"] or "", x["file"] or "", x["line"] or 0)):
        key = (f["name"], f["file"], f["line"])
        if key not in seen:
            seen.add(key)
            unique.append(f)

    markdown = format_markdown(unique)
    if args.output:
        with open(args.output, "w", encoding="utf-8") as handle:
            handle.write(markdown + "\n")
    else:
        print(markdown)

    return 0


if __name__ == "__main__":
    sys.exit(main())