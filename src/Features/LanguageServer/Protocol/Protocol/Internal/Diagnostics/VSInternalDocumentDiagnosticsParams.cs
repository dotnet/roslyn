// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing a diagnostic pull request for a specific document.
    /// </summary>
    internal class VSInternalDocumentDiagnosticsParams : VSInternalDiagnosticParams, IPartialResultParams<VSInternalDiagnosticReport[]>
    {
        /// <summary>
        /// Gets or sets an optional token that a server can use to report work done progress.
        /// </summary>
        [JsonPropertyName(Methods.WorkDoneTokenName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IProgress<VSInternalDiagnosticReport[]>? WorkDoneToken { get; set; }

        /// <summary>
        /// Gets or sets an optional token that a server can use to report partial results (e.g. streaming) to the client.
        /// </summary>
        [JsonPropertyName(Methods.PartialResultTokenName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IProgress<VSInternalDiagnosticReport[]>? PartialResultToken { get; set; }
    }
}
