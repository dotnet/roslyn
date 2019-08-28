// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
#if COMPILERCORE
    using Resources = CodeAnalysisResources;
#else
    using Resources = WorkspacesResources;
#endif

    using EncodingKind = ObjectWriter.EncodingKind;

    /// <summary>
    /// An <see cref="ObjectReader"/> that deserializes objects from a byte stream.
    /// </summary>
    internal sealed partial class ObjectReader : IDisposable
    {
        /// <summary>
        /// We start the version at something reasonably random.  That way an older file, with 
        /// some random start-bytes, has little chance of matching our version.  When incrementing
        /// this version, just change VersionByte2.
        /// </summary>
        internal const byte VersionByte1 = 0b10101010;
        internal const byte VersionByte2 = 0b00001001;

        private readonly BinaryReader _reader;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Map of reference id's to deserialized objects.
        ///
        /// These are not readonly because they're structs and we mutate them.
        /// </summary>
        private ReaderReferenceMap<object> _objectReferenceMap;
        private ReaderReferenceMap<string> _stringReferenceMap;

        /// <summary>
        /// Copy of the global binder data that maps from Types to the appropriate reading-function
        /// for that type.  Types register functions directly with <see cref="ObjectBinder"/>, but 
        /// that means that <see cref="ObjectBinder"/> is both static and locked.  This gives us 
        /// local copy we can work with without needing to worry about anyone else mutating.
        /// </summary>
        private readonly ObjectBinderSnapshot _binderSnapshot;

        private int _recursionDepth;

        /// <summary>
        /// Creates a new instance of a <see cref="ObjectReader"/>.
        /// </summary>
        /// <param name="stream">The stream to read objects from.</param>
        /// <param name="cancellationToken"></param>
        private ObjectReader(
            Stream stream,
            CancellationToken cancellationToken)
        {
            // String serialization assumes both reader and writer to be of the same endianness.
            // It can be adjusted for BigEndian if needed.
            Debug.Assert(BitConverter.IsLittleEndian);

            _reader = new BinaryReader(stream, Encoding.UTF8);
            _objectReferenceMap = ReaderReferenceMap<object>.Create();
            _stringReferenceMap = ReaderReferenceMap<string>.Create();

            // Capture a copy of the current static binder state.  That way we don't have to 
            // access any locks while we're doing our processing.
            _binderSnapshot = ObjectBinder.GetSnapshot();

            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Attempts to create a <see cref="ObjectReader"/> from the provided <paramref name="stream"/>.
        /// If the <paramref name="stream"/> does not start with a valid header, then <see langword="null"/> will
        /// be returned.
        /// </summary>
        public static ObjectReader TryGetReader(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            if (stream == null)
            {
                return null;
            }

            if (stream.ReadByte() != VersionByte1 ||
                stream.ReadByte() != VersionByte2)
            {
                return null;
            }

            return new ObjectReader(stream, cancellationToken);
        }

        public void Dispose()
        {
            _objectReferenceMap.Dispose();
            _stringReferenceMap.Dispose();
            _recursionDepth = 0;
        }

        public bool ReadBoolean() => _reader.ReadBoolean();
        public byte ReadByte() => _reader.ReadByte();
        // read as ushort because BinaryWriter fails on chars that are unicode surrogates
        public char ReadChar() => (char)_reader.ReadUInt16();
        public decimal ReadDecimal() => _reader.ReadDecimal();
        public double ReadDouble() => _reader.ReadDouble();
        public float ReadSingle() => _reader.ReadSingle();
        public int ReadInt32() => _reader.ReadInt32();
        public long ReadInt64() => _reader.ReadInt64();
        public sbyte ReadSByte() => _reader.ReadSByte();
        public short ReadInt16() => _reader.ReadInt16();
        public uint ReadUInt32() => _reader.ReadUInt32();
        public ulong ReadUInt64() => _reader.ReadUInt64();
        public ushort ReadUInt16() => _reader.ReadUInt16();
        public string ReadString() => ReadStringValue();

        public Guid ReadGuid()
        {
            var accessor = new ObjectWriter.GuidAccessor
            {
                Low64 = ReadInt64(),
                High64 = ReadInt64()
            };

            return accessor.Guid;
        }

        public object ReadValue()
        {
            var oldDepth = _recursionDepth;
            _recursionDepth++;

            object value;
            if (_recursionDepth % ObjectWriter.MaxRecursionDepth == 0)
            {
                // If we're recursing too deep, move the work to another thread to do so we
                // don't blow the stack.
                var task = Task.Factory.StartNew(
                    () => ReadValueWorker(),
                    _cancellationToken,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);

                // We must not proceed until the additional task completes. After returning from a read, the underlying
                // stream providing access to raw memory will be closed; if this occurs before the separate thread
                // completes its read then an access violation can occur attempting to read from unmapped memory.
                //
                // CANCELLATION: If cancellation is required, DO NOT attempt to cancel the operation by cancelling this
                // wait. Cancellation must only be implemented by modifying 'task' to cancel itself in a timely manner
                // so the wait can complete.
                value = task.GetAwaiter().GetResult();
            }
            else
            {
                value = ReadValueWorker();
            }

            _recursionDepth--;
            Debug.Assert(oldDepth == _recursionDepth);

            return value;
        }

        private object ReadValueWorker()
        {
            var kind = (EncodingKind)_reader.ReadByte();
            switch (kind)
            {
                case EncodingKind.Null: return null;
                case EncodingKind.Boolean_True: return true;
                case EncodingKind.Boolean_False: return false;
                case EncodingKind.Int8: return _reader.ReadSByte();
                case EncodingKind.UInt8: return _reader.ReadByte();
                case EncodingKind.Int16: return _reader.ReadInt16();
                case EncodingKind.UInt16: return _reader.ReadUInt16();
                case EncodingKind.Int32: return _reader.ReadInt32();
                case EncodingKind.Int32_1Byte: return (int)_reader.ReadByte();
                case EncodingKind.Int32_2Bytes: return (int)_reader.ReadUInt16();
                case EncodingKind.Int32_0:
                case EncodingKind.Int32_1:
                case EncodingKind.Int32_2:
                case EncodingKind.Int32_3:
                case EncodingKind.Int32_4:
                case EncodingKind.Int32_5:
                case EncodingKind.Int32_6:
                case EncodingKind.Int32_7:
                case EncodingKind.Int32_8:
                case EncodingKind.Int32_9:
                case EncodingKind.Int32_10:
                    return (int)kind - (int)EncodingKind.Int32_0;
                case EncodingKind.UInt32: return _reader.ReadUInt32();
                case EncodingKind.UInt32_1Byte: return (uint)_reader.ReadByte();
                case EncodingKind.UInt32_2Bytes: return (uint)_reader.ReadUInt16();
                case EncodingKind.UInt32_0:
                case EncodingKind.UInt32_1:
                case EncodingKind.UInt32_2:
                case EncodingKind.UInt32_3:
                case EncodingKind.UInt32_4:
                case EncodingKind.UInt32_5:
                case EncodingKind.UInt32_6:
                case EncodingKind.UInt32_7:
                case EncodingKind.UInt32_8:
                case EncodingKind.UInt32_9:
                case EncodingKind.UInt32_10:
                    return (uint)((int)kind - (int)EncodingKind.UInt32_0);
                case EncodingKind.Int64: return _reader.ReadInt64();
                case EncodingKind.UInt64: return _reader.ReadUInt64();
                case EncodingKind.Float4: return _reader.ReadSingle();
                case EncodingKind.Float8: return _reader.ReadDouble();
                case EncodingKind.Decimal: return _reader.ReadDecimal();
                case EncodingKind.Char:
                    // read as ushort because BinaryWriter fails on chars that are unicode surrogates
                    return (char)_reader.ReadUInt16();
                case EncodingKind.StringUtf8:
                case EncodingKind.StringUtf16:
                case EncodingKind.StringRef_4Bytes:
                case EncodingKind.StringRef_1Byte:
                case EncodingKind.StringRef_2Bytes:
                    return ReadStringValue(kind);
                case EncodingKind.ObjectRef_4Bytes: return _objectReferenceMap.GetValue(_reader.ReadInt32());
                case EncodingKind.ObjectRef_1Byte: return _objectReferenceMap.GetValue(_reader.ReadByte());
                case EncodingKind.ObjectRef_2Bytes: return _objectReferenceMap.GetValue(_reader.ReadUInt16());
                case EncodingKind.Object: return ReadObject();
                case EncodingKind.DateTime: return DateTime.FromBinary(_reader.ReadInt64());
                case EncodingKind.Array:
                case EncodingKind.Array_0:
                case EncodingKind.Array_1:
                case EncodingKind.Array_2:
                case EncodingKind.Array_3:
                    return ReadArray(kind);
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        /// <summary>
        /// An reference-id to object map, that can share base data efficiently.
        /// </summary>
        private struct ReaderReferenceMap<T> where T : class
        {
            private readonly List<T> _values;

            internal static readonly ObjectPool<List<T>> s_objectListPool
                = new ObjectPool<List<T>>(() => new List<T>(20));

            private ReaderReferenceMap(List<T> values)
                => _values = values;

            public static ReaderReferenceMap<T> Create()
                => new ReaderReferenceMap<T>(s_objectListPool.Allocate());

            public void Dispose()
            {
                _values.Clear();
                s_objectListPool.Free(_values);
            }


            public int GetNextObjectId()
            {
                var id = _values.Count;
                _values.Add(null);
                return id;
            }

            public void AddValue(T value)
                => _values.Add(value);

            public void AddValue(int index, T value)
                => _values[index] = value;

            public T GetValue(int referenceId)
                => _values[referenceId];
        }

        internal uint ReadCompressedUInt()
        {
            var info = _reader.ReadByte();
            byte marker = (byte)(info & ObjectWriter.ByteMarkerMask);
            byte byte0 = (byte)(info & ~ObjectWriter.ByteMarkerMask);

            if (marker == ObjectWriter.Byte1Marker)
            {
                return byte0;
            }

            if (marker == ObjectWriter.Byte2Marker)
            {
                var byte1 = _reader.ReadByte();
                return (((uint)byte0) << 8) | byte1;
            }

            if (marker == ObjectWriter.Byte4Marker)
            {
                var byte1 = _reader.ReadByte();
                var byte2 = _reader.ReadByte();
                var byte3 = _reader.ReadByte();

                return (((uint)byte0) << 24) | (((uint)byte1) << 16) | (((uint)byte2) << 8) | byte3;
            }

            throw ExceptionUtilities.UnexpectedValue(marker);
        }

        private string ReadStringValue()
        {
            var kind = (EncodingKind)_reader.ReadByte();
            return kind == EncodingKind.Null ? null : ReadStringValue(kind);
        }

        private string ReadStringValue(EncodingKind kind)
        {
            switch (kind)
            {
                case EncodingKind.StringRef_1Byte:
                    return _stringReferenceMap.GetValue(_reader.ReadByte());

                case EncodingKind.StringRef_2Bytes:
                    return _stringReferenceMap.GetValue(_reader.ReadUInt16());

                case EncodingKind.StringRef_4Bytes:
                    return _stringReferenceMap.GetValue(_reader.ReadInt32());

                case EncodingKind.StringUtf16:
                case EncodingKind.StringUtf8:
                    return ReadStringLiteral(kind);

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private unsafe string ReadStringLiteral(EncodingKind kind)
        {
            string value;
            if (kind == EncodingKind.StringUtf8)
            {
                value = _reader.ReadString();
            }
            else
            {
                // This is rare, just allocate UTF16 bytes for simplicity.
                int characterCount = (int)ReadCompressedUInt();
                byte[] bytes = _reader.ReadBytes(characterCount * sizeof(char));
                fixed (byte* bytesPtr = bytes)
                {
                    value = new string((char*)bytesPtr, 0, characterCount);
                }
            }

            _stringReferenceMap.AddValue(value);
            return value;
        }

        private Array ReadArray(EncodingKind kind)
        {
            int length;
            switch (kind)
            {
                case EncodingKind.Array_0:
                    length = 0;
                    break;
                case EncodingKind.Array_1:
                    length = 1;
                    break;
                case EncodingKind.Array_2:
                    length = 2;
                    break;
                case EncodingKind.Array_3:
                    length = 3;
                    break;
                default:
                    length = (int)this.ReadCompressedUInt();
                    break;
            }

            // SUBTLE: If it was a primitive array, only the EncodingKind byte of the element type was written, instead of encoding as a type.
            var elementKind = (EncodingKind)_reader.ReadByte();

            var elementType = ObjectWriter.s_reverseTypeMap[(int)elementKind];
            if (elementType != null)
            {
                return this.ReadPrimitiveTypeArrayElements(elementType, elementKind, length);
            }
            else
            {
                // custom type case
                elementType = this.ReadTypeAfterTag();

                // recursive: create instance and read elements next in stream
                Array array = Array.CreateInstance(elementType, length);

                for (int i = 0; i < length; ++i)
                {
                    var value = this.ReadValue();
                    array.SetValue(value, i);
                }

                return array;
            }
        }

        private Array ReadPrimitiveTypeArrayElements(Type type, EncodingKind kind, int length)
        {
            Debug.Assert(ObjectWriter.s_reverseTypeMap[(int)kind] == type);

            // optimizations for supported array type by binary reader
            if (type == typeof(byte)) { return _reader.ReadBytes(length); }
            if (type == typeof(char)) { return _reader.ReadChars(length); }

            // optimizations for string where object reader/writer has its own mechanism to
            // reduce duplicated strings
            if (type == typeof(string)) { return ReadStringArrayElements(CreateArray<string>(length)); }
            if (type == typeof(bool)) { return ReadBooleanArrayElements(CreateArray<bool>(length)); }

            // otherwise, read elements directly from underlying binary writer
            switch (kind)
            {
                case EncodingKind.Int8: return ReadInt8ArrayElements(CreateArray<sbyte>(length));
                case EncodingKind.Int16: return ReadInt16ArrayElements(CreateArray<short>(length));
                case EncodingKind.Int32: return ReadInt32ArrayElements(CreateArray<int>(length));
                case EncodingKind.Int64: return ReadInt64ArrayElements(CreateArray<long>(length));
                case EncodingKind.UInt16: return ReadUInt16ArrayElements(CreateArray<ushort>(length));
                case EncodingKind.UInt32: return ReadUInt32ArrayElements(CreateArray<uint>(length));
                case EncodingKind.UInt64: return ReadUInt64ArrayElements(CreateArray<ulong>(length));
                case EncodingKind.Float4: return ReadFloat4ArrayElements(CreateArray<float>(length));
                case EncodingKind.Float8: return ReadFloat8ArrayElements(CreateArray<double>(length));
                case EncodingKind.Decimal: return ReadDecimalArrayElements(CreateArray<decimal>(length));
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private bool[] ReadBooleanArrayElements(bool[] array)
        {
            // Confirm the type to be read below is ulong
            Debug.Assert(BitVector.BitsPerWord == 64);

            var wordLength = BitVector.WordsRequired(array.Length);

            var count = 0;
            for (var i = 0; i < wordLength; i++)
            {
                var word = _reader.ReadUInt64();

                for (var p = 0; p < BitVector.BitsPerWord; p++)
                {
                    if (count >= array.Length)
                    {
                        return array;
                    }

                    array[count++] = BitVector.IsTrue(word, p);
                }
            }

            return array;
        }

        private static T[] CreateArray<T>(int length)
        {
            if (length == 0)
            {
                // quick check
                return Array.Empty<T>();
            }
            else
            {
                return new T[length];
            }
        }

        private string[] ReadStringArrayElements(string[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = this.ReadStringValue();
            }

            return array;
        }

        private sbyte[] ReadInt8ArrayElements(sbyte[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadSByte();
            }

            return array;
        }

        private short[] ReadInt16ArrayElements(short[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadInt16();
            }

            return array;
        }

        private int[] ReadInt32ArrayElements(int[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadInt32();
            }

            return array;
        }

        private long[] ReadInt64ArrayElements(long[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadInt64();
            }

            return array;
        }

        private ushort[] ReadUInt16ArrayElements(ushort[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadUInt16();
            }

            return array;
        }

        private uint[] ReadUInt32ArrayElements(uint[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadUInt32();
            }

            return array;
        }

        private ulong[] ReadUInt64ArrayElements(ulong[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadUInt64();
            }

            return array;
        }

        private decimal[] ReadDecimalArrayElements(decimal[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadDecimal();
            }

            return array;
        }

        private float[] ReadFloat4ArrayElements(float[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadSingle();
            }

            return array;
        }

        private double[] ReadFloat8ArrayElements(double[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadDouble();
            }

            return array;
        }

        public Type ReadType()
        {
            _reader.ReadByte();
            return Type.GetType(ReadString());
        }

        private Type ReadTypeAfterTag()
            => _binderSnapshot.GetTypeFromId(this.ReadInt32());

        private object ReadObject()
        {
            var objectId = _objectReferenceMap.GetNextObjectId();

            // reading an object may recurse.  So we need to grab our ID up front as we'll
            // end up making our sub-objects before we make this object.

            var typeReader = _binderSnapshot.GetTypeReaderFromId(this.ReadInt32());

            // recursive: read and construct instance immediately from member elements encoding next in the stream
            var instance = typeReader(this);

            if (instance.ShouldReuseInSerialization)
            {
                _objectReferenceMap.AddValue(objectId, instance);
            }

            return instance;
        }

        private static Exception DeserializationReadIncorrectNumberOfValuesException(string typeName)
        {
            throw new InvalidOperationException(String.Format(Resources.Deserialization_reader_for_0_read_incorrect_number_of_values, typeName));
        }

        private static Exception NoSerializationTypeException(string typeName)
        {
            return new InvalidOperationException(string.Format(Resources.The_type_0_is_not_understood_by_the_serialization_binder, typeName));
        }

        private static Exception NoSerializationReaderException(string typeName)
        {
            return new InvalidOperationException(string.Format(Resources.Cannot_serialize_type_0, typeName));
        }
    }
}
