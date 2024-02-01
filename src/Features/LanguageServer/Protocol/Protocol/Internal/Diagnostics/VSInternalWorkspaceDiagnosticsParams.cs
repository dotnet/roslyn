// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing a diagnostic pull request for all documents.
    /// </summary>
    [DataContract]
    internal class VSInternalWorkspaceDiagnosticsParams : IPartialResultParams<VSInternalWorkspaceDiagnosticReport[]>
    {
        /// <summary>
        /// Gets or sets the current state of the documents the client already has received.
        /// </summary>
        [DataMember(Name = "_vs_previousResults")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSInternalDiagnosticParams[]? PreviousResults { get; set; }

        /// <summary>
        /// Gets or sets an optional token that a server can use to report work done progress.
        /// </summary>
        [DataMember(Name = Methods.WorkDoneTokenName)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IProgress<VSInternalWorkspaceDiagnosticReport[]>? WorkDoneToken { get; set; }

        /// <summary>
        /// Gets or sets an optional token that a server can use to report partial results (e.g. streaming) to the client.
        /// </summary>
        [DataMember(Name = Methods.PartialResultTokenName)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IProgress<VSInternalWorkspaceDiagnosticReport[]>? PartialResultToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating what kind of diagnostic this request is querying for.
        /// </summary>
        [DataMember(Name = "_vs_queryingDiagnosticKind")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSInternalDiagnosticKind? QueryingDiagnosticKind { get; set; }
    }
}
