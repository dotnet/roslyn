// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class IteratorMethodToStateMachineRewriter
    {
        /// <summary>
        /// Analyses method body for yields in try blocks and labels that they contain.
        /// </summary>
        private sealed class YieldsInTryAnalysis : LabelCollector
        {
            // all try blocks with yields in them and complete set of labels inside those try blocks
            // NOTE: non-yielding try blocks are transparently ignored - i.e. their labels are included
            //       in the label set of the nearest yielding-try parent  
            private Dictionary<BoundTryStatement, HashSet<LabelSymbol>> _labelsInYieldingTrys;

            // transient accumulators.
            private bool _seenYield;

            public YieldsInTryAnalysis(BoundStatement body)
            {
                _seenYield = false;
                this.Visit(body);
            }

            /// <summary>
            /// Returns true if given try or any of its nested try blocks contain yields
            /// </summary>
            public bool ContainsYields(BoundTryStatement statement)
            {
                return _labelsInYieldingTrys != null && _labelsInYieldingTrys.ContainsKey(statement);
            }

            /// <summary>
            /// Returns true if body contains yield returns within try blocks.
            /// </summary>
            public bool ContainsYieldsInTrys()
            {
                return _labelsInYieldingTrys != null;
            }

            /// <summary>
            /// Labels reachable from within this frame without invoking its finally. 
            /// null if there are none such labels.
            /// </summary>
            internal HashSet<LabelSymbol> Labels(BoundTryStatement statement)
            {
                return _labelsInYieldingTrys[statement];
            }

            public override BoundNode VisitTryStatement(BoundTryStatement node)
            {
                var origSeenYield = _seenYield;
                var origLabels = this.currentLabels;

                // sibling try blocks do not see each other's yields
                _seenYield = false;
                this.currentLabels = null;

                base.VisitTryStatement(node);

                if (_seenYield)
                {
                    // this try yields !

                    var yieldingTryLabels = _labelsInYieldingTrys;
                    if (yieldingTryLabels == null)
                    {
                        _labelsInYieldingTrys = yieldingTryLabels = new Dictionary<BoundTryStatement, HashSet<LabelSymbol>>();
                    }

                    yieldingTryLabels.Add(node, currentLabels);
                    currentLabels = origLabels;
                }
                else
                {
                    // this is a boring non-yielding try

                    // currentLabels = currentLabels U origLabels ;
                    if (currentLabels == null)
                    {
                        currentLabels = origLabels;
                    }
                    else if (origLabels != null)
                    {
                        currentLabels.UnionWith(origLabels);
                    }
                }

                _seenYield = _seenYield | origSeenYield;
                return null;
            }

            public override BoundNode VisitYieldReturnStatement(BoundYieldReturnStatement node)
            {
                _seenYield = true;
                return base.VisitYieldReturnStatement(node);
            }

            public override BoundNode VisitExpressionStatement(BoundExpressionStatement node)
            {
                // expressions cannot contain labels, branches or yields.
                return null;
            }
        }
    }

    /// <summary>
    /// Analyses method body for labels.
    /// </summary>
    internal abstract class LabelCollector : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
    {
        // transient accumulator.
        protected HashSet<LabelSymbol> currentLabels;

        public override BoundNode VisitLabelStatement(BoundLabelStatement node)
        {
            CollectLabel(node.Label);
            return base.VisitLabelStatement(node);
        }

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            CollectLabel(node.ConstantTargetOpt);
            CollectLabel(node.BreakLabel);
            return base.VisitSwitchStatement(node);
        }

        public override BoundNode VisitSwitchLabel(BoundSwitchLabel node)
        {
            CollectLabel(node.Label);
            return base.VisitSwitchLabel(node);
        }

        private void CollectLabel(LabelSymbol label)
        {
            if ((object)label != null)
            {
                var currentLabels = this.currentLabels;
                if (currentLabels == null)
                {
                    this.currentLabels = currentLabels = new HashSet<LabelSymbol>();
                }
                currentLabels.Add(label);
            }
        }
    }
}
