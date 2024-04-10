// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// <see cref="VSSymbolInformation"/> extends <see cref="SymbolInformation"/> providing additional properties used by Visual Studio.
    /// </summary>
    [DataContract]
    internal class VSSymbolInformation : SymbolInformation
    {
        /// <summary>
        /// Gets or sets the icon associated with the symbol. If specified, this icon is used instead of <see cref="SymbolKind" />.
        /// </summary>
        [DataMember(Name = "_vs_icon")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSImageId? Icon { get; set; }

        /// <summary>
        /// Gets or sets the description of the symbol.
        /// </summary>
        [DataMember(Name = "_vs_description")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the hint text for the symbol.
        /// </summary>
        [DataMember(Name = "_vs_hintText")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? HintText { get; set; }
    }
}
