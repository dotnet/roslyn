// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the symbol kind setting in initialization.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentSymbolClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class SymbolKindSetting
    {
        /// <summary>
        /// Gets or sets the types of symbol kind the client supports.
        /// </summary>
        [DataMember(Name = "valueSet")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SymbolKind[]? ValueSet
        {
            get;
            set;
        }
    }
}