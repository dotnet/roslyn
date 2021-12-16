// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    internal class SemanticTokensHelpers
    {
        internal static readonly string[] RoslynCustomTokenTypes =
        {
            ClassificationTypeNames.ConstantName,
            ClassificationTypeNames.ControlKeyword,
            ClassificationTypeNames.DelegateName,
            ClassificationTypeNames.ExcludedCode,
            ClassificationTypeNames.ExtensionMethodName,
            ClassificationTypeNames.FieldName,
            ClassificationTypeNames.LabelName,
            ClassificationTypeNames.LocalName,
            ClassificationTypeNames.MethodName,
            ClassificationTypeNames.ModuleName,
            ClassificationTypeNames.OperatorOverloaded,
            ClassificationTypeNames.RecordClassName,

            // Preprocessor
            ClassificationTypeNames.PreprocessorKeyword,
            ClassificationTypeNames.PreprocessorText,

            ClassificationTypeNames.Punctuation,

            // Regex
            ClassificationTypeNames.RegexAlternation,
            ClassificationTypeNames.RegexAnchor,
            ClassificationTypeNames.RegexCharacterClass,
            ClassificationTypeNames.RegexComment,
            ClassificationTypeNames.RegexGrouping,
            ClassificationTypeNames.RegexOtherEscape,
            ClassificationTypeNames.RegexQuantifier,
            ClassificationTypeNames.RegexSelfEscapedCharacter,
            ClassificationTypeNames.RegexText,

            ClassificationTypeNames.StringEscapeCharacter,
            ClassificationTypeNames.Text,
            ClassificationTypeNames.VerbatimStringLiteral,
            ClassificationTypeNames.WhiteSpace,

            // XML
            ClassificationTypeNames.XmlDocCommentAttributeName,
            ClassificationTypeNames.XmlDocCommentAttributeQuotes,
            ClassificationTypeNames.XmlDocCommentAttributeValue,
            ClassificationTypeNames.XmlDocCommentCDataSection,
            ClassificationTypeNames.XmlDocCommentComment,
            ClassificationTypeNames.XmlDocCommentDelimiter,
            ClassificationTypeNames.XmlDocCommentEntityReference,
            ClassificationTypeNames.XmlDocCommentName,
            ClassificationTypeNames.XmlDocCommentProcessingInstruction,
            ClassificationTypeNames.XmlDocCommentText,
            ClassificationTypeNames.XmlLiteralAttributeName,
            ClassificationTypeNames.XmlLiteralAttributeQuotes,
            ClassificationTypeNames.XmlLiteralAttributeValue,
            ClassificationTypeNames.XmlLiteralCDataSection,
            ClassificationTypeNames.XmlLiteralComment,
            ClassificationTypeNames.XmlLiteralDelimiter,
            ClassificationTypeNames.XmlLiteralEmbeddedExpression,
            ClassificationTypeNames.XmlLiteralEntityReference,
            ClassificationTypeNames.XmlLiteralName,
            ClassificationTypeNames.XmlLiteralProcessingInstruction,
            ClassificationTypeNames.XmlLiteralText
        };

        // TO-DO: Expand this mapping once support for custom token types is added:
        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1085998
        private static readonly Dictionary<string, string> s_classificationTypeToSemanticTokenTypeMap =
            new Dictionary<string, string>
            {
                [ClassificationTypeNames.ClassName] = LSP.SemanticTokenTypes.Class,
                [ClassificationTypeNames.Comment] = LSP.SemanticTokenTypes.Comment,
                [ClassificationTypeNames.EnumMemberName] = LSP.SemanticTokenTypes.EnumMember,
                [ClassificationTypeNames.EnumName] = LSP.SemanticTokenTypes.Enum,
                [ClassificationTypeNames.EventName] = LSP.SemanticTokenTypes.Event,
                [ClassificationTypeNames.Identifier] = LSP.SemanticTokenTypes.Variable,
                [ClassificationTypeNames.InterfaceName] = LSP.SemanticTokenTypes.Interface,
                [ClassificationTypeNames.Keyword] = LSP.SemanticTokenTypes.Keyword,
                [ClassificationTypeNames.NamespaceName] = LSP.SemanticTokenTypes.Namespace,
                [ClassificationTypeNames.NumericLiteral] = LSP.SemanticTokenTypes.Number,
                [ClassificationTypeNames.Operator] = LSP.SemanticTokenTypes.Operator,
                [ClassificationTypeNames.ParameterName] = LSP.SemanticTokenTypes.Parameter,
                [ClassificationTypeNames.PropertyName] = LSP.SemanticTokenTypes.Property,
                [ClassificationTypeNames.StringLiteral] = LSP.SemanticTokenTypes.String,
                [ClassificationTypeNames.StructName] = LSP.SemanticTokenTypes.Struct,
                [ClassificationTypeNames.TypeParameterName] = LSP.SemanticTokenTypes.TypeParameter,
            };

        /// <summary>
        /// Returns the semantic tokens data for a given document with an optional range.
        /// </summary>
        internal static async Task<int[]> ComputeSemanticTokensDataAsync(
            Document document,
            Dictionary<string, int> tokenTypesToIndex,
            LSP.Range? range,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // By default we calculate the tokens for the full document span, although the user 
            // can pass in a range if they wish.
            var textSpan = range == null ? root.FullSpan : ProtocolConversions.RangeToTextSpan(range, text);

            var classifiedSpans = await Classifier.GetClassifiedSpansAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(classifiedSpans, "classifiedSpans is null");

            // TO-DO: We should implement support for streaming once this LSP bug is fixed:
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1132601
            return ComputeTokens(text.Lines, classifiedSpans.ToArray(), tokenTypesToIndex);
        }

        private static int[] ComputeTokens(
            TextLineCollection lines,
            ClassifiedSpan[] classifiedSpans,
            Dictionary<string, int> tokenTypesToIndex)
        {
            using var _ = ArrayBuilder<int>.GetInstance(classifiedSpans.Length, out var data);

            // We keep track of the last line number and last start character since tokens are
            // reported relative to each other.
            var lastLineNumber = 0;
            var lastStartCharacter = 0;

            for (var currentClassifiedSpanIndex = 0; currentClassifiedSpanIndex < classifiedSpans.Length; currentClassifiedSpanIndex++)
            {
                currentClassifiedSpanIndex = ComputeNextToken(
                    lines, ref lastLineNumber, ref lastStartCharacter, classifiedSpans,
                    currentClassifiedSpanIndex, tokenTypesToIndex,
                    out var deltaLine, out var startCharacterDelta, out var tokenLength,
                    out var tokenType, out var tokenModifiers);

                data.AddRange(deltaLine, startCharacterDelta, tokenLength, tokenType, tokenModifiers);
            }

            return data.ToArray();
        }

        private static int ComputeNextToken(
            TextLineCollection lines,
            ref int lastLineNumber,
            ref int lastStartCharacter,
            ClassifiedSpan[] classifiedSpans,
            int currentClassifiedSpanIndex,
            Dictionary<string, int> tokenTypesToIndex,
            out int deltaLineOut,
            out int startCharacterDeltaOut,
            out int tokenLengthOut,
            out int tokenTypeOut,
            out int tokenModifiersOut)
        {
            // Each semantic token is represented in LSP by five numbers:
            //     1. Token line number delta, relative to the previous token
            //     2. Token start character delta, relative to the previous token
            //     3. Token length
            //     4. Token type (index) - looked up in SemanticTokensLegend.tokenTypes
            //     5. Token modifiers - each set bit will be looked up in SemanticTokensLegend.tokenModifiers

            var classifiedSpan = classifiedSpans[currentClassifiedSpanIndex];
            var originalTextSpan = classifiedSpan.TextSpan;
            var linePosition = lines.GetLinePositionSpan(originalTextSpan).Start;
            var lineNumber = linePosition.Line;

            // 1. Token line number delta, relative to the previous token
            var deltaLine = lineNumber - lastLineNumber;
            Contract.ThrowIfTrue(deltaLine < 0, $"deltaLine is less than 0: {deltaLine}");

            // 2. Token start character delta, relative to the previous token
            // (Relative to 0 or the previous token’s start if they're on the same line)
            var deltaStartCharacter = linePosition.Character;
            if (lastLineNumber == lineNumber)
            {
                deltaStartCharacter -= lastStartCharacter;
            }

            lastLineNumber = lineNumber;
            lastStartCharacter = linePosition.Character;

            // 3. Token length
            var tokenLength = originalTextSpan.Length;

            // We currently only have one modifier (static). The logic below will need to change in the future if other
            // modifiers are added in the future.
            var modifierBits = TokenModifiers.None;
            var tokenTypeIndex = 0;

            // Classified spans with the same text span should be combined into one token.
            while (classifiedSpans[currentClassifiedSpanIndex].TextSpan == originalTextSpan)
            {
                var classificationType = classifiedSpans[currentClassifiedSpanIndex].ClassificationType;
                if (classificationType != ClassificationTypeNames.StaticSymbol)
                {
                    // 4. Token type - looked up in SemanticTokensLegend.tokenTypes (language server defined mapping
                    // from integer to LSP token types).
                    tokenTypeIndex = GetTokenTypeIndex(classificationType, tokenTypesToIndex);
                }
                else
                {
                    // 5. Token modifiers - each set bit will be looked up in SemanticTokensLegend.tokenModifiers
                    modifierBits = TokenModifiers.Static;
                }

                // Break out of the loop if we have no more classified spans left, or if the next classified span has
                // a different text span than our current text span.
                if (currentClassifiedSpanIndex + 1 >= classifiedSpans.Length || classifiedSpans[currentClassifiedSpanIndex + 1].TextSpan != originalTextSpan)
                {
                    break;
                }

                currentClassifiedSpanIndex++;
            }

            deltaLineOut = deltaLine;
            startCharacterDeltaOut = deltaStartCharacter;
            tokenLengthOut = tokenLength;
            tokenTypeOut = tokenTypeIndex;
            tokenModifiersOut = (int)modifierBits;

            return currentClassifiedSpanIndex;
        }

        private static int GetTokenTypeIndex(string classificationType, Dictionary<string, int> tokenTypesToIndex)
        {
            if (!s_classificationTypeToSemanticTokenTypeMap.TryGetValue(classificationType, out var tokenTypeStr))
            {
                tokenTypeStr = classificationType;
            }

            Contract.ThrowIfFalse(tokenTypesToIndex.TryGetValue(tokenTypeStr, out var tokenTypeIndex), "No matching token type index found.");
            return tokenTypeIndex;
        }
    }
}
