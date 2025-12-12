// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;

/// <summary>
/// Data type for the project load telemetry event.
/// Intentionally matches https://github.com/OmniSharp/omnisharp-roslyn/blob/master/src/OmniSharp.Abstractions/Models/Events/ProjectConfigurationMessage.cs
/// except for SdkVersion, which is unused by the client in the O# version.
/// </summary>

internal sealed record ProjectLoadTelemetryEvent(
    // The project guid (if it came from a solution), or a hash representing the file path and contents.
    [property: JsonPropertyName("ProjectId")] string ProjectId,
    [property: JsonPropertyName("SessionId")] string SessionId,
    [property: JsonPropertyName("OutputKind")] int OutputKind,
    [property: JsonPropertyName("ProjectCapabilities")] IEnumerable<string> ProjectCapabilities,
    [property: JsonPropertyName("TargetFrameworks")] IEnumerable<string> TargetFrameworks,
    [property: JsonPropertyName("References")] IEnumerable<string> References,
    [property: JsonPropertyName("FileExtensions")] IEnumerable<string> FileExtensions,
    [property: JsonPropertyName("FileCounts")] IEnumerable<int> FileCounts,
    [property: JsonPropertyName("SdkStyleProject")] bool SdkStyleProject,
    [property: JsonPropertyName("HasSolutionFile")] bool HasSolutionFile,
    [property: JsonPropertyName("IsFileBasedProgram")] bool IsFileBasedProgram,
    [property: JsonPropertyName("IsMiscellaneousFile")] bool IsMiscellaneousFile)
{
}
