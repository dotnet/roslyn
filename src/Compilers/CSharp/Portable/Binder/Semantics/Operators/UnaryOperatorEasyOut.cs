// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class OverloadResolution
    {
        internal static class UnopEasyOut
        {
            private const UnaryOperatorKind ERR = UnaryOperatorKind.Error;

            private const UnaryOperatorKind BOL = UnaryOperatorKind.Bool;
            private const UnaryOperatorKind CHR = UnaryOperatorKind.Char;
            private const UnaryOperatorKind I08 = UnaryOperatorKind.SByte;
            private const UnaryOperatorKind U08 = UnaryOperatorKind.Byte;
            private const UnaryOperatorKind I16 = UnaryOperatorKind.Short;
            private const UnaryOperatorKind U16 = UnaryOperatorKind.UShort;
            private const UnaryOperatorKind I32 = UnaryOperatorKind.Int;
            private const UnaryOperatorKind U32 = UnaryOperatorKind.UInt;
            private const UnaryOperatorKind I64 = UnaryOperatorKind.Long;
            private const UnaryOperatorKind U64 = UnaryOperatorKind.ULong;
            private const UnaryOperatorKind NIN = UnaryOperatorKind.NInt;
            private const UnaryOperatorKind NUI = UnaryOperatorKind.NUInt;
            private const UnaryOperatorKind R32 = UnaryOperatorKind.Float;
            private const UnaryOperatorKind R64 = UnaryOperatorKind.Double;
            private const UnaryOperatorKind DEC = UnaryOperatorKind.Decimal;
            private const UnaryOperatorKind LBOL = UnaryOperatorKind.Lifted | UnaryOperatorKind.Bool;
            private const UnaryOperatorKind LCHR = UnaryOperatorKind.Lifted | UnaryOperatorKind.Char;
            private const UnaryOperatorKind LI08 = UnaryOperatorKind.Lifted | UnaryOperatorKind.SByte;
            private const UnaryOperatorKind LU08 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Byte;
            private const UnaryOperatorKind LI16 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Short;
            private const UnaryOperatorKind LU16 = UnaryOperatorKind.Lifted | UnaryOperatorKind.UShort;
            private const UnaryOperatorKind LI32 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Int;
            private const UnaryOperatorKind LU32 = UnaryOperatorKind.Lifted | UnaryOperatorKind.UInt;
            private const UnaryOperatorKind LI64 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Long;
            private const UnaryOperatorKind LU64 = UnaryOperatorKind.Lifted | UnaryOperatorKind.ULong;
            private const UnaryOperatorKind LNI = UnaryOperatorKind.Lifted | UnaryOperatorKind.NInt;
            private const UnaryOperatorKind LNU = UnaryOperatorKind.Lifted | UnaryOperatorKind.NUInt;
            private const UnaryOperatorKind LR32 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Float;
            private const UnaryOperatorKind LR64 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Double;
            private const UnaryOperatorKind LDEC = UnaryOperatorKind.Lifted | UnaryOperatorKind.Decimal;

            private static readonly UnaryOperatorKind[] s_increment =
                //obj   str  bool   chr   i08   i16   i32   i64   u08   u16   u32   u64  nint nuint   r32   r64   dec  
                { ERR,  ERR,  ERR,  CHR,  I08,  I16,  I32,  I64,  U08,  U16,  U32,  U64,  NIN,  NUI,  R32,  R64,  DEC,
               /* lifted */   ERR, LCHR, LI08, LI16, LI32, LI64, LU08, LU16, LU32, LU64,  LNI,  LNU, LR32, LR64, LDEC };

            private static readonly UnaryOperatorKind[] s_plus =
                //obj   str  bool   chr   i08   i16   i32   i64   u08   u16   u32   u64  nint nuint   r32   r64   dec  
                { ERR,  ERR,  ERR,  I32,  I32,  I32,  I32,  I64,  I32,  I32,  U32,  U64,  NIN,  NUI,  R32,  R64,  DEC,
               /* lifted */   ERR, LI32, LI32, LI32, LI32, LI64, LI32, LI32, LU32, LU64,  LNI,  LNU, LR32, LR64, LDEC };

            private static readonly UnaryOperatorKind[] s_minus =
                //obj   str  bool   chr   i08   i16   i32   i64   u08   u16   u32   u64  nint nuint   r32   r64   dec  
                { ERR,  ERR,  ERR,  I32,  I32,  I32,  I32,  I64,  I32,  I32,  I64,  ERR,  NIN,  ERR,  R32,  R64,  DEC,
               /* lifted */   ERR, LI32, LI32, LI32, LI32, LI64, LI32, LI32, LI64,  ERR,  LNI,  ERR,  LR32, LR64, LDEC };

            private static readonly UnaryOperatorKind[] s_logicalNegation =
                //obj   str  bool   chr   i08   i16   i32   i64   u08   u16   u32   u64  nint nuint   r32   r64   dec  
                { ERR,  ERR,  BOL,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR,
               /* lifted */  LBOL,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR,  ERR };

            private static readonly UnaryOperatorKind[] s_bitwiseComplement =
                //obj   str  bool   chr   i08   i16   i32   i64   u08   u16   u32   u64  nint nuint   r32   r64   dec  
                { ERR,  ERR,  ERR,  I32,  I32,  I32,  I32,  I64,  I32,  I32,  U32,  U64,  NIN,  NUI,  ERR,  ERR,  ERR,
               /* lifted */   ERR, LI32, LI32, LI32, LI32, LI64, LI32, LI32, LU32, LU64,  LNI,  LNU,  ERR,  ERR,  ERR };

            private static readonly UnaryOperatorKind[][] s_opkind =
            {
                /* ++ */  s_increment,
                /* -- */  s_increment,
                /* ++ */  s_increment,
                /* -- */  s_increment,
                /* +  */  s_plus,
                /* -  */  s_minus,
                /* !  */  s_logicalNegation,
                /* ~  */  s_bitwiseComplement
            };

            public static UnaryOperatorKind OpKind(UnaryOperatorKind kind, TypeSymbol operand)
            {
                int index = operand.TypeToIndex();
                if (index < 0)
                {
                    return UnaryOperatorKind.Error;
                }
                int kindIndex = kind.OperatorIndex();
                var result = (kindIndex >= s_opkind.Length) ? UnaryOperatorKind.Error : s_opkind[kindIndex][index];
                return result == UnaryOperatorKind.Error ? result : result | kind;
            }
        }

        private void UnaryOperatorEasyOut(UnaryOperatorKind kind, BoundExpression operand, UnaryOperatorOverloadResolutionResult result)
        {
            var operandType = operand.Type;
            if (operandType is null)
            {
                return;
            }

            var easyOut = UnopEasyOut.OpKind(kind, operandType);

            if (easyOut == UnaryOperatorKind.Error)
            {
                return;
            }

            UnaryOperatorSignature signature = this.Compilation.BuiltInOperators.GetSignature(easyOut);

            Conversion? conversion = Conversions.FastClassifyConversion(operandType, signature.OperandType);

            Debug.Assert(conversion.HasValue && conversion.Value.IsImplicit);

            result.Results.Add(UnaryOperatorAnalysisResult.Applicable(signature, conversion.Value));
        }
    }
}
