// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Parameters for semantic tokens full Document request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokensParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class SemanticTokensParams : ITextDocumentParams, IPartialResultParams<SemanticTokensPartialResult>
    {
        /// <summary>
        /// Gets or sets an identifier for the document to fetch semantic tokens from.
        /// </summary>
        [DataMember(Name = "textDocument")]
        public TextDocumentIdentifier TextDocument { get; set; }

        /// <summary>
        /// Gets or sets the value of the Progress instance.
        /// </summary>
        [DataMember(Name = Methods.PartialResultTokenName, IsRequired = false)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IProgress<SemanticTokensPartialResult>? PartialResultToken
        {
            get;
            set;
        }
    }
}
