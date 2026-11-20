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
    // Keep alias lookup data out of the ordinary map so documents without aliases retain descriptor-only sets.
    private readonly AliasMap? _aliasMap;

    public string? TagNamePrefix { get; }
    public TagHelperCollection TagHelpers { get; }

    internal readonly record struct TagHelperCandidate(TagHelperDescriptor Descriptor, string? AliasTagName)
    {
        // Child-content tags keep their own name; the alias changes the component name in their parent constraint.
        public string? TagNameOverride => Descriptor.Kind != TagHelperKind.ChildContent ? AliasTagName : null;
        public string? ParentTagOverride => Descriptor.Kind == TagHelperKind.ChildContent ? AliasTagName : null;
    }

    /// <summary>
    /// Instantiates a new instance of the <see cref="TagHelperBinder"/>.
    /// </summary>
    /// <param name="tagNamePrefix">The tag helper prefix being used by the document.</param>
    /// <param name="tagHelpers">The <see cref="TagHelperDescriptor"/>s that the <see cref="TagHelperBinder"/>
    /// will pull from.</param>
    public TagHelperBinder(string? tagNamePrefix, TagHelperCollection tagHelpers, ImmutableArray<TagHelperAlias> aliases = default)
        : this(tagNamePrefix, tagHelpers, CreateAliasCandidates(aliases))
    {
    }

    internal TagHelperBinder(string? tagNamePrefix, TagHelperCollection tagHelpers, TagHelperCandidate[] aliasCandidates)
    {
        TagNamePrefix = tagNamePrefix;
        TagHelpers = tagHelpers;

        ProcessDescriptors(tagHelpers, tagNamePrefix, out _tagNameToTagHelpersMap, out _catchAllTagHelpers);

        if (aliasCandidates.Length == 0)
        {
            _aliasMap = null;
        }
        else
        {
            ProcessAliases(aliasCandidates, tagNamePrefix, out var aliasTagNameToCandidatesMap, out var catchAllAliasTagHelpers);
            _aliasMap = new(aliasTagNameToCandidatesMap, catchAllAliasTagHelpers);
        }
    }

    private sealed class AliasMap(
        ReadOnlyDictionary<string, TagHelperCandidateSet> tagNameToCandidates,
        TagHelperCandidateSet catchAllCandidates)
    {
        public ReadOnlyDictionary<string, TagHelperCandidateSet> TagNameToCandidates { get; } = tagNameToCandidates;
        public TagHelperCandidateSet CatchAllCandidates { get; } = catchAllCandidates;
    }

    internal static TagHelperCandidate[] CreateAliasCandidates(ImmutableArray<TagHelperAlias> aliases)
    {
        if (aliases.IsDefaultOrEmpty)
        {
            return [];
        }

        var candidates = new TagHelperCandidate[aliases.Length];
        for (var i = 0; i < aliases.Length; i++)
        {
            var alias = aliases[i];
            candidates[i] = new(alias.Descriptor, alias.GetTagName());
        }

        return candidates;
    }

    private static void ProcessDescriptors(
        TagHelperCollection descriptors,
        string? tagNamePrefix,
        out ReadOnlyDictionary<string, TagHelperSet> tagNameToDescriptorsMap,
        out TagHelperSet catchAllDescriptors)
    {
        // Initialize a MemoryBuilder of TagHelperSet.Builders. We need a builder for each unique tag name.
        var builders = new MemoryBuilder<TagHelperSet.Builder>(initialCapacity: 32, clearArray: true);

        // Keep track of what needs to be added in the second pass.
        // There will be an entry for every tag matching rule.
        // Each entry consists of an index to identify a builder and the descriptor to add to it.
        var toAdd = new MemoryBuilder<(int, TagHelperDescriptor)>(initialCapacity: descriptors.Count * 4, clearArray: true);

        // Use a special TagHelperSet.Builder to track catch-all tag helpers.
        var catchAllBuilder = new TagHelperSet.Builder();

        // At most, there should only be one catch-all tag helper per descriptor.
        var catchAllToAdd = new MemoryBuilder<TagHelperDescriptor>(initialCapacity: descriptors.Count, clearArray: true);

        // The builders are indexed using a map of "tag name" to the index of the builder in the array.
        using var _1 = SpecializedPools.GetPooledStringDictionary<int>(ignoreCase: true, out var tagNameToBuilderIndexMap);

        try
        {
            foreach (var tagHelper in descriptors)
            {
                foreach (var rule in tagHelper.TagMatchingRules)
                {
                    if (rule.TagName == TagHelperMatchingConventions.ElementCatchAllName)
                    {
                        catchAllBuilder.IncreaseSize();
                        catchAllToAdd.Append(tagHelper);
                        continue;
                    }

                    if (!tagNameToBuilderIndexMap.TryGetValue(rule.TagName, out var builderIndex))
                    {
                        builderIndex = builders.Length;
                        builders.Append(default(TagHelperSet.Builder));
                        tagNameToBuilderIndexMap.Add(rule.TagName, builderIndex);
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
        finally
        {
            builders.Dispose();
            toAdd.Dispose();
            catchAllToAdd.Dispose();
        }
    }

    private static void ProcessAliases(
        TagHelperCandidate[] candidates,
        string? tagNamePrefix,
        out ReadOnlyDictionary<string, TagHelperCandidateSet> tagNameToCandidatesMap,
        out TagHelperCandidateSet catchAllCandidates)
    {
        var builders = new MemoryBuilder<TagHelperCandidateSet.Builder>(initialCapacity: 32, clearArray: true);
        var toAdd = new MemoryBuilder<(int, TagHelperCandidate)>(initialCapacity: candidates.Length * 4, clearArray: true);
        var catchAllToAdd = new MemoryBuilder<TagHelperCandidate>(initialCapacity: candidates.Length, clearArray: true);
        var catchAllBuilder = new TagHelperCandidateSet.Builder();

        using var _1 = SpecializedPools.GetPooledStringDictionary<int>(ignoreCase: true, out var tagNameToBuilderIndexMap);

        try
        {
            foreach (var candidate in candidates)
            {
                foreach (var rule in candidate.Descriptor.TagMatchingRules)
                {
                    var tagName = candidate.TagNameOverride ?? rule.TagName;

                    if (tagName == TagHelperMatchingConventions.ElementCatchAllName)
                    {
                        catchAllBuilder.IncreaseSize();
                        catchAllToAdd.Append(candidate);
                        continue;
                    }

                    if (!tagNameToBuilderIndexMap.TryGetValue(tagName, out var builderIndex))
                    {
                        builderIndex = builders.Length;
                        builders.Append(default(TagHelperCandidateSet.Builder));
                        tagNameToBuilderIndexMap.Add(tagName, builderIndex);
                    }

                    builders[builderIndex].IncreaseSize();
                    toAdd.Append((builderIndex, candidate));
                }
            }

            foreach (var (builderIndex, candidate) in toAdd.AsMemory().Span)
            {
                builders[builderIndex].Add(candidate);
            }

            foreach (var candidate in catchAllToAdd.AsMemory().Span)
            {
                catchAllBuilder.Add(candidate);
            }

            var map = new Dictionary<string, TagHelperCandidateSet>(capacity: tagNameToBuilderIndexMap.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var (tagName, builderIndex) in tagNameToBuilderIndexMap)
            {
                map.Add(tagNamePrefix + tagName, builders[builderIndex].ToSet());
            }

            tagNameToCandidatesMap = new ReadOnlyDictionary<string, TagHelperCandidateSet>(map);
            catchAllCandidates = catchAllBuilder.ToSet();
        }
        finally
        {
            builders.Dispose();
            toAdd.Dispose();
            catchAllToAdd.Dispose();
        }
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

        if (_aliasMap is null)
        {
            return GetBindingWithoutAliases(tagName, parentTagName, attributes, tagNameSpan, parentTagNameSpan);
        }

        using var resultsBuilder = new PooledArrayBuilder<TagHelperBoundRulesInfo>();
        using var tempRulesBuilder = new PooledArrayBuilder<TagMatchingRuleDescriptor>();
        using var pooledSet = HashSetPool<TagHelperCandidate>.GetPooledObject(out var distinctSet);

        // First, try ordinary tag helpers so documents without aliases stay on the original path.
        if (_tagNameToTagHelpersMap.TryGetValue(tagName, out var matchingTagHelpers))
        {
            foreach (var descriptor in matchingTagHelpers)
            {
                CollectBoundRulesInfo(
                    new(descriptor, AliasTagName: null),
                    tagNameSpan, parentTagNameSpan, attributes,
                    ref resultsBuilder.AsRef(), ref tempRulesBuilder.AsRef(), distinctSet);
            }
        }

        // Alias candidates are kept separately so they don't widen every descriptor set.
        if (_aliasMap.TagNameToCandidates.TryGetValue(tagName, out var matchingAliases))
        {
            foreach (var candidate in matchingAliases)
            {
                CollectBoundRulesInfo(
                    candidate,
                    tagNameSpan, parentTagNameSpan, attributes,
                    ref resultsBuilder.AsRef(), ref tempRulesBuilder.AsRef(), distinctSet);
            }
        }

        // Next, try any "catch all" descriptors.
        foreach (var descriptor in _catchAllTagHelpers)
        {
            CollectBoundRulesInfo(
                new(descriptor, AliasTagName: null),
                tagNameSpan, parentTagNameSpan, attributes,
                ref resultsBuilder.AsRef(), ref tempRulesBuilder.AsRef(), distinctSet);
        }

        foreach (var candidate in _aliasMap.CatchAllCandidates)
        {
            CollectBoundRulesInfo(
                candidate,
                tagNameSpan, parentTagNameSpan, attributes,
                ref resultsBuilder.AsRef(), ref tempRulesBuilder.AsRef(), distinctSet);
        }

        return resultsBuilder.Count > 0
            ? new(resultsBuilder.ToImmutableAndClear(), tagName, parentTagName, attributes, TagNamePrefix)
            : null;

        static void CollectBoundRulesInfo(
            TagHelperCandidate candidate,
            ReadOnlySpan<char> tagName,
            ReadOnlySpan<char> parentTagName,
            ImmutableArray<KeyValuePair<string, string>> attributes,
            ref PooledArrayBuilder<TagHelperBoundRulesInfo> resultsBuilder,
            ref PooledArrayBuilder<TagMatchingRuleDescriptor> tempRulesBuilder,
            HashSet<TagHelperCandidate> distinctSet)
        {
            if (!distinctSet.Add(candidate))
            {
                return;
            }

            Debug.Assert(tempRulesBuilder.Count == 0);

            foreach (var rule in candidate.Descriptor.TagMatchingRules)
            {
                if (TagHelperMatchingConventions.SatisfiesRule(
                    rule, tagName, parentTagName, attributes, candidate.TagNameOverride, candidate.ParentTagOverride))
                {
                    tempRulesBuilder.Add(rule);
                }
            }

            if (tempRulesBuilder.Count > 0)
            {
                resultsBuilder.Add(new(candidate.Descriptor, tempRulesBuilder.ToImmutable()));
            }

            tempRulesBuilder.Clear();
        }
    }

    private TagHelperBinding? GetBindingWithoutAliases(
        string tagName,
        string? parentTagName,
        ImmutableArray<KeyValuePair<string, string>> attributes,
        ReadOnlySpan<char> tagNameWithoutPrefix,
        ReadOnlySpan<char> parentTagNameWithoutPrefix)
    {
        using var resultsBuilder = new PooledArrayBuilder<TagHelperBoundRulesInfo>();
        using var tempRulesBuilder = new PooledArrayBuilder<TagMatchingRuleDescriptor>();
        using var pooledSet = HashSetPool<TagHelperDescriptor>.GetPooledObject(out var distinctSet);

        if (_tagNameToTagHelpersMap.TryGetValue(tagName, out var matchingDescriptors))
        {
            CollectBoundRulesInfo(
                matchingDescriptors,
                tagNameWithoutPrefix, parentTagNameWithoutPrefix, attributes,
                ref resultsBuilder.AsRef(), ref tempRulesBuilder.AsRef(), distinctSet);
        }

        CollectBoundRulesInfo(
            _catchAllTagHelpers,
            tagNameWithoutPrefix, parentTagNameWithoutPrefix, attributes,
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
                    // We've already seen this descriptor, so skip it.
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
