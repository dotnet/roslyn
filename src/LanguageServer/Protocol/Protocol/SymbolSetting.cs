// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Capabilities specific to the <c>workspace/symbol</c> request.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_symbol">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class SymbolSetting : DynamicRegistrationSetting
    {
        /// <summary>
        /// Specific capabilities for <see cref="SymbolKind"/> in <c>workspace/symbol</c> requests
        /// </summary>
        [JsonPropertyName("symbolKind")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SymbolKindSetting? SymbolKind
        {
            get;
            set;
        }
        /// <summary>
        /// The client supports tags on <see cref="SymbolInformation"/> and <see cref="WorkspaceSymbol"/>.
        /// <para>
        /// Clients supporting tags have to handle unknown tags gracefully.
        /// </para>
        /// </summary>
        /// <remarks>Since LSP 3.16</remarks>
        [JsonPropertyName("tagSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SymbolTagSupport? TagSupport { get; init; }

        /// <summary>
        /// The client support partial workspace symbols. The client will send the
        /// request `workspaceSymbol/resolve` to the server to resolve additional
        /// properties.
        /// </summary>
        /// <remarks>Since LSP 3.17</remarks>
        [JsonPropertyName("resolveSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public WorkspaceSymbolResolveSupport? ResolveSupport { get; init; }
    }
}
