// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Client capabilities specific to the code lens requests scoped to the workspace.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeLensWorkspaceClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    internal class CodeLensWorkspaceSetting
    {
        /// <summary>
        /// Whether the client implementation supports a refresh request sent from the
        /// server to the client.
        ///
        /// Note that this event is global and will force the client to refresh all
        /// code lenses currently shown. It should be used with absolute care and is
        /// useful for situation where a server for example detect a project wide
        /// change that requires such a calculation.
        /// </summary>
        [JsonPropertyName("refreshSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool RefreshSupport { get; set; }
    }
}
