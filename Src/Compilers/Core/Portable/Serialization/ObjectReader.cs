// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A class that reads both primitive values and non-cyclical object graphs from a stream that was constructed using 
    /// the ObjectWriter class.
    /// </summary>
    internal sealed class ObjectReader : ObjectReaderWriterBase, IDisposable
    {
        private readonly BinaryReader reader;
        private readonly ObjectReaderData dataMap;
        private readonly ObjectBinder binder;

        internal ObjectReader(
            Stream stream,
            ObjectReaderData defaultData = null,
            ObjectBinder binder = null)
        {
            // String serialization assumes both reader and writer to be of the same endianness.
            // It can be adjusted for BigEndian if needed.
            Debug.Assert(BitConverter.IsLittleEndian);

            this.reader = new BinaryReader(stream, Encoding.UTF8);
            this.dataMap = new ObjectReaderData(defaultData);
            this.binder = binder;
        }

        public void Dispose()
        {
            this.dataMap.Dispose();
        }

        /// <summary>
        /// Read a Boolean value from the stream. This value must have been written using <see cref="ObjectWriter.WriteBoolean(bool)"/>.
        /// </summary>
        public bool ReadBoolean()
        {
            return this.reader.ReadBoolean();
        }

        /// <summary>
        /// Read a Byte value from the stream. This value must have been written using <see cref="ObjectWriter.WriteByte(byte)"/>.
        /// </summary>
        public byte ReadByte()
        {
            return this.reader.ReadByte();
        }

        /// <summary>
        /// Read a Char value from the stream. This value must have been written using <see cref="ObjectWriter.WriteChar(char)"/>.
        /// </summary>
        public char ReadChar()
        {
            // char was written as UInt16 because BinaryWriter fails on characters that are unicode surrogates.
            return (char)this.reader.ReadUInt16();
        }

        /// <summary>
        /// Read a Decimal value from the stream. This value must have been written using <see cref="ObjectWriter.WriteDecimal(decimal)"/>.
        /// </summary>
        public decimal ReadDecimal()
        {
            return this.reader.ReadDecimal();
        }

        /// <summary>
        /// Read a Double value from the stream. This value must have been written using <see cref="ObjectWriter.WriteDouble(double)"/>.
        /// </summary>
        public double ReadDouble()
        {
            return this.reader.ReadDouble();
        }

        /// <summary>
        /// Read a Single value from the stream. This value must have been written using <see cref="ObjectWriter.WriteSingle(float)"/>.
        /// </summary>
        public float ReadSingle()
        {
            return this.reader.ReadSingle();
        }

        /// <summary>
        /// Read a Int32 value from the stream. This value must have been written using <see cref="ObjectWriter.WriteInt32(int)"/>.
        /// </summary>
        public int ReadInt32()
        {
            return this.reader.ReadInt32();
        }

        /// <summary>
        /// Read a Int64 value from the stream. This value must have been written using <see cref="ObjectWriter.WriteInt64(long)"/>.
        /// </summary>
        public long ReadInt64()
        {
            return this.reader.ReadInt64();
        }

        /// <summary>
        /// Read a SByte value from the stream. This value must have been written using <see cref="ObjectWriter.WriteSByte(sbyte)"/>.
        /// </summary>
        public sbyte ReadSByte()
        {
            return this.reader.ReadSByte();
        }

        /// <summary>
        /// Read a Int16 value from the stream. This value must have been written using <see cref="ObjectWriter.WriteInt16(short)"/>.
        /// </summary>
        public short ReadInt16()
        {
            return this.reader.ReadInt16();
        }

        /// <summary>
        /// Read a UInt32 value from the stream. This value must have been written using <see cref="ObjectWriter.WriteUInt32(uint)"/>.
        /// </summary>
        public uint ReadUInt32()
        {
            return this.reader.ReadUInt32();
        }

        /// <summary>
        /// Read a UInt64 value from the stream. This value must have been written using <see cref="ObjectWriter.WriteUInt64(ulong)"/>.
        /// </summary>
        public ulong ReadUInt64()
        {
            return this.reader.ReadUInt64();
        }

        /// <summary>
        /// Read a UInt16 value from the stream. This value must have been written using <see cref="ObjectWriter.WriteUInt16(ushort)"/>.
        /// </summary>
        public ushort ReadUInt16()
        {
            return this.reader.ReadUInt16();
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
            var info = reader.ReadByte();
            byte marker = (byte)(info & ByteMarkerMask);
            byte byte0 = (byte)(info & ~ByteMarkerMask);

            if (marker == Byte1Marker)
            {
                return byte0;
            }
            else if (marker == Byte2Marker)
            {
                var byte1 = reader.ReadByte();
                return (((uint)byte0) << 8) | byte1;
            }
            else if (marker == Byte4Marker)
            {
                var byte1 = reader.ReadByte();
                var byte2 = reader.ReadByte();
                var byte3 = reader.ReadByte();

                return (((uint)byte0) << 24) | (((uint)byte1) << 16) | (((uint)byte2) << 8) | byte3;
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(marker);
            }
        }

        private static readonly object Int32Zero = 0;
        private static readonly object BooleanTrue = true;
        private static readonly object BooleanFalse = false;

        /// <summary>
        /// Read a value from the stream. The value must have been written using ObjectWriter.WriteValue.
        /// </summary>
        public object ReadValue()
        {
            DataKind kind = (DataKind)reader.ReadByte();
            switch (kind)
            {
                case DataKind.Null:
                    return null;
                case DataKind.Boolean_T:
                    return BooleanTrue;
                case DataKind.Boolean_F:
                    return BooleanFalse;
                case DataKind.Int8:
                    return reader.ReadSByte();
                case DataKind.UInt8:
                    return reader.ReadByte();
                case DataKind.Int16:
                    return reader.ReadInt16();
                case DataKind.UInt16:
                    return reader.ReadUInt16();
                case DataKind.Int32:
                    return reader.ReadInt32();
                case DataKind.Int32_B:
                    return (int)reader.ReadByte();
                case DataKind.Int32_S:
                    return (int)reader.ReadUInt16();
                case DataKind.Int32_Z:
                    return Int32Zero;
                case DataKind.UInt32:
                    return reader.ReadUInt32();
                case DataKind.Int64:
                    return reader.ReadInt64();
                case DataKind.UInt64:
                    return reader.ReadUInt64();
                case DataKind.Float4:
                    return reader.ReadSingle();
                case DataKind.Float8:
                    return reader.ReadDouble();
                case DataKind.Decimal:
                    return reader.ReadDecimal();
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
            DataKind kind = (DataKind)reader.ReadByte();
            if (kind == DataKind.Null)
            {
                return null;
            }
            else
            {
                return ReadString(kind);
            }
        }

        private string ReadString(DataKind kind)
        {
            switch (kind)
            {
                case DataKind.StringRef_B:
                    return (string)this.dataMap.GetValue(reader.ReadByte());

                case DataKind.StringRef_S:
                    return (string)this.dataMap.GetValue(reader.ReadUInt16());

                case DataKind.StringRef:
                    return (string)this.dataMap.GetValue(reader.ReadInt32());

                case DataKind.StringUtf16:
                case DataKind.StringUtf8:
                    return ReadStringLiteral(kind);

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private unsafe string ReadStringLiteral(DataKind kind)
        {
            int id = this.dataMap.GetNextId();
            string value;
            if (kind == DataKind.StringUtf8)
            {
                value = reader.ReadString();
            }
            else
            {
                // This is rare, just allocate UTF16 bytes for simplicity.

                int characterCount = (int)ReadCompressedUInt();
                byte[] bytes = reader.ReadBytes(characterCount * sizeof(char));
                fixed (byte* bytesPtr = bytes)
                {
                    value = new string((char*)bytesPtr, 0, characterCount);
                }
            }

            this.dataMap.AddValue(id, value);
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
                case DataKind.Array:
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
            DataKind kind = (DataKind)reader.ReadByte();
            return ReadType(kind);
        }

        private Type ReadType(DataKind kind)
        {
            switch (kind)
            {
                case DataKind.TypeRef_B:
                    return (Type)dataMap.GetValue(reader.ReadByte());

                case DataKind.TypeRef_S:
                    return (Type)dataMap.GetValue(reader.ReadUInt16());

                case DataKind.TypeRef:
                    return (Type)dataMap.GetValue(reader.ReadInt32());

                case DataKind.Type:
                    int id = dataMap.GetNextId();
                    var assemblyName = this.ReadString();
                    var typeName = this.ReadString();

                    if (this.binder == null)
                    {
                        throw NoBinderException(typeName);
                    }

                    var type = this.binder.GetType(assemblyName, typeName);
                    dataMap.AddValue(id, type);
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
                return Enum.ToObject(enumType, reader.ReadInt32());
            }

            if (type == typeof(short))
            {
                return Enum.ToObject(enumType, reader.ReadInt16());
            }

            if (type == typeof(byte))
            {
                return Enum.ToObject(enumType, reader.ReadByte());
            }

            if (type == typeof(long))
            {
                return Enum.ToObject(enumType, reader.ReadInt64());
            }

            if (type == typeof(sbyte))
            {
                return Enum.ToObject(enumType, reader.ReadSByte());
            }

            if (type == typeof(ushort))
            {
                return Enum.ToObject(enumType, reader.ReadUInt16());
            }

            if (type == typeof(uint))
            {
                return Enum.ToObject(enumType, reader.ReadUInt32());
            }

            if (type == typeof(ulong))
            {
                return Enum.ToObject(enumType, reader.ReadUInt64());
            }

            throw ExceptionUtilities.UnexpectedValue(enumType);
        }

        private object ReadObject(DataKind kind)
        {
            switch (kind)
            {
                case DataKind.ObjectRef_B:
                    return dataMap.GetValue(reader.ReadByte());

                case DataKind.ObjectRef_S:
                    return dataMap.GetValue(reader.ReadUInt16());

                case DataKind.ObjectRef:
                    return dataMap.GetValue(reader.ReadInt32());

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
            int id = dataMap.GetNextId();

            Type type = this.ReadType();
            var instance = CreateInstance(type);

            dataMap.AddValue(id, instance);
            return instance;
        }

        private object CreateInstance(Type type)
        {
            if (this.binder == null)
            {
                return NoBinderException(type.FullName);
            }

            var reader = this.binder.GetReader(type);
            if (reader == null)
            {
                return NoReaderException(type.FullName);
            }

            return reader(this);
        }

        private Exception NoBinderException(string typeName)
        {
            throw new InvalidOperationException(string.Format("Cannot deserialize type '{0}', no binder supplied.".NeedsLocalization(), typeName));
        }

        private Exception NoReaderException(string typeName)
        {
            throw new InvalidOperationException(string.Format("Cannot deserialize type '{0}', it has no deserialization reader.".NeedsLocalization(), typeName));
        }
    }
}