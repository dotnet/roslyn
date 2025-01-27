// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the parameter for the 'textDocument/foldingRange' request.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#foldingRangeParams">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    internal class FoldingRangeParams : ITextDocumentParams, IWorkDoneProgressParams, IPartialResultParams<FoldingRange[]>
    {
        /// <summary>
        /// Gets or sets the text document associated with the folding range request.
        /// </summary>
        [JsonPropertyName("textDocument")]
        [JsonRequired]
        public TextDocumentIdentifier TextDocument { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName(Methods.WorkDoneTokenName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName(Methods.PartialResultTokenName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IProgress<FoldingRange[]>? PartialResultToken { get; set; }
    }
}
