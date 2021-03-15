﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            // Multi-line tokens are not supported by VS (tracked by https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1265495).
            // Roslyn's classifier however can return multi-line classified spans, so we must break these up into single-line spans.
            var updatedClassifiedSpans = ConvertMultiLineToSingleLineSpans(text, classifiedSpans.ToArray());

            // TO-DO: We should implement support for streaming if LSP adds support for it:
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1276300
            return ComputeTokens(text.Lines, updatedClassifiedSpans, tokenTypesToIndex);
        }

        private static ClassifiedSpan[] ConvertMultiLineToSingleLineSpans(SourceText text, ClassifiedSpan[] classifiedSpans)
        {
            using var _ = ArrayBuilder<ClassifiedSpan>.GetInstance(out var updatedClassifiedSpans);

            for (var spanIndex = 0; spanIndex < classifiedSpans.Length; spanIndex++)
            {
                var span = classifiedSpans[spanIndex];
                text.GetLinesAndOffsets(span.TextSpan, out var startLine, out var startOffset, out var endLine, out var endOffSet);

                // If the start and end of the classified span are not on the same line, we're dealing with a multi-line span.
                // Since VS doesn't support multi-line spans/tokens, we need to break the span up into single-line spans.
                if (startLine != endLine)
                {
                    spanIndex = ConvertToSingleLineSpan(
                        text, classifiedSpans, updatedClassifiedSpans, spanIndex, span.ClassificationType,
                        startLine, startOffset, endLine, endOffSet);
                }
                else
                {
                    // This is already a single-line span, so no modification is necessary.
                    updatedClassifiedSpans.Add(span);
                }
            }

            return updatedClassifiedSpans.ToArray();

            static int ConvertToSingleLineSpan(
                SourceText text,
                ClassifiedSpan[] originalClassifiedSpans,
                ArrayBuilder<ClassifiedSpan> updatedClassifiedSpans,
                int spanIndex,
                string classificationType,
                int startLine,
                int startOffset,
                int endLine,
                int endOffSet)
            {
                var numLinesInSpan = endLine - startLine + 1;
                Contract.ThrowIfTrue(numLinesInSpan < 1);

                var updatedSpanIndex = spanIndex;

                for (var currentLine = 0; currentLine < numLinesInSpan; currentLine++)
                {
                    TextSpan? textSpan;

                    // Case 1: First line of span
                    if (currentLine == 0)
                    {
                        var absoluteStartOffset = text.Lines[startLine].Start + startOffset;
                        var spanLength = text.Lines[startLine].End - absoluteStartOffset;
                        textSpan = new TextSpan(absoluteStartOffset, spanLength);
                    }
                    // Case 2: Any of the span's middle lines
                    else if (currentLine != numLinesInSpan - 1)
                    {
                        textSpan = text.Lines[startLine + currentLine].Span;
                    }
                    // Case 3: Last line of span
                    else
                    {
                        textSpan = new TextSpan(text.Lines[endLine].Start, endOffSet);
                    }

                    var updatedClassifiedSpan = new ClassifiedSpan(textSpan.Value, classificationType);
                    updatedClassifiedSpans.Add(updatedClassifiedSpan);

                    // Since spans are expected to be ordered, when breaking up a multi-line span, we may have to insert
                    // other spans in-between. For example, we may encounter this case when breaking up a multi-line verbatim
                    // string literal containing escape characters:
                    //     var x = @"one ""
                    //               two";
                    // The check below ensures we correctly return the spans in the correct order, i.e. 'one', '""', 'two'.
                    while (updatedSpanIndex + 1 < originalClassifiedSpans.Length &&
                        textSpan.Value.Contains(originalClassifiedSpans[updatedSpanIndex + 1].TextSpan))
                    {
                        updatedClassifiedSpans.Add(originalClassifiedSpans[updatedSpanIndex + 1]);
                        updatedSpanIndex++;
                    }
                }

                return updatedSpanIndex;
            }
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
