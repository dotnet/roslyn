// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Roslyn.Utilities
{
    internal static class TempPathUtilities
    {
        /// <summary>
        /// Gets a temp directory which is expected to meet the following access constraints:
        /// - The directory's permissions are restricted to the current user.
        /// - The directory's parent directories permit only authorized users to write files, recursively up to the drive root.
        /// </summary>
        /// <remarks>
        /// Access constraints are not verified. If they are not met in practice, it is considered an environment misconfiguration.
        ///
        /// The parent directory access constraint reflects the fact that files in a parent directory can affect behavior of tools run on files in a subdirectory.
        /// Therefore, the temp directory should only allow the current user or higher-privileged users to write files to its parent directories.
        /// We rely on OS-level defaults to uphold this constraint for the temp directory we choose.
        /// There is also no requirement that other users are disallowed from creating folders in a parent directory of temp.
        /// For example, any authenticated user is allowed to create folders in 'C:\' on Windows.
        ///
        /// On Windows and macOS, 'Path.GetTempPath()' is used, and existence of the directory is not verified.
        /// The return value of 'Path.GetTempPath()' conventionally meets access constraints on these platforms.
        /// See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.path.gettemppath"/>.
        ///
        /// On Linux, '$XDG_CACHE_HOME' is used, with fallbacks and creating the directory if needed,
        /// in accordance with <see href="https://specifications.freedesktop.org/basedir/0.8/" />.
        /// 'Path.GetTempPath()' is not used on Linux because it conventionally returns '/tmp/'.
        /// '/tmp/' usually allows any user to write to it, so any subdirectory of it does not meet the access constraints.
        /// </remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-security-best-practices?view=visualstudio#run-your-build-in-a-verified-dedicated-location"/>
        internal static string GetTempPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
#pragma warning disable RS0030 // Do not use banned APIs
                return Path.GetTempPath();
#pragma warning restore RS0030 // Do not use banned APIs
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // https://specifications.freedesktop.org/basedir/0.8/#variables
                // > $XDG_CACHE_HOME defines the base directory relative to which user-specific non-essential data files should be stored.
#pragma warning disable RS0030 // Do not use banned APIs
                return CreateUserTempDirectoryLinuxCore(Environment.GetEnvironmentVariable("XDG_CACHE_HOME"));
#pragma warning restore RS0030 // Do not use banned APIs
            }

            throw new InvalidOperationException("GetTempPath is not supported on the current platform.");
        }

        /// <summary>Internal for testing only.</summary>
#if NET
        [SupportedOSPlatform("linux")]
#endif
        internal static string CreateUserTempDirectoryLinuxCore(string? xdgCacheHome)
        {
#pragma warning disable RS0030 // Do not use banned APIs
            var pathInfo = new DirectoryInfo(getLinuxCachePath(xdgCacheHome));
#pragma warning restore RS0030 // Do not use banned APIs

            // https://specifications.freedesktop.org/basedir/0.8/#referencing states:
            // > If, when attempting to write a file, the destination directory is
            // > non-existent an attempt should be made to create it with permission 0700.
            // > If the destination directory exists already the permissions should not be changed.

            // Currently, we cannot assume that this code path will never run on netstandard.
            // Manually match semantics of 'Directory.CreateDirectory(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute)'.
            // Create the parent directories with the default mode (0777 & ~umask), and the cache directory itself with 0700 mode.
            // Assume all pre-existing parent directories up to the drive root are writable only by authorized users.
            // Assume the umask blocks unauthorized users from writing to any parent directories we create.
            // (for example, via conventional umasks such as 002 or 022.)
            if (pathInfo.Parent is { } parentDirectory)
            {
                parentDirectory.Create();
            }

            const uint mode0700 = 0b111_000_000;
            const int eexist = 17;
            if (mkdir(pathInfo.FullName, mode0700) != 0)
            {
                var errno = Marshal.GetLastWin32Error();
                // Don't throw on mkdir failure if it was due to a directory already existing at 'pathInfo.FullName'.
#pragma warning disable RS0030 // Do not use banned APIs
                if (errno != eexist || !Directory.Exists(pathInfo.FullName))
#pragma warning restore RS0030 // Do not use banned APIs
                {
                    var innerException = new Win32Exception(errno);
                    throw new IOException(
                        $"Unable to create directory '{pathInfo.FullName}': {innerException.Message}",
                        innerException);
                }
            }

            return pathInfo.FullName;

            static string getLinuxCachePath(string? xdgCacheHome)
            {
                // https://specifications.freedesktop.org/basedir/0.8/#basics
                // > All paths set in these environment variables must be absolute.
                // > If an implementation encounters a relative path in any of these variables it should consider the path invalid and ignore it.
                // (Note: this applies to $XDG-prefixed variables, not to $HOME.)
                if (xdgCacheHome is not null && isAbsoluteLinuxPath(xdgCacheHome))
                {
                    return xdgCacheHome;
                }

                // > If $XDG_CACHE_HOME is either not set or empty, a default equal to $HOME/.cache should be used.
#pragma warning disable RS0030 // Do not use banned APIs
                var home = Environment.GetEnvironmentVariable("HOME");
#pragma warning restore RS0030 // Do not use banned APIs
                if (!string.IsNullOrEmpty(home))
                {
                    return Path.Combine(home, ".cache");
                }

                throw new InvalidOperationException("Unable to get a temp path because both $XDG_CACHE_HOME and $HOME were not found.");
            }

            static bool isAbsoluteLinuxPath(string path)
            {
                // Note: due to wide range of use sites that this file is used in,
                // we prefer a simplistic and Linux-specific absolute path check, versus using a System.IO.Path helper.

                // https://man7.org/linux/man-pages/man7/path_resolution.7.html
                // > If the pathname starts with the '/' character, the starting lookup
                // > directory is the root directory of the calling process.
                // > ...
                // > Pathnames starting with a '/' character are called absolute pathnames.
                // > Pathnames not starting with a '/' are called relative pathnames.
                return path.Length > 0 && path[0] == '/';
            }

            [DllImport("libc", SetLastError = true)]
            static extern int mkdir(string path, uint mode);
        }
    }
}
