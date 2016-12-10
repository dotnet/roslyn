// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
#if COMPILERCORE
    using Resources = CodeAnalysisResources;
#else
    using Resources = WorkspacesResources;
#endif

    /// <summary>
    /// An <see cref="ObjectWriter"/> that serializes objects to a byte stream.
    /// </summary>
    internal sealed partial class StreamObjectWriter : ObjectWriter, IDisposable
    {
        private readonly BinaryWriter _writer;
        private readonly ObjectBinder _binder;
        private readonly bool _recursive;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Map of serialized object's reference ids.
        /// </summary>
        private readonly ReferenceMap _referenceMap;

        /// <summary>
        /// The stack of values (object members or array elements) in order to be emitted.
        /// </summary>
        private readonly Stack<Variant> _valueStack;

        /// <summary>
        /// The list of member values written by the member writer
        /// </summary>
        private readonly List<Variant> _memberList;

        /// <summary>
        /// An <see cref="ObjectWriter"/> that is used to write object members into a list of variants.
        /// </summary>
        private readonly VariantListWriter _memberWriter;

        // collection pools to reduce GC overhead
        internal static readonly ObjectPool<List<Variant>> s_variantListPool
            = new ObjectPool<List<Variant>>(() => new List<Variant>(20));

        internal static readonly ObjectPool<Stack<Variant>> s_variantStackPool
            = new ObjectPool<Stack<Variant>>(() => new Stack<Variant>(20));

        /// <summary>
        /// Creates a new instance of a <see cref="StreamObjectWriter"/>.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="knownObjects">An optional list of objects assumed known by the corresponding <see cref="StreamObjectReader"/>.</param>
        /// <param name="binder">A binder that provides object and type encoding.</param>
        /// <param name="recursive">True if the writer encodes objects recursively.</param>
        /// <param name="cancellationToken"></param>
        public StreamObjectWriter(
            Stream stream,
            ObjectData knownObjects = null,
            ObjectBinder binder = null,
            bool recursive = true,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // String serialization assumes both reader and writer to be of the same endianness.
            // It can be adjusted for BigEndian if needed.
            Debug.Assert(BitConverter.IsLittleEndian);

            _writer = new BinaryWriter(stream, Encoding.UTF8);
            _referenceMap = new ReferenceMap(knownObjects);
            _binder = binder ?? FixedObjectBinder.Empty;
            _recursive = recursive;
            _cancellationToken = cancellationToken;

            WriteVersion();

            if (_recursive)
            {
                _writer.Write((byte)EncodingKind.Recursive);
            }
            else
            {
                _writer.Write((byte)EncodingKind.NonRecursive);
                _valueStack = s_variantStackPool.Allocate();
                _memberList = s_variantListPool.Allocate();
                _memberWriter = new VariantListWriter(_memberList);
            }
        }

        private void WriteVersion()
        {
            _writer.Write(StreamObjectReader.VersionByte1);
            _writer.Write(StreamObjectReader.VersionByte2);
        }

        public void Dispose()
        {
            _referenceMap.Dispose();

            if (!_recursive)
            {
                _memberList.Clear();
                s_variantListPool.Free(_memberList);

                _valueStack.Clear();
                s_variantStackPool.Free(_valueStack);
            }
        }

        public override void WriteBoolean(bool value)
        {
            _writer.Write(value);
        }

        public override void WriteByte(byte value)
        {
            _writer.Write(value);
        }

        public override void WriteChar(char ch)
        {
            // written as ushort because BinaryWriter fails on chars that are unicode surrogates
            _writer.Write((ushort)ch);
        }

        public override void WriteDecimal(decimal value)
        {
            _writer.Write(value);
        }

        public override void WriteDouble(double value)
        {
            _writer.Write(value);
        }

        public override void WriteSingle(float value)
        {
            _writer.Write(value);
        }

        public override void WriteInt32(int value)
        {
            _writer.Write(value);
        }

        public override void WriteInt64(long value)
        {
            _writer.Write(value);
        }

        public override void WriteSByte(sbyte value)
        {
            _writer.Write(value);
        }

        public override void WriteInt16(short value)
        {
            _writer.Write(value);
        }

        public override void WriteUInt32(uint value)
        {
            _writer.Write(value);
        }

        public override void WriteUInt64(ulong value)
        {
            _writer.Write(value);
        }

        public override void WriteUInt16(ushort value)
        {
            _writer.Write(value);
        }

        public override void WriteString(string value)
        {
            WriteStringValue(value);
        }

        public override void WriteValue(object value)
        {
            if (_recursive)
            {
                WriteVariant(Variant.FromBoxedObject(value));
            }
            else
            {
                _valueStack.Push(Variant.FromBoxedObject(value));
                Emit();
            }
        }

        private void Emit()
        {
            // emit all values on the stack
            while (_valueStack.Count > 0)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var value = _valueStack.Pop();
                WriteVariant(value);
            }
        }

        private void WriteVariant(Variant value)
        {
            switch (value.Kind)
            {
                case VariantKind.Null:
                    _writer.Write((byte)EncodingKind.Null);
                    break;

                case VariantKind.Boolean:
                    _writer.Write((byte)(value.AsBoolean() ? EncodingKind.Boolean_True : EncodingKind.Boolean_False));
                    break;

                case VariantKind.Byte:
                    _writer.Write((byte)EncodingKind.UInt8);
                    _writer.Write(value.AsByte());
                    break;

                case VariantKind.SByte:
                    _writer.Write((byte)EncodingKind.Int8);
                    _writer.Write(value.AsSByte());
                    break;

                case VariantKind.Int16:
                    _writer.Write((byte)EncodingKind.Int16);
                    _writer.Write(value.AsInt16());
                    break;

                case VariantKind.UInt16:
                    _writer.Write((byte)EncodingKind.UInt16);
                    _writer.Write(value.AsUInt16());
                    break;

                case VariantKind.Int32:
                    {
                        var v = value.AsInt32();
                        if (v >= 0 && v <= 10)
                        {
                            _writer.Write((byte)((int)EncodingKind.Int32_0 + v));
                        }
                        else if (v >= 0 && v < byte.MaxValue)
                        {
                            _writer.Write((byte)EncodingKind.Int32_1Byte);
                            _writer.Write((byte)v);
                        }
                        else if (v >= 0 && v < ushort.MaxValue)
                        {
                            _writer.Write((byte)EncodingKind.Int32_2Bytes);
                            _writer.Write((ushort)v);
                        }
                        else
                        {
                            _writer.Write((byte)EncodingKind.Int32);
                            _writer.Write(v);
                        }
                    }
                    break;

                case VariantKind.UInt32:
                    {
                        var v = value.AsUInt32();
                        if (v >= 0 && v <= 10)
                        {
                            _writer.Write((byte)((int)EncodingKind.UInt32_0 + v));
                        }
                        else if (v >= 0 && v < byte.MaxValue)
                        {
                            _writer.Write((byte)EncodingKind.UInt32_1Byte);
                            _writer.Write((byte)v);
                        }
                        else if (v >= 0 && v < ushort.MaxValue)
                        {
                            _writer.Write((byte)EncodingKind.UInt32_2Bytes);
                            _writer.Write((ushort)v);
                        }
                        else
                        {
                            _writer.Write((byte)EncodingKind.UInt32);
                            _writer.Write(v);
                        }
                    }
                    break;

                case VariantKind.Int64:
                    _writer.Write((byte)EncodingKind.Int64);
                    _writer.Write(value.AsInt64());
                    break;

                case VariantKind.UInt64:
                    _writer.Write((byte)EncodingKind.UInt64);
                    _writer.Write(value.AsUInt64());
                    break;

                case VariantKind.Decimal:
                    _writer.Write((byte)EncodingKind.Decimal);
                    _writer.Write(value.AsDecimal());
                    break;

                case VariantKind.Float4:
                    _writer.Write((byte)EncodingKind.Float4);
                    _writer.Write(value.AsSingle());
                    break;

                case VariantKind.Float8:
                    _writer.Write((byte)EncodingKind.Float8);
                    _writer.Write(value.AsDouble());
                    break;

                case VariantKind.Char:
                    _writer.Write((byte)EncodingKind.Char);
                    _writer.Write((ushort)value.AsChar());  // written as ushort because BinaryWriter fails on chars that are unicode surrogates
                    break;

                case VariantKind.String:
                    WriteStringValue(value.AsString());
                    break;

                case VariantKind.BoxedEnum:
                    var e = value.AsBoxedEnum();
                    WriteBoxedEnum(e, e.GetType());
                    break;

                case VariantKind.DateTime:
                    _writer.Write((byte)EncodingKind.DateTime);
                    _writer.Write(value.AsDateTime().ToBinary());
                    break;

                case VariantKind.Type:
                    WriteType(value.AsType());
                    break;

                case VariantKind.Array:
                    WriteArray(value.AsArray());
                    break;

                case VariantKind.Object:
                    WriteObject(value.AsObject());
                    break;
            }
        }

        /// <summary>
        /// An <see cref="ObjectWriter"/> that writes into a list of <see cref="Variant"/>.
        /// </summary>
        private class VariantListWriter : ObjectWriter
        {
            private readonly List<Variant> _list;

            public VariantListWriter(List<Variant> list)
            {
                _list = list;
            }

            public override void WriteBoolean(bool value)
            {
                _list.Add(Variant.FromBoolean(value));
            }

            public override void WriteByte(byte value)
            {
                _list.Add(Variant.FromByte(value));
            }

            public override void WriteChar(char ch)
            {
                _list.Add(Variant.FromChar(ch));
            }

            public override void WriteDecimal(decimal value)
            {
                _list.Add(Variant.FromDecimal(value));
            }

            public override void WriteDouble(double value)
            {
                _list.Add(Variant.FromDouble(value));
            }

            public override void WriteSingle(float value)
            {
                _list.Add(Variant.FromSingle(value));
            }

            public override void WriteInt32(int value)
            {
                _list.Add(Variant.FromInt32(value));
            }

            public override void WriteInt64(long value)
            {
                _list.Add(Variant.FromInt64(value));
            }

            public override void WriteSByte(sbyte value)
            {
                _list.Add(Variant.FromSByte(value));
            }

            public override void WriteInt16(short value)
            {
                _list.Add(Variant.FromInt16(value));
            }

            public override void WriteUInt32(uint value)
            {
                _list.Add(Variant.FromUInt32(value));
            }

            public override void WriteUInt64(ulong value)
            {
                _list.Add(Variant.FromUInt64(value));
            }

            public override void WriteUInt16(ushort value)
            {
                _list.Add(Variant.FromUInt16(value));
            }

            public override void WriteString(string value)
            {
                if (value == null)
                {
                    _list.Add(Variant.Null);
                }
                else
                {
                    _list.Add(Variant.FromString(value));
                }
            }

            public override void WriteValue(object value)
            {
                _list.Add(Variant.FromBoxedObject(value));
            }
        }

        /// <summary>
        /// An object reference to reference-id map, that can share base data efficiently.
        /// </summary>
        private class ReferenceMap
        {
            private readonly ImmutableDictionary<object, int> _baseMap;
            private readonly Dictionary<object, int> _valueToIdMap;
            private int _nextId;

            // note: uses value equality so strings get unified for better compaction
            private static readonly ObjectPool<Dictionary<object, int>> s_dictionaryPool =
                new ObjectPool<Dictionary<object, int>>(() => new Dictionary<object, int>(128));

            private ReferenceMap(ImmutableDictionary<object, int> baseMap)
            {
                _baseMap = baseMap;
                _valueToIdMap = s_dictionaryPool.Allocate();
                _nextId = _baseMap != null ? _baseMap.Count : 0;
            }

            public ReferenceMap(ObjectData data)
                : this(data != null ? GetBaseMap(data) : null)
            {
            }

            private static readonly ConditionalWeakTable<ObjectData, ImmutableDictionary<object, int>> s_baseDataMap
                = new ConditionalWeakTable<ObjectData, ImmutableDictionary<object, int>>();

            private static ImmutableDictionary<object, int> GetBaseMap(ObjectData data)
            {
                ImmutableDictionary<object, int> baseData;
                if (!s_baseDataMap.TryGetValue(data, out baseData))
                {
                    baseData = s_baseDataMap.GetValue(data, CreateBaseMap);
                }

                return baseData;
            }

            private static ImmutableDictionary<object, int> CreateBaseMap(ObjectData data)
            {
                var builder = ImmutableDictionary<object, int>.Empty.ToBuilder();
                for (int i = 0; i < data.Objects.Length; i++)
                {
                    builder.Add(data.Objects[i], i);
                }

                return builder.ToImmutable();
            }

            public void Dispose()
            {
                // If the map grew too big, don't return it to the pool.
                // When testing with the Roslyn solution, this dropped only 2.5% of requests.
                if (_valueToIdMap.Count > 1024)
                {
                    s_dictionaryPool.ForgetTrackedObject(_valueToIdMap);
                }
                else
                {
                    _valueToIdMap.Clear();
                    s_dictionaryPool.Free(_valueToIdMap);
                }
            }

            public bool TryGetReferenceId(object value, out int referenceId)
            {
                if (_baseMap != null && _baseMap.TryGetValue(value, out referenceId))
                {
                    return true;
                }

                return _valueToIdMap.TryGetValue(value, out referenceId);
            }

            public int Add(object value)
            {
                var id = _nextId++;
                _valueToIdMap.Add(value, id);
                return id;
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
                byte byte0 = (byte)(((value >> 8) & 0xFFu) | Byte2Marker);
                byte byte1 = (byte)(value & 0xFFu);

                // high-bytes to low-bytes
                _writer.Write(byte0);
                _writer.Write(byte1);
            }
            else if (value <= (uint.MaxValue >> 2))
            {
                byte byte0 = (byte)(((value >> 24) & 0xFFu) | Byte4Marker);
                byte byte1 = (byte)((value >> 16) & 0xFFu);
                byte byte2 = (byte)((value >> 8) & 0xFFu);
                byte byte3 = (byte)(value & 0xFFu);

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

        private unsafe void WriteStringValue(string value)
        {
            if (value == null)
            {
                _writer.Write((byte)EncodingKind.Null);
            }
            else
            {
                int id;
                if (_referenceMap.TryGetReferenceId(value, out id))
                {
                    Debug.Assert(id >= 0);
                    if (id <= byte.MaxValue)
                    {
                        _writer.Write((byte)EncodingKind.StringRef_1Byte);
                        _writer.Write((byte)id);
                    }
                    else if (id <= ushort.MaxValue)
                    {
                        _writer.Write((byte)EncodingKind.StringRef_2Bytes);
                        _writer.Write((ushort)id);
                    }
                    else
                    {
                        _writer.Write((byte)EncodingKind.StringRef_4Bytes);
                        _writer.Write(id);
                    }
                }
                else
                {
                    _referenceMap.Add(value);

                    if (value.IsValidUnicodeString())
                    {
                        // Usual case - the string can be encoded as UTF8:
                        // We can use the UTF8 encoding of the binary writer.

                        _writer.Write((byte)EncodingKind.StringUtf8);
                        _writer.Write(value);
                    }
                    else
                    {
                        _writer.Write((byte)EncodingKind.StringUtf16);

                        // This is rare, just allocate UTF16 bytes for simplicity.
                        byte[] bytes = new byte[(uint)value.Length * sizeof(char)];
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

        private void WriteBoxedEnum(object value, Type enumType)
        {
            _writer.Write((byte)EncodingKind.Enum);
            this.WriteType(enumType);

            var type = Enum.GetUnderlyingType(enumType);

            if (type == typeof(int))
            {
                _writer.Write((int)value);
            }
            else if (type == typeof(short))
            {
                _writer.Write((short)value);
            }
            else if (type == typeof(byte))
            {
                _writer.Write((byte)value);
            }
            else if (type == typeof(long))
            {
                _writer.Write((long)value);
            }
            else if (type == typeof(sbyte))
            {
                _writer.Write((sbyte)value);
            }
            else if (type == typeof(ushort))
            {
                _writer.Write((ushort)value);
            }
            else if (type == typeof(uint))
            {
                _writer.Write((uint)value);
            }
            else if (type == typeof(ulong))
            {
                _writer.Write((ulong)value);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(type);
            }
        }

        private void WriteArray(Array array)
        {
            int length = array.GetLength(0);

            switch (length)
            {
                case 0:
                    _writer.Write((byte)EncodingKind.Array_0);
                    break;
                case 1:
                    _writer.Write((byte)EncodingKind.Array_1);
                    break;
                case 2:
                    _writer.Write((byte)EncodingKind.Array_2);
                    break;
                case 3:
                    _writer.Write((byte)EncodingKind.Array_3);
                    break;
                default:
                    _writer.Write((byte)EncodingKind.Array);
                    this.WriteCompressedUInt((uint)length);
                    break;
            }

            var elementType = array.GetType().GetElementType();

            EncodingKind elementKind;
            if (s_typeMap.TryGetValue(elementType, out elementKind))
            {
                this.WritePrimitiveType(elementType, elementKind);
                this.WritePrimitiveTypeArrayElements(elementType, elementKind, array);
            }
            else
            {
                // emit header up front
                this.WriteType(elementType);

                if (_recursive)
                {
                    // recursive: write elements now
                    _recursionDepth++;
                    StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
                    if (_recursionDepth > MaxRecursionDepth)
                    {
                        throw new RecursionDepthExceeded();
                    }

                    for (int i = 0; i < array.Length; i++)
                    {
                        this.WriteValue(array.GetValue(i));
                    }

                    _recursionDepth--;
                }
                else
                {
                    // non-recursive: push elements in reverse order so we later emit first element first
                    for (int i = array.Length - 1; i >= 0; i--)
                    {
                        _valueStack.Push(Variant.FromBoxedObject(array.GetValue(i)));
                    }
                }
            }
        }

        private void WritePrimitiveTypeArrayElements(Type type, EncodingKind kind, Array instance)
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
                    case EncodingKind.Int8:
                        WriteInt8ArrayElements((sbyte[])instance);
                        return;
                    case EncodingKind.Int16:
                        WriteInt16ArrayElements((short[])instance);
                        return;
                    case EncodingKind.Int32:
                        WriteInt32ArrayElements((int[])instance);
                        return;
                    case EncodingKind.Int64:
                        WriteInt64ArrayElements((long[])instance);
                        return;
                    case EncodingKind.UInt16:
                        WriteUInt16ArrayElements((ushort[])instance);
                        return;
                    case EncodingKind.UInt32:
                        WriteUInt32ArrayElements((uint[])instance);
                        return;
                    case EncodingKind.UInt64:
                        WriteUInt64ArrayElements((ulong[])instance);
                        return;
                    case EncodingKind.Float4:
                        WriteFloat4ArrayElements((float[])instance);
                        return;
                    case EncodingKind.Float8:
                        WriteFloat8ArrayElements((double[])instance);
                        return;
                    case EncodingKind.Decimal:
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

        private void WritePrimitiveType(Type type, EncodingKind kind)
        {
            Debug.Assert(s_typeMap[type] == kind);
            _writer.Write((byte)kind);
        }

        private void WriteType(Type type)
        {
            int id;
            if (_referenceMap.TryGetReferenceId(type, out id))
            {
                Debug.Assert(id >= 0);
                if (id <= byte.MaxValue)
                {
                    _writer.Write((byte)EncodingKind.TypeRef_1Byte);
                    _writer.Write((byte)id);
                }
                else if (id <= ushort.MaxValue)
                {
                    _writer.Write((byte)EncodingKind.TypeRef_2Bytes);
                    _writer.Write((ushort)id);
                }
                else
                {
                    _writer.Write((byte)EncodingKind.TypeRef_4Bytes);
                    _writer.Write(id);
                }
            }
            else
            {
                _referenceMap.Add(type);

                _writer.Write((byte)EncodingKind.Type);

                TypeKey key;
                if (!_binder.TryGetTypeKey(type, out key))
                {
                    throw NoSerializationTypeException(type.FullName);
                }

                this.WriteStringValue(key.AssemblyName);
                this.WriteStringValue(key.TypeName);
            }
        }

        private int _recursionDepth;
        private const int MaxRecursionDepth = 50;

        internal class RecursionDepthExceeded : Exception
        {
            public RecursionDepthExceeded()
            {
            }
        }

        private void WriteObject(object instance)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            // write object ref if we already know this instance
            int id;
            if (_referenceMap.TryGetReferenceId(instance, out id))
            {
                Debug.Assert(id >= 0);
                if (id <= byte.MaxValue)
                {
                    _writer.Write((byte)EncodingKind.ObjectRef_1Byte);
                    _writer.Write((byte)id);
                }
                else if (id <= ushort.MaxValue)
                {
                    _writer.Write((byte)EncodingKind.ObjectRef_2Bytes);
                    _writer.Write((ushort)id);
                }
                else
                {
                    _writer.Write((byte)EncodingKind.ObjectRef_4Bytes);
                    _writer.Write(id);
                }
            }
            else
            {
                Action<ObjectWriter, object> typeWriter;
                if (!_binder.TryGetWriter(instance, out typeWriter))
                {
                    throw NoSerializationWriterException(instance.GetType().FullName);
                }

                if (_recursive)
                {
                    _recursionDepth++;
                    StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
                    if (_recursionDepth > MaxRecursionDepth)
                    {
                        throw new RecursionDepthExceeded();
                    }

                    // emit object header up front
                    this.WriteObjectHeader(instance, 0);

                    typeWriter(this, instance);

                    _recursionDepth--;
                }
                else
                {
                    // gather instance members by writing them into a list of variants
                    _memberList.Clear();
                    typeWriter(_memberWriter, instance);

                    // emit object header up front
                    this.WriteObjectHeader(instance, (uint)_memberList.Count);

                    // all object members are emitted as variant values (tagged in stream) so we can later read them non-recursively.
                    // push all members in reverse order so we later emit the first member written first
                    for (int i = _memberList.Count - 1; i >= 0; i--)
                    {
                        _valueStack.Push(_memberList[i]);
                    }
                }
            }
        }

        private void WriteObjectHeader(object instance, uint memberCount)
        {
            _referenceMap.Add(instance);

            _writer.Write((byte)EncodingKind.Object);

            Type type = instance.GetType();
            this.WriteType(type);

            if (!_recursive)
            {
                this.WriteCompressedUInt(memberCount);
            }
        }

        private static Exception NoSerializationTypeException(string typeName)
        {
            return new InvalidOperationException(string.Format(Resources.The_type_0_is_not_understood_by_the_serialization_binder, typeName));
        }

        private static Exception NoSerializationWriterException(string typeName)
        {
            return new InvalidOperationException(string.Format(Resources.Cannot_serialize_type_0, typeName));
        }

        // we have s_typeMap and s_reversedTypeMap since there is no bidirectional map in compiler
        internal static readonly ImmutableDictionary<Type, EncodingKind> s_typeMap = ImmutableDictionary.CreateRange<Type, EncodingKind>(
            new KeyValuePair<Type, EncodingKind>[]
            {
                KeyValuePair.Create(typeof(bool), EncodingKind.BooleanType),
                KeyValuePair.Create(typeof(char), EncodingKind.Char),
                KeyValuePair.Create(typeof(string), EncodingKind.StringType),
                KeyValuePair.Create(typeof(sbyte), EncodingKind.Int8),
                KeyValuePair.Create(typeof(short), EncodingKind.Int16),
                KeyValuePair.Create(typeof(int), EncodingKind.Int32),
                KeyValuePair.Create(typeof(long), EncodingKind.Int64),
                KeyValuePair.Create(typeof(byte), EncodingKind.UInt8),
                KeyValuePair.Create(typeof(ushort), EncodingKind.UInt16),
                KeyValuePair.Create(typeof(uint), EncodingKind.UInt32),
                KeyValuePair.Create(typeof(ulong), EncodingKind.UInt64),
                KeyValuePair.Create(typeof(float), EncodingKind.Float4),
                KeyValuePair.Create(typeof(double), EncodingKind.Float8),
                KeyValuePair.Create(typeof(decimal), EncodingKind.Decimal),
            });

        internal static readonly ImmutableDictionary<EncodingKind, Type> s_reverseTypeMap 
            = s_typeMap.ToImmutableDictionary(kv => kv.Value, kv => kv.Key);

        /// <summary>
        /// byte marker mask for encoding compressed uint 
        /// </summary>
        internal static readonly byte ByteMarkerMask = 3 << 6;

        /// <summary>
        /// byte marker bits for uint encoded in 1 byte.
        /// </summary>
        internal static readonly byte Byte1Marker = 0;

        /// <summary>
        /// byte marker bits for uint encoded in 2 bytes.
        /// </summary>
        internal static readonly byte Byte2Marker = 1 << 6;

        /// <summary>
        /// byte marker bits for uint encoded in 4 bytes.
        /// </summary>
        internal static readonly byte Byte4Marker = 2 << 6;

        /// <summary>
        /// The encoding prefix byte used when encoding <see cref="Variant"/> values.
        /// </summary>
        internal enum EncodingKind : byte
        {
            /// <summary>
            /// The stream is encoding using recursive object serialization
            /// </summary>
            Recursive,

            /// <summary>
            /// The stream is encoded using non-recursive object serialzation
            /// </summary>
            NonRecursive,

            /// <summary>
            /// The null value
            /// </summary>
            Null,

            /// <summary>
            /// A type
            /// </summary>
            Type,

            /// <summary>
            /// A type reference with the id encoded as 1 byte. 
            /// </summary>
            TypeRef_1Byte,

            /// <summary>
            /// A Type reference with the id encoded as 2 bytes.
            /// </summary>
            TypeRef_2Bytes,

            /// <summary>
            /// A type reference with the id encoded as 4 bytes.
            /// </summary>
            TypeRef_4Bytes,

            /// <summary>
            /// An object with member values encoded as variants
            /// </summary>
            Object,

            /// <summary>
            /// An object reference with the id encoded as 1 byte.
            /// </summary>
            ObjectRef_1Byte,

            /// <summary>
            /// An object reference with the id encode as 2 bytes.
            /// </summary>
            ObjectRef_2Bytes,

            /// <summary>
            /// An object reference with the id encoded as 4 bytes.
            /// </summary>
            ObjectRef_4Bytes,

            /// <summary>
            /// A string encoded as UTF8 (using BinaryWriter.Write(string))
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
            /// An enum value
            /// </summary>
            Enum,

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
            StringType
        }

        internal enum VariantKind
        {
            None = 0,
            Null,
            Boolean,
            SByte,
            Byte,
            Int16,
            UInt16,
            Int32,
            UInt32,
            Int64,
            UInt64,
            Decimal,
            Float4,
            Float8,
            Char,
            String,
            Object,
            BoxedEnum,
            DateTime,
            Array,
            Type
        }

        internal struct Variant
        {
            public readonly VariantKind Kind;
            private readonly decimal _image;
            private readonly object _instance;

            private Variant(VariantKind kind, decimal image, object instance = null)
            {
                Kind = kind;
                _image = image;
                _instance = instance;
            }

            public static readonly Variant None = new Variant(VariantKind.None, image: 0, instance: null);
            public static readonly Variant Null = new Variant(VariantKind.Null, image: 0, instance: null);

            public static Variant FromBoolean(bool value)
            {
                return new Variant(VariantKind.Boolean, value ? 1 : 0);
            }

            public static Variant FromSByte(sbyte value)
            {
                return new Variant(VariantKind.SByte, value);
            }

            public static Variant FromByte(byte value)
            {
                return new Variant(VariantKind.Byte, value);
            }

            public static Variant FromInt16(short value)
            {
                return new Variant(VariantKind.Int16, value);
            }

            public static Variant FromUInt16(ushort value)
            {
                return new Variant(VariantKind.UInt16, value);
            }

            public static Variant FromInt32(int value)
            {
                return new Variant(VariantKind.Int32, value);
            }

            public static Variant FromInt64(long value)
            {
                return new Variant(VariantKind.Int64, value);
            }

            public static Variant FromUInt32(uint value)
            {
                return new Variant(VariantKind.UInt32, value);
            }

            public static Variant FromUInt64(ulong value)
            {
                return new Variant(VariantKind.UInt64, value);
            }

            public static Variant FromSingle(float value)
            {
                return new Variant(VariantKind.Float4, BitConverter.DoubleToInt64Bits(value));
            }

            public static Variant FromDouble(double value)
            {
                return new Variant(VariantKind.Float8, BitConverter.DoubleToInt64Bits(value));
            }

            public static Variant FromChar(char value)
            {
                return new Variant(VariantKind.Char, value);
            }

            public static Variant FromString(string value)
            {
                return new Variant(VariantKind.String, image: 0, instance: value);
            }

            public static Variant FromDecimal(Decimal value)
            {
                return new Variant(VariantKind.Decimal, value);
            }

            public static Variant FromBoxedEnum(object value)
            {
                return new Variant(VariantKind.BoxedEnum, image: 0, instance: value);
            }

            public static Variant FromDateTime(DateTime value)
            {
                return new Variant(VariantKind.DateTime, image: value.ToBinary(), instance: null);
            }

            public static Variant FromType(Type value)
            {
                return new Variant(VariantKind.Type, image: 0, instance: value);
            }

            public static Variant FromArray(Array array)
            {
                return new Variant(VariantKind.Array, image: 0, instance: array);
            }

            public static Variant FromObject(object value)
            {
                return new Variant(VariantKind.Object, image: 0, instance: value);
            }

            public bool AsBoolean()
            {
                Debug.Assert(Kind == VariantKind.Boolean);
                return _image != 0;
            }

            public sbyte AsSByte()
            {
                Debug.Assert(Kind == VariantKind.SByte);
                return (sbyte)_image;
            }

            public byte AsByte()
            {
                Debug.Assert(Kind == VariantKind.Byte);
                return (byte)_image;
            }

            public short AsInt16()
            {
                Debug.Assert(Kind == VariantKind.Int16);
                return (short)_image;
            }

            public ushort AsUInt16()
            {
                Debug.Assert(Kind == VariantKind.UInt16);
                return (ushort)_image;
            }

            public int AsInt32()
            {
                Debug.Assert(Kind == VariantKind.Int32);
                return (int)_image;
            }

            public uint AsUInt32()
            {
                Debug.Assert(Kind == VariantKind.UInt32);
                return (uint)_image;
            }

            public long AsInt64()
            {
                Debug.Assert(Kind == VariantKind.Int64);
                return (long)_image;
            }

            public ulong AsUInt64()
            {
                Debug.Assert(Kind == VariantKind.UInt64);
                return (ulong)_image;
            }

            public decimal AsDecimal()
            {
                Debug.Assert(Kind == VariantKind.Decimal);
                return _image;
            }

            public float AsSingle()
            {
                Debug.Assert(Kind == VariantKind.Float4);
                return (float)BitConverter.Int64BitsToDouble((long)_image);
            }

            public double AsDouble()
            {
                Debug.Assert(Kind == VariantKind.Float8);
                return BitConverter.Int64BitsToDouble((long)_image);
            }

            public char AsChar()
            {
                Debug.Assert(Kind == VariantKind.Char);
                return (char)_image;
            }

            public string AsString()
            {
                Debug.Assert(Kind == VariantKind.String);
                return (string)_instance;
            }

            public object AsObject()
            {
                Debug.Assert(Kind == VariantKind.Object);
                return _instance;
            }

            public object AsBoxedEnum()
            {
                Debug.Assert(Kind == VariantKind.BoxedEnum);
                return _instance;
            }

            public DateTime AsDateTime()
            {
                Debug.Assert(Kind == VariantKind.DateTime);
                return DateTime.FromBinary((long)_image);
            }

            public Type AsType()
            {
                Debug.Assert(Kind == VariantKind.Type);
                return (Type)_instance;
            }

            public Array AsArray()
            {
                Debug.Assert(Kind == VariantKind.Array);
                return (Array)_instance;
            }

            public static Variant FromBoxedObject(object value)
            {
                if (value == null)
                {
                    return Variant.Null;
                }
                else
                {
                    var type = value.GetType();
                    var typeInfo = type.GetTypeInfo();

                    if (typeInfo.IsEnum)
                    {
                        return FromBoxedEnum(value);
                    }
                    else if (type == typeof(bool))
                    {
                        return FromBoolean((bool)value);
                    }
                    else if (type == typeof(int))
                    {
                        return FromInt32((int)value);
                    }
                    else if (type == typeof(string))
                    {
                        return FromString((string)value);
                    }
                    else if (type == typeof(short))
                    {
                        return FromInt16((short)value);
                    }
                    else if (type == typeof(long))
                    {
                        return FromInt64((long)value);
                    }
                    else if (type == typeof(char))
                    {
                        return FromChar((char)value);
                    }
                    else if (type == typeof(sbyte))
                    {
                        return FromSByte((sbyte)value);
                    }
                    else if (type == typeof(byte))
                    {
                        return FromByte((byte)value);
                    }
                    else if (type == typeof(ushort))
                    {
                        return FromUInt16((ushort)value);
                    }
                    else if (type == typeof(uint))
                    {
                        return FromUInt32((uint)value);
                    }
                    else if (type == typeof(ulong))
                    {
                        return FromUInt64((ulong)value);
                    }
                    else if (type == typeof(decimal))
                    {
                        return FromDecimal((decimal)value);
                    }
                    else if (type == typeof(float))
                    {
                        return FromSingle((float)value);
                    }
                    else if (type == typeof(double))
                    {
                        return FromDouble((double)value);
                    }
                    else if (type == typeof(DateTime))
                    {
                        return FromDateTime((DateTime)value);
                    }
                    else if (type.IsArray)
                    {
                        var instance = (Array)value;

                        if (instance.Rank > 1)
                        {
                            throw new InvalidOperationException(Resources.Arrays_with_more_than_one_dimension_cannot_be_serialized);
                        }

                        return Variant.FromArray(instance);
                    }
                    else if (value is Type)
                    {
                        return Variant.FromType((Type)value);
                    }
                    else
                    {
                        return Variant.FromObject(value);
                    }
                }
            }

            public object ToBoxedObject()
            {
                switch (this.Kind)
                {
                    case VariantKind.Array:
                        return this.AsArray();
                    case VariantKind.Boolean:
                        return this.AsBoolean();
                    case VariantKind.BoxedEnum:
                        return this.AsBoxedEnum();
                    case VariantKind.Byte:
                        return this.AsByte();
                    case VariantKind.Char:
                        return this.AsChar();
                    case VariantKind.DateTime:
                        return this.AsDateTime();
                    case VariantKind.Decimal:
                        return this.AsDecimal();
                    case VariantKind.Float4:
                        return this.AsSingle();
                    case VariantKind.Float8:
                        return this.AsDouble();
                    case VariantKind.Int16:
                        return this.AsInt16();
                    case VariantKind.Int32:
                        return this.AsInt32();
                    case VariantKind.Int64:
                        return this.AsInt64();
                    case VariantKind.Null:
                        return null;
                    case VariantKind.Object:
                        return this.AsObject();
                    case VariantKind.SByte:
                        return this.AsSByte();
                    case VariantKind.String:
                        return this.AsString();
                    case VariantKind.Type:
                        return this.AsType();
                    case VariantKind.UInt16:
                        return this.AsUInt16();
                    case VariantKind.UInt32:
                        return this.AsUInt32();
                    case VariantKind.UInt64:
                        return this.AsUInt64();
                    default:
                        throw ExceptionUtilities.UnexpectedValue(this.Kind);
                }
            }
        }
    }
}
