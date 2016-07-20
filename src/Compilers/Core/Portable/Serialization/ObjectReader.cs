// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A class that reads both primitive values and non-cyclical object graphs from a stream that was constructed using 
    /// the ObjectWriter class.
    /// </summary>
    internal sealed class ObjectReader : ObjectReaderWriterBase, IDisposable
    {
        private readonly BinaryReader _reader;
        private readonly ObjectReaderData _dataMap;
        private readonly ObjectBinder _binder;

        internal ObjectReader(
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
        }

        public void Dispose()
        {
            _dataMap.Dispose();
        }

        /// <summary>
        /// Read a Boolean value from the stream. This value must have been written using <see cref="ObjectWriter.WriteBoolean(bool)"/>.
        /// </summary>
        public bool ReadBoolean()
        {
            return _reader.ReadBoolean();
        }

        /// <summary>
        /// Read a Byte value from the stream. This value must have been written using <see cref="ObjectWriter.WriteByte(byte)"/>.
        /// </summary>
        public byte ReadByte()
        {
            return _reader.ReadByte();
        }

        /// <summary>
        /// Read a Char value from the stream. This value must have been written using <see cref="ObjectWriter.WriteChar(char)"/>.
        /// </summary>
        public char ReadChar()
        {
            // char was written as UInt16 because BinaryWriter fails on characters that are unicode surrogates.
            return (char)_reader.ReadUInt16();
        }

        /// <summary>
        /// Read a Decimal value from the stream. This value must have been written using <see cref="ObjectWriter.WriteDecimal(decimal)"/>.
        /// </summary>
        public decimal ReadDecimal()
        {
            return _reader.ReadDecimal();
        }

        /// <summary>
        /// Read a Double value from the stream. This value must have been written using <see cref="ObjectWriter.WriteDouble(double)"/>.
        /// </summary>
        public double ReadDouble()
        {
            return _reader.ReadDouble();
        }

        /// <summary>
        /// Read a Single value from the stream. This value must have been written using <see cref="ObjectWriter.WriteSingle(float)"/>.
        /// </summary>
        public float ReadSingle()
        {
            return _reader.ReadSingle();
        }

        /// <summary>
        /// Read a Int32 value from the stream. This value must have been written using <see cref="ObjectWriter.WriteInt32(int)"/>.
        /// </summary>
        public int ReadInt32()
        {
            return _reader.ReadInt32();
        }

        /// <summary>
        /// Read a Int64 value from the stream. This value must have been written using <see cref="ObjectWriter.WriteInt64(long)"/>.
        /// </summary>
        public long ReadInt64()
        {
            return _reader.ReadInt64();
        }

        /// <summary>
        /// Read a SByte value from the stream. This value must have been written using <see cref="ObjectWriter.WriteSByte(sbyte)"/>.
        /// </summary>
        public sbyte ReadSByte()
        {
            return _reader.ReadSByte();
        }

        /// <summary>
        /// Read a Int16 value from the stream. This value must have been written using <see cref="ObjectWriter.WriteInt16(short)"/>.
        /// </summary>
        public short ReadInt16()
        {
            return _reader.ReadInt16();
        }

        /// <summary>
        /// Read a UInt32 value from the stream. This value must have been written using <see cref="ObjectWriter.WriteUInt32(uint)"/>.
        /// </summary>
        public uint ReadUInt32()
        {
            return _reader.ReadUInt32();
        }

        /// <summary>
        /// Read a UInt64 value from the stream. This value must have been written using <see cref="ObjectWriter.WriteUInt64(ulong)"/>.
        /// </summary>
        public ulong ReadUInt64()
        {
            return _reader.ReadUInt64();
        }

        /// <summary>
        /// Read a UInt16 value from the stream. This value must have been written using <see cref="ObjectWriter.WriteUInt16(ushort)"/>.
        /// </summary>
        public ushort ReadUInt16()
        {
            return _reader.ReadUInt16();
        }

        /// <summary>
        /// Read a DateTime value from the stream. This value must have been written using the <see cref="ObjectWriter.WriteDateTime(DateTime)"/>.
        /// </summary>
        public DateTime ReadDateTime()
        {
            return DateTime.FromBinary(this.ReadInt64());
        }

        /// <summary>
        /// Read a compressed 30-bit integer value from the stream. This value must have been written using <see cref="ObjectWriter.WriteCompressedUInt(uint)"/>.
        /// </summary>
        public uint ReadCompressedUInt()
        {
            var info = _reader.ReadByte();
            byte marker = (byte)(info & ByteMarkerMask);
            byte byte0 = (byte)(info & ~ByteMarkerMask);

            if (marker == Byte1Marker)
            {
                return byte0;
            }

            if (marker == Byte2Marker)
            {
                var byte1 = _reader.ReadByte();
                return (((uint)byte0) << 8) | byte1;
            }

            if (marker == Byte4Marker)
            {
                var byte1 = _reader.ReadByte();
                var byte2 = _reader.ReadByte();
                var byte3 = _reader.ReadByte();

                return (((uint)byte0) << 24) | (((uint)byte1) << 16) | (((uint)byte2) << 8) | byte3;
            }

            throw ExceptionUtilities.UnexpectedValue(marker);
        }

        /// <summary>
        /// Read a value from the stream. The value must have been written using ObjectWriter.WriteValue.
        /// </summary>
        public object ReadValue()
        {
            var kind = (DataKind)_reader.ReadByte();
            switch (kind)
            {
                case DataKind.Null:
                    return null;
                case DataKind.Boolean_T:
                    return Boxes.BoxedTrue;
                case DataKind.Boolean_F:
                    return Boxes.BoxedFalse;
                case DataKind.Int8:
                    return _reader.ReadSByte();
                case DataKind.UInt8:
                    return _reader.ReadByte();
                case DataKind.Int16:
                    return _reader.ReadInt16();
                case DataKind.UInt16:
                    return _reader.ReadUInt16();
                case DataKind.Int32:
                    return _reader.ReadInt32();
                case DataKind.Int32_B:
                    return (int)_reader.ReadByte();
                case DataKind.Int32_S:
                    return (int)_reader.ReadUInt16();
                case DataKind.Int32_Z:
                    return Boxes.BoxedInt32Zero;
                case DataKind.UInt32:
                    return _reader.ReadUInt32();
                case DataKind.Int64:
                    return _reader.ReadInt64();
                case DataKind.UInt64:
                    return _reader.ReadUInt64();
                case DataKind.Float4:
                    return _reader.ReadSingle();
                case DataKind.Float8:
                    return _reader.ReadDouble();
                case DataKind.Decimal:
                    return _reader.ReadDecimal();
                case DataKind.DateTime:
                    return this.ReadDateTime();
                case DataKind.Char:
                    return this.ReadChar();
                case DataKind.StringUtf8:
                case DataKind.StringUtf16:
                case DataKind.StringRef:
                case DataKind.StringRef_B:
                case DataKind.StringRef_S:
                    return ReadString(kind);
                case DataKind.Object_W:
                case DataKind.ObjectRef:
                case DataKind.ObjectRef_B:
                case DataKind.ObjectRef_S:
                    return ReadObject(kind);
                case DataKind.Type:
                case DataKind.TypeRef:
                case DataKind.TypeRef_B:
                case DataKind.TypeRef_S:
                    return ReadType(kind);
                case DataKind.Enum:
                    return ReadEnum();
                case DataKind.Array:
                case DataKind.Array_0:
                case DataKind.Array_1:
                case DataKind.Array_2:
                case DataKind.Array_3:
                    return ReadArray(kind);
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        /// <summary>
        /// Read a String value from the stream. This value must have been written using ObjectWriter.WriteString.
        /// </summary>
        public string ReadString()
        {
            var kind = (DataKind)_reader.ReadByte();
            return kind == DataKind.Null ? null : ReadString(kind);
        }

        private string ReadString(DataKind kind)
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

        private Array ReadArray(DataKind kind)
        {
            int length;
            switch (kind)
            {
                case DataKind.Array_0:
                    length = 0;
                    break;
                case DataKind.Array_1:
                    length = 1;
                    break;
                case DataKind.Array_2:
                    length = 2;
                    break;
                case DataKind.Array_3:
                    length = 3;
                    break;
                default:
                    length = (int)this.ReadCompressedUInt();
                    break;
            }

            Type elementType = this.ReadType();
            Array array = Array.CreateInstance(elementType, length);
            for (int i = 0; i < length; i++)
            {
                var value = this.ReadValue();
                array.SetValue(value, i);
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
                    var assemblyName = this.ReadString();
                    var typeName = this.ReadString();

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

        private object ReadEnum()
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

        private object ReadObject(DataKind kind)
        {
            switch (kind)
            {
                case DataKind.ObjectRef_B:
                    return _dataMap.GetValue(_reader.ReadByte());

                case DataKind.ObjectRef_S:
                    return _dataMap.GetValue(_reader.ReadUInt16());

                case DataKind.ObjectRef:
                    return _dataMap.GetValue(_reader.ReadInt32());

                case DataKind.Object_W:
                    return this.ReadReadableObject();

                case DataKind.Array:
                    return this.ReadArray(kind);

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private object ReadReadableObject()
        {
            int id = _dataMap.GetNextId();

            Type type = this.ReadType();
            var instance = CreateInstance(type);

            _dataMap.AddValue(id, instance);
            return instance;
        }

        private object CreateInstance(Type type)
        {
            if (_binder == null)
            {
                return NoBinderException(type.FullName);
            }

            var reader = _binder.GetReader(type);
            if (reader == null)
            {
                return NoReaderException(type.FullName);
            }

            return reader(this);
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
