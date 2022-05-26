// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                    //          obj  str  bool chr  i08  i16  i32  i64  u08  u16  u32  u64 nint nuint r32  r64  dec bool? chr? i08? i16? i32? i64? u08? u16? u32? u64?nint?nuint?r32? r64? dec? 
                    /*  obj */{ IDN, XRF, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB, UNB },
                    /*  str */{ IRF, IDN, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC },
                    /* bool */{ BOX, NOC, IDN, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NUL, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC },
                    /*  chr */{ BOX, NOC, NOC, IDN, XNM, XNM, NUM, NUM, XNM, NUM, NUM, NUM, NUM, NUM, NUM, NUM, NUM, NOC, NUL, XNL, XNL, NUL, NUL, XNL, NUL, NUL, NUL, NUL, NUL, NUL, NUL, NUL },
                    /*  i08 */{ BOX, NOC, NOC, XNM, IDN, NUM, NUM, NUM, XNM, XNM, XNM, XNM, NUM, XNM, NUM, NUM, NUM, NOC, XNL, NUL, NUL, NUL, NUL, XNL, XNL, XNL, XNL, NUL, XNL, NUL, NUL, NUL },
                    /*  i16 */{ BOX, NOC, NOC, XNM, XNM, IDN, NUM, NUM, XNM, XNM, XNM, XNM, NUM, XNM, NUM, NUM, NUM, NOC, XNL, XNL, NUL, NUL, NUL, XNL, XNL, XNL, XNL, NUL, XNL, NUL, NUL, NUL },
                    /*  i32 */{ BOX, NOC, NOC, XNM, XNM, XNM, IDN, NUM, XNM, XNM, XNM, XNM, NUM, XNM, NUM, NUM, NUM, NOC, XNL, XNL, XNL, NUL, NUL, XNL, XNL, XNL, XNL, NUL, XNL, NUL, NUL, NUL },
                    /*  i64 */{ BOX, NOC, NOC, XNM, XNM, XNM, XNM, IDN, XNM, XNM, XNM, XNM, XNM, XNM, NUM, NUM, NUM, NOC, XNL, XNL, XNL, XNL, NUL, XNL, XNL, XNL, XNL, XNL, XNL, NUL, NUL, NUL },
                    /*  u08 */{ BOX, NOC, NOC, XNM, XNM, NUM, NUM, NUM, IDN, NUM, NUM, NUM, NUM, NUM, NUM, NUM, NUM, NOC, XNL, XNL, NUL, NUL, NUL, NUL, NUL, NUL, NUL, NUL, NUL, NUL, NUL, NUL },
                    /*  u16 */{ BOX, NOC, NOC, XNM, XNM, XNM, NUM, NUM, XNM, IDN, NUM, NUM, NUM, NUM, NUM, NUM, NUM, NOC, XNL, XNL, XNL, NUL, NUL, XNL, NUL, NUL, NUL, NUL, NUL, NUL, NUL, NUL },
                    /*  u32 */{ BOX, NOC, NOC, XNM, XNM, XNM, XNM, NUM, XNM, XNM, IDN, NUM, XNM, NUM, NUM, NUM, NUM, NOC, XNL, XNL, XNL, XNL, NUL, XNL, XNL, NUL, NUL, XNL, NUL, NUL, NUL, NUL },
                    /*  u64 */{ BOX, NOC, NOC, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, IDN, XNM, XNM, NUM, NUM, NUM, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NUL, XNL, XNL, NUL, NUL, NUL },
                    /* nint */{ BOX, NOC, NOC, XNM, XNM, XNM, XNM, NUM, XNM, XNM, XNM, XNM, IDN, XNM, NUM, NUM, NUM, NOC, XNL, XNL, XNL, XNL, NUL, XNL, XNL, XNL, XNL, NUL, XNL, NUL, NUL, NUL },
                    /*nuint */{ BOX, NOC, NOC, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, NUM, XNM, IDN, NUM, NUM, NUM, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NUL, XNL, NUL, NUL, NUL, NUL },
                    /*  r32 */{ BOX, NOC, NOC, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, IDN, NUM, XNM, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NUL, NUL, XNL },
                    /*  r64 */{ BOX, NOC, NOC, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, IDN, XNM, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NUL, XNL },
                    /*  dec */{ BOX, NOC, NOC, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, XNM, IDN, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NUL },
                    /*bool? */{ BOX, NOC, XNL, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, IDN, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC },
                    /* chr? */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, IDN, XNL, XNL, NUL, NUL, XNL, NUL, NUL, NUL, NUL, NUL, NUL, NUL, NUL },
                    /* i08? */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, IDN, NUL, NUL, NUL, XNL, XNL, XNL, XNL, NUL, XNL, NUL, NUL, NUL },
                    /* i16? */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, IDN, NUL, NUL, XNL, XNL, XNL, XNL, NUL, XNL, NUL, NUL, NUL },
                    /* i32? */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, XNL, IDN, NUL, XNL, XNL, XNL, XNL, NUL, XNL, NUL, NUL, NUL },
                    /* i64? */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, XNL, XNL, IDN, XNL, XNL, XNL, XNL, XNL, XNL, NUL, NUL, NUL },
                    /* u08? */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, NUL, NUL, NUL, IDN, NUL, NUL, NUL, NUL, NUL, NUL, NUL, NUL },
                    /* u16? */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, XNL, NUL, NUL, XNL, IDN, NUL, NUL, NUL, NUL, NUL, NUL, NUL },
                    /* u32? */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, XNL, XNL, NUL, XNL, XNL, IDN, NUL, XNL, NUL, NUL, NUL, NUL },
                    /* u64? */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, IDN, XNL, XNL, NUL, NUL, NUL },
                    /*nint? */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, XNL, XNL, NUL, XNL, XNL, XNL, XNL, IDN, XNL, NUL, NUL, NUL },
                    /*nuint?*/{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NUL, XNL, IDN, NUL, NUL, NUL },
                    /* r32? */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, IDN, NUL, XNL },
                    /* r64? */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, IDN, XNL },
                    /* dec? */{ BOX, NOC, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, NOC, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, XNL, IDN }
               };
            }

            public static ConversionKind ClassifyConversion(TypeSymbol source, TypeSymbol target)
            {
                int sourceIndex = source.TypeToIndex();
                if (sourceIndex < 0)
                {
                    return ConversionKind.NoConversion;
                }
                int targetIndex = target.TypeToIndex();
                if (targetIndex < 0)
                {
                    return ConversionKind.NoConversion;
                }
                return (ConversionKind)s_convkind[sourceIndex, targetIndex];
            }
        }
    }
}
