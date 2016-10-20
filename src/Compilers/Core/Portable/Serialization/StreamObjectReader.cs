// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Threading;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A class that deserializes objects from a stream.
    /// </summary>
    internal sealed partial class StreamObjectReader : ObjectReader, IDisposable
    {
        private readonly BinaryReader _reader;
        private readonly ReaderData _dataMap;
        private readonly ObjectBinder _binder;
        private readonly CancellationToken _cancellationToken;
        private readonly Stack<Variant> _valueStack;
        private readonly Stack<Consumer> _consumerStack;
        private readonly VariantReader _variantReader;

        public StreamObjectReader(
            Stream stream,
            ObjectData data = null,
            ObjectBinder binder = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // String serialization assumes both reader and writer to be of the same endianness.
            // It can be adjusted for BigEndian if needed.
            Debug.Assert(BitConverter.IsLittleEndian);

            _reader = new BinaryReader(stream, Encoding.UTF8);
            _dataMap = ReaderData.Create(data);
            _binder = binder;
            _cancellationToken = cancellationToken;
            _valueStack = new Stack<Variant>();
            _consumerStack = new Stack<Consumer>();
            _variantReader = new VariantReader();
        }

        public void Dispose()
        {
            _dataMap.Dispose();
        }

        public override bool ReadBoolean()
        {
            return _reader.ReadBoolean();
        }

        public override byte ReadByte()
        {
            return _reader.ReadByte();
        }

        public override char ReadChar()
        {
            // read as ushort because writer fails on chars that are unicode surrogates
            return (char)_reader.ReadUInt16();
        }

        public override decimal ReadDecimal()
        {
            return _reader.ReadDecimal();
        }

        public override double ReadDouble()
        {
            return _reader.ReadDouble();
        }

        public override float ReadSingle()
        {
            return _reader.ReadSingle();
        }

        public override int ReadInt32()
        {
            return _reader.ReadInt32();
        }

        public override long ReadInt64()
        {
            return _reader.ReadInt64();
        }

        public override sbyte ReadSByte()
        {
            return _reader.ReadSByte();
        }

        public override short ReadInt16()
        {
            return _reader.ReadInt16();
        }

        public override uint ReadUInt32()
        {
            return _reader.ReadUInt32();
        }

        public override ulong ReadUInt64()
        {
            return _reader.ReadUInt64();
        }

        public override ushort ReadUInt16()
        {
            return _reader.ReadUInt16();
        }

        public override DateTime ReadDateTime()
        {
            return DateTime.FromBinary(_reader.ReadInt64());
        }

        public override string ReadString()
        {
            return ReadStringValue();
        }

        public override object ReadValue()
        {
            var v = ReadVariant();

            // if we didn't get anything, it must have been an object or array header
            if (v.Kind == VariantKind.None)
            {
                v = ConsumeAndConstruct();
            }

            return v.ToBoxedObject();
        }

        private Variant ConsumeAndConstruct()
        {
            Debug.Assert(_consumerStack.Count > 0);

            // keep reading until we've got all the elements to construct the object or array
            while (_consumerStack.Count > 0)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var consumer = _consumerStack.Peek();
                if (consumer.ElementCount > 0 && _valueStack.Count < consumer.StackStart + consumer.ElementCount)
                {
                    var element = ReadVariant();
                    if (element.Kind != VariantKind.None)
                    {
                        _valueStack.Push(element);
                    }
                }
                else
                {
                    consumer = _consumerStack.Pop();

                    var constructed = (consumer.Reader != null)
                        ? ConstructObject(consumer.Type, consumer.ElementCount, consumer.Reader, consumer.Id)
                        : ConstructArray(consumer.Type, consumer.ElementCount);

                    _valueStack.Push(constructed);
                }
            }

            Debug.Assert(_valueStack.Count == 1);
            return _valueStack.Pop();
        }

        private struct Consumer
        {
            public readonly Type Type;
            public readonly int ElementCount;
            public readonly int StackStart;
            public readonly Func<ObjectReader, object> Reader;
            public readonly int Id;

            public Consumer(Type type, int elementCount, int stackStart, Func<ObjectReader, object> reader, int id)
            {
                this.Type = type;
                this.ElementCount = elementCount;
                this.StackStart = stackStart;
                this.Reader = reader;
                this.Id = id;
            }
        }

        private Variant ReadVariant()
        {
            var kind = (DataKind)_reader.ReadByte();
            switch (kind)
            {
                case DataKind.Null:
                    return Variant.Null;
                case DataKind.Boolean_T:
                    return Variant.FromBoolean(true);
                case DataKind.Boolean_F:
                    return Variant.FromBoolean(false);
                case DataKind.Int8:
                    return Variant.FromSByte(_reader.ReadSByte());
                case DataKind.UInt8:
                    return Variant.FromByte(_reader.ReadByte());
                case DataKind.Int16:
                    return Variant.FromInt16(_reader.ReadInt16());
                case DataKind.UInt16:
                    return Variant.FromUInt16(_reader.ReadUInt16());
                case DataKind.Int32:
                    return Variant.FromInt32(_reader.ReadInt32());
                case DataKind.Int32_B:
                    return Variant.FromInt32((int)_reader.ReadByte());
                case DataKind.Int32_S:
                    return Variant.FromInt32((int)_reader.ReadUInt16());
                case DataKind.Int32_0:
                case DataKind.Int32_1:
                case DataKind.Int32_2:
                case DataKind.Int32_3:
                case DataKind.Int32_4:
                case DataKind.Int32_5:
                case DataKind.Int32_6:
                case DataKind.Int32_7:
                case DataKind.Int32_8:
                case DataKind.Int32_9:
                case DataKind.Int32_10:
                    return Variant.FromInt32((int)kind - (int)DataKind.Int32_0);
                case DataKind.UInt32:
                    return Variant.FromUInt32(_reader.ReadUInt32());
                case DataKind.UInt32_B:
                    return Variant.FromUInt32((uint)_reader.ReadByte());
                case DataKind.UInt32_S:
                    return Variant.FromUInt32((uint)_reader.ReadUInt16());
                case DataKind.UInt32_0:
                case DataKind.UInt32_1:
                case DataKind.UInt32_2:
                case DataKind.UInt32_3:
                case DataKind.UInt32_4:
                case DataKind.UInt32_5:
                case DataKind.UInt32_6:
                case DataKind.UInt32_7:
                case DataKind.UInt32_8:
                case DataKind.UInt32_9:
                case DataKind.UInt32_10:
                    return Variant.FromUInt32((uint)((int)kind - (int)DataKind.UInt32_0));
                case DataKind.Int64:
                    return Variant.FromInt64(_reader.ReadInt64());
                case DataKind.UInt64:
                    return Variant.FromUInt64(_reader.ReadUInt64());
                case DataKind.Float4:
                    return Variant.FromSingle(_reader.ReadSingle());
                case DataKind.Float8:
                    return Variant.FromDouble(_reader.ReadDouble());
                case DataKind.Decimal:
                    return Variant.FromDecimal(_reader.ReadDecimal());
                case DataKind.DateTime:
                    return Variant.FromDateTime(DateTime.FromBinary(_reader.ReadInt64()));
                case DataKind.Char:
                    // read as ushort because writer fails on chars that are unicode surrogates
                    return Variant.FromChar((char)_reader.ReadUInt16());
                case DataKind.StringUtf8:
                case DataKind.StringUtf16:
                case DataKind.StringRef:
                case DataKind.StringRef_B:
                case DataKind.StringRef_S:
                    return Variant.FromString(ReadStringValue(kind));
                case DataKind.Object_W:
                    return ReadReadableObject();
                case DataKind.ObjectRef:
                    return Variant.FromObject(_dataMap.GetValue(_reader.ReadInt32()));
                case DataKind.ObjectRef_B:
                    return Variant.FromObject(_dataMap.GetValue(_reader.ReadByte()));
                case DataKind.ObjectRef_S:
                    return Variant.FromObject(_dataMap.GetValue(_reader.ReadUInt16()));
                case DataKind.Type:
                case DataKind.TypeRef:
                case DataKind.TypeRef_B:
                case DataKind.TypeRef_S:
                    return Variant.FromType(ReadType(kind));
                case DataKind.Enum:
                    return Variant.FromBoxedEnum(ReadBoxedEnum());
                case DataKind.Array:
                case DataKind.Array_0:
                case DataKind.Array_1:
                case DataKind.Array_2:
                case DataKind.Array_3:
                case DataKind.ValueArray:
                case DataKind.ValueArray_0:
                case DataKind.ValueArray_1:
                case DataKind.ValueArray_2:
                case DataKind.ValueArray_3:
                    return ReadArray(kind);
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private class VariantReader : ObjectReader
        {
            private readonly List<Variant> _list;
            private int _index;

            public VariantReader()
            {
                _list = new List<Variant>();
            }

            public List<Variant> List => _list;

            public void Reset()
            {
                _list.Clear();
                _index = 0;
            }

            public int Position => _index;

            private Variant Next()
            {
                return _list[_index++];
            }

            public override bool ReadBoolean()
            {
                return Next().AsBoolean();
            }

            public override byte ReadByte()
            {
                return Next().AsByte();
            }

            public override char ReadChar()
            {
                return Next().AsChar();
            }

            public override decimal ReadDecimal()
            {
                return Next().AsDecimal();
            }

            public override double ReadDouble()
            {
                return Next().AsDouble();
            }

            public override float ReadSingle()
            {
                return Next().AsSingle();
            }

            public override int ReadInt32()
            {
                return Next().AsInt32();
            }

            public override long ReadInt64()
            {
                return Next().AsInt64();
            }

            public override sbyte ReadSByte()
            {
                return Next().AsSByte();
            }

            public override short ReadInt16()
            {
                return Next().AsInt16();
            }

            public override uint ReadUInt32()
            {
                return Next().AsUInt32();
            }

            public override ulong ReadUInt64()
            {
                return Next().AsUInt64();
            }

            public override ushort ReadUInt16()
            {
                return Next().AsUInt16();
            }

            public override DateTime ReadDateTime()
            {
                return Next().AsDateTime();
            }

            public override String ReadString()
            {
                var next = Next();
                if (next.Kind == VariantKind.Null)
                {
                    return null;
                }
                else
                {
                    return next.AsString();
                }
            }

            public override Object ReadValue()
            {
                return Next().ToBoxedObject();
            }
        }

        private uint ReadCompressedUInt()
        {
            var info = _reader.ReadByte();
            byte marker = (byte)(info & StreamObjectWriter.ByteMarkerMask);
            byte byte0 = (byte)(info & ~StreamObjectWriter.ByteMarkerMask);

            if (marker == StreamObjectWriter.Byte1Marker)
            {
                return byte0;
            }

            if (marker == StreamObjectWriter.Byte2Marker)
            {
                var byte1 = _reader.ReadByte();
                return (((uint)byte0) << 8) | byte1;
            }

            if (marker == StreamObjectWriter.Byte4Marker)
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
            var kind = (DataKind)_reader.ReadByte();
            return kind == DataKind.Null ? null : ReadStringValue(kind);
        }

        private string ReadStringValue(DataKind kind)
        {
            switch (kind)
            {
                case DataKind.StringRef_B:
                    return (string)_dataMap.GetValue(_reader.ReadByte());

                case DataKind.StringRef_S:
                    return (string)_dataMap.GetValue(_reader.ReadUInt16());

                case DataKind.StringRef:
                    return (string)_dataMap.GetValue(_reader.ReadInt32());

                case DataKind.StringUtf16:
                case DataKind.StringUtf8:
                    return ReadStringLiteral(kind);

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private unsafe string ReadStringLiteral(DataKind kind)
        {
            int id = _dataMap.GetNextId();
            string value;
            if (kind == DataKind.StringUtf8)
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

            _dataMap.AddValue(id, value);
            return value;
        }

        private Variant ReadArray(DataKind kind)
        {
            int length;
            switch (kind)
            {
                case DataKind.Array_0:
                case DataKind.ValueArray_0:
                    length = 0;
                    break;
                case DataKind.Array_1:
                case DataKind.ValueArray_1:
                    length = 1;
                    break;
                case DataKind.Array_2:
                case DataKind.ValueArray_2:
                    length = 2;
                    break;
                case DataKind.Array_3:
                case DataKind.ValueArray_3:
                    length = 3;
                    break;
                default:
                    length = (int)this.ReadCompressedUInt();
                    break;
            }

            var elementKind = (DataKind)_reader.ReadByte();

            // optimization for primitive type array
            Type elementType;
            if (StreamObjectWriter.s_reverseTypeMap.TryGetValue(elementKind, out elementType))
            {
                return Variant.FromArray(this.ReadPrimitiveTypeArrayElements(elementType, elementKind, length));
            }
            else
            {
                // custom type case
                elementType = this.ReadType(elementKind);

                _consumerStack.Push(new Consumer(elementType, length, _valueStack.Count, null, 0));
                return Variant.None;
            }
        }

        private Variant ConstructArray(Type elementType, int length)
        {
            if (_valueStack.Count < length)
            {
                throw new InvalidOperationException($"Deserialization for array expects to read {length} elements, but only {_valueStack.Count} elements are available.");
            }

            Array array = Array.CreateInstance(elementType, length);

            // values are on stack in reverse order
            for (int i = length - 1; i >= 0; i--)
            {
                var value = _valueStack.Pop().ToBoxedObject();
                array.SetValue(value, i);
            }

            return Variant.FromArray(array);
        }

        private Array ReadPrimitiveTypeArrayElements(Type type, DataKind kind, int length)
        {
            Debug.Assert(StreamObjectWriter.s_reverseTypeMap[kind] == type);

            // optimizations for supported array type by binary reader
            if (type == typeof(byte))
            {
                return _reader.ReadBytes(length);
            }

            if (type == typeof(char))
            {
                return _reader.ReadChars(length);
            }

            // optimizations for string where object reader/writer has its own mechanism to
            // reduce duplicated strings
            if (type == typeof(string))
            {
                return ReadStringArrayElements(CreateArray<string>(length));
            }

            if (type == typeof(bool))
            {
                return ReadBooleanArrayElements(CreateArray<bool>(length));
            }

            // otherwise, read elements directly from underlying binary writer
            switch (kind)
            {
                case DataKind.Int8:
                    return ReadInt8ArrayElements(CreateArray<sbyte>(length));
                case DataKind.Int16:
                    return ReadInt16ArrayElements(CreateArray<short>(length));
                case DataKind.Int32:
                    return ReadInt32ArrayElements(CreateArray<int>(length));
                case DataKind.Int64:
                    return ReadInt64ArrayElements(CreateArray<long>(length));
                case DataKind.UInt16:
                    return ReadUInt16ArrayElements(CreateArray<ushort>(length));
                case DataKind.UInt32:
                    return ReadUInt32ArrayElements(CreateArray<uint>(length));
                case DataKind.UInt64:
                    return ReadUInt64ArrayElements(CreateArray<ulong>(length));
                case DataKind.Float4:
                    return ReadFloat4ArrayElements(CreateArray<float>(length));
                case DataKind.Float8:
                    return ReadFloat8ArrayElements(CreateArray<double>(length));
                case DataKind.Decimal:
                    return ReadDecimalArrayElements(CreateArray<decimal>(length));
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private bool[] ReadBooleanArrayElements(bool[] array)
        {
            var wordLength = BitVector.WordsRequired(array.Length);

            var count = 0;
            for (var i = 0; i < wordLength; i++)
            {
                var word = _reader.ReadUInt32();

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

        private Type ReadType()
        {
            var kind = (DataKind)_reader.ReadByte();
            return ReadType(kind);
        }

        private Type ReadType(DataKind kind)
        {
            switch (kind)
            {
                case DataKind.TypeRef_B:
                    return (Type)_dataMap.GetValue(_reader.ReadByte());

                case DataKind.TypeRef_S:
                    return (Type)_dataMap.GetValue(_reader.ReadUInt16());

                case DataKind.TypeRef:
                    return (Type)_dataMap.GetValue(_reader.ReadInt32());

                case DataKind.Type:
                    int id = _dataMap.GetNextId();
                    var assemblyName = this.ReadStringValue();
                    var typeName = this.ReadStringValue();

                    if (_binder == null)
                    {
                        throw NoBinderException(typeName);
                    }

                    var type = _binder.GetType(new TypeKey(assemblyName, typeName));
                    _dataMap.AddValue(id, type);
                    return type;

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private object ReadBoxedEnum()
        {
            var enumType = this.ReadType();
            var type = Enum.GetUnderlyingType(enumType);

            if (type == typeof(int))
            {
                return Enum.ToObject(enumType, _reader.ReadInt32());
            }

            if (type == typeof(short))
            {
                return Enum.ToObject(enumType, _reader.ReadInt16());
            }

            if (type == typeof(byte))
            {
                return Enum.ToObject(enumType, _reader.ReadByte());
            }

            if (type == typeof(long))
            {
                return Enum.ToObject(enumType, _reader.ReadInt64());
            }

            if (type == typeof(sbyte))
            {
                return Enum.ToObject(enumType, _reader.ReadSByte());
            }

            if (type == typeof(ushort))
            {
                return Enum.ToObject(enumType, _reader.ReadUInt16());
            }

            if (type == typeof(uint))
            {
                return Enum.ToObject(enumType, _reader.ReadUInt32());
            }

            if (type == typeof(ulong))
            {
                return Enum.ToObject(enumType, _reader.ReadUInt64());
            }

            throw ExceptionUtilities.UnexpectedValue(enumType);
        }

        private Variant ReadReadableObject()
        {
            int id = _dataMap.GetNextId();

            Type type = this.ReadType();
            uint memberCount = this.ReadCompressedUInt();

            if (_binder == null)
            {
                throw NoBinderException(type.FullName);
            }

            var reader = _binder.GetReader(type);
            if (reader == null)
            {
                throw NoReaderException(type.FullName);
            }

            if (memberCount == 0)
            {
                return ConstructObject(type, (int)memberCount, reader, id);
            }
            else
            {
                _consumerStack.Push(new Consumer(type, (int)memberCount, _valueStack.Count, reader, id));
                return Variant.None;
            }
        }

        private Variant ConstructObject(Type type, int memberCount, Func<ObjectReader, object> reader, int id)
        {
            _variantReader.Reset();

            // take members from the stack
            for (int i = 0; i < memberCount; i++)
            {
                _variantReader.List.Add(_valueStack.Pop());
            }

            // reverse list so that first member to be read is first
            _variantReader.List.Reverse();

            // invoke the deserialization constructor to create instance and read & assign members           
            var instance = reader(_variantReader);

            if (_variantReader.Position != memberCount)
            {
                throw new InvalidOperationException($"Deserialization constructor for '{type.Name}' was expected to read {memberCount} values, but read {_variantReader.Position} values instead.");
            }

            _dataMap.AddValue(id, instance);

            return Variant.FromObject(instance);
        }

        private static Exception NoBinderException(string typeName)
        {
#if COMPILERCORE
            return new InvalidOperationException(string.Format(CodeAnalysisResources.NoBinderException, typeName));
#else
            return new InvalidOperationException(string.Format(Microsoft.CodeAnalysis.WorkspacesResources.Cannot_deserialize_type_0_no_binder_supplied, typeName));
#endif
        }

        private static Exception NoReaderException(string typeName)
        {
#if COMPILERCORE
            return new InvalidOperationException(string.Format(CodeAnalysisResources.NoReaderException, typeName));
#else
            return new InvalidOperationException(string.Format(Microsoft.CodeAnalysis.WorkspacesResources.Cannot_deserialize_type_0_it_has_no_deserialization_reader, typeName));
#endif
        }
    }
}
