// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
        private abstract class BaseSwitchLocalRewriter : PatternLocalRewriter
        {
            /// <summary>
            /// Map from switch section's syntax to the lowered code for the section. The code for a section
            /// includes the code to assign to the pattern variables and evaluate the when clause. Since a
            /// when clause can yield a false value, it can jump back to a label in the lowered decision dag.
            /// </summary>
            private readonly PooledDictionary<SyntaxNode, ArrayBuilder<BoundStatement>> _switchArms = PooledDictionary<SyntaxNode, ArrayBuilder<BoundStatement>>.GetInstance();

            /// <summary>
            /// The lowered decision dag. This includes all of the code to decide which pattern
            /// is matched, but not the code to assign to pattern variables and evaluate when clauses.
            /// </summary>
            private readonly ArrayBuilder<BoundStatement> _loweredDecisionDag = ArrayBuilder<BoundStatement>.GetInstance();

            /// <summary>
            /// The label in the code for the beginning of code for each node of the dag.
            /// </summary>
            private readonly PooledDictionary<BoundDecisionDagNode, LabelSymbol> _dagNodeLabels = PooledDictionary<BoundDecisionDagNode, LabelSymbol>.GetInstance();

            protected BaseSwitchLocalRewriter(
                SyntaxNode node,
                LocalRewriter localRewriter,
                ImmutableArray<SyntaxNode> arms)
                : base(node, localRewriter)
            {
                foreach (var arm in arms)
                {
                    var armBuilder = ArrayBuilder<BoundStatement>.GetInstance();

                    // We start each switch block of a switch statement with a hidden sequence point so that
                    // we do not appear to be in the previous switch block when we begin.
                    if (IsSwitchStatement)
                        armBuilder.Add(_factory.HiddenSequencePoint());

                    _switchArms.Add(arm, armBuilder);
                }
            }

            private void ComputeLabelSet(BoundDecisionDag decisionDag)
            {
                // Nodes with more than one predecessor are assigned a label
                var hasPredecessor = PooledHashSet<BoundDecisionDagNode>.GetInstance();
                foreach (BoundDecisionDagNode node in decisionDag.TopologicallySortedNodes)
                {
                    switch (node)
                    {
                        case BoundWhenDecisionDagNode w:
                            GetDagNodeLabel(node);
                            if (w.WhenFalse != null)
                            {
                                GetDagNodeLabel(w.WhenFalse);
                            }
                            break;
                        case BoundLeafDecisionDagNode d:
                            // Leaf can branch directly to the target
                            _dagNodeLabels[node] = d.Label;
                            break;
                        case BoundEvaluationDecisionDagNode e:
                            notePredecessor(e.Next);
                            break;
                        case BoundTestDecisionDagNode p:
                            notePredecessor(p.WhenTrue);
                            notePredecessor(p.WhenFalse);
                            break;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(node.Kind);
                    }
                }

                hasPredecessor.Free();
                return;

                void notePredecessor(BoundDecisionDagNode successor)
                {
                    if (successor != null && !hasPredecessor.Add(successor))
                    {
                        GetDagNodeLabel(successor);
                    }
                }
            }

            protected new void Free()
            {
                _dagNodeLabels.Free();
                _switchArms.Free();
                base.Free();
            }

            protected virtual LabelSymbol GetDagNodeLabel(BoundDecisionDagNode dag)
            {
                if (!_dagNodeLabels.TryGetValue(dag, out LabelSymbol label))
                {
                    _dagNodeLabels.Add(dag, label = dag is BoundLeafDecisionDagNode d ? d.Label : _factory.GenerateLabel("dagNode"));
                }

                return label;
            }

            /// <summary>
            /// A utility class that is used to scan a when clause to determine if it might assign a variable,
            /// directly or indirectly. Used to determine if we can skip the allocation of pattern-matching
            /// temporary variables and use user-declared variables instead, because we can conclude that they
            /// are not mutated while the pattern-matching automaton is running.
            /// </summary>
            protected class WhenClauseMightAssignWalker : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
            {
                private bool _mightAssignSomething;

                public bool MightAssignSomething(BoundExpression expr)
                {
                    if (expr == null || expr.ConstantValue != null)
                    {
                        return false;
                    }

                    this._mightAssignSomething = false;
                    this.Visit(expr);
                    return this._mightAssignSomething;
                }

                public override BoundNode Visit(BoundNode node)
                {
                    // Stop visiting once we determine something might get assigned
                    return this._mightAssignSomething ? null : base.Visit(node);
                }

                public override BoundNode VisitCall(BoundCall node)
                {
                    bool mightMutate =
                        // might be a call to a local function that assigns something
                        node.Method.MethodKind == MethodKind.LocalFunction ||
                        // or perhaps we are passing a variable by ref and mutating it that way
                        !node.ArgumentRefKindsOpt.IsDefault;

                    if (mightMutate)
                        _mightAssignSomething = true;
                    else
                        base.VisitCall(node);

                    return null;
                }

                public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
                {
                    _mightAssignSomething = true;
                    return null;
                }

                public override BoundNode VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
                {
                    _mightAssignSomething = true;
                    return null;
                }

                public override BoundNode VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
                {
                    _mightAssignSomething = true;
                    return null;
                }

                public override BoundNode VisitIncrementOperator(BoundIncrementOperator node)
                {
                    _mightAssignSomething = true;
                    return null;
                }

                public override BoundNode VisitDynamicInvocation(BoundDynamicInvocation node)
                {
                    // perhaps we are passing a variable by ref and mutating it that way
                    if (!node.ArgumentRefKindsOpt.IsDefault)
                        _mightAssignSomething = true;
                    else
                        base.VisitDynamicInvocation(node);

                    return null;
                }

                public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
                {
                    // perhaps we are passing a variable by ref and mutating it that way
                    if (!node.ArgumentRefKindsOpt.IsDefault)
                        _mightAssignSomething = true;
                    else
                        base.VisitObjectCreationExpression(node);

                    return null;
                }

                public override BoundNode VisitDynamicObjectCreationExpression(BoundDynamicObjectCreationExpression node)
                {
                    if (!node.ArgumentRefKindsOpt.IsDefault)
                        _mightAssignSomething = true;
                    else
                        base.VisitDynamicObjectCreationExpression(node);

                    return null;
                }

                public override BoundNode VisitObjectInitializerMember(BoundObjectInitializerMember node)
                {
                    // Although ref indexers are not declarable in C#, they may be usable
                    if (!node.ArgumentRefKindsOpt.IsDefault)
                        _mightAssignSomething = true;
                    else
                        base.VisitObjectInitializerMember(node);

                    return null;
                }

                public override BoundNode VisitIndexerAccess(BoundIndexerAccess node)
                {
                    // Although property arguments with ref indexers are not declarable in C#, they may be usable
                    if (!node.ArgumentRefKindsOpt.IsDefault)
                        _mightAssignSomething = true;
                    else
                        base.VisitIndexerAccess(node);

                    return null;
                }

                public override BoundNode VisitDynamicIndexerAccess(BoundDynamicIndexerAccess node)
                {
                    if (!node.ArgumentRefKindsOpt.IsDefault)
                        _mightAssignSomething = true;
                    else
                        base.VisitDynamicIndexerAccess(node);

                    return null;
                }
            }

            protected BoundDecisionDag ShareTempsIfPossibleAndEvaluateInput(
                BoundDecisionDag decisionDag,
                BoundExpression loweredSwitchGoverningExpression,
                ArrayBuilder<BoundStatement> result,
                out BoundExpression savedInputExpression)
            {
                // Note that a when-clause can contain an assignment to a
                // pattern variable declared in a different when-clause (e.g. in the same section, or
                // in a different section via the use of a local function), so we need to analyze all
                // of the when clauses to see if they are all simple enough to conclude that they do
                // not mutate pattern variables.
                var mightAssignWalker = new WhenClauseMightAssignWalker();
                bool canShareTemps =
                    !decisionDag.TopologicallySortedNodes
                    .Any(node => node is BoundWhenDecisionDagNode w && mightAssignWalker.MightAssignSomething(w.WhenExpression));

                if (canShareTemps)
                {
                    decisionDag = ShareTempsAndEvaluateInput(loweredSwitchGoverningExpression, decisionDag, expr => result.Add(_factory.ExpressionStatement(expr)), out savedInputExpression);
                }
                else
                {
                    // assign the input expression to its temp.
                    BoundExpression inputTemp = _tempAllocator.GetTemp(BoundDagTemp.ForOriginalInput(loweredSwitchGoverningExpression));
                    Debug.Assert(inputTemp != loweredSwitchGoverningExpression);
                    result.Add(_factory.Assignment(inputTemp, loweredSwitchGoverningExpression));
                    savedInputExpression = inputTemp;
                }

                // In a switch statement, there is a hidden sequence point after evaluating the input at the start of
                // the code to handle the decision dag. This is necessary so that jumps back from a `when` clause into
                // the decision dag do not appear to jump back up to the enclosing construct.
                if (IsSwitchStatement)
                    result.Add(_factory.HiddenSequencePoint());

                return decisionDag;
            }

            /// <summary>
            /// Lower the given nodes into _loweredDecisionDag. Should only be called once per instance of this.
            /// </summary>
            protected (ImmutableArray<BoundStatement> loweredDag, ImmutableDictionary<SyntaxNode, ImmutableArray<BoundStatement>> switchSections) LowerDecisionDag(BoundDecisionDag decisionDag)
            {
                Debug.Assert(this._loweredDecisionDag.IsEmpty());
                ComputeLabelSet(decisionDag);
                LowerDecisionDagCore(decisionDag);
                ImmutableArray<BoundStatement> loweredDag = _loweredDecisionDag.ToImmutableAndFree();
                var switchSections = _switchArms.ToImmutableDictionary(kv => kv.Key, kv => kv.Value.ToImmutableAndFree());
                _switchArms.Clear();
                return (loweredDag, switchSections);
            }

            private void LowerDecisionDagCore(BoundDecisionDag decisionDag)
            {
                ImmutableArray<BoundDecisionDagNode> sortedNodes = decisionDag.TopologicallySortedNodes;
                var firstNode = sortedNodes[0];
                switch (firstNode)
                {
                    case BoundWhenDecisionDagNode _:
                    case BoundLeafDecisionDagNode _:
                        // If the first node is a leaf or when clause rather than the code for the
                        // lowered decision dag, jump there to start.
                        _loweredDecisionDag.Add(_factory.Goto(GetDagNodeLabel(firstNode)));
                        break;
                }

                // Code for each when clause goes in the separate code section for its switch section.
                foreach (BoundDecisionDagNode node in sortedNodes)
                {
                    if (node is BoundWhenDecisionDagNode w)
                    {
                        LowerWhenClause(w);
                    }
                }

                ImmutableArray<BoundDecisionDagNode> nodesToLower = sortedNodes.WhereAsArray(n => n.Kind != BoundKind.WhenDecisionDagNode && n.Kind != BoundKind.LeafDecisionDagNode);
                var loweredNodes = PooledHashSet<BoundDecisionDagNode>.GetInstance();
                for (int i = 0, length = nodesToLower.Length; i < length; i++)
                {
                    BoundDecisionDagNode node = nodesToLower[i];
                    if (loweredNodes.Contains(node))
                    {
                        Debug.Assert(!_dagNodeLabels.TryGetValue(node, out _));
                        continue;
                    }

                    if (_dagNodeLabels.TryGetValue(node, out LabelSymbol label))
                    {
                        _loweredDecisionDag.Add(_factory.Label(label));
                    }

                    // If we can generate an IL switch instruction, do so
                    if (GenerateSwitchDispatch(node, loweredNodes))
                    {
                        continue;
                    }

                    // If we can generate a type test and cast more efficiently as an `is` followed by a null check, do so
                    if (GenerateTypeTestAndCast(node, loweredNodes, nodesToLower, i))
                    {
                        continue;
                    }

                    // We pass the node that will follow so we can permit a test to fall through if appropriate
                    BoundDecisionDagNode nextNode = ((i + 1) < length) ? nodesToLower[i + 1] : null;
                    if (nextNode != null && loweredNodes.Contains(nextNode))
                    {
                        nextNode = null;
                    }

                    LowerDecisionDagNode(node, nextNode);
                }

                loweredNodes.Free();
            }

            /// <summary>
            /// If we have a type test followed by a cast to that type, and the types are reference types,
            /// then we can replace the pair of them by a conversion using `as` and a null check.
            /// </summary>
            /// <returns>true if we generated code for the test</returns>
            private bool GenerateTypeTestAndCast(
                BoundDecisionDagNode node,
                HashSet<BoundDecisionDagNode> loweredNodes,
                ImmutableArray<BoundDecisionDagNode> nodesToLower,
                int indexOfNode)
            {
                Debug.Assert(node == nodesToLower[indexOfNode]);
                if (node is BoundTestDecisionDagNode testNode &&
                    testNode.WhenTrue is BoundEvaluationDecisionDagNode evaluationNode &&
                    TryLowerTypeTestAndCast(testNode.Test, evaluationNode.Evaluation, out BoundExpression sideEffect, out BoundExpression test)
                    )
                {
                    var whenTrue = evaluationNode.Next;
                    var whenFalse = testNode.WhenFalse;
                    bool canEliminateEvaluationNode = !this._dagNodeLabels.ContainsKey(evaluationNode);

                    if (canEliminateEvaluationNode)
                        loweredNodes.Add(evaluationNode);

                    var nextNode =
                        (indexOfNode + 2 < nodesToLower.Length) &&
                        canEliminateEvaluationNode &&
                        nodesToLower[indexOfNode + 1] == evaluationNode &&
                        !loweredNodes.Contains(nodesToLower[indexOfNode + 2]) ? nodesToLower[indexOfNode + 2] : null;

                    _loweredDecisionDag.Add(_factory.ExpressionStatement(sideEffect));
                    GenerateTest(test, whenTrue, whenFalse, nextNode);
                    return true;
                }

                return false;
            }

            private void GenerateTest(BoundExpression test, BoundDecisionDagNode whenTrue, BoundDecisionDagNode whenFalse, BoundDecisionDagNode nextNode)
            {
                // Because we have already "optimized" away tests for a constant switch expression, the test should be nontrivial.
                _factory.Syntax = test.Syntax;
                Debug.Assert(test != null);

                if (nextNode == whenFalse)
                {
                    _loweredDecisionDag.Add(_factory.ConditionalGoto(test, GetDagNodeLabel(whenTrue), jumpIfTrue: true));
                    // fall through to false path
                }
                else if (nextNode == whenTrue)
                {
                    _loweredDecisionDag.Add(_factory.ConditionalGoto(test, GetDagNodeLabel(whenFalse), jumpIfTrue: false));
                    // fall through to true path
                }
                else
                {
                    _loweredDecisionDag.Add(_factory.ConditionalGoto(test, GetDagNodeLabel(whenTrue), jumpIfTrue: true));
                    _loweredDecisionDag.Add(_factory.Goto(GetDagNodeLabel(whenFalse)));
                }
            }

            /// <summary>
            /// Generate a switch dispatch for a contiguous sequence of dag nodes if applicable.
            /// Returns true if it was applicable.
            /// </summary>
            private bool GenerateSwitchDispatch(BoundDecisionDagNode node, HashSet<BoundDecisionDagNode> loweredNodes)
            {
                Debug.Assert(!loweredNodes.Contains(node));

                // We only generate a switch dispatch if we have two or more value tests in a row
                if (!(node is BoundTestDecisionDagNode firstTestNode &&
                     firstTestNode.Test is BoundDagValueTest firstTest &&
                     firstTestNode.WhenFalse is BoundTestDecisionDagNode whenFalse &&
                     whenFalse.Test is BoundDagValueTest secondTest &&
                     !loweredNodes.Contains(whenFalse) &&
                     !this._dagNodeLabels.ContainsKey(whenFalse) &&
                     firstTest.Input == secondTest.Input &&
                     firstTest.Input.Type.IsValidV6SwitchGoverningType()))
                {
                    // https://github.com/dotnet/roslyn/issues/12509 Could optimize float, double, decimal value switches. For now use if-then-else
                    return false;
                }

                // Gather up the (value, label) pairs, starting with the first one
                var cases = ArrayBuilder<(ConstantValue value, LabelSymbol label)>.GetInstance();
                cases.Add((value: firstTest.Value, label: GetDagNodeLabel(firstTestNode.WhenTrue)));

                BoundTestDecisionDagNode previous = firstTestNode;
                while (previous.WhenFalse is BoundTestDecisionDagNode p &&
                    p.Test is BoundDagValueTest vd &&
                    vd.Input == firstTest.Input &&
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
                if (firstTest.Input.Type.SpecialType == SpecialType.System_String)
                {
                    EnsureStringHashFunction(cases.Count, node.Syntax);
                    stringEquality = _localRewriter.UnsafeGetSpecialTypeMethod(node.Syntax, SpecialMember.System_String__op_Equality);
                }

                LabelSymbol defaultLabel = GetDagNodeLabel(previous.WhenFalse);
                var dispatch = new BoundSwitchDispatch(
                    node.Syntax, _tempAllocator.GetTemp(firstTest.Input), cases.ToImmutableAndFree(), defaultLabel, stringEquality);
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
                    // we're not generating code, so we don't need the hash function
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

            private void LowerWhenClause(BoundWhenDecisionDagNode whenClause)
            {
                // This node is used even when there is no when clause, to record bindings. In the case that there
                // is no when clause, whenClause.WhenExpression and whenClause.WhenFalse are null, and the syntax for this
                // node is the case clause.

                // We need to assign the pattern variables in the code where they are in scope, so we produce a branch
                // to the section where they are in scope and evaluate the when clause there.
                var whenTrue = (BoundLeafDecisionDagNode)whenClause.WhenTrue;
                LabelSymbol labelToSectionScope = GetDagNodeLabel(whenClause);

                // We need the section syntax to get the section builder from the map. Unfortunately this is a bit awkward
                SyntaxNode sectionSyntax = whenClause.Syntax is SwitchLabelSyntax l ? l.Parent : whenClause.Syntax;
                bool foundSectionBuilder = _switchArms.TryGetValue(sectionSyntax, out ArrayBuilder<BoundStatement> sectionBuilder);
                Debug.Assert(foundSectionBuilder);
                sectionBuilder.Add(_factory.Label(labelToSectionScope));
                foreach (BoundPatternBinding binding in whenClause.Bindings)
                {
                    BoundExpression left = _localRewriter.VisitExpression(binding.VariableAccess);
                    // Since a switch does not add variables to the enclosing scope, the pattern variables
                    // are locals even in a script and rewriting them should have no effect.
                    Debug.Assert(left.Kind == BoundKind.Local && left == binding.VariableAccess);
                    BoundExpression right = _tempAllocator.GetTemp(binding.TempContainingValue);
                    if (left != right)
                    {
                        sectionBuilder.Add(_factory.Assignment(left, right));
                    }
                }

                var whenFalse = whenClause.WhenFalse;
                var trueLabel = GetDagNodeLabel(whenTrue);
                if (whenClause.WhenExpression != null && whenClause.WhenExpression.ConstantValue != ConstantValue.True)
                {
                    _factory.Syntax = whenClause.Syntax;
                    BoundStatement conditionalGoto = _factory.ConditionalGoto(_localRewriter.VisitExpression(whenClause.WhenExpression), trueLabel, jumpIfTrue: true);

                    // Only add instrumentation (such as a sequence point) if the node is not compiler-generated.
                    if (IsSwitchStatement && !whenClause.WhenExpression.WasCompilerGenerated && _localRewriter.Instrument)
                    {
                        conditionalGoto = _localRewriter._instrumenter.InstrumentSwitchWhenClauseConditionalGotoBody(whenClause.WhenExpression, conditionalGoto);
                    }

                    sectionBuilder.Add(conditionalGoto);

                    Debug.Assert(whenFalse != null);

                    // We hide the jump back into the decision dag, as it is not logically part of the when clause
                    BoundStatement jump = _factory.Goto(GetDagNodeLabel(whenFalse));
                    sectionBuilder.Add(IsSwitchStatement ? _factory.HiddenSequencePoint(jump) : jump);
                }
                else
                {
                    Debug.Assert(whenFalse == null);
                    sectionBuilder.Add(_factory.Goto(trueLabel));
                }
            }

            /// <summary>
            /// Translate the decision dag for node, given that it will be followed by the translation for nextNode.
            /// </summary>
            private void LowerDecisionDagNode(BoundDecisionDagNode node, BoundDecisionDagNode nextNode)
            {
                _factory.Syntax = node.Syntax;
                switch (node)
                {
                    case BoundEvaluationDecisionDagNode evaluationNode:
                        {
                            BoundExpression sideEffect = LowerEvaluation(evaluationNode.Evaluation);
                            Debug.Assert(sideEffect != null);
                            _loweredDecisionDag.Add(_factory.ExpressionStatement(sideEffect));

                            // We add a hidden sequence point after the evaluation's side-effect, which may be a call out
                            // to user code such as `Deconstruct` or a property get, to permit edit-and-continue to
                            // synchronize on changes.
                            if (IsSwitchStatement)
                                _loweredDecisionDag.Add(_factory.HiddenSequencePoint());

                            if (nextNode != evaluationNode.Next)
                            {
                                // We only need a goto if we would not otherwise fall through to the desired state
                                _loweredDecisionDag.Add(_factory.Goto(GetDagNodeLabel(evaluationNode.Next)));
                            }
                        }

                        break;

                    case BoundTestDecisionDagNode testNode:
                        {
                            BoundExpression test = base.LowerTest(testNode.Test);
                            GenerateTest(test, testNode.WhenTrue, testNode.WhenFalse, nextNode);
                        }

                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(node.Kind);
                }
            }
        }
    }
}
