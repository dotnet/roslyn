// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Semantics
{
    internal sealed partial class ConditionalChoiceExpression : IConditionalChoiceExpression
    {
        public ConditionalChoiceExpression(IOperation condition, IOperation ifTrue, IOperation ifFalse, ITypeSymbol resultType, SyntaxNode syntax) :
            this(condition,
                ifTrue,
                ifFalse,
                IsInvalidOperation(condition, ifTrue, ifFalse, resultType),
                syntax,
                resultType,
                default(Optional<object>))
        {
        }

        private static bool IsInvalidOperation(IOperation condition, IOperation ifTrue, IOperation ifFalse, ITypeSymbol resultType)
        {
            return (condition == null || condition.IsInvalid || ifTrue == null || ifTrue.IsInvalid || ifFalse == null || ifFalse.IsInvalid || resultType == null);
        }
    }

    internal sealed partial class ExpressionStatement : IExpressionStatement
    {
        public ExpressionStatement(IOperation target, IOperation value, SyntaxNode syntax) :
            this(new AssignmentExpression(target, value, IsInvalidOperation(target, value), syntax, target.Type, default(Optional<object>)),
                syntax)
        {
        }

        public ExpressionStatement(IOperation target, IOperation value, BinaryOperationKind binaryOperationKind, IMethodSymbol operatorMethod, SyntaxNode syntax) :
            this(new CompoundAssignmentExpression(
                    binaryOperationKind,
                    target,
                    value,
                    operatorMethod != null,
                    operatorMethod,
                    IsInvalidOperation(target, value),
                    syntax,
                    target.Type,
                    default(Optional<object>)),
                syntax)
        {
        }

        private ExpressionStatement(IOperation expression, SyntaxNode syntax) :
            this(expression, expression.IsInvalid, syntax, type: null, constantValue: default(Optional<object>))
        {
        }

        private static bool IsInvalidOperation(IOperation target, IOperation value)
        {
            return target == null || target.IsInvalid || value == null || value.IsInvalid;
        }
    }

    internal sealed partial class LiteralExpression : ILiteralExpression
    {
        public LiteralExpression(long value, ITypeSymbol resultType, SyntaxNode syntax) :
            this(value.ToString(), isInvalid: false, syntax: syntax, type: resultType, constantValue: new Optional<object>(value))
        {
        }

        public LiteralExpression(ConstantValue value, ITypeSymbol resultType, SyntaxNode syntax) :
            this(value.GetValueToDisplay(), value.IsBad, syntax, resultType, new Optional<object>(value.Value))
        {
        }
    }

    internal sealed partial class BinaryOperatorExpression : IBinaryOperatorExpression
    {
        public BinaryOperatorExpression(BinaryOperationKind binaryOperationKind, IOperation left, IOperation right, ITypeSymbol resultType, SyntaxNode syntax) :
            this(binaryOperationKind, left, right,
                usesOperatorMethod: false, operatorMethod: null,
                isInvalid: IsInvalidOperation(binaryOperationKind, left, right, resultType),
                syntax: syntax, type: resultType, constantValue: default(Optional<object>))
        {
        }

        private static bool IsInvalidOperation(BinaryOperationKind binaryOperationKind, IOperation left, IOperation right, ITypeSymbol type)
        {
            return left == null || left.IsInvalid || right == null
                   || right.IsInvalid || binaryOperationKind == BinaryOperationKind.Invalid || type == null;
        }
    }

    internal sealed partial class ArrayCreationExpression : IArrayCreationExpression
    {
        public ArrayCreationExpression(IArrayTypeSymbol arrayType, ImmutableArray<IOperation> elementValues, SyntaxNode syntax) :
            this(arrayType.ElementType,
                ImmutableArray.Create<IOperation>(new LiteralExpression(elementValues.Count(), resultType: null, syntax: syntax)),
                new ArrayInitializer(elementValues, elementValues.Any(v => v.IsInvalid), syntax, arrayType, default(Optional<object>)),
                syntax,
                arrayType,
                default(Optional<object>))
        {
        }

        private ArrayCreationExpression(ITypeSymbol elementType, ImmutableArray<IOperation> dimensionSizes, IArrayInitializer initializer, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            this(elementType, dimensionSizes, initializer, initializer.IsInvalid, syntax, type, constantValue)
        {
        }
    }

    internal partial class InvalidExpression : IInvalidExpression
    {
        public InvalidExpression(SyntaxNode syntax, ImmutableArray<IOperation> children) :
            this(children: children, isInvalid: true, syntax: syntax, type: null, constantValue: default(Optional<object>))
        {
        }
    }
}
