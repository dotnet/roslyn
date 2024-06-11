// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.DebugConfiguration;

[DataContract]
internal class ProjectDebugConfiguration
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

    [JsonProperty(PropertyName = "projectPath")]
    public string ProjectPath { get; }

    [JsonProperty(PropertyName = "outputPath")]
    public string OutputPath { get; }

    [JsonProperty(PropertyName = "projectName")]
    public string ProjectName { get; }

    [JsonProperty(PropertyName = "targetsDotnetCore")]
    public bool TargetsDotnetCore { get; }

    [JsonProperty(PropertyName = "isExe")]
    public bool IsExe { get; }

    [JsonProperty(PropertyName = "solutionPath")]
    public string? SolutionPath { get; }
}
