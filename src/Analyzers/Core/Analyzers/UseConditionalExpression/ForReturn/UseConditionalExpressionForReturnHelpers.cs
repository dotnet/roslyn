// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseConditionalExpression;

internal static class UseConditionalExpressionForReturnHelpers
{
    public static bool TryMatchPattern(
        ISyntaxFacts syntaxFacts,
        IConditionalOperation ifOperation,
        ISymbol containingSymbol,
        out bool isRef,
        [NotNullWhen(true)] out IOperation? trueStatement,
        [NotNullWhen(true)] out IOperation? falseStatement,
        out IReturnOperation? trueReturn,
        out IReturnOperation? falseReturn)
    {
        isRef = false;

        trueReturn = null;
        falseReturn = null;

        trueStatement = ifOperation.WhenTrue;
        falseStatement = ifOperation.WhenFalse;

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
            if (ifOperation.Parent is not IBlockOperation parentBlock)
                return false;

            var ifIndex = parentBlock.Operations.IndexOf(ifOperation);
            if (ifIndex < 0)
                return false;

            if (ifIndex + 1 >= parentBlock.Operations.Length)
                return false;

            falseStatement = parentBlock.Operations[ifIndex + 1];
            if (falseStatement.IsImplicit)
                return false;
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
        var trueThrow = trueStatement as IThrowOperation;
        var falseThrow = falseStatement as IThrowOperation;

        var anyReturn = trueReturn ?? falseReturn;
        if (UseConditionalExpressionHelpers.HasInconvertibleThrowStatement(
                syntaxFacts, anyReturn.GetRefKind(containingSymbol) != RefKind.None,
                trueThrow, falseThrow))
        {
            return false;
        }

        if (trueReturn != null &&
            falseReturn != null &&
            trueReturn.Kind != falseReturn.Kind)
        {
            // Not allowed if these are different types of returns.  i.e.
            // "yield return ..." and "return ...".
            return false;
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

        isRef = anyReturn.GetRefKind(containingSymbol) != RefKind.None;
        return UseConditionalExpressionHelpers.CanConvert(
            syntaxFacts, ifOperation, trueStatement, falseStatement);
    }

    private static bool IsReturnExprOrThrow(IOperation? statement)
    {
        // We can only convert a `throw expr` to a throw expression, not `throw;`
        if (statement is IThrowOperation throwOperation)
            return throwOperation.Exception != null;

        return statement is IReturnOperation returnOp && returnOp.ReturnedValue != null;
    }
}
