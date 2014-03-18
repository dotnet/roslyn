// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Internal cahe of built-in operators.
    /// Cache is compilation-specific becuse it uses compilation-specific SpecialTypes.
    /// </summary>
    internal class BuiltInOperators
    {
        private readonly CSharpCompilation Compilation;

        //actual lazily-constructed caches of built-in operators.
        private ImmutableArray<UnaryOperatorSignature>[] builtInUnaryOperators;
        private ImmutableArray<BinaryOperatorSignature>[][] builtInOperators;

        internal BuiltInOperators(CSharpCompilation compilation)
        {
            this.Compilation = compilation;
        }

        internal void GetSimpleBuiltInOperators(UnaryOperatorKind kind, ArrayBuilder<UnaryOperatorSignature> operators)
        {
            if (builtInUnaryOperators == null)
            {
                var allOperators = new ImmutableArray<UnaryOperatorSignature>[]
                {
                    (new []
                    {
                        GetSignature(UnaryOperatorKind.SBytePostfixIncrement),
                        GetSignature(UnaryOperatorKind.BytePostfixIncrement),
                        GetSignature(UnaryOperatorKind.ShortPostfixIncrement),
                        GetSignature(UnaryOperatorKind.UShortPostfixIncrement),
                        GetSignature(UnaryOperatorKind.IntPostfixIncrement),
                        GetSignature(UnaryOperatorKind.UIntPostfixIncrement),
                        GetSignature(UnaryOperatorKind.LongPostfixIncrement),
                        GetSignature(UnaryOperatorKind.ULongPostfixIncrement),
                        GetSignature(UnaryOperatorKind.CharPostfixIncrement),
                        GetSignature(UnaryOperatorKind.FloatPostfixIncrement),
                        GetSignature(UnaryOperatorKind.DoublePostfixIncrement),
                        GetSignature(UnaryOperatorKind.DecimalPostfixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedSBytePostfixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedBytePostfixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedShortPostfixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedUShortPostfixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedIntPostfixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedUIntPostfixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedLongPostfixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedULongPostfixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedCharPostfixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedFloatPostfixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedDoublePostfixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedDecimalPostfixIncrement),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(UnaryOperatorKind.SBytePostfixDecrement),
                        GetSignature(UnaryOperatorKind.BytePostfixDecrement),
                        GetSignature(UnaryOperatorKind.ShortPostfixDecrement),
                        GetSignature(UnaryOperatorKind.UShortPostfixDecrement),
                        GetSignature(UnaryOperatorKind.IntPostfixDecrement),
                        GetSignature(UnaryOperatorKind.UIntPostfixDecrement),
                        GetSignature(UnaryOperatorKind.LongPostfixDecrement),
                        GetSignature(UnaryOperatorKind.ULongPostfixDecrement),
                        GetSignature(UnaryOperatorKind.CharPostfixDecrement),
                        GetSignature(UnaryOperatorKind.FloatPostfixDecrement),
                        GetSignature(UnaryOperatorKind.DoublePostfixDecrement),
                        GetSignature(UnaryOperatorKind.DecimalPostfixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedSBytePostfixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedBytePostfixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedShortPostfixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedUShortPostfixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedIntPostfixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedUIntPostfixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedLongPostfixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedULongPostfixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedCharPostfixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedFloatPostfixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedDoublePostfixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedDecimalPostfixDecrement),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(UnaryOperatorKind.SBytePrefixIncrement),
                        GetSignature(UnaryOperatorKind.BytePrefixIncrement),
                        GetSignature(UnaryOperatorKind.ShortPrefixIncrement),
                        GetSignature(UnaryOperatorKind.UShortPrefixIncrement),
                        GetSignature(UnaryOperatorKind.IntPrefixIncrement),
                        GetSignature(UnaryOperatorKind.UIntPrefixIncrement),
                        GetSignature(UnaryOperatorKind.LongPrefixIncrement),
                        GetSignature(UnaryOperatorKind.ULongPrefixIncrement),
                        GetSignature(UnaryOperatorKind.CharPrefixIncrement),
                        GetSignature(UnaryOperatorKind.FloatPrefixIncrement),
                        GetSignature(UnaryOperatorKind.DoublePrefixIncrement),
                        GetSignature(UnaryOperatorKind.DecimalPrefixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedSBytePrefixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedBytePrefixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedShortPrefixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedUShortPrefixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedIntPrefixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedUIntPrefixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedLongPrefixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedULongPrefixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedCharPrefixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedFloatPrefixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedDoublePrefixIncrement),
                        GetSignature(UnaryOperatorKind.LiftedDecimalPrefixIncrement),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(UnaryOperatorKind.SBytePrefixDecrement),
                        GetSignature(UnaryOperatorKind.BytePrefixDecrement),
                        GetSignature(UnaryOperatorKind.ShortPrefixDecrement),
                        GetSignature(UnaryOperatorKind.UShortPrefixDecrement),
                        GetSignature(UnaryOperatorKind.IntPrefixDecrement),
                        GetSignature(UnaryOperatorKind.UIntPrefixDecrement),
                        GetSignature(UnaryOperatorKind.LongPrefixDecrement),
                        GetSignature(UnaryOperatorKind.ULongPrefixDecrement),
                        GetSignature(UnaryOperatorKind.CharPrefixDecrement),
                        GetSignature(UnaryOperatorKind.FloatPrefixDecrement),
                        GetSignature(UnaryOperatorKind.DoublePrefixDecrement),
                        GetSignature(UnaryOperatorKind.DecimalPrefixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedSBytePrefixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedBytePrefixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedShortPrefixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedUShortPrefixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedIntPrefixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedUIntPrefixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedLongPrefixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedULongPrefixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedCharPrefixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedFloatPrefixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedDoublePrefixDecrement),
                        GetSignature(UnaryOperatorKind.LiftedDecimalPrefixDecrement),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(UnaryOperatorKind.IntUnaryPlus),
                        GetSignature(UnaryOperatorKind.UIntUnaryPlus),
                        GetSignature(UnaryOperatorKind.LongUnaryPlus),
                        GetSignature(UnaryOperatorKind.ULongUnaryPlus),
                        GetSignature(UnaryOperatorKind.FloatUnaryPlus),
                        GetSignature(UnaryOperatorKind.DoubleUnaryPlus),
                        GetSignature(UnaryOperatorKind.DecimalUnaryPlus),
                        GetSignature(UnaryOperatorKind.LiftedIntUnaryPlus),
                        GetSignature(UnaryOperatorKind.LiftedUIntUnaryPlus),
                        GetSignature(UnaryOperatorKind.LiftedLongUnaryPlus),
                        GetSignature(UnaryOperatorKind.LiftedULongUnaryPlus),
                        GetSignature(UnaryOperatorKind.LiftedFloatUnaryPlus),
                        GetSignature(UnaryOperatorKind.LiftedDoubleUnaryPlus),
                        GetSignature(UnaryOperatorKind.LiftedDecimalUnaryPlus),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(UnaryOperatorKind.IntUnaryMinus),
                        GetSignature(UnaryOperatorKind.LongUnaryMinus),
                        GetSignature(UnaryOperatorKind.FloatUnaryMinus),
                        GetSignature(UnaryOperatorKind.DoubleUnaryMinus),
                        GetSignature(UnaryOperatorKind.DecimalUnaryMinus),
                        GetSignature(UnaryOperatorKind.LiftedIntUnaryMinus),
                        GetSignature(UnaryOperatorKind.LiftedLongUnaryMinus),
                        GetSignature(UnaryOperatorKind.LiftedFloatUnaryMinus),
                        GetSignature(UnaryOperatorKind.LiftedDoubleUnaryMinus),
                        GetSignature(UnaryOperatorKind.LiftedDecimalUnaryMinus),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(UnaryOperatorKind.BoolLogicalNegation),
                        GetSignature(UnaryOperatorKind.LiftedBoolLogicalNegation),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(UnaryOperatorKind.IntBitwiseComplement),
                        GetSignature(UnaryOperatorKind.UIntBitwiseComplement),
                        GetSignature(UnaryOperatorKind.LongBitwiseComplement),
                        GetSignature(UnaryOperatorKind.ULongBitwiseComplement),
                        GetSignature(UnaryOperatorKind.LiftedIntBitwiseComplement),
                        GetSignature(UnaryOperatorKind.LiftedUIntBitwiseComplement),
                        GetSignature(UnaryOperatorKind.LiftedLongBitwiseComplement),
                        GetSignature(UnaryOperatorKind.LiftedULongBitwiseComplement),
                    }).AsImmutableOrNull(),
                    // No built-in operator true or operator false
                    (new UnaryOperatorSignature [0]).AsImmutableOrNull(),
                    (new UnaryOperatorSignature [0]).AsImmutableOrNull(),
                };

                Interlocked.CompareExchange(ref builtInUnaryOperators, allOperators, null);
            }

            operators.AddRange(builtInUnaryOperators[kind.OperatorIndex()]);
        }

        internal UnaryOperatorSignature GetSignature(UnaryOperatorKind kind)
        {
            TypeSymbol opType = null;
            if (kind.IsLifted())
            {
                var nullable = Compilation.GetSpecialType(SpecialType.System_Nullable_T);

                switch (kind.OperandTypes())
                {
                    case UnaryOperatorKind.SByte: opType = nullable.Construct(Compilation.GetSpecialType(SpecialType.System_SByte)); break;
                    case UnaryOperatorKind.Byte: opType = nullable.Construct(Compilation.GetSpecialType(SpecialType.System_Byte)); break;
                    case UnaryOperatorKind.Short: opType = nullable.Construct(Compilation.GetSpecialType(SpecialType.System_Int16)); break;
                    case UnaryOperatorKind.UShort: opType = nullable.Construct(Compilation.GetSpecialType(SpecialType.System_UInt16)); break;
                    case UnaryOperatorKind.Int: opType = nullable.Construct(Compilation.GetSpecialType(SpecialType.System_Int32)); break;
                    case UnaryOperatorKind.UInt: opType = nullable.Construct(Compilation.GetSpecialType(SpecialType.System_UInt32)); break;
                    case UnaryOperatorKind.Long: opType = nullable.Construct(Compilation.GetSpecialType(SpecialType.System_Int64)); break;
                    case UnaryOperatorKind.ULong: opType = nullable.Construct(Compilation.GetSpecialType(SpecialType.System_UInt64)); break;
                    case UnaryOperatorKind.Char: opType = nullable.Construct(Compilation.GetSpecialType(SpecialType.System_Char)); break;
                    case UnaryOperatorKind.Float: opType = nullable.Construct(Compilation.GetSpecialType(SpecialType.System_Single)); break;
                    case UnaryOperatorKind.Double: opType = nullable.Construct(Compilation.GetSpecialType(SpecialType.System_Double)); break;
                    case UnaryOperatorKind.Decimal: opType = nullable.Construct(Compilation.GetSpecialType(SpecialType.System_Decimal)); break;
                    case UnaryOperatorKind.Bool: opType = nullable.Construct(Compilation.GetSpecialType(SpecialType.System_Boolean)); break;
                }
            }
            else
            {
                switch (kind.OperandTypes())
                {
                    case UnaryOperatorKind.SByte: opType = Compilation.GetSpecialType(SpecialType.System_SByte); break;
                    case UnaryOperatorKind.Byte: opType = Compilation.GetSpecialType(SpecialType.System_Byte); break;
                    case UnaryOperatorKind.Short: opType = Compilation.GetSpecialType(SpecialType.System_Int16); break;
                    case UnaryOperatorKind.UShort: opType = Compilation.GetSpecialType(SpecialType.System_UInt16); break;
                    case UnaryOperatorKind.Int: opType = Compilation.GetSpecialType(SpecialType.System_Int32); break;
                    case UnaryOperatorKind.UInt: opType = Compilation.GetSpecialType(SpecialType.System_UInt32); break;
                    case UnaryOperatorKind.Long: opType = Compilation.GetSpecialType(SpecialType.System_Int64); break;
                    case UnaryOperatorKind.ULong: opType = Compilation.GetSpecialType(SpecialType.System_UInt64); break;
                    case UnaryOperatorKind.Char: opType = Compilation.GetSpecialType(SpecialType.System_Char); break;
                    case UnaryOperatorKind.Float: opType = Compilation.GetSpecialType(SpecialType.System_Single); break;
                    case UnaryOperatorKind.Double: opType = Compilation.GetSpecialType(SpecialType.System_Double); break;
                    case UnaryOperatorKind.Decimal: opType = Compilation.GetSpecialType(SpecialType.System_Decimal); break;
                    case UnaryOperatorKind.Bool: opType = Compilation.GetSpecialType(SpecialType.System_Boolean); break;
                }
            }
            Debug.Assert((object)opType != null);
            return new UnaryOperatorSignature(kind, opType, opType);
        }

        internal void GetSimpleBuiltInOperators(BinaryOperatorKind kind, ArrayBuilder<BinaryOperatorSignature> operators)
        {
            if (builtInOperators == null)
            {
                var logicalOperators = new ImmutableArray<BinaryOperatorSignature>[]
                {
                    ImmutableArray<BinaryOperatorSignature>.Empty, //multiplication
                    ImmutableArray<BinaryOperatorSignature>.Empty, //addition
                    ImmutableArray<BinaryOperatorSignature>.Empty, //subtraction
                    ImmutableArray<BinaryOperatorSignature>.Empty, //division
                    ImmutableArray<BinaryOperatorSignature>.Empty, //remainder
                    ImmutableArray<BinaryOperatorSignature>.Empty, //left shift
                    ImmutableArray<BinaryOperatorSignature>.Empty, //right shift
                    ImmutableArray<BinaryOperatorSignature>.Empty, //equal
                    ImmutableArray<BinaryOperatorSignature>.Empty, //not equal
                    ImmutableArray<BinaryOperatorSignature>.Empty, //greater than
                    ImmutableArray<BinaryOperatorSignature>.Empty, //less than
                    ImmutableArray<BinaryOperatorSignature>.Empty, //greater than or equal
                    ImmutableArray<BinaryOperatorSignature>.Empty, //less than or equal
                    ImmutableArray.Create<BinaryOperatorSignature>(GetSignature(BinaryOperatorKind.LogicalBoolAnd)), //and
                    ImmutableArray<BinaryOperatorSignature>.Empty, //xor
                    ImmutableArray.Create<BinaryOperatorSignature>(GetSignature(BinaryOperatorKind.LogicalBoolOr)), //or
                };

                var nonLogicalOperators = new ImmutableArray<BinaryOperatorSignature>[]
                {
                    (new []
                    {
                        GetSignature(BinaryOperatorKind.IntMultiplication),
                        GetSignature(BinaryOperatorKind.UIntMultiplication),
                        GetSignature(BinaryOperatorKind.LongMultiplication),
                        GetSignature(BinaryOperatorKind.ULongMultiplication),
                        GetSignature(BinaryOperatorKind.FloatMultiplication),
                        GetSignature(BinaryOperatorKind.DoubleMultiplication),
                        GetSignature(BinaryOperatorKind.DecimalMultiplication),
                        GetSignature(BinaryOperatorKind.LiftedIntMultiplication),
                        GetSignature(BinaryOperatorKind.LiftedUIntMultiplication),
                        GetSignature(BinaryOperatorKind.LiftedLongMultiplication),
                        GetSignature(BinaryOperatorKind.LiftedULongMultiplication),
                        GetSignature(BinaryOperatorKind.LiftedFloatMultiplication),
                        GetSignature(BinaryOperatorKind.LiftedDoubleMultiplication),
                        GetSignature(BinaryOperatorKind.LiftedDecimalMultiplication),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(BinaryOperatorKind.IntAddition),
                        GetSignature(BinaryOperatorKind.UIntAddition),
                        GetSignature(BinaryOperatorKind.LongAddition),
                        GetSignature(BinaryOperatorKind.ULongAddition),
                        GetSignature(BinaryOperatorKind.FloatAddition),
                        GetSignature(BinaryOperatorKind.DoubleAddition),
                        GetSignature(BinaryOperatorKind.DecimalAddition),
                        GetSignature(BinaryOperatorKind.LiftedIntAddition),
                        GetSignature(BinaryOperatorKind.LiftedUIntAddition),
                        GetSignature(BinaryOperatorKind.LiftedLongAddition),
                        GetSignature(BinaryOperatorKind.LiftedULongAddition),
                        GetSignature(BinaryOperatorKind.LiftedFloatAddition),
                        GetSignature(BinaryOperatorKind.LiftedDoubleAddition),
                        GetSignature(BinaryOperatorKind.LiftedDecimalAddition),
                        GetSignature(BinaryOperatorKind.StringConcatenation),
                        GetSignature(BinaryOperatorKind.StringAndObjectConcatenation),
                        GetSignature(BinaryOperatorKind.ObjectAndStringConcatenation),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(BinaryOperatorKind.IntSubtraction),
                        GetSignature(BinaryOperatorKind.UIntSubtraction),
                        GetSignature(BinaryOperatorKind.LongSubtraction),
                        GetSignature(BinaryOperatorKind.ULongSubtraction),
                        GetSignature(BinaryOperatorKind.FloatSubtraction),
                        GetSignature(BinaryOperatorKind.DoubleSubtraction),
                        GetSignature(BinaryOperatorKind.DecimalSubtraction),
                        GetSignature(BinaryOperatorKind.LiftedIntSubtraction),
                        GetSignature(BinaryOperatorKind.LiftedUIntSubtraction),
                        GetSignature(BinaryOperatorKind.LiftedLongSubtraction),
                        GetSignature(BinaryOperatorKind.LiftedULongSubtraction),
                        GetSignature(BinaryOperatorKind.LiftedFloatSubtraction),
                        GetSignature(BinaryOperatorKind.LiftedDoubleSubtraction),
                        GetSignature(BinaryOperatorKind.LiftedDecimalSubtraction),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(BinaryOperatorKind.IntDivision),
                        GetSignature(BinaryOperatorKind.UIntDivision),
                        GetSignature(BinaryOperatorKind.LongDivision),
                        GetSignature(BinaryOperatorKind.ULongDivision),
                        GetSignature(BinaryOperatorKind.FloatDivision),
                        GetSignature(BinaryOperatorKind.DoubleDivision),
                        GetSignature(BinaryOperatorKind.DecimalDivision),
                        GetSignature(BinaryOperatorKind.LiftedIntDivision),
                        GetSignature(BinaryOperatorKind.LiftedUIntDivision),
                        GetSignature(BinaryOperatorKind.LiftedLongDivision),
                        GetSignature(BinaryOperatorKind.LiftedULongDivision),
                        GetSignature(BinaryOperatorKind.LiftedFloatDivision),
                        GetSignature(BinaryOperatorKind.LiftedDoubleDivision),
                        GetSignature(BinaryOperatorKind.LiftedDecimalDivision),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(BinaryOperatorKind.IntRemainder),
                        GetSignature(BinaryOperatorKind.UIntRemainder),
                        GetSignature(BinaryOperatorKind.LongRemainder),
                        GetSignature(BinaryOperatorKind.ULongRemainder),
                        GetSignature(BinaryOperatorKind.FloatRemainder),
                        GetSignature(BinaryOperatorKind.DoubleRemainder),
                        GetSignature(BinaryOperatorKind.DecimalRemainder),
                        GetSignature(BinaryOperatorKind.LiftedIntRemainder),
                        GetSignature(BinaryOperatorKind.LiftedUIntRemainder),
                        GetSignature(BinaryOperatorKind.LiftedLongRemainder),
                        GetSignature(BinaryOperatorKind.LiftedULongRemainder),
                        GetSignature(BinaryOperatorKind.LiftedFloatRemainder),
                        GetSignature(BinaryOperatorKind.LiftedDoubleRemainder),
                        GetSignature(BinaryOperatorKind.LiftedDecimalRemainder),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(BinaryOperatorKind.IntLeftShift),
                        GetSignature(BinaryOperatorKind.UIntLeftShift),
                        GetSignature(BinaryOperatorKind.LongLeftShift),
                        GetSignature(BinaryOperatorKind.ULongLeftShift),
                        GetSignature(BinaryOperatorKind.LiftedIntLeftShift),
                        GetSignature(BinaryOperatorKind.LiftedUIntLeftShift),
                        GetSignature(BinaryOperatorKind.LiftedLongLeftShift),
                        GetSignature(BinaryOperatorKind.LiftedULongLeftShift),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(BinaryOperatorKind.IntRightShift),
                        GetSignature(BinaryOperatorKind.UIntRightShift),
                        GetSignature(BinaryOperatorKind.LongRightShift),
                        GetSignature(BinaryOperatorKind.ULongRightShift),
                        GetSignature(BinaryOperatorKind.LiftedIntRightShift),
                        GetSignature(BinaryOperatorKind.LiftedUIntRightShift),
                        GetSignature(BinaryOperatorKind.LiftedLongRightShift),
                        GetSignature(BinaryOperatorKind.LiftedULongRightShift),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(BinaryOperatorKind.IntEqual),
                        GetSignature(BinaryOperatorKind.UIntEqual),
                        GetSignature(BinaryOperatorKind.LongEqual),
                        GetSignature(BinaryOperatorKind.ULongEqual),
                        GetSignature(BinaryOperatorKind.FloatEqual),
                        GetSignature(BinaryOperatorKind.DoubleEqual),
                        GetSignature(BinaryOperatorKind.DecimalEqual),
                        GetSignature(BinaryOperatorKind.BoolEqual),
                        GetSignature(BinaryOperatorKind.LiftedIntEqual),
                        GetSignature(BinaryOperatorKind.LiftedUIntEqual),
                        GetSignature(BinaryOperatorKind.LiftedLongEqual),
                        GetSignature(BinaryOperatorKind.LiftedULongEqual),
                        GetSignature(BinaryOperatorKind.LiftedFloatEqual),
                        GetSignature(BinaryOperatorKind.LiftedDoubleEqual),
                        GetSignature(BinaryOperatorKind.LiftedDecimalEqual),
                        GetSignature(BinaryOperatorKind.LiftedBoolEqual),
                        GetSignature(BinaryOperatorKind.ObjectEqual),
                        GetSignature(BinaryOperatorKind.StringEqual),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(BinaryOperatorKind.IntNotEqual),
                        GetSignature(BinaryOperatorKind.UIntNotEqual),
                        GetSignature(BinaryOperatorKind.LongNotEqual),
                        GetSignature(BinaryOperatorKind.ULongNotEqual),
                        GetSignature(BinaryOperatorKind.FloatNotEqual),
                        GetSignature(BinaryOperatorKind.DoubleNotEqual),
                        GetSignature(BinaryOperatorKind.DecimalNotEqual),
                        GetSignature(BinaryOperatorKind.BoolNotEqual),
                        GetSignature(BinaryOperatorKind.LiftedIntNotEqual),
                        GetSignature(BinaryOperatorKind.LiftedUIntNotEqual),
                        GetSignature(BinaryOperatorKind.LiftedLongNotEqual),
                        GetSignature(BinaryOperatorKind.LiftedULongNotEqual),
                        GetSignature(BinaryOperatorKind.LiftedFloatNotEqual),
                        GetSignature(BinaryOperatorKind.LiftedDoubleNotEqual),
                        GetSignature(BinaryOperatorKind.LiftedDecimalNotEqual),
                        GetSignature(BinaryOperatorKind.LiftedBoolNotEqual),
                        GetSignature(BinaryOperatorKind.ObjectNotEqual),
                        GetSignature(BinaryOperatorKind.StringNotEqual),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(BinaryOperatorKind.IntGreaterThan),
                        GetSignature(BinaryOperatorKind.UIntGreaterThan),
                        GetSignature(BinaryOperatorKind.LongGreaterThan),
                        GetSignature(BinaryOperatorKind.ULongGreaterThan),
                        GetSignature(BinaryOperatorKind.FloatGreaterThan),
                        GetSignature(BinaryOperatorKind.DoubleGreaterThan),
                        GetSignature(BinaryOperatorKind.DecimalGreaterThan),
                        GetSignature(BinaryOperatorKind.LiftedIntGreaterThan),
                        GetSignature(BinaryOperatorKind.LiftedUIntGreaterThan),
                        GetSignature(BinaryOperatorKind.LiftedLongGreaterThan),
                        GetSignature(BinaryOperatorKind.LiftedULongGreaterThan),
                        GetSignature(BinaryOperatorKind.LiftedFloatGreaterThan),
                        GetSignature(BinaryOperatorKind.LiftedDoubleGreaterThan),
                        GetSignature(BinaryOperatorKind.LiftedDecimalGreaterThan),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(BinaryOperatorKind.IntLessThan),
                        GetSignature(BinaryOperatorKind.UIntLessThan),
                        GetSignature(BinaryOperatorKind.LongLessThan),
                        GetSignature(BinaryOperatorKind.ULongLessThan),
                        GetSignature(BinaryOperatorKind.FloatLessThan),
                        GetSignature(BinaryOperatorKind.DoubleLessThan),
                        GetSignature(BinaryOperatorKind.DecimalLessThan),
                        GetSignature(BinaryOperatorKind.LiftedIntLessThan),
                        GetSignature(BinaryOperatorKind.LiftedUIntLessThan),
                        GetSignature(BinaryOperatorKind.LiftedLongLessThan),
                        GetSignature(BinaryOperatorKind.LiftedULongLessThan),
                        GetSignature(BinaryOperatorKind.LiftedFloatLessThan),
                        GetSignature(BinaryOperatorKind.LiftedDoubleLessThan),
                        GetSignature(BinaryOperatorKind.LiftedDecimalLessThan),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(BinaryOperatorKind.IntGreaterThanOrEqual),
                        GetSignature(BinaryOperatorKind.UIntGreaterThanOrEqual),
                        GetSignature(BinaryOperatorKind.LongGreaterThanOrEqual),
                        GetSignature(BinaryOperatorKind.ULongGreaterThanOrEqual),
                        GetSignature(BinaryOperatorKind.FloatGreaterThanOrEqual),
                        GetSignature(BinaryOperatorKind.DoubleGreaterThanOrEqual),
                        GetSignature(BinaryOperatorKind.DecimalGreaterThanOrEqual),
                        GetSignature(BinaryOperatorKind.LiftedIntGreaterThanOrEqual),
                        GetSignature(BinaryOperatorKind.LiftedUIntGreaterThanOrEqual),
                        GetSignature(BinaryOperatorKind.LiftedLongGreaterThanOrEqual),
                        GetSignature(BinaryOperatorKind.LiftedULongGreaterThanOrEqual),
                        GetSignature(BinaryOperatorKind.LiftedFloatGreaterThanOrEqual),
                        GetSignature(BinaryOperatorKind.LiftedDoubleGreaterThanOrEqual),
                        GetSignature(BinaryOperatorKind.LiftedDecimalGreaterThanOrEqual),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(BinaryOperatorKind.IntLessThanOrEqual),
                        GetSignature(BinaryOperatorKind.UIntLessThanOrEqual),
                        GetSignature(BinaryOperatorKind.LongLessThanOrEqual),
                        GetSignature(BinaryOperatorKind.ULongLessThanOrEqual),
                        GetSignature(BinaryOperatorKind.FloatLessThanOrEqual),
                        GetSignature(BinaryOperatorKind.DoubleLessThanOrEqual),
                        GetSignature(BinaryOperatorKind.DecimalLessThanOrEqual),
                        GetSignature(BinaryOperatorKind.LiftedIntLessThanOrEqual),
                        GetSignature(BinaryOperatorKind.LiftedUIntLessThanOrEqual),
                        GetSignature(BinaryOperatorKind.LiftedLongLessThanOrEqual),
                        GetSignature(BinaryOperatorKind.LiftedULongLessThanOrEqual),
                        GetSignature(BinaryOperatorKind.LiftedFloatLessThanOrEqual),
                        GetSignature(BinaryOperatorKind.LiftedDoubleLessThanOrEqual),
                        GetSignature(BinaryOperatorKind.LiftedDecimalLessThanOrEqual),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(BinaryOperatorKind.IntAnd),
                        GetSignature(BinaryOperatorKind.UIntAnd),
                        GetSignature(BinaryOperatorKind.LongAnd),
                        GetSignature(BinaryOperatorKind.ULongAnd),
                        GetSignature(BinaryOperatorKind.BoolAnd),
                        GetSignature(BinaryOperatorKind.LiftedIntAnd),
                        GetSignature(BinaryOperatorKind.LiftedUIntAnd),
                        GetSignature(BinaryOperatorKind.LiftedLongAnd),
                        GetSignature(BinaryOperatorKind.LiftedULongAnd),
                        GetSignature(BinaryOperatorKind.LiftedBoolAnd),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(BinaryOperatorKind.IntXor),
                        GetSignature(BinaryOperatorKind.UIntXor),
                        GetSignature(BinaryOperatorKind.LongXor),
                        GetSignature(BinaryOperatorKind.ULongXor),
                        GetSignature(BinaryOperatorKind.BoolXor),
                        GetSignature(BinaryOperatorKind.LiftedIntXor),
                        GetSignature(BinaryOperatorKind.LiftedUIntXor),
                        GetSignature(BinaryOperatorKind.LiftedLongXor),
                        GetSignature(BinaryOperatorKind.LiftedULongXor),
                        GetSignature(BinaryOperatorKind.LiftedBoolXor),
                    }).AsImmutableOrNull(),
                    (new []
                    {
                        GetSignature(BinaryOperatorKind.IntOr),
                        GetSignature(BinaryOperatorKind.UIntOr),
                        GetSignature(BinaryOperatorKind.LongOr),
                        GetSignature(BinaryOperatorKind.ULongOr),
                        GetSignature(BinaryOperatorKind.BoolOr),
                        GetSignature(BinaryOperatorKind.LiftedIntOr),
                        GetSignature(BinaryOperatorKind.LiftedUIntOr),
                        GetSignature(BinaryOperatorKind.LiftedLongOr),
                        GetSignature(BinaryOperatorKind.LiftedULongOr),
                        GetSignature(BinaryOperatorKind.LiftedBoolOr),
                    }).AsImmutableOrNull(),
                };

                var allOperators = new[] { nonLogicalOperators, logicalOperators };

                Interlocked.CompareExchange(ref builtInOperators, allOperators, null);
            }

            operators.AddRange(builtInOperators[kind.IsLogical() ? 1 : 0][kind.OperatorIndex()]);
        }

        internal BinaryOperatorSignature GetSignature(BinaryOperatorKind kind)
        {
            var left = LeftType(kind);
            switch (kind.Operator())
            {
                case BinaryOperatorKind.Multiplication:
                case BinaryOperatorKind.Division:
                case BinaryOperatorKind.Subtraction:
                case BinaryOperatorKind.Remainder:
                case BinaryOperatorKind.And:
                case BinaryOperatorKind.Or:
                case BinaryOperatorKind.Xor:
                    return new BinaryOperatorSignature(kind, left, left, left);
                case BinaryOperatorKind.Addition:
                    return new BinaryOperatorSignature(kind, LeftType(kind), RightType(kind), ReturnType(kind));
                case BinaryOperatorKind.LeftShift:

                case BinaryOperatorKind.RightShift:
                    TypeSymbol returnType = Compilation.GetSpecialType(SpecialType.System_Int32);

                    if (kind.IsLifted())
                    {
                        returnType = Compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(returnType);
                    }

                    return new BinaryOperatorSignature(kind, left, returnType, left);

                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThanOrEqual:
                    return new BinaryOperatorSignature(kind, left, left, Compilation.GetSpecialType(SpecialType.System_Boolean));
            }
            return new BinaryOperatorSignature(kind, LeftType(kind), RightType(kind), ReturnType(kind));
        }

        private TypeSymbol LeftType(BinaryOperatorKind kind)
        {
            if (kind.IsLifted())
            {
                return LiftedType(kind);
            }
            else
            {
                switch (kind.OperandTypes())
                {
                    case BinaryOperatorKind.Int: return Compilation.GetSpecialType(SpecialType.System_Int32);
                    case BinaryOperatorKind.UInt: return Compilation.GetSpecialType(SpecialType.System_UInt32);
                    case BinaryOperatorKind.Long: return Compilation.GetSpecialType(SpecialType.System_Int64);
                    case BinaryOperatorKind.ULong: return Compilation.GetSpecialType(SpecialType.System_UInt64);
                    case BinaryOperatorKind.Float: return Compilation.GetSpecialType(SpecialType.System_Single);
                    case BinaryOperatorKind.Double: return Compilation.GetSpecialType(SpecialType.System_Double);
                    case BinaryOperatorKind.Decimal: return Compilation.GetSpecialType(SpecialType.System_Decimal);
                    case BinaryOperatorKind.Bool: return Compilation.GetSpecialType(SpecialType.System_Boolean);
                    case BinaryOperatorKind.ObjectAndString:
                    case BinaryOperatorKind.Object:
                        return Compilation.GetSpecialType(SpecialType.System_Object);
                    case BinaryOperatorKind.String:
                    case BinaryOperatorKind.StringAndObject:
                        return Compilation.GetSpecialType(SpecialType.System_String);
                }
            }
            Debug.Assert(false, "Bad operator kind in left type");
            return null;
        }

        private TypeSymbol RightType(BinaryOperatorKind kind)
        {
            if (kind.IsLifted())
            {
                return LiftedType(kind);
            }
            else
            {
                switch (kind.OperandTypes())
                {
                    case BinaryOperatorKind.Int: return Compilation.GetSpecialType(SpecialType.System_Int32);
                    case BinaryOperatorKind.UInt: return Compilation.GetSpecialType(SpecialType.System_UInt32);
                    case BinaryOperatorKind.Long: return Compilation.GetSpecialType(SpecialType.System_Int64);
                    case BinaryOperatorKind.ULong: return Compilation.GetSpecialType(SpecialType.System_UInt64);
                    case BinaryOperatorKind.Float: return Compilation.GetSpecialType(SpecialType.System_Single);
                    case BinaryOperatorKind.Double: return Compilation.GetSpecialType(SpecialType.System_Double);
                    case BinaryOperatorKind.Decimal: return Compilation.GetSpecialType(SpecialType.System_Decimal);
                    case BinaryOperatorKind.Bool: return Compilation.GetSpecialType(SpecialType.System_Boolean);
                    case BinaryOperatorKind.ObjectAndString:
                    case BinaryOperatorKind.String:
                        return Compilation.GetSpecialType(SpecialType.System_String);
                    case BinaryOperatorKind.StringAndObject:
                    case BinaryOperatorKind.Object:
                        return Compilation.GetSpecialType(SpecialType.System_Object);
                }
            }
            Debug.Assert(false, "Bad operator kind in right type");
            return null;
        }

        private TypeSymbol ReturnType(BinaryOperatorKind kind)
        {
            if (kind.IsLifted())
            {
                return LiftedType(kind);
            }
            else
            {
                switch (kind.OperandTypes())
                {
                    case BinaryOperatorKind.Int: return Compilation.GetSpecialType(SpecialType.System_Int32);
                    case BinaryOperatorKind.UInt: return Compilation.GetSpecialType(SpecialType.System_UInt32);
                    case BinaryOperatorKind.Long: return Compilation.GetSpecialType(SpecialType.System_Int64);
                    case BinaryOperatorKind.ULong: return Compilation.GetSpecialType(SpecialType.System_UInt64);
                    case BinaryOperatorKind.Float: return Compilation.GetSpecialType(SpecialType.System_Single);
                    case BinaryOperatorKind.Double: return Compilation.GetSpecialType(SpecialType.System_Double);
                    case BinaryOperatorKind.Decimal: return Compilation.GetSpecialType(SpecialType.System_Decimal);
                    case BinaryOperatorKind.Bool: return Compilation.GetSpecialType(SpecialType.System_Boolean);
                    case BinaryOperatorKind.Object: return Compilation.GetSpecialType(SpecialType.System_Object);
                    case BinaryOperatorKind.ObjectAndString:
                    case BinaryOperatorKind.StringAndObject:
                    case BinaryOperatorKind.String:
                        return Compilation.GetSpecialType(SpecialType.System_String);
                }
            }
            Debug.Assert(false, "Bad operator kind in return type");
            return null;
        }

        private TypeSymbol LiftedType(BinaryOperatorKind kind)
        {
            Debug.Assert(kind.IsLifted());

            var nullable = Compilation.GetSpecialType(SpecialType.System_Nullable_T);

            switch (kind.OperandTypes())
            {
                case BinaryOperatorKind.Int: return nullable.Construct(Compilation.GetSpecialType(SpecialType.System_Int32));
                case BinaryOperatorKind.UInt: return nullable.Construct(Compilation.GetSpecialType(SpecialType.System_UInt32));
                case BinaryOperatorKind.Long: return nullable.Construct(Compilation.GetSpecialType(SpecialType.System_Int64));
                case BinaryOperatorKind.ULong: return nullable.Construct(Compilation.GetSpecialType(SpecialType.System_UInt64));
                case BinaryOperatorKind.Float: return nullable.Construct(Compilation.GetSpecialType(SpecialType.System_Single));
                case BinaryOperatorKind.Double: return nullable.Construct(Compilation.GetSpecialType(SpecialType.System_Double));
                case BinaryOperatorKind.Decimal: return nullable.Construct(Compilation.GetSpecialType(SpecialType.System_Decimal));
                case BinaryOperatorKind.Bool: return nullable.Construct(Compilation.GetSpecialType(SpecialType.System_Boolean));
            }
            Debug.Assert(false, "Bad operator kind in lifted type");
            return null;
        }

        internal static bool IsValidObjectEquality(Conversions Conversions, TypeSymbol leftType, bool leftIsNull, TypeSymbol rightType, bool rightIsNull, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // SPEC: The predefined reference type equality operators require one of the following:

            // SPEC: (1) Both operands are a value of a type known to be a reference-type or the literal null. 
            // SPEC:     Furthermore, an explicit reference conversion exists from the type of either 
            // SPEC:     operand to the type of the other operand. Or:
            // SPEC: (2) One operand is a value of type T where T is a type-parameter and the other operand is 
            // SPEC:     the literal null. Furthermore T does not have the value type constraint.

            // SPEC ERROR: Notice that the spec calls out that an explicit reference conversion must exist;
            // SPEC ERROR: in fact it should say that an explicit reference conversion, implicit reference
            // SPEC ERROR: conversion or identity conversion must exist. The conversion from object to object
            // SPEC ERROR: is not classified as a reference conversion at all; it is an identity conversion.

            // Dev10 does not follow the spec exactly for type parameters. Specifically, in Dev10,
            // if a type parameter argument is known to be a value type, or if a type parameter
            // argument is not known to be either a value type or reference type and the other
            // argument is not null, reference type equality cannot be applied. Otherwise, the
            // effective base class of the type parameter is used to determine the conversion
            // to the other argument type. (See ExpressionBinder::GetRefEqualSigs.)

            if (((object)leftType != null) && leftType.IsTypeParameter())
            {
                if (leftType.IsValueType || (!leftType.IsReferenceType && !rightIsNull))
                {
                    return false;
                }

                leftType = ((TypeParameterSymbol)leftType).EffectiveBaseClass(ref useSiteDiagnostics);
                Debug.Assert((object)leftType != null);
            }

            if (((object)rightType != null) && rightType.IsTypeParameter())
            {
                if (rightType.IsValueType || (!rightType.IsReferenceType && !leftIsNull))
                {
                    return false;
                }

                rightType = ((TypeParameterSymbol)rightType).EffectiveBaseClass(ref useSiteDiagnostics);
                Debug.Assert((object)rightType != null);
            }

            var leftIsReferenceType = ((object)leftType != null) && leftType.IsReferenceType;
            if (!leftIsReferenceType && !leftIsNull)
            {
                return false;
            }

            var rightIsReferenceType = ((object)rightType != null) && rightType.IsReferenceType;
            if (!rightIsReferenceType && !rightIsNull)
            {
                return false;
            }

            // If at least one side is null then clearly a conversion exists.
            if (leftIsNull || rightIsNull)
            {
                return true;
            }

            var leftConversion = Conversions.ClassifyConversion(leftType, rightType, ref useSiteDiagnostics);
            if (leftConversion.IsIdentity || leftConversion.IsReference)
            {
                return true;
            }

            var rightConversion = Conversions.ClassifyConversion(rightType, leftType, ref useSiteDiagnostics);
            if (rightConversion.IsIdentity || rightConversion.IsReference)
            {
                return true;
            }

            return false;
        }
    }
}
