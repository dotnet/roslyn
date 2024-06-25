// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the parameters sent for a textDocument/_vs_uriPresentation request.
    /// </summary>
    internal class VSInternalUriPresentationParams : ITextDocumentParams
    {
        /// <summary>
        /// Gets or sets the identifier for the text document to be operate on.
        /// </summary>
        [JsonPropertyName("_vs_textDocument")]
        [JsonRequired]
        public TextDocumentIdentifier TextDocument
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the range.
        /// </summary>
        [JsonPropertyName("_vs_range")]
        [JsonRequired]
        public Range Range
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the URI values. Valid for DropKind.Uris.
        /// </summary>
        [JsonPropertyName("_vs_uris")]
        public Uri[]? Uris
        {
            get;
            set;
        }
    }
}
