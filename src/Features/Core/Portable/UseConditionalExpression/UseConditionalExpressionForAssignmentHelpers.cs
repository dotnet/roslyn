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

            if (!(trueStatement is IExpressionStatementOperation trueExprStatement) ||
                !(falseStatement is IExpressionStatementOperation falseExprStatement) ||
                !(trueExprStatement.Operation is ISimpleAssignmentOperation trueAssignment) ||
                !(falseExprStatement.Operation is ISimpleAssignmentOperation falseAssignment) ||
                !(trueAssignment.Target is ILocalReferenceOperation trueReference) ||
                !(falseAssignment.Target is ILocalReferenceOperation falseReference) ||
                !trueReference.Local.Equals(variable) ||
                !falseReference.Local.Equals(variable))
            {
                return false;
            }

            trueAssignmentOperation = trueAssignment;
            falseAssignmentOperation = falseAssignment;
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
