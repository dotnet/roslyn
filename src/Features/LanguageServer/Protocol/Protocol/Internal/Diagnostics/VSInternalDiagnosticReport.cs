// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing a diagnostic pull request report.
    /// </summary>
    [DataContract]
    internal class VSInternalDiagnosticReport
    {
        /// <summary>
        /// Gets or sets the server-generated version number for the diagnostics.
        /// This is treated as a black box by the client: it is stored on the client
        /// for each textDocument and sent back to the server when requesting
        /// diagnostics.The server can use this result ID to avoid resending
        /// diagnostics that had previously been sent.
        /// </summary>
        [DataMember(Name = "_vs_resultId")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ResultId { get; set; }

        /// <summary>
        /// Gets or sets a (potentially incomplete) list of Diagnostics for the document.
        /// Subsequent DiagnosticReports for the same document will be appended.
        /// </summary>
        /// <remarks>
        /// Is null if no changes in the diagnostics. Is empty if there is no diagnostic.
        /// </remarks>
        [DataMember(Name = "_vs_diagnostics")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Diagnostic[]? Diagnostics { get; set; }

        /// <summary>
        /// Gets or sets an identifier associated with all the diagnostics in this report.
        ///
        /// If the <see cref="Identifier" /> property matches the supersedes property of another report,
        /// <see cref="Diagnostic" /> entries tagged with <see cref="VSDiagnosticTags.PotentialDuplicate" /> will
        /// be hidden in the editor.
        /// </summary>
        [DataMember(Name = "_vs_identifier")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? Identifier { get; set; }

        /// <summary>
        /// Gets or sets an indicator of which diagnostic report is superseded by this report.
        /// </summary>
        /// <remarks>
        /// Diagnostics in a superseded report will be hidden if they have the PotentialDuplicate VSDiagnosticTag.
        /// </remarks>
        [DataMember(Name = "_vs_supersedes")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? Supersedes { get; set; }

        /// <summary>
        /// Gets or sets an optional key used to associate diagnostics with lines
        /// of text in the output window(diagnostics can have an additional
        /// outputId and the (outputKey, outputId) uniquely identify
        /// a line of text in the output window).
        /// </summary>
        [DataMember(Name = "_vs_outputKey")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Guid? OutputKey { get; set; }

        /// <summary>
        /// Gets or sets the document version.
        /// </summary>
        [DataMember(Name = "_vs_version")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? Version { get; set; }
    }
}
