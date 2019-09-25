// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Internal cache of built-in operators.
    /// Cache is compilation-specific because it uses compilation-specific SpecialTypes.
    /// </summary>
    internal class BuiltInOperators
    {
        private readonly CSharpCompilation _compilation;

        //actual lazily-constructed caches of built-in operators.
        private ImmutableArray<UnaryOperatorSignature>[] _builtInUnaryOperators;
        private ImmutableArray<BinaryOperatorSignature>[][] _builtInOperators;

        internal BuiltInOperators(CSharpCompilation compilation)
        {
            _compilation = compilation;
        }

        // PERF: Use int instead of UnaryOperatorKind so the compiler can use array literal initialization.
        //       The most natural type choice, Enum arrays, are not blittable due to a CLR limitation.
        private ImmutableArray<UnaryOperatorSignature> GetSignaturesFromUnaryOperatorKinds(int[] operatorKinds)
        {
            var builder = ArrayBuilder<UnaryOperatorSignature>.GetInstance();
            foreach (var kind in operatorKinds)
            {
                builder.Add(GetSignature((UnaryOperatorKind)kind));
            }

            return builder.ToImmutableAndFree();
        }

        internal void GetSimpleBuiltInOperators(UnaryOperatorKind kind, ArrayBuilder<UnaryOperatorSignature> operators)
        {
            if (_builtInUnaryOperators == null)
            {
                var allOperators = new ImmutableArray<UnaryOperatorSignature>[]
                {
                    GetSignaturesFromUnaryOperatorKinds(new []
                    {
                        (int)UnaryOperatorKind.SBytePostfixIncrement,
                        (int)UnaryOperatorKind.BytePostfixIncrement,
                        (int)UnaryOperatorKind.ShortPostfixIncrement,
                        (int)UnaryOperatorKind.UShortPostfixIncrement,
                        (int)UnaryOperatorKind.IntPostfixIncrement,
                        (int)UnaryOperatorKind.UIntPostfixIncrement,
                        (int)UnaryOperatorKind.LongPostfixIncrement,
                        (int)UnaryOperatorKind.ULongPostfixIncrement,
                        (int)UnaryOperatorKind.CharPostfixIncrement,
                        (int)UnaryOperatorKind.FloatPostfixIncrement,
                        (int)UnaryOperatorKind.DoublePostfixIncrement,
                        (int)UnaryOperatorKind.DecimalPostfixIncrement,
                        (int)UnaryOperatorKind.LiftedSBytePostfixIncrement,
                        (int)UnaryOperatorKind.LiftedBytePostfixIncrement,
                        (int)UnaryOperatorKind.LiftedShortPostfixIncrement,
                        (int)UnaryOperatorKind.LiftedUShortPostfixIncrement,
                        (int)UnaryOperatorKind.LiftedIntPostfixIncrement,
                        (int)UnaryOperatorKind.LiftedUIntPostfixIncrement,
                        (int)UnaryOperatorKind.LiftedLongPostfixIncrement,
                        (int)UnaryOperatorKind.LiftedULongPostfixIncrement,
                        (int)UnaryOperatorKind.LiftedCharPostfixIncrement,
                        (int)UnaryOperatorKind.LiftedFloatPostfixIncrement,
                        (int)UnaryOperatorKind.LiftedDoublePostfixIncrement,
                        (int)UnaryOperatorKind.LiftedDecimalPostfixIncrement,
                    }),
                    GetSignaturesFromUnaryOperatorKinds(new []
                    {
                        (int)UnaryOperatorKind.SBytePostfixDecrement,
                        (int)UnaryOperatorKind.BytePostfixDecrement,
                        (int)UnaryOperatorKind.ShortPostfixDecrement,
                        (int)UnaryOperatorKind.UShortPostfixDecrement,
                        (int)UnaryOperatorKind.IntPostfixDecrement,
                        (int)UnaryOperatorKind.UIntPostfixDecrement,
                        (int)UnaryOperatorKind.LongPostfixDecrement,
                        (int)UnaryOperatorKind.ULongPostfixDecrement,
                        (int)UnaryOperatorKind.CharPostfixDecrement,
                        (int)UnaryOperatorKind.FloatPostfixDecrement,
                        (int)UnaryOperatorKind.DoublePostfixDecrement,
                        (int)UnaryOperatorKind.DecimalPostfixDecrement,
                        (int)UnaryOperatorKind.LiftedSBytePostfixDecrement,
                        (int)UnaryOperatorKind.LiftedBytePostfixDecrement,
                        (int)UnaryOperatorKind.LiftedShortPostfixDecrement,
                        (int)UnaryOperatorKind.LiftedUShortPostfixDecrement,
                        (int)UnaryOperatorKind.LiftedIntPostfixDecrement,
                        (int)UnaryOperatorKind.LiftedUIntPostfixDecrement,
                        (int)UnaryOperatorKind.LiftedLongPostfixDecrement,
                        (int)UnaryOperatorKind.LiftedULongPostfixDecrement,
                        (int)UnaryOperatorKind.LiftedCharPostfixDecrement,
                        (int)UnaryOperatorKind.LiftedFloatPostfixDecrement,
                        (int)UnaryOperatorKind.LiftedDoublePostfixDecrement,
                        (int)UnaryOperatorKind.LiftedDecimalPostfixDecrement,
                    }),
                    GetSignaturesFromUnaryOperatorKinds(new []
                    {
                        (int)UnaryOperatorKind.SBytePrefixIncrement,
                        (int)UnaryOperatorKind.BytePrefixIncrement,
                        (int)UnaryOperatorKind.ShortPrefixIncrement,
                        (int)UnaryOperatorKind.UShortPrefixIncrement,
                        (int)UnaryOperatorKind.IntPrefixIncrement,
                        (int)UnaryOperatorKind.UIntPrefixIncrement,
                        (int)UnaryOperatorKind.LongPrefixIncrement,
                        (int)UnaryOperatorKind.ULongPrefixIncrement,
                        (int)UnaryOperatorKind.CharPrefixIncrement,
                        (int)UnaryOperatorKind.FloatPrefixIncrement,
                        (int)UnaryOperatorKind.DoublePrefixIncrement,
                        (int)UnaryOperatorKind.DecimalPrefixIncrement,
                        (int)UnaryOperatorKind.LiftedSBytePrefixIncrement,
                        (int)UnaryOperatorKind.LiftedBytePrefixIncrement,
                        (int)UnaryOperatorKind.LiftedShortPrefixIncrement,
                        (int)UnaryOperatorKind.LiftedUShortPrefixIncrement,
                        (int)UnaryOperatorKind.LiftedIntPrefixIncrement,
                        (int)UnaryOperatorKind.LiftedUIntPrefixIncrement,
                        (int)UnaryOperatorKind.LiftedLongPrefixIncrement,
                        (int)UnaryOperatorKind.LiftedULongPrefixIncrement,
                        (int)UnaryOperatorKind.LiftedCharPrefixIncrement,
                        (int)UnaryOperatorKind.LiftedFloatPrefixIncrement,
                        (int)UnaryOperatorKind.LiftedDoublePrefixIncrement,
                        (int)UnaryOperatorKind.LiftedDecimalPrefixIncrement,
                    }),
                    GetSignaturesFromUnaryOperatorKinds(new []
                    {
                        (int)UnaryOperatorKind.SBytePrefixDecrement,
                        (int)UnaryOperatorKind.BytePrefixDecrement,
                        (int)UnaryOperatorKind.ShortPrefixDecrement,
                        (int)UnaryOperatorKind.UShortPrefixDecrement,
                        (int)UnaryOperatorKind.IntPrefixDecrement,
                        (int)UnaryOperatorKind.UIntPrefixDecrement,
                        (int)UnaryOperatorKind.LongPrefixDecrement,
                        (int)UnaryOperatorKind.ULongPrefixDecrement,
                        (int)UnaryOperatorKind.CharPrefixDecrement,
                        (int)UnaryOperatorKind.FloatPrefixDecrement,
                        (int)UnaryOperatorKind.DoublePrefixDecrement,
                        (int)UnaryOperatorKind.DecimalPrefixDecrement,
                        (int)UnaryOperatorKind.LiftedSBytePrefixDecrement,
                        (int)UnaryOperatorKind.LiftedBytePrefixDecrement,
                        (int)UnaryOperatorKind.LiftedShortPrefixDecrement,
                        (int)UnaryOperatorKind.LiftedUShortPrefixDecrement,
                        (int)UnaryOperatorKind.LiftedIntPrefixDecrement,
                        (int)UnaryOperatorKind.LiftedUIntPrefixDecrement,
                        (int)UnaryOperatorKind.LiftedLongPrefixDecrement,
                        (int)UnaryOperatorKind.LiftedULongPrefixDecrement,
                        (int)UnaryOperatorKind.LiftedCharPrefixDecrement,
                        (int)UnaryOperatorKind.LiftedFloatPrefixDecrement,
                        (int)UnaryOperatorKind.LiftedDoublePrefixDecrement,
                        (int)UnaryOperatorKind.LiftedDecimalPrefixDecrement,
                    }),
                    GetSignaturesFromUnaryOperatorKinds(new []
                    {
                        (int)UnaryOperatorKind.IntUnaryPlus,
                        (int)UnaryOperatorKind.UIntUnaryPlus,
                        (int)UnaryOperatorKind.LongUnaryPlus,
                        (int)UnaryOperatorKind.ULongUnaryPlus,
                        (int)UnaryOperatorKind.FloatUnaryPlus,
                        (int)UnaryOperatorKind.DoubleUnaryPlus,
                        (int)UnaryOperatorKind.DecimalUnaryPlus,
                        (int)UnaryOperatorKind.LiftedIntUnaryPlus,
                        (int)UnaryOperatorKind.LiftedUIntUnaryPlus,
                        (int)UnaryOperatorKind.LiftedLongUnaryPlus,
                        (int)UnaryOperatorKind.LiftedULongUnaryPlus,
                        (int)UnaryOperatorKind.LiftedFloatUnaryPlus,
                        (int)UnaryOperatorKind.LiftedDoubleUnaryPlus,
                        (int)UnaryOperatorKind.LiftedDecimalUnaryPlus,
                    }),
                    GetSignaturesFromUnaryOperatorKinds(new []
                    {
                        (int)UnaryOperatorKind.IntUnaryMinus,
                        (int)UnaryOperatorKind.LongUnaryMinus,
                        (int)UnaryOperatorKind.FloatUnaryMinus,
                        (int)UnaryOperatorKind.DoubleUnaryMinus,
                        (int)UnaryOperatorKind.DecimalUnaryMinus,
                        (int)UnaryOperatorKind.LiftedIntUnaryMinus,
                        (int)UnaryOperatorKind.LiftedLongUnaryMinus,
                        (int)UnaryOperatorKind.LiftedFloatUnaryMinus,
                        (int)UnaryOperatorKind.LiftedDoubleUnaryMinus,
                        (int)UnaryOperatorKind.LiftedDecimalUnaryMinus,
                    }),
                    GetSignaturesFromUnaryOperatorKinds(new []
                    {
                        (int)UnaryOperatorKind.BoolLogicalNegation,
                        (int)UnaryOperatorKind.LiftedBoolLogicalNegation,
                    }),
                    GetSignaturesFromUnaryOperatorKinds(new []
                    {
                        (int)UnaryOperatorKind.IntBitwiseComplement,
                        (int)UnaryOperatorKind.UIntBitwiseComplement,
                        (int)UnaryOperatorKind.LongBitwiseComplement,
                        (int)UnaryOperatorKind.ULongBitwiseComplement,
                        (int)UnaryOperatorKind.LiftedIntBitwiseComplement,
                        (int)UnaryOperatorKind.LiftedUIntBitwiseComplement,
                        (int)UnaryOperatorKind.LiftedLongBitwiseComplement,
                        (int)UnaryOperatorKind.LiftedULongBitwiseComplement,
                    }),
                    // No built-in operator true or operator false
                    ImmutableArray<UnaryOperatorSignature>.Empty,
                    ImmutableArray<UnaryOperatorSignature>.Empty,
                };

                Interlocked.CompareExchange(ref _builtInUnaryOperators, allOperators, null);
            }

            operators.AddRange(_builtInUnaryOperators[kind.OperatorIndex()]);
        }

        internal UnaryOperatorSignature GetSignature(UnaryOperatorKind kind)
        {
            TypeSymbol opType;
            switch (kind.OperandTypes())
            {
                case UnaryOperatorKind.SByte: opType = _compilation.GetSpecialType(SpecialType.System_SByte); break;
                case UnaryOperatorKind.Byte: opType = _compilation.GetSpecialType(SpecialType.System_Byte); break;
                case UnaryOperatorKind.Short: opType = _compilation.GetSpecialType(SpecialType.System_Int16); break;
                case UnaryOperatorKind.UShort: opType = _compilation.GetSpecialType(SpecialType.System_UInt16); break;
                case UnaryOperatorKind.Int: opType = _compilation.GetSpecialType(SpecialType.System_Int32); break;
                case UnaryOperatorKind.UInt: opType = _compilation.GetSpecialType(SpecialType.System_UInt32); break;
                case UnaryOperatorKind.Long: opType = _compilation.GetSpecialType(SpecialType.System_Int64); break;
                case UnaryOperatorKind.ULong: opType = _compilation.GetSpecialType(SpecialType.System_UInt64); break;
                case UnaryOperatorKind.Char: opType = _compilation.GetSpecialType(SpecialType.System_Char); break;
                case UnaryOperatorKind.Float: opType = _compilation.GetSpecialType(SpecialType.System_Single); break;
                case UnaryOperatorKind.Double: opType = _compilation.GetSpecialType(SpecialType.System_Double); break;
                case UnaryOperatorKind.Decimal: opType = _compilation.GetSpecialType(SpecialType.System_Decimal); break;
                case UnaryOperatorKind.Bool: opType = _compilation.GetSpecialType(SpecialType.System_Boolean); break;
                default: throw ExceptionUtilities.UnexpectedValue(kind.OperandTypes());
            }

            if (kind.IsLifted())
            {
                opType = _compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(opType);
            }

            return new UnaryOperatorSignature(kind, opType, opType);
        }

        // PERF: Use int instead of BinaryOperatorKind so the compiler can use array literal initialization.
        //       The most natural type choice, Enum arrays, are not blittable due to a CLR limitation.
        private ImmutableArray<BinaryOperatorSignature> GetSignaturesFromBinaryOperatorKinds(int[] operatorKinds)
        {
            var builder = ArrayBuilder<BinaryOperatorSignature>.GetInstance();
            foreach (var kind in operatorKinds)
            {
                builder.Add(GetSignature((BinaryOperatorKind)kind));
            }

            return builder.ToImmutableAndFree();
        }

        internal void GetSimpleBuiltInOperators(BinaryOperatorKind kind, ArrayBuilder<BinaryOperatorSignature> operators)
        {
            if (_builtInOperators == null)
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
                    GetSignaturesFromBinaryOperatorKinds(new []
                    {
                        (int)BinaryOperatorKind.IntMultiplication,
                        (int)BinaryOperatorKind.UIntMultiplication,
                        (int)BinaryOperatorKind.LongMultiplication,
                        (int)BinaryOperatorKind.ULongMultiplication,
                        (int)BinaryOperatorKind.FloatMultiplication,
                        (int)BinaryOperatorKind.DoubleMultiplication,
                        (int)BinaryOperatorKind.DecimalMultiplication,
                        (int)BinaryOperatorKind.LiftedIntMultiplication,
                        (int)BinaryOperatorKind.LiftedUIntMultiplication,
                        (int)BinaryOperatorKind.LiftedLongMultiplication,
                        (int)BinaryOperatorKind.LiftedULongMultiplication,
                        (int)BinaryOperatorKind.LiftedFloatMultiplication,
                        (int)BinaryOperatorKind.LiftedDoubleMultiplication,
                        (int)BinaryOperatorKind.LiftedDecimalMultiplication,
                    }),
                    GetSignaturesFromBinaryOperatorKinds(new []
                    {
                        (int)BinaryOperatorKind.IntAddition,
                        (int)BinaryOperatorKind.UIntAddition,
                        (int)BinaryOperatorKind.LongAddition,
                        (int)BinaryOperatorKind.ULongAddition,
                        (int)BinaryOperatorKind.FloatAddition,
                        (int)BinaryOperatorKind.DoubleAddition,
                        (int)BinaryOperatorKind.DecimalAddition,
                        (int)BinaryOperatorKind.LiftedIntAddition,
                        (int)BinaryOperatorKind.LiftedUIntAddition,
                        (int)BinaryOperatorKind.LiftedLongAddition,
                        (int)BinaryOperatorKind.LiftedULongAddition,
                        (int)BinaryOperatorKind.LiftedFloatAddition,
                        (int)BinaryOperatorKind.LiftedDoubleAddition,
                        (int)BinaryOperatorKind.LiftedDecimalAddition,
                        (int)BinaryOperatorKind.StringConcatenation,
                        (int)BinaryOperatorKind.StringAndObjectConcatenation,
                        (int)BinaryOperatorKind.ObjectAndStringConcatenation,
                    }),
                    GetSignaturesFromBinaryOperatorKinds(new []
                    {
                        (int)BinaryOperatorKind.IntSubtraction,
                        (int)BinaryOperatorKind.UIntSubtraction,
                        (int)BinaryOperatorKind.LongSubtraction,
                        (int)BinaryOperatorKind.ULongSubtraction,
                        (int)BinaryOperatorKind.FloatSubtraction,
                        (int)BinaryOperatorKind.DoubleSubtraction,
                        (int)BinaryOperatorKind.DecimalSubtraction,
                        (int)BinaryOperatorKind.LiftedIntSubtraction,
                        (int)BinaryOperatorKind.LiftedUIntSubtraction,
                        (int)BinaryOperatorKind.LiftedLongSubtraction,
                        (int)BinaryOperatorKind.LiftedULongSubtraction,
                        (int)BinaryOperatorKind.LiftedFloatSubtraction,
                        (int)BinaryOperatorKind.LiftedDoubleSubtraction,
                        (int)BinaryOperatorKind.LiftedDecimalSubtraction,
                    }),
                    GetSignaturesFromBinaryOperatorKinds(new []
                    {
                        (int)BinaryOperatorKind.IntDivision,
                        (int)BinaryOperatorKind.UIntDivision,
                        (int)BinaryOperatorKind.LongDivision,
                        (int)BinaryOperatorKind.ULongDivision,
                        (int)BinaryOperatorKind.FloatDivision,
                        (int)BinaryOperatorKind.DoubleDivision,
                        (int)BinaryOperatorKind.DecimalDivision,
                        (int)BinaryOperatorKind.LiftedIntDivision,
                        (int)BinaryOperatorKind.LiftedUIntDivision,
                        (int)BinaryOperatorKind.LiftedLongDivision,
                        (int)BinaryOperatorKind.LiftedULongDivision,
                        (int)BinaryOperatorKind.LiftedFloatDivision,
                        (int)BinaryOperatorKind.LiftedDoubleDivision,
                        (int)BinaryOperatorKind.LiftedDecimalDivision,
                    }),
                    GetSignaturesFromBinaryOperatorKinds(new []
                    {
                        (int)BinaryOperatorKind.IntRemainder,
                        (int)BinaryOperatorKind.UIntRemainder,
                        (int)BinaryOperatorKind.LongRemainder,
                        (int)BinaryOperatorKind.ULongRemainder,
                        (int)BinaryOperatorKind.FloatRemainder,
                        (int)BinaryOperatorKind.DoubleRemainder,
                        (int)BinaryOperatorKind.DecimalRemainder,
                        (int)BinaryOperatorKind.LiftedIntRemainder,
                        (int)BinaryOperatorKind.LiftedUIntRemainder,
                        (int)BinaryOperatorKind.LiftedLongRemainder,
                        (int)BinaryOperatorKind.LiftedULongRemainder,
                        (int)BinaryOperatorKind.LiftedFloatRemainder,
                        (int)BinaryOperatorKind.LiftedDoubleRemainder,
                        (int)BinaryOperatorKind.LiftedDecimalRemainder,
                    }),
                    GetSignaturesFromBinaryOperatorKinds(new []
                    {
                        (int)BinaryOperatorKind.IntLeftShift,
                        (int)BinaryOperatorKind.UIntLeftShift,
                        (int)BinaryOperatorKind.LongLeftShift,
                        (int)BinaryOperatorKind.ULongLeftShift,
                        (int)BinaryOperatorKind.LiftedIntLeftShift,
                        (int)BinaryOperatorKind.LiftedUIntLeftShift,
                        (int)BinaryOperatorKind.LiftedLongLeftShift,
                        (int)BinaryOperatorKind.LiftedULongLeftShift,
                    }),
                    GetSignaturesFromBinaryOperatorKinds(new []
                    {
                        (int)BinaryOperatorKind.IntRightShift,
                        (int)BinaryOperatorKind.UIntRightShift,
                        (int)BinaryOperatorKind.LongRightShift,
                        (int)BinaryOperatorKind.ULongRightShift,
                        (int)BinaryOperatorKind.LiftedIntRightShift,
                        (int)BinaryOperatorKind.LiftedUIntRightShift,
                        (int)BinaryOperatorKind.LiftedLongRightShift,
                        (int)BinaryOperatorKind.LiftedULongRightShift,
                    }),
                    GetSignaturesFromBinaryOperatorKinds(new []
                    {
                        (int)BinaryOperatorKind.IntEqual,
                        (int)BinaryOperatorKind.UIntEqual,
                        (int)BinaryOperatorKind.LongEqual,
                        (int)BinaryOperatorKind.ULongEqual,
                        (int)BinaryOperatorKind.FloatEqual,
                        (int)BinaryOperatorKind.DoubleEqual,
                        (int)BinaryOperatorKind.DecimalEqual,
                        (int)BinaryOperatorKind.BoolEqual,
                        (int)BinaryOperatorKind.LiftedIntEqual,
                        (int)BinaryOperatorKind.LiftedUIntEqual,
                        (int)BinaryOperatorKind.LiftedLongEqual,
                        (int)BinaryOperatorKind.LiftedULongEqual,
                        (int)BinaryOperatorKind.LiftedFloatEqual,
                        (int)BinaryOperatorKind.LiftedDoubleEqual,
                        (int)BinaryOperatorKind.LiftedDecimalEqual,
                        (int)BinaryOperatorKind.LiftedBoolEqual,
                        (int)BinaryOperatorKind.ObjectEqual,
                        (int)BinaryOperatorKind.StringEqual,
                    }),
                    GetSignaturesFromBinaryOperatorKinds(new []
                    {
                        (int)BinaryOperatorKind.IntNotEqual,
                        (int)BinaryOperatorKind.UIntNotEqual,
                        (int)BinaryOperatorKind.LongNotEqual,
                        (int)BinaryOperatorKind.ULongNotEqual,
                        (int)BinaryOperatorKind.FloatNotEqual,
                        (int)BinaryOperatorKind.DoubleNotEqual,
                        (int)BinaryOperatorKind.DecimalNotEqual,
                        (int)BinaryOperatorKind.BoolNotEqual,
                        (int)BinaryOperatorKind.LiftedIntNotEqual,
                        (int)BinaryOperatorKind.LiftedUIntNotEqual,
                        (int)BinaryOperatorKind.LiftedLongNotEqual,
                        (int)BinaryOperatorKind.LiftedULongNotEqual,
                        (int)BinaryOperatorKind.LiftedFloatNotEqual,
                        (int)BinaryOperatorKind.LiftedDoubleNotEqual,
                        (int)BinaryOperatorKind.LiftedDecimalNotEqual,
                        (int)BinaryOperatorKind.LiftedBoolNotEqual,
                        (int)BinaryOperatorKind.ObjectNotEqual,
                        (int)BinaryOperatorKind.StringNotEqual,
                    }),
                    GetSignaturesFromBinaryOperatorKinds(new []
                    {
                        (int)BinaryOperatorKind.IntGreaterThan,
                        (int)BinaryOperatorKind.UIntGreaterThan,
                        (int)BinaryOperatorKind.LongGreaterThan,
                        (int)BinaryOperatorKind.ULongGreaterThan,
                        (int)BinaryOperatorKind.FloatGreaterThan,
                        (int)BinaryOperatorKind.DoubleGreaterThan,
                        (int)BinaryOperatorKind.DecimalGreaterThan,
                        (int)BinaryOperatorKind.LiftedIntGreaterThan,
                        (int)BinaryOperatorKind.LiftedUIntGreaterThan,
                        (int)BinaryOperatorKind.LiftedLongGreaterThan,
                        (int)BinaryOperatorKind.LiftedULongGreaterThan,
                        (int)BinaryOperatorKind.LiftedFloatGreaterThan,
                        (int)BinaryOperatorKind.LiftedDoubleGreaterThan,
                        (int)BinaryOperatorKind.LiftedDecimalGreaterThan,
                    }),
                    GetSignaturesFromBinaryOperatorKinds(new []
                    {
                        (int)BinaryOperatorKind.IntLessThan,
                        (int)BinaryOperatorKind.UIntLessThan,
                        (int)BinaryOperatorKind.LongLessThan,
                        (int)BinaryOperatorKind.ULongLessThan,
                        (int)BinaryOperatorKind.FloatLessThan,
                        (int)BinaryOperatorKind.DoubleLessThan,
                        (int)BinaryOperatorKind.DecimalLessThan,
                        (int)BinaryOperatorKind.LiftedIntLessThan,
                        (int)BinaryOperatorKind.LiftedUIntLessThan,
                        (int)BinaryOperatorKind.LiftedLongLessThan,
                        (int)BinaryOperatorKind.LiftedULongLessThan,
                        (int)BinaryOperatorKind.LiftedFloatLessThan,
                        (int)BinaryOperatorKind.LiftedDoubleLessThan,
                        (int)BinaryOperatorKind.LiftedDecimalLessThan,
                    }),
                    GetSignaturesFromBinaryOperatorKinds(new []
                    {
                        (int)BinaryOperatorKind.IntGreaterThanOrEqual,
                        (int)BinaryOperatorKind.UIntGreaterThanOrEqual,
                        (int)BinaryOperatorKind.LongGreaterThanOrEqual,
                        (int)BinaryOperatorKind.ULongGreaterThanOrEqual,
                        (int)BinaryOperatorKind.FloatGreaterThanOrEqual,
                        (int)BinaryOperatorKind.DoubleGreaterThanOrEqual,
                        (int)BinaryOperatorKind.DecimalGreaterThanOrEqual,
                        (int)BinaryOperatorKind.LiftedIntGreaterThanOrEqual,
                        (int)BinaryOperatorKind.LiftedUIntGreaterThanOrEqual,
                        (int)BinaryOperatorKind.LiftedLongGreaterThanOrEqual,
                        (int)BinaryOperatorKind.LiftedULongGreaterThanOrEqual,
                        (int)BinaryOperatorKind.LiftedFloatGreaterThanOrEqual,
                        (int)BinaryOperatorKind.LiftedDoubleGreaterThanOrEqual,
                        (int)BinaryOperatorKind.LiftedDecimalGreaterThanOrEqual,
                    }),
                    GetSignaturesFromBinaryOperatorKinds(new []
                    {
                        (int)BinaryOperatorKind.IntLessThanOrEqual,
                        (int)BinaryOperatorKind.UIntLessThanOrEqual,
                        (int)BinaryOperatorKind.LongLessThanOrEqual,
                        (int)BinaryOperatorKind.ULongLessThanOrEqual,
                        (int)BinaryOperatorKind.FloatLessThanOrEqual,
                        (int)BinaryOperatorKind.DoubleLessThanOrEqual,
                        (int)BinaryOperatorKind.DecimalLessThanOrEqual,
                        (int)BinaryOperatorKind.LiftedIntLessThanOrEqual,
                        (int)BinaryOperatorKind.LiftedUIntLessThanOrEqual,
                        (int)BinaryOperatorKind.LiftedLongLessThanOrEqual,
                        (int)BinaryOperatorKind.LiftedULongLessThanOrEqual,
                        (int)BinaryOperatorKind.LiftedFloatLessThanOrEqual,
                        (int)BinaryOperatorKind.LiftedDoubleLessThanOrEqual,
                        (int)BinaryOperatorKind.LiftedDecimalLessThanOrEqual,
                    }),
                    GetSignaturesFromBinaryOperatorKinds(new []
                    {
                        (int)BinaryOperatorKind.IntAnd,
                        (int)BinaryOperatorKind.UIntAnd,
                        (int)BinaryOperatorKind.LongAnd,
                        (int)BinaryOperatorKind.ULongAnd,
                        (int)BinaryOperatorKind.BoolAnd,
                        (int)BinaryOperatorKind.LiftedIntAnd,
                        (int)BinaryOperatorKind.LiftedUIntAnd,
                        (int)BinaryOperatorKind.LiftedLongAnd,
                        (int)BinaryOperatorKind.LiftedULongAnd,
                        (int)BinaryOperatorKind.LiftedBoolAnd,
                    }),
                    GetSignaturesFromBinaryOperatorKinds(new []
                    {
                        (int)BinaryOperatorKind.IntXor,
                        (int)BinaryOperatorKind.UIntXor,
                        (int)BinaryOperatorKind.LongXor,
                        (int)BinaryOperatorKind.ULongXor,
                        (int)BinaryOperatorKind.BoolXor,
                        (int)BinaryOperatorKind.LiftedIntXor,
                        (int)BinaryOperatorKind.LiftedUIntXor,
                        (int)BinaryOperatorKind.LiftedLongXor,
                        (int)BinaryOperatorKind.LiftedULongXor,
                        (int)BinaryOperatorKind.LiftedBoolXor,
                    }),
                    GetSignaturesFromBinaryOperatorKinds(new []
                    {
                        (int)BinaryOperatorKind.IntOr,
                        (int)BinaryOperatorKind.UIntOr,
                        (int)BinaryOperatorKind.LongOr,
                        (int)BinaryOperatorKind.ULongOr,
                        (int)BinaryOperatorKind.BoolOr,
                        (int)BinaryOperatorKind.LiftedIntOr,
                        (int)BinaryOperatorKind.LiftedUIntOr,
                        (int)BinaryOperatorKind.LiftedLongOr,
                        (int)BinaryOperatorKind.LiftedULongOr,
                        (int)BinaryOperatorKind.LiftedBoolOr,
                    }),
                };

                var allOperators = new[] { nonLogicalOperators, logicalOperators };

                Interlocked.CompareExchange(ref _builtInOperators, allOperators, null);
            }

            operators.AddRange(_builtInOperators[kind.IsLogical() ? 1 : 0][kind.OperatorIndex()]);
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
                    return new BinaryOperatorSignature(kind, left, RightType(kind), ReturnType(kind));
                case BinaryOperatorKind.LeftShift:

                case BinaryOperatorKind.RightShift:
                    TypeSymbol returnType = _compilation.GetSpecialType(SpecialType.System_Int32);

                    if (kind.IsLifted())
                    {
                        returnType = _compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(returnType);
                    }

                    return new BinaryOperatorSignature(kind, left, returnType, left);

                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThanOrEqual:
                    return new BinaryOperatorSignature(kind, left, left, _compilation.GetSpecialType(SpecialType.System_Boolean));
            }
            return new BinaryOperatorSignature(kind, left, RightType(kind), ReturnType(kind));
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
                    case BinaryOperatorKind.Int: return _compilation.GetSpecialType(SpecialType.System_Int32);
                    case BinaryOperatorKind.UInt: return _compilation.GetSpecialType(SpecialType.System_UInt32);
                    case BinaryOperatorKind.Long: return _compilation.GetSpecialType(SpecialType.System_Int64);
                    case BinaryOperatorKind.ULong: return _compilation.GetSpecialType(SpecialType.System_UInt64);
                    case BinaryOperatorKind.Float: return _compilation.GetSpecialType(SpecialType.System_Single);
                    case BinaryOperatorKind.Double: return _compilation.GetSpecialType(SpecialType.System_Double);
                    case BinaryOperatorKind.Decimal: return _compilation.GetSpecialType(SpecialType.System_Decimal);
                    case BinaryOperatorKind.Bool: return _compilation.GetSpecialType(SpecialType.System_Boolean);
                    case BinaryOperatorKind.ObjectAndString:
                    case BinaryOperatorKind.Object:
                        return _compilation.GetSpecialType(SpecialType.System_Object);
                    case BinaryOperatorKind.String:
                    case BinaryOperatorKind.StringAndObject:
                        return _compilation.GetSpecialType(SpecialType.System_String);
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
                    case BinaryOperatorKind.Int: return _compilation.GetSpecialType(SpecialType.System_Int32);
                    case BinaryOperatorKind.UInt: return _compilation.GetSpecialType(SpecialType.System_UInt32);
                    case BinaryOperatorKind.Long: return _compilation.GetSpecialType(SpecialType.System_Int64);
                    case BinaryOperatorKind.ULong: return _compilation.GetSpecialType(SpecialType.System_UInt64);
                    case BinaryOperatorKind.Float: return _compilation.GetSpecialType(SpecialType.System_Single);
                    case BinaryOperatorKind.Double: return _compilation.GetSpecialType(SpecialType.System_Double);
                    case BinaryOperatorKind.Decimal: return _compilation.GetSpecialType(SpecialType.System_Decimal);
                    case BinaryOperatorKind.Bool: return _compilation.GetSpecialType(SpecialType.System_Boolean);
                    case BinaryOperatorKind.ObjectAndString:
                    case BinaryOperatorKind.String:
                        return _compilation.GetSpecialType(SpecialType.System_String);
                    case BinaryOperatorKind.StringAndObject:
                    case BinaryOperatorKind.Object:
                        return _compilation.GetSpecialType(SpecialType.System_Object);
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
                    case BinaryOperatorKind.Int: return _compilation.GetSpecialType(SpecialType.System_Int32);
                    case BinaryOperatorKind.UInt: return _compilation.GetSpecialType(SpecialType.System_UInt32);
                    case BinaryOperatorKind.Long: return _compilation.GetSpecialType(SpecialType.System_Int64);
                    case BinaryOperatorKind.ULong: return _compilation.GetSpecialType(SpecialType.System_UInt64);
                    case BinaryOperatorKind.Float: return _compilation.GetSpecialType(SpecialType.System_Single);
                    case BinaryOperatorKind.Double: return _compilation.GetSpecialType(SpecialType.System_Double);
                    case BinaryOperatorKind.Decimal: return _compilation.GetSpecialType(SpecialType.System_Decimal);
                    case BinaryOperatorKind.Bool: return _compilation.GetSpecialType(SpecialType.System_Boolean);
                    case BinaryOperatorKind.Object: return _compilation.GetSpecialType(SpecialType.System_Object);
                    case BinaryOperatorKind.ObjectAndString:
                    case BinaryOperatorKind.StringAndObject:
                    case BinaryOperatorKind.String:
                        return _compilation.GetSpecialType(SpecialType.System_String);
                }
            }
            Debug.Assert(false, "Bad operator kind in return type");
            return null;
        }

        private TypeSymbol LiftedType(BinaryOperatorKind kind)
        {
            Debug.Assert(kind.IsLifted());

            var nullable = _compilation.GetSpecialType(SpecialType.System_Nullable_T);

            switch (kind.OperandTypes())
            {
                case BinaryOperatorKind.Int: return nullable.Construct(_compilation.GetSpecialType(SpecialType.System_Int32));
                case BinaryOperatorKind.UInt: return nullable.Construct(_compilation.GetSpecialType(SpecialType.System_UInt32));
                case BinaryOperatorKind.Long: return nullable.Construct(_compilation.GetSpecialType(SpecialType.System_Int64));
                case BinaryOperatorKind.ULong: return nullable.Construct(_compilation.GetSpecialType(SpecialType.System_UInt64));
                case BinaryOperatorKind.Float: return nullable.Construct(_compilation.GetSpecialType(SpecialType.System_Single));
                case BinaryOperatorKind.Double: return nullable.Construct(_compilation.GetSpecialType(SpecialType.System_Double));
                case BinaryOperatorKind.Decimal: return nullable.Construct(_compilation.GetSpecialType(SpecialType.System_Decimal));
                case BinaryOperatorKind.Bool: return nullable.Construct(_compilation.GetSpecialType(SpecialType.System_Boolean));
            }
            Debug.Assert(false, "Bad operator kind in lifted type");
            return null;
        }

        internal static bool IsValidObjectEquality(Conversions Conversions, TypeSymbol leftType, bool leftIsNull, bool leftIsDefault, TypeSymbol rightType, bool rightIsNull, bool rightIsDefault, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // SPEC: The predefined reference type equality operators require one of the following:

            // SPEC: (1) Both operands are a value of a type known to be a reference-type or the literal null.
            // SPEC:     Furthermore, an explicit reference conversion exists from the type of either
            // SPEC:     operand to the type of the other operand. Or:
            // SPEC: (2) One operand is a value of type T where T is a type-parameter and the other operand is 
            // SPEC:     the literal null. Furthermore T does not have the value type constraint.
            // SPEC: (3) One operand is the literal default and the other operand is a reference-type.

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
            if (!leftIsReferenceType && !leftIsNull && !leftIsDefault)
            {
                return false;
            }

            var rightIsReferenceType = ((object)rightType != null) && rightType.IsReferenceType;
            if (!rightIsReferenceType && !rightIsNull && !rightIsDefault)
            {
                return false;
            }

            if (leftIsDefault && rightIsDefault)
            {
                return false;
            }

            // If at least one side is null or default then clearly a conversion exists.
            if (leftIsNull || rightIsNull || leftIsDefault || rightIsDefault)
            {
                return true;
            }

            var leftConversion = Conversions.ClassifyConversionFromType(leftType, rightType, ref useSiteDiagnostics);
            if (leftConversion.IsIdentity || leftConversion.IsReference)
            {
                return true;
            }

            var rightConversion = Conversions.ClassifyConversionFromType(rightType, leftType, ref useSiteDiagnostics);
            if (rightConversion.IsIdentity || rightConversion.IsReference)
            {
                return true;
            }

            return false;
        }
    }
}
