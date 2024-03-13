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
        [System.Text.Json.Serialization.JsonPropertyName("_vs_icon")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public VSImageId? Icon { get; set; }

        /// <summary>
        /// Gets or sets the description of the symbol.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("_vs_description")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the hint text for the symbol.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("_vs_hintText")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? HintText { get; set; }
    }
}
