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
                // When results are combined, "Neutral" can be overridden, but "Failure" can't.
                // The result might remain neutral at the end, e.g. when all cases are `break;` alone,
                // in which case we cannot convert this to a switch expression.
                var analysisResult = new Analyzer().VisitSwitch(operation, unused: null);
                return !analysisResult.IsNeutral && !analysisResult.IsFailure;
            }

            public override AnalysisResult VisitSwitch(ISwitchOperation operation, object unused)
            {
                // Fail if the switch statement is empty or any of sections have more than one `case` label
                // Once we have "or" patterns, we can relax this to accept multi-case sections.
                if (operation.Cases.Length == 0 || operation.Cases.Any(@case => @case.Clauses.Length != 1))
                {
                    return AnalysisResult.Failure;
                }

                // Iterate over all sections and match section bodies to
                // see if they are all well-formed for a switch expression
                return Aggregate(operation.Cases, (result, @case) => AnalysisResult.Match(result, VisitList(@case.Body)));
            }

            private AnalysisResult VisitList(ImmutableArray<IOperation> operations)
            {
                // This is eaither a block or switch section body. Here we "combine" the result, since there could be
                // compatible statements like the ending `break;` or multiple simple assignments
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
                        // No point to continue if any operation is
                        // not well-formed for a switch expression 
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
                // Visit the target which could a local or field reference,
                // in which case we can assign the switch expression to it.
                return Visit(operation.Target, /*unused*/argument: null);
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
                return AnalysisResult.Failure;
            }

            public override AnalysisResult VisitBranch(IBranchOperation operation, object unused)
            {
                // Only `break` is allowed which gives Neutral result to be able to combine with other valid statements.
                return operation.BranchKind == BranchKind.Break ? AnalysisResult.Neutral : AnalysisResult.Failure;
            }

            public override AnalysisResult VisitExpressionStatement(IExpressionStatementOperation operation, object unused)
            {
                return Visit(operation.Operation, /*unused*/argument: null);
            }

            public override AnalysisResult VisitThrow(IThrowOperation operation, object unused)
            {
                // A "throw" statement can be converted to a throw expression.
                return operation.Exception is null ? AnalysisResult.Failure : AnalysisResult.Neutral;
            }

            public override AnalysisResult DefaultVisit(IOperation operation, object unused)
            {
                // In all other cases we return failure result.
                return AnalysisResult.Failure;
            }
        }
    }
}
