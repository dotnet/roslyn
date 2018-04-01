// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static class UseConditionalExpressionForAssignmentHelpers 
    {
        public static bool TryMatchPattern(
            IConditionalOperation ifOperation,
            out IVariableDeclarationGroupOperation localDeclarationStatement,
            out ISimpleAssignmentOperation trueAssignmentOperation,
            out ISimpleAssignmentOperation falseAssignmentOperation)
        {
            localDeclarationStatement = null;
            trueAssignmentOperation = null;
            falseAssignmentOperation = null;

            var parentBlock = ifOperation.Parent as IBlockOperation;
            if (parentBlock == null)
            {
                return false;
            }

            // var syntaxFacts = GetSyntaxFactsService();
            var ifIndex = parentBlock.Operations.IndexOf(ifOperation);
            if (ifIndex <= 0)
            {
                return false;
            }

            localDeclarationStatement = parentBlock.Operations[ifIndex - 1] as IVariableDeclarationGroupOperation;
            if (localDeclarationStatement == null)
            {
                return false;
            }

            if (localDeclarationStatement.Declarations.Length != 1)
            {
                return false;
            }

            var declarationOperation = localDeclarationStatement.Declarations[0];
            var declarators = declarationOperation.Declarators;
            if (declarators.Length != 1)
            {
                return false;
            }

            var declarator = declarators[0];
            var variable = declarator.Symbol;
            var variableName = variable.Name;

            var variableInitialier = declarator.Initializer;
            if (variableInitialier?.Value != null)
            {
                var unwrapped = UnwrapImplicitConversion(variableInitialier.Value);
                // the variable has to either not have an initializer, or it needs to be basic
                // literal/default expression.
                if (!(unwrapped is ILiteralOperation) &&
                    !(unwrapped is IDefaultValueOperation))
                {
                    return false;
                }
            }

            var trueStatement = ifOperation.WhenTrue;
            var falseStatement = ifOperation.WhenFalse;

            trueStatement = UnwrapSingleStatementBlock(trueStatement);
            falseStatement = UnwrapSingleStatementBlock(falseStatement);

            return TryGetAssignment(trueStatement, variable, out trueAssignmentOperation) &&
                   TryGetAssignment(falseStatement, variable, out falseAssignmentOperation);
        }

        private static bool TryGetAssignment(
            IOperation statement, ILocalSymbol variable, out ISimpleAssignmentOperation assignment)
        {
            // Both the WhenTrue and WhenFalse statements must be of the form:
            //      local = expr;
            if (!(statement is IExpressionStatementOperation exprStatement) ||
                !(exprStatement.Operation is ISimpleAssignmentOperation assignmentOp) ||
                !(assignmentOp.Target is ILocalReferenceOperation reference) ||
                !reference.Local.Equals(variable))
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

        private static IOperation UnwrapImplicitConversion(IOperation value)
            => value is IConversionOperation conversion && conversion.IsImplicit
                ? conversion.Operand
                : value;
    }
}
