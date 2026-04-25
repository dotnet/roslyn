// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal class TagHelperCompletionService : ITagHelperCompletionService
{
    private static readonly HashSetPool<TagHelperDescriptor> s_shortNameSetPool =
        HashSetPool<TagHelperDescriptor>.Create(ShortNameToFullyQualifiedComparer.Instance);

    private static readonly HashSet<TagHelperDescriptor> s_emptyHashSet = new();

    // This API attempts to understand a users context as they're typing in a Razor file to provide TagHelper based attribute IntelliSense.
    //
    // Scenarios for TagHelper attribute IntelliSense follows:
    // 1. TagHelperDescriptor's have matching required attribute names
    //  -> Provide IntelliSense for the required attributes of those descriptors to lead users towards a TagHelperified element.
    // 2. TagHelperDescriptor entirely applies to current element. Tag name, attributes, everything is fulfilled.
    //  -> Provide IntelliSense for the bound attributes for the applied descriptors.
    //
    // Within each of the above scenarios if an attribute completion has a corresponding bound attribute we associate it with the corresponding
    // BoundAttributeDescriptor. By doing this a user can see what C# type a TagHelper expects for the attribute.
    public AttributeCompletionResult GetAttributeCompletions(AttributeCompletionContext completionContext)
    {
        ArgHelper.ThrowIfNull(completionContext);

        var attributeCompletions = completionContext.ExistingCompletions.ToDictionary(
            completion => completion,
            _ => new HashSet<BoundAttributeDescriptor>(),
            StringComparer.OrdinalIgnoreCase);

        var documentContext = completionContext.DocumentContext;
        var tagHelpersForTag = TagHelperFacts.GetTagHelpersGivenTag(documentContext, completionContext.CurrentTagName, completionContext.CurrentParentTagName);
        if (tagHelpersForTag.IsEmpty)
        {
            // If the current tag has no possible descriptors then we can't have any additional attributes.
            return AttributeCompletionResult.Create(attributeCompletions);
        }

        var prefix = documentContext.Prefix ?? string.Empty;
        Debug.Assert(completionContext.CurrentTagName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        var applicableTagHelperBinding = TagHelperFacts.GetTagHelperBinding(
            documentContext,
            completionContext.CurrentTagName,
            completionContext.Attributes,
            completionContext.CurrentParentTagName,
            completionContext.CurrentParentIsTagHelper);

        using var _ = HashSetPool<TagHelperDescriptor>.GetPooledObject(out var applicableDescriptors);

        if (applicableTagHelperBinding is { TagHelpers: var tagHelpers })
        {
            applicableDescriptors.UnionWith(tagHelpers);
        }

        var unprefixedTagName = completionContext.CurrentTagName[prefix.Length..];

        if (!completionContext.InHTMLSchema(unprefixedTagName) &&
            applicableDescriptors.All(descriptor => descriptor.TagOutputHint is null))
        {
            // This isn't a known HTML tag and no descriptor has an output element hint. Remove all previous completions.
            attributeCompletions.Clear();
        }

        foreach (var tagHelper in tagHelpersForTag)
        {
            if (applicableDescriptors.Contains(tagHelper))
            {
                foreach (var attributeDescriptor in tagHelper.BoundAttributes)
                {
                    if (!attributeDescriptor.Name.IsNullOrEmpty())
                    {
                        UpdateCompletions(attributeDescriptor.Name, attributeDescriptor);
                    }

                    if (!string.IsNullOrEmpty(attributeDescriptor.IndexerNamePrefix))
                    {
                        UpdateCompletions(attributeDescriptor.IndexerNamePrefix + "...", attributeDescriptor);
                    }
                }
            }
            else
            {
                var htmlNameToBoundAttribute = new Dictionary<string, BoundAttributeDescriptor>(StringComparer.OrdinalIgnoreCase);
                foreach (var attributeDescriptor in tagHelper.BoundAttributes)
                {
                    if (attributeDescriptor.Name != null)
                    {
                        htmlNameToBoundAttribute[attributeDescriptor.Name] = attributeDescriptor;
                    }

                    if (!attributeDescriptor.IndexerNamePrefix.IsNullOrEmpty())
                    {
                        htmlNameToBoundAttribute[attributeDescriptor.IndexerNamePrefix] = attributeDescriptor;
                    }
                }

                foreach (var rule in tagHelper.TagMatchingRules)
                {
                    foreach (var requiredAttribute in rule.Attributes)
                    {
                        if (htmlNameToBoundAttribute.TryGetValue(requiredAttribute.Name, out var attributeDescriptor))
                        {
                            UpdateCompletions(requiredAttribute.DisplayName, attributeDescriptor);
                        }
                        else
                        {
                            UpdateCompletions(requiredAttribute.DisplayName, possibleDescriptor: null);
                        }
                    }
                }
            }
        }

        var completionResult = AttributeCompletionResult.Create(attributeCompletions);
        return completionResult;

        void UpdateCompletions(string attributeName, BoundAttributeDescriptor? possibleDescriptor)
        {
            if (completionContext.Attributes.Any(attribute => string.Equals(attribute.Key, attributeName, StringComparison.OrdinalIgnoreCase)) &&
                (completionContext.CurrentAttributeName is null ||
                !string.Equals(attributeName, completionContext.CurrentAttributeName, StringComparison.OrdinalIgnoreCase)))
            {
                // Attribute is already present on this element and it is not the attribute in focus.
                // It shouldn't exist in the completion list.
                return;
            }

            if (!attributeCompletions.TryGetValue(attributeName, out var rules))
            {
                rules = new HashSet<BoundAttributeDescriptor>();
                attributeCompletions[attributeName] = rules;
            }

            if (possibleDescriptor != null)
            {
                rules.Add(possibleDescriptor);
            }
        }
    }

    public ElementCompletionResult GetElementCompletions(ElementCompletionContext completionContext)
    {
        ArgHelper.ThrowIfNull(completionContext);

        var elementCompletions = new Dictionary<string, HashSet<TagHelperDescriptor>>(StringComparer.Ordinal);

        AddAllowedChildrenCompletions(completionContext, elementCompletions);

        if (elementCompletions.Count > 0)
        {
            // If the containing element is already a TagHelper and only allows certain children.
            var emptyResult = ElementCompletionResult.Create(elementCompletions);
            return emptyResult;
        }

        var tagAttributes = completionContext.Attributes;

        var catchAllTagHelpers = new HashSet<TagHelperDescriptor>();
        var prefix = completionContext.DocumentContext.Prefix ?? string.Empty;

        var possibleChildTagHelpers = TagHelperFacts.GetTagHelpersGivenParent(
            completionContext.DocumentContext,
            completionContext.ContainingParentTagName);

        possibleChildTagHelpers = FilterFullyQualifiedTagHelpers(possibleChildTagHelpers);

        foreach (var possibleDescriptor in possibleChildTagHelpers)
        {
            var addRuleCompletions = false;
            var checkAttributeRules = true;
            var outputHint = possibleDescriptor.TagOutputHint;

            foreach (var rule in possibleDescriptor.TagMatchingRules)
            {
                if (!TagHelperMatchingConventions.SatisfiesParentTag(rule, completionContext.ContainingParentTagName.AsSpan()))
                {
                    continue;
                }

                if (rule.TagName == TagHelperMatchingConventions.ElementCatchAllName)
                {
                    catchAllTagHelpers.Add(possibleDescriptor);
                }
                else if (elementCompletions.ContainsKey(rule.TagName))
                {
                    // If we've previously added a completion item for this rules tag, then we want to add this item
                    addRuleCompletions = true;
                }
                else if (completionContext.ExistingCompletions.Contains(rule.TagName))
                {
                    // If Html wants to show a completion item for rules tag, then we want to add this item
                    addRuleCompletions = true;
                }
                else if (outputHint != null)
                {
                    // If the current descriptor has an output hint we need to make sure it shows up only when its output hint would normally show up.
                    // Example: We have a MyTableTagHelper that has an output hint of "table" and a MyTrTagHelper that has an output hint of "tr".
                    // If we try typing in a situation like this: <body > | </body>
                    // We'd expect to only get "my-table" as a completion because the "body" tag doesn't allow "tr" tags.
                    addRuleCompletions = completionContext.ExistingCompletions.Contains(outputHint);
                }
                else if (!completionContext.InHTMLSchema(rule.TagName) || rule.TagName.Any(c => char.IsUpper(c)))
                {
                    // If there is an unknown HTML schema tag that doesn't exist in the current completion we should add it. This happens for
                    // TagHelpers that target non-schema oriented tags.
                    // The second condition is a workaround for the fact that InHTMLSchema does a case insensitive comparison.
                    // We want completions to not dedupe by casing. E.g, we want to show both <div> and <DIV> completion items separately.
                    addRuleCompletions = true;

                    // If the tag is not in the Html schema, then don't check attribute rules. Normally we want to check them so that
                    // users don't see html tag and tag helper completions for the same thing, where the tag helper doesn't apply. In
                    // cases where the tag is not html, we want to show it even if it doesn't apply, as the user could fix that later.
                    checkAttributeRules = false;
                }

                // If we think this completion should be added based on tag name, thats great, but lets also make sure the attributes are correct
                if (addRuleCompletions && (!checkAttributeRules || TagHelperMatchingConventions.SatisfiesAttributes(rule, tagAttributes)))
                {
                    UpdateCompletions(prefix + rule.TagName, possibleDescriptor, elementCompletions);
                }
            }
        }

        // We needed to track all catch-alls and update their completions after all other completions have been completed.
        // This way, any TagHelper added completions will also have catch-alls listed under their entries.
        foreach (var catchAllTagHelper in catchAllTagHelpers)
        {
            foreach (var (completionTagName, completionTagHelpers) in elementCompletions)
            {
                if (completionTagHelpers.Count > 0 ||
                    (!string.IsNullOrEmpty(prefix) && completionTagName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    // The current completion either has other TagHelper's associated with it or is prefixed with a non-empty
                    // TagHelper prefix.
                    UpdateCompletions(completionTagName, catchAllTagHelper, elementCompletions, completionTagHelpers);
                }
            }
        }

        var result = ElementCompletionResult.Create(elementCompletions);
        return result;

        static void UpdateCompletions(string tagName, TagHelperDescriptor possibleDescriptor, Dictionary<string, HashSet<TagHelperDescriptor>> elementCompletions, HashSet<TagHelperDescriptor>? tagHelperDescriptors = null)
        {
            if (possibleDescriptor.BoundAttributes.Any(static boundAttribute => boundAttribute.IsDirectiveAttribute))
            {
                // This is a TagHelper that ultimately represents a DirectiveAttribute. In classic Razor TagHelper land TagHelpers with bound attribute descriptors
                // are valuable to show in the completion list to understand what was possible for a certain tag; however, with Blazor directive attributes stand
                // on their own and shouldn't be indicated at the element level completion.
                return;
            }

            HashSet<TagHelperDescriptor>? existingRuleDescriptors;
            if (tagHelperDescriptors is not null)
            {
                existingRuleDescriptors = tagHelperDescriptors;
            }
            else if (!elementCompletions.TryGetValue(tagName, out existingRuleDescriptors))
            {
                existingRuleDescriptors = new HashSet<TagHelperDescriptor>();
                elementCompletions[tagName] = existingRuleDescriptors;
            }

            existingRuleDescriptors.Add(possibleDescriptor);
        }
    }

    private void AddAllowedChildrenCompletions(
        ElementCompletionContext completionContext,
        Dictionary<string, HashSet<TagHelperDescriptor>> elementCompletions)
    {
        if (completionContext.ContainingTagName is null)
        {
            // If we're at the root then there's no containing TagHelper to specify allowed children.
            return;
        }

        var prefix = completionContext.DocumentContext.Prefix ?? string.Empty;

        var binding = TagHelperFacts.GetTagHelperBinding(
            completionContext.DocumentContext,
            completionContext.ContainingParentTagName,
            completionContext.Attributes,
            parentTag: null,
            parentIsTagHelper: false);

        if (binding is null)
        {
            // Containing tag is not a TagHelper; therefore, it allows any children.
            return;
        }

        foreach (var tagHelper in binding.TagHelpers)
        {
            foreach (var childTag in tagHelper.AllowedChildTags)
            {
                var prefixedName = string.Concat(prefix, childTag.Name);
                var tagHelpersForTag = TagHelperFacts.GetTagHelpersGivenTag(
                    completionContext.DocumentContext,
                    prefixedName,
                    completionContext.ContainingParentTagName);

                if (tagHelpersForTag.IsEmpty)
                {
                    if (!elementCompletions.ContainsKey(prefixedName))
                    {
                        elementCompletions[prefixedName] = s_emptyHashSet;
                    }

                    continue;
                }

                if (!elementCompletions.TryGetValue(prefixedName, out var existingRuleDescriptors))
                {
                    existingRuleDescriptors = new HashSet<TagHelperDescriptor>();
                    elementCompletions[prefixedName] = existingRuleDescriptors;
                }

                existingRuleDescriptors.UnionWith(tagHelpersForTag);
            }
        }
    }

    private static TagHelperCollection FilterFullyQualifiedTagHelpers(TagHelperCollection tagHelpers)
    {
        // We want to filter 'tagHelpers' and remove any tag helpers that require a fully-qualified name match
        // but have a short name match present.

        // First, collect all "short name" tag helpers, i.e. those that do not require a fully qualified name match.
        using var _ = s_shortNameSetPool.GetPooledObject(out var shortNameSet);

        foreach (var tagHelper in tagHelpers)
        {
            if (!tagHelper.IsFullyQualifiedNameMatch)
            {
                shortNameSet.Add(tagHelper);
            }
        }

        return tagHelpers.Where(shortNameSet, static (tagHelper, shortNameSet) =>
        {
            // We want to keep tag helpers that either:
            // 1. Do not require a fully qualified name match (i.e., short name tag helpers).
            // 2. Are fully qualified tag helpers that do not have a corresponding short name tag helper.
            return !tagHelper.IsFullyQualifiedNameMatch || !shortNameSet.Contains(tagHelper);
        });
    }
}
