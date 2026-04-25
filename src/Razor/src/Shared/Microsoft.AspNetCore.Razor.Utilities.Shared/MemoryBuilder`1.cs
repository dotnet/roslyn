// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// Inspired by https://github.com/dotnet/runtime/blob/9c7ee976fd771c183e98cf629e3776bba4e45ccc/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/ValueListBuilder.cs

namespace Microsoft.AspNetCore.Razor;

/// <summary>
///  Temporary builder that uses <see cref="ArrayPool{T}"/> to back a <see cref="Memory{T}"/>.
/// </summary>
internal ref struct MemoryBuilder<T>
{
    private Memory<T> _memory;
    private T[]? _arrayFromPool;
    private int _length;
    private bool _clearArray;

    public MemoryBuilder(int initialCapacity = 0, bool clearArray = false)
    {
        ArgHelper.ThrowIfNegative(initialCapacity);

        if (initialCapacity > 0)
        {
            _arrayFromPool = ArrayPool<T>.Shared.Rent(initialCapacity);
            _memory = _arrayFromPool;
        }

        _clearArray = clearArray;
    }

    public void Dispose()
    {
        var toReturn = _arrayFromPool;
        if (toReturn is not null)
        {
            ArrayPool<T>.Shared.Return(toReturn, _clearArray);

            _memory = default;
            _arrayFromPool = null;
            _length = 0;
            _clearArray = false;
        }
    }

    public readonly bool IsEmpty => _length == 0;

    public int Length
    {
        readonly get => _length;
        set
        {
            Debug.Assert(value >= 0);
            Debug.Assert(value <= _memory.Length);

            _length = value;
        }
    }

    public readonly ref T this[int index]
    {
        get
        {
            Debug.Assert(index >= 0 && index < _length);

            return ref _memory.Span[index];
        }
    }

    public readonly Memory<T> AsMemory()
        => _memory[.._length];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(T item)
    {
        var index = _length;
        var memory = _memory;

        if ((uint)index < (uint)memory.Length)
        {
            memory.Span[index] = item;
            _length = index + 1;
        }
        else
        {
            AppendWithResize(item);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(ReadOnlySpan<T> source)
    {
        var index = _length;
        var memory = _memory;

        if (source.Length == 1 && (uint)index < (uint)memory.Length)
        {
            memory.Span[index] = source[0];
            _length = index + 1;
        }
        else
        {
            AppendWithResize(source);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AppendWithResize(T item)
    {
        Debug.Assert(_length == _memory.Length);
        var index = _length;
        Grow(1);
        _memory.Span[index] = item;
        _length = index + 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AppendWithResize(ReadOnlySpan<T> source)
    {
        if ((uint)(_length + source.Length) > (uint)_memory.Length)
        {
            Grow(_memory.Length - _length + source.Length);
        }

        source.CopyTo(_memory.Span[_length..]);
        _length += source.Length;
    }

    private void Grow(int additionalCapacityRequired = 1)
    {
        Debug.Assert(additionalCapacityRequired > 0);

        const int ArrayMaxLength = 0x7FFFFFC7; // same as Array.MaxLength

        // Double the size of the array. If it's currently empty, default to size 4.
        var nextCapacity = Math.Max(
            _memory.Length != 0 ? _memory.Length * 2 : 4,
            _memory.Length + additionalCapacityRequired);

        // If nextCapacity exceeds the possible length of an array, then we want to downgrade to
        // either ArrayMaxLength, if that's large enough to hold an additional item, or
        // _memory.Length + 1, if that's larger than ArrayMaxLength. Essentially, we don't want
        // to simply clamp to ArrayMaxLength if that isn't actually large enough. Instead, if
        // we've grown too large, we want to OOM when Rent is called below.
        if ((uint)nextCapacity > ArrayMaxLength)
        {
            // Note: it's not possible for _memory.Length + 1 to overflow because that would mean
            // _memory is pointing to an array with length int.MaxValue, which is larger than
            // Array.MaxLength. We would have OOM'd before getting here.

            nextCapacity = Math.Max(_memory.Length + 1, ArrayMaxLength);
        }

        Debug.Assert(nextCapacity > _memory.Length);

        var newArray = ArrayPool<T>.Shared.Rent(nextCapacity);
        _memory.Span.CopyTo(newArray);

        var toReturn = _arrayFromPool;
        _memory = newArray;
        _arrayFromPool = newArray;

        if (toReturn != null)
        {
            ArrayPool<T>.Shared.Return(toReturn, _clearArray);
        }
    }

    public void Push(T item)
    {
        Append(item);
    }

    public readonly T Peek()
    {
        return this[^1];
    }

    public T Pop()
    {
        var item = this[^1];
        _length--;

        return item;
    }

    public bool TryPop([MaybeNullWhen(false)] out T result)
    {
        if (IsEmpty)
        {
            result = default;
            return false;
        }

        result = Pop();
        return true;
    }
}

/// <summary>
///  Encapsulates a method that operates on a <see cref="MemoryBuilder{T}"/> and an argument, typically for building content.
/// </summary>
/// <typeparam name="T">
///  The type of elements in the memory builder.
/// </typeparam>
/// <typeparam name="TArg">
///  The type of the argument passed to the delegate.
/// </typeparam>
/// <param name="builder">
///  A reference to the memory builder to operate on.
/// </param>
/// <param name="arg">
///  The argument to pass to the delegate.
/// </param>
/// <returns>
///  A string result from the operation.
/// </returns>
internal delegate void MemoryBuilderAction<T, in TArg>(ref MemoryBuilder<T> builder, TArg arg);

/// <summary>
///  Encapsulates a method that operates on a <see cref="MemoryBuilder{T}"/> and an argument, returning a result of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="T">
///  The type of elements in the memory builder.
/// </typeparam>
/// <typeparam name="TArg">
///  The type of the argument passed to the delegate.
/// </typeparam>
/// <typeparam name="TResult">
///  The type of the result returned by the delegate.
/// </typeparam>
/// <param name="builder">
///  A reference to the memory builder to operate on.
/// </param>
/// <param name="arg">
///  The argument to pass to the delegate.
/// </param>
/// <returns>
///  A result of type <typeparamref name="TResult"/> from the operation.
/// </returns>
internal delegate TResult MemoryBuilderFunc<T, in TArg, out TResult>(ref MemoryBuilder<T> builder, TArg arg);
