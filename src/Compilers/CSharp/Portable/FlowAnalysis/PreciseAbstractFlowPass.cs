// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class PreciseAbstractFlowPass<LocalState> : BoundTreeVisitor
        where LocalState : AbstractFlowPass<LocalState>.AbstractLocalState
    {
        protected int _recursionDepth;

        /// <summary>
        /// The compilation in which the analysis is taking place.  This is needed to determine which
        /// conditional methods will be compiled and which will be omitted.
        /// </summary>
        protected readonly CSharpCompilation compilation;

        /// <summary>
        /// The method whose body is being analyzed, or the field whose initializer is being analyzed.
        /// It is used for
        /// references to method parameters. Thus, 'member' should not be used directly, but
        /// 'MethodParameters', 'MethodThisParameter' and 'AnalyzeOutParameters(...)' should be used
        /// instead.
        /// </summary>
        private readonly Symbol _member;

        /// <summary>
        /// The bound node of the method or initializer being analyzed.
        /// </summary>
        protected readonly BoundNode methodMainNode;

        /// <summary>
        /// The flow analysis state at each label, computed by merging the state from branches to
        /// that label with the state when we fall into the label.  Entries are created when the
        /// label is encountered.  One case deserves special attention: when the destination of the
        /// branch is a label earlier in the code, it is possible (though rarely occurs in practice)
        /// that we are changing the state at a label that we've already analyzed. In that case we
        /// run another pass of the analysis to allow those changes to propagate. This repeats until
        /// no further changes to the state of these labels occurs.  This can result in quadratic
        /// performance in unlikely but possible code such as this: "int x; if (cond) goto l1; x =
        /// 3; l5: print x; l4: goto l5; l3: goto l4; l2: goto l3; l1: goto l2;"
        /// </summary>
        private readonly PooledDictionary<LabelSymbol, LocalState> _labels;

        /// <summary>
        /// Set to true after an analysis scan if the analysis was incomplete due to a backward
        /// "goto" branch changing some analysis result.  In this case the caller scans again (until
        /// this is false). Since the analysis proceeds by monotonically changing the state computed
        /// at each label, this must terminate.
        /// </summary>
        internal bool backwardBranchChanged;

        /// <summary>
        /// See property PendingBranches
        /// </summary>
        private ArrayBuilder<PendingBranch> _pendingBranches;

        /// <summary>
        /// All of the labels seen so far in this forward scan of the body
        /// </summary>
        private PooledHashSet<BoundStatement> _labelsSeen;

        /// <summary>
        /// If we are tracking exceptions, then by convention the first entry in the pending branches
        /// buffer contains a summary of the states that can arise from exceptions.
        /// </summary>
        private readonly bool _trackExceptions;

        /// <summary>
        /// Pending escapes generated in the current scope (or more deeply nested scopes). When jump
        /// statements (goto, break, continue, return) are processed, they are placed in the
        /// pendingBranches buffer to be processed later by the code handling the destination
        /// statement. As a special case, the processing of try-finally statements might modify the
        /// contents of the pendingBranches buffer to take into account the behavior of
        /// "intervening" finally clauses.
        /// </summary>
        protected ArrayBuilder<PendingBranch> PendingBranches
        {
            get
            {
                return _pendingBranches;
            }
        }

        /// <summary>
        /// The definite assignment and/or reachability state at the point currently being analyzed.
        /// </summary>
        protected LocalState State;
        protected LocalState StateWhenTrue;
        protected LocalState StateWhenFalse;
        protected bool IsConditionalState;

        protected void SetConditionalState(LocalState whenTrue, LocalState whenFalse)
        {
            IsConditionalState = true;
            State = default(LocalState);
            StateWhenTrue = whenTrue;
            StateWhenFalse = whenFalse;
        }

        protected void SetState(LocalState newState)
        {
            StateWhenTrue = StateWhenFalse = default(LocalState);
            IsConditionalState = false;
            State = newState;
        }

        protected void Split()
        {
            Debug.Assert(!_trackExceptions || _pendingBranches[0].Branch == null);
            if (!IsConditionalState)
            {
                SetConditionalState(State, State.Clone());
            }
        }

        protected void Unsplit()
        {
            Debug.Assert(!_trackExceptions || _pendingBranches[0].Branch == null);
            if (IsConditionalState)
            {
                IntersectWith(ref StateWhenTrue, ref StateWhenFalse);
                SetState(StateWhenTrue);
            }
        }

        /// <summary>
        /// Where all diagnostics are deposited.
        /// </summary>
        protected DiagnosticBag Diagnostics { get; }

        #region Region
        // For region analysis, we maintain some extra data.
        protected enum RegionPlace { Before, Inside, After };
        protected RegionPlace regionPlace; // tells whether we are currently analyzing code before, during, or after the region
        protected readonly BoundNode firstInRegion, lastInRegion;
        private readonly bool _trackRegions;

        /// <summary>
        /// A cache of the state at the backward branch point of each loop.  This is not needed
        /// during normal flow analysis, but is needed for DataFlowsOut region analysis.
        /// </summary>
        private readonly Dictionary<BoundLoopStatement, LocalState> _loopHeadState;
        #endregion Region

        protected PreciseAbstractFlowPass(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            BoundNode firstInRegion = null,
            BoundNode lastInRegion = null,
            bool trackRegions = false,
            bool trackExceptions = false)
        {
            Debug.Assert(node != null);
            var equalsValue = node as BoundEqualsValue;
            if (equalsValue != null)
            {
                node = equalsValue.Value;
            }

            if (firstInRegion != null && lastInRegion != null)
            {
                trackRegions = true;
            }

            if (trackRegions)
            {
                Debug.Assert(firstInRegion != null);
                Debug.Assert(lastInRegion != null);
                int startLocation = firstInRegion.Syntax.SpanStart;
                int endLocation = lastInRegion.Syntax.Span.End;
                int length = endLocation - startLocation;
                Debug.Assert(length > 0, "last comes before first");
                this.RegionSpan = new TextSpan(startLocation, length);
            }

            _pendingBranches = ArrayBuilder<PendingBranch>.GetInstance();
            _labelsSeen = PooledHashSet<BoundStatement>.GetInstance();
            _labels = PooledDictionary<LabelSymbol, LocalState>.GetInstance();
            this.Diagnostics = DiagnosticBag.GetInstance();
            this.compilation = compilation;
            _member = member;
            this.methodMainNode = node;
            this.firstInRegion = firstInRegion;
            this.lastInRegion = lastInRegion;
            _loopHeadState = new Dictionary<BoundLoopStatement, LocalState>(ReferenceEqualityComparer.Instance);
            _trackRegions = trackRegions;
            _trackExceptions = trackExceptions;
        }

        protected abstract string Dump(LocalState state);

        protected string Dump()
        {
            return Dump(this.State);
        }

#if DEBUG
        protected string DumpLabels()
        {
            StringBuilder result = new StringBuilder();
            result.Append("Labels{");
            bool first = true;
            foreach (var key in _labels.Keys)
            {
                if (!first) result.Append(", ");
                string name = key.Name;
                if (string.IsNullOrEmpty(name)) name = "<Label>" + key.GetHashCode();
                result.Append(name).Append(": ").Append(this.Dump(_labels[key]));
                first = false;
            }
            result.Append("}");
            return result.ToString();
        }
#endif

        /// <summary>
        /// Subclasses may override EnterRegion to perform any actions at the entry to the region.
        /// </summary>
        protected virtual void EnterRegion()
        {
            Debug.Assert(this.regionPlace == RegionPlace.Before);
            this.regionPlace = RegionPlace.Inside;
        }

        /// <summary>
        /// Subclasses may override LeaveRegion to perform any action at the end of the region.
        /// </summary>
        protected virtual void LeaveRegion()
        {
            Debug.Assert(IsInside);
            this.regionPlace = RegionPlace.After;
        }

        protected readonly TextSpan RegionSpan;

        protected bool RegionContains(TextSpan span)
        {
            // TODO: There are no scenarios involving a zero-length span
            // currently. If the assert fails, add a corresponding test.
            Debug.Assert(span.Length > 0);
            if (span.Length == 0)
            {
                return RegionSpan.Contains(span.Start);
            }
            return RegionSpan.Contains(span);
        }

        protected bool IsInside
        {
            get
            {
                return regionPlace == RegionPlace.Inside;
            }
        }

        public override BoundNode Visit(BoundNode node)
        {
            return VisitAlways(node);
        }

        protected BoundNode VisitAlways(BoundNode node)
        {
            BoundNode result = null;

            // We scan even expressions, because we must process lambdas contained within them.
            if (node != null)
            {
                if (_trackRegions)
                {
                    if (node == this.firstInRegion && this.regionPlace == RegionPlace.Before) EnterRegion();
                    result = VisitWithStackGuard(node);
                    if (node == this.lastInRegion && this.regionPlace == RegionPlace.Inside) LeaveRegion();
                }
                else
                {
                    result = VisitWithStackGuard(node);
                }
            }

            return result;
        }

        private BoundNode VisitWithStackGuard(BoundNode node)
        {
            var expression = node as BoundExpression;
            if (expression != null)
            {
                return VisitExpressionWithStackGuard(ref _recursionDepth, expression);
            }

            return base.Visit(node);
        }

        protected override BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            return (BoundExpression)base.Visit(node);
        }

        protected override bool ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()
        {
            return false; // just let the original exception to bubble up.
        }

        /// <summary>
        /// A pending branch.  There are created for a return, break, continue, goto statement,
        /// yield return, yield break, await expression, and if PreciseAbstractFlowPass.trackExceptions
        /// is true for other
        /// constructs that can cause an exception to be raised such as a throw statement or method
        /// invocation.
        /// The idea is that we don't know if the branch will eventually reach its destination
        /// because of an intervening finally block that cannot complete normally.  So we store them
        /// up and handle them as we complete processing each construct.  At the end of a block, if
        /// there are any pending branches to a label in that block we process the branch.  Otherwise
        /// we relay it up to the enclosing construct as a pending branch of the enclosing
        /// construct.
        /// </summary>
        internal class PendingBranch
        {
            public readonly BoundNode Branch;
            public LocalState State;
            public LabelSymbol Label
            {
                get
                {
                    if (Branch == null) return null;
                    switch (Branch.Kind)
                    {
                        case BoundKind.GotoStatement: return ((BoundGotoStatement)Branch).Label;
                        case BoundKind.ConditionalGoto: return ((BoundConditionalGoto)Branch).Label;
                        case BoundKind.BreakStatement: return ((BoundBreakStatement)Branch).Label;
                        case BoundKind.ContinueStatement: return ((BoundContinueStatement)Branch).Label;
                        default: return null;
                    }
                }
            }

            public PendingBranch(BoundNode branch, LocalState state)
            {
                this.Branch = branch;
                this.State = state;
            }
        }

        abstract protected LocalState ReachableState();
        abstract protected LocalState UnreachableState();

        /// <summary>
        /// Perform a single pass of flow analysis.  Note that after this pass,
        /// this.backwardBranchChanged indicates if a further pass is required.
        /// </summary>
        protected virtual ImmutableArray<PendingBranch> Scan(ref bool badRegion)
        {
            var oldPending = SavePending();
            Visit(methodMainNode);
            RestorePending(oldPending);
            if (_trackRegions && regionPlace != RegionPlace.After) badRegion = true;
            ImmutableArray<PendingBranch> result = RemoveReturns();
            return result;
        }

        protected ImmutableArray<PendingBranch> Analyze(ref bool badRegion)
        {
            ImmutableArray<PendingBranch> returns;
            do
            {
                // the entry point of a method is assumed reachable
                regionPlace = RegionPlace.Before;
                this.State = ReachableState();
                _pendingBranches.Clear();
                if (_trackExceptions) _pendingBranches.Add(new PendingBranch(null, ReachableState()));
                this.backwardBranchChanged = false;
                this.Diagnostics.Clear();
                returns = this.Scan(ref badRegion);
            }
            while (this.backwardBranchChanged);

            return returns;
        }

        protected virtual void Free()
        {
            this.Diagnostics.Free();
            _pendingBranches.Free();
            _labelsSeen.Free();
            _labels.Free();
        }

        /// <summary>
        /// If a method is currently being analyzed returns its parameters, returns an empty array
        /// otherwise.
        /// </summary>
        protected ImmutableArray<ParameterSymbol> MethodParameters
        {
            get
            {
                var method = _member as MethodSymbol;
                return (object)method == null ? ImmutableArray<ParameterSymbol>.Empty : method.Parameters;
            }
        }

        /// <summary>
        /// If a method is currently being analyzed returns its 'this' parameter, returns null
        /// otherwise.
        /// </summary>
        protected ParameterSymbol MethodThisParameter
        {
            get
            {
                var method = _member as MethodSymbol;
                return (object)method == null ? null : method.ThisParameter;
            }
        }

        /// <summary>
        /// Specifies whether or not method's out parameters should be analyzed. If there's more
        /// than one location in the method being analyzed, then the method is partial and we prefer
        /// to report an out parameter in partial method error.
        /// </summary>
        /// <param name="location">location to be used</param>
        /// <returns>true if the out parameters of the method should be analyzed</returns>
        protected bool ShouldAnalyzeOutParameters(out Location location)
        {
            var method = _member as MethodSymbol;
            if ((object)method == null || method.Locations.Length != 1)
            {
                location = null;
                return false;
            }
            else
            {
                location = method.Locations[0];
                return true;
            }
        }

        /// <summary>
        /// Return the flow analysis state associated with a label.
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        protected virtual LocalState LabelState(LabelSymbol label)
        {
            LocalState result;
            if (_labels.TryGetValue(label, out result))
            {
                return result;
            }

            result = UnreachableState();
            _labels.Add(label, result);
            return result;
        }

        /// <summary>
        /// Return to the caller the set of pending return statements.
        /// </summary>
        /// <returns></returns>
        protected virtual ImmutableArray<PendingBranch> RemoveReturns()
        {
            ImmutableArray<PendingBranch> result;
            if (_trackExceptions)
            {
                // when we are tracking exceptions, we use pendingBranches[0] to
                // track exception states.
                result = _pendingBranches
                        .Where(b => b.Branch != null)
                        .AsImmutableOrNull();

                var oldExceptions = _pendingBranches[0];
                Debug.Assert(oldExceptions.Branch == null);
                _pendingBranches.Clear();
                _pendingBranches.Add(oldExceptions);
            }
            else
            {
                result = _pendingBranches.ToImmutable();
                _pendingBranches.Clear();
            }

            // The caller should have handled and cleared labelsSeen.
            Debug.Assert(_labelsSeen.Count == 0);
            return result;
        }

        /// <summary>
        /// Set the current state to one that indicates that it is unreachable.
        /// </summary>
        protected void SetUnreachable()
        {
            this.State = UnreachableState();
        }

        protected void VisitLvalue(BoundExpression node)
        {
            if (_trackRegions && node == this.firstInRegion && this.regionPlace == RegionPlace.Before) EnterRegion();
            switch (node.Kind)
            {
                case BoundKind.Parameter:
                    VisitLvalueParameter((BoundParameter)node);
                    break;

                case BoundKind.Local:
                case BoundKind.ThisReference:
                case BoundKind.BaseReference:
                    // no need for it to be previously assigned: it is on the left.
                    break;

                case BoundKind.PropertyAccess:
                    var access = (BoundPropertyAccess)node;

                    if (Binder.AccessingAutopropertyFromConstructor(access, _member))
                    {
                        var backingField = (access.PropertySymbol as SourcePropertySymbol)?.BackingField;
                        if (backingField != null)
                        {
                            VisitFieldAccessInternal(access.ReceiverOpt, backingField);
                            break;
                        }
                    }

                    goto default;

                case BoundKind.FieldAccess:
                    {
                        BoundFieldAccess node1 = (BoundFieldAccess)node;
                        VisitFieldAccessInternal(node1.ReceiverOpt, node1.FieldSymbol);
                        break;
                    }

                case BoundKind.EventAccess:
                    {
                        BoundEventAccess node1 = (BoundEventAccess)node;
                        VisitFieldAccessInternal(node1.ReceiverOpt, node1.EventSymbol.AssociatedField);
                        break;
                    }

                default:
                    VisitRvalue(node);
                    break;
            }

            if (_trackRegions && node == this.lastInRegion && this.regionPlace == RegionPlace.Inside) LeaveRegion();
        }

        /// <summary>
        /// Visit a boolean condition expression.
        /// </summary>
        /// <param name="node"></param>
        protected void VisitCondition(BoundExpression node)
        {
            Visit(node);
            AdjustConditionalState(node);
        }

        private void AdjustConditionalState(BoundExpression node)
        {
            if (IsConstantTrue(node))
            {
                Unsplit();
                SetConditionalState(this.State, UnreachableState());
            }
            else if (IsConstantFalse(node))
            {
                Unsplit();
                SetConditionalState(UnreachableState(), this.State);
            }
            else if ((object)node.Type == null || node.Type.SpecialType != SpecialType.System_Boolean)
            {
                // a dynamic type or a type with operator true/false
                Unsplit();
            }

            Split();
        }

        /// <summary>
        /// Visit a general expression, where we will only need to determine if variables are
        /// assigned (or not). That is, we will not be needing AssignedWhenTrue and
        /// AssignedWhenFalse.
        /// </summary>
        /// <param name="node"></param>
        protected BoundNode VisitRvalue(BoundExpression node)
        {
            Debug.Assert(!_trackExceptions || this.PendingBranches.Count > 0 && this.PendingBranches[0].Branch == null);
            var result = Visit(node);
            Unsplit();
            return result;
        }

        /// <summary>
        /// Visit a statement.
        /// </summary>
        [DebuggerHidden]
        protected virtual void VisitStatement(BoundStatement statement)
        {
            Debug.Assert(!_trackExceptions || this.PendingBranches.Count > 0 && this.PendingBranches[0].Branch == null);
            Visit(statement);
        }

        private static bool IsConstantTrue(BoundExpression node)
        {
            return node.ConstantValue == ConstantValue.True;
        }

        private static bool IsConstantFalse(BoundExpression node)
        {
            return node.ConstantValue == ConstantValue.False;
        }

        private static bool IsConstantNull(BoundExpression node)
        {
            return node.ConstantValue == ConstantValue.Null;
        }

        /// <summary>
        /// Called at the point in a loop where the backwards branch would go to.
        /// </summary>
        private void LoopHead(BoundLoopStatement node)
        {
            LocalState previousState;
            if (_loopHeadState.TryGetValue(node, out previousState))
            {
                IntersectWith(ref this.State, ref previousState);
            }

            _loopHeadState[node] = this.State.Clone();
        }

        /// <summary>
        /// Called at the point in a loop where the backward branch is placed.
        /// </summary>
        private void LoopTail(BoundLoopStatement node)
        {
            var oldState = _loopHeadState[node];
            if (IntersectWith(ref oldState, ref this.State))
            {
                _loopHeadState[node] = oldState;
                this.backwardBranchChanged = true;
            }
        }

        /// <summary>
        /// Used to resolve break statements in each statement form that has a break statement
        /// (loops, switch).
        /// </summary>
        private void ResolveBreaks(LocalState breakState, LabelSymbol label)
        {
            var pendingBranches = _pendingBranches;
            var count = pendingBranches.Count;

            if (count != 0)
            {
                int stillPending = 0;
                for (int i = 0; i < count; i++)
                {
                    var pending = pendingBranches[i];
                    if (pending.Label == label)
                    {
                        IntersectWith(ref breakState, ref pending.State);
                    }
                    else
                    {
                        if (stillPending != i)
                        {
                            pendingBranches[stillPending] = pending;
                        }
                        stillPending++;
                    }
                }

                pendingBranches.Clip(stillPending);
            }

            SetState(breakState);
        }

        /// <summary>
        /// Used to resolve continue statements in each statement form that supports it.
        /// </summary>
        private void ResolveContinues(LabelSymbol continueLabel)
        {
            var pendingBranches = _pendingBranches;
            var count = pendingBranches.Count;

            if (count != 0)
            {
                int stillPending = 0;
                for (int i = 0; i < count; i++)
                {
                    var pending = pendingBranches[i];
                    if (pending.Label == continueLabel)
                    {
                        // Technically, nothing in the language specification depends on the state
                        // at the continue label, so we could just discard them instead of merging
                        // the states. In fact, we need not have added continue statements to the
                        // pending jump queue in the first place if we were interested solely in the
                        // flow analysis.  However, region analysis (in support of extract method)
                        // and other forms of more precise analysis
                        // depend on continue statements appearing in the pending branch queue, so
                        // we process them from the queue here.
                        IntersectWith(ref this.State, ref pending.State);
                    }
                    else
                    {
                        if (stillPending != i)
                        {
                            pendingBranches[stillPending] = pending;
                        }
                        stillPending++;
                    }
                }

                pendingBranches.Clip(stillPending);
            }
        }

        /// <summary>
        /// Subclasses override this if they want to take special actions on processing a goto
        /// statement, when both the jump and the label have been located.
        /// </summary>
        protected virtual void NoteBranch(PendingBranch pending, BoundStatement gotoStmt, BoundStatement target)
        {
            target.AssertIsLabeledStatement();
        }

        protected virtual void NotePossibleException(BoundNode node)
        {
            Debug.Assert(_trackExceptions);
            IntersectWith(ref _pendingBranches[0].State, ref this.State);
        }

        /// <summary>
        /// To handle a label, we resolve all branches to that label.  Returns true if the state of
        /// the label changes as a result.
        /// </summary>
        /// <param name="label">Target label</param>
        /// <param name="target">Statement containing the target label</param>
        private bool ResolveBranches(LabelSymbol label, BoundStatement target)
        {
            if (target != null)
            {
                target.AssertIsLabeledStatementWithLabel(label);
            }

            bool labelStateChanged = false;
            var pendingBranches = _pendingBranches;
            var count = pendingBranches.Count;

            if (count != 0)
            {
                int stillPending = 0;
                for (int i = 0; i < count; i++)
                {
                    var pending = pendingBranches[i];
                    if (pending.Label == label)
                    {
                        ResolveBranch(pending, label, target, ref labelStateChanged);
                    }
                    else
                    {
                        if (stillPending != i)
                        {
                            pendingBranches[stillPending] = pending;
                        }
                        stillPending++;
                    }
                }

                pendingBranches.Clip(stillPending);
            }

            return labelStateChanged;
        }

        protected virtual void ResolveBranch(PendingBranch pending, LabelSymbol label, BoundStatement target, ref bool labelStateChanged)
        {
            var state = LabelState(label);
            if (target != null) NoteBranch(pending, (BoundStatement)pending.Branch, target);
            var changed = IntersectWith(ref state, ref pending.State);
            if (changed)
            {
                labelStateChanged = true;
                _labels[label] = state;
            }
        }

        private bool ResolveBranches(BoundLabeledStatement target)
        {
            return ResolveBranches(target.Label, target);
        }

        protected struct SavedPending
        {
            public readonly ArrayBuilder<PendingBranch> PendingBranches;
            public readonly PooledHashSet<BoundStatement> LabelsSeen;

            public SavedPending(ref ArrayBuilder<PendingBranch> pendingBranches, ref PooledHashSet<BoundStatement> labelsSeen)
            {
                this.PendingBranches = pendingBranches;
                this.LabelsSeen = labelsSeen;
                pendingBranches = ArrayBuilder<PendingBranch>.GetInstance();
                labelsSeen = PooledHashSet<BoundStatement>.GetInstance();
            }
        }

        /// <summary>
        /// Since branches cannot branch into constructs, only out, we save the pending branches
        /// when visiting more nested constructs.  When tracking exceptions, we store the current
        /// state as the exception state for the following code.
        /// </summary>
        protected SavedPending SavePending()
        {
            var result = new SavedPending(ref _pendingBranches, ref _labelsSeen);
            if (_trackExceptions)
            {
                _pendingBranches.Add(new PendingBranch(null, this.State.Clone()));
            }

            return result;
        }

        /// <summary>
        /// We use this to restore the old set of pending branches after visiting a construct that contains nested statements.
        /// </summary>
        /// <param name="oldPending">The old pending branches, which are to be merged with the current ones</param>
        protected void RestorePending(SavedPending oldPending)
        {
            foreach (var node in _labelsSeen)
            {
                switch (node.Kind)
                {
                    case BoundKind.LabeledStatement:
                        {
                            var label = (BoundLabeledStatement)node;
                            backwardBranchChanged |= ResolveBranches(label.Label, label);
                        }
                        break;
                    case BoundKind.LabelStatement:
                        {
                            var label = (BoundLabelStatement)node;
                            backwardBranchChanged |= ResolveBranches(label.Label, label);
                        }
                        break;
                    case BoundKind.SwitchSection:
                        {
                            var sec = (BoundSwitchSection)node;
                            foreach (var label in sec.BoundSwitchLabels)
                            {
                                backwardBranchChanged |= ResolveBranches(label.Label, sec);
                            }
                        }
                        break;
                    default:
                        // there are no other kinds of labels
                        throw ExceptionUtilities.UnexpectedValue(node.Kind);
                }
            }

            int i = 0;
            int n = _pendingBranches.Count;
            if (_trackExceptions)
            {
                Debug.Assert(oldPending.PendingBranches[0].Branch == null);
                Debug.Assert(this.PendingBranches[0].Branch == null);
                this.IntersectWith(ref oldPending.PendingBranches[0].State, ref this.PendingBranches[0].State);
                i++;
            }

            for (; i < n; i++)
            {
                oldPending.PendingBranches.Add(this.PendingBranches[i]);
            }
            _pendingBranches.Free();
            _pendingBranches = oldPending.PendingBranches;

            // We only use SavePending/RestorePending when there could be no branch into the region between them.
            // So there is no need to save the labels seen between the calls.  If there were such a need, we would
            // do "this.labelsSeen.UnionWith(oldPending.LabelsSeen);" instead of the following assignment
            _labelsSeen.Free();
            _labelsSeen = oldPending.LabelsSeen;
        }

        #region visitors

        /// <summary>
        /// Since each language construct must be handled according to the rules of the language specification,
        /// the default visitor reports that the construct for the node is not implemented in the compiler.
        /// </summary>
        public override BoundNode DefaultVisit(BoundNode node)
        {
            Debug.Assert(false, $"Should Visit{node.Kind} be overridden in {this.GetType().Name}?");
            return null;
        }

        public override BoundNode VisitAttribute(BoundAttribute node)
        {
            // No flow analysis is ever done in attributes (or their arguments).
            return null;
        }

        public override BoundNode VisitDynamicObjectCreationExpression(BoundDynamicObjectCreationExpression node)
        {
            VisitArguments(node.Arguments, node.ArgumentRefKindsOpt, null);
            VisitRvalue(node.InitializerExpressionOpt);
            if (_trackExceptions) NotePossibleException(node);
            return null;
        }

        public override BoundNode VisitDynamicIndexerAccess(BoundDynamicIndexerAccess node)
        {
            VisitRvalue(node.ReceiverOpt);
            VisitArguments(node.Arguments, node.ArgumentRefKindsOpt, null);
            if (_trackExceptions) NotePossibleException(node);
            return null;
        }

        public override BoundNode VisitDynamicMemberAccess(BoundDynamicMemberAccess node)
        {
            VisitRvalue(node.Receiver);
            if (_trackExceptions) NotePossibleException(node);
            return null;
        }

        public override BoundNode VisitDynamicInvocation(BoundDynamicInvocation node)
        {
            VisitRvalue(node.Expression);
            VisitArguments(node.Arguments, node.ArgumentRefKindsOpt, null);
            if (_trackExceptions) NotePossibleException(node);
            return null;
        }

        public override BoundNode VisitInterpolatedString(BoundInterpolatedString node)
        {
            foreach (var expr in node.Parts)
            {
                VisitRvalue(expr);
            }
            return null;
        }

        public override BoundNode VisitStringInsert(BoundStringInsert node)
        {
            VisitRvalue(node.Value);
            if (node.Alignment != null) VisitRvalue(node.Alignment);
            if (node.Format != null) VisitRvalue(node.Format);
            return null;
        }

        public override BoundNode VisitArgList(BoundArgList node)
        {
            // The "__arglist" expression that is legal inside a varargs method has no 
            // effect on flow analysis and it has no children.
            return null;
        }

        public override BoundNode VisitArgListOperator(BoundArgListOperator node)
        {
            // When we have M(__arglist(x, y, z)) we must visit x, y and z.
            VisitArguments(node.Arguments, node.ArgumentRefKindsOpt, null);
            return null;
        }

        public override BoundNode VisitRefTypeOperator(BoundRefTypeOperator node)
        {
            VisitRvalue(node.Operand);
            return null;
        }

        public override BoundNode VisitMakeRefOperator(BoundMakeRefOperator node)
        {
            // Note that we require that the variable whose reference we are taking
            // has been initialized; it is similar to passing the variable as a ref parameter.

            VisitRvalue(node.Operand);
            return null;
        }

        public override BoundNode VisitRefValueOperator(BoundRefValueOperator node)
        {
            VisitRvalue(node.Operand);
            return null;
        }

        public override BoundNode VisitGlobalStatementInitializer(BoundGlobalStatementInitializer node)
        {
            VisitStatement(node.Statement);
            return null;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            if (_trackExceptions) NotePossibleException(node);

            // Control-flow analysis does NOT dive into a lambda, while data-flow analysis does.
            return null;
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            // Control-flow analysis does NOT dive into a local function, while data-flow analysis does.
            return null;
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            return null;
        }

        public override BoundNode VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            if (node.InitializerOpt != null)
            {
                VisitRvalue(node.InitializerOpt); // analyze the expression
            }
            return null;
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            foreach (var statement in node.Statements)
                VisitStatement(statement);

            return null;
        }

        public override BoundNode VisitFieldInitializer(BoundFieldInitializer node)
        {
            Visit(node.InitialValue);
            return null;
        }

        public override BoundNode VisitExpressionStatement(BoundExpressionStatement node)
        {
            VisitRvalue(node.Expression);
            return null;
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            // If the method being called is a partial method without a definition, or is a conditional method
            // whose condition is not true, then the call has no effect and it is ignored for the purposes of
            // definite assignment analysis.
            bool callsAreOmitted = node.Method.CallsAreOmitted(node.SyntaxTree);
            LocalState savedState = default(LocalState);

            if (callsAreOmitted)
            {
                savedState = this.State.Clone();
                SetUnreachable();
            }

            VisitReceiverBeforeCall(node.ReceiverOpt, node.Method);
            VisitArguments(node.Arguments, node.ArgumentRefKindsOpt, node.Method);
            UpdateStateForCall(node);
            VisitReceiverAfterCall(node.ReceiverOpt, node.Method);

            if (callsAreOmitted)
            {
                this.State = savedState;
            }

            return null;
        }

        protected virtual void UpdateStateForCall(BoundCall node)
        {
            if (_trackExceptions) NotePossibleException(node);
        }

        private void VisitReceiverBeforeCall(BoundExpression receiverOpt, MethodSymbol method)
        {
            if ((object)method == null || method.MethodKind != MethodKind.Constructor)
            {
                VisitRvalue(receiverOpt);
            }
        }

        private void VisitReceiverAfterCall(BoundExpression receiverOpt, MethodSymbol method)
        {
            NamedTypeSymbol containingType;
            if (receiverOpt != null && ((object)method == null || method.MethodKind == MethodKind.Constructor || (object)(containingType = method.ContainingType) != null && !method.IsStatic && !containingType.IsReferenceType && !TypeIsImmutable(containingType)))
            {
                WriteArgument(receiverOpt, (object)method != null && method.MethodKind == MethodKind.Constructor ? RefKind.Out : RefKind.Ref, method);
            }
        }

        /// <summary>
        /// Certain (struct) types are known by the compiler to be immutable.  In these cases calling a method on
        /// the type is known (by flow analysis) not to write the receiver.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static bool TypeIsImmutable(TypeSymbol t)
        {
            switch (t.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_DateTime:
                    return true;
                default:
                    var ont = t.OriginalDefinition as TypeSymbol;
                    return (object)ont != null && ont.SpecialType == SpecialType.System_Nullable_T;
            }
        }

        public override BoundNode VisitIndexerAccess(BoundIndexerAccess node)
        {
            VisitRvalue(node.ReceiverOpt);
            var method = node.Indexer.GetOwnOrInheritedGetMethod() ?? node.Indexer.SetMethod;
            VisitArguments(node.Arguments, node.ArgumentRefKindsOpt, method);
            if (_trackExceptions && (object)method != null) NotePossibleException(node);
            if ((object)method != null) VisitReceiverAfterCall(node.ReceiverOpt, method);
            return null;
        }

        public override BoundNode VisitEventAssignmentOperator(BoundEventAssignmentOperator node)
        {
            VisitRvalue(node.ReceiverOpt);
            VisitRvalue(node.Argument);
            if (_trackExceptions) NotePossibleException(node);
            return null;
        }

        private void VisitArguments(ImmutableArray<BoundExpression> arguments, ImmutableArray<RefKind> refKindsOpt, MethodSymbol method)
        {
            if (refKindsOpt.IsDefault)
            {
                for (int i = 0; i < arguments.Length; i++)
                {
                    VisitRvalue(arguments[i]);
                }
            }
            else
            {
                // first value and ref parameters are read...
                for (int i = 0; i < arguments.Length; i++)
                {
                    RefKind refKind = refKindsOpt.Length <= i ? RefKind.None : refKindsOpt[i];
                    if (refKind != RefKind.Out)
                    {
                        VisitRvalue(arguments[i]);
                    }
                    else
                    {
                        VisitLvalue(arguments[i]);
                    }
                }
                // and then ref and out parameters are written...
                for (int i = 0; i < arguments.Length; i++)
                {
                    RefKind refKind = refKindsOpt.Length <= i ? RefKind.None : refKindsOpt[i];
                    if (refKind != RefKind.None)
                    {
                        WriteArgument(arguments[i], refKind, method);
                    }
                }
            }
        }

        protected virtual void WriteArgument(BoundExpression arg, RefKind refKind, MethodSymbol method)
        {
        }

        public override BoundNode VisitBadExpression(BoundBadExpression node)
        {
            foreach (var child in node.ChildBoundNodes)
            {
                VisitRvalue(child as BoundExpression);
            }

            if (_trackExceptions) NotePossibleException(node);
            return null;
        }

        public override BoundNode VisitBadStatement(BoundBadStatement node)
        {
            foreach (var child in node.ChildBoundNodes)
            {
                if (child is BoundStatement)
                    VisitStatement(child as BoundStatement);
                else
                    VisitRvalue(child as BoundExpression);
            }

            if (_trackExceptions) NotePossibleException(node);
            return null;
        }

        public override BoundNode VisitArrayInitialization(BoundArrayInitialization node)
        {
            foreach (var child in node.Initializers)
            {
                VisitRvalue(child);
            }

            return null;
        }

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            var methodGroup = node.Argument as BoundMethodGroup;
            if (methodGroup != null)
            {
                if ((object)node.MethodOpt != null && !node.MethodOpt.IsStatic)
                {
                    if (_trackRegions)
                    {
                        if (methodGroup == this.firstInRegion && this.regionPlace == RegionPlace.Before) EnterRegion();
                        VisitRvalue(methodGroup.ReceiverOpt);
                        if (methodGroup == this.lastInRegion && IsInside) LeaveRegion();
                    }
                    else
                    {
                        VisitRvalue(methodGroup.ReceiverOpt);
                    }
                }
            }
            else
            {
                VisitRvalue(node.Argument);
            }

            if (_trackExceptions) NotePossibleException(node);
            return null;
        }

        public override BoundNode VisitTypeExpression(BoundTypeExpression node)
        {
            return null;
        }

        public override BoundNode VisitTypeOrValueExpression(BoundTypeOrValueExpression node)
        {
            // If we're seeing a node of this kind, then we failed to resolve the member access
            // as either a type or a property/field/event/local/parameter.  In such cases,
            // the second interpretation applies so just visit the node for that.
            return this.Visit(node.Data.ValueExpression);
        }

        public override BoundNode VisitLiteral(BoundLiteral node)
        {
            return null;
        }

        public override BoundNode VisitConversion(BoundConversion node)
        {
            if (node.ConversionKind == ConversionKind.MethodGroup)
            {
                if (node.IsExtensionMethod || ((object)node.SymbolOpt != null && !node.SymbolOpt.IsStatic))
                {
                    BoundExpression receiver = ((BoundMethodGroup)node.Operand).ReceiverOpt;
                    // A method group's "implicit this" is only used for instance methods.
                    if (_trackRegions)
                    {
                        if (node.Operand == this.firstInRegion && this.regionPlace == RegionPlace.Before) EnterRegion();
                        Visit(receiver);
                        if (node.Operand == this.lastInRegion && IsInside) LeaveRegion();
                    }
                    else
                    {
                        Visit(receiver);
                    }
                }
            }
            else
            {
                Visit(node.Operand);
                if (_trackExceptions && node.HasExpressionSymbols()) NotePossibleException(node);
            }

            return null;
        }

        public override BoundNode VisitIfStatement(BoundIfStatement node)
        {
            // 5.3.3.5 If statements
            VisitCondition(node.Condition);
            LocalState trueState = StateWhenTrue;
            LocalState falseState = StateWhenFalse;
            SetState(trueState);
            VisitStatement(node.Consequence);
            trueState = this.State;
            SetState(falseState);
            if (node.AlternativeOpt != null)
            {
                VisitStatement(node.AlternativeOpt);
            }

            IntersectWith(ref this.State, ref trueState);
            return null;
        }

        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            var oldPending = SavePending(); // we do not allow branches into a try statement

            var initialState = this.State.Clone();
            VisitStatement(node.TryBlock);
            var endState = this.State;

            var tryPending = SavePending();

            if (!node.CatchBlocks.IsEmpty)
            {
                var catchState = initialState.Clone();
                foreach (var pend in tryPending.PendingBranches)
                {
                    IntersectWith(ref catchState, ref pend.State);
                }

                foreach (var catchBlock in node.CatchBlocks)
                {
                    SetState(catchState.Clone());

                    if (catchBlock.ExceptionSourceOpt != null)
                    {
                        VisitLvalue(catchBlock.ExceptionSourceOpt);
                    }

                    if (catchBlock.ExceptionFilterOpt != null)
                    {
                        VisitCondition(catchBlock.ExceptionFilterOpt);
                        SetState(StateWhenTrue);
                    }

                    VisitStatement(catchBlock.Body);
                    IntersectWith(ref endState, ref this.State);
                }
            }

            if (node.FinallyBlockOpt != null)
            {
                // branches from the finally block, while illegal, should still not be considered
                // to execute the finally block before occurring.  Also, we do not handle branches
                // *into* the finally block.
                SetState(endState);
                var catchPending = SavePending();
                VisitStatement(node.FinallyBlockOpt);
                endState = this.State;

                foreach (var pend in tryPending.PendingBranches)
                {
                    if (pend.Branch != null && pend.Branch.Kind != BoundKind.YieldReturnStatement)
                    {
                        SetState(pend.State);
                        VisitStatement(node.FinallyBlockOpt);
                        pend.State = this.State;
                    }
                }
                foreach (var pend in catchPending.PendingBranches)
                {
                    if (pend.Branch != null && pend.Branch.Kind != BoundKind.YieldReturnStatement)
                    {
                        SetState(pend.State);
                        VisitStatement(node.FinallyBlockOpt);
                        pend.State = this.State;
                    }
                }

                SetState(endState);
                RestorePending(catchPending);
            }
            else
            {
                SetState(endState);
            }

            RestorePending(tryPending);
            RestorePending(oldPending);
            return null;
        }

        protected virtual LocalState AllBitsSet() // required for DataFlowsOutWalker
        {
            return default(LocalState);
        }

        public override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            var result = VisitRvalue(node.ExpressionOpt);
            _pendingBranches.Add(new PendingBranch(node, this.State));
            SetUnreachable();
            return result;
        }

        public override BoundNode VisitThisReference(BoundThisReference node)
        {
            return null;
        }

        public override BoundNode VisitPreviousSubmissionReference(BoundPreviousSubmissionReference node)
        {
            return null;
        }

        public override BoundNode VisitHostObjectMemberReference(BoundHostObjectMemberReference node)
        {
            return null;
        }

        public override BoundNode VisitParameter(BoundParameter node)
        {
            return null;
        }

        protected virtual void VisitLvalueParameter(BoundParameter node)
        {
        }

        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            if (_trackExceptions) NotePossibleException(node);

            VisitArguments(node.Arguments, node.ArgumentRefKindsOpt, node.Constructor);
            VisitRvalue(node.InitializerExpressionOpt);
            return null;
        }

        public override BoundNode VisitNewT(BoundNewT node)
        {
            VisitRvalue(node.InitializerExpressionOpt);
            if (_trackExceptions) NotePossibleException(node);
            return null;
        }

        public override BoundNode VisitNoPiaObjectCreationExpression(BoundNoPiaObjectCreationExpression node)
        {
            VisitRvalue(node.InitializerExpressionOpt);
            if (_trackExceptions) NotePossibleException(node);
            return null;
        }

        // represents anything that occurs at the invocation of the property setter
        protected virtual void PropertySetter(BoundExpression node, BoundExpression receiver, MethodSymbol setter, BoundExpression value = null)
        {
            if (_trackExceptions) NotePossibleException(node);
            VisitReceiverAfterCall(receiver, setter);
        }

        // returns false if expression is not a property access 
        // or if the property has a backing field
        // and accessed in a corresponding constructor
        private bool RegularPropertyAccess(BoundExpression expr)
        {
            if (expr.Kind != BoundKind.PropertyAccess)
            {
                return false;
            }

            return !Binder.AccessingAutopropertyFromConstructor((BoundPropertyAccess)expr, _member);
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            // TODO: should events be handled specially too?
            if (RegularPropertyAccess(node.Left))
            {
                var left = (BoundPropertyAccess)node.Left;
                var property = left.PropertySymbol;
                var method = property.GetOwnOrInheritedSetMethod() ?? property.GetMethod;
                VisitReceiverBeforeCall(left.ReceiverOpt, method);
                VisitRvalue(node.Right);
                PropertySetter(node, left.ReceiverOpt, method, node.Right);
            }
            else
            {
                VisitLvalue(node.Left);
                VisitRvalue(node.Right);
            }

            return null;
        }

        public override BoundNode VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            // TODO: should events be handled specially too?
            if (RegularPropertyAccess(node.Left))
            {
                var left = (BoundPropertyAccess)node.Left;
                var property = left.PropertySymbol;
                var readMethod = property.GetOwnOrInheritedGetMethod() ?? property.SetMethod;
                var writeMethod = property.GetOwnOrInheritedSetMethod() ?? property.GetMethod;
                Debug.Assert(node.HasAnyErrors || (object)readMethod != (object)writeMethod);
                VisitReceiverBeforeCall(left.ReceiverOpt, readMethod);
                if (_trackExceptions) NotePossibleException(node);
                VisitReceiverAfterCall(left.ReceiverOpt, readMethod);
                VisitRvalue(node.Right);
                PropertySetter(node, left.ReceiverOpt, writeMethod);
                if (_trackExceptions) NotePossibleException(node);
                VisitReceiverAfterCall(left.ReceiverOpt, writeMethod);
            }
            else
            {
                VisitRvalue(node.Left);
                VisitRvalue(node.Right);
                if (_trackExceptions && node.HasExpressionSymbols()) NotePossibleException(node);
            }

            return null;
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            VisitFieldAccessInternal(node.ReceiverOpt, node.FieldSymbol);
            return null;
        }

        private void VisitFieldAccessInternal(BoundExpression receiverOpt, FieldSymbol fieldSymbol)
        {
            if (MayRequireTracking(receiverOpt, fieldSymbol) || (object)fieldSymbol != null && fieldSymbol.IsFixed)
            {
                VisitLvalue(receiverOpt);
            }
            else
            {
                VisitRvalue(receiverOpt);
            }
        }

        public override BoundNode VisitFieldInfo(BoundFieldInfo node)
        {
            return null;
        }

        public override BoundNode VisitMethodInfo(BoundMethodInfo node)
        {
            return null;
        }

        protected static bool MayRequireTracking(BoundExpression receiverOpt, FieldSymbol fieldSymbol)
        {
            return
                (object)fieldSymbol != null && //simplifies calling pattern for events
                receiverOpt != null &&
                !fieldSymbol.IsStatic &&
                !fieldSymbol.IsFixed &&
                receiverOpt.Type.IsStructType() &&
                !receiverOpt.Type.IsPrimitiveRecursiveStruct();
        }

        public override BoundNode VisitPropertyAccess(BoundPropertyAccess node)
        {
            var property = node.PropertySymbol;

            if (Binder.AccessingAutopropertyFromConstructor(node, _member))
            {
                var backingField = (property as SourcePropertySymbol)?.BackingField;
                if (backingField != null)
                {
                    VisitFieldAccessInternal(node.ReceiverOpt, backingField);
                    return null;
                }
            }

            var method = property.GetOwnOrInheritedGetMethod() ?? property.SetMethod;
            VisitReceiverBeforeCall(node.ReceiverOpt, method);
            if (_trackExceptions) NotePossibleException(node);
            VisitReceiverAfterCall(node.ReceiverOpt, method);
            return null;
            // TODO: In an expression such as
            //    M().Prop = G();
            // Exceptions thrown from M() occur before those from G(), but exceptions from the property accessor
            // occur after both.  The precise abstract flow pass does not yet currently have this quite right.
            // Probably what is needed is a VisitPropertyAccessInternal(BoundPropertyAccess node, bool read)
            // which should assume that the receiver will have been handled by the caller.  This can be invoked
            // twice for read/write operations such as
            //    M().Prop += 1
            // or at the appropriate place in the sequence for read or write operations.
            // Do events require any special handling too?
        }

        public override BoundNode VisitEventAccess(BoundEventAccess node)
        {
            VisitFieldAccessInternal(node.ReceiverOpt, node.EventSymbol.AssociatedField);
            if (_trackExceptions) NotePossibleException(node);
            return null;
        }

        public override BoundNode VisitRangeVariable(BoundRangeVariable node)
        {
            // query variables are always definitely assigned; no need to analyze
            return null;
        }

        public override BoundNode VisitQueryClause(BoundQueryClause node)
        {
            VisitRvalue(node.UnoptimizedForm ?? node.Value);
            return null;
        }

        public override BoundNode VisitMultipleLocalDeclarations(BoundMultipleLocalDeclarations node)
        {
            foreach (var v in node.LocalDeclarations)
                Visit(v);
            return null;
        }

        public override BoundNode VisitWhileStatement(BoundWhileStatement node)
        {
            // while (node.Condition) { node.Body; node.ContinueLabel: } node.BreakLabel:
            LoopHead(node);
            VisitCondition(node.Condition);
            LocalState bodyState = StateWhenTrue;
            LocalState breakState = StateWhenFalse;
            SetState(bodyState);
            VisitStatement(node.Body);
            ResolveContinues(node.ContinueLabel);
            LoopTail(node);
            ResolveBreaks(breakState, node.BreakLabel);
            return null;
        }

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            // visit switch header
            LocalState breakState = VisitSwitchHeader(node);
            SetUnreachable();

            // visit switch block
            VisitSwitchBlock(node);
            IntersectWith(ref breakState, ref this.State);
            ResolveBreaks(breakState, node.BreakLabel);
            return null;
        }

        private LocalState VisitSwitchHeader(BoundSwitchStatement node)
        {
            // Initial value for the Break state for a switch statement is established as follows:
            //  Break state = UnreachableState if either of the following is true:
            //  (1) there is a default label, or
            //  (2) the switch expression is constant and there is a matching case label.
            //  Otherwise, the Break state = current state.

            // visit switch expression
            VisitRvalue(node.BoundExpression);
            LocalState breakState = this.State;

            // For a switch statement, we simulate a possible jump to the switch labels to ensure that
            // the label is not treated as an unused label and a pending branch to the label is noted.

            // However, if switch expression is a constant, we must have determined the single target label
            // at bind time, i.e. node.ConstantTargetOpt, and we must simulate a jump only to this label.

            var constantTargetOpt = node.ConstantTargetOpt;
            if ((object)constantTargetOpt == null)
            {
                bool hasDefaultLabel = false;
                foreach (var section in node.SwitchSections)
                {
                    foreach (var boundSwitchLabel in section.BoundSwitchLabels)
                    {
                        var label = boundSwitchLabel.Label;
                        hasDefaultLabel = hasDefaultLabel || label.IdentifierNodeOrToken.Kind() == SyntaxKind.DefaultSwitchLabel;
                        SetState(breakState.Clone());
                        var simulatedGoto = new BoundGotoStatement(node.Syntax, label);
                        VisitGotoStatement(simulatedGoto);
                    }
                }

                if (hasDefaultLabel)
                {
                    // Condition (1) for an unreachable break state is satisfied
                    breakState = UnreachableState();
                }
            }
            else if (!node.BreakLabel.Equals(constantTargetOpt))
            {
                SetState(breakState.Clone());
                var simulatedGoto = new BoundGotoStatement(node.Syntax, constantTargetOpt);
                VisitGotoStatement(simulatedGoto);

                // Condition (1) or (2) for an unreachable break state is satisfied
                breakState = UnreachableState();
            }

            return breakState;
        }

        private void VisitSwitchBlock(BoundSwitchStatement node)
        {
            var afterSwitchState = UnreachableState();
            var switchSections = node.SwitchSections;
            var iLastSection = (switchSections.Length - 1);
            // visit switch sections
            for (var iSection = 0; iSection <= iLastSection; iSection++)
            {
                VisitSwitchSection(switchSections[iSection], iSection == iLastSection);
                // Even though it is illegal for the end of a switch section to be reachable, in erroneous
                // code it may be reachable.  We treat that as an implicit break (branch to afterSwitchState).
                IntersectWith(ref afterSwitchState, ref this.State);
            }

            SetState(afterSwitchState);
        }

        public virtual BoundNode VisitSwitchSection(BoundSwitchSection node, bool lastSection)
        {
            return VisitSwitchSection(node);
        }

        public override BoundNode VisitSwitchSection(BoundSwitchSection node)
        {
            // visit switch section labels
            foreach (var boundSwitchLabel in node.BoundSwitchLabels)
            {
                VisitRvalue(boundSwitchLabel.ExpressionOpt);
                VisitSwitchSectionLabel(boundSwitchLabel.Label, node);
            }

            // visit switch section body
            VisitStatementList(node);

            return null;
        }

        public override BoundNode VisitArrayAccess(BoundArrayAccess node)
        {
            VisitRvalue(node.Expression);
            foreach (var i in node.Indices)
            {
                VisitRvalue(i);
            }

            if (_trackExceptions && node.HasExpressionSymbols()) NotePossibleException(node);
            return null;
        }

        public override BoundNode VisitBinaryOperator(BoundBinaryOperator node)
        {
            if (node.OperatorKind.IsLogical())
            {
                Debug.Assert(!node.OperatorKind.IsUserDefined());
                VisitBinaryLogicalOperatorChildren(node);
            }
            else
            {
                VisitBinaryOperatorChildren(node);
            }

            return null;
        }

        public override BoundNode VisitUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator node)
        {
            VisitBinaryLogicalOperatorChildren(node);
            return null;
        }

        private void VisitBinaryLogicalOperatorChildren(BoundExpression node)
        {
            // Do not blow the stack due to a deep recursion on the left.
            var stack = ArrayBuilder<BoundExpression>.GetInstance();

            BoundExpression binary;
            BoundExpression child = node;

            while (true)
            {
                var childKind = child.Kind;

                if (childKind == BoundKind.BinaryOperator)
                {
                    var binOp = (BoundBinaryOperator)child;

                    if (!binOp.OperatorKind.IsLogical())
                    {
                        break;
                    }

                    binary = child;
                    child = binOp.Left;
                }
                else if (childKind == BoundKind.UserDefinedConditionalLogicalOperator)
                {
                    binary = child;
                    child = ((BoundUserDefinedConditionalLogicalOperator)binary).Left;
                }
                else
                {
                    break;
                }

                stack.Push(binary);
            }

            Debug.Assert(stack.Count > 0);

            VisitCondition(child);

            while (true)
            {
                binary = stack.Pop();

                BinaryOperatorKind kind;
                BoundExpression right;
                switch (binary.Kind)
                {
                    case BoundKind.BinaryOperator:
                        var binOp = (BoundBinaryOperator)binary;
                        kind = binOp.OperatorKind;
                        right = binOp.Right;
                        break;
                    case BoundKind.UserDefinedConditionalLogicalOperator:
                        var udBinOp = (BoundUserDefinedConditionalLogicalOperator)binary;
                        kind = udBinOp.OperatorKind;
                        right = udBinOp.Right;
                        break;
                    default:
                        throw ExceptionUtilities.Unreachable;
                }

                var op = kind.Operator();
                var isAnd = op == BinaryOperatorKind.And;
                var isBool = kind.OperandTypes() == BinaryOperatorKind.Bool;

                Debug.Assert(isAnd || op == BinaryOperatorKind.Or);

                var leftTrue = this.StateWhenTrue;
                var leftFalse = this.StateWhenFalse;
                SetState(isAnd ? leftTrue : leftFalse);

                VisitCondition(right);
                if (!isBool)
                {
                    this.Unsplit();
                    this.Split();
                }

                var resultTrue = this.StateWhenTrue;
                var resultFalse = this.StateWhenFalse;
                if (isAnd)
                {
                    IntersectWith(ref resultFalse, ref leftFalse);
                }
                else
                {
                    IntersectWith(ref resultTrue, ref leftTrue);
                }
                SetConditionalState(resultTrue, resultFalse);

                if (!isBool)
                {
                    this.Unsplit();
                }

                if (_trackExceptions && binary.HasExpressionSymbols())
                {
                    NotePossibleException(binary);
                }

                if (stack.Count == 0)
                {
                    break;
                }

                AdjustConditionalState(binary);
            }

            Debug.Assert((object)binary == node);
            stack.Free();
        }

        private void VisitBinaryOperatorChildren(BoundBinaryOperator node)
        {
            // It is common in machine-generated code for there to be deep recursion on the left side of a binary
            // operator, for example, if you have "a + b + c + ... " then the bound tree will be deep on the left
            // hand side. To mitigate the risk of stack overflow we use an explicit stack.
            //
            // Of course we must ensure that we visit the left hand side before the right hand side.
            var stack = ArrayBuilder<BoundBinaryOperator>.GetInstance();
            stack.Push(node);

            BoundBinaryOperator binary;
            BoundExpression child = node.Left;

            while (true)
            {
                binary = child as BoundBinaryOperator;
                if (binary == null || binary.OperatorKind.IsLogical())
                {
                    break;
                }

                stack.Push(binary);
                child = binary.Left;
            }

            VisitRvalue(child);

            while (true)
            {
                binary = stack.Pop();
                VisitRvalue(binary.Right);

                if (_trackExceptions && binary.HasExpressionSymbols())
                {
                    NotePossibleException(binary);
                }

                if (stack.Count == 0)
                {
                    break;
                }

                Unsplit(); // VisitRvalue does this
            }

            Debug.Assert((object)binary == node);
            stack.Free();
        }

        public override BoundNode VisitUnaryOperator(BoundUnaryOperator node)
        {
            if (node.OperatorKind == UnaryOperatorKind.BoolLogicalNegation)
            {
                // We have a special case for the ! unary operator, which can operate in a boolean context (5.3.3.26)
                VisitCondition(node.Operand);
                // it inverts the sense of assignedWhenTrue and assignedWhenFalse.
                SetConditionalState(StateWhenFalse, StateWhenTrue);
            }
            else
            {
                VisitRvalue(node.Operand);
                if (_trackExceptions && node.HasExpressionSymbols()) NotePossibleException(node);
            }
            return null;
        }

        public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
        {
            VisitRvalue(node.Expression);
            _pendingBranches.Add(new PendingBranch(node, this.State.Clone()));
            if (_trackExceptions) NotePossibleException(node);
            return null;
        }

        public override BoundNode VisitIncrementOperator(BoundIncrementOperator node)
        {
            // TODO: should we also specially handle events?
            if (RegularPropertyAccess(node.Operand))
            {
                var left = (BoundPropertyAccess)node.Operand;
                var property = left.PropertySymbol;
                var readMethod = property.GetOwnOrInheritedGetMethod() ?? property.SetMethod;
                var writeMethod = property.GetOwnOrInheritedSetMethod() ?? property.GetMethod;
                Debug.Assert(node.HasAnyErrors || (object)readMethod != (object)writeMethod);
                VisitReceiverBeforeCall(left.ReceiverOpt, readMethod);
                if (_trackExceptions) NotePossibleException(node); // a read
                VisitReceiverAfterCall(left.ReceiverOpt, readMethod);
                PropertySetter(node, left.ReceiverOpt, writeMethod); // followed by a write
            }
            else
            {
                VisitRvalue(node.Operand);
                if (_trackExceptions && node.HasExpressionSymbols()) NotePossibleException(node);
            }

            return null;
        }

        public override BoundNode VisitArrayCreation(BoundArrayCreation node)
        {
            foreach (var e1 in node.Bounds)
                VisitRvalue(e1);

            if (node.InitializerOpt != null && !node.InitializerOpt.Initializers.IsDefault)
            {
                foreach (var element in node.InitializerOpt.Initializers)
                    VisitRvalue(element);
            }

            if (_trackExceptions) NotePossibleException(node);
            return null;
        }

        public override BoundNode VisitForStatement(BoundForStatement node)
        {
            if (node.Initializer != null)
            {
                VisitStatement(node.Initializer);
            }
            LoopHead(node);
            LocalState bodyState, breakState;
            if (node.Condition != null)
            {
                VisitCondition(node.Condition);
                bodyState = this.StateWhenTrue;
                breakState = this.StateWhenFalse;
            }
            else
            {
                bodyState = this.State;
                breakState = UnreachableState();
            }

            SetState(bodyState);
            VisitStatement(node.Body);
            ResolveContinues(node.ContinueLabel);
            if (node.Increment != null)
            {
                VisitStatement(node.Increment);
            }

            LoopTail(node);
            ResolveBreaks(breakState, node.BreakLabel);
            return null;
        }

        public override BoundNode VisitForEachStatement(BoundForEachStatement node)
        {
            // foreach ( var v in node.Expression ) { node.Body; node.ContinueLabel: } node.BreakLabel:
            VisitRvalue(node.Expression);
            var breakState = this.State.Clone();
            LoopHead(node);
            VisitForEachIterationVariable(node);
            VisitStatement(node.Body);
            ResolveContinues(node.ContinueLabel);
            LoopTail(node);
            ResolveBreaks(breakState, node.BreakLabel);
            return null;
        }

        public virtual void VisitForEachIterationVariable(BoundForEachStatement node)
        {
        }

        public override BoundNode VisitAsOperator(BoundAsOperator node)
        {
            VisitRvalue(node.Operand);
            return null;
        }

        public override BoundNode VisitIsOperator(BoundIsOperator node)
        {
            VisitRvalue(node.Operand);
            return null;
        }

        public override BoundNode VisitMethodGroup(BoundMethodGroup node)
        {
            if (node.ReceiverOpt != null)
            {
                // An explicit or implicit receiver, for example in an expression such as (x.Foo is Action, or Foo is Action), is considered to be read.
                VisitRvalue(node.ReceiverOpt);
            }

            return null;
        }

        public override BoundNode VisitNullCoalescingOperator(BoundNullCoalescingOperator node)
        {
            VisitRvalue(node.LeftOperand);
            if (IsConstantNull(node.LeftOperand))
            {
                VisitRvalue(node.RightOperand);
            }
            else
            {
                var savedState = this.State.Clone();
                if (node.LeftOperand.ConstantValue != null)
                {
                    SetUnreachable();
                }
                VisitRvalue(node.RightOperand);
                IntersectWith(ref this.State, ref savedState);
            }
            return null;
        }

        public override BoundNode VisitConditionalAccess(BoundConditionalAccess node)
        {
            VisitRvalue(node.Receiver);

            if (node.Receiver.ConstantValue != null && !IsConstantNull(node.Receiver))
            {
                VisitRvalue(node.AccessExpression);
            }
            else
            {
                var savedState = this.State.Clone();
                if (IsConstantNull(node.Receiver))
                {
                    SetUnreachable();
                }

                VisitRvalue(node.AccessExpression);
                IntersectWith(ref this.State, ref savedState);
            }
            return null;
        }

        public override BoundNode VisitLoweredConditionalAccess(BoundLoweredConditionalAccess node)
        {
            VisitRvalue(node.Receiver);

            var savedState = this.State.Clone();

            VisitRvalue(node.WhenNotNull);
            IntersectWith(ref this.State, ref savedState);

            if (node.WhenNullOpt != null)
            {
                savedState = this.State.Clone();
                VisitRvalue(node.WhenNullOpt);
                IntersectWith(ref this.State, ref savedState);
            }

            return null;
        }

        public override BoundNode VisitConditionalReceiver(BoundConditionalReceiver node)
        {
            return null;
        }

        public override BoundNode VisitComplexConditionalReceiver(BoundComplexConditionalReceiver node)
        {
            var savedState = this.State.Clone();

            VisitRvalue(node.ValueTypeReceiver);
            IntersectWith(ref this.State, ref savedState);

            savedState = this.State.Clone();
            VisitRvalue(node.ReferenceTypeReceiver);
            IntersectWith(ref this.State, ref savedState);

            return null;
        }

        public override BoundNode VisitSequence(BoundSequence node)
        {
            var sideEffects = node.SideEffects;
            if (!sideEffects.IsEmpty)
            {
                foreach (var se in sideEffects)
                {
                    VisitRvalue(se);
                }
            }

            VisitRvalue(node.Value);
            return null;
        }

        public override BoundNode VisitSequencePoint(BoundSequencePoint node)
        {
            if (node.StatementOpt != null)
                VisitStatement(node.StatementOpt);
            return null;
        }

        public override BoundNode VisitSequencePointExpression(BoundSequencePointExpression node)
        {
            VisitRvalue(node.Expression);
            return null;
        }

        public override BoundNode VisitSequencePointWithSpan(BoundSequencePointWithSpan node)
        {
            if (node.StatementOpt != null)
                VisitStatement(node.StatementOpt);
            return null;
        }

        public override BoundNode VisitStatementList(BoundStatementList node)
        {
            return VisitStatementListWorker(node);
        }

        private BoundNode VisitStatementListWorker(BoundStatementList node)
        {
            foreach (var statement in node.Statements)
            {
                VisitStatement(statement);
            }

            return null;
        }

        public override BoundNode VisitTypeOrInstanceInitializers(BoundTypeOrInstanceInitializers node)
        {
            return VisitStatementListWorker(node);
        }

        public override BoundNode VisitUnboundLambda(UnboundLambda node)
        {
            // The presence of this node suggests an error was detected in an earlier phase.
            return VisitLambda(node.BindForErrorRecovery());
        }

        public override BoundNode VisitBreakStatement(BoundBreakStatement node)
        {
            _pendingBranches.Add(new PendingBranch(node, this.State));
            SetUnreachable();
            return null;
        }

        public override BoundNode VisitContinueStatement(BoundContinueStatement node)
        {
            // While continue statements do no affect definite assignment, subclasses
            // such as region flow analysis depend on their presence as pending branches.
            _pendingBranches.Add(new PendingBranch(node, this.State));
            SetUnreachable();
            return null;
        }

        public override BoundNode VisitConditionalOperator(BoundConditionalOperator node)
        {
            VisitCondition(node.Condition);
            var consequenceState = this.StateWhenTrue;
            var alternativeState = this.StateWhenFalse;
            if (IsConstantTrue(node.Condition))
            {
                SetState(alternativeState);
                Visit(node.Alternative);
                SetState(consequenceState);
                Visit(node.Consequence);
                // it may be a boolean state at this point.
            }
            else if (IsConstantFalse(node.Condition))
            {
                SetState(consequenceState);
                Visit(node.Consequence);
                SetState(alternativeState);
                Visit(node.Alternative);
                // it may be a boolean state at this point.
            }
            else
            {
                SetState(consequenceState);
                Visit(node.Consequence);
                Unsplit();
                consequenceState = this.State;
                SetState(alternativeState);
                Visit(node.Alternative);
                Unsplit();
                IntersectWith(ref this.State, ref consequenceState);
                // it may not be a boolean state at this point (5.3.3.28)
            }

            return null;
        }

        public override BoundNode VisitBaseReference(BoundBaseReference node)
        {
            // TODO: in a struct constructor, "this" is not initially assigned.
            return null;
        }

        public override BoundNode VisitDoStatement(BoundDoStatement node)
        {
            // do { statements; node.ContinueLabel: } while (node.Condition) node.BreakLabel:
            LoopHead(node);
            VisitStatement(node.Body);
            ResolveContinues(node.ContinueLabel);
            VisitCondition(node.Condition);
            LocalState breakState = this.StateWhenFalse;
            SetState(this.StateWhenTrue);
            LoopTail(node);
            ResolveBreaks(breakState, node.BreakLabel);
            return null;
        }

        public override BoundNode VisitGotoStatement(BoundGotoStatement node)
        {
            _pendingBranches.Add(new PendingBranch(node, this.State));
            SetUnreachable();
            return null;
        }

        private void VisitLabel(LabelSymbol label, BoundStatement node)
        {
            node.AssertIsLabeledStatementWithLabel(label);
            ResolveBranches(label, node);
            var state = LabelState(label);
            IntersectWith(ref this.State, ref state);
            _labels[label] = this.State.Clone();
            _labelsSeen.Add(node);
        }

        protected virtual void VisitLabel(BoundLabeledStatement node)
        {
            VisitLabel(node.Label, node);
        }

        protected virtual void VisitSwitchSectionLabel(LabelSymbol label, BoundSwitchSection node)
        {
            VisitLabel(label, node);
        }

        public override BoundNode VisitLabelStatement(BoundLabelStatement node)
        {
            VisitLabel(node.Label, node);
            return null;
        }

        public override BoundNode VisitLabeledStatement(BoundLabeledStatement node)
        {
            VisitLabel(node);
            VisitStatement(node.Body);
            return null;
        }

        public override BoundNode VisitLockStatement(BoundLockStatement node)
        {
            VisitRvalue(node.Argument);
            if (_trackExceptions) NotePossibleException(node);
            VisitStatement(node.Body);
            return null;
        }

        public override BoundNode VisitNoOpStatement(BoundNoOpStatement node)
        {
            return null;
        }

        public override BoundNode VisitNamespaceExpression(BoundNamespaceExpression node)
        {
            return null;
        }

        public override BoundNode VisitUsingStatement(BoundUsingStatement node)
        {
            if (node.ExpressionOpt != null)
            {
                VisitRvalue(node.ExpressionOpt);
            }

            if (node.DeclarationsOpt != null)
            {
                VisitStatement(node.DeclarationsOpt);
            }

            if (_trackExceptions) NotePossibleException(node);
            VisitStatement(node.Body);
            return null;
        }

        public override BoundNode VisitFixedStatement(BoundFixedStatement node)
        {
            VisitStatement(node.Declarations);
            VisitStatement(node.Body);
            return null;
        }

        public override BoundNode VisitFixedLocalCollectionInitializer(BoundFixedLocalCollectionInitializer node)
        {
            VisitRvalue(node.Expression);
            return null;
        }

        public override BoundNode VisitThrowStatement(BoundThrowStatement node)
        {
            BoundExpression expr = node.ExpressionOpt;
            if (expr != null)
            {
                VisitRvalue(expr);
            }

            if (_trackExceptions) NotePossibleException(node);
            SetUnreachable();
            return null;
        }

        public override BoundNode VisitYieldBreakStatement(BoundYieldBreakStatement node)
        {
            _pendingBranches.Add(new PendingBranch(node, this.State));
            SetUnreachable();
            return null;
        }

        public override BoundNode VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            VisitRvalue(node.Expression);
            _pendingBranches.Add(new PendingBranch(node, this.State.Clone()));
            return null;
        }

        public override BoundNode VisitDefaultOperator(BoundDefaultOperator node)
        {
            return null;
        }

        public override BoundNode VisitTypeOfOperator(BoundTypeOfOperator node)
        {
            VisitTypeExpression(node.SourceType);
            return null;
        }

        public override BoundNode VisitNameOfOperator(BoundNameOfOperator node)
        {
            var savedState = this.State;
            SetState(UnreachableState());
            Visit(node.Argument);
            SetState(savedState);
            return null;
        }

        public override BoundNode VisitAddressOfOperator(BoundAddressOfOperator node)
        {
            VisitAddressOfOperator(node, shouldReadOperand: false);
            return null;
        }

        /// <summary>
        /// If the operand is definitely assigned, we may want to perform a read (in addition to
        /// a write) so that the operand can show up as ReadInside/DataFlowsIn.
        /// </summary>
        protected void VisitAddressOfOperator(BoundAddressOfOperator node, bool shouldReadOperand)
        {
            BoundExpression operand = node.Operand;

            if (shouldReadOperand)
            {
                this.VisitRvalue(operand);
            }
            else
            {
                this.VisitLvalue(operand);
            }

            this.WriteArgument(operand, RefKind.Out, null); //Out because we know it will definitely be assigned.
        }

        public override BoundNode VisitPointerIndirectionOperator(BoundPointerIndirectionOperator node)
        {
            VisitRvalue(node.Operand);
            return null;
        }

        public override BoundNode VisitPointerElementAccess(BoundPointerElementAccess node)
        {
            VisitRvalue(node.Expression);
            VisitRvalue(node.Index);
            return null;
        }

        public override BoundNode VisitSizeOfOperator(BoundSizeOfOperator node)
        {
            return null;
        }

        public override BoundNode VisitStackAllocArrayCreation(BoundStackAllocArrayCreation node)
        {
            VisitRvalue(node.Count);
            return null;
        }

        public override BoundNode VisitAnonymousObjectCreationExpression(BoundAnonymousObjectCreationExpression node)
        {
            //  visit arguments as r-values
            VisitArguments(node.Arguments, default(ImmutableArray<RefKind>), node.Constructor);

            //  ignore declarations
            //node.Declarations
            if (_trackExceptions) NotePossibleException(node);
            return null;
        }

        public override BoundNode VisitArrayLength(BoundArrayLength node)
        {
            VisitRvalue(node.Expression);
            return null;
        }

        public override BoundNode VisitConditionalGoto(BoundConditionalGoto node)
        {
            VisitRvalue(node.Condition);
            _pendingBranches.Add(new PendingBranch(node, this.State.Clone()));
            return null;
        }

        public override BoundNode VisitObjectInitializerExpression(BoundObjectInitializerExpression node)
        {
            return VisitObjectOrCollectionInitializerExpression(node.Initializers);
        }

        public override BoundNode VisitCollectionInitializerExpression(BoundCollectionInitializerExpression node)
        {
            return VisitObjectOrCollectionInitializerExpression(node.Initializers);
        }

        private BoundNode VisitObjectOrCollectionInitializerExpression(ImmutableArray<BoundExpression> initializers)
        {
            foreach (var initializer in initializers)
            {
                VisitRvalue(initializer);
                if (_trackExceptions) NotePossibleException(initializer);
            }

            return null;
        }

        public override BoundNode VisitObjectInitializerMember(BoundObjectInitializerMember node)
        {
            var arguments = node.Arguments;
            if (!arguments.IsDefaultOrEmpty)
            {
                foreach (var argument in arguments)
                {
                    VisitRvalue(argument);
                }
            }

            return null;
        }

        public override BoundNode VisitDynamicObjectInitializerMember(BoundDynamicObjectInitializerMember node)
        {
            return null;
        }

        public override BoundNode VisitCollectionElementInitializer(BoundCollectionElementInitializer node)
        {
            if (node.AddMethod.CallsAreOmitted(node.SyntaxTree))
            {
                // If the underlying add method is a partial method without a definition, or is a conditional method
                // whose condition is not true, then the call has no effect and it is ignored for the purposes of
                // definite assignment analysis.

                LocalState savedState = savedState = this.State.Clone();
                SetUnreachable();

                VisitArguments(node.Arguments, default(ImmutableArray<RefKind>), node.AddMethod);

                this.State = savedState;
            }
            else
            {
                VisitArguments(node.Arguments, default(ImmutableArray<RefKind>), node.AddMethod);
            }

            if (_trackExceptions) NotePossibleException(node);
            return null;
        }

        public override BoundNode VisitDynamicCollectionElementInitializer(BoundDynamicCollectionElementInitializer node)
        {
            VisitArguments(node.Arguments, default(ImmutableArray<RefKind>), method: null);
            if (_trackExceptions) NotePossibleException(node);
            return null;
        }

        public override BoundNode VisitImplicitReceiver(BoundImplicitReceiver node)
        {
            return null;
        }

        #endregion visitors
    }
}
