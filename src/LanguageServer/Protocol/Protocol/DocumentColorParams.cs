// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the parameters sent for a textDocument/documentColor request.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentColorParams">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.6</remarks>
    internal class DocumentColorParams : ITextDocumentParams, IWorkDoneProgressParams, IPartialResultParams<ColorInformation[]>
    {
        /// <summary>
        /// The <see cref="TextDocumentIdentifier"/> to provide color information for.
        /// </summary>
        [JsonPropertyName("textDocument")]
        public TextDocumentIdentifier TextDocument { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName(Methods.WorkDoneTokenName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName(Methods.PartialResultTokenName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IProgress<ColorInformation[]>? PartialResultToken { get; set; }
    }
}
