// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    internal readonly struct SemanticTokenSchema
    {
        /// <summary>
        /// Mapping from roslyn <see cref="ClassificationTypeNames"/> to the LSP <see cref="SemanticTokenTypes"/> we
        /// should use for them.  If something is not mapped, we will pass along the roslyn type name along.
        /// </summary>
        public readonly IReadOnlyDictionary<string, string> TokenTypeMap;

        /// <summary>
        /// Mapping from classification name to the index in <see cref="CustomTokenTypes"/>.  Required since we report
        /// tokens back to LSP as a series of ints, and LSP needs a way to decipher them.
        /// </summary>
        public readonly IReadOnlyDictionary<string, int> TokenTypeToIndex;

        /// <summary>
        /// The token types that Roslyn specifically defines for a particular client.
        /// </summary>
        public readonly ImmutableArray<string> CustomTokenTypes;

        /// <summary>
        /// Equivalent to <see cref="CustomTokenTypes"/> and <see cref="SemanticTokenTypes.AllTypes"/> combined.
        /// </summary>
        public readonly ImmutableArray<string> AllTokenTypes;

        public SemanticTokenSchema(IReadOnlyDictionary<string, string> tokenTypeMap)
        {
            TokenTypeMap = tokenTypeMap;

            CustomTokenTypes = ClassificationTypeNames.AllTypeNames
                .Where(type => !tokenTypeMap.ContainsKey(type) && !ClassificationTypeNames.AdditiveTypeNames.Contains(type))
                .Order()
                .ToImmutableArray();

            AllTokenTypes = SemanticTokenTypes.AllTypes.Concat(CustomTokenTypes).ToImmutableArray();

            var tokenTypeToIndex = new Dictionary<string, int>();

            foreach (var lspTokenType in SemanticTokenTypes.AllTypes)
                tokenTypeToIndex.Add(lspTokenType, tokenTypeToIndex.Count);

            foreach (var roslynTokenType in CustomTokenTypes)
                tokenTypeToIndex.Add(roslynTokenType, tokenTypeToIndex.Count);

            TokenTypeToIndex = tokenTypeToIndex;
        }
    }
}
