// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;

namespace Microsoft.CodeAnalysis.AnalyzerUtilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class InvocationCountDataFlowOperationVisitor : GlobalFlowStateDataFlowOperationVisitor
    {
        public InvocationCountDataFlowOperationVisitor(GlobalFlowStateAnalysisContext analysisContext)
            : base(analysisContext, hasPredicatedGlobalState: true)
        {
        }


        // public override InvocationCountAbstractValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
        //     IMethodSymbol method,
        //     IOperation? visitedInstance,
        //     ImmutableArray<IArgumentOperation> visitedArguments,
        //     bool invokedAsDelegate,
        //     IOperation originalOperation,
        //     InvocationCountAbstractValue defaultValue)
        // {
        //     // TODO: Instead of hard code, this should be changed to a function.
        //     if (InvocationCountAnalysisHelper.CauseEnumeration(originalOperation))
        //     {
        //         // if (visitedInstance == null
        //         //     && method.IsExtensionMethod
        //         //     && !invocationOperation.Arguments.IsEmpty
        //         //     && AnalysisEntityFactory.TryCreate(invocationOperation.Arguments[0], out var analysisEntity))
        //         // {
        //         //     var existingValue = GetAbstractValue(analysisEntity);
        //         //     var newValue = InvocationCountValueDomain.Instance.Merge(existingValue, InvocationCountAbstractValue.OneTime);
        //         //     SetAbstractValue(analysisEntity, newValue);
        //         //     return newValue;
        //         // }
        //     }
        //
        //     return base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
        //         method,
        //         visitedInstance,
        //         visitedArguments,
        //         invokedAsDelegate,
        //         originalOperation,
        //         defaultValue);
        // }
    }
}