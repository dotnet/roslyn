// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static class UseConditionalExpressionForReturnHelpers 
    {
        public static bool TryMatchPattern(
            IConditionalOperation ifOperation)
        {
            //localDeclarationStatement = null;
            //trueAssignmentOperation = null;
            //falseAssignmentOperation = null;

            var parentBlock = ifOperation.Parent as IBlockOperation;
            if (parentBlock == null)
            {
                return false;
            }

            var ifIndex = parentBlock.Operations.IndexOf(ifOperation);
            if (ifIndex <= 0)
            {
                return false;
            }            

            var trueStatement = ifOperation.WhenTrue;
            var falseStatement = ifOperation.WhenFalse;

            // we support:
            //
            //      if (expr)
            //          return a;
            //      else
            //          return b;
            //
            // and
            //
            //      if (expr)
            //          return a;
            //      
            //      return b;

            if (falseStatement == null && ifIndex + 1 < parentBlock.Operations.Length)
            {
                falseStatement = parentBlock.Operations[ifIndex + 1];
                if (falseStatement.IsImplicit)
                {
                    return false;
                }
            }

            trueStatement = UnwrapSingleStatementBlock(trueStatement);
            falseStatement = UnwrapSingleStatementBlock(falseStatement);

            if (!(trueStatement is IReturnOperation trueReturn) ||
                !(falseStatement is IReturnOperation falseReturn) ||
                trueReturn.ReturnedValue == null ||
                falseReturn.ReturnedValue == null)
            {
                return false;
            }

            return true;
        }

        private static IOperation UnwrapSingleStatementBlock(IOperation statement)
            => statement is IBlockOperation block && block.Operations.Length == 1
                ? block.Operations[0]
                : statement;
    }
}
