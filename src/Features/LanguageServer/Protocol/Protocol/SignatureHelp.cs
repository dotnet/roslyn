// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the signature of something callable. This class is returned from the textDocument/signatureHelp request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#signatureHelp">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class SignatureHelp
    {
        /// <summary>
        /// Gets or sets an array of signatures associated with the callable item.
        /// </summary>
        [DataMember(Name = "signatures")]
        [JsonProperty(Required = Required.Always)]
        public SignatureInformation[] Signatures
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the active signature. If the value is omitted or falls outside the range of Signatures it defaults to zero.
        /// </summary>
        [DataMember(Name = "activeSignature")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? ActiveSignature
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the active parameter. If the value is omitted or falls outside the range of Signatures[ActiveSignature].Parameters it defaults to zero.
        /// </summary>
        [DataMember(Name = "activeParameter")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? ActiveParameter
        {
            get;
            set;
        }
    }
}
