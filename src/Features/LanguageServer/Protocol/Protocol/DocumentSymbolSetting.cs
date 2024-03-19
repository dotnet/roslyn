// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the initialization setting for document symbols.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentSymbolClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class DocumentSymbolSetting : DynamicRegistrationSetting
    {
        /// <summary>
        /// Gets or sets the <see cref="SymbolKindSetting"/> capabilities.
        /// </summary>
        [DataMember(Name = "symbolKind")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SymbolKindSetting? SymbolKind
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the document has hierarchical symbol support.
        /// </summary>
        [DataMember(Name = "hierarchicalDocumentSymbolSupport")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool HierarchicalDocumentSymbolSupport
        {
            get;
            set;
        }
    }
}