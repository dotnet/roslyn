// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Text;
    using Newtonsoft.Json;

    /// <summary>
    /// Parameter for workspace/_vs_spellCheckableRanges.
    /// </summary>
    [DataContract]
    internal class VSInternalWorkspaceSpellCheckableParams : IPartialResultParams<VSInternalWorkspaceSpellCheckableReport[]>
    {
        /// <summary>
        /// Gets or sets the current state of the documents the client already has received.
        /// </summary>
        [DataMember(Name = "_vs_previousResults")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSInternalStreamingParams[]? PreviousResults { get; set; }

        /// <summary>
        /// Gets or sets an optional token that a server can use to report partial results (e.g. streaming) to the client.
        /// </summary>
        [DataMember(Name = "_vs_partialResultToken")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IProgress<VSInternalWorkspaceSpellCheckableReport[]>? PartialResultToken
        {
            get;
            set;
        }
    }
}
