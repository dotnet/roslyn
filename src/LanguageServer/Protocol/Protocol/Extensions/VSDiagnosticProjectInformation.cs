// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// <see cref="VSDiagnosticProjectInformation"/> represents the project and context in which the <see cref="VSDiagnostic"/> is generated.
/// </summary>
internal sealed class VSDiagnosticProjectInformation
{
    /// <summary>
    /// Gets or sets a human-readable identifier for the project in which the diagnostic was generated.
    /// </summary>
    [JsonPropertyName("_vs_projectName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProjectName { get; set; }

    /// <summary>
    /// Gets or sets a human-readable identifier for the build context (e.g. Win32 or MacOS)
    /// in which the diagnostic was generated.
    /// </summary>
    [JsonPropertyName("_vs_context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Context { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the project in which the diagnostic was generated.
    /// </summary>
    [JsonPropertyName("_vs_projectIdentifier")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProjectIdentifier { get; set; }
}
