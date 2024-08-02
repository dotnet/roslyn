// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Classification;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    internal readonly struct SemanticTokensSchema
    {
        // TO-DO: Expand this mapping once support for custom token types is added:
        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1085998

        /// <summary>
        /// Core VS classifications, only map a few things to LSP.  The rest we keep as our own standard classification
        /// type names so those continue to work in VS.
        /// </summary>
        private static readonly ImmutableDictionary<string, string> s_vsDirectTypeMap = new Dictionary<string, string>()
        {
            [ClassificationTypeNames.Comment] = SemanticTokenTypes.Comment,
            [ClassificationTypeNames.Identifier] = SemanticTokenTypes.Variable,
            [ClassificationTypeNames.Keyword] = SemanticTokenTypes.Keyword,
            [ClassificationTypeNames.NumericLiteral] = SemanticTokenTypes.Number,
            [ClassificationTypeNames.Operator] = SemanticTokenTypes.Operator,
            [ClassificationTypeNames.StringLiteral] = SemanticTokenTypes.String,
        }.ToImmutableDictionary();

        /// <summary>
        /// The 'pure' set of classification types maps exact Roslyn matches to the well defined values actually in LSP.
        /// For example "class name" to "class".  Importantly though, if there is no exact match, we do not map things
        /// along.  This allows the user to theme things however they want.  
        /// </summary>
        private static readonly ImmutableDictionary<string, string> s_pureLspDirectTypeMap = s_vsDirectTypeMap.Concat(new Dictionary<string, string>
        {
            [ClassificationTypeNames.ClassName] = SemanticTokenTypes.Class,
            [ClassificationTypeNames.StructName] = SemanticTokenTypes.Struct,
            [ClassificationTypeNames.NamespaceName] = SemanticTokenTypes.Namespace,
            [ClassificationTypeNames.EnumName] = SemanticTokenTypes.Enum,
            [ClassificationTypeNames.InterfaceName] = SemanticTokenTypes.Interface,
            [ClassificationTypeNames.TypeParameterName] = SemanticTokenTypes.TypeParameter,
            [ClassificationTypeNames.ParameterName] = SemanticTokenTypes.Parameter,
            [ClassificationTypeNames.LocalName] = SemanticTokenTypes.Variable,
            [ClassificationTypeNames.PropertyName] = SemanticTokenTypes.Property,
            [ClassificationTypeNames.MethodName] = SemanticTokenTypes.Method,
            [ClassificationTypeNames.EnumMemberName] = SemanticTokenTypes.EnumMember,
            [ClassificationTypeNames.EventName] = SemanticTokenTypes.Event,
            [ClassificationTypeNames.PreprocessorKeyword] = SemanticTokenTypes.Macro,
            // in https://code.visualstudio.com/api/language-extensions/semantic-highlight-guide#standard-token-types-and-modifiers
            [ClassificationTypeNames.LabelName] = "label",
        }).ToImmutableDictionary();

        /// <summary>
        /// A schema for mapping classification type names to 'pure' LSP token names.  This includes classification type names
        /// that are directly mapped to LSP semantic token types as well as mappings from roslyn classification type names to
        /// LSP compatible custom token type names.
        /// </summary>
        private static readonly SemanticTokensSchema s_pureLspTokenSchema = new(ClassificationTypeNames.AllTypeNames
            .Where(classificationTypeName => !ClassificationTypeNames.AdditiveTypeNames.Contains(classificationTypeName))
            .ToImmutableDictionary(
                classificationTypeName => classificationTypeName,
                classificationTypeName => IDictionaryExtensions.GetValueOrDefault(s_pureLspDirectTypeMap, classificationTypeName) ?? CustomLspSemanticTokenNames.ClassificationTypeNameToCustomTokenName[classificationTypeName]));

        /// <summary>
        /// Mapping from roslyn <see cref="ClassificationTypeNames"/> to the LSP token name.  This is either a standard
        /// <see cref="SemanticTokenTypes"/> or a custom token name.
        /// </summary>
        public readonly IReadOnlyDictionary<string, string> TokenTypeMap;

        /// <summary>
        /// Mapping from the semantic token type name to the index in <see cref="AllTokenTypes"/>.  Required since we report
        /// tokens back to LSP as a series of ints, and LSP needs a way to decipher them.
        /// </summary>
        public readonly IReadOnlyDictionary<string, int> TokenTypeToIndex;

        /// <summary>
        /// Equivalent to see <see cref="SemanticTokenTypes.AllTypes"/> combined with the remaining custom token names from <see cref="TokenTypeMap"/> 
        /// </summary>
        public readonly ImmutableArray<string> AllTokenTypes;

        public SemanticTokensSchema(IReadOnlyDictionary<string, string> tokenTypeMap)
        {
            TokenTypeMap = tokenTypeMap;

            // Get all custom token type names that don't directly map to an built-in LSP semantic token type.
            var customTokenTypes = TokenTypeMap.Values
                .Where(tokenType => !SemanticTokenTypes.AllTypes.Contains(tokenType))
                .Order()
                .ToImmutableArray();

            AllTokenTypes = [.. SemanticTokenTypes.AllTypes, .. customTokenTypes];

            var tokenTypeToIndex = new Dictionary<string, int>();

            foreach (var lspTokenType in SemanticTokenTypes.AllTypes)
                tokenTypeToIndex.Add(lspTokenType, tokenTypeToIndex.Count);

            foreach (var roslynTokenType in customTokenTypes)
                tokenTypeToIndex.Add(roslynTokenType, tokenTypeToIndex.Count);

            TokenTypeToIndex = tokenTypeToIndex;
        }

        public static SemanticTokensSchema GetSchema(bool clientSupportsVisualStudioExtensions)
            => clientSupportsVisualStudioExtensions
                ? LegacyTokensSchemaForLSIF
                : s_pureLspTokenSchema;

        public static SemanticTokensSchema LegacyTokenSchemaForRazor
            => LegacyTokensSchemaForLSIF;

        public static SemanticTokensSchema LegacyTokensSchemaForLSIF { get; } = new(ClassificationTypeNames.AllTypeNames
            .Where(classificationTypeName => !ClassificationTypeNames.AdditiveTypeNames.Contains(classificationTypeName))
            .ToImmutableDictionary(
                classificationTypeName => classificationTypeName,
                classificationTypeName => IDictionaryExtensions.GetValueOrDefault(s_vsDirectTypeMap, classificationTypeName) ?? classificationTypeName));

        public static string[] TokenModifiers =
        [
            // This must be in the same order as SemanticTokens.TokenModifiers, but skip the "None" item
            SemanticTokenModifiers.Static,
            nameof(SemanticTokens.TokenModifiers.ReassignedVariable),
            SemanticTokenModifiers.Deprecated,
        ];
    }
}
