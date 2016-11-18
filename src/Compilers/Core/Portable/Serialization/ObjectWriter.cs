// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A class that writes both primitive values and non-cyclical object graphs to a stream that may be
    /// later read back using the ObjectReader class.
    /// </summary>
    internal sealed class ObjectWriter : ObjectReaderWriterBase, IDisposable
    {
        private readonly BinaryWriter _writer;
        private readonly ObjectWriterData _dataMap;
        private readonly RecordingObjectBinder _binder;
        private readonly CancellationToken _cancellationToken;

        internal ObjectWriter(
            Stream stream,
            ObjectWriterData defaultData = null,
            RecordingObjectBinder binder = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // String serialization assumes both reader and writer to be of the same endianness.
            // It can be adjusted for BigEndian if needed.
            Debug.Assert(BitConverter.IsLittleEndian);

            _writer = new BinaryWriter(stream, Encoding.UTF8);
            _dataMap = new ObjectWriterData(defaultData);
            _binder = binder ?? new SimpleRecordingObjectBinder();
            _cancellationToken = cancellationToken;
        }

        public ObjectBinder Binder
        {
            get { return _binder; }
        }

        public void Dispose()
        {
            _dataMap.Dispose();
        }

        /// <summary>
        /// Writes a Boolean value to the stream.
        /// </summary>
        public void WriteBoolean(bool value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a Byte value to the stream.
        /// </summary>
        public void WriteByte(byte value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a Char value to the stream.
        /// </summary>
        public void WriteChar(char ch)
        {
            // write as UInt16 because binary writer fails on chars that are unicode surrogates
            _writer.Write((ushort)ch);
        }

        /// <summary>
        /// Writes a Decimal value to the stream.
        /// </summary>
        public void WriteDecimal(decimal value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a Double value to the stream.
        /// </summary>
        public void WriteDouble(double value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a Single value to the stream.
        /// </summary>
        public void WriteSingle(float value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a Int32 value to the stream.
        /// </summary>
        public void WriteInt32(int value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a Int64 value to the stream.
        /// </summary>
        public void WriteInt64(long value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a SByte value to the stream.
        /// </summary>
        public void WriteSByte(sbyte value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a Int16 value to the stream.
        /// </summary>
        public void WriteInt16(short value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a UInt32 value to the stream.
        /// </summary>
        public void WriteUInt32(uint value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a UInt64 value to the stream.
        /// </summary>
        public void WriteUInt64(ulong value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a UInt16 value to the stream.
        /// </summary>
        public void WriteUInt16(ushort value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a DateTime value to the stream.
        /// </summary>
        public void WriteDateTime(DateTime value)
        {
            this.WriteInt64(value.ToBinary());
        }

        /// <summary>
        /// Writes a compressed 30 bit integer to the stream. (not 32 bit)
        /// </summary>
        public void WriteCompressedUInt(uint value)
        {
            if (value <= (byte.MaxValue >> 2))
            {
                _writer.Write((byte)value);
            }
            else if (value <= (ushort.MaxValue >> 2))
            {
                byte byte0 = (byte)(((value >> 8) & 0xFF) | Byte2Marker);
                byte byte1 = (byte)(value & 0xFF);

                // high-bytes to low-bytes
                _writer.Write(byte0);
                _writer.Write(byte1);
            }
            else if (value <= (uint.MaxValue >> 2))
            {
                // high-bytes to low-bytes
                byte byte0 = (byte)(((value >> 24) & 0xFF) | Byte4Marker);
                byte byte1 = (byte)((value >> 16) & 0xFF);
                byte byte2 = (byte)((value >> 8) & 0xFF);
                byte byte3 = (byte)(value & 0xFF);

                // hit-bits with 4-byte marker
                _writer.Write(byte0);
                _writer.Write(byte1);
                _writer.Write(byte2);
                _writer.Write(byte3);
            }
            else
            {
#if COMPILERCORE
                throw new ArgumentException(CodeAnalysisResources.ValueTooLargeToBeRepresented);
#else
                throw new ArgumentException(WorkspacesResources.Value_too_large_to_be_represented_as_a_30_bit_unsigned_integer);
#endif
            }
        }

        /// <summary>
        /// Writes a String value to the stream.
        /// </summary>
        public unsafe void WriteString(string value)
        {
            if (value == null)
            {
                _writer.Write((byte)DataKind.Null);
            }
            else
            {
                int id;
                if (_dataMap.TryGetId(value, out id))
                {
                    Debug.Assert(id >= 0);
                    if (id <= byte.MaxValue)
                    {
                        _writer.Write((byte)DataKind.StringRef_B);
                        _writer.Write((byte)id);
                    }
                    else if (id <= ushort.MaxValue)
                    {
                        _writer.Write((byte)DataKind.StringRef_S);
                        _writer.Write((ushort)id);
                    }
                    else
                    {
                        _writer.Write((byte)DataKind.StringRef);
                        _writer.Write(id);
                    }
                }
                else
                {
                    _dataMap.Add(value);

                    if (value.IsValidUnicodeString())
                    {
                        // Usual case - the string can be encoded as UTF8:
                        // We can use the UTF8 encoding of the binary writer.

                        _writer.Write((byte)DataKind.StringUtf8);
                        _writer.Write(value);
                    }
                    else
                    {
                        _writer.Write((byte)DataKind.StringUtf16);

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

        /// <summary>
        /// Writes any value (primitive or object graph) to the stream.
        /// </summary>
        public void WriteValue(object value)
        {
            if (value == null)
            {
                _writer.Write((byte)DataKind.Null);
            }
            else
            {
                var type = value.GetType();
                if (type.GetTypeInfo().IsEnum)
                {
                    WriteEnum(value, type);
                }
                else if (type == typeof(bool))
                {
                    if ((bool)value)
                    {
                        _writer.Write((byte)DataKind.Boolean_T);
                    }
                    else
                    {
                        _writer.Write((byte)DataKind.Boolean_F);
                    }
                }
                else if (type == typeof(int))
                {
                    int v = (int)value;
                    if (v == 0)
                    {
                        _writer.Write((byte)DataKind.Int32_Z);
                    }
                    else if (v >= 0 && v < byte.MaxValue)
                    {
                        _writer.Write((byte)DataKind.Int32_B);
                        _writer.Write((byte)v);
                    }
                    else if (v >= 0 && v < ushort.MaxValue)
                    {
                        _writer.Write((byte)DataKind.Int32_S);
                        _writer.Write((ushort)v);
                    }
                    else
                    {
                        _writer.Write((byte)DataKind.Int32);
                        _writer.Write(v);
                    }
                }
                else if (type == typeof(string))
                {
                    this.WriteString((string)value);
                }
                else if (type == typeof(short))
                {
                    _writer.Write((byte)DataKind.Int16);
                    _writer.Write((short)value);
                }
                else if (type == typeof(long))
                {
                    _writer.Write((byte)DataKind.Int64);
                    _writer.Write((long)value);
                }
                else if (type == typeof(char))
                {
                    _writer.Write((byte)DataKind.Char);
                    this.WriteChar((char)value);
                }
                else if (type == typeof(sbyte))
                {
                    _writer.Write((byte)DataKind.Int8);
                    _writer.Write((sbyte)value);
                }
                else if (type == typeof(byte))
                {
                    _writer.Write((byte)DataKind.UInt8);
                    _writer.Write((byte)value);
                }
                else if (type == typeof(ushort))
                {
                    _writer.Write((byte)DataKind.UInt16);
                    _writer.Write((ushort)value);
                }
                else if (type == typeof(uint))
                {
                    _writer.Write((byte)DataKind.UInt32);
                    _writer.Write((uint)value);
                }
                else if (type == typeof(ulong))
                {
                    _writer.Write((byte)DataKind.UInt64);
                    _writer.Write((ulong)value);
                }
                else if (type == typeof(decimal))
                {
                    _writer.Write((byte)DataKind.Decimal);
                    _writer.Write((decimal)value);
                }
                else if (type == typeof(float))
                {
                    _writer.Write((byte)DataKind.Float4);
                    _writer.Write((float)value);
                }
                else if (type == typeof(double))
                {
                    _writer.Write((byte)DataKind.Float8);
                    _writer.Write((double)value);
                }
                else if (type == typeof(DateTime))
                {
                    _writer.Write((byte)DataKind.DateTime);
                    this.WriteDateTime((DateTime)value);
                }
                else if (type.IsArray)
                {
                    this.WriteArray((Array)value);
                }
                else if (value is Type)
                {
                    this.WriteType((Type)value);
                }
                else
                {
                    this.WriteObject(value);
                }
            }
        }

        private void WriteEnum(object value, Type enumType)
        {
            _writer.Write((byte)DataKind.Enum);
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

        private void WriteArray(Array instance)
        {
            if (instance.Rank > 1)
            {
#if COMPILERCORE
                throw new InvalidOperationException(CodeAnalysisResources.ArraysWithMoreThanOneDimensionCannotBeSerialized);
#else
                throw new InvalidOperationException(WorkspacesResources.Arrays_with_more_than_one_dimension_cannot_be_serialized);
#endif
            }

            int length = instance.GetLength(0);

            switch (length)
            {
                case 0:
                    _writer.Write((byte)DataKind.Array_0);
                    break;
                case 1:
                    _writer.Write((byte)DataKind.Array_1);
                    break;
                case 2:
                    _writer.Write((byte)DataKind.Array_2);
                    break;
                case 3:
                    _writer.Write((byte)DataKind.Array_3);
                    break;
                default:
                    _writer.Write((byte)DataKind.Array);
                    this.WriteCompressedUInt((uint)length);
                    break;
            }

            // get type of array
            var elementType = instance.GetType().GetElementType();

            // optimization for primitive type array
            DataKind elementKind;
            if (s_typeMap.TryGetValue(elementType, out elementKind))
            {
                this.WritePrimitiveType(elementType, elementKind);
                this.WritePrimitiveTypeArrayElements(elementType, elementKind, instance);

                return;
            }

            // custom type case
            this.WriteType(elementType);
            foreach (var value in instance)
            {
                this.WriteValue(value);
            }
        }

        private void WritePrimitiveTypeArrayElements(Type type, DataKind kind, Array instance)
        {
            Debug.Assert(s_typeMap[type] == kind);

            // optimization for type underlying binary writer knows about
            if (type == typeof(byte))
            {
                _writer.Write((byte[])instance);
                return;
            }

            if (type == typeof(char))
            {
                _writer.Write((char[])instance);
                return;
            }

            // optimization for string which object writer has
            // its own optimization to reduce repeated string
            if (type == typeof(string))
            {
                WritePrimitiveTypeArrayElements((string[])instance, WriteString);
                return;
            }

            // optimization for bool array
            if (type == typeof(bool))
            {
                WriteBooleanArray((bool[])instance);
                return;
            }

            // otherwise, write elements directly to underlying binary writer
            switch (kind)
            {
                case DataKind.Int8:
                    WritePrimitiveTypeArrayElements((sbyte[])instance, _writer.Write);
                    return;
                case DataKind.Int16:
                    WritePrimitiveTypeArrayElements((short[])instance, _writer.Write);
                    return;
                case DataKind.Int32:
                    WritePrimitiveTypeArrayElements((int[])instance, _writer.Write);
                    return;
                case DataKind.Int64:
                    WritePrimitiveTypeArrayElements((long[])instance, _writer.Write);
                    return;
                case DataKind.UInt16:
                    WritePrimitiveTypeArrayElements((ushort[])instance, _writer.Write);
                    return;
                case DataKind.UInt32:
                    WritePrimitiveTypeArrayElements((uint[])instance, _writer.Write);
                    return;
                case DataKind.UInt64:
                    WritePrimitiveTypeArrayElements((ulong[])instance, _writer.Write);
                    return;
                case DataKind.Float4:
                    WritePrimitiveTypeArrayElements((float[])instance, _writer.Write);
                    return;
                case DataKind.Float8:
                    WritePrimitiveTypeArrayElements((double[])instance, _writer.Write);
                    return;
                case DataKind.Decimal:
                    WritePrimitiveTypeArrayElements((decimal[])instance, _writer.Write);
                    return;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private void WriteBooleanArray(bool[] array)
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

        private static void WritePrimitiveTypeArrayElements<T>(T[] array, Action<T> write)
        {
            for (var i = 0; i < array.Length; i++)
            {
                write(array[i]);
            }
        }

        private void WritePrimitiveType(Type type, DataKind kind)
        {
            Debug.Assert(s_typeMap[type] == kind);
            _writer.Write((byte)kind);
        }

        private void WriteType(Type type)
        {
            int id;
            if (_dataMap.TryGetId(type, out id))
            {
                Debug.Assert(id >= 0);
                if (id <= byte.MaxValue)
                {
                    _writer.Write((byte)DataKind.TypeRef_B);
                    _writer.Write((byte)id);
                }
                else if (id <= ushort.MaxValue)
                {
                    _writer.Write((byte)DataKind.TypeRef_S);
                    _writer.Write((ushort)id);
                }
                else
                {
                    _writer.Write((byte)DataKind.TypeRef);
                    _writer.Write(id);
                }
            }
            else
            {
                _dataMap.Add(type);

                _binder?.Record(type);

                _writer.Write((byte)DataKind.Type);

                string assemblyName = type.GetTypeInfo().Assembly.FullName;
                string typeName = type.FullName;

                // assembly name
                this.WriteString(assemblyName);

                // type name
                this.WriteString(typeName);
            }
        }

        private void WriteObject(object instance)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            // write object ref if we already know this instance
            int id;
            if (_dataMap.TryGetId(instance, out id))
            {
                Debug.Assert(id >= 0);
                if (id <= byte.MaxValue)
                {
                    _writer.Write((byte)DataKind.ObjectRef_B);
                    _writer.Write((byte)id);
                }
                else if (id <= ushort.MaxValue)
                {
                    _writer.Write((byte)DataKind.ObjectRef_S);
                    _writer.Write((ushort)id);
                }
                else
                {
                    _writer.Write((byte)DataKind.ObjectRef);
                    _writer.Write(id);
                }
            }
            else
            {
                // otherwise add this instance to the map
                _dataMap.Add(instance);

                var iwriteable = instance as IObjectWritable;
                if (iwriteable != null)
                {
                    this.WriteWritableObject(iwriteable);
                    return;
                }

                throw NotWritableException(instance.GetType().FullName);
            }
        }

        private void WriteWritableObject(IObjectWritable instance)
        {
            _writer.Write((byte)DataKind.Object_W);

            Type type = instance.GetType();
            this.WriteType(type);

            _binder?.Record(instance);

            instance.WriteTo(this);
        }

        private static Exception NotWritableException(string typeName)
        {
#if COMPILERCORE
            throw new InvalidOperationException(string.Format(CodeAnalysisResources.NotWritableException, typeName));
#else
            throw new InvalidOperationException(string.Format(WorkspacesResources.The_type_0_cannot_be_written_it_does_not_implement_IObjectWritable, typeName));
#endif
        }
    }
}
