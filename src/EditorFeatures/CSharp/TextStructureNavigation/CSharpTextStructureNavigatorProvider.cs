// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor.Implementation.TextStructureNavigation;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.TextStructureNavigation
{
    [Export(typeof(ITextStructureNavigatorProvider))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    internal class CSharpTextStructureNavigatorProvider : AbstractTextStructureNavigatorProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpTextStructureNavigatorProvider(
            ITextStructureNavigatorSelectorService selectorService,
            IContentTypeRegistryService contentTypeService,
            IUIThreadOperationExecutor uIThreadOperationExecutor)
            : base(selectorService, contentTypeService, uIThreadOperationExecutor)
        {
        }

        protected override bool ShouldSelectEntireTriviaFromStart(SyntaxTrivia trivia)
            => trivia.IsRegularOrDocComment();

        protected override bool IsWithinNaturalLanguage(SyntaxToken token, int position)
        {
            switch (token.Kind())
            {
                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.UTF8StringLiteralToken:
                    // This, in combination with the override of GetExtentOfWordFromToken() below, treats the closing
                    // quote as a separate token.  This maintains behavior with VS2013.
                    return !IsAtClosingQuote(token, position);

                case SyntaxKind.SingleLineRawStringLiteralToken:
                case SyntaxKind.MultiLineRawStringLiteralToken:
                case SyntaxKind.UTF8SingleLineRawStringLiteralToken:
                case SyntaxKind.UTF8MultiLineRawStringLiteralToken:
                    {
                        // Like with normal string literals, treat the closing quotes as as the end of the string so that
                        // navigation ends there and doesn't go past them.
                        var end = GetStartOfRawStringLiteralEndDelimiter(token);
                        return position < end;
                    }

                case SyntaxKind.CharacterLiteralToken:
                    // Before the ' is considered outside the character
                    return position != token.SpanStart;

                case SyntaxKind.InterpolatedStringTextToken:
                case SyntaxKind.XmlTextLiteralToken:
                    return true;
            }

            return false;
        }

        private static int GetStartOfRawStringLiteralEndDelimiter(SyntaxToken token)
        {
            var text = token.ToString();
            var start = 0;
            var end = text.Length;

            if (token.IsKind(SyntaxKind.UTF8MultiLineRawStringLiteralToken, SyntaxKind.UTF8SingleLineRawStringLiteralToken))
            {
                // Skip past the u8 suffix
                end -= "u8".Length;
            }

            while (start < end && text[start] == '"')
                start++;

            while (end > start && text[end - 1] == '"')
                end--;

            return token.SpanStart + end;
        }

        private static bool IsAtClosingQuote(SyntaxToken token, int position)
            => token.Kind() switch
            {
                SyntaxKind.StringLiteralToken => position == token.Span.End - 1 && token.Text[^1] == '"',
                SyntaxKind.UTF8StringLiteralToken => position == token.Span.End - 3 && token.Text is [.., '"', 'u' or 'U', '8'],
                _ => throw ExceptionUtilities.Unreachable
            };

        protected override TextExtent GetExtentOfWordFromToken(SyntaxToken token, SnapshotPoint position)
        {
            if (token.IsKind(SyntaxKind.StringLiteralToken, SyntaxKind.UTF8StringLiteralToken) && IsAtClosingQuote(token, position.Position))
            {
                // Special case to treat the closing quote of a string literal as a separate token.  This allows the
                // cursor to stop during word navigation (Ctrl+LeftArrow, etc.) immediately before AND after the
                // closing quote, just like it did in VS2013 and like it currently does for interpolated strings.
                var span = new Span(position.Position, token.Span.End - position.Position);
                return new TextExtent(new SnapshotSpan(position.Snapshot, span), isSignificant: true);
            }
            else if (token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken,
                SyntaxKind.MultiLineRawStringLiteralToken,
                SyntaxKind.UTF8SingleLineRawStringLiteralToken,
                SyntaxKind.UTF8MultiLineRawStringLiteralToken))
            {
                var delimiterStart = GetStartOfRawStringLiteralEndDelimiter(token);
                return new TextExtent(new SnapshotSpan(position.Snapshot, Span.FromBounds(delimiterStart, token.Span.End)), isSignificant: true);
            }
            else
            {
                return base.GetExtentOfWordFromToken(token, position);
            }
        }
    }
}
