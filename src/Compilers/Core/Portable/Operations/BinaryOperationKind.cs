// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Kinds of binary operations.
    /// </summary>
    public enum BinaryOperationKind
    {
        None = 0x0,

        OperatorMethodAdd = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.Add,
        OperatorMethodSubtract = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.Subtract,
        OperatorMethodMultiply = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.Multiply,
        OperatorMethodDivide = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.Divide,
        OperatorMethodIntegerDivide = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.IntegerDivide,
        OperatorMethodRemainder = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.Remainder,
        OperatorMethodLeftShift = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.LeftShift,
        OperatorMethodRightShift = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.RightShift,
        OperatorMethodAnd = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.And,
        OperatorMethodOr = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.Or,
        OperatorMethodExclusiveOr = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.ExclusiveOr,
        OperatorMethodConditionalAnd = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.ConditionalAnd,
        OperatorMethodConditionalOr = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.ConditionalOr,

        IntegerAdd = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.Add,
        IntegerSubtract = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.Subtract,
        IntegerMultiply = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.Multiply,
        IntegerDivide = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.Divide,
        IntegerRemainder = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.Remainder,
        IntegerLeftShift = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.LeftShift,
        IntegerRightShift = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.RightShift,
        IntegerAnd = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.And,
        IntegerOr = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.Or,
        IntegerExclusiveOr = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.ExclusiveOr,

        UnsignedAdd = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.Add,
        UnsignedSubtract = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.Subtract,
        UnsignedMultiply = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.Multiply,
        UnsignedDivide = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.Divide,
        UnsignedRemainder = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.Remainder,
        UnsignedLeftShift = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.LeftShift,
        UnsignedRightShift = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.RightShift,
        UnsignedAnd = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.And,
        UnsignedOr = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.Or,
        UnsignedExclusiveOr = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.ExclusiveOr,

        FloatingAdd = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.Add,
        FloatingSubtract = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.Subtract,
        FloatingMultiply = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.Multiply,
        FloatingDivide = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.Divide,
        FloatingRemainder = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.Remainder,
        FloatingPower = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.Power,

        DecimalAdd = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.Add,
        DecimalSubtract = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.Subtract,
        DecimalMultiply = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.Multiply,
        DecimalDivide = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.Divide,

        BooleanAnd = BinaryOperandsKind.Boolean | SimpleBinaryOperationKind.And,
        BooleanOr = BinaryOperandsKind.Boolean | SimpleBinaryOperationKind.Or,
        BooleanExclusiveOr = BinaryOperandsKind.Boolean | SimpleBinaryOperationKind.ExclusiveOr,
        BooleanConditionalAnd = BinaryOperandsKind.Boolean | SimpleBinaryOperationKind.ConditionalAnd,
        BooleanConditionalOr = BinaryOperandsKind.Boolean | SimpleBinaryOperationKind.ConditionalOr,

        EnumAdd = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.Add,
        EnumSubtract = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.Subtract,
        EnumAnd = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.And,
        EnumOr = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.Or,
        EnumExclusiveOr = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.ExclusiveOr,

        PointerIntegerAdd = BinaryOperandsKind.PointerInteger | SimpleBinaryOperationKind.Add,
        IntegerPointerAdd = BinaryOperandsKind.IntegerPointer | SimpleBinaryOperationKind.Add,
        PointerIntegerSubtract = BinaryOperandsKind.PointerInteger | SimpleBinaryOperationKind.Subtract,
        PointerSubtract = BinaryOperandsKind.Pointer | SimpleBinaryOperationKind.Subtract,

        DynamicAdd = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.Add,
        DynamicSubtract = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.Subtract,
        DynamicMultiply = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.Multiply,
        DynamicDivide = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.Divide,
        DynamicRemainder = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.Remainder,
        DynamicLeftShift = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.LeftShift,
        DynamicRightShift = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.RightShift,
        DynamicAnd = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.And,
        DynamicOr = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.Or,
        DynamicExclusiveOr = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.ExclusiveOr,

        ObjectAdd = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Add,
        ObjectSubtract = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Subtract,
        ObjectMultiply = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Multiply,
        ObjectDivide = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Divide,
        ObjectIntegerDivide = BinaryOperandsKind.Object | SimpleBinaryOperationKind.IntegerDivide,
        ObjectRemainder = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Remainder,
        ObjectPower = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Power,
        ObjectLeftShift = BinaryOperandsKind.Object | SimpleBinaryOperationKind.LeftShift,
        ObjectRightShift = BinaryOperandsKind.Object | SimpleBinaryOperationKind.RightShift,
        ObjectAnd = BinaryOperandsKind.Object | SimpleBinaryOperationKind.And,
        ObjectOr = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Or,
        ObjectExclusiveOr = BinaryOperandsKind.Object | SimpleBinaryOperationKind.ExclusiveOr,
        ObjectConditionalAnd = BinaryOperandsKind.Object | SimpleBinaryOperationKind.ConditionalAnd,
        ObjectConditionalOr = BinaryOperandsKind.Object | SimpleBinaryOperationKind.ConditionalOr,
        ObjectConcatenate = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Concatenate,

        StringConcatenate = BinaryOperandsKind.String | SimpleBinaryOperationKind.Concatenate,

        // Relational operations.

        OperatorMethodEquals = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.Equals,
        OperatorMethodNotEquals = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.NotEquals,
        OperatorMethodLessThan = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.LessThan,
        OperatorMethodLessThanOrEqual = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.LessThanOrEqual,
        OperatorMethodGreaterThanOrEqual = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.GreaterThanOrEqual,
        OperatorMethodGreaterThan = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.GreaterThan,
        OperatorMethodPower = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.Power,

        IntegerEquals = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.Equals,
        IntegerNotEquals = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.NotEquals,
        IntegerLessThan = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.LessThan,
        IntegerLessThanOrEqual = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.LessThanOrEqual,
        IntegerGreaterThanOrEqual = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.GreaterThanOrEqual,
        IntegerGreaterThan = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.GreaterThan,

        UnsignedLessThan = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.LessThan,
        UnsignedLessThanOrEqual = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.LessThanOrEqual,
        UnsignedGreaterThanOrEqual = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.GreaterThanOrEqual,
        UnsignedGreaterThan = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.GreaterThan,

        FloatingEquals = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.Equals,
        FloatingNotEquals = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.NotEquals,
        FloatingLessThan = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.LessThan,
        FloatingLessThanOrEqual = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.LessThanOrEqual,
        FloatingGreaterThanOrEqual = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.GreaterThanOrEqual,
        FloatingGreaterThan = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.GreaterThan,

        DecimalEquals = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.Equals,
        DecimalNotEquals = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.NotEquals,
        DecimalLessThan = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.LessThan,
        DecimalLessThanOrEqual = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.LessThanOrEqual,
        DecimalGreaterThanOrEqual = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.GreaterThanOrEqual,
        DecimalGreaterThan = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.GreaterThan,

        BooleanEquals = BinaryOperandsKind.Boolean | SimpleBinaryOperationKind.Equals,
        BooleanNotEquals = BinaryOperandsKind.Boolean | SimpleBinaryOperationKind.NotEquals,

        StringEquals = BinaryOperandsKind.String | SimpleBinaryOperationKind.Equals,
        StringNotEquals = BinaryOperandsKind.String | SimpleBinaryOperationKind.NotEquals,
        StringLike = BinaryOperandsKind.String | SimpleBinaryOperationKind.Like,

        DelegateEquals = BinaryOperandsKind.Delegate | SimpleBinaryOperationKind.Equals,
        DelegateNotEquals = BinaryOperandsKind.Delegate | SimpleBinaryOperationKind.NotEquals,

        NullableEquals = BinaryOperandsKind.Nullable | SimpleBinaryOperationKind.Equals,
        NullableNotEquals = BinaryOperandsKind.Nullable | SimpleBinaryOperationKind.NotEquals,

        ObjectEquals = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Equals,
        ObjectNotEquals = BinaryOperandsKind.Object | SimpleBinaryOperationKind.NotEquals,
        ObjectVBEquals = BinaryOperandsKind.Object | SimpleBinaryOperationKind.ObjectValueEquals,
        ObjectVBNotEquals = BinaryOperandsKind.Object | SimpleBinaryOperationKind.ObjectValueNotEquals,
        ObjectLike = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Like,
        ObjectLessThan = BinaryOperandsKind.Object | SimpleBinaryOperationKind.LessThan,
        ObjectLessThanOrEqual = BinaryOperandsKind.Object | SimpleBinaryOperationKind.LessThanOrEqual,
        ObjectGreaterThanOrEqual = BinaryOperandsKind.Object | SimpleBinaryOperationKind.GreaterThanOrEqual,
        ObjectGreaterThan = BinaryOperandsKind.Object | SimpleBinaryOperationKind.GreaterThan,

        EnumEquals = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.Equals,
        EnumNotEquals = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.NotEquals,
        EnumLessThan = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.LessThan,
        EnumLessThanOrEqual = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.LessThanOrEqual,
        EnumGreaterThanOrEqual = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.GreaterThanOrEqual,
        EnumGreaterThan = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.GreaterThan,

        PointerEquals = BinaryOperandsKind.Pointer | SimpleBinaryOperationKind.Equals,
        PointerNotEquals = BinaryOperandsKind.Pointer | SimpleBinaryOperationKind.NotEquals,
        PointerLessThan = BinaryOperandsKind.Pointer | SimpleBinaryOperationKind.LessThan,
        PointerLessThanOrEqual = BinaryOperandsKind.Pointer | SimpleBinaryOperationKind.LessThanOrEqual,
        PointerGreaterThanOrEqual = BinaryOperandsKind.Pointer | SimpleBinaryOperationKind.GreaterThanOrEqual,
        PointerGreaterThan = BinaryOperandsKind.Pointer | SimpleBinaryOperationKind.GreaterThan,

        DynamicEquals = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.Equals,
        DynamicNotEquals = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.NotEquals,
        DynamicLessThan = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.LessThan,
        DynamicLessThanOrEqual = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.LessThanOrEqual,
        DynamicGreaterThanOrEqual = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.GreaterThanOrEqual,
        DynamicGreaterThan = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.GreaterThan,

        Invalid = BinaryOperandsKind.Invalid | SimpleBinaryOperationKind.Invalid
    }
}

