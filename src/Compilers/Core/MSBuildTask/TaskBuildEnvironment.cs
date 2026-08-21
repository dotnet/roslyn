// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using Roslyn.Utilities;
namespace Microsoft.CodeAnalysis.CommandLine;

internal sealed class TaskBuildEnvironment(TaskEnvironment taskEnvironment) : IBuildEnvironment
{
    internal TaskEnvironment TaskEnvironment { get; } = taskEnvironment;
    public string CurrentDirectory => TaskEnvironment.ProjectDirectory;
    public string GetFullPath(string path) => TaskEnvironment.GetAbsolutePath(path).Value;
    public string? GetEnvironmentVariable(string name) => TaskEnvironment.GetEnvironmentVariable(name);
    public IReadOnlyDictionary<string, string> GetEnvironmentVariables() => TaskEnvironment.GetEnvironmentVariables();

    /// <summary>
    /// Gets the value of the temporary path for the provided environment settings. This behavior
    /// is OS specific.
    ///   - On Windows it seeks to emulate Path.GetTempPath as closely as possible with 
    ///     provided working directory.
    /// </summary>
    public string? GetTempPath()
    {
        var path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? getTempPathWindows()
            : getTempPathLinux();
        return path is null ? path : GetFullPath(path);

        string? getTempPathLinux()
        {
            // Unix temp path is fine: it does not use the working directory
            // (it uses ${TMPDIR} if set, otherwise, it returns /tmp)
            var tempPath = GetEnvironmentVariable("TMPDIR");
            return !string.IsNullOrEmpty(tempPath) ? tempPath : "/tmp";
        }

        string? getTempPathWindows()
        {
            var tmp = GetEnvironmentVariable("TMP");
            if (Path.IsPathRooted(tmp))
            {
                return tmp;
            }

            var temp = GetEnvironmentVariable("TEMP");
            if (Path.IsPathRooted(temp))
            {
                return temp;
            }

            if (!string.IsNullOrEmpty(TaskEnvironment.ProjectDirectory))
            {
                if (!string.IsNullOrEmpty(tmp))
                {
                    return Path.Combine(TaskEnvironment.ProjectDirectory, tmp);
                }

                if (!string.IsNullOrEmpty(temp))
                {
                    return Path.Combine(TaskEnvironment.ProjectDirectory, temp);
                }
            }

            var userProfile = GetEnvironmentVariable("USERPROFILE");
            if (Path.IsPathRooted(userProfile))
            {
                return userProfile;
            }

            return GetEnvironmentVariable("SYSTEMROOT");
        }
    }
}
