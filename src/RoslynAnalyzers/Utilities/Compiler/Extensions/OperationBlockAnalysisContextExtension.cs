//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//#if HAS_IOPERATION

//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.Diagnostics;
//using Microsoft.CodeAnalysis.Operations;

//namespace Analyzer.Utilities.Extensions
//{
//    internal static class OperationBlockAnalysisContextExtension
//    {
//#pragma warning disable RS1012 // Start action has no registered actions.
//        public static bool IsMethodNotImplementedOrSupported(this OperationBlockStartAnalysisContext context, bool checkPlatformNotSupported = false)
//#pragma warning restore RS1012 // Start action has no registered actions.
//        {
//            // Note that VB method bodies with 1 action have 3 operations.
//            // The first is the actual operation, the second is a label statement, and the third is a return
//            // statement. The last two are implicit in these scenarios.

//            var operationBlocks = context.OperationBlocks.WhereAsArray(operation => !operation.IsOperationNoneRoot());

//            IBlockOperation? methodBlock = null;
//            if (operationBlocks.Length == 1 && operationBlocks[0].Kind == OperationKind.Block)
//            {
//                methodBlock = (IBlockOperation)operationBlocks[0];
//            }
//            else if (operationBlocks.Length > 1)
//            {
//                foreach (var block in operationBlocks)
//                {
//                    if (block.Kind == OperationKind.Block)
//                    {
//                        methodBlock = (IBlockOperation)block;
//                        break;
//                    }
//                }
//            }

//            if (methodBlock != null)
//            {
//                static bool IsSingleStatementBody(IBlockOperation body)
//                {
//                    return body.Operations.Length == 1 ||
//                        (body.Operations.Length == 3 && body.Syntax.Language == LanguageNames.VisualBasic &&
//                         body.Operations[1] is ILabeledOperation labeledOp && labeledOp.IsImplicit &&
//                         body.Operations[2] is IReturnOperation returnOp && returnOp.IsImplicit);
//                }

//                if (IsSingleStatementBody(methodBlock) &&
//                    methodBlock.Operations[0].GetTopmostExplicitDescendants() is { } descendants &&
//                    descendants.Length == 1 &&
//                    descendants[0] is IThrowOperation throwOperation &&
//                    throwOperation.GetThrownExceptionType() is ITypeSymbol createdExceptionType)
//                {
//                    if (SymbolEqualityComparer.Default.Equals(
//                            context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemNotImplementedException),
//                            createdExceptionType.OriginalDefinition) ||
//                        SymbolEqualityComparer.Default.Equals(
//                            context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemNotSupportedException),
//                            createdExceptionType.OriginalDefinition) ||
//                        (checkPlatformNotSupported &&
//                        SymbolEqualityComparer.Default.Equals(
//                            context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemPlatformNotSupportedException),
//                            createdExceptionType.OriginalDefinition)))
//                    {
//                        return true;
//                    }
//                }
//            }

//            return false;
//        }
//    }
//}

//#endif
