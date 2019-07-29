// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    using System.Collections.Immutable;
    using System.Threading.Tasks;
#if COMPILERCORE
    using Resources = CodeAnalysisResources;
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
        /// Map of serialized object's reference ids.  The object-reference-map uses reference equality
        /// for performance.  While the string-reference-map uses value-equality for greater cache hits 
        /// and reuse.
        /// 
        /// These are not readonly because they're structs and we mutate them.
        /// 
        /// When we write out objects/strings we give each successive, unique, item a monotonically 
        /// increasing integral ID starting at 0.  I.e. the first object gets ID-0, the next gets 
        /// ID-1 and so on and so forth.  We do *not* include these IDs with the object when it is
        /// written out.  We only include the ID if we hit the object *again* while writing.
        /// 
        /// During reading, the reader knows to give each object it reads the same monotonically 
        /// increasing integral value.  i.e. the first object it reads is put into an array at position
        /// 0, the next at position 1, and so on.  Then, when the reader reads in an object-reference
        /// it can just retrieved it directly from that array.
        /// 
        /// In other words, writing and reading take advantage of the fact that they know they will
        /// write and read objects in the exact same order.  So they only need the IDs for references
        /// and not the objects themselves because the ID is inferred from the order the object is
        /// written or read in.
        /// </summary>
        private WriterReferenceMap _objectReferenceMap;
        private WriterReferenceMap _stringReferenceMap;

        /// <summary>
        /// Copy of the global binder data that maps from Types to the appropriate reading-function
        /// for that type.  Types register functions directly with <see cref="ObjectBinder"/>, but 
        /// that means that <see cref="ObjectBinder"/> is both static and locked.  This gives us 
        /// local copy we can work with without needing to worry about anyone else mutating.
        /// </summary>
        private readonly ObjectBinderSnapshot _binderSnapshot;

        private int _recursionDepth;
        internal const int MaxRecursionDepth = 50;

        /// <summary>
        /// Creates a new instance of a <see cref="ObjectWriter"/>.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="cancellationToken"></param>
        public ObjectWriter(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            // String serialization assumes both reader and writer to be of the same endianness.
            // It can be adjusted for BigEndian if needed.
            Debug.Assert(BitConverter.IsLittleEndian);

            _writer = new BinaryWriter(stream, Encoding.UTF8);
            _objectReferenceMap = new WriterReferenceMap(valueEquality: false);
            _stringReferenceMap = new WriterReferenceMap(valueEquality: true);
            _cancellationToken = cancellationToken;

            // Capture a copy of the current static binder state.  That way we don't have to 
            // access any locks while we're doing our processing.
            _binderSnapshot = ObjectBinder.GetSnapshot();

            WriteVersion();
        }

        private void WriteVersion()
        {
            _writer.Write(ObjectReader.VersionByte1);
            _writer.Write(ObjectReader.VersionByte2);
        }

        public void Dispose()
        {
            _objectReferenceMap.Dispose();
            _stringReferenceMap.Dispose();
            _recursionDepth = 0;
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
        public void WriteString(string value) => WriteStringValue(value);

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

        public void WriteValue(object value)
        {
            Debug.Assert(value == null || !value.GetType().GetTypeInfo().IsEnum, "Enum should not be written with WriteValue.  Write them as ints instead.");

            if (value == null)
            {
                _writer.Write((byte)EncodingKind.Null);
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
                // common values of literals in code, and so would be hte likely hits if we do
                // have a primitive type we're serializing out.
                if (value.GetType() == typeof(int))
                {
                    WriteEncodedInt32((int)value);
                }
                else if (value.GetType() == typeof(double))
                {
                    _writer.Write((byte)EncodingKind.Float8);
                    _writer.Write((double)value);
                }
                else if (value.GetType() == typeof(bool))
                {
                    _writer.Write((byte)((bool)value ? EncodingKind.Boolean_True : EncodingKind.Boolean_False));
                }
                else if (value.GetType() == typeof(char))
                {
                    _writer.Write((byte)EncodingKind.Char);
                    _writer.Write((ushort)(char)value);  // written as ushort because BinaryWriter fails on chars that are unicode surrogates
                }
                else if (value.GetType() == typeof(byte))
                {
                    _writer.Write((byte)EncodingKind.UInt8);
                    _writer.Write((byte)value);
                }
                else if (value.GetType() == typeof(short))
                {
                    _writer.Write((byte)EncodingKind.Int16);
                    _writer.Write((short)value);
                }
                else if (value.GetType() == typeof(long))
                {
                    _writer.Write((byte)EncodingKind.Int64);
                    _writer.Write((long)value);
                }
                else if (value.GetType() == typeof(sbyte))
                {
                    _writer.Write((byte)EncodingKind.Int8);
                    _writer.Write((sbyte)value);
                }
                else if (value.GetType() == typeof(float))
                {
                    _writer.Write((byte)EncodingKind.Float4);
                    _writer.Write((float)value);
                }
                else if (value.GetType() == typeof(ushort))
                {
                    _writer.Write((byte)EncodingKind.UInt16);
                    _writer.Write((ushort)value);
                }
                else if (value.GetType() == typeof(uint))
                {
                    WriteEncodedUInt32((uint)value);
                }
                else if (value.GetType() == typeof(ulong))
                {
                    _writer.Write((byte)EncodingKind.UInt64);
                    _writer.Write((ulong)value);
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(value.GetType());
                }
            }
            else if (value.GetType() == typeof(decimal))
            {
                _writer.Write((byte)EncodingKind.Decimal);
                _writer.Write((decimal)value);
            }
            else if (value.GetType() == typeof(DateTime))
            {
                _writer.Write((byte)EncodingKind.DateTime);
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
            else
            {
                WriteObject(instance: value, instanceAsWritableOpt: null);
            }
        }

        public void WriteValue(IObjectWritable value)
        {
            if (value == null)
            {
                _writer.Write((byte)EncodingKind.Null);
                return;
            }

            WriteObject(instance: value, instanceAsWritableOpt: value);
        }

        private void WriteEncodedInt32(int v)
        {
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

        private void WriteEncodedUInt32(uint v)
        {
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

        /// <summary>
        /// An object reference to reference-id map, that can share base data efficiently.
        /// </summary>
        private struct WriterReferenceMap
        {
            private readonly Dictionary<object, int> _valueToIdMap;
            private readonly bool _valueEquality;
            private int _nextId;

            private static readonly ObjectPool<Dictionary<object, int>> s_referenceDictionaryPool =
                new ObjectPool<Dictionary<object, int>>(() => new Dictionary<object, int>(128, ReferenceEqualityComparer.Instance));

            private static readonly ObjectPool<Dictionary<object, int>> s_valueDictionaryPool =
                new ObjectPool<Dictionary<object, int>>(() => new Dictionary<object, int>(128));

            public WriterReferenceMap(bool valueEquality)
            {
                _valueEquality = valueEquality;
                _valueToIdMap = GetDictionaryPool(valueEquality).Allocate();
                _nextId = 0;
            }

            private static ObjectPool<Dictionary<object, int>> GetDictionaryPool(bool valueEquality)
                => valueEquality ? s_valueDictionaryPool : s_referenceDictionaryPool;

            public void Dispose()
            {
                var pool = GetDictionaryPool(_valueEquality);

                // If the map grew too big, don't return it to the pool.
                // When testing with the Roslyn solution, this dropped only 2.5% of requests.
                if (_valueToIdMap.Count > 1024)
                {
                    pool.ForgetTrackedObject(_valueToIdMap);
                }
                else
                {
                    _valueToIdMap.Clear();
                    pool.Free(_valueToIdMap);
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
                if (_stringReferenceMap.TryGetReferenceId(value, out int id))
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
                    _stringReferenceMap.Add(value, isReusable: true);

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

            if (s_typeMap.TryGetValue(elementType, out var elementKind))
            {
                this.WritePrimitiveType(elementType, elementKind);
                this.WritePrimitiveTypeArrayElements(elementType, elementKind, array);
            }
            else
            {
                // emit header up front
                this.WriteKnownType(elementType);

                // recursive: write elements now
                var oldDepth = _recursionDepth;
                _recursionDepth++;

                if (_recursionDepth % MaxRecursionDepth == 0)
                {
                    // If we're recursing too deep, move the work to another thread to do so we
                    // don't blow the stack.  'LongRunning' ensures that we get a dedicated thread
                    // to do this work.  That way we don't end up blocking the threadpool.
                    var task = Task.Factory.StartNew(
                        a => WriteArrayValues((Array)a),
                        array,
                        _cancellationToken,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default);

                    // We must not proceed until the additional task completes. After returning from a write, the underlying
                    // stream providing access to raw memory will be closed; if this occurs before the separate thread
                    // completes its write then an access violation can occur attempting to write to unmapped memory.
                    //
                    // CANCELLATION: If cancellation is required, DO NOT attempt to cancel the operation by cancelling this
                    // wait. Cancellation must only be implemented by modifying 'task' to cancel itself in a timely manner
                    // so the wait can complete.
                    task.GetAwaiter().GetResult();
                }
                else
                {
                    WriteArrayValues(array);
                }

                _recursionDepth--;
                Debug.Assert(_recursionDepth == oldDepth);
            }
        }

        private void WriteArrayValues(Array array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                this.WriteValue(array.GetValue(i));
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

        public void WriteType(Type type)
        {
            _writer.Write((byte)EncodingKind.Type);
            this.WriteString(type.AssemblyQualifiedName);
        }

        private void WriteKnownType(Type type)
        {
            _writer.Write((byte)EncodingKind.Type);
            this.WriteInt32(_binderSnapshot.GetTypeId(type));
        }

        private void WriteObject(object instance, IObjectWritable instanceAsWritableOpt)
        {
            Debug.Assert(instance != null);
            Debug.Assert(instanceAsWritableOpt == null || instance == instanceAsWritableOpt);

            _cancellationToken.ThrowIfCancellationRequested();

            // write object ref if we already know this instance
            if (_objectReferenceMap.TryGetReferenceId(instance, out var id))
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
                var writable = instanceAsWritableOpt;
                if (writable == null)
                {
                    writable = instance as IObjectWritable;
                    if (writable == null)
                    {
                        throw NoSerializationWriterException($"{instance.GetType()} must implement {nameof(IObjectWritable)}");
                    }
                }

                var oldDepth = _recursionDepth;
                _recursionDepth++;

                if (_recursionDepth % MaxRecursionDepth == 0)
                {
                    // If we're recursing too deep, move the work to another thread to do so we
                    // don't blow the stack.  'LongRunning' ensures that we get a dedicated thread
                    // to do this work.  That way we don't end up blocking the threadpool.
                    var task = Task.Factory.StartNew(
                        obj => WriteObjectWorker((IObjectWritable)obj),
                        writable,
                        _cancellationToken,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default);

                    // We must not proceed until the additional task completes. After returning from a write, the underlying
                    // stream providing access to raw memory will be closed; if this occurs before the separate thread
                    // completes its write then an access violation can occur attempting to write to unmapped memory.
                    //
                    // CANCELLATION: If cancellation is required, DO NOT attempt to cancel the operation by cancelling this
                    // wait. Cancellation must only be implemented by modifying 'task' to cancel itself in a timely manner
                    // so the wait can complete.
                    task.GetAwaiter().GetResult();
                }
                else
                {
                    WriteObjectWorker(writable);
                }

                _recursionDepth--;
                Debug.Assert(_recursionDepth == oldDepth);
            }
        }

        private void WriteObjectWorker(IObjectWritable writable)
        {
            _objectReferenceMap.Add(writable, writable.ShouldReuseInSerialization);

            // emit object header up front
            _writer.Write((byte)EncodingKind.Object);

            // Directly write out the type-id for this object.  i.e. no need to write out the 'Type'
            // tag since we just wrote out the 'Object' tag
            this.WriteInt32(_binderSnapshot.GetTypeId(writable.GetType()));
            writable.WriteTo(this);
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
        // Note: s_typeMap is effectively immutable.  However, for maxiumum perf we use mutable types because
        // they are used in hotspots.
        internal static readonly Dictionary<Type, EncodingKind> s_typeMap;

        /// <summary>
        /// Indexed by EncodingKind.
        /// </summary>
        internal static readonly ImmutableArray<Type> s_reverseTypeMap;

        static ObjectWriter()
        {
            s_typeMap = new Dictionary<Type, EncodingKind>
            {
                { typeof(bool), EncodingKind.BooleanType },
                { typeof(char), EncodingKind.Char },
                { typeof(string), EncodingKind.StringType },
                { typeof(sbyte), EncodingKind.Int8 },
                { typeof(short), EncodingKind.Int16 },
                { typeof(int), EncodingKind.Int32 },
                { typeof(long), EncodingKind.Int64 },
                { typeof(byte), EncodingKind.UInt8 },
                { typeof(ushort), EncodingKind.UInt16 },
                { typeof(uint), EncodingKind.UInt32 },
                { typeof(ulong), EncodingKind.UInt64 },
                { typeof(float), EncodingKind.Float4 },
                { typeof(double), EncodingKind.Float8 },
                { typeof(decimal), EncodingKind.Decimal },
            };

            var temp = new Type[(int)EncodingKind.Last];

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

        internal enum EncodingKind : byte
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


            Last = StringType + 1,
        }
    }
}
