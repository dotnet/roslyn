// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;

/// <summary>
/// Data type for the project load telemetry event.
/// Intentionally matches https://github.com/OmniSharp/omnisharp-roslyn/blob/master/src/OmniSharp.Abstractions/Models/Events/ProjectConfigurationMessage.cs
/// except for SdkVersion, which is unused by the client in the O# version.
/// </summary>

[DataContract]
internal record ProjectLoadTelemetryEvent(
    // The project guid (if it came from a solution), or a hash representing the file path and contents.
    [property: DataMember(Name = "ProjectId")] string ProjectId,
    [property: DataMember(Name = "SessionId")] string SessionId,
    [property: DataMember(Name = "OutputKind")] int OutputKind,
    [property: DataMember(Name = "ProjectCapabilities")] IEnumerable<string> ProjectCapabilities,
    [property: DataMember(Name = "TargetFrameworks")] IEnumerable<string> TargetFrameworks,
    [property: DataMember(Name = "References")] IEnumerable<string> References,
    [property: DataMember(Name = "FileExtensions")] IEnumerable<string> FileExtensions,
    [property: DataMember(Name = "FileCounts")] IEnumerable<int> FileCounts,
    [property: DataMember(Name = "SdkStyleProject")] bool SdkStyleProject)
{
}
