// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Tooltip;
using Microsoft.VisualStudio.Editor.Razor;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal class TagHelperCompletionProvider(ITagHelperCompletionService tagHelperCompletionService) : IRazorCompletionItemProvider
{
    // Internal for testing
    internal static readonly ImmutableArray<RazorCommitCharacter> MinimizedAttributeCommitCharacters = RazorCommitCharacter.CreateArray(["=", " "]);
    internal static readonly ImmutableArray<RazorCommitCharacter> AttributeCommitCharacters = RazorCommitCharacter.CreateArray(["="]);
    internal static readonly ImmutableArray<RazorCommitCharacter> AttributeSnippetCommitCharacters = RazorCommitCharacter.CreateArray(["="], insert: false);

    private static readonly ImmutableArray<RazorCommitCharacter> s_elementCommitCharacters = RazorCommitCharacter.CreateArray([" ", ">"]);
    private static readonly ImmutableArray<RazorCommitCharacter> s_elementCommitCharacters_WithoutSpace = RazorCommitCharacter.CreateArray([">"]);

    private readonly ITagHelperCompletionService _tagHelperCompletionService = tagHelperCompletionService;

    public ImmutableArray<RazorCompletionItem> GetCompletionItems(RazorCompletionContext context)
    {
        var owner = context.Owner;
        if (owner is null)
        {
            Debug.Fail("Owner should never be null.");
            return [];
        }

        owner = CompletionContextHelper.AdjustSyntaxNodeForCompletion(owner);
        if (owner is null)
        {
            return [];
        }

        if (HtmlFacts.TryGetElementInfo(owner, out var containingTagNameToken, out var attributes, out _) &&
            containingTagNameToken.Span.IntersectsWith(context.AbsoluteIndex))
        {
            // Trying to complete the element type
            var stringifiedAttributes = TagHelperFacts.StringifyAttributes(attributes);
            var containingElement = owner.Parent;
            var elementCompletions = GetElementCompletions(containingElement, containingTagNameToken.Content, stringifiedAttributes, context);
            return elementCompletions;
        }

        if (HtmlFacts.TryGetAttributeInfo(
                owner,
                out containingTagNameToken,
                out var prefixLocation,
                out var selectedAttributeName,
                out var selectedAttributeNameLocation,
                out attributes) &&
            (selectedAttributeName is null ||
            selectedAttributeNameLocation?.IntersectsWith(context.AbsoluteIndex) == true ||
            (prefixLocation?.IntersectsWith(context.AbsoluteIndex) ?? false)))
        {
            if (prefixLocation.HasValue &&
                prefixLocation.Value.Length == 1 &&
                selectedAttributeNameLocation.HasValue &&
                selectedAttributeNameLocation.Value.Length > 1 &&
                selectedAttributeNameLocation.Value.Start != context.AbsoluteIndex &&
                !InOrAtEndOfAttribute(owner, context.AbsoluteIndex))
            {
                // To align with HTML completion behavior we only want to provide completion items if we're trying to resolve completion at the
                // beginning of an HTML attribute name or at the end of possible partially written attribute. We do extra checks on prefix locations here in order to rule out malformed cases when the Razor
                // compiler incorrectly parses multi-line attributes while in the middle of typing out an element. For instance:
                //
                // <SurveyPrompt |
                // @code { ... }
                //
                // Will be interpreted as having an `@code` attribute name due to multi-line attributes being a thing. Ultimately this is mostly a
                // heuristic that we have to apply in order to workaround limitations of the Razor compiler.
                return [];
            }

            var stringifiedAttributes = TagHelperFacts.StringifyAttributes(attributes);

            return GetAttributeCompletions(owner, containingTagNameToken.Content, selectedAttributeName, stringifiedAttributes, context.TagHelperDocumentContext, context.Options);

            static bool InOrAtEndOfAttribute(RazorSyntaxNode attributeSyntax, int absoluteIndex)
            {
                // When we are in the middle of writing an attribute it is treated as a minimilized one, e.g.:
                // <form asp$$ - 'asp' is parsed as MarkupMinimizedTagHelperAttributeSyntax (tag helper)
                // <SurveyPrompt Titl$$ - 'Titl' is parsed as MarkupMinimizedTagHelperAttributeSyntax as well (razor component)
                // Need to check for MarkupMinimizedAttributeBlockSyntax in order to handle cases when html tag becomes a tag helper only with certain attributes
                // We allow the absoluteIndex to be anywhere in the attribute, and for non minimized attributes,
                // so that `<SurveyPrompt Title=""` doesn't return only the html completions, because that has the effect of overwriting the casing of the attribute.
                return attributeSyntax is MarkupMinimizedTagHelperAttributeSyntax or MarkupMinimizedAttributeBlockSyntax or MarkupTagHelperAttributeSyntax &&
                       attributeSyntax.Span.Start < absoluteIndex && attributeSyntax.Span.End >= absoluteIndex;
            }
        }

        // Invalid location for TagHelper completions.
        return [];
    }

    private ImmutableArray<RazorCompletionItem> GetAttributeCompletions(
        RazorSyntaxNode containingAttribute,
        string containingTagName,
        string? selectedAttributeName,
        ImmutableArray<KeyValuePair<string, string>> attributes,
        TagHelperDocumentContext tagHelperDocumentContext,
        RazorCompletionOptions options)
    {
        var ancestors = containingAttribute.Parent.Ancestors();
        var nonDirectiveAttributeTagHelpers = tagHelperDocumentContext.TagHelpers.Where(
            static tagHelper => !tagHelper.BoundAttributes.Any(static attribute => attribute.IsDirectiveAttribute));
        var filteredContext = TagHelperDocumentContext.GetOrCreate(tagHelperDocumentContext.Prefix, nonDirectiveAttributeTagHelpers);
        var (ancestorTagName, ancestorIsTagHelper) = TagHelperFacts.GetNearestAncestorTagInfo(ancestors);
        var attributeCompletionContext = new AttributeCompletionContext(
            filteredContext,
            existingCompletions: [],
            containingTagName,
            selectedAttributeName,
            attributes,
            ancestorTagName,
            ancestorIsTagHelper,
            HtmlFacts.IsHtmlTagName);

        using var completionItems = new PooledArrayBuilder<RazorCompletionItem>();
        var completionResult = _tagHelperCompletionService.GetAttributeCompletions(attributeCompletionContext);

        foreach (var (displayText, boundAttributes) in completionResult.Completions)
        {
            var filterText = displayText;

            // This is a little bit of a hack because the information returned by _razorTagHelperCompletionService.GetAttributeCompletions
            // does not have enough information for us to determine if a completion is an indexer completion or not. Therefore we have to
            // jump through a few hoops below to:
            //   1. Determine if this specific completion is an indexer based completion
            //   2. Resolve an appropriate snippet if it is. This is more troublesome because we need to remove the ... suffix to accurately
            //      build a snippet that makes sense for the user to type.
            var isIndexer = filterText.EndsWith("...", StringComparison.Ordinal);
            if (isIndexer)
            {
                filterText = filterText[..^3];
            }

            var attributeContext = ResolveAttributeContext(boundAttributes, isIndexer, options.SnippetsSupported);
            var attributeCommitCharacters = options.UseVsCodeCompletionCommitCharacters ? [] : ResolveAttributeCommitCharacters(attributeContext);
            var isSnippet = false;
            var insertText = filterText;

            // Do not turn attributes into snippets if we are in an already written full attribute (https://github.com/dotnet/razor-tooling/issues/6724)
            if (containingAttribute is not (MarkupTagHelperAttributeSyntax or MarkupAttributeBlockSyntax) &&
                TryResolveInsertText(insertText, attributeContext, options.AutoInsertAttributeQuotes, out var snippetText))
            {
                isSnippet = true;
                insertText = snippetText;
            }

            // We change the sort text depending on the tag name due to TagHelper/non-TagHelper concerns. For instance lets say you have a TagHelper that binds to `input`.
            // Chances are you're expecting to get every other `input` completion item in addition to the TagHelper completion items and the sort order should be the default
            // because HTML completion items are 100% as applicable as other items.
            //
            // Next assume that we have a TagHelper that binds `custom` (or even `Custom`); this is a special scenario where the user has effectively created a new HTML tag
            // meaning they're probably expecting to provide all of the attributes necessary for that tag to operate. Meaning, HTML attribute completions are less important.
            // To make sure we prioritize our attribute completions above all other types of completions we set the priority to high so they're showed in the completion list
            // above all other completion items.
            var sortText = HtmlFacts.IsHtmlTagName(containingTagName)
                ? CompletionSortTextHelper.DefaultSortPriority
                : CompletionSortTextHelper.HighSortPriority;

            var attributeDescriptions = boundAttributes.SelectAsArray(boundAttribute => BoundAttributeDescriptionInfo.From(boundAttribute, isIndexer));

            var razorCompletionItem = RazorCompletionItem.CreateTagHelperAttribute(
                displayText: displayText,
                insertText: insertText,
                sortText: sortText,
                descriptionInfo: new(attributeDescriptions),
                commitCharacters: attributeCommitCharacters,
                isSnippet: isSnippet);

            completionItems.Add(razorCompletionItem);
        }

        return completionItems.ToImmutableAndClear();
    }

    private static bool TryResolveInsertText(string baseInsertText, AttributeContext context, bool autoInsertAttributeQuotes, [NotNullWhen(true)] out string? snippetText)
    {
        if (context == AttributeContext.FullSnippet)
        {
            snippetText = autoInsertAttributeQuotes
                ? $"{baseInsertText}=\"$0\""
                : $"{baseInsertText}=$0";

            return true;
        }

        snippetText = null;
        return false;
    }

    private ImmutableArray<RazorCompletionItem> GetElementCompletions(
        RazorSyntaxNode containingElement,
        string containingTagName,
        ImmutableArray<KeyValuePair<string, string>> attributes,
        RazorCompletionContext context)
    {
        var ancestors = containingElement.Ancestors();
        var (ancestorTagName, ancestorIsTagHelper) = TagHelperFacts.GetNearestAncestorTagInfo(ancestors);
        var elementCompletionContext = new ElementCompletionContext(
            context.TagHelperDocumentContext,
            context.ExistingCompletions,
            containingTagName,
            attributes,
            ancestorTagName,
            ancestorIsTagHelper,
            HtmlFacts.IsHtmlTagName);

        var completionResult = _tagHelperCompletionService.GetElementCompletions(elementCompletionContext);
        using var completionItems = new PooledArrayBuilder<RazorCompletionItem>();

        var commitChars = context.Options.CommitElementsWithSpace
            ? s_elementCommitCharacters
            : s_elementCommitCharacters_WithoutSpace;

        foreach (var (displayText, tagHelpers) in completionResult.Completions)
        {
            var descriptionInfo = new AggregateBoundElementDescription(tagHelpers.SelectAsArray(BoundElementDescriptionInfo.From));

            // Always add the regular completion item
            var razorCompletionItem = RazorCompletionItem.CreateTagHelperElement(
                displayText: displayText,
                insertText: displayText,
                descriptionInfo,
                commitCharacters: commitChars,
                isSnippet: false);

            completionItems.Add(razorCompletionItem);

            AddCompletionItemWithRequiredAttributesSnippet(
                ref completionItems.AsRef(),
                context,
                tagHelpers,
                displayText,
                descriptionInfo,
                commitChars);

            AddCompletionItemWithUsingDirective(ref completionItems.AsRef(), context, commitChars, displayText, descriptionInfo);
        }

        return completionItems.ToImmutableAndClear();
    }

    private static void AddCompletionItemWithUsingDirective(ref PooledArrayBuilder<RazorCompletionItem> completionItems, RazorCompletionContext context, ImmutableArray<RazorCommitCharacter> commitChars, string displayText, AggregateBoundElementDescription descriptionInfo)
    {
        // If this is a fully qualified name (contains a dot), it means there's an out-of-scope component
        // so we add an additional completion item with @using hint and additional edits that will insert
        // the @using correctly.
        var lastDotIndex = displayText.LastIndexOf('.');
        if (lastDotIndex == -1)
        {
            return;
        }

        var @namespace = displayText[..lastDotIndex];
        var shortName = displayText[(lastDotIndex + 1)..]; // Get the short name after the last dot
        var displayTextWithUsing = $"{shortName} - @using {@namespace}";

        var addUsingEdit = UsingDirectiveHelper.CreateAddUsingTextEdit(@namespace, context.CodeDocument);

        var razorCompletionItemWithUsing = RazorCompletionItem.CreateTagHelperElement(
            displayText: displayTextWithUsing,
            insertText: shortName,
            descriptionInfo,
            commitCharacters: commitChars,
            additionalTextEdits: [addUsingEdit]);

        completionItems.Add(razorCompletionItemWithUsing);
    }

    private const string BooleanTypeString = "System.Boolean";

    private static AttributeContext ResolveAttributeContext(
        IEnumerable<BoundAttributeDescriptor> boundAttributes,
        bool indexerCompletion,
        bool snippetsSupported)
    {
        if (indexerCompletion)
        {
            return AttributeContext.Indexer;
        }
        else if (boundAttributes.Any(static b => b.TypeName == BooleanTypeString))
        {
            // Have to use string type because IsBooleanProperty isn't set
            return AttributeContext.Minimized;
        }
        else if (snippetsSupported)
        {
            return AttributeContext.FullSnippet;
        }

        return AttributeContext.Full;
    }

    private static ImmutableArray<RazorCommitCharacter> ResolveAttributeCommitCharacters(AttributeContext attributeContext)
    {
        return attributeContext switch
        {
            AttributeContext.Indexer => [],
            AttributeContext.Minimized => MinimizedAttributeCommitCharacters,
            AttributeContext.Full => AttributeCommitCharacters,
            AttributeContext.FullSnippet => AttributeSnippetCommitCharacters,
            _ => throw new InvalidOperationException("Unexpected context"),
        };
    }

    private static void AddCompletionItemWithRequiredAttributesSnippet(
        ref PooledArrayBuilder<RazorCompletionItem> completionItems,
        RazorCompletionContext context,
        IEnumerable<TagHelperDescriptor> tagHelpers,
        string displayText,
        AggregateBoundElementDescription descriptionInfo,
        ImmutableArray<RazorCommitCharacter> commitChars)
    {
        // If snippets are not supported, exit early
        if (!context.Options.SnippetsSupported)
        {
            return;
        }

        if (TryGetEditorRequiredAttributesSnippet(tagHelpers, displayText, out var snippetText))
        {
            var snippetCompletionItem = RazorCompletionItem.CreateTagHelperElement(
                displayText: SR.FormatComponentCompletionWithRequiredAttributesLabel(displayText),
                insertText: snippetText,
                descriptionInfo: descriptionInfo,
                commitCharacters: commitChars,
                isSnippet: true);

            completionItems.Add(snippetCompletionItem);
        }
    }

    private static bool TryGetEditorRequiredAttributesSnippet(
        IEnumerable<TagHelperDescriptor> tagHelpers,
        string tagName,
        [NotNullWhen(true)] out string? snippetText)
    {
        // For components, there should only be one tag helper descriptor per component name
        // Get EditorRequired attributes from the first component tag helper
        var componentTagHelper = tagHelpers.FirstOrDefault(th => th.Kind == TagHelperKind.Component);
        if (componentTagHelper is null)
        {
            snippetText = null;
            return false;
        }

        var requiredAttributes = componentTagHelper.EditorRequiredAttributes;
        if (requiredAttributes.Length == 0)
        {
            snippetText = null;
            return false;
        }

        // Build snippet with placeholders for each required attribute
        using var _ = StringBuilderPool.GetPooledObject(out var builder);
        builder.Append(tagName);

        var tabStopIndex = 1;
        foreach (var attribute in requiredAttributes)
        {
            builder.Append(' ');
            builder.Append(attribute.Name);
            builder.Append("=\"$");
            builder.Append(tabStopIndex);
            builder.Append('"');

            tabStopIndex++;
        }

        // Add final tab stop for the element content
        builder.Append(">$0</");
        builder.Append(tagName);
        builder.Append('>');

        snippetText = builder.ToString();
        return true;
    }

    private enum AttributeContext
    {
        Indexer,
        Minimized,
        Full,
        FullSnippet
    }
}
