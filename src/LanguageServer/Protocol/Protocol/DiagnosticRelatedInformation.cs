// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents a related message and source code location for a diagnostic.
    /// This should be used to point to code locations that cause or are related to
    /// a diagnostics, e.g when duplicating a symbol in a scope.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#diagnosticRelatedInformation">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class DiagnosticRelatedInformation
    {
        /// <summary>
        /// Gets or sets the location for the related information.
        /// </summary>
        [JsonPropertyName("location")]
        public Location Location { get; set; }

        /// <summary>
        /// Gets or sets the message for the related information.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
