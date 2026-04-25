// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copied from https://github.com/dotnet/aspnetcore/blob/207bc7d00c00bf454dc83789d6824609b1d8b02a/src/Shared/PooledArrayBufferWriter.cs,
// which is derived from https://github.com/dotnet/runtime/blob/abb5c348f242e546b5768ad70ebe4051f08f7d24/src/libraries/Common/src/System/Text/Json/PooledByteBufferWriter.cs

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor;

namespace System.Buffers;

/// <summary>
///  <see cref="IBufferWriter{T}"/> that uses <see cref="ArrayPool{T}"/>.
/// </summary>
internal sealed class PooledArrayBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    private T[]? _rentedBuffer;
    private int _index;

    private const int MinimumBufferSize = 256;

    public PooledArrayBufferWriter()
    {
        _rentedBuffer = ArrayPool<T>.Shared.Rent(MinimumBufferSize);
        _index = 0;
    }

    public PooledArrayBufferWriter(int initialCapacity)
    {
        ArgHelper.ThrowIfNegativeOrZero(initialCapacity);

        _rentedBuffer = ArrayPool<T>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    public ReadOnlyMemory<T> WrittenMemory
    {
        get
        {
            CheckIfDisposed();

            return _rentedBuffer.AsMemory(0, _index);
        }
    }

    public int WrittenCount
    {
        get
        {
            CheckIfDisposed();

            return _index;
        }
    }

    public int Capacity
    {
        get
        {
            CheckIfDisposed();

            return _rentedBuffer.Length;
        }
    }

    public int FreeCapacity
    {
        get
        {
            CheckIfDisposed();

            return _rentedBuffer.Length - _index;
        }
    }

    public void Clear()
    {
        CheckIfDisposed();

        ClearHelper();
    }

    private void ClearHelper()
    {
        Debug.Assert(_rentedBuffer != null);

        _rentedBuffer.AsSpan(0, _index).Clear();
        _index = 0;
    }

    // Returns the rented buffer back to the pool
    public void Dispose()
    {
        if (_rentedBuffer == null)
        {
            return;
        }

        ClearHelper();
        ArrayPool<T>.Shared.Return(_rentedBuffer);
        _rentedBuffer = null;
    }

    [MemberNotNull(nameof(_rentedBuffer))]
    private void CheckIfDisposed()
    {
        if (_rentedBuffer == null)
        {
            ThrowObjectDisposed();
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ThrowObjectDisposed()
        {
            throw new ObjectDisposedException(nameof(ArrayBufferWriter<T>));
        }
    }

    public void Advance(int count)
    {
        CheckIfDisposed();

        ArgHelper.ThrowIfNegative(count);

        if (_index > _rentedBuffer.Length - count)
        {
            ThrowCannotAdvance(_rentedBuffer.Length);
        }

        _index += count;

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ThrowCannotAdvance(int capacity)
        {
            throw new InvalidOperationException(SR.FormatCannot_advance_past_end_of_the_buffer_which_has_a_size_of_0(capacity));
        }
    }

    public Memory<T> GetMemory(int sizeHint = 0)
    {
        CheckIfDisposed();

        CheckAndResizeBuffer(sizeHint);
        return _rentedBuffer.AsMemory(_index);
    }

    public Span<T> GetSpan(int sizeHint = 0)
    {
        CheckIfDisposed();

        CheckAndResizeBuffer(sizeHint);
        return _rentedBuffer.AsSpan(_index);
    }

    private void CheckAndResizeBuffer(int sizeHint)
    {
        Debug.Assert(_rentedBuffer != null);

        ArgHelper.ThrowIfNegative(sizeHint);

        if (sizeHint == 0)
        {
            sizeHint = MinimumBufferSize;
        }

        var availableSpace = _rentedBuffer!.Length - _index;

        if (sizeHint > availableSpace)
        {
            var growBy = Math.Max(sizeHint, _rentedBuffer.Length);

            var newSize = checked(_rentedBuffer.Length + growBy);

            var oldBuffer = _rentedBuffer;

            _rentedBuffer = ArrayPool<T>.Shared.Rent(newSize);

            Debug.Assert(oldBuffer.Length >= _index);
            Debug.Assert(_rentedBuffer.Length >= _index);

            var previousBuffer = oldBuffer.AsSpan(0, _index);
            previousBuffer.CopyTo(_rentedBuffer);
            previousBuffer.Clear();
            ArrayPool<T>.Shared.Return(oldBuffer);
        }

        Debug.Assert(_rentedBuffer.Length - _index > 0);
        Debug.Assert(_rentedBuffer.Length - _index >= sizeHint);
    }
}
