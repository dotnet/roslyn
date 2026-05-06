// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
///  Represents an immutable, ordered collection of <see cref="TagHelperDescriptor"/> instances.
/// </summary>
/// <remarks>
///  <para>
///   <see cref="TagHelperCollection"/> provides high-performance access to tag helper descriptors with
///   automatic deduplication based on <see cref="TagHelperDescriptor.Checksum"/>. The collection is
///   optimized for common operations including indexing, searching, and enumeration.
///  </para>
///  <para>
///   Collections can be created using collection expressions, factory methods, or by merging existing
///   collections. Large collections (>8 items) automatically use hash-based lookup tables for O(1)
///   search performance.
///  </para>
///  <para>
///   This type supports collection expressions:
///   <code>
///    TagHelperCollection collection = [tagHelper1, tagHelper2, tagHelper3];
///   </code>
///  </para>
/// </remarks>
[CollectionBuilder(typeof(TagHelperCollection), methodName: "Create")]
public abstract partial class TagHelperCollection : IEquatable<TagHelperCollection>, IReadOnlyList<TagHelperDescriptor>
{
    /// <summary>
    ///  Gets an empty <see cref="TagHelperCollection"/>.
    /// </summary>
    /// <returns>
    ///  A singleton empty collection instance.
    /// </returns>
    public static TagHelperCollection Empty => EmptyCollection.Instance;

    /// <summary>
    ///  Gets a value indicating whether the collection is empty.
    /// </summary>
    /// <returns>
    ///  <see langword="true"/> if the collection contains no elements; otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsEmpty => Count == 0;

    private SegmentAccessor Segments => new(this);

    /// <summary>
    ///  Gets the number of memory segments that make up this collection.
    /// </summary>
    /// <returns>
    ///  The number of contiguous memory segments.
    /// </returns>
    protected abstract int SegmentCount { get; }

    /// <summary>
    ///  Gets the memory segment at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the segment to retrieve.</param>
    /// <returns>
    ///  A <see cref="ReadOnlyMemory{T}"/> containing the tag helper descriptors in the segment.
    /// </returns>
    protected abstract ReadOnlyMemory<TagHelperDescriptor> GetSegment(int index);

    /// <summary>
    ///  Gets the number of tag helper descriptors in the collection.
    /// </summary>
    /// <returns>
    ///  The total number of tag helper descriptors.
    /// </returns>
    public abstract int Count { get; }

    /// <summary>
    ///  Gets the <see cref="TagHelperDescriptor"/> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the tag helper descriptor to retrieve.</param>
    /// <returns>
    ///  The tag helper descriptor at the specified index.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="index"/> is less than 0 or greater than or equal to <see cref="Count"/>.
    /// </exception>
    public abstract TagHelperDescriptor this[int index] { get; }

    /// <summary>
    ///  Gets the computed checksum for this collection based on the checksums of all contained descriptors.
    /// </summary>
    /// <returns>
    ///  A checksum representing the content of this collection.
    /// </returns>
    internal abstract Checksum Checksum { get; }

    /// <summary>
    ///  Determines whether the specified object is equal to the current <see cref="TagHelperCollection"/>.
    /// </summary>
    /// <param name="obj">The object to compare with the current collection.</param>
    /// <returns>
    ///  <see langword="true"/> if the specified object is a <see cref="TagHelperCollection"/> that contains
    ///  the same tag helper descriptors in the same order; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals(object? obj)
        => obj is TagHelperCollection other && Equals(other);

    /// <summary>
    ///  Determines whether the specified <see cref="TagHelperCollection"/> is equal to the current collection.
    /// </summary>
    /// <param name="other">The collection to compare with the current collection.</param>
    /// <returns>
    ///  <see langword="true"/> if the specified collection contains the same tag helper descriptors
    ///  in the same order; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///  Equality is determined by comparing the computed checksums of both collections for performance.
    ///  Collections with the same content will have identical checksums regardless of their internal structure.
    /// </remarks>
    public bool Equals(TagHelperCollection? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Count != other.Count)
        {
            return false;
        }

        return Checksum.Equals(other.Checksum);
    }

    /// <summary>
    ///  Returns a hash code for the current <see cref="TagHelperCollection"/>.
    /// </summary>
    /// <returns>
    ///  A hash code for the current collection.
    /// </returns>
    /// <remarks>
    ///  The hash code is derived from the collection's checksum, ensuring that collections
    ///  with identical content have the same hash code.
    /// </remarks>
    public override int GetHashCode()
        => Checksum.GetHashCode();

    /// <summary>
    ///  Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    ///  An <see cref="Enumerator"/> for the collection.
    /// </returns>
    public Enumerator GetEnumerator()
        => new(this);

    /// <summary>
    ///  Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    ///  An <see cref="IEnumerator{T}"/> for the collection.
    /// </returns>
    IEnumerator<TagHelperDescriptor> IEnumerable<TagHelperDescriptor>.GetEnumerator()
        => new EnumeratorImpl(this);

    /// <summary>
    ///  Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    ///  An <see cref="IEnumerator"/> for the collection.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator()
        => new EnumeratorImpl(this);

    /// <summary>
    ///  Searches for the specified <see cref="TagHelperDescriptor"/> and returns the zero-based index
    ///  of the first occurrence within the collection.
    /// </summary>
    /// <param name="item">The tag helper descriptor to locate in the collection.</param>
    /// <returns>
    ///  The zero-based index of the first occurrence of <paramref name="item"/> within the collection,
    ///  if found; otherwise, -1.
    /// </returns>
    /// <remarks>
    ///  The search is performed using the descriptor's checksum for efficient comparison.
    ///  For collections with more than 8 items, this operation uses a hash-based lookup table
    ///  for O(1) performance. Smaller collections use linear search.
    /// </remarks>
    public abstract int IndexOf(TagHelperDescriptor item);

    /// <summary>
    ///  Determines whether the collection contains a specific <see cref="TagHelperDescriptor"/>.
    /// </summary>
    /// <param name="item">The tag helper descriptor to locate in the collection.</param>
    /// <returns>
    ///  <see langword="true"/> if <paramref name="item"/> is found in the collection; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///  This method uses <see cref="IndexOf(TagHelperDescriptor)"/> internally and benefits from
    ///  the same performance optimizations.
    /// </remarks>
    public bool Contains(TagHelperDescriptor item)
        => IndexOf(item) >= 0;

    /// <summary>
    ///  Copies all the tag helper descriptors in the collection to a compatible one-dimensional span,
    ///  starting at the beginning of the target span.
    /// </summary>
    /// <param name="destination">
    ///  The one-dimensional <see cref="Span{T}"/> that is the destination of the descriptors
    ///  copied from the collection.
    /// </param>
    /// <exception cref="ArgumentException">
    ///  The <paramref name="destination"/> span is too short to contain all the descriptors in the collection.
    /// </exception>
    public abstract void CopyTo(Span<TagHelperDescriptor> destination);

    /// <summary>
    ///  Filters the collection based on a predicate and returns a new <see cref="TagHelperCollection"/>
    ///  containing only the tag helper descriptors that satisfy the condition.
    /// </summary>
    /// <typeparam name="TState">The type of the state object passed to the predicate.</typeparam>
    /// <param name="state">The state object to pass to the predicate function.</param>
    /// <param name="predicate">A function to test each tag helper descriptor for a condition.</param>
    /// <returns>
    ///  A new <see cref="TagHelperCollection"/> that contains the tag helper descriptors from the
    ///  current collection that satisfy the condition specified by <paramref name="predicate"/>.
    /// </returns>
    /// <remarks>
    ///  <para>
    ///   This method preserves the order of elements and automatically handles deduplication.
    ///   The resulting collection maintains the same performance characteristics as the original.
    ///  </para>
    ///  <para>
    ///   If no elements match the predicate, <see cref="Empty"/> is returned.
    ///   If all elements match the predicate, this collection is returned..
    ///  </para>
    ///  <para>
    ///   This overload allows passing state to the predicate without creating closures, which can
    ///   improve performance by avoiding allocations.
    ///  </para>
    /// </remarks>
    public TagHelperCollection Where<TState>(TState state, Func<TagHelperDescriptor, TState, bool> predicate)
    {
        if (IsEmpty)
        {
            return [];
        }

        // Note: We don't have to worry about checking for duplicates since this
        // collection is already de-duped.
        using var segments = new PooledArrayBuilder<ReadOnlyMemory<TagHelperDescriptor>>();

        foreach (var segment in Segments)
        {
            var span = segment.Span;
            var segmentStart = 0;

            for (var i = 0; i < span.Length; i++)
            {
                if (predicate(span[i], state))
                {
                    // Item matches predicate, continue building current segment
                    continue;
                }

                // Item doesn't match predicate - close current segment if it has items
                if (i > segmentStart)
                {
                    segments.Add(segment[segmentStart..i]);
                }

                // Start new segment after this filtered item
                segmentStart = i + 1;
            }

            // Close final segment if it has items
            if (segmentStart < span.Length)
            {
                segments.Add(segment[segmentStart..]);
            }
        }

        return segments.Count switch
        {
            0 => Empty,

            // If there's only one segment and its length is different from the original count,
            // we need to create a new collection. Otherwise, the predicate matched all items and
            // we can just return the current collection.
            1 => segments[0].Length != Count
                ? new SingleSegmentCollection(segments[0])
                : this,

            _ => new MultiSegmentCollection(segments.ToImmutableAndClear())
        };
    }

    /// <summary>
    ///  Filters the collection based on a predicate and returns a new <see cref="TagHelperCollection"/>
    ///  containing only the tag helper descriptors that satisfy the condition.
    /// </summary>
    /// <param name="predicate">A function to test each tag helper descriptor for a condition.</param>
    /// <returns>
    ///  A new <see cref="TagHelperCollection"/> that contains the tag helper descriptors from the
    ///  current collection that satisfy the condition specified by <paramref name="predicate"/>.
    /// </returns>
    /// <remarks>
    ///  <para>
    ///   This method preserves the order of elements and automatically handles deduplication.
    ///   The resulting collection maintains the same performance characteristics as the original.
    ///  </para>
    ///  <para>
    ///   If no elements match the predicate, <see cref="Empty"/> is returned.
    ///   If all elements match the predicate, this collection is returned..
    ///  </para>
    /// </remarks>
    public TagHelperCollection Where(Predicate<TagHelperDescriptor> predicate)
    {
        if (IsEmpty)
        {
            return [];
        }

        // Note: We don't have to worry about checking for duplicates since this
        // collection is already de-duped.
        using var segments = new PooledArrayBuilder<ReadOnlyMemory<TagHelperDescriptor>>();

        foreach (var segment in Segments)
        {
            var span = segment.Span;
            var segmentStart = 0;

            for (var i = 0; i < span.Length; i++)
            {
                if (predicate(span[i]))
                {
                    // Item matches predicate, continue building current segment
                    continue;
                }

                // Item doesn't match predicate - close current segment if it has items
                if (i > segmentStart)
                {
                    segments.Add(segment[segmentStart..i]);
                }

                // Start new segment after this filtered item
                segmentStart = i + 1;
            }

            // Close final segment if it has items
            if (segmentStart < span.Length)
            {
                segments.Add(segment[segmentStart..]);
            }
        }

        return segments.Count switch
        {
            0 => Empty,

            // If there's only one segment and its length is different from the original count,
            // we need to create a new collection. Otherwise, the predicate matched all items and
            // we can just return the current collection.
            1 => segments[0].Length != Count
                ? new SingleSegmentCollection(segments[0])
                : this,

            _ => new MultiSegmentCollection(segments.ToImmutableAndClear())
        };
    }
}
