// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Server capabilities for pull diagnostics.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#diagnosticOptions">Language Server Protocol specification</see> for additional information.
/// </summary>
internal class DiagnosticOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether work done progress is supported.
    /// </summary>
    [JsonPropertyName("workDoneProgress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool WorkDoneProgress { get; init; }

    /// <summary>
    /// Gets or sets the identifier in which the diagnostics are bucketed by the client.
    /// </summary>
    [JsonPropertyName("identifier")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Identifier
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the language has inter file dependencies.
    /// </summary>
    [JsonPropertyName("interFileDependencies")]
    public bool InterFileDependencies
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the server provides support for workspace diagnostics as well.
    /// </summary>
    [JsonPropertyName("workspaceDiagnostics")]
    public bool WorkspaceDiagnostics
    {
        get;
        set;
    }
}
