// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class MethodToStateMachineRewriter : MethodToClassRewriter
    {
        internal readonly MethodSymbol OriginalMethod;

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
        protected readonly LocalSymbol? cachedThis;

        protected readonly FieldSymbol? instanceIdField;

        /// <summary>
        /// Allocates resumable states, i.e. states that resume execution of the state machine after await expression or yield return.
        /// </summary>
        private readonly ResumableStateMachineStateAllocator _resumableStateAllocator;

        /// <summary>
        /// For each distinct label, the set of states that need to be dispatched to that label.
        /// Note that there is a dispatch occurring at every try-finally statement, so this
        /// variable takes on a new set of values inside each try block.
        /// </summary>
        private Dictionary<LabelSymbol, List<StateMachineState>> _dispatches = new Dictionary<LabelSymbol, List<StateMachineState>>();

        /// <summary>
        /// A pool of fields used to hoist locals. They appear in this set when not in scope,
        /// so that members of this set may be allocated to locals when the locals come into scope.
        /// </summary>
        private Dictionary<TypeSymbol, ArrayBuilder<StateMachineFieldSymbol>>? _lazyAvailableReusableHoistedFields;

        /// <summary>
        /// We collect all the hoisted fields for locals, so that we can clear them so the GC can collect references.
        /// </summary>
        private readonly ArrayBuilder<FieldSymbol> _fieldsForCleanup;

        /// <summary>
        /// Fields allocated for temporary variables are given unique names distinguished by a number at the end.
        /// This counter ensures they are unique within a given translated method.
        /// </summary>
        private int _nextHoistedFieldId = 1;

        /// <summary>
        /// The set of local variables and parameters that were hoisted and need a proxy.
        /// </summary>
        private readonly IReadOnlySet<Symbol> _hoistedVariables;

        private readonly SynthesizedLocalOrdinalsDispenser _synthesizedLocalOrdinals;
        private int _nextFreeHoistedLocalSlot;

        /// <summary>
        /// EnC support: the rewriter stores debug info for each await/yield in this builder.
        /// </summary>
        private readonly ArrayBuilder<StateMachineStateDebugInfo> _stateDebugInfoBuilder;

        // Instrumentation related bound nodes:
        protected BoundBlockInstrumentation? instrumentation;

        private readonly RefInitializationHoister<StateMachineFieldSymbol, BoundFieldAccess> _refInitializationHoister;

        // new:
        public MethodToStateMachineRewriter(
            SyntheticBoundNodeFactory F,
            MethodSymbol originalMethod,
            FieldSymbol state,
            FieldSymbol? instanceIdField,
            IReadOnlySet<Symbol> hoistedVariables,
            IReadOnlyDictionary<Symbol, CapturedSymbolReplacement> nonReusableLocalProxies,
            ImmutableArray<FieldSymbol> nonReusableFieldsForCleanup,
            SynthesizedLocalOrdinalsDispenser synthesizedLocalOrdinals,
            ArrayBuilder<StateMachineStateDebugInfo> stateMachineStateDebugInfoBuilder,
            VariableSlotAllocator? slotAllocatorOpt,
            int nextFreeHoistedLocalSlot,
            BindingDiagnosticBag diagnostics)
            : base(slotAllocatorOpt, F.CompilationState, diagnostics)
        {
            Debug.Assert(F != null);
            Debug.Assert(originalMethod != null);
            Debug.Assert(state != null);
            Debug.Assert(nonReusableLocalProxies != null);
            Debug.Assert(!nonReusableFieldsForCleanup.IsDefault);
            Debug.Assert(diagnostics != null);
            Debug.Assert(hoistedVariables != null);
            Debug.Assert(nextFreeHoistedLocalSlot >= 0);

            this.F = F;
            this.stateField = state;
            this.instanceIdField = instanceIdField;
            this.cachedState = F.SynthesizedLocal(F.SpecialType(SpecialType.System_Int32), syntax: F.Syntax, kind: SynthesizedLocalKind.StateMachineCachedState);
            this.OriginalMethod = originalMethod;
            _hoistedVariables = hoistedVariables;
            _synthesizedLocalOrdinals = synthesizedLocalOrdinals;
            _nextFreeHoistedLocalSlot = nextFreeHoistedLocalSlot;

            foreach (var proxy in nonReusableLocalProxies)
            {
                this.proxies.Add(proxy.Key, proxy.Value);
            }

            _fieldsForCleanup = new ArrayBuilder<FieldSymbol>(nonReusableFieldsForCleanup.Length);
            _fieldsForCleanup.AddRange(nonReusableFieldsForCleanup);

            // create cache local for reference type "this" in Release
            var thisParameter = originalMethod.ThisParameter;
            CapturedSymbolReplacement? thisProxy;
            if (thisParameter is not null &&
                thisParameter.Type.IsReferenceType &&
                proxies.TryGetValue(thisParameter, out thisProxy) &&
                F.Compilation.Options.OptimizationLevel == OptimizationLevel.Release)
            {
                BoundExpression thisProxyReplacement = thisProxy.Replacement(F.Syntax, static (frameType, F) => F.This(), F);
                Debug.Assert(thisProxyReplacement.Type is not null);
                this.cachedThis = F.SynthesizedLocal(thisProxyReplacement.Type, syntax: F.Syntax, kind: SynthesizedLocalKind.FrameCache);
            }

            _stateDebugInfoBuilder = stateMachineStateDebugInfoBuilder;

            // Use the first state number that is not used by any previous version of the state machine
            // for the first added state that doesn't match any states of the previous state machine.
            // Note the initial states of the previous and the current state machine are always the same.
            // Note the previous state machine might not have any non-initial states.
            _resumableStateAllocator = new ResumableStateMachineStateAllocator(
                slotAllocatorOpt,
                firstState: FirstIncreasingResumableState,
                increasing: true);

            _refInitializationHoister = new RefInitializationHoister<StateMachineFieldSymbol, BoundFieldAccess>(F, OriginalMethod, TypeMap);
        }
#nullable disable

        protected abstract StateMachineState FirstIncreasingResumableState { get; }
        protected abstract HotReloadExceptionCode EncMissingStateErrorCode { get; }

        /// <summary>
        /// Generate return statements from the state machine method body.
        /// </summary>
        protected abstract BoundStatement GenerateReturn(bool finished);

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
#nullable enable
        protected void AddResumableState(SyntaxNode awaitOrYieldReturnSyntax, AwaitDebugId awaitId, out StateMachineState state, out GeneratedLabelSymbol resumeLabel)
            => AddResumableState(_resumableStateAllocator, awaitOrYieldReturnSyntax, awaitId, out state, out resumeLabel);

        protected void AddResumableState(ResumableStateMachineStateAllocator allocator, SyntaxNode awaitOrYieldReturnSyntax, AwaitDebugId awaitId, out StateMachineState stateNumber, out GeneratedLabelSymbol resumeLabel)
        {
            stateNumber = allocator.AllocateState(awaitOrYieldReturnSyntax, awaitId);
            AddStateDebugInfo(awaitOrYieldReturnSyntax, awaitId, stateNumber);
            AddState(stateNumber, out resumeLabel);
        }

        protected void AddStateDebugInfo(SyntaxNode node, AwaitDebugId awaitId, StateMachineState state)
        {
            RoslynDebug.Assert(SyntaxBindingUtilities.BindsToResumableStateMachineState(node) || SyntaxBindingUtilities.BindsToTryStatement(node), $"Unexpected syntax: {node.Kind()}");

            int syntaxOffset = CurrentMethod.CalculateLocalSyntaxOffset(node.SpanStart, node.SyntaxTree);
            _stateDebugInfoBuilder.Add(new StateMachineStateDebugInfo(syntaxOffset, awaitId, state));
        }

        protected void AddState(StateMachineState stateNumber, out GeneratedLabelSymbol resumeLabel)
        {
            _dispatches ??= new Dictionary<LabelSymbol, List<StateMachineState>>();

            resumeLabel = F.GenerateLabel("stateMachine");
            _dispatches.Add(resumeLabel, new List<StateMachineState> { stateNumber });
        }

        /// <summary>
        /// Generates code that switches over states and jumps to the target labels listed in <see cref="_dispatches"/>.
        /// </summary>
        /// <param name="isOutermost">
        /// If this is the outermost state dispatch switching over all states of the state machine - i.e. not state dispatch generated for a try-block.
        /// </param>
        protected BoundStatement Dispatch(bool isOutermost)
        {
            var sections = from kv in _dispatches
                           orderby kv.Value[0]
                           select F.SwitchSection(kv.Value.SelectAsArray(state => (int)state), F.Goto(kv.Key));

            var result = F.Switch(F.Local(cachedState), sections.ToImmutableArray());

            // Suspension states that were generated for any previous generation of the state machine
            // but are not present in the current version (awaits/yields have been deleted) need to be dispatched to a throw expression.
            // When an instance of previous version of the state machine is suspended in a state that does not exist anymore
            // in the current version dispatch that state to a throw expression. We do not know for sure where to resume in the new version of the method.
            // Guessing would likely result in unexpected behavior. Resuming in an incorrect point might result in an execution of code that
            // has already been executed or skipping code that initializes some user state.
            if (isOutermost)
            {
                var missingStateDispatch = GenerateMissingStateDispatch();
                if (missingStateDispatch != null)
                {
                    result = F.Block(result, missingStateDispatch);
                }
            }

            return result;
        }

        protected virtual BoundStatement? GenerateMissingStateDispatch()
            => _resumableStateAllocator.GenerateThrowMissingStateDispatch(F, F.Local(cachedState), EncMissingStateErrorCode);

#nullable disable
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
                    Debug.Assert(local.SynthesizedKind == SynthesizedLocalKind.Spill ||
                                 (local.SynthesizedKind == SynthesizedLocalKind.ForEachArray && local.Type.HasInlineArrayAttribute(out _) && local.Type.TryGetInlineArrayElementField() is object));
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
            var variableCleanup = ArrayBuilder<BoundExpression>.GetInstance();

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
                    foreach (var field in ((CapturedToExpressionSymbolReplacement<StateMachineFieldSymbol>)proxy).HoistedSymbols)
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

        /// <summary>
        /// Clear fields allocated to temporaries when the underlying variable goes out of scope, so
        /// that they do not cause unnecessary object retention.
        /// </summary>
        private void AddVariableCleanup(ArrayBuilder<BoundExpression> cleanup, FieldSymbol field)
        {
            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(F.Diagnostics, F.Compilation.Assembly);
            bool isManaged = field.Type.IsManagedType(ref useSiteInfo);
            F.Diagnostics.Add(field.GetFirstLocationOrNone(), useSiteInfo);
            if (isManaged)
            {
                cleanup.Add(F.AssignmentExpression(F.Field(F.This(), field), F.NullOrDefault(field.Type)));
            }
        }

#nullable enable
        protected BoundBlock GenerateAllHoistedLocalsCleanup()
        {
            var variableCleanup = ArrayBuilder<BoundExpression>.GetInstance();

            foreach (FieldSymbol fieldSymbol in _fieldsForCleanup)
            {
                AddVariableCleanup(variableCleanup, fieldSymbol);
            }

            var result = F.Block(variableCleanup.SelectAsArray((e, f) => (BoundStatement)f.ExpressionStatement(e), F));

            variableCleanup.Free();

            return result;
        }

        private StateMachineFieldSymbol GetOrAllocateReusableHoistedField(TypeSymbol type, out bool reused, LocalSymbol? local = null)
        {
            ArrayBuilder<StateMachineFieldSymbol>? fields;
            if (_lazyAvailableReusableHoistedFields != null && _lazyAvailableReusableHoistedFields.TryGetValue(type, out fields) && fields.Count > 0)
            {
                var field = fields.Last();
                fields.RemoveLast();
                reused = true;
                return field;
            }

            reused = false;
            var slotIndex = _nextHoistedFieldId++;

            StateMachineFieldSymbol createdField;
            if (local?.SynthesizedKind == SynthesizedLocalKind.UserDefined)
            {
                string fieldName = GeneratedNames.MakeHoistedLocalFieldName(SynthesizedLocalKind.UserDefined, slotIndex, local.Name);
                createdField = F.StateMachineField(type, fieldName, SynthesizedLocalKind.UserDefined, slotIndex);
            }
            else
            {
                createdField = F.StateMachineField(type, GeneratedNames.ReusableHoistedLocalFieldName(slotIndex));
            }

            _fieldsForCleanup.Add(createdField);
            return createdField;
        }
#nullable disable

        private void FreeReusableHoistedField(StateMachineFieldSymbol field)
        {
            ArrayBuilder<StateMachineFieldSymbol> fields;
            if (_lazyAvailableReusableHoistedFields == null || !_lazyAvailableReusableHoistedFields.TryGetValue(field.Type, out fields))
            {
                if (_lazyAvailableReusableHoistedFields == null)
                {
                    _lazyAvailableReusableHoistedFields = new Dictionary<TypeSymbol, ArrayBuilder<StateMachineFieldSymbol>>(Symbols.SymbolEqualityComparer.IgnoringDynamicTupleNamesAndNullability);
                }

                _lazyAvailableReusableHoistedFields.Add(field.Type, fields = new ArrayBuilder<StateMachineFieldSymbol>());
            }

            fields.Add(field);
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
            if (node.Instrumentation != null)
            {
                // Stash away the instrumentation node, it will be used when generating MoveNext method.
                instrumentation = (BoundBlockInstrumentation)Visit(node.Instrumentation);
            }

            return PossibleIteratorScope(node.Locals, () => VisitBlock(node, removeInstrumentation: true));
        }

        public override BoundNode VisitStateMachineInstanceId(BoundStateMachineInstanceId node)
            => F.Field(F.This(), instanceIdField);

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
            throw ExceptionUtilities.Unreachable(); // for statements have been lowered away by now
        }

        public override BoundNode VisitUsingStatement(BoundUsingStatement node)
        {
            throw ExceptionUtilities.Unreachable(); // using statements have been lowered away by now
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

            Debug.Assert(leftLocal.SynthesizedKind == SynthesizedLocalKind.Spill ||
                         (leftLocal.SynthesizedKind == SynthesizedLocalKind.ForEachArray && leftLocal.Type.HasInlineArrayAttribute(out _) && leftLocal.Type.TryGetInlineArrayElementField() is object));
            Debug.Assert(node.IsRef);

            // We have an assignment to a variable that has not yet been assigned a proxy.
            // So we assign the proxy before translating the assignment.
            var visitedRight = (BoundExpression)Visit(node.Right);
            return _refInitializationHoister.HoistRefInitialization(
                leftLocal,
                visitedRight,
                proxies,
                createHoistedSymbol,
                createHoistedAccess,
                this,
                isRuntimeAsync: false);

            static StateMachineFieldSymbol createHoistedSymbol(TypeSymbol type, MethodToStateMachineRewriter @this, LocalSymbol assignedLocal)
            {
                StateMachineFieldSymbol hoistedSymbol;

                // https://github.com/dotnet/roslyn/issues/79793 - consider whether runtime async will need some of this work for enc
                if (@this.F.Compilation.Options.OptimizationLevel == OptimizationLevel.Debug)
                {
                    const SynthesizedLocalKind kind = SynthesizedLocalKind.AwaitByRefSpill;
                    SyntaxNode awaitSyntax = assignedLocal.GetDeclaratorSyntax();
#pragma warning disable format
                    Debug.Assert(assignedLocal.SynthesizedKind switch
                                 {
                                     SynthesizedLocalKind.Spill => awaitSyntax.IsKind(SyntaxKind.AwaitExpression) || awaitSyntax.IsKind(SyntaxKind.SwitchExpression),
                                     SynthesizedLocalKind.ForEachArray => awaitSyntax is CommonForEachStatementSyntax,
                                     _ => false
                                 });
#pragma warning restore format
                    int syntaxOffset = @this.OriginalMethod.CalculateLocalSyntaxOffset(LambdaUtilities.GetDeclaratorPosition(awaitSyntax), awaitSyntax.SyntaxTree);

                    Debug.Assert(awaitSyntax != null);

                    int ordinal = @this._synthesizedLocalOrdinals.AssignLocalOrdinal(kind, syntaxOffset);
                    var id = new LocalDebugId(syntaxOffset, ordinal);

                    // Editing await expression is not allowed. Thus all spilled fields will be present in the previous state machine.
                    // However, it may happen that the type changes, in which case we need to allocate a new slot.
                    int slotIndex;
                    if (@this.slotAllocator == null ||
                        !@this.slotAllocator.TryGetPreviousHoistedLocalSlotIndex(
                            awaitSyntax,
                            @this.F.ModuleBuilderOpt.Translate(type, awaitSyntax, @this.Diagnostics.DiagnosticBag),
                            kind,
                            id,
                            @this.Diagnostics.DiagnosticBag,
                            out slotIndex))
                    {
                        slotIndex = @this._nextFreeHoistedLocalSlot++;
                    }

                    string fieldName = GeneratedNames.MakeHoistedLocalFieldName(kind, slotIndex);
                    hoistedSymbol = @this.F.StateMachineField(type, fieldName, new LocalSlotDebugInfo(kind, id), slotIndex);
                    @this._fieldsForCleanup.Add(hoistedSymbol);
                }
                else
                {
                    hoistedSymbol = @this.GetOrAllocateReusableHoistedField(type, out _);
                }

                return hoistedSymbol;
            }

            static BoundFieldAccess createHoistedAccess(StateMachineFieldSymbol fieldSymbol, MethodToStateMachineRewriter @this)
                => @this.F.Field(@this.F.This(), fieldSymbol);
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
            Debug.Assert(this is AsyncIteratorMethodToStateMachineRewriter or AsyncMethodToStateMachineRewriter);

            var oldDispatches = _dispatches;

            _dispatches = null;

            BoundBlock tryBlock = F.Block((BoundStatement)this.Visit(node.TryBlock));
            GeneratedLabelSymbol dispatchLabel = null;
            if (_dispatches != null)
            {
                dispatchLabel = F.GenerateLabel("tryDispatch");

                tryBlock = F.Block(
                    F.HiddenSequencePoint(),
                    Dispatch(isOutermost: false),
                    tryBlock);

                oldDispatches ??= new Dictionary<LabelSymbol, List<StateMachineState>>();
                oldDispatches.Add(dispatchLabel, new List<StateMachineState>(from kv in _dispatches.Values from n in kv orderby n select n));
            }

            _dispatches = oldDispatches;

            ImmutableArray<BoundCatchBlock> catchBlocks = this.VisitList(node.CatchBlocks);

            BoundBlock finallyBlockOpt = node.FinallyBlockOpt == null ? null : F.Block(
                F.HiddenSequencePoint(),
                F.If(
                    condition: ShouldEnterFinallyBlock(),
                    thenClause: VisitFinally(node.FinallyBlockOpt)
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

        protected virtual BoundBlock VisitFinally(BoundBlock finallyBlock)
        {
            return (BoundBlock)this.Visit(finallyBlock);
        }

        protected virtual BoundBinaryOperator ShouldEnterFinallyBlock()
        {
            Debug.Assert(this is AsyncMethodToStateMachineRewriter);
            return F.IntLessThan(F.Local(cachedState), F.Literal(StateMachineState.FirstUnusedState));
        }

        /// <summary>
        /// Set the state field and the cached state
        /// </summary>
        protected BoundExpressionStatement GenerateSetBothStates(StateMachineState stateNumber)
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
                var fetchThis = proxy.Replacement(F.Syntax, static (frameType, F) => F.This(), F);
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
                return proxy.Replacement(F.Syntax, static (frameType, F) => F.This(), F);
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
            return proxy.Replacement(F.Syntax, static (frameType, F) => F.This(), F);
        }

        #endregion
    }
}
