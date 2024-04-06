// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Options for the full document semantic tokens classification provider.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokensOptions">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class SemanticTokensFullOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the server supports deltas for full documents.
        /// </summary>
        [DataMember(Name = "delta")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Delta
        {
            get;
            set;
        }
    }
}
