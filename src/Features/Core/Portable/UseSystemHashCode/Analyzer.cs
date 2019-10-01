// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseSystemHashCode
{
    internal struct Analyzer
    {
        private readonly IMethodSymbol _getHashCodeMethod;
        private readonly IMethodSymbol _objectGetHashCodeMethod;
        private readonly INamedTypeSymbol _containingType;
        private readonly INamedTypeSymbol _equalityComparerTypeOpt;

        public Analyzer(IMethodSymbol getHashCodeMethod, IMethodSymbol objectGetHashCode, INamedTypeSymbol equalityComparerTypeOpt)
        {
            _getHashCodeMethod = getHashCodeMethod;
            _containingType = _getHashCodeMethod.ContainingType.OriginalDefinition;
            _objectGetHashCodeMethod = objectGetHashCode;
            _equalityComparerTypeOpt = equalityComparerTypeOpt;
        }

        public ImmutableArray<ISymbol> GetHashedMembers(IBlockOperation blockOperation)
        {
            // Unwind through nested blocks.
            while (blockOperation.Operations.Length == 1 &&
                   blockOperation.Operations[0] is IBlockOperation childBlock)
            {
                blockOperation = childBlock;
            }

            // Needs to be of the form:
            //
            //      // accumulator
            //      var hashCode = <initializer>
            //
            //      // 1-N member hashes mixed into the accumulator.
            //      hashCode = (hashCode op constant) op member_hash
            //
            //      // return of the value.
            //      return hashCode;

            var statements = blockOperation.Operations;
            if (statements.Length == 0)
            {
                return default;
            }

            var firstStatement = statements.First();
            var lastStatement = statements.Last();

            if (!(firstStatement is IVariableDeclarationGroupOperation varDeclStatement) ||
                !(lastStatement is IReturnOperation returnStatement))
            {
                return default;
            }

            var variables = varDeclStatement.GetDeclaredVariables();
            if (variables.Length != 1)
            {
                return default;
            }

            if (varDeclStatement.Declarations.Length != 1)
            {
                return default;
            }

            var declaration = varDeclStatement.Declarations[0];
            if (declaration.Declarators.Length != 1)
            {
                return default;
            }

            var declarator = declaration.Declarators[0];
            if (declarator.Initializer == null ||
                declarator.Initializer.Value == null)
            {
                return default;
            }

            var accumulatorVariable = declarator.Symbol;
            if (!(IsLocalReference(returnStatement.ReturnedValue, accumulatorVariable)))
            {
                return default;
            }

            var initializerValue = declarator.Initializer.Value;
            var hashedSymbols = ArrayBuilder<ISymbol>.GetInstance();
            if (!IsLiteralNumber(initializerValue) &&
                !TryGetHashedSymbol(accumulatorVariable, hashedSymbols, initializerValue))
            {
                return default;
            }

            for (var i = 1; i < statements.Length - 1; i++)
            {
                var statement = statements[i];
                if (!(statement is IExpressionStatementOperation expressionStatement) ||
                    !(expressionStatement.Operation is ISimpleAssignmentOperation simpleAssignment) ||
                    !IsLocalReference(simpleAssignment.Target, accumulatorVariable) ||
                    !TryGetHashedSymbol(accumulatorVariable, hashedSymbols, simpleAssignment.Value))
                {
                    return default;
                }
            }

            return hashedSymbols.ToImmutableAndFree();
        }

        private bool TryGetHashedSymbol(
            ILocalSymbol accumulatorVariable, ArrayBuilder<ISymbol> hashedSymbols, IOperation value)
        {
            value = Unwrap(value);
            if (value is IInvocationOperation invocation)
            {
                var targetMethod = invocation.TargetMethod;
                if (OverridesSystemObject(_objectGetHashCodeMethod, targetMethod))
                {
                    // (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
                    // recurse on the value we're calling GetHashCode on.
                    return TryGetHashedSymbol(accumulatorVariable, hashedSymbols, invocation.Instance);
                }

                if (targetMethod.Name == nameof(GetHashCode) &&
                    Equals(_equalityComparerTypeOpt, targetMethod.ContainingType.OriginalDefinition) &&
                    invocation.Arguments.Length == 1)
                {
                    // EqualityComparer<T>.Default.GetHashCode(i)
                    return TryGetHashedSymbol(accumulatorVariable, hashedSymbols, invocation.Arguments[0].Value);
                }
            }

            // (hashCode op1 constant) op1 hashed_value
            if (value is IBinaryOperation topBinary)
            {
                return topBinary.LeftOperand is IBinaryOperation leftBinary &&
                       IsLocalReference(leftBinary.LeftOperand, accumulatorVariable) &&
                       IsLiteralNumber(leftBinary.RightOperand) &&
                       TryGetHashedSymbol(accumulatorVariable, hashedSymbols, topBinary.RightOperand);
            }

            if (value is IInstanceReferenceOperation instanceReference)
            {
                // reference to this/base.
                return instanceReference.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance;
            }

            if (value is IConditionalOperation conditional &&
                conditional.Condition is IBinaryOperation binary)
            {
                if (binary.RightOperand.IsNullLiteral() &&
                    TryGetFieldOrProperty(binary.LeftOperand, out _))
                {
                    if (binary.OperatorKind == BinaryOperatorKind.Equals)
                    {
                        // (StringProperty == null ? 0 : StringProperty.GetHashCode())
                        return TryGetHashedSymbol(accumulatorVariable, hashedSymbols, conditional.WhenFalse);
                    }
                    else if (binary.OperatorKind == BinaryOperatorKind.NotEquals)
                    {
                        // (StringProperty != null ? StringProperty.GetHashCode() : 0)
                        return TryGetHashedSymbol(accumulatorVariable, hashedSymbols, conditional.WhenTrue);
                    }
                }
            }

            if (TryGetFieldOrProperty(value, out var fieldOrProp) &&
                Equals(fieldOrProp.ContainingType.OriginalDefinition, _containingType))
            {
                return Add(hashedSymbols, fieldOrProp);
            }

            return false;
        }

        private static bool TryGetFieldOrProperty(IOperation operation, out ISymbol symbol)
        {
            if (operation is IFieldReferenceOperation fieldReference)
            {
                symbol = fieldReference.Member;
                return true;
            }

            if (operation is IPropertyReferenceOperation propertyReference)
            {
                symbol = propertyReference.Member;
                return true;
            }

            symbol = null;
            return false;
        }

        private bool Add(ArrayBuilder<ISymbol> hashedSymbols, ISymbol member)
        {
            foreach (var symbol in hashedSymbols)
            {
                if (Equals(symbol, member))
                {
                    return false;
                }
            }

            hashedSymbols.Add(member);
            return true;
        }

        private static bool IsLiteralNumber(IOperation value)
        {
            value = Unwrap(value);
            return value is IUnaryOperation unary
                ? unary.OperatorKind == UnaryOperatorKind.Minus && IsLiteralNumber(unary.Operand)
                : value.IsNumericLiteral();
        }

        private static bool IsLocalReference(IOperation value, ILocalSymbol accumulatorVariable)
            => Unwrap(value) is ILocalReferenceOperation localReference && accumulatorVariable.Equals(localReference.Local);

        private static IOperation Unwrap(IOperation value)
        {
            while (true)
            {
                if (value is IConversionOperation conversion)
                {
                    value = conversion.Operand;
                }
                else if (value is IParenthesizedOperation parenthesized)
                {
                    value = parenthesized.Operand;
                }
                else
                {
                    return value;
                }
            }
        }

        public static bool OverridesSystemObject(IMethodSymbol objectGetHashCode, IMethodSymbol method)
        {
            for (var current = method; current != null; current = current.OverriddenMethod)
            {
                if (objectGetHashCode.Equals(current))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
