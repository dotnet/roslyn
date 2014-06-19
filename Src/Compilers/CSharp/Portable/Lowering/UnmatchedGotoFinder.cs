// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal sealed class UnmatchedGotoFinder : BoundTreeWalker
    {
        private readonly Dictionary<BoundNode, HashSet<LabelSymbol>> unmatchedLabelsCache; // NB: never modified.

        private HashSet<LabelSymbol> gotos;
        private HashSet<LabelSymbol> targets;

        private UnmatchedGotoFinder(Dictionary<BoundNode, HashSet<LabelSymbol>> unmatchedLabelsCache)
        {
            Debug.Assert(unmatchedLabelsCache != null);
            this.unmatchedLabelsCache = unmatchedLabelsCache;
        }

        public static HashSet<LabelSymbol> Find(BoundNode node, Dictionary<BoundNode, HashSet<LabelSymbol>> unmatchedLabelsCache)
        {
            UnmatchedGotoFinder finder = new UnmatchedGotoFinder(unmatchedLabelsCache);
            finder.Visit(node);
            HashSet<LabelSymbol> gotos = finder.gotos;
            HashSet<LabelSymbol> targets = finder.targets;
            if (gotos != null && targets != null)
            {
                gotos.RemoveAll(targets);
            }
            return gotos;
        }

        public override BoundNode Visit(BoundNode node)
        {
            HashSet<LabelSymbol> unmatched;
            if (node != null && unmatchedLabelsCache.TryGetValue(node, out unmatched))
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

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            AddTarget(node.BreakLabel);
            return base.VisitSwitchStatement(node);
        }

        public override BoundNode VisitSwitchLabel(BoundSwitchLabel node)
        {
            AddTarget(node.Label);
            return base.VisitSwitchLabel(node);
        }

        private void AddGoto(LabelSymbol label)
        {
            if (gotos == null)
            {
                gotos = new HashSet<LabelSymbol>();
            }

            gotos.Add(label);
        }

        private void AddTarget(LabelSymbol label)
        {
            if (targets == null)
            {
                targets = new HashSet<LabelSymbol>();
            }

            targets.Add(label);
        }
    }
}
