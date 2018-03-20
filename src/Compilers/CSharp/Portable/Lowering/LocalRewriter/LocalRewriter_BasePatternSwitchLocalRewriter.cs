// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        /// <summary>
        /// A common base class for lowering the pattern switch statement and the pattern switch expression.
        /// </summary>
        private class BasePatternSwitchLocalRewriter : PatternLocalRewriter
        {
            /// <summary>
            /// Map from switch section's syntax to the lowered code for the section. The code for a section
            /// includes the code to assign to the pattern variables and evaluate the when clause. Since a
            /// when clause can yield a false value, it can jump back to a label in the lowered decision dag.
            /// </summary>
            protected readonly Dictionary<SyntaxNode, ArrayBuilder<BoundStatement>> _switchArms = new Dictionary<SyntaxNode, ArrayBuilder<BoundStatement>>();

            /// <summary>
            /// In a switch expression, some labels may first reached by a backward branch, and
            /// it may occur when something (from the enclosing expression) is on the stack.
            /// To satisfy the verifier, the caller must arrange forward jumps to these labels. The set of
            /// states whose labels will need such forward jumps (if something can be on the stack) is stored in
            /// _backwardLabels. In practice, this is exclusively the set of states that are reached
            /// when a when-clause evaluates to false.
            /// PROTOTYPE(patterns2): This is a placeholder. It is not used yet for lowering the
            /// switch expression, where it will be needed.
            /// </summary>
            private readonly ArrayBuilder<BoundDecisionDag> _backwardLabels = ArrayBuilder<BoundDecisionDag>.GetInstance();

            /// <summary>
            /// The lowered decision dag. This includes all of the code to decide which pattern
            /// is matched, but not the code to assign to pattern variables and evaluate when clauses.
            /// </summary>
            protected readonly ArrayBuilder<BoundStatement> _loweredDecisionDag = ArrayBuilder<BoundStatement>.GetInstance();

            /// <summary>
            /// The label in the code for the beginning of code for each node of the dag.
            /// </summary>
            private readonly Dictionary<BoundDecisionDag, LabelSymbol> _dagNodeLabels = new Dictionary<BoundDecisionDag, LabelSymbol>();

            protected BasePatternSwitchLocalRewriter(LocalRewriter localRewriter, BoundExpression loweredInput, ImmutableArray<SyntaxNode> arms, BoundDecisionDag decisionDag)
                : base(localRewriter, loweredInput)
            {
                foreach (var arm in arms)
                {
                    _switchArms.Add(arm, new ArrayBuilder<BoundStatement>());
                }

                ImmutableArray<BoundDecisionDag> sortedNodes = decisionDag.TopologicallySortedNodes();
                ComputeLabelSet(sortedNodes);
                LowerDecisionDag(sortedNodes);
            }

            private void ComputeLabelSet(ImmutableArray<BoundDecisionDag> sortedNodes)
            {
                // Nodes with more than one predecessor are assigned a label
                var hasPredecessor = PooledHashSet<BoundDecisionDag>.GetInstance();
                void notePreedecesssor(BoundDecisionDag successor)
                {
                    if (successor != null && !hasPredecessor.Add(successor))
                    {
                        GetDagNodeLabel(successor);
                    }
                }

                foreach (BoundDecisionDag node in sortedNodes)
                {
                    switch (node)
                    {
                        case BoundWhenClause w:
                            GetDagNodeLabel(node);
                            //Debug.Assert(w.WhenTrue != null);
                            //GetDagNodeLabel(w.WhenTrue);
                            if (w.WhenFalse != null)
                            {
                                GetDagNodeLabel(w.WhenFalse);
                                _backwardLabels.Add(w.WhenFalse);
                            }
                            break;
                        case BoundDecision d:
                            // Final decisions can branch directly to the target
                            _dagNodeLabels[node] = d.Label;
                            break;
                        case BoundEvaluationPoint e:
                            notePreedecesssor(e.Next);
                            break;
                        case BoundDecisionPoint p:
                            notePreedecesssor(p.WhenTrue);
                            notePreedecesssor(p.WhenFalse);
                            break;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(node.Kind);
                    }
                }

                hasPredecessor.Free();
            }

            protected new void Free()
            {
                base.Free();
                _loweredDecisionDag.Free();
                _backwardLabels.Free();
            }

            protected virtual LabelSymbol GetDagNodeLabel(BoundDecisionDag dag)
            {
                if (!_dagNodeLabels.TryGetValue(dag, out LabelSymbol label))
                {
                    _dagNodeLabels.Add(dag, label = dag is BoundDecision d ? d.Label : _factory.GenerateLabel("dagNode"));
                }

                return label;
            }

            /// <summary>
            /// Lower the given nodes into _loweredDecisionDag.
            /// </summary>
            private void LowerDecisionDag(ImmutableArray<BoundDecisionDag> sortedNodes)
            {
                switch (sortedNodes[0])
                {
                    case BoundWhenClause wc:
                        // If the first node is in a when clause's section rather than the code for the
                        // lowered decision dag, jump there to start.
                        _loweredDecisionDag.Add(_factory.Goto(GetDagNodeLabel(wc)));
                        break;
                    case BoundDecision d:
                        // If the first node is a decision rather than the code for the
                        // lowered decision dag, jump there to start.
                        _loweredDecisionDag.Add(_factory.Goto(GetDagNodeLabel(d)));
                        break;
                }

                // Call LowerDecisionDagNode with each node and its following node in the generation order.
                // However, some nodes can be emitted more efficiently as a switch dispatch (possibly out
                // of order).
                var loweredNodes = PooledHashSet<BoundDecisionDag>.GetInstance();
                BoundDecisionDag previous = null;
                foreach (BoundDecisionDag node in sortedNodes)
                {
                    if (node is BoundWhenClause w)
                    {
                        // Code for the when clause goes in a separate code section for the switch section.
                        LowerWhenClause(w);
                        continue;
                    }

                    // BoundDecision nodes do not get any code generated for them.
                    if (node is BoundDecision || loweredNodes.Contains(node))
                    {
                        continue;
                    }

                    if (previous != null && !loweredNodes.Contains(previous))
                    {
                        // Lower the node "previous".
                        if (!GenerateSwitchDispatch(previous, loweredNodes))
                        {
                            LowerDecisionDagNode(previous, node);
                        }

                        loweredNodes.Add(previous);
                    }

                    previous = node;
                }

                // Lower the final node
                if (previous != null && !loweredNodes.Contains(previous))
                {
                    LowerDecisionDagNode(previous, null);
                }

                loweredNodes.Free();
            }

            /// <summary>
            /// Generate a switch dispatch for a contiguous sequence of dag nodes if applicable.
            /// Returns true if it was applicable.
            /// </summary>
            private bool GenerateSwitchDispatch(BoundDecisionDag node, HashSet<BoundDecisionDag> loweredNodes)
            {
                Debug.Assert(!loweredNodes.Contains(node));

                // We only generate a switch dispatch if we have two or more value decisions in a row
                if (!(node is BoundDecisionPoint firstDecisionPoint &&
                     firstDecisionPoint.Decision is BoundNonNullValueDecision firstDecision &&
                     firstDecisionPoint.WhenFalse is BoundDecisionPoint whenFalse &&
                     whenFalse.Decision is BoundNonNullValueDecision secondDecision &&
                     !loweredNodes.Contains(whenFalse) &&
                     !this._dagNodeLabels.ContainsKey(whenFalse) &&
                     firstDecision.Input == secondDecision.Input &&
                     firstDecision.Input.Type.IsValidV6SwitchGoverningType()))
                {
                    // PROTOTYPE(patterns2): Should optimize float, double, decimal value switches. For now use if-then-else
                    return false;
                }

                // Gather up the (value, label) pairs, starting with the first one
                var cases = ArrayBuilder<(ConstantValue value, LabelSymbol label)>.GetInstance();
                cases.Add((value: firstDecision.Value, label: GetDagNodeLabel(firstDecisionPoint.WhenTrue)));

                BoundDecisionPoint previous = firstDecisionPoint;
                while (previous.WhenFalse is BoundDecisionPoint p &&
                    p.Decision is BoundNonNullValueDecision vd &&
                    vd.Input == firstDecision.Input &&
                    !this._dagNodeLabels.ContainsKey(p) &&
                    !loweredNodes.Contains(p))
                {
                    cases.Add((value: vd.Value, label: GetDagNodeLabel(p.WhenTrue)));
                    loweredNodes.Add(p);
                    previous = p;
                }

                // If we are emitting a hash table based string switch,
                // we need to generate a helper method for computing
                // string hash value in <PrivateImplementationDetails> class.

                MethodSymbol stringEquality = null;
                if (firstDecision.Input.Type.SpecialType == SpecialType.System_String)
                {
                    EnsureStringHashFunction(cases.Count, node.Syntax);
                    stringEquality = _localRewriter.UnsafeGetSpecialTypeMethod(node.Syntax, SpecialMember.System_String__op_Equality);
                }

                if (_dagNodeLabels.TryGetValue(node, out LabelSymbol nodeLabel))
                {
                    _loweredDecisionDag.Add(_factory.Label(nodeLabel));
                }

                LabelSymbol defaultLabel = GetDagNodeLabel(previous.WhenFalse);
                var dispatch = new BoundSwitchDispatch(
                    node.Syntax, _tempAllocator.GetTemp(firstDecision.Input), cases.ToImmutableAndFree(), defaultLabel, stringEquality);
                _loweredDecisionDag.Add(dispatch);
                return true;
            }

            /// <summary>
            /// Checks whether we are generating a hash table based string switch and
            /// we need to generate a new helper method for computing string hash value.
            /// Creates the method if needed.
            /// </summary>
            private void EnsureStringHashFunction(int labelsCount, SyntaxNode syntaxNode)
            {
                var module = _localRewriter.EmitModule;
                if (module == null)
                {
                    return;
                }

                // For string switch statements, we need to determine if we are generating a hash
                // table based jump table or a non hash jump table, i.e. linear string comparisons
                // with each case label. We use the Dev10 Heuristic to determine this
                // (see SwitchStringJumpTableEmitter.ShouldGenerateHashTableSwitch() for details).
                if (!CodeAnalysis.CodeGen.SwitchStringJumpTableEmitter.ShouldGenerateHashTableSwitch(module, labelsCount))
                {
                    return;
                }

                // If we are generating a hash table based jump table, we use a simple customizable
                // hash function to hash the string constants corresponding to the case labels.
                // See SwitchStringJumpTableEmitter.ComputeStringHash().
                // We need to emit this function to compute the hash value into the compiler generated
                // <PrivateImplementationDetails> class. 
                // If we have at least one string switch statement in a module that needs a
                // hash table based jump table, we generate a single public string hash synthesized method
                // that is shared across the module.

                // If we have already generated the helper, possibly for another switch
                // or on another thread, we don't need to regenerate it.
                var privateImplClass = module.GetPrivateImplClass(syntaxNode, _localRewriter._diagnostics);
                if (privateImplClass.GetMethod(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedStringHashFunctionName) != null)
                {
                    return;
                }

                // cannot emit hash method if have no access to Chars.
                var charsMember = _localRewriter._compilation.GetSpecialTypeMember(SpecialMember.System_String__Chars);
                if ((object)charsMember == null || charsMember.GetUseSiteDiagnostic() != null)
                {
                    return;
                }

                TypeSymbol returnType = _factory.SpecialType(SpecialType.System_UInt32);
                TypeSymbol paramType = _factory.SpecialType(SpecialType.System_String);

                var method = new SynthesizedStringSwitchHashMethod(module.SourceModule, privateImplClass, returnType, paramType);
                privateImplClass.TryAddSynthesizedMethod(method);
            }

            private void LowerWhenClause(BoundWhenClause whenClause)
            {
                // This node is used even when there is no when clause, to record bindings. In the case that there
                // is no when clause, whenClause.WhenExpression and whenClause.WhenFalse are null, and the syntax for this
                // node is the case clause.

                // We need to assign the pattern variables in the code where they are in scope, so we produce a branch
                // to the section where they are in scope and evaluate the when clause there.
                var whenTrue = (BoundDecision)whenClause.WhenTrue;
                LabelSymbol labelToSectionScope = GetDagNodeLabel(whenClause);

                // We need the section syntax to get the section builder from the map. Unfortunately this is a bit awkward
                SyntaxNode sectionSyntax;
                switch (whenClause.Syntax)
                {
                    case WhenClauseSyntax w:
                        sectionSyntax = w.Parent.Parent;
                        break;
                    case SwitchLabelSyntax l:
                        sectionSyntax = l.Parent;
                        break;
                    case SwitchExpressionArmSyntax a:
                        sectionSyntax = a;
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(whenClause.Syntax.Kind());
                }

                bool foundSectionBuilder = _switchArms.TryGetValue(sectionSyntax, out ArrayBuilder<BoundStatement> sectionBuilder);
                Debug.Assert(foundSectionBuilder);
                sectionBuilder.Add(_factory.Label(labelToSectionScope));
                foreach ((BoundExpression left, BoundDagTemp right) in whenClause.Bindings)
                {
                    sectionBuilder.Add(_factory.Assignment(left, _tempAllocator.GetTemp(right)));
                }

                var whenFalse = whenClause.WhenFalse;
                var trueLabel = GetDagNodeLabel(whenTrue);
                if (whenClause.WhenExpression != null && whenClause.WhenExpression.ConstantValue != ConstantValue.True)
                {
                    // PROTOTYPE(patterns2): there should perhaps be a sequence point (for e.g. a breakpoint) on the when clause.
                    // However, it is not clear that is wanted for the switch expression as that would be a breakpoint where the stack is nonempty.
                    sectionBuilder.Add(_factory.ConditionalGoto(_localRewriter.VisitExpression(whenClause.WhenExpression), trueLabel, jumpIfTrue: true));
                    Debug.Assert(whenFalse != null);
                    Debug.Assert(_backwardLabels.Contains(whenFalse));
                    sectionBuilder.Add(_factory.Goto(GetDagNodeLabel(whenFalse)));
                }
                else
                {
                    Debug.Assert(whenFalse == null);
                    sectionBuilder.Add(_factory.Goto(trueLabel));
                }
            }

            /// <summary>
            /// Translate the decision tree for node, given that it will be followed by the translation for nextNode.
            /// </summary>
            private void LowerDecisionDagNode(BoundDecisionDag node, BoundDecisionDag nextNode)
            {
                if (this._dagNodeLabels.TryGetValue(node, out LabelSymbol label))
                {
                    _loweredDecisionDag.Add(_factory.Label(label));
                }

                switch (node)
                {
                    case BoundEvaluationPoint evaluationPoint:
                        {
                            BoundExpression sideEffect = LowerEvaluation(evaluationPoint.Evaluation);
                            Debug.Assert(sideEffect != null);
                            _loweredDecisionDag.Add(_factory.ExpressionStatement(sideEffect));
                            if (nextNode != evaluationPoint.Next)
                            {
                                // We only need a goto if we would not otherwise fall through to the desired state
                                _loweredDecisionDag.Add(_factory.Goto(GetDagNodeLabel(evaluationPoint.Next)));
                            }
                        }

                        break;

                    case BoundDecisionPoint decisionPoint:
                        {
                            // PROTOTYPE(patterns2): should translate a chain of constant value tests into a switch instruction as before
                            BoundExpression test = base.LowerDecision(decisionPoint.Decision);

                            // Because we have already "optimized" away tests for a constant switch expression, the decision should be nontrivial.
                            Debug.Assert(test != null);

                            if (nextNode == decisionPoint.WhenFalse)
                            {
                                _loweredDecisionDag.Add(_factory.ConditionalGoto(test, GetDagNodeLabel(decisionPoint.WhenTrue), jumpIfTrue: true));
                                // fall through to false decision
                            }
                            else if (nextNode == decisionPoint.WhenTrue)
                            {
                                _loweredDecisionDag.Add(_factory.ConditionalGoto(test, GetDagNodeLabel(decisionPoint.WhenFalse), jumpIfTrue: false));
                                // fall through to true decision
                            }
                            else
                            {
                                _loweredDecisionDag.Add(_factory.ConditionalGoto(test, GetDagNodeLabel(decisionPoint.WhenTrue), jumpIfTrue: true));
                                _loweredDecisionDag.Add(_factory.Goto(GetDagNodeLabel(decisionPoint.WhenFalse)));
                            }
                        }

                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(node.Kind);
                }
            }
        }
    }
}
