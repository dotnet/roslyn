// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static class UseConditionalExpressionForReturnHelpers
    {
        public static bool TryMatchPattern(
            ISyntaxFactsService syntaxFacts,
            IConditionalOperation ifOperation,
            out IReturnOperation trueReturn,
            out IReturnOperation falseReturn)
        {
            trueReturn = null;
            falseReturn = null;

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

            if (falseStatement == null)
            {
                if (!(ifOperation.Parent is IBlockOperation parentBlock))
                {
                    return false;
                }

                var ifIndex = parentBlock.Operations.IndexOf(ifOperation);
                if (ifIndex < 0)
                {
                    return false;
                }

                if (ifIndex + 1 < parentBlock.Operations.Length)
                {
                    falseStatement = parentBlock.Operations[ifIndex + 1];
                    if (falseStatement.IsImplicit)
                    {
                        return false;
                    }
                }
            }

            trueStatement = UseConditionalExpressionHelpers.UnwrapSingleStatementBlock(trueStatement);
            falseStatement = UseConditionalExpressionHelpers.UnwrapSingleStatementBlock(falseStatement);

            // Both return-statements must be of the form "return value"
            if (!(trueStatement is IReturnOperation trueReturnOp) ||
                !(falseStatement is IReturnOperation falseReturnOp) ||
                trueReturnOp.ReturnedValue == null ||
                falseReturnOp.ReturnedValue == null)
            {
                return false;
            }

            if (trueReturnOp.Kind != falseReturnOp.Kind)
            {
                // Not allowed if these are different types of returns.  i.e.
                // "yield return ..." and "return ...".
                return false;
            }

            if (trueReturnOp.Kind == OperationKind.YieldBreak)
            {
                // This check is just paranoia.  We likely shouldn't get here since we already
                // checked if .ReturnedValue was null above.
                return false;
            }

            if (trueReturnOp.Kind == OperationKind.YieldReturn &&
                ifOperation.WhenFalse == null)
            {
                // we have the following:
                //
                //   if (...) {
                //       yield return ...
                //   }
                //
                //   yield return ...
                //
                // It is *not* correct to replace this with:
                //
                //      yield return ... ? ... ? ...
                //
                // as both yields need to be hit.
                return false;
            }

            trueReturn = trueReturnOp;
            falseReturn = falseReturnOp;

            return UseConditionalExpressionHelpers.CanConvert(
                syntaxFacts, ifOperation, trueReturn, falseReturn);
        }
    }
}
