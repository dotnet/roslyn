// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    public static class UnaryAndBinaryOperationExtensions
    {
        const int SimpleUnaryOperationKindMask = 0xff;
        const int UnaryOperandKindMask = 0xff00;
        const int SimpleBinaryOperationKindMask = 0xff;
        const int BinaryOperandsKindMask = 0xff00;

        /// <summary>
        /// Get unary operation kind independent of data type.
        /// </summary>
        public static SimpleUnaryOperationKind GetSimpleUnaryOperationKind(this IUnaryOperatorExpression unary)
        {
            return GetSimpleUnaryOperationKind(unary.UnaryOperationKind);
        }

        /// <summary>
        /// Get unary operation kind independent of data type.
        /// </summary>
        public static SimpleUnaryOperationKind GetSimpleUnaryOperationKind(this IIncrementExpression increment)
        {
            return GetSimpleUnaryOperationKind(increment.IncrementOperationKind);
        }

        /// <summary>
        /// Get unary operand kind.
        /// </summary>
        public static UnaryOperandKind GetUnaryOperandKind(this IUnaryOperatorExpression unary)
        {
            return GetUnaryOperandKind(unary.UnaryOperationKind);
        }

        /// <summary>
        /// Get unary operand kind.
        /// </summary>
        public static UnaryOperandKind GetUnaryOperandKind(this IIncrementExpression increment)
        {
            return GetUnaryOperandKind(increment.IncrementOperationKind);
        }

        /// <summary>
        /// Get binary operation kind independent of data type.
        /// </summary>
        public static SimpleBinaryOperationKind GetSimpleBinaryOperationKind(this IBinaryOperatorExpression binary)
        {
            return GetSimpleBinaryOperationKind(binary.BinaryOperationKind);
        }

        /// <summary>
        /// Get binary operation kind independent of data type.
        /// </summary>
        public static SimpleBinaryOperationKind GetSimpleBinaryOperationKind(this ICompoundAssignmentExpression compoundAssignment)
        {
            return GetSimpleBinaryOperationKind(compoundAssignment.BinaryOperationKind);
        }

        /// <summary>
        /// Get binary operand kinds.
        /// </summary>
        public static BinaryOperandsKind GetBinaryOperandsKind(this IBinaryOperatorExpression binary)
        {
            return GetBinaryOperandsKind(binary.BinaryOperationKind);
        }

        /// <summary>
        /// Get binary operand kinds.
        /// </summary>
        public static BinaryOperandsKind GetBinaryOperandsKind(this ICompoundAssignmentExpression compoundAssignment)
        {
            return GetBinaryOperandsKind(compoundAssignment.BinaryOperationKind);
        }

        public static SimpleUnaryOperationKind GetSimpleUnaryOperationKind(UnaryOperationKind kind)
        {
            return (SimpleUnaryOperationKind)((int)kind & UnaryAndBinaryOperationExtensions.SimpleUnaryOperationKindMask);
        }

        public static UnaryOperandKind GetUnaryOperandKind(UnaryOperationKind kind)
        {
            return (UnaryOperandKind)((int)kind & UnaryAndBinaryOperationExtensions.UnaryOperandKindMask);
        }

        public static SimpleBinaryOperationKind GetSimpleBinaryOperationKind(BinaryOperationKind kind)
        {
            return (SimpleBinaryOperationKind)((int)kind & UnaryAndBinaryOperationExtensions.SimpleBinaryOperationKindMask);
        }

        public static BinaryOperandsKind GetBinaryOperandsKind(BinaryOperationKind kind)
        {
            return (BinaryOperandsKind)((int)kind & UnaryAndBinaryOperationExtensions.BinaryOperandsKindMask);
        }
    }
}

