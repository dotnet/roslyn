// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class ConversionsBase
    {
        private static class ConversionEasyOut
        {
            // There are situations in which we know that there is no unusual conversion going on
            // (such as a conversion involving constants, enumerated types, and so on.) In those
            // situations we can classify conversions via a simple table lookup:

            // PERF: Use byte instead of ConversionKind so the compiler can use array literal initialization.
            //       The most natural type choice, Enum arrays, are not blittable due to a CLR limitation.
            private static readonly byte[,] s_convkind;

            static ConversionEasyOut()
            {
                const byte IDN = (byte)ConversionKind.Identity;
                const byte IRF = (byte)ConversionKind.ImplicitReference;
                const byte XRF = (byte)ConversionKind.ExplicitReference;
                const byte XNM = (byte)ConversionKind.ExplicitNumeric;
                const byte NOC = (byte)ConversionKind.NoConversion;
                const byte BOX = (byte)ConversionKind.Boxing;
                const byte UNB = (byte)ConversionKind.Unboxing;
                const byte NUM = (byte)ConversionKind.ImplicitNumeric;
                const byte NUL = (byte)ConversionKind.ImplicitNullable;
                const byte XNL = (byte)ConversionKind.ExplicitNullable;

                s_convkind = new byte[,] {
                    // Converting Y to X:
                    //                    ----------------regular--------------------                      ----------------nullable-------------------
                    //          obj  str  bool chr  i08  i16  i32  i64  u08  u16  u32  u64 nint nuint r32  r64  dec  bool chr  i08  i16  i32  i64  u08  u16  u32  u64 nint nuint r32  r64  dec  
                    /*  obj */{ IDN, XRF, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB },
                    /*  str */{ IRF, IDN, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC },
                    /* bool */{ BOX, NOC, IDN, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NUL, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC },
                    /*  chr */{ BOX, NOC, NOC, IDN, XNM, XNM, NUM, NUM, XNM, NUM, NUM, NUM, NUM, NUM, NUM, NUM, NUM, NOC, NUL, XNL, XNL, NUL, NUL, XNL, NUL, NUL, NUL, NUL, NUL, NUL, NUL, NUL },
                    /*  i08 */{ BOX, NOC, NOC, XNM, IDN, NUM, NUM, NUM, XNM, XNM, XNM, XNM, NUM, NUM, NUM, NUM, NUM, NOC, XNL, NUL, NUL, NUL, NUL, XNL, XNL, XNL, XNL, NUL, NUL, NUL, NUL, NUL },
                    /*  i16 */{ BOX, NOC, NOC, XNM, XNM, IDN, NUM, NUM, XNM, XNM, XNM, XNM, NUM, NUM, NUM, NUM, NUM, NOC, XNL, XNL, NUL, NUL, NUL, XNL, XNL, XNL, XNL, NUL, NUL, NUL, NUL, NUL },
                    /*  i32 */{ BOX, NOC, NOC, XNM, XNM, XNM, IDN, NUM, XNM, XNM, XNM, XNM, NUM, XNM, NUM, NUM, NUM, NOC, XNL, XNL, XNL, NUL, NUL, XNL, XNL, XNL, XNL, NUL, NUL, NUL, NUL, NUL },
                    /*  i64 */{ BOX, NOC, NOC, XNM, XNM, XNM, XNM, IDN, XNM, XNM, XNM, XNM, XNM, XNM, NUM, NUM, NUM, NOC, XNL, XNL, XNL, XNL, NUL, XNL, XNL, XNL, XNL, NUL, NUL, NUL, NUL, NUL },
                    /*  u08 */{ BOX, NOC, NOC, XNM, XNM, NUM, NUM, NUM, IDN, NUM, NUM, NUM, NUM, NUM, NUM, NUM, NUM, NOC, XNL, XNL, NUL, NUL, NUL, NUL, NUL, NUL, NUL, NUL, NUL, NUL, NUL, NUL },
                    /*  u16 */{ BOX, NOC, NOC, XNM, XNM, XNM, NUM, NUM, XNM, IDN, NUM, NUM, NUM, NUM, NUM, NUM, NUM, NOC, XNL, XNL, XNL, NUL, NUL, XNL, NUL, NUL, NUL, NUL, NUL, NUL, NUL, NUL },
                    /*  u32 */{ BOX, NOC, NOC, XNM, XNM, XNM, XNM, NUM, XNM, XNM, IDN, NUM, XNM, NUM, NUM, NUM, NUM, NOC, XNL, XNL, XNL, XNL, NUL, XNL, XNL, NUL, NUL, NUL, NUL, NUL, NUL, NUL },
                    /*  u64 */{ BOX, NOC, NOC, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, IDN, XNM, NUM, NUM, NUM, NUM, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NUL, NUL, NUL, NUL, NUL, NUL },
                    /* nint */{ BOX, NOC, NOC, XNM, XNM, XNM, IDN, NUM, XNM, XNM, XNM, XNM, NUM, XNM, NUM, NUM, NOC, NOC, XNL, XNL, XNL, NUL, NUL, XNL, XNL, XNL, XNL, NUL, NUL, NUL, NUL, NUL },
                    /*nuint */{ BOX, NOC, NOC, XNM, XNM, XNM, XNM, NUM, XNM, XNM, IDN, NUM, XNM, NUM, NUM, NUM, NOC, NOC, XNL, XNL, XNL, XNL, NUL, XNL, XNL, NUL, NUL, NUL, NUL, NUL, NUL, NUL },
                    /*  r32 */{ BOX, NOC, NOC, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, IDN, NUM, XNM, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NUL, NUL, NUL, NUL, XNL },
                    /*  r64 */{ BOX, NOC, NOC, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, IDN, XNM, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NUL, NUL, XNL, NUL, XNL },
                    /*  dec */{ BOX, NOC, NOC, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, NOC, NOC, XNM, XNM, IDN, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NUL, NUL, XNL, XNL, NUL },
                    /*nbool */{ BOX, NOC, XNL, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, IDN, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC },
                    /* nchr */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, IDN, XNL, XNL, NUL, NUL, XNL, NUL, NUL, NUL, XNL, XNL, NUL, NUL, NUL },
                    /* ni08 */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, IDN, NUL, NUL, NUL, XNL, XNL, XNL, XNL, XNL, XNL, NUL, NUL, NUL },
                    /* ni16 */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, IDN, NUL, NUL, XNL, XNL, XNL, XNL, XNL, XNL, NUL, NUL, NUL },
                    /* ni32 */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, XNL, IDN, NUL, XNL, XNL, XNL, XNL, XNL, XNL, NUL, NUL, NUL },
                    /* ni64 */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, XNL, XNL, IDN, XNL, XNL, XNL, XNL, XNL, XNL, NUL, NUL, NUL },
                    /* nu08 */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, NUL, NUL, NUL, IDN, NUL, NUL, NUL, XNL, XNL, NUL, NUL, NUL },
                    /* nu16 */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, XNL, NUL, NUL, XNL, IDN, NUL, NUL, XNL, XNL, NUL, NUL, NUL },
                    /* nu32 */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, XNL, XNL, NUL, XNL, XNL, IDN, NUL, XNL, XNL, NUL, NUL, NUL },
                    /* nu64 */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, IDN, XNL, XNL, NUL, NUL, NUL },
                    /*nnint */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, NOC, XNL, XNL, XNL, IDN, NUL, XNL, XNL, XNL, XNL, XNL, XNL, NUL, NUL, NOC },
                    /*nnuint*/{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, NOC, XNL, XNL, XNL, XNL, NUL, XNL, XNL, IDN, NUL, XNL, XNL, NUL, NUL, NOC },
                    /* nr32 */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, IDN, NUL, XNL },
                    /* nr64 */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, IDN, XNL },
                    /* ndec */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, NOC, XNL, XNL, XNL, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, IDN }
               };
            }

            private static int TypeToIndex(TypeSymbol type)
            {
                switch (type.GetSpecialTypeSafe())
                {
                    case SpecialType.System_Object: return 0;
                    case SpecialType.System_String: return 1;
                    case SpecialType.System_Boolean: return 2;
                    case SpecialType.System_Char: return 3;
                    case SpecialType.System_SByte: return 4;
                    case SpecialType.System_Int16: return 5;
                    case SpecialType.System_Int32: return 6;
                    case SpecialType.System_Int64: return 7;
                    case SpecialType.System_Byte: return 8;
                    case SpecialType.System_UInt16: return 9;
                    case SpecialType.System_UInt32: return 10;
                    case SpecialType.System_UInt64: return 11;
                    case SpecialType.System_IntPtr when ((NamedTypeSymbol)type).IsNativeInt: return 12;
                    case SpecialType.System_UIntPtr when ((NamedTypeSymbol)type).IsNativeInt: return 13;
                    case SpecialType.System_Single: return 14;
                    case SpecialType.System_Double: return 15;
                    case SpecialType.System_Decimal: return 16;

                    case SpecialType.None:
                        if ((object)type != null && type.IsNullableType())
                        {
                            TypeSymbol underlyingType = type.GetNullableUnderlyingType();

                            switch (underlyingType.GetSpecialTypeSafe())
                            {
                                case SpecialType.System_Boolean: return 17;
                                case SpecialType.System_Char: return 18;
                                case SpecialType.System_SByte: return 19;
                                case SpecialType.System_Int16: return 20;
                                case SpecialType.System_Int32: return 21;
                                case SpecialType.System_Int64: return 22;
                                case SpecialType.System_Byte: return 23;
                                case SpecialType.System_UInt16: return 24;
                                case SpecialType.System_UInt32: return 25;
                                case SpecialType.System_UInt64: return 26;
                                case SpecialType.System_IntPtr when ((NamedTypeSymbol)type).IsNativeInt: return 27;
                                case SpecialType.System_UIntPtr when ((NamedTypeSymbol)type).IsNativeInt: return 28;
                                case SpecialType.System_Single: return 29;
                                case SpecialType.System_Double: return 30;
                                case SpecialType.System_Decimal: return 31;
                            }
                        }

                        // fall through
                        goto default;

                    default:
                        return -1;
                }
            }

            public static ConversionKind ClassifyConversion(TypeSymbol source, TypeSymbol target)
            {
                int sourceIndex = TypeToIndex(source);
                if (sourceIndex < 0)
                {
                    return ConversionKind.NoConversion;
                }
                int targetIndex = TypeToIndex(target);
                if (targetIndex < 0)
                {
                    return ConversionKind.NoConversion;
                }
                return (ConversionKind)s_convkind[sourceIndex, targetIndex];
            }
        }
    }
}
