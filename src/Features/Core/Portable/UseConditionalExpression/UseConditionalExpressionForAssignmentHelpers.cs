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

            var parentBlock = ifOperation.Parent as IBlockOperation;
            if (parentBlock == null)
            {
                return false;
            }

            // var syntaxFacts = GetSyntaxFactsService();
            var ifIndex = parentBlock.Operations.IndexOf(ifOperation);
            if (ifIndex < 0)
            {
                return false;
            }

            var trueStatement = ifOperation.WhenTrue;
            var falseStatement = ifOperation.WhenFalse;

            trueStatement = UnwrapSingleStatementBlock(trueStatement);
            falseStatement = UnwrapSingleStatementBlock(falseStatement);

            if (!TryGetAssignment(trueStatement, out trueAssignment) ||
                !TryGetAssignment(falseStatement, out falseAssignment))
            {
                return false;
            }

            return syntaxFacts.AreEquivalent(
                trueAssignment.Target.Syntax,
                falseAssignment.Target.Syntax);
        }

        private static bool TryGetAssignment(
            IOperation statement, out ISimpleAssignmentOperation assignment)
        {
            // Both the WhenTrue and WhenFalse statements must be of the form:
            //      local = expr;
            if (!(statement is IExpressionStatementOperation exprStatement) ||
                !(exprStatement.Operation is ISimpleAssignmentOperation assignmentOp) ||
                assignmentOp.Target == null ||
                assignmentOp.Target.Type == null)
            {
                assignment = default;
                return false;
            }

            assignment = assignmentOp;
            return true;
        }

        private static IOperation UnwrapSingleStatementBlock(IOperation statement)
            => statement is IBlockOperation block && block.Operations.Length == 1
                ? block.Operations[0]
                : statement;
    }
}
