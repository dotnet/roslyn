// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A region analysis walker that computes whether or not the region completes normally.  It does this by determining
    /// if the point at which the region ends is reachable.
    /// </summary>
    internal class RegionReachableWalker : AbstractRegionControlFlowPass
    {
        internal static void Analyze(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion,
            out bool startPointIsReachable, out bool endPointIsReachable)
        {
            var walker = new RegionReachableWalker(compilation, member, node, firstInRegion, lastInRegion);
            var diagnostics = DiagnosticBag.GetInstance();
            bool badRegion = false;
            try
            {
                walker.Analyze(ref badRegion, diagnostics);
                startPointIsReachable = badRegion || walker.reginStartPointIsReachable.GetValueOrDefault(true);
                endPointIsReachable = badRegion || walker.reginEndPointIsReachable.GetValueOrDefault(walker.State.Alive);
            }
            finally
            {
                diagnostics.Free();
                walker.Free();
            }
        }

        private bool? reginStartPointIsReachable;
        private bool? reginEndPointIsReachable;

        private RegionReachableWalker(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion)
            : base(compilation, member, node, firstInRegion, lastInRegion)
        {
        }

        protected override void EnterRegion()
        {
            reginStartPointIsReachable = this.State.Alive;
            base.EnterRegion();
        }

        override protected void LeaveRegion()
        {
            reginEndPointIsReachable = this.State.Alive;
            base.LeaveRegion();
        }
    }
}