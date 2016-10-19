// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A class that deserializes objects from a stream.
    /// </summary>
    internal sealed class StreamObjectReader : ObjectReader, IDisposable
    {
        private readonly BinaryReader _reader;
        private readonly ObjectReaderData _dataMap;
        private readonly ObjectBinder _binder;
        private readonly Stack<Variant> _valueStack;
        private readonly ListReader _valueReader;

        internal StreamObjectReader(
            Stream stream,
            ObjectReaderData defaultData = null,
            ObjectBinder binder = null)
        {
            // String serialization assumes both reader and writer to be of the same endianness.
            // It can be adjusted for BigEndian if needed.
            Debug.Assert(BitConverter.IsLittleEndian);

            _reader = new BinaryReader(stream, Encoding.UTF8);
            _dataMap = new ObjectReaderData(defaultData);
            _binder = binder;
            _valueStack = new Stack<Variant>();
            _valueReader = new ListReader();
        }

        public void Dispose()
        {
            _dataMap.Dispose();
        }

        public override bool ReadBoolean()
        {
            return Consume().AsBoolean();
        }

        public override byte ReadByte()
        {
            return Consume().AsByte();
        }

        public override char ReadChar()
        {
            return Consume().AsChar();
        }

        public override decimal ReadDecimal()
        {
            return Consume().AsDecimal();
        }

        public override double ReadDouble()
        {
            return Consume().AsDouble();
        }

        public override float ReadSingle()
        {
            return Consume().AsSingle();
        }

        public override int ReadInt32()
        {
            return Consume().AsInt32();
        }

        public override long ReadInt64()
        {
            return Consume().AsInt64();
        }

        public override sbyte ReadSByte()
        {
            return Consume().AsSByte();
        }

        public override short ReadInt16()
        {
            return Consume().AsInt16();
        }

        public override uint ReadUInt32()
        {
            return Consume().AsUInt32();
        }

        public override ulong ReadUInt64()
        {
            return Consume().AsUInt64();
        }

        public override ushort ReadUInt16()
        {
            return Consume().AsUInt16();
        }

        public override DateTime ReadDateTime()
        {
            return Consume().AsDateTime();
        }

        public override string ReadString()
        {
            var v = Consume();
            if (v.Kind == VariantKind.Null)
            {
                return null;
            }
            else
            {
                return v.AsString();
            }
        }

        public override object ReadValue()
        {
            return Consume().ToBoxedObject();
        }

        private Variant Consume()
        {
            while (true)
            {
                var kind = (DataKind)_reader.ReadByte();
                switch (kind)
                {
                    case DataKind.Null:
                        _valueStack.Push(Variant.Null);
                        break;
                    case DataKind.Boolean_T:
                        _valueStack.Push(Variant.FromBoolean(true));
                        break;
                    case DataKind.Boolean_F:
                        _valueStack.Push(Variant.FromBoolean(false));
                        break;
                    case DataKind.Int8:
                        _valueStack.Push(Variant.FromSByte(_reader.ReadSByte()));
                        break;
                    case DataKind.UInt8:
                        _valueStack.Push(Variant.FromByte(_reader.ReadByte()));
                        break;
                    case DataKind.Int16:
                        _valueStack.Push(Variant.FromInt16(_reader.ReadInt16()));
                        break;
                    case DataKind.UInt16:
                        _valueStack.Push(Variant.FromUInt16(_reader.ReadUInt16()));
                        break;
                    case DataKind.Int32:
                        _valueStack.Push(Variant.FromInt32(_reader.ReadInt32()));
                        break;
                    case DataKind.Int32_B:
                        _valueStack.Push(Variant.FromInt32((int)_reader.ReadByte()));
                        break;
                    case DataKind.Int32_S:
                        _valueStack.Push(Variant.FromInt32((int)_reader.ReadUInt16()));
                        break;
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
                        _valueStack.Push(Variant.FromInt32((int)kind - (int)DataKind.Int32_0));
                        break;
                    case DataKind.UInt32:
                        _valueStack.Push(Variant.FromUInt32(_reader.ReadUInt32()));
                        break;
                    case DataKind.UInt32_B:
                        _valueStack.Push(Variant.FromUInt32((uint)_reader.ReadByte()));
                        break;
                    case DataKind.UInt32_S:
                        _valueStack.Push(Variant.FromUInt32((uint)_reader.ReadUInt16()));
                        break;
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
                        _valueStack.Push(Variant.FromUInt32((uint)((int)kind - (int)DataKind.UInt32_0)));
                        break;
                    case DataKind.Int64:
                        _valueStack.Push(Variant.FromInt64(_reader.ReadInt64()));
                        break;
                    case DataKind.UInt64:
                        _valueStack.Push(Variant.FromUInt64(_reader.ReadUInt64()));
                        break;
                    case DataKind.Float4:
                        _valueStack.Push(Variant.FromSingle(_reader.ReadSingle()));
                        break;
                    case DataKind.Float8:
                        _valueStack.Push(Variant.FromDouble(_reader.ReadDouble()));
                        break;
                    case DataKind.Decimal:
                        _valueStack.Push(Variant.FromDecimal(_reader.ReadDecimal()));
                        break;
                    case DataKind.DateTime:
                        _valueStack.Push(Variant.FromDateTime(DateTime.FromBinary(_reader.ReadInt64())));
                        break;
                    case DataKind.Char:
                        // read as ushort because writer fails on chars that are unicode surrogates
                        _valueStack.Push(Variant.FromChar((char)_reader.ReadUInt16()));
                        break;
                    case DataKind.StringUtf8:
                    case DataKind.StringUtf16:
                    case DataKind.StringRef:
                    case DataKind.StringRef_B:
                    case DataKind.StringRef_S:
                        _valueStack.Push(Variant.FromString(ConsumeString(kind)));
                        break;
                    case DataKind.Object_W:
                        _valueStack.Push(Variant.FromObject(ConsumeReadableObject()));
                        break;
                    case DataKind.ObjectRef:
                        _valueStack.Push(Variant.FromObject(_dataMap.GetValue(_reader.ReadInt32())));
                        break;
                    case DataKind.ObjectRef_B:
                        _valueStack.Push(Variant.FromObject(_dataMap.GetValue(_reader.ReadByte())));
                        break;
                    case DataKind.ObjectRef_S:
                        _valueStack.Push(Variant.FromObject(_dataMap.GetValue(_reader.ReadUInt16())));
                        break;
                    case DataKind.Type:
                    case DataKind.TypeRef:
                    case DataKind.TypeRef_B:
                    case DataKind.TypeRef_S:
                        _valueStack.Push(Variant.FromType(ConsumeType(kind)));
                        break;
                    case DataKind.Enum:
                        _valueStack.Push(Variant.FromBoxedEnum(ConsumeEnum()));
                        break;
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
                        _valueStack.Push(Variant.FromArray(ConsumeArray(kind)));
                        break;
                    case DataKind.End:
                        return _valueStack.Pop();
                    default:
                        throw ExceptionUtilities.UnexpectedValue(kind);
                }
            }
        }

        private class ListReader : ObjectReader
        {
            internal readonly List<Variant> _list;
            internal int _index;

            public ListReader()
            {
                _list = new List<Variant>();
            }

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

        private uint ConsumeCompressedUInt()
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

        private string ConsumeString()
        {
            var kind = (DataKind)_reader.ReadByte();
            return kind == DataKind.Null ? null : ConsumeString(kind);
        }

        private string ConsumeString(DataKind kind)
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
                    return ConsumeStringLiteral(kind);

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private unsafe string ConsumeStringLiteral(DataKind kind)
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

                int characterCount = (int)ConsumeCompressedUInt();
                byte[] bytes = _reader.ReadBytes(characterCount * sizeof(char));
                fixed (byte* bytesPtr = bytes)
                {
                    value = new string((char*)bytesPtr, 0, characterCount);
                }
            }

            _dataMap.AddValue(id, value);
            return value;
        }

        private Array ConsumeArray(DataKind kind)
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
                    length = (int)this.ConsumeCompressedUInt();
                    break;
            }

            var elementKind = (DataKind)_reader.ReadByte();

            // optimization for primitive type array
            Type elementType;
            if (StreamObjectWriter.s_reverseTypeMap.TryGetValue(elementKind, out elementType))
            {
                return this.ConsumePrimitiveTypeArrayElements(elementType, elementKind, length);
            }
            else
            {
                // custom type case
                elementType = this.ConsumeType(elementKind);

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

                return array;
            }
        }

        private Array ConsumePrimitiveTypeArrayElements(Type type, DataKind kind, int length)
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
                return ReadPrimitiveTypeArrayElements(length, ConsumeString);
            }

            if (type == typeof(bool))
            {
                return ConsumeBooleanArray(length);
            }

            // otherwise, read elements directly from underlying binary writer
            switch (kind)
            {
                case DataKind.Int8:
                    return ReadPrimitiveTypeArrayElements(length, _reader.ReadSByte);
                case DataKind.Int16:
                    return ReadPrimitiveTypeArrayElements(length, _reader.ReadInt16);
                case DataKind.Int32:
                    return ReadPrimitiveTypeArrayElements(length, _reader.ReadInt32);
                case DataKind.Int64:
                    return ReadPrimitiveTypeArrayElements(length, _reader.ReadInt64);
                case DataKind.UInt16:
                    return ReadPrimitiveTypeArrayElements(length, _reader.ReadUInt16);
                case DataKind.UInt32:
                    return ReadPrimitiveTypeArrayElements(length, _reader.ReadUInt32);
                case DataKind.UInt64:
                    return ReadPrimitiveTypeArrayElements(length, _reader.ReadUInt64);
                case DataKind.Float4:
                    return ReadPrimitiveTypeArrayElements(length, _reader.ReadSingle);
                case DataKind.Float8:
                    return ReadPrimitiveTypeArrayElements(length, _reader.ReadDouble);
                case DataKind.Decimal:
                    return ReadPrimitiveTypeArrayElements(length, _reader.ReadDecimal);
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private bool[] ConsumeBooleanArray(int length)
        {
            if (length == 0)
            {
                //  simple check
                return Array.Empty<bool>();
            }

            var array = new bool[length];
            var wordLength = BitVector.WordsRequired(length);

            var count = 0;
            for (var i = 0; i < wordLength; i++)
            {
                var word = _reader.ReadUInt32();

                for (var p = 0; p < BitVector.BitsPerWord; p++)
                {
                    if (count >= length)
                    {
                        return array;
                    }

                    array[count++] = BitVector.IsTrue(word, p);
                }
            }

            return array;
        }

        private static T[] ReadPrimitiveTypeArrayElements<T>(int length, Func<T> read)
        {
            if (length == 0)
            {
                // quick check
                return Array.Empty<T>();
            }

            var array = new T[length];
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = read();
            }

            return array;
        }

        private Type ConsumeType()
        {
            var kind = (DataKind)_reader.ReadByte();
            return ConsumeType(kind);
        }

        private Type ConsumeType(DataKind kind)
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
                    var assemblyName = this.ConsumeString();
                    var typeName = this.ConsumeString();

                    if (_binder == null)
                    {
                        throw NoBinderException(typeName);
                    }

                    var type = _binder.GetType(assemblyName, typeName);
                    _dataMap.AddValue(id, type);
                    return type;

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private object ConsumeEnum()
        {
            var enumType = this.ConsumeType();
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

        private object ConsumeReadableObject()
        {
            int id = _dataMap.GetNextId();

            Type type = this.ConsumeType();
            uint memberCount = this.ConsumeCompressedUInt();

            if (_binder == null)
            {
                return NoBinderException(type.FullName);
            }

            var reader = _binder.GetReader(type);
            if (reader == null)
            {
                return NoReaderException(type.FullName);
            }

            // member values from value stack, copy them into value reader.
            if (_valueStack.Count < memberCount)
            {
                throw new InvalidOperationException($"Deserialization constructor for '{type.Name}' is expected to read {memberCount} values, but only {_valueStack.Count} are available.");
            }

            var list = _valueReader._list;
            _valueReader._index = 0;
            list.Clear();

            // take members from value stack
            for (int i = 0; i < memberCount; i++)
            {
                list.Add(_valueStack.Pop());
            }

            // reverse list so that first member to be read is first
            list.Reverse();

            // invoke the deserialization constructor to create instance and read & assign members           
            var instance = reader(_valueReader);

            if (_valueReader._index != memberCount)
            {
                throw new InvalidOperationException($"Deserialization constructor for '{type.Name}' was expected to read {memberCount} values, but read {_valueReader._index} values instead.");
            }

            _dataMap.AddValue(id, instance);
            return instance;
        }

        private static Exception NoBinderException(string typeName)
        {
#if COMPILERCORE
            throw new InvalidOperationException(string.Format(CodeAnalysisResources.NoBinderException, typeName));
#else
            throw new InvalidOperationException(string.Format(Microsoft.CodeAnalysis.WorkspacesResources.Cannot_deserialize_type_0_no_binder_supplied, typeName));
#endif
        }

        private static Exception NoReaderException(string typeName)
        {
#if COMPILERCORE
            throw new InvalidOperationException(string.Format(CodeAnalysisResources.NoReaderException, typeName));
#else
            throw new InvalidOperationException(string.Format(Microsoft.CodeAnalysis.WorkspacesResources.Cannot_deserialize_type_0_it_has_no_deserialization_reader, typeName));
#endif
        }
    }
}
