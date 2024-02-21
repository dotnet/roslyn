// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost;

internal static class MonoMSBuildDiscovery
{
    private static IEnumerable<string>? s_searchPaths;
    private static string? s_monoRuntimeExecutablePath;
    private static string? s_monoLibDirPath;
    private static string? s_monoMSBuildDirectory;

    private static IEnumerable<string> GetSearchPaths()
    {
        if (s_searchPaths == null)
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            if (path == null)
            {
                return Array.Empty<string>();
            }

            s_searchPaths = path
                .Split(Path.PathSeparator)
                .Select(p => p.Trim('"'));
        }

        return s_searchPaths;
    }

    // http://man7.org/linux/man-pages/man3/realpath.3.html
    // CharSet.Ansi is UTF8 on Unix
    [DllImport("libc", EntryPoint = "realpath", CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr Unix_realpath(string path, IntPtr buffer);

    // http://man7.org/linux/man-pages/man3/free.3.html
    [DllImport("libc", EntryPoint = "free", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Unix_free(IntPtr ptr);

    /// <summary>
    /// Returns the canonicalized absolute path from a given path, expanding symbolic links and resolving
    /// references to /./, /../ and extra '/' path characters.
    /// </summary>
    private static string? RealPath(string path)
    {
        if (PlatformInformation.IsWindows)
        {
            throw new PlatformNotSupportedException($"{nameof(RealPath)} can only be called on Unix.");
        }

        var ptr = Unix_realpath(path, IntPtr.Zero);
        var result = Marshal.PtrToStringAnsi(ptr); // uses UTF8 on Unix
        Unix_free(ptr);

        return result;
    }

    /// <summary>
    /// Returns the fully qualified path to the mono executable.
    /// </summary>
    private static string? GetMonoRuntimeExecutablePath()
    {
        Contract.ThrowIfTrue(PlatformInformation.IsWindows);

        if (s_monoRuntimeExecutablePath == null)
        {
            var monoPath = GetSearchPaths()
                .Select(p => Path.Combine(p, "mono"))
                .FirstOrDefault(File.Exists);

            if (monoPath == null)
            {
                return null;
            }

            s_monoRuntimeExecutablePath = RealPath(monoPath);
        }

        return s_monoRuntimeExecutablePath;
    }

    /// <summary>
    /// Returns the path to the mono lib directory, usually /usr/bin/mono.
    /// </summary>
    private static string? GetMonoLibDirPath()
    {
        Contract.ThrowIfTrue(PlatformInformation.IsWindows);

        const string DefaultMonoLibPath = "/usr/lib/mono";
        if (Directory.Exists(DefaultMonoLibPath))
        {
            return DefaultMonoLibPath;
        }

        // The normal Unix path doesn't exist, so we'll fallback to finding Mono using the
        // runtime location. This is the likely situation on macOS.

        if (s_monoLibDirPath == null)
        {
            var monoRuntimePath = GetMonoRuntimeExecutablePath();
            if (monoRuntimePath == null)
            {
                return null;
            }

            var monoDirPath = Path.GetDirectoryName(monoRuntimePath)!;

            var monoLibDirPath = Path.Combine(monoDirPath, "..", "lib", "mono");
            monoLibDirPath = Path.GetFullPath(monoLibDirPath);

            s_monoLibDirPath = Directory.Exists(monoLibDirPath)
                ? monoLibDirPath
                : null;
        }

        return s_monoLibDirPath;
    }

    /// <summary>
    /// Returns the path to MSBuild, the actual directory containing MSBuild.dll and friends. Usually should end in Current/bin.
    /// </summary>
    public static string? GetMonoMSBuildDirectory()
    {
        Contract.ThrowIfTrue(PlatformInformation.IsWindows);

        if (s_monoMSBuildDirectory == null)
        {
            var monoLibDirPath = GetMonoLibDirPath();
            if (monoLibDirPath == null)
                return null;

            var monoMSBuildDirPath = Path.Combine(monoLibDirPath, "msbuild");
            var monoMSBuildDir = new DirectoryInfo(Path.GetFullPath(monoMSBuildDirPath));

            if (!monoMSBuildDir.Exists)
                return null;

            // Inside this is either a Current directory or a 15.0 directory, so find it; the previous code at 
            // https://github.com/OmniSharp/omnisharp-roslyn/blob/dde8119c40f4e3920eb5ea894cbca047033bd9aa/src/OmniSharp.Host/MSBuild/Discovery/MSBuildInstanceProvider.cs#L48-L58
            // ensured we had a correctly normalized path in case the underlying file system might have been case insensitive.
            var versionDirectory =
                monoMSBuildDir.EnumerateDirectories().SingleOrDefault(d => d.Name == "Current") ??
                monoMSBuildDir.EnumerateDirectories().SingleOrDefault(d => d.Name == "15.0");

            if (versionDirectory == null)
                return null;

            // Fetch the bin directory underneath, continuing to be case insensitive
            s_monoMSBuildDirectory = versionDirectory.EnumerateDirectories().SingleOrDefault(d => string.Equals(d.Name, "bin", StringComparison.OrdinalIgnoreCase))?.FullName;
        }

        return s_monoMSBuildDirectory;
    }
}
