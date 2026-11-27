// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class ProjectDependencyHelperTests : IDisposable
{
    private readonly TempRoot _tempRoot = new();

    public void Dispose()
        => _tempRoot.Dispose();

    [Fact]
    public void NeedsRestore_MissingAssetsFile()
    {
        var projectAssetsPath = Path.Combine(_tempRoot.CreateDirectory().Path, "missing.assets.json");

        Assert.True(NeedsRestore(projectAssetsPath, ("Package", "1.0.0")));
    }

    [Fact]
    public void NeedsRestore_NoPackageReferencesDoesNotParseAssetsFile()
    {
        var projectAssetsPath = WriteAssetsFile("not json");

        Assert.False(NeedsRestore(projectAssetsPath));
    }

    [Theory]
    [InlineData("""{"libraries":{"Package/1.0.0":{}}""")]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("\u0001\u0002\u0003")]
    [InlineData("""{"version":3,"libraries":"unterminated""")]
    public void NeedsRestore_UnreadableAssetsFile(string contents)
    {
        // The lock file model this replaced reported no resolved packages when it failed to parse, so an
        // unreadable file has to report a restore rather than failing the project load.
        var projectAssetsPath = WriteAssetsFile(contents);

        Assert.True(NeedsRestore(projectAssetsPath, ("Package", "1.0.0")));
    }

    [Fact]
    public void NeedsRestore_UnreadableAssetsFileLogsVersion()
    {
        var projectAssetsPath = WriteAssetsFile("""{"version":3,"libraries":{"Package/1.0.0":{}}""");
        var logger = new TestLogger();
        var expectedMessage = string.Format(
            LanguageServerResources.Failed_to_read_project_assets_file_0_version_1_2,
            projectAssetsPath,
            "3",
            string.Empty);

        Assert.True(NeedsRestore(projectAssetsPath, logger, ("Package", "1.0.0")));

        var message = Assert.Single(logger.Messages);
        Assert.StartsWith(expectedMessage, message);
    }

    [Fact]
    public void NeedsRestore_UnreadableAssetsFileWithoutVersionLogsUnknown()
    {
        var projectAssetsPath = WriteAssetsFile("""{"libraries":{"Package/1.0.0":{}}""");
        var logger = new TestLogger();
        var expectedMessage = string.Format(
            LanguageServerResources.Failed_to_read_project_assets_file_0_version_1_2,
            projectAssetsPath,
            ProjectDependencyHelper.UnknownVersion,
            string.Empty);

        Assert.True(NeedsRestore(projectAssetsPath, logger, ("Package", "1.0.0")));

        var message = Assert.Single(logger.Messages);
        Assert.StartsWith(expectedMessage, message);
    }

    [Theory]
    [InlineData("Package", "1.0.0", "Package/1.0.0", false)]
    [InlineData("PACKAGE", "1.0.0", "package/1.0.0", false)]
    [InlineData("Package", "[1.0.0]", "Package/1.0.0", false)]
    [InlineData("Package", "[1.0.0,2.0.0)", "Package/1.5.0", false)]
    [InlineData("Package", "(1.0.0,2.0.0)", "Package/1.0.0", true)]
    [InlineData("Package", "[2.0.0]", "Package/1.0.0", true)]
    [InlineData("Package", "not a range", "Package/1.0.0", false)]
    [InlineData("Other", "1.0.0", "Package/1.0.0", true)]
    public void NeedsRestore_PackageNameAndVersion(
        string packageName,
        string versionRange,
        string restoredLibrary,
        bool expectedNeedsRestore)
    {
        var projectAssetsPath = WriteAssetsFile($"{{\"version\":3,\"libraries\":{{\"{restoredLibrary}\":{{\"type\":\"package\"}}}}}}");

        Assert.Equal(expectedNeedsRestore, NeedsRestore(projectAssetsPath, (packageName, versionRange)));
    }

    [Fact]
    public void NeedsRestore_UsesTopLevelLibrariesFromRealisticAssetsFile()
    {
        var projectAssetsPath = WriteAssetsFile("""
                {
                  "version": 3,
                  "targets": {
                    "net10.0": {
                      "Misleading.Package/9.0.0": {
                        "type": "package",
                        "compile": {}
                      }
                    }
                  },
                  "libraries": {
                    "Newtonsoft.Json/13.0.3": {
                      "sha512": "hash",
                      "type": "package",
                      "path": "newtonsoft.json/13.0.3",
                      "files": [
                        ".nupkg.metadata",
                        "lib/net6.0/Newtonsoft.Json.dll"
                      ]
                    },
                    "Microsoft.CodeAnalysis.Common/5.0.0": {
                      "sha512": "hash",
                      "type": "package",
                      "path": "microsoft.codeanalysis.common/5.0.0",
                      "files": []
                    }
                  },
                  "projectFileDependencyGroups": {
                    "net10.0": [
                      "Newtonsoft.Json >= 13.0.0"
                    ]
                  },
                  "packageFolders": {
                    "C:\\Users\\test\\.nuget\\packages\\": {}
                  },
                  "project": {
                    "version": "1.0.0",
                    "restore": {
                      "projectName": "TestProject"
                    }
                  }
                }
                """);

        Assert.False(NeedsRestore(
            projectAssetsPath,
            ("newtonsoft.json", "[13.0.0,14.0.0)"),
            ("Microsoft.CodeAnalysis.Common", "[5.0.0]")));
    }

    [Fact]
    public void NeedsRestore_HandlesLibraryNameAcrossBufferBoundary()
    {
        // Large enough that the library name lands in a later read, but still smaller than the read buffer.
        var padding = new string('x', 12 * 1024);
        var projectAssetsPath = WriteAssetsFile($"{{\"padding\":\"{padding}\",\"libraries\":{{\"Package/1.0.0\":{{}}}}}}");

        Assert.False(NeedsRestore(projectAssetsPath, ("Package", "1.0.0")));
    }

    [Fact]
    public void NeedsRestore_HandlesTokenLargerThanBuffer()
    {
        // A single token too large for the initial buffer, which forces the read buffer to grow.
        var padding = new string('x', 64 * 1024);
        var projectAssetsPath = WriteAssetsFile($"{{\"padding\":\"{padding}\",\"libraries\":{{\"Package/1.0.0\":{{}}}}}}");

        Assert.False(NeedsRestore(projectAssetsPath, ("Package", "1.0.0")));
    }

    [Fact]
    public void NeedsRestore_HandlesLibraryNameLargerThanBuffer()
    {
        // The grown buffer has to preserve the library key itself, not just skip past oversized values.
        var packageName = new string('x', 64 * 1024);
        var projectAssetsPath = WriteAssetsFile($"{{\"libraries\":{{\"{packageName}/1.0.0\":{{}}}}}}");

        Assert.False(NeedsRestore(projectAssetsPath, (packageName, "1.0.0")));
    }

    [Fact]
    public void NeedsRestore_SkipsByteOrderMark()
    {
        var file = _tempRoot.CreateFile();
        File.WriteAllBytes(
            file.Path,
            [.. Encoding.UTF8.Preamble, .. """{"version":3,"libraries":{"Package/1.0.0":{"type":"package"}}}"""u8]);

        Assert.False(NeedsRestore(file.Path, ("Package", "1.0.0")));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("\"nope\"")]
    [InlineData("12")]
    public void NeedsRestore_LibrariesIsNotAnObject(string librariesValue)
    {
        // Valid JSON of an unexpected shape. LockFileFormat also read these as having no libraries rather
        // than failing, so they report every reference as unresolved instead of throwing.
        var projectAssetsPath = WriteAssetsFile($"{{\"version\":3,\"libraries\":{librariesValue}}}");

        Assert.True(NeedsRestore(projectAssetsPath, ("Package", "1.0.0")));
    }

    [Fact]
    public void NeedsRestore_MatchesProjectLibraries()
    {
        // Project libraries share the "Name/Version" key shape and were matched by the lock file model too.
        var projectAssetsPath = WriteAssetsFile("""{"version":3,"libraries":{"Package/1.0.0":{"type":"project"}}}""");

        Assert.False(NeedsRestore(projectAssetsPath, ("Package", "1.0.0")));
    }

    private string WriteAssetsFile(string contents)
    {
        var file = _tempRoot.CreateFile();
        file.WriteAllText(contents);
        return file.Path;
    }

    private static bool NeedsRestore(string projectAssetsPath, params (string Name, string VersionRange)[] packageReferences)
        => NeedsRestore(projectAssetsPath, NullLogger.Instance, packageReferences);

    private static bool NeedsRestore(string projectAssetsPath, ILogger logger, params (string Name, string VersionRange)[] packageReferences)
        => ProjectDependencyHelper.TestAccessor.CheckProjectAssetsForUnresolvedDependencies(
            projectAssetsPath, packageReferences, logger);

    private sealed class TestLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }
}
