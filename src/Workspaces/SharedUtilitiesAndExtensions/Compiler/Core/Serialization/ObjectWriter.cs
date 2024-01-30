// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis;
using EncodingExtensions = Microsoft.CodeAnalysis.EncodingExtensions;
using System.IO.Pipelines;
using System.Buffers;

namespace Roslyn.Utilities
{
    using System.Buffers;
    using System.Buffers.Binary;
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using Humanizer.Bytes;
#if COMPILERCORE
    using Resources = CodeAnalysisResources;
#elif CODE_STYLE
    using Resources = CodeStyleResources;
#else
    using Resources = WorkspacesResources;
#endif

    /// <summary>
    /// An <see cref="ObjectWriter"/> that serializes objects to a byte stream.
    /// </summary>
    internal sealed partial class ObjectWriter : IDisposable
    {
        private readonly PipeWriter _writer;
        private readonly bool _leaveOpen;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Map of serialized string reference ids.  The string-reference-map uses value-equality for greater cache hits
        /// and reuse.
        ///
        /// This is a mutable struct, and as such is not readonly.
        ///
        /// When we write out strings we give each successive, unique, item a monotonically increasing integral ID
        /// starting at 0.  I.e. the first string gets ID-0, the next gets ID-1 and so on and so forth.  We do *not*
        /// include these IDs with the object when it is written out.  We only include the ID if we hit the object
        /// *again* while writing.
        ///
        /// During reading, the reader knows to give each string it reads the same monotonically increasing integral
        /// value.  i.e. the first string it reads is put into an array at position 0, the next at position 1, and so
        /// on.  Then, when the reader reads in a string-reference it can just retrieved it directly from that array.
        ///
        /// In other words, writing and reading take advantage of the fact that they know they will write and read
        /// strings in the exact same order.  So they only need the IDs for references and not the strings themselves
        /// because the ID is inferred from the order the object is written or read in.
        /// </summary>
        private WriterReferenceMap _stringReferenceMap;

        /// <summary>
        /// Creates a new instance of a <see cref="ObjectWriter"/>.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        public ObjectWriter(
            PipeWriter writer,
            bool leaveOpen,
            CancellationToken cancellationToken)
        {
            // String serialization assumes both reader and writer to be of the same endianness.
            // It can be adjusted for BigEndian if needed.
            Debug.Assert(BitConverter.IsLittleEndian);

            _writer = writer;
            _leaveOpen = leaveOpen;
            _stringReferenceMap = new WriterReferenceMap();
            _cancellationToken = cancellationToken;

            WriteVersion();
        }

        public void Dispose()
        {
            if (!_leaveOpen)
                _writer.Complete();
            _stringReferenceMap.Dispose();
        }

        private void WriteVersion()
        {
            WriteByte(ObjectReader.VersionByte1);
            WriteByte(ObjectReader.VersionByte2);
        }

        public void WriteByte(byte value)
        {
            var span = _writer.GetSpan(1);
            span[0] = value;
            _writer.Advance(1);
        }

        public void WriteSByte(sbyte value)
            => WriteByte(unchecked((byte)value));

        public void WriteInt16(short value)
        {
            const int size = 2;
            var span = _writer.GetSpan(size);
            BinaryPrimitives.WriteInt16LittleEndian(span, value);
            _writer.Advance(size);
        }

        public void WriteUInt16(ushort value)
            => WriteInt16(unchecked((short)value));

        public void WriteInt32(int value)
        {
            const int size = 4;
            var span = _writer.GetSpan(size);
            BinaryPrimitives.WriteInt32LittleEndian(span, value);
            _writer.Advance(size);
        }

        public void WriteUInt32(uint value)
            => WriteInt32(unchecked((int)value));

        public void WriteInt64(long value)
        {
            const int size = 8;
            var span = _writer.GetSpan(size);
            BinaryPrimitives.WriteInt64LittleEndian(span, value);
            _writer.Advance(size);
        }

        public void WriteUInt64(ulong value)
            => WriteInt64(unchecked((long)value));

        public void WriteDouble(double value)
        {
            const int size = 8;
            var span = _writer.GetSpan(size);
            var bits = BitConverter.DoubleToInt64Bits(value);
            MemoryMarshal.Write(span, ref bits);
            _writer.Advance(size);
        }

        public void WriteSingle(float value)
        {
            const int size = 4;
            var span = _writer.GetSpan(size);
            var bits = BitConverter.DoubleToInt64Bits(value);
            MemoryMarshal.Write(span, ref bits);
            _writer.Advance(size);
        }

        public void WriteBoolean(bool value)
            => WriteByte(value ? (byte)1 : (byte)0);

        // written as ushort because BinaryWriter fails on chars that are unicode surrogates
        public void WriteChar(char ch)
            => WriteUInt16((ushort)ch);

        public async ValueTask WriteStringAsync(string? value)
        {
            if (value == null)
            {
                WriteByte((byte)TypeCode.Null);
            }
            else
            {
                if (_stringReferenceMap.TryGetReferenceId(value, out var id))
                {
                    Debug.Assert(id >= 0);
                    if (id <= byte.MaxValue)
                    {
                        WriteByte((byte)TypeCode.StringRef_1Byte);
                        WriteByte((byte)id);
                    }
                    else if (id <= ushort.MaxValue)
                    {
                        WriteByte((byte)TypeCode.StringRef_2Bytes);
                        WriteUInt16((ushort)id);
                    }
                    else
                    {
                        WriteByte((byte)TypeCode.StringRef_4Bytes);
                        WriteInt32(id);
                    }
                }
                else
                {
                    _stringReferenceMap.Add(value);

                    if (value.IsValidUnicodeString())
                    {
                        WriteUtf8String();
                    }
                    else
                    {
                        WriteUtf16String();
                    }

                    await _writer.FlushAsync(_cancellationToken).ConfigureAwait(false);
                }
            }

            return;

            void WriteUtf8String()
            {
                // Usual case - the string can be encoded as UTF-8:
                // We can use the UTF-8 encoding of the binary writer.

                WriteByte((byte)TypeCode.StringUtf8);
                var byteCount = Encoding.UTF8.GetByteCount(value);
                WriteCompressedUInt(unchecked((uint)byteCount));

#if NETSTANDARD
                var bytes = System.Buffers.ArrayPool<byte>.Shared.Rent(byteCount);
                var encodedCount = Encoding.UTF8.GetBytes(value, 0, value.Length, bytes, 0);
                Contract.ThrowIfTrue(byteCount != encodedCount);
                var writerSpan = _writer.GetSpan(byteCount);
                bytes.AsSpan().Slice(0, byteCount).CopyTo(writerSpan);
                _writer.Advance(byteCount);
                System.Buffers.ArrayPool<byte>.Shared.Return(bytes);
#else
                var writtenBytes = Encoding.UTF8.GetBytes(value, _writer);
                Contract.ThrowIfTrue(byteCount != writtenBytes);
                // don't need to Advance.  GetBytes already does that.
#endif
            }

            void WriteUtf16String()
            {
                var span = value.AsSpan();
                var bytes = MemoryMarshal.AsBytes(span);

                WriteByte((byte)TypeCode.StringUtf16);
                WriteCompressedUInt(unchecked((uint)bytes.Length));
                _writer.Write(bytes);

                // don't need to Advance.  _writer.Write already does that.
            }
        }

        public void WriteDecimal(decimal value)
            => throw new NotImplementedException();

        /// <summary>
        /// Used so we can easily grab the low/high 64bits of a guid for serialization.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        internal struct GuidAccessor
        {
            [FieldOffset(0)]
            public Guid Guid;

            [FieldOffset(0)]
            public long Low64;
            [FieldOffset(8)]
            public long High64;
        }

        public void WriteGuid(Guid guid)
        {
            var accessor = new GuidAccessor { Guid = guid };
            WriteInt64(accessor.Low64);
            WriteInt64(accessor.High64);
        }

        public async ValueTask WriteValueAsync(object? value)
        {
            Debug.Assert(value == null || !value.GetType().GetTypeInfo().IsEnum, "Enum should not be written with WriteValue.  Write them as ints instead.");

            if (value == null)
            {
                WriteByte((byte)TypeCode.Null);
                return;
            }

            var type = value.GetType();
            var typeInfo = type.GetTypeInfo();
            Debug.Assert(!typeInfo.IsEnum, "Enums should not be written with WriteObject.  Write them out as integers instead.");

            // Perf: Note that JIT optimizes each expression value.GetType() == typeof(T) to a single register comparison.
            // Also the checks are sorted by commonality of the checked types.

            // The primitive types are
            // Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32,
            // Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single.
            if (typeInfo.IsPrimitive)
            {
                // Note: int, double, bool, char, have been chosen to go first as they're they
                // common values of literals in code, and so would be the likely hits if we do
                // have a primitive type we're serializing out.
                if (value.GetType() == typeof(int))
                {
                    WriteEncodedInt32((int)value);
                }
                else if (value.GetType() == typeof(double))
                {
                    WriteByte((byte)TypeCode.Float8);
                    WriteDouble((double)value);
                }
                else if (value.GetType() == typeof(bool))
                {
                    WriteByte((byte)((bool)value ? TypeCode.Boolean_True : TypeCode.Boolean_False));
                }
                else if (value.GetType() == typeof(char))
                {
                    WriteByte((byte)TypeCode.Char);
                    WriteUInt16((ushort)(char)value);  // written as ushort because BinaryWriter fails on chars that are unicode surrogates
                }
                else if (value.GetType() == typeof(byte))
                {
                    WriteByte((byte)TypeCode.UInt8);
                    WriteByte((byte)value);
                }
                else if (value.GetType() == typeof(short))
                {
                    WriteByte((byte)TypeCode.Int16);
                    WriteInt16((short)value);
                }
                else if (value.GetType() == typeof(long))
                {
                    WriteByte((byte)TypeCode.Int64);
                    WriteInt64((long)value);
                }
                else if (value.GetType() == typeof(sbyte))
                {
                    WriteByte((byte)TypeCode.Int8);
                    WriteSByte((sbyte)value);
                }
                else if (value.GetType() == typeof(float))
                {
                    WriteByte((byte)TypeCode.Float4);
                    WriteSingle((float)value);
                }
                else if (value.GetType() == typeof(ushort))
                {
                    WriteByte((byte)TypeCode.UInt16);
                    WriteUInt16((ushort)value);
                }
                else if (value.GetType() == typeof(uint))
                {
                    WriteEncodedUInt32((uint)value);
                }
                else if (value.GetType() == typeof(ulong))
                {
                    WriteByte((byte)TypeCode.UInt64);
                    WriteUInt64((ulong)value);
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(value.GetType());
                }
            }
            else if (value.GetType() == typeof(decimal))
            {
                WriteByte((byte)TypeCode.DateTime);
                ((decimal)value).GetBits(out var isNegative, out var scale, out var low, out var mid, out var high);
                WriteBoolean(isNegative);
                WriteByte(scale);
                WriteUInt32(low);
                WriteUInt32(mid);
                WriteUInt32(high);
            }
            else if (value.GetType() == typeof(DateTime))
            {
                WriteByte((byte)TypeCode.DateTime);
                WriteInt64(((DateTime)value).ToBinary());
            }
            else if (value.GetType() == typeof(string))
            {
                await WriteStringAsync((string)value).ConfigureAwait(false);
            }
            else if (type.IsArray)
            {
                var instance = (Array)value;

                if (instance.Rank > 1)
                {
                    throw new InvalidOperationException(Resources.Arrays_with_more_than_one_dimension_cannot_be_serialized);
                }

                await WriteArrayAsync(instance).ConfigureAwait(false);
            }
            else if (value is Encoding encoding)
            {
                await WriteEncodingAsync(encoding).ConfigureAwait(false);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported object type: {value.GetType()}");
            }
        }

        /// <summary>
        /// Write an array of bytes. The array data is provided as a
        /// <see cref="ReadOnlySpan{T}">ReadOnlySpan</see>&lt;<see cref="byte"/>&gt;, and deserialized to a byte array.
        /// </summary>
        /// <param name="span">The array data.</param>
        public void WriteValue(ReadOnlySpan<byte> span)
        {
            var length = span.Length;
            switch (length)
            {
                case 0:
                    WriteByte((byte)TypeCode.Array_0);
                    break;
                case 1:
                    WriteByte((byte)TypeCode.Array_1);
                    break;
                case 2:
                    WriteByte((byte)TypeCode.Array_2);
                    break;
                case 3:
                    WriteByte((byte)TypeCode.Array_3);
                    break;
                default:
                    WriteByte((byte)TypeCode.Array);
                    WriteCompressedUInt((uint)length);
                    break;
            }

            var elementType = typeof(byte);
            Debug.Assert(s_typeMap[elementType] == TypeCode.UInt8);

            WritePrimitiveType(elementType, TypeCode.UInt8);

#if NETCOREAPP
            _writer.Write(span);
#else
            // BinaryWriter in .NET Framework does not support ReadOnlySpan<byte>, so we use a temporary buffer to write
            // arrays of data. The buffer is chosen to be no larger than 8K, which avoids allocations in the large
            // object heap.
            var buffer = new byte[Math.Min(length, 8192)];
            for (var offset = 0; offset < length; offset += buffer.Length)
            {
                var segmentLength = Math.Min(buffer.Length, length - offset);
                span.Slice(offset, segmentLength).CopyTo(buffer.AsSpan());
                _writer.Write(buffer, 0, segmentLength);
            }
#endif
        }

        private void WriteEncodedInt32(int v)
        {
            if (v >= 0 && v <= 10)
            {
                WriteByte((byte)((int)TypeCode.Int32_0 + v));
            }
            else if (v >= 0 && v < byte.MaxValue)
            {
                WriteByte((byte)TypeCode.Int32_1Byte);
                WriteByte((byte)v);
            }
            else if (v >= 0 && v < ushort.MaxValue)
            {
                WriteByte((byte)TypeCode.Int32_2Bytes);
                WriteUInt16((ushort)v);
            }
            else
            {
                WriteByte((byte)TypeCode.Int32);
                WriteInt32(v);
            }
        }

        private void WriteEncodedUInt32(uint v)
        {
            if (v >= 0 && v <= 10)
            {
                WriteByte((byte)((int)TypeCode.UInt32_0 + v));
            }
            else if (v >= 0 && v < byte.MaxValue)
            {
                WriteByte((byte)TypeCode.UInt32_1Byte);
                WriteByte((byte)v);
            }
            else if (v >= 0 && v < ushort.MaxValue)
            {
                WriteByte((byte)TypeCode.UInt32_2Bytes);
                WriteUInt16((ushort)v);
            }
            else
            {
                WriteByte((byte)TypeCode.UInt32);
                WriteUInt32(v);
            }
        }

        /// <summary>
        /// An object reference to reference-id map, that can share base data efficiently.
        /// </summary>
        private struct WriterReferenceMap
        {
            // PERF: Use segmented collection to avoid Large Object Heap allocations during serialization.
            // https://github.com/dotnet/roslyn/issues/43401
            private readonly SegmentedDictionary<object, int> _valueToIdMap;
            private int _nextId;

            private static readonly ObjectPool<SegmentedDictionary<object, int>> s_valueDictionaryPool =
                new(() => new SegmentedDictionary<object, int>(128));

            public WriterReferenceMap()
            {
                _valueToIdMap = s_valueDictionaryPool.Allocate();
                _nextId = 0;
            }

            public void Dispose()
            {
                // If the map grew too big, don't return it to the pool.
                // When testing with the Roslyn solution, this dropped only 2.5% of requests.
                if (_valueToIdMap.Count > 1024)
                {
                    s_valueDictionaryPool.ForgetTrackedObject(_valueToIdMap);
                }
                else
                {
                    _valueToIdMap.Clear();
                    s_valueDictionaryPool.Free(_valueToIdMap);
                }
            }

            public bool TryGetReferenceId(object value, out int referenceId)
                => _valueToIdMap.TryGetValue(value, out referenceId);

            public void Add(object value)
            {
                var id = _nextId++;
                _valueToIdMap.Add(value, id);
            }
        }

        internal void WriteCompressedUInt(uint value)
        {
            if (value <= (byte.MaxValue >> 2))
            {
                WriteByte((byte)value);
            }
            else if (value <= (ushort.MaxValue >> 2))
            {
                var byte0 = (byte)(((value >> 8) & 0xFFu) | Byte2Marker);
                var byte1 = (byte)(value & 0xFFu);

                // high-bytes to low-bytes
                WriteByte(byte0);
                WriteByte(byte1);
            }
            else if (value <= (uint.MaxValue >> 2))
            {
                var byte0 = (byte)(((value >> 24) & 0xFFu) | Byte4Marker);
                var byte1 = (byte)((value >> 16) & 0xFFu);
                var byte2 = (byte)((value >> 8) & 0xFFu);
                var byte3 = (byte)(value & 0xFFu);

                // high-bytes to low-bytes
                WriteByte(byte0);
                WriteByte(byte1);
                WriteByte(byte2);
                WriteByte(byte3);
            }
            else
            {
                throw new ArgumentException(Resources.Value_too_large_to_be_represented_as_a_30_bit_unsigned_integer);
            }
        }

        private async ValueTask WriteArrayAsync(Array array)
        {
            var length = array.GetLength(0);

            switch (length)
            {
                case 0:
                    WriteByte((byte)TypeCode.Array_0);
                    break;
                case 1:
                    WriteByte((byte)TypeCode.Array_1);
                    break;
                case 2:
                    WriteByte((byte)TypeCode.Array_2);
                    break;
                case 3:
                    WriteByte((byte)TypeCode.Array_3);
                    break;
                default:
                    WriteByte((byte)TypeCode.Array);
                    WriteCompressedUInt((uint)length);
                    break;
            }

            var elementType = array.GetType().GetElementType()!;

            if (s_typeMap.TryGetValue(elementType, out var elementKind))
            {
                WritePrimitiveType(elementType, elementKind);
                await WritePrimitiveTypeArrayElementsAsync(elementType, elementKind, array).ConfigureAwait(false);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported array element type: {elementType}");
            }
        }

        private async ValueTask WriteArrayValuesAsync(Array array)
        {
            for (var i = 0; i < array.Length; i++)
                await WriteValueAsync(array.GetValue(i)).ConfigureAwait(false);
        }

        private async ValueTask WritePrimitiveTypeArrayElementsAsync(Type type, TypeCode kind, Array instance)
        {
            Debug.Assert(s_typeMap[type] == kind);

            // optimization for type underlying binary writer knows about
            if (type == typeof(byte))
            {
                _writer.Write((byte[])instance);
            }
            else if (type == typeof(char))
            {
                _writer.Write(MemoryMarshal.AsBytes(((char[])instance).AsSpan()));
            }
            else if (type == typeof(string))
            {
                // optimization for string which object writer has
                // its own optimization to reduce repeated string
                await WriteStringArrayElementsAsync((string[])instance).ConfigureAwait(false);
            }
            else if (type == typeof(bool))
            {
                // optimization for bool array
                WriteBooleanArrayElements((bool[])instance);
            }
            else
            {
                // otherwise, write elements directly to underlying binary writer
                switch (kind)
                {
                    case TypeCode.Int8:
                        WriteInt8ArrayElements((sbyte[])instance);
                        return;
                    case TypeCode.Int16:
                        WriteInt16ArrayElements((short[])instance);
                        return;
                    case TypeCode.Int32:
                        WriteInt32ArrayElements((int[])instance);
                        return;
                    case TypeCode.Int64:
                        WriteInt64ArrayElements((long[])instance);
                        return;
                    case TypeCode.UInt16:
                        WriteUInt16ArrayElements((ushort[])instance);
                        return;
                    case TypeCode.UInt32:
                        WriteUInt32ArrayElements((uint[])instance);
                        return;
                    case TypeCode.UInt64:
                        WriteUInt64ArrayElements((ulong[])instance);
                        return;
                    case TypeCode.Float4:
                        WriteFloat4ArrayElements((float[])instance);
                        return;
                    case TypeCode.Float8:
                        WriteFloat8ArrayElements((double[])instance);
                        return;
                    case TypeCode.Decimal:
                        WriteDecimalArrayElements((decimal[])instance);
                        return;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(kind);
                }
            }
        }

        private void WriteBooleanArrayElements(bool[] array)
        {
            // convert bool array to bit array
            var bits = BitVector.Create(array.Length);
            for (var i = 0; i < array.Length; i++)
            {
                bits[i] = array[i];
            }

            // send over bit array
            foreach (var word in bits.Words())
                WriteUInt64(word);
        }

        private async ValueTask WriteStringArrayElementsAsync(string[] array)
        {
            for (var i = 0; i < array.Length; i++)
                await WriteStringAsync(array[i]).ConfigureAwait(false);
        }

        private void WriteInt8ArrayElements(sbyte[] array)
        {
            for (var i = 0; i < array.Length; i++)
                WriteSByte(array[i]);
        }

        private void WriteInt16ArrayElements(short[] array)
        {
            for (var i = 0; i < array.Length; i++)
                WriteInt16(array[i]);
        }

        private void WriteInt32ArrayElements(int[] array)
        {
            for (var i = 0; i < array.Length; i++)
                WriteInt32(array[i]);
        }

        private void WriteInt64ArrayElements(long[] array)
        {
            for (var i = 0; i < array.Length; i++)
                WriteInt64(array[i]);
        }

        private void WriteUInt16ArrayElements(ushort[] array)
        {
            for (var i = 0; i < array.Length; i++)
                WriteUInt16(array[i]);
        }

        private void WriteUInt32ArrayElements(uint[] array)
        {
            for (var i = 0; i < array.Length; i++)
                WriteUInt32(array[i]);
        }

        private void WriteUInt64ArrayElements(ulong[] array)
        {
            for (var i = 0; i < array.Length; i++)
                WriteUInt64(array[i]);
        }

        private void WriteDecimalArrayElements(decimal[] array)
        {
            for (var i = 0; i < array.Length; i++)
                WriteDecimal(array[i]);
        }

        private void WriteFloat4ArrayElements(float[] array)
        {
            for (var i = 0; i < array.Length; i++)
                WriteSingle(array[i]);
        }

        private void WriteFloat8ArrayElements(double[] array)
        {
            for (var i = 0; i < array.Length; i++)
                WriteDouble(array[i]);
        }

        private void WritePrimitiveType(Type type, TypeCode kind)
        {
            Debug.Assert(s_typeMap[type] == kind);
            WriteByte((byte)kind);
        }

        public async ValueTask WriteEncodingAsync(Encoding? encoding)
        {
            if (encoding == null)
            {
                WriteByte((byte)TypeCode.Null);
            }
            else if (encoding.TryGetEncodingKind(out var kind))
            {
                WriteByte((byte)ToTypeCode(kind));
            }
            else if (encoding.CodePage > 0)
            {
                WriteByte((byte)TypeCode.EncodingCodePage);
                WriteInt32(encoding.CodePage);
            }
            else
            {
                WriteByte((byte)TypeCode.EncodingName);
                await WriteStringAsync(encoding.WebName).ConfigureAwait(false);
            }
        }

        // we have s_typeMap and s_reversedTypeMap since there is no bidirectional map in compiler
        // Note: s_typeMap is effectively immutable.  However, for maximum perf we use mutable types because
        // they are used in hotspots.
        internal static readonly Dictionary<Type, TypeCode> s_typeMap;

        /// <summary>
        /// Indexed by <see cref="TypeCode"/>.
        /// </summary>
        internal static readonly ImmutableArray<Type> s_reverseTypeMap;

        static ObjectWriter()
        {
            s_typeMap = new Dictionary<Type, TypeCode>
            {
                { typeof(bool), TypeCode.BooleanType },
                { typeof(char), TypeCode.Char },
                { typeof(string), TypeCode.StringType },
                { typeof(sbyte), TypeCode.Int8 },
                { typeof(short), TypeCode.Int16 },
                { typeof(int), TypeCode.Int32 },
                { typeof(long), TypeCode.Int64 },
                { typeof(byte), TypeCode.UInt8 },
                { typeof(ushort), TypeCode.UInt16 },
                { typeof(uint), TypeCode.UInt32 },
                { typeof(ulong), TypeCode.UInt64 },
                { typeof(float), TypeCode.Float4 },
                { typeof(double), TypeCode.Float8 },
                { typeof(decimal), TypeCode.Decimal },
            };

            var temp = new Type[(int)TypeCode.Last];

            foreach (var kvp in s_typeMap)
            {
                temp[(int)kvp.Value] = kvp.Key;
            }

            s_reverseTypeMap = ImmutableArray.Create(temp);
        }

        /// <summary>
        /// byte marker mask for encoding compressed uint
        /// </summary>
        internal const byte ByteMarkerMask = 3 << 6;

        /// <summary>
        /// byte marker bits for uint encoded in 1 byte.
        /// </summary>
        internal const byte Byte1Marker = 0;

        /// <summary>
        /// byte marker bits for uint encoded in 2 bytes.
        /// </summary>
        internal const byte Byte2Marker = 1 << 6;

        /// <summary>
        /// byte marker bits for uint encoded in 4 bytes.
        /// </summary>
        internal const byte Byte4Marker = 2 << 6;

        internal enum TypeCode : byte
        {
            /// <summary>
            /// The null value
            /// </summary>
            Null,

            /// <summary>
            /// A string encoded as UTF-8 (using BinaryWriter.Write(string))
            /// </summary>
            StringUtf8,

            /// <summary>
            /// A string encoded as UTF16 (as array of UInt16 values)
            /// </summary>
            StringUtf16,

            /// <summary>
            /// A reference to a string with the id encoded as 1 byte.
            /// </summary>
            StringRef_1Byte,

            /// <summary>
            /// A reference to a string with the id encoded as 2 bytes.
            /// </summary>
            StringRef_2Bytes,

            /// <summary>
            /// A reference to a string with the id encoded as 4 bytes.
            /// </summary>
            StringRef_4Bytes,

            /// <summary>
            /// The boolean value true.
            /// </summary>
            Boolean_True,

            /// <summary>
            /// The boolean value char.
            /// </summary>
            Boolean_False,

            /// <summary>
            /// A character value encoded as 2 bytes.
            /// </summary>
            Char,

            /// <summary>
            /// An Int8 value encoded as 1 byte.
            /// </summary>
            Int8,

            /// <summary>
            /// An Int16 value encoded as 2 bytes.
            /// </summary>
            Int16,

            /// <summary>
            /// An Int32 value encoded as 4 bytes.
            /// </summary>
            Int32,

            /// <summary>
            /// An Int32 value encoded as 1 byte.
            /// </summary>
            Int32_1Byte,

            /// <summary>
            /// An Int32 value encoded as 2 bytes.
            /// </summary>
            Int32_2Bytes,

            /// <summary>
            /// The Int32 value 0
            /// </summary>
            Int32_0,

            /// <summary>
            /// The Int32 value 1
            /// </summary>
            Int32_1,

            /// <summary>
            /// The Int32 value 2
            /// </summary>
            Int32_2,

            /// <summary>
            /// The Int32 value 3
            /// </summary>
            Int32_3,

            /// <summary>
            /// The Int32 value 4
            /// </summary>
            Int32_4,

            /// <summary>
            /// The Int32 value 5
            /// </summary>
            Int32_5,

            /// <summary>
            /// The Int32 value 6
            /// </summary>
            Int32_6,

            /// <summary>
            /// The Int32 value 7
            /// </summary>
            Int32_7,

            /// <summary>
            /// The Int32 value 8
            /// </summary>
            Int32_8,

            /// <summary>
            /// The Int32 value 9
            /// </summary>
            Int32_9,

            /// <summary>
            /// The Int32 value 10
            /// </summary>
            Int32_10,

            /// <summary>
            /// An Int64 value encoded as 8 bytes
            /// </summary>
            Int64,

            /// <summary>
            /// A UInt8 value encoded as 1 byte.
            /// </summary>
            UInt8,

            /// <summary>
            /// A UIn16 value encoded as 2 bytes.
            /// </summary>
            UInt16,

            /// <summary>
            /// A UInt32 value encoded as 4 bytes.
            /// </summary>
            UInt32,

            /// <summary>
            /// A UInt32 value encoded as 1 byte.
            /// </summary>
            UInt32_1Byte,

            /// <summary>
            /// A UInt32 value encoded as 2 bytes.
            /// </summary>
            UInt32_2Bytes,

            /// <summary>
            /// The UInt32 value 0
            /// </summary>
            UInt32_0,

            /// <summary>
            /// The UInt32 value 1
            /// </summary>
            UInt32_1,

            /// <summary>
            /// The UInt32 value 2
            /// </summary>
            UInt32_2,

            /// <summary>
            /// The UInt32 value 3
            /// </summary>
            UInt32_3,

            /// <summary>
            /// The UInt32 value 4
            /// </summary>
            UInt32_4,

            /// <summary>
            /// The UInt32 value 5
            /// </summary>
            UInt32_5,

            /// <summary>
            /// The UInt32 value 6
            /// </summary>
            UInt32_6,

            /// <summary>
            /// The UInt32 value 7
            /// </summary>
            UInt32_7,

            /// <summary>
            /// The UInt32 value 8
            /// </summary>
            UInt32_8,

            /// <summary>
            /// The UInt32 value 9
            /// </summary>
            UInt32_9,

            /// <summary>
            /// The UInt32 value 10
            /// </summary>
            UInt32_10,

            /// <summary>
            /// A UInt64 value encoded as 8 bytes.
            /// </summary>
            UInt64,

            /// <summary>
            /// A float value encoded as 4 bytes.
            /// </summary>
            Float4,

            /// <summary>
            /// A double value encoded as 8 bytes.
            /// </summary>
            Float8,

            /// <summary>
            /// A decimal value encoded as 12 bytes.
            /// </summary>
            Decimal,

            /// <summary>
            /// A DateTime value
            /// </summary>
            DateTime,

            /// <summary>
            /// An array with length encoded as compressed uint
            /// </summary>
            Array,

            /// <summary>
            /// An array with zero elements
            /// </summary>
            Array_0,

            /// <summary>
            /// An array with one element
            /// </summary>
            Array_1,

            /// <summary>
            /// An array with 2 elements
            /// </summary>
            Array_2,

            /// <summary>
            /// An array with 3 elements
            /// </summary>
            Array_3,

            /// <summary>
            /// The boolean type
            /// </summary>
            BooleanType,

            /// <summary>
            /// The string type
            /// </summary>
            StringType,

            /// <summary>
            /// Encoding serialized as <see cref="Encoding.WebName"/>.
            /// </summary>
            EncodingName,

            /// <summary>
            /// Encoding serialized as <see cref="TextEncodingKind"/>.
            /// </summary>
            FirstWellKnownTextEncoding,
            LastWellKnownTextEncoding = FirstWellKnownTextEncoding + EncodingExtensions.LastTextEncodingKind - EncodingExtensions.FirstTextEncodingKind,

            /// <summary>
            /// Encoding serialized as <see cref="Encoding.CodePage"/>.
            /// </summary>
            EncodingCodePage,

            Last,
        }

        internal static TypeCode ToTypeCode(TextEncodingKind kind)
        {
            Debug.Assert(kind is >= EncodingExtensions.FirstTextEncodingKind and <= EncodingExtensions.LastTextEncodingKind);
            return TypeCode.FirstWellKnownTextEncoding + (byte)(kind - EncodingExtensions.FirstTextEncodingKind);
        }

        internal static TextEncodingKind ToEncodingKind(TypeCode code)
        {
            Debug.Assert(code is >= TypeCode.FirstWellKnownTextEncoding and <= TypeCode.LastWellKnownTextEncoding);
            return EncodingExtensions.FirstTextEncodingKind + (byte)(code - TypeCode.FirstWellKnownTextEncoding);
        }
    }
}
