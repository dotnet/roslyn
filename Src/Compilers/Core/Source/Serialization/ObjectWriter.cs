// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A class that writes both primitive values and non-cyclical object graphs to a stream that may be
    /// later read back using the ObjectReader class.
    /// </summary>
    internal class ObjectWriter : ObjectReaderWriterBase, IDisposable
    {
        private readonly BinaryWriter writer;
        private readonly ObjectWriterData dataMap;
        private readonly RecordingObjectBinder binder;
        private readonly CancellationToken cancellationToken;

        internal ObjectWriter(
            Stream stream,
            ObjectWriterData defaultData = null,
            RecordingObjectBinder binder = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            this.writer = new BinaryWriter(stream, MultiByteEncoding.Instance);
            this.dataMap = new ObjectWriterData(defaultData);
            this.binder = binder ?? new RecordingObjectBinder();
            this.cancellationToken = cancellationToken;
        }

        public ObjectBinder Binder
        {
            get { return this.binder; }
        }

        public void Dispose()
        {
            this.dataMap.Dispose();
        }

        /// <summary>
        /// Writes a Boolean value to the stream.
        /// </summary>
        public void WriteBoolean(bool value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a Byte value to the stream.
        /// </summary>
        public void WriteByte(byte value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a Char value to the stream.
        /// </summary>
        public void WriteChar(char ch)
        {
            // write as UInt16 because binary writer fails on chars that are unicode surrogates
            this.writer.Write((ushort)ch);
        }

        /// <summary>
        /// Writes a Decimal value to the stream.
        /// </summary>
        public void WriteDecimal(decimal value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a Double value to the stream.
        /// </summary>
        public void WriteDouble(double value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a Single value to the stream.
        /// </summary>
        public void WriteSingle(float value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a Int32 value to the stream.
        /// </summary>
        public void WriteInt32(int value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a Int64 value to the stream.
        /// </summary>
        public void WriteInt64(long value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a SByte value to the stream.
        /// </summary>
        public void WriteSByte(sbyte value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a Int16 value to the stream.
        /// </summary>
        public void WriteInt16(short value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a UInt32 value to the stream.
        /// </summary>
        public void WriteUInt32(uint value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a UInt64 value to the stream.
        /// </summary>
        public void WriteUInt64(ulong value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a UInt16 value to the stream.
        /// </summary>
        public void WriteUInt16(ushort value)
        {
            this.writer.Write(value);
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
                writer.Write((byte)value);
            }
            else if (value <= (ushort.MaxValue >> 2))
            {
                byte byte0 = (byte)(((value >> 8) & 0xFF) | Byte2Marker);
                byte byte1 = (byte)(value & 0xFF);

                // high-bytes to low-bytes
                writer.Write(byte0);
                writer.Write(byte1);
            }
            else if (value <= (uint.MaxValue >> 2))
            {
                // high-bytes to low-bytes
                byte byte0 = (byte)(((value >> 24) & 0xFF) | Byte4Marker);
                byte byte1 = (byte)((value >> 16) & 0xFF);
                byte byte2 = (byte)((value >> 8) & 0xFF);
                byte byte3 = (byte)(value & 0xFF);

                // hit-bits with 4-byte marker
                writer.Write(byte0);
                writer.Write(byte1);
                writer.Write(byte2);
                writer.Write(byte3);
            }
            else
            {
#if COMPILERCORE
                throw new ArgumentException(CodeAnalysisResources.ValueTooLargeToBeRepresented);
#else
                throw new ArgumentException(WorkspacesResources.ValueTooLargeToBeRepresented);
#endif
            }
        }

        /// <summary>
        /// Writes a String value to the stream.
        /// </summary>
        public void WriteString(string value)
        {
            if (value == null)
            {
                writer.Write((byte)DataKind.Null);
            }
            else
            {
                int id;
                if (dataMap.TryGetId(value, out id))
                {
                    Debug.Assert(id >= 0);
                    if (id <= byte.MaxValue)
                    {
                        writer.Write((byte)DataKind.StringRef_B);
                        writer.Write((byte)id);
                    }
                    else if (id <= ushort.MaxValue)
                    {
                        writer.Write((byte)DataKind.StringRef_S);
                        writer.Write((ushort)id);
                    }
                    else
                    {
                        writer.Write((byte)DataKind.StringRef);
                        writer.Write(id);
                    }
                }
                else
                {
                    dataMap.Add(value);
                    writer.Write((byte)DataKind.String);
                    writer.Write(value);
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
                writer.Write((byte)DataKind.Null);
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
                        writer.Write((byte)DataKind.Boolean_T);
                    }
                    else
                    {
                        writer.Write((byte)DataKind.Boolean_F);
                    }
                }
                else if (type == typeof(int))
                {
                    int v = (int)value;
                    if (v == 0)
                    {
                        writer.Write((byte)DataKind.Int32_Z);
                    }
                    else if (v >= 0 && v < byte.MaxValue)
                    {
                        writer.Write((byte)DataKind.Int32_B);
                        writer.Write((byte)v);
                    }
                    else if (v >= 0 && v < ushort.MaxValue)
                    {
                        writer.Write((byte)DataKind.Int32_S);
                        writer.Write((ushort)v);
                    }
                    else
                    {
                        writer.Write((byte)DataKind.Int32);
                        writer.Write(v);
                    }
                }
                else if (type == typeof(string))
                {
                    this.WriteString((string)value);
                }
                else if (type == typeof(short))
                {
                    writer.Write((byte)DataKind.Int16);
                    writer.Write((short)value);
                }
                else if (type == typeof(long))
                {
                    writer.Write((byte)DataKind.Int64);
                    writer.Write((long)value);
                }
                else if (type == typeof(char))
                {
                    writer.Write((byte)DataKind.Char);
                    this.WriteChar((char)value);
                }
                else if (type == typeof(sbyte))
                {
                    writer.Write((byte)DataKind.Int8);
                    writer.Write((sbyte)value);
                }
                else if (type == typeof(byte))
                {
                    writer.Write((byte)DataKind.UInt8);
                    writer.Write((byte)value);
                }
                else if (type == typeof(ushort))
                {
                    writer.Write((byte)DataKind.UInt16);
                    writer.Write((ushort)value);
                }
                else if (type == typeof(uint))
                {
                    writer.Write((byte)DataKind.UInt32);
                    writer.Write((uint)value);
                }
                else if (type == typeof(ulong))
                {
                    writer.Write((byte)DataKind.UInt64);
                    writer.Write((ulong)value);
                }
                else if (type == typeof(decimal))
                {
                    writer.Write((byte)DataKind.Decimal);
                    writer.Write((decimal)value);
                }
                else if (type == typeof(float))
                {
                    writer.Write((byte)DataKind.Float4);
                    writer.Write((float)value);
                }
                else if (type == typeof(double))
                {
                    writer.Write((byte)DataKind.Float8);
                    writer.Write((double)value);
                }
                else if (type == typeof(DateTime))
                {
                    writer.Write((byte)DataKind.DateTime);
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
            writer.Write((byte)DataKind.Enum);
            this.WriteType(enumType);

            var type = Enum.GetUnderlyingType(enumType);

            if (type == typeof(int))
            {
                writer.Write((int)value);
            }
            else if (type == typeof(short))
            {
                writer.Write((short)value);
            }
            else if (type == typeof(byte))
            {
                writer.Write((byte)value);
            }
            else if (type == typeof(long))
            {
                writer.Write((long)value);
            }
            else if (type == typeof(sbyte))
            {
                writer.Write((sbyte)value);
            }
            else if (type == typeof(ushort))
            {
                writer.Write((ushort)value);
            }
            else if (type == typeof(uint))
            {
                writer.Write((uint)value);
            }
            else if (type == typeof(ulong))
            {
                writer.Write((ulong)value);
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
                throw new InvalidOperationException(WorkspacesResources.ArraysWithMoreThanOneDimensionCannotBeSerialized);
#endif
            }

            int length = instance.GetLength(0);

            switch (length)
            {
                case 0:
                    writer.Write((byte)DataKind.Array_0);
                    break;
                case 1:
                    writer.Write((byte)DataKind.Array_1);
                    break;
                case 2:
                    writer.Write((byte)DataKind.Array_2);
                    break;
                case 3:
                    writer.Write((byte)DataKind.Array_3);
                    break;
                default:
                    writer.Write((byte)DataKind.Array);
                    this.WriteCompressedUInt((uint)length);
                    break;
            }

            this.WriteType(instance.GetType().GetElementType());

            for (int i = 0; i < length; i++)
            {
                this.WriteValue(instance.GetValue(i));
            }
        }

        private void WriteType(Type type)
        {
            int id;
            if (dataMap.TryGetId(type, out id))
            {
                Debug.Assert(id >= 0);
                if (id <= byte.MaxValue)
                {
                    writer.Write((byte)DataKind.TypeRef_B);
                    writer.Write((byte)id);
                }
                else if (id <= ushort.MaxValue)
                {
                    writer.Write((byte)DataKind.TypeRef_S);
                    writer.Write((ushort)id);
                }
                else
                {
                    writer.Write((byte)DataKind.TypeRef);
                    writer.Write(id);
                }
            }
            else
            {
                dataMap.Add(type);

                if (this.binder != null)
                {
                    this.binder.Record(type);
                }

                writer.Write((byte)DataKind.Type);

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
            this.cancellationToken.ThrowIfCancellationRequested();

            // write object ref if we already know this instance
            int id;
            if (dataMap.TryGetId(instance, out id))
            {
                Debug.Assert(id >= 0);
                if (id <= byte.MaxValue)
                {
                    writer.Write((byte)DataKind.ObjectRef_B);
                    writer.Write((byte)id);
                }
                else if (id <= ushort.MaxValue)
                {
                    writer.Write((byte)DataKind.ObjectRef_S);
                    writer.Write((ushort)id);
                }
                else
                {
                    writer.Write((byte)DataKind.ObjectRef);
                    writer.Write(id);
                }
            }
            else
            {
                // otherwise add this instance to the map
                dataMap.Add(instance);

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
            writer.Write((byte)DataKind.Object_W);

            Type type = instance.GetType();
            this.WriteType(type);

            if (this.binder != null)
            {
                this.binder.Record(instance);
            }

            instance.WriteTo(this);
        }

        private Exception NotWritableException(string typeName)
        {
            throw new InvalidOperationException(string.Format("The type '{0}' cannot be written, it does not implement IObjectWritable".NeedsLocalization(), typeName));
        }
    }
}