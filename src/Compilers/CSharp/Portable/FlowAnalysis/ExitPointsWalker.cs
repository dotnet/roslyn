// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A region analysis walker that records jumps out of the region.
    /// </summary>
    internal class ExitPointsWalker : AbstractRegionControlFlowPass
    {
        private readonly ArrayBuilder<LabelSymbol> _labelsInside;
        private readonly ArrayBuilder<StatementSyntax> _branchesOutOf;

        private ExitPointsWalker(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion)
            : base(compilation, member, node, firstInRegion, lastInRegion)
        {
            _labelsInside = new ArrayBuilder<LabelSymbol>();
            _branchesOutOf = ArrayBuilder<StatementSyntax>.GetInstance();
        }

        protected override void Free()
        {
            if (_branchesOutOf != null)
            {
                _branchesOutOf.Free();
            }

            _labelsInside.Free();
            base.Free();
        }

        internal static ImmutableArray<StatementSyntax> Analyze(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion)
        {
            var walker = new ExitPointsWalker(compilation, member, node, firstInRegion, lastInRegion);
            try
            {
                return walker.Analyze();
            }
            finally
            {
                walker.Free();
            }
        }

        private ImmutableArray<StatementSyntax> Analyze()
        {
            bool badRegion = false;

            // only one pass is needed.
            Scan(ref badRegion);

            if (badRegion)
            {
                return ImmutableArray<StatementSyntax>.Empty;
            }

            _branchesOutOf.Sort((x, y) => x.SpanStart - y.SpanStart);
            return _branchesOutOf.ToImmutable();
        }

        public override BoundNode VisitLabelStatement(BoundLabelStatement node)
        {
            if (IsInside) _labelsInside.Add(node.Label);
            return base.VisitLabelStatement(node);
        }

        public override BoundNode VisitDoStatement(BoundDoStatement node)
        {
            if (IsInside)
            {
                _labelsInside.Add(node.BreakLabel);
                _labelsInside.Add(node.ContinueLabel);
            }
            return base.VisitDoStatement(node);
        }

        public override BoundNode VisitForEachStatement(BoundForEachStatement node)
        {
            if (IsInside)
            {
                _labelsInside.Add(node.BreakLabel);
                _labelsInside.Add(node.ContinueLabel);
            }
            return base.VisitForEachStatement(node);
        }

        public override BoundNode VisitForStatement(BoundForStatement node)
        {
            if (IsInside)
            {
                _labelsInside.Add(node.BreakLabel);
                _labelsInside.Add(node.ContinueLabel);
            }
            return base.VisitForStatement(node);
        }

        public override BoundNode VisitWhileStatement(BoundWhileStatement node)
        {
            if (IsInside)
            {
                _labelsInside.Add(node.BreakLabel);
            }
            return base.VisitWhileStatement(node);
        }

        protected override void EnterRegion()
        {
            base.EnterRegion();
        }

        protected override void LeaveRegion()
        {
            foreach (var pending in PendingBranches.AsEnumerable())
            {
                if (pending.Branch == null || !RegionContains(pending.Branch.Syntax.Span)) continue;
                switch (pending.Branch.Kind)
                {
                    case BoundKind.GotoStatement:
                        if (_labelsInside.Contains(((BoundGotoStatement)pending.Branch).Label)) continue;
                        break;
                    case BoundKind.BreakStatement:
                        if (_labelsInside.Contains(((BoundBreakStatement)pending.Branch).Label)) continue;
                        break;
                    case BoundKind.ContinueStatement:
                        if (_labelsInside.Contains(((BoundContinueStatement)pending.Branch).Label)) continue;
                        break;
                    case BoundKind.YieldBreakStatement:
                    case BoundKind.ReturnStatement:
                        // Return statements are included
                        break;
                    case BoundKind.YieldReturnStatement:
                    case BoundKind.AwaitExpression:
                    case BoundKind.UsingStatement:
                    case BoundKind.ForEachStatement when ((BoundForEachStatement)pending.Branch).EnumeratorInfoOpt is { MoveNextAwaitableInfo: not null }:
                        // We don't do anything with yield return statements, async using statement, async foreach statement, or await expressions;
                        // they are treated as if they are not jumps.
                        continue;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(pending.Branch.Kind);
                }
                _branchesOutOf.Add((StatementSyntax)pending.Branch.Syntax);
            }

            base.LeaveRegion();
        }
    }
}
