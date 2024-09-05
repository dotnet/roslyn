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
    internal sealed class VSInternalRelatedDocumentParams : VSInternalStreamingParams, IPartialResultParams<VSInternalRelatedDocumentReport[]>
    {
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
        /// <summary>
        /// Gets or sets the server-generated version number for the related documents result. This is treated as a
        /// black box by the client: it is stored on the client for each textDocument and sent back to the server when
        /// requesting related documents. The server can use this result ID to avoid resending results
        /// that had previously been sent.
        /// </summary>
        [JsonPropertyName("_vs_resultId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ResultId { get; set; }

        [JsonPropertyName("_vs_file_paths")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? FilePaths { get; set; }
    }
}
