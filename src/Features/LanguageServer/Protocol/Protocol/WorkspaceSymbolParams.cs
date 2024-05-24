// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents the parameter that's sent with the 'workspace/symbol' request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspaceSymbolParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class WorkspaceSymbolParams : IPartialResultParams<SymbolInformation[]>
    {
        /// <summary>
        /// Gets or sets the query (a non-empty string).
        /// </summary>
        [JsonPropertyName("query")]
        public string Query
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value of the Progress instance.
        /// </summary>
        [JsonPropertyName(Methods.PartialResultTokenName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IProgress<SymbolInformation[]>? PartialResultToken
        {
            get;
            set;
        }
    }
}
