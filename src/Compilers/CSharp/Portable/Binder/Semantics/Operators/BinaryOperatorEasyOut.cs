// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class OverloadResolution
    {
        internal static class BinopEasyOut
        {
            private const BinaryOperatorKind ERR = BinaryOperatorKind.Error;
            private const BinaryOperatorKind OBJ = BinaryOperatorKind.Object;
            private const BinaryOperatorKind STR = BinaryOperatorKind.String;
            private const BinaryOperatorKind OSC = BinaryOperatorKind.ObjectAndString;
            private const BinaryOperatorKind SOC = BinaryOperatorKind.StringAndObject;
            private const BinaryOperatorKind INT = BinaryOperatorKind.Int;
            private const BinaryOperatorKind UIN = BinaryOperatorKind.UInt;
            private const BinaryOperatorKind LNG = BinaryOperatorKind.Long;
            private const BinaryOperatorKind ULG = BinaryOperatorKind.ULong;
            private const BinaryOperatorKind NIN = BinaryOperatorKind.NInt;
            private const BinaryOperatorKind NUI = BinaryOperatorKind.NUInt;
            private const BinaryOperatorKind FLT = BinaryOperatorKind.Float;
            private const BinaryOperatorKind DBL = BinaryOperatorKind.Double;
            private const BinaryOperatorKind DEC = BinaryOperatorKind.Decimal;
            private const BinaryOperatorKind BOL = BinaryOperatorKind.Bool;
            private const BinaryOperatorKind LIN = BinaryOperatorKind.Lifted | BinaryOperatorKind.Int;
            private const BinaryOperatorKind LUN = BinaryOperatorKind.Lifted | BinaryOperatorKind.UInt;
            private const BinaryOperatorKind LLG = BinaryOperatorKind.Lifted | BinaryOperatorKind.Long;
            private const BinaryOperatorKind LUL = BinaryOperatorKind.Lifted | BinaryOperatorKind.ULong;
            private const BinaryOperatorKind LNI = BinaryOperatorKind.Lifted | BinaryOperatorKind.NInt;
            private const BinaryOperatorKind LNU = BinaryOperatorKind.Lifted | BinaryOperatorKind.NUInt;
            private const BinaryOperatorKind LFL = BinaryOperatorKind.Lifted | BinaryOperatorKind.Float;
            private const BinaryOperatorKind LDB = BinaryOperatorKind.Lifted | BinaryOperatorKind.Double;
            private const BinaryOperatorKind LDC = BinaryOperatorKind.Lifted | BinaryOperatorKind.Decimal;
            private const BinaryOperatorKind LBL = BinaryOperatorKind.Lifted | BinaryOperatorKind.Bool;

            // Overload resolution for Y * / - % < > <= >= X
            private static readonly BinaryOperatorKind[,] s_arithmetic =
            {
                //          obj  str  bool chr  i08  i16  i32  i64  u08  u16  u32  u64 nint nuint r32  r64  dec bool? chr? i08? i16? i32? i64? u08? u16? u32? u64?nint?nuint?r32? r64? dec? 
                /*  obj */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  str */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* bool */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  chr */{ ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, NIN, NUI, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /*  i08 */{ ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, NIN, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /*  i16 */{ ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, NIN, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /*  i32 */{ ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, NIN, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /*  i64 */{ ERR, ERR, ERR, LNG, LNG, LNG, LNG, LNG, LNG, LNG, LNG, ERR, LNG, ERR, FLT, DBL, DEC, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LLG, ERR, LFL, LDB, LDC },
                /*  u08 */{ ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, NIN, NUI, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /*  u16 */{ ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, NIN, NUI, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /*  u32 */{ ERR, ERR, ERR, UIN, LNG, LNG, LNG, LNG, UIN, UIN, UIN, ULG, LNG, NUI, FLT, DBL, DEC, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LLG, LNU, LFL, LDB, LDC },
                /*  u64 */{ ERR, ERR, ERR, ULG, ERR, ERR, ERR, ERR, ULG, ULG, ULG, ULG, ERR, ULG, FLT, DBL, DEC, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LFL, LDB, LDC },
                /* nint */{ ERR, ERR, ERR, NIN, NIN, NIN, NIN, LNG, NIN, NIN, LNG, ERR, NIN, ERR, FLT, DBL, DEC, ERR, LNI, LNI, LNI, LNI, LLG, LNI, LNI, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /*nuint */{ ERR, ERR, ERR, NUI, ERR, ERR, ERR, ERR, NUI, NUI, NUI, ULG, ERR, NUI, FLT, DBL, DEC, ERR, LNU, ERR, ERR, ERR, ERR, LNU, LNU, LNU, LUL, ERR, LNU, LFL, LDB, LDC },
                /*  r32 */{ ERR, ERR, ERR, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, DBL, ERR, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR },
                /*  r64 */{ ERR, ERR, ERR, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, ERR, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR },
                /*  dec */{ ERR, ERR, ERR, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, ERR, ERR, DEC, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC },
                /*bool? */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* chr? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /* i08? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /* i16? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /* i32? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /* i64? */{ ERR, ERR, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LLG, ERR, LFL, LDB, LDC, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LLG, ERR, LFL, LDB, LDC },
                /* u08? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /* u16? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /* u32? */{ ERR, ERR, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LLG, LNU, LFL, LDB, LDC, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LLG, LNU, LFL, LDB, LDC },
                /* u64? */{ ERR, ERR, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LFL, LDB, LDC, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LFL, LDB, LDC },
                /*nint? */{ ERR, ERR, ERR, LNI, LNI, LNI, LNI, LLG, LNI, LNI, LLG, ERR, LNI, ERR, LFL, LDB, LDC, ERR, LNI, LNI, LNI, LNI, LLG, LNI, LNI, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /*nuint?*/{ ERR, ERR, ERR, LNU, ERR, ERR, ERR, ERR, LNU, LNU, LNU, LUL, ERR, LNU, LFL, LDB, LDC, ERR, LNU, ERR, ERR, ERR, ERR, LNU, LNU, LNU, LUL, ERR, LNU, LFL, LDB, LDC },
                /* r32? */{ ERR, ERR, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR },
                /* r64? */{ ERR, ERR, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR },
                /* dec? */{ ERR, ERR, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC },
            };

            // Overload resolution for Y + X
            private static readonly BinaryOperatorKind[,] s_addition =
            {
                //          obj  str  bool chr  i08  i16  i32  i64  u08  u16  u32  u64 nint nuint r32  r64  dec bool? chr? i08? i16? i32? i64? u08? u16? u32? u64?nint?nuint?r32? r64? dec? 
                /*  obj */{ ERR, OSC, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  str */{ SOC, STR, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC },
                /* bool */{ ERR, OSC, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  chr */{ ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, NIN, NUI, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /*  i08 */{ ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, NIN, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /*  i16 */{ ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, NIN, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /*  i32 */{ ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, NIN, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /*  i64 */{ ERR, OSC, ERR, LNG, LNG, LNG, LNG, LNG, LNG, LNG, LNG, ERR, LNG, ERR, FLT, DBL, DEC, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LLG, ERR, LFL, LDB, LDC },
                /*  u08 */{ ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, NIN, NUI, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /*  u16 */{ ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, NIN, NUI, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /*  u32 */{ ERR, OSC, ERR, UIN, LNG, LNG, LNG, LNG, UIN, UIN, UIN, ULG, LNG, NUI, FLT, DBL, DEC, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LLG, LNU, LFL, LDB, LDC },
                /*  u64 */{ ERR, OSC, ERR, ULG, ERR, ERR, ERR, ERR, ULG, ULG, ULG, ULG, ERR, ULG, FLT, DBL, DEC, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LFL, LDB, LDC },
                /* nint */{ ERR, OSC, ERR, NIN, NIN, NIN, NIN, LNG, NIN, NIN, LNG, ERR, NIN, ERR, FLT, DBL, DEC, ERR, LNI, LNI, LNI, LNI, LLG, LNI, LNI, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /*nuint */{ ERR, OSC, ERR, NUI, ERR, ERR, ERR, ERR, NUI, NUI, NUI, ULG, ERR, NUI, FLT, DBL, DEC, ERR, LNU, ERR, ERR, ERR, ERR, LNU, LNU, LNU, LUL, ERR, LNU, LFL, LDB, LDC },
                /*  r32 */{ ERR, OSC, ERR, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, DBL, ERR, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR },
                /*  r64 */{ ERR, OSC, ERR, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, ERR, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR },
                /*  dec */{ ERR, OSC, ERR, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, ERR, ERR, DEC, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC },
                /*bool? */{ ERR, OSC, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* chr? */{ ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /* i08? */{ ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /* i16? */{ ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /* i32? */{ ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /* i64? */{ ERR, OSC, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LLG, ERR, LFL, LDB, LDC, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LLG, ERR, LFL, LDB, LDC },
                /* u08? */{ ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /* u16? */{ ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /* u32? */{ ERR, OSC, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LLG, LNU, LFL, LDB, LDC, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LLG, LNU, LFL, LDB, LDC },
                /* u64? */{ ERR, OSC, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LFL, LDB, LDC, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LFL, LDB, LDC },
                /*nint? */{ ERR, OSC, ERR, LNI, LNI, LNI, LNI, LLG, LNI, LNI, LLG, ERR, LNI, ERR, LFL, LDB, LDC, ERR, LNI, LNI, LNI, LNI, LLG, LNI, LNI, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /*nuint?*/{ ERR, OSC, ERR, LNU, ERR, ERR, ERR, ERR, LNU, LNU, LNU, LUL, ERR, LNU, LFL, LDB, LDC, ERR, LNU, ERR, ERR, ERR, ERR, LNU, LNU, LNU, LUL, ERR, LNU, LFL, LDB, LDC },
                /* r32? */{ ERR, OSC, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR },
                /* r64? */{ ERR, OSC, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR },
                /* dec? */{ ERR, OSC, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC },
            };

            // Overload resolution for Y << >> >>> X
            private static readonly BinaryOperatorKind[,] s_shift =
            {
                //          obj  str  bool chr  i08  i16  i32  i64  u08  u16  u32  u64 nint nuint r32  r64  dec bool? chr? i08? i16? i32? i64? u08? u16? u32? u64?nint?nuint?r32? r64? dec? 
                /*  obj */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  str */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* bool */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  chr */{ ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  i08 */{ ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  i16 */{ ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  i32 */{ ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  i64 */{ ERR, ERR, ERR, LNG, LNG, LNG, LNG, ERR, LNG, LNG, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LLG, LLG, LLG, LLG, ERR, LLG, LLG, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  u08 */{ ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  u16 */{ ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  u32 */{ ERR, ERR, ERR, UIN, UIN, UIN, UIN, ERR, UIN, UIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LUN, LUN, LUN, LUN, ERR, LUN, LUN, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  u64 */{ ERR, ERR, ERR, ULG, ULG, ULG, ULG, ERR, ULG, ULG, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LUL, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* nint */{ ERR, ERR, ERR, NIN, NIN, NIN, NIN, ERR, NIN, NIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LNI, LNI, LNI, LNI, ERR, LNI, LNI, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*nuint */{ ERR, ERR, ERR, NUI, NUI, NUI, NUI, ERR, NUI, NUI, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LNU, LNU, LNU, LNU, ERR, LNU, LNU, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  r32 */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  r64 */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  dec */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*bool? */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* chr? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* i08? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* i16? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* i32? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* i64? */{ ERR, ERR, ERR, LLG, LLG, LLG, LLG, ERR, LLG, LLG, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LLG, LLG, LLG, LLG, ERR, LLG, LLG, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* u08? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* u16? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* u32? */{ ERR, ERR, ERR, LUN, LUN, LUN, LUN, ERR, LUN, LUN, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LUN, LUN, LUN, LUN, ERR, LUN, LUN, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* u64? */{ ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LUL, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LUL, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*nint? */{ ERR, ERR, ERR, LNI, LNI, LNI, LNI, ERR, LNI, LNI, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LNI, LNI, LNI, LNI, ERR, LNI, LNI, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*nuint?*/{ ERR, ERR, ERR, LNU, LNU, LNU, LNU, ERR, LNU, LNU, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LNU, LNU, LNU, LNU, ERR, LNU, LNU, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* r32? */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* r64? */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* dec? */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
            };

            // Overload resolution for Y == != X
            // Note that these are the overload resolution rules; overload resolution might pick an invalid operator.
            // For example, overload resolution on object == decimal chooses the object/object overload, which then
            // is not legal because decimal must be a reference type. But we don't know to give that error *until*
            // overload resolution has chosen the reference equality operator.
            private static readonly BinaryOperatorKind[,] s_equality =
            {
                //          obj  str  bool chr  i08  i16  i32  i64  u08  u16  u32  u64 nint nuint r32  r64  dec bool? chr? i08? i16? i32? i64? u08? u16? u32? u64?nint?nuint?r32? r64? dec? 
                /*  obj */{ OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ },
                /*  str */{ OBJ, STR, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ },
                /* bool */{ OBJ, OBJ, BOL, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, LBL, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ },
                /*  chr */{ OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, NIN, NUI, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /*  i08 */{ OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, NIN, ERR, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /*  i16 */{ OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, NIN, ERR, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /*  i32 */{ OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, NIN, ERR, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /*  i64 */{ OBJ, OBJ, OBJ, LNG, LNG, LNG, LNG, LNG, LNG, LNG, LNG, ERR, LNG, ERR, FLT, DBL, DEC, OBJ, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LLG, ERR, LFL, LDB, LDC },
                /*  u08 */{ OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, NIN, NUI, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /*  u16 */{ OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, NIN, NUI, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /*  u32 */{ OBJ, OBJ, OBJ, UIN, LNG, LNG, LNG, LNG, UIN, UIN, UIN, ULG, LNG, NUI, FLT, DBL, DEC, OBJ, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LLG, LNU, LFL, LDB, LDC },
                /*  u64 */{ OBJ, OBJ, OBJ, ULG, ERR, ERR, ERR, ERR, ULG, ULG, ULG, ULG, ERR, ULG, FLT, DBL, DEC, OBJ, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LFL, LDB, LDC },
                /* nint */{ OBJ, OBJ, OBJ, NIN, NIN, NIN, NIN, LNG, NIN, NIN, LNG, ERR, NIN, ERR, FLT, DBL, DEC, OBJ, LNI, LNI, LNI, LNI, LLG, LNI, LNI, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /*nuint */{ OBJ, OBJ, OBJ, NUI, ERR, ERR, ERR, ERR, NUI, NUI, NUI, ULG, ERR, NUI, FLT, DBL, DEC, OBJ, LNU, ERR, ERR, ERR, ERR, LNU, LNU, LNU, LUL, ERR, LNU, LFL, LDB, LDC },
                /*  r32 */{ OBJ, OBJ, OBJ, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, DBL, OBJ, OBJ, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, OBJ },
                /*  r64 */{ OBJ, OBJ, OBJ, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, OBJ, OBJ, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, OBJ },
                /*  dec */{ OBJ, OBJ, OBJ, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, OBJ, OBJ, DEC, OBJ, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, OBJ, OBJ, LDC },
                /*bool? */{ OBJ, OBJ, LBL, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, LBL, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ },
                /* chr? */{ OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /* i08? */{ OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /* i16? */{ OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /* i32? */{ OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /* i64? */{ OBJ, OBJ, OBJ, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LLG, ERR, LFL, LDB, LDC, OBJ, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LLG, ERR, LFL, LDB, LDC },
                /* u08? */{ OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /* u16? */{ OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, LFL, LDB, LDC },
                /* u32? */{ OBJ, OBJ, OBJ, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LLG, LNU, LFL, LDB, LDC, OBJ, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LLG, LNU, LFL, LDB, LDC },
                /* u64? */{ OBJ, OBJ, OBJ, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LFL, LDB, LDC, OBJ, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LFL, LDB, LDC },
                /*nint? */{ OBJ, OBJ, OBJ, LNI, LNI, LNI, LNI, LLG, LNI, LNI, LLG, ERR, LNI, ERR, LFL, LDB, LDC, OBJ, LNI, LNI, LNI, LNI, LLG, LNI, LNI, LLG, ERR, LNI, ERR, LFL, LDB, LDC },
                /*nuint?*/{ OBJ, OBJ, OBJ, LNU, ERR, ERR, ERR, ERR, LNU, LNU, LNU, LUL, ERR, LNU, LFL, LDB, LDC, OBJ, LNU, ERR, ERR, ERR, ERR, LNU, LNU, LNU, LUL, ERR, LNU, LFL, LDB, LDC },
                /* r32? */{ OBJ, OBJ, OBJ, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, OBJ, OBJ, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, OBJ },
                /* r64? */{ OBJ, OBJ, OBJ, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, OBJ, OBJ, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, OBJ },
                /* dec? */{ OBJ, OBJ, OBJ, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, OBJ, OBJ, LDC, OBJ, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, OBJ, OBJ, LDC },
            };

            // Overload resolution for Y | & ^ || && X
            private static readonly BinaryOperatorKind[,] s_logical =
            {
                //          obj  str  bool chr  i08  i16  i32  i64  u08  u16  u32  u64 nint nuint r32  r64  dec bool? chr? i08? i16? i32? i64? u08? u16? u32? u64?nint?nuint?r32? r64? dec? 
                /*  obj */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  str */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* bool */{ ERR, ERR, BOL, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LBL, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  chr */{ ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, NIN, NUI, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, ERR, ERR, ERR },
                /*  i08 */{ ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, NIN, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, ERR, ERR, ERR },
                /*  i16 */{ ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, NIN, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, ERR, ERR, ERR },
                /*  i32 */{ ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, NIN, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, ERR, ERR, ERR },
                /*  i64 */{ ERR, ERR, ERR, LNG, LNG, LNG, LNG, LNG, LNG, LNG, LNG, ERR, LNG, ERR, ERR, ERR, ERR, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LLG, ERR, ERR, ERR, ERR },
                /*  u08 */{ ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, NIN, NUI, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, ERR, ERR, ERR },
                /*  u16 */{ ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, NIN, NUI, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, ERR, ERR, ERR },
                /*  u32 */{ ERR, ERR, ERR, UIN, LNG, LNG, LNG, LNG, UIN, UIN, UIN, ULG, LNG, NUI, ERR, ERR, ERR, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LLG, LNU, ERR, ERR, ERR },
                /*  u64 */{ ERR, ERR, ERR, ULG, ERR, ERR, ERR, ERR, ULG, ULG, ULG, ULG, ERR, ULG, ERR, ERR, ERR, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, ERR, ERR, ERR },
                /* nint */{ ERR, ERR, ERR, NIN, NIN, NIN, NIN, LNG, NIN, NIN, LNG, ERR, NIN, ERR, ERR, ERR, ERR, ERR, LNI, LNI, LNI, LNI, LLG, LNI, LNI, LLG, ERR, LNI, ERR, ERR, ERR, ERR },
                /*nuint */{ ERR, ERR, ERR, NUI, ERR, ERR, ERR, ERR, NUI, NUI, NUI, ULG, ERR, NUI, ERR, ERR, ERR, ERR, LNU, ERR, ERR, ERR, ERR, LNU, LNU, LNU, LUL, ERR, LNU, ERR, ERR, ERR },
                /*  r32 */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  r64 */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  dec */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*bool? */{ ERR, ERR, LBL, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LBL, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* chr? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, ERR, ERR, ERR },
                /* i08? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, ERR, ERR, ERR },
                /* i16? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, ERR, ERR, ERR },
                /* i32? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LNI, ERR, ERR, ERR, ERR },
                /* i64? */{ ERR, ERR, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LLG, ERR, ERR, ERR, ERR, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LLG, ERR, ERR, ERR, ERR },
                /* u08? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, ERR, ERR, ERR },
                /* u16? */{ ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LNI, LNU, ERR, ERR, ERR },
                /* u32? */{ ERR, ERR, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LLG, LNU, ERR, ERR, ERR, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LLG, LNU, ERR, ERR, ERR },
                /* u64? */{ ERR, ERR, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, ERR, ERR, ERR, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, ERR, ERR, ERR },
                /*nint? */{ ERR, ERR, ERR, LNI, LNI, LNI, LNI, LLG, LNI, LNI, LLG, ERR, LNI, ERR, ERR, ERR, ERR, ERR, LNI, LNI, LNI, LNI, LLG, LNI, LNI, LLG, ERR, LNI, ERR, ERR, ERR, ERR },
                /*nuint?*/{ ERR, ERR, ERR, LNU, ERR, ERR, ERR, ERR, LNU, LNU, LNU, LUL, ERR, LNU, ERR, ERR, ERR, ERR, LNU, ERR, ERR, ERR, ERR, LNU, LNU, LNU, LUL, ERR, LNU, ERR, ERR, ERR },
                /* r32? */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* r64? */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* dec? */{ ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
            };

            private static readonly BinaryOperatorKind[][,] s_opkind =
            {
                /* *   */ s_arithmetic,
                /* +   */ s_addition,
                /* -   */ s_arithmetic,
                /* /   */ s_arithmetic,
                /* %   */ s_arithmetic,
                /* >>  */ s_shift,
                /* <<  */ s_shift,
                /* ==  */ s_equality,
                /* !=  */ s_equality,
                /* >   */ s_arithmetic,
                /* <   */ s_arithmetic,
                /* >=  */ s_arithmetic,
                /* <=  */ s_arithmetic,
                /* &   */ s_logical,
                /* |   */ s_logical,
                /* ^   */ s_logical,
                /* >>> */ s_shift,
            };

            public static BinaryOperatorKind OpKind(BinaryOperatorKind kind, TypeSymbol left, TypeSymbol right)
            {
                int leftIndex = left.TypeToIndex();
                if (leftIndex < 0)
                {
                    return BinaryOperatorKind.Error;
                }
                int rightIndex = right.TypeToIndex();
                if (rightIndex < 0)
                {
                    return BinaryOperatorKind.Error;
                }

                var result = BinaryOperatorKind.Error;

                // kind.OperatorIndex() collapses '&' and '&&' (and '|' and '||').  To correct
                // this problem, we handle kinds satisfying IsLogical() separately.  Fortunately,
                // such operators only work on boolean types, so there's no need to write out
                // a whole new table.
                //
                // Example: int & int is legal, but int && int is not, so we can't use the same
                // table for both operators.
                if (!kind.IsLogical() || (leftIndex == (int)BinaryOperatorKind.Bool && rightIndex == (int)BinaryOperatorKind.Bool))
                {
                    result = s_opkind[kind.OperatorIndex()][leftIndex, rightIndex];
                }

                return result == BinaryOperatorKind.Error ? result : result | kind;
            }
        }

        private void BinaryOperatorEasyOut(BinaryOperatorKind kind, BoundExpression left, BoundExpression right, BinaryOperatorOverloadResolutionResult result)
        {
            var leftType = left.Type;
            if (leftType is null)
            {
                return;
            }

            var rightType = right.Type;
            if (rightType is null)
            {
                return;
            }

            if (PossiblyUnusualConstantOperation(left, right))
            {
                return;
            }

            var easyOut = BinopEasyOut.OpKind(kind, leftType, rightType);

            if (easyOut == BinaryOperatorKind.Error)
            {
                return;
            }

            BinaryOperatorSignature signature = this.Compilation.builtInOperators.GetSignature(easyOut);

            Conversion leftConversion = Conversions.FastClassifyConversion(leftType, signature.LeftType);
            Conversion rightConversion = Conversions.FastClassifyConversion(rightType, signature.RightType);

            Debug.Assert(leftConversion.Exists && leftConversion.IsImplicit);
            Debug.Assert(rightConversion.Exists && rightConversion.IsImplicit);

            result.Results.Add(BinaryOperatorAnalysisResult.Applicable(signature, leftConversion, rightConversion));
        }

        private static bool PossiblyUnusualConstantOperation(BoundExpression left, BoundExpression right)
        {
            Debug.Assert(left != null);
            Debug.Assert((object?)left.Type != null);
            Debug.Assert(right != null);
            Debug.Assert((object?)right.Type != null);

            // If there are "special" conversions available on either expression
            // then the early out is not accurate. For example, "myuint + myint" 
            // would normally be determined by the easy out as "long + long". But
            // "myuint + 1" does not choose that overload because there is a special
            // conversion from 1 to uint. 

            // If we have one or more constants, then both operands have to be 
            // int, both have to be bool, or both have to be string. Otherwise
            // we skip the easy out and go for the slow path.

            if (left.ConstantValueOpt == null && right.ConstantValueOpt == null)
            {
                // Neither is constant. Go for the easy out.
                return false;
            }

            // One or both operands are constants. See if they are both int, bool or string.

            if (left.Type.SpecialType != right.Type.SpecialType)
            {
                // They are unequal types. Go for the slow path.
                return true;
            }

            if (left.Type.SpecialType == SpecialType.System_Int32 ||
                left.Type.SpecialType == SpecialType.System_Boolean ||
                left.Type.SpecialType == SpecialType.System_String)
            {
                // They are both int, both bool, or both string. Go for the fast path.
                return false;
            }

            // We don't know what's going on. Go for the slow path.
            return true;
        }
    }
}
