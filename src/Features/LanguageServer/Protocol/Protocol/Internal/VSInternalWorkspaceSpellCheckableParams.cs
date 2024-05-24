// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Parameter for workspace/_vs_spellCheckableRanges.
    /// </summary>
    internal class VSInternalWorkspaceSpellCheckableParams : IPartialResultParams<VSInternalWorkspaceSpellCheckableReport[]>
    {
        /// <summary>
        /// Gets or sets the current state of the documents the client already has received.
        /// </summary>
        [JsonPropertyName("_vs_previousResults")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public VSInternalStreamingParams[]? PreviousResults { get; set; }

        /// <summary>
        /// Gets or sets an optional token that a server can use to report partial results (e.g. streaming) to the client.
        /// </summary>
        [JsonPropertyName("_vs_partialResultToken")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IProgress<VSInternalWorkspaceSpellCheckableReport[]>? PartialResultToken
        {
            get;
            set;
        }
    }
}
