// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This class implements the region control flow analysis operations. Region control flow
    /// analysis provides information about statements which enter and leave a region. The analysis
    /// is done lazily. When created, it performs no analysis, but simply caches the arguments.
    /// Then, the first time one of the analysis results is used it computes that one result and
    /// caches it. Each result is computed using a custom algorithm.
    /// </summary>
    internal class CSharpControlFlowAnalysis : ControlFlowAnalysis
    {
        private readonly RegionAnalysisContext _context;

        private ImmutableArray<SyntaxNode> _entryPoints;
        private ImmutableArray<SyntaxNode> _exitPoints;
        private object _regionStartPointIsReachable;
        private object _regionEndPointIsReachable;
        private bool? _succeeded;

        internal CSharpControlFlowAnalysis(RegionAnalysisContext context)
        {
            _context = context;
        }

        /// <summary>
        /// A collection of statements outside the region that jump into the region.
        /// </summary>
        public override ImmutableArray<SyntaxNode> EntryPoints
        {
            get
            {
                if (_entryPoints == null)
                {
                    _succeeded = !_context.Failed;
                    var result = _context.Failed ? ImmutableArray<SyntaxNode>.Empty :
                            ((IEnumerable<SyntaxNode>)EntryPointsWalker.Analyze(_context.Compilation, _context.Member, _context.BoundNode, _context.FirstInRegion, _context.LastInRegion, out _succeeded)).ToImmutableArray();
                    ImmutableInterlocked.InterlockedInitialize(ref _entryPoints, result);
                }

                return _entryPoints;
            }
        }

        /// <summary>
        /// A collection of statements inside the region that jump to locations outside the region.
        /// </summary>
        public override ImmutableArray<SyntaxNode> ExitPoints
        {
            get
            {
                if (_exitPoints == null)
                {
                    var result = Succeeded
                        ? ImmutableArray<SyntaxNode>.CastUp(ExitPointsWalker.Analyze(_context.Compilation, _context.Member, _context.BoundNode, _context.FirstInRegion, _context.LastInRegion))
                        : ImmutableArray<SyntaxNode>.Empty;
                    ImmutableInterlocked.InterlockedInitialize(ref _exitPoints, result);
                }

                return _exitPoints;
            }
        }

        /// <summary>
        /// Returns true if and only if the endpoint of the last statement in the region is reachable or the region contains no
        /// statements.
        /// </summary>
        public sealed override bool EndPointIsReachable
        {
            // To determine if the region completes normally, we just check if
            // its last statement completes normally.
            get
            {
                if (_regionEndPointIsReachable == null)
                {
                    ComputeReachability();
                }

                return (bool)_regionEndPointIsReachable;
            }
        }

        public sealed override bool StartPointIsReachable
        {
            // To determine if the region completes normally, we just check if
            // its last statement completes normally.
            get
            {
                if (_regionStartPointIsReachable == null)
                {
                    ComputeReachability();
                }

                return (bool)_regionStartPointIsReachable;
            }
        }

        private void ComputeReachability()
        {
            bool startIsReachable, endIsReachable;
            if (Succeeded)
            {
                RegionReachableWalker.Analyze(_context.Compilation, _context.Member, _context.BoundNode, _context.FirstInRegion, _context.LastInRegion, out startIsReachable, out endIsReachable);
            }
            else
            {
                startIsReachable = endIsReachable = true;
            }
            Interlocked.CompareExchange(ref _regionEndPointIsReachable, endIsReachable, null);
            Interlocked.CompareExchange(ref _regionStartPointIsReachable, startIsReachable, null);
        }

        /// <summary>
        /// A collection of return (or yield break) statements found within the region that return from the enclosing method or lambda.
        /// </summary>
        public override ImmutableArray<SyntaxNode> ReturnStatements
        {
            // Return statements out of the region are computed in precisely the same
            // way that jumps out of the region are computed.
            get
            {
                return ExitPoints.WhereAsArray(s => s.IsKind(SyntaxKind.ReturnStatement) || s.IsKind(SyntaxKind.YieldBreakStatement));
            }
        }

        /// <summary>
        /// Returns true if and only if analysis was successful.  Analysis can fail if the region does not properly span a single expression,
        /// a single statement, or a contiguous series of statements within the enclosing block.
        /// </summary>
        public sealed override bool Succeeded
        {
            get
            {
                if (_succeeded == null)
                {
                    var discarded = EntryPoints;
                }

                return _succeeded.Value;
            }
        }
    }
}
