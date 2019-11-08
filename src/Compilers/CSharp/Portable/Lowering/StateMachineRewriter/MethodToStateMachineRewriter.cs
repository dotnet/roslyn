// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class MethodToStateMachineRewriter : MethodToClassRewriter
    {
        internal readonly MethodSymbol OriginalMethod;

        /// <summary>
        /// True if we need to generate the code to do the bookkeeping so we can "finalize" the state machine
        /// by executing code from its current state through the enclosing finally blocks.  This is true for
        /// iterators and false for async.
        /// </summary>
        private readonly bool _useFinalizerBookkeeping;

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

        /// <summary>
        /// Cached "this" local, used to store the captured "this", which is safe to cache locally since "this" 
        /// is semantically immutable.
        /// It would be hard for such caching to happen at JIT level (since JIT does not know that it never changes).
        /// NOTE: this field is null when we are not caching "this" which happens when
        ///       - not optimizing
        ///       - method is not capturing "this" at all
        ///       - containing type is a struct 
        ///       (we could cache "this" as a ref local for struct containers, 
        ///       but such caching would not save as much indirection and could actually 
        ///       be done at JIT level, possibly more efficiently)
        /// </summary>
        protected readonly LocalSymbol cachedThis;

        private int _nextState;

        /// <summary>
        /// For each distinct label, the set of states that need to be dispatched to that label.
        /// Note that there is a dispatch occurring at every try-finally statement, so this
        /// variable takes on a new set of values inside each try block.
        /// </summary>
        private Dictionary<LabelSymbol, List<int>> _dispatches = new Dictionary<LabelSymbol, List<int>>();

        /// <summary>
        /// A try block might have no state (transitions) within it, in which case it does not need
        /// to have a state to represent finalization.  This flag tells us whether the current try
        /// block that we are within has a finalizer state.  Initially true as we have the (trivial)
        /// finalizer state of -1 at the top level.  Not used if !this.useFinalizerBookkeeping.
        /// </summary>
        private bool _hasFinalizerState = true;

        /// <summary>
        /// If hasFinalizerState is true, this is the state for finalization from anywhere in this
        /// try block.  Initially set to -1, representing the no-op finalization required at the top
        /// level.  Not used if !this.useFinalizerBookkeeping.
        /// </summary>
        private int _currentFinalizerState = -1;

        /// <summary>
        /// A pool of fields used to hoist locals. They appear in this set when not in scope,
        /// so that members of this set may be allocated to locals when the locals come into scope.
        /// </summary>
        private Dictionary<TypeSymbol, ArrayBuilder<StateMachineFieldSymbol>> _lazyAvailableReusableHoistedFields;

        /// <summary>
        /// Fields allocated for temporary variables are given unique names distinguished by a number at the end.
        /// This counter ensures they are unique within a given translated method.
        /// </summary>
        private int _nextHoistedFieldId = 1;

        /// <summary>
        /// Used to enumerate the instance fields of a struct.
        /// </summary>
        private readonly EmptyStructTypeCache _emptyStructTypeCache = EmptyStructTypeCache.CreateNeverEmpty();

        /// <summary>
        /// The set of local variables and parameters that were hoisted and need a proxy.
        /// </summary>
        private readonly IReadOnlySet<Symbol> _hoistedVariables;

        private readonly SynthesizedLocalOrdinalsDispenser _synthesizedLocalOrdinals;
        private int _nextFreeHoistedLocalSlot;

        // new:
        public MethodToStateMachineRewriter(
            SyntheticBoundNodeFactory F,
            MethodSymbol originalMethod,
            FieldSymbol state,
            IReadOnlySet<Symbol> hoistedVariables,
            IReadOnlyDictionary<Symbol, CapturedSymbolReplacement> nonReusableLocalProxies,
            SynthesizedLocalOrdinalsDispenser synthesizedLocalOrdinals,
            VariableSlotAllocator slotAllocatorOpt,
            int nextFreeHoistedLocalSlot,
            DiagnosticBag diagnostics,
            bool useFinalizerBookkeeping)
            : base(slotAllocatorOpt, F.CompilationState, diagnostics)
        {
            Debug.Assert(F != null);
            Debug.Assert(originalMethod != null);
            Debug.Assert(state != null);
            Debug.Assert(nonReusableLocalProxies != null);
            Debug.Assert(diagnostics != null);
            Debug.Assert(hoistedVariables != null);
            Debug.Assert(nextFreeHoistedLocalSlot >= 0);

            this.F = F;
            this.stateField = state;
            this.cachedState = F.SynthesizedLocal(F.SpecialType(SpecialType.System_Int32), syntax: F.Syntax, kind: SynthesizedLocalKind.StateMachineCachedState);
            _useFinalizerBookkeeping = useFinalizerBookkeeping;
            _hasFinalizerState = useFinalizerBookkeeping;
            this.OriginalMethod = originalMethod;
            _hoistedVariables = hoistedVariables;
            _synthesizedLocalOrdinals = synthesizedLocalOrdinals;
            _nextFreeHoistedLocalSlot = nextFreeHoistedLocalSlot;

            foreach (var proxy in nonReusableLocalProxies)
            {
                this.proxies.Add(proxy.Key, proxy.Value);
            }

            // create cache local for reference type "this" in Release
            var thisParameter = originalMethod.ThisParameter;
            CapturedSymbolReplacement thisProxy;
            if (thisParameter is object { Type: { IsReferenceType: true } } && proxies.TryGetValue(thisParameter, out thisProxy) && F is { Compilation: { Options: { OptimizationLevel: OptimizationLevel.Release } } })
            {
                BoundExpression thisProxyReplacement = thisProxy.Replacement(F.Syntax, frameType => F.This());
                this.cachedThis = F.SynthesizedLocal(thisProxyReplacement.Type, syntax: F.Syntax, kind: SynthesizedLocalKind.FrameCache);
            }
        }

        protected override bool NeedsProxy(Symbol localOrParameter)
        {
            Debug.Assert(localOrParameter.Kind == SymbolKind.Local || localOrParameter.Kind == SymbolKind.Parameter);
            return _hoistedVariables.Contains(localOrParameter);
        }

        protected override TypeMap TypeMap
        {
            get { return ((SynthesizedContainer)F.CurrentType).TypeMap; }
        }

        protected override MethodSymbol CurrentMethod
        {
            get { return F.CurrentFunction; }
        }

        protected override NamedTypeSymbol ContainingType
        {
            get { return OriginalMethod.ContainingType; }
        }

        internal IReadOnlySet<Symbol> HoistedVariables
        {
            get
            {
                return _hoistedVariables;
            }
        }

        protected override BoundExpression FramePointer(SyntaxNode syntax, NamedTypeSymbol frameClass)
        {
            var oldSyntax = F.Syntax;
            F.Syntax = syntax;
            var result = F.This();
            Debug.Assert(TypeSymbol.Equals(frameClass, result.Type, TypeCompareKind.ConsiderEverything2));
            F.Syntax = oldSyntax;
            return result;
        }

        protected void AddState(out int stateNumber, out GeneratedLabelSymbol resumeLabel)
        {
            stateNumber = _nextState++;

            if (_useFinalizerBookkeeping && !_hasFinalizerState)
            {
                _currentFinalizerState = _nextState++;
                _hasFinalizerState = true;
            }

            AddState(stateNumber, out resumeLabel);
        }

        protected void AddState(int stateNumber, out GeneratedLabelSymbol resumeLabel)
        {
            if (_dispatches == null)
            {
                _dispatches = new Dictionary<LabelSymbol, List<int>>();
            }

            resumeLabel = F.GenerateLabel("stateMachine");
            var states = new List<int>();
            states.Add(stateNumber);
            _dispatches.Add(resumeLabel, states);
        }

        protected BoundStatement Dispatch()
        {
            return F.Switch(F.Local(cachedState),
                    (from kv in _dispatches orderby kv.Value[0] select F.SwitchSection(kv.Value, F.Goto(kv.Key))).ToImmutableArray()
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

            var hoistedLocalsWithDebugScopes = ArrayBuilder<StateMachineFieldSymbol>.GetInstance();
            foreach (var local in locals)
            {
                if (!NeedsProxy(local))
                {
                    continue;
                }

                // Ref synthesized variables have proxies that are allocated in VisitAssignmentOperator.
                if (local.RefKind != RefKind.None)
                {
                    Debug.Assert(local.SynthesizedKind == SynthesizedLocalKind.Spill);
                    continue;
                }

                CapturedSymbolReplacement proxy;
                bool reused = false;
                if (!proxies.TryGetValue(local, out proxy))
                {
                    proxy = new CapturedToStateMachineFieldReplacement(GetOrAllocateReusableHoistedField(TypeMap.SubstituteType(local.Type).Type, out reused, local), isReusable: true);
                    proxies.Add(local, proxy);
                }

                // We need to produce hoisted local scope debug information for user locals as well as 
                // lambda display classes, since Dev12 EE uses them to determine which variables are displayed 
                // in Locals window.
                if ((local.SynthesizedKind == SynthesizedLocalKind.UserDefined && local.ScopeDesignatorOpt?.Kind() != SyntaxKind.SwitchSection) ||
                    local.SynthesizedKind == SynthesizedLocalKind.LambdaDisplayClass)
                {
                    // NB: This is the case when the local backed by recycled field will not be visible in debugger.
                    //     It may be possible in the future, but for now a backing field can be mapped only to a single local.
                    if (!reused)
                    {
                        hoistedLocalsWithDebugScopes.Add(((CapturedToStateMachineFieldReplacement)proxy).HoistedField);
                    }
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

                var simpleProxy = proxy as CapturedToStateMachineFieldReplacement;
                if (simpleProxy != null)
                {
                    AddVariableCleanup(variableCleanup, simpleProxy.HoistedField);

                    if (proxy.IsReusable)
                    {
                        FreeReusableHoistedField(simpleProxy.HoistedField);
                    }
                }
                else
                {
                    foreach (var field in ((CapturedToExpressionSymbolReplacement)proxy).HoistedFields)
                    {
                        AddVariableCleanup(variableCleanup, field);

                        if (proxy.IsReusable)
                        {
                            FreeReusableHoistedField(field);
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
            if (hoistedLocalsWithDebugScopes.Count != 0)
            {
                translatedStatement = MakeStateMachineScope(hoistedLocalsWithDebugScopes.ToImmutable(), translatedStatement);
            }

            hoistedLocalsWithDebugScopes.Free();

            return translatedStatement;
        }

        /// <remarks>
        /// Must remain in sync with <see cref="TryUnwrapBoundStateMachineScope"/>.
        /// </remarks>
        internal BoundBlock MakeStateMachineScope(ImmutableArray<StateMachineFieldSymbol> hoistedLocals, BoundStatement statement)
        {
            return F.Block(new BoundStateMachineScope(F.Syntax, hoistedLocals, statement));
        }

        /// <remarks>
        /// Must remain in sync with <see cref="MakeStateMachineScope"/>.
        /// </remarks>
        internal static bool TryUnwrapBoundStateMachineScope(ref BoundStatement statement, out ImmutableArray<StateMachineFieldSymbol> hoistedLocals)
        {
            if (statement.Kind == BoundKind.Block)
            {
                var rewrittenBlock = (BoundBlock)statement;
                var rewrittenStatements = rewrittenBlock.Statements;
                if (rewrittenStatements.Length == 1 && rewrittenStatements[0].Kind == BoundKind.StateMachineScope)
                {
                    var stateMachineScope = (BoundStateMachineScope)rewrittenStatements[0];
                    statement = stateMachineScope.Statement;
                    hoistedLocals = stateMachineScope.Fields;
                    return true;
                }
            }

            hoistedLocals = ImmutableArray<StateMachineFieldSymbol>.Empty;
            return false;
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
            foreach (var f in _emptyStructTypeCache.GetStructInstanceFields(type))
            {
                if (MightContainReferences(f.Type)) return true;
            }
            return false;
        }

        private StateMachineFieldSymbol GetOrAllocateReusableHoistedField(TypeSymbol type, out bool reused, LocalSymbol local = null)
        {
            ArrayBuilder<StateMachineFieldSymbol> fields;
            if (_lazyAvailableReusableHoistedFields != null && _lazyAvailableReusableHoistedFields.TryGetValue(type, out fields) && fields.Count > 0)
            {
                var field = fields.Last();
                fields.RemoveLast();
                reused = true;
                return field;
            }

            reused = false;
            var slotIndex = _nextHoistedFieldId++;

            if (local?.SynthesizedKind == SynthesizedLocalKind.UserDefined)
            {
                string fieldName = GeneratedNames.MakeHoistedLocalFieldName(SynthesizedLocalKind.UserDefined, slotIndex, local.Name);
                return F.StateMachineField(type, fieldName, SynthesizedLocalKind.UserDefined, slotIndex);
            }

            return F.StateMachineField(type, GeneratedNames.ReusableHoistedLocalFieldName(slotIndex));
        }

        private void FreeReusableHoistedField(StateMachineFieldSymbol field)
        {
            ArrayBuilder<StateMachineFieldSymbol> fields;
            if (_lazyAvailableReusableHoistedFields == null || !_lazyAvailableReusableHoistedFields.TryGetValue(field.Type, out fields))
            {
                if (_lazyAvailableReusableHoistedFields == null)
                {
                    _lazyAvailableReusableHoistedFields = new Dictionary<TypeSymbol, ArrayBuilder<StateMachineFieldSymbol>>(TypeSymbol.EqualsIgnoringDynamicTupleNamesAndNullabilityComparer);
                }

                _lazyAvailableReusableHoistedFields.Add(field.Type, fields = new ArrayBuilder<StateMachineFieldSymbol>());
            }

            fields.Add(field);
        }

        private BoundExpression HoistRefInitialization(SynthesizedLocal local, BoundAssignmentOperator node)
        {
            Debug.Assert(local.SynthesizedKind == SynthesizedLocalKind.Spill);
            Debug.Assert(local.SyntaxOpt != null);
            Debug.Assert(this.OriginalMethod.IsAsync);

            var right = (BoundExpression)Visit(node.Right);

            var sideEffects = ArrayBuilder<BoundExpression>.GetInstance();
            bool needsSacrificialEvaluation = false;
            var hoistedFields = ArrayBuilder<StateMachineFieldSymbol>.GetInstance();

            AwaitExpressionSyntax awaitSyntaxOpt;
            int syntaxOffset;
            if (F.Compilation.Options.OptimizationLevel == OptimizationLevel.Debug)
            {
                awaitSyntaxOpt = (AwaitExpressionSyntax)local.GetDeclaratorSyntax();
                syntaxOffset = OriginalMethod.CalculateLocalSyntaxOffset(LambdaUtilities.GetDeclaratorPosition(awaitSyntaxOpt), awaitSyntaxOpt.SyntaxTree);
            }
            else
            {
                // These are only used to calculate debug id for ref-spilled variables, 
                // no need to do so in release build.
                awaitSyntaxOpt = null;
                syntaxOffset = -1;
            }

            var replacement = HoistExpression(right, awaitSyntaxOpt, syntaxOffset, local.RefKind, sideEffects, hoistedFields, ref needsSacrificialEvaluation);

            proxies.Add(local, new CapturedToExpressionSymbolReplacement(replacement, hoistedFields.ToImmutableAndFree(), isReusable: true));

            if (needsSacrificialEvaluation)
            {
                var type = TypeMap.SubstituteType(local.Type).Type;
                var sacrificialTemp = F.SynthesizedLocal(type, refKind: RefKind.Ref);
                Debug.Assert(TypeSymbol.Equals(type, replacement.Type, TypeCompareKind.ConsiderEverything2));
                return F.Sequence(ImmutableArray.Create(sacrificialTemp), sideEffects.ToImmutableAndFree(), F.AssignmentExpression(F.Local(sacrificialTemp), replacement, isRef: true));
            }

            if (sideEffects.Count == 0)
            {
                sideEffects.Free();
                return null;
            }

            var last = sideEffects.Last();
            sideEffects.RemoveLast();
            return F.Sequence(ImmutableArray<LocalSymbol>.Empty, sideEffects.ToImmutableAndFree(), last);
        }

        private BoundExpression HoistExpression(
            BoundExpression expr,
            AwaitExpressionSyntax awaitSyntaxOpt,
            int syntaxOffset,
            RefKind refKind,
            ArrayBuilder<BoundExpression> sideEffects,
            ArrayBuilder<StateMachineFieldSymbol> hoistedFields,
            ref bool needsSacrificialEvaluation)
        {
            switch (expr.Kind)
            {
                case BoundKind.ArrayAccess:
                    {
                        var array = (BoundArrayAccess)expr;
                        BoundExpression expression = HoistExpression(array.Expression, awaitSyntaxOpt, syntaxOffset, RefKind.None, sideEffects, hoistedFields, ref needsSacrificialEvaluation);
                        var indices = ArrayBuilder<BoundExpression>.GetInstance();
                        foreach (var index in array.Indices)
                        {
                            indices.Add(HoistExpression(index, awaitSyntaxOpt, syntaxOffset, RefKind.None, sideEffects, hoistedFields, ref needsSacrificialEvaluation));
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
                            if (refKind != RefKind.None || field.FieldSymbol.IsReadOnly) return expr;
                            goto default;
                        }

                        if (refKind == RefKind.None)
                        {
                            goto default;
                        }

                        var isFieldOfStruct = !field.FieldSymbol.ContainingType.IsReferenceType;

                        var receiver = HoistExpression(field.ReceiverOpt, awaitSyntaxOpt, syntaxOffset,
                            isFieldOfStruct ? refKind : RefKind.None, sideEffects, hoistedFields, ref needsSacrificialEvaluation);
                        if (receiver.Kind != BoundKind.ThisReference && !isFieldOfStruct)
                        {
                            needsSacrificialEvaluation = true; // need the null check in field receiver
                        }

                        return F.Field(receiver, field.FieldSymbol);
                    }

                case BoundKind.ThisReference:
                case BoundKind.BaseReference:
                case BoundKind.DefaultExpression:
                    return expr;

                case BoundKind.Call:
                    var call = (BoundCall)expr;
                    // NOTE: There are two kinds of 'In' arguments that we may see at this point:
                    //       - `RefKindExtensions.StrictIn`     (originally specified with 'In' modifier)
                    //       - `RefKind.In`                     (specified with no modifiers and matched an 'In' parameter)
                    //
                    //       It is allowed to spill ordinary `In` arguments by value if reference-preserving spilling is not possible.
                    //       The "strict" ones do not permit implicit copying, so the same situation should result in an error.
                    if (refKind != RefKind.None && refKind != RefKind.In)
                    {
                        Debug.Assert(call.Method.RefKind != RefKind.None);
                        F.Diagnostics.Add(ErrorCode.ERR_RefReturningCallAndAwait, F.Syntax.Location, call.Method);
                    }
                    // method call is not referentially transparent, we can only spill the result value.
                    refKind = RefKind.None;
                    goto default;

                case BoundKind.ConditionalOperator:
                    var conditional = (BoundConditionalOperator)expr;
                    // NOTE: There are two kinds of 'In' arguments that we may see at this point:
                    //       - `RefKindExtensions.StrictIn`     (originally specified with 'In' modifier)
                    //       - `RefKind.In`                     (specified with no modifiers and matched an 'In' parameter)
                    //
                    //       It is allowed to spill ordinary `In` arguments by value if reference-preserving spilling is not possible.
                    //       The "strict" ones do not permit implicit copying, so the same situation should result in an error.
                    if (refKind != RefKind.None && refKind != RefKind.RefReadOnly)
                    {
                        Debug.Assert(conditional.IsRef);
                        F.Diagnostics.Add(ErrorCode.ERR_RefConditionalAndAwait, F.Syntax.Location);
                    }
                    // conditional expr is not referentially transparent, we can only spill the result value.
                    refKind = RefKind.None;
                    goto default;

                default:
                    if (expr.ConstantValue != null)
                    {
                        return expr;
                    }

                    if (refKind != RefKind.None)
                    {
                        throw ExceptionUtilities.UnexpectedValue(expr.Kind);
                    }

                    TypeSymbol fieldType = expr.Type;
                    StateMachineFieldSymbol hoistedField;
                    if (F.Compilation.Options.OptimizationLevel == OptimizationLevel.Debug)
                    {
                        const SynthesizedLocalKind kind = SynthesizedLocalKind.AwaitByRefSpill;

                        Debug.Assert(awaitSyntaxOpt != null);

                        int ordinal = _synthesizedLocalOrdinals.AssignLocalOrdinal(kind, syntaxOffset);
                        var id = new LocalDebugId(syntaxOffset, ordinal);

                        // Editing await expression is not allowed. Thus all spilled fields will be present in the previous state machine.
                        // However, it may happen that the type changes, in which case we need to allocate a new slot.
                        int slotIndex;
                        if (slotAllocatorOpt == null ||
                            !slotAllocatorOpt.TryGetPreviousHoistedLocalSlotIndex(
                                awaitSyntaxOpt,
                                F.ModuleBuilderOpt.Translate(fieldType, awaitSyntaxOpt, Diagnostics),
                                kind,
                                id,
                                Diagnostics,
                                out slotIndex))
                        {
                            slotIndex = _nextFreeHoistedLocalSlot++;
                        }

                        string fieldName = GeneratedNames.MakeHoistedLocalFieldName(kind, slotIndex);
                        hoistedField = F.StateMachineField(expr.Type, fieldName, new LocalSlotDebugInfo(kind, id), slotIndex);
                    }
                    else
                    {
                        hoistedField = GetOrAllocateReusableHoistedField(fieldType, reused: out _);
                    }

                    hoistedFields.Add(hoistedField);

                    var replacement = F.Field(F.This(), hoistedField);
                    sideEffects.Add(F.AssignmentExpression(replacement, expr));
                    return replacement;
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

        public override BoundNode VisitScope(BoundScope node)
        {
            Debug.Assert(!node.Locals.IsEmpty);
            var newLocalsBuilder = ArrayBuilder<LocalSymbol>.GetInstance();
            var hoistedLocalsWithDebugScopes = ArrayBuilder<StateMachineFieldSymbol>.GetInstance();
            bool localsRewritten = false;
            foreach (var local in node.Locals)
            {
                // BoundScope is only used for switch
                Debug.Assert(local.SynthesizedKind == SynthesizedLocalKind.UserDefined &&
                    (local.ScopeDesignatorOpt?.Kind() == SyntaxKind.SwitchSection ||
                     local.ScopeDesignatorOpt?.Kind() == SyntaxKind.SwitchExpressionArm));

                LocalSymbol localToUse;
                if (TryRewriteLocal(local, out localToUse))
                {
                    newLocalsBuilder.Add(localToUse);
                    localsRewritten |= ((object)local != localToUse);
                    continue;
                }

                hoistedLocalsWithDebugScopes.Add(((CapturedToStateMachineFieldReplacement)proxies[local]).HoistedField);
            }

            var statements = VisitList(node.Statements);

            // wrap the node in an iterator scope for debugging
            if (hoistedLocalsWithDebugScopes.Count != 0)
            {
                BoundStatement translated;

                if (newLocalsBuilder.Count == 0)
                {
                    newLocalsBuilder.Free();
                    translated = new BoundStatementList(node.Syntax, statements);
                }
                else
                {
                    translated = node.Update(newLocalsBuilder.ToImmutableAndFree(), statements);
                }

                return MakeStateMachineScope(hoistedLocalsWithDebugScopes.ToImmutable(), translated);
            }
            else
            {
                hoistedLocalsWithDebugScopes.Free();
                ImmutableArray<LocalSymbol> newLocals;

                if (localsRewritten)
                {
                    newLocals = newLocalsBuilder.ToImmutableAndFree();
                }
                else
                {
                    newLocalsBuilder.Free();
                    newLocals = node.Locals;
                }

                return node.Update(newLocals, statements);
            }
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
                Debug.Assert(!node.IsRef);
                return base.VisitAssignmentOperator(node);
            }

            // TODO (move to AsyncMethodToStateMachineRewriter, this is not applicable to iterators)

            // User-declared variables are preassigned their proxies, and by-value synthesized variables
            // are assigned proxies at the beginning of their scope by the enclosing construct.
            // Here we handle ref temps. Ref synthesized variables are the target of a ref assignment operator before
            // being used in any other way.

            Debug.Assert(leftLocal.SynthesizedKind == SynthesizedLocalKind.Spill);
            Debug.Assert(node.IsRef);

            // We have an assignment to a variable that has not yet been assigned a proxy.
            // So we assign the proxy before translating the assignment.
            return HoistRefInitialization((SynthesizedLocal)leftLocal, node);
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
            var oldDispatches = _dispatches;
            var oldFinalizerState = _currentFinalizerState;
            var oldHasFinalizerState = _hasFinalizerState;

            _dispatches = null;
            _currentFinalizerState = -1;
            _hasFinalizerState = false;

            BoundBlock tryBlock = F.Block((BoundStatement)this.Visit(node.TryBlock));
            GeneratedLabelSymbol dispatchLabel = null;
            if (_dispatches != null)
            {
                dispatchLabel = F.GenerateLabel("tryDispatch");
                if (_hasFinalizerState)
                {
                    // cause the current finalizer state to arrive here and then "return false"
                    var finalizer = F.GenerateLabel("finalizer");
                    _dispatches.Add(finalizer, new List<int>() { _currentFinalizerState });
                    var skipFinalizer = F.GenerateLabel("skipFinalizer");
                    tryBlock = F.Block(
                        F.HiddenSequencePoint(),
                        Dispatch(),
                        F.Goto(skipFinalizer),
                        F.Label(finalizer), // code for the finalizer here
                        GenerateSetBothStates(StateMachineStates.NotStartedStateMachine),
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

                oldDispatches.Add(dispatchLabel, new List<int>(from kv in _dispatches.Values from n in kv orderby n select n));
            }

            _hasFinalizerState = oldHasFinalizerState;
            _currentFinalizerState = oldFinalizerState;
            _dispatches = oldDispatches;

            ImmutableArray<BoundCatchBlock> catchBlocks = this.VisitList(node.CatchBlocks);
            BoundBlock finallyBlockOpt = node.FinallyBlockOpt == null ? null : F.Block(
                F.HiddenSequencePoint(),
                F.If(
                    condition: ShouldEnterFinallyBlock(),
                    thenClause: (BoundBlock)this.Visit(node.FinallyBlockOpt)
                ),
                F.HiddenSequencePoint());

            BoundStatement result = node.Update(tryBlock, catchBlocks, finallyBlockOpt, node.FinallyLabelOpt, node.PreferFaultHandler);

            if ((object)dispatchLabel != null)
            {
                result = F.Block(
                    F.HiddenSequencePoint(),
                    F.Label(dispatchLabel),
                    result);
            }

            return result;
        }

        protected virtual BoundBinaryOperator ShouldEnterFinallyBlock()
        {
            return F.IntLessThan(F.Local(cachedState), F.Literal(StateMachineStates.FirstUnusedState));
        }

        /// <summary>
        /// Set the state field and the cached state
        /// </summary>
        protected BoundExpressionStatement GenerateSetBothStates(int stateNumber)
        {
            // this.state = cachedState = stateNumber;
            return F.Assignment(F.Field(F.This(), stateField), F.AssignmentExpression(F.Local(cachedState), F.Literal(stateNumber)));
        }

        protected BoundStatement CacheThisIfNeeded()
        {
            // restore "this" cache, if there is a cache
            if ((object)this.cachedThis != null)
            {
                CapturedSymbolReplacement proxy = proxies[this.OriginalMethod.ThisParameter];
                var fetchThis = proxy.Replacement(F.Syntax, frameType => F.This());
                return F.Assignment(F.Local(this.cachedThis), fetchThis);
            }

            // do nothing
            return F.StatementList();
        }

        public sealed override BoundNode VisitThisReference(BoundThisReference node)
        {
            // if "this" is cached, return it.
            if ((object)this.cachedThis != null)
            {
                return F.Local(this.cachedThis);
            }

            var thisParameter = this.OriginalMethod.ThisParameter;
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

            // if "this" is cached, return it.
            if ((object)this.cachedThis != null)
            {
                return F.Local(this.cachedThis);
            }

            CapturedSymbolReplacement proxy = proxies[this.OriginalMethod.ThisParameter];
            Debug.Assert(proxy != null);
            return proxy.Replacement(F.Syntax, frameType => F.This());
        }

        #endregion
    }
}
