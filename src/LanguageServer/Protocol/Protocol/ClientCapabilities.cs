// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents client capabilities.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#clientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </para>
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
        /// Capabilities specific to the notebook document support.
        /// </summary>
        /// <remarks>Since LSP 3.17</remarks>
        [JsonPropertyName("notebook")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public NotebookDocumentClientCapabilities? Notebook { get; init; }

        /// <summary>
        /// Window specific client capabilities.
        /// </summary>
        [JsonPropertyName("window")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public WindowClientCapabilities? Window { get; init; }

        /// <summary>
        /// Capabilities specific to the notebook document support.
        /// </summary>
        /// <remarks>Since LSP 3.17</remarks>
        [JsonPropertyName("general")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeneralClientCapabilities? General { get; init; }

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
