// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class PrimitiveTypeCodeExtensions
    {
        public static bool Is64BitIntegral(this Cci.PrimitiveTypeCode kind)
        {
            switch (kind)
            {
                case Cci.PrimitiveTypeCode.Int64:
                case Cci.PrimitiveTypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsSigned(this Cci.PrimitiveTypeCode kind)
        {
            switch (kind)
            {
                case Cci.PrimitiveTypeCode.Int8:
                case Cci.PrimitiveTypeCode.Int16:
                case Cci.PrimitiveTypeCode.Int32:
                case Cci.PrimitiveTypeCode.Int64:
                case Cci.PrimitiveTypeCode.IntPtr:
                case Cci.PrimitiveTypeCode.Float32:
                case Cci.PrimitiveTypeCode.Float64:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsUnsigned(this Cci.PrimitiveTypeCode kind)
        {
            switch (kind)
            {
                case Cci.PrimitiveTypeCode.UInt8:
                case Cci.PrimitiveTypeCode.UInt16:
                case Cci.PrimitiveTypeCode.UInt32:
                case Cci.PrimitiveTypeCode.UInt64:
                case Cci.PrimitiveTypeCode.UIntPtr:
                case Cci.PrimitiveTypeCode.Char:
                case Cci.PrimitiveTypeCode.Pointer:
                case Cci.PrimitiveTypeCode.FunctionPointer:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsFloatingPoint(this Cci.PrimitiveTypeCode kind)
        {
            switch (kind)
            {
                case Cci.PrimitiveTypeCode.Float32:
                case Cci.PrimitiveTypeCode.Float64:
                    return true;
                default:
                    return false;
            }
        }

        public static ConstantValueTypeDiscriminator GetConstantValueTypeDiscriminator(this Cci.PrimitiveTypeCode type)
        {
            switch (type)
            {
                case Cci.PrimitiveTypeCode.Int8: return ConstantValueTypeDiscriminator.SByte;
                case Cci.PrimitiveTypeCode.UInt8: return ConstantValueTypeDiscriminator.Byte;
                case Cci.PrimitiveTypeCode.Int16: return ConstantValueTypeDiscriminator.Int16;
                case Cci.PrimitiveTypeCode.UInt16: return ConstantValueTypeDiscriminator.UInt16;
                case Cci.PrimitiveTypeCode.Int32: return ConstantValueTypeDiscriminator.Int32;
                case Cci.PrimitiveTypeCode.UInt32: return ConstantValueTypeDiscriminator.UInt32;
                case Cci.PrimitiveTypeCode.Int64: return ConstantValueTypeDiscriminator.Int64;
                case Cci.PrimitiveTypeCode.UInt64: return ConstantValueTypeDiscriminator.UInt64;
                case Cci.PrimitiveTypeCode.Char: return ConstantValueTypeDiscriminator.Char;
                case Cci.PrimitiveTypeCode.Boolean: return ConstantValueTypeDiscriminator.Boolean;
                case Cci.PrimitiveTypeCode.Float32: return ConstantValueTypeDiscriminator.Single;
                case Cci.PrimitiveTypeCode.Float64: return ConstantValueTypeDiscriminator.Double;
                case Cci.PrimitiveTypeCode.String: return ConstantValueTypeDiscriminator.String;
                default:
                    throw ExceptionUtilities.UnexpectedValue(type);
            }
        }
    }
}
