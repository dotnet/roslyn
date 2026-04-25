// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using RazorSyntaxList = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxList<Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode>;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal abstract class DirectiveAttributeCompletionItemProviderBase : IRazorCompletionItemProvider
{
    public abstract ImmutableArray<RazorCompletionItem> GetCompletionItems(RazorCompletionContext context);

    // Internal for testing
    internal static bool TryGetAttributeInfo(
        RazorSyntaxNode attributeLeafOwner,
        out TextSpan? prefixLocation,
        [NotNullWhen(true)] out string? attributeName,
        out TextSpan attributeNameLocation,
        out string? parameterName,
        out TextSpan parameterLocation)
    {
        var attribute = attributeLeafOwner.Parent;

        // The null check on the `NamePrefix` field is required for cases like:
        // `<svg xml:base=""x| ></svg>` where there's no `NamePrefix` available.
        switch (attribute)
        {
            case MarkupMinimizedAttributeBlockSyntax minimizedMarkupAttribute:
                prefixLocation = minimizedMarkupAttribute.NamePrefix?.Span;
                SplitAttributeNameIntoParts(
                    minimizedMarkupAttribute.Name.GetContent(),
                    minimizedMarkupAttribute.Name.Span,
                    out attributeName,
                    out attributeNameLocation,
                    out parameterName,
                    out parameterLocation);
                return true;

            case MarkupAttributeBlockSyntax markupAttribute:
                prefixLocation = markupAttribute.NamePrefix?.Span;
                SplitAttributeNameIntoParts(
                    markupAttribute.Name.GetContent(),
                    markupAttribute.Name.Span,
                    out attributeName,
                    out attributeNameLocation,
                    out parameterName,
                    out parameterLocation);
                return true;

            case MarkupMinimizedTagHelperAttributeSyntax minimizedTagHelperAttribute:
                prefixLocation = minimizedTagHelperAttribute.NamePrefix?.Span;
                SplitAttributeNameIntoParts(
                    minimizedTagHelperAttribute.Name.GetContent(),
                    minimizedTagHelperAttribute.Name.Span,
                    out attributeName,
                    out attributeNameLocation,
                    out parameterName,
                    out parameterLocation);
                return true;

            case MarkupTagHelperAttributeSyntax tagHelperAttribute:
                prefixLocation = tagHelperAttribute.NamePrefix?.Span;
                SplitAttributeNameIntoParts(
                    tagHelperAttribute.Name.GetContent(),
                    tagHelperAttribute.Name.Span,
                    out attributeName,
                    out attributeNameLocation,
                    out parameterName,
                    out parameterLocation);
                return true;

            case MarkupTagHelperDirectiveAttributeSyntax directiveAttribute:
                {
                    var attributeNameNode = directiveAttribute.Name;
                    var directiveAttributeTransition = directiveAttribute.Transition;
                    var nameStart = directiveAttributeTransition?.SpanStart ?? attributeNameNode.SpanStart;
                    var nameEnd = attributeNameNode?.Span.End ?? directiveAttributeTransition.AssumeNotNull().Span.End;
                    prefixLocation = directiveAttribute.NamePrefix?.Span;
                    attributeName = string.Concat(directiveAttributeTransition?.GetContent(), attributeNameNode?.GetContent());
                    attributeNameLocation = new TextSpan(nameStart, nameEnd - nameStart);
                    parameterName = directiveAttribute.ParameterName?.GetContent();
                    parameterLocation = directiveAttribute.ParameterName?.Span ?? default;
                    return true;
                }

            case MarkupMinimizedTagHelperDirectiveAttributeSyntax minimizedDirectiveAttribute:
                {
                    var attributeNameNode = minimizedDirectiveAttribute.Name;
                    var directiveAttributeTransition = minimizedDirectiveAttribute.Transition;
                    var nameStart = directiveAttributeTransition?.SpanStart ?? attributeNameNode.SpanStart;
                    var nameEnd = attributeNameNode?.Span.End ?? directiveAttributeTransition.AssumeNotNull().Span.End;
                    prefixLocation = minimizedDirectiveAttribute.NamePrefix?.Span;
                    attributeName = string.Concat(directiveAttributeTransition?.GetContent(), attributeNameNode?.GetContent());
                    attributeNameLocation = new TextSpan(nameStart, nameEnd - nameStart);
                    parameterName = minimizedDirectiveAttribute.ParameterName?.GetContent();
                    parameterLocation = minimizedDirectiveAttribute.ParameterName?.Span ?? default;
                    return true;
                }
        }

        prefixLocation = null;
        attributeName = null;
        attributeNameLocation = default;
        parameterName = null;
        parameterLocation = default;
        return false;
    }

    // Internal for testing
    internal static bool TryGetElementInfo(
        RazorSyntaxNode element,
        [NotNullWhen(true)] out string? containingTagName,
        out ImmutableArray<string> attributeNames)
    {
        if (element is MarkupStartTagSyntax startTag)
        {
            containingTagName = startTag.Name.Content;
            attributeNames = ExtractAttributeNames(startTag.Attributes);
            return true;
        }

        if (element is MarkupTagHelperStartTagSyntax startTagHelper)
        {
            containingTagName = startTagHelper.Name.Content;
            attributeNames = ExtractAttributeNames(startTagHelper.Attributes);
            return true;
        }

        containingTagName = null;
        attributeNames = default;
        return false;
    }

    private static ImmutableArray<string> ExtractAttributeNames(RazorSyntaxList attributes)
    {
        using var attributeNames = new PooledArrayBuilder<string>(capacity: attributes.Count);

        foreach (var attribute in attributes)
        {
            switch (attribute)
            {
                case MarkupTagHelperAttributeSyntax tagHelperAttribute:
                    attributeNames.Add(tagHelperAttribute.Name.GetContent());
                    break;

                case MarkupMinimizedTagHelperAttributeSyntax minimizedTagHelperAttribute:
                    attributeNames.Add(minimizedTagHelperAttribute.Name.GetContent());
                    break;

                case MarkupAttributeBlockSyntax markupAttribute:
                    attributeNames.Add(markupAttribute.Name.GetContent());
                    break;

                case MarkupMinimizedAttributeBlockSyntax minimizedMarkupAttribute:
                    attributeNames.Add(minimizedMarkupAttribute.Name.GetContent());
                    break;

                case MarkupTagHelperDirectiveAttributeSyntax directiveAttribute:
                    attributeNames.Add(directiveAttribute.FullName);
                    break;

                case MarkupMinimizedTagHelperDirectiveAttributeSyntax minimizedDirectiveAttribute:
                    attributeNames.Add(minimizedDirectiveAttribute.FullName);
                    break;
            }
        }

        return attributeNames.ToImmutableAndClear();
    }

    private static void SplitAttributeNameIntoParts(
        string attributeName,
        TextSpan attributeNameLocation,
        out string name,
        out TextSpan nameLocation,
        out string? parameterName,
        out TextSpan parameterLocation)
    {
        name = attributeName;
        nameLocation = attributeNameLocation;
        parameterName = null;
        parameterLocation = default;

        // It's possible that the attribute looks like a directive attribute but is incomplete.
        // We should try and extract out the transition and parameter.

        if (attributeName.Length == 0 || attributeName[0] != '@')
        {
            // Doesn't look like a directive attribute. Not an incomplete directive attribute.
            return;
        }

        var colonIndex = attributeName.IndexOf(':');
        if (colonIndex == -1)
        {
            // There's no parameter, the existing attribute name and location is sufficient.
            return;
        }

        name = attributeName[..colonIndex];
        nameLocation = new TextSpan(attributeNameLocation.Start, name.Length);
        parameterName = attributeName[(colonIndex + 1)..];
        parameterLocation = new TextSpan(attributeNameLocation.Start + colonIndex + 1, parameterName.Length);
    }
}
