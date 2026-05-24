// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

/// <summary>
///  Wraps a pooled <see cref="ImmutableArray{T}.Builder"/> but doesn't allocate it until
///  it's needed. Note: Dispose this to ensure that the pooled array builder is returned
///  to the pool.
///
///  There is significant effort to avoid retrieving the <see cref="ImmutableArray{T}.Builder"/>.
///  For very small arrays of length 4 or less, the elements will be stored on the stack. If the array
///  grows larger than 4 elements, a builder will be employed. Afterward, the build will
///  continue to be used, even if the arrays shrinks and has fewer than 4 elements.
/// </summary>
[NonCopyable]
[CollectionBuilder(typeof(PooledArrayBuilder), nameof(PooledArrayBuilder.Create))]
internal partial struct PooledArrayBuilder<T> : IDisposable
{
    public static PooledArrayBuilder<T> Empty => default;

    /// <summary>
    ///  The number of items that can be stored inline.
    /// </summary>
    private const int InlineCapacity = 4;

    private ArrayBuilderPool<T>? _builderPool;

    /// <summary>
    ///  A builder to be used as storage after the first time that the number
    ///  of items exceeds <see cref="InlineCapacity"/>. Once the builder is used,
    ///  it is still used even if the number of items shrinks below <see cref="InlineCapacity"/>.
    ///  Essentially, if this field is non-null, it will be used as storage.
    /// </summary>
    private ImmutableArray<T>.Builder? _builder;

    /// <summary>
    ///  An optional initial capacity for the builder.
    /// </summary>
    private int? _capacity;

    private T _element0;
    private T _element1;
    private T _element2;
    private T _element3;

    /// <summary>
    ///  The number of inline elements. Note that this value is only used when <see cref="_builder"/> is <see langword="null"/>.
    /// </summary>
    private int _inlineCount;

    public PooledArrayBuilder(int? capacity = null, ArrayBuilderPool<T>? builderPool = null)
    {
        _capacity = capacity is > InlineCapacity ? capacity : null;
        _builderPool = builderPool;
        _element0 = default!;
        _element1 = default!;
        _element2 = default!;
        _element3 = default!;
        _inlineCount = 0;
    }

    private PooledArrayBuilder(in PooledArrayBuilder<T> builder)
    {
        // This is an intentional copy used to create an Enumerator.
#pragma warning disable RS0042
        this = builder;
#pragma warning restore RS0042
    }

    public void Dispose()
    {
        // Return _builder to the pool if necessary. Note that we don't need to clear the inline elements here
        // because this type is intended to be allocated on the stack and the GC can reclaim objects from the
        // stack after the last use of a reference to them.
        if (_builder is { } innerBuilder)
        {
            _builderPool?.Return(innerBuilder);
            _builder = null;
        }
    }

    /// <summary>
    ///  Retrieves the inner <see cref="_builder"/>, moving any inline elements to it if necessary.
    /// </summary>
    private ImmutableArray<T>.Builder GetBuilder()
    {
        if (!TryGetBuilder(out var builder))
        {
            MoveInlineItemsToBuilder();
            builder = _builder;
        }

        return builder;
    }

    /// <summary>
    ///  Retrieves the inner <see cref="_builder"/>.
    /// </summary>
    /// <returns>
    ///  Returns <see langword="true"/> if <see cref="_builder"/> is available; otherwise <see langword="false"/>
    /// </returns>
    /// <remarks>
    ///  This should only be used by methods that will not add to the inner <see cref="_builder"/>.
    /// </remarks>
    private readonly bool TryGetBuilder([NotNullWhen(true)] out ImmutableArray<T>.Builder? builder)
    {
        builder = _builder;
        return builder is not null;
    }

    /// <summary>
    ///  Retrieves the inner <see cref="_builder"/> and resets its capacity if necessary.
    /// </summary>
    /// <returns>
    ///  Returns <see langword="true"/> if <see cref="_builder"/> is available; otherwise <see langword="false"/>
    /// </returns>
    /// <remarks>
    ///  This should only be used by methods that will add to the inner <see cref="_builder"/>.
    /// </remarks>
    private readonly bool TryGetBuilderAndEnsureCapacity([NotNullWhen(true)] out ImmutableArray<T>.Builder? builder)
    {
        if (TryGetBuilder(out builder))
        {
            if (builder.Capacity == 0 && _capacity is int capacity)
            {
                builder.Capacity = capacity;
            }
        }

        return builder is not null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearInlineElement(int index)
    {
        Debug.Assert(_inlineCount <= InlineCapacity);

        // Clearing out an item makes it potentially available for garbage collection.
        // Note: On .NET Core, we can be a bit more judicious and only zero-out
        // fields that contain references to heap-allocated objects.

#if NETCOREAPP
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
#endif
        {
            SetInlineElement(index, default!);
        }
    }

    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            if (TryGetBuilder(out var builder))
            {
                return builder[index];
            }

            if (index >= _inlineCount)
            {
                ThrowIndexOutOfRangeException();
            }

            return GetInlineElement(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (TryGetBuilder(out var builder))
            {
                builder[index] = value;
                return;
            }

            if (index >= _inlineCount)
            {
                ThrowIndexOutOfRangeException();
            }

            SetInlineElement(index, value);
        }
    }

    public T this[Index index]
    {
        readonly get => this[index.GetOffset(Count)];
        set => this[index.GetOffset(Count)] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly T GetInlineElement(int index)
    {
        Debug.Assert(_inlineCount <= InlineCapacity);

        return index switch
        {
            0 => _element0,
            1 => _element1,
            2 => _element2,
            3 => _element3,
            _ => Assumed.Unreachable<T>()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetInlineElement(int index, T value)
    {
        Debug.Assert(_inlineCount <= InlineCapacity);

        switch (index)
        {
            case 0:
                _element0 = value;
                break;

            case 1:
                _element1 = value;
                break;

            case 2:
                _element2 = value;
                break;

            case 3:
                _element3 = value;
                break;

            default:
                Assumed.Unreachable();
                break;
        }
    }

    public readonly int Count
        => _builder?.Count ?? _inlineCount;

    public readonly int Capacity
        => _builder?.Capacity ?? _capacity ?? InlineCapacity;

    public void Add(T item)
        => Insert(Count, item);

    public void AddRange(ImmutableArray<T> items)
        => InsertRange(Count, items);

    public void AddRange(ReadOnlySpan<T> items)
    {
        // Note: We don't delegate this overload to InsertRange(ReadOnlySpan<T>) because
        // ImmutableArray<T>.Builder supports an AddRange overload that takes a ReadOnlySpan<T>.
        // Delegating to InsertRange(ReadOnlySpan<T>) could unnecessarily introduce an
        // array allocation and copy.

        if (items.IsEmpty)
        {
            return;
        }

        if (TryGetBuilderAndEnsureCapacity(out var builder))
        {
            builder.AddRange(items);
        }
        else if (_inlineCount + items.Length <= InlineCapacity)
        {
            foreach (var item in items)
            {
                SetInlineElement(_inlineCount, item);
                _inlineCount++;
            }
        }
        else
        {
            MoveInlineItemsToBuilder();
            _builder.AddRange(items);
        }
    }

    public void AddRange<TList>(TList list)
        where TList : struct, IReadOnlyList<T>
        => AddRange(list, startIndex: 0, list.Count);

    public void AddRange<TList>(TList list, int startIndex, int count)
        where TList : struct, IReadOnlyList<T>
    {
        // Note: We don't delegate this overload to InsertRange<TList>(TList, int, int) because
        // it requires extra allocations that aren't necessary for AddRange(...).

        if (count == 0)
        {
            return;
        }

        var (start, end) = (startIndex, startIndex + count - 1);

        if (TryGetBuilderAndEnsureCapacity(out var builder))
        {
            for (var i = start; i <= end; i++)
            {
                builder.Add(list[i]);
            }

            return;
        }

        if (_inlineCount + count <= InlineCapacity)
        {
            for (var i = start; i <= end; i++)
            {
                SetInlineElement(_inlineCount, list[i]);
                _inlineCount++;
            }
        }
        else
        {
            MoveInlineItemsToBuilder();

            for (var i = start; i <= end; i++)
            {
                _builder.Add(list[i]);
            }
        }
    }

    public void AddRange(IEnumerable<T> items)
        => InsertRange(Count, items);

    public void Clear()
    {
        if (TryGetBuilder(out var builder))
        {
            // Keep using a real builder to avoid churn in the object pool.
            builder.Clear();
        }
        else
        {
            var oldCapacity = _capacity;
            this = Empty;
            _capacity = oldCapacity;
        }
    }

    public readonly Enumerator GetEnumerator()
        => new(in this);

    public void Insert(int index, T item)
    {
        Debug.Assert(index >= 0 && index <= Count);

        if (TryGetBuilderAndEnsureCapacity(out var builder))
        {
            builder.Insert(index, item);
        }
        else if (_inlineCount < InlineCapacity)
        {
            // Shift elements if not inserting at the end.
            if (index < _inlineCount)
            {
                ShiftInlineItemsByOffset(index, offset: 1);
            }

            SetInlineElement(index, item);
            _inlineCount++;
        }
        else
        {
            MoveInlineItemsToBuilder();
            _builder.Insert(index, item);
        }
    }

    public void InsertRange(int index, ImmutableArray<T> items)
    {
        Debug.Assert(index >= 0 && index <= Count);

        if (items.IsEmpty)
        {
            return;
        }

        if (TryGetBuilderAndEnsureCapacity(out var builder))
        {
            builder.InsertRange(index, items);
        }
        else if (_inlineCount + items.Length <= InlineCapacity)
        {
            // Shift elements if not inserting at the end.
            if (index < _inlineCount)
            {
                ShiftInlineItemsByOffset(index, offset: items.Length);
            }

            foreach (var item in items)
            {
                SetInlineElement(index++, item);
                _inlineCount++;
            }
        }
        else
        {
            MoveInlineItemsToBuilder();
            _builder.InsertRange(index, items);
        }
    }

    public void InsertRange(int index, ReadOnlySpan<T> items)
    {
        Debug.Assert(index >= 0 && index <= Count);

        if (items.IsEmpty)
        {
            return;
        }

        if (TryGetBuilderAndEnsureCapacity(out var builder))
        {
            builder.InsertRange(index, items);
        }
        else if (_inlineCount + items.Length <= InlineCapacity)
        {
            // Shift elements if not inserting at the end.
            if (index < _inlineCount)
            {
                ShiftInlineItemsByOffset(index, offset: items.Length);
            }

            foreach (var item in items)
            {
                SetInlineElement(index++, item);
                _inlineCount++;
            }
        }
        else
        {
            MoveInlineItemsToBuilder();
            _builder.InsertRange(index, items);
        }
    }

    public void InsertRange<TList>(int index, TList list)
        where TList : struct, IReadOnlyList<T>
        => InsertRange(index, list, startIndex: 0, list.Count);

    public void InsertRange<TList>(int index, TList list, int startIndex, int count)
        where TList : struct, IReadOnlyList<T>
    {
        if (index == Count)
        {
            // AddRange doesn't delegate to this method. Instead, this delegates to AddRange for
            // insertions at the end.
            AddRange(list, startIndex, count);
            return;
        }

        if (count == 0)
        {
            return;
        }

        var (start, end) = (startIndex, startIndex + count - 1);

        if (TryGetBuilderAndEnsureCapacity(out var builder))
        {
            var spacer = ImmutableCollectionsMarshal.AsImmutableArray(new T[count]);
            builder.InsertRange(index, spacer);

            for (var i = start; i <= end; i++)
            {
                builder[index++] = list[i];
            }

            return;
        }

        if (_inlineCount + count <= InlineCapacity)
        {
            // Shift elements if not inserting at the end.
            if (index < _inlineCount)
            {
                ShiftInlineItemsByOffset(index, offset: count);
            }

            for (var i = start; i <= end; i++)
            {
                SetInlineElement(index++, list[i]);
                _inlineCount++;
            }
        }
        else
        {
            MoveInlineItemsToBuilder();

            var spacer = ImmutableCollectionsMarshal.AsImmutableArray(new T[count]);
            _builder.InsertRange(index, spacer);

            for (var i = start; i <= end; i++)
            {
                _builder[index++] = list[i];
            }
        }
    }

    public void InsertRange(int index, IEnumerable<T> items)
    {
        if (TryGetBuilderAndEnsureCapacity(out var builder))
        {
            builder.InsertRange(index, items);
            return;
        }

        if (!items.TryGetCount(out var itemCount))
        {
            // We couldn't retrieve a count, so we have to enumerate the elements.
            foreach (var item in items)
            {
                Insert(index++, item);
            }

            return;
        }

        if (itemCount == 0)
        {
            // No items, so there's nothing to do.
            return;
        }

        if (_inlineCount + itemCount <= InlineCapacity)
        {
            // Shift elements if not inserting at the end.
            if (index < _inlineCount)
            {
                ShiftInlineItemsByOffset(index, offset: itemCount);
            }

            // The items can fit into our inline elements.
            foreach (var item in items)
            {
                SetInlineElement(index++, item);
                _inlineCount++;
            }
        }
        else
        {
            // The items can't fit into our inline elements, so we switch to a builder.
            MoveInlineItemsToBuilder();
            _builder.InsertRange(index, items);
        }
    }

    public void RemoveAt(int index)
    {
        if (TryGetBuilderAndEnsureCapacity(out var builder))
        {
            builder.RemoveAt(index);
            return;
        }

        if (index < 0 || index >= _inlineCount)
        {
            ThrowIndexOutOfRangeException();
        }

        // Copy inline elements depending on the index to be removed.
        switch (index)
        {
            case 0:
                _element0 = _element1;

                if (_inlineCount > 1)
                {
                    _element1 = _element2;
                }

                if (_inlineCount > 2)
                {
                    _element2 = _element3;
                }

                break;

            case 1:
                _element1 = _element2;

                if (_inlineCount > 2)
                {
                    _element2 = _element3;
                }

                break;

            case 2:
                _element2 = _element3;
                break;
        }

        // Clear the last element if necessary.
        if (_inlineCount > 3)
        {
            ClearInlineElement(3);
        }

        // Decrement the count.
        _inlineCount--;
    }

    public void RemoveAt(Index index)
        => RemoveAt(index.GetOffset(Count));

    /// <summary>
    ///  Returns the current contents as an <see cref="ImmutableArray{T}"/> and sets
    ///  the collection to a zero length array.
    /// </summary>
    /// <remarks>
    ///  If <see cref="ImmutableArray{T}.Builder.Capacity"/> equals <see cref="Count"/>, the
    ///  internal array will be extracted as an <see cref="ImmutableArray{T}"/> without copying
    ///  the contents. Otherwise, the contents will be copied into a new array. The collection
    ///  will then be set to a zero-length array.
    /// </remarks>
    /// <returns>An immutable array.</returns>
    public ImmutableArray<T> ToImmutableAndClear()
    {
        if (TryGetBuilder(out var builder))
        {
            return builder.ToImmutableAndClear();
        }

        var inlineArray = InlineItemsToImmutableArray();

        var oldCapacity = _capacity;
        this = Empty;
        _capacity = oldCapacity;

        return inlineArray;
    }

    public readonly ImmutableArray<T> ToImmutable()
    {
        if (TryGetBuilder(out var builder))
        {
            return builder.ToImmutable();
        }

        return InlineItemsToImmutableArray();
    }

    private readonly ImmutableArray<T> InlineItemsToImmutableArray()
    {
        Debug.Assert(_inlineCount <= InlineCapacity);

        return _inlineCount switch
        {
            0 => [],
            1 => [_element0],
            2 => [_element0, _element1],
            3 => [_element0, _element1, _element2],
            _ => [_element0, _element1, _element2, _element3]
        };
    }

    public readonly T[] ToArray()
        => ImmutableCollectionsMarshal.AsArray(ToImmutable()).AssumeNotNull();

    public T[] ToArrayAndClear()
        => ImmutableCollectionsMarshal.AsArray(ToImmutableAndClear()).AssumeNotNull();

    public void Push(T item)
    {
        Add(item);
    }

    public readonly T Peek()
    {
        return this[^1];
    }

    public T Pop()
    {
        var index = ^1;
        var item = this[index];
        RemoveAt(index);

        return item;
    }

    public bool TryPop([MaybeNullWhen(false)] out T item)
    {
        if (Count == 0)
        {
            item = default;
            return false;
        }

        item = Pop();
        return true;
    }

    /// <summary>
    ///  Determines whether this builder contains any elements.
    /// </summary>
    /// <returns>
    ///  <see langword="true"/> if this builder contains any elements; otherwise, <see langword="false"/>.
    /// </returns>
    public readonly bool Any()
        => Count > 0;

    /// <summary>
    ///  Determines whether any element in this builder satisfies a condition.
    /// </summary>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  <see langword="true"/> if this builder is not empty and at least one of its elements passes
    ///  the test in the specified predicate; otherwise, <see langword="false"/>.
    /// </returns>
    public readonly bool Any(Func<T, bool> predicate)
    {
        foreach (var item in this)
        {
            if (predicate(item))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///  Determines whether any element in this builder satisfies a condition.
    /// </summary>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  <see langword="true"/> if this builder is not empty and at least one of its elements passes
    ///  the test in the specified predicate; otherwise, <see langword="false"/>.
    /// </returns>
    public readonly bool Any<TArg>(TArg arg, Func<T, TArg, bool> predicate)
    {
        foreach (var item in this)
        {
            if (predicate(item, arg))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///  Determines whether all elements in this builder satisfy a condition.
    /// </summary>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  <see langword="true"/> if every element in this builder passes the test
    ///  in the specified predicate, or if the builder is empty; otherwise,
    ///  <see langword="false"/>.</returns>
    public readonly bool All(Func<T, bool> predicate)
    {
        foreach (var item in this)
        {
            if (!predicate(item))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///  Determines whether all elements in this builder satisfy a condition.
    /// </summary>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  <see langword="true"/> if every element in this builder passes the test
    ///  in the specified predicate, or if the builder is empty; otherwise,
    ///  <see langword="false"/>.</returns>
    public readonly bool All<TArg>(TArg arg, Func<T, TArg, bool> predicate)
    {
        foreach (var item in this)
        {
            if (!predicate(item, arg))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Searches for the specified item.
    /// </summary>
    /// <param name="item">The item to search for.</param>
    /// <returns>The 0-based index where the item was found; or -1 if it could not be found.</returns>
    public readonly int IndexOf(T item)
        => IndexOf(item, EqualityComparer<T>.Default);

    /// <summary>
    /// Searches for the specified item.
    /// </summary>
    /// <param name="item">The item to search for.</param>
    /// <param name="comparer">The equality comparer to use in the search.</param>
    /// <returns>The 0-based index where the item was found; or -1 if it could not be found.</returns>
    public readonly int IndexOf(T item, IEqualityComparer<T> comparer)
    {
        var n = Count;
        for (var i = 0; i < n; i++)
        {
            if (comparer.Equals(this[i], item))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Determines whether the specified item exists in this collection.
    /// </summary>
    /// <param name="item">The item to search for.</param>
    /// <returns><c>true</c> if an equal value was found in this collection; <c>false</c> otherwise.</returns>
    public readonly bool Contains(T item)
        => Contains(item, EqualityComparer<T>.Default);

    /// <summary>
    /// Determines whether the specified item exists in this collection.
    /// </summary>
    /// <param name="item">The item to search for.</param>
    /// <param name="comparer">The equality comparer to use in the search.</param>
    /// <returns><c>true</c> if an equal value was found in this collection; <c>false</c> otherwise.</returns>
    public readonly bool Contains(T item, IEqualityComparer<T> comparer)
        => IndexOf(item, comparer) >= 0;

    /// <summary>
    ///  Returns the first element in this builder.
    /// </summary>
    /// <returns>
    ///  The first element in this builder.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  The builder is empty.
    /// </exception>
    public readonly T First()
        => Count > 0 ? this[0] : ThrowInvalidOperation(SR.Contains_no_elements);

    /// <summary>
    ///  Returns the first element in this builder that satisfies a specified condition.
    /// </summary>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  The first element in this builder that passes the test in the specified predicate function.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  No element satisfies the condition in <paramref name="predicate"/>.
    /// </exception>
    public readonly T First(Func<T, bool> predicate)
    {
        foreach (var item in this)
        {
            if (predicate(item))
            {
                return item;
            }
        }

        return ThrowInvalidOperation(SR.Contains_no_matching_elements);
    }

    /// <summary>
    ///  Returns the first element in this builder that satisfies a specified condition.
    /// </summary>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  The first element in this builder that passes the test in the specified predicate function.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  No element satisfies the condition in <paramref name="predicate"/>.
    /// </exception>
    public readonly T First<TArg>(TArg arg, Func<T, TArg, bool> predicate)
    {
        foreach (var item in this)
        {
            if (predicate(item, arg))
            {
                return item;
            }
        }

        return ThrowInvalidOperation(SR.Contains_no_matching_elements);
    }

    /// <summary>
    ///  Returns the first element in this builder, or a default value if the builder is empty.
    /// </summary>
    /// <returns>
    ///  <see langword="default"/>(<typeparamref name="T"/>) if this builder is empty; otherwise,
    ///  the first element in this builder.
    /// </returns>
    public readonly T? FirstOrDefault()
        => Count > 0 ? this[0] : default;

    /// <summary>
    ///  Returns the first element in this builder, or a specified default value if the builder is empty.
    /// </summary>
    /// <param name="defaultValue">
    ///  The default value to return if this builder is empty.
    /// </param>
    /// <returns>
    ///  <paramref name="defaultValue"/> if this builder is empty; otherwise,
    ///  the first element in this builder.
    /// </returns>
    public readonly T FirstOrDefault(T defaultValue)
        => Count > 0 ? this[0] : defaultValue;

    /// <summary>
    ///  Returns the first element in this builder that satisfies a condition, or a default value
    ///  if no such element is found.
    /// </summary>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  <see langword="default"/>(<typeparamref name="T"/>) if this builder is empty or if no element
    ///  passes the test specified by <paramref name="predicate"/>; otherwise, the first element in this
    ///  builder that passes the test specified by <paramref name="predicate"/>.
    /// </returns>
    public readonly T? FirstOrDefault(Func<T, bool> predicate)
    {
        foreach (var item in this)
        {
            if (predicate(item))
            {
                return item;
            }
        }

        return default;
    }

    /// <summary>
    ///  Returns the first element in this builder that satisfies a condition, or a specified default value
    ///  if no such element is found.
    /// </summary>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <param name="defaultValue">
    ///  The default value to return if this builder is empty.
    /// </param>
    /// <returns>
    ///  <paramref name="defaultValue"/> if this builder is empty or if no element
    ///  passes the test specified by <paramref name="predicate"/>; otherwise, the first element in this
    ///  builder that passes the test specified by <paramref name="predicate"/>.
    /// </returns>
    public readonly T FirstOrDefault(Func<T, bool> predicate, T defaultValue)
    {
        foreach (var item in this)
        {
            if (predicate(item))
            {
                return item;
            }
        }

        return defaultValue;
    }

    /// <summary>
    ///  Returns the first element in this builder that satisfies a condition, or a default value
    ///  if no such element is found.
    /// </summary>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  <see langword="default"/>(<typeparamref name="T"/>) if this builder is empty or if no element
    ///  passes the test specified by <paramref name="predicate"/>; otherwise, the first element in this
    ///  builder that passes the test specified by <paramref name="predicate"/>.
    /// </returns>
    public readonly T? FirstOrDefault<TArg>(TArg arg, Func<T, TArg, bool> predicate)
    {
        foreach (var item in this)
        {
            if (predicate(item, arg))
            {
                return item;
            }
        }

        return default;
    }

    /// <summary>
    ///  Returns the first element in this builder that satisfies a condition, or a default value
    ///  if no such element is found.
    /// </summary>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <param name="defaultValue">
    ///  The default value to return if this builder is empty.
    /// </param>
    /// <returns>
    ///  <paramref name="defaultValue"/> if this builder is empty or if no element
    ///  passes the test specified by <paramref name="predicate"/>; otherwise, the first element in this
    ///  builder that passes the test specified by <paramref name="predicate"/>.
    /// </returns>
    public readonly T FirstOrDefault<TArg>(TArg arg, Func<T, TArg, bool> predicate, T defaultValue)
    {
        foreach (var item in this)
        {
            if (predicate(item, arg))
            {
                return item;
            }
        }

        return defaultValue;
    }

    /// <summary>
    ///  Returns the last element in this builder.
    /// </summary>
    /// <returns>
    ///  The value at the last position in this builder.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  The builder is empty.
    /// </exception>
    public readonly T Last()
        => Count > 0 ? this[^1] : ThrowInvalidOperation(SR.Contains_no_elements);

    /// <summary>
    ///  Returns the last element in this builder that satisfies a specified condition.
    /// </summary>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  The last element in this builder that passes the test in the specified predicate function.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  No element satisfies the condition in <paramref name="predicate"/>.
    /// </exception>
    public readonly T Last(Func<T, bool> predicate)
    {
        for (var i = Count - 1; i >= 0; i--)
        {
            var item = this[i];
            if (predicate(item))
            {
                return item;
            }
        }

        return ThrowInvalidOperation(SR.Contains_no_matching_elements);
    }

    /// <summary>
    ///  Returns the last element in this builder that satisfies a specified condition.
    /// </summary>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  The last element in this builder that passes the test in the specified predicate function.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  No element satisfies the condition in <paramref name="predicate"/>.
    /// </exception>
    public readonly T Last<TArg>(TArg arg, Func<T, TArg, bool> predicate)
    {
        for (var i = Count - 1; i >= 0; i--)
        {
            var item = this[i];
            if (predicate(item, arg))
            {
                return item;
            }
        }

        return ThrowInvalidOperation(SR.Contains_no_matching_elements);
    }

    /// <summary>
    ///  Returns the last element in this builder, or a default value if the builder is empty.
    /// </summary>
    /// <returns>
    ///  <see langword="default"/>(<typeparamref name="T"/>) if this builder is empty; otherwise,
    ///  the last element in this builder.
    /// </returns>
    public readonly T? LastOrDefault()
        => Count > 0 ? this[^1] : default;

    /// <summary>
    ///  Returns the last element in this builder, or a specified default value if the builder is empty.
    /// </summary>
    /// <param name="defaultValue">
    ///  The default value to return if this builder is empty.
    /// </param>
    /// <returns>
    ///  <paramref name="defaultValue"/> if this builder is empty; otherwise,
    ///  the last element in this builder.
    /// </returns>
    public readonly T LastOrDefault(T defaultValue)
        => Count > 0 ? this[^1] : defaultValue;

    /// <summary>
    ///  Returns the last element in this builder that satisfies a condition, or a default value
    ///  if no such element is found.
    /// </summary>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  <see langword="default"/>(<typeparamref name="T"/>) if this builder is empty or if no element
    ///  passes the test specified by <paramref name="predicate"/>; otherwise, the last element in this
    ///  builder that passes the test specified by <paramref name="predicate"/>.
    /// </returns>
    public readonly T? LastOrDefault(Func<T, bool> predicate)
    {
        for (var i = Count - 1; i >= 0; i--)
        {
            var item = this[i];
            if (predicate(item))
            {
                return item;
            }
        }

        return default;
    }

    /// <summary>
    ///  Returns the last element in this builder that satisfies a condition, or a specified default value
    ///  if no such element is found.
    /// </summary>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <param name="defaultValue">
    ///  The default value to return if this builder is empty.
    /// </param>
    /// <returns>
    ///  <paramref name="defaultValue"/> if this builder is empty or if no element
    ///  passes the test specified by <paramref name="predicate"/>; otherwise, the last element in this
    ///  builder that passes the test specified by <paramref name="predicate"/>.
    /// </returns>
    public readonly T LastOrDefault(Func<T, bool> predicate, T defaultValue)
    {
        for (var i = Count - 1; i >= 0; i--)
        {
            var item = this[i];
            if (predicate(item))
            {
                return item;
            }
        }

        return defaultValue;
    }

    /// <summary>
    ///  Returns the last element in this builder that satisfies a condition, or a default value
    ///  if no such element is found.
    /// </summary>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  <see langword="default"/>(<typeparamref name="T"/>) if this builder is empty or if no element
    ///  passes the test specified by <paramref name="predicate"/>; otherwise, the last element in this
    ///  builder that passes the test specified by <paramref name="predicate"/>.
    /// </returns>
    public readonly T? LastOrDefault<TArg>(TArg arg, Func<T, TArg, bool> predicate)
    {
        for (var i = Count - 1; i >= 0; i--)
        {
            var item = this[i];
            if (predicate(item, arg))
            {
                return item;
            }
        }

        return default;
    }

    /// <summary>
    ///  Returns the last element in this builder that satisfies a condition, or a default value
    ///  if no such element is found.
    /// </summary>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <param name="defaultValue">
    ///  The default value to return if this builder is empty.
    /// </param>
    /// <returns>
    ///  <paramref name="defaultValue"/> if this builder is empty or if no element
    ///  passes the test specified by <paramref name="predicate"/>; otherwise, the last element in this
    ///  builder that passes the test specified by <paramref name="predicate"/>.
    /// </returns>
    public readonly T LastOrDefault<TArg>(TArg arg, Func<T, TArg, bool> predicate, T defaultValue)
    {
        for (var i = Count - 1; i >= 0; i--)
        {
            var item = this[i];
            if (predicate(item, arg))
            {
                return item;
            }
        }

        return defaultValue;
    }

    /// <summary>
    ///  Returns the only element in this builder, and throws an exception if there is not exactly one element.
    /// </summary>
    /// <returns>
    ///  The single element in this builder.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  The builder is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///  The builder contains more than one element.
    /// </exception>
    public readonly T Single()
    {
        return Count switch
        {
            1 => this[0],
            0 => ThrowInvalidOperation(SR.Contains_no_elements),
            _ => ThrowInvalidOperation(SR.Contains_more_than_one_element)
        };
    }

    /// <summary>
    ///  Returns the only element in this builder that satisfies a specified condition,
    ///  and throws an exception if more than one such element exists.
    /// </summary>
    /// <param name="predicate">
    ///  A function to test an element for a condition.
    /// </param>
    /// <returns>
    ///  The single element in this builder that satisfies a condition.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  No element satisfies the condition in <paramref name="predicate"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///  More than one element satisfies the condition in <paramref name="predicate"/>.
    /// </exception>
    public readonly T Single(Func<T, bool> predicate)
    {
        var firstSeen = false;
        T? result = default;

        foreach (var item in this)
        {
            if (predicate(item))
            {
                if (firstSeen)
                {
                    return ThrowInvalidOperation(SR.Contains_more_than_one_matching_element);
                }

                firstSeen = true;
                result = item;
            }
        }

        if (!firstSeen)
        {
            return ThrowInvalidOperation(SR.Contains_no_matching_elements);
        }

        return result!;
    }

    /// <summary>
    ///  Returns the only element in this builder that satisfies a specified condition,
    ///  and throws an exception if more than one such element exists.
    /// </summary>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test an element for a condition.
    /// </param>
    /// <returns>
    ///  The single element in this builder that satisfies a condition.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  No element satisfies the condition in <paramref name="predicate"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///  More than one element satisfies the condition in <paramref name="predicate"/>.
    /// </exception>
    public readonly T Single<TArg>(TArg arg, Func<T, TArg, bool> predicate)
    {
        var firstSeen = false;
        T? result = default;

        foreach (var item in this)
        {
            if (predicate(item, arg))
            {
                if (firstSeen)
                {
                    return ThrowInvalidOperation(SR.Contains_more_than_one_matching_element);
                }

                firstSeen = true;
                result = item;
            }
        }

        if (!firstSeen)
        {
            return ThrowInvalidOperation(SR.Contains_no_matching_elements);
        }

        return result!;
    }

    /// <summary>
    ///  Returns the only element in this builder, or a default value if the builder is empty;
    ///  this method throws an exception if there is more than one element in the builder.
    /// </summary>
    /// <returns>
    ///  The single element in this builder, or <see langword="default"/>(<typeparamref name="T"/>)
    ///  if this builder contains no elements.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  The builder contains more than one element.
    /// </exception>
    public readonly T? SingleOrDefault()
    {
        return Count switch
        {
            1 => this[0],
            0 => default,
            _ => ThrowInvalidOperation(SR.Contains_more_than_one_element)
        };
    }

    /// <summary>
    ///  Returns the only element in this builder, or a specified default value if the builder is empty;
    ///  this method throws an exception if there is more than one element in the builder.
    /// </summary>
    /// <param name="defaultValue">
    ///  The default value to return if this builder is empty.
    /// </param>
    /// <returns>
    ///  The single element in this builder, or <paramref name="defaultValue"/>
    ///  if this builder contains no elements.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  The builder contains more than one element.
    /// </exception>
    public readonly T SingleOrDefault(T defaultValue)
    {
        return Count switch
        {
            1 => this[0],
            0 => defaultValue,
            _ => ThrowInvalidOperation(SR.Contains_more_than_one_element)
        };
    }

    /// <summary>
    ///  Returns the only element in this builder that satisfies a specified condition or a default
    ///  value if no such element exists; this method throws an exception if more than one element
    ///  satisfies the condition.
    /// </summary>
    /// <param name="predicate">
    ///  A function to test an element for a condition.
    /// </param>
    /// <returns>
    ///  The single element in this builder that satisfies the condition, or
    ///  <see langword="default"/>(<typeparamref name="T"/>) if no such element is found.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  More than one element satisfies the condition in predicate.
    /// </exception>
    public readonly T? SingleOrDefault(Func<T, bool> predicate)
    {
        var firstSeen = false;
        T? result = default;

        foreach (var item in this)
        {
            if (predicate(item))
            {
                if (firstSeen)
                {
                    return ThrowInvalidOperation(SR.Contains_more_than_one_matching_element);
                }

                firstSeen = true;
                result = item;
            }
        }

        return result;
    }

    /// <summary>
    ///  Returns the only element in this builder that satisfies a specified condition or a specified default
    ///  value if no such element exists; this method throws an exception if more than one element
    ///  satisfies the condition.
    /// </summary>
    /// <param name="predicate">
    ///  A function to test an element for a condition.
    /// </param>
    /// <param name="defaultValue">
    ///  The default value to return if this builder is empty.
    /// </param>
    /// <returns>
    ///  The single element in this builder that satisfies the condition, or
    ///  <paramref name="defaultValue"/> if no such element is found.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  More than one element satisfies the condition in predicate.
    /// </exception>
    public readonly T SingleOrDefault(Func<T, bool> predicate, T defaultValue)
    {
        var firstSeen = false;
        var result = defaultValue;

        foreach (var item in this)
        {
            if (predicate(item))
            {
                if (firstSeen)
                {
                    return ThrowInvalidOperation(SR.Contains_more_than_one_matching_element);
                }

                firstSeen = true;
                result = item;
            }
        }

        return result;
    }

    /// <summary>
    ///  Returns the only element in this builder that satisfies a specified condition or a default
    ///  value if no such element exists; this method throws an exception if more than one element
    ///  satisfies the condition.
    /// </summary>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test an element for a condition.
    /// </param>
    /// <returns>
    ///  The single element in this builder that satisfies the condition, or
    ///  <see langword="default"/>(<typeparamref name="T"/>) if no such element is found.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  More than one element satisfies the condition in predicate.
    /// </exception>
    public readonly T? SingleOrDefault<TArg>(TArg arg, Func<T, TArg, bool> predicate)
    {
        var firstSeen = false;
        T? result = default;

        foreach (var item in this)
        {
            if (predicate(item, arg))
            {
                if (firstSeen)
                {
                    return ThrowInvalidOperation(SR.Contains_more_than_one_matching_element);
                }

                firstSeen = true;
                result = item;
            }
        }

        return result;
    }

    /// <summary>
    ///  Returns the only element in this builder that satisfies a specified condition or a specified default
    ///  value if no such element exists; this method throws an exception if more than one element
    ///  satisfies the condition.
    /// </summary>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test an element for a condition.
    /// </param>
    /// <param name="defaultValue">
    ///  The default value to return if this builder is empty.
    /// </param>
    /// <returns>
    ///  The single element in this builder that satisfies the condition, or
    ///  <paramref name="defaultValue"/> if no such element is found.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  More than one element satisfies the condition in predicate.
    /// </exception>
    public readonly T SingleOrDefault<TArg>(TArg arg, Func<T, TArg, bool> predicate, T defaultValue)
    {
        var firstSeen = false;
        var result = defaultValue;

        foreach (var item in this)
        {
            if (predicate(item, arg))
            {
                if (firstSeen)
                {
                    return ThrowInvalidOperation(SR.Contains_more_than_one_matching_element);
                }

                firstSeen = true;
                result = item;
            }
        }

        return result;
    }

    /// <summary>
    ///  This is present to help the JIT inline methods that need to throw an <see cref="IndexOutOfRangeException"/>.
    /// </summary>
    [DoesNotReturn]
    private static void ThrowIndexOutOfRangeException()
        => throw new IndexOutOfRangeException();

    /// <summary>
    ///  This is present to help the JIT inline methods that need to throw an <see cref="InvalidOperationException"/>.
    /// </summary>
    [DoesNotReturn]
    private static T ThrowInvalidOperation(string message)
        => ThrowHelper.ThrowInvalidOperationException<T>(message);

    [MemberNotNull(nameof(_builder))]
    private void MoveInlineItemsToBuilder()
    {
        Debug.Assert(_builder is null);

        _builderPool ??= ArrayBuilderPool<T>.Default;
        var builder = _builderPool.Get();

        if (_capacity is int capacity)
        {
            builder.Capacity = capacity;
        }
        else if (builder.Capacity < InlineCapacity)
        {
            builder.Capacity = InlineCapacity;
        }

        _builder = builder;

        // Add the inline items and clear their field values.
        for (var i = 0; i < _inlineCount; i++)
        {
            builder.Add(GetInlineElement(i));
            ClearInlineElement(i);
        }

        // Since _inlineCount tracks the number of inline items used, we zero it out here.
        _inlineCount = 0;
    }

    private void ShiftInlineItemsByOffset(int index, int offset)
    {
        Debug.Assert(_builder is null);
        Debug.Assert(index >= 0 && index < _inlineCount);
        Debug.Assert(offset > 0);
        Debug.Assert(offset + _inlineCount <= InlineCapacity);

        for (var i = _inlineCount - 1; i >= index; i--)
        {
            SetInlineElement(i + offset, GetInlineElement(i));
        }
    }

    /// <summary>
    ///  Sorts the contents of this builder.
    /// </summary>
    public void Sort()
    {
        var builder = GetBuilder();
        builder.Sort();
    }

    /// <summary>
    ///  Sorts the contents of this builder using the provided <see cref="IComparer{T}"/>.
    /// </summary>
    public void Sort(IComparer<T> comparer)
    {
        var builder = GetBuilder();
        builder.Sort(comparer);
    }

    /// <summary>
    ///  Sorts the contents of this builder using the provided <see cref="Comparison{T}"/>.
    /// </summary>
    public void Sort(Comparison<T> comparison)
    {
        var builder = GetBuilder();
        builder.Sort(comparison);
    }

    public readonly ImmutableArray<T> ToImmutableOrdered()
    {
        var result = ToImmutable();
        result.Unsafe().Order();

        return result;
    }

    public readonly ImmutableArray<T> ToImmutableOrdered(IComparer<T> comparer)
    {
        var result = ToImmutable();
        result.Unsafe().Order(comparer);

        return result;
    }

    public readonly ImmutableArray<T> ToImmutableOrdered(Comparison<T> comparison)
    {
        var result = ToImmutable();
        result.Unsafe().Order(comparison);

        return result;
    }

    public readonly ImmutableArray<T> ToImmutableOrderedDescending()
    {
        var result = ToImmutable();
        result.Unsafe().OrderDescending();

        return result;
    }

    public readonly ImmutableArray<T> ToImmutableOrderedDescending(IComparer<T> comparer)
    {
        var result = ToImmutable();
        result.Unsafe().OrderDescending(comparer);

        return result;
    }

    public readonly ImmutableArray<T> ToImmutableOrderedDescending(Comparison<T> comparison)
    {
        var result = ToImmutable();
        result.Unsafe().OrderDescending(comparison);

        return result;
    }

    public readonly ImmutableArray<T> ToImmutableOrderedBy<TKey>(Func<T, TKey> keySelector)
    {
        var result = ToImmutable();
        result.Unsafe().OrderBy(keySelector);

        return result;
    }

    public readonly ImmutableArray<T> ToImmutableOrderedBy<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer)
    {
        var result = ToImmutable();
        result.Unsafe().OrderBy(keySelector, comparer);

        return result;
    }

    public readonly ImmutableArray<T> ToImmutableOrderedBy<TKey>(Func<T, TKey> keySelector, Comparison<TKey> comparison)
    {
        var result = ToImmutable();
        result.Unsafe().OrderBy(keySelector, comparison);

        return result;
    }

    public readonly ImmutableArray<T> ToImmutableOrderedByDescending<TKey>(Func<T, TKey> keySelector)
    {
        var result = ToImmutable();
        result.Unsafe().OrderByDescending(keySelector);

        return result;
    }

    public readonly ImmutableArray<T> ToImmutableOrderedByDescending<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer)
    {
        var result = ToImmutable();
        result.Unsafe().OrderByDescending(keySelector, comparer);

        return result;
    }

    public readonly ImmutableArray<T> ToImmutableOrderedByDescending<TKey>(Func<T, TKey> keySelector, Comparison<TKey> comparison)
    {
        var result = ToImmutable();
        result.Unsafe().OrderByDescending(keySelector, comparison);

        return result;
    }

    public readonly ImmutableArray<T> ToImmutableReversed()
    {
        var result = ToImmutable();
        result.Unsafe().Reverse();

        return result;
    }

    public ImmutableArray<T> ToImmutableOrderedAndClear()
    {
        var result = ToImmutableAndClear();
        result.Unsafe().Order();

        return result;
    }

    public ImmutableArray<T> ToImmutableOrderedAndClear(IComparer<T> comparer)
    {
        var result = ToImmutableAndClear();
        result.Unsafe().Order(comparer);

        return result;
    }

    public ImmutableArray<T> ToImmutableOrderedAndClear(Comparison<T> comparison)
    {
        var result = ToImmutableAndClear();
        result.Unsafe().Order(comparison);

        return result;
    }

    public ImmutableArray<T> ToImmutableOrderedDescendingAndClear()
    {
        var result = ToImmutableAndClear();
        result.Unsafe().OrderDescending();

        return result;
    }

    public ImmutableArray<T> ToImmutableOrderedDescendingAndClear(IComparer<T> comparer)
    {
        var result = ToImmutableAndClear();
        result.Unsafe().OrderDescending(comparer);

        return result;
    }

    public ImmutableArray<T> ToImmutableOrderedDescendingAndClear(Comparison<T> comparison)
    {
        var result = ToImmutableAndClear();
        result.Unsafe().OrderDescending(comparison);

        return result;
    }

    public ImmutableArray<T> ToImmutableOrderedByAndClear<TKey>(Func<T, TKey> keySelector)
    {
        var result = ToImmutableAndClear();
        result.Unsafe().OrderBy(keySelector);

        return result;
    }

    public ImmutableArray<T> ToImmutableOrderedByAndClear<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer)
    {
        var result = ToImmutableAndClear();
        result.Unsafe().OrderBy(keySelector, comparer);

        return result;
    }

    public ImmutableArray<T> ToImmutableOrderedByAndClear<TKey>(Func<T, TKey> keySelector, Comparison<TKey> comparison)
    {
        var result = ToImmutableAndClear();
        result.Unsafe().OrderBy(keySelector, comparison);

        return result;
    }

    public ImmutableArray<T> ToImmutableOrderedByDescendingAndClear<TKey>(Func<T, TKey> keySelector)
    {
        var result = ToImmutableAndClear();
        result.Unsafe().OrderByDescending(keySelector);

        return result;
    }

    public ImmutableArray<T> ToImmutableOrderedByDescendingAndClear<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer)
    {
        var result = ToImmutableAndClear();
        result.Unsafe().OrderByDescending(keySelector, comparer);

        return result;
    }

    public ImmutableArray<T> ToImmutableOrderedByDescendingAndClear<TKey>(Func<T, TKey> keySelector, Comparison<TKey> comparison)
    {
        var result = ToImmutableAndClear();
        result.Unsafe().OrderByDescending(keySelector, comparison);

        return result;
    }

    public ImmutableArray<T> ToImmutableReversedAndClear()
    {
        var result = ToImmutableAndClear();
        result.Unsafe().Reverse();

        return result;
    }

    internal readonly TestAccessor GetTestAccessor() => new(in this);

    internal readonly struct TestAccessor(ref readonly PooledArrayBuilder<T> builder)
    {
        public ImmutableArray<T>.Builder? InnerArrayBuilder { get; } = builder._builder;
        public int? Capacity { get; } = builder._capacity;
        public int InlineItemCount { get; } = builder._inlineCount;
    }
}
