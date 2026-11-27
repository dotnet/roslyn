// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Enables retrieval of <see cref="TagHelperBinding"/>'s.
/// </summary>
internal sealed partial class TagHelperBinder
{
    private readonly TagHelperSet _catchAllTagHelpers;
    private readonly ReadOnlyDictionary<string, TagHelperSet> _tagNameToTagHelpersMap;

    public string? TagNamePrefix { get; }
    public TagHelperCollection TagHelpers { get; }

    /// <summary>
    /// Instantiates a new instance of the <see cref="TagHelperBinder"/>.
    /// </summary>
    /// <param name="tagNamePrefix">The tag helper prefix being used by the document.</param>
    /// <param name="tagHelpers">The <see cref="TagHelperDescriptor"/>s that the <see cref="TagHelperBinder"/>
    /// will pull from.</param>
    public TagHelperBinder(string? tagNamePrefix, TagHelperCollection tagHelpers)
    {
        TagNamePrefix = tagNamePrefix;
        TagHelpers = tagHelpers;

        ProcessDescriptors(tagHelpers, tagNamePrefix, out _tagNameToTagHelpersMap, out _catchAllTagHelpers);
    }

    private static void ProcessDescriptors(
        TagHelperCollection descriptors,
        string? tagNamePrefix,
        out ReadOnlyDictionary<string, TagHelperSet> tagNameToDescriptorsMap,
        out TagHelperSet catchAllDescriptors)
    {
        // Initialize a MemoryBuilder of TagHelperSet.Builders. We need a builder for each unique tag name.
        using var builders = new MemoryBuilder<TagHelperSet.Builder>(initialCapacity: 32, clearArray: true);

        // Keep track of what needs to be added in the second pass.
        // There will be an entry for every tag matching rule.
        // Each entry consists of an index to identify a builder and the TagHelperDescriptor to add to it.
        using var toAdd = new MemoryBuilder<(int, TagHelperDescriptor)>(initialCapacity: descriptors.Count * 4, clearArray: true);

        // Use a special TagHelperSet.Builder to track catch-all tag helpers.
        var catchAllBuilder = new TagHelperSet.Builder();

        // At most, there should only be one catch-all tag helper per descriptor.
        using var catchAllToAdd = new MemoryBuilder<TagHelperDescriptor>(initialCapacity: descriptors.Count, clearArray: true);

        // The builders are indexed using a map of "tag name" to the index of the builder in the array.
        using var _1 = SpecializedPools.GetPooledStringDictionary<int>(ignoreCase: true, out var tagNameToBuilderIndexMap);

        foreach (var tagHelper in descriptors)
        {

            foreach (var rule in tagHelper.TagMatchingRules)
            {
                var tagName = rule.TagName;

                if (tagName == TagHelperMatchingConventions.ElementCatchAllName)
                {
                    catchAllBuilder.IncreaseSize();
                    catchAllToAdd.Append(tagHelper);
                    continue;
                }

                if (!tagNameToBuilderIndexMap.TryGetValue(tagName, out var builderIndex))
                {
                    builderIndex = builders.Length;
                    builders.Append(default(TagHelperSet.Builder));

                    tagNameToBuilderIndexMap.Add(tagName, builderIndex);
                }

                builders[builderIndex].IncreaseSize();
                toAdd.Append((builderIndex, tagHelper));
            }
        }

        // Next, we walk through toAdd and add each descriptor to the appropriate builder.
        // Because we counted first, we know that each builder will allocate exactly the
        // space needed for the final result.
        foreach (var (builderIndex, tagHelper) in toAdd.AsMemory().Span)
        {
            builders[builderIndex].Add(tagHelper);
        }

        foreach (var tagHelper in catchAllToAdd.AsMemory().Span)
        {
            catchAllBuilder.Add(tagHelper);
        }

        // Build the final dictionary.
        var map = new Dictionary<string, TagHelperSet>(capacity: tagNameToBuilderIndexMap.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var (tagName, builderIndex) in tagNameToBuilderIndexMap)
        {
            map.Add(tagNamePrefix + tagName, builders[builderIndex].ToSet());
        }

        tagNameToDescriptorsMap = new ReadOnlyDictionary<string, TagHelperSet>(map);

        // Build the "catch all" tag helpers set.
        catchAllDescriptors = catchAllBuilder.ToSet();
    }

    /// <summary>
    /// Gets all tag helpers that match the given HTML tag criteria.
    /// </summary>
    /// <param name="tagName">The name of the HTML tag to match. Providing a '*' tag name
    /// retrieves catch-all <see cref="TagHelperDescriptor"/>s (descriptors that target every tag).</param>
    /// <param name="attributes">Attributes on the HTML tag.</param>
    /// <param name="parentTagName">The parent tag name of the given <paramref name="tagName"/> tag.</param>
    /// <param name="parentIsTagHelper">Is the parent tag of the given <paramref name="tagName"/> tag a tag helper.</param>
    /// <returns><see cref="TagHelperDescriptor"/>s that apply to the given HTML tag criteria.
    /// Will return <see langword="null"/> if no <see cref="TagHelperDescriptor"/>s are a match.</returns>
    public TagHelperBinding? GetBinding(
        string tagName,
        ImmutableArray<KeyValuePair<string, string>> attributes,
        string? parentTagName,
        bool parentIsTagHelper)
    {
        var tagNameSpan = tagName.AsSpan();
        var parentTagNameSpan = parentTagName.AsSpan();
        var tagNamePrefixSpan = TagNamePrefix.AsSpan();

        if (!tagNamePrefixSpan.IsEmpty)
        {
            if (!tagNameSpan.StartsWith(tagNamePrefixSpan, StringComparison.OrdinalIgnoreCase))
            {
                // The tag name doesn't start with the prefix. So, we're done.
                return null;
            }

            tagNameSpan = tagNameSpan[tagNamePrefixSpan.Length..];

            if (parentIsTagHelper)
            {
                Debug.Assert(
                    parentTagNameSpan.StartsWith(tagNamePrefixSpan, StringComparison.OrdinalIgnoreCase),
                    "If the parent is a tag helper, it must start with the tag name prefix.");

                parentTagNameSpan = parentTagNameSpan[tagNamePrefixSpan.Length..];
            }
        }

        using var resultsBuilder = new PooledArrayBuilder<TagHelperBoundRulesInfo>();
        using var tempRulesBuilder = new PooledArrayBuilder<TagMatchingRuleDescriptor>();
        using var pooledSet = HashSetPool<TagHelperDescriptor>.GetPooledObject(out var distinctSet);

        // First, try any tag helpers with this tag name.
        if (_tagNameToTagHelpersMap.TryGetValue(tagName, out var matchingDescriptors))
        {
            CollectBoundRulesInfo(
                matchingDescriptors,
                tagNameSpan, parentTagNameSpan, attributes,
                ref resultsBuilder.AsRef(), ref tempRulesBuilder.AsRef(), distinctSet);
        }

        // Next, try any "catch all" descriptors.
        CollectBoundRulesInfo(
            _catchAllTagHelpers,
            tagNameSpan, parentTagNameSpan, attributes,
            ref resultsBuilder.AsRef(), ref tempRulesBuilder.AsRef(), distinctSet);

        return resultsBuilder.Count > 0
            ? new(resultsBuilder.ToImmutableAndClear(), tagName, parentTagName, attributes, TagNamePrefix)
            : null;

        static void CollectBoundRulesInfo(
            TagHelperSet descriptors,
            ReadOnlySpan<char> tagName,
            ReadOnlySpan<char> parentTagName,
            ImmutableArray<KeyValuePair<string, string>> attributes,
            ref PooledArrayBuilder<TagHelperBoundRulesInfo> resultsBuilder,
            ref PooledArrayBuilder<TagMatchingRuleDescriptor> tempRulesBuilder,
            HashSet<TagHelperDescriptor> distinctSet)
        {
            foreach (var descriptor in descriptors)
            {
                if (!distinctSet.Add(descriptor))
                {
                    // We're already seen this descriptor, skip it.
                    continue;
                }

                Debug.Assert(tempRulesBuilder.Count == 0);

                foreach (var rule in descriptor.TagMatchingRules)
                {
                    if (TagHelperMatchingConventions.SatisfiesRule(rule, tagName, parentTagName, attributes))
                    {
                        tempRulesBuilder.Add(rule);
                    }
                }

                if (tempRulesBuilder.Count > 0)
                {
                    resultsBuilder.Add(new(descriptor, tempRulesBuilder.ToImmutable()));
                }

                tempRulesBuilder.Clear();
            }
        }
    }
}
