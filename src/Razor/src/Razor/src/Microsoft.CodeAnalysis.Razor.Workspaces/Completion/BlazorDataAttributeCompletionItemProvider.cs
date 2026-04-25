// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET
using System;
#endif
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Tooltip;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.CodeAnalysis.Razor.Completion;

/// <summary>
/// Provides completions for Blazor-specific data-* attributes used for enhanced navigation and form handling.
/// </summary>
internal class BlazorDataAttributeCompletionItemProvider : IRazorCompletionItemProvider
{
    private static readonly ImmutableArray<RazorCommitCharacter> AttributeCommitCharacters = RazorCommitCharacter.CreateArray(["="]);
    private static readonly ImmutableArray<RazorCommitCharacter> AttributeSnippetCommitCharacters = RazorCommitCharacter.CreateArray(["="], insert: false);

    // Define the Blazor-specific data attributes
    private static readonly ImmutableArray<(string Name, string Description)> s_blazorDataAttributes =
    [
        ("data-enhance", "Opts in to enhanced form handling for a form element."),
        ("data-enhance-nav", "Disables enhanced navigation for a link or DOM subtree."),
        ("data-permanent", "Marks an element to be preserved when handling enhanced navigation or form requests.")
    ];

    public ImmutableArray<RazorCompletionItem> GetCompletionItems(RazorCompletionContext context)
    {
        // Only provide completions for component files
        if (!context.SyntaxTree.Options.FileKind.IsComponent())
        {
            return [];
        }

        var owner = CompletionContextHelper.AdjustSyntaxNodeForCompletion(context.Owner);
        if (owner is null)
        {
            return [];
        }

        // Check if we're in an attribute context
        if (!HtmlFacts.TryGetAttributeInfo(
                owner,
                out var containingTagNameToken,
                out var prefixLocation,
                out var selectedAttributeName,
                out var selectedAttributeNameLocation,
                out var attributes))
        {
            return [];
        }

        // Only provide completions when we're completing an attribute name
        if (!CompletionContextHelper.IsAttributeNameCompletionContext(
                selectedAttributeName,
                selectedAttributeNameLocation,
                prefixLocation,
                context.AbsoluteIndex))
        {
            return [];
        }

        // Don't provide completions if the user is typing a directive attribute (starts with @)
        if (selectedAttributeName?.StartsWith('@') == true)
        {
            return [];
        }

        var containingTagName = containingTagNameToken.Content;

        using var completionItems = new PooledArrayBuilder<RazorCompletionItem>();

        foreach (var (attributeName, description) in s_blazorDataAttributes)
        {
            // Only show data-enhance for form elements
            if (attributeName == "data-enhance" &&
                !string.Equals(containingTagName, "form", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // If not currently editing this attribute, check if it already exists
            if (selectedAttributeName != attributeName)
            {
                var alreadyExists = false;
                foreach (var attribute in attributes)
                {
                    var existingAttributeName = attribute switch
                    {
                        MarkupAttributeBlockSyntax attributeBlock => attributeBlock.Name.GetContent(),
                        MarkupMinimizedAttributeBlockSyntax minimizedAttributeBlock => minimizedAttributeBlock.Name.GetContent(),
                        _ => null
                    };

                    if (existingAttributeName == attributeName)
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (alreadyExists)
                {
                    // Attribute already exists and is not the one currently being edited
                    continue;
                }
            }

            var insertText = attributeName;
            var isSnippet = context.Options.SnippetsSupported;

            // Add snippet text for attribute value if snippets are supported
            if (isSnippet)
            {
                var snippetSuffix = context.Options.AutoInsertAttributeQuotes ? "=\"$0\"" : "=$0";
                insertText = attributeName + snippetSuffix;
            }

            // VSCode doesn't use commit characters for attribute completions
            var commitCharacters = context.Options.UseVsCodeCompletionCommitCharacters
                ? ImmutableArray<RazorCommitCharacter>.Empty
                : (isSnippet ? AttributeSnippetCommitCharacters : AttributeCommitCharacters);

            var descriptionInfo = new AttributeDescriptionInfo(
                Name: attributeName,
                Documentation: description);

            var completionItem = RazorCompletionItem.CreateAttribute(
                displayText: attributeName,
                insertText: insertText,
                descriptionInfo: descriptionInfo,
                commitCharacters: commitCharacters,
                isSnippet: isSnippet);

            completionItems.Add(completionItem);
        }

        return completionItems.ToImmutable();
    }
}
