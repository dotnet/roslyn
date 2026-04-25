// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Tooltip;
using Microsoft.VisualStudio.Editor.Razor;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal partial class DirectiveAttributeCompletionItemProvider : DirectiveAttributeCompletionItemProviderBase
{
    private const string Ellipsis = "...";
    private const string QuotedAttributeValueSnippetSuffix = "=\"$0\"";
    private const string UnquotedAttributeValueSnippetSuffix = "=$0";

    public override ImmutableArray<RazorCompletionItem> GetCompletionItems(RazorCompletionContext context)
    {
        if (!context.SyntaxTree.Options.FileKind.IsComponent())
        {
            // Directive attributes are only supported in components
            return [];
        }

        var owner = context.Owner;
        if (owner is null)
        {
            return [];
        }

        if (!TryGetAttributeInfo(owner, out _, out var attributeName, out var attributeNameLocation, out var parameterName, out var parameterNameLocation))
        {
            // Either we're not in an attribute or the attribute is so malformed that we can't provide proper completions.
            return [];
        }

        if (!TryGetElementInfo(owner.Parent.Parent, out var containingTagName, out var attributes))
        {
            // This should never be the case, it means that we're operating on an attribute that doesn't have a tag.
            return [];
        }

        // We don't provide Directive Attribute completions when we're in the middle of
        // another unrelated (doesn't start with @) partially completed attribute.
        // <svg xml:| ></svg> (attributeName = "xml:") should not get any directive attribute completions.
        if (!attributeName.IsNullOrWhiteSpace() && !attributeName.StartsWith('@'))
        {
            return [];
        }

        var isAttributeRequest = attributeNameLocation.IntersectsWith(context.AbsoluteIndex);
        var isParameterRequest = parameterNameLocation.IntersectsWith(context.AbsoluteIndex);

        if (!isAttributeRequest && !isParameterRequest)
        {
            // This class only provides completions on attribute/parameter names.
            return [];
        }

        var inSnippetContext = InSnippetContext(owner, context.Options);

        var completionContext = new DirectiveAttributeCompletionContext()
        {
            SelectedAttributeName = attributeName,
            SelectedParameterName = parameterName,
            ExistingAttributes = attributes,
            UseSnippets = inSnippetContext,
            InAttributeName = isAttributeRequest,
            InParameterName = isParameterRequest,
            Options = context.Options
        };

        return GetAttributeCompletions(containingTagName, completionContext, context.TagHelperDocumentContext);

        static bool InSnippetContext(RazorSyntaxNode owner, RazorCompletionOptions options)
        {
            return options.SnippetsSupported
                // Don't create snippet text when attribute is already in the tag and we are trying to replace it
                // Otherwise you could have something like @onabort=""=""
                && owner is not (MarkupTagHelperDirectiveAttributeSyntax or MarkupAttributeBlockSyntax)
                && owner.Parent is not (MarkupTagHelperDirectiveAttributeSyntax or MarkupAttributeBlockSyntax);
        }
    }

    // Internal for testing
    internal static ImmutableArray<RazorCompletionItem> GetAttributeCompletions(
        string containingTagName,
        DirectiveAttributeCompletionContext completionContext,
        TagHelperDocumentContext documentContext)
    {
        var tagHelpersForTag = TagHelperFacts.GetTagHelpersGivenTag(documentContext, containingTagName, parentTag: null);
        if (tagHelpersForTag.IsEmpty)
        {
            // If the current tag has no possible tag helpers then we can't have any directive attributes.
            return [];
        }

        // Use ordinal dictionary because attributes are case sensitive when matching
        using var _ = SpecializedPools.GetPooledStringDictionary<AttributeCompletionDetails>(out var attributeCompletions);

        // Collect indexer bound attributes and their parent tag helper type names. Indexer attributes indicate an attribute prefix
        // for which they apply. That can be used in an attribute name context to determine potential parameters. E.g.,
        // there exists an indexer indicating it applies to attributes that start with "@bind-" and specifies six different
        // parameters applicable for those attributes (":format", ":event", ":culture", ":get", ":set", ":after")
        var indexerAttributes = new MemoryBuilder<BoundAttributeDescriptor>(initialCapacity: 8, clearArray: true);
        try
        {
            CollectIndexerDescriptors(tagHelpersForTag, completionContext, ref indexerAttributes);

            foreach (var tagHelper in tagHelpersForTag)
            {
                foreach (var attribute in tagHelper.BoundAttributes)
                {
                    if (attribute.IsDirectiveAttribute)
                    {
                        AddAttributeNameCompletions(attribute, completionContext, attributeCompletions);
                        AddParameterNameCompletions(attribute, indexerAttributes.AsMemory().Span, completionContext, attributeCompletions);
                    }
                }
            }

            // Use the mapping populated above to create completion items
            return CreateCompletionItems(completionContext, attributeCompletions);
        }
        finally
        {
            indexerAttributes.Dispose();
        }
    }

    private static void CollectIndexerDescriptors(
        TagHelperCollection tagHelpersForTag,
        DirectiveAttributeCompletionContext completionContext,
        ref MemoryBuilder<BoundAttributeDescriptor> builder)
    {
        if (completionContext.InParameterName)
        {
            // No need to calculate the indexers When in a parameter name
            return;
        }

        foreach (var tagHelper in tagHelpersForTag)
        {
            foreach (var attribute in tagHelper.BoundAttributes)
            {
                if (attribute.IsDirectiveAttribute && !attribute.IndexerNamePrefix.IsNullOrEmpty())
                {
                    builder.Append(attribute);
                }
            }
        }
    }

    private static void AddAttributeNameCompletions(
        BoundAttributeDescriptor attribute,
        DirectiveAttributeCompletionContext completionContext,
        Dictionary<string, AttributeCompletionDetails> attributeCompletions)
    {
        if (!completionContext.InAttributeName)
        {
            // Only add attribute name completions when in an attribute name context
            return;
        }

        var isIndexer = completionContext.SelectedAttributeName.EndsWith(Ellipsis, StringComparison.Ordinal);
        var descriptionInfo = BoundAttributeDescriptionInfo.From(attribute, isIndexer, attribute.Parent.TypeName);

        var tagHelper = attribute.Parent;

        if (!TryAddAttributeCompletion(attribute.Name, descriptionInfo, tagHelper, completionContext, attributeCompletions) &&
            attribute.Parameters.Length > 0)
        {
            // This attribute has parameters and the base attribute name (@bind) is already satisfied. We need to check if there are any valid
            // parameters left to be provided, if so, we need to still represent the base attribute name in the completion list.

            foreach (var parameter in attribute.Parameters)
            {
                if (!completionContext.AlreadySatisfiesParameter(parameter, attribute))
                {
                    // This bound attribute parameter has not had a completion entry added for it, re-represent the base attribute name in the completion list
                    AddAttributeCompletion(attribute.Name, descriptionInfo, tagHelper, completionContext, attributeCompletions);
                    break;
                }
            }
        }

        if (!attribute.IndexerNamePrefix.IsNullOrEmpty())
        {
            TryAddAttributeCompletion(
                attribute.IndexerNamePrefix + Ellipsis, descriptionInfo, tagHelper, completionContext, attributeCompletions);
        }
    }

    private static void AddParameterNameCompletions(
        BoundAttributeDescriptor attribute,
        ReadOnlySpan<BoundAttributeDescriptor> indexerAttributes,
        DirectiveAttributeCompletionContext completionContext,
        Dictionary<string, AttributeCompletionDetails> attributeCompletions)
    {
        if (completionContext.InAttributeName && !attribute.IndexerNamePrefix.IsNullOrEmpty())
        {
            // Don't add parameters on indexers in attribute name contexts
            return;
        }

        if (completionContext.InParameterName && !completionContext.CanSatisfyAttribute(attribute))
        {
            // Don't add parameters when the selected attribute name can't satisfy the given attribute descriptor in parameter name contexts
            return;
        }

        // Add indexer parameter completions first so they display first in completion descriptions.
        foreach (var indexerAttribute in indexerAttributes)
        {
            var indexerNamePrefix = indexerAttribute.IndexerNamePrefix.AssumeNotNull();

            if (!attribute.Name.StartsWith(indexerNamePrefix))
            {
                continue;
            }

            AddCompletionsForParameters(attribute, indexerAttribute.Parameters, completionContext, attributeCompletions);
        }

        // Then add regular parameter completions
        AddCompletionsForParameters(attribute, attribute.Parameters, completionContext, attributeCompletions);

        return;

        static void AddCompletionsForParameters(
            BoundAttributeDescriptor attribute,
            ImmutableArray<BoundAttributeParameterDescriptor> parameters,
            DirectiveAttributeCompletionContext completionContext,
            Dictionary<string, AttributeCompletionDetails> attributeCompletions)
        {
            var tagHelper = attribute.Parent;

            foreach (var parameter in parameters)
            {
                if (completionContext.AlreadySatisfiesParameter(parameter, attribute))
                {
                    // There's already an existing attribute that satisfies this parameter, don't show it in the completion list.
                    continue;
                }

                var displayName = completionContext.InParameterName
                    ? parameter.Name
                    : $"{attribute.Name}:{parameter.Name}";

                AddParameterCompletion(
                    displayName,
                    descriptionInfo: BoundAttributeDescriptionInfo.From(parameter),
                    tagHelper,
                    completionContext,
                    attributeCompletions);
            }
        }
    }

    private static ImmutableArray<RazorCompletionItem> CreateCompletionItems(
        DirectiveAttributeCompletionContext completionContext,
        Dictionary<string, AttributeCompletionDetails> attributeCompletions)
    {
        using var completionItems = new PooledArrayBuilder<RazorCompletionItem>(capacity: attributeCompletions.Count);

        foreach (var (displayText, (kind, descriptions, commitCharacters)) in attributeCompletions)
        {
            var isIndexer = displayText.EndsWith(Ellipsis, StringComparison.Ordinal);
            var isSnippet = !isIndexer && completionContext.UseSnippets;
            var autoInsertAttributeQuotes = completionContext.Options.AutoInsertAttributeQuotes;

            var insertText = ComputeInsertText(displayText, isIndexer, isSnippet, autoInsertAttributeQuotes);

            Debug.Assert(kind is RazorCompletionItemKind.DirectiveAttribute or RazorCompletionItemKind.DirectiveAttributeParameter);

            var razorCompletionItem = kind == RazorCompletionItemKind.DirectiveAttribute
                ? RazorCompletionItem.CreateDirectiveAttribute(displayText, insertText, descriptionInfo: new(descriptions), commitCharacters, isSnippet)
                : RazorCompletionItem.CreateDirectiveAttributeParameter(displayText, insertText, descriptionInfo: new(descriptions), commitCharacters, isSnippet);

            completionItems.Add(razorCompletionItem);
        }

        return completionItems.ToImmutableAndClear();
    }

    private static string ComputeInsertText(string displayText, bool isIndexer, bool isSnippet, bool autoInsertAttributeQuotes)
    {
        var originalInsertText = displayText.AsMemory();

        // Strip off the @ from the insertion text. This change is here to align the insertion text with the
        // completion hooks into VS and VSCode. Basically, completion triggers when `@` is typed so we don't
        // want to insert `@bind` because `@` already exists.
        var insertText = originalInsertText.Span.StartsWith('@')
            ? originalInsertText[1..]
            : originalInsertText;

        // Indexer attribute, we don't want to insert with the triple dot.
        if (isIndexer)
        {
            Debug.Assert(insertText.Span.EndsWith(Ellipsis, StringComparison.Ordinal));
            return insertText[..^3].ToString();
        }

        if (isSnippet)
        {
            var suffixText = autoInsertAttributeQuotes
                ? QuotedAttributeValueSnippetSuffix
                : UnquotedAttributeValueSnippetSuffix;

            // We are trying for snippet text only for non-indexer attributes, e.g. *not* something like "@bind-..."
            return string.Create(
                length: insertText.Length + suffixText.Length,
                state: (insertText, suffixText),
                static (destination, state) =>
                {
                    var (insertText, suffixText) = state;

                    insertText.Span.CopyTo(destination);
                    suffixText.AsSpan().CopyTo(destination[insertText.Length..]);
                });
        }

        // Don't create another string unnecessarily, even though ReadOnlySpan.ToString() special-cases
        // the string to avoid allocation.
        return insertText.Span == originalInsertText.Span
            ? displayText
            : insertText.ToString();
    }

    private static bool TryAddAttributeCompletion(
        string attributeName,
        BoundAttributeDescriptionInfo descriptionInfo,
        TagHelperDescriptor tagHelper,
        DirectiveAttributeCompletionContext completionContext,
        Dictionary<string, AttributeCompletionDetails> attributeCompletions)
    {
        if (completionContext.SelectedAttributeName != attributeName &&
            completionContext.ExistingAttributes.Contains(attributeName))
        {
            // Attribute is already present on this element and it is not the selected attribute.
            // It shouldn't exist in the completion list.
            return false;
        }

        AddAttributeCompletion(attributeName, descriptionInfo, tagHelper, completionContext, attributeCompletions);
        return true;
    }

    private static void AddAttributeCompletion(
        string attributeName,
        BoundAttributeDescriptionInfo descriptionInfo,
        TagHelperDescriptor tagHelper,
        DirectiveAttributeCompletionContext completionContext,
        Dictionary<string, AttributeCompletionDetails> attributeCompletions)
        => AddCompletion(RazorCompletionItemKind.DirectiveAttribute,
            attributeName, descriptionInfo, tagHelper, completionContext, attributeCompletions);

    private static void AddParameterCompletion(
        string attributeName,
        BoundAttributeDescriptionInfo descriptionInfo,
        TagHelperDescriptor tagHelper,
        DirectiveAttributeCompletionContext completionContext,
        Dictionary<string, AttributeCompletionDetails> attributeCompletions)
        => AddCompletion(RazorCompletionItemKind.DirectiveAttributeParameter,
            attributeName, descriptionInfo, tagHelper, completionContext, attributeCompletions);

    private static void AddCompletion(
        RazorCompletionItemKind kind,
        string attributeName,
        BoundAttributeDescriptionInfo descriptionInfo,
        TagHelperDescriptor tagHelper,
        DirectiveAttributeCompletionContext completionContext,
        Dictionary<string, AttributeCompletionDetails> attributeCompletions)
    {
        ImmutableArray<BoundAttributeDescriptionInfo> descriptions;
        ImmutableArray<RazorCommitCharacter> commitCharacters;

        if (attributeCompletions.TryGetValue(attributeName, out var existingDetails))
        {
            (descriptions, commitCharacters) = existingDetails;

            if (!descriptions.Contains(descriptionInfo))
            {
                descriptions = descriptions.Add(descriptionInfo);
            }
        }
        else
        {
            descriptions = [descriptionInfo];
            commitCharacters = [];
        }

        // Verify not an indexer attribute, as those don't commit with standard chars
        if (!attributeName.EndsWith(Ellipsis, StringComparison.Ordinal))
        {
            // We always add "=" as a commit character in Visual Studio.
            var useEqualsCommit = !completionContext.Options.UseVsCodeCompletionCommitCharacters ||
                                  commitCharacters.Any(static c => c.Character == "=");

            var useSpaceCommit = commitCharacters.Any(static c => c.Character == " ") ||
                                 tagHelper.BoundAttributes.Any(static a => a.IsBooleanProperty);

            commitCharacters = DefaultCommitCharacters.Get(useEqualsCommit, useSpaceCommit, completionContext.UseSnippets);
        }

        attributeCompletions[attributeName] = new(kind, descriptions, commitCharacters);
    }
}
