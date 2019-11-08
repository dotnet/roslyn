// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Helper class to detect <see cref="IFlowCaptureOperation"/>s that are l-value captures.
    /// L-value captures are essentially captures of a symbol's location/address.
    /// Corresponding <see cref="IFlowCaptureReferenceOperation"/>s which share the same
    /// <see cref="CaptureId"/> as this flow capture, dereferences and writes to this location
    /// subsequently in the flow graph.
    /// For example, consider the below code:
    ///     a[i] = x ?? a[j];
    /// The control flow graph contains an initial flow capture of "a[i]" to capture the l-value
    /// of this array element:
    ///     FC0 (a[i])
    /// Then it evaluates the right hand side, which can have different
    /// values on different control flow paths, and the resultant value is then written
    /// to the captured location:
    ///     FCR0 = result
    /// </summary>
    /// <remarks>
    /// NOTE: This type is a workaround for https://github.com/dotnet/roslyn/issues/31007
    /// and it can be deleted once that feature is implemented.
    /// </remarks>
    internal static class LValueFlowCapturesProvider
    {
        public static ImmutableDictionary<CaptureId, FlowCaptureKind> CreateLValueFlowCaptures(ControlFlowGraph cfg)
        {
            // This method identifies flow capture reference operations that are target of an assignment
            // and marks them as lvalue flow captures.
            // Control flow graph can also contain flow captures
            // that are r-value captures at some point and l-values captures at other point in
            // the flow graph. Specifically, for an ICoalesceOperation a flow capture acts
            // as both an r-value and l-value flow capture.

            ImmutableDictionary<CaptureId, FlowCaptureKind>.Builder lvalueFlowCaptureIdBuilder = null;
            var rvalueFlowCaptureIds = PooledHashSet<CaptureId>.GetInstance();

            try
            {
                foreach (var flowCaptureReference in cfg.DescendantOperations<IFlowCaptureReferenceOperation>(OperationKind.FlowCaptureReference))
                {
                    if (flowCaptureReference is
                    {
                        Parent: IAssignmentOperation { Target: flowCaptureReference } assignment
                    }
||
                        flowCaptureReference.IsInLeftOfDeconstructionAssignment(out _))
                    {
                        lvalueFlowCaptureIdBuilder ??= ImmutableDictionary.CreateBuilder<CaptureId, FlowCaptureKind>();
                        var captureKind = flowCaptureReference.Parent.IsAnyCompoundAssignment() || rvalueFlowCaptureIds.Contains(flowCaptureReference.Id)
                            ? FlowCaptureKind.LValueAndRValueCapture
                            : FlowCaptureKind.LValueCapture;
                        lvalueFlowCaptureIdBuilder.Add(flowCaptureReference.Id, captureKind);
                    }
                    else
                    {
                        rvalueFlowCaptureIds.Add(flowCaptureReference.Id);
                    }
                }

#if DEBUG
                if (lvalueFlowCaptureIdBuilder != null)
                {
                    foreach (var kvp in lvalueFlowCaptureIdBuilder)
                    {
                        Debug.Assert(kvp.Value == FlowCaptureKind.LValueAndRValueCapture || !rvalueFlowCaptureIds.Contains(kvp.Key), "Flow capture used as both an r-value and an l-value, but with incorrect flow capture kind");
                    }
                }
#endif

                return lvalueFlowCaptureIdBuilder != null ? lvalueFlowCaptureIdBuilder.ToImmutable() : ImmutableDictionary<CaptureId, FlowCaptureKind>.Empty;
            }
            finally
            {
                rvalueFlowCaptureIds.Free();
            }
        }
    }
}
