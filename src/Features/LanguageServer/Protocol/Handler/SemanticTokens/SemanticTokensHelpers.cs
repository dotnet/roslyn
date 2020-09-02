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

        // TO-DO: Change this mapping once support for custom token types is added:
        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1085998
        private static readonly Dictionary<string, string> s_classificationTypeToSemanticTokenTypeMap =
            new Dictionary<string, string>
            {
                [ClassificationTypeNames.ClassName] = LSP.SemanticTokenTypes.Class,
                [ClassificationTypeNames.Comment] = LSP.SemanticTokenTypes.Comment,
                [ClassificationTypeNames.ConstantName] = ClassificationTypeNames.ConstantName, // TO-DO: Change to custom type
                [ClassificationTypeNames.ControlKeyword] = ClassificationTypeNames.ControlKeyword, // TO-DO: Change to custom type
                [ClassificationTypeNames.DelegateName] = ClassificationTypeNames.DelegateName, // TO-DO: Change to custom type
                [ClassificationTypeNames.EnumMemberName] = LSP.SemanticTokenTypes.EnumMember,
                [ClassificationTypeNames.EnumName] = LSP.SemanticTokenTypes.Enum,
                [ClassificationTypeNames.EventName] = LSP.SemanticTokenTypes.Event,
                [ClassificationTypeNames.ExcludedCode] = ClassificationTypeNames.ExcludedCode, // TO-DO: Change to custom type
                [ClassificationTypeNames.ExtensionMethodName] = ClassificationTypeNames.ExtensionMethodName, // TO-DO: Change to custom type
                [ClassificationTypeNames.FieldName] = ClassificationTypeNames.FieldName, // TO-DO: Change to custom type
                [ClassificationTypeNames.Identifier] = LSP.SemanticTokenTypes.Variable,
                [ClassificationTypeNames.InterfaceName] = LSP.SemanticTokenTypes.Interface,
                [ClassificationTypeNames.Keyword] = LSP.SemanticTokenTypes.Keyword,
                [ClassificationTypeNames.LabelName] = ClassificationTypeNames.LabelName, // TO-DO: Change to custom type
                [ClassificationTypeNames.LocalName] = ClassificationTypeNames.LocalName, // TO-DO: Change to custom type
                [ClassificationTypeNames.MethodName] = ClassificationTypeNames.MethodName, // TO-DO: Change to custom type
                [ClassificationTypeNames.ModuleName] = ClassificationTypeNames.ModuleName, // TO-DO: Change to custom type
                [ClassificationTypeNames.NamespaceName] = LSP.SemanticTokenTypes.Namespace,
                [ClassificationTypeNames.NumericLiteral] = LSP.SemanticTokenTypes.Number,
                [ClassificationTypeNames.Operator] = LSP.SemanticTokenTypes.Operator,
                [ClassificationTypeNames.OperatorOverloaded] = ClassificationTypeNames.OperatorOverloaded, // TO-DO: Change to custom type
                [ClassificationTypeNames.ParameterName] = LSP.SemanticTokenTypes.Parameter,
                [ClassificationTypeNames.PreprocessorKeyword] = ClassificationTypeNames.PreprocessorKeyword, // TO-DO: Change to custom type
                [ClassificationTypeNames.PreprocessorText] = ClassificationTypeNames.PreprocessorText, // TO-DO: Change to custom type
                [ClassificationTypeNames.PropertyName] = LSP.SemanticTokenTypes.Property,
                [ClassificationTypeNames.Punctuation] = ClassificationTypeNames.Punctuation, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexAlternation] = ClassificationTypeNames.RegexAlternation, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexAnchor] = ClassificationTypeNames.RegexAnchor, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexCharacterClass] = ClassificationTypeNames.RegexCharacterClass, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexComment] = ClassificationTypeNames.RegexComment, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexGrouping] = ClassificationTypeNames.RegexGrouping, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexOtherEscape] = ClassificationTypeNames.RegexOtherEscape, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexQuantifier] = ClassificationTypeNames.RegexQuantifier, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexSelfEscapedCharacter] = ClassificationTypeNames.RegexSelfEscapedCharacter, // TO-DO: Change to custom type
                [ClassificationTypeNames.RegexText] = ClassificationTypeNames.RegexText, // TO-DO: Change to custom type
                [ClassificationTypeNames.StringEscapeCharacter] = ClassificationTypeNames.StringEscapeCharacter, // TO-DO: Change to custom type
                [ClassificationTypeNames.StringLiteral] = LSP.SemanticTokenTypes.String,
                [ClassificationTypeNames.StructName] = LSP.SemanticTokenTypes.Struct,
                [ClassificationTypeNames.Text] = ClassificationTypeNames.Text, // TO-DO: Change to custom type
                [ClassificationTypeNames.TypeParameterName] = LSP.SemanticTokenTypes.TypeParameter,
                [ClassificationTypeNames.VerbatimStringLiteral] = ClassificationTypeNames.VerbatimStringLiteral, // TO-DO: Change to custom type
                [ClassificationTypeNames.WhiteSpace] = ClassificationTypeNames.WhiteSpace, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentAttributeName] = ClassificationTypeNames.XmlDocCommentAttributeName, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentAttributeQuotes] = ClassificationTypeNames.XmlDocCommentAttributeQuotes, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentAttributeValue] = ClassificationTypeNames.XmlDocCommentAttributeValue, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentCDataSection] = ClassificationTypeNames.XmlDocCommentCDataSection, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentComment] = ClassificationTypeNames.XmlDocCommentComment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentDelimiter] = ClassificationTypeNames.XmlDocCommentDelimiter, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentEntityReference] = ClassificationTypeNames.XmlDocCommentEntityReference, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentName] = ClassificationTypeNames.XmlDocCommentName, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentProcessingInstruction] = ClassificationTypeNames.XmlDocCommentProcessingInstruction, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlDocCommentText] = ClassificationTypeNames.XmlDocCommentText, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralAttributeName] = ClassificationTypeNames.XmlLiteralAttributeName, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralAttributeQuotes] = ClassificationTypeNames.XmlLiteralAttributeQuotes, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralAttributeValue] = ClassificationTypeNames.XmlLiteralAttributeValue, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralCDataSection] = ClassificationTypeNames.XmlLiteralCDataSection, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralComment] = ClassificationTypeNames.XmlLiteralComment, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralDelimiter] = ClassificationTypeNames.XmlLiteralDelimiter, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralEmbeddedExpression] = ClassificationTypeNames.XmlLiteralEmbeddedExpression, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralEntityReference] = ClassificationTypeNames.XmlLiteralEntityReference, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralName] = ClassificationTypeNames.XmlLiteralName, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralProcessingInstruction] = ClassificationTypeNames.XmlLiteralProcessingInstruction, // TO-DO: Change to custom type
                [ClassificationTypeNames.XmlLiteralText] = ClassificationTypeNames.XmlLiteralText, // TO-DO: Change to custom type
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

            UnprocessedSemanticToken? tokenToProcess = null;

            for (var currentClassifiedSpanIndex = 0; currentClassifiedSpanIndex < classifiedSpans.Length; currentClassifiedSpanIndex++)
            {
                currentClassifiedSpanIndex = ComputeNextToken(
                    lines, ref lastLineNumber, ref lastStartCharacter, classifiedSpans,
                    currentClassifiedSpanIndex, tokenTypesToIndex,
                    out var deltaLine, out var startCharacterDelta, out var tokenLength,
                    out var tokenType, out var tokenModifiers, out var unprocessedToken);

                data.AddRange(deltaLine, startCharacterDelta, tokenLength, tokenType, tokenModifiers);

                tokenToProcess ??= unprocessedToken;

                // Potentially process the unprocessed token (if one exists).
                // An unprocessed token may occur in files with strings containing regex or escape characters. For example, using
                // the string "Hello \n World", the earlier the call we make to Classifier.GetClassifiedSpansAsync will return two
                // classified spans: one for the entire string, and one for the escape character '\n'. However, this is problematic
                // when returning semantic tokens to LSP, since there is no way to indicate overlapping tokens unless one is a modifier.
                // We must therefore break the resulting classified spans into three tokens: "Hello", "\n", and "World", the last of
                // which is processed as an unprocessed token.
                if (tokenToProcess.HasValue && ProcessUnprocessedToken(
                    lines, tokenToProcess.Value, classifiedSpans, currentClassifiedSpanIndex,
                    data, ref lastLineNumber, ref lastStartCharacter, out tokenToProcess))
                {
                    tokenToProcess = null;
                }
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
            out int tokenModifiersOut,
            out UnprocessedSemanticToken? unprocessedTokenOut)
        {
            unprocessedTokenOut = null;

            // Each semantic token is represented in LSP by five numbers:
            //     1. Token line number delta, relative to the previous token
            //     2. Token start character delta, relative to the previous token
            //     3. Token length
            //     4. Token type (index) - looked up in SemanticTokensLegend.tokenTypes
            //     5. Token modifiers - each set bit will be looked up in SemanticTokensLegend.tokenModifiers

            var classifiedSpan = classifiedSpans[currentClassifiedSpanIndex];
            var originalTextSpan = classifiedSpan.TextSpan;

            // 1. Token line number delta, relative to the previous token
            // 2. Token start character delta, relative to the previous token
            ComputeLineNumberAndStartCharacterDelta(
                lines, originalTextSpan, ref lastLineNumber, ref lastStartCharacter, out var deltaLine, out var deltaStartCharacter);

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

                    // Note: There is the possibility of overlapping non-identical spans, e.g. in the case of strings
                    // with escape or regex characters.
                    // If the current text span overlaps with another span but is not identical (identical = modifier),
                    // we shorten the token and potentially process the cut off portion later into a separate token.
                    if (currentClassifiedSpanIndex + 1 < classifiedSpans.Length &&
                        classifiedSpans[currentClassifiedSpanIndex + 1].TextSpan != originalTextSpan &&
                        classifiedSpans[currentClassifiedSpanIndex + 1].TextSpan.Start < originalTextSpan.End)
                    {
                        tokenLength = classifiedSpans[currentClassifiedSpanIndex + 1].TextSpan.Start - originalTextSpan.Start;
                        unprocessedTokenOut = new UnprocessedSemanticToken(originalTextSpan.End, tokenTypeIndex);
                    }
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

        private static void ComputeLineNumberAndStartCharacterDelta(
            TextLineCollection lines,
            TextSpan tokenTextSpan,
            ref int lastLineNumber,
            ref int lastStartCharacter,
            out int deltaLineOut,
            out int deltaStartCharacterOut)
        {
            var linePosition = lines.GetLinePositionSpan(tokenTextSpan).Start;
            var lineNumber = linePosition.Line;

            // 1. Token line number delta, relative to the previous token
            deltaLineOut = lineNumber - lastLineNumber;
            Contract.ThrowIfTrue(deltaLineOut < 0, $"deltaLine is less than 0: {deltaLineOut}");

            // 2. Token start character delta, relative to the previous token
            // (Relative to 0 or the previous token’s start if they're on the same line)
            deltaStartCharacterOut = linePosition.Character;
            if (lastLineNumber == lineNumber)
            {
                deltaStartCharacterOut -= lastStartCharacter;
            }

            lastLineNumber = lineNumber;
            lastStartCharacter = linePosition.Character;
        }

        private static int GetTokenTypeIndex(string classificationType, Dictionary<string, int> tokenTypesToIndex)
        {
            s_classificationTypeToSemanticTokenTypeMap.TryGetValue(classificationType, out var tokenTypeStr);
            Contract.ThrowIfNull(tokenTypeStr, "tokenTypeStr is null.");
            Contract.ThrowIfFalse(tokenTypesToIndex.TryGetValue(tokenTypeStr, out var tokenTypeIndex), "No matching token type index found.");
            return tokenTypeIndex;
        }

        private static bool ProcessUnprocessedToken(
            TextLineCollection lines,
            UnprocessedSemanticToken unprocessedToken,
            ClassifiedSpan[] classifiedSpans,
            int currentClassifiedSpanIndex,
            ArrayBuilder<int> data,
            ref int lastLineNumber,
            ref int lastStartCharacter,
            out UnprocessedSemanticToken? unprocessedTokenRemainder)
        {
            // If the end of the unprocessed token is at or before the end of the current token, we can discard the unprocessed token.
            // [TO:DO Provide Regex example]
            if (unprocessedToken._tokenSpanEnd <= classifiedSpans[currentClassifiedSpanIndex].TextSpan.End)
            {
                unprocessedTokenRemainder = null;
                return true;
            }

            // If the start of the next token is at or before the end of the unprocessed token, ignore the unprocessed token for now
            // and possibly process it later.
            if (currentClassifiedSpanIndex + 1 < classifiedSpans.Length &&
                classifiedSpans[currentClassifiedSpanIndex + 1].TextSpan.Start <= unprocessedToken._tokenSpanEnd)
            {
                unprocessedTokenRemainder = unprocessedToken;
                return false;
            }

            var currentClassifiedTextSpan = classifiedSpans[currentClassifiedSpanIndex].TextSpan;
            var unprocessedTextSpan = new TextSpan(currentClassifiedTextSpan.End, unprocessedToken._tokenSpanEnd - currentClassifiedTextSpan.End);

            // We still might not be able to process the full unprocessed token, e.g. if the string has multiple escape charcters scattered
            // throughout. We need to check the next-next token to be sure.

            ComputeLineNumberAndStartCharacterDelta(
                lines, unprocessedTextSpan, ref lastLineNumber, ref lastStartCharacter, out var deltaLine, out var deltaStartCharacter);

            data.AddRange(deltaLine, deltaStartCharacter, unprocessedTextSpan.Length, unprocessedToken._tokenType, 0);

            unprocessedTokenRemainder = null;
            return true;
        }

        private struct UnprocessedSemanticToken
        {
            internal int _tokenSpanEnd;
            internal int _tokenType;

            public UnprocessedSemanticToken(int tokenSpanEnd, int tokenType)
            {
                _tokenSpanEnd = tokenSpanEnd;
                _tokenType = tokenType;
            }
        }
    }
}
