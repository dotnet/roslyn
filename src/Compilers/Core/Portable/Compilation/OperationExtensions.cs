// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Semantics
{
    public static class OperationExtensions
    {
        public static IEnumerable<IOperation> Descendants(this IOperation operation)
        {
            if (operation == null)
            {
                return SpecializedCollections.EmptyEnumerable<IOperation>();
            }
            var list = new List<IOperation>();
            var collector = new OperationCollector(list);
            collector.Visit(operation);
            list.RemoveAt(0);
            return list;
        }

        public static IEnumerable<IOperation> DescendantsAndSelf(this IOperation operation)
        {
            if (operation == null)
            {
                return SpecializedCollections.EmptyEnumerable<IOperation>();
            }
            var list = new List<IOperation>();
            var collector = new OperationCollector(list);
            collector.Visit(operation);
            return list;
        }

        /// <summary>
        /// Get unary operation kind dependent of operand type.
        /// </summary>
        internal static TypedUnaryOperationKind GetTypedUnaryOperationKind(this IUnaryOperatorExpression unary)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get unary operation kind dependent of operand type.
        /// </summary>
        internal static TypedUnaryOperationKind GetTypedUnaryOperationKind(this IIncrementExpression increment)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get unary operand kind.
        /// </summary>
        internal static UnaryOperandKind GetUnaryOperandKind(this IUnaryOperatorExpression unary)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get unary operand kind.
        /// </summary>
        internal static UnaryOperandKind GetUnaryOperandKind(this IIncrementExpression increment)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get binary operation kind dependent of operands type.
        /// </summary>
        internal static TypedBinaryOperationKind GetTypedBinaryOperationKind(this IBinaryOperatorExpression binary)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get binary operation kind dependent of operands type.
        /// </summary>
        internal static TypedBinaryOperationKind GetTypedBinaryOperationKind(this ICompoundAssignmentExpression compoundAssignment)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get binary operand kinds.
        /// </summary>
        internal static BinaryOperandsKind GetBinaryOperandsKind(this IBinaryOperatorExpression binary)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get binary operand kinds.
        /// </summary>
        internal static BinaryOperandsKind GetBinaryOperandsKind(this ICompoundAssignmentExpression compoundAssignment)
        {
            throw new NotImplementedException();
        }

        internal static TypedBinaryOperationKind GetEqualityOperationKind(this ISingleValueCaseClause singleValueCaseClause)
        {
            throw new NotImplementedException();
        }

        internal static TypedBinaryOperationKind GetEqualityOperationKind(this IRelationalCaseClause relationalCaseClause)
        {
            throw new NotImplementedException();
        }

        public static IOperation GetRootOperation(this ISymbol symbol, CancellationToken cancellationToken = default(CancellationToken))
        {
            var symbolWithOperation = symbol as ISymbolWithOperation;
            if (symbolWithOperation != null)
            {
                return symbolWithOperation.GetRootOperation(cancellationToken);
            }
            else
            {
                return null;
            }
        }

        private sealed class OperationCollector : OperationWalker
        {
            private readonly List<IOperation> _list;

            public OperationCollector(List<IOperation> list)
            {
                _list = list;
            }

            public override void Visit(IOperation operation)
            {
                if (operation != null)
                {
                    _list.Add(operation);
                }
                base.Visit(operation);
            }
        }
    }

    internal interface ISymbolWithOperation
    {
        IOperation GetRootOperation(CancellationToken cancellationToken);
    }
}