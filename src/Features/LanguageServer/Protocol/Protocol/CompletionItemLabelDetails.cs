// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using Newtonsoft.Json;
    using System.Runtime.Serialization;

    /// <summary>
    /// Additional details for a completion item label.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#completionItemLabelDetails">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class CompletionItemLabelDetails
    {
        /// <summary>
        /// Gets or sets an optional string which is rendered less prominently directly after label, without any spacing.
        /// </summary>
        [DataMember(Name = "detail")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Detail
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an optional string which is rendered less prominently after detail.
        /// </summary>
        [DataMember(Name = "description")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Description
        {
            get;
            set;
        }
    }
}
