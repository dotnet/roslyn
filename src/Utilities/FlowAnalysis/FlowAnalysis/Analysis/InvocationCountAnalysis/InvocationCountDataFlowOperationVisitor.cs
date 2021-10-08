// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.AnalyzerUtilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class InvocationCountDataFlowOperationVisitor : GlobalFlowStateDataFlowOperationVisitor
    {
        private readonly ImmutableArray<IMethodSymbol> _wellKnownLinqMethodCausingEnumeration;
        public InvocationCountDataFlowOperationVisitor(
            GlobalFlowStateAnalysisContext analysisContext,
            ImmutableArray<IMethodSymbol> wellKnownLinqMethodCausingEnumeration)
            : base(analysisContext, hasPredicatedGlobalState: false)
        {
            _wellKnownLinqMethodCausingEnumeration = wellKnownLinqMethodCausingEnumeration;
        }

        public override GlobalFlowStateAnalysisValueSet VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
            IMethodSymbol method,
            IOperation? visitedInstance,
            ImmutableArray<IArgumentOperation> visitedArguments,
            bool invokedAsDelegate,
            IOperation originalOperation,
            GlobalFlowStateAnalysisValueSet defaultValue)
        {
            var value = base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(method, visitedInstance, visitedArguments, invokedAsDelegate, originalOperation, defaultValue);

            if (_wellKnownLinqMethodCausingEnumeration.Contains(method)
                && !visitedArguments.IsEmpty
                && AnalysisEntityFactory.TryCreate(visitedArguments[0].Value, out var analysisEntity))
            {
                if (HasAbstractValue(analysisEntity))
                {
                    var existingAbstractValue = GetAbstractValue(analysisEntity);
                    var newAbstractValue = existingAbstractValue.WithAdditionalAnalysisValues(
                        GlobalFlowStateAnalysisValueSet.Create(new InvocationCountAbstractValue(originalOperation)), negate: false);
                    SetAbstractValue(analysisEntity, newAbstractValue);
                    return newAbstractValue;
                }
                else
                {
                    var newAbstractValue = GlobalFlowStateAnalysisValueSet.Create(new InvocationCountAbstractValue(originalOperation));
                    SetAbstractValue(analysisEntity, newAbstractValue);
                    return newAbstractValue;
                }
            }

            return value;
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