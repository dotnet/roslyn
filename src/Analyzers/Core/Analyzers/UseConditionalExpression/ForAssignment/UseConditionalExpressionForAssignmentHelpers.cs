// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseConditionalExpression;

internal static class UseConditionalExpressionForAssignmentHelpers
{
    public static bool TryMatchPattern(
        ISyntaxFacts syntaxFacts,
        IConditionalOperation ifOperation,
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
        return UseConditionalExpressionHelpers.CanConvert(
            syntaxFacts, ifOperation, trueStatement, falseStatement);

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
        if (statement is IExpressionStatementOperation exprStatement &&
            exprStatement.Operation is ISimpleAssignmentOperation assignmentOp &&
            assignmentOp.Target != null)
        {
            assignment = assignmentOp;
            return true;
        }

        return false;
    }
}
