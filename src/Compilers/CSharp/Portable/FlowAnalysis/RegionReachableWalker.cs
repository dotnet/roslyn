// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
                startPointIsReachable = badRegion || walker._regionStartPointIsReachable.GetValueOrDefault(true);
                endPointIsReachable = badRegion || walker._regionEndPointIsReachable.GetValueOrDefault(walker.State.Alive);
            }
            finally
            {
                diagnostics.Free();
                walker.Free();
            }
        }

        private bool? _regionStartPointIsReachable;
        private bool? _regionEndPointIsReachable;

        private RegionReachableWalker(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion)
            : base(compilation, member, node, firstInRegion, lastInRegion)
        {
        }

        protected override void EnterRegion()
        {
            _regionStartPointIsReachable = this.State.Alive;
            base.EnterRegion();
        }

        protected override void LeaveRegion()
        {
            _regionEndPointIsReachable = this.State.Alive;
            base.LeaveRegion();
        }
    }
}
