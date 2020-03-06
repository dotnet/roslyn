// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.Extensions
{
    internal static class OperationBlockAnalysisContextExtension
    {
#pragma warning disable RS1012 // Start action has no registered actions.
        public static bool IsMethodNotImplementedOrSupported(this OperationBlockStartAnalysisContext context)
#pragma warning restore RS1012 // Start action has no registered actions.
        {
            // Note that VB method bodies with 1 action have 3 operations.
            // The first is the actual operation, the second is a label statement, and the third is a return
            // statement. The last two are implicit in these scenarios.

            var operationBlocks = context.OperationBlocks.WhereAsArray(operation => !operation.IsOperationNoneRoot());

            IBlockOperation? methodBlock = null;
            if (operationBlocks.Length == 1 && operationBlocks[0].Kind == OperationKind.Block)
            {
                methodBlock = (IBlockOperation)operationBlocks[0];
            }
            else if (operationBlocks.Length > 1)
            {
                foreach (var block in operationBlocks)
                {
                    if (block.Kind == OperationKind.Block)
                    {
                        methodBlock = (IBlockOperation)block;
                        break;
                    }
                }
            }

            if (methodBlock != null)
            {
                static bool IsSingleStatementBody(IBlockOperation body)
                {
                    return body.Operations.Length == 1 ||
                        (body.Operations.Length == 3 && body.Syntax.Language == LanguageNames.VisualBasic &&
                         body.Operations[1] is ILabeledOperation labeledOp && labeledOp.IsImplicit &&
                         body.Operations[2] is IReturnOperation returnOp && returnOp.IsImplicit);
                }

                if (IsSingleStatementBody(methodBlock))
                {
                    var innerOperation = methodBlock.Operations.First();

                    // Because of https://github.com/dotnet/roslyn/issues/23152, there can be an expression-statement
                    // wrapping expression-bodied throw operations. Compensate by unwrapping if necessary.
                    if (innerOperation.Kind == OperationKind.ExpressionStatement &&
                        innerOperation is IExpressionStatementOperation exprStatement)
                    {
                        innerOperation = exprStatement.Operation;
                    }

                    // Special case for methods with a return type and using arrowed expression where the throw-statement
                    // is wrapped.
                    // For example: public int GetSomething(int val) => throw new NotImplementedException();
                    if (innerOperation.Kind == OperationKind.Return &&
                        innerOperation is IReturnOperation @return &&
                        @return.ReturnedValue.Kind == OperationKind.Conversion &&
                        @return.ReturnedValue is IConversionOperation conversion &&
                        conversion.Operand.Kind == OperationKind.Throw)
                    {
                        innerOperation = conversion.Operand;
                    }

                    if (innerOperation.Kind == OperationKind.Throw &&
                        innerOperation is IThrowOperation throwOperation &&
                        throwOperation.GetThrownExceptionType() is ITypeSymbol createdExceptionType)
                    {
                        if (Equals(context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemNotImplementedException), createdExceptionType.OriginalDefinition)
                            || Equals(context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemNotSupportedException), createdExceptionType.OriginalDefinition))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}

#endif
