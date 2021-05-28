
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal partial class ControlFlowGraphBuilder
    {
        private sealed class FlowCaptureReferenceSearcher : OperationWalker
        {
            private readonly RegionBuilder _region;
            private readonly BasicBlockBuilder _block;
            private bool _found;

            private FlowCaptureReferenceSearcher(RegionBuilder region, BasicBlockBuilder block)
            {
                _region = region;
                this._block = block;
            }

            public static bool ContainsReferenceToCapturesEndingWithBlock(RegionBuilder region, BasicBlockBuilder block)
            {
                Debug.Assert(block.BranchValue != null);
                if (!KeepSearching(region, block)) return false;

                var flowCaptureReferenceSearcher = new FlowCaptureReferenceSearcher(region, block);
                flowCaptureReferenceSearcher.Visit(block.BranchValue);
                return flowCaptureReferenceSearcher._found;
            }

            private static bool KeepSearching([NotNullWhen(true)] RegionBuilder? region, BasicBlockBuilder block)
                => region?.LastBlock == block;

            public override void DefaultVisit(IOperation operation)
            {
                if (_found) return;
                base.DefaultVisit(operation);
            }

            public override void VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation)
            {
                RegionBuilder? current = _region;
                while (KeepSearching(current, _block))
                {
                    if (current.HasCaptureIds && current.CaptureIds.Contains(operation.Id))
                    {
                        _found = true;
                        return;
                    }

                    current = current.Enclosing;
                }
            }
        }
    }
}
