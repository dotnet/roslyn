// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing a diagnostic pull request report.
    /// </summary>
    internal class VSInternalDiagnosticReport
    {
        /// <summary>
        /// Gets or sets the server-generated version number for the diagnostics.
        /// This is treated as a black box by the client: it is stored on the client
        /// for each textDocument and sent back to the server when requesting
        /// diagnostics.The server can use this result ID to avoid resending
        /// diagnostics that had previously been sent.
        /// </summary>
        [JsonPropertyName("_vs_resultId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ResultId { get; set; }

        /// <summary>
        /// Gets or sets a (potentially incomplete) list of Diagnostics for the document.
        /// Subsequent DiagnosticReports for the same document will be appended.
        /// </summary>
        /// <remarks>
        /// Is null if no changes in the diagnostics. Is empty if there is no diagnostic.
        /// </remarks>
        [JsonPropertyName("_vs_diagnostics")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Diagnostic[]? Diagnostics { get; set; }

        /// <summary>
        /// Gets or sets an identifier associated with all the diagnostics in this report.
        ///
        /// If the <see cref="Identifier" /> property matches the supersedes property of another report,
        /// <see cref="Diagnostic" /> entries tagged with <see cref="VSDiagnosticTags.PotentialDuplicate" /> will
        /// be hidden in the editor.
        /// </summary>
        [JsonPropertyName("_vs_identifier")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Identifier { get; set; }

        /// <summary>
        /// Gets or sets an indicator of which diagnostic report is superseded by this report.
        /// </summary>
        /// <remarks>
        /// Diagnostics in a superseded report will be hidden if they have the PotentialDuplicate VSDiagnosticTag.
        /// </remarks>
        [JsonPropertyName("_vs_supersedes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Supersedes { get; set; }

        /// <summary>
        /// Gets or sets an optional key used to associate diagnostics with lines
        /// of text in the output window(diagnostics can have an additional
        /// outputId and the (outputKey, outputId) uniquely identify
        /// a line of text in the output window).
        /// </summary>
        [JsonPropertyName("_vs_outputKey")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Guid? OutputKey { get; set; }

        /// <summary>
        /// Gets or sets the document version.
        /// </summary>
        [JsonPropertyName("_vs_version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Version { get; set; }
    }
}
