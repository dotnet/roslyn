// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// LSP Params for textDocument/mapCode calls.
    /// </summary>
    [DataContract]
    internal class VSInternalMapCodeParams
    {
        /// <summary>
        /// Set of code blocks, associated with documents and regions, to map.
        /// </summary>
        [DataMember(Name = "_vs_mappings")]
        public VSInternalMapCodeMapping[] Mappings
        {
            get;
            set;
        }

        /// <summary>
        /// Changes that should be applied to the workspace by the mapper before performing
        /// the mapping operation.
        /// </summary>
        [DataMember(Name = "_vs_updates")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public WorkspaceEdit? Updates
        {
            get;
            set;
        }
    }
}
