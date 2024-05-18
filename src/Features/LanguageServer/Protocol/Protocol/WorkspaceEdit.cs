// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing a request sent from a language server to modify resources in the workspace.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspaceEdit">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class WorkspaceEdit
    {
        /// <summary>
        /// Gets or sets a dictionary holding changes to existing resources.
        /// </summary>
        [JsonPropertyName("changes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, TextEdit[]>? Changes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an array representing versioned document changes.
        /// </summary>
        [JsonPropertyName("documentChanges")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<TextDocumentEdit[], SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[]>? DocumentChanges
        {
            get;
            set;
        }
    }
}
