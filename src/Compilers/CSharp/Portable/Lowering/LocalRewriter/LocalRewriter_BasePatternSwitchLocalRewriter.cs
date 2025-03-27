// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        /// <summary>
        /// A common base class for lowering the pattern switch statement and the pattern switch expression.
        /// </summary>
        private abstract class BaseSwitchLocalRewriter : DecisionDagRewriter
        {
            /// <summary>
            /// Map from when clause's syntax to the lowered code for the matched pattern. The code for a section
            /// includes the code to assign to the pattern variables and evaluate the when clause. Since a
            /// when clause can yield a false value, it can jump back to a label in the lowered decision dag.
            /// </summary>
            private readonly PooledDictionary<SyntaxNode, ArrayBuilder<BoundStatement>> _switchArms = PooledDictionary<SyntaxNode, ArrayBuilder<BoundStatement>>.GetInstance();

            protected override ArrayBuilder<BoundStatement> BuilderForSection(SyntaxNode whenClauseSyntax)
            {
                // We need the section syntax to get the section builder from the map. Unfortunately this is a bit awkward
                SyntaxNode? sectionSyntax = whenClauseSyntax is SwitchLabelSyntax l ? l.Parent : whenClauseSyntax;
                Debug.Assert(sectionSyntax is { });
                bool found = _switchArms.TryGetValue(sectionSyntax, out ArrayBuilder<BoundStatement>? result);
                if (!found || result == null)
                    throw new InvalidOperationException();

                return result;
            }

            protected BaseSwitchLocalRewriter(
                SyntaxNode node,
                LocalRewriter localRewriter,
                ImmutableArray<SyntaxNode> arms,
                bool generateInstrumentation)
                : base(node, localRewriter, generateInstrumentation)
            {
                foreach (var arm in arms)
                {
                    var armBuilder = ArrayBuilder<BoundStatement>.GetInstance();

                    // We start each switch block of a switch statement with a hidden sequence point so that
                    // we do not appear to be in the previous switch block when we begin.
                    if (GenerateInstrumentation)
                        armBuilder.Add(_factory.HiddenSequencePoint());

                    _switchArms.Add(arm, armBuilder);
                }
            }

            protected new void Free()
            {
                _switchArms.Free();
                base.Free();
            }

            /// <summary>
            /// Lower the given nodes into _loweredDecisionDag. Should only be called once per instance of this.
            /// </summary>
            protected (ImmutableArray<BoundStatement> loweredDag, ImmutableDictionary<SyntaxNode, ImmutableArray<BoundStatement>> switchSections) LowerDecisionDag(BoundDecisionDag decisionDag)
            {
                var loweredDag = LowerDecisionDagCore(decisionDag);
                var switchSections = _switchArms.ToImmutableDictionary(kv => kv.Key, kv => kv.Value.ToImmutableAndFree());
                _switchArms.Clear();
                return (loweredDag, switchSections);
            }
        }
    }
}
