// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            return SwitchStatementLocalRewriter.Rewrite(this, node);
        }

        private sealed class SwitchStatementLocalRewriter : BaseSwitchLocalRewriter
        {
            /// <summary>
            /// A map from section syntax to the first label in that section.
            /// </summary>
            private readonly Dictionary<SyntaxNode, LabelSymbol> _sectionLabels = PooledDictionary<SyntaxNode, LabelSymbol>.GetInstance();

            public static BoundStatement Rewrite(LocalRewriter localRewriter, BoundSwitchStatement node)
            {
                var rewriter = new SwitchStatementLocalRewriter(node, localRewriter);
                BoundStatement result = rewriter.LowerSwitchStatement(node);
                rewriter.Free();
                return result;
            }

            /// <summary>
            /// We revise the returned label for a leaf so that all leaves in the same switch section are given the same label.
            /// This enables the switch emitter to produce better code.
            /// </summary>
            protected override LabelSymbol GetDagNodeLabel(BoundDecisionDagNode dag)
            {
                var result = base.GetDagNodeLabel(dag);
                if (dag is BoundLeafDecisionDagNode d)
                {
                    SyntaxNode? section = d.Syntax.Parent;

                    // It is possible that the leaf represents a compiler-generated default for a switch statement in the EE.
                    // In that case d.Syntax is the whole switch statement, and its parent is null. We are only interested
                    // in leaves that result from explicit switch case labels in a switch section.
                    if (section?.Kind() == SyntaxKind.SwitchSection)
                    {
                        if (_sectionLabels.TryGetValue(section, out LabelSymbol? replacementLabel))
                        {
                            return replacementLabel;
                        }

                        _sectionLabels.Add(section, result);
                    }
                }

                return result;
            }

            private SwitchStatementLocalRewriter(BoundSwitchStatement node, LocalRewriter localRewriter)
                : base(node.Syntax, localRewriter, node.SwitchSections.SelectAsArray(section => section.Syntax),
                      // Only add instrumentation (such as sequence points) if the node is not compiler-generated.
                      generateInstrumentation: localRewriter.Instrument && !node.WasCompilerGenerated)
            {
            }

            private BoundStatement LowerSwitchStatement(BoundSwitchStatement node)
            {
                _factory.Syntax = node.Syntax;
                var result = ArrayBuilder<BoundStatement>.GetInstance();
                var outerVariables = ArrayBuilder<LocalSymbol>.GetInstance();
                var loweredSwitchGoverningExpression = _localRewriter.VisitExpression(node.Expression);
                if (!node.WasCompilerGenerated && _localRewriter.Instrument)
                {
                    // EnC: We need to insert a hidden sequence point to handle function remapping in case
                    // the containing method is edited while methods invoked in the expression are being executed.
                    var instrumentedExpression = _localRewriter._instrumenter.InstrumentSwitchStatementExpression(node, loweredSwitchGoverningExpression, _factory);
                    if (loweredSwitchGoverningExpression.ConstantValue == null)
                    {
                        loweredSwitchGoverningExpression = instrumentedExpression;
                    }
                    else
                    {
                        // If the expression is a constant, we leave it alone (the decision dag lowering code needs
                        // to see that constant). But we add an additional leading statement with the instrumented expression.
                        result.Add(_factory.ExpressionStatement(instrumentedExpression));
                    }
                }

                // The set of variables attached to the outer block
                outerVariables.AddRange(node.InnerLocals);

                // Evaluate the input and set up sharing for dag temps with user variables
                BoundDecisionDag decisionDag = ShareTempsIfPossibleAndEvaluateInput(
                    node.GetDecisionDagForLowering(_factory.Compilation),
                    loweredSwitchGoverningExpression, result, out _);

                // In a switch statement, there is a hidden sequence point after evaluating the input at the start of
                // the code to handle the decision dag. This is necessary so that jumps back from a `when` clause into
                // the decision dag do not appear to jump back up to the enclosing construct.
                if (GenerateInstrumentation)
                {
                    // Since there may have been no code to evaluate the input, add a no-op for any previous sequence point to bind to.
                    if (result.Count == 0)
                        result.Add(_factory.NoOp(NoOpStatementFlavor.Default));

                    result.Add(_factory.HiddenSequencePoint());
                }

                // lower the decision dag.
                (ImmutableArray<BoundStatement> loweredDag, ImmutableDictionary<SyntaxNode, ImmutableArray<BoundStatement>> switchSections) =
                    LowerDecisionDag(decisionDag);

                if (_whenNodeIdentifierLocal is not null)
                {
                    outerVariables.Add(_whenNodeIdentifierLocal);
                }

                // then add the rest of the lowered dag that references that input
                result.Add(_factory.Block(loweredDag));

                // A branch to the default label when no switch case matches is included in the
                // decision dag, so the code in `result` is unreachable at this point.

                // Lower each switch section.
                foreach (BoundSwitchSection section in node.SwitchSections)
                {
                    _factory.Syntax = section.Syntax;
                    var sectionBuilder = ArrayBuilder<BoundStatement>.GetInstance();
                    sectionBuilder.AddRange(switchSections[section.Syntax]);
                    foreach (BoundSwitchLabel switchLabel in section.SwitchLabels)
                    {
                        sectionBuilder.Add(_factory.Label(switchLabel.Label));
                    }

                    // Add the translated body of the switch section
                    sectionBuilder.AddRange(_localRewriter.VisitList(section.Statements));

                    // By the semantics of the switch statement, the end of each section is required to be unreachable.
                    // So we can just seal the block and there is no need to follow it by anything.
                    ImmutableArray<BoundStatement> statements = sectionBuilder.ToImmutableAndFree();

                    if (section.Locals.IsEmpty)
                    {
                        result.Add(_factory.StatementList(statements));
                    }
                    else
                    {
                        // Lifetime of these locals is expanded to the entire switch body, as it is possible to capture
                        // them in a different section by using a local function as an intermediary.
                        outerVariables.AddRange(section.Locals);

                        // Note the language scope of the locals, even though they are included for the purposes of
                        // lifetime analysis in the enclosing scope.
                        result.Add(new BoundScope(section.Syntax, section.Locals, statements));
                    }
                }

                // Dispatch temps are in scope throughout the switch statement, as they are used
                // both in the dispatch section to hold temporary values from the translation of
                // the decision dag, and in the branches where the temp values are assigned to the
                // pattern variables of matched patterns.
                outerVariables.AddRange(_tempAllocator.AllTemps());

                _factory.Syntax = node.Syntax;
                if (GenerateInstrumentation)
                    result.Add(_factory.HiddenSequencePoint());

                result.Add(_factory.Label(node.BreakLabel));
                BoundStatement translatedSwitch = _factory.Block(outerVariables.ToImmutableAndFree(), node.InnerLocalFunctions, result.ToImmutableAndFree());

                if (GenerateInstrumentation)
                    translatedSwitch = _localRewriter._instrumenter.InstrumentSwitchStatement(node, translatedSwitch);

                return translatedSwitch;
            }
        }
    }
}
