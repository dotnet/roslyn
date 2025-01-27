// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the rename parameters for the textDocument/rename request.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#renameParams">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class RenameParams : TextDocumentPositionParams, IWorkDoneProgressParams
    {
        /// <summary>
        /// The new name of the symbol. If the given name is not valid the
        /// request must return a ResponseError with an
        /// appropriate message set.
        /// </summary>
        [JsonPropertyName("newName")]
        [JsonRequired]
        public string NewName { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName(Methods.WorkDoneTokenName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }
    }
}
