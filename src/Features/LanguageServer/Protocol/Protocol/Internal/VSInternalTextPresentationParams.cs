// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the parameters sent for a textDocument/_vs_textPresentation request.
    /// </summary>
    [DataContract]
    internal class VSInternalTextPresentationParams : ITextDocumentParams
    {
        /// <summary>
        /// Gets or sets the identifier for the text document to be operate on.
        /// </summary>
        [DataMember(Name = "_vs_textDocument")]
        [JsonProperty(Required = Required.Always)]
        public TextDocumentIdentifier TextDocument
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the range.
        /// </summary>
        [DataMember(Name = "_vs_range")]
        [JsonProperty(Required = Required.Always)]
        public Range Range
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        [DataMember(Name = "_vs_text")]
        public string? Text
        {
            get;
            set;
        }
    }
}
