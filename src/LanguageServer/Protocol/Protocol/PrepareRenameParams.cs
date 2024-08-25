// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the parameters for the 'textDocument/prepareRename' request.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#prepareRenameParams">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.12</remarks>
    internal class PrepareRenameParams : ITextDocumentPositionParams, IWorkDoneProgressParams
    {
        /// <inheritdoc/>
        [JsonPropertyName("textDocument")]
        [JsonRequired]
        public TextDocumentIdentifier TextDocument { get; set; }

        /// <summary>
        /// The position in which the rename is requested.
        /// </summary>
        [JsonPropertyName("position")]
        [JsonRequired]
        public Position Position { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName(Methods.WorkDoneTokenName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }
    }
}
