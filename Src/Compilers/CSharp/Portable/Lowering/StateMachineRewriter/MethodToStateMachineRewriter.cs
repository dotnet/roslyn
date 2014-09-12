// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class MethodToStateMachineRewriter : MethodToClassRewriter
    {
        public MethodToStateMachineRewriter(
            SyntheticBoundNodeFactory F,
            MethodSymbol originalMethod,
            FieldSymbol state,
            IReadOnlySet<Symbol> variablesCaptured,
            IReadOnlyDictionary<Symbol, CapturedSymbolReplacement> nonReusableLocalProxies,
            DiagnosticBag diagnostics,
            bool useFinalizerBookkeeping)
            : base(F.CompilationState, diagnostics)
        {
            Debug.Assert(F != null);
            Debug.Assert(originalMethod != null);
            Debug.Assert(state != null);
            Debug.Assert(nonReusableLocalProxies != null);
            Debug.Assert(diagnostics != null);
            Debug.Assert(variablesCaptured != null);

            this.F = F;
            this.stateField = state;
            this.cachedState = F.SynthesizedLocal(F.SpecialType(SpecialType.System_Int32), kind: SynthesizedLocalKind.StateMachineCachedState);
            this.useFinalizerBookkeeping = useFinalizerBookkeeping;
            this.hasFinalizerState = useFinalizerBookkeeping;
            this.originalMethod = originalMethod;
            this.variablesCaptured = variablesCaptured;

            foreach (var proxy in nonReusableLocalProxies)
            {
                this.proxies.Add(proxy.Key, proxy.Value);
            }
        }


        /// <summary>
        /// True if we need to generate the code to do the bookkeeping so we can "finalize" the state machine
        /// by executing code from its current state through the enclosing finally blocks.  This is true for
        /// iterators and false for async.
        /// </summary>
        private readonly bool useFinalizerBookkeeping;

        /// <summary>
        /// Generate return statements from the state machine method body.
        /// </summary>
        protected abstract BoundStatement GenerateReturn(bool finished);

        protected readonly SyntheticBoundNodeFactory F;

        /// <summary>
        /// The "state" of the state machine that is the translation of the iterator method.
        /// </summary>
        protected readonly FieldSymbol stateField;

        /// <summary>
        /// Cached "state" of the state machine within the MoveNext method.  We work with a copy of
        /// the state to avoid shared mutable state between threads.  (Two threads can be executing
        /// in a Task's MoveNext method because an awaited task may complete after the awaiter has
        /// tested whether the subtask is complete but before the awaiter has returned)
        /// </summary>
        protected readonly LocalSymbol cachedState;

        private int nextState = 0;

        /// <summary>
        /// For each distinct label, the set of states that need to be dispatched to that label.
        /// Note that there is a dispatch occurring at every try-finally statement, so this
        /// variable takes on a new set of values inside each try block.
        /// </summary>
        private Dictionary<LabelSymbol, List<int>> dispatches = new Dictionary<LabelSymbol, List<int>>();

        /// <summary>
        /// A mapping from each state of the state machine to the new state that will be used to execute
        /// finally blocks in case the state machine is disposed.  The Dispose method computes the new state
        /// and then runs MoveNext.  Not used if !this.useFinalizerBookkeeping.
        /// </summary>
        protected Dictionary<int, int> finalizerStateMap = new Dictionary<int, int>();

        /// <summary>
        /// A try block might have no state (transitions) within it, in which case it does not need
        /// to have a state to represent finalization.  This flag tells us whether the current try
        /// block that we are within has a finalizer state.  Initially true as we have the (trivial)
        /// finalizer state of -1 at the top level.  Not used if !this.useFinalizerBookkeeping.
        /// </summary>
        private bool hasFinalizerState = true;

        /// <summary>
        /// If hasFinalizerState is true, this is the state for finalization from anywhere in this
        /// try block.  Initially set to -1, representing the no-op finalization required at the top
        /// level.  Not used if !this.useFinalizerBookkeeping.
        /// </summary>
        private int currentFinalizerState = -1;

        /// <summary>
        /// A pool of fields used to hoist locals. They appear in this set when not in scope,
        /// so that members of this set may be allocated to locals when the locals come into scope.
        /// </summary>
        private Dictionary<TypeSymbol, ArrayBuilder<SynthesizedFieldSymbolBase>> availableHoistedFields = new Dictionary<TypeSymbol, ArrayBuilder<SynthesizedFieldSymbolBase>>(TypeSymbol.EqualsIgnoringDynamicComparer);

        /// <summary>
        /// Fields allocated for temporary variables are given unique names distinguished by a number at the end.
        /// This counter ensures they are unique within a given translated method.
        /// </summary>
        private int nextHoistedFieldId = 1;

        /// <summary>
        /// Used to enumerate the instance fields of a struct.
        /// </summary>
        private EmptyStructTypeCache emptyStructTypeCache = new NeverEmptyStructTypeCache();

        /// <summary>
        /// The set of captured variables seen in the method body.
        /// It's the minimal set of variables that have to be hoisted since their def-use arc crosses await/yield.
        /// Other variables may be hoisted to improve debugging experience.
        /// </summary>
        private readonly IReadOnlySet<Symbol> variablesCaptured;

        protected override bool NeedsProxy(Symbol localOrParameter)
        {
            Debug.Assert(localOrParameter.Kind == SymbolKind.Local || localOrParameter.Kind == SymbolKind.Parameter);
            return variablesCaptured.Contains(localOrParameter);
        }

        protected override TypeMap TypeMap
        {
            get { return ((SynthesizedContainer)F.CurrentClass).TypeMap; }
        }

        protected override MethodSymbol CurrentMethod
        {
            get { return F.CurrentMethod; }
        }

        private readonly MethodSymbol originalMethod;

        protected override NamedTypeSymbol ContainingType
        {
            get { return originalMethod.ContainingType; }
        }

        protected override BoundExpression FramePointer(CSharpSyntaxNode syntax, NamedTypeSymbol frameClass)
        {
            var oldSyntax = F.Syntax;
            F.Syntax = syntax;
            var result = F.This();
            Debug.Assert(frameClass == result.Type);
            F.Syntax = oldSyntax;
            return result;
        }

        protected void AddState(out int stateNumber, out GeneratedLabelSymbol resumeLabel)
        {
            stateNumber = nextState++;

            if (dispatches == null)
            {
                dispatches = new Dictionary<LabelSymbol, List<int>>();
            }

            if (this.useFinalizerBookkeeping && !hasFinalizerState)
            {
                currentFinalizerState = nextState++;
                hasFinalizerState = true;
            }

            resumeLabel = F.GenerateLabel("stateMachine");
            List<int> states = new List<int>();
            states.Add(stateNumber);
            dispatches.Add(resumeLabel, states);

            if (this.useFinalizerBookkeeping)
            {
                finalizerStateMap.Add(stateNumber, currentFinalizerState);
            }
        }

        protected BoundStatement Dispatch()
        {
            return F.Switch(F.Local(cachedState),
                    from kv in dispatches orderby kv.Value[0] select F.SwitchSection(kv.Value, F.Goto(kv.Key))
                    );
        }

#if DEBUG
        public override BoundNode VisitSequence(BoundSequence node)
        {
            // Spilled local temps do not appear here in a sequence expression, because any temps in a
            // sequence expression that need to be spilled would have been moved up to the
            // statement level by the AwaitLiftingRewriter.
            foreach (var local in node.Locals)
            {
                Debug.Assert(!NeedsProxy(local) || proxies.ContainsKey(local));
            }

            return base.VisitSequence(node);
        }
#endif

        /// <summary>
        /// Translate a statement that declares a given set of locals.  Also allocates and frees hoisted temps as
        /// required for the translation.
        /// </summary>
        /// <param name="locals">The set of locals declared in the original version of this statement</param>
        /// <param name="wrapped">A delegate to return the translation of the body of this statement</param>
        private BoundStatement PossibleIteratorScope(ImmutableArray<LocalSymbol> locals, Func<BoundStatement> wrapped)
        {
            if (locals.IsDefaultOrEmpty)
            {
                return wrapped();
            }

            var hoistedUserDefinedLocals = ArrayBuilder<SynthesizedFieldSymbolBase>.GetInstance();
            foreach (var local in locals)
            {
                if (!NeedsProxy(local))
                {
                    continue;
                }

                // Ref synthesized variables have proxies that are allocated in VisitAssignmentOperator.
                if (local.RefKind != RefKind.None) 
                {
                    Debug.Assert(local.SynthesizedLocalKind == SynthesizedLocalKind.AwaitSpill);
                    continue;
                }

                Debug.Assert(local.SynthesizedLocalKind == SynthesizedLocalKind.None || 
                             local.SynthesizedLocalKind.IsLongLived());

                CapturedSymbolReplacement proxy;
                if (!proxies.TryGetValue(local, out proxy))
                {
                    proxy = new CapturedToFrameSymbolReplacement(GetOrAllocateHoistedField(TypeMap.SubstituteType(local.Type)), isReusable: true);
                    proxies.Add(local, proxy);
                }

                if (local.SynthesizedLocalKind == SynthesizedLocalKind.None)
                {
                    hoistedUserDefinedLocals.Add(((CapturedToFrameSymbolReplacement)proxy).HoistedField);
                }
            }

            var translatedStatement = wrapped();
            var variableCleanup = ArrayBuilder<BoundAssignmentOperator>.GetInstance();

            // produce cleanup code for all fields of locals defined by this block 
            // as well as all proxies allocated by VisitAssignmentOperator within this block:
            foreach (var local in locals)
            {
                CapturedSymbolReplacement proxy;
                if (!proxies.TryGetValue(local, out proxy))
                {
                    continue;
                }

                var simpleProxy = proxy as CapturedToFrameSymbolReplacement;
                if (simpleProxy != null)
                {
                    AddVariableCleanup(variableCleanup, simpleProxy.HoistedField);

                    if (proxy.IsReusable)
                    {
                        FreeHoistedField(simpleProxy.HoistedField);
                    }
                }
                else
                {
                    foreach (var field in ((CapturedToExpressionSymbolReplacement)proxy).HoistedFields)
                    {
                        AddVariableCleanup(variableCleanup, field);

                        if (proxy.IsReusable)
                        {
                            FreeHoistedField(field);
                        }
                    }
                }
            }

            if (variableCleanup.Count != 0)
            {
                translatedStatement = F.Block(
                    translatedStatement,
                    F.Block(variableCleanup.SelectAsArray((e, f) => (BoundStatement)f.ExpressionStatement(e), F)));
            }

            variableCleanup.Free();

            // wrap the node in an iterator scope for debugging
            if (hoistedUserDefinedLocals.Count != 0)
            {
                translatedStatement = F.Block(new BoundIteratorScope(F.Syntax, hoistedUserDefinedLocals.ToImmutable(), translatedStatement));
            }

            hoistedUserDefinedLocals.Free();

            return translatedStatement;
        }

        private void AddVariableCleanup(ArrayBuilder<BoundAssignmentOperator> cleanup, FieldSymbol field)
        {
            if (MightContainReferences(field.Type))
            {
                cleanup.Add(F.AssignmentExpression(F.Field(F.This(), field), F.NullOrDefault(field.Type)));
            }
        }

        /// <summary>
        /// Might the given type be, or contain, managed references?  This is used to determine which
        /// fields allocated to temporaries should be cleared when the underlying variable goes out of scope, so
        /// that they do not cause unnecessary object retention.
        /// </summary>
        private bool MightContainReferences(TypeSymbol type)
        {
            if (type.IsReferenceType || type.TypeKind == TypeKind.TypeParameter) return true; // type parameter or reference type
            if (type.TypeKind != TypeKind.Struct) return false; // enums, etc
            if (type.SpecialType == SpecialType.System_TypedReference) return true;
            if (type.SpecialType != SpecialType.None) return false; // int, etc
            if (!type.IsFromCompilation(this.CompilationState.ModuleBuilderOpt.Compilation)) return true; // perhaps from ref assembly
            foreach (var f in emptyStructTypeCache.GetStructInstanceFields(type))
            {
                if (MightContainReferences(f.Type)) return true;
            }
            return false;
        }

        private SynthesizedFieldSymbolBase GetOrAllocateHoistedField(TypeSymbol type)
        {
            ArrayBuilder<SynthesizedFieldSymbolBase> fields;
            if (availableHoistedFields.TryGetValue(type, out fields) && fields.Count > 0)
            {
                var field = fields.Last();
                fields.RemoveLast();
                return field;
            }

            var fieldName = GeneratedNames.ReusableHoistedLocalFieldName(nextHoistedFieldId++);
            return F.StateMachineField(type, fieldName, isPublic: true);
        }

        private void FreeHoistedField(SynthesizedFieldSymbolBase field)
        {
            ArrayBuilder<SynthesizedFieldSymbolBase> fields;
            if (!availableHoistedFields.TryGetValue(field.Type, out fields))
            {
                availableHoistedFields.Add(field.Type, fields = new ArrayBuilder<SynthesizedFieldSymbolBase>());
            }

            fields.Add(field);
        }

        private BoundExpression HoistRefInitialization(LocalSymbol local, BoundAssignmentOperator node)
        {
            var right = (BoundExpression)Visit(node.Right);
            var sideEffects = ArrayBuilder<BoundExpression>.GetInstance();
            bool needsSacrificialEvaluation = false;

            var hoistedFields = ArrayBuilder<SynthesizedFieldSymbolBase>.GetInstance();
            var replacement = HoistExpression(right, true, sideEffects, hoistedFields, ref needsSacrificialEvaluation);

            Debug.Assert(local.SynthesizedLocalKind == SynthesizedLocalKind.AwaitSpill);
            proxies.Add(local, new CapturedToExpressionSymbolReplacement(replacement, hoistedFields.ToImmutableAndFree()));

            if (needsSacrificialEvaluation)
            {
                var type = TypeMap.SubstituteType(local.Type);
                var sacrificalTemp = F.SynthesizedLocal(type, refKind: RefKind.Ref);
                Debug.Assert(type == replacement.Type);
                return F.Sequence(ImmutableArray.Create(sacrificalTemp), sideEffects.ToImmutableAndFree(), F.AssignmentExpression(F.Local(sacrificalTemp), replacement, refKind: RefKind.Ref));
            }
            else if (sideEffects.Count == 0)
            {
                sideEffects.Free();
                return null;
            }
            else
            {
                var last = sideEffects.Last();
                sideEffects.RemoveLast();
                return F.Sequence(ImmutableArray<LocalSymbol>.Empty, sideEffects.ToImmutableAndFree(), last);
            }
        }

        private BoundExpression HoistExpression(
            BoundExpression expr,
            bool isRef,
            ArrayBuilder<BoundExpression> sideEffects,
            ArrayBuilder<SynthesizedFieldSymbolBase> hoistedFields,
            ref bool needsSacrificialEvaluation)
        {
            switch (expr.Kind)
            {
                case BoundKind.ArrayAccess:
                    {
                        var array = (BoundArrayAccess)expr;
                        BoundExpression expression = HoistExpression(array.Expression, false, sideEffects, hoistedFields, ref needsSacrificialEvaluation);
                        var indices = ArrayBuilder<BoundExpression>.GetInstance();
                        foreach (var index in array.Indices)
                        {
                            indices.Add(HoistExpression(index, false, sideEffects, hoistedFields, ref needsSacrificialEvaluation));
                        }

                        needsSacrificialEvaluation = true; // need to force array index out of bounds exceptions
                        return array.Update(expression, indices.ToImmutableAndFree(), array.Type);
                    }

                case BoundKind.FieldAccess:
                    {
                        var field = (BoundFieldAccess)expr;
                        if (field.FieldSymbol.IsStatic)
                        {
                            // the address of a static field, and the value of a readonly static field, is stable
                            if (isRef || field.FieldSymbol.IsReadOnly) return expr;
                            goto default;
                        }

                        if (!isRef)
                        {
                            goto default;
                        }

                        var isFieldOfStruct = !field.FieldSymbol.ContainingType.IsReferenceType;
                        
                        var receiver = HoistExpression(field.ReceiverOpt, isFieldOfStruct, sideEffects, hoistedFields, ref needsSacrificialEvaluation);
                        if (receiver.Kind != BoundKind.ThisReference && !isFieldOfStruct)
                        {
                            needsSacrificialEvaluation = true; // need the null check in field receiver
                        }

                        return F.Field(receiver, field.FieldSymbol);
                    }

                case BoundKind.ThisReference:
                case BoundKind.BaseReference:
                case BoundKind.DefaultOperator:
                    return expr;

                default:
                    {
                        if (expr.ConstantValue != null)
                        {
                            return expr;
                        }

                        if (isRef)
                        {
                            throw ExceptionUtilities.UnexpectedValue(expr.Kind);
                        }

                        var hoistedField = GetOrAllocateHoistedField(expr.Type);
                        hoistedFields.Add(hoistedField);

                        var replacement = F.Field(F.This(), hoistedField);
                        sideEffects.Add(F.AssignmentExpression(replacement, expr));
                        return replacement;
                    }
            }
        }

        #region Visitors

        public override BoundNode Visit(BoundNode node)
        {
            if (node == null) return node;
            var oldSyntax = F.Syntax;
            F.Syntax = node.Syntax;
            var result = base.Visit(node);
            F.Syntax = oldSyntax;
            return result;
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            return PossibleIteratorScope(node.Locals, () => (BoundStatement)base.VisitBlock(node));
        }

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            Debug.Assert(node.OuterLocals.IsEmpty);
            return PossibleIteratorScope(node.InnerLocals, () => (BoundStatement)base.VisitSwitchStatement(node));
        }

        public override BoundNode VisitForStatement(BoundForStatement node)
        {
            throw ExceptionUtilities.Unreachable; // for statements have been lowered away by now
        }

        public override BoundNode VisitUsingStatement(BoundUsingStatement node)
        {
            throw ExceptionUtilities.Unreachable; // using statements have been lowered away by now
        }

        public override BoundNode VisitExpressionStatement(BoundExpressionStatement node)
        {
            // ref assignments might be translated away (into nothing).  If so just
            // return no statement.  The enclosing statement list will just omit it.
            BoundExpression expression = (BoundExpression)this.Visit(node.Expression);
            return (expression == null) ? null : node.Update(expression);
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            if (node.Left.Kind != BoundKind.Local)
            {
                return base.VisitAssignmentOperator(node);
            }

            var leftLocal = ((BoundLocal)node.Left).LocalSymbol;
            if (!NeedsProxy(leftLocal))
            {
                return base.VisitAssignmentOperator(node);
            }

            if (proxies.ContainsKey(leftLocal))
            {
                Debug.Assert(node.RefKind == RefKind.None);
                return base.VisitAssignmentOperator(node);
            }

            // User-declared variables are preassigned their proxies, and by-value synthesized variables
            // are assigned proxies at the beginning of their scope by the enclosing construct.
            // Here we handle ref temps. By-ref synthesized variables are the target of a ref assignment operator before
            // being used in any other way.

            Debug.Assert(leftLocal.SynthesizedLocalKind == SynthesizedLocalKind.AwaitSpill);
            Debug.Assert(node.RefKind != RefKind.None);

            // We have an assignment to a variable that has not yet been assigned a proxy.
            // So we assign the proxy before translating the assignment.
            return HoistRefInitialization(leftLocal, node);
        }

        /// <summary>
        /// The try statement is the most complex part of the state machine transformation.
        /// Since the CLR will not allow a 'goto' into the scope of a try statement, we must
        /// generate the dispatch to the state's label stepwise.  That is done by translating
        /// the try statements from the inside to the outside.  Within a try statement, we
        /// start with an empty dispatch table (representing the mapping from state numbers
        /// to labels).  During translation of the try statement's body, the dispatch table
        /// will be filled in with the data necessary to dispatch once we're inside the try
        /// block.  We generate that at the head of the translated try statement.  Then, we
        /// copy all of the states from that table into the table for the enclosing construct,
        /// but associate them with a label just before the translated try block.  That way
        /// the enclosing construct will generate the code necessary to get control into the
        /// try block for all of those states.
        /// </summary>
        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            var oldDispatches = dispatches;
            var oldFinalizerState = currentFinalizerState;
            var oldHasFinalizerState = hasFinalizerState;

            dispatches = null;
            currentFinalizerState = -1;
            hasFinalizerState = false;

            BoundBlock tryBlock = F.Block((BoundStatement)this.Visit(node.TryBlock));
            GeneratedLabelSymbol dispatchLabel = null;
            if (dispatches != null)
            {
                dispatchLabel = F.GenerateLabel("tryDispatch");
                if (hasFinalizerState)
                {
                    // cause the current finalizer state to arrive here and then "return false"
                    var finalizer = F.GenerateLabel("finalizer");
                    dispatches.Add(finalizer, new List<int>() { this.currentFinalizerState });
                    var skipFinalizer = F.GenerateLabel("skipFinalizer");
                    tryBlock = F.Block(
                        F.HiddenSequencePoint(),
                        Dispatch(),
                        F.Goto(skipFinalizer),
                        F.Label(finalizer), // code for the finalizer here
                        F.Assignment(F.Field(F.This(), stateField), F.AssignmentExpression(F.Local(cachedState), F.Literal(StateMachineStates.NotStartedStateMachine))),
                        GenerateReturn(false),
                        F.Label(skipFinalizer),
                        tryBlock);
                }
                else
                {
                    tryBlock = F.Block(
                        F.HiddenSequencePoint(),
                        Dispatch(),
                        tryBlock);
                }

                if (oldDispatches == null)
                {
                    Debug.Assert(!oldHasFinalizerState);
                    oldDispatches = new Dictionary<LabelSymbol, List<int>>();
                }

                oldDispatches.Add(dispatchLabel, new List<int>(from kv in dispatches.Values from n in kv orderby n select n));
            }

            hasFinalizerState = oldHasFinalizerState;
            currentFinalizerState = oldFinalizerState;
            dispatches = oldDispatches;

            ImmutableArray<BoundCatchBlock> catchBlocks = this.VisitList(node.CatchBlocks);
            BoundBlock finallyBlockOpt = node.FinallyBlockOpt == null ? null : F.Block(
                F.HiddenSequencePoint(),
                F.If(
                    condition: F.IntLessThan(F.Local(cachedState), F.Literal(StateMachineStates.FirstUnusedState)),
                    thenClause: (BoundBlock)this.Visit(node.FinallyBlockOpt)
                ),
                F.HiddenSequencePoint());

            BoundStatement result = node.Update(tryBlock, catchBlocks, finallyBlockOpt, node.PreferFaultHandler);
            if ((object)dispatchLabel != null)
            {
                result = F.Block(
                    F.HiddenSequencePoint(),
                    F.Label(dispatchLabel),
                    result);
            }

            return result;
        }

        public override BoundNode VisitThisReference(BoundThisReference node)
        {
            var thisParameter = this.originalMethod.ThisParameter;
            CapturedSymbolReplacement proxy;
            if ((object)thisParameter == null || !proxies.TryGetValue(thisParameter, out proxy))
            {
                // This can occur in a delegate creation expression because the method group
                // in the argument can have a "this" receiver even when "this"
                // is not captured because a static method is selected.  But we do preserve
                // the method group and its receiver in the bound tree, so the "this"
                // receiver must be rewritten.

                //TODO: It seems we may capture more than needed here.

                // TODO: Why don't we drop "this" while lowering if method is static? 
                //       Actually, considering that method group expression does not evaluate to a particular value 
                //       why do we have it in the lowered tree at all?
                return node.Update(VisitType(node.Type));
            }
            else
            {
                Debug.Assert(proxy != null);
                return proxy.Replacement(F.Syntax, frameType => F.This());
            }
        }

        public override BoundNode VisitBaseReference(BoundBaseReference node)
        {
            // TODO: fix up the type of the resulting node to be the base type
            CapturedSymbolReplacement proxy = proxies[this.originalMethod.ThisParameter];
            Debug.Assert(proxy != null);
            return proxy.Replacement(F.Syntax, frameType => F.This());
        }

        #endregion
    }
}
