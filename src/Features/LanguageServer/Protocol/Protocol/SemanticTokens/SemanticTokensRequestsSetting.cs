// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Requests client settings for semantic tokens.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokensClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class SemanticTokensRequestsSetting
    {
        /// <summary>
        /// Gets or sets a value indicating whether the client will send the
        /// `textDocument/semanticTokens/range` request if the server provides a
        /// corresponding handler.
        /// </summary>
        [DataMember(Name = "range")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, object>? Range { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the client will send the
        /// `textDocument/semanticTokens/full` request if the server provides a
        /// corresponding handler.
        /// </summary>
        [DataMember(Name = "full")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, SemanticTokensRequestsFullSetting>? Full { get; set; }
    }
}
