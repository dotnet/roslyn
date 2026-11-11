// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.FileSystem
{
#if NET
    public class TempPathUtilitiesTests : IClassFixture<TempPathUtilitiesTests.TempPathTestFixture>
    {
        private const string TempPathResultPrefix = "TempPath:";
        private const string TempPathTestHost = """
            #:include TempPathUtilities.cs

            Console.WriteLine($"TempPath:{Roslyn.Utilities.TempPathUtilities.GetTempPath()}");
            """;

        private readonly TempPathTestFixture _fixture;

        public TempPathUtilitiesTests(TempPathTestFixture fixture)
        {
            _fixture = fixture;
        }

        public sealed class TempPathTestFixture : IDisposable
        {
            private readonly TempRoot _tempRoot = new();

            public string HostPath { get; }
            public string HostDirectory { get; }

            public TempPathTestFixture()
            {
                var hostDirectory = _tempRoot.CreateDirectory();
                HostDirectory = hostDirectory.Path;
                HostPath = hostDirectory.CreateFile("TempPathTestHost.cs").WriteAllText(TempPathTestHost).Path;
                hostDirectory.CreateFile("TempPathUtilities.cs").WriteAllText(readResource("TempPathUtilities.cs"));

                static string readResource(string name)
                {
                    using var stream = typeof(TempPathUtilitiesTests).Assembly.GetManifestResourceStream(name)
                        ?? throw new InvalidOperationException($"Resource '{name}' not found.");
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
            }

            public void Dispose()
                => _tempRoot.Dispose();
        }

        private const UnixFileMode Mode0700 = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
        private const UnixFileMode Mode0777 =
            Mode0700 | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

        /// <summary>
        /// Get a ProcessResult from calling 'TempPathUtilities.GetTempPath()' in a subprocess.
        /// </summary>
        /// <remarks>
        /// GetTempPath behavior depends on environment variables.
        /// Some tests are run using a subprocess, so we can observe behavior with
        /// different env vars, without needing to modify the current process env.
        /// </remarks>
        private ProcessResult GetTempPathSubprocessResult(string xdgCacheHome, string home)
        {
            return ProcessUtilities.Run(
                DotNetCoreSdk.ExePath!,
                $"run --file \"{_fixture.HostPath}\" --verbosity quiet -e \"XDG_CACHE_HOME={xdgCacheHome}\" -e \"HOME={home}\"",
                workingDirectory: _fixture.HostDirectory);
        }

        private string GetTempPathSubprocessValue(string xdgCacheHome, string home)
        {
            var result = GetTempPathSubprocessResult(xdgCacheHome, home);
            Assert.True(result.ExitCode == 0, result.ToString());
            return Assert.Single(result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
                line => line.StartsWith(TempPathResultPrefix, StringComparison.Ordinal))[TempPathResultPrefix.Length..];
        }

        [ConditionalFact(typeof(LinuxOnly), typeof(DotNetCoreSdk.IsAvailable))]
        [SupportedOSPlatform("linux")]
        public void GetTempPath_Linux_UsesXdgCacheHomeInSubprocess()
        {
            using var temp = new TempRoot();
            var testDirectory = temp.CreateDirectory();
            string xdgCacheHome = Path.Join(testDirectory.Path, "xdg", ".cache");

            string tempPath = GetTempPathSubprocessValue(xdgCacheHome, home: "");

            Assert.Equal(xdgCacheHome, tempPath);
            Assert.True(Directory.Exists(tempPath));
            Assert.Equal(Mode0700, File.GetUnixFileMode(tempPath));
        }

        [ConditionalFact(typeof(LinuxOnly), typeof(DotNetCoreSdk.IsAvailable))]
        [SupportedOSPlatform("linux")]
        public void GetTempPath_Linux_UsesHomeFallbackInSubprocess()
        {
            using var temp = new TempRoot();
            var testDirectory = temp.CreateDirectory();
            string home = Path.Join(testDirectory.Path, "home");
            string expectedPath = Path.Join(home, ".cache");

            string tempPath = GetTempPathSubprocessValue(xdgCacheHome: "", home);

            Assert.Equal(expectedPath, tempPath);
            Assert.True(Directory.Exists(tempPath));
            Assert.Equal(Mode0700, File.GetUnixFileMode(tempPath));
        }

        [ConditionalFact(typeof(LinuxOnly), typeof(DotNetCoreSdk.IsAvailable))]
        [SupportedOSPlatform("linux")]
        public void GetTempPath_Linux_PreservesExistingXdgCacheHomePermissionsInSubprocess()
        {
            using var temp = new TempRoot();
            var testDirectory = temp.CreateDirectory();
            string xdgCacheHome = Directory.CreateDirectory(Path.Join(testDirectory.Path, "xdg", ".cache")).FullName;
            File.SetUnixFileMode(xdgCacheHome, Mode0777);

            string tempPath = GetTempPathSubprocessValue(xdgCacheHome, home: "");

            Assert.Equal(xdgCacheHome, tempPath);
            Assert.Equal(Mode0777, File.GetUnixFileMode(tempPath));
        }

        [ConditionalFact(typeof(LinuxOnly), typeof(DotNetCoreSdk.IsAvailable))]
        [SupportedOSPlatform("linux")]
        public void GetTempPath_Linux_PreservesExistingParentPermissionsInSubprocess()
        {
            using var temp = new TempRoot();
            var testDirectory = temp.CreateDirectory();
            string parent = Directory.CreateDirectory(Path.Join(testDirectory.Path, "parent")).FullName;
            const UnixFileMode parentMode = Mode0700 | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;
            File.SetUnixFileMode(parent, parentMode);
            string xdgCacheHome = Path.Join(parent, ".cache");

            string tempPath = GetTempPathSubprocessValue(xdgCacheHome, home: "");

            Assert.Equal(xdgCacheHome, tempPath);
            Assert.Equal(parentMode, File.GetUnixFileMode(parent));
            Assert.Equal(Mode0700, File.GetUnixFileMode(tempPath));
        }

        [ConditionalFact(typeof(LinuxOnly), typeof(DotNetCoreSdk.IsAvailable))]
        [SupportedOSPlatform("linux")]
        public void GetTempPath_Linux_UsesExistingSymbolicLinkInSubprocess()
        {
            using var temp = new TempRoot();
            var testDirectory = temp.CreateDirectory();
            string target = Directory.CreateDirectory(Path.Join(testDirectory.Path, "target")).FullName;
            File.SetUnixFileMode(target, Mode0777);
            string xdgCacheHome = Path.Join(testDirectory.Path, ".cache");
            Directory.CreateSymbolicLink(xdgCacheHome, target);

            string tempPath = GetTempPathSubprocessValue(xdgCacheHome, home: "");

            Assert.Equal(xdgCacheHome, tempPath);
            Assert.Equal(target, new DirectoryInfo(xdgCacheHome).LinkTarget);
            Assert.Equal(Mode0777, File.GetUnixFileMode(target));
        }

        [ConditionalFact(typeof(LinuxOnly), typeof(DotNetCoreSdk.IsAvailable))]
        [SupportedOSPlatform("linux")]
        public void GetTempPath_Linux_IntermediatePathIsFileFailsInSubprocess()
        {
            using var temp = new TempRoot();
            var testDirectory = temp.CreateDirectory();
            string parent = Path.Join(testDirectory.Path, "parent");
            File.WriteAllText(parent, "contents");
            string xdgCacheHome = Path.Join(parent, ".cache");

            var result = GetTempPathSubprocessResult(xdgCacheHome, home: "");

            Assert.NotEqual(0, result.ExitCode);
            Assert.NotEmpty(result.Errors);
            Assert.Equal("contents", File.ReadAllText(parent));
            Assert.False(Directory.Exists(xdgCacheHome));
        }

        [ConditionalFact(typeof(LinuxOnly), typeof(DotNetCoreSdk.IsAvailable))]
        [SupportedOSPlatform("linux")]
        public void GetTempPath_Linux_EmptyXdgCacheHomeAndHomeFailsInSubprocess()
        {
            var result = GetTempPathSubprocessResult(xdgCacheHome: "", home: "");

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains(nameof(InvalidOperationException), result.Errors);
            Assert.DoesNotContain(TempPathResultPrefix, result.Output);
        }

        [ConditionalFact(typeof(LinuxOnly))]
        [SupportedOSPlatform("linux")]
        public void GetTempPath_Linux_CreatesDirectoryWithCorrectPermissions()
        {
            var root = Directory.CreateTempSubdirectory();
            try
            {
                string xdgCacheHome = Path.Join(root.FullName, ".cache");
                string? tempPath = TempPathUtilities.CreateUserTempDirectoryLinuxCore(xdgCacheHome: xdgCacheHome);
                Assert.Equal(xdgCacheHome, tempPath);
                Assert.Equal(Mode0700, new DirectoryInfo(tempPath).UnixFileMode);
            }
            finally
            {
                root.Delete(recursive: true);
            }
        }

        [ConditionalFact(typeof(LinuxOnly))]
        [SupportedOSPlatform("linux")]
        public void GetTempPath_Linux_CreatesNestedDirectories()
        {
            var root = Directory.CreateTempSubdirectory();
            try
            {
                Assert.True(root.Exists); // 'CreateUserTempDirectoryLinuxCore' does not create 'root' itself

                // Ensure that the parent directories, have the same permissions as a 'Directory.CreateDirectory(string)' call.
                DirectoryInfo baseline = Directory.CreateDirectory(Path.Join(root.FullName, "baseline"));

                string parentDir = Path.Join(root.FullName, "parent");
                string innerDir = Path.Join(parentDir, "inner");
                string xdgCacheHome = Path.Join(innerDir, ".cache");
                string tempDir = TempPathUtilities.CreateUserTempDirectoryLinuxCore(xdgCacheHome: xdgCacheHome);
                Assert.Equal(xdgCacheHome, tempDir);
                Assert.Equal(Mode0700, new DirectoryInfo(tempDir).UnixFileMode);
                Assert.Equal(baseline.UnixFileMode, new DirectoryInfo(parentDir).UnixFileMode);
                Assert.Equal(baseline.UnixFileMode, new DirectoryInfo(innerDir).UnixFileMode);
            }
            finally
            {
                root.Delete(recursive: true);
            }
        }

        [ConditionalTheory(typeof(LinuxOnly))]
        [SupportedOSPlatform("linux")]
        [InlineData("/")]
        [InlineData("//")]
        [InlineData("///")]
        public void GetTempPath_Linux_CreatesNestedDirectories_TrailingSeparator(string trailing)
        {
            var root = Directory.CreateTempSubdirectory();
            try
            {
                Assert.True(root.Exists); // 'CreateUserTempDirectoryLinuxCore' does not create 'root' itself

                // Ensure that the parent directories, have the same permissions as a 'Directory.CreateDirectory(string)' call.
                DirectoryInfo baseline = Directory.CreateDirectory(Path.Join(root.FullName, "baseline"));

                string parentDir = Path.Join(root.FullName, "parent");
                string innerDir = Path.Join(parentDir, "inner");
                // Ensure correct permissions are used even when the path has a trailing separator.
                string xdgCacheHome = Path.Join(innerDir, ".cache") + trailing;
                string tempDir = TempPathUtilities.CreateUserTempDirectoryLinuxCore(xdgCacheHome: xdgCacheHome);
                // The return value will be normalized to have a single trailing separator
                Assert.Equal(Path.Join(innerDir, ".cache") + "/", tempDir);
                Assert.Equal(Mode0700, new DirectoryInfo(tempDir).UnixFileMode);
                Assert.Equal(baseline.UnixFileMode, new DirectoryInfo(parentDir).UnixFileMode);
                Assert.Equal(baseline.UnixFileMode, new DirectoryInfo(innerDir).UnixFileMode);
            }
            finally
            {
                root.Delete(recursive: true);
            }
        }

        [ConditionalFact(typeof(LinuxOnly))]
        [SupportedOSPlatform("linux")]
        public void GetTempPath_Linux_CreatesNestedDirectories_TrailingSeparator_TrailingDot()
        {
            var root = Directory.CreateTempSubdirectory();
            try
            {
                Assert.True(root.Exists); // 'CreateUserTempDirectoryLinuxCore' does not create 'root' itself

                // Ensure that the parent directories, have the same permissions as a 'Directory.CreateDirectory(string)' call.
                DirectoryInfo baseline = Directory.CreateDirectory(Path.Join(root.FullName, "baseline"));

                string parentDir = Path.Join(root.FullName, "parent");
                string xdgCacheHome = Path.Join(parentDir, ".cache", ".");
                string tempDir = TempPathUtilities.CreateUserTempDirectoryLinuxCore(xdgCacheHome: xdgCacheHome);
                Assert.Equal(Path.Join(parentDir, ".cache"), tempDir);
                Assert.Equal(Mode0700, new DirectoryInfo(tempDir).UnixFileMode);
                Assert.Equal(baseline.UnixFileMode, new DirectoryInfo(parentDir).UnixFileMode);
            }
            finally
            {
                root.Delete(recursive: true);
            }
        }

        [ConditionalFact(typeof(LinuxOnly))]
        [SupportedOSPlatform("linux")]
        public void GetTempPath_Linux_CreatesNestedDirectories_TrailingSeparator_TrailingDotDot()
        {
            var root = Directory.CreateTempSubdirectory();
            try
            {
                Assert.True(root.Exists); // 'CreateUserTempDirectoryLinuxCore' does not create 'root' itself

                // Ensure that the parent directories, have the same permissions as a 'Directory.CreateDirectory(string)' call.
                DirectoryInfo baseline = Directory.CreateDirectory(Path.Join(root.FullName, "baseline"));

                string parentDir = Path.Join(root.FullName, "parent");
                string xdgCacheHome = Path.Join(parentDir, ".cache", "BAD", "..");
                string tempDir = TempPathUtilities.CreateUserTempDirectoryLinuxCore(xdgCacheHome: xdgCacheHome);
                Assert.Equal(Path.Join(parentDir, ".cache"), tempDir);
                Assert.Equal(Mode0700, new DirectoryInfo(tempDir).UnixFileMode);
                Assert.Equal(baseline.UnixFileMode, new DirectoryInfo(parentDir).UnixFileMode);
                Assert.False(Directory.Exists(Path.Join(parentDir, ".cache", "BAD")));
            }
            finally
            {
                root.Delete(recursive: true);
            }
        }

        [ConditionalFact(typeof(LinuxOnly))]
        [SupportedOSPlatform("linux")]
        public void GetTempPath_Linux_CacheDirectoryIsFile()
        {
            var root = Directory.CreateTempSubdirectory();
            try
            {
                string xdgCacheHome = Path.Join(root.FullName, ".cache");
                File.WriteAllText(xdgCacheHome, "contents");

                // If a file already exists at the path, we throw to match Directory.CreateDirectory() behavior
                var exception = Assert.Throws<IOException>(
                    () => TempPathUtilities.CreateUserTempDirectoryLinuxCore(xdgCacheHome: xdgCacheHome));

                const int eexist = 17;
                Assert.Equal(eexist, Assert.IsType<Win32Exception>(exception.InnerException).NativeErrorCode);
                Assert.Equal("contents", File.ReadAllText(xdgCacheHome));
            }
            finally
            {
                root.Delete(recursive: true);
            }
        }

        [ConditionalFact(typeof(LinuxOnly))]
        [SupportedOSPlatform("linux")]
        public void GetTempPath_Linux_CacheDirectoryIsDriveRoot()
        {
            string tempPath = TempPathUtilities.CreateUserTempDirectoryLinuxCore(xdgCacheHome: "/");

            Assert.Equal("/", tempPath);
            Assert.True(Directory.Exists(tempPath));
        }

        [ConditionalFact(typeof(LinuxOnly))]
        [SupportedOSPlatform("linux")]
        public void GetTempPath_Linux_DoesNotUseRelativeXdgCacheHome()
        {
            // When $XDG_CACHE_HOME is not an absolute path, its value is ignored and the default fallback of '$HOME/.cache' is used instead
            string tempPath = TempPathUtilities.CreateUserTempDirectoryLinuxCore(xdgCacheHome: ".mycache");
            string expected = Path.Join(Environment.GetEnvironmentVariable("HOME"), ".cache");
            Assert.Equal(expected, tempPath);
        }

        [ConditionalTheory(typeof(LinuxOnly))]
        [SupportedOSPlatform("linux")]
        [InlineData("")]
        [InlineData(null)]
        public void GetTempPath_Linux_IgnoresEmptyOrNotSetXdgCacheHome(string? xdgCacheHome)
        {
            string tempPath = TempPathUtilities.CreateUserTempDirectoryLinuxCore(xdgCacheHome);
            string expected = Path.Join(Environment.GetEnvironmentVariable("HOME"), ".cache");
            Assert.Equal(expected, tempPath);
        }

        [ConditionalFact(typeof(LinuxOnly))]
        [SupportedOSPlatform("linux")]
        public void GetTempPath_Linux_PreservesPermissionsOfExistingDirectory()
        {
            var root = Directory.CreateTempSubdirectory();
            try
            {
                string xdgCacheHome = Path.Join(root.FullName, ".cache");

                // If the directory already exists, then its mode is preserved
                var directoryInfo = new DirectoryInfo(xdgCacheHome);
                directoryInfo.Create();
                const UnixFileMode mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
                directoryInfo.UnixFileMode = mode;

                TempPathUtilities.CreateUserTempDirectoryLinuxCore(xdgCacheHome);

                directoryInfo.Refresh();
                Assert.Equal(mode, directoryInfo.UnixFileMode);
            }
            finally
            {
                root.Delete(recursive: true);
            }
        }
    }
#endif
}
