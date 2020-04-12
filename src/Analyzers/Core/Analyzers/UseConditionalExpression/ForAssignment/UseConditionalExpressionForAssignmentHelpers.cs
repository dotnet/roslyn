// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static class UseConditionalExpressionForAssignmentHelpers
    {
        public static bool TryMatchPattern(
            ISyntaxFacts syntaxFacts,
            IConditionalOperation ifOperation,
            out ISimpleAssignmentOperation? trueAssignment,
            out IThrowOperation? trueThrow,
            out ISimpleAssignmentOperation? falseAssignment,
            out IThrowOperation? falseThrow)
        {
            trueAssignment = null;
            trueThrow = null;
            falseAssignment = null;
            falseThrow = null;

            var trueStatement = ifOperation.WhenTrue;
            var falseStatement = ifOperation.WhenFalse;

            trueStatement = UseConditionalExpressionHelpers.UnwrapSingleStatementBlock(trueStatement);
            falseStatement = UseConditionalExpressionHelpers.UnwrapSingleStatementBlock(falseStatement);

            if (!TryGetAssignmentOrThrow(trueStatement, out trueAssignment, out trueThrow) ||
                !TryGetAssignmentOrThrow(falseStatement, out falseAssignment, out falseThrow))
            {
                return false;
            }

            // Can't convert to `x ? throw ... : throw ...` as there's no best common type between the two (even when
            // throwing the same exception type).
            if (trueThrow != null && falseThrow != null)
                return false;

            if (trueThrow != null || falseThrow != null)
            {
                if (!syntaxFacts.SupportsThrowExpression(ifOperation.Syntax.SyntaxTree.Options))
                    return false;
            }

            // `ref` can't be used with `throw`.
            var isRef = trueAssignment?.IsRef == true || falseAssignment?.IsRef == true;
            if (isRef && (trueThrow != null || falseThrow != null))
                return false;

            // The left side of both assignment statements has to be syntactically identical (modulo
            // trivia differences).
            if (trueAssignment != null && falseAssignment != null &&
                !syntaxFacts.AreEquivalent(trueAssignment.Target.Syntax, falseAssignment.Target.Syntax))
            {
                return false;
            }

            return UseConditionalExpressionHelpers.CanConvert(
                syntaxFacts, ifOperation,
                (IOperation?)trueAssignment ?? trueThrow,
                (IOperation?)falseAssignment ?? falseThrow);
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
