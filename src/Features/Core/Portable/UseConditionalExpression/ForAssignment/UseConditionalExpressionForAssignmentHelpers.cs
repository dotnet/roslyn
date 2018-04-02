// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static class UseConditionalExpressionForAssignmentHelpers 
    {
        public static bool TryMatchPattern(
            ISyntaxFactsService syntaxFacts,
            IConditionalOperation ifOperation,
            out ISimpleAssignmentOperation trueAssignment,
            out ISimpleAssignmentOperation falseAssignment)
        {
            trueAssignment = null;
            falseAssignment = null;

            var trueStatement = ifOperation.WhenTrue;
            var falseStatement = ifOperation.WhenFalse;

            trueStatement = UseConditionalExpressionHelpers.UnwrapSingleStatementBlock(trueStatement);
            falseStatement = UseConditionalExpressionHelpers.UnwrapSingleStatementBlock(falseStatement);

            if (!TryGetAssignment(trueStatement, out trueAssignment) ||
                !TryGetAssignment(falseStatement, out falseAssignment))
            {
                return false;
            }

            // The left side of both assignment statements has to be syntactically identical (modulo
            // trivia differences).
            return syntaxFacts.AreEquivalent(
                trueAssignment.Target.Syntax,
                falseAssignment.Target.Syntax);
        }

        private static bool TryGetAssignment(
            IOperation statement, out ISimpleAssignmentOperation assignment)
        {
            // Both the WhenTrue and WhenFalse statements must be of the form:
            //      target = value;
            if (!(statement is IExpressionStatementOperation exprStatement) ||
                !(exprStatement.Operation is ISimpleAssignmentOperation assignmentOp) ||
                assignmentOp.Target == null)
            {
                assignment = default;
                return false;
            }

            assignment = assignmentOp;
            return true;
        }
    }
}
