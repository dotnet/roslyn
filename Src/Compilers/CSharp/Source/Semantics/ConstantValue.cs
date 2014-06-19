// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Roslyn.Compilers.CSharp
{
    internal enum ConstantValueTypeDiscriminator
    {
        Null,
        SByte,
        Byte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Char,
        Boolean,
        Single,
        Double,
        String
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct ConstantValue
    {
        [FieldOffset(0)]
        public readonly ConstantValueTypeDiscriminator discriminator;

        [FieldOffset(4)]
        public readonly sbyte sbyteValue;
        
        [FieldOffset(4)]
        public readonly byte byteValue;
        
        [FieldOffset(4)]
        public readonly short int16Value;
        
        [FieldOffset(4)]
        public readonly ushort uint16Value;
        
        [FieldOffset(4)]
        public readonly int int32Value;
        
        [FieldOffset(4)]
        public readonly uint uint32Value;
        
        [FieldOffset(4)]
        public readonly long int64Value;
        
        [FieldOffset(4)]
        public readonly ulong uint64Value;
        
        [FieldOffset(4)]
        public readonly char charValue;
        
        [FieldOffset(4)]
        public readonly bool booleanValue;
        
        [FieldOffset(4)]
        public readonly float singleValue;

        [FieldOffset(4)]
        public readonly double doubleValue;

        [FieldOffset(12)]
        public readonly string stringValue;

        // TODO: Do these fields need to be offset at 8 and 16 for alignment on x64?
        // TODO: Decimal is not here because it's awful.

        public ConstantValue(object value)
        {
            Debug.Assert(BitConverter.IsLittleEndian);

            this = default(ConstantValue);
            var discriminator = GetTypeDiscriminator(value);
            switch (discriminator)
            {
                case ConstantValueTypeDiscriminator.Null: break;
                case ConstantValueTypeDiscriminator.SByte: this.sbyteValue = (sbyte)value; break;
                case ConstantValueTypeDiscriminator.Byte: this.byteValue = (byte)value; break;
                case ConstantValueTypeDiscriminator.Int16: this.int16Value = (short)value; break;
                case ConstantValueTypeDiscriminator.UInt16: this.uint16Value = (ushort)value; break;
                case ConstantValueTypeDiscriminator.Int32: this.int32Value = (int)value; break;
                case ConstantValueTypeDiscriminator.UInt32: this.uint32Value = (uint)value; break;
                case ConstantValueTypeDiscriminator.Int64: this.int64Value = (long)value; break;
                case ConstantValueTypeDiscriminator.UInt64: this.uint64Value = (ulong)value; break;
                case ConstantValueTypeDiscriminator.Char: this.charValue = (char)value; break;
                case ConstantValueTypeDiscriminator.Boolean: this.booleanValue = (bool)value; break;
                case ConstantValueTypeDiscriminator.Single: this.singleValue = (float)value; break;
                case ConstantValueTypeDiscriminator.Double: this.doubleValue = (double)value; break;
                case ConstantValueTypeDiscriminator.String: this.stringValue = (string)value; break;
                default: throw new InvalidOperationException();
            }

            this.discriminator = discriminator;
        }

        public object Value
        {
            get
            {
                switch (this.discriminator)
                {
                    case ConstantValueTypeDiscriminator.Null: return null;
                    case ConstantValueTypeDiscriminator.SByte: return sbyteValue;
                    case ConstantValueTypeDiscriminator.Byte: return byteValue;
                    case ConstantValueTypeDiscriminator.Int16: return int16Value;
                    case ConstantValueTypeDiscriminator.UInt16: return uint16Value;
                    case ConstantValueTypeDiscriminator.Int32: return int32Value;
                    case ConstantValueTypeDiscriminator.UInt32: return uint32Value;
                    case ConstantValueTypeDiscriminator.Int64: return int64Value;
                    case ConstantValueTypeDiscriminator.UInt64: return uint64Value;
                    case ConstantValueTypeDiscriminator.Char: return charValue;
                    case ConstantValueTypeDiscriminator.Boolean: return booleanValue;
                    case ConstantValueTypeDiscriminator.Single: return singleValue;
                    case ConstantValueTypeDiscriminator.Double: return doubleValue;
                    case ConstantValueTypeDiscriminator.String: return stringValue;
                    default: throw new InvalidOperationException();
                }
            }
        }

        public static ConstantValueTypeDiscriminator GetTypeDiscriminator(object value)
        {
            if (value == null) return ConstantValueTypeDiscriminator.Null;
            if (value is sbyte) return ConstantValueTypeDiscriminator.SByte;
            if (value is byte) return ConstantValueTypeDiscriminator.Byte;
            if (value is short) return ConstantValueTypeDiscriminator.Int16;
            if (value is ushort) return ConstantValueTypeDiscriminator.UInt16;
            if (value is int) return ConstantValueTypeDiscriminator.Int32;
            if (value is uint) return ConstantValueTypeDiscriminator.UInt32;
            if (value is long) return ConstantValueTypeDiscriminator.Int64;
            if (value is ulong) return ConstantValueTypeDiscriminator.UInt64;
            if (value is char) return ConstantValueTypeDiscriminator.Char;
            if (value is bool) return ConstantValueTypeDiscriminator.Boolean;
            if (value is float) return ConstantValueTypeDiscriminator.Single;
            if (value is double) return ConstantValueTypeDiscriminator.Double;
            if (value is string) return ConstantValueTypeDiscriminator.String;
            throw new InvalidOperationException();
        }

        public static bool operator ==(ConstantValue c1, ConstantValue c2)
        {
            return (c1.discriminator == c2.discriminator) && (c1.int64Value == c2.int64Value) && (c1.stringValue == c2.stringValue);
        }

        public static bool operator !=(ConstantValue c1, ConstantValue c2)
        {
            return !(c1 == c2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is ConstantValue))
            {
                return false;
            }

            return this == (ConstantValue)obj;
        }

        public override int GetHashCode()
        {
            int h1 = this.discriminator.GetHashCode();
            int h2 = this.int64Value.GetHashCode();
            int h3 = this.stringValue != null ? this.stringValue.GetHashCode() : 0;
            int h4 = ((h1 << 5) + h1) ^ h2;
            return ((h3 << 5) + h3) ^ h4;
        }

        public override string ToString()
        {
            string valueToDisplay;
            switch (this.discriminator)
            {
                case ConstantValueTypeDiscriminator.Null:
                    valueToDisplay = "null";
                    break;
                case ConstantValueTypeDiscriminator.String:
                    if (this.stringValue == null)
                        valueToDisplay = "null";
                    else
                        valueToDisplay = String.Format("\"{0}\"", this.stringValue);

                    break;
                default:
                    valueToDisplay = this.Value.ToString();
                    break;
            }

            return String.Format("{0}({1}: {2})", this.GetType().Name, valueToDisplay, this.discriminator);
        }
    }
}