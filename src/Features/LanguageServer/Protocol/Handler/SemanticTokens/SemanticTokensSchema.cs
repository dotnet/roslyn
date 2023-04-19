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
    internal readonly struct SemanticTokensSchema
    {
        // TO-DO: Expand this mapping once support for custom token types is added:
        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1085998

        /// <summary>
        /// Core VS classifications, only map a few things to LSP.  The rest we keep as our own standard classification
        /// type names so those continue to work in VS.
        /// </summary>
        private static readonly SemanticTokensSchema s_vsTokenSchema = new(
             new Dictionary<string, string>
             {
                 [ClassificationTypeNames.Comment] = SemanticTokenTypes.Comment,
                 [ClassificationTypeNames.Identifier] = SemanticTokenTypes.Variable,
                 [ClassificationTypeNames.Keyword] = SemanticTokenTypes.Keyword,
                 [ClassificationTypeNames.NumericLiteral] = SemanticTokenTypes.Number,
                 [ClassificationTypeNames.Operator] = SemanticTokenTypes.Operator,
                 [ClassificationTypeNames.StringLiteral] = SemanticTokenTypes.String,
             });

        /// <summary>
        /// The 'pure' set of classification types maps everything reasonable to the well defined values actually in LSP.
        /// </summary>
        private static readonly SemanticTokensSchema s_pureLspTokenSchema = new(
            s_vsTokenSchema.TokenTypeMap.Concat(new Dictionary<string, string>
            {
                // No specific lsp property for this.
                [ClassificationTypeNames.ControlKeyword] = SemanticTokenTypes.Keyword,

                // No specific lsp property for this.
                [ClassificationTypeNames.OperatorOverloaded] = SemanticTokenTypes.Operator,

                // No specific lsp property for this.
                [ClassificationTypeNames.VerbatimStringLiteral] = SemanticTokenTypes.String,

                // No specific lsp property for all of these
                [ClassificationTypeNames.ClassName] = SemanticTokenTypes.Class,
                [ClassificationTypeNames.RecordClassName] = SemanticTokenTypes.Class,
                [ClassificationTypeNames.DelegateName] = SemanticTokenTypes.Class,
                [ClassificationTypeNames.ModuleName] = SemanticTokenTypes.Class,

                // No specific lsp property for both of these
                [ClassificationTypeNames.StructName] = SemanticTokenTypes.Struct,
                [ClassificationTypeNames.RecordStructName] = SemanticTokenTypes.Struct,

                [ClassificationTypeNames.NamespaceName] = SemanticTokenTypes.Namespace,
                [ClassificationTypeNames.EnumName] = SemanticTokenTypes.Enum,
                [ClassificationTypeNames.InterfaceName] = SemanticTokenTypes.Interface,
                [ClassificationTypeNames.TypeParameterName] = SemanticTokenTypes.TypeParameter,
                [ClassificationTypeNames.ParameterName] = SemanticTokenTypes.Parameter,
                [ClassificationTypeNames.LocalName] = SemanticTokenTypes.Variable,

                // No specific lsp property for all of these
                [ClassificationTypeNames.PropertyName] = SemanticTokenTypes.Property,
                [ClassificationTypeNames.FieldName] = SemanticTokenTypes.Property,
                [ClassificationTypeNames.ConstantName] = SemanticTokenTypes.Property,

                // No specific lsp property for all of these
                [ClassificationTypeNames.MethodName] = SemanticTokenTypes.Method,
                [ClassificationTypeNames.ExtensionMethodName] = SemanticTokenTypes.Method,

                [ClassificationTypeNames.EnumMemberName] = SemanticTokenTypes.EnumMember,
                [ClassificationTypeNames.EventName] = SemanticTokenTypes.Event,
                [ClassificationTypeNames.PreprocessorKeyword] = SemanticTokenTypes.Macro,

                // in https://code.visualstudio.com/api/language-extensions/semantic-highlight-guide#standard-token-types-and-modifiers
                [ClassificationTypeNames.LabelName] = "label",

                // No specific lsp property for all of these
                [ClassificationTypeNames.RegexComment] = SemanticTokenTypes.Regexp,
                [ClassificationTypeNames.RegexCharacterClass] = SemanticTokenTypes.Regexp,
                [ClassificationTypeNames.RegexAnchor] = SemanticTokenTypes.Regexp,
                [ClassificationTypeNames.RegexQuantifier] = SemanticTokenTypes.Regexp,
                [ClassificationTypeNames.RegexGrouping] = SemanticTokenTypes.Regexp,
                [ClassificationTypeNames.RegexAlternation] = SemanticTokenTypes.Regexp,
                [ClassificationTypeNames.RegexText] = SemanticTokenTypes.Regexp,
                [ClassificationTypeNames.RegexSelfEscapedCharacter] = SemanticTokenTypes.Regexp,
                [ClassificationTypeNames.RegexOtherEscape] = SemanticTokenTypes.Regexp,

                // TODO: Missing lsp classifications for xml doc comments, xml literals (vb), json.

                // TODO: Missing specific lsp classifications for the following classification type names.

#if false
                public const string ExcludedCode = "excluded code";
                public const string WhiteSpace = "whitespace";
                public const string Text = "text";

                internal const string ReassignedVariable = "reassigned variable";
                public const string StaticSymbol = "static symbol";

                public const string PreprocessorText = "preprocessor text";
                public const string Punctuation = "punctuation";
                public const string StringEscapeCharacter = "string - escape character";
#endif

            }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

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

        public SemanticTokensSchema(IReadOnlyDictionary<string, string> tokenTypeMap)
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

        public static SemanticTokensSchema GetSchema(ClientCapabilities capabilities)
            => capabilities.HasVisualStudioLspCapability()
                ? s_vsTokenSchema
                : s_pureLspTokenSchema;

        public static SemanticTokensSchema LegacyTokenSchemaForRazor
            => s_vsTokenSchema;

        public static SemanticTokensSchema LegacyTokensSchemaForLSIF
            => s_vsTokenSchema;
    }
}
