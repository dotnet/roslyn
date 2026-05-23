// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copied from https://github.com/dotnet/runtime

#if !NETCOREAPP3_0_OR_GREATER

using System.Diagnostics;

namespace System.Buffers;

/// <summary>
/// Represents a heap-based, array-backed output sink into which <typeparam name="T"/> data can be written.
/// </summary>
internal sealed class ArrayBufferWriter<T> : IBufferWriter<T>
{
    // Copy of Array.MaxLength.
    // Used by projects targeting .NET Framework.
    private const int ArrayMaxLength = 0x7FFFFFC7;

    private const int DefaultInitialBufferSize = 256;

    private T[] _buffer;
    private int _index;

    /// <summary>
    /// Creates an instance of an <see cref="ArrayBufferWriter{T}"/>, in which data can be written to,
    /// with the default initial capacity.
    /// </summary>
    public ArrayBufferWriter()
    {
        _buffer = Array.Empty<T>();
        _index = 0;
    }

    /// <summary>
    /// Creates an instance of an <see cref="ArrayBufferWriter{T}"/>, in which data can be written to,
    /// with an initial capacity specified.
    /// </summary>
    /// <param name="initialCapacity">The minimum capacity with which to initialize the underlying buffer.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="initialCapacity"/> is not positive (i.e. less than or equal to 0).
    /// </exception>
    public ArrayBufferWriter(int initialCapacity)
    {
        if (initialCapacity <= 0)
            throw new ArgumentException(null, nameof(initialCapacity));

        _buffer = new T[initialCapacity];
        _index = 0;
    }

    /// <summary>
    /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public ReadOnlyMemory<T> WrittenMemory => _buffer.AsMemory(0, _index);

    /// <summary>
    /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<T> WrittenSpan => _buffer.AsSpan(0, _index);

    /// <summary>
    /// Returns the amount of data written to the underlying buffer so far.
    /// </summary>
    public int WrittenCount => _index;

    /// <summary>
    /// Returns the total amount of space within the underlying buffer.
    /// </summary>
    public int Capacity => _buffer.Length;

    /// <summary>
    /// Returns the amount of space available that can still be written into without forcing the underlying buffer to grow.
    /// </summary>
    public int FreeCapacity => _buffer.Length - _index;

    /// <summary>
    /// Clears the data written to the underlying buffer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You must reset or clear the <see cref="ArrayBufferWriter{T}"/> before trying to re-use it.
    /// </para>
    /// <para>
    /// The <see cref="ResetWrittenCount"/> method is faster since it only sets to zero the writer's index
    /// while the <see cref="Clear"/> method additionally zeroes the content of the underlying buffer.
    /// </para>
    /// </remarks>
    /// <seealso cref="ResetWrittenCount"/>
    public void Clear()
    {
        Debug.Assert(_buffer.Length >= _index);
        _buffer.AsSpan(0, _index).Clear();
        _index = 0;
    }

    /// <summary>
    /// Resets the data written to the underlying buffer without zeroing its content.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You must reset or clear the <see cref="ArrayBufferWriter{T}"/> before trying to re-use it.
    /// </para>
    /// <para>
    /// If you reset the writer using the <see cref="ResetWrittenCount"/> method, the underlying buffer will not be cleared.
    /// </para>
    /// </remarks>
    /// <seealso cref="Clear"/>
    public void ResetWrittenCount() => _index = 0;

    /// <summary>
    /// Notifies <see cref="IBufferWriter{T}"/> that <paramref name="count"/> amount of data was written to the output <see cref="Span{T}"/>/<see cref="Memory{T}"/>
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="count"/> is negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to advance past the end of the underlying buffer.
    /// </exception>
    /// <remarks>
    /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
    /// </remarks>
    public void Advance(int count)
    {
        if (count < 0)
            throw new ArgumentException(null, nameof(count));

        if (_index > _buffer.Length - count)
            ThrowInvalidOperationException_AdvancedTooFar(_buffer.Length);

        _index += count;
    }

    /// <summary>
    /// Returns a <see cref="Memory{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
    /// If no <paramref name="sizeHint"/> is provided (or it's equal to <code>0</code>), some non-empty buffer is returned.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sizeHint"/> is negative.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This will never return an empty <see cref="Memory{T}"/>.
    /// </para>
    /// <para>
    /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
    /// </para>
    /// <para>
    /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
    /// </para>
    /// <para>
    /// If you reset the writer using the <see cref="ResetWrittenCount"/> method, this method may return a non-cleared <see cref="Memory{T}"/>.
    /// </para>
    /// <para>
    /// If you clear the writer using the <see cref="Clear"/> method, this method will return a <see cref="Memory{T}"/> with its content zeroed.
    /// </para>
    /// </remarks>
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        Debug.Assert(_buffer.Length > _index);
        return _buffer.AsMemory(_index);
    }

    /// <summary>
    /// Returns a <see cref="Span{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
    /// If no <paramref name="sizeHint"/> is provided (or it's equal to <code>0</code>), some non-empty buffer is returned.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sizeHint"/> is negative.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This will never return an empty <see cref="Span{T}"/>.
    /// </para>
    /// <para>
    /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
    /// </para>
    /// <para>
    /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
    /// </para>
    /// <para>
    /// If you reset the writer using the <see cref="ResetWrittenCount"/> method, this method may return a non-cleared <see cref="Span{T}"/>.
    /// </para>
    /// <para>
    /// If you clear the writer using the <see cref="Clear"/> method, this method will return a <see cref="Span{T}"/> with its content zeroed.
    /// </para>
    /// </remarks>
    public Span<T> GetSpan(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        Debug.Assert(_buffer.Length > _index);
        return _buffer.AsSpan(_index);
    }

    private void CheckAndResizeBuffer(int sizeHint)
    {
        if (sizeHint < 0)
            throw new ArgumentException(nameof(sizeHint));

        if (sizeHint == 0)
        {
            sizeHint = 1;
        }

        if (sizeHint > FreeCapacity)
        {
            var currentLength = _buffer.Length;

            // Attempt to grow by the larger of the sizeHint and double the current size.
            var growBy = Math.Max(sizeHint, currentLength);

            if (currentLength == 0)
            {
                growBy = Math.Max(growBy, DefaultInitialBufferSize);
            }

            var newSize = currentLength + growBy;

            if ((uint)newSize > int.MaxValue)
            {
                // Attempt to grow to ArrayMaxLength.
                var needed = (uint)(currentLength - FreeCapacity + sizeHint);
                Debug.Assert(needed > currentLength);

                if (needed > ArrayMaxLength)
                {
                    ThrowOutOfMemoryException(needed);
                }

                newSize = ArrayMaxLength;
            }

            Array.Resize(ref _buffer, newSize);
        }

        Debug.Assert(FreeCapacity > 0 && FreeCapacity >= sizeHint);
    }

    private static void ThrowInvalidOperationException_AdvancedTooFar(int capacity)
    {
        throw new InvalidOperationException(SR.FormatCannot_advance_past_end_of_the_buffer_which_has_a_size_of_0(capacity));
    }

    private static void ThrowOutOfMemoryException(uint capacity)
    {
        throw new OutOfMemoryException(SR.FormatCannot_allocate_a_buffer_of_size_0(capacity));
    }
}
#endif
