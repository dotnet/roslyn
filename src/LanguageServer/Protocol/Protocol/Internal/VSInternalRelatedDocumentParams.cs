// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Parameter for copilot/_related_documents.
    /// </summary>
    internal sealed class VSInternalRelatedDocumentParams : IPartialResultParams<VSInternalRelatedDocumentReport[]>
    {
        /// <summary>
        /// Gets or sets the document for which the feature is being requested for.
        /// </summary>
        [JsonPropertyName("_vs_textDocument")]
        [JsonRequired]
        public TextDocumentIdentifier TextDocument { get; set; }

        /// <summary>
        /// Gets or sets the value which indicates the position within the document.
        /// </summary>
        [JsonPropertyName("position")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Position? Position { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName(Methods.PartialResultTokenName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IProgress<VSInternalRelatedDocumentReport[]>? PartialResultToken { get; set; }
    }

    internal sealed class VSInternalRelatedDocumentReport
    {
        [JsonPropertyName("_vs_file_paths")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? FilePaths { get; set; }
    }
}
