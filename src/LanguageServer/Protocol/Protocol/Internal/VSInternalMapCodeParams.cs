﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// LSP Params for textDocument/mapCode calls.
    /// </summary>
    internal class VSInternalMapCodeParams
    {
        /// <summary>
        /// Set of code blocks, associated with documents and regions, to map.
        /// </summary>
        [JsonPropertyName("_vs_mappings")]
        public VSInternalMapCodeMapping[] Mappings
        {
            get;
            set;
        }

        /// <summary>
        /// Changes that should be applied to the workspace by the mapper before performing
        /// the mapping operation.
        /// </summary>
        [JsonPropertyName("_vs_updates")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public WorkspaceEdit? Updates
        {
            get;
            set;
        }
    }
}
