
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Roslyn.Utilities;
namespace Microsoft.CodeAnalysis;

internal sealed class StandardBuildEnvironment : IBuildEnvironment
{
    internal static readonly StandardBuildEnvironment Instance = new StandardBuildEnvironment();

    public string CurrentDirectory => Directory.GetCurrentDirectory();
    public string? GetTempPath() => Path.GetTempPath();
    public string GetFullyQualifiedPath(string path) => Path.Combine(CurrentDirectory, path);
    public string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);

    IReadOnlyDictionary<string, string> IBuildEnvironment.GetEnvironmentVariables() => GetEnvironmentVariables();

    public static Dictionary<string, string> GetEnvironmentVariables()
    {
        var environmentVariables = Environment.GetEnvironmentVariables();
        var map = new Dictionary<string, string>(capacity: environmentVariables.Count, Environment.EnvironmentVariableComparer);
        foreach (System.Collections.DictionaryEntry entry in environmentVariables)
        {
            map[(string)entry.Key] = (string?)entry.Value ?? "";
        }

        return map;
    }
}
