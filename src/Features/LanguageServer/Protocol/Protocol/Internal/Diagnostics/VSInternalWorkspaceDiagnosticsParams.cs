// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing a diagnostic pull request for all documents.
    /// </summary>
    internal class VSInternalWorkspaceDiagnosticsParams : IPartialResultParams<VSInternalWorkspaceDiagnosticReport[]>
    {
        /// <summary>
        /// Gets or sets the current state of the documents the client already has received.
        /// </summary>
        [JsonPropertyName("_vs_previousResults")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public VSInternalDiagnosticParams[]? PreviousResults { get; set; }

        /// <summary>
        /// Gets or sets an optional token that a server can use to report work done progress.
        /// </summary>
        [JsonPropertyName(Methods.WorkDoneTokenName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IProgress<VSInternalWorkspaceDiagnosticReport[]>? WorkDoneToken { get; set; }

        /// <summary>
        /// Gets or sets an optional token that a server can use to report partial results (e.g. streaming) to the client.
        /// </summary>
        [JsonPropertyName(Methods.PartialResultTokenName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IProgress<VSInternalWorkspaceDiagnosticReport[]>? PartialResultToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating what kind of diagnostic this request is querying for.
        /// </summary>
        [JsonPropertyName("_vs_queryingDiagnosticKind")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public VSInternalDiagnosticKind? QueryingDiagnosticKind { get; set; }
    }
}
