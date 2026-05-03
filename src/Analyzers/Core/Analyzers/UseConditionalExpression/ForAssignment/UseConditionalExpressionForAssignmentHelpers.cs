// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseConditionalExpression;

internal static class UseConditionalExpressionForAssignmentHelpers
{
    public static bool TryMatchPattern(
        ISyntaxFacts syntaxFacts,
        IConditionalOperation ifOperation,
        CancellationToken cancellationToken,
        out bool isRef,
        [NotNullWhen(true)] out IOperation trueStatement,
        [NotNullWhen(true)] out IOperation? falseStatement,
        out ISimpleAssignmentOperation? trueAssignment,
        out ISimpleAssignmentOperation? falseAssignment)
    {
        isRef = false;
        falseAssignment = null;

        trueStatement = ifOperation.WhenTrue;
        falseStatement = ifOperation.WhenFalse;

        trueStatement = UseConditionalExpressionHelpers.UnwrapSingleStatementBlock(trueStatement);
        falseStatement = UseConditionalExpressionHelpers.UnwrapSingleStatementBlock(falseStatement);

        if (!TryGetAssignmentOrThrow(trueStatement, out trueAssignment, out var trueThrow) ||
            !TryGetAssignmentOrThrow(falseStatement, out falseAssignment, out var falseThrow))
        {
            return false;
        }

        var anyAssignment = trueAssignment ?? falseAssignment;
        if (UseConditionalExpressionHelpers.HasInconvertibleThrowStatement(
                syntaxFacts, anyAssignment?.IsRef == true, trueThrow, falseThrow))
        {
            return false;
        }

        // The left side of both assignment statements has to be syntactically identical (modulo
        // trivia differences).
        if (trueAssignment != null && falseAssignment != null &&
            !syntaxFacts.AreEquivalent(trueAssignment.Target.Syntax, falseAssignment.Target.Syntax))
        {
            return false;
        }

        // If both assignments are discards type check is required.
        // Since discard can discard value of any type, after converting if statement to a conditional expression
        // it can produce compiler error if types are not the same and there is no implicit conversion between them, e.g.:
        // if (flag)
        // {
        //     _ = 5;
        // }
        // else
        // {
        //     _ = "";
        // }
        // This code can result in `_ = flag ? 5 : ""`, which immediately produces CS0173
        if (trueAssignment?.Target is IDiscardOperation &&
            falseAssignment?.Target is IDiscardOperation &&
            !AreEqualOrHaveImplicitConversion(trueAssignment.Type, falseAssignment.Type, trueAssignment.SemanticModel!.Compilation))
        {
            return false;
        }

        if (ReferencesDeclaredVariableInAssignment(ifOperation.Condition, trueAssignment?.Target, falseAssignment?.Target))
            return false;

        isRef = trueAssignment?.IsRef == true;
        if (!UseConditionalExpressionHelpers.CanConvert(
                syntaxFacts, ifOperation, trueStatement, falseStatement, cancellationToken))
        {
            return false;
        }

        // Can't convert `if (x != null) x.y = ...` into `x.y = x != null ? ... : ...` as the initial `x.y` reference
        // will happen first and can throw.
        foreach (var nullCheckedExpression in GetNullCheckedExpressions(ifOperation.Condition))
        {
            if (nullCheckedExpression is { Type.IsValueType: true })
                continue;

            if (ContainsReference(nullCheckedExpression.Syntax, trueAssignment?.Target.Syntax) ||
                ContainsReference(nullCheckedExpression.Syntax, falseAssignment?.Target.Syntax))
            {
                return false;
            }
        }

        return true;

        static bool AreEqualOrHaveImplicitConversion(ITypeSymbol? firstType, ITypeSymbol? secondType, Compilation compilation)
        {
            if (SymbolEqualityComparer.Default.Equals(firstType, secondType))
                return true;

            if (firstType is null || secondType is null)
                return false;

            return compilation.ClassifyCommonConversion(firstType, secondType).IsImplicit
                 ^ compilation.ClassifyCommonConversion(secondType, firstType).IsImplicit;
        }

        static bool ReferencesDeclaredVariableInAssignment(IOperation condition, IOperation? trueTarget, IOperation? falseTarget)
        {
            if (trueTarget is not null || falseTarget is not null)
            {
                using var _1 = PooledHashSet<ILocalSymbol>.GetInstance(out var symbolsDeclaredInConditional);
                foreach (var operation in condition.DescendantsAndSelf())
                {
                    // `if (x is String s)`
                    if (operation is IDeclarationPatternOperation { DeclaredSymbol: ILocalSymbol local })
                        symbolsDeclaredInConditional.AddIfNotNull(local);

                    // `if (Goo(out String s))`
                    if (operation is IDeclarationExpressionOperation { Expression: ILocalReferenceOperation localReference })
                        symbolsDeclaredInConditional.AddIfNotNull(localReference.Local);
                }

                if (symbolsDeclaredInConditional.Count > 0)
                {
                    return ContainsLocalReference(symbolsDeclaredInConditional, trueTarget) ||
                           ContainsLocalReference(symbolsDeclaredInConditional, falseTarget);
                }
            }

            return false;
        }

        static bool ContainsLocalReference(HashSet<ILocalSymbol> declaredPatternSymbols, IOperation? target)
        {
            if (target is not null)
            {
                foreach (var operation in target.DescendantsAndSelf())
                {
                    if (operation is ILocalReferenceOperation { Local: var local } &&
                        declaredPatternSymbols.Contains(local))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static IEnumerable<IOperation> GetNullCheckedExpressions(IOperation operation)
        {
            foreach (var current in operation.DescendantsAndSelf())
            {
                // x != null  is a null check of x
                if (current is IBinaryOperation { OperatorKind: BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals } binaryOperation)
                {
                    if (binaryOperation.LeftOperand.ConstantValue is { HasValue: true, Value: null })
                    {
                        yield return binaryOperation.RightOperand;
                    }
                    else if (binaryOperation.RightOperand.ConstantValue is { HasValue: true, Value: null })
                    {
                        yield return binaryOperation.LeftOperand;
                    }
                }
                else if (current is IIsPatternOperation isPatternOperation)
                {
                    // x is Y y    is a null check of x
                    yield return isPatternOperation.Value;
                }
                else if (current is IIsTypeOperation isTypeOperation)
                {
                    // x is Y    is a null check of x
                    yield return isTypeOperation.ValueOperand;
                }
            }

            yield break;
        }

        bool ContainsReference(SyntaxNode nullCheckedExpression, SyntaxNode? within)
            => within?.DescendantNodes().Any(n => syntaxFacts.AreEquivalent(n, nullCheckedExpression)) is true;
    }

    private static bool TryGetAssignmentOrThrow(
        [NotNullWhen(true)] IOperation? statement,
        out ISimpleAssignmentOperation? assignment,
        out IThrowOperation? throwOperation)
    {
        assignment = null;
        throwOperation = null;

        if (statement is IThrowOperation throwOp)
        {
            throwOperation = throwOp;

            // We can only convert a `throw expr` to a throw expression, not `throw;`
            return throwOperation.Exception != null;
        }

        // Both the WhenTrue and WhenFalse statements must be of the form:
        //      target = value;
        if (statement is IExpressionStatementOperation exprStatement)
        {
            if (exprStatement.Operation is ISimpleAssignmentOperation { Target: not null } assignmentOp1)
            {
                assignment = assignmentOp1;
                return true;
            }

            if (exprStatement.Operation is IConditionalAccessOperation { WhenNotNull: ISimpleAssignmentOperation assignmentOp2 })
            {
                assignment = assignmentOp2;
                return true;
            }
        }

        return false;
    }
}
