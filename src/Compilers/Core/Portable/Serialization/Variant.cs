// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Roslyn.Utilities
{
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

        public static Variant FromDateTime(DateTime value)
        {
            return new Variant(VariantKind.DateTime, value.ToBinary());
        }

        public static Variant FromDecimal(Decimal value)
        {
            return new Variant(VariantKind.Decimal, value);
        }

        public static Variant FromBoxedEnum(object value)
        {
            return new Variant(VariantKind.BoxedEnum, image: 0, instance: value);
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

        public DateTime AsDateTime()
        {
            Debug.Assert(Kind == VariantKind.DateTime);
            return DateTime.FromBinary((long)_image);
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
#if COMPILERCORE
                        throw new InvalidOperationException(CodeAnalysisResources.ArraysWithMoreThanOneDimensionCannotBeSerialized);
#else
                            throw new InvalidOperationException(WorkspacesResources.Arrays_with_more_than_one_dimension_cannot_be_serialized);
#endif
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
//                case VariantKind.ArrayHeader:
//                    return this.AsArrayHeader();
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
//                case VariantKind.ObjectHeader:
//                    return this.AsObjectHeader();
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