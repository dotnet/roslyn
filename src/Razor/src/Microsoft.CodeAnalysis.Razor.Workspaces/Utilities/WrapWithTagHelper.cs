// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Utilities;

internal static class WrapWithTagHelper
{
    /// <summary>
    /// Returns the range to use for wrapping a selection with a tag.
    /// </summary>
    /// <returns>Returns null if the range specified is not valid</returns>
    public static bool TryGetValidWrappingRange(RazorCodeDocument codeDocument, LinePositionSpan range, out LinePositionSpan wrappingRange)
    {
        wrappingRange = range;

        var sourceText = codeDocument.Source.Text;

        if (!sourceText.TryGetAbsoluteIndex(range.Start, out var hostDocumentIndex))
        {
            return false;
        }

        // First thing we do is make sure we start at a non-whitespace character. This is important because in some
        // situations the whitespace can be technically C#, but move one character to the right and it's HTML. eg
        //
        // @if (true) {
        //   |   <p></p>
        // }
        //
        // Limiting this to only whitespace on the same line, as it's not clear what user expectation would be otherwise.
        var requestSpan = sourceText.GetTextSpan(range);
        if (sourceText.TryGetFirstNonWhitespaceOffset(requestSpan, out var offset, out var newLineCount) &&
            newLineCount == 0)
        {
            wrappingRange = new LinePositionSpan(
                start: new LinePosition(
                    line: range.Start.Line,
                    character: range.Start.Character + offset),
                end: range.End);
            requestSpan = sourceText.GetTextSpan(wrappingRange);
            hostDocumentIndex += offset;
        }

        // Since we're at the start of the selection, lets prefer the language to the right of the cursor if possible.
        // That way with the following situation:
        //
        // @if (true) {
        //   |<p></p>
        // }
        //
        // Instead of C#, which certainly would be expected to go in an if statement, we'll see HTML, which obviously
        // is the better choice for this operation.
        var languageKind = codeDocument.GetLanguageKind(hostDocumentIndex, rightAssociative: true);

        // However, reverse scenario is possible as well, when we have
        // <div>
        // |@if (true) {}
        // <p></p>
        // </div>
        // in which case right-associative GetLanguageKind will return Razor and left-associative will return HTML
        // We should hand that case as well, see https://github.com/dotnet/razor/issues/10819
        if (languageKind is RazorLanguageKind.Razor)
        {
            languageKind = codeDocument.GetLanguageKind(hostDocumentIndex, rightAssociative: false);
        }

        if (languageKind is not RazorLanguageKind.Html)
        {
            // In general, we don't support C# for obvious reasons, but we can support implicit expressions. ie
            //
            // <p>@curr$$entCount</p>
            //
            // We can expand the range to encompass the whole implicit expression, and then it will wrap as expected.
            // Similarly if they have selected the implicit expression, then we can continue. ie
            //
            // <p>[|@currentCount|]</p>

            var root = codeDocument.GetRequiredSyntaxRoot();
            var node = root.FindNode(requestSpan, includeWhitespace: false, getInnermostNodeForTie: true);
            if (node?.FirstAncestorOrSelf<CSharpImplicitExpressionSyntax>() is { Parent: CSharpCodeBlockSyntax codeBlock } &&
                (requestSpan == codeBlock.Span || requestSpan.Length == 0))
            {
                // Pretend we're in Html so the rest of the logic can continue
                wrappingRange = sourceText.GetLinePositionSpan(codeBlock.Span);
                return true;
            }

            return false;
        }

        return true;
    }
}
