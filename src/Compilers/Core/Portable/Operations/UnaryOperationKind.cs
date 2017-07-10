// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Kinds of unary operations.
    /// </summary>
    public enum UnaryOperationKind
    {
        None = 0x0,

        OperatorMethodBitwiseNegation = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.BitwiseNegation,
        OperatorMethodLogicalNot = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.LogicalNot,
        OperatorMethodPostfixIncrement = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.PostfixIncrement,
        OperatorMethodPostfixDecrement = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.PostfixDecrement,
        OperatorMethodPrefixIncrement = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.PrefixIncrement,
        OperatorMethodPrefixDecrement = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.PrefixDecrement,
        OperatorMethodPlus = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.Plus,
        OperatorMethodMinus = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.Minus,
        OperatorMethodTrue = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.True,
        OperatorMethodFalse = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.False,

        IntegerBitwiseNegation = UnaryOperandKind.Integer | SimpleUnaryOperationKind.BitwiseNegation,
        IntegerPlus = UnaryOperandKind.Integer | SimpleUnaryOperationKind.Plus,
        IntegerMinus = UnaryOperandKind.Integer | SimpleUnaryOperationKind.Minus,
        IntegerPostfixIncrement = UnaryOperandKind.Integer | SimpleUnaryOperationKind.PostfixIncrement,
        IntegerPostfixDecrement = UnaryOperandKind.Integer | SimpleUnaryOperationKind.PostfixDecrement,
        IntegerPrefixIncrement = UnaryOperandKind.Integer | SimpleUnaryOperationKind.PrefixIncrement,
        IntegerPrefixDecrement = UnaryOperandKind.Integer | SimpleUnaryOperationKind.PrefixDecrement,

        UnsignedPostfixIncrement = UnaryOperandKind.Unsigned | SimpleUnaryOperationKind.PostfixIncrement,
        UnsignedPostfixDecrement = UnaryOperandKind.Unsigned | SimpleUnaryOperationKind.PostfixDecrement,
        UnsignedPrefixIncrement = UnaryOperandKind.Unsigned | SimpleUnaryOperationKind.PrefixIncrement,
        UnsignedPrefixDecrement = UnaryOperandKind.Unsigned | SimpleUnaryOperationKind.PrefixDecrement,

        FloatingPlus = UnaryOperandKind.Floating | SimpleUnaryOperationKind.Plus,
        FloatingMinus = UnaryOperandKind.Floating | SimpleUnaryOperationKind.Minus,
        FloatingPostfixIncrement = UnaryOperandKind.Floating | SimpleUnaryOperationKind.PostfixIncrement,
        FloatingPostfixDecrement = UnaryOperandKind.Floating | SimpleUnaryOperationKind.PostfixDecrement,
        FloatingPrefixIncrement = UnaryOperandKind.Floating | SimpleUnaryOperationKind.PrefixIncrement,
        FloatingPrefixDecrement = UnaryOperandKind.Floating | SimpleUnaryOperationKind.PrefixDecrement,

        DecimalPlus = UnaryOperandKind.Decimal | SimpleUnaryOperationKind.Plus,
        DecimalMinus = UnaryOperandKind.Decimal | SimpleUnaryOperationKind.Minus,
        DecimalPostfixIncrement = UnaryOperandKind.Decimal | SimpleUnaryOperationKind.PostfixIncrement,
        DecimalPostfixDecrement = UnaryOperandKind.Decimal | SimpleUnaryOperationKind.PostfixDecrement,
        DecimalPrefixIncrement = UnaryOperandKind.Decimal | SimpleUnaryOperationKind.PrefixIncrement,
        DecimalPrefixDecrement = UnaryOperandKind.Decimal | SimpleUnaryOperationKind.PrefixDecrement,

        BooleanBitwiseNegation = UnaryOperandKind.Boolean | SimpleUnaryOperationKind.BitwiseNegation,
        BooleanLogicalNot = UnaryOperandKind.Boolean | SimpleUnaryOperationKind.LogicalNot,

        EnumPostfixIncrement = UnaryOperandKind.Enum | SimpleUnaryOperationKind.PostfixIncrement,
        EnumPostfixDecrement = UnaryOperandKind.Enum | SimpleUnaryOperationKind.PostfixDecrement,
        EnumPrefixIncrement = UnaryOperandKind.Enum | SimpleUnaryOperationKind.PrefixIncrement,
        EnumPrefixDecrement = UnaryOperandKind.Enum | SimpleUnaryOperationKind.PrefixDecrement,

        PointerPostfixIncrement = UnaryOperandKind.Pointer | SimpleUnaryOperationKind.PostfixIncrement,
        PointerPostfixDecrement = UnaryOperandKind.Pointer | SimpleUnaryOperationKind.PostfixDecrement,
        PointerPrefixIncrement = UnaryOperandKind.Pointer | SimpleUnaryOperationKind.PrefixIncrement,
        PointerPrefixDecrement = UnaryOperandKind.Pointer | SimpleUnaryOperationKind.PrefixDecrement,

        DynamicBitwiseNegation = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.BitwiseNegation,
        DynamicLogicalNot = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.LogicalNot,
        DynamicTrue = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.True,
        DynamicFalse = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.False,
        DynamicPlus = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.Plus,
        DynamicMinus = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.Minus,
        DynamicPostfixIncrement = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.PostfixIncrement,
        DynamicPostfixDecrement = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.PostfixDecrement,
        DynamicPrefixIncrement = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.PrefixIncrement,
        DynamicPrefixDecrement = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.PrefixDecrement,

        ObjectPlus = UnaryOperandKind.Object | SimpleUnaryOperationKind.Plus,
        ObjectMinus = UnaryOperandKind.Object | SimpleUnaryOperationKind.Minus,
        ObjectNot = UnaryOperandKind.Object | SimpleUnaryOperationKind.BitwiseOrLogicalNot,

        Invalid = UnaryOperandKind.Invalid | SimpleUnaryOperationKind.Invalid
    }
}

