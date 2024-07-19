// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents client capabilities.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#clientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class ClientCapabilities
    {
        /// <summary>
        /// Gets or sets the workspace capabilities.
        /// </summary>
        [JsonPropertyName("workspace")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public WorkspaceClientCapabilities? Workspace
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the text document capabilities.
        /// </summary>
        [JsonPropertyName("textDocument")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TextDocumentClientCapabilities? TextDocument
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the experimental capabilities.
        /// </summary>
        [JsonPropertyName("experimental")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Experimental
        {
            get;
            set;
        }
    }
}
