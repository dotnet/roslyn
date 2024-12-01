// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Extension class for SymbolInformation with fields specific to Visual Studio functionalities.
    /// </summary>
    /// <remarks>
    /// This is a temporary protocol and should not be used.
    /// </remarks>
    internal class VSInternalSymbolInformation : VSSymbolInformation
    {
        /// <summary>
        /// Gets or sets the string kind used for icons.
        /// </summary>
        [JsonPropertyName("_vs_vsKind")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public VSInternalKindAndModifier? VSKind { get; set; }
    }
}
