// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests;

public sealed class RuntimeHostInfoTests(ITestOutputHelper output) : TestBase
{
    private readonly ITestOutputHelper _output = output;

    [Fact, WorkItem("https://github.com/dotnet/msbuild/issues/12669")]
    public void DotNetInPath()
    {
        using var tempRoot = new TempRoot();
        var testDir = tempRoot.CreateDirectory();
        var globalDotNetDir = testDir.CreateDirectory("global-dotnet");
        var globalDotNetExe = globalDotNetDir.CreateFile($"dotnet{PlatformInformation.ExeExtension}");

        ApplyEnvironmentVariables(
        [
            new("PATH", globalDotNetDir.Path),
            new(RuntimeHostInfo.DotNetHostPathEnvironmentName, ""),
            new(RuntimeHostInfo.DotNetExperimentalHostPathEnvironmentName, ""),
        ],
        () =>
        {
            AssertEx.Equal(globalDotNetDir.Path, RuntimeHostInfo.GetToolDotNetRoot(_output.WriteLine));
        });
    }

    [Fact, WorkItem("https://github.com/dotnet/msbuild/issues/12669")]
    public void DotNetInPath_None()
    {
        ApplyEnvironmentVariables(
        [
            new("PATH", ""),
            new(RuntimeHostInfo.DotNetHostPathEnvironmentName, ""),
            new(RuntimeHostInfo.DotNetExperimentalHostPathEnvironmentName, ""),
        ],
        () =>
        {
            Assert.Null(RuntimeHostInfo.GetToolDotNetRoot(_output.WriteLine));
        });
    }

    [Fact, WorkItem("https://github.com/dotnet/msbuild/issues/12669")]
    public void DotNetInPath_Symlinked()
    {
        using var tempRoot = new TempRoot();
        var testDir = tempRoot.CreateDirectory();
        var globalDotNetDir = testDir.CreateDirectory("global-dotnet");
        var globalDotNetExe = globalDotNetDir.CreateFile($"dotnet{PlatformInformation.ExeExtension}");
        var binDir = testDir.CreateDirectory("bin");
        var symlinkPath = Path.Combine(binDir.Path, $"dotnet{PlatformInformation.ExeExtension}");

        // Create symlink from binDir to the actual dotnet executable
        File.CreateSymbolicLink(path: symlinkPath, pathToTarget: globalDotNetExe.Path);

        ApplyEnvironmentVariables(
        [
            new("PATH", binDir.Path),
            new(RuntimeHostInfo.DotNetHostPathEnvironmentName, ""),
            new(RuntimeHostInfo.DotNetExperimentalHostPathEnvironmentName, ""),
        ],
        () =>
        {
            AssertEx.Equal(globalDotNetDir.Path, RuntimeHostInfo.GetToolDotNetRoot(_output.WriteLine));
        });
    }
}

#if !NET
file static class NativeMethods
{
    extension(File)
    {
        /// <remarks>
        /// Only used by tests currently (might need some hardening if this is to be used by production code).
        /// </remarks>
        public static void CreateSymbolicLink(string path, string pathToTarget)
        {
            bool ok = CreateSymbolicLink(
                lpSymlinkFileName: path,
                lpTargetFileName: pathToTarget,
                dwFlags: SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE);
            if (!ok)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateSymbolicLink(
        string lpSymlinkFileName,
        string lpTargetFileName,
        uint dwFlags);

    private const uint SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 0x2;
}
#endif
