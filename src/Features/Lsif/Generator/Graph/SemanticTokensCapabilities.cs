// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    internal sealed class SemanticTokensCapabilities
    {
        public SemanticTokensCapabilities(
            IReadOnlyList<string>? tokenTypes,
            IReadOnlyList<string>? tokenModifiers)
        {
            this.TokenTypes = tokenTypes;
            this.TokenModifiers = tokenModifiers;
        }

        [JsonProperty("tokenTypes")]
        public IReadOnlyList<string>? TokenTypes { get; }

        [JsonProperty("tokenModifiers")]
        public IReadOnlyList<string>? TokenModifiers { get; }
    }
}
