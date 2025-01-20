// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// <see cref="VSSymbolInformation"/> extends <see cref="SymbolInformation"/> providing additional properties used by Visual Studio.
    /// </summary>
    internal class VSSymbolInformation
#pragma warning disable CS0618 // SymbolInformation is obsolete but this class is not (yet)
        : SymbolInformation
#pragma warning restore
    {
        /// <summary>
        /// Gets or sets the icon associated with the symbol. If specified, this icon is used instead of <see cref="SymbolKind" />.
        /// </summary>
        [JsonPropertyName("_vs_icon")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public VSImageId? Icon { get; set; }

        /// <summary>
        /// Gets or sets the description of the symbol.
        /// </summary>
        [JsonPropertyName("_vs_description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the hint text for the symbol.
        /// </summary>
        [JsonPropertyName("_vs_hintText")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? HintText { get; set; }
    }
}
