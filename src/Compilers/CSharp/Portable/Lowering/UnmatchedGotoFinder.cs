// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Compiles a list of all labels that are targeted by gotos within a
    /// node, but are not declared within the node.
    /// </summary>
    internal sealed class UnmatchedGotoFinder : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
    {
        private readonly Dictionary<BoundNode, HashSet<LabelSymbol>> _unmatchedLabelsCache; // NB: never modified.

        private HashSet<LabelSymbol> _gotos;
        private HashSet<LabelSymbol> _targets;

        private UnmatchedGotoFinder(Dictionary<BoundNode, HashSet<LabelSymbol>> unmatchedLabelsCache, int recursionDepth)
            : base(recursionDepth)
        {
            Debug.Assert(unmatchedLabelsCache != null);
            _unmatchedLabelsCache = unmatchedLabelsCache;
        }

        public static HashSet<LabelSymbol> Find(BoundNode node, Dictionary<BoundNode, HashSet<LabelSymbol>> unmatchedLabelsCache, int recursionDepth)
        {
            UnmatchedGotoFinder finder = new UnmatchedGotoFinder(unmatchedLabelsCache, recursionDepth);
            finder.Visit(node);
            HashSet<LabelSymbol> gotos = finder._gotos;
            HashSet<LabelSymbol> targets = finder._targets;
            if (gotos != null && targets != null)
            {
                gotos.RemoveAll(targets);
            }
            return gotos;
        }

        public override BoundNode Visit(BoundNode node)
        {
            HashSet<LabelSymbol> unmatched;
            if (node != null && _unmatchedLabelsCache.TryGetValue(node, out unmatched))
            {
                if (unmatched != null)
                {
                    foreach (LabelSymbol label in unmatched)
                    {
                        AddGoto(label);
                    }
                }

                return null; // Don't visit children.
            }

            return base.Visit(node);
        }

        public override BoundNode VisitGotoStatement(BoundGotoStatement node)
        {
            AddGoto(node.Label);
            return base.VisitGotoStatement(node);
        }

        public override BoundNode VisitConditionalGoto(BoundConditionalGoto node)
        {
            AddGoto(node.Label);
            return base.VisitConditionalGoto(node);
        }

        public override BoundNode VisitSwitchDispatch(BoundSwitchDispatch node)
        {
            AddGoto(node.DefaultLabel);
            foreach ((_, LabelSymbol label) in node.Cases)
            {
                AddGoto(label);
            }

            return base.VisitSwitchDispatch(node);
        }

        public override BoundNode VisitLabelStatement(BoundLabelStatement node)
        {
            AddTarget(node.Label);
            return base.VisitLabelStatement(node);
        }

        public override BoundNode VisitLabeledStatement(BoundLabeledStatement node)
        {
            AddTarget(node.Label);
            return base.VisitLabeledStatement(node);
        }

        private void AddGoto(LabelSymbol label)
        {
            if (_gotos == null)
            {
                _gotos = new HashSet<LabelSymbol>();
            }

            _gotos.Add(label);
        }

        private void AddTarget(LabelSymbol label)
        {
            if (_targets == null)
            {
                _targets = new HashSet<LabelSymbol>();
            }

            _targets.Add(label);
        }
    }
}
