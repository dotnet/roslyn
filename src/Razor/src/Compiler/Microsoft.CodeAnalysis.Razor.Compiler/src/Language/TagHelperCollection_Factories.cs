// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    /// <summary>
    ///  Creates a new <see cref="TagHelperCollection"/> from the specified span of tag helper descriptors.
    /// </summary>
    /// <param name="span">The span of tag helper descriptors to include in the collection.</param>
    /// <returns>
    ///  A new <see cref="TagHelperCollection"/> containing the specified descriptors with automatic
    ///  deduplication based on checksums.
    /// </returns>
    /// <remarks>
    ///  <para>
    ///   This method automatically deduplicates descriptors based on their checksums and optimizes
    ///   the internal structure based on the number of elements. Empty spans return the singleton
    ///   <see cref="Empty"/> instance, single elements use optimized storage, and larger collections
    ///   use segmented storage with hash-based lookup tables when beneficial.
    ///  </para>
    /// </remarks>
    public static TagHelperCollection Create(ReadOnlySpan<TagHelperDescriptor> span)
    {
        return span switch
        {
            [] => Empty,
            [var singleItem] => new SingleSegmentCollection(singleItem),
            var items => BuildCollection(items),
        };

        static TagHelperCollection BuildCollection(ReadOnlySpan<TagHelperDescriptor> span)
        {
            using var builder = new FixedSizeBuilder(span.Length);

            builder.AddRange(span);

            return builder.ToCollection();
        }
    }

    /// <summary>
    ///  Creates a new <see cref="TagHelperCollection"/> from the specified immutable array of tag helper descriptors.
    /// </summary>
    /// <param name="array">The immutable array of tag helper descriptors to include in the collection.</param>
    /// <returns>
    ///  A new <see cref="TagHelperCollection"/> containing the specified descriptors with automatic
    ///  deduplication based on checksums.
    /// </returns>
    /// <remarks>
    ///  <para>
    ///   This method leverages the memory-efficient nature of <see cref="ImmutableArray{T}"/> by using
    ///   its underlying memory directly when possible. The collection automatically deduplicates
    ///   descriptors based on their checksums.
    ///  </para>
    ///  <para>
    ///   Empty arrays return the singleton <see cref="Empty"/> instance, and single-element arrays
    ///   use optimized storage that shares the original array's memory.
    ///  </para>
    ///  <para>
    ///   This method is given higher overload resolution priority because it uses the underlying memory
    ///   of the <see cref="ImmutableArray{T}"/> and can be more efficient than
    ///   <see cref="Create(ReadOnlySpan{TagHelperDescriptor})"/>, which must create a new array to hold the elements.
    ///  </para>
    /// </remarks>
    [OverloadResolutionPriority(1)]
    public static TagHelperCollection Create(params ImmutableArray<TagHelperDescriptor> array)
    {
        // Note: We intentionally do *not* delegate to the Create(ReadOnlySpan<TagHelperDescriptor>)
        // overload, which must copy all of the elements from the span that's passed in.
        // We can use the underlying memory of the ImmutableArray directly.
        var segment = array.AsMemory();

        return segment.Span switch
        {
            [] => Empty,
            [TagHelperDescriptor] => new SingleSegmentCollection(segment),
            _ => BuildCollection(segment)
        };

        static TagHelperCollection BuildCollection(ReadOnlyMemory<TagHelperDescriptor> segment)
        {
            using var builder = new SegmentBuilder();

            builder.AddSegment(segment);

            return builder.ToCollection();
        }
    }

    /// <summary>
    ///  Creates a new <see cref="TagHelperCollection"/> from the specified enumerable of tag helper descriptors.
    /// </summary>
    /// <param name="source">The enumerable of tag helper descriptors to include in the collection.</param>
    /// <returns>
    ///  A new <see cref="TagHelperCollection"/> containing the specified descriptors with automatic
    ///  deduplication based on checksums.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <remarks>
    ///  <para>
    ///   This method optimizes for enumerables that provide a count (such as arrays, lists, and other
    ///   collections) by pre-allocating the appropriate storage. For arbitrary enumerables without
    ///   a known count, it uses a growing buffer approach.
    ///  </para>
    ///  <para>
    ///   The collection automatically deduplicates descriptors based on their checksums and maintains
    ///   the order of first occurrence for duplicate items.
    ///  </para>
    /// </remarks>
    public static TagHelperCollection Create(IEnumerable<TagHelperDescriptor> source)
    {
        if (source.TryGetCount(out var count))
        {
            // Copy the IEnumerable to an immutable array and delegate to the
            // Create(ImmutableArray<TagHelperDescriptor>) method.

            // Note: We intentionally do *not* delegate to the Create(ReadOnlySpan<TagHelperDescriptor>)
            // overload, which must copy all of the elements from the span that's passed in.
            var array = new TagHelperDescriptor[count];
            source.CopyTo(array);

            return Create(ImmutableCollectionsMarshal.AsImmutableArray(array));
        }

        // Fallback for an arbitrary IEnumerable with no count.

        // Copy the IEnumerable to a MemoryBuilder and delegate to the other Create method.
        // Note that we can pass a span to the underlying pooled array below because
        // Create(ReadOnlySpan<TagHelperDescriptor>) copies the elements into a new array.
        using var builder = new MemoryBuilder<TagHelperDescriptor>(clearArray: true);

        foreach (var item in source)
        {
            builder.Append(item);
        }

        return Create(builder.AsMemory().Span);
    }

    /// <summary>
    ///  Merges multiple <see cref="TagHelperCollection"/> instances into a single collection.
    /// </summary>
    /// <param name="collections">The span of collections to merge.</param>
    /// <returns>
    ///  A new <see cref="TagHelperCollection"/> containing all unique tag helper descriptors from
    ///  the input collections, with duplicates removed based on checksums.
    /// </returns>
    /// <remarks>
    ///  <para>
    ///   This method efficiently merges collections by first filtering out empty collections and
    ///   those with duplicate checksums. The resulting collection maintains the order of elements
    ///   as they appear in the input collections, with the first occurrence of duplicates preserved.
    ///  </para>
    ///  <para>
    ///   If no collections are provided or all are empty, <see cref="Empty"/> is returned.
    ///   If only one non-empty unique collection is provided, that collection is returned directly.
    ///  </para>
    /// </remarks>
    [OverloadResolutionPriority(1)]
    public static TagHelperCollection Merge(params ReadOnlySpan<TagHelperCollection> collections)
    {
        switch (collections)
        {
            case []:
                return Empty;

            case [var singleCollection]:
                return singleCollection;
        }

        // First, collect the "mergeable" collections, i.e., those that are not empty and have unique checksums.
        using var _ = CollectMergeableCollections(collections, out var mergeableCollections);

        return mergeableCollections switch
        {
            [] => Empty,
            [var single] => single,
            _ => MergeMultipleCollections(mergeableCollections)
        };

        static PooledArray<TagHelperCollection> CollectMergeableCollections(
            ReadOnlySpan<TagHelperCollection> collections, out ReadOnlySpan<TagHelperCollection> result)
        {
            var pooledArray = ArrayPool<TagHelperCollection>.Shared.GetPooledArraySpan(
                minimumLength: collections.Length, clearOnReturn: true, out var destination);

            using var _ = ChecksumSetPool.Default.GetPooledObject(out var checksums);
            var index = 0;

            foreach (var collection in collections)
            {
                // Only add non-empty collections with unique checksums.
                if (!collection.IsEmpty && checksums.Add(collection.Checksum))
                {
                    destination[index++] = collection;
                }
            }

            result = destination[..index];
            return pooledArray;
        }

        static TagHelperCollection MergeMultipleCollections(ReadOnlySpan<TagHelperCollection> collections)
        {
            Debug.Assert(collections.Length >= 2);

            // Calculate number of segments to set the initial capacity of the SegmentBuilder.
            var segmentCount = 0;

            foreach (var collection in collections)
            {
                segmentCount += collection.Segments.Count;
            }

            using var builder = new SegmentBuilder(capacity: segmentCount);

            foreach (var collection in collections)
            {
                foreach (var segment in collection.Segments)
                {
                    builder.AddSegment(segment);
                }
            }

            return builder.ToCollection();
        }
    }

    /// <summary>
    ///  Merges multiple <see cref="TagHelperCollection"/> instances into a single collection.
    /// </summary>
    /// <param name="collections">The immutable array of collections to merge.</param>
    /// <returns>
    ///  A new <see cref="TagHelperCollection"/> containing all unique tag helper descriptors from
    ///  the input collections, with duplicates removed based on checksums.
    /// </returns>
    /// <remarks>
    ///  This method delegates to <see cref="Merge(ReadOnlySpan{TagHelperCollection})"/> for efficient
    ///  processing. See that method's documentation for detailed behavior information.
    /// </remarks>
    public static TagHelperCollection Merge(ImmutableArray<TagHelperCollection> collections)
        => Merge(collections.AsSpan());

    /// <summary>
    ///  Merges multiple <see cref="TagHelperCollection"/> instances into a single collection.
    /// </summary>
    /// <param name="source">The enumerable of collections to merge.</param>
    /// <returns>
    ///  A new <see cref="TagHelperCollection"/> containing all unique tag helper descriptors from
    ///  the input collections, with duplicates removed based on checksums.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <remarks>
    ///  <para>
    ///   This method optimizes for enumerables that provide a count by pre-allocating storage.
    ///   For arbitrary enumerables without a known count, it uses a growing buffer approach.
    ///  </para>
    ///  <para>
    ///   The method efficiently filters out empty collections and those with duplicate checksums,
    ///   maintaining the order of elements as they appear in the input collections.
    ///  </para>
    /// </remarks>
    public static TagHelperCollection Merge(IEnumerable<TagHelperCollection> source)
    {
        if (source.TryGetCount(out var count))
        {
            using var _ = ArrayPool<TagHelperCollection>.Shared.GetPooledArraySpan(
                minimumLength: count, clearOnReturn: true, out var collections);

            source.CopyTo(collections);

            return Merge(collections);
        }

        // Fallback for arbitrary IEnumerable
        using var builder = new MemoryBuilder<TagHelperCollection>(clearArray: true);

        foreach (var collection in source)
        {
            builder.Append(collection);
        }

        return Merge(builder.AsMemory().Span);
    }

    /// <summary>
    ///  Merges two <see cref="TagHelperCollection"/> instances into a single collection.
    /// </summary>
    /// <param name="first">The first collection to merge.</param>
    /// <param name="second">The second collection to merge.</param>
    /// <returns>
    ///  A new <see cref="TagHelperCollection"/> containing all unique tag helper descriptors from
    ///  both input collections, with duplicates removed based on checksums.
    /// </returns>
    /// <remarks>
    ///  <para>
    ///   This method provides optimized handling for the common case of merging exactly two collections.
    ///   It includes fast-path optimizations for empty collections and identical collections.
    ///  </para>
    ///  <para>
    ///   If either collection is empty, the other collection is returned directly.
    ///   If both collections are equal (same checksum), the first collection is returned.
    ///  </para>
    /// </remarks>
    public static TagHelperCollection Merge(TagHelperCollection first, TagHelperCollection second)
    {
        if (first.IsEmpty)
        {
            return second;
        }

        if (second.IsEmpty)
        {
            return first;
        }

        if (first.Equals(second))
        {
            return first;
        }

        using var _ = ArrayPool<TagHelperCollection>.Shared.GetPooledArraySpan(
            minimumLength: 2, clearOnReturn: true, out var collections);

        collections[0] = first;
        collections[1] = second;

        return Merge(collections);
    }

    public delegate void BuildAction(ref RefBuilder builder);
    public delegate void BuildAction<in TState>(ref RefBuilder builder, TState state);

    /// <summary>
    ///  Builds a new <see cref="TagHelperCollection"/> using a builder pattern with state.
    /// </summary>
    /// <typeparam name="TState">The type of the state object passed to the build action.</typeparam>
    /// <param name="state">The state object to pass to the build action.</param>
    /// <param name="action">The action that defines how to build the collection.</param>
    /// <returns>
    ///  A new <see cref="TagHelperCollection"/> built according to the specified action.
    /// </returns>
    /// <remarks>
    ///  <para>
    ///   This method provides a flexible way to build collections using a callback pattern.
    ///   The builder automatically handles deduplication and optimizes the internal structure
    ///   based on the final number of elements.
    ///  </para>
    ///  <para>
    ///   The state parameter allows passing data to the build action without creating closures,
    ///   which can improve performance by avoiding allocations.
    ///  </para>
    /// </remarks>
    public static TagHelperCollection Build<TState>(TState state, BuildAction<TState> action)
    {
        var builder = new RefBuilder();

        return BuildCore(ref builder, state, action);
    }

    /// <summary>
    ///  Builds a new <see cref="TagHelperCollection"/> using a builder pattern with state and initial capacity.
    /// </summary>
    /// <typeparam name="TState">The type of the state object passed to the build action.</typeparam>
    /// <param name="state">The state object to pass to the build action.</param>
    /// <param name="initialCapacity">The initial capacity hint for the builder.</param>
    /// <param name="action">The action that defines how to build the collection.</param>
    /// <returns>
    ///  A new <see cref="TagHelperCollection"/> built according to the specified action.
    /// </returns>
    /// <remarks>
    ///  <para>
    ///   This overload allows specifying an initial capacity hint to optimize memory allocation
    ///   when the approximate number of elements is known in advance.
    ///  </para>
    ///  <para>
    ///   The state parameter allows passing data to the build action without creating closures,
    ///   improving performance by avoiding allocations.
    ///  </para>
    /// </remarks>
    public static TagHelperCollection Build<TState>(TState state, int initialCapacity, BuildAction<TState> action)
    {
        var builder = new RefBuilder(initialCapacity);

        return BuildCore(ref builder, state, action);
    }

    private static TagHelperCollection BuildCore<TState>(ref RefBuilder builder, TState state, BuildAction<TState> action)
    {
        try
        {
            action(ref builder, state);
            return builder.ToCollection();
        }
        finally
        {
            builder.Dispose();
        }
    }

    /// <summary>
    ///  Builds a new <see cref="TagHelperCollection"/> using a builder pattern.
    /// </summary>
    /// <param name="action">The action that defines how to build the collection.</param>
    /// <returns>
    ///  A new <see cref="TagHelperCollection"/> built according to the specified action.
    /// </returns>
    /// <remarks>
    ///  This method provides a flexible way to build collections using a callback pattern.
    ///  The builder automatically handles deduplication and optimizes the internal structure
    ///  based on the final number of elements.
    /// </remarks>
    public static TagHelperCollection Build(BuildAction action)
    {
        var builder = new RefBuilder();

        return BuildCore(ref builder, action);
    }

    /// <summary>
    ///  Builds a new <see cref="TagHelperCollection"/> using a builder pattern with initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity hint for the builder.</param>
    /// <param name="action">The action that defines how to build the collection.</param>
    /// <returns>
    ///  A new <see cref="TagHelperCollection"/> built according to the specified action.
    /// </returns>
    /// <remarks>
    ///  This overload allows specifying an initial capacity hint to optimize memory allocation
    ///  when the approximate number of elements is known in advance.
    /// </remarks>
    public static TagHelperCollection Build(int initialCapacity, BuildAction action)
    {
        var builder = new RefBuilder(initialCapacity);

        return BuildCore(ref builder, action);
    }

    private static TagHelperCollection BuildCore(ref RefBuilder builder, BuildAction action)
    {
        try
        {
            action(ref builder);
            return builder.ToCollection();
        }
        finally
        {
            builder.Dispose();
        }
    }
}
