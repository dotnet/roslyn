// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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
            private const BinaryOperatorKind FLT = BinaryOperatorKind.Float;
            private const BinaryOperatorKind DBL = BinaryOperatorKind.Double;
            private const BinaryOperatorKind DEC = BinaryOperatorKind.Decimal;
            private const BinaryOperatorKind BOL = BinaryOperatorKind.Bool;
            private const BinaryOperatorKind LIN = BinaryOperatorKind.Lifted | BinaryOperatorKind.Int;
            private const BinaryOperatorKind LUN = BinaryOperatorKind.Lifted | BinaryOperatorKind.UInt;
            private const BinaryOperatorKind LLG = BinaryOperatorKind.Lifted | BinaryOperatorKind.Long;
            private const BinaryOperatorKind LUL = BinaryOperatorKind.Lifted | BinaryOperatorKind.ULong;
            private const BinaryOperatorKind LFL = BinaryOperatorKind.Lifted | BinaryOperatorKind.Float;
            private const BinaryOperatorKind LDB = BinaryOperatorKind.Lifted | BinaryOperatorKind.Double;
            private const BinaryOperatorKind LDC = BinaryOperatorKind.Lifted | BinaryOperatorKind.Decimal;
            private const BinaryOperatorKind LBL = BinaryOperatorKind.Lifted | BinaryOperatorKind.Bool;

            // UNDONE: The lifted bits make these tables very redundant. We could make them smaller (and slower)
            // UNDONE: by having just the unlifted table and doing type manipulation. 

            // UNDONE: We could also make these tables smaller by special-casing string and object.

            // Overload resolution for Y * / - % < > <= >= X
            private static readonly BinaryOperatorKind[,] s_arithmetic =
            {
                //                    ----------------regular-------------------                       ----------------nullable-------------------
                //          obj  str  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  
                /*  obj */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  str */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* bool */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  chr */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /*  i08 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /*  i16 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /*  i32 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /*  i64 */
                      { ERR, ERR, ERR, LNG, LNG, LNG, LNG, LNG, LNG, LNG, LNG, ERR, FLT, DBL, DEC, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC },
                /*  u08 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /*  u16 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /*  u32 */
                      { ERR, ERR, ERR, UIN, LNG, LNG, LNG, LNG, UIN, UIN, UIN, ULG, FLT, DBL, DEC, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC },
                /*  u64 */
                      { ERR, ERR, ERR, ULG, ERR, ERR, ERR, ERR, ULG, ULG, ULG, ULG, FLT, DBL, DEC, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC },
                /*  r32 */
                      { ERR, ERR, ERR, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, DBL, ERR, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR },
                /*  r64 */
                      { ERR, ERR, ERR, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, ERR, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR },
                /*  dec */
                      { ERR, ERR, ERR, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, ERR, ERR, DEC, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC },
                /*nbool */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* nchr */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /* ni08 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /* ni16 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /* ni32 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /* ni64 */
                      { ERR, ERR, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC },
                /* nu08 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /* nu16 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /* nu32 */
                      { ERR, ERR, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC },
                /* nu64 */
                      { ERR, ERR, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC },
                /* nr32 */
                      { ERR, ERR, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR },
                /* nr64 */
                      { ERR, ERR, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR },
                /* ndec */
                      { ERR, ERR, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC },
            };

            // Overload resolution for Y + X
            private static readonly BinaryOperatorKind[,] s_addition =
            {
                //                    ----------------regular-------------------                       ----------------nullable-------------------
                //          obj  str  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  
                /*  obj */
                      { ERR, OSC, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  str */
                      { SOC, STR, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC, SOC },
                /* bool */
                      { ERR, OSC, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  chr */
                      { ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /*  i08 */
                      { ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /*  i16 */
                      { ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /*  i32 */
                      { ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /*  i64 */
                      { ERR, OSC, ERR, LNG, LNG, LNG, LNG, LNG, LNG, LNG, LNG, ERR, FLT, DBL, DEC, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC },
                /*  u08 */
                      { ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /*  u16 */
                      { ERR, OSC, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /*  u32 */
                      { ERR, OSC, ERR, UIN, LNG, LNG, LNG, LNG, UIN, UIN, UIN, ULG, FLT, DBL, DEC, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC },
                /*  u64 */
                      { ERR, OSC, ERR, ULG, ERR, ERR, ERR, ERR, ULG, ULG, ULG, ULG, FLT, DBL, DEC, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC },
                /*  r32 */
                      { ERR, OSC, ERR, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, DBL, ERR, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR },
                /*  r64 */
                      { ERR, OSC, ERR, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, ERR, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR },
                /*  dec */
                      { ERR, OSC, ERR, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, ERR, ERR, DEC, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC },
                /*nbool */
                      { ERR, OSC, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* nchr */
                      { ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /* ni08 */
                      { ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /* ni16 */
                      { ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /* ni32 */
                      { ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /* ni64 */
                      { ERR, OSC, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC },
                /* nu08 */
                      { ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /* nu16 */
                      { ERR, OSC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /* nu32 */
                      { ERR, OSC, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC },
                /* nu64 */
                      { ERR, OSC, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC },
                /* nr32 */
                      { ERR, OSC, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR, ERR, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, ERR },
                /* nr64 */
                      { ERR, OSC, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR, ERR, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, ERR },
                /* ndec */
                      { ERR, OSC, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC, ERR, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, ERR, ERR, LDC },
            };

            // Overload resolution for Y << >> X
            private static readonly BinaryOperatorKind[,] s_shift =
            {
                //                    ----------------regular-------------------                       ----------------nullable-------------------
                //          obj  str  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  
                /*  obj */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  str */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* bool */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  chr */
                      { ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                /*  i08 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                /*  i16 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                /*  i32 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                /*  i64 */
                      { ERR, ERR, ERR, LNG, LNG, LNG, LNG, ERR, LNG, LNG, ERR, ERR, ERR, ERR, ERR, ERR, LLG, LLG, LLG, LLG, ERR, LLG, LLG, ERR, ERR, ERR, ERR, ERR },
                /*  u08 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                /*  u16 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, ERR, INT, INT, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                /*  u32 */
                      { ERR, ERR, ERR, UIN, UIN, UIN, UIN, ERR, UIN, UIN, ERR, ERR, ERR, ERR, ERR, ERR, LUN, LUN, LUN, LUN, ERR, LUN, LUN, ERR, ERR, ERR, ERR, ERR },
                /*  u64 */
                      { ERR, ERR, ERR, ULG, ULG, ULG, ULG, ERR, ULG, ULG, ERR, ERR, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LUL, ERR, ERR, ERR, ERR, ERR },
                /*  r32 */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  r64 */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  dec */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*nbool */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* nchr */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                /* ni08 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                /* ni16 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                /* ni32 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                /* ni64 */
                      { ERR, ERR, ERR, LLG, LLG, LLG, LLG, ERR, LLG, LLG, ERR, ERR, ERR, ERR, ERR, ERR, LLG, LLG, LLG, LLG, ERR, LLG, LLG, ERR, ERR, ERR, ERR, ERR },
                /* nu08 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                /* nu16 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, ERR, LIN, LIN, ERR, ERR, ERR, ERR, ERR },
                /* nu32 */
                      { ERR, ERR, ERR, LUN, LUN, LUN, LUN, ERR, LUN, LUN, ERR, ERR, ERR, ERR, ERR, ERR, LUN, LUN, LUN, LUN, ERR, LUN, LUN, ERR, ERR, ERR, ERR, ERR },
                /* nu64 */
                      { ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LUL, ERR, ERR, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, LUL, LUL, ERR, ERR, ERR, ERR, ERR },
                /* nr32 */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* nr64 */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* ndec */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
            };

            // Overload resolution for Y == != X
            // Note that these are the overload resolution rules; overload resolution might pick an invalid operator.
            // For example, overload resolution on object == decimal chooses the object/object overload, which then
            // is not legal because decimal must be a reference type. But we don't know to give that error *until*
            // overload resolution has chosen the reference equality operator.
            private static readonly BinaryOperatorKind[,] s_equality =
            {
                //                    ----------------regular-------------------                       ----------------nullable-------------------
                //          obj  str  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  
                /*  obj */
                      { OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ },
                /*  str */
                      { OBJ, STR, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ },
                /* bool */
                      { OBJ, OBJ, BOL, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, LBL, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ },
                /*  chr */
                      { OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /*  i08 */
                      { OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /*  i16 */
                      { OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /*  i32 */
                      { OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /*  i64 */
                      { OBJ, OBJ, OBJ, LNG, LNG, LNG, LNG, LNG, LNG, LNG, LNG, ERR, FLT, DBL, DEC, OBJ, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC },
                /*  u08 */
                      { OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /*  u16 */
                      { OBJ, OBJ, OBJ, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, FLT, DBL, DEC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /*  u32 */
                      { OBJ, OBJ, OBJ, UIN, LNG, LNG, LNG, LNG, UIN, UIN, UIN, ULG, FLT, DBL, DEC, OBJ, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC },
                /*  u64 */
                      { OBJ, OBJ, OBJ, ULG, ERR, ERR, ERR, ERR, ULG, ULG, ULG, ULG, FLT, DBL, DEC, OBJ, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC },
                /*  r32 */
                      { OBJ, OBJ, OBJ, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, FLT, DBL, OBJ, OBJ, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, OBJ },
                /*  r64 */
                      { OBJ, OBJ, OBJ, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, DBL, OBJ, OBJ, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, OBJ },
                /*  dec */
                      { OBJ, OBJ, OBJ, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, DEC, OBJ, OBJ, DEC, OBJ, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, OBJ, OBJ, LDC },
                /*nbool */
                      { OBJ, OBJ, LBL, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, LBL, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ, OBJ },
                /* nchr */
                      { OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /* ni08 */
                      { OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /* ni16 */
                      { OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /* ni32 */
                      { OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, LFL, LDB, LDC },
                /* ni64 */
                      { OBJ, OBJ, OBJ, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC, OBJ, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, LFL, LDB, LDC },
                /* nu08 */
                      { OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /* nu16 */
                      { OBJ, OBJ, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC, OBJ, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, LFL, LDB, LDC },
                /* nu32 */
                      { OBJ, OBJ, OBJ, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC, OBJ, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, LFL, LDB, LDC },
                /* nu64 */
                      { OBJ, OBJ, OBJ, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC, OBJ, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, LFL, LDB, LDC },
                /* nr32 */
                      { OBJ, OBJ, OBJ, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, OBJ, OBJ, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LFL, LDB, OBJ },
                /* nr64 */
                      { OBJ, OBJ, OBJ, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, OBJ, OBJ, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, LDB, OBJ },
                /* ndec */
                      { OBJ, OBJ, OBJ, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, OBJ, OBJ, LDC, OBJ, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, LDC, OBJ, OBJ, LDC },
            };

            // Overload resolution for Y | & ^ || && X
            private static readonly BinaryOperatorKind[,] s_logical =
            {
                //                    ----------------regular-------------------                       ----------------nullable-------------------
                //          obj  str  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  bool chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  
                /*  obj */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  str */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* bool */
                      { ERR, ERR, BOL, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LBL, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  chr */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR },
                /*  i08 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR },
                /*  i16 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR },
                /*  i32 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, LNG, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR },
                /*  i64 */
                      { ERR, ERR, ERR, LNG, LNG, LNG, LNG, LNG, LNG, LNG, LNG, ERR, ERR, ERR, ERR, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, ERR, ERR, ERR },
                /*  u08 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR },
                /*  u16 */
                      { ERR, ERR, ERR, INT, INT, INT, INT, LNG, INT, INT, UIN, ULG, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR },
                /*  u32 */
                      { ERR, ERR, ERR, UIN, LNG, LNG, LNG, LNG, UIN, UIN, UIN, ULG, ERR, ERR, ERR, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, ERR, ERR, ERR },
                /*  u64 */
                      { ERR, ERR, ERR, ULG, ERR, ERR, ERR, ERR, ULG, ULG, ULG, ULG, ERR, ERR, ERR, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, ERR, ERR },
                /*  r32 */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  r64 */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*  dec */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /*nbool */
                      { ERR, ERR, LBL, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, LBL, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* nchr */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR },
                /* ni08 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR },
                /* ni16 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR },
                /* ni32 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LLG, ERR, ERR, ERR, ERR },
                /* ni64 */
                      { ERR, ERR, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, ERR, ERR, ERR, ERR, LLG, LLG, LLG, LLG, LLG, LLG, LLG, LLG, ERR, ERR, ERR, ERR },
                /* nu08 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR },
                /* nu16 */
                      { ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR, ERR, LIN, LIN, LIN, LIN, LLG, LIN, LIN, LUN, LUL, ERR, ERR, ERR },
                /* nu32 */
                      { ERR, ERR, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, ERR, ERR, ERR, ERR, LUN, LLG, LLG, LLG, LLG, LUN, LUN, LUN, LUL, ERR, ERR, ERR },
                /* nu64 */
                      { ERR, ERR, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, ERR, ERR, ERR, LUL, ERR, ERR, ERR, ERR, LUL, LUL, LUL, LUL, ERR, ERR, ERR },
                /* nr32 */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* nr64 */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
                /* ndec */
                      { ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR },
            };

            private static readonly BinaryOperatorKind[][,] s_opkind =
            {
                /* *  */ s_arithmetic,
                /* +  */ s_addition,
                /* -  */ s_arithmetic,
                /* /  */ s_arithmetic,
                /* %  */ s_arithmetic,
                /* >> */ s_shift,
                /* << */ s_shift,
                /* == */ s_equality,
                /* != */ s_equality,
                /* >  */ s_arithmetic,
                /* <  */ s_arithmetic,
                /* >= */ s_arithmetic,
                /* <= */ s_arithmetic,
                /* &  */ s_logical,
                /* |  */ s_logical,
                /* ^  */ s_logical,
            };

            private static int? TypeToIndex(TypeSymbol type)
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
                    case SpecialType.System_Single: return 12;
                    case SpecialType.System_Double: return 13;
                    case SpecialType.System_Decimal: return 14;

                    case SpecialType.None:
                        if ((object)type != null && type.IsNullableType())
                        {
                            TypeSymbol underlyingType = type.GetNullableUnderlyingType();

                            switch (underlyingType.GetSpecialTypeSafe())
                            {
                                case SpecialType.System_Boolean: return 15;
                                case SpecialType.System_Char: return 16;
                                case SpecialType.System_SByte: return 17;
                                case SpecialType.System_Int16: return 18;
                                case SpecialType.System_Int32: return 19;
                                case SpecialType.System_Int64: return 20;
                                case SpecialType.System_Byte: return 21;
                                case SpecialType.System_UInt16: return 22;
                                case SpecialType.System_UInt32: return 23;
                                case SpecialType.System_UInt64: return 24;
                                case SpecialType.System_Single: return 25;
                                case SpecialType.System_Double: return 26;
                                case SpecialType.System_Decimal: return 27;
                            }
                        }

                        // fall through
                        goto default;

                    default: return null;
                }
            }

            public static BinaryOperatorKind OpKind(BinaryOperatorKind kind, TypeSymbol left, TypeSymbol right)
            {
                int? leftIndex = TypeToIndex(left);
                if (leftIndex == null)
                {
                    return BinaryOperatorKind.Error;
                }
                int? rightIndex = TypeToIndex(right);
                if (rightIndex == null)
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
                    result = s_opkind[kind.OperatorIndex()][leftIndex.Value, rightIndex.Value];
                }

                return result == BinaryOperatorKind.Error ? result : result | kind;
            }
        }

        private void BinaryOperatorEasyOut(BinaryOperatorKind kind, BoundExpression left, BoundExpression right, BinaryOperatorOverloadResolutionResult result)
        {
            var leftType = left.Type;
            if ((object)leftType == null)
            {
                return;
            }

            var rightType = right.Type;
            if ((object)rightType == null)
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

            Conversion leftConversion = Conversions.FastClassifyConversion(left.Type, signature.LeftType);
            Conversion rightConversion = Conversions.FastClassifyConversion(right.Type, signature.RightType);

            Debug.Assert(leftConversion is { Exists: true, IsImplicit: true });
            Debug.Assert(rightConversion.Exists && rightConversion.IsImplicit);

            result.Results.Add(BinaryOperatorAnalysisResult.Applicable(signature, leftConversion, rightConversion));
        }

        private static bool PossiblyUnusualConstantOperation(BoundExpression left, BoundExpression right)
        {
            Debug.Assert(left != null);
            Debug.Assert((object)left.Type != null);
            Debug.Assert(right != null);
            Debug.Assert((object)right.Type != null);

            // If there are "special" conversions available on either expression
            // then the early out is not accurate. For example, "myuint + myint" 
            // would normally be determined by the easy out as "long + long". But
            // "myuint + 1" does not choose that overload because there is a special
            // conversion from 1 to uint. 

            // If we have one or more constants, then both operands have to be 
            // int, both have to be bool, or both have to be string. Otherwise
            // we skip the easy out and go for the slow path.

            if (left.ConstantValue == null && right.ConstantValue == null)
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
