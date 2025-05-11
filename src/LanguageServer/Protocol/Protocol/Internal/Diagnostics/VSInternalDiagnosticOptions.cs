// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Server provided options for pull diagnostic requests.
/// </summary>
internal record class VSInternalDiagnosticOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// Gets or sets a list of id's used to identify diagnostics that may be coming
    /// from build systems instead of a language server.
    ///
    /// VS client will then use the information to do any merging logic in the Error List.
    /// Maps to <see cref="VSDiagnostic.Identifier"/>.
    /// </summary>
    [JsonPropertyName("_vs_buildOnlyDiagnosticIds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? BuildOnlyDiagnosticIds { get; init; }

    /// <summary>
    /// Gets or sets a list of diagnostic kinds used to query diagnostics in each context.
    /// </summary>
    [JsonPropertyName("_vs_diagnosticKinds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VSInternalDiagnosticKind[]? DiagnosticKinds { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the server provides support for sending diagnostics requests for all project contexts.
    /// </summary>
    [JsonPropertyName("_vs_supportsMultipleContextDiagnostics")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool SupportsMultipleContextsDiagnostics { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether work done progress is supported.
    /// </summary>
    [JsonPropertyName("_vs_workDoneProgress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool WorkDoneProgress { get; init; }
}
