// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal unsafe partial class BlobBuilder
    {
        // The implementation is akin to StringBuilder. 
        // The differences:
        // - BlobBuilder allows efficient sequential write of the built content to a stream. 
        // - BlobBuilder allows for chunk allocation customization. A custom allocator can use pooling strategy, for example.

        internal const int DefaultChunkSize = 256;

        // Must be at least the size of the largest primitive type we write atomically (decimal).
        internal const int MinChunkSize = 16;

        // Builders are linked like so:
        // 
        // [1:first]->[2]->[3:last]<-[4:head]
        //     ^_______________|
        //
        // In this case the content represented is a sequence (1,2,3,4).
        // This structure optimizes for append write operations and sequential enumeration from the start of the chain.
        // Data can only be written to the head node. Other nodes are "frozen".
        private BlobBuilder _nextOrPrevious;
        private BlobBuilder FirstChunk => _nextOrPrevious._nextOrPrevious;

        // The sum of lengths of all preceding chunks (not including the current chunk).
        private int _previousLength;

        private byte[] _buffer;

        // The length of data in the buffer in lower 31 bits.
        // Head: highest bit is 0, length may be 0.
        // Non-head: highest bit is 1, lower 31 bits are not all 0.
        private uint _length;

        private const uint IsFrozenMask = 0x80000000;
        private bool IsHead => (_length & IsFrozenMask) == 0;
        private int Length => (int)(_length & ~IsFrozenMask);
        private uint FrozenLength => _length | IsFrozenMask;

        public BlobBuilder(int size = DefaultChunkSize)
        {
            if (size < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            // the writer assumes little-endian architecture:
            if (!BitConverter.IsLittleEndian)
            {
                throw new PlatformNotSupportedException();
            }

            _nextOrPrevious = this;
            _buffer = new byte[Math.Max(MinChunkSize, size)];
        }

        protected virtual BlobBuilder AllocateChunk(int minimalSize)
        {
            return new BlobBuilder(Math.Max(_buffer.Length, minimalSize));
        }

        protected virtual void FreeChunk()
        {
            // nop
        }

        public void Clear()
        {
            if (!IsHead)
            {
                ThrowHeadRequired();
            }

            // Swap buffer with the first chunk.
            // Note that we need to keep holding on all allocated buffers,
            // so that builders with custom allocator can release them.
            var first = FirstChunk;
            if (first != this)
            {
                var firstBuffer = first._buffer;
                first._length = FrozenLength;
                first._buffer = _buffer;
                _buffer = firstBuffer;
            }

            // free all chunks except for the current one
            foreach (BlobBuilder chunk in GetChunks())
            {
                if (chunk != this)
                {
                    chunk.ClearChunk();
                    chunk.FreeChunk();
                }
            }

            ClearChunk();
        }

        protected void Free()
        {
            Clear();
            FreeChunk();
        }

        // internal for testing
        internal void ClearChunk()
        {
            _length = 0;
            _previousLength = 0;
            _nextOrPrevious = this;
        }

        private void CheckInvariants()
        {
#if DEBUG
            Debug.Assert(_buffer != null);
            Debug.Assert(Length >= 0 && Length <= _buffer.Length);
            Debug.Assert(_previousLength >= 0);
            Debug.Assert(_nextOrPrevious != null);

            if (IsHead)
            {
                // last chunk:
                int totalLength = 0;
                foreach (var chunk in GetChunks())
                {
                    Debug.Assert(chunk.IsHead || chunk.Length > 0);
                    totalLength += chunk.Length;
                }

                Debug.Assert(totalLength == Count);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowHeadRequired()
        {
            // TODO: message
            throw new InvalidOperationException();
        }

        public int Count => _previousLength + Length;

        // TODO: remove
        internal int Position => Count;

        private int FreeBytes => _buffer.Length - Length;

        // internal for testing
        internal int BufferSize => _buffer.Length;

        // internal for testing
        internal Chunks GetChunks()
        {
            if (!IsHead)
            {
                ThrowHeadRequired();
            }

            return new Chunks(this);
        }

        /// <summary>
        /// Returns a sequence of all blobs that represent the content of the builder.
        /// </summary>
        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public Blobs GetBlobs()
        {
            if (!IsHead)
            {
                ThrowHeadRequired();
            }

            return new Blobs(this);
        }

        /// <summary>
        /// Compares the current content of this writer with another one.
        /// </summary>
        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public bool ContentEquals(BlobBuilder other)
        {
            if (!IsHead)
            {
                ThrowHeadRequired();
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other == null)
            {
                return false;
            }

            if (!other.IsHead)
            {
                ThrowHeadRequired();
            }

            if (Count != other.Count)
            {
                return false;
            }

            var leftEnumerator = GetChunks();
            var rightEnumerator = other.GetChunks();
            int leftStart = 0;
            int rightStart = 0;

            bool leftContinues = leftEnumerator.MoveNext();
            bool rightContinues = rightEnumerator.MoveNext();

            while (leftContinues && rightContinues)
            {
                Debug.Assert(leftStart == 0 || rightStart == 0);

                var left = leftEnumerator.Current;
                var right = rightEnumerator.Current;

                int minLength = Math.Min(left.Length - leftStart, right.Length - rightStart);
                if (!ByteSequenceComparer.Equals(left._buffer, leftStart, right._buffer, rightStart, minLength))
                {
                    return false;
                }

                leftStart += minLength;
                rightStart += minLength;

                // nothing remains in left chunk to compare:
                if (leftStart == left.Length)
                {
                    leftContinues = leftEnumerator.MoveNext();
                    leftStart = 0;
                }

                // nothing remains in left chunk to compare:
                if (rightStart == right.Length)
                {
                    rightContinues = rightEnumerator.MoveNext();
                    rightStart = 0;
                }
            }

            return leftContinues == rightContinues;
        }

        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public byte[] ToArray()
        {
            return ToArray(0, Count);
        }

        /// <exception cref="ArgumentOutOfRangeException">Range specified by <paramref name="start"/> and <paramref name="byteCount"/> falls outside of the bounds of the buffer content.</exception>
        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public byte[] ToArray(int start, int byteCount)
        {
            BlobUtilities.ValidateRange(Count, start, byteCount);

            var result = new byte[byteCount];

            int chunkStartPosition = 0;
            int resultOffset = 0;
            foreach (var chunk in GetChunks())
            {
                int chunkEndPosition = chunkStartPosition + chunk.Length;

                if (chunkEndPosition > start)
                {
                    int bytesToCopy = Math.Min(chunk.Length, result.Length - resultOffset);
                    if (bytesToCopy == 0)
                    {
                        break;
                    }

                    Array.Copy(chunk._buffer, Math.Max(start - chunkStartPosition, 0), result, resultOffset, bytesToCopy);

                    resultOffset += bytesToCopy;
                }

                chunkStartPosition = chunkEndPosition;
            }

            Debug.Assert(resultOffset == result.Length);
            return result;
        }

        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public ImmutableArray<byte> ToImmutableArray()
        {
            return ToImmutableArray(0, Count);
        }

        /// <exception cref="ArgumentOutOfRangeException">Range specified by <paramref name="start"/> and <paramref name="byteCount"/> falls outside of the bounds of the buffer content.</exception>
        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public ImmutableArray<byte> ToImmutableArray(int start, int byteCount)
        {
            var array = ToArray(start, byteCount);
            return ImmutableArrayInterop.DangerousToImmutableArray(ref array);
        }

        /// <exception cref="ArgumentNullException"><paramref name="destination"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public void WriteContentTo(Stream destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            foreach (var chunk in GetChunks())
            {
                destination.Write(chunk._buffer, 0, chunk.Length);
            }
        }

        /// <exception cref="ArgumentNullException"><paramref name="destination"/> is default(<see cref="BlobWriter"/>).</exception>
        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public void WriteContentTo(ref BlobWriter destination)
        {
            if (destination.IsDefault)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            foreach (var chunk in GetChunks())
            {
                destination.WriteBytes(chunk._buffer, 0, chunk.Length);
            }
        }

        /// <exception cref="ArgumentNullException"><paramref name="destination"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Content is not available, the builder has been linked with another one.</exception>
        public void WriteContentTo(BlobBuilder destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            foreach (var chunk in GetChunks())
            {
                destination.WriteBytes(chunk._buffer, 0, chunk.Length);
            }
        }

        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void LinkPrefix(BlobBuilder prefix)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            // TODO: consider copying data from right to left while there is space

            if (!prefix.IsHead || !IsHead)
            {
                ThrowHeadRequired();
            }

            // avoid chaining empty chunks:
            if (prefix.Count == 0)
            {
                return;
            }

            _previousLength += prefix.Count;

            // prefix is not a head anymore:
            prefix._length = prefix.FrozenLength;

            // First and last chunks:
            //
            // [PrefixFirst]->[]->[PrefixLast] <- [prefix]    [First]->[]->[Last] <- [this]    
            //       ^_________________|                          ^___________|                 
            //
            // Degenerate cases:
            // this == First == Last and/or prefix == PrefixFirst == PrefixLast.
            var first = FirstChunk;
            var prefixFirst = prefix.FirstChunk;
            var last = _nextOrPrevious;
            var prefixLast = prefix._nextOrPrevious;

            // Relink like so:
            // [PrefixFirst]->[]->[PrefixLast] -> [prefix] -> [First]->[]->[Last] <- [this] 
            //      ^________________________________________________________|

            _nextOrPrevious = (last != this) ? last : prefix;
            prefix._nextOrPrevious = (first != this) ? first : (prefixFirst != prefix) ? prefixFirst : prefix;

            if (last != this)
            {
                last._nextOrPrevious = (prefixFirst != prefix) ? prefixFirst : prefix;
            }

            if (prefixLast != prefix)
            {
                prefixLast._nextOrPrevious = prefix;
            }

            prefix.CheckInvariants();
            CheckInvariants();
        }

        /// <exception cref="ArgumentNullException"><paramref name="suffix"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void LinkSuffix(BlobBuilder suffix)
        {
            if (suffix == null)
            {
                throw new ArgumentNullException(nameof(suffix));
            }

            // TODO: consider copying data from right to left while there is space

            if (!IsHead || !suffix.IsHead)
            {
                ThrowHeadRequired();
            }

            // avoid chaining empty chunks:
            if (suffix.Count == 0)
            {
                return;
            }

            // swap buffers of the heads:
            var suffixBuffer = suffix._buffer;
            uint suffixLength = suffix._length;
            suffix._buffer = _buffer;
            suffix._length = FrozenLength; // suffix is not a head anymore
            _buffer = suffixBuffer;
            _length = suffixLength;

            int suffixPreviousLength = suffix._previousLength;
            suffix._previousLength = _previousLength;
            _previousLength = _previousLength + suffix.Length + suffixPreviousLength;

            // First and last chunks:
            //
            // [First]->[]->[Last] <- [this]    [SuffixFirst]->[]->[SuffixLast]  <- [suffix]
            //    ^___________|                       ^_________________|
            //
            // Degenerate cases:
            // this == First == Last and/or suffix == SuffixFirst == SuffixLast.
            var first = FirstChunk;
            var suffixFirst = suffix.FirstChunk;
            var last = _nextOrPrevious;
            var suffixLast = suffix._nextOrPrevious;

            // Relink like so:
            // [First]->[]->[Last] -> [suffix] -> [SuffixFirst]->[]->[SuffixLast]  <- [this]
            //    ^_______________________________________________________|
            _nextOrPrevious = suffixLast;
            suffix._nextOrPrevious = (suffixFirst != suffix) ? suffixFirst : (first != this) ? first : suffix;

            if (last != this)
            {
                last._nextOrPrevious = suffix;
            }

            if (suffixLast != suffix)
            {
                suffixLast._nextOrPrevious = (first != this) ? first : suffix;
            }

            CheckInvariants();
            suffix.CheckInvariants();
        }

        private void AddLength(int value)
        {
            _length += (uint)value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Expand(int newLength)
        {
            // TODO: consider converting the last chunk to a smaller one if there is too much empty space left

            // May happen only if the derived class attempts to write to a builder that is not last,
            // or if a builder prepended to another one is not discarded.
            if (!IsHead)
            {
                ThrowHeadRequired();
            }

            var newChunk = AllocateChunk(Math.Max(newLength, MinChunkSize));
            if (newChunk.BufferSize < newLength)
            {
                // TODO: message
                throw new InvalidOperationException();
            }

            var newBuffer = newChunk._buffer;

            if (_length == 0)
            {
                // If the first write into an empty buffer needs more space than the buffer provides, swap the buffers.
                newChunk._buffer = _buffer;
                _buffer = newBuffer;
            }
            else
            {
                // Otherwise append the new buffer.
                var last = _nextOrPrevious;
                var first = FirstChunk;

                if (last == this)
                {
                    // single chunk in the chain
                    _nextOrPrevious = newChunk;
                }
                else
                {
                    newChunk._nextOrPrevious = first;
                    last._nextOrPrevious = newChunk;
                    _nextOrPrevious = newChunk;
                }

                newChunk._buffer = _buffer;
                newChunk._length = FrozenLength;
                newChunk._previousLength = _previousLength;

                _buffer = newBuffer;
                _previousLength += Length;
                _length = 0;
            }

            CheckInvariants();
        }

        /// <summary>
        /// Reserves a contiguous block of bytes.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="byteCount"/> is negative.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public BlobWriter ReserveBytes(int byteCount)
        {
            if (byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }

            int start = ReserveBytesImpl(byteCount);
            return new BlobWriter(_buffer, start, byteCount);
        }

        private int ReserveBytesImpl(int byteCount)
        {
            Debug.Assert(byteCount >= 0);

            // If write is attempted to a frozen builder we fall back 
            // to expand where an exception is thrown:
            uint result = _length;
            if (result > _buffer.Length - byteCount)
            {
                Expand(byteCount);
                result = 0;
            }

            _length = result + (uint)byteCount;
            return (int)result;
        }

        private int ReserveBytesPrimitive(int byteCount)
        {
            Debug.Assert(byteCount < MinChunkSize);
            return ReserveBytesImpl(byteCount);
        }

        /// <exception cref="ArgumentOutOfRangeException"><paramref name="byteCount"/> is negative.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteBytes(byte value, int byteCount)
        {
            if (byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }

            if (!IsHead)
            {
                ThrowHeadRequired();
            }

            int bytesToCurrent = Math.Min(FreeBytes, byteCount);

            _buffer.WriteBytes(Length, value, bytesToCurrent);
            AddLength(bytesToCurrent);

            int remaining = byteCount - bytesToCurrent;
            if (remaining > 0)
            {
                Expand(remaining);

                _buffer.WriteBytes(0, value, remaining);
                AddLength(remaining);
            }
        }

        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="byteCount"/> is negative.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public unsafe void WriteBytes(byte* buffer, int byteCount)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }

            if (!IsHead)
            {
                ThrowHeadRequired();
            }

            WriteBytesUnchecked(buffer, byteCount);
        }

        private unsafe void WriteBytesUnchecked(byte* buffer, int byteCount)
        {
            int bytesToCurrent = Math.Min(FreeBytes, byteCount);

            Marshal.Copy((IntPtr)buffer, _buffer, Length, bytesToCurrent);

            AddLength(bytesToCurrent);

            int remaining = byteCount - bytesToCurrent;
            if (remaining > 0)
            {
                Expand(remaining);

                Marshal.Copy((IntPtr)(buffer + bytesToCurrent), _buffer, 0, remaining);
                AddLength(remaining);
            }
        }

        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="byteCount"/> is negative.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        /// <returns>Bytes successfully written from the <paramref name="source" />.</returns>
        public int TryWriteBytes(Stream source, int byteCount)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }

            if (byteCount == 0)
            {
                return 0;
            }

            int bytesRead = 0;
            int bytesToCurrent = Math.Min(FreeBytes, byteCount);

            if (bytesToCurrent > 0)
            {
                bytesRead = source.TryReadAll(_buffer, Length, bytesToCurrent);
                AddLength(bytesRead);

                if (bytesRead != bytesToCurrent)
                {
                    return bytesRead;
                }
            }

            int remaining = byteCount - bytesToCurrent;
            if (remaining > 0)
            {
                Expand(remaining);
                bytesRead = source.TryReadAll(_buffer, 0, remaining);
                AddLength(bytesRead);

                bytesRead += bytesToCurrent;
            }

            return bytesRead;
        }

        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteBytes(ImmutableArray<byte> buffer)
        {
            WriteBytes(buffer, 0, buffer.IsDefault ? 0 : buffer.Length);
        }

        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Range specified by <paramref name="start"/> and <paramref name="byteCount"/> falls outside of the bounds of the <paramref name="buffer"/>.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteBytes(ImmutableArray<byte> buffer, int start, int byteCount)
        {
            WriteBytes(ImmutableArrayInterop.DangerousGetUnderlyingArray(buffer), start, byteCount);
        }

        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public unsafe void WriteBytes(byte[] buffer)
        {
            WriteBytes(buffer, 0, buffer?.Length ?? 0);
        }

        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Range specified by <paramref name="start"/> and <paramref name="byteCount"/> falls outside of the bounds of the <paramref name="buffer"/>.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public unsafe void WriteBytes(byte[] buffer, int start, int byteCount)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            BlobUtilities.ValidateRange(buffer.Length, start, byteCount);

            if (!IsHead)
            {
                ThrowHeadRequired();
            }

            // an empty array has no element pointer:
            if (buffer.Length == 0)
            {
                return;
            }

            fixed (byte* ptr = buffer)
            {
                WriteBytesUnchecked(ptr + start, byteCount);
            }
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void PadTo(int position)
        {
            WriteBytes(0, position - Count);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void Align(int alignment)
        {
            int position = Count;
            WriteBytes(0, BitArithmeticUtilities.Align(position, alignment) - position);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteBoolean(bool value)
        {
            WriteByte((byte)(value ? 1 : 0));
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteByte(byte value)
        {
            int start = ReserveBytesPrimitive(sizeof(byte));
            _buffer.WriteByte(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteSByte(sbyte value)
        {
            WriteByte(unchecked((byte)value));
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteDouble(double value)
        {
            int start = ReserveBytesPrimitive(sizeof(double));
            _buffer.WriteDouble(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteSingle(float value)
        {
            int start = ReserveBytesPrimitive(sizeof(float));
            _buffer.WriteSingle(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteInt16(short value)
        {
            WriteUInt16(unchecked((ushort)value));
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteUInt16(ushort value)
        {
            int start = ReserveBytesPrimitive(sizeof(ushort));
            _buffer.WriteUInt16(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        internal void WriteUInt16BE(ushort value)
        {
            int start = ReserveBytesPrimitive(sizeof(ushort));
            _buffer.WriteUInt16BE(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        internal void WriteUInt32BE(uint value)
        {
            int start = ReserveBytesPrimitive(sizeof(uint));
            _buffer.WriteUInt32BE(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteInt32(int value)
        {
            WriteUInt32(unchecked((uint)value));
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteUInt32(uint value)
        {
            int start = ReserveBytesPrimitive(sizeof(uint));
            _buffer.WriteUInt32(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteInt64(long value)
        {
            WriteUInt64(unchecked((ulong)value));
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteUInt64(ulong value)
        {
            int start = ReserveBytesPrimitive(sizeof(ulong));
            _buffer.WriteUInt64(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteDecimal(decimal value)
        {
            int start = ReserveBytesPrimitive(BlobUtilities.SizeOfSerializedDecimal);
            _buffer.WriteDecimal(start, value);
        }

        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteDateTime(DateTime value)
        {
            WriteInt64(value.Ticks);
        }

        /// <summary>
        /// Writes a reference to a heap (heap index) or a table (row id).
        /// </summary>
        /// <remarks>
        /// References may be small (2B) or large (4B).
        /// </remarks>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        internal void WriteReference(uint reference, int size)
        {
            Debug.Assert(size == 2 || size == 4);

            if (size == 2)
            {
                Debug.Assert((ushort)reference == reference);
                WriteUInt16((ushort)reference);
            }
            else
            {
                WriteUInt32(reference);
            }
        }

        /// <summary>
        /// Writes UTF16 (little-endian) encoded string at the current position.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteUTF16(char[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!IsHead)
            {
                ThrowHeadRequired();
            }

            if (value.Length == 0)
            {
                return;
            }

            fixed (char* ptr = value)
            {
                WriteBytesUnchecked((byte*)ptr, value.Length * sizeof(char));
            }
        }

        /// <summary>
        /// Writes UTF16 (little-endian) encoded string at the current position.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteUTF16(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!IsHead)
            {
                ThrowHeadRequired();
            }

            fixed (char* ptr = value)
            {
                WriteBytesUnchecked((byte*)ptr, value.Length * sizeof(char));
            }
        }

        /// <summary>
        /// Writes string in SerString format (see ECMA-335-II 23.3 Custom attributes): 
        /// The string is UTF8 encoded and prefixed by the its size in bytes. 
        /// Null string is represented as a single byte 0xFF.
        /// </summary>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteSerializedString(string value)
        {
            if (value == null)
            {
                WriteByte(0xff);
                return;
            }

            WriteUTF8(value, 0, value.Length, allowUnpairedSurrogates: true, prependSize: true);
        }

        /// <summary>
        /// Writes UTF8 encoded string at the current position.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteUTF8(string value, bool allowUnpairedSurrogates)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            WriteUTF8(value, 0, value.Length, allowUnpairedSurrogates, prependSize: false);
        }

        private void WriteUTF8(string str, int start, int length, bool allowUnpairedSurrogates, bool prependSize)
        {
            if (!IsHead)
            {
                ThrowHeadRequired();
            }

            fixed (char* strPtr = str)
            {
                char* currentPtr = strPtr + start;
                char* nextPtr;

                // the max size of compressed int is 4B:
                int byteLimit = FreeBytes - (prependSize ? sizeof(uint) : 0);

                int bytesToCurrent = BlobUtilities.GetUTF8ByteCount(currentPtr, length, byteLimit, out nextPtr);
                int charsToCurrent = (int)(nextPtr - currentPtr);
                int charsToNext = str.Length - charsToCurrent;
                int bytesToNext = BlobUtilities.GetUTF8ByteCount(nextPtr, charsToNext);

                if (prependSize)
                {
                    WriteCompressedInteger((uint)(bytesToCurrent + bytesToNext));
                }

                _buffer.WriteUTF8(Length, currentPtr, charsToCurrent, bytesToCurrent, allowUnpairedSurrogates);
                AddLength(bytesToCurrent);

                if (bytesToNext > 0)
                {
                    Expand(bytesToNext);

                    _buffer.WriteUTF8(0, nextPtr, charsToNext, bytesToNext, allowUnpairedSurrogates);
                    AddLength(bytesToNext);
                }
            }
        }

        /// <summary>
        /// Implements compressed signed integer encoding as defined by ECMA-335-II chapter 23.2: Blobs and signatures.
        /// </summary>
        /// <remarks>
        /// If the value lies between -64 (0xFFFFFFC0) and 63 (0x3F), inclusive, encode as a one-byte integer: 
        /// bit 7 clear, value bits 5 through 0 held in bits 6 through 1, sign bit (value bit 31) in bit 0.
        /// 
        /// If the value lies between -8192 (0xFFFFE000) and 8191 (0x1FFF), inclusive, encode as a two-byte integer: 
        /// 15 set, bit 14 clear, value bits 12 through 0 held in bits 13 through 1, sign bit(value bit 31) in bit 0.
        /// 
        /// If the value lies between -268435456 (0xF000000) and 268435455 (0x0FFFFFFF), inclusive, encode as a four-byte integer: 
        /// 31 set, 30 set, bit 29 clear, value bits 27 through 0 held in bits 28 through 1, sign bit(value bit 31) in bit 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> can't be represented as a compressed signed integer.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        internal void WriteCompressedSignedInteger(int value)
        {
            BlobWriterImpl.WriteCompressedSignedInteger(this, value);
        }

        /// <summary>
        /// Implements compressed unsigned integer encoding as defined by ECMA-335-II chapter 23.2: Blobs and signatures.
        /// </summary>
        /// <remarks>
        /// If the value lies between 0 (0x00) and 127 (0x7F), inclusive, 
        /// encode as a one-byte integer (bit 7 is clear, value held in bits 6 through 0).
        /// 
        /// If the value lies between 28 (0x80) and 214 – 1 (0x3FFF), inclusive, 
        /// encode as a 2-byte integer with bit 15 set, bit 14 clear(value held in bits 13 through 0).
        /// 
        /// Otherwise, encode as a 4-byte integer, with bit 31 set, bit 30 set, bit 29 clear (value held in bits 28 through 0).
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> can't be represented as a compressed integer.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        internal void WriteCompressedInteger(uint value)
        {
            BlobWriterImpl.WriteCompressedInteger(this, value);
        }

        /// <summary>
        /// Writes a constant value (see ECMA-335 Partition II section 22.9) at the current position.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not of a constant type.</exception>
        /// <exception cref="InvalidOperationException">Builder is not writable, it has been linked with another one.</exception>
        public void WriteConstant(object value)
        {
            BlobWriterImpl.WriteConstant(this, value);
        }

        internal string GetDebuggerDisplay()
        {
            return IsHead ? 
                string.Join("->", GetChunks().Select(chunk => $"[{Display(chunk._buffer, chunk.Length)}]")) : 
                $"<{Display(_buffer, Length)}>";
        }

        private static string Display(byte[] bytes, int length)
        {
            const int MaxDisplaySize = 64;

            return (length <= MaxDisplaySize) ?
                BitConverter.ToString(bytes, 0, length) :
                BitConverter.ToString(bytes, 0, MaxDisplaySize / 2) + "-...-" + BitConverter.ToString(bytes, length - MaxDisplaySize / 2, MaxDisplaySize / 2);
        }
    }
}
