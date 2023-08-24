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
    [property: DataMember(Name = "ProjectId")] string ProjectId,
    [property: DataMember(Name = "SessionId")] string SessionId,
    [property: DataMember(Name = "OutputKind")] int OutputKind,
    [property: DataMember(Name = "ProjectCapabilities")] string[] ProjectCapabilities,
    [property: DataMember(Name = "TargetFrameworks")] string[] TargetFrameworks,
    [property: DataMember(Name = "References")] string[] References,
    [property: DataMember(Name = "FileExtensions")] string[] FileExtensions,
    [property: DataMember(Name = "FileCounts")] int[] FileCounts,
    [property: DataMember(Name = "SdkStyleProject")] bool SdkStyleProject)
{
}
