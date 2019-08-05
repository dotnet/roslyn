// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Helper class to detect <see cref="IFlowCaptureOperation"/>s that are l-value captures.
    /// </summary>
    internal static class LValueFlowCapturesProvider
    {
        private static readonly ConditionalWeakTable<ControlFlowGraph, ImmutableHashSet<CaptureId>> s_lValueFlowCapturesCache =
            new ConditionalWeakTable<ControlFlowGraph, ImmutableHashSet<CaptureId>>();

        public static ImmutableHashSet<CaptureId> GetOrCreateLValueFlowCaptures(ControlFlowGraph cfg)
            => s_lValueFlowCapturesCache.GetValue(cfg, CreateLValueFlowCaptures);

        private static ImmutableHashSet<CaptureId> CreateLValueFlowCaptures(ControlFlowGraph cfg)
        {
            var lvalueFlowCaptureIdBuilder = PooledHashSet<CaptureId>.GetInstance();
#if DEBUG
            var rvalueFlowCaptureIds = new HashSet<CaptureId>();
#endif
            foreach (var flowCaptureReference in cfg.DescendantOperations<IFlowCaptureReferenceOperation>(OperationKind.FlowCaptureReference))
            {
                if (flowCaptureReference.Parent is IAssignmentOperation assignment &&
                    assignment.Target == flowCaptureReference)
                {
                    lvalueFlowCaptureIdBuilder.Add(flowCaptureReference.Id);
                }
#if DEBUG
                else
                {
                    rvalueFlowCaptureIds.Add(flowCaptureReference.Id);
                }
#endif
            }

#if DEBUG
            if (lvalueFlowCaptureIdBuilder.Count > 0)
            {
                foreach (var captureId in lvalueFlowCaptureIdBuilder)
                {
                    Debug.Assert(!rvalueFlowCaptureIds.Contains(captureId), "Flow capture used as both an r-value and an l-value?");
                }
            }
#endif

            return lvalueFlowCaptureIdBuilder.ToImmutableAndFree();
        }
    }
}
