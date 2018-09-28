// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis
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
            ImmutableHashSet<CaptureId>.Builder lvalueFlowCaptureIdBuilder = null;
#if DEBUG
            var rvalueFlowCaptureIds = new HashSet<CaptureId>();
#endif
            foreach (var flowCaptureReference in cfg.DescendantOperations<IFlowCaptureReferenceOperation>(OperationKind.FlowCaptureReference))
            {
                if (flowCaptureReference.Parent is IAssignmentOperation assignment &&
                    assignment.Target == flowCaptureReference ||
                    flowCaptureReference.IsInLeftOfDeconstructionAssignment(out _))
                {
                    lvalueFlowCaptureIdBuilder = lvalueFlowCaptureIdBuilder ?? ImmutableHashSet.CreateBuilder<CaptureId>();
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
            if (lvalueFlowCaptureIdBuilder != null)
            {
                foreach (var captureId in lvalueFlowCaptureIdBuilder)
                {
                    Debug.Assert(!rvalueFlowCaptureIds.Contains(captureId), "Flow capture used as both an r-value and an l-value?");
                }
            }
#endif

            return lvalueFlowCaptureIdBuilder != null ? lvalueFlowCaptureIdBuilder.ToImmutable() : ImmutableHashSet<CaptureId>.Empty;
        }
    }
}
