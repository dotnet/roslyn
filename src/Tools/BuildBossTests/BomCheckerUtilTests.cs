// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BuildBoss;
using Xunit;

namespace BuildBoss.UnitTests;

public sealed class BomCheckerUtilTests
{
    [Fact]
    public void CheckFilesUsesRoslynEditorConfigMatchingAndPrecedence()
    {
        using var repository = new TemporaryRepository();
        repository.WriteText(".editorconfig", """
            root = true
            [*.{cs,vb}]
            charset = utf-8-bom
            [src/{A,B}/file[0-9].cs]
            charset = utf-8
            [src/B/file2.cs]
            charset = utf-8-bom
            """);
        repository.WriteBytes("src/A/file1.cs", withBom: false);
        repository.WriteBytes("src/B/file2.cs", withBom: true);
        repository.WriteBytes("ordinary.cs", withBom: true);

        var output = new StringWriter();
        var succeeded = new BomCheckerUtil(repository.Path).CheckFiles(
            new[] { "src/A/file1.cs", "src/B/file2.cs", "ordinary.cs" },
            output);

        Assert.True(succeeded, output.ToString());
    }

    [Fact]
    public void CheckFilesHonorsNestedRootAndRazorStyleOverrides()
    {
        using var repository = new TemporaryRepository();
        repository.WriteText(".editorconfig", """
            root = true
            [*]
            charset = utf-8-bom
            """);
        repository.WriteText("src/Razor/.editorconfig", """
            root = true
            [*.{cs,csx,vb,vbx}]
            charset = utf-8-bom
            [*.json]
            charset = utf-8
            [*.sh]
            charset = utf-8
            """);
        repository.WriteBytes("src/Razor/Test.cs", withBom: true);
        repository.WriteBytes("src/Razor/settings.json", withBom: false);
        repository.WriteText("src/Razor/script.sh", "#!/usr/bin/env bash\n", withBom: false);

        var output = new StringWriter();
        var succeeded = new BomCheckerUtil(repository.Path).CheckFiles(
            new[] { "src/Razor/Test.cs", "src/Razor/settings.json", "src/Razor/script.sh" },
            output);

        Assert.True(succeeded, output.ToString());
    }

    [Fact]
    public void CheckFilesDistinguishesMissingCharsetFromExplicitUnsetForSdkSyncedSources()
    {
        using var repository = new TemporaryRepository();
        repository.WriteText(".editorconfig", "root = true\n");
        repository.WriteText("src/Workspaces/CSharp/Portable/SyncedSource/FileBasedPrograms/.editorconfig", """
            root = true
            [Unset.cs]
            charset = unset
            """);
        repository.WriteBytes("src/Workspaces/CSharp/Portable/SyncedSource/FileBasedPrograms/Unset.cs", withBom: false);
        repository.WriteBytes("src/Workspaces/CSharp/Portable/SyncedSource/FileBasedPrograms/Absent.cs", withBom: false);

        var output = new StringWriter();
        var succeeded = new BomCheckerUtil(repository.Path).CheckFiles(
            new[]
            {
                "src/Workspaces/CSharp/Portable/SyncedSource/FileBasedPrograms/Unset.cs",
                "src/Workspaces/CSharp/Portable/SyncedSource/FileBasedPrograms/Absent.cs",
            },
            output);

        Assert.False(succeeded, output.ToString());
        Assert.Equal(
            "src/Workspaces/CSharp/Portable/SyncedSource/FileBasedPrograms/Absent.cs: expected utf-8-bom" + Environment.NewLine,
            output.ToString());
    }

    [Fact]
    public void CheckFilesRejectsBomPrefixedShebangAndUnsupportedCharset()
    {
        using var repository = new TemporaryRepository();
        repository.WriteText(".editorconfig", """
            root = true
            [*.sh]
            charset = utf-8
            [*.txt]
            charset = utf-16
            """);
        repository.WriteText("script.sh", "#!/usr/bin/env bash\n", withBom: true);
        repository.WriteText("unsupported.txt", "text", withBom: false);

        var output = new StringWriter();
        var succeeded = new BomCheckerUtil(repository.Path).CheckFiles(new[] { "script.sh", "unsupported.txt" }, output);

        Assert.False(succeeded);
        Assert.Contains("script.sh: expected shebang at byte zero (no BOM)", output.ToString());
        Assert.Contains("unsupported.txt: unsupported charset 'utf-16'", output.ToString());
    }

    [Fact]
    public void CheckEnumeratesTrackedFilesAndReturnsFailureForBomViolation()
    {
        using var repository = new TemporaryRepository();
        repository.WriteText(".editorconfig", "root = true\n[*.cs]\ncharset = utf-8-bom\n");
        repository.WriteText("tracked.cs", "class C { }\n", withBom: false);
        repository.WriteText("untracked.cs", "class U { }\n", withBom: false);
        repository.RunGit("add", ".editorconfig", "tracked.cs");

        var output = new StringWriter();
        var succeeded = new BomCheckerUtil(repository.Path).Check(output);

        Assert.False(succeeded);
        Assert.Contains("tracked.cs: expected utf-8-bom", output.ToString());
        Assert.DoesNotContain("untracked.cs", output.ToString());
    }

    [Fact]
    public void BomOnlyModeReturnsNonzeroForTrackedBomViolation()
    {
        using var repository = new TemporaryRepository();
        repository.WriteText(".editorconfig", "root = true\n[*.cs]\ncharset = utf-8-bom\n");
        repository.WriteText("tracked.cs", "class C { }\n", withBom: false);
        repository.RunGit("add", ".editorconfig", "tracked.cs");

        var exitCode = Program.Main(new[] { "-r", repository.Path, "--check-bom-only" });

        Assert.Equal(1, exitCode);
        Assert.Contains(
            "tracked.cs: expected utf-8-bom",
            File.ReadAllText(System.IO.Path.Combine(repository.Path, "artifacts", "log", "Debug", "BuildBoss", "BOM.log")));
    }

    [Fact]
    public void CheckReportsGitFailureAndStandardError()
    {
        using var repository = new TemporaryRepository(initializeGit: false);

        var output = new StringWriter();
        var succeeded = new BomCheckerUtil(repository.Path).Check(output);

        Assert.False(succeeded);
        Assert.Contains("git ls-files failed with exit code", output.ToString());
        Assert.Contains("not a git repository", output.ToString());
    }

    private sealed class TemporaryRepository : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        public TemporaryRepository(bool initializeGit = true)
        {
            Directory.CreateDirectory(Path);
            if (initializeGit)
            {
                RunGit("init", "--quiet");
            }
        }

        public void WriteText(string relativePath, string text, bool withBom = false)
        {
            var path = GetPath(relativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: withBom);
            var content = encoding.GetBytes(text);
            File.WriteAllBytes(path, withBom ? encoding.GetPreamble().Concat(content).ToArray() : content);
        }

        public void WriteBytes(string relativePath, bool withBom)
            => WriteText(relativePath, "content", withBom);

        public void RunGit(params string[] arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = string.Join(" ", arguments.Select(QuoteArgument)),
                WorkingDirectory = Path,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            using var process = Process.Start(startInfo)!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"git {string.Join(" ", arguments)} failed: {output}{error}");
        }

        public void Dispose()
        {
            foreach (var filePath in Directory.GetFiles(Path, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
            }

            Directory.Delete(Path, recursive: true);
        }

        private string GetPath(string relativePath)
            => System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));

        private static string QuoteArgument(string argument)
            => argument.Contains(" ") ? $"\"{argument}\"" : argument;
    }
}
