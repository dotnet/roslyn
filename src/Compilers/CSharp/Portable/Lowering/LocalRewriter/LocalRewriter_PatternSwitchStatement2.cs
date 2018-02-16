// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitPatternSwitchStatement2(BoundPatternSwitchStatement2 node)
        {
            var rewriter = new PatternSwitchLocalRewriter2(this, node);
            return rewriter.LowerDecisionDag(node.DecisionDag);
        }

        private struct PatternSwitchLocalRewriter2
        {
            private readonly LocalRewriter _localRewriter;
            private readonly SyntheticBoundNodeFactory _factory;

            /// <summary>
            /// Map from switch section's syntax to the lowered code for the section.
            /// </summary>
            private readonly Dictionary<SyntaxNode, ArrayBuilder<BoundStatement>> _switchSections;

            /// <summary>
            /// The lowered decision dag.
            /// </summary>
            private ArrayBuilder<BoundStatement> _loweredDecisionDag;

            // Dispatch temps are in scope throughout the switch statement, as they are used
            // both in the dispatch section to hold temporary values from the translation of
            // the decision dag, and in the branches where the temp values are assigned to the
            // pattern variables of matched patterns.
            private ArrayBuilder<LocalSymbol> _dispatchTemps;

            public PatternSwitchLocalRewriter2(LocalRewriter localRewriter, BoundPatternSwitchStatement2 node)
            {
                this._switchSections = new Dictionary<SyntaxNode, ArrayBuilder<BoundStatement>>();
                this._loweredDecisionDag = ArrayBuilder<BoundStatement>.GetInstance();
                this._dispatchTemps = ArrayBuilder<LocalSymbol>.GetInstance();
                this._localRewriter = localRewriter;
                this._factory = localRewriter._factory;
                this._factory.Syntax = node.Syntax;
                foreach (var section in node.SwitchSections)
                {
                    _switchSections.Add(section.Syntax, ArrayBuilder<BoundStatement>.GetInstance());
                }
            }

            internal BoundStatement LowerDecisionDag(BoundDecisionDag decisionDag)
            {
                throw new NotImplementedException();
            }
        }
    }
}
