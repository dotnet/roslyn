// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class SpecialTypeExtensions
    {
        /// <summary>
        /// Checks if a type is considered a "built-in integral" by CLR.
        /// </summary>
        public static bool IsClrInteger(this SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if a type is a primitive of a fixed size.
        /// </summary>
        public static bool IsBlittable(this SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsValueType(this SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_Void:
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_Nullable_T:
                case SpecialType.System_DateTime:
                case SpecialType.System_TypedReference:
                case SpecialType.System_ArgIterator:
                case SpecialType.System_RuntimeArgumentHandle:
                case SpecialType.System_RuntimeFieldHandle:
                case SpecialType.System_RuntimeMethodHandle:
                case SpecialType.System_RuntimeTypeHandle:
                    return true;
                default:
                    return false;
            }
        }

        public static int SizeInBytes(this SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_SByte:
                    return sizeof(sbyte);
                case SpecialType.System_Byte:
                    return sizeof(byte);
                case SpecialType.System_Int16:
                    return sizeof(short);
                case SpecialType.System_UInt16:
                    return sizeof(ushort);
                case SpecialType.System_Int32:
                    return sizeof(int);
                case SpecialType.System_UInt32:
                    return sizeof(uint);
                case SpecialType.System_Int64:
                    return sizeof(long);
                case SpecialType.System_UInt64:
                    return sizeof(ulong);
                case SpecialType.System_Char:
                    return sizeof(char);
                case SpecialType.System_Single:
                    return sizeof(float);
                case SpecialType.System_Double:
                    return sizeof(double);
                case SpecialType.System_Boolean:
                    return sizeof(bool);

                case SpecialType.System_Decimal:
                    //This isn't in the spec, but it is handled by dev10
                    return sizeof(decimal);

                default:
                    // invalid
                    return 0;
            }
        }

        /// <summary>
        /// These special types are structs that contain fields of the same type
        /// (e.g. System.Int32 contains a field of type System.Int32).
        /// </summary>
        public static bool IsPrimitiveRecursiveStruct(this SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_Char:
                case SpecialType.System_Double:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_SByte:
                case SpecialType.System_Single:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsValidEnumUnderlyingType(this SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsNumericType(this SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if a type is considered a "built-in integral" by CLR.
        /// </summary>
        public static bool IsIntegralType(this SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsUnsignedIntegralType(this SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_Byte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsSignedIntegralType(this SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// For signed integer types return number of bits for their representation minus 1. 
        /// I.e. 7 for Int8, 31 for Int32, etc.
        /// Used for checking loop end condition for VB for loop.
        /// </summary>
        public static int VBForToShiftBits(this SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_SByte:
                    return 7;
                case SpecialType.System_Int16:
                    return 15;
                case SpecialType.System_Int32:
                    return 31;
                case SpecialType.System_Int64:
                    return 63;
                default:
                    throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(specialType);
            }
        }

        public static SpecialType FromRuntimeTypeOfLiteralValue(object value)
        {
            RoslynDebug.Assert(value != null);

            // Perf: Note that JIT optimizes each expression val.GetType() == typeof(T) to a single register comparison.
            // Also the checks are sorted by commonality of the checked types.

            if (value.GetType() == typeof(int))
            {
                return SpecialType.System_Int32;
            }

            if (value.GetType() == typeof(string))
            {
                return SpecialType.System_String;
            }

            if (value.GetType() == typeof(bool))
            {
                return SpecialType.System_Boolean;
            }

            if (value.GetType() == typeof(char))
            {
                return SpecialType.System_Char;
            }

            if (value.GetType() == typeof(long))
            {
                return SpecialType.System_Int64;
            }

            if (value.GetType() == typeof(double))
            {
                return SpecialType.System_Double;
            }

            if (value.GetType() == typeof(uint))
            {
                return SpecialType.System_UInt32;
            }

            if (value.GetType() == typeof(ulong))
            {
                return SpecialType.System_UInt64;
            }

            if (value.GetType() == typeof(float))
            {
                return SpecialType.System_Single;
            }

            if (value.GetType() == typeof(decimal))
            {
                return SpecialType.System_Decimal;
            }

            if (value.GetType() == typeof(short))
            {
                return SpecialType.System_Int16;
            }

            if (value.GetType() == typeof(ushort))
            {
                return SpecialType.System_UInt16;
            }

            if (value.GetType() == typeof(DateTime))
            {
                return SpecialType.System_DateTime;
            }

            if (value.GetType() == typeof(byte))
            {
                return SpecialType.System_Byte;
            }

            if (value.GetType() == typeof(sbyte))
            {
                return SpecialType.System_SByte;
            }

            return SpecialType.None;
        }

        /// <summary>
        /// Tells whether a different code path can be taken based on the fact, that a given type is a special type.
        /// This method is called in places where conditions like <c>specialType != SpecialType.None</c> were previously used.
        /// The main reason for this method to exist is to prevent such conditions, which introduce silent code changes every time a new special type is added.
        /// This doesn't mean the checked special type range of this method cannot be modified,
        /// but rather that each usage of this method needs to be reviewed to make sure everything works as expected in such cases
        /// </summary>
        public static bool CanOptimizeBehavior(this SpecialType specialType)
            => specialType is >= SpecialType.System_Object and <= SpecialType.System_Runtime_CompilerServices_InlineArrayAttribute;

        /// <summary>
        /// Convert a boxed primitive (generally of the backing type of an enum) into a ulong.
        /// </summary>
        internal static ulong ConvertUnderlyingValueToUInt64(this SpecialType enumUnderlyingType, object value)
        {
            RoslynDebug.Assert(value != null);
            Debug.Assert(value.GetType().IsPrimitive);

            unchecked
            {
                return enumUnderlyingType switch
                {
                    SpecialType.System_SByte => (ulong)(sbyte)value,
                    SpecialType.System_Int16 => (ulong)(short)value,
                    SpecialType.System_Int32 => (ulong)(int)value,
                    SpecialType.System_Int64 => (ulong)(long)value,
                    SpecialType.System_Byte => (byte)value,
                    SpecialType.System_UInt16 => (ushort)value,
                    SpecialType.System_UInt32 => (uint)value,
                    SpecialType.System_UInt64 => (ulong)value,
                    _ => throw ExceptionUtilities.UnexpectedValue(enumUnderlyingType),
                };
            }
        }

    }
}
