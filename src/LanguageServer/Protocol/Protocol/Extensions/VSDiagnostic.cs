// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// <see cref="VSDiagnostic"/> extends <see cref="Diagnostic"/> providing additional properties used by Visual Studio.
/// </summary>
internal sealed class VSDiagnostic : Diagnostic
{
    /// <summary>
    /// Gets or sets the project and context (e.g. Win32, MacOS, etc.) in which the diagnostic was generated.
    /// </summary>
    [JsonPropertyName("_vs_projects")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VSDiagnosticProjectInformation[]? Projects { get; set; }

    /// <summary>
    /// Gets or sets an expanded description of the diagnostic.
    /// </summary>
    [JsonPropertyName("_vs_expandedMessage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExpandedMessage { get; set; }

    /// <summary>
    /// Gets or sets a message shown when the user hovers over an error. If <see langword="null" />, then <see cref="Diagnostic.Message"/>
    /// is used (use <see cref="VSDiagnosticTags.SuppressEditorToolTip"/> to prevent a tool tip from being shown).
    /// </summary>
    [JsonPropertyName("_vs_toolTip")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolTip { get; set; }

    /// <summary>
    /// Gets or sets a non-human-readable identier allowing consolidation of multiple equivalent diagnostics
    /// (e.g. the same syntax error from builds targeting different platforms).
    /// </summary>
    [JsonPropertyName("_vs_identifier")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Identifier { get; set; }

    /// <summary>
    /// Gets or sets a string describing the diagnostic types (e.g. Security, Performance, Style, etc.).
    /// </summary>
    [JsonPropertyName("_vs_diagnosticType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DiagnosticType { get; set; }

    /// <summary>
    /// Gets or sets a rank associated with this diagnostic, used for the default sort.
    /// <see cref="VSDiagnosticRank.Default"/> will be used if no rank is specified.
    /// </summary>
    [JsonPropertyName("_vs_diagnosticRank")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VSDiagnosticRank? DiagnosticRank { get; set; }

    /// <summary>
    /// Gets or sets an ID used to associate this diagnostic with a corresponding line in the output window.
    /// </summary>
    [JsonPropertyName("_vs_outputId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OutputId { get; set; }
}
