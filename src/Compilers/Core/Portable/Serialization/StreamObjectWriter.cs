// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    /// A class that serializes objects to a stream.
    /// </summary>
    internal sealed class StreamObjectWriter : ObjectWriter, IDisposable
    {
        private readonly BinaryWriter _writer;
        private readonly ObjectWriterData _dataMap;
        private readonly RecordingObjectBinder _binder;
        private readonly CancellationToken _cancellationToken;
        private readonly Stack<Variant> _valueStack;
        private readonly ListWriter _valueWriter;

        internal StreamObjectWriter(
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
            _valueStack = new Stack<Variant>();
            _valueWriter = new ListWriter();
        }

        public ObjectBinder Binder
        {
            get { return _binder; }
        }

        public void Dispose()
        {
            _dataMap.Dispose();
        }

        public override void WriteBoolean(bool value)
        {
            _valueStack.Push(Variant.FromBoolean(value));
            Emit();
        }

        public override void WriteByte(byte value)
        {
            _valueStack.Push(Variant.FromByte(value));
            Emit();
        }

        public override void WriteChar(char ch)
        {
            _valueStack.Push(Variant.FromChar(ch));
            Emit();
        }

        public override void WriteDecimal(decimal value)
        {
            _valueStack.Push(Variant.FromDecimal(value));
            Emit();
        }

        public override void WriteDouble(double value)
        {
            _valueStack.Push(Variant.FromDouble(value));
            Emit();
        }

        public override void WriteSingle(float value)
        {
            _valueStack.Push(Variant.FromSingle(value));
            Emit();
        }

        public override void WriteInt32(int value)
        {
            _valueStack.Push(Variant.FromInt32(value));
            Emit();
        }

        public override void WriteInt64(long value)
        {
            _valueStack.Push(Variant.FromInt64(value));
            Emit();
        }

        public override void WriteSByte(sbyte value)
        {
            _valueStack.Push(Variant.FromSByte(value));
            Emit();
        }

        public override void WriteInt16(short value)
        {
            _valueStack.Push(Variant.FromInt16(value));
            Emit();
        }

        public override void WriteUInt32(uint value)
        {
            _valueStack.Push(Variant.FromUInt32(value));
            Emit();
        }

        public override void WriteUInt64(ulong value)
        {
            _valueStack.Push(Variant.FromUInt64(value));
            Emit();
        }

        public override void WriteUInt16(ushort value)
        {
            _valueStack.Push(Variant.FromUInt16(value));
            Emit();
        }

        public override void WriteDateTime(DateTime value)
        {
            _valueStack.Push(Variant.FromDateTime(value));
            Emit();
        }

        public override void WriteString(string value)
        {
            if (value == null)
            {
                _valueStack.Push(Variant.Null);
            }
            else
            {
                _valueStack.Push(Variant.FromString(value));
            }

            Emit();
        }

        public override void WriteValue(object value)
        {
            _valueStack.Push(Variant.FromBoxedObject(value));
            Emit();
        }

        private void Emit()
        {
            // keep reducing to primitives value that can be written to stream
            while (_valueStack.Count > 0)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var value = _valueStack.Pop();
                EmitOrReduce(value);
            }

            // write end marker for end of object serialization
            _writer.Write((byte)DataKind.End);
        }

        private void EmitOrReduce(Variant value)
        {
            switch (value.Kind)
            {
                case VariantKind.Null:
                    _writer.Write((byte)DataKind.Null);
                    break;

                case VariantKind.Boolean:
                    _writer.Write((byte)(value.AsBoolean() ? DataKind.Boolean_T : DataKind.Boolean_F));
                    break;

                case VariantKind.Byte:
                    _writer.Write((byte)DataKind.UInt8);
                    _writer.Write(value.AsByte());
                    break;

                case VariantKind.SByte:
                    _writer.Write((byte)DataKind.Int8);
                    _writer.Write(value.AsSByte());
                    break;

                case VariantKind.Int16:
                    _writer.Write((byte)DataKind.Int16);
                    _writer.Write(value.AsInt16());
                    break;

                case VariantKind.UInt16:
                    _writer.Write((byte)DataKind.UInt16);
                    _writer.Write(value.AsUInt16());
                    break;

                case VariantKind.Int32:
                    {
                        var v = value.AsInt32();
                        if (v >= 0 && v <= 10)
                        {
                            _writer.Write((byte)((int)DataKind.Int32_0 + v));
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
                    break;

                case VariantKind.UInt32:
                    {
                        var v = value.AsUInt32();
                        if (v >= 0 && v <= 10)
                        {
                            _writer.Write((byte)((int)DataKind.UInt32_0 + v));
                        }
                        else if (v >= 0 && v < byte.MaxValue)
                        {
                            _writer.Write((byte)DataKind.UInt32_B);
                            _writer.Write((byte)v);
                        }
                        else if (v >= 0 && v < ushort.MaxValue)
                        {
                            _writer.Write((byte)DataKind.UInt32_S);
                            _writer.Write((ushort)v);
                        }
                        else
                        {
                            _writer.Write((byte)DataKind.UInt32);
                            _writer.Write(v);
                        }
                    }
                    break;

                case VariantKind.Int64:
                    _writer.Write((byte)DataKind.Int64);
                    _writer.Write(value.AsInt64());
                    break;

                case VariantKind.UInt64:
                    _writer.Write((byte)DataKind.UInt64);
                    _writer.Write(value.AsUInt64());
                    break;

                case VariantKind.Decimal:
                    _writer.Write((byte)DataKind.Decimal);
                    _writer.Write(value.AsDecimal());
                    break;

                case VariantKind.Float4:
                    _writer.Write((byte)DataKind.Float4);
                    _writer.Write(value.AsSingle());
                    break;

                case VariantKind.Float8:
                    _writer.Write((byte)DataKind.Float8);
                    _writer.Write(value.AsDouble());
                    break;

                case VariantKind.DateTime:
                    _writer.Write((byte)DataKind.DateTime);
                    _writer.Write(value.AsDateTime().ToBinary());
                    break;

                case VariantKind.Char:
                    _writer.Write((byte)DataKind.Char);
                    _writer.Write((ushort)value.AsChar());  // write as ushort because write fails on chars that are unicode surrogates
                    break;

                case VariantKind.String:
                    EmitString(value.AsString());
                    break;

                case VariantKind.BoxedEnum:
                    var e = value.AsBoxedEnum();
                    EmitEnum(e, e.GetType());
                    break;

                case VariantKind.Type:
                    EmitType(value.AsType());
                    break;

                case VariantKind.Array:
                    EmitArray(value.AsArray());
                    break;

                case VariantKind.ArrayHeader:
                    EmitArrayHeader(value.AsArrayHeader());
                    break;

                case VariantKind.Object:
                    EmitOrReduceObject(value.AsObject());
                    break;

                case VariantKind.ObjectHeader:
                    uint memberCount;
                    EmitObjectHeader(value.AsObjectHeader(out memberCount), memberCount);
                    break;
            }
        }

        private class ListWriter : ObjectWriter
        {
            internal readonly List<Variant> _list;

            public ListWriter()
            {
                _list = new List<Variant>();
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

            public override void WriteDateTime(DateTime value)
            {
                _list.Add(Variant.FromDateTime(value));
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

        private void EmitCompressedUInt(uint value)
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

        private unsafe void EmitString(string value)
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

                        EmitCompressedUInt((uint)value.Length);
                        _writer.Write(bytes);
                    }
                }
            }
        }

        private void EmitEnum(object value, Type enumType)
        {
            _writer.Write((byte)DataKind.Enum);
            this.EmitType(enumType);

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

        private void EmitArray(Array array)
        {
            var elementType = array.GetType().GetElementType();

            DataKind elementKind;
            if (s_typeMap.TryGetValue(elementType, out elementKind))
            {
                int length = array.GetLength(0);

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
                        this.EmitCompressedUInt((uint)length);
                        break;
                }

                this.EmitPrimitiveType(elementType, elementKind);
                this.EmitPrimitiveTypeArrayElements(elementType, elementKind, array);
            }
            else
            {
                var list = _valueWriter._list;
                list.Clear();

                foreach (var element in array)
                {
                    _valueWriter.WriteValue(element);
                }

                // push header first so we write it to stream last
                _valueStack.Push(Variant.FromArrayHeader(array));

                // push elements in reverse order so we process first element first
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    _valueStack.Push(list[i]);
                }
            }
        }

        private void EmitArrayHeader(Array array)
        {
            int length = array.GetLength(0);

            switch (length)
            {
                case 0:
                    _writer.Write((byte)DataKind.ValueArray_0);
                    break;
                case 1:
                    _writer.Write((byte)DataKind.ValueArray_1);
                    break;
                case 2:
                    _writer.Write((byte)DataKind.ValueArray_2);
                    break;
                case 3:
                    _writer.Write((byte)DataKind.ValueArray_3);
                    break;
                default:
                    _writer.Write((byte)DataKind.ValueArray);
                    this.EmitCompressedUInt((uint)length);
                    break;
            }

            var elementType = array.GetType().GetElementType();
            this.EmitType(elementType);
        }

        private void EmitPrimitiveTypeArrayElements(Type type, DataKind kind, Array instance)
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
                EmitPrimitiveTypeArrayElements((string[])instance, EmitString);
            }
            else if (type == typeof(bool))
            {
                // optimization for bool array
                EmitBooleanArray((bool[])instance);
            }
            else
            {
                // otherwise, write elements directly to underlying binary writer
                switch (kind)
                {
                    case DataKind.Int8:
                        EmitPrimitiveTypeArrayElements((sbyte[])instance, _writer.Write);
                        return;
                    case DataKind.Int16:
                        EmitPrimitiveTypeArrayElements((short[])instance, _writer.Write);
                        return;
                    case DataKind.Int32:
                        EmitPrimitiveTypeArrayElements((int[])instance, _writer.Write);
                        return;
                    case DataKind.Int64:
                        EmitPrimitiveTypeArrayElements((long[])instance, _writer.Write);
                        return;
                    case DataKind.UInt16:
                        EmitPrimitiveTypeArrayElements((ushort[])instance, _writer.Write);
                        return;
                    case DataKind.UInt32:
                        EmitPrimitiveTypeArrayElements((uint[])instance, _writer.Write);
                        return;
                    case DataKind.UInt64:
                        EmitPrimitiveTypeArrayElements((ulong[])instance, _writer.Write);
                        return;
                    case DataKind.Float4:
                        EmitPrimitiveTypeArrayElements((float[])instance, _writer.Write);
                        return;
                    case DataKind.Float8:
                        EmitPrimitiveTypeArrayElements((double[])instance, _writer.Write);
                        return;
                    case DataKind.Decimal:
                        EmitPrimitiveTypeArrayElements((decimal[])instance, _writer.Write);
                        return;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(kind);
                }
            }
        }

        private void EmitBooleanArray(bool[] array)
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

        private static void EmitPrimitiveTypeArrayElements<T>(T[] array, Action<T> write)
        {
            for (var i = 0; i < array.Length; i++)
            {
                write(array[i]);
            }
        }

        private void EmitPrimitiveType(Type type, DataKind kind)
        {
            Debug.Assert(s_typeMap[type] == kind);
            _writer.Write((byte)kind);
        }

        private void EmitType(Type type)
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
                this.EmitString(assemblyName);

                // type name
                this.EmitString(typeName);
            }
        }

        private void EmitOrReduceObject(object instance)
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
                var iwriteable = instance as IObjectWritable;
                if (iwriteable != null)
                {
                    this.ReduceWritableObject(iwriteable);
                    return;
                }

                throw NotWritableException(instance.GetType().FullName);
            }
        }

        private void ReduceWritableObject(IObjectWritable instance)
        {
            var list = _valueWriter._list;
            list.Clear();

            instance.WriteTo(_valueWriter);

            // push first so we write header after all members
            _valueStack.Push(Variant.FromObjectHeader(instance, (uint)list.Count));

            // push all members in reverse order so we process the first member written first
            for (int i = list.Count - 1; i >= 0; i--)
            {
                _valueStack.Push(list[i]);
            }
        }

        private void EmitObjectHeader(object instance, uint memberCount)
        {
            _dataMap.Add(instance);

            _writer.Write((byte)DataKind.Object_W);

            Type type = instance.GetType();
            this.EmitType(type);
            this.EmitCompressedUInt(memberCount);

            _binder?.Record(instance);
        }

        private static Exception NotWritableException(string typeName)
        {
#if COMPILERCORE
            throw new InvalidOperationException(string.Format(CodeAnalysisResources.NotWritableException, typeName));
#else
            throw new InvalidOperationException(string.Format(WorkspacesResources.The_type_0_cannot_be_written_it_does_not_implement_IObjectWritable, typeName));
#endif
        }

        // we have s_typeMap and s_reversedTypeMap since there is no bidirectional map in compiler
        internal static readonly ImmutableDictionary<Type, DataKind> s_typeMap = ImmutableDictionary.CreateRange<Type, DataKind>(
            new KeyValuePair<Type, DataKind>[]
            {
                KeyValuePair.Create(typeof(bool), DataKind.BooleanType),
                KeyValuePair.Create(typeof(char), DataKind.Char),
                KeyValuePair.Create(typeof(string), DataKind.StringType),
                KeyValuePair.Create(typeof(sbyte), DataKind.Int8),
                KeyValuePair.Create(typeof(short), DataKind.Int16),
                KeyValuePair.Create(typeof(int), DataKind.Int32),
                KeyValuePair.Create(typeof(long), DataKind.Int64),
                KeyValuePair.Create(typeof(byte), DataKind.UInt8),
                KeyValuePair.Create(typeof(ushort), DataKind.UInt16),
                KeyValuePair.Create(typeof(uint), DataKind.UInt32),
                KeyValuePair.Create(typeof(ulong), DataKind.UInt64),
                KeyValuePair.Create(typeof(float), DataKind.Float4),
                KeyValuePair.Create(typeof(double), DataKind.Float8),
                KeyValuePair.Create(typeof(decimal), DataKind.Decimal),
            });

        internal static readonly ImmutableDictionary<DataKind, Type> s_reverseTypeMap = s_typeMap.ToImmutableDictionary(kv => kv.Value, kv => kv.Key);

        // byte marker for encoding compressed uint
        internal static readonly byte ByteMarkerMask = 3 << 6;
        internal static readonly byte Byte1Marker = 0;
        internal static readonly byte Byte2Marker = 1 << 6;
        internal static readonly byte Byte4Marker = 2 << 6;
    }
}
