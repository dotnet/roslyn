// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class SpecialTypeExtensions
    {
        public static bool CanBeConst(this SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Byte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                case SpecialType.System_String:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsValidVolatileFieldType(this SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Char:
                case SpecialType.System_Single:
                case SpecialType.System_Boolean:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                    return true;
                default:
                    return false;
            }
        }

        public static int FixedBufferElementSizeInBytes(this SpecialType specialType)
        {
            // SizeInBytes() handles decimal (contrary to the language spec).  But decimal is not allowed
            // as a fixed buffer element type.
            return specialType == SpecialType.System_Decimal ? 0 : specialType.SizeInBytes();
        }
    }
}
