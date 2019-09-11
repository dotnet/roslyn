// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;

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
            var rvalueFlowCaptureIds = new Dictionary<CaptureId, HashSet<IFlowCaptureReferenceOperation>>();
#endif
            foreach (var flowCaptureReference in cfg.DescendantOperations<IFlowCaptureReferenceOperation>(OperationKind.FlowCaptureReference))
            {
                if (flowCaptureReference.IsLValueFlowCaptureReference())
                {
                    lvalueFlowCaptureIdBuilder.Add(flowCaptureReference.Id);
                }
#if DEBUG
                else
                {
                    if (!rvalueFlowCaptureIds.TryGetValue(flowCaptureReference.Id, out var operations))
                    {
                        operations = new HashSet<IFlowCaptureReferenceOperation>();
                        rvalueFlowCaptureIds[flowCaptureReference.Id] = operations;
                    }

                    operations.Add(flowCaptureReference);
                }
#endif
            }

#if DEBUG
            if (lvalueFlowCaptureIdBuilder.Count > 0)
            {
                foreach (var captureId in lvalueFlowCaptureIdBuilder)
                {
                    if (rvalueFlowCaptureIds.ContainsKey(captureId))
                    {
                        // Flow capture reference is used on left side as well as right side for
                        // CFG generated for coalesce assignment operation ('??=')
                        // Do not fire an assert for this known anomaly.
                        var operations = rvalueFlowCaptureIds[captureId];
                        if (operations.Count == 1 &&
                            operations.Single().Parent?.Kind == OperationKind.FlowCapture)
                        {
                            continue;
                        }

                        Debug.Fail("Flow capture used as both an r-value and an l-value?");
                    }
                }
            }
#endif

            return lvalueFlowCaptureIdBuilder.ToImmutableAndFree();
        }
    }
}
