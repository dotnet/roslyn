// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static class UseConditionalExpressionForAssignmentHelpers
    {
        public static bool TryMatchPattern(
            ISyntaxFacts syntaxFacts,
            IConditionalOperation ifOperation,
            [NotNullWhen(true)] out IOperation trueStatement,
            [NotNullWhen(true)] out IOperation falseStatement,
            out ISimpleAssignmentOperation? trueAssignment,
            out ISimpleAssignmentOperation? falseAssignment)
        {
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

            return UseConditionalExpressionHelpers.CanConvert(
                syntaxFacts, ifOperation, trueStatement, falseStatement);
        }

        private static bool TryGetAssignmentOrThrow(
            IOperation statement,
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
}
