// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the symbol setting for initialization.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_symbol">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class SymbolSetting : DynamicRegistrationSetting
    {
        /// <summary>
        /// Gets or sets the <see cref="SymbolKindSetting"/> information.
        /// </summary>
        [DataMember(Name = "symbolKind")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SymbolKindSetting? SymbolKind
        {
            get;
            set;
        }
    }
}