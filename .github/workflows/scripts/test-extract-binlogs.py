import os, shutil, stat, subprocess, sys, tempfile, zipfile

SCRIPT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "extract-binlogs.py")


def run(archive, dest, prefix="1", budget=10_000_000):
    return subprocess.run(
        [sys.executable, SCRIPT, archive, dest, prefix, str(budget)],
        capture_output=True, text=True)


def fresh(tmp, name):
    d = os.path.join(tmp, name)
    os.makedirs(d, exist_ok=True)
    return d


def main():
    tmp = tempfile.mkdtemp()
    failures = []

    def check(label, cond, detail=""):
        print(("PASS  " if cond else "FAIL  ") + label + ("  " + detail if detail and not cond else ""))
        if not cond:
            failures.append(label)

    # 1. Happy path: nested binlogs plus unrelated files.
    a = os.path.join(tmp, "ok.zip")
    with zipfile.ZipFile(a, "w") as z:
        z.writestr("logs/Build.binlog", b"AAAA")
        z.writestr("logs/nested/deep/Other.BINLOG", b"BBBBBB")
        z.writestr("logs/console.txt", b"noise")
    d = fresh(tmp, "ok")
    r = run(a, d)
    names = sorted(os.listdir(d))
    check("happy path extracts only binlogs", r.returncode == 0 and r.stdout.strip() == "2 10", r.stdout + r.stderr)
    check("happy path uses generated names", names == ["1_0.binlog", "1_1.binlog"], str(names))
    check("happy path is flat (no archive dirs)", not any(os.path.isdir(os.path.join(d, n)) for n in names))

    # 2. Path traversal anywhere in the archive is rejected wholesale.
    a = os.path.join(tmp, "traversal.zip")
    with zipfile.ZipFile(a, "w") as z:
        z.writestr("good.binlog", b"AAAA")
        z.writestr("../../evil.binlog", b"BBBB")
    d = fresh(tmp, "traversal")
    r = run(a, d)
    check("traversal entry rejected", r.returncode != 0 and "unsafe path" in r.stderr)
    check("traversal leaves nothing outside dest", not os.path.exists(os.path.join(tmp, "evil.binlog")))

    # 3. Absolute path rejected.
    a = os.path.join(tmp, "abs.zip")
    with zipfile.ZipFile(a, "w") as z:
        z.writestr("/etc/evil.binlog", b"AAAA")
    r = run(a, fresh(tmp, "abs"))
    check("absolute path rejected", r.returncode != 0 and "unsafe path" in r.stderr)

    # 4. Windows drive-relative path rejected.
    a = os.path.join(tmp, "drive.zip")
    with zipfile.ZipFile(a, "w") as z:
        z.writestr("c:evil.binlog", b"AAAA")
    r = run(a, fresh(tmp, "drive"))
    check("drive-relative path rejected", r.returncode != 0 and "unsafe path" in r.stderr)

    # 5. Symlink entry rejected (this is the link-redirect attack).
    a = os.path.join(tmp, "symlink.zip")
    with zipfile.ZipFile(a, "w") as z:
        info = zipfile.ZipInfo("link.binlog")
        info.external_attr = (stat.S_IFLNK | 0o777) << 16
        z.writestr(info, b"/etc/passwd")
    r = run(a, fresh(tmp, "symlink"))
    check("symlink entry rejected", r.returncode != 0 and "unsupported type" in r.stderr)

    # 6. Budget enforced on actual bytes written, not declared metadata.
    a = os.path.join(tmp, "bomb.zip")
    with zipfile.ZipFile(a, "w", zipfile.ZIP_DEFLATED) as z:
        z.writestr("huge.binlog", b"\0" * (5 * 1024 * 1024))
    d = fresh(tmp, "bomb")
    r = run(a, d, budget=1024)
    check("over-budget extraction fails", r.returncode != 0 and "budget" in r.stderr)
    partial = os.path.join(d, "1_0.binlog")
    check("over-budget partial file stays under budget+chunk",
          not os.path.exists(partial) or os.path.getsize(partial) <= 1024 + (1 << 20))

    # 7. Corrupt archive fails rather than silently extracting nothing.
    a = os.path.join(tmp, "corrupt.zip")
    with open(a, "wb") as f:
        f.write(b"this is not a zip file at all")
    r = run(a, fresh(tmp, "corrupt"))
    check("corrupt archive rejected", r.returncode != 0)

    # 8. Archive with no binlogs reports zero (caller treats it as a skip).
    a = os.path.join(tmp, "empty.zip")
    with zipfile.ZipFile(a, "w") as z:
        z.writestr("logs/console.txt", b"noise")
    r = run(a, fresh(tmp, "empty"))
    check("no-binlog archive reports 0", r.returncode == 0 and r.stdout.strip() == "0 0", r.stdout + r.stderr)

    # 9. Same basename in different folders must not collide.
    a = os.path.join(tmp, "collide.zip")
    with zipfile.ZipFile(a, "w") as z:
        z.writestr("x/Build.binlog", b"AAAA")
        z.writestr("y/Build.binlog", b"BBBB")
    d = fresh(tmp, "collide")
    r = run(a, d)
    check("same basenames do not collide", r.returncode == 0 and len(os.listdir(d)) == 2, str(os.listdir(d)))

    # 10. Distinct prefixes keep artifacts separate in a shared directory.
    d = fresh(tmp, "shared")
    r1 = run(a, d, prefix="1")
    r2 = run(a, d, prefix="2")
    check("distinct prefixes coexist",
          r1.returncode == 0 and r2.returncode == 0 and len(os.listdir(d)) == 4, str(os.listdir(d)))

    shutil.rmtree(tmp, ignore_errors=True)
    print()
    if failures:
        print(f"{len(failures)} FAILED: {failures}")
        sys.exit(1)
    print("all extractor tests passed")


main()
