// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents client capabilities.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#clientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class ClientCapabilities
    {
        /// <summary>
        /// Gets or sets the workspace capabilities.
        /// </summary>
        [DataMember(Name = "workspace")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public WorkspaceClientCapabilities? Workspace
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the text document capabilities.
        /// </summary>
        [DataMember(Name = "textDocument")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TextDocumentClientCapabilities? TextDocument
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the experimental capabilities.
        /// </summary>
        [DataMember(Name = "experimental")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object? Experimental
        {
            get;
            set;
        }
    }
}
