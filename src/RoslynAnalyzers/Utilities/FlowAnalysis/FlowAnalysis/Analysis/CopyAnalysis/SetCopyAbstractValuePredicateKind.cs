// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;

    public partial class CopyAnalysis : ForwardDataFlowAnalysis<CopyAnalysisData, CopyAnalysisContext, CopyAnalysisResult, CopyBlockAnalysisResult, CopyAbstractValue>
    {
        /// <summary>
        /// Predicate kind for <see cref="CopyDataFlowOperationVisitor.SetAbstractValue(CopyAnalysisData, AnalysisEntity, CopyAbstractValue, System.Func{AnalysisEntity, CopyAbstractValue}, SetCopyAbstractValuePredicateKind?, bool)"/>
        /// to indicate if the copy equality check for the SetAbstractValue call is a reference compare or a value compare operation.
        /// </summary>
        internal enum SetCopyAbstractValuePredicateKind
        {
            ValueCompare,
            ReferenceCompare
        }
    }
}
