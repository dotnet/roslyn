// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the folding range request parameter.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#foldingRangeParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class FoldingRangeParams : ITextDocumentParams
    {
        /// <summary>
        /// Gets or sets the text document associated with the folding range request.
        /// </summary>
        [JsonPropertyName("textDocument")]
        public TextDocumentIdentifier TextDocument
        {
            get;
            set;
        }
    }
}
