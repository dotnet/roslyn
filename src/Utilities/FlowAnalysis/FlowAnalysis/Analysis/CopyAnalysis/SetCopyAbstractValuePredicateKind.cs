// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;

    public partial class CopyAnalysis : ForwardDataFlowAnalysis<CopyAnalysisData, CopyAnalysisContext, CopyAnalysisResult, CopyBlockAnalysisResult, CopyAbstractValue>
    {
        /// <summary>
        /// Predicate kind for <see cref="CopyDataFlowOperationVisitor.SetAbstractValue(CopyAnalysisData, AnalysisEntity, CopyAbstractValue, System.Func{AnalysisEntity, CopyAbstractValue}, SetCopyAbstractValuePredicateKind?, bool)"/>
        /// to indicte if the copy equality check for the SetAbstractValue call is a reference compare or a value compare operation.
        /// </summary>
        internal enum SetCopyAbstractValuePredicateKind
        {
            ValueCompare,
            ReferenceCompare
        }
    }
}
