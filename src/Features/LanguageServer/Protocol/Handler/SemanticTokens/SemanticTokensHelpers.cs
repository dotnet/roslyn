// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    internal class SemanticTokensHelpers
    {
        /// <summary>
        /// Returns the semantic tokens data for a given document with an optional range.
        /// </summary>
        public static async Task<int[]> ComputeSemanticTokensDataAsync(
            ClientCapabilities capabilities,
            Document document,
            LSP.Range? range,
            ClassificationOptions options,
            CancellationToken cancellationToken)
        {
            var tokenTypesToIndex = SemanticTokensSchema.GetSchema(capabilities.HasVisualStudioLspCapability()).TokenTypeToIndex;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            // By default we calculate the tokens for the full document span, although the user 
            // can pass in a range if they wish.
            var textSpan = range is null ? root.FullSpan : ProtocolConversions.RangeToTextSpan(range, text);

            var classifiedSpans = await GetClassifiedSpansForDocumentAsync(
                document, textSpan, options, cancellationToken).ConfigureAwait(false);

            // Multi-line tokens are not supported by VS (tracked by https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1265495).
            // Roslyn's classifier however can return multi-line classified spans, so we must break these up into single-line spans.
            var updatedClassifiedSpans = ConvertMultiLineToSingleLineSpans(text, classifiedSpans);

            // TO-DO: We should implement support for streaming if LSP adds support for it:
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1276300
            return ComputeTokens(capabilities, text.Lines, updatedClassifiedSpans, tokenTypesToIndex);
        }

        private static async Task<ClassifiedSpan[]> GetClassifiedSpansForDocumentAsync(
            Document document,
            TextSpan textSpan,
            ClassificationOptions options,
            CancellationToken cancellationToken)
        {
            var classificationService = document.GetRequiredLanguageService<IClassificationService>();
            using var _ = ArrayBuilder<ClassifiedSpan>.GetInstance(out var classifiedSpans);

            // We always return both syntactic and semantic classifications.  If there is a syntactic classifier running on the client
            // then the semantic token classifications will override them.

            // `includeAdditiveSpans` will add token modifiers such as 'static', which we want to include in LSP.
            var spans = await ClassifierHelper.GetClassifiedSpansAsync(
                document, textSpan, options, includeAdditiveSpans: true, cancellationToken).ConfigureAwait(false);

            // The spans returned to us may include some empty spans, which we don't care about. We also don't care
            // about the 'text' classification.  It's added for everything between real classifications (including
            // whitespace), and just means 'don't classify this'.  No need for us to actually include that in
            // semantic tokens as it just wastes space in the result.
            var nonEmptySpans = spans.Where(s => !s.TextSpan.IsEmpty && s.ClassificationType != ClassificationTypeNames.Text);
            classifiedSpans.AddRange(nonEmptySpans);

            // Classified spans are not guaranteed to be returned in a certain order so we sort them to be safe.
            classifiedSpans.Sort(ClassifiedSpanComparer.Instance);
            return classifiedSpans.ToArray();
        }

        public static ClassifiedSpan[] ConvertMultiLineToSingleLineSpans(SourceText text, ClassifiedSpan[] classifiedSpans)
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
                    ConvertToSingleLineSpan(
                        text, classifiedSpans, updatedClassifiedSpans, ref spanIndex, span.ClassificationType,
                        startLine, startOffset, endLine, endOffSet);
                }
                else
                {
                    // This is already a single-line span, so no modification is necessary.
                    updatedClassifiedSpans.Add(span);
                }
            }

            return updatedClassifiedSpans.ToArray();

            static void ConvertToSingleLineSpan(
                SourceText text,
                ClassifiedSpan[] originalClassifiedSpans,
                ArrayBuilder<ClassifiedSpan> updatedClassifiedSpans,
                ref int spanIndex,
                string classificationType,
                int startLine,
                int startOffset,
                int endLine,
                int endOffSet)
            {
                var numLinesInSpan = endLine - startLine + 1;
                Contract.ThrowIfTrue(numLinesInSpan < 1);

                for (var currentLine = 0; currentLine < numLinesInSpan; currentLine++)
                {
                    TextSpan textSpan;
                    var line = text.Lines[startLine + currentLine];

                    // Case 1: First line of span
                    if (currentLine == 0)
                    {
                        var absoluteStart = line.Start + startOffset;

                        // This start could be past the regular end of the line if it's within the newline character if we have a CRLF newline. In that case, just skip emitting a span for the LF.
                        // One example where this could happen is an embedded regular expression that we're classifying; regular expression comments contained within a multi-line string
                        // contain the carriage return but not the linefeed, so the linefeed could be the start of the next classification.
                        textSpan = TextSpan.FromBounds(Math.Min(absoluteStart, line.End), line.End);
                    }
                    // Case 2: Any of the span's middle lines
                    else if (currentLine != numLinesInSpan - 1)
                    {
                        textSpan = line.Span;
                    }
                    // Case 3: Last line of span
                    else
                    {
                        textSpan = new TextSpan(line.Start, endOffSet);
                    }

                    // Omit 0-length spans created in this fashion.
                    if (textSpan.Length > 0)
                    {
                        var updatedClassifiedSpan = new ClassifiedSpan(textSpan, classificationType);
                        updatedClassifiedSpans.Add(updatedClassifiedSpan);
                    }

                    // Since spans are expected to be ordered, when breaking up a multi-line span, we may have to insert
                    // other spans in-between. For example, we may encounter this case when breaking up a multi-line verbatim
                    // string literal containing escape characters:
                    //     var x = @"one ""
                    //               two";
                    // The check below ensures we correctly return the spans in the correct order, i.e. 'one', '""', 'two'.
                    while (spanIndex + 1 < originalClassifiedSpans.Length &&
                        textSpan.Contains(originalClassifiedSpans[spanIndex + 1].TextSpan))
                    {
                        updatedClassifiedSpans.Add(originalClassifiedSpans[spanIndex + 1]);
                        spanIndex++;
                    }
                }
            }
        }

        private static int[] ComputeTokens(
            ClientCapabilities capabilities,
            TextLineCollection lines,
            ClassifiedSpan[] classifiedSpans,
            IReadOnlyDictionary<string, int> tokenTypesToIndex)
        {
            using var _ = ArrayBuilder<int>.GetInstance(classifiedSpans.Length, out var data);

            // We keep track of the last line number and last start character since tokens are
            // reported relative to each other.
            var lastLineNumber = 0;
            var lastStartCharacter = 0;

            var tokenTypeMap = SemanticTokensSchema.GetSchema(capabilities.HasVisualStudioLspCapability()).TokenTypeMap;

            for (var currentClassifiedSpanIndex = 0; currentClassifiedSpanIndex < classifiedSpans.Length; currentClassifiedSpanIndex++)
            {
                currentClassifiedSpanIndex = ComputeNextToken(
                    lines, ref lastLineNumber, ref lastStartCharacter, classifiedSpans,
                    currentClassifiedSpanIndex, tokenTypeMap, tokenTypesToIndex,
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
            IReadOnlyDictionary<string, string> tokenTypeMap,
            IReadOnlyDictionary<string, int> tokenTypesToIndex,
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
            Contract.ThrowIfFalse(tokenLength > 0);

            // We currently only have one modifier (static). The logic below will need to change in the future if other
            // modifiers are added in the future.
            var modifierBits = TokenModifiers.None;
            var tokenTypeIndex = 0;

            // Classified spans with the same text span should be combined into one token.
            while (classifiedSpans[currentClassifiedSpanIndex].TextSpan == originalTextSpan)
            {
                var classificationType = classifiedSpans[currentClassifiedSpanIndex].ClassificationType;
                if (classificationType == ClassificationTypeNames.StaticSymbol)
                {
                    // 4. Token modifiers - each set bit will be looked up in SemanticTokensLegend.tokenModifiers
                    modifierBits = TokenModifiers.Static;
                }
                else if (classificationType == ClassificationTypeNames.ReassignedVariable)
                {
                    // 5. Token modifiers - each set bit will be looked up in SemanticTokensLegend.tokenModifiers
                    modifierBits = TokenModifiers.ReassignedVariable;
                }
                else
                {
                    // 6. Token type - looked up in SemanticTokensLegend.tokenTypes (language server defined mapping
                    // from integer to LSP token types).
                    tokenTypeIndex = GetTokenTypeIndex(classificationType);
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

            int GetTokenTypeIndex(string classificationType)
            {
                if (!tokenTypeMap.TryGetValue(classificationType, out var tokenTypeStr))
                {
                    tokenTypeStr = classificationType;
                }

                Contract.ThrowIfFalse(tokenTypesToIndex.TryGetValue(tokenTypeStr, out var tokenTypeIndex), "No matching token type index found.");
                return tokenTypeIndex;
            }
        }

        private class ClassifiedSpanComparer : IComparer<ClassifiedSpan>
        {
            public static readonly ClassifiedSpanComparer Instance = new();

            public int Compare(ClassifiedSpan x, ClassifiedSpan y) => x.TextSpan.CompareTo(y.TextSpan);
        }
    }
}
