// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Inlay hint registration options.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#inlayHintRegistrationOptions">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class InlayHintRegistrationOptions : InlayHintOptions, ITextDocumentRegistrationOptions, IStaticRegistrationOptions
    {
        /// <summary>
        /// Gets or sets the document filters for this registration option.
        /// </summary>
        [JsonPropertyName("documentSelector")]
        public DocumentFilter[]? DocumentSelector
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether work done progress is supported.
        /// </summary>
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id
        {
            get;
            set;
        }
    }
}
