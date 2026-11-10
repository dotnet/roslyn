// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// The binding information for Tag Helpers resulted to a <see cref="RazorCodeDocument"/>. Represents the
/// Tag Helper information after processing by directives.
/// </summary>
internal sealed class TagHelperDocumentContext
{
    private static readonly CleanableWeakCache<(string? Prefix, Checksum TagHelpersChecksum, Checksum AliasChecksum), TagHelperDocumentContext> s_cache = new(cleanUpThreshold: 20);

    public string? Prefix { get; }
    public TagHelperCollection TagHelpers { get; }
    internal Checksum AliasChecksum { get; }
    private ImmutableArray<TagHelperAlias> Aliases { get; }
    private TagHelperBinder.TagHelperCandidate[]? _aliasCandidates;
    private int _aliasCandidatesState;
    public bool HasAliases => !Aliases.IsEmpty;

    private TagHelperBinder? _binder;

    private TagHelperDocumentContext(string? prefix, TagHelperCollection tagHelpers, ImmutableArray<TagHelperAlias> aliases, Checksum aliasChecksum)
    {
        Prefix = prefix;
        TagHelpers = tagHelpers;
        Aliases = aliases;
        AliasChecksum = aliasChecksum;
    }

    public static TagHelperDocumentContext GetOrCreate(TagHelperCollection tagHelpers)
        => GetOrCreate(prefix: null, tagHelpers);

    public static TagHelperDocumentContext GetOrCreate(string? prefix, TagHelperCollection tagHelpers)
        => GetOrCreate(prefix, tagHelpers, aliases: null);

    public static TagHelperDocumentContext GetOrCreate(string? prefix, TagHelperCollection tagHelpers, List<TagHelperAlias>? aliases)
    {
        ArgHelper.ThrowIfNull(tagHelpers);

        // A checksum distinguishes alias candidates without keeping their snapshot objects alive in the cache key.
        var aliasChecksum = CreateAliasChecksum(aliases);
        return s_cache.GetOrAdd(
            key: (prefix, tagHelpers.Checksum, aliasChecksum),
            arg: (prefix, tagHelpers, aliases, aliasChecksum),
            static arg => new(arg.prefix, arg.tagHelpers, arg.aliases is { Count: > 0 } aliases ? [.. aliases] : [], arg.aliasChecksum));
    }

    public TagHelperBinder GetBinder()
        => _binder ?? InterlockedOperations.Initialize(ref _binder, new TagHelperBinder(Prefix, TagHelpers, GetAliasCandidates()));

    public bool IsAliasTagName(string tagName)
    {
        var tagNameSpan = tagName.AsSpan();
        var prefixSpan = Prefix.AsSpan();
        if (!prefixSpan.IsEmpty && tagNameSpan.StartsWith(prefixSpan, StringComparison.OrdinalIgnoreCase))
        {
            tagNameSpan = tagNameSpan[prefixSpan.Length..];
        }

        foreach (var alias in Aliases)
        {
            if (alias.MatchesTagName(tagNameSpan))
            {
                return true;
            }
        }

        return false;
    }

    public TagHelperCandidateEnumerator GetCandidates()
        => new(TagHelpers, GetAliasCandidates());

    // Cache the alias tag names because both binder construction and completion need them.
    private TagHelperBinder.TagHelperCandidate[] GetAliasCandidates()
    {
        if (!HasAliases)
        {
            return [];
        }

        // Read the initialization state even when the array is present so its contents are safely published.
        InterlockedOperations.Initialize(
            ref _aliasCandidates,
            ref _aliasCandidatesState,
            Aliases,
            static aliases => TagHelperBinder.CreateAliasCandidates(aliases));

        Debug.Assert(_aliasCandidates is not null);
        return _aliasCandidates;
    }

    public struct TagHelperCandidateEnumerator
    {
        private readonly TagHelperCollection _tagHelpers;
        private readonly TagHelperBinder.TagHelperCandidate[] _aliasCandidates;
        private int _descriptorIndex;
        private int _aliasIndex;

        internal TagHelperCandidateEnumerator(
            TagHelperCollection tagHelpers,
            TagHelperBinder.TagHelperCandidate[] aliasCandidates)
        {
            _tagHelpers = tagHelpers;
            _aliasCandidates = aliasCandidates;
            _descriptorIndex = -1;
            _aliasIndex = -1;
            Current = default;
        }

        public readonly TagHelperCandidateEnumerator GetEnumerator()
            => this;

        public TagHelperBinder.TagHelperCandidate Current { get; private set; }

        public bool MoveNext()
        {
            if (_descriptorIndex < _tagHelpers.Count - 1)
            {
                _descriptorIndex++;
                Current = new(_tagHelpers[_descriptorIndex], AliasTagName: null);
                return true;
            }

            _aliasIndex++;
            if (_aliasIndex < _aliasCandidates.Length)
            {
                Current = _aliasCandidates[_aliasIndex];
                return true;
            }

            return false;
        }
    }

    private static Checksum CreateAliasChecksum(List<TagHelperAlias>? aliases)
    {
        if (aliases is not { Count: > 0 })
        {
            return default;
        }

        var builder = new Checksum.Builder();
        builder.Append(aliases.Count);
        foreach (var alias in aliases)
        {
            alias.AppendToChecksum(in builder);
        }

        return builder.FreeAndGetChecksum();
    }
}
