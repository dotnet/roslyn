// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER

using System.Runtime.CompilerServices;

#pragma warning disable RS0016 // Add public types and members to the declared API (this is a supporting forwarder for an internal polyfill API)
[assembly: TypeForwardedTo(typeof(System.Collections.Generic.IReadOnlySet<>))]
#pragma warning restore RS0016 // Add public types and members to the declared API

#else

namespace System.Collections.Generic;

/// <summary>
/// Provides a readonly abstraction of a set.
/// </summary>
/// <typeparam name="T">The type of elements in the set.</typeparam>
internal interface IReadOnlySet<T> : IReadOnlyCollection<T>
{
    /// <summary>
    /// Determines if the set contains a specific item
    /// </summary>
    /// <param name="item">The item to check if the set contains.</param>
    /// <returns><see langword="true" /> if found; otherwise <see langword="false" />.</returns>
    bool Contains(T item);

    /// <summary>
    /// Determines whether the current set is a proper (strict) subset of a specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current set.</param>
    /// <returns><see langword="true" /> if the current set is a proper subset of other; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">other is <see langword="null" />.</exception>
    bool IsProperSubsetOf(IEnumerable<T> other);

    /// <summary>
    /// Determines whether the current set is a proper (strict) superset of a specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current set.</param>
    /// <returns><see langword="true" /> if the collection is a proper superset of other; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">other is <see langword="null" />.</exception>
    bool IsProperSupersetOf(IEnumerable<T> other);

    /// <summary>
    /// Determine whether the current set is a subset of a specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current set.</param>
    /// <returns><see langword="true" /> if the current set is a subset of other; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">other is <see langword="null" />.</exception>
    bool IsSubsetOf(IEnumerable<T> other);

    /// <summary>
    /// Determine whether the current set is a super set of a specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current set</param>
    /// <returns><see langword="true" /> if the current set is a subset of other; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">other is <see langword="null" />.</exception>
    bool IsSupersetOf(IEnumerable<T> other);

    /// <summary>
    /// Determines whether the current set overlaps with the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current set.</param>
    /// <returns><see langword="true" /> if the current set and other share at least one common element; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">other is <see langword="null" />.</exception>
    bool Overlaps(IEnumerable<T> other);

    /// <summary>
    /// Determines whether the current set and the specified collection contain the same elements.
    /// </summary>
    /// <param name="other">The collection to compare to the current set.</param>
    /// <returns><see langword="true" /> if the current set is equal to other; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">other is <see langword="null" />.</exception>
    bool SetEquals(IEnumerable<T> other);
}

#endif
