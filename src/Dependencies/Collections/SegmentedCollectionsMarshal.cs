// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Collections;

/// <summary>
/// An unsafe class that provides a set of methods to access the underlying data representations of immutable segmented
/// collections.
/// </summary>
internal static class SegmentedCollectionsMarshal
{
    /// <summary>
    /// Gets the backing storage array for a <see cref="SegmentedArray{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the array.</typeparam>
    /// <param name="array">The segmented array.</param>
    /// <returns>The backing storage array for the segmented array. Note that replacing segments within the returned
    /// value will invalidate the <see cref="SegmentedArray{T}"/> data structure.</returns>
    public static T[][] AsSegments<T>(SegmentedArray<T> array)
        => SegmentedArray<T>.PrivateMarshal.AsSegments(array);

    /// <summary>
    /// Gets a <see cref="SegmentedArray{T}"/> value wrapping the input T[][].
    /// </summary>
    /// <typeparam name="T">The type of elements in the input.</typeparam>
    /// <param name="length">The combined length of the input arrays</param>
    /// <param name="segments">The input array to wrap in the returned <see cref="SegmentedArray{T}"/> value.</param>
    /// <returns>A <see cref="SegmentedArray{T}"/> value wrapping <paramref name="segments"/>.</returns>
    /// <remarks>
    /// <para>
    /// When using this method, callers should take extra care to ensure that they're the sole owners of the input
    /// array, and that it won't be modified once the returned <see cref="SegmentedArray{T}"/> value starts
    /// being used. Doing so might cause undefined behavior in code paths which don't expect the contents of a given
    /// <see cref="SegmentedArray{T}"/> values to change outside their control.
    /// </para>
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="segments"/> is <see langword="null"/></exception>
    public static SegmentedArray<T> AsSegmentedArray<T>(int length, T[][] segments)
        => SegmentedArray<T>.PrivateMarshal.AsSegmentedArray(length, segments);

    /// <summary>
    /// Gets either a ref to a <typeparamref name="TValue"/> in the <see cref="SegmentedDictionary{TKey, TValue}"/> or a
    /// ref null if it does not exist in the <paramref name="dictionary"/>.
    /// </summary>
    /// <param name="dictionary">The dictionary to get the ref to <typeparamref name="TValue"/> from.</param>
    /// <param name="key">The key used for lookup.</param>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <remarks>
    /// Items should not be added or removed from the <see cref="SegmentedDictionary{TKey, TValue}"/> while the ref
    /// <typeparamref name="TValue"/> is in use. The ref null can be detected using <see cref="M:System.Runtime.CompilerServices.Unsafe.IsNullRef``1(``0@)"/>.
    /// </remarks>
    [SuppressMessage("Documentation", "CA1200", Justification = "Not all targets can resolve the documented method reference.")]
    public static ref TValue GetValueRefOrNullRef<TKey, TValue>(SegmentedDictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
        => ref SegmentedDictionary<TKey, TValue>.PrivateMarshal.FindValue(dictionary, key);

    /// <summary>
    /// Gets either a read-only ref to a <typeparamref name="TValue"/> in the <see cref="ImmutableSegmentedDictionary{TKey, TValue}"/>
    /// or a ref null if it does not exist in the <paramref name="dictionary"/>.
    /// </summary>
    /// <param name="dictionary">The dictionary to get the ref to <typeparamref name="TValue"/> from.</param>
    /// <param name="key">The key used for lookup.</param>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <remarks>
    /// The ref null can be detected using <see cref="M:System.Runtime.CompilerServices.Unsafe.IsNullRef``1(``0@)"/>.
    /// </remarks>
    [SuppressMessage("Documentation", "CA1200", Justification = "Not all targets can resolve the documented method reference.")]
    public static ref readonly TValue GetValueRefOrNullRef<TKey, TValue>(ImmutableSegmentedDictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
        => ref ImmutableSegmentedDictionary<TKey, TValue>.PrivateMarshal.FindValue(dictionary, key);

    /// <summary>
    /// Gets either a ref to a <typeparamref name="TValue"/> in the <see cref="ImmutableSegmentedDictionary{TKey, TValue}.Builder"/>
    /// or a ref null if it does not exist in the <paramref name="dictionary"/>.
    /// </summary>
    /// <param name="dictionary">The dictionary to get the ref to <typeparamref name="TValue"/> from.</param>
    /// <param name="key">The key used for lookup.</param>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <remarks>
    /// Items should not be added or removed from the <see cref="ImmutableSegmentedDictionary{TKey, TValue}.Builder"/>
    /// while the ref <typeparamref name="TValue"/> is in use. The ref null can be detected using
    /// <see cref="M:System.Runtime.CompilerServices.Unsafe.IsNullRef``1(``0@)"/>.
    /// </remarks>
    [SuppressMessage("Documentation", "CA1200", Justification = "Not all targets can resolve the documented method reference.")]
    public static ref TValue GetValueRefOrNullRef<TKey, TValue>(ImmutableSegmentedDictionary<TKey, TValue>.Builder dictionary, TKey key)
        where TKey : notnull
        => ref ImmutableSegmentedDictionary<TKey, TValue>.Builder.PrivateMarshal.FindValue(dictionary, key);

    /// <summary>
    /// Gets an <see cref="ImmutableSegmentedList{T}"/> value wrapping the input <see cref="SegmentedList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the input segmented list.</typeparam>
    /// <param name="list">The input segmented list to wrap in the returned <see cref="ImmutableSegmentedList{T}"/> value.</param>
    /// <returns>An <see cref="ImmutableSegmentedList{T}"/> value wrapping <paramref name="list"/>.</returns>
    /// <remarks>
    /// <para>
    /// When using this method, callers should take extra care to ensure that they're the sole owners of the input
    /// list, and that it won't be modified once the returned <see cref="ImmutableSegmentedList{T}"/> value starts
    /// being used. Doing so might cause undefined behavior in code paths which don't expect the contents of a given
    /// <see cref="ImmutableSegmentedList{T}"/> values to change after its creation.
    /// </para>
    /// <para>
    /// If <paramref name="list"/> is <see langword="null"/>, the returned <see cref="ImmutableSegmentedList{T}"/> value
    /// will be uninitialized (i.e. its <see cref="ImmutableSegmentedList{T}.IsDefault"/> property will be
    /// <see langword="true"/>).
    /// </para>
    /// </remarks>
    public static ImmutableSegmentedList<T> AsImmutableSegmentedList<T>(SegmentedList<T>? list)
        => ImmutableSegmentedList<T>.PrivateMarshal.AsImmutableSegmentedList(list);

    /// <summary>
    /// Gets the underlying <see cref="SegmentedList{T}"/> for an input <see cref="ImmutableSegmentedList{T}"/> value.
    /// </summary>
    /// <typeparam name="T">The type of elements in the input <see cref="ImmutableSegmentedList{T}"/> value.</typeparam>
    /// <param name="list">The input <see cref="ImmutableSegmentedList{T}"/> value to get the underlying <see cref="SegmentedList{T}"/> from.</param>
    /// <returns>The underlying <see cref="SegmentedList{T}"/> for <paramref name="list"/>, if present; otherwise, <see langword="null"/>.</returns>
    /// <remarks>
    /// <para>
    /// When using this method, callers should make sure to not pass the resulting underlying list to methods that
    /// might mutate it. Doing so might cause undefined behavior in code paths using <paramref name="list"/> which
    /// don't expect the contents of the <see cref="ImmutableSegmentedList{T}"/> value to change.
    /// </para>
    /// <para>
    /// If <paramref name="list"/> is uninitialized (i.e. its <see cref="ImmutableSegmentedList{T}.IsDefault"/> property is
    /// <see langword="true"/>), the resulting <see cref="SegmentedList{T}"/> will be <see langword="null"/>.
    /// </para>
    /// </remarks>
    public static SegmentedList<T>? AsSegmentedList<T>(ImmutableSegmentedList<T> list)
        => ImmutableSegmentedList<T>.PrivateMarshal.AsSegmentedList(list);

    /// <summary>
    /// Gets an <see cref="ImmutableSegmentedHashSet{T}"/> value wrapping the input <see cref="SegmentedHashSet{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the input segmented hash set.</typeparam>
    /// <param name="set">The input segmented hash set to wrap in the returned <see cref="ImmutableSegmentedHashSet{T}"/> value.</param>
    /// <returns>An <see cref="ImmutableSegmentedHashSet{T}"/> value wrapping <paramref name="set"/>.</returns>
    /// <remarks>
    /// <para>
    /// When using this method, callers should take extra care to ensure that they're the sole owners of the input
    /// set, and that it won't be modified once the returned <see cref="ImmutableSegmentedHashSet{T}"/> value starts
    /// being used. Doing so might cause undefined behavior in code paths which don't expect the contents of a given
    /// <see cref="ImmutableSegmentedHashSet{T}"/> values to change after its creation.
    /// </para>
    /// <para>
    /// If <paramref name="set"/> is <see langword="null"/>, the returned <see cref="ImmutableSegmentedHashSet{T}"/>
    /// value will be uninitialized (i.e. its <see cref="ImmutableSegmentedHashSet{T}.IsDefault"/> property will be
    /// <see langword="true"/>).
    /// </para>
    /// </remarks>
    public static ImmutableSegmentedHashSet<T> AsImmutableSegmentedHashSet<T>(SegmentedHashSet<T>? set)
        => ImmutableSegmentedHashSet<T>.PrivateMarshal.AsImmutableSegmentedHashSet(set);

    /// <summary>
    /// Gets the underlying <see cref="SegmentedHashSet{T}"/> for an input <see cref="ImmutableSegmentedHashSet{T}"/> value.
    /// </summary>
    /// <typeparam name="T">The type of elements in the input <see cref="ImmutableSegmentedHashSet{T}"/> value.</typeparam>
    /// <param name="set">The input <see cref="ImmutableSegmentedHashSet{T}"/> value to get the underlying <see cref="SegmentedHashSet{T}"/> from.</param>
    /// <returns>The underlying <see cref="SegmentedHashSet{T}"/> for <paramref name="set"/>, if present; otherwise, <see langword="null"/>.</returns>
    /// <remarks>
    /// <para>
    /// When using this method, callers should make sure to not pass the resulting underlying hash set to methods that
    /// might mutate it. Doing so might cause undefined behavior in code paths using <paramref name="set"/> which
    /// don't expect the contents of the <see cref="ImmutableSegmentedHashSet{T}"/> value to change.
    /// </para>
    /// <para>
    /// If <paramref name="set"/> is uninitialized (i.e. its <see cref="ImmutableSegmentedHashSet{T}.IsDefault"/>
    /// property is <see langword="true"/>), the resulting <see cref="SegmentedHashSet{T}"/> will be <see langword="null"/>.
    /// </para>
    /// </remarks>
    public static SegmentedHashSet<T>? AsSegmentedHashSet<T>(ImmutableSegmentedHashSet<T> set)
        => ImmutableSegmentedHashSet<T>.PrivateMarshal.AsSegmentedHashSet(set);

    /// <summary>
    /// Gets an <see cref="ImmutableSegmentedDictionary{TKey, TValue}"/> value wrapping the input <see cref="SegmentedDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the input segmented dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the input segmented dictionary.</typeparam>
    /// <param name="dictionary">The input segmented dictionary to wrap in the returned <see cref="ImmutableSegmentedDictionary{TKey, TValue}"/> value.</param>
    /// <returns>An <see cref="ImmutableSegmentedDictionary{TKey, TValue}"/> value wrapping <paramref name="dictionary"/>.</returns>
    /// <remarks>
    /// <para>
    /// When using this method, callers should take extra care to ensure that they're the sole owners of the input
    /// dictionary, and that it won't be modified once the returned <see cref="ImmutableSegmentedDictionary{TKey, TValue}"/>
    /// value starts being used. Doing so might cause undefined behavior in code paths which don't expect the contents
    /// of a given <see cref="ImmutableSegmentedDictionary{TKey, TValue}"/> values to change after its creation.
    /// </para>
    /// <para>
    /// If <paramref name="dictionary"/> is <see langword="null"/>, the returned <see cref="ImmutableSegmentedDictionary{TKey, TValue}"/>
    /// value will be uninitialized (i.e. its <see cref="ImmutableSegmentedDictionary{TKey, TValue}.IsDefault"/>
    /// property will be <see langword="true"/>).
    /// </para>
    /// </remarks>
    public static ImmutableSegmentedDictionary<TKey, TValue> AsImmutableSegmentedDictionary<TKey, TValue>(SegmentedDictionary<TKey, TValue>? dictionary)
        where TKey : notnull
        => ImmutableSegmentedDictionary<TKey, TValue>.PrivateMarshal.AsImmutableSegmentedDictionary(dictionary);

    /// <summary>
    /// Gets the underlying <see cref="SegmentedDictionary{TKey, TValue}"/> for an input <see cref="ImmutableSegmentedDictionary{TKey, TValue}"/> value.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the input <see cref="ImmutableSegmentedDictionary{TKey, TValue}"/> value.</typeparam>
    /// <typeparam name="TValue">The type of values in the input <see cref="ImmutableSegmentedDictionary{TKey, TValue}"/> value.</typeparam>
    /// <param name="dictionary">The input <see cref="ImmutableSegmentedDictionary{TKey, TValue}"/> value to get the underlying <see cref="SegmentedDictionary{TKey, TValue}"/> from.</param>
    /// <returns>The underlying <see cref="SegmentedDictionary{TKey, TValue}"/> for <paramref name="dictionary"/>, if present; otherwise, <see langword="null"/>.</returns>
    /// <remarks>
    /// <para>
    /// When using this method, callers should make sure to not pass the resulting underlying dictionary to methods that
    /// might mutate it. Doing so might cause undefined behavior in code paths using <paramref name="dictionary"/> which
    /// don't expect the contents of the <see cref="ImmutableSegmentedDictionary{TKey, TValue}"/> value to change.
    /// </para>
    /// <para>
    /// If <paramref name="dictionary"/> is uninitialized (i.e. its <see cref="ImmutableSegmentedDictionary{TKey, TValue}.IsDefault"/>
    /// property is <see langword="true"/>), the resulting <see cref="SegmentedDictionary{TKey, TValue}"/> will be <see langword="null"/>.
    /// </para>
    /// </remarks>
    public static SegmentedDictionary<TKey, TValue>? AsSegmentedDictionary<TKey, TValue>(ImmutableSegmentedDictionary<TKey, TValue> dictionary)
        where TKey : notnull
        => ImmutableSegmentedDictionary<TKey, TValue>.PrivateMarshal.AsSegmentedDictionary(dictionary);
}
