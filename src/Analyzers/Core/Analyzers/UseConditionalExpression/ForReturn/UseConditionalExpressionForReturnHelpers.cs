// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static class UseConditionalExpressionForReturnHelpers
    {
        public static bool TryMatchPattern(
            ISyntaxFacts syntaxFacts,
            IConditionalOperation ifOperation,
            Func<IReturnOperation?, bool> returnIsRef,
            out IReturnOperation? trueReturn,
            out IThrowOperation? trueThrow,
            out IReturnOperation? falseReturn,
            out IThrowOperation? falseThrow)
        {
            trueReturn = null;
            trueThrow = null;
            falseReturn = null;
            falseThrow = null;

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
            //
            // note: either (but not both) of these statements can be throw-statements.

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
            if (!IsReturnExprOrThrow(trueStatement) ||
                !IsReturnExprOrThrow(falseStatement))
            {
                return false;
            }

            trueReturn = trueStatement as IReturnOperation;
            falseReturn = falseStatement as IReturnOperation;
            trueThrow = trueStatement as IThrowOperation;
            falseThrow = falseStatement as IThrowOperation;

            // Can't convert to `x ? throw ... : throw ...` as there's no best common type between the two (even when
            // throwing the same exception type).
            if (trueThrow != null && falseThrow != null)
                return false;

            var anyReturn = trueReturn ?? falseReturn;
            var anyThrow = trueThrow ?? falseThrow;

            if (anyThrow != null)
            {
                // can only convert to a conditional expression if the lang supports throw-exprs.
                if (!syntaxFacts.SupportsThrowExpression(ifOperation.Syntax.SyntaxTree.Options))
                    return false;

                // `ref` can't be used with `throw`.
                if (returnIsRef(anyReturn))
                    return false;
            }

            if (trueReturn != null && falseReturn != null)
            {
                if (trueReturn.Kind != falseReturn.Kind)
                {
                    // Not allowed if these are different types of returns.  i.e.
                    // "yield return ..." and "return ...".
                    return false;
                }
            }

            if (trueReturn?.Kind == OperationKind.YieldBreak)
            {
                // This check is just paranoia.  We likely shouldn't get here since we already
                // checked if .ReturnedValue was null above.
                return false;
            }

            if (trueReturn?.Kind == OperationKind.YieldReturn &&
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

            return UseConditionalExpressionHelpers.CanConvert(
                syntaxFacts, ifOperation, trueStatement, falseStatement);
        }

        private static bool IsReturnExprOrThrow(IOperation statement)
        {
            // We can only convert a `throw expr` to a throw expression, not `throw;`
            if (statement is IThrowOperation throwOperation)
                return throwOperation.Exception != null;

            return statement is IReturnOperation returnOp && returnOp.ReturnedValue != null;
        }
    }
}
