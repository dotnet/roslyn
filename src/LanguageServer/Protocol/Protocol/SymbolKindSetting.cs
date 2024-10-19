// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the <see cref="SymbolKind"/> values that the client supports.
    /// </summary>
    internal class SymbolKindSetting
    {
        /// <summary>
        /// The symbol kind values the client supports.
        /// <para>
        /// When this property exists the client also guarantees that it will handle values outside
        /// its set gracefully and falls back to a default value when unknown.
        /// </para>
        /// <para>
        /// If this property is not present the client only supports the symbol kinds
        /// from <see cref="SymbolKind.File"/> to <see cref="SymbolKind.Array"/> as
        /// defined in the initial version of the protocol.
        /// </para>
        /// </summary>
        [JsonPropertyName("valueSet")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SymbolKind[]? ValueSet
        {
            get;
            set;
        }
    }
}
