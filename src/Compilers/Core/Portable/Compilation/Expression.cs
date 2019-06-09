// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    internal class Expression
    {
        public static ConstantValue SynthesizeNumeric(ITypeSymbol type, int value)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Int32:
                    return ConstantValue.Create(value);
                case SpecialType.System_Int64:
                    return ConstantValue.Create((long)value);
                case SpecialType.System_UInt32:
                    return ConstantValue.Create((uint)value);
                case SpecialType.System_UInt64:
                    return ConstantValue.Create((ulong)value);
                case SpecialType.System_UInt16:
                    return ConstantValue.Create((ushort)value);
                case SpecialType.System_Int16:
                    return ConstantValue.Create((short)value);
                case SpecialType.System_SByte:
                    return ConstantValue.Create((sbyte)value);
                case SpecialType.System_Byte:
                    return ConstantValue.Create((byte)value);
                case SpecialType.System_Char:
                    return ConstantValue.Create((char)value);
                case SpecialType.System_Boolean:
                    return ConstantValue.Create(value != 0);
                case SpecialType.System_Single:
                    return ConstantValue.Create((float)value);
                case SpecialType.System_Double:
                    return ConstantValue.Create((double)value);
                case SpecialType.System_Object:
                    return ConstantValue.Create(1, ConstantValueTypeDiscriminator.Int32);
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                return SynthesizeNumeric(((INamedTypeSymbol)type).EnumUnderlyingType, value);
            }

            return ConstantValue.Bad;
        }
    }
}
