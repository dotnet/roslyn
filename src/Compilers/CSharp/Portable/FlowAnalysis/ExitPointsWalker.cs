// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A region analysis walker that records jumps out of the region.
    /// </summary>
    internal class ExitPointsWalker : AbstractRegionControlFlowPass
    {
        private readonly ArrayBuilder<LabelSymbol> _labelsInside;
        private ArrayBuilder<StatementSyntax> _branchesOutOf;

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

        internal static IEnumerable<StatementSyntax> Analyze(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion)
        {
            var walker = new ExitPointsWalker(compilation, member, node, firstInRegion, lastInRegion);
            try
            {
                bool badRegion = false;
                walker.Analyze(ref badRegion);
                var result = walker._branchesOutOf.ToImmutableAndFree();
                walker._branchesOutOf = null;
                return badRegion ? SpecializedCollections.EmptyEnumerable<StatementSyntax>() : result;
            }
            finally
            {
                walker.Free();
            }
        }

        private void Analyze(ref bool badRegion)
        {
            // only one pass is needed.
            Scan(ref badRegion);
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

        override protected void EnterRegion()
        {
            base.EnterRegion();
        }

        override protected void LeaveRegion()
        {
            foreach (var pending in PendingBranches)
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
                    case BoundKind.ForEachStatement when ((BoundForEachStatement)pending.Branch).AwaitOpt != null:
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
