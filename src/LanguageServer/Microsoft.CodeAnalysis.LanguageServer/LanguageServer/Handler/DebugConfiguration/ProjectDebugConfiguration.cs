// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.DebugConfiguration;

internal sealed class ProjectDebugConfiguration
{
    public ProjectDebugConfiguration(string projectPath, string outputPath, string projectName, bool targetsDotnetCore, bool isExe, string? solutionPath)
    {
        ProjectPath = projectPath;
        OutputPath = outputPath;
        ProjectName = projectName;
        TargetsDotnetCore = targetsDotnetCore;
        IsExe = isExe;
        SolutionPath = solutionPath;
    }

    [JsonPropertyName("projectPath")]
    public string ProjectPath { get; }

    [JsonPropertyName("outputPath")]
    public string OutputPath { get; }

    [JsonPropertyName("projectName")]
    public string ProjectName { get; }

    [JsonPropertyName("targetsDotnetCore")]
    public bool TargetsDotnetCore { get; }

    [JsonPropertyName("isExe")]
    public bool IsExe { get; }

    [JsonPropertyName("solutionPath")]
    public string? SolutionPath { get; }
}
