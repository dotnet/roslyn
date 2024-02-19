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

namespace Roslyn.Utilities
{
    using System.Collections.Immutable;
    using System.Threading.Tasks;
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
        private readonly BinaryWriter _writer;
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
        /// <param name="stream">The stream to write to.</param>
        /// <param name="leaveOpen">True to leave the <paramref name="stream"/> open after the <see cref="ObjectWriter"/> is disposed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public ObjectWriter(
            Stream stream,
            bool leaveOpen = false,
            CancellationToken cancellationToken = default)
        {
            // String serialization assumes both reader and writer to be of the same endianness.
            // It can be adjusted for BigEndian if needed.
            Debug.Assert(BitConverter.IsLittleEndian);

            _writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen);
            _stringReferenceMap = new WriterReferenceMap();
            _cancellationToken = cancellationToken;

            WriteVersion();
        }

        private void WriteVersion()
        {
            _writer.Write(ObjectReader.VersionByte1);
            _writer.Write(ObjectReader.VersionByte2);
        }

        public void Dispose()
        {
            _writer.Dispose();
            _stringReferenceMap.Dispose();
        }

        public void WriteBoolean(bool value) => _writer.Write(value);
        public void WriteByte(byte value) => _writer.Write(value);
        // written as ushort because BinaryWriter fails on chars that are unicode surrogates
        public void WriteChar(char ch) => _writer.Write((ushort)ch);
        public void WriteDecimal(decimal value) => _writer.Write(value);
        public void WriteDouble(double value) => _writer.Write(value);
        public void WriteSingle(float value) => _writer.Write(value);
        public void WriteInt32(int value) => _writer.Write(value);
        public void WriteInt64(long value) => _writer.Write(value);
        public void WriteSByte(sbyte value) => _writer.Write(value);
        public void WriteInt16(short value) => _writer.Write(value);
        public void WriteUInt32(uint value) => _writer.Write(value);
        public void WriteUInt64(ulong value) => _writer.Write(value);
        public void WriteUInt16(ushort value) => _writer.Write(value);
        public void WriteString(string? value) => WriteStringValue(value);

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

        public void WriteValue(object? value)
        {
            Debug.Assert(value == null || !value.GetType().GetTypeInfo().IsEnum, "Enum should not be written with WriteValue.  Write them as ints instead.");

            if (value == null)
            {
                _writer.Write((byte)TypeCode.Null);
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
                    _writer.Write((byte)TypeCode.Float8);
                    _writer.Write((double)value);
                }
                else if (value.GetType() == typeof(bool))
                {
                    _writer.Write((byte)((bool)value ? TypeCode.Boolean_True : TypeCode.Boolean_False));
                }
                else if (value.GetType() == typeof(char))
                {
                    _writer.Write((byte)TypeCode.Char);
                    _writer.Write((ushort)(char)value);  // written as ushort because BinaryWriter fails on chars that are unicode surrogates
                }
                else if (value.GetType() == typeof(byte))
                {
                    _writer.Write((byte)TypeCode.UInt8);
                    _writer.Write((byte)value);
                }
                else if (value.GetType() == typeof(short))
                {
                    _writer.Write((byte)TypeCode.Int16);
                    _writer.Write((short)value);
                }
                else if (value.GetType() == typeof(long))
                {
                    _writer.Write((byte)TypeCode.Int64);
                    _writer.Write((long)value);
                }
                else if (value.GetType() == typeof(sbyte))
                {
                    _writer.Write((byte)TypeCode.Int8);
                    _writer.Write((sbyte)value);
                }
                else if (value.GetType() == typeof(float))
                {
                    _writer.Write((byte)TypeCode.Float4);
                    _writer.Write((float)value);
                }
                else if (value.GetType() == typeof(ushort))
                {
                    _writer.Write((byte)TypeCode.UInt16);
                    _writer.Write((ushort)value);
                }
                else if (value.GetType() == typeof(uint))
                {
                    WriteEncodedUInt32((uint)value);
                }
                else if (value.GetType() == typeof(ulong))
                {
                    _writer.Write((byte)TypeCode.UInt64);
                    _writer.Write((ulong)value);
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(value.GetType());
                }
            }
            else if (value.GetType() == typeof(decimal))
            {
                _writer.Write((byte)TypeCode.Decimal);
                _writer.Write((decimal)value);
            }
            else if (value.GetType() == typeof(DateTime))
            {
                _writer.Write((byte)TypeCode.DateTime);
                _writer.Write(((DateTime)value).ToBinary());
            }
            else if (value.GetType() == typeof(string))
            {
                WriteStringValue((string)value);
            }
            else if (type.IsArray)
            {
                var instance = (Array)value;

                if (instance.Rank > 1)
                {
                    throw new InvalidOperationException(Resources.Arrays_with_more_than_one_dimension_cannot_be_serialized);
                }

                WriteArray(instance);
            }
            else if (value is Encoding encoding)
            {
                WriteEncoding(encoding);
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
                    _writer.Write((byte)TypeCode.Array_0);
                    break;
                case 1:
                    _writer.Write((byte)TypeCode.Array_1);
                    break;
                case 2:
                    _writer.Write((byte)TypeCode.Array_2);
                    break;
                case 3:
                    _writer.Write((byte)TypeCode.Array_3);
                    break;
                default:
                    _writer.Write((byte)TypeCode.Array);
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
                _writer.Write((byte)((int)TypeCode.Int32_0 + v));
            }
            else if (v >= 0 && v < byte.MaxValue)
            {
                _writer.Write((byte)TypeCode.Int32_1Byte);
                _writer.Write((byte)v);
            }
            else if (v >= 0 && v < ushort.MaxValue)
            {
                _writer.Write((byte)TypeCode.Int32_2Bytes);
                _writer.Write((ushort)v);
            }
            else
            {
                _writer.Write((byte)TypeCode.Int32);
                _writer.Write(v);
            }
        }

        private void WriteEncodedUInt32(uint v)
        {
            if (v >= 0 && v <= 10)
            {
                _writer.Write((byte)((int)TypeCode.UInt32_0 + v));
            }
            else if (v >= 0 && v < byte.MaxValue)
            {
                _writer.Write((byte)TypeCode.UInt32_1Byte);
                _writer.Write((byte)v);
            }
            else if (v >= 0 && v < ushort.MaxValue)
            {
                _writer.Write((byte)TypeCode.UInt32_2Bytes);
                _writer.Write((ushort)v);
            }
            else
            {
                _writer.Write((byte)TypeCode.UInt32);
                _writer.Write(v);
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

            public void Add(object value, bool isReusable)
            {
                var id = _nextId++;

                if (isReusable)
                {
                    _valueToIdMap.Add(value, id);
                }
            }
        }

        internal void WriteCompressedUInt(uint value)
        {
            if (value <= (byte.MaxValue >> 2))
            {
                _writer.Write((byte)value);
            }
            else if (value <= (ushort.MaxValue >> 2))
            {
                var byte0 = (byte)(((value >> 8) & 0xFFu) | Byte2Marker);
                var byte1 = (byte)(value & 0xFFu);

                // high-bytes to low-bytes
                _writer.Write(byte0);
                _writer.Write(byte1);
            }
            else if (value <= (uint.MaxValue >> 2))
            {
                var byte0 = (byte)(((value >> 24) & 0xFFu) | Byte4Marker);
                var byte1 = (byte)((value >> 16) & 0xFFu);
                var byte2 = (byte)((value >> 8) & 0xFFu);
                var byte3 = (byte)(value & 0xFFu);

                // high-bytes to low-bytes
                _writer.Write(byte0);
                _writer.Write(byte1);
                _writer.Write(byte2);
                _writer.Write(byte3);
            }
            else
            {
                throw new ArgumentException(Resources.Value_too_large_to_be_represented_as_a_30_bit_unsigned_integer);
            }
        }

        private unsafe void WriteStringValue(string? value)
        {
            if (value == null)
            {
                _writer.Write((byte)TypeCode.Null);
            }
            else
            {
                if (_stringReferenceMap.TryGetReferenceId(value, out var id))
                {
                    Debug.Assert(id >= 0);
                    if (id <= byte.MaxValue)
                    {
                        _writer.Write((byte)TypeCode.StringRef_1Byte);
                        _writer.Write((byte)id);
                    }
                    else if (id <= ushort.MaxValue)
                    {
                        _writer.Write((byte)TypeCode.StringRef_2Bytes);
                        _writer.Write((ushort)id);
                    }
                    else
                    {
                        _writer.Write((byte)TypeCode.StringRef_4Bytes);
                        _writer.Write(id);
                    }
                }
                else
                {
                    _stringReferenceMap.Add(value, isReusable: true);

                    if (value.IsValidUnicodeString())
                    {
                        // Usual case - the string can be encoded as UTF-8:
                        // We can use the UTF-8 encoding of the binary writer.

                        _writer.Write((byte)TypeCode.StringUtf8);
                        _writer.Write(value);
                    }
                    else
                    {
                        _writer.Write((byte)TypeCode.StringUtf16);

                        // This is rare, just allocate UTF16 bytes for simplicity.
                        var bytes = new byte[(uint)value.Length * sizeof(char)];
                        fixed (char* valuePtr = value)
                        {
                            Marshal.Copy((IntPtr)valuePtr, bytes, 0, bytes.Length);
                        }

                        WriteCompressedUInt((uint)value.Length);
                        _writer.Write(bytes);
                    }
                }
            }
        }

        private void WriteArray(Array array)
        {
            var length = array.GetLength(0);

            switch (length)
            {
                case 0:
                    _writer.Write((byte)TypeCode.Array_0);
                    break;
                case 1:
                    _writer.Write((byte)TypeCode.Array_1);
                    break;
                case 2:
                    _writer.Write((byte)TypeCode.Array_2);
                    break;
                case 3:
                    _writer.Write((byte)TypeCode.Array_3);
                    break;
                default:
                    _writer.Write((byte)TypeCode.Array);
                    this.WriteCompressedUInt((uint)length);
                    break;
            }

            var elementType = array.GetType().GetElementType()!;

            if (s_typeMap.TryGetValue(elementType, out var elementKind))
            {
                this.WritePrimitiveType(elementType, elementKind);
                this.WritePrimitiveTypeArrayElements(elementType, elementKind, array);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported array element type: {elementType}");
            }
        }

        private void WriteArrayValues(Array array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                this.WriteValue(array.GetValue(i));
            }
        }

        private void WritePrimitiveTypeArrayElements(Type type, TypeCode kind, Array instance)
        {
            Debug.Assert(s_typeMap[type] == kind);

            // optimization for type underlying binary writer knows about
            if (type == typeof(byte))
            {
                _writer.Write((byte[])instance);
            }
            else if (type == typeof(char))
            {
                _writer.Write((char[])instance);
            }
            else if (type == typeof(string))
            {
                // optimization for string which object writer has
                // its own optimization to reduce repeated string
                WriteStringArrayElements((string[])instance);
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
            {
                _writer.Write(word);
            }
        }

        private void WriteStringArrayElements(string[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                WriteStringValue(array[i]);
            }
        }

        private void WriteInt8ArrayElements(sbyte[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteInt16ArrayElements(short[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteInt32ArrayElements(int[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteInt64ArrayElements(long[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteUInt16ArrayElements(ushort[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteUInt32ArrayElements(uint[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteUInt64ArrayElements(ulong[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteDecimalArrayElements(decimal[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteFloat4ArrayElements(float[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteFloat8ArrayElements(double[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WritePrimitiveType(Type type, TypeCode kind)
        {
            Debug.Assert(s_typeMap[type] == kind);
            _writer.Write((byte)kind);
        }

        public void WriteType(Type type)
        {
            _writer.Write((byte)TypeCode.Type);
            this.WriteString(type.AssemblyQualifiedName);
        }

        public void WriteEncoding(Encoding? encoding)
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
                WriteString(encoding.WebName);
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
            /// A type
            /// </summary>
            Type,

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
