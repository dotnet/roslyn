// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor.Implementation.TextStructureNavigation;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.TextStructureNavigation;

[Export(typeof(ITextStructureNavigatorProvider))]
[ContentType(ContentTypeNames.CSharpContentType)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpTextStructureNavigatorProvider(
    ITextStructureNavigatorSelectorService selectorService,
    IContentTypeRegistryService contentTypeService,
    IUIThreadOperationExecutor uIThreadOperationExecutor) : AbstractTextStructureNavigatorProvider(selectorService, contentTypeService, uIThreadOperationExecutor)
{
    protected override bool ShouldSelectEntireTriviaFromStart(SyntaxTrivia trivia)
        => trivia.IsRegularOrDocComment();

    protected override TextExtent GetExtentOfWordFromToken(ITextStructureNavigator naturalLanguageNavigator, SyntaxToken token, SnapshotPoint position)
    {
        var snapshot = position.Snapshot;

        // Legacy behavior.  We let the editor handle these.  Note: this can be revisited if we think we would do a better
        // job handling these.
        if (token.Kind() is SyntaxKind.InterpolatedStringTextToken or SyntaxKind.XmlTextLiteralToken)
            return naturalLanguageNavigator.GetExtentOfWord(position);

        // Legacy behavior.  If we're on the start of a char literal, we select the entire thing.  For anything else, we
        // defer to the editor. Note: this can be revisited if we think we would do a better
        if (token.Kind() is SyntaxKind.CharacterLiteralToken)
        {
            if (token.SpanStart == position)
                return GetTokenExtent(token, snapshot);

            return naturalLanguageNavigator.GetExtentOfWord(position);
        }

        // For string literals, if we're on the starting quote, we want to select the entire string.
        //
        // If we're on the closing quote, we want to treat it as separate token.  This allows the cursor to stop during
        // word navigation (Ctrl+LeftArrow, etc.) immediately before AND after the closing quote, just like it did in
        // VS2013 and like it currently does for interpolated strings.
        //
        // If we're in the middle of the string, we want to let the editor take over.  but if it selects a span outside
        // of the string, we'll clamp the result back to within the string.

        var isNormalStringLiteral = token.Kind() is SyntaxKind.StringLiteralToken or SyntaxKind.Utf8StringLiteralToken;
        var isRawStringLiteral = token.Kind() is SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.Utf8SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken or SyntaxKind.Utf8MultiLineRawStringLiteralToken;

        if (!isNormalStringLiteral && !isRawStringLiteral)
        {
            // Not a string literal. Just select the entire token.
            return GetTokenExtent(token, snapshot);
        }

        // At the start of the string, select the start span.
        var (startSpan, contentSpan, endSpan) = GetStringLiteralParts();
        if (startSpan.Contains(position))
            return new TextExtent(startSpan.ToSnapshotSpan(snapshot), isSignificant: true);

        // If at the end, select the end piece only.
        if (endSpan.Contains(position))
            return new TextExtent(endSpan.ToSnapshotSpan(snapshot), isSignificant: true);

        // We're in the middle.  Defer to the editor.  But make sure we don't go outside of the middle section.
        var naturalExtent = naturalLanguageNavigator.GetExtentOfWord(position);

        var intersection = naturalExtent.Span.Intersection(contentSpan.ToSpan());
        return intersection is null ? naturalExtent : new TextExtent(intersection.Value, isSignificant: naturalExtent.IsSignificant);

        (TextSpan startSpan, TextSpan contentSpan, TextSpan endSpan) GetStringLiteralParts()
        {
            var start = token.Span.Start;
            var contentStart = start;

            if (CharAt(contentStart) == '@')
                contentStart++;

            if (CharAt(contentStart) == '"')
                contentStart++;

            if (isRawStringLiteral)
            {
                while (CharAt(contentStart) == '"')
                    contentStart++;
            }

            var end = Math.Max(contentStart, token.Span.End);
            var contentEnd = end;

            if (CharAt(contentEnd - 1) == '8')
                contentEnd--;

            if (CharAt(contentEnd - 1) is 'u' or 'U')
                contentEnd--;

            if (CharAt(contentEnd - 1) == '"')
                contentEnd--;

            if (isRawStringLiteral)
            {
                while (CharAt(contentEnd - 1) == '"')
                    contentEnd--;
            }

            // Ensure that in error conditions like a naked `"` that we don't end up with invalid bounds.
            contentEnd = Math.Max(contentStart, contentEnd);
            return (TextSpan.FromBounds(start, contentStart), TextSpan.FromBounds(contentStart, contentEnd), TextSpan.FromBounds(contentEnd, end));
        }

        char CharAt(int position)
            => position >= 0 && position < snapshot.Length ? snapshot[position] : '\0';
    }
}
