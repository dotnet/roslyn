// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;

#if DEBUG
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#endif

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Helper class to detect <see cref="IFlowCaptureOperation"/>s that are l-value captures.
    /// </summary>
    internal static class LValueFlowCapturesProvider
    {
        private static readonly ConditionalWeakTable<ControlFlowGraph, ImmutableHashSet<CaptureId>> s_lValueFlowCapturesCache = new();

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
                        operations = [];
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
                    if (rvalueFlowCaptureIds.TryGetValue(captureId, out var operations))
                    {
                        // Flow capture reference is used on left side as well as right side for
                        // CFG generated for coalesce assignment operation ('??=')
                        // Do not fire an assert for this known anomaly.
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
