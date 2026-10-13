// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests;

internal sealed class TestableBuildEnvironment(
    string currentDirectory,
    Dictionary<string, string>? environmentVariables = null,
    string? tempPath = null) : IBuildEnvironment
{
    public string CurrentDirectory { get; } = currentDirectory;
    public Dictionary<string, string> EnvironmentVariables { get; } = environmentVariables ?? new Dictionary<string, string>();
    public string? TempPath { get; } = tempPath;

    public string GetFullyQualifiedPath(string path) => Path.Combine(CurrentDirectory, path);
    public string? GetEnvironmentVariable(string name) => EnvironmentVariables.TryGetValue(name, out var value) ? value : null;
    public IReadOnlyDictionary<string, string> GetEnvironmentVariables() => EnvironmentVariables;
    public string? GetTempPath() => TempPath;
}