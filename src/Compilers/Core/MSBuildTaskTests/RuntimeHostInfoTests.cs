// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests;

public sealed class RuntimeHostInfoTests(ITestOutputHelper output) : TestBase
{
    private readonly ITestOutputHelper _output = output;

    /// <summary>
    /// Normalizes a path by resolving any symbolic links or subst drives (on Windows).
    /// This ensures paths can be compared even when one is a subst drive (e.g., T:\ -> D:\a\_work\1\s\artifacts\tmp\Debug).
    /// </summary>
    private string NormalizePath(string path)
    {
        try
        {
            // Our test infra uses `subst` which the .NET Core's implementation of `ResolveLinkTarget` wouldn't resolve,
            // hence we always use our Win32 polyfill on Windows to ensure paths are fully normalized and can be compared in tests.
            var resolvedPath = PlatformInformation.IsWindows
                ? NativeMethods.ResolveLinkTargetWin32(path, returnFinalTarget: true)
                : File.ResolveLinkTarget(path, returnFinalTarget: true);
            if (resolvedPath != null)
            {
                return resolvedPath.FullName;
            }
        }
        catch (Exception ex)
        {
            // If resolution fails, use the original path
            _output.WriteLine($"Failed to resolve symbolic link for path '{path}': {ex}");
        }

        return path;
    }

    [Fact, WorkItem("https://github.com/dotnet/msbuild/issues/12669")]
    public void DotNetInPath()
    {
        using var tempRoot = new TempRoot();
        var testDir = tempRoot.CreateDirectory();
        var globalDotNetDir = testDir.CreateDirectory("global-dotnet");
        var globalDotNetExe = globalDotNetDir.CreateFile($"dotnet{PlatformInformation.ExeExtension}");

        var result = ApplyEnvironmentVariables(
        [
            new("PATH", globalDotNetDir.Path),
            new(RuntimeHostInfo.DotNetHostPathEnvironmentName, ""),
            new(RuntimeHostInfo.DotNetExperimentalHostPathEnvironmentName, ""),
        ],
        () => RuntimeHostInfo.GetToolDotNetRoot(_output.WriteLine));

        Assert.NotNull(result);
        AssertEx.Equal(NormalizePath(globalDotNetDir.Path), result);
    }

    [Fact, WorkItem("https://github.com/dotnet/msbuild/issues/12669")]
    public void DotNetInPath_None()
    {
        var result = ApplyEnvironmentVariables(
        [
            new("PATH", ""),
            new(RuntimeHostInfo.DotNetHostPathEnvironmentName, ""),
            new(RuntimeHostInfo.DotNetExperimentalHostPathEnvironmentName, ""),
        ],
        () => RuntimeHostInfo.GetToolDotNetRoot(_output.WriteLine));

        Assert.Null(result);
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

        var result = ApplyEnvironmentVariables(
        [
            new("PATH", binDir.Path),
            new(RuntimeHostInfo.DotNetHostPathEnvironmentName, ""),
            new(RuntimeHostInfo.DotNetExperimentalHostPathEnvironmentName, ""),
        ],
        () => RuntimeHostInfo.GetToolDotNetRoot(_output.WriteLine));

        Assert.NotNull(result);
        AssertEx.Equal(NormalizePath(globalDotNetDir.Path), result);
    }
}

#if !NET
file static class TestNativeMethods
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
