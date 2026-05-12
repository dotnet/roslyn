// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal class DirectiveAttributeEventParameterCompletionItemProvider : IRazorCompletionItemProvider
{
    private static readonly ImmutableArray<RazorCompletionItem> s_eventCompletionItems = HtmlFacts.FormEvents
        .SelectAsArray(e => RazorCompletionItem.CreateDirectiveAttributeEventParameterHtmlEventValue(e, e, s_commitCharacters));

    private static readonly ImmutableArray<RazorCommitCharacter> s_commitCharacters = RazorCommitCharacter.CreateArray(["\"", " ", "'"]);

    public ImmutableArray<RazorCompletionItem> GetCompletionItems(RazorCompletionContext context)
    {
        var owner = context.Owner?.Parent;

        if (owner is MarkupTagHelperAttributeValueSyntax parentValueSyntax)
        {
            owner = parentValueSyntax.Parent;
        }

        if (owner is not MarkupTagHelperDirectiveAttributeSyntax directiveAttributeSyntax)
        {
            return [];
        }

        if (directiveAttributeSyntax is not
            {
                Colon.IsMissing: false,
                ParameterName: { IsMissing: false, LiteralTokens: [{ Content: "event" }] },
                EqualsToken.IsMissing: false,
                ValuePrefix.IsMissing: false,
                Value: { IsMissing: false } valueSyntax,
            })
        {
            return [];
        }

        if (!valueSyntax.Span.Contains(context.AbsoluteIndex) && valueSyntax.EndPosition != context.AbsoluteIndex)
        {
            return [];
        }

        return s_eventCompletionItems;
    }
}
