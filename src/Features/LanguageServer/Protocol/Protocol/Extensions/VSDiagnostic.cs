// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// <see cref="VSDiagnostic"/> extends <see cref="Diagnostic"/> providing additional properties used by Visual Studio.
    /// </summary>
    [DataContract]
    internal class VSDiagnostic : Diagnostic
    {
        /// <summary>
        /// Gets or sets the project and context (e.g. Win32, MacOS, etc.) in which the diagnostic was generated.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("_vs_projects")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public VSDiagnosticProjectInformation[]? Projects { get; set; }

        /// <summary>
        /// Gets or sets an expanded description of the diagnostic.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("_vs_expandedMessage")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? ExpandedMessage { get; set; }

        /// <summary>
        /// Gets or sets a message shown when the user hovers over an error. If <see langword="null" />, then <see cref="Diagnostic.Message"/>
        /// is used (use <see cref="VSDiagnosticTags.SuppressEditorToolTip"/> to prevent a tool tip from being shown).
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("_vs_toolTip")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? ToolTip { get; set; }

        /// <summary>
        /// Gets or sets a non-human-readable identier allowing consolidation of multiple equivalent diagnostics
        /// (e.g. the same syntax error from builds targeting different platforms).
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("_vs_identifier")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Identifier { get; set; }

        /// <summary>
        /// Gets or sets a string describing the diagnostic types (e.g. Security, Performance, Style, etc.).
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("_vs_diagnosticType")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? DiagnosticType { get; set; }

        /// <summary>
        /// Gets or sets a rank associated with this diagnostic, used for the default sort.
        /// <see cref="VSDiagnosticRank.Default"/> will be used if no rank is specified.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("_vs_diagnosticRank")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public VSDiagnosticRank? DiagnosticRank { get; set; }

        /// <summary>
        /// Gets or sets an ID used to associate this diagnostic with a corresponding line in the output window.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("_vs_outputId")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public int? OutputId { get; set; }
    }
}
