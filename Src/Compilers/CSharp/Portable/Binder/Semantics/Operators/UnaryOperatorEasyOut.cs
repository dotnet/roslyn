// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class OverloadResolution
    {
        internal static class UnopEasyOut
        {
            const UnaryOperatorKind ERR = UnaryOperatorKind.Error;

            const UnaryOperatorKind BOL = UnaryOperatorKind.Bool;
            const UnaryOperatorKind CHR = UnaryOperatorKind.Char;
            const UnaryOperatorKind I08 = UnaryOperatorKind.SByte;
            const UnaryOperatorKind U08 = UnaryOperatorKind.Byte;
            const UnaryOperatorKind I16 = UnaryOperatorKind.Short;
            const UnaryOperatorKind U16 = UnaryOperatorKind.UShort;
            const UnaryOperatorKind I32 = UnaryOperatorKind.Int;
            const UnaryOperatorKind U32 = UnaryOperatorKind.UInt;
            const UnaryOperatorKind I64 = UnaryOperatorKind.Long;
            const UnaryOperatorKind U64 = UnaryOperatorKind.ULong;
            const UnaryOperatorKind R32 = UnaryOperatorKind.Float;
            const UnaryOperatorKind R64 = UnaryOperatorKind.Double;
            const UnaryOperatorKind DEC = UnaryOperatorKind.Decimal;
            const UnaryOperatorKind LBOL = UnaryOperatorKind.Lifted | UnaryOperatorKind.Bool;
            const UnaryOperatorKind LCHR = UnaryOperatorKind.Lifted | UnaryOperatorKind.Char;
            const UnaryOperatorKind LI08 = UnaryOperatorKind.Lifted | UnaryOperatorKind.SByte;
            const UnaryOperatorKind LU08 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Byte;
            const UnaryOperatorKind LI16 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Short;
            const UnaryOperatorKind LU16 = UnaryOperatorKind.Lifted | UnaryOperatorKind.UShort;
            const UnaryOperatorKind LI32 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Int;
            const UnaryOperatorKind LU32 = UnaryOperatorKind.Lifted | UnaryOperatorKind.UInt;
            const UnaryOperatorKind LI64 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Long;
            const UnaryOperatorKind LU64 = UnaryOperatorKind.Lifted | UnaryOperatorKind.ULong;
            const UnaryOperatorKind LR32 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Float;
            const UnaryOperatorKind LR64 = UnaryOperatorKind.Lifted | UnaryOperatorKind.Double;
            const UnaryOperatorKind LDEC = UnaryOperatorKind.Lifted | UnaryOperatorKind.Decimal;


            private static readonly UnaryOperatorKind[] increment =
            //obj  str  bool chr   i08   i16   i32   i64   u08   u16   u32   u64   r32   r64   dec  
                { ERR, ERR, ERR, CHR,  I08,  I16,  I32,  I64,  U08,  U16,  U32,  U64,  R32,  R64,  DEC,
                            /* lifted */
                            ERR, LCHR, LI08, LI16, LI32, LI64, LU08, LU16, LU32, LU64, LR32, LR64, LDEC };

            private static readonly UnaryOperatorKind[] plus =
            //obj  str  bool chr   i08   i16   i32   i64   u08   u16   u32   u64   r32   r64   dec  
                { ERR, ERR, ERR, I32,  I32,  I32,  I32,  I64,  I32,  I32,  U32,  U64,  R32,  R64,  DEC,
                            /* lifted */
                            ERR, LI32, LI32, LI32, LI32, LI64, LI32, LI32, LU32, LU64, LR32, LR64, LDEC };

            private static readonly UnaryOperatorKind[] minus =
            //obj  str  bool chr   i08   i16   i32   i64   u08   u16   u32   u64   r32   r64   dec  
                { ERR, ERR, ERR, I32,  I32,  I32,  I32,  I64,  I32,  I32,  I64,  ERR,  R32,  R64,  DEC,
                            /* lifted */
                            ERR, LI32, LI32, LI32, LI32, LI64, LI32, LI32, LI64, ERR,  LR32, LR64, LDEC };

            private static readonly UnaryOperatorKind[] logicalNegation =
            //obj  str  bool  chr  i08  i16  i32  i64  u08  u16  u32  u64  r32  r64  dec  
                { ERR, ERR, BOL,  ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR,
                            /* lifted */
                            LBOL, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR, ERR };

            private static readonly UnaryOperatorKind[] bitwiseComplement =
            //obj  str  bool chr   i08   i16   i32   i64   u08   u16   u32   u64   r32  r64  dec  
                { ERR, ERR, ERR, I32,  I32,  I32,  I32,  I64,  I32,  I32,  U32,  U64,  ERR, ERR, ERR,
                            /* lifted */
                            ERR, LI32, LI32, LI32, LI32, LI64, LI32, LI32, LU32, LU64, ERR, ERR, ERR  };

            private static readonly UnaryOperatorKind[][] opkind =
            {
                /* ++ */  increment,
                /* -- */  increment,
                /* ++ */  increment,
                /* -- */  increment,
                /* +  */  plus,
                /* -  */  minus,
                /* !  */  logicalNegation,
                /* ~  */  bitwiseComplement
            };

            // UNDONE: This code is repeated in a bunch of places.
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

            public static UnaryOperatorKind OpKind(UnaryOperatorKind kind, TypeSymbol operand)
            {
                int? index = TypeToIndex(operand);
                if (index == null)
                {
                    return UnaryOperatorKind.Error;
                }
                int kindIndex = kind.OperatorIndex();
                var result = (kindIndex >= opkind.Length) ? UnaryOperatorKind.Error : opkind[kindIndex][index.Value];
                return result == UnaryOperatorKind.Error ? result : result | kind;
            }
        }

        private void UnaryOperatorEasyOut(UnaryOperatorKind kind, BoundExpression operand, UnaryOperatorOverloadResolutionResult result)
        {
            var operandType = operand.Type;
            if ((object)operandType == null)
            {
                return;
            }

            var easyOut = UnopEasyOut.OpKind(kind, operandType);

            if (easyOut == UnaryOperatorKind.Error)
            {
                return;
            }

            UnaryOperatorSignature signature = this.Compilation.builtInOperators.GetSignature(easyOut);

            Conversion? conversion = Conversions.FastClassifyConversion(operandType, signature.OperandType);

            Debug.Assert(conversion.HasValue && conversion.Value.IsImplicit);

            result.Results.Add(UnaryOperatorAnalysisResult.Applicable(signature, conversion.Value));
        }
    }
}