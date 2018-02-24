// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitPatternSwitchStatement2(BoundPatternSwitchStatement2 node)
        {
            return PatternSwitchLocalRewriter2.Rewrite(this, node);
        }

        private class PatternSwitchLocalRewriter2 : BasePatternSwitchLocalRewriter
        {
            /// <summary>
            /// Map from switch section's syntax to the lowered code for the section. The code for a section
            /// includes the code to assign to the pattern variables and evaluate the when clause. Since a
            /// when clause can yield a false value, it can jump back to a label in the lowered decision dag.
            /// </summary>
            private Dictionary<SyntaxNode, ArrayBuilder<BoundStatement>> _switchSections => base._switchArms;

            private PatternSwitchLocalRewriter2(BoundPatternSwitchStatement2 node, LocalRewriter localRewriter)
                : base(localRewriter, localRewriter.VisitExpression(node.Expression), node.SwitchSections.SelectAsArray(section => section.Syntax), node.DecisionDag)
            {
            }

            public static BoundNode Rewrite(LocalRewriter localRewriter, BoundPatternSwitchStatement2 node)
            {
                var rewriter = new PatternSwitchLocalRewriter2(node, localRewriter);
                BoundStatement result = rewriter.LowerPatternSwitchStatement2(node);
                rewriter.Free();
                return result;
            }

            private BoundStatement LowerPatternSwitchStatement2(BoundPatternSwitchStatement2 node)
            {
                var result = ArrayBuilder<BoundStatement>.GetInstance();

                // Assign the input to a temp
                result.Add(_factory.Assignment(_tempAllocator.GetTemp(base._inputTemp), base._loweredInput));

                // then add the rest of the lowered dag that references that input
                result.Add(_factory.Block(this._loweredDecisionDag.ToImmutable()));
                // A branch to the default label when no switch case matches is included in the
                // decision tree, so the code in result is unreachable at this point.

                // Lower each switch section.
                foreach (BoundPatternSwitchSection section in node.SwitchSections)
                {
                    ArrayBuilder<BoundStatement> sectionStatementBuilder = _switchSections[section.Syntax];
                    foreach (BoundPatternSwitchLabel switchLabel in section.SwitchLabels)
                    {
                        sectionStatementBuilder.Add(_factory.Label(switchLabel.Label));
                    }

                    foreach (BoundStatement statement in section.Statements)
                    {
                        var stmt = (BoundStatement)_localRewriter.Visit(statement);
                        if (stmt != null) sectionStatementBuilder.Add(stmt);
                    }
                    // By the semantics of the switch statement, the end of each section is unreachable.

                    result.Add(_factory.Block(section.Locals, sectionStatementBuilder.ToImmutable()));
                }

                result.Add(_factory.Label(node.BreakLabel));

                // Dispatch temps are in scope throughout the switch statement, as they are used
                // both in the dispatch section to hold temporary values from the translation of
                // the decision dag, and in the branches where the temp values are assigned to the
                // pattern variables of matched patterns.
                return _factory.Block(_tempAllocator.AllTemps(), result.ToImmutableAndFree());
            }
        }
    }
}
