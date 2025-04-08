// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Class representing a diagnostic pull request for a specific document.
/// </summary>
internal sealed class VSInternalDocumentDiagnosticsParams : VSInternalDiagnosticParams, IPartialResultParams<VSInternalDiagnosticReport[]>, IWorkDoneProgressParams
{
    /// <inheritdoc/>
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress> WorkDoneToken { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.PartialResultTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<VSInternalDiagnosticReport[]>? PartialResultToken { get; set; }
}
