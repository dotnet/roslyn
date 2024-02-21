// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the parameters sent for a textDocument/documentHighlight request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentHighlightParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class DocumentHighlightParams
        : TextDocumentPositionParams,
        IPartialResultParams<DocumentHighlight[]>
    {
        /// <summary>
        /// Gets or sets the value of the PartialResultToken instance.
        /// </summary>
        [DataMember(Name = "partialResultToken")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IProgress<DocumentHighlight[]>? PartialResultToken
        {
            get;
            set;
        }
    }
}
