// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Server capabilities for pull diagnostics.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#diagnosticOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal class DiagnosticOptions : IWorkDoneProgressOptions
{
    /// <inheritdoc/>
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
    /// Whether the language has inter-file dependencies meaning that
    /// editing code in one file can result in a different diagnostic
    /// set in another file.
    /// <para>
    /// Inter file dependencies are common for most programming
    /// languages and typically uncommon for linters.
    /// </para>
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
