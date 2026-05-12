// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Text;

using RazorSyntaxToken = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxToken;

namespace Microsoft.CodeAnalysis.Razor.LinkedEditingRange;

internal static class LinkedEditingRangeHelper
{
    // The regex below excludes characters that can never be valid in a TagHelper name.
    // This is loosely based off logic from the Razor compiler:
    // https://github.com/dotnet/aspnetcore/blob/9da42b9fab4c61fe46627ac0c6877905ec845d5a/src/Razor/Microsoft.AspNetCore.Razor.Language/src/Legacy/HtmlTokenizer.cs
    public static readonly string WordPattern = @"!?[^ <>!\/\?\[\]=""\\@" + Environment.NewLine + "]+";

    public static LinePositionSpan[]? GetLinkedSpans(LinePosition linePosition, RazorCodeDocument codeDocument)
    {
        if (!codeDocument.Source.Text.TryGetSourceLocation(linePosition, out var validLocation))
        {
            return null;
        }

        var syntaxTree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();

        // We only care if the user is within a TagHelper or HTML tag with a valid start and end tag.
        if (TryGetNearestMarkupNameTokens(syntaxTree, validLocation, out var startTagNameToken, out var endTagNameToken) &&
            (startTagNameToken.Span.Contains(validLocation.AbsoluteIndex) || endTagNameToken.Span.Contains(validLocation.AbsoluteIndex) ||
            startTagNameToken.Span.End == validLocation.AbsoluteIndex || endTagNameToken.Span.End == validLocation.AbsoluteIndex))
        {
            var startSpan = startTagNameToken.GetLinePositionSpan(codeDocument.Source);
            var endSpan = endTagNameToken.GetLinePositionSpan(codeDocument.Source);

            return [startSpan, endSpan];
        }

        return null;
    }

    private static bool TryGetNearestMarkupNameTokens(
        RazorSyntaxTree syntaxTree,
        SourceLocation location,
        out RazorSyntaxToken startTagNameToken,
        out RazorSyntaxToken endTagNameToken)
    {
        var owner = syntaxTree.Root.FindInnermostNode(location.AbsoluteIndex);
        var element = owner?.FirstAncestorOrSelf<MarkupSyntaxNode>(
            a => a.Kind is SyntaxKind.MarkupTagHelperElement || a.Kind is SyntaxKind.MarkupElement);

        if (element is null)
        {
            startTagNameToken = default;
            endTagNameToken = default;
            return false;
        }

        if (element is BaseMarkupElementSyntax { StartTag: var startTag, EndTag: var endTag })
        {
            startTagNameToken = startTag?.Name ?? default;
            endTagNameToken = endTag?.Name ?? default;

            return startTagNameToken.IsValid() && endTagNameToken.IsValid();
        }

        throw new InvalidOperationException("Element is expected to be a MarkupTagHelperElement or MarkupElement.");
    }
}
