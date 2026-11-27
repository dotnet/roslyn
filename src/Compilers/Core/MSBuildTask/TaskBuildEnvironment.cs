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

internal sealed class TaskBuildEnvironment : IBuildEnvironment
{
    internal TaskEnvironment TaskEnvironment { get; }
    public string CurrentDirectory => TaskEnvironment.ProjectDirectory;

    internal TaskBuildEnvironment(TaskEnvironment taskEnvironment)
    {
        TaskEnvironment = taskEnvironment;
    }

    internal TaskBuildEnvironment(string projectDirectory, Dictionary<string, string> environment)
        : this(TaskEnvironment.CreateWithProjectDirectoryAndEnvironment(projectDirectory, environment))
    {
    }

    public string GetFullyQualifiedPath(string path) => TaskEnvironment.GetAbsolutePath(path).Value;
    public string? GetEnvironmentVariable(string name) => TaskEnvironment.GetEnvironmentVariable(name);
    public IReadOnlyDictionary<string, string> GetEnvironmentVariables() => TaskEnvironment.GetEnvironmentVariables();
    public string? GetTempPath() => TaskEnvironment.GetTempPath();
}
