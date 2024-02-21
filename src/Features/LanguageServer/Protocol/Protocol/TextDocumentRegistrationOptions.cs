// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the registration options for many different text document functions.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentRegistrationOptions">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class TextDocumentRegistrationOptions : ITextDocumentRegistrationOptions
    {
        /// <summary>
        /// Gets or sets the document filters for this registration option.
        /// </summary>
        [DataMember(Name = "documentSelector")]
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DocumentFilter[]? DocumentSelector
        {
            get;
            set;
        }
    }
}
