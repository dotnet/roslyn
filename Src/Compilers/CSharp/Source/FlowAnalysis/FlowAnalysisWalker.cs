using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// The implementation of the flow analysis compilation phase.  It works by walking over the
    /// statements and expressions of a method body, maintaining state (this.state) the reflects the
    /// definite assignment and reachability state at the current location being analyzed.
    /// 
    /// When adding support for a new kind of bound tree node, the relevant sections of the language
    /// specification should be consulted to determine the appropriate handling in this phase.
    /// </summary>
    partial class FlowAnalysisWalker
    {
        /// <summary>
        /// The source tree in which the analysis is taking place.
        /// </summary>
        private readonly SyntaxTree tree;

        /// <summary>
        /// The compilation in which the analysis is taking place.  This is needed to determine which
        /// conditional methods will be compiled and which will be omitted.
        /// </summary>
        private readonly Compilation compilation;

        /// <summary>
        /// The method whose body is being analyzed.
        /// </summary>
        private readonly MethodSymbol method;

        /// <summary>
        /// The bound code of the method being analyzed.
        /// </summary>
        private readonly BoundStatement block;

        /// <summary>
        /// A cache mapping a method to its "this" symbol.  This is temporary until the method body
        /// binder starts doing this itself.
        /// </summary>
        private readonly ThisSymbolCache thisSymbolCache;

        /// <summary>
        /// Variables that were read anywhere.
        /// </summary>
        private readonly HashSet<Symbol> readVariables = new HashSet<Symbol>();

        /// <summary>
        /// Variables that were initialized or written anywhere.
        /// </summary>
        private readonly HashSet<Symbol> writtenVariables = new HashSet<Symbol>();

        /// <summary>
        /// A cache of the state at the backward branch point of each loop.  This is not needed
        /// during normal flow analysis, but is needed for region analysis.
        /// </summary>
        private readonly Dictionary<BoundLoopStatement, FlowAnalysisLocalState> loopHeadState;

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
        Dictionary<LabelSymbol, FlowAnalysisLocalState?> labels = new Dictionary<LabelSymbol, FlowAnalysisLocalState?>();

        /// <summary>
        /// Set to true after an analysis scan if the analysis was incomplete due to a backward
        /// "goto" branch changing some analysis result.  In this case the caller scans again (until
        /// this is false). Since the analysis proceeds by monotonically changing the state computed
        /// at each label, this must terminate.
        /// </summary>
        internal bool backwardBranchChanged = false;

        /// <summary>
        /// Pending escapes generated in the current scope (or more deeply nested scopes). When jump
        /// statements (goto, break, continue, return) are processed, they are placed in the
        /// pendingBranches buffer to be processed later by the code handling the destination
        /// statement. As a special case, the processing of try-finally statements might modify the
        /// contents of the pendingBranches buffer to take into account the behavior of
        /// "intervening" finally clauses.
        /// </summary>
        protected ArrayBuilder<PendingBranch> pendingBranches = ArrayBuilder<PendingBranch>.GetInstance();

        /// <summary>
        /// A mapping from local variables to the index of their slot in a flow analysis local state.
        /// TODO: represent fields of locals of struct type too.
        /// TODO: could we store this as a field of the LocalSymbol for performance?
        /// </summary>
        Dictionary<Symbol, int> variableSlot = new Dictionary<Symbol, int>();

        /// <summary>
        /// A mapping from the local variable slot to the symbol for the local variable itself.  This
        /// is used in the implementationof region analysis (support for extract method) to compute
        /// the set of variables "always assigned" in a region of code.
        /// </summary>
        protected Symbol[] variableBySlot = new Symbol[10];

        /// <summary>
        /// Variable slots are allocated to local variables sequentially and never reused.  This is
        /// the index of the next slot number to use.
        /// </summary>
        protected int nextVariableSlot = 1;

        /// <summary>
        /// Tracks variables for which we have already reported a definite assignment error.  This
        /// allows us to report at most one such error per variable.
        /// </summary>
        private BitArray alreadyReported;

        /// <summary>
        /// The definite assignment and reachability state at the point currently being analyzed.
        /// </summary>
        protected FlowAnalysisLocalState state;

        /// <summary>
        /// Some variables that should be considered initially assigned.  Used for region analysis.
        /// </summary>
        protected readonly HashSet<Symbol> initiallyAssignedVariables;

        protected FlowAnalysisWalker(
            Compilation compilation,
            SyntaxTree tree,
            MethodSymbol method,
            BoundStatement block,
            ThisSymbolCache thisSymbolCache = null,
            HashSet<Symbol> initiallyAssignedVariables = null,
            bool trackUnassignmentsInLoops = false)
        {
            this.compilation = compilation;
            this.tree = tree;
            this.method = method;
            this.block = block;
            this.thisSymbolCache = thisSymbolCache ?? new ThisSymbolCache();
            this.initiallyAssignedVariables = initiallyAssignedVariables;
            this.loopHeadState = trackUnassignmentsInLoops ? new Dictionary<BoundLoopStatement, FlowAnalysisLocalState>() : null;

            // TODO: mark "this" as not assigned in a struct constructor (unless it has no fields)
            // TODO: accommodate instance variables of a local variable of struct type.
        }

        /// <summary>
        /// Perform a single pass of flow analysis.  Note that after this pass,
        /// this.backwardBranchChanged indicates if a further pass is required.
        /// </summary>
        protected virtual void Scan()
        {
            // the entry point of a method is assumed reachable
            state = FlowAnalysisLocalState.ReachableState();

            // label out parameters as not assigned.
            foreach (var parameter in method.Parameters)
            {
                if (parameter.RefKind == RefKind.Out)
                {
                    SetSlotState(MakeSlot(parameter), initiallyAssignedVariables != null && initiallyAssignedVariables.Contains(parameter));
                }
                else
                {
                    // this code has no effect except in the region analysis APIs, which assign
                    // variable slots to all parameters.
                    int slot = VariableSlot(parameter);
                    SetSlotState(slot, true);
                }
            }

            // TODO: if this is the body of an initial struct constructor, mark "this" as unassigned.
            this.backwardBranchChanged = false;
            if (this.Diagnostics != null)
                this.Diagnostics.Free();
            this.Diagnostics = DiagnosticBag.GetInstance();  // clear reported diagnostics
            this.alreadyReported = BitArray.Empty;           // no variables yet reported unassigned
            VisitStatement(block);

            // check that each local variable is used somewhere (or warn if it isn't)
            foreach (var symbol in variableBySlot)
            {
                if (symbol != null && symbol.Kind == SymbolKind.Local && !readVariables.Contains(symbol))
                {
                    Diagnostics.Add(writtenVariables.Contains(symbol) ? ErrorCode.WRN_UnreferencedVarAssg : ErrorCode.WRN_UnreferencedVar, symbol.Locations[0], symbol.Name);
                }
            }

            // check that each out parameter is definitely assigned at the end of the method if
            // there's more than one location, then the method is partial and we prefer to report an
            // out parameter in partial method error
            if (method.Locations.Count == 1)
            {
                foreach (ParameterSymbol parameter in method.Parameters)
                {
                    if (parameter.RefKind == RefKind.Out)
                    {
                        var slot = VariableSlot(parameter);
                        if (!state.IsAssigned(slot))
                        {
                            ReportUnassignedOutParameter(parameter, null, method.Locations[0]);
                        }

                        foreach (PendingBranch returnBranch in pendingBranches)
                        {
                            if (!returnBranch.State.IsAssigned(slot))
                            {
                                ReportUnassignedOutParameter(parameter, returnBranch.Branch.Syntax, null);
                            }
                        }
                    }
                }
            }
            // TODO: handle "this" in struct constructor.
        }

        protected virtual void ReportUnassignedOutParameter(ParameterSymbol parameter, SyntaxNode node, Location location)
        {
            if (Diagnostics != null)
            {
                if (location == null)
                {
                    location = new SourceLocation(tree, node);
                }

                Diagnostics.Add(ErrorCode.ERR_ParamUnassigned, location, parameter.Name);
            }
        }

        /// <summary>
        /// Perform flow analysis, reporting all necessary diagnostics.  Returns true if the end of
        /// the body might be reachable..
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="tree"></param>
        /// <param name="method"></param>
        /// <param name="block"></param>
        /// <param name="diagnostics"></param>
        /// <returns></returns>
        public static bool Analyze(Compilation compilation, SyntaxTree tree, MethodSymbol method, BoundStatement block, DiagnosticBag diagnostics)
        {
            var walker = new FlowAnalysisWalker(compilation, tree, method, block);
            var result = walker.Analyze(diagnostics);
            walker.Free();
            return result;
        }

        /// <summary>
        /// Analyze the body, reporting all necessary diagnostics.  Returns true if the end of the
        /// body might be reachable.
        /// </summary>
        /// <param name="diagnostics"></param>
        /// <returns></returns>
        protected bool Analyze(DiagnosticBag diagnostics)
        {
            ReadOnlyArray<PendingBranch> returns = Analyze();

            if (diagnostics != null)
            {
                diagnostics.Add(this.Diagnostics);
                if (method != null && !method.ReturnsVoid && !Unreachable)
                {
                    // if there's more than one location, then the method is partial and we
                    // have already reported a non-void partial method error
                    if (method.Locations.Count == 1)
                    {
                        diagnostics.Add(ErrorCode.ERR_ReturnExpected, method.Locations[0], method);
                    }
                }
            }

            // TODO: if in the body of a struct constructor, check that "this" is assigned at each return.
            return !CLRUnreachable;
        }

        protected ReadOnlyArray<PendingBranch> Analyze()
        {
            ReadOnlyArray<PendingBranch> returns;
            do
            {
                this.Scan();
                returns = this.RemoveReturns();
            }
            while (this.backwardBranchChanged);
            return returns;
        }

        /// <summary>
        /// Where all diagnostics are deposited.
        /// </summary>
        private DiagnosticBag Diagnostics { get; set; }

        protected virtual void Free()
        {
            Diagnostics.Free();
            Diagnostics = null;
            pendingBranches.Free();
            pendingBranches = null;
            this.alreadyReported = BitArray.Null;
        }

        /// <summary>
        /// Return the flow analysis state associated with a label.
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        FlowAnalysisLocalState? LabelState(LabelSymbol label)
        {
            FlowAnalysisLocalState? result;
            if (labels.TryGetValue(label, out result))
                return result;

            result = FlowAnalysisLocalState.UnreachableState(nextVariableSlot);
            labels.Add(label, result);
            return result;
        }

        private ParameterSymbol thisSymbol;
        protected ParameterSymbol ThisSymbol
        {
            get
            {
                if (thisSymbol == null)
                {
                    Interlocked.CompareExchange<ParameterSymbol>(ref thisSymbol, thisSymbolCache.ThisForMethod(method), null);
                }

                return thisSymbol;
            }
        }

        protected virtual void NoteRead(Symbol variable)
        {
            if (variable != null)
            {
                readVariables.Add(variable);
            }
        }

        protected virtual void NoteWrite(Symbol variable, BoundExpression value)
        {
            if (variable != null)
            {
                writtenVariables.Add(variable);
                var local = variable as LocalSymbol;
                if (local != null && local.Type.IsReferenceType || !value.IsConstant)
                {
                    // We duplicate Dev10's behavior here.  The reasoning is, I would guess, that
                    // othewise unread variables of reference type are useful because they keep
                    // objects alive, i.e., they are read by the VM.  And variables that are
                    // intialized by non-constant expressions presumably are useful because they
                    // enable us to clearly document a discarded return value from a method
                    // invocation, e.g. var discardedValue = F(); >shrug<
                    readVariables.Add(variable);
                }
            }
        }

        /// <summary>
        /// Return to the caller the set of pending return statements.
        /// </summary>
        /// <returns></returns>
        private ReadOnlyArray<PendingBranch> RemoveReturns()
        {
            var result = pendingBranches.ToReadOnly();
            // Debug.Assert(AllReturns(result)); // if anything other than return were pending, we would have produced diagnostics earlier.
            this.pendingBranches.Clear();
            return result;
        }

        /// <summary>
        /// Locals are given slots when their declarations are encountered.  We only need give slots
        /// to local variables, out parameters, and the "this" variable of a struct constructs.
        /// Other variables are not given slots, and are therefore not tracked by the analysis.  This
        /// returns -1 for a variable that is not tracked.  We do not need to track references to
        /// variables that occur before the variable is declared, as those are reported in an
        /// earlier phase as "use before declaration". That allows us to avoid giving slots to local
        /// variables before processing their declarations.
        /// </summary>
        /// <param name="local"></param>
        /// <returns></returns>
        protected int VariableSlot(Symbol local)
        {
            int slot;
            return (variableSlot.TryGetValue(local, out slot)) ? slot : -1;
        }

        /// <summary>
        /// Force a variable to have a slot.
        /// </summary>
        /// <param name="local"></param>
        /// <returns></returns>
        protected int MakeSlot(Symbol local)
        {
            int slot;

            // Since analysis may proceed in multiple passes, it is possible the slot is already assigned.
            if (variableSlot.TryGetValue(local, out slot))
            {
                return slot;
            }

            slot = nextVariableSlot++;
            variableSlot.Add(local, slot);
            if (slot >= variableBySlot.Length)
            {
                Array.Resize(ref this.variableBySlot, slot * 2);
            }

            variableBySlot[slot] = local;
            return slot;
        }

        /// <summary>
        /// Is the current state unreachable from the language specification's point of view?
        /// </summary>
        protected bool Unreachable
        {
            get
            {
                return !state.Reachable;
            }
        }

        /// <summary>
        /// Is the current state reachable in the generated code from the point of view of the CLR
        /// verifier? This is a more precise (less conservative) test than this.Unreachable.
        /// </summary>
        protected bool CLRUnreachable
        {
            get
            {
                return !state.Reachable || state.Assigned[0];
            }
        }

        /// <summary>
        /// Set the current state to one that indicates that it is unreachable.
        /// </summary>
        protected void SetUnreachable()
        {
            this.state = FlowAnalysisLocalState.UnreachableState(this.nextVariableSlot);
        }

        /// <summary>
        /// Check that the given variable is definitely assigned.  If not, produce an error.
        /// </summary>
        /// <param name="local"></param>
        /// <param name="node"></param>
        protected void CheckAssigned(Symbol local, SyntaxNode node)
        {
            if (local != null && state.Reachable && !state.IsAssigned(VariableSlot(local)))
            {
                ReportUnassigned(local, node);
            }

            NoteRead(local);
        }

        /// <summary>
        /// Report a given variable as not definitely assigned.  Once a variable has been so
        /// reported, we suppress further reports of that variable.
        /// </summary>
        /// <param name="local"></param>
        /// <param name="node"></param>
        protected virtual void ReportUnassigned(Symbol local, SyntaxNode node)
        {
            int slot = VariableSlot(local);
            if (!alreadyReported[slot])
            {
                Diagnostics.Add(ErrorCode.ERR_UseDefViolation, new SourceLocation(tree, node), local.Name);
            }

            alreadyReported[slot] = true; // mark the variable's slot so that we don't complain about the variable again
        }

        /// <summary>
        /// Mark a variable as assigned (or unassigned).
        /// </summary>
        protected virtual void Assign(BoundNode node, BoundExpression value, bool assigned = true)
        {
            switch (node.Kind)
            {
                case BoundKind.LocalDeclaration:
                    {
                        var local = node as BoundLocalDeclaration;
                        var symbol = local.LocalSymbol;
                        int slot = VariableSlot(symbol);
                        bool written = assigned || !state.Reachable || initiallyAssignedVariables != null && initiallyAssignedVariables.Contains(symbol);
                        SetSlotState(slot, written);
                        if (assigned && local.Initializer != null) NoteWrite(symbol, value);
                        break;
                    }

                case BoundKind.Local:
                    {
                        var local = node as BoundLocal;
                        var symbol = local.LocalSymbol;
                        int slot = VariableSlot(symbol);
                        SetSlotState(slot, assigned);
                        if (assigned) NoteWrite(symbol, value);
                        break;
                    }

                case BoundKind.Parameter:
                    {
                        var local = node as BoundParameter;
                        var symbol = local.ParameterSymbol;
                        int slot = VariableSlot(symbol);
                        SetSlotState(slot, assigned);
                        if (assigned) NoteWrite(symbol, value);
                        break;
                    }

                case BoundKind.ThisReference:
                    {
                        // var local = node as BoundThisReference;
                        int slot = VariableSlot(ThisSymbol);
                        SetSlotState(slot, assigned);
                        if (assigned) NoteWrite(ThisSymbol, value);
                        break;
                    }

                case BoundKind.FieldAccess:
                case BoundKind.PropertyAccess:
                    {
                        // TODO: track instance fields and properties of structs
                        break;
                    }

                case BoundKind.ArrayAccess:
                    {
                        // array elements are not tracked for definite assignment.
                        break;
                    }

                // TODO: special case for assigning to an instance field of a local variable of struct type
                default:
                    Unimplemented(node, "assigning to " + node.Kind);
                    break;
            }
        }

        protected void SetSlotState(int slot, bool assigned)
        {
            if (assigned)
            {
                state.Assign(slot);
            }
            else
            {
                state.Unassign(slot);
            }
        }

        /// <summary>
        /// Visit a node, computing the definite assignment state at its end.
        /// </summary>
        /// <param name="node"></param>
        protected virtual void Visit(BoundNode node)
        {
            if (node != null)
            {
                node.Accept(this, null);
            }
        }

        protected void VisitLvalue(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundKind.Local:
                case BoundKind.Parameter:
                case BoundKind.ThisReference:
                case BoundKind.BaseReference:
                    // no need for it to be assigned: it is on the left.
                    break;

                // case BoundKind.FieldAccess: // TODO: special case for instance field selected from a local variable of struct type.
                default:
                    VisitExpression(node);
                    break;
            }
        }

        /// <summary>
        /// Visit a boolean condition expression, where we will be wanting AssignedWhenTrue and
        /// AssignedWhenFalse.
        /// </summary>
        /// <param name="node"></param>
        protected void VisitCondition(BoundExpression node)
        {
            Debug.Assert(!this.state.Assigned.IsNull);
            Visit(node);

            // We implement the foundational rules missing from the language specification:
            // v is "definitely assigned when true" after a constant expression whose value is false.
            // v is "definitely assigned when false" after a constant expression whose value is true.
            // These rules are to be added to the language specification.
            // It was the lack of these foundational rules that led to the invention of the concept
            // of "unreachable expression" in the native compiler.
            if (IsConstantTrue(node))
            {
                state.Merge();
                this.state = new FlowAnalysisLocalState(this.state.Reachable, this.state.Assigned, BitArray.AllSet(nextVariableSlot));
            }
            else if (IsConstantFalse(node))
            {
                state.Merge();
                this.state = new FlowAnalysisLocalState(this.state.Reachable, BitArray.AllSet(nextVariableSlot), this.state.Assigned);
            }
            else
            {
                state.Split();
            }
        }

        /// <summary>
        /// Visit a general expression, where we will only need to determine if variables are
        /// assigned (or not). That is, we will not be needing AssignedWhenTrue and
        /// AssignedWhenFalse.
        /// </summary>
        /// <param name="node"></param>
        protected void VisitExpression(BoundExpression node)
        {
            Debug.Assert(!this.state.Assigned.IsNull);
            Visit(node);
            state.Merge();
        }

        /// <summary>
        /// Visit a statement.  Note that the language spec requires each statement to be reachable,
        /// so we report an unreachable statement if it is not reachable.
        /// </summary>
        /// <param name="statement"></param>
        protected void VisitStatement(BoundStatement statement)
        {
            Debug.Assert(!this.state.Assigned.IsNull);

            // We use variable slot 0 to record, in unreachable code, when a warning has
            // (unassigned) or has not (assigned) been produced.
            if (!state.Reachable && state.IsAssigned(0))
            {
                // prefer to place the diagnostic on a nonempty statement
                var unreachable = FirstNonempty(statement);

                // Dev10 refuses to mark empty statements as unreachable; we follow suit.
                if (unreachable != null)
                {
                    Diagnostics.Add(ErrorCode.WRN_UnreachableCode, new SourceLocation(tree, unreachable.Syntax));
                    state.Unassign(0); // suppress cascaded diagnostics
                }
            }

            Visit(statement);
        }

        /// <summary>
        /// Return the first nonempty statement in the given statement, or null if it is all empty.
        /// This is used to reproduce the Dev10 behavior for unreachable statements, where the
        /// diagnostic is only reported when there is a "nonempty" statement.
        /// </summary>
        private BoundStatement FirstNonempty(BoundStatement statement)
        {
            if (statement == null)
            {
                return null;
            }

            switch (statement.Kind)
            {
                default:
                    return statement;

                case BoundKind.Block:
                    foreach (var s in ((BoundBlock)statement).Statements)
                    {
                        var result = FirstNonempty(s);
                        if (result != null) return result;
                    }

                    return null;

                case BoundKind.NoOpStatement:
                    return null;
            }
        }

        private bool IsConstantTrue(BoundExpression node)
        {
            if (!node.IsConstant)
            {
                return false;
            }

            var constantValue = node.ConstantValue;
            if (constantValue.Discriminator != ConstantValueTypeDiscriminator.Boolean)
            {
                return false;
            }

            return constantValue.BooleanValue;
        }

        private bool IsConstantFalse(BoundExpression node)
        {
            if (!node.IsConstant)
            {
                return false;
            }

            var constantValue = node.ConstantValue;
            if (constantValue.Discriminator != ConstantValueTypeDiscriminator.Boolean)
            {
                return false;
            }

            return !constantValue.BooleanValue;
        }

        private bool IsConstantNull(BoundExpression node)
        {
            if (!node.IsConstant)
            {
                return false;
            }

            return node.ConstantValue.IsNull;
        }

        private object VisitCompoundOperator(BoundCompoundOperator node)
        {
            VisitExpression(node.Left);  // left is both read and written
            VisitExpression(node.Right);
            Assign(node.Left, node.Right);
            return null;
        }

        /// <summary>
        /// Called at the point in a loop where the backwards branch would go to.
        /// </summary>
        void LoopHead(BoundLoopStatement node)
        {
            if (loopHeadState != null)
            {
                FlowAnalysisLocalState previousState;
                if (loopHeadState.TryGetValue(node, out previousState))
                {
                    previousState.Join(this.state);
                    this.state.Join(previousState);
                }
                else
                {
                    loopHeadState[node] = this.state.Clone();
                }
            }
        }

        /// <summary>
        /// Called at the point in a loop where the backward branch is placed.
        /// </summary>
        void LoopTail(BoundLoopStatement node)
        {
            if (loopHeadState != null)
            {
                if (loopHeadState[node].Join(this.state))
                {
                    this.backwardBranchChanged = true;
                }
            }
        }

        /// <summary>
        /// Used to resolve break statements in each statement form that has a break statement
        /// (loops, switch).
        /// </summary>
        /// <param name="oldPendingBranches"></param>
        /// <param name="breakState"></param>
        /// <param name="breakLabel"></param>
        private void ResolveBreaks(ArrayBuilder<PendingBranch> oldPendingBranches, FlowAnalysisLocalState breakState, LabelSymbol breakLabel)
        {
            foreach (var pending in this.pendingBranches)
            {
                switch (pending.Branch.Kind)
                {
                    case BoundKind.BreakStatement:
                        Debug.Assert((pending.Branch as BoundBreakStatement).Label == breakLabel);
                        breakState.Join(pending.State);
                        break;
                    default:
                        oldPendingBranches.Add(pending);
                        break;
                }
            }

            this.pendingBranches.Free();
            this.pendingBranches = oldPendingBranches;
            this.state = breakState;
        }

        /// <summary>
        /// Used to resolve continue statements in each statement form that supports it.
        /// </summary>
        /// <param name="continueLabal"></param>
        private void ResolveContinues(LabelSymbol continueLabal)
        {
            var newPendingBranches = ArrayBuilder<PendingBranch>.GetInstance();
            foreach (var pending in this.pendingBranches)
            {
                switch (pending.Branch.Kind)
                {
                    case BoundKind.ContinueStatement:
                        Debug.Assert((pending.Branch as BoundContinueStatement).Label == continueLabal);
                        // Technically, nothing in the language specification depends on the state
                        // at the continue label, so we could just discard them instead of merging
                        // the states. In fact, we need not have added continue statements to the
                        // pending jump queue in the first place if we were interested solely in the
                        // flow analysis.  However, region analysis (in support of extract method)
                        // depends on continue statements appearing in the pending branch queue, so
                        // we process them from the queue here.
                        this.state.Join(pending.State);
                        break;

                    default:
                        newPendingBranches.Add(pending);
                        break;
                }
            }
            this.pendingBranches.Free();
            this.pendingBranches = newPendingBranches;
        }

        /// <summary>
        /// Subclasses override this if they want to take special actions on processing a goto
        /// statement, when both the jump and the label have been located.
        /// </summary>
        /// <param name="pending"></param>
        /// <param name="gotoStmt"></param>
        /// <param name="labelStmt"></param>
        protected virtual void NoteBranch(PendingBranch pending, BoundGotoStatement gotoStmt, BoundLabelStatement labelStmt)
        {
        }

        /// <summary>
        /// To handle a label, we resolve all branches to that label.  Returns true if the state of
        /// the label changes as a result.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private bool ResolveBranches(BoundLabelStatement target)
        {
            bool labelStateChanged = false;
            var newPendingBranches = ArrayBuilder<PendingBranch>.GetInstance();
            foreach (var pending in this.pendingBranches)
            {
                var branch = pending.Branch as BoundGotoStatement;
                if (branch != null && branch.Label == target.Label)
                {
                    var state = LabelState(target.Label);
                    NoteBranch(pending, branch, target);
                    labelStateChanged |= state.Value.Join(pending.State);
                }
                else
                {
                    newPendingBranches.Add(pending);
                }
            }

            this.pendingBranches.Free();
            this.pendingBranches = newPendingBranches;
            return labelStateChanged;
        }

        /// <summary>
        /// Since branches cannot branch into constructs, only out, we save the pending branches when visiting more nested constructs.
        /// </summary>
        /// <returns></returns>
        protected ArrayBuilder<PendingBranch> SavePending()
        {
            var oldPending = this.pendingBranches;
            this.pendingBranches = ArrayBuilder<PendingBranch>.GetInstance();
            return oldPending;
        }

        /// <summary>
        /// We use this to restore the old set of pending branches after visiting a construct that contains nested statements.
        /// </summary>
        /// <param name="oldPending">The old pending branches, which are to be merged with the current ones</param>
        protected void RestorePending(ArrayBuilder<PendingBranch> oldPending)
        {
            oldPending.AddRange(this.pendingBranches);
            this.pendingBranches.Free();
            this.pendingBranches = oldPending;
        }

        /// <summary>
        /// Report an unimplemented language construct.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="feature"></param>
        /// <returns></returns>
        protected object Unimplemented(BoundNode node, String feature)
        {
            Diagnostics.Add(ErrorCode.ERR_NotYetImplementedInRoslyn, new SourceLocation(tree, node.Syntax), feature);
            return null;
        }
    }
}