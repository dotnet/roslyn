// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.Cci
{
    internal static class BlobWriterImpl
    {
        // Performance considerations:
        // Ideally we wouldn't need to have duplicate implementations of the bellow methods and use 
        // the following pattern. However, the JIT currently doesn't inline the interface calls.
        // 
        //   interface IPrimitiveWriter
        //   {
        //       void WriteByte(byte value);
        //       void WriteUInt16BE(ushort value);
        //       void WriteUInt32BE(uint value);
        //       ...
        //   }
        // 
        //   static class BlobWriterImpl<T> where T : struct, IPrimitiveWriter
        //   {
        //       public static void WriteCompressedInteger(ref T writer, uint value)
        //       {
        //           if (...)
        //           {
        //               writer.WriteByte((byte)value);
        //           }
        //           else if (...)
        //           {
        //               writer.WriteUInt16BE((ushort)value);
        //           } 
        //           else if (...)
        //           {
        //               writer.WriteUInt32BE(value);
        //           } 
        //       }
        //   }

        internal const int SingleByteCompressedIntegerMaxValue = 0x7f;
        internal const int TwoByteCompressedIntegerMaxValue = 0x3fff;
        internal const int MaxCompressedIntegerValue = 0x1fffffff;

        internal static int GetCompressedIntegerSize(int value)
        {
            Debug.Assert(value <= MaxCompressedIntegerValue);

            if (value <= SingleByteCompressedIntegerMaxValue)
            {
                return 1;
            }

            if (value <= TwoByteCompressedIntegerMaxValue)
            {
                return 2;
            }

            return 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowValueArgumentOutOfRange()
        {
            throw new ArgumentOutOfRangeException("value");
        }

        internal static void WriteCompressedInteger(ref BlobWriter writer, uint value)
        {
            unchecked
            {
                if (value <= SingleByteCompressedIntegerMaxValue)
                {
                    writer.WriteByte((byte)value);
                }
                else if (value <= TwoByteCompressedIntegerMaxValue)
                {
                    writer.WriteUInt16BE((ushort)(0x8000 | value));
                }
                else if (value <= MaxCompressedIntegerValue)
                {
                    writer.WriteUInt32BE(0xc0000000 | value);
                }
                else
                {
                    ThrowValueArgumentOutOfRange();
                }
            }
        }

        internal static void WriteCompressedInteger(BlobBuilder writer, uint value)
        {
            unchecked
            {
                if (value <= SingleByteCompressedIntegerMaxValue)
                {
                    writer.WriteByte((byte)value);
                }
                else if (value <= TwoByteCompressedIntegerMaxValue)
                {
                    writer.WriteUInt16BE((ushort)(0x8000 | value));
                }
                else if (value <= MaxCompressedIntegerValue)
                {
                    writer.WriteUInt32BE(0xc0000000 | value);
                }
                else
                {
                    ThrowValueArgumentOutOfRange();
                }
            }
        }

        internal static void WriteCompressedSignedInteger(ref BlobWriter writer, int value)
        {
            unchecked
            {
                const int b6 = (1 << 6) - 1;
                const int b13 = (1 << 13) - 1;
                const int b28 = (1 << 28) - 1;

                // 0xffffffff for negative value
                // 0x00000000 for non-negative
                int signMask = value >> 31;

                if ((value & ~b6) == (signMask & ~b6))
                {
                    int n = ((value & b6) << 1) | (signMask & 1);
                    writer.WriteByte((byte)n);
                }
                else if ((value & ~b13) == (signMask & ~b13))
                {
                    int n = ((value & b13) << 1) | (signMask & 1);
                    writer.WriteUInt16BE((ushort)(0x8000 | n));
                }
                else if ((value & ~b28) == (signMask & ~b28))
                {
                    int n = ((value & b28) << 1) | (signMask & 1);
                    writer.WriteUInt32BE(0xc0000000 | (uint)n);
                }
                else
                {
                    ThrowValueArgumentOutOfRange();
                }
            }
        }

        internal static void WriteCompressedSignedInteger(BlobBuilder writer, int value)
        {
            unchecked
            {
                const int b6 = (1 << 6) - 1;
                const int b13 = (1 << 13) - 1;
                const int b28 = (1 << 28) - 1;

                // 0xffffffff for negative value
                // 0x00000000 for non-negative
                int signMask = value >> 31;

                if ((value & ~b6) == (signMask & ~b6))
                {
                    int n = ((value & b6) << 1) | (signMask & 1);
                    writer.WriteByte((byte)n);
                }
                else if ((value & ~b13) == (signMask & ~b13))
                {
                    int n = ((value & b13) << 1) | (signMask & 1);
                    writer.WriteUInt16BE((ushort)(0x8000 | n));
                }
                else if ((value & ~b28) == (signMask & ~b28))
                {
                    int n = ((value & b28) << 1) | (signMask & 1);
                    writer.WriteUInt32BE(0xc0000000 | (uint)n);
                }
                else
                {
                    ThrowValueArgumentOutOfRange();
                }
            }
        }

        internal static void WriteConstant(ref BlobWriter writer, object value)
        {
            if (value == null)
            {
                // The encoding of Type for the nullref value for FieldInit is ELEMENT_TYPE_CLASS with a Value of a 32-bit.
                writer.WriteUInt32(0);
                return;
            }

            var type = value.GetType();
            if (type.GetTypeInfo().IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            if (type == typeof(bool))
            {
                writer.WriteBoolean((bool)value);
            }
            else if (type == typeof(int))
            {
                writer.WriteInt32((int)value);
            }
            else if (type == typeof(string))
            {
                writer.WriteUTF16((string)value);
            }
            else if (type == typeof(byte))
            {
                writer.WriteByte((byte)value);
            }
            else if (type == typeof(char))
            {
                writer.WriteUInt16((char)value);
            }
            else if (type == typeof(double))
            {
                writer.WriteDouble((double)value);
            }
            else if (type == typeof(short))
            {
                writer.WriteInt16((short)value);
            }
            else if (type == typeof(long))
            {
                writer.WriteInt64((long)value);
            }
            else if (type == typeof(sbyte))
            {
                writer.WriteSByte((sbyte)value);
            }
            else if (type == typeof(float))
            {
                writer.WriteSingle((float)value);
            }
            else if (type == typeof(ushort))
            {
                writer.WriteUInt16((ushort)value);
            }
            else if (type == typeof(uint))
            {
                writer.WriteUInt32((uint)value);
            }
            else if (type == typeof(ulong))
            {
                writer.WriteUInt64((ulong)value);
            }
            else
            {
                // TODO: message
                throw new ArgumentException();
            }
        }

        internal static void WriteConstant(BlobBuilder writer, object value)
        {
            if (value == null)
            {
                // The encoding of Type for the nullref value for FieldInit is ELEMENT_TYPE_CLASS with a Value of a 32-bit.
                writer.WriteUInt32(0);
                return;
            }

            var type = value.GetType();
            if (type.GetTypeInfo().IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            if (type == typeof(bool))
            {
                writer.WriteBoolean((bool)value);
            }
            else if (type == typeof(int))
            {
                writer.WriteInt32((int)value);
            }
            else if (type == typeof(string))
            {
                writer.WriteUTF16((string)value);
            }
            else if (type == typeof(byte))
            {
                writer.WriteByte((byte)value);
            }
            else if (type == typeof(char))
            {
                writer.WriteUInt16((char)value);
            }
            else if (type == typeof(double))
            {
                writer.WriteDouble((double)value);
            }
            else if (type == typeof(short))
            {
                writer.WriteInt16((short)value);
            }
            else if (type == typeof(long))
            {
                writer.WriteInt64((long)value);
            }
            else if (type == typeof(sbyte))
            {
                writer.WriteSByte((sbyte)value);
            }
            else if (type == typeof(float))
            {
                writer.WriteSingle((float)value);
            }
            else if (type == typeof(ushort))
            {
                writer.WriteUInt16((ushort)value);
            }
            else if (type == typeof(uint))
            {
                writer.WriteUInt32((uint)value);
            }
            else if (type == typeof(ulong))
            {
                writer.WriteUInt64((ulong)value);
            }
            else
            {
                // TODO: message
                throw new ArgumentException();
            }
        }
    }
}
