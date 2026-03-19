// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal enum ConstantValueTypeDiscriminator : byte
    {
        Nothing,
        Null = Nothing,
        Bad,
        SByte,
        Byte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        NInt,
        NUInt,
        Char,
        Boolean,
        Single,
        Double,
        String,
        Decimal,
        DateTime,
        // Note: new values may need handling in CryptographicHashProvider.ComputeSourceHash
    }

    internal abstract partial class ConstantValue : IEquatable<ConstantValue?>, IFormattable
    {
        public abstract ConstantValueTypeDiscriminator Discriminator { get; }

        internal abstract SpecialType SpecialType { get; }

        public virtual string? StringValue { get { throw new InvalidOperationException(); } }
        internal virtual Rope? RopeValue { get { throw new InvalidOperationException(); } }

        public virtual bool BooleanValue { get { throw new InvalidOperationException(); } }

        public virtual sbyte SByteValue { get { throw new InvalidOperationException(); } }
        public virtual byte ByteValue { get { throw new InvalidOperationException(); } }

        // If we can get SByteValue, we can automatically get Int16Value, Int32Value, Int64Value. 
        // This is needed when constant values are reinterpreted during constant folding - 
        // for example a Byte value may be read via UIntValue accessor when folding Byte + Uint
        //
        // I have decided that default implementation of Int16Value in terms of SByteValue is appropriate here.
        // Same pattern is used for providing default implementation of Int32Value in terms of Int16Value and so on.
        //
        // An alternative solution would be to override Int16Value, Int32Value, Int64Value whenever I override SByteValue
        // and so on for Int16Value, Int32Value. That could work mildly faster but would result in a lot more code.

        public virtual short Int16Value { get { return SByteValue; } }
        public virtual ushort UInt16Value { get { return ByteValue; } }

        public virtual int Int32Value { get { return Int16Value; } }
        public virtual uint UInt32Value { get { return UInt16Value; } }

        public virtual long Int64Value { get { return Int32Value; } }
        public virtual ulong UInt64Value { get { return UInt32Value; } }

        public virtual char CharValue { get { throw new InvalidOperationException(); } }
        public virtual decimal DecimalValue { get { throw new InvalidOperationException(); } }
        public virtual DateTime DateTimeValue { get { throw new InvalidOperationException(); } }

        public virtual double DoubleValue { get { throw new InvalidOperationException(); } }
        public virtual float SingleValue { get { throw new InvalidOperationException(); } }

        // returns true if value is in its default (zero-inited) form.
        public virtual bool IsDefaultValue { get { return false; } }
        public virtual bool IsOne { get { return false; } }

        // NOTE: We do not have IsNumericZero. 
        //       The reason is that integral zeroes are same as default values
        //       and singles, floats and decimals have multiple zero values. 
        //       It appears that in all cases so far we considered isDefaultValue, and not about value being 
        //       arithmetic zero (especially when definition is ambiguous).

        public const ConstantValue? NotAvailable = null;

        public static ConstantValue Bad { get { return ConstantValueBad.Instance; } }
        public static ConstantValue Null { get { return ConstantValueNull.Instance; } }
        public static ConstantValue Nothing { get { return Null; } }
        // Null, Nothing and Unset are all ConstantValueNull. Null and Nothing are equivalent and represent the null and
        // nothing constants in C# and VB.  Unset indicates an uninitialized ConstantValue.
        public static ConstantValue Unset { get { return ConstantValueNull.Uninitialized; } }

        public static ConstantValue True { get { return ConstantValueOne.Boolean; } }
        public static ConstantValue False { get { return ConstantValueDefault.Boolean; } }

        public static ConstantValue Create(string? value)
        {
            if (value == null)
            {
                return Null;
            }

            return new ConstantValueString(value);
        }

        internal static ConstantValue CreateFromRope(Rope value)
        {
            RoslynDebug.Assert(value != null);
            return new ConstantValueString(value);
        }

        public static ConstantValue Create(char value)
        {
            if (value == default(char))
            {
                return ConstantValueDefault.Char;
            }
            else if (value == (char)1)
            {
                return ConstantValueOne.Char;
            }

            return new ConstantValueI16(value);
        }

        public static ConstantValue Create(sbyte value)
        {
            if (value == 0)
            {
                return ConstantValueDefault.SByte;
            }
            else if (value == 1)
            {
                return ConstantValueOne.SByte;
            }

            return new ConstantValueI8(value);
        }

        public static ConstantValue Create(byte value)
        {
            if (value == 0)
            {
                return ConstantValueDefault.Byte;
            }
            else if (value == 1)
            {
                return ConstantValueOne.Byte;
            }

            return new ConstantValueI8(value);
        }

        public static ConstantValue Create(Int16 value)
        {
            if (value == 0)
            {
                return ConstantValueDefault.Int16;
            }
            else if (value == 1)
            {
                return ConstantValueOne.Int16;
            }

            return new ConstantValueI16(value);
        }

        public static ConstantValue Create(UInt16 value)
        {
            if (value == 0)
            {
                return ConstantValueDefault.UInt16;
            }
            else if (value == 1)
            {
                return ConstantValueOne.UInt16;
            }

            return new ConstantValueI16(value);
        }

        public static ConstantValue Create(Int32 value)
        {
            if (value == 0)
            {
                return ConstantValueDefault.Int32;
            }
            else if (value == 1)
            {
                return ConstantValueOne.Int32;
            }

            return new ConstantValueI32(value);
        }

        public static ConstantValue Create(UInt32 value)
        {
            if (value == 0)
            {
                return ConstantValueDefault.UInt32;
            }
            else if (value == 1)
            {
                return ConstantValueOne.UInt32;
            }

            return new ConstantValueI32(value);
        }

        public static ConstantValue Create(Int64 value)
        {
            if (value == 0)
            {
                return ConstantValueDefault.Int64;
            }
            else if (value == 1)
            {
                return ConstantValueOne.Int64;
            }

            return new ConstantValueI64(value);
        }

        public static ConstantValue Create(UInt64 value)
        {
            if (value == 0)
            {
                return ConstantValueDefault.UInt64;
            }
            else if (value == 1)
            {
                return ConstantValueOne.UInt64;
            }

            return new ConstantValueI64(value);
        }

        public static ConstantValue CreateNativeInt(Int32 value)
        {
            if (value == 0)
            {
                return ConstantValueDefault.NInt;
            }
            else if (value == 1)
            {
                return ConstantValueOne.NInt;
            }

            return new ConstantValueNativeInt(value);
        }

        public static ConstantValue CreateNativeUInt(UInt32 value)
        {
            if (value == 0)
            {
                return ConstantValueDefault.NUInt;
            }
            else if (value == 1)
            {
                return ConstantValueOne.NUInt;
            }

            return new ConstantValueNativeInt(value);
        }

        public static ConstantValue Create(bool value)
        {
            if (value)
            {
                return ConstantValueOne.Boolean;
            }
            else
            {
                return ConstantValueDefault.Boolean;
            }
        }

        public static ConstantValue Create(float value)
        {
            if (BitConverter.DoubleToInt64Bits(value) == 0)
            {
                return ConstantValueDefault.Single;
            }
            else if (value == 1)
            {
                return ConstantValueOne.Single;
            }

            return new ConstantValueSingle(value);
        }

        public static ConstantValue CreateSingle(double value)
        {
            if (BitConverter.DoubleToInt64Bits(value) == 0)
            {
                return ConstantValueDefault.Single;
            }
            else if (value == 1)
            {
                return ConstantValueOne.Single;
            }

            return new ConstantValueSingle(value);
        }

        public static ConstantValue Create(double value)
        {
            if (BitConverter.DoubleToInt64Bits(value) == 0)
            {
                return ConstantValueDefault.Double;
            }
            else if (value == 1)
            {
                return ConstantValueOne.Double;
            }

            return new ConstantValueDouble(value);
        }

        public static ConstantValue Create(decimal value)
        {
            // The significant bits should be preserved even for Zero or One.
            // The fourth element of the returned array contains the scale factor and sign.
            int[] decimalBits = System.Decimal.GetBits(value);
            if (decimalBits[3] == 0)
            {
                if (value == 0)
                {
                    return ConstantValueDefault.Decimal;
                }
                else if (value == 1)
                {
                    return ConstantValueOne.Decimal;
                }
            }

            return new ConstantValueDecimal(value);
        }

        public static ConstantValue Create(DateTime value)
        {
            if (value == default(DateTime))
            {
                return ConstantValueDefault.DateTime;
            }

            return new ConstantValueDateTime(value);
        }

        public static ConstantValue Create(object value, SpecialType st)
        {
            var discriminator = GetDiscriminator(st);
            Debug.Assert(discriminator != ConstantValueTypeDiscriminator.Bad);
            return Create(value, discriminator);
        }

        public static ConstantValue? CreateSizeOf(SpecialType st)
        {
            int size = st.SizeInBytes();
            return (size == 0) ? null : ConstantValue.Create(size);
        }

        public static ConstantValue Create(object value, ConstantValueTypeDiscriminator discriminator)
        {
            switch (discriminator)
            {
                case ConstantValueTypeDiscriminator.Null: return Null;
                case ConstantValueTypeDiscriminator.SByte: return Create((sbyte)value);
                case ConstantValueTypeDiscriminator.Byte: return Create((byte)value);
                case ConstantValueTypeDiscriminator.Int16: return Create((short)value);
                case ConstantValueTypeDiscriminator.UInt16: return Create((ushort)value);
                case ConstantValueTypeDiscriminator.Int32: return Create((int)value);
                case ConstantValueTypeDiscriminator.UInt32: return Create((uint)value);
                case ConstantValueTypeDiscriminator.Int64: return Create((long)value);
                case ConstantValueTypeDiscriminator.UInt64: return Create((ulong)value);
                case ConstantValueTypeDiscriminator.NInt: return CreateNativeInt((int)value);
                case ConstantValueTypeDiscriminator.NUInt: return CreateNativeUInt((uint)value);
                case ConstantValueTypeDiscriminator.Char: return Create((char)value);
                case ConstantValueTypeDiscriminator.Boolean: return Create((bool)value);
                case ConstantValueTypeDiscriminator.Single:
                    // values for singles may actually have double precision
                    return value is double ?
                        CreateSingle((double)value) :
                        Create((float)value);
                case ConstantValueTypeDiscriminator.Double: return Create((double)value);
                case ConstantValueTypeDiscriminator.Decimal: return Create((decimal)value);
                case ConstantValueTypeDiscriminator.DateTime: return Create((DateTime)value);
                case ConstantValueTypeDiscriminator.String: return Create((string)value);
                default:
                    throw new InvalidOperationException();  //Not using ExceptionUtilities.UnexpectedValue() because this failure path is tested.
            }
        }

        public static ConstantValue Default(SpecialType st)
        {
            var discriminator = GetDiscriminator(st);
            Debug.Assert(discriminator != ConstantValueTypeDiscriminator.Bad);
            return Default(discriminator);
        }

        public static ConstantValue Default(ConstantValueTypeDiscriminator discriminator)
        {
            switch (discriminator)
            {
                case ConstantValueTypeDiscriminator.Bad: return Bad;

                case ConstantValueTypeDiscriminator.SByte: return ConstantValueDefault.SByte;
                case ConstantValueTypeDiscriminator.Byte: return ConstantValueDefault.Byte;
                case ConstantValueTypeDiscriminator.Int16: return ConstantValueDefault.Int16;
                case ConstantValueTypeDiscriminator.UInt16: return ConstantValueDefault.UInt16;
                case ConstantValueTypeDiscriminator.Int32: return ConstantValueDefault.Int32;
                case ConstantValueTypeDiscriminator.UInt32: return ConstantValueDefault.UInt32;
                case ConstantValueTypeDiscriminator.Int64: return ConstantValueDefault.Int64;
                case ConstantValueTypeDiscriminator.UInt64: return ConstantValueDefault.UInt64;
                case ConstantValueTypeDiscriminator.NInt: return ConstantValueDefault.NInt;
                case ConstantValueTypeDiscriminator.NUInt: return ConstantValueDefault.NUInt;
                case ConstantValueTypeDiscriminator.Char: return ConstantValueDefault.Char;
                case ConstantValueTypeDiscriminator.Boolean: return ConstantValueDefault.Boolean;
                case ConstantValueTypeDiscriminator.Single: return ConstantValueDefault.Single;
                case ConstantValueTypeDiscriminator.Double: return ConstantValueDefault.Double;
                case ConstantValueTypeDiscriminator.Decimal: return ConstantValueDefault.Decimal;
                case ConstantValueTypeDiscriminator.DateTime: return ConstantValueDefault.DateTime;

                case ConstantValueTypeDiscriminator.Null:
                case ConstantValueTypeDiscriminator.String: return Null;
            }

            throw ExceptionUtilities.UnexpectedValue(discriminator);
        }

        internal static ConstantValueTypeDiscriminator GetDiscriminator(SpecialType st)
        {
            switch (st)
            {
                case SpecialType.System_SByte: return ConstantValueTypeDiscriminator.SByte;
                case SpecialType.System_Byte: return ConstantValueTypeDiscriminator.Byte;
                case SpecialType.System_Int16: return ConstantValueTypeDiscriminator.Int16;
                case SpecialType.System_UInt16: return ConstantValueTypeDiscriminator.UInt16;
                case SpecialType.System_Int32: return ConstantValueTypeDiscriminator.Int32;
                case SpecialType.System_UInt32: return ConstantValueTypeDiscriminator.UInt32;
                case SpecialType.System_Int64: return ConstantValueTypeDiscriminator.Int64;
                case SpecialType.System_UInt64: return ConstantValueTypeDiscriminator.UInt64;
                case SpecialType.System_IntPtr: return ConstantValueTypeDiscriminator.NInt;
                case SpecialType.System_UIntPtr: return ConstantValueTypeDiscriminator.NUInt;
                case SpecialType.System_Char: return ConstantValueTypeDiscriminator.Char;
                case SpecialType.System_Boolean: return ConstantValueTypeDiscriminator.Boolean;
                case SpecialType.System_Single: return ConstantValueTypeDiscriminator.Single;
                case SpecialType.System_Double: return ConstantValueTypeDiscriminator.Double;
                case SpecialType.System_Decimal: return ConstantValueTypeDiscriminator.Decimal;
                case SpecialType.System_DateTime: return ConstantValueTypeDiscriminator.DateTime;
                case SpecialType.System_String: return ConstantValueTypeDiscriminator.String;
            }

            return ConstantValueTypeDiscriminator.Bad;
        }

        public string GetPrimitiveTypeName()
        {
            return Discriminator switch
            {
                ConstantValueTypeDiscriminator.SByte => "sbyte",
                ConstantValueTypeDiscriminator.Byte => "byte",
                ConstantValueTypeDiscriminator.Int16 => "short",
                ConstantValueTypeDiscriminator.UInt16 => "ushort",
                ConstantValueTypeDiscriminator.Int32 => "int",
                ConstantValueTypeDiscriminator.NInt => "nint",
                ConstantValueTypeDiscriminator.UInt32 => "uint",
                ConstantValueTypeDiscriminator.NUInt => "nuint",
                ConstantValueTypeDiscriminator.Int64 => "long",
                ConstantValueTypeDiscriminator.UInt64 => "ulong",
                ConstantValueTypeDiscriminator.Char => "char",
                ConstantValueTypeDiscriminator.Boolean => "bool",
                ConstantValueTypeDiscriminator.Single => "float",
                ConstantValueTypeDiscriminator.Double => "double",
                ConstantValueTypeDiscriminator.String => "string",
                ConstantValueTypeDiscriminator.Decimal => "decimal",
                ConstantValueTypeDiscriminator.DateTime => "DateTime",
                ConstantValueTypeDiscriminator.Null or ConstantValueTypeDiscriminator.Bad => throw ExceptionUtilities.UnexpectedValue(Discriminator),
                _ => throw ExceptionUtilities.UnexpectedValue(Discriminator)
            };
        }

        private static SpecialType GetSpecialType(ConstantValueTypeDiscriminator discriminator)
        {
            switch (discriminator)
            {
                case ConstantValueTypeDiscriminator.SByte: return SpecialType.System_SByte;
                case ConstantValueTypeDiscriminator.Byte: return SpecialType.System_Byte;
                case ConstantValueTypeDiscriminator.Int16: return SpecialType.System_Int16;
                case ConstantValueTypeDiscriminator.UInt16: return SpecialType.System_UInt16;
                case ConstantValueTypeDiscriminator.Int32: return SpecialType.System_Int32;
                case ConstantValueTypeDiscriminator.UInt32: return SpecialType.System_UInt32;
                case ConstantValueTypeDiscriminator.Int64: return SpecialType.System_Int64;
                case ConstantValueTypeDiscriminator.UInt64: return SpecialType.System_UInt64;
                case ConstantValueTypeDiscriminator.NInt: return SpecialType.System_IntPtr;
                case ConstantValueTypeDiscriminator.NUInt: return SpecialType.System_UIntPtr;
                case ConstantValueTypeDiscriminator.Char: return SpecialType.System_Char;
                case ConstantValueTypeDiscriminator.Boolean: return SpecialType.System_Boolean;
                case ConstantValueTypeDiscriminator.Single: return SpecialType.System_Single;
                case ConstantValueTypeDiscriminator.Double: return SpecialType.System_Double;
                case ConstantValueTypeDiscriminator.Decimal: return SpecialType.System_Decimal;
                case ConstantValueTypeDiscriminator.DateTime: return SpecialType.System_DateTime;
                case ConstantValueTypeDiscriminator.String: return SpecialType.System_String;
                default: return SpecialType.None;
            }
        }

        public object? Value
        {
            get
            {
                switch (this.Discriminator)
                {
                    case ConstantValueTypeDiscriminator.Bad: return null;
                    case ConstantValueTypeDiscriminator.Null: return null;
                    case ConstantValueTypeDiscriminator.SByte: return Boxes.Box(SByteValue);
                    case ConstantValueTypeDiscriminator.Byte: return Boxes.Box(ByteValue);
                    case ConstantValueTypeDiscriminator.Int16: return Boxes.Box(Int16Value);
                    case ConstantValueTypeDiscriminator.UInt16: return Boxes.Box(UInt16Value);
                    case ConstantValueTypeDiscriminator.Int32: return Boxes.Box(Int32Value);
                    case ConstantValueTypeDiscriminator.UInt32: return Boxes.Box(UInt32Value);
                    case ConstantValueTypeDiscriminator.Int64: return Boxes.Box(Int64Value);
                    case ConstantValueTypeDiscriminator.UInt64: return Boxes.Box(UInt64Value);
                    case ConstantValueTypeDiscriminator.NInt: return Boxes.Box(Int32Value);
                    case ConstantValueTypeDiscriminator.NUInt: return Boxes.Box(UInt32Value);
                    case ConstantValueTypeDiscriminator.Char: return Boxes.Box(CharValue);
                    case ConstantValueTypeDiscriminator.Boolean: return Boxes.Box(BooleanValue);
                    case ConstantValueTypeDiscriminator.Single: return Boxes.Box(SingleValue);
                    case ConstantValueTypeDiscriminator.Double: return Boxes.Box(DoubleValue);
                    case ConstantValueTypeDiscriminator.Decimal: return Boxes.Box(DecimalValue);
                    case ConstantValueTypeDiscriminator.DateTime: return DateTimeValue;
                    case ConstantValueTypeDiscriminator.String: return StringValue;
                    default: throw ExceptionUtilities.UnexpectedValue(this.Discriminator);
                }
            }
        }

        public static bool IsIntegralType(ConstantValueTypeDiscriminator discriminator)
        {
            switch (discriminator)
            {
                case ConstantValueTypeDiscriminator.SByte:
                case ConstantValueTypeDiscriminator.Byte:
                case ConstantValueTypeDiscriminator.Int16:
                case ConstantValueTypeDiscriminator.UInt16:
                case ConstantValueTypeDiscriminator.Int32:
                case ConstantValueTypeDiscriminator.UInt32:
                case ConstantValueTypeDiscriminator.Int64:
                case ConstantValueTypeDiscriminator.UInt64:
                case ConstantValueTypeDiscriminator.NInt:
                case ConstantValueTypeDiscriminator.NUInt:
                    return true;

                default:
                    return false;
            }
        }

        public bool IsIntegral
        {
            get
            {
                return IsIntegralType(this.Discriminator);
            }
        }

        public bool IsNegativeNumeric
        {
            get
            {
                switch (this.Discriminator)
                {
                    case ConstantValueTypeDiscriminator.SByte:
                        return SByteValue < 0;
                    case ConstantValueTypeDiscriminator.Int16:
                        return Int16Value < 0;
                    case ConstantValueTypeDiscriminator.Int32:
                    case ConstantValueTypeDiscriminator.NInt:
                        return Int32Value < 0;
                    case ConstantValueTypeDiscriminator.Int64:
                        return Int64Value < 0;
                    case ConstantValueTypeDiscriminator.Single:
                        return SingleValue < 0;
                    case ConstantValueTypeDiscriminator.Double:
                        return DoubleValue < 0;
                    case ConstantValueTypeDiscriminator.Decimal:
                        return DecimalValue < 0;

                    default:
                        return false;
                }
            }
        }

        public bool IsNumeric
        {
            get
            {
                switch (this.Discriminator)
                {
                    case ConstantValueTypeDiscriminator.SByte:
                    case ConstantValueTypeDiscriminator.Int16:
                    case ConstantValueTypeDiscriminator.Int32:
                    case ConstantValueTypeDiscriminator.Int64:
                    case ConstantValueTypeDiscriminator.Single:
                    case ConstantValueTypeDiscriminator.Double:
                    case ConstantValueTypeDiscriminator.Decimal:
                    case ConstantValueTypeDiscriminator.Byte:
                    case ConstantValueTypeDiscriminator.UInt16:
                    case ConstantValueTypeDiscriminator.UInt32:
                    case ConstantValueTypeDiscriminator.UInt64:
                    case ConstantValueTypeDiscriminator.NInt:
                    case ConstantValueTypeDiscriminator.NUInt:
                        return true;

                    default:
                        return false;
                }
            }
        }

        public static bool IsUnsignedIntegralType(ConstantValueTypeDiscriminator discriminator)
        {
            switch (discriminator)
            {
                case ConstantValueTypeDiscriminator.Byte:
                case ConstantValueTypeDiscriminator.UInt16:
                case ConstantValueTypeDiscriminator.UInt32:
                case ConstantValueTypeDiscriminator.UInt64:
                case ConstantValueTypeDiscriminator.NUInt:
                    return true;

                default:
                    return false;
            }
        }

        public bool IsUnsigned
        {
            get
            {
                return IsUnsignedIntegralType(this.Discriminator);
            }
        }

        public static bool IsBooleanType(ConstantValueTypeDiscriminator discriminator)
        {
            return discriminator == ConstantValueTypeDiscriminator.Boolean;
        }

        public bool IsBoolean
        {
            get
            {
                return this.Discriminator == ConstantValueTypeDiscriminator.Boolean;
            }
        }

        public static bool IsCharType(ConstantValueTypeDiscriminator discriminator)
        {
            return discriminator == ConstantValueTypeDiscriminator.Char;
        }

        public bool IsChar
        {
            get
            {
                return this.Discriminator == ConstantValueTypeDiscriminator.Char;
            }
        }

        public static bool IsStringType(ConstantValueTypeDiscriminator discriminator)
        {
            return discriminator == ConstantValueTypeDiscriminator.String;
        }

        [MemberNotNullWhen(true, nameof(StringValue))]
        public bool IsString
        {
            get
            {
                return this.Discriminator == ConstantValueTypeDiscriminator.String;
            }
        }

        public static bool IsDecimalType(ConstantValueTypeDiscriminator discriminator)
        {
            return discriminator == ConstantValueTypeDiscriminator.Decimal;
        }

        public bool IsDecimal
        {
            get
            {
                return this.Discriminator == ConstantValueTypeDiscriminator.Decimal;
            }
        }

        public static bool IsDateTimeType(ConstantValueTypeDiscriminator discriminator)
        {
            return discriminator == ConstantValueTypeDiscriminator.DateTime;
        }

        public bool IsDateTime
        {
            get
            {
                return this.Discriminator == ConstantValueTypeDiscriminator.DateTime;
            }
        }

        public static bool IsFloatingType(ConstantValueTypeDiscriminator discriminator)
        {
            return discriminator == ConstantValueTypeDiscriminator.Double ||
                discriminator == ConstantValueTypeDiscriminator.Single;
        }

        public bool IsFloating
        {
            get
            {
                return this.Discriminator == ConstantValueTypeDiscriminator.Double ||
                    this.Discriminator == ConstantValueTypeDiscriminator.Single;
            }
        }

        public bool IsBad
        {
            get
            {
                return this.Discriminator == ConstantValueTypeDiscriminator.Bad;
            }
        }

        public bool IsNull
        {
            get
            {
                return ReferenceEquals(this, Null);
            }
        }

        public bool IsNothing
        {
            get
            {
                return ReferenceEquals(this, Nothing);
            }
        }

        public void Serialize(BlobBuilder writer)
        {
            switch (this.Discriminator)
            {
                case ConstantValueTypeDiscriminator.Boolean:
                    writer.WriteBoolean(this.BooleanValue);
                    break;

                case ConstantValueTypeDiscriminator.SByte:
                    writer.WriteSByte(this.SByteValue);
                    break;

                case ConstantValueTypeDiscriminator.Byte:
                    writer.WriteByte(this.ByteValue);
                    break;

                case ConstantValueTypeDiscriminator.Char:
                case ConstantValueTypeDiscriminator.Int16:
                    writer.WriteInt16(this.Int16Value);
                    break;

                case ConstantValueTypeDiscriminator.UInt16:
                    writer.WriteUInt16(this.UInt16Value);
                    break;

                case ConstantValueTypeDiscriminator.Single:
                    writer.WriteSingle(this.SingleValue);
                    break;

                case ConstantValueTypeDiscriminator.Int32:
                    writer.WriteInt32(this.Int32Value);
                    break;

                case ConstantValueTypeDiscriminator.UInt32:
                    writer.WriteUInt32(this.UInt32Value);
                    break;

                case ConstantValueTypeDiscriminator.Double:
                    writer.WriteDouble(this.DoubleValue);
                    break;

                case ConstantValueTypeDiscriminator.Int64:
                    writer.WriteInt64(this.Int64Value);
                    break;

                case ConstantValueTypeDiscriminator.UInt64:
                    writer.WriteUInt64(this.UInt64Value);
                    break;

                default: throw ExceptionUtilities.UnexpectedValue(this.Discriminator);
            }
        }

        public override string ToString()
        {
            string? valueToDisplay = this.GetValueToDisplay();
            return String.Format("{0}({1}: {2})", this.GetType().Name, valueToDisplay, this.Discriminator);
        }

        public virtual string ToString(string? format, IFormatProvider? provider)
        {
            return Discriminator switch
            {
                ConstantValueTypeDiscriminator.SByte => SByteValue.ToString(provider),
                ConstantValueTypeDiscriminator.Byte => ByteValue.ToString(provider),
                ConstantValueTypeDiscriminator.Int16 => Int16Value.ToString(provider),
                ConstantValueTypeDiscriminator.UInt16 => UInt16Value.ToString(provider),
                ConstantValueTypeDiscriminator.NInt or ConstantValueTypeDiscriminator.Int32 => Int32Value.ToString(provider),
                ConstantValueTypeDiscriminator.NUInt or ConstantValueTypeDiscriminator.UInt32 => UInt32Value.ToString(provider),
                ConstantValueTypeDiscriminator.UInt64 => UInt64Value.ToString(provider),
                ConstantValueTypeDiscriminator.Int64 => Int64Value.ToString(provider),
                ConstantValueTypeDiscriminator.Char => CharValue.ToString(provider),
                ConstantValueTypeDiscriminator.Boolean => BooleanValue.ToString(provider),
                ConstantValueTypeDiscriminator.Single => SingleValue.ToString(provider),
                ConstantValueTypeDiscriminator.Double => DoubleValue.ToString(provider),
                ConstantValueTypeDiscriminator.Decimal => DecimalValue.ToString(provider),
                ConstantValueTypeDiscriminator.DateTime => DateTimeValue.ToString(provider),
                _ => throw ExceptionUtilities.UnexpectedValue(Discriminator)
            };
        }

        internal virtual string? GetValueToDisplay()
        {
            return this.Value?.ToString();
        }

        internal bool IsIntegralValueZeroOrOne(out bool isOne)
        {
            if (IsDefaultValue)
            {
                isOne = false;
            }
            else if (IsOne)
            {
                isOne = true;
            }
            else
            {
                isOne = default;
                return false;
            }

            return IsIntegral || IsBoolean || IsChar;
        }

        // equal constants must have matching discriminators
        // derived types override this if equivalence is more than just discriminators match. 
        // singletons also override this since they only need a reference compare.
        public virtual bool Equals(ConstantValue? other)
        {
            if (ReferenceEquals(other, this))
            {
                return true;
            }

            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return this.Discriminator == other.Discriminator;
        }

        public static bool operator ==(ConstantValue? left, ConstantValue? right)
        {
            if (ReferenceEquals(right, left))
            {
                return true;
            }

            if (ReferenceEquals(left, null))
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(ConstantValue? left, ConstantValue? right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return ((int)this.Discriminator).GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as ConstantValue);
        }
    }
}
