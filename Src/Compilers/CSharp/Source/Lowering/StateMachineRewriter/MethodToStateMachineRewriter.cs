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
        private readonly HashSet<Symbol> variablesCaptured;
        protected override HashSet<Symbol> VariablesCaptured
        {
            get { return this.variablesCaptured; }
        }

        public MethodToStateMachineRewriter(
            SyntheticBoundNodeFactory F,
            MethodSymbol originalMethod,
            FieldSymbol state,
            HashSet<Symbol> variablesCaptured,
            Dictionary<Symbol, CapturedSymbolReplacement> initialProxies,
            DiagnosticBag diagnostics,
            bool useFinalizerBookkeeping,
            bool generateDebugInfo)
            : base(F.CompilationState, diagnostics, generateDebugInfo)
        {
            this.F = F;
            this.stateField = state;
            this.cachedState = F.SynthesizedLocal(F.SpecialType(SpecialType.System_Int32), "cachedState");
            this.variablesCaptured = variablesCaptured;
            this.useFinalizerBookkeeping = useFinalizerBookkeeping;
            this.hasFinalizerState = useFinalizerBookkeeping;
            this.originalMethod = originalMethod;
            foreach (var p in initialProxies) this.proxies.Add(p.Key, p.Value);
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
        /// A pool of fields used to hoist temporary variables.  They appear in this set when not in scope,
        /// so that members of this set may be allocated to temps when the temps come into scope.
        /// </summary>
        private ArrayBuilder<SynthesizedFieldSymbolBase> availableFields = new ArrayBuilder<SynthesizedFieldSymbolBase>();

        /// <summary>
        /// A map from the identity of a temp local symbol in the original method to the set of fields
        /// allocated to the temp in the translation.  When a temp goes out of scope, it is "freed" up
        /// for allocation to other temps of the same type.
        /// </summary>
        Dictionary<LocalSymbol, ArrayBuilder<SynthesizedFieldSymbolBase>> freeTempsMap = new Dictionary<LocalSymbol, ArrayBuilder<SynthesizedFieldSymbolBase>>();

        /// <summary>
        /// Fields allocated for temporary variables are given unique names distinguished by a number at the end.
        /// This counter ensures they are unique within a given translated method.
        /// </summary>
        private int nextTempNumber = 1;

        /// <summary>
        /// Used to compute if a struct type is actually empty.
        /// </summary>
        private EmptyStructTypeCache emptyStructTypeCache = new EmptyStructTypeCache(null);

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
            if (!node.Locals.IsDefaultOrEmpty)
            {
                foreach (var local in node.Locals)
                {
                    Debug.Assert(!VariablesCaptured.Contains(local) || proxies.ContainsKey(local));
                }
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
        private BoundNode PossibleIteratorScope(ImmutableArray<LocalSymbol> locals, Func<BoundStatement> wrapped)
        {
            if (locals.IsDefaultOrEmpty) return wrapped();
            var proxyFields = ArrayBuilder<SynthesizedFieldSymbolBase>.GetInstance();
            foreach (var local in locals)
            {
                if (!VariablesCaptured.Contains(local)) continue;
                CapturedSymbolReplacement proxy;
                if (proxies.TryGetValue(local, out proxy))
                {
                    // All of the user-declared variables have pre-allocated proxies
                    var field = proxy.HoistedField;
                    Debug.Assert((object)field != null);
                    Debug.Assert(local.DeclarationKind != LocalDeclarationKind.CompilerGenerated); // temps have lazily allocated proxies
                    if (local.DeclarationKind != LocalDeclarationKind.CompilerGenerated) proxyFields.Add(field);
                }
                else
                {
                    if (local.RefKind == RefKind.None)
                    {
                        SynthesizedFieldSymbolBase field = MakeHoistedTemp(local, TypeMap.SubstituteType(local.Type));
                        proxy = new CapturedToFrameSymbolReplacement(field);
                        proxies.Add(local, proxy);
                    }
                    // ref temporary variables have proxies that are allocated on demand
                    // See VisitAssignmentOperator.
                }
            }

            var translatedStatement = wrapped();

            // produce code to free (e.g. mark as available for reuse) and clear (e.g. set to null) the proxies for any temps for these locals
            var clearTemps = ArrayBuilder<BoundAssignmentOperator>.GetInstance();
            foreach (var local in locals)
            {
                ArrayBuilder<SynthesizedFieldSymbolBase> frees;
                if (freeTempsMap.TryGetValue(local, out frees))
                {
                    Debug.Assert(local.DeclarationKind == LocalDeclarationKind.CompilerGenerated); // only temps are managed this way
                    freeTempsMap.Remove(local);
                    foreach (var field in frees)
                    {
                        if (MightContainReferences(field.Type))
                        {
                            clearTemps.Add(F.AssignmentExpression(F.Field(F.This(), field), F.NullOrDefault(field.Type)));
                        }
                        FreeTemp(field);
                    }
                    frees.Free();
                }
            }

            if (clearTemps.Count != 0)
            {
                translatedStatement = F.Block(
                    translatedStatement,
                    F.Block(clearTemps.Select(e => F.ExpressionStatement(e)).AsImmutable<BoundStatement>())
                    );
            }
            clearTemps.Free();

            // wrap the node in an iterator scope for debugging
            if (proxyFields.Count != 0)
            {
                translatedStatement = new BoundIteratorScope(F.Syntax, proxyFields.ToImmutable(), translatedStatement);
            }
            proxyFields.Free();

            return translatedStatement;
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
            CSharpCompilation Compilation = this.CompilationState.ModuleBuilderOpt.Compilation;
            if (type.DeclaringCompilation != Compilation) return true; // perhaps from ref assembly
            if (emptyStructTypeCache.IsEmptyStructType(type)) return false;
            foreach (var f in type.GetMembers())
            {
                if (f.Kind == SymbolKind.Field && !f.IsStatic && MightContainReferences(((FieldSymbol)f).Type)) return true;
            }
            return false;
        }

        /// <summary>
        /// Allocate a temp of the given type, and note that it should be freed and cleared when the
        /// given local symbol goes out of scope.
        /// </summary>
        private SynthesizedFieldSymbolBase MakeHoistedTemp(LocalSymbol local, TypeSymbol type)
        {
            Debug.Assert(local.DeclarationKind == LocalDeclarationKind.CompilerGenerated);
            SynthesizedFieldSymbolBase result = AllocTemp(type);
            ArrayBuilder<SynthesizedFieldSymbolBase> freeFields;
            if (!this.freeTempsMap.TryGetValue(local, out freeFields))
            {
                this.freeTempsMap.Add(local, freeFields = ArrayBuilder<SynthesizedFieldSymbolBase>.GetInstance());
            }
            freeFields.Add(result);
            return result;
        }

        /// <summary>
        /// Allocate a field of the state machine of the given type, to serve as a temporary.
        /// </summary>
        private SynthesizedFieldSymbolBase AllocTemp(TypeSymbol type)
        {
            SynthesizedFieldSymbolBase result = null;

            // See if we've allocated a temp field we can reuse.  Not particularly efficient, but
            // there should not normally be a lot of hoisted temps.
            for (int i = 0; i < availableFields.Count; i++)
            {
                SynthesizedFieldSymbolBase f = availableFields[i];
                if (f.Type == type)
                {
                    result = f;
                    availableFields.RemoveAt(i);
                    break;
                }
            }

            if ((object)result == null)
            {
                var fieldName = GeneratedNames.SpillTempName(nextTempNumber++);
                result = F.StateMachineField(type, fieldName, isPublic: true);
            }

            return result;
        }

        /// <summary>
        /// Add a state machine field to the set of fields available for allocation to temps
        /// </summary>
        private void FreeTemp(SynthesizedFieldSymbolBase field)
        {
            availableFields.Add(field);
        }

        private BoundExpression HoistRefInitialization(LocalSymbol local, BoundAssignmentOperator node)
        {
            var right = (BoundExpression)Visit(node.Right);
            var sideEffects = ArrayBuilder<BoundExpression>.GetInstance();
            bool needsSacrificialEvaluation = false;
            var replacement = HoistExpression(local, right, true, sideEffects, ref needsSacrificialEvaluation);
            proxies.Add(local, new CapturedToExpressionSymbolReplacement(replacement));
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
            LocalSymbol top,
            BoundExpression expr,
            bool isRef,
            ArrayBuilder<BoundExpression> sideEffects,
            ref bool needsSacrificialEvaluation)
        {
            switch (expr.Kind)
            {
                case BoundKind.ArrayAccess:
                    {
                        var array = (BoundArrayAccess)expr;
                        BoundExpression expression = HoistExpression(top, array.Expression, false, sideEffects, ref needsSacrificialEvaluation);
                        var indices = ArrayBuilder<BoundExpression>.GetInstance();
                        foreach (var index in array.Indices)
                        {
                            indices.Add(HoistExpression(top, index, false, sideEffects, ref needsSacrificialEvaluation));
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
                        if (!isRef) goto default;
                        var isFieldOfStruct = !field.FieldSymbol.ContainingType.IsReferenceType;
                        var receiver = HoistExpression(top, field.ReceiverOpt, isFieldOfStruct, sideEffects, ref needsSacrificialEvaluation);
                        if (receiver.Kind != BoundKind.ThisReference && !isFieldOfStruct) needsSacrificialEvaluation = true; // need the null check in field receiver
                        return F.Field(receiver, field.FieldSymbol);
                    }
                case BoundKind.ThisReference:
                case BoundKind.BaseReference:
                case BoundKind.DefaultOperator:
                    return expr;
                default:
                    {
                        if (expr.ConstantValue != null) return expr;
                        if (isRef) throw ExceptionUtilities.UnexpectedValue(expr.Kind);
                        var hoistingField = MakeHoistedTemp(top, expr.Type);
                        var replacement = F.Field(F.This(), hoistingField);
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
            return PossibleIteratorScope(node.LocalsOpt, () => (BoundStatement)base.VisitBlock(node));
        }

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            Debug.Assert(node.OuterLocals.IsEmpty);
            return PossibleIteratorScope(node.InnerLocalsOpt, () => (BoundStatement)base.VisitSwitchStatement(node));
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
            if (node.Left.Kind != BoundKind.Local) return (BoundExpression)base.VisitAssignmentOperator(node);
            var left = (BoundLocal)node.Left;
            var local = left.LocalSymbol;
            if (!variablesCaptured.Contains(local)) return (BoundExpression)base.VisitAssignmentOperator(node);
            if (proxies.ContainsKey(local))
            {
                Debug.Assert(node.RefKind == RefKind.None);
                return (BoundExpression)base.VisitAssignmentOperator(node);
            }

            // user-declared variables are preassigned their proxies, and value temps
            // are assigned proxies at the beginning of their scope by the enclosing construct.
            // Here we handle ref temps.  Ref temps are the target of a ref assignment operator before
            // being used in any other way.
            Debug.Assert(local.DeclarationKind == LocalDeclarationKind.CompilerGenerated);
            Debug.Assert(node.RefKind != RefKind.None);

            // we have an assignment to a variable that has not yet been assigned a proxy.
            // So we assign the proxy before translating the assignment.
            return HoistRefInitialization(local, node);
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
                        tryBlock
                        );
                }
                else
                {
                    tryBlock = F.Block(
                        F.HiddenSequencePoint(),
                        Dispatch(),
                        tryBlock
                        );
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
