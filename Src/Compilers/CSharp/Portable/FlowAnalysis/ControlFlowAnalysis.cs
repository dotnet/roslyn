// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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
        private readonly RegionAnalysisContext context;

        private IEnumerable<SyntaxNode> entryPoints;
        private IEnumerable<SyntaxNode> exitPoints;
        private object regionStartPointIsReachable;
        private object regionEndPointIsReachable;
        private bool? succeeded = null;

        internal CSharpControlFlowAnalysis(RegionAnalysisContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// A collection of statements outside the region that jump into the region.
        /// </summary>
        public override IEnumerable<SyntaxNode> EntryPoints
        {
            get
            {
                if (entryPoints == null)
                {
                    this.succeeded = !context.Failed;
                    var result = context.Failed ? Enumerable.Empty<SyntaxNode>() :
                            EntryPointsWalker.Analyze(context.Compilation, context.Member, context.BoundNode, context.FirstInRegion, context.LastInRegion, out this.succeeded);
                    Interlocked.CompareExchange(ref entryPoints, result, null);
                }

                return entryPoints;
            }
        }

        /// <summary>
        /// A collection of statements inside the region that jump to locations outside the region.
        /// </summary>
        public override IEnumerable<SyntaxNode> ExitPoints
        {
            get
            {
                if (exitPoints == null)
                {
                    var result = Succeeded
                        ? ExitPointsWalker.Analyze(context.Compilation, context.Member, context.BoundNode, context.FirstInRegion, context.LastInRegion)
                        : Enumerable.Empty<SyntaxNode>();
                    Interlocked.CompareExchange(ref exitPoints, result, null);
                }

                return exitPoints;
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
                if (regionEndPointIsReachable == null)
                {
                    ComputeReachability();
                }

                return (bool)regionEndPointIsReachable;
            }
        }

        public sealed override bool StartPointIsReachable
        {
            // To determine if the region completes normally, we just check if
            // its last statement completes normally.
            get
            {
                if (regionStartPointIsReachable == null)
                {
                    ComputeReachability();
                }

                return (bool)regionStartPointIsReachable;
            }
        }

        private void ComputeReachability()
        {
            bool startIsReachable, endIsReachable;
            if (Succeeded)
            {
                RegionReachableWalker.Analyze(context.Compilation, context.Member, context.BoundNode, context.FirstInRegion, context.LastInRegion, out startIsReachable, out endIsReachable);
            }
            else
            {
                startIsReachable = endIsReachable = true;
            }
            Interlocked.CompareExchange(ref regionEndPointIsReachable, endIsReachable, null);
            Interlocked.CompareExchange(ref regionStartPointIsReachable, startIsReachable, null);
        }

        /// <summary>
        /// A collection of return (or yield break) statements found within the region that return from the enclosing method or lambda.
        /// </summary>
        // [Obsolete("The return statements in a region are now included in the result of ExitPoints.", false)]
        public override IEnumerable<SyntaxNode> ReturnStatements
        {
            // Return statements out of the region are computed in precisely the same
            // way that jumps out of the region are computed.
            get
            {
                return ExitPoints.Where(s => s.IsKind(SyntaxKind.ReturnStatement) || s.IsKind(SyntaxKind.YieldBreakStatement));
            }
        }

        /// <summary>
        /// Returns true iff analysis was successful.  Analysis can fail if the region does not properly span a single expression,
        /// a single statement, or a contiguous series of statements within the enclosing block.
        /// </summary>
        public sealed override bool Succeeded
        {
            get
            {
                if (succeeded == null)
                {
                    var discarded = EntryPoints;
                }

                return succeeded.Value;
            }
        }
    }
}