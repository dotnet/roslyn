// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    internal sealed partial class ConvertSwitchStatementToExpressionDiagnosticAnalyzer
    {
        private sealed class Analyzer : OperationVisitor<object, AnalysisResult>
        {
            private Analyzer()
            {
            }

            public static bool CanConvertToSwitchExpression(ISwitchOperation operation)
            {
                var analysisResult = new Analyzer().VisitSwitch(operation, unused: null);
                return !analysisResult.IsNeutral && !analysisResult.IsFailure;
            }

            public override AnalysisResult VisitSwitch(ISwitchOperation operation, object unused)
            {
                if (operation.Cases.Length == 0 || operation.Cases.Any(@case => @case.Clauses.Length != 1))
                {
                    return AnalysisResult.Failure;
                }

                return Aggregate(operation.Cases, (result, @case) => AnalysisResult.Match(result, VisitList(@case.Body)));
            }

            private AnalysisResult VisitList(ImmutableArray<IOperation> operations)
            {
                return Aggregate(operations, (result, operation) => AnalysisResult.Combine(result, Visit(operation, /*unused*/argument: null)));
            }

            private static AnalysisResult Aggregate<T>(ImmutableArray<T> operations, Func<AnalysisResult, T, AnalysisResult> func)
            {
                var result = AnalysisResult.Neutral;
                foreach (var operation in operations)
                {
                    result = func(result, operation);
                    if (result.IsFailure)
                    {
                        break;
                    }
                }

                return result;
            }

            public override AnalysisResult VisitReturn(IReturnOperation operation, object unused)
            {
                return AnalysisResult.Return;
            }

            public override AnalysisResult VisitSimpleAssignment(ISimpleAssignmentOperation operation, object unused)
            {
                return Visit(operation.Target, argument: null);
            }

            public override AnalysisResult VisitLocalReference(ILocalReferenceOperation operation, object unused)
            {
                return AnalysisResult.Assignment(operation.Local);
            }

            public override AnalysisResult VisitFieldReference(IFieldReferenceOperation operation, object unused)
            {
                return AnalysisResult.Assignment(operation.Field);
            }

            public override AnalysisResult VisitBlock(IBlockOperation operation, object unused)
            {
                return VisitList(operation.Operations);
            }

            public override AnalysisResult VisitBranch(IBranchOperation operation, object unused)
            {
                return operation.BranchKind == BranchKind.Break ? AnalysisResult.Neutral : AnalysisResult.Failure;
            }

            public override AnalysisResult VisitExpressionStatement(IExpressionStatementOperation operation, object unused)
            {
                return Visit(operation.Operation, argument: null);
            }

            public override AnalysisResult VisitThrow(IThrowOperation operation, object unused)
            {
                return AnalysisResult.Neutral;
            }

            public override AnalysisResult DefaultVisit(IOperation operation, object unused)
            {
                return AnalysisResult.Failure;
            }
        }
    }
}
