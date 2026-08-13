// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.BuildTasks;
using Roslyn.Utilities;
namespace Microsoft.CodeAnalysis.CommandLine;

internal static class Extensions
{
    extension(TaskEnvironment taskEnvironment)
    {
        internal string GetFullPath(string path)
        {
            var fullPath = taskEnvironment.GetAbsolutePath(path).Value;
            return fullPath;
        }

        internal string GetFullPathNoThrow(string path)
        {
            try
            {
                path = taskEnvironment.GetFullPath(path);
            }
            catch (Exception e) when (Utilities.IsIoRelatedException(e)) { }
            return path;
        }

        /// <summary>
        /// Gets the value of the temporary path for the provided environment settings. This behavior
        /// is OS specific.
        ///   - On Windows it seeks to emulate Path.GetTempPath as closely as possible with 
        ///     provided working directory.
        /// </summary>
        internal string? GetTempPath()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? getTempPathWindows()
                : getTempPathLinux();

            string? getTempPathLinux()
            {
                // Unix temp path is fine: it does not use the working directory
                // (it uses ${TMPDIR} if set, otherwise, it returns /tmp)
                var tempPath = taskEnvironment.GetEnvironmentVariable("TMPDIR");
                return !string.IsNullOrEmpty(tempPath) ? tempPath : "/tmp";
            }

            string? getTempPathWindows()
            {
                var tmp = taskEnvironment.GetEnvironmentVariable("TMP");
                if (Path.IsPathRooted(tmp))
                {
                    return tmp;
                }

                var temp = taskEnvironment.GetEnvironmentVariable("TEMP");
                if (Path.IsPathRooted(temp))
                {
                    return temp;
                }

                if (!string.IsNullOrEmpty(taskEnvironment.ProjectDirectory))
                {
                    if (!string.IsNullOrEmpty(tmp))
                    {
                        return Path.Combine(taskEnvironment.ProjectDirectory, tmp);
                    }

                    if (!string.IsNullOrEmpty(temp))
                    {
                        return Path.Combine(taskEnvironment.ProjectDirectory, temp);
                    }
                }

                var userProfile = taskEnvironment.GetEnvironmentVariable("USERPROFILE");
                if (Path.IsPathRooted(userProfile))
                {
                    return userProfile;
                }

                return taskEnvironment.GetEnvironmentVariable("SYSTEMROOT");
            }
        }
    }
}