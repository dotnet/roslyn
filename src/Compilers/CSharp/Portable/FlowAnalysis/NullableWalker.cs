// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if DEBUG
// See comment in DefiniteAssignment.
#define REFERENCE_STATE
#endif

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Nullability flow analysis.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal sealed partial class NullableWalker : LocalDataFlowPass<NullableWalker.LocalState>
    {
        /// <summary>
        /// Used to copy variable slots and types from the NullableWalker for the containing method
        /// or lambda to the NullableWalker created for a nested lambda or local function.
        /// </summary>
        internal sealed class VariableState
        {
            // Consider referencing the collections directly from the original NullableWalker
            // rather than copying the collections. (Items are added to the collections
            // but never replaced so the collections are lazily populated but otherwise immutable.)
            internal readonly ImmutableDictionary<VariableIdentifier, int> VariableSlot;
            internal readonly ImmutableArray<VariableIdentifier> VariableBySlot;
            internal readonly ImmutableDictionary<Symbol, TypeWithAnnotations> VariableTypes;

            // The nullable state of all variables captured at the point where the function or lambda appeared.
            internal readonly LocalState VariableNullableStates;

            internal VariableState(
                ImmutableDictionary<VariableIdentifier, int> variableSlot,
                ImmutableArray<VariableIdentifier> variableBySlot,
                ImmutableDictionary<Symbol, TypeWithAnnotations> variableTypes,
                LocalState variableNullableStates)
            {
                VariableSlot = variableSlot;
                VariableBySlot = variableBySlot;
                VariableTypes = variableTypes;
                VariableNullableStates = variableNullableStates;
            }

            internal VariableState Clone()
            {
                return new VariableState(VariableSlot, VariableBySlot, VariableTypes, VariableNullableStates.Clone());
            }
        }

        /// <summary>
        /// Represents the result of visiting an expression.
        /// Contains a result type which tells us whether the expression may be null,
        /// and an l-value type which tells us whether we can assign null to the expression.
        /// </summary>
        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        private readonly struct VisitResult
        {
            public readonly TypeWithState RValueType;
            public readonly TypeWithAnnotations LValueType;

            public VisitResult(TypeWithState rValueType, TypeWithAnnotations lValueType)
            {
                RValueType = rValueType;
                LValueType = lValueType;
                // https://github.com/dotnet/roslyn/issues/34993: Doesn't hold true for Tuple_Assignment_10. See if we can make it hold true
                //Debug.Assert((RValueType.Type is null && LValueType.TypeSymbol is null) ||
                //             RValueType.Type.Equals(LValueType.TypeSymbol, TypeCompareKind.ConsiderEverything | TypeCompareKind.AllIgnoreOptions));
            }

            public VisitResult(TypeWithState valueType)
                : this(valueType, valueType.ToTypeWithAnnotations())
            {
            }

            public VisitResult(TypeSymbol type, NullableAnnotation annotation, NullableFlowState state)
            {
                RValueType = TypeWithState.Create(type, state);
                LValueType = TypeWithAnnotations.Create(type, annotation);
                Debug.Assert(RValueType.Type.Equals(LValueType.Type, TypeCompareKind.ConsiderEverything));
            }

            internal string GetDebuggerDisplay() => $"{{LValue: {LValueType.GetDebuggerDisplay()}, RValue: {RValueType.GetDebuggerDisplay()}}}";
        }

        /// <summary>
        /// Represents the result of visiting an argument expression.
        /// In addition to storing the <see cref="VisitResult"/>, also stores the <see cref="LocalState"/>
        /// for reanalyzing a lambda.
        /// </summary>
        [DebuggerDisplay("{VisitResult.GetDebuggerDisplay(), nq}")]
        private readonly struct VisitArgumentResult
        {
            public readonly VisitResult VisitResult;
            public readonly Optional<LocalState> StateForLambda;
            public TypeWithState RValueType => VisitResult.RValueType;
            public TypeWithAnnotations LValueType => VisitResult.LValueType;

            public VisitArgumentResult(VisitResult visitResult, Optional<LocalState> stateForLambda)
            {
                VisitResult = visitResult;
                StateForLambda = stateForLambda;
            }
        }

        /// <summary>
        /// The inferred type at the point of declaration of var locals and parameters.
        /// </summary>
        private readonly PooledDictionary<Symbol, TypeWithAnnotations> _variableTypes = PooledDictionary<Symbol, TypeWithAnnotations>.GetInstance();

        /// <summary>
        /// Binder for symbol being analyzed.
        /// </summary>
        private readonly Binder _binder;

        /// <summary>
        /// Conversions with nullability and unknown matching any.
        /// </summary>
        private readonly Conversions _conversions;

        /// <summary>
        /// Use the the parameter types and nullability from _methodSignatureOpt for initial
        /// parameter state. If false, the signature of _member is used instead.
        /// </summary>
        private readonly bool _useMethodSignatureParameterTypes;

        /// <summary>
        /// Method signature used for return type or parameter types. Distinct from _member
        /// signature when _member is a lambda and type is inferred from MethodTypeInferrer.
        /// </summary>
        private readonly MethodSymbol _methodSignatureOpt;

        /// <summary>
        /// Return statements and the result types from analyzing the returned expressions. Used when inferring lambda return type in MethodTypeInferrer.
        /// </summary>
        private readonly ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)> _returnTypesOpt;

        /// <summary>
        /// Invalid type, used only to catch Visit methods that do not set
        /// _result.Type. See VisitExpressionWithoutStackGuard.
        /// </summary>
        private static readonly TypeWithState _invalidType = TypeWithState.Create(ErrorTypeSymbol.UnknownResultType, NullableFlowState.NotNull);

        /// <summary>
        /// Contains the map of expressions to inferred nullabilities and types used by the optional rewriter phase of the
        /// compiler.
        /// </summary>
        private readonly ImmutableDictionary<BoundExpression, (NullabilityInfo Info, TypeSymbol Type)>.Builder _analyzedNullabilityMapOpt;

        /// <summary>
        /// Contains the map of expression and original symbol to reinferred symbols, used by the optional
        /// rewriter phase of the compiler.
        /// </summary>
        private readonly ImmutableDictionary<(BoundNode, Symbol), Symbol>.Builder _updatedSymbolMapOpt;

        /// <summary>
        /// Manages creating snapshots of the walker as appropriate. Null if we're not taking snapshots of
        /// this walker.
        /// </summary>
        private readonly SnapshotManager.Builder _snapshotBuilderOpt;

        // https://github.com/dotnet/roslyn/issues/35043: remove this when all expression are supported
        private bool _disableNullabilityAnalysis;

        /// <summary>
        /// State of method group receivers, used later when analyzing the conversion to a delegate.
        /// (Could be replaced by _analyzedNullabilityMapOpt if that map is always available.)
        /// </summary>
        private PooledDictionary<BoundExpression, TypeWithState> _methodGroupReceiverMapOpt;

        /// <summary>
        /// True if we're analyzing speculative code. This turns off some initialization steps
        /// that would otherwise be taken.
        /// </summary>
        private readonly bool _isSpeculative;

#if DEBUG
        /// <summary>
        /// Contains the expressions that should not be inserted into <see cref="_analyzedNullabilityMapOpt"/>.
        /// </summary>
        private static readonly ImmutableArray<BoundKind> s_skippedExpressions = ImmutableArray.Create(BoundKind.ArrayInitialization,
            BoundKind.ObjectInitializerExpression,
            BoundKind.CollectionInitializerExpression,
            BoundKind.DynamicCollectionElementInitializer);
#endif

        /// <summary>
        /// The result and l-value type of the last visited expression.
        /// </summary>
        private VisitResult _visitResult;

        /// <summary>
        /// The visit result of the receiver for the current conditional access.
        ///
        /// For example: A conditional invocation uses a placeholder as a receiver. By storing the
        /// visit result from the actual receiver ahead of time, we can give this placeholder a correct result.
        /// </summary>
        private VisitResult _currentConditionalReceiverVisitResult;

        /// <summary>
        /// The result type represents the state of the last visited expression.
        /// </summary>
        private TypeWithState ResultType
        {
            get => _visitResult.RValueType;
        }

        private void SetResultType(BoundExpression expression, TypeWithState type, bool updateAnalyzedNullability = true)
        {
            SetResult(expression, resultType: type, lvalueType: type.ToTypeWithAnnotations(), updateAnalyzedNullability: updateAnalyzedNullability);
        }

        /// <summary>
        /// Force the inference of the LValueResultType from ResultType.
        /// </summary>
        private void UseRvalueOnly(BoundExpression expression)
        {
            SetResult(expression, ResultType, ResultType.ToTypeWithAnnotations(), isLvalue: false);
        }

        private TypeWithAnnotations LvalueResultType
        {
            get => _visitResult.LValueType;
        }

        private void SetLvalueResultType(BoundExpression expression, TypeWithAnnotations type)
        {
            SetResult(expression, resultType: type.ToTypeWithState(), lvalueType: type);
        }

        /// <summary>
        /// Force the inference of the ResultType from LValueResultType.
        /// </summary>
        private void UseLvalueOnly(BoundExpression expression)
        {
            SetResult(expression, LvalueResultType.ToTypeWithState(), LvalueResultType, isLvalue: true);
        }

        private void SetInvalidResult() => SetResult(expression: null, _invalidType, _invalidType.ToTypeWithAnnotations(), updateAnalyzedNullability: false);

        private void SetResult(BoundExpression expression, TypeWithState resultType, TypeWithAnnotations lvalueType, bool updateAnalyzedNullability = true, bool? isLvalue = null)
        {
            _visitResult = new VisitResult(resultType, lvalueType);
            if (updateAnalyzedNullability)
            {
                SetAnalyzedNullability(expression, _visitResult, isLvalue);
            }
        }

        private bool ShouldMakeNotNullRvalue(BoundExpression node) => node.IsSuppressed || node.HasAnyErrors || !IsReachable();

        /// <summary>
        /// Sets the analyzed nullability of the expression to be the given result.
        /// </summary>
        private void SetAnalyzedNullability(BoundExpression expr, VisitResult result, bool? isLvalue = null)
        {
            if (expr == null || _disableNullabilityAnalysis) return;

#if DEBUG
            // https://github.com/dotnet/roslyn/issues/34993: This assert is essential for ensuring that we aren't
            // changing the observable results of GetTypeInfo beyond nullability information.
            //Debug.Assert(AreCloseEnough(expr.Type, result.RValueType.Type),
            //             $"Cannot change the type of {expr} from {expr.Type} to {result.RValueType.Type}");
#endif

            if (_analyzedNullabilityMapOpt != null)
            {
                // https://github.com/dotnet/roslyn/issues/34993: enable and verify these assertions
#if false
                if (_analyzedNullabilityMapOpt.TryGetValue(expr, out var existing))
                {
                    if (!(result.RValueType.State == NullableFlowState.NotNull && ShouldMakeNotNullRvalue(expr, State.Reachable)))
                    {
                        switch (isLvalue)
                        {
                            case true:
                                Debug.Assert(existing.Info.Annotation == result.LValueType.NullableAnnotation.ToPublicAnnotation(),
                                    $"Tried to update the nullability of {expr} from {existing.Info.Annotation} to {result.LValueType.NullableAnnotation}");
                                break;

                            case false:
                                Debug.Assert(existing.Info.FlowState == result.RValueType.State,
                                    $"Tried to update the nullability of {expr} from {existing.Info.FlowState} to {result.RValueType.State}");
                                break;

                            case null:
                                Debug.Assert(existing.Info.Equals((NullabilityInfo)result),
                                    $"Tried to update the nullability of {expr} from ({existing.Info.Annotation}, {existing.Info.FlowState}) to ({result.LValueType.NullableAnnotation}, {result.RValueType.State})");
                                break;
                        }
                    }
                }
#endif
                _analyzedNullabilityMapOpt[expr] = (new NullabilityInfo(result.LValueType.ToPublicAnnotation(), result.RValueType.State.ToPublicFlowState()),
                                                    // https://github.com/dotnet/roslyn/issues/35046 We're dropping the result if the type doesn't match up completely
                                                    // with the existing type
                                                    expr.Type?.Equals(result.RValueType.Type, TypeCompareKind.AllIgnoreOptions) == true ? result.RValueType.Type : expr.Type);
            }
        }

        /// <summary>
        /// Placeholder locals, e.g. for objects being constructed.
        /// </summary>
        private PooledDictionary<object, PlaceholderLocal> _placeholderLocalsOpt;

        /// <summary>
        /// For methods with annotations, we'll need to visit the arguments twice.
        /// Once for diagnostics and once for result state (but disabling diagnostics).
        /// </summary>
        private bool _disableDiagnostics = false;

        /// <summary>
        /// Used to allow <see cref="MakeSlot(BoundExpression)"/> to substitute the correct slot for a <see cref="BoundConditionalReceiver"/> when
        /// it's encountered.
        /// </summary>
        private int _lastConditionalAccessSlot = -1;

        private bool IsAnalyzingAttribute => methodMainNode.Kind == BoundKind.Attribute;

        protected override void Free()
        {
            _methodGroupReceiverMapOpt?.Free();
            _variableTypes.Free();
            _placeholderLocalsOpt?.Free();
            base.Free();
        }

        private NullableWalker(
            CSharpCompilation compilation,
            Symbol symbol,
            bool useMethodSignatureParameterTypes,
            MethodSymbol methodSignatureOpt,
            BoundNode node,
            Binder binder,
            Conversions conversions,
            VariableState initialState,
            ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)> returnTypesOpt,
            ImmutableDictionary<BoundExpression, (NullabilityInfo, TypeSymbol)>.Builder analyzedNullabilityMapOpt,
            ImmutableDictionary<(BoundNode, Symbol), Symbol>.Builder updatedSymbolMapOpt,
            SnapshotManager.Builder snapshotBuilderOpt,
            bool isSpeculative = false)
            // Members of variables are tracked up to a fixed depth, to avoid cycles. The
            // maxSlotDepth value is arbitrary but large enough to allow most scenarios.
            : base(compilation, symbol, node, EmptyStructTypeCache.CreatePrecise(), trackUnassignments: true, maxSlotDepth: 5)
        {
            _binder = binder;
            _conversions = (Conversions)conversions.WithNullability(true);
            _useMethodSignatureParameterTypes = (object)methodSignatureOpt != null && useMethodSignatureParameterTypes;
            _methodSignatureOpt = methodSignatureOpt;
            _analyzedNullabilityMapOpt = analyzedNullabilityMapOpt;
            _updatedSymbolMapOpt = updatedSymbolMapOpt;
            _returnTypesOpt = returnTypesOpt;
            _snapshotBuilderOpt = snapshotBuilderOpt;
            _isSpeculative = isSpeculative;

            if (initialState != null)
            {
                var variableBySlot = initialState.VariableBySlot;
                nextVariableSlot = variableBySlot.Length;
                foreach (var (variable, slot) in initialState.VariableSlot)
                {
                    Debug.Assert(slot < nextVariableSlot);
                    _variableSlot.Add(variable, slot);
                }
                this.variableBySlot = variableBySlot.ToArray();
                foreach (var (key, value) in initialState.VariableTypes)
                {
                    _variableTypes.Add(key, value);
                }
                this.State = initialState.VariableNullableStates.Clone();
            }
        }

        public string GetDebuggerDisplay()
        {
            if (this.IsConditionalState)
            {
                return $"{{{GetType().Name} WhenTrue:{Dump(StateWhenTrue)} WhenFalse:{Dump(StateWhenFalse)}{"}"}";
            }
            else
            {
                return $"{{{GetType().Name} {Dump(State)}{"}"}";
            }
        }

        // For purpose of nullability analysis, awaits create pending branches, so async usings and foreachs do too
        public sealed override bool AwaitUsingAndForeachAddsPendingBranch => true;

        protected override bool ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()
        {
            return true;
        }

        protected override ImmutableArray<PendingBranch> Scan(ref bool badRegion)
        {
            if (_returnTypesOpt != null)
            {
                _returnTypesOpt.Clear();
            }
            this.Diagnostics.Clear();
            this.regionPlace = RegionPlace.Before;
            if (!_isSpeculative)
            {
                ParameterSymbol methodThisParameter = MethodThisParameter;
                EnterParameters(); // assign parameters
                if (methodThisParameter is object)
                {
                    EnterParameter(methodThisParameter, methodThisParameter.TypeWithAnnotations);
                }

                // We need to create a snapshot even of the first node, because we want to have the state of the initial parameters.
                _snapshotBuilderOpt?.TakeIncrementalSnapshot(methodMainNode, State);
            }

            ImmutableArray<PendingBranch> pendingReturns = base.Scan(ref badRegion);
            return pendingReturns;
        }

        internal static void Analyze(
            CSharpCompilation compilation,
            MethodSymbol method,
            BoundNode node,
            DiagnosticBag diagnostics)
        {
            if (method.IsImplicitlyDeclared && !method.IsImplicitConstructor && !method.IsScriptInitializer)
            {
                return;
            }
            var binder = compilation.GetBinderFactory(node.SyntaxTree).GetBinder(node.Syntax);
            var conversions = binder.Conversions;
            Analyze(compilation,
                method,
                node,
                binder,
                conversions,
                diagnostics,
                useMethodSignatureParameterTypes: false,
                methodSignatureOpt: method,
                initialState: null,
                analyzedNullabilityMapOpt: null,
                updatedSymbolMapOpt: null,
                snapshotBuilderOpt: null,
                returnTypesOpt: null);
        }

        internal static BoundNode AnalyzeAndRewrite(
            CSharpCompilation compilation,
            Symbol symbol,
            BoundNode node,
            Binder binder,
            DiagnosticBag diagnostics,
            bool createSnapshots,
            out SnapshotManager snapshotManager)
        {
            var analyzedNullabilities = ImmutableDictionary.CreateBuilder<BoundExpression, (NullabilityInfo, TypeSymbol)>(EqualityComparer<BoundExpression>.Default, NullabilityInfoTypeComparer.Instance);
            var updatedSymbols = ImmutableDictionary.CreateBuilder<(BoundNode, Symbol), Symbol>(ExpressionAndSymbolEqualityComparer.Instance, SymbolEqualityComparer.ConsiderEverything);
            var methodSymbol = symbol as MethodSymbol;
            // Attributes don't have a symbol, which is what SnapshotBuilder uses as an index for maintaining global state.
            // Until we have a workaround for this, disable snapshots for null symbols.
            // https://github.com/dotnet/roslyn/issues/36066
            var snapshotBuilder = createSnapshots && symbol != null ? new SnapshotManager.Builder() : null;
            Analyze(
                compilation,
                symbol,
                node,
                binder,
                binder.Conversions,
                diagnostics,
                useMethodSignatureParameterTypes: !(methodSymbol is null),
                methodSignatureOpt: methodSymbol,
                initialState: null,
                analyzedNullabilities,
                updatedSymbols,
                snapshotBuilder,
                returnTypesOpt: null);

            var analyzedNullabilitiesMap = analyzedNullabilities.ToImmutable();
            var updatedSymbolsMap = updatedSymbols.ToImmutable();
            snapshotManager = snapshotBuilder?.ToManagerAndFree();

#if DEBUG
            // https://github.com/dotnet/roslyn/issues/34993 Enable for all calls
            if (compilation.NullableSemanticAnalysisEnabled)
            {
                DebugVerifier.Verify(analyzedNullabilitiesMap, updatedSymbolsMap, snapshotManager, node);
            }
#endif
            return new NullabilityRewriter(analyzedNullabilitiesMap, updatedSymbolsMap).Visit(node);
        }

        internal static BoundNode AnalyzeAndRewriteSpeculation(
            int position,
            BoundNode node,
            Binder binder,
            SnapshotManager originalSnapshots,
            bool takeNewSnapshots,
            out SnapshotManager newSnapshots)
        {
            var analyzedNullabilities = ImmutableDictionary.CreateBuilder<BoundExpression, (NullabilityInfo, TypeSymbol)>(EqualityComparer<BoundExpression>.Default, NullabilityInfoTypeComparer.Instance);
            var updatedSymbols = ImmutableDictionary.CreateBuilder<(BoundNode, Symbol), Symbol>(ExpressionAndSymbolEqualityComparer.Instance, SymbolEqualityComparer.ConsiderEverything);
            var newSnapshotBuilder = takeNewSnapshots ? new SnapshotManager.Builder() : null;
            var (walker, initialState, symbol) = originalSnapshots.RestoreWalkerToAnalyzeNewNode(position, node, binder, analyzedNullabilities, updatedSymbols, newSnapshotBuilder);
            try
            {
                Analyze(walker, symbol, diagnostics: null, initialState, snapshotBuilderOpt: newSnapshotBuilder);
            }
            finally
            {
                walker.Free();
            }

            var analyzedNullabilitiesMap = analyzedNullabilities.ToImmutable();
            var updatedSymbolsMap = updatedSymbols.ToImmutable();
            newSnapshots = newSnapshotBuilder?.ToManagerAndFree();

#if DEBUG
            if (binder.Compilation.NullableSemanticAnalysisEnabled)
            {
                DebugVerifier.Verify(analyzedNullabilitiesMap, updatedSymbolsMap, newSnapshots, node);
            }
#endif

            return new NullabilityRewriter(analyzedNullabilitiesMap, updatedSymbolsMap).Visit(node);
        }

        internal static void AnalyzeIfNeeded(
            Binder binder,
            BoundAttribute attribute,
            DiagnosticBag diagnostics)
        {
            var compilation = binder.Compilation;
            if (compilation.LanguageVersion < MessageID.IDS_FeatureNullableReferenceTypes.RequiredVersion() || !compilation.ShouldRunNullableWalker)
            {
                return;
            }

            Analyze(
                compilation,
                symbol: null,
                attribute,
                binder,
                binder.Conversions,
                diagnostics,
                useMethodSignatureParameterTypes: false,
                methodSignatureOpt: null,
                initialState: null,
                analyzedNullabilityMapOpt: null,
                updatedSymbolMapOpt: null,
                snapshotBuilderOpt: null,
                returnTypesOpt: null);
        }

        internal static void Analyze(
            CSharpCompilation compilation,
            BoundLambda lambda,
            Conversions conversions,
            DiagnosticBag diagnostics,
            MethodSymbol delegateInvokeMethod,
            VariableState initialState,
            ImmutableDictionary<BoundExpression, (NullabilityInfo, TypeSymbol)>.Builder analyzedNullabilityMapOpt,
            ImmutableDictionary<(BoundNode, Symbol), Symbol>.Builder updatedSymbolMapOpt,
            SnapshotManager.Builder snapshotBuilderOpt,
            ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)> returnTypesOpt)
        {
            Analyze(
                compilation,
                lambda.Symbol,
                lambda.Body,
                lambda.Binder,
                conversions,
                diagnostics,
                useMethodSignatureParameterTypes: !lambda.UnboundLambda.HasExplicitlyTypedParameterList,
                methodSignatureOpt: delegateInvokeMethod,
                initialState,
                analyzedNullabilityMapOpt,
                updatedSymbolMapOpt,
                snapshotBuilderOpt,
                returnTypesOpt);
        }

        private static void Analyze(
            CSharpCompilation compilation,
            Symbol symbol,
            BoundNode node,
            Binder binder,
            Conversions conversions,
            DiagnosticBag diagnostics,
            bool useMethodSignatureParameterTypes,
            MethodSymbol methodSignatureOpt,
            VariableState initialState,
            ImmutableDictionary<BoundExpression, (NullabilityInfo, TypeSymbol)>.Builder analyzedNullabilityMapOpt,
            ImmutableDictionary<(BoundNode, Symbol), Symbol>.Builder updatedSymbolMapOpt,
            SnapshotManager.Builder snapshotBuilderOpt,
            ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)> returnTypesOpt)
        {
            Debug.Assert(diagnostics != null);
            var walker = new NullableWalker(compilation,
                                                    symbol,
                                                    useMethodSignatureParameterTypes,
                                                    methodSignatureOpt,
                                                    node,
                                                    binder,
                                                    conversions,
                                                    initialState,
                                                    returnTypesOpt,
                                                    analyzedNullabilityMapOpt,
                                                    updatedSymbolMapOpt,
                                                    snapshotBuilderOpt);

            try
            {
                Analyze(walker, symbol, diagnostics, initialState, snapshotBuilderOpt);
            }
            finally
            {
                walker.Free();
            }
        }

        private static void Analyze(
            NullableWalker walker,
            Symbol symbol,
            DiagnosticBag diagnostics,
            VariableState initialState,
            SnapshotManager.Builder snapshotBuilderOpt)
        {
            try
            {
                bool badRegion = false;
                Optional<LocalState> initialLocalState = initialState is null ? default : new Optional<LocalState>(initialState.VariableNullableStates);
                var previousSlot = snapshotBuilderOpt?.EnterNewWalker(symbol) ?? -1;
                ImmutableArray<PendingBranch> returns = walker.Analyze(ref badRegion, initialLocalState);
                snapshotBuilderOpt?.ExitWalker(walker.SaveSharedState(), previousSlot);
                diagnostics?.AddRange(walker.Diagnostics);
                Debug.Assert(!badRegion);
            }
            catch (CancelledByStackGuardException ex) when (diagnostics != null)
            {
                ex.AddAnError(diagnostics);
            }
        }

        private SharedWalkerState SaveSharedState() =>
            new SharedWalkerState(
                _variableSlot.ToImmutableDictionary(),
                ImmutableArray.Create(variableBySlot, start: 0, length: nextVariableSlot),
                _variableTypes.ToImmutableDictionary(),
                _symbol);

        private void TakeIncrementalSnapshot(BoundNode node)
        {
            Debug.Assert(!IsConditionalState);
            _snapshotBuilderOpt?.TakeIncrementalSnapshot(node, State);
        }

        private void SetUpdatedSymbol(BoundNode node, Symbol originalSymbol, Symbol updatedSymbol)
        {
            if (_updatedSymbolMapOpt is object)
            {
                var key = (node, originalSymbol);
                if ((object)originalSymbol != updatedSymbol)
                {
                    _updatedSymbolMapOpt[key] = updatedSymbol;
                }
                else
                {
                    // If the symbol is reset, remove the updated symbol so we don't needlessly update the
                    // bound node later on. We do this unconditionally, as Remove will just return false
                    // if the key wasn't in the dictionary.
                    _ = _updatedSymbolMapOpt.Remove(key);
                }
            }
        }

        protected override void Normalize(ref LocalState state)
        {
            if (!state.Reachable)
                return;

            int oldNext = state.Capacity;
            state.EnsureCapacity(nextVariableSlot);
            Populate(ref state, oldNext);
        }

        private void Populate(ref LocalState state, int start)
        {
            int capacity = state.Capacity;
            for (int slot = start; slot < capacity; slot++)
            {
                PopulateOneSlot(ref state, slot);
            }
        }

        private void PopulateOneSlot(ref LocalState state, int slot)
        {
            state[slot] = GetDefaultState(ref state, slot);
        }

        private NullableFlowState GetDefaultState(ref LocalState state, int slot)
        {
            if (!state.Reachable)
                return NullableFlowState.NotNull;

            if (slot == 0)
                return NullableFlowState.MaybeNull;

            var variable = variableBySlot[slot];
            var symbol = variable.Symbol;

            switch (symbol.Kind)
            {
                case SymbolKind.Local:
                    {
                        var local = (LocalSymbol)symbol;
                        if (!_variableTypes.TryGetValue(local, out TypeWithAnnotations localType))
                        {
                            localType = local.TypeWithAnnotations;
                        }

                        return localType.ToTypeWithState().State;
                    }
                case SymbolKind.Parameter:
                    {
                        var parameter = (ParameterSymbol)symbol;
                        if (!_variableTypes.TryGetValue(parameter, out TypeWithAnnotations parameterType))
                        {
                            parameterType = parameter.TypeWithAnnotations;
                        }

                        return parameterType.ToTypeWithState().State;
                    }
                case SymbolKind.Field:
                case SymbolKind.Property:
                case SymbolKind.Event:
                    return GetDefaultState(symbol);
                case SymbolKind.ErrorType:
                    return NullableFlowState.NotNull;
                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        protected override bool TryGetReceiverAndMember(BoundExpression expr, out BoundExpression receiver, out Symbol member)
        {
            receiver = null;
            member = null;

            switch (expr.Kind)
            {
                case BoundKind.FieldAccess:
                    {
                        var fieldAccess = (BoundFieldAccess)expr;
                        var fieldSymbol = fieldAccess.FieldSymbol;
                        member = fieldSymbol;
                        if (fieldSymbol.IsFixedSizeBuffer)
                        {
                            return false;
                        }
                        if (fieldSymbol.IsStatic)
                        {
                            return true;
                        }
                        receiver = fieldAccess.ReceiverOpt;
                        break;
                    }
                case BoundKind.EventAccess:
                    {
                        var eventAccess = (BoundEventAccess)expr;
                        var eventSymbol = eventAccess.EventSymbol;
                        // https://github.com/dotnet/roslyn/issues/29901 Use AssociatedField for field-like events?
                        member = eventSymbol;
                        if (eventSymbol.IsStatic)
                        {
                            return true;
                        }
                        receiver = eventAccess.ReceiverOpt;
                        break;
                    }
                case BoundKind.PropertyAccess:
                    {
                        var propAccess = (BoundPropertyAccess)expr;
                        var propSymbol = propAccess.PropertySymbol;
                        member = propSymbol;
                        if (propSymbol.IsStatic)
                        {
                            return true;
                        }
                        receiver = propAccess.ReceiverOpt;
                        break;
                    }
            }

            Debug.Assert(member?.RequiresInstanceReceiver() ?? true);

            return (object)member != null &&
                (object)receiver != null &&
                receiver.Kind != BoundKind.TypeExpression &&
                (object)receiver.Type != null;
        }

        protected override int MakeSlot(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundKind.ThisReference:
                case BoundKind.BaseReference:
                    {
                        var method = getTopLevelMethod(_symbol as MethodSymbol);
                        var thisParameter = method?.ThisParameter;
                        return (object)thisParameter != null ? GetOrCreateSlot(thisParameter) : -1;
                    }
                case BoundKind.Conversion:
                    {
                        int slot = getPlaceholderSlot(node);
                        if (slot > 0)
                        {
                            return slot;
                        }
                        var conv = (BoundConversion)node;
                        switch (conv.Conversion.Kind)
                        {
                            case ConversionKind.ExplicitNullable:
                                {
                                    var operand = conv.Operand;
                                    var operandType = operand.Type;
                                    var convertedType = conv.Type;
                                    if (AreNullableAndUnderlyingTypes(operandType, convertedType, out _))
                                    {
                                        // Explicit conversion of Nullable<T> to T is equivalent to Nullable<T>.Value.
                                        // For instance, in the following, when evaluating `((A)a).B` we need to recognize
                                        // the nullability of `(A)a` (not nullable) and the slot (the slot for `a.Value`).
                                        //   struct A { B? B; }
                                        //   struct B { }
                                        //   if (a?.B != null) _ = ((A)a).B.Value; // no warning
                                        int containingSlot = MakeSlot(operand);
                                        return containingSlot < 0 ? -1 : GetNullableOfTValueSlot(operandType, containingSlot, out _);
                                    }
                                }
                                break;
                            case ConversionKind.Identity:
                            case ConversionKind.ImplicitReference:
                            case ConversionKind.ExplicitReference:
                            case ConversionKind.ImplicitTupleLiteral:
                            case ConversionKind.ExplicitTupleLiteral:
                            case ConversionKind.Boxing:
                            case ConversionKind.Unboxing:
                                // No need to create a slot for the boxed value (in the Boxing case) since assignment already
                                // clones slots and there is not another scenario where creating a slot is observable.
                                return MakeSlot(conv.Operand);
                        }
                    }
                    break;
                case BoundKind.DefaultLiteral:
                case BoundKind.DefaultExpression:
                case BoundKind.ObjectCreationExpression:
                case BoundKind.DynamicObjectCreationExpression:
                case BoundKind.AnonymousObjectCreationExpression:
                case BoundKind.NewT:
                case BoundKind.TupleLiteral:
                case BoundKind.ConvertedTupleLiteral:
                    return getPlaceholderSlot(node);
                case BoundKind.ConditionalReceiver:
                    {
                        return _lastConditionalAccessSlot;
                    }
                default:
                    {
                        int slot = getPlaceholderSlot(node);
                        return (slot > 0) ? slot : base.MakeSlot(node);
                    }
            }

            return -1;

            int getPlaceholderSlot(BoundExpression expr)
            {
                if (_placeholderLocalsOpt != null && _placeholderLocalsOpt.TryGetValue(expr, out PlaceholderLocal placeholder))
                {
                    return GetOrCreateSlot(placeholder);
                }
                return -1;
            }

            static MethodSymbol getTopLevelMethod(MethodSymbol method)
            {
                while ((object)method != null)
                {
                    var container = method.ContainingSymbol;
                    if (container.Kind == SymbolKind.NamedType)
                    {
                        return method;
                    }
                    method = container as MethodSymbol;
                }
                return null;
            }
        }

        private void VisitAndUnsplitAll<T>(ImmutableArray<T> nodes) where T : BoundNode
        {
            if (nodes.IsDefault)
            {
                return;
            }

            foreach (var node in nodes)
            {
                Visit(node);
                Unsplit();
            }
        }

        private void VisitWithoutDiagnostics(BoundNode node)
        {
            var previousDiagnostics = _disableDiagnostics;
            _disableDiagnostics = true;
            Visit(node);
            _disableDiagnostics = previousDiagnostics;
        }

        protected override void VisitRvalue(BoundExpression node, bool isKnownToBeAnLvalue = false)
        {
            Visit(node);
            VisitRvalueEpilogue(node);
        }

        /// <summary>
        /// The contents of this method, particularly <see cref="UseRvalueOnly"/>, are problematic when
        /// inlined. The methods themselves are small but they end up allocating significantly larger
        /// frames due to the use of biggish value types within them. The <see cref="VisitRvalue"/> method
        /// is used on a hot path for fluent calls and this size change is enough that it causes us
        /// to exceed our thresholds in EndToEndTests.OverflowOnFluentCall.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void VisitRvalueEpilogue(BoundExpression node)
        {
            Unsplit();
            UseRvalueOnly(node); // drop lvalue part
        }

        private TypeWithState VisitRvalueWithState(BoundExpression node)
        {
            VisitRvalue(node);
            return ResultType;
        }

        private TypeWithAnnotations VisitLvalueWithAnnotations(BoundExpression node)
        {
            Visit(node);
            Unsplit();
            return LvalueResultType;
        }

        private static object GetTypeAsDiagnosticArgument(TypeSymbol typeOpt)
        {
            return typeOpt ?? (object)"<null>";
        }

        private static object GetParameterAsDiagnosticArgument(ParameterSymbol parameterOpt)
        {
            return parameterOpt is null ?
                (object)"" :
                new FormattedSymbol(parameterOpt, SymbolDisplayFormat.ShortFormat);
        }

        private static object GetContainingSymbolAsDiagnosticArgument(ParameterSymbol parameterOpt)
        {
            var containingSymbol = parameterOpt?.ContainingSymbol;
            return containingSymbol is null ?
                (object)"" :
                new FormattedSymbol(containingSymbol, SymbolDisplayFormat.MinimallyQualifiedFormat);
        }

        private enum AssignmentKind
        {
            Assignment,
            Return,
            Argument,
            ForEachIterationVariable
        }

        /// <summary>
        /// Reports top-level nullability problem in assignment.
        /// </summary>
        private bool ReportNullableAssignmentIfNecessary(
            BoundExpression value,
            TypeWithAnnotations targetType,
            TypeWithState valueType,
            bool useLegacyWarnings,
            AssignmentKind assignmentKind = AssignmentKind.Assignment,
            ParameterSymbol parameterOpt = null,
            Conversion conversion = default,
            Location location = null)
        {
            if (value == null ||
                !targetType.HasType ||
                targetType.Type.IsValueType ||
                targetType.CanBeAssignedNull ||
                valueType.IsNotNull)
            {
                return false;
            }

            location ??= value.Syntax.GetLocation();
            var unwrappedValue = SkipReferenceConversions(value);
            if (unwrappedValue.IsSuppressed)
            {
                return false;
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            if (RequiresSafetyWarningWhenNullIntroduced(targetType))
            {
                if (conversion.Kind == ConversionKind.UnsetConversionKind)
                    conversion = this._conversions.ClassifyImplicitConversionFromType(valueType.Type, targetType.Type, ref useSiteDiagnostics);

                if (conversion.IsImplicit && !conversion.IsDynamic)
                {
                    // For type parameters that cannot be annotated, the analysis must report those
                    // places where null values first sneak in, like `default`, `null`, and `GetFirstOrDefault`,
                    // as a safety diagnostic.  This is NOT one of those places.
                    return false;
                }

                useLegacyWarnings = false;
            }

            if (reportNullLiteralAssignmentIfNecessary(value, location, valueType.ToTypeWithAnnotations()))
            {
                return true;
            }

            if (assignmentKind == AssignmentKind.Argument)
            {
                ReportDiagnostic(ErrorCode.WRN_NullReferenceArgument, location,
                    GetParameterAsDiagnosticArgument(parameterOpt),
                    GetContainingSymbolAsDiagnosticArgument(parameterOpt));
            }
            else if (useLegacyWarnings)
            {
                ReportNonSafetyDiagnostic(location);
            }
            else
            {
                ReportDiagnostic(assignmentKind switch { AssignmentKind.Return => ErrorCode.WRN_NullReferenceReturn, AssignmentKind.ForEachIterationVariable => ErrorCode.WRN_NullReferenceIterationVariable, _ => ErrorCode.WRN_NullReferenceAssignment }, location);
            }

            return true;

            // Report warning converting null literal to non-nullable reference type.
            // target (e.g.: `object x = null;` or calling `void F(object y)` with `F(null)`).
            bool reportNullLiteralAssignmentIfNecessary(BoundExpression expr, Location location, TypeWithAnnotations exprType)
            {
                if (expr.ConstantValue?.IsNull != true)
                {
                    return false;
                }

                // For type parameters that cannot be annotated, the analysis must report those
                // places where null values first sneak in, like `default`, `null`, and `GetFirstOrDefault`,
                // as a safety diagnostic.  This is one of those places.
                if (useLegacyWarnings && !RequiresSafetyWarningWhenNullIntroduced(exprType))
                {
                    ReportNonSafetyDiagnostic(location);
                }
                else
                {
                    ReportDiagnostic(assignmentKind == AssignmentKind.Return ? ErrorCode.WRN_NullReferenceReturn : ErrorCode.WRN_NullAsNonNullable, location);
                }
                return true;
            }
        }

        private static bool IsDefaultValue(BoundExpression expr)
        {
            switch (expr.Kind)
            {
                case BoundKind.Conversion:
                    {
                        var conversion = (BoundConversion)expr;
                        var conversionKind = conversion.Conversion.Kind;
                        return (conversionKind == ConversionKind.DefaultLiteral || conversionKind == ConversionKind.NullLiteral) &&
                            IsDefaultValue(conversion.Operand);
                    }
                case BoundKind.DefaultLiteral:
                case BoundKind.DefaultExpression:
                    return true;
                default:
                    return false;
            }
        }

        private void ReportNullabilityMismatchInAssignment(SyntaxNode syntaxNode, object sourceType, object destinationType)
        {
            ReportDiagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, syntaxNode, sourceType, destinationType);
        }

        private void ReportNullabilityMismatchInAssignment(Location location, object sourceType, object destinationType)
        {
            ReportDiagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, location, sourceType, destinationType);
        }

        /// <summary>
        /// Update tracked value on assignment.
        /// </summary>
        private void TrackNullableStateForAssignment(
            BoundExpression valueOpt,
            TypeWithAnnotations targetType,
            int targetSlot,
            TypeWithState valueType,
            int valueSlot = -1)
        {
            Debug.Assert(!IsConditionalState);

            if (this.State.Reachable)
            {
                if (!targetType.HasType)
                {
                    return;
                }

                if (targetSlot <= 0 || targetSlot == valueSlot)
                {
                    return;
                }

                if (targetSlot >= this.State.Capacity) Normalize(ref this.State);

                var newState = valueType.State;
                SetStateAndTrackForFinally(ref this.State, targetSlot, newState);
                InheritDefaultState(targetSlot);

                // https://github.com/dotnet/roslyn/issues/33428: Can the areEquivalentTypes check be removed
                // if InheritNullableStateOfMember asserts the member is valid for target and value?
                if (areEquivalentTypes(targetType, valueType))
                {
                    if (targetType.Type.IsReferenceType ||
                        targetType.TypeKind == TypeKind.TypeParameter ||
                        targetType.IsNullableType())
                    {
                        if (valueSlot > 0)
                        {
                            InheritNullableStateOfTrackableType(targetSlot, valueSlot, skipSlot: targetSlot);
                        }
                    }
                    else if (EmptyStructTypeCache.IsTrackableStructType(targetType.Type))
                    {
                        InheritNullableStateOfTrackableStruct(targetType.Type, targetSlot, valueSlot, isDefaultValue: !(valueOpt is null) && IsDefaultValue(valueOpt), skipSlot: targetSlot);
                    }
                }
            }

            static bool areEquivalentTypes(TypeWithAnnotations target, TypeWithState assignedValue) =>
                target.Type.Equals(assignedValue.Type, TypeCompareKind.AllIgnoreOptions);
        }

        private void ReportNonSafetyDiagnostic(Location location)
        {
            ReportDiagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, location);
        }

        private void ReportDiagnostic(ErrorCode errorCode, SyntaxNode syntaxNode, params object[] arguments)
        {
            ReportDiagnostic(errorCode, syntaxNode.GetLocation(), arguments);
        }

        private void ReportDiagnostic(ErrorCode errorCode, Location location, params object[] arguments)
        {
            Debug.Assert(ErrorFacts.NullableWarnings.Contains(MessageProvider.Instance.GetIdForErrorCode((int)errorCode)));
            if (IsReachable() && !_disableDiagnostics)
            {
                Diagnostics.Add(errorCode, location, arguments);
            }
        }

        private void InheritNullableStateOfTrackableStruct(TypeSymbol targetType, int targetSlot, int valueSlot, bool isDefaultValue, int skipSlot = -1)
        {
            Debug.Assert(targetSlot > 0);
            Debug.Assert(EmptyStructTypeCache.IsTrackableStructType(targetType));

            if (skipSlot < 0)
            {
                skipSlot = targetSlot;
            }

            if (!isDefaultValue && valueSlot > 0)
            {
                InheritNullableStateOfTrackableType(targetSlot, valueSlot, skipSlot);
            }
            else
            {
                foreach (var field in _emptyStructTypeCache.GetStructInstanceFields(targetType))
                {
                    InheritNullableStateOfMember(targetSlot, valueSlot, field, isDefaultValue: isDefaultValue, skipSlot);
                }
            }
        }

        // 'skipSlot' is the original target slot that should be skipped in case of cycles.
        private void InheritNullableStateOfMember(int targetContainerSlot, int valueContainerSlot, Symbol member, bool isDefaultValue, int skipSlot)
        {
            Debug.Assert(targetContainerSlot > 0);
            Debug.Assert(skipSlot > 0);
            // https://github.com/dotnet/roslyn/issues/33428: Ensure member is valid for target and value.

            TypeWithAnnotations fieldOrPropertyType = member.GetTypeOrReturnType();

            if (fieldOrPropertyType.Type.IsReferenceType ||
                fieldOrPropertyType.TypeKind == TypeKind.TypeParameter ||
                fieldOrPropertyType.IsNullableType())
            {
                int targetMemberSlot = GetOrCreateSlot(member, targetContainerSlot);
                if (targetMemberSlot > 0)
                {
                    NullableFlowState value = isDefaultValue ? NullableFlowState.MaybeNull : fieldOrPropertyType.ToTypeWithState().State;
                    int valueMemberSlot = -1;

                    if (valueContainerSlot > 0)
                    {
                        valueMemberSlot = VariableSlot(member, valueContainerSlot);
                        if (valueMemberSlot == skipSlot)
                        {
                            return;
                        }
                        value = valueMemberSlot > 0 && valueMemberSlot < this.State.Capacity ?
                            this.State[valueMemberSlot] :
                            NullableFlowState.NotNull;
                    }

                    SetStateAndTrackForFinally(ref this.State, targetMemberSlot, value);
                    if (valueMemberSlot > 0)
                    {
                        InheritNullableStateOfTrackableType(targetMemberSlot, valueMemberSlot, skipSlot);
                    }
                }
            }
            else if (EmptyStructTypeCache.IsTrackableStructType(fieldOrPropertyType.Type))
            {
                int targetMemberSlot = GetOrCreateSlot(member, targetContainerSlot);
                if (targetMemberSlot > 0)
                {
                    int valueMemberSlot = (valueContainerSlot > 0) ? GetOrCreateSlot(member, valueContainerSlot) : -1;
                    if (valueMemberSlot == skipSlot)
                    {
                        return;
                    }
                    InheritNullableStateOfTrackableStruct(fieldOrPropertyType.Type, targetMemberSlot, valueMemberSlot, isDefaultValue: isDefaultValue, skipSlot);
                }
            }
        }

        /// <summary>
        /// Whenever assigning a variable, and that variable is not declared at the point the state is being set,
        /// and the new state might be <see cref="NullableFlowState.MaybeNull"/>, this method should be called to perform the
        /// state setting and to ensure the mutation is visible outside the finally block when the mutation occurs in a
        /// finally block.
        /// </summary>
        private void SetStateAndTrackForFinally(ref LocalState state, int slot, NullableFlowState newState)
        {
            state[slot] = newState;
            if (newState == NullableFlowState.MaybeNull && _tryState.HasValue)
            {
                var tryState = _tryState.Value;
                tryState[slot] = NullableFlowState.MaybeNull;
                _tryState = tryState;
            }
        }

        private void InheritDefaultState(int targetSlot)
        {
            Debug.Assert(targetSlot > 0);

            // Reset the state of any members of the target.
            for (int slot = targetSlot + 1; slot < nextVariableSlot; slot++)
            {
                var variable = variableBySlot[slot];
                if (variable.ContainingSlot != targetSlot)
                {
                    continue;
                }

                SetStateAndTrackForFinally(ref this.State, slot, GetDefaultState(variable.Symbol));
                InheritDefaultState(slot);
            }
        }

        private NullableFlowState GetDefaultState(Symbol symbol)
            => ApplyUnconditionalAnnotations(symbol.GetTypeOrReturnType().ToTypeWithState(), GetRValueAnnotations(symbol)).State;

        private void InheritNullableStateOfTrackableType(int targetSlot, int valueSlot, int skipSlot)
        {
            Debug.Assert(targetSlot > 0);
            Debug.Assert(valueSlot > 0);

            // Clone the state for members that have been set on the value.
            for (int slot = valueSlot + 1; slot < nextVariableSlot; slot++)
            {
                var variable = variableBySlot[slot];
                if (variable.ContainingSlot != valueSlot)
                {
                    continue;
                }
                var member = variable.Symbol;
                Debug.Assert(member.Kind == SymbolKind.Field || member.Kind == SymbolKind.Property || member.Kind == SymbolKind.Event);
                InheritNullableStateOfMember(targetSlot, valueSlot, member, isDefaultValue: false, skipSlot);
            }
        }

        protected override LocalState TopState()
        {
            var state = LocalState.ReachableState(capacity: nextVariableSlot);
            Populate(ref state, start: 0);
            return state;
        }

        protected override LocalState UnreachableState()
        {
            return LocalState.UnreachableState;
        }

        protected override LocalState ReachableBottomState()
        {
            // Create a reachable state in which all variables are known to be non-null.
            return LocalState.ReachableState(capacity: nextVariableSlot);
        }

        private void EnterParameters()
        {
            var methodSymbol = _symbol as MethodSymbol;
            if (methodSymbol is null)
            {
                return;
            }

            var methodParameters = methodSymbol.Parameters;
            var signatureParameters = _useMethodSignatureParameterTypes ? _methodSignatureOpt.Parameters : methodParameters;
            for (int i = 0; i < methodParameters.Length; i++)
            {
                var parameter = methodParameters[i];
                // In error scenarios, the method can potentially have more parameters than the signature. If so, use the parameter type for those
                // errored parameters
                var parameterType = i >= signatureParameters.Length ? parameter.TypeWithAnnotations : signatureParameters[i].TypeWithAnnotations;
                EnterParameter(parameter, parameterType);
            }
        }

        private void EnterParameter(ParameterSymbol parameter, TypeWithAnnotations parameterType)
        {
            _variableTypes[parameter] = parameterType;
            int slot = GetOrCreateSlot(parameter);

            Debug.Assert(!IsConditionalState);
            if (slot > 0)
            {
                this.State[slot] = parameterType.ToTypeWithState().State;
                if (EmptyStructTypeCache.IsTrackableStructType(parameterType.Type))
                {
                    InheritNullableStateOfTrackableStruct(
                        parameterType.Type,
                        slot,
                        valueSlot: -1,
                        isDefaultValue: parameter.ExplicitDefaultConstantValue?.IsNull == true);
                }
            }
        }

        protected override BoundNode VisitReturnStatementNoAdjust(BoundReturnStatement node)
        {
            Debug.Assert(!IsConditionalState);

            BoundExpression expr = node.ExpressionOpt;
            if (expr == null)
            {
                return null;
            }

            // Should not convert to method return type when inferring return type (when _returnTypesOpt != null).
            if (_returnTypesOpt == null &&
                TryGetReturnType(out TypeWithAnnotations returnType))
            {
                if (node.RefKind == RefKind.None)
                {
                    VisitOptionalImplicitConversion(expr, returnType, useLegacyWarnings: false, trackMembers: false, AssignmentKind.Return);
                }
                else
                {
                    // return ref expr;
                    VisitRefExpression(expr, returnType);
                }
            }
            else
            {
                var result = VisitRvalueWithState(expr);
                if (_returnTypesOpt != null)
                {
                    _returnTypesOpt.Add((node, result.ToTypeWithAnnotations()));
                }
            }

            return null;
        }

        private TypeWithState VisitRefExpression(BoundExpression expr, TypeWithAnnotations destinationType)
        {
            Visit(expr);
            TypeWithState resultType = ResultType;
            if (!expr.IsSuppressed && RemoveConversion(expr, includeExplicitConversions: false).expression.Kind != BoundKind.ThrowExpression)
            {
                var lvalueResultType = LvalueResultType;
                if (IsNullabilityMismatch(lvalueResultType, destinationType))
                {
                    // declared types must match
                    ReportNullabilityMismatchInAssignment(expr.Syntax, lvalueResultType, destinationType);
                }
                else
                {
                    // types match, but state would let a null in
                    ReportNullableAssignmentIfNecessary(expr, destinationType, resultType, useLegacyWarnings: false);
                }
            }

            return resultType;
        }

        private bool TryGetReturnType(out TypeWithAnnotations type)
        {
            var method = _symbol as MethodSymbol;
            if (method is null)
            {
                type = default;
                return false;
            }

            var returnType = (_methodSignatureOpt ?? method).ReturnTypeWithAnnotations;
            Debug.Assert((object)returnType != LambdaSymbol.ReturnTypeIsBeingInferred);

            if (returnType.IsVoidType())
            {
                type = default;
                return false;
            }

            if (!method.IsAsync)
            {
                type = returnType;
                return true;
            }

            if (returnType.Type.IsGenericTaskType(compilation))
            {
                type = ((NamedTypeSymbol)returnType.Type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Single();
                return true;
            }

            type = default;
            return false;
        }

        private static bool RequiresSafetyWarningWhenNullIntroduced(TypeWithAnnotations typeWithAnnotations)
        {
            return
                typeWithAnnotations is { Type: TypeSymbol type, NullableAnnotation: NullableAnnotation.NotAnnotated } &&
                type.IsTypeParameterDisallowingAnnotation() &&
                !type.IsNullableTypeOrTypeParameter();
        }

        private static bool RequiresSafetyWarningWhenNullIntroduced(TypeSymbol type)
        {
            return
                type.IsTypeParameterDisallowingAnnotation() &&
                !type.IsNullableTypeOrTypeParameter();
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            var local = node.LocalSymbol;
            int slot = GetOrCreateSlot(local);
            var type = GetDeclaredLocalResult(local);

            if (!node.Type.Equals(type.Type, TypeCompareKind.ConsiderEverything | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes | TypeCompareKind.IgnoreDynamicAndTupleNames))
            {
                // When the local is used before or during initialization, there can potentially be a mismatch between node.LocalSymbol.Type and node.Type. We
                // need to prefer node.Type as we shouldn't be changing the type of the BoundLocal node during rewrite.
                // https://github.com/dotnet/roslyn/issues/34158
                Debug.Assert(node.Type.IsErrorType() || type.Type.IsErrorType());
                type = TypeWithAnnotations.Create(node.Type, type.NullableAnnotation);
            }

            SetResult(node, GetAdjustedResult(type, slot), type);
            return null;
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            DeclareLocals(node.Locals);
            foreach (var statement in node.Statements)
            {
                VisitStatement(statement);
            }

            return null;
        }

        public override BoundNode VisitDoStatement(BoundDoStatement node)
        {
            DeclareLocals(node.Locals);
            return base.VisitDoStatement(node);
        }

        public override BoundNode VisitWhileStatement(BoundWhileStatement node)
        {
            DeclareLocals(node.Locals);
            return base.VisitWhileStatement(node);
        }

        public override BoundNode VisitForStatement(BoundForStatement node)
        {
            DeclareLocals(node.OuterLocals);
            DeclareLocals(node.InnerLocals);
            return base.VisitForStatement(node);
        }

        public override BoundNode VisitForEachStatement(BoundForEachStatement node)
        {
            DeclareLocals(node.IterationVariables);
            return base.VisitForEachStatement(node);
        }

        public override BoundNode VisitUsingStatement(BoundUsingStatement node)
        {
            DeclareLocals(node.Locals);
            return base.VisitUsingStatement(node);
        }

        public override BoundNode VisitFixedStatement(BoundFixedStatement node)
        {
            DeclareLocals(node.Locals);
            return base.VisitFixedStatement(node);
        }

        public override BoundNode VisitConstructorMethodBody(BoundConstructorMethodBody node)
        {
            DeclareLocals(node.Locals);
            return base.VisitConstructorMethodBody(node);
        }

        private void DeclareLocal(LocalSymbol local)
        {
            if (local.DeclarationKind != LocalDeclarationKind.None)
            {
                int slot = GetOrCreateSlot(local);
                if (slot > 0)
                {
                    PopulateOneSlot(ref this.State, slot);
                    InheritDefaultState(slot);
                }
            }
        }

        private void DeclareLocals(ImmutableArray<LocalSymbol> locals)
        {
            foreach (var local in locals)
            {
                DeclareLocal(local);
            }
        }

        public override BoundNode VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            var local = node.LocalSymbol;
            int slot = GetOrCreateSlot(local);

            // We need visit the optional arguments so that we can return nullability information
            // about them, but we don't want to communciate any information about anything underneath.
            // Additionally, tests like Scope_DeclaratorArguments_06 can have conditional expressions
            // in the optional arguments that can leave us in a split state, so we want to make sure
            // we are not in a conditional state after.
            Debug.Assert(!IsConditionalState);
            var oldDisable = _disableDiagnostics;
            _disableDiagnostics = true;
            var currentState = State;
            VisitAndUnsplitAll(node.ArgumentsOpt);
            _disableDiagnostics = oldDisable;
            SetState(currentState);
            if (node.DeclaredTypeOpt != null)
            {
                VisitTypeExpression(node.DeclaredTypeOpt);
            }

            var initializer = node.InitializerOpt;
            if (initializer is null)
            {
                return null;
            }

            TypeWithAnnotations type = local.TypeWithAnnotations;
            TypeWithState valueType;
            bool inferredType = node.InferredType;
            if (local.IsRef)
            {
                valueType = VisitRefExpression(initializer, type);
            }
            else
            {
                valueType = VisitOptionalImplicitConversion(initializer, targetTypeOpt: inferredType ? default : type, useLegacyWarnings: true, trackMembers: true, AssignmentKind.Assignment);
            }

            if (inferredType)
            {
                if (valueType.HasNullType)
                {
                    Debug.Assert(type.Type.IsErrorType());
                    valueType = type.ToTypeWithState();
                }

                type = valueType.ToTypeWithAnnotations();
                _variableTypes[local] = type;

                if (node.DeclaredTypeOpt != null)
                {
                    SetAnalyzedNullability(node.DeclaredTypeOpt, new VisitResult(valueType, type), true);
                }
            }

            TrackNullableStateForAssignment(initializer, type, slot, valueType, MakeSlot(initializer));
            return null;
        }

        protected override BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            Debug.Assert(!IsConditionalState);
            SetInvalidResult();
            _ = base.VisitExpressionWithoutStackGuard(node);
            TypeWithState resultType = ResultType;

#if DEBUG
            // Verify Visit method set _result.
            Debug.Assert((object)resultType.Type != _invalidType.Type);
            Debug.Assert(AreCloseEnough(resultType.Type, node.Type));
#endif

            if (ShouldMakeNotNullRvalue(node))
            {
                var result = resultType.WithNotNullState();
                SetResult(node, result, LvalueResultType);
            }
            return null;
        }

#if DEBUG
        // For asserts only.
        private static bool AreCloseEnough(TypeSymbol typeA, TypeSymbol typeB)
        {
            // https://github.com/dotnet/roslyn/issues/34993: We should be able to tighten this to ensure that we're actually always returning the same type,
            // not error if one is null or ignoring certain types
            if ((object)typeA == typeB)
            {
                return true;
            }
            if (typeA is null || typeB is null)
            {
                return typeA?.IsErrorType() != false && typeB?.IsErrorType() != false;
            }
            return canIgnoreAnyType(typeA) ||
                canIgnoreAnyType(typeB) ||
                typeA.Equals(typeB, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes | TypeCompareKind.IgnoreDynamicAndTupleNames); // Ignore TupleElementNames (see https://github.com/dotnet/roslyn/issues/23651).

            bool canIgnoreAnyType(TypeSymbol type)
            {
                return (object)type.VisitType((t, unused1, unused2) => canIgnoreType(t), (object)null) != null;
            }
            bool canIgnoreType(TypeSymbol type)
            {
                return type.IsErrorType() || type.IsDynamic() || type.HasUseSiteError || (type.IsAnonymousType && canIgnoreAnonymousType((NamedTypeSymbol)type));
            }
            bool canIgnoreAnonymousType(NamedTypeSymbol type)
            {
                return AnonymousTypeManager.GetAnonymousTypePropertyTypesWithAnnotations(type).Any(t => canIgnoreAnyType(t.Type));
            }
        }
#endif

        public override BoundNode Visit(BoundNode node)
        {
            TakeIncrementalSnapshot(node);
            return base.Visit(node);
        }

        protected override void VisitStatement(BoundStatement statement)
        {
            SetInvalidResult();
            base.VisitStatement(statement);
            SetInvalidResult();
        }

        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            Debug.Assert(!IsConditionalState);
            var arguments = node.Arguments;
            var argumentResults = VisitArguments(node, arguments, node.ArgumentRefKindsOpt, node.Constructor, node.ArgsToParamsOpt, node.Expanded, invokedAsExtensionMethod: false).results;
            VisitObjectOrDynamicObjectCreation(node, arguments, argumentResults, node.InitializerExpressionOpt);
            return null;
        }

        private void VisitObjectOrDynamicObjectCreation(
            BoundExpression node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<VisitArgumentResult> argumentResults,
            BoundExpression initializerOpt)
        {
            Debug.Assert(node.Kind == BoundKind.ObjectCreationExpression ||
                node.Kind == BoundKind.DynamicObjectCreationExpression ||
                node.Kind == BoundKind.NewT);
            var argumentTypes = argumentResults.SelectAsArray(ar => ar.RValueType);

            int slot = -1;
            TypeSymbol type = node.Type;
            NullableFlowState resultState = NullableFlowState.NotNull;
            if ((object)type != null)
            {
                slot = GetOrCreatePlaceholderSlot(node);
                if (slot > 0)
                {
                    var constructor = (node as BoundObjectCreationExpression)?.Constructor;
                    bool isDefaultValueTypeConstructor = constructor?.IsDefaultValueTypeConstructor() == true;

                    if (EmptyStructTypeCache.IsTrackableStructType(type))
                    {
                        var tupleType = constructor?.ContainingType as TupleTypeSymbol;
                        if ((object)tupleType != null && !isDefaultValueTypeConstructor)
                        {
                            // new System.ValueTuple<T1, ..., TN>(e1, ..., eN)
                            TrackNullableStateOfTupleElements(slot, tupleType, arguments, argumentTypes, useRestField: true);
                        }
                        else
                        {
                            InheritNullableStateOfTrackableStruct(
                                type,
                                slot,
                                valueSlot: -1,
                                isDefaultValue: isDefaultValueTypeConstructor);
                        }
                    }
                    else if (type.IsNullableType())
                    {
                        if (isDefaultValueTypeConstructor)
                        {
                            // a nullable value type created with its default constructor is by definition null
                            resultState = NullableFlowState.MaybeNull;
                        }
                        else if (constructor.ParameterCount == 1)
                        {
                            // if we deal with one-parameter ctor that takes underlying, then Value state is inferred from the argument.
                            var parameterType = constructor.ParameterTypesWithAnnotations[0];
                            if (AreNullableAndUnderlyingTypes(type, parameterType.Type, out TypeWithAnnotations underlyingType))
                            {
                                var operand = arguments[0];
                                int valueSlot = MakeSlot(operand);
                                if (valueSlot > 0)
                                {
                                    TrackNullableStateOfNullableValue(slot, type, operand, underlyingType.ToTypeWithState(), valueSlot);
                                }
                            }
                        }
                    }

                    this.State[slot] = resultState;
                }
            }

            if (initializerOpt != null)
            {
                VisitObjectCreationInitializer(null, slot, initializerOpt);
            }

            SetResultType(node, TypeWithState.Create(type, resultState));
        }

        private void VisitObjectCreationInitializer(Symbol containingSymbol, int containingSlot, BoundExpression node)
        {
            TakeIncrementalSnapshot(node);
            switch (node.Kind)
            {
                case BoundKind.ObjectInitializerExpression:
                    checkImplicitReceiver();
                    foreach (var initializer in ((BoundObjectInitializerExpression)node).Initializers)
                    {
                        switch (initializer.Kind)
                        {
                            case BoundKind.AssignmentOperator:
                                VisitObjectElementInitializer(containingSlot, (BoundAssignmentOperator)initializer);
                                break;
                            default:
                                VisitRvalue(initializer);
                                break;
                        }
                    }
                    break;
                case BoundKind.CollectionInitializerExpression:
                    checkImplicitReceiver();
                    foreach (var initializer in ((BoundCollectionInitializerExpression)node).Initializers)
                    {
                        switch (initializer.Kind)
                        {
                            case BoundKind.CollectionElementInitializer:
                                VisitCollectionElementInitializer((BoundCollectionElementInitializer)initializer);
                                break;
                            default:
                                VisitRvalue(initializer);
                                break;
                        }
                    }
                    break;
                default:
                    Debug.Assert((object)containingSymbol != null);
                    if ((object)containingSymbol != null)
                    {
                        var type = containingSymbol.GetTypeOrReturnType();
                        TypeWithState resultType = VisitOptionalImplicitConversion(node, type, useLegacyWarnings: false, trackMembers: true, AssignmentKind.Assignment);
                        TrackNullableStateForAssignment(node, type, containingSlot, resultType, MakeSlot(node));
                    }
                    break;
            }

            void checkImplicitReceiver()
            {
                if (containingSlot >= 0)
                {
                    _ = ReportPossibleNullReceiverIfNeeded(node.Type, this.State[containingSlot], checkNullableValueType: false, node.Syntax, out _);
                }
            }
        }

        private void VisitObjectElementInitializer(int containingSlot, BoundAssignmentOperator node)
        {
            TakeIncrementalSnapshot(node);
            var left = node.Left;
            switch (left.Kind)
            {
                case BoundKind.ObjectInitializerMember:
                    {
                        var objectInitializer = (BoundObjectInitializerMember)left;
                        TakeIncrementalSnapshot(left);
                        var symbol = objectInitializer.MemberSymbol;
                        if (!objectInitializer.Arguments.IsDefaultOrEmpty)
                        {
                            VisitArguments(objectInitializer, objectInitializer.Arguments, objectInitializer.ArgumentRefKindsOpt, (PropertySymbol)symbol, objectInitializer.ArgsToParamsOpt, objectInitializer.Expanded);
                        }

                        if ((object)symbol != null)
                        {
                            int slot = (containingSlot < 0) ? -1 : GetOrCreateSlot(symbol, containingSlot);
                            VisitObjectCreationInitializer(symbol, slot, node.Right);
                            // https://github.com/dotnet/roslyn/issues/35040: Should likely be setting _resultType in VisitObjectCreationInitializer
                            // and using that value instead of reconstructing here
                        }

                        var result = new VisitResult(objectInitializer.Type, NullableAnnotation.NotAnnotated, NullableFlowState.NotNull);
                        SetAnalyzedNullability(objectInitializer, result);
                        SetAnalyzedNullability(node, result);
                    }
                    break;
                default:
                    Visit(node);
                    break;
            }
        }

        private new void VisitCollectionElementInitializer(BoundCollectionElementInitializer node)
        {
            // Note: we analyze even omitted calls
            (var reinferredMethod, _, _) = VisitArguments(node, node.Arguments, refKindsOpt: default, node.AddMethod, node.ArgsToParamsOpt, node.Expanded, node.InvokedAsExtensionMethod);
            if (node.ImplicitReceiverOpt != null)
            {
                Debug.Assert(node.ImplicitReceiverOpt.Kind == BoundKind.ImplicitReceiver);
                SetAnalyzedNullability(node.ImplicitReceiverOpt, new VisitResult(node.ImplicitReceiverOpt.Type, NullableAnnotation.NotAnnotated, NullableFlowState.NotNull));
            }
            SetUnknownResultNullability(node);
            SetUpdatedSymbol(node, node.AddMethod, reinferredMethod);
        }

        private void SetNotNullResult(BoundExpression node)
        {
            SetResultType(node, TypeWithState.Create(node.Type, NullableFlowState.NotNull));
        }

        /// <summary>
        /// Returns true if the type is a struct with no fields or properties.
        /// </summary>
        protected override bool IsEmptyStructType(TypeSymbol type)
        {
            if (type.TypeKind != TypeKind.Struct)
            {
                return false;
            }

            // EmptyStructTypeCache.IsEmptyStructType() returns false
            // if there are non-cyclic fields.
            if (!_emptyStructTypeCache.IsEmptyStructType(type))
            {
                return false;
            }

            if (type.SpecialType != SpecialType.None)
            {
                return true;
            }

            var members = ((NamedTypeSymbol)type).GetMembersUnordered();

            // EmptyStructTypeCache.IsEmptyStructType() returned true. If there are fields,
            // at least one of those fields must be cyclic, so treat the type as empty.
            if (members.Any(m => m.Kind == SymbolKind.Field))
            {
                return true;
            }

            // If there are properties, the type is not empty.
            if (members.Any(m => m.Kind == SymbolKind.Property))
            {
                return false;
            }

            return true;
        }

        private int GetOrCreatePlaceholderSlot(BoundExpression node)
        {
            if (IsEmptyStructType(node.Type))
            {
                return -1;
            }

            return GetOrCreatePlaceholderSlot(node, TypeWithAnnotations.Create(node.Type, NullableAnnotation.NotAnnotated));
        }

        private int GetOrCreatePlaceholderSlot(object identifier, TypeWithAnnotations type)
        {
            _placeholderLocalsOpt ??= PooledDictionary<object, PlaceholderLocal>.GetInstance();
            if (!_placeholderLocalsOpt.TryGetValue(identifier, out PlaceholderLocal placeholder))
            {
                placeholder = new PlaceholderLocal(_symbol, identifier, type);
                _placeholderLocalsOpt.Add(identifier, placeholder);
            }

            Debug.Assert((object)placeholder != null);
            return GetOrCreateSlot(placeholder, forceSlotEvenIfEmpty: true);
        }

        public override BoundNode VisitAnonymousObjectCreationExpression(BoundAnonymousObjectCreationExpression node)
        {
            Debug.Assert(!IsConditionalState);
            Debug.Assert(node.Type.IsAnonymousType);

            var anonymousType = (NamedTypeSymbol)node.Type;
            var arguments = node.Arguments;
            var argumentTypes = arguments.SelectAsArray((arg, self) =>
                self.VisitRvalueWithState(arg), this);
            var argumentsWithAnnotations = argumentTypes.SelectAsArray(arg =>
                arg.ToTypeWithAnnotations());

            if (argumentsWithAnnotations.All(argType => argType.HasType))
            {
                anonymousType = AnonymousTypeManager.ConstructAnonymousTypeSymbol(anonymousType, argumentsWithAnnotations);
                int receiverSlot = GetOrCreatePlaceholderSlot(node);
                int currentDeclarationIndex = 0;
                for (int i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];
                    var argumentType = argumentTypes[i];
                    var property = AnonymousTypeManager.GetAnonymousTypeProperty(anonymousType, i);
                    TrackNullableStateForAssignment(argument, property.TypeWithAnnotations, GetOrCreateSlot(property, receiverSlot), argumentType, MakeSlot(argument));

                    var currentDeclaration = getDeclaration(node, property, ref currentDeclarationIndex);
                    if (currentDeclaration is object)
                    {
                        TakeIncrementalSnapshot(currentDeclaration);
                        SetAnalyzedNullability(currentDeclaration, new VisitResult(argumentType, property.TypeWithAnnotations));
                    }
                }
            }

            SetResultType(node, TypeWithState.Create(anonymousType, NullableFlowState.NotNull));
            return null;

            static BoundAnonymousPropertyDeclaration getDeclaration(BoundAnonymousObjectCreationExpression node, PropertySymbol currentProperty, ref int currentDeclarationIndex)
            {
                if (currentDeclarationIndex >= node.Declarations.Length)
                {
                    return null;
                }

                var currentDeclaration = node.Declarations[currentDeclarationIndex];

                if (currentDeclaration.Property.MemberIndexOpt == currentProperty.MemberIndexOpt)
                {
                    currentDeclarationIndex++;
                    return currentDeclaration;
                }

                return null;
            }
        }

        public override BoundNode VisitArrayCreation(BoundArrayCreation node)
        {
            foreach (var expr in node.Bounds)
            {
                VisitRvalue(expr);
            }
            TypeSymbol resultType = (node.InitializerOpt == null) ? node.Type : VisitArrayInitializer(node);
            SetResultType(node, TypeWithState.Create(resultType, NullableFlowState.NotNull));
            return null;
        }

        private ArrayTypeSymbol VisitArrayInitializer(BoundArrayCreation node)
        {
            TakeIncrementalSnapshot(node.InitializerOpt);
            BoundArrayInitialization initialization = node.InitializerOpt;
            var expressions = ArrayBuilder<BoundExpression>.GetInstance(initialization.Initializers.Length);
            GetArrayElements(initialization, expressions);
            int n = expressions.Count;

            // Consider recording in the BoundArrayCreation
            // whether the array was implicitly typed, rather than relying on syntax.
            bool isInferred = node.Syntax.Kind() == SyntaxKind.ImplicitArrayCreationExpression;
            var arrayType = (ArrayTypeSymbol)node.Type;
            var elementType = arrayType.ElementTypeWithAnnotations;
            if (!isInferred)
            {
                foreach (var expr in expressions)
                {
                    _ = VisitOptionalImplicitConversion(expr, elementType, useLegacyWarnings: false, trackMembers: false, AssignmentKind.Assignment);
                }
            }
            else
            {
                var expressionsNoConversions = ArrayBuilder<BoundExpression>.GetInstance(n);
                var conversions = ArrayBuilder<Conversion>.GetInstance(n);
                var resultTypes = ArrayBuilder<TypeWithState>.GetInstance(n);
                var placeholderBuilder = ArrayBuilder<BoundExpression>.GetInstance(n);
                foreach (var expression in expressions)
                {
                    // collect expressions, conversions and result types
                    (BoundExpression expressionNoConversion, Conversion conversion) = RemoveConversion(expression, includeExplicitConversions: false);
                    expressionsNoConversions.Add(expressionNoConversion);
                    conversions.Add(conversion);
                    SnapshotWalkerThroughConversionGroup(expression, expressionNoConversion);
                    var resultType = VisitRvalueWithState(expressionNoConversion);
                    resultTypes.Add(resultType);
                    placeholderBuilder.Add(CreatePlaceholderIfNecessary(expressionNoConversion, resultType.ToTypeWithAnnotations()));
                }

                var placeholders = placeholderBuilder.ToImmutableAndFree();

                TypeSymbol bestType = null;
                if (!node.HasErrors)
                {
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    bestType = BestTypeInferrer.InferBestType(placeholders, _conversions, ref useSiteDiagnostics);
                }

                TypeWithAnnotations inferredType = (bestType is null)
                    ? elementType.SetUnknownNullabilityForReferenceTypes()
                    : TypeWithAnnotations.Create(bestType);

                if ((object)bestType != null)
                {
                    // Convert elements to best type to determine element top-level nullability and to report nested nullability warnings
                    for (int i = 0; i < n; i++)
                    {
                        var expressionNoConversion = expressionsNoConversions[i];
                        var expression = GetConversionIfApplicable(expressions[i], expressionNoConversion);
                        resultTypes[i] = VisitConversion(expression, expressionNoConversion, conversions[i], inferredType, resultTypes[i], checkConversion: true,
                            fromExplicitCast: false, useLegacyWarnings: false, AssignmentKind.Assignment, reportRemainingWarnings: true, reportTopLevelWarnings: false);
                    }

                    // Set top-level nullability on inferred element type
                    var elementState = BestTypeInferrer.GetNullableState(resultTypes);
                    inferredType = TypeWithState.Create(inferredType.Type, elementState).ToTypeWithAnnotations();

                    for (int i = 0; i < n; i++)
                    {
                        // Report top-level warnings
                        _ = VisitConversion(conversionOpt: null, conversionOperand: expressionsNoConversions[i], Conversion.Identity, targetTypeWithNullability: inferredType, operandType: resultTypes[i],
                            checkConversion: true, fromExplicitCast: false, useLegacyWarnings: false, AssignmentKind.Assignment, reportRemainingWarnings: false);
                    }
                }
                else
                {
                    // We need to ensure that we're tracking the inferred type with nullability of any conversions that
                    // were stripped off.
                    for (int i = 0; i < n; i++)
                    {
                        TrackAnalyzedNullabilityThroughConversionGroup(inferredType.ToTypeWithState(), expressions[i] as BoundConversion, expressionsNoConversions[i]);
                    }
                }

                expressionsNoConversions.Free();
                conversions.Free();
                resultTypes.Free();

                arrayType = arrayType.WithElementType(inferredType);
            }

            expressions.Free();
            SetInvalidResult();
            return arrayType;
        }

        /// <summary>
        /// Applies a method similar to <see cref="VisitArrayInitializer(BoundArrayCreation)"/>
        /// The expressions returned from a lambda are not converted though, so we'll have to classify fresh conversions.
        /// Note: even if some conversions fail, we'll proceed to infer top-level nullability. That is reasonable in common cases.
        /// </summary>
        internal static TypeWithAnnotations BestTypeForLambdaReturns(
            ArrayBuilder<(BoundExpression, TypeWithAnnotations)> returns,
            CSharpCompilation compilation,
            BoundNode node,
            Conversions conversions)
        {
            var walker = new NullableWalker(compilation,
                                            symbol: null,
                                            useMethodSignatureParameterTypes: false,
                                            methodSignatureOpt: null,
                                            node,
                                            binder: null,
                                            conversions: conversions,
                                            initialState: null,
                                            returnTypesOpt: null,
                                            analyzedNullabilityMapOpt: null,
                                            updatedSymbolMapOpt: null,
                                            snapshotBuilderOpt: null);

            int n = returns.Count;
            var resultTypes = ArrayBuilder<TypeWithAnnotations>.GetInstance(n);
            var placeholdersBuilder = ArrayBuilder<BoundExpression>.GetInstance(n);
            for (int i = 0; i < n; i++)
            {
                var (returnExpr, resultType) = returns[i];
                resultTypes.Add(resultType);
                placeholdersBuilder.Add(CreatePlaceholderIfNecessary(returnExpr, resultType));
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var placeholders = placeholdersBuilder.ToImmutableAndFree();
            TypeSymbol bestType = BestTypeInferrer.InferBestType(placeholders, walker._conversions, ref useSiteDiagnostics);

            TypeWithAnnotations inferredType;
            if ((object)bestType != null)
            {
                // Note: so long as we have a best type, we can proceed.
                var bestTypeWithObliviousAnnotation = TypeWithAnnotations.Create(bestType);
                ConversionsBase conversionsWithoutNullability = walker._conversions.WithNullability(false);
                for (int i = 0; i < n; i++)
                {
                    BoundExpression placeholder = placeholders[i];
                    Conversion conversion = conversionsWithoutNullability.ClassifyConversionFromExpression(placeholder, bestType, ref useSiteDiagnostics);
                    resultTypes[i] = walker.VisitConversion(conversionOpt: null, placeholder, conversion, bestTypeWithObliviousAnnotation, resultTypes[i].ToTypeWithState(),
                        checkConversion: false, fromExplicitCast: false, useLegacyWarnings: false, AssignmentKind.Return,
                        reportRemainingWarnings: false, reportTopLevelWarnings: false).ToTypeWithAnnotations();
                }

                // Set top-level nullability on inferred type
                inferredType = TypeWithAnnotations.Create(bestType, BestTypeInferrer.GetNullableAnnotation(resultTypes));
            }
            else
            {
                inferredType = default;
            }

            resultTypes.Free();
            walker.Free();

            return inferredType;
        }

        private static void GetArrayElements(BoundArrayInitialization node, ArrayBuilder<BoundExpression> builder)
        {
            foreach (var child in node.Initializers)
            {
                if (child.Kind == BoundKind.ArrayInitialization)
                {
                    GetArrayElements((BoundArrayInitialization)child, builder);
                }
                else
                {
                    builder.Add(child);
                }
            }
        }

        public override BoundNode VisitArrayAccess(BoundArrayAccess node)
        {
            Debug.Assert(!IsConditionalState);

            Visit(node.Expression);

            Debug.Assert(!IsConditionalState);
            Debug.Assert(!node.Expression.Type.IsValueType);
            // https://github.com/dotnet/roslyn/issues/30598: Mark receiver as not null
            // after indices have been visited, and only if the receiver has not changed.
            _ = CheckPossibleNullReceiver(node.Expression);

            var type = ResultType.Type as ArrayTypeSymbol;

            foreach (var i in node.Indices)
            {
                VisitRvalue(i);
            }

            TypeWithAnnotations result;
            if (node.Indices.Length == 1 &&
                TypeSymbol.Equals(node.Indices[0].Type, compilation.GetWellKnownType(WellKnownType.System_Range), TypeCompareKind.ConsiderEverything2))
            {
                result = TypeWithAnnotations.Create(type);
            }
            else
            {
                result = type?.ElementTypeWithAnnotations ?? default;
            }
            SetLvalueResultType(node, result);

            return null;
        }

        private TypeWithState InferResultNullability(BinaryOperatorKind operatorKind, MethodSymbol methodOpt, TypeSymbol resultType, TypeWithState leftType, TypeWithState rightType)
        {
            NullableFlowState resultState = NullableFlowState.NotNull;
            if (operatorKind.IsUserDefined())
            {
                if ((object)methodOpt != null && methodOpt.ParameterCount == 2)
                {
                    return operatorKind.IsLifted() && !operatorKind.IsComparison()
                        ? GetLiftedReturnType(methodOpt.ReturnTypeWithAnnotations, leftType.State.Join(rightType.State))
                        : GetReturnTypeWithState(methodOpt);
                }
            }
            else if (!operatorKind.IsDynamic() && !resultType.IsValueType)
            {
                switch (operatorKind.Operator() | operatorKind.OperandTypes())
                {
                    case BinaryOperatorKind.DelegateCombination:
                        resultState = leftType.State.Meet(rightType.State);
                        break;
                    case BinaryOperatorKind.DelegateRemoval:
                        resultState = NullableFlowState.MaybeNull; // Delegate removal can produce null.
                        break;
                    default:
                        resultState = NullableFlowState.NotNull;
                        break;
                }
            }

            if (operatorKind.IsLifted() && !operatorKind.IsComparison())
            {
                resultState = leftType.State.Join(rightType.State);
            }

            return TypeWithState.Create(resultType, resultState);
        }

        protected override void VisitBinaryOperatorChildren(ArrayBuilder<BoundBinaryOperator> stack)
        {
            var binary = stack.Pop();

            var (leftOperand, leftConversion) = RemoveConversion(binary.Left, includeExplicitConversions: false);
            VisitRvalue(leftOperand);

            while (true)
            {
                AfterLeftChildHasBeenVisited(leftOperand, leftConversion, binary);

                if (stack.Count == 0)
                {
                    break;
                }

                Unsplit(); // VisitRvalue does this
                leftOperand = binary;
                leftConversion = Conversion.Identity;
                binary = stack.Pop();
            }
        }

        private void AfterLeftChildHasBeenVisited(BoundExpression leftOperand, Conversion leftConversion, BoundBinaryOperator binary)
        {
            Debug.Assert(!IsConditionalState);
            TypeWithState leftType = ResultType;

            var (rightOperand, rightConversion) = RemoveConversion(binary.Right, includeExplicitConversions: false);
            var rightType = VisitRvalueWithState(rightOperand);

            Debug.Assert(!IsConditionalState);
            // At this point, State.Reachable may be false for
            // invalid code such as `s + throw new Exception()`.

            var method = binary.MethodOpt;

            if (binary.OperatorKind.IsUserDefined() &&
                method?.ParameterCount == 2)
            {
                // Update method based on inferred operand type.
                TypeSymbol methodContainer = method.ContainingType;
                bool isLifted = binary.OperatorKind.IsLifted();
                TypeWithState leftUnderlyingType = GetNullableUnderlyingTypeIfNecessary(isLifted, leftType);
                TypeWithState rightUnderlyingType = GetNullableUnderlyingTypeIfNecessary(isLifted, rightType);
                TypeSymbol asMemberOfType = getTypeIfIsOrDerivedFrom(methodContainer, leftUnderlyingType.Type.StrippedType()) ??
                    getTypeIfIsOrDerivedFrom(methodContainer, rightUnderlyingType.Type.StrippedType());
                if (!(asMemberOfType is null))
                {
                    method = (MethodSymbol)AsMemberOfType(asMemberOfType, method);
                }

                // Analyze operator call properly (honoring [Disallow|Allow|Maybe|NotNull] attribute annotations) https://github.com/dotnet/roslyn/issues/32671
                var parameters = method.Parameters;
                visitOperandConversion(binary.Left, leftOperand, leftConversion, parameters[0], leftUnderlyingType);
                visitOperandConversion(binary.Right, rightOperand, rightConversion, parameters[1], rightUnderlyingType);

                void visitOperandConversion(
                    BoundExpression expr,
                    BoundExpression operand,
                    Conversion conversion,
                    ParameterSymbol parameter,
                    TypeWithState operandType)
                {
                    _ = VisitConversion(
                        expr as BoundConversion,
                        operand,
                        conversion,
                        parameter.TypeWithAnnotations,
                        operandType,
                        checkConversion: true,
                        fromExplicitCast: false,
                        useLegacyWarnings: false,
                        AssignmentKind.Argument,
                        parameter);
                }
            }
            else
            {
                // Assume this is a built-in operator in which case the parameter types are unannotated.
                visitOperandConversion(binary.Left, leftOperand, leftConversion, leftType);
                visitOperandConversion(binary.Right, rightOperand, rightConversion, rightType);

                void visitOperandConversion(
                    BoundExpression expr,
                    BoundExpression operand,
                    Conversion conversion,
                    TypeWithState operandType)
                {
                    if (expr.Type is null)
                    {
                        Debug.Assert(operand == expr);
                    }
                    else
                    {
                        _ = VisitConversion(
                            expr as BoundConversion,
                            operand,
                            conversion,
                            TypeWithAnnotations.Create(expr.Type),
                            operandType,
                            checkConversion: true,
                            fromExplicitCast: false,
                            useLegacyWarnings: false,
                            AssignmentKind.Argument);
                    }
                }
            }

            Debug.Assert(!IsConditionalState);
            // For nested binary operators, this can be the only time they're visited due to explicit stack used in AbstractFlowPass.VisitBinaryOperator,
            // so we need to set the flow-analyzed type here.
            var inferredResult = InferResultNullability(binary.OperatorKind, method, binary.Type, leftType, rightType);
            SetResult(binary, inferredResult, inferredResult.ToTypeWithAnnotations());

            BinaryOperatorKind op = binary.OperatorKind.Operator();

            // learn from null constant
            if (op == BinaryOperatorKind.Equal || op == BinaryOperatorKind.NotEqual)
            {
                BoundExpression operandComparedToNull = null;

                if (binary.Right.ConstantValue?.IsNull == true)
                {
                    operandComparedToNull = binary.Left;
                }
                else if (binary.Left.ConstantValue?.IsNull == true)
                {
                    operandComparedToNull = binary.Right;
                }

                if (operandComparedToNull != null)
                {
                    // Set all nested conditional slots. For example in a?.b?.c we'll set a, b, and c.
                    bool nonNullCase = op != BinaryOperatorKind.Equal; // true represents WhenTrue
                    splitAndLearnFromNonNullTest(operandComparedToNull, whenTrue: nonNullCase);

                    // `x == null` and `x != null` are pure null tests so update the null-state in the alternative branch too
                    LearnFromNullTest(operandComparedToNull, ref nonNullCase ? ref StateWhenFalse : ref StateWhenTrue);
                    return;
                }
            }

            // learn from comparison between non-null and maybe-null, possibly updating maybe-null to non-null
            BoundExpression operandComparedToNonNull = null;
            if (leftType.IsNotNull && rightType.MayBeNull)
            {
                operandComparedToNonNull = binary.Right;
            }
            else if (rightType.IsNotNull && leftType.MayBeNull)
            {
                operandComparedToNonNull = binary.Left;
            }

            if (operandComparedToNonNull != null)
            {
                switch (op)
                {
                    case BinaryOperatorKind.Equal:
                    case BinaryOperatorKind.GreaterThan:
                    case BinaryOperatorKind.LessThan:
                    case BinaryOperatorKind.GreaterThanOrEqual:
                    case BinaryOperatorKind.LessThanOrEqual:
                        operandComparedToNonNull = SkipReferenceConversions(operandComparedToNonNull);
                        splitAndLearnFromNonNullTest(operandComparedToNonNull, whenTrue: true);
                        return;
                    case BinaryOperatorKind.NotEqual:
                        operandComparedToNonNull = SkipReferenceConversions(operandComparedToNonNull);
                        splitAndLearnFromNonNullTest(operandComparedToNonNull, whenTrue: false);
                        return;
                };
            }

            void splitAndLearnFromNonNullTest(BoundExpression operandComparedToNonNull, bool whenTrue)
            {
                var slotBuilder = ArrayBuilder<int>.GetInstance();
                GetSlotsToMarkAsNotNullable(operandComparedToNonNull, slotBuilder);
                if (slotBuilder.Count != 0)
                {
                    Split();
                    ref LocalState stateToUpdate = ref whenTrue ? ref this.StateWhenTrue : ref this.StateWhenFalse;
                    MarkSlotsAsNotNull(slotBuilder, ref stateToUpdate);
                }
                slotBuilder.Free();
            }

            TypeSymbol getTypeIfIsOrDerivedFrom(TypeSymbol baseType, TypeSymbol derivedType)
            {
                if (derivedType is null)
                {
                    return null;
                }
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                var conversion = _conversions.ClassifyBuiltInConversion(derivedType, baseType, ref useSiteDiagnostics);
                if (conversion.Exists && !conversion.IsExplicit)
                {
                    return derivedType;
                }
                return null;
            }
        }

        /// <summary>
        /// If we learn that the operand is non-null, we can infer that certain
        /// sub-expressions were also non-null.
        /// Get all nested conditional slots for those sub-expressions. For example in a?.b?.c we'll set a, b, and c.
        /// Only returns slots for tracked expressions.
        /// </summary>
        private void GetSlotsToMarkAsNotNullable(BoundExpression operand, ArrayBuilder<int> slotBuilder)
        {
            Debug.Assert(operand != null);
            var previousConditionalAccessSlot = _lastConditionalAccessSlot;

            try
            {
                while (true)
                {
                    // Due to the nature of binding, if there are conditional access they will be at the top of the bound tree,
                    // potentially with a conversion on top of it. We go through any conditional accesses, adding slots for the
                    // conditional receivers if they have them. If we ever get to a receiver that MakeSlot doesn't return a slot
                    // for, nothing underneath is trackable and we bail at that point. Example:
                    //
                    //     a?.GetB()?.C // a is a field, GetB is a method, and C is a property
                    //
                    // The top of the tree is the a?.GetB() conditional call. We'll ask for a slot for a, and we'll get one because
                    // fields have slots. The AccessExpression of the BoundConditionalAccess is another BoundConditionalAccess, this time
                    // with a receiver of the GetB() BoundCall. Attempting to get a slot for this receiver will fail, and we'll
                    // return an array with just the slot for a.
                    int slot;
                    switch (operand.Kind)
                    {
                        case BoundKind.Conversion:
                            // https://github.com/dotnet/roslyn/issues/33879 Detect when conversion has a nullable operand
                            operand = ((BoundConversion)operand).Operand;
                            continue;
                        case BoundKind.ConditionalAccess:
                            var conditional = (BoundConditionalAccess)operand;

                            slot = MakeSlot(conditional.Receiver);
                            if (slot > 0)
                            {
                                // We need to continue the walk regardless of whether the receiver should be updated.
                                var receiverType = conditional.Receiver.Type;
                                if (PossiblyNullableType(receiverType))
                                {
                                    slotBuilder.Add(slot);
                                }

                                if (receiverType.IsNullableType())
                                {
                                    slot = GetNullableOfTValueSlot(receiverType, slot, out _);
                                }
                            }

                            if (slot > 0)
                            {
                                // When MakeSlot is called on the nested AccessExpression, it will recurse through receivers
                                // until it gets to the BoundConditionalReceiver associated with this node. In our override,
                                // we substitute this slot when we encounter a BoundConditionalReceiver, and reset the
                                // _lastConditionalAccess field.
                                _lastConditionalAccessSlot = slot;
                                operand = conditional.AccessExpression;
                                continue;
                            }

                            // If there's no slot for this receiver, there cannot be another slot for any of the remaining
                            // access expressions.
                            break;
                        default:
                            // Attempt to create a slot for the current thing. If there were any more conditional accesses,
                            // they would have been on top, so this is the last thing we need to specially handle.

                            // https://github.com/dotnet/roslyn/issues/33879 When we handle unconditional access survival (ie after
                            // c.D has been invoked, c must be nonnull or we've thrown a NullRef), revisit whether
                            // we need more special handling here

                            slot = MakeSlot(operand);
                            if (slot > 0 && PossiblyNullableType(operand.Type))
                            {
                                slotBuilder.Add(slot);
                            }

                            break;
                    }

                    return;
                }
            }
            finally
            {
                _lastConditionalAccessSlot = previousConditionalAccessSlot;
            }
        }

        private static bool PossiblyNullableType(TypeSymbol operandType) => operandType?.CanContainNull() == true;

        private static void MarkSlotsAsNotNull(ArrayBuilder<int> slots, ref LocalState stateToUpdate)
        {
            foreach (int slot in slots)
            {
                stateToUpdate[slot] = NullableFlowState.NotNull;
            }
        }

        private void LearnFromNonNullTest(BoundExpression expression, ref LocalState state)
        {
            var slotBuilder = ArrayBuilder<int>.GetInstance();
            GetSlotsToMarkAsNotNullable(expression, slotBuilder);
            MarkSlotsAsNotNull(slotBuilder, ref state);
            slotBuilder.Free();
        }

        private void LearnFromNonNullTest(int slot, ref LocalState state)
        {
            state[slot] = NullableFlowState.NotNull;
        }

        private int LearnFromNullTest(BoundExpression expression, ref LocalState state)
        {
            // nothing to learn about a constant
            if (expression.ConstantValue != null)
            {
                return -1;
            }

            // We should not blindly strip conversions here. Tracked by https://github.com/dotnet/roslyn/issues/36164
            var expressionWithoutConversion = RemoveConversion(expression, includeExplicitConversions: true).expression;
            var slot = MakeSlot(expressionWithoutConversion);
            return LearnFromNullTest(slot, expressionWithoutConversion.Type, ref state);
        }

        private int LearnFromNullTest(int slot, TypeSymbol expressionType, ref LocalState state)
        {
            if (slot > 0 && PossiblyNullableType(expressionType))
            {
                state[slot] = NullableFlowState.MaybeNull;
            }

            return slot;
        }

        private static BoundExpression SkipReferenceConversions(BoundExpression possiblyConversion)
        {
            while (possiblyConversion.Kind == BoundKind.Conversion)
            {
                var conversion = (BoundConversion)possiblyConversion;
                switch (conversion.ConversionKind)
                {
                    case ConversionKind.ImplicitReference:
                    case ConversionKind.ExplicitReference:
                        possiblyConversion = conversion.Operand;
                        break;
                    default:
                        return possiblyConversion;
                }
            }

            return possiblyConversion;
        }

        public override BoundNode VisitNullCoalescingAssignmentOperator(BoundNullCoalescingAssignmentOperator node)
        {
            BoundExpression leftOperand = node.LeftOperand;
            BoundExpression rightOperand = node.RightOperand;
            int leftSlot = MakeSlot(leftOperand);

            TypeWithAnnotations targetType = VisitLvalueWithAnnotations(leftOperand);
            var leftState = this.State.Clone();
            LearnFromNonNullTest(leftOperand, ref leftState);
            LearnFromNullTest(leftOperand, ref this.State);
            if (node.IsNullableValueTypeAssignment)
            {
                targetType = TypeWithAnnotations.Create(node.Type, NullableAnnotation.NotAnnotated);
            }
            TypeWithState rightResult = VisitOptionalImplicitConversion(rightOperand, targetType, useLegacyWarnings: UseLegacyWarnings(leftOperand, targetType), trackMembers: false, AssignmentKind.Assignment);
            TrackNullableStateForAssignment(rightOperand, targetType, leftSlot, rightResult, MakeSlot(rightOperand));
            Join(ref this.State, ref leftState);
            TypeWithState resultType = GetNullCoalescingResultType(rightResult, targetType.Type);
            SetResultType(node, resultType);
            return null;
        }

        public override BoundNode VisitNullCoalescingOperator(BoundNullCoalescingOperator node)
        {
            Debug.Assert(!IsConditionalState);

            BoundExpression leftOperand = node.LeftOperand;
            BoundExpression rightOperand = node.RightOperand;

            TypeWithState leftResult = VisitRvalueWithState(leftOperand);
            TypeWithState rightResult;

            if (IsConstantNull(leftOperand))
            {
                rightResult = VisitRvalueWithState(rightOperand);
                // Should be able to use rightResult for the result of the operator but
                // binding may have generated a different result type in the case of errors.
                SetResultType(node, TypeWithState.Create(node.Type, rightResult.State));
                return null;
            }

            var whenNotNull = this.State.Clone();
            LearnFromNonNullTest(leftOperand, ref whenNotNull);
            LearnFromNullTest(leftOperand, ref this.State);

            bool leftIsConstant = leftOperand.ConstantValue != null;
            if (leftIsConstant)
            {
                SetUnreachable();
            }

            // https://github.com/dotnet/roslyn/issues/29955 For cases where the left operand determines
            // the type, we should unwrap the right conversion and re-apply.
            rightResult = VisitRvalueWithState(rightOperand);

            Join(ref this.State, ref whenNotNull);

            if (rightOperand.ConstantValue?.IsBoolean ?? false)
            {
                Split();
                if (rightOperand.ConstantValue.BooleanValue)
                {
                    StateWhenFalse = whenNotNull;
                }
                else
                {
                    StateWhenTrue = whenNotNull;
                }
            }

            var leftResultType = leftResult.Type;
            var rightResultType = rightResult.Type;

            var resultType = node.OperatorResultKind switch
            {
                BoundNullCoalescingOperatorResultKind.NoCommonType => node.Type,
                BoundNullCoalescingOperatorResultKind.LeftType => getLeftResultType(leftResultType, rightResultType),
                BoundNullCoalescingOperatorResultKind.LeftUnwrappedType => getLeftResultType(leftResultType.StrippedType(), rightResultType),
                BoundNullCoalescingOperatorResultKind.RightType => getRightResultType(leftResultType, rightResultType),
                BoundNullCoalescingOperatorResultKind.LeftUnwrappedRightType => getRightResultType(leftResultType.StrippedType(), rightResultType),
                BoundNullCoalescingOperatorResultKind.RightDynamicType => rightResultType,
                _ => throw ExceptionUtilities.UnexpectedValue(node.OperatorResultKind),
            };

            SetResultType(node, GetNullCoalescingResultType(rightResult, resultType));
            return null;

            TypeSymbol getLeftResultType(TypeSymbol leftType, TypeSymbol rightType)
            {
                Debug.Assert(rightType is object);
                // If there was an identity conversion between the two operands (in short, if there
                // is no implicit conversion on the right operand), then check nullable conversions
                // in both directions since it's possible the right operand is the better result type.
                if ((node.RightOperand as BoundConversion)?.ExplicitCastInCode != false &&
                    GenerateConversionForConditionalOperator(node.LeftOperand, leftType, rightType, reportMismatch: false).Exists)
                {
                    return rightType;
                }

                GenerateConversionForConditionalOperator(node.RightOperand, rightType, leftType, reportMismatch: true);
                return leftType;
            }

            TypeSymbol getRightResultType(TypeSymbol leftType, TypeSymbol rightType)
            {
                GenerateConversionForConditionalOperator(node.LeftOperand, leftType, rightType, reportMismatch: true);
                return rightType;
            }
        }

        private static TypeWithState GetNullCoalescingResultType(TypeWithState rightResult, TypeSymbol resultType)
        {
            NullableFlowState resultState = rightResult.State;
            return TypeWithState.Create(resultType, resultState);
        }

        public override BoundNode VisitConditionalAccess(BoundConditionalAccess node)
        {
            Debug.Assert(!IsConditionalState);

            var receiver = node.Receiver;
            var receiverType = VisitRvalueWithState(receiver);
            _currentConditionalReceiverVisitResult = _visitResult;
            var previousConditionalAccessSlot = _lastConditionalAccessSlot;

            var receiverState = this.State.Clone();
            if (IsConstantNull(node.Receiver))
            {
                SetUnreachable();
                _lastConditionalAccessSlot = -1;
            }
            else
            {
                // In the when-null branch, the receiver is known to be maybe-null.
                // In the other branch, the receiver is known to be non-null.
                _lastConditionalAccessSlot = LearnFromNullTest(receiver, ref receiverState);
                LearnFromNonNullTest(receiver, ref this.State);
            }

            var accessTypeWithAnnotations = VisitLvalueWithAnnotations(node.AccessExpression);
            TypeSymbol accessType = accessTypeWithAnnotations.Type;
            Join(ref this.State, ref receiverState);

            var oldType = node.Type;
            var resultType =
                oldType.IsVoidType() || oldType.IsErrorType() ? oldType :
                oldType.IsNullableType() && !accessType.IsNullableType() ? MakeNullableOf(accessTypeWithAnnotations) :
                accessType;

            // If the result type does not allow annotations, then we produce a warning because
            // the result may be null.
            if (RequiresSafetyWarningWhenNullIntroduced(resultType))
            {
                ReportDiagnostic(ErrorCode.WRN_ConditionalAccessMayReturnNull, node.Syntax, accessType);
            }

            // Per LDM 2019-02-13 decision, the result of a conditional access "may be null" even if
            // both the receiver and right-hand-side are believed not to be null.
            SetResultType(node, TypeWithState.Create(resultType, NullableFlowState.MaybeNull));
            _currentConditionalReceiverVisitResult = default;
            _lastConditionalAccessSlot = previousConditionalAccessSlot;
            return null;
        }

        public override BoundNode VisitConditionalOperator(BoundConditionalOperator node)
        {
            VisitCondition(node.Condition);
            var consequenceState = this.StateWhenTrue;
            var alternativeState = this.StateWhenFalse;

            TypeWithState consequenceRValue;
            TypeWithState alternativeRValue;

            if (node.IsRef)
            {
                TypeWithAnnotations consequenceLValue;
                TypeWithAnnotations alternativeLValue;

                (consequenceLValue, consequenceRValue) = visitConditionalRefOperand(consequenceState, node.Consequence);
                consequenceState = this.State;
                (alternativeLValue, alternativeRValue) = visitConditionalRefOperand(alternativeState, node.Alternative);
                Join(ref this.State, ref consequenceState);

                TypeSymbol refResultType = node.Type.SetUnknownNullabilityForReferenceTypes();
                if (IsNullabilityMismatch(consequenceLValue, alternativeLValue))
                {
                    // l-value types must match
                    ReportNullabilityMismatchInAssignment(node.Syntax, consequenceLValue, alternativeLValue);
                }
                else if (!node.HasErrors)
                {
                    refResultType = consequenceRValue.Type.MergeNullability(alternativeRValue.Type, VarianceKind.None);
                }

                var lValueAnnotation = consequenceLValue.NullableAnnotation.EnsureCompatible(alternativeLValue.NullableAnnotation);
                var rValueState = consequenceRValue.State.Join(alternativeRValue.State);

                SetResult(node, TypeWithState.Create(refResultType, rValueState), TypeWithAnnotations.Create(refResultType, lValueAnnotation));
                return null;
            }

            BoundExpression consequence;
            BoundExpression alternative;
            Conversion consequenceConversion;
            Conversion alternativeConversion;
            bool consequenceEndReachable;
            bool alternativeEndReachable;

            // In cases where one branch is unreachable, we don't need to Unsplit the state
            if (!alternativeState.Reachable)
            {
                (alternative, alternativeConversion, alternativeRValue) = visitConditionalOperand(alternativeState, node.Alternative);
                (consequence, consequenceConversion, consequenceRValue) = visitConditionalOperand(consequenceState, node.Consequence);
                alternativeEndReachable = false;
                consequenceEndReachable = IsReachable();
            }
            else if (!consequenceState.Reachable)
            {
                (consequence, consequenceConversion, consequenceRValue) = visitConditionalOperand(consequenceState, node.Consequence);
                (alternative, alternativeConversion, alternativeRValue) = visitConditionalOperand(alternativeState, node.Alternative);
                consequenceEndReachable = false;
                alternativeEndReachable = IsReachable();
            }
            else
            {
                (consequence, consequenceConversion, consequenceRValue) = visitConditionalOperand(consequenceState, node.Consequence);
                Unsplit();
                consequenceState = this.State;
                consequenceEndReachable = consequenceState.Reachable;

                (alternative, alternativeConversion, alternativeRValue) = visitConditionalOperand(alternativeState, node.Alternative);
                Unsplit();
                alternativeEndReachable = this.State.Reachable;
                Join(ref this.State, ref consequenceState);
            }

            TypeSymbol resultType;
            if (node.HasErrors)
            {
                resultType = null;
            }
            else
            {
                // Determine nested nullability using BestTypeInferrer.
                // If a branch is unreachable, we could use the nested nullability of the other
                // branch, but that requires using the nullability of the branch as it applies to the
                // target type. For instance, the result of the conditional in the following should
                // be `IEnumerable<object>` not `object[]`:
                //   object[] a = ...;
                //   IEnumerable<object?> b = ...;
                //   var c = true ? a : b;
                BoundExpression consequencePlaceholder = CreatePlaceholderIfNecessary(consequence, consequenceRValue.ToTypeWithAnnotations());
                BoundExpression alternativePlaceholder = CreatePlaceholderIfNecessary(alternative, alternativeRValue.ToTypeWithAnnotations());
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                resultType = BestTypeInferrer.InferBestTypeForConditionalOperator(consequencePlaceholder, alternativePlaceholder, _conversions, out _, ref useSiteDiagnostics);
            }

            NullableFlowState resultState;
            if (resultType is null)
            {
                resultType = node.Type.SetUnknownNullabilityForReferenceTypes();
                resultState = NullableFlowState.NotNull;

                var resultTypeWithState = TypeWithState.Create(resultType, resultState);

                if (consequence != node.Consequence)
                {
                    TrackAnalyzedNullabilityThroughConversionGroup(resultTypeWithState, (BoundConversion)node.Consequence, consequence);
                }

                if (alternative != node.Alternative)
                {
                    TrackAnalyzedNullabilityThroughConversionGroup(resultTypeWithState, (BoundConversion)node.Alternative, alternative);
                }
            }
            else
            {
                var resultTypeWithAnnotations = TypeWithAnnotations.Create(resultType);

                TypeWithState convertedConsequenceResult = convertResult(
                    node.Consequence,
                    consequence,
                    consequenceConversion,
                    resultTypeWithAnnotations,
                    consequenceRValue,
                    consequenceEndReachable);

                TypeWithState convertedAlternativeResult = convertResult(
                    node.Alternative,
                    alternative,
                    alternativeConversion,
                    resultTypeWithAnnotations,
                    alternativeRValue,
                    alternativeEndReachable);

                resultState = convertedConsequenceResult.State.Join(convertedAlternativeResult.State);
            }

            SetResultType(node, TypeWithState.Create(resultType, resultState));
            return null;

            (BoundExpression, Conversion, TypeWithState) visitConditionalOperand(LocalState state, BoundExpression operand)
            {
                Conversion conversion;
                SetState(state);
                Debug.Assert(!node.IsRef);

                BoundExpression operandNoConversion;
                (operandNoConversion, conversion) = RemoveConversion(operand, includeExplicitConversions: false);
                SnapshotWalkerThroughConversionGroup(operand, operandNoConversion);
                Visit(operandNoConversion);
                return (operandNoConversion, conversion, ResultType);
            }

            (TypeWithAnnotations LValueType, TypeWithState RValueType) visitConditionalRefOperand(LocalState state, BoundExpression operand)
            {
                SetState(state);
                Debug.Assert(node.IsRef);
                TypeWithAnnotations lValueType = VisitLvalueWithAnnotations(operand);
                return (lValueType, ResultType);
            }

            TypeWithState convertResult(
                BoundExpression node,
                BoundExpression operand,
                Conversion conversion,
                TypeWithAnnotations targetType,
                TypeWithState operandType,
                bool isReachable)
            {
                bool previousDisabledDiagnostics = _disableDiagnostics;
                // If the node is not reachable, then we're only visiting to get
                // nullability information for the public API, and not to produce diagnostics.
                // Disable diagnostics, and return default for the resulting state
                // to indicate that warnings were suppressed.
                if (!isReachable)
                {
                    _disableDiagnostics = true;
                }

                var resultType = VisitConversion(
                    GetConversionIfApplicable(node, operand),
                    operand,
                    conversion,
                    targetType,
                    operandType,
                    checkConversion: true,
                    fromExplicitCast: false,
                    useLegacyWarnings: false,
                    AssignmentKind.Assignment,
                    reportTopLevelWarnings: false);

                if (!isReachable)
                {
                    resultType = default;
                    _disableDiagnostics = previousDisabledDiagnostics;
                }

                return resultType;
            }
        }

        bool IsReachable()
            => this.IsConditionalState ? (this.StateWhenTrue.Reachable || this.StateWhenFalse.Reachable) : this.State.Reachable;

        /// <summary>
        /// Placeholders are bound expressions with type and state.
        /// But for typeless expressions (such as `null` or `(null, null)` we hold onto the original bound expression,
        /// as it will be useful for conversions from expression.
        /// </summary>
        private static BoundExpression CreatePlaceholderIfNecessary(BoundExpression expr, TypeWithAnnotations type)
        {
            return !type.HasType ?
                expr :
                new BoundExpressionWithNullability(expr.Syntax, expr, type.NullableAnnotation, type.Type);
        }

        public override BoundNode VisitConditionalReceiver(BoundConditionalReceiver node)
        {
            var rvalueType = _currentConditionalReceiverVisitResult.RValueType.Type;
            if (rvalueType?.IsNullableType() == true)
            {
                rvalueType = rvalueType.GetNullableUnderlyingType();
            }
            SetResultType(node, TypeWithState.Create(rvalueType, NullableFlowState.NotNull));
            return null;
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            // Note: we analyze even omitted calls
            TypeWithState receiverType = VisitCallReceiver(node);
            ReinferMethodAndVisitArguments(node, receiverType);
            return null;
        }

        private void ReinferMethodAndVisitArguments(BoundCall node, TypeWithState receiverType)
        {
            var method = node.Method;
            ImmutableArray<RefKind> refKindsOpt = node.ArgumentRefKindsOpt;
            if (!receiverType.HasNullType)
            {
                // Update method based on inferred receiver type.
                method = (MethodSymbol)AsMemberOfType(receiverType.Type, method);
            }

            ImmutableArray<VisitArgumentResult> results;
            bool returnNotNull;
            (method, results, returnNotNull) = VisitArguments(node, node.Arguments, refKindsOpt, method.Parameters, node.ArgsToParamsOpt,
                node.Expanded, node.InvokedAsExtensionMethod, method);

            LearnFromEqualsMethod(method, node, receiverType, results);

            LearnFromCompareExchangeMethod(method, node, results);

            if (method.MethodKind == MethodKind.LocalFunction)
            {
                var localFunc = (LocalFunctionSymbol)method.OriginalDefinition;
                ReplayReadsAndWrites(localFunc, node.Syntax, writes: true);
            }

            var returnState = GetReturnTypeWithState(method);
            if (returnNotNull)
            {
                returnState = returnState.WithNotNullState();
            }

            SetResult(node, returnState, method.ReturnTypeWithAnnotations);
            SetUpdatedSymbol(node, node.Method, method);
        }

        private void LearnFromEqualsMethod(MethodSymbol method, BoundCall node, TypeWithState receiverType, ImmutableArray<VisitArgumentResult> results)
        {
            // easy out
            var parameterCount = method.ParameterCount;
            var arguments = node.Arguments;
            if ((parameterCount != 1 && parameterCount != 2)
                || parameterCount != arguments.Length
                || method.MethodKind != MethodKind.Ordinary
                || method.ReturnType.SpecialType != SpecialType.System_Boolean
                || (method.Name != SpecialMembers.GetDescriptor(SpecialMember.System_Object__Equals).Name
                    && method.Name != SpecialMembers.GetDescriptor(SpecialMember.System_Object__ReferenceEquals).Name))
            {
                return;
            }


            var isStaticEqualsMethod = method.Equals(compilation.GetSpecialTypeMember(SpecialMember.System_Object__EqualsObjectObject))
                    || method.Equals(compilation.GetSpecialTypeMember(SpecialMember.System_Object__ReferenceEquals));
            if (isStaticEqualsMethod ||
                isWellKnownEqualityMethodOrImplementation(compilation, method, WellKnownMember.System_Collections_Generic_IEqualityComparer_T__Equals))
            {
                Debug.Assert(arguments.Length == 2);
                learnFromEqualsMethodArguments(arguments[0], results[0].RValueType, arguments[1], results[1].RValueType);
                return;
            }

            var isObjectEqualsMethodOrOverride = method.GetLeastOverriddenMethod(accessingTypeOpt: null)
                .Equals(compilation.GetSpecialTypeMember(SpecialMember.System_Object__Equals));
            if (isObjectEqualsMethodOrOverride ||
                isWellKnownEqualityMethodOrImplementation(compilation, method, WellKnownMember.System_IEquatable_T__Equals))
            {
                Debug.Assert(arguments.Length == 1);
                learnFromEqualsMethodArguments(node.ReceiverOpt, receiverType, arguments[0], results[0].RValueType);
                return;
            }

            static bool isWellKnownEqualityMethodOrImplementation(CSharpCompilation compilation, MethodSymbol method, WellKnownMember wellKnownMember)
            {
                var wellKnownMethod = compilation.GetWellKnownTypeMember(wellKnownMember);
                if (wellKnownMethod is null)
                {
                    return false;
                }

                var wellKnownType = wellKnownMethod.ContainingType;
                var parameterType = method.Parameters[0].TypeWithAnnotations;
                var constructedType = wellKnownType.Construct(ImmutableArray.Create(parameterType));

                Symbol constructedMethod = null;
                foreach (var member in constructedType.GetMembers(WellKnownMemberNames.ObjectEquals))
                {
                    if (member.OriginalDefinition.Equals(wellKnownMethod))
                    {
                        constructedMethod = member;
                        break;
                    }
                }

                Debug.Assert(constructedMethod != null, "the original definition is present but the constructed method isn't present");

                // FindImplementationForInterfaceMember doesn't check if this method is itself the interface method we're looking for
                if (constructedMethod.Equals(method))
                {
                    return true;
                }

                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                var implementationMethod = method.ContainingType.FindImplementationForInterfaceMember(constructedMethod, ref useSiteDiagnostics);
                return method.Equals(implementationMethod);
            }

            void learnFromEqualsMethodArguments(BoundExpression left, TypeWithState leftType, BoundExpression right, TypeWithState rightType)
            {
                // comparing anything to a null literal gives maybe-null when true and not-null when false
                // comparing a maybe-null to a not-null gives us not-null when true, nothing learned when false
                if (left.ConstantValue?.IsNull == true)
                {
                    Split();
                    LearnFromNullTest(right, ref StateWhenTrue);
                    LearnFromNonNullTest(right, ref StateWhenFalse);
                }
                else if (right.ConstantValue?.IsNull == true)
                {
                    Split();
                    LearnFromNullTest(left, ref StateWhenTrue);
                    LearnFromNonNullTest(left, ref StateWhenFalse);
                }
                else if (leftType.MayBeNull && rightType.IsNotNull)
                {
                    Split();
                    LearnFromNonNullTest(left, ref StateWhenTrue);
                }
                else if (rightType.MayBeNull && leftType.IsNotNull)
                {
                    Split();
                    LearnFromNonNullTest(right, ref StateWhenTrue);
                }
            }
        }

        private void LearnFromCompareExchangeMethod(MethodSymbol method, BoundCall node, ImmutableArray<VisitArgumentResult> results)
        {
            var isCompareExchangeMethod = method.Equals(compilation.GetWellKnownTypeMember(WellKnownMember.System_Threading_Interlocked__CompareExchange), SymbolEqualityComparer.ConsiderEverything.CompareKind)
                || method.OriginalDefinition.Equals(compilation.GetWellKnownTypeMember(WellKnownMember.System_Threading_Interlocked__CompareExchange_T), SymbolEqualityComparer.ConsiderEverything.CompareKind);
            if (!isCompareExchangeMethod)
            {
                return;
            }

            var arguments = node.Arguments;
            if (arguments.Length != method.ParameterCount)
            {
                return;
            }

            // In general a call to CompareExchange of the form:
            //
            // Interlocked.CompareExchange(ref location, value, comparand);
            //
            // will be analyzed similarly to the following:
            //
            // if (location == comparand)
            // {
            //     location = value;
            // }

            var locationSlot = MakeSlot(arguments[0]);
            if (locationSlot != -1)
            {
                var comparand = arguments[2];
                var valueFlowState = results[1].RValueType.State;
                if (comparand.ConstantValue?.IsNull == true)
                {
                    // If location contained a null, then the write `location = value` definitely occurred
                    State[locationSlot] = valueFlowState;
                }
                else
                {
                    var locationFlowState = results[0].RValueType.State;
                    // A write may have occurred
                    State[locationSlot] = valueFlowState.Join(locationFlowState);
                }
            }
        }

        private TypeWithState VisitCallReceiver(BoundCall node)
        {
            var receiverOpt = node.ReceiverOpt;
            TypeWithState receiverType = default;

            if (receiverOpt != null)
            {
                receiverType = VisitRvalueWithState(receiverOpt);

                // methods which are members of Nullable<T> (ex: ToString, GetHashCode) can be invoked on null receiver.
                // However, inherited methods (ex: GetType) are invoked on a boxed value (since base types are reference types)
                // and therefore in those cases nullable receivers should be checked for nullness.
                bool checkNullableValueType = false;

                var type = receiverType.Type;
                var method = node.Method;
                if (method.RequiresInstanceReceiver &&
                    type?.IsNullableType() == true &&
                    method.ContainingType.IsReferenceType)
                {
                    checkNullableValueType = true;
                }
                else if (method.OriginalDefinition == compilation.GetSpecialTypeMember(SpecialMember.System_Nullable_T_get_Value))
                {
                    // call to get_Value may not occur directly in source, but may be inserted as a result of premature lowering.
                    // One example where we do it is foreach with nullables.
                    // The reason is Dev10 compatibility (see: UnwrapCollectionExpressionIfNullable in ForEachLoopBinder.cs)
                    // Regardless of the reasons, we know that the method does not tolerate nulls.
                    checkNullableValueType = true;
                }

                // https://github.com/dotnet/roslyn/issues/30598: Mark receiver as not null
                // after arguments have been visited, and only if the receiver has not changed.
                _ = CheckPossibleNullReceiver(receiverOpt, checkNullableValueType);
            }

            return receiverType;
        }

        private TypeWithState GetReturnTypeWithState(MethodSymbol method)
        {
            return TypeWithState.Create(method.ReturnTypeWithAnnotations, GetRValueAnnotations(method));
        }

        private FlowAnalysisAnnotations GetRValueAnnotations(Symbol symbol)
        {
            // Annotations are ignored when binding an attribute to avoid cycles. (Members used
            // in attributes are error scenarios, so missing warnings should not be important.)
            if (IsAnalyzingAttribute)
            {
                return FlowAnalysisAnnotations.None;
            }

            var annotations = symbol switch
            {
                MethodSymbol method => method.ReturnTypeFlowAnalysisAnnotations,
                PropertySymbol property => property.GetOwnOrInheritedGetMethod()?.ReturnTypeFlowAnalysisAnnotations ?? FlowAnalysisAnnotations.None,
                ParameterSymbol parameter => parameter.FlowAnalysisAnnotations,
                FieldSymbol field => field.FlowAnalysisAnnotations,
                _ => FlowAnalysisAnnotations.None
            };

            return annotations & (FlowAnalysisAnnotations.MaybeNull | FlowAnalysisAnnotations.NotNull);
        }

        private FlowAnalysisAnnotations GetParameterAnnotations(ParameterSymbol parameter)
        {
            // Annotations are ignored when binding an attribute to avoid cycles. (Members used
            // in attributes are error scenarios, so missing warnings should not be important.)
            return IsAnalyzingAttribute ? FlowAnalysisAnnotations.None : parameter.FlowAnalysisAnnotations;
        }

        /// <summary>
        /// Fix a TypeWithAnnotations based on Maybe/NotNull annotations prior to a conversion or assignment.
        /// Note this does not work for nullable value types, so an additional check with <see cref="CheckDisallowedNullAssignment"/> may be required.
        /// </summary>
        private static TypeWithAnnotations ApplyLValueAnnotations(TypeWithAnnotations declaredType, FlowAnalysisAnnotations flowAnalysisAnnotations)
        {
            if ((flowAnalysisAnnotations & FlowAnalysisAnnotations.DisallowNull) == FlowAnalysisAnnotations.DisallowNull)
            {
                return declaredType.AsNotAnnotated();
            }
            else if ((flowAnalysisAnnotations & FlowAnalysisAnnotations.AllowNull) == FlowAnalysisAnnotations.AllowNull)
            {
                return declaredType.AsAnnotated();
            }

            return declaredType;
        }

        /// <summary>
        /// Update the null-state based on MaybeNull/NotNull
        /// </summary>
        private static TypeWithState ApplyUnconditionalAnnotations(TypeWithState typeWithState, FlowAnalysisAnnotations annotations)
        {
            if ((annotations & FlowAnalysisAnnotations.MaybeNull) == FlowAnalysisAnnotations.MaybeNull)
            {
                return TypeWithState.Create(typeWithState.Type, NullableFlowState.MaybeNull);
            }

            if ((annotations & FlowAnalysisAnnotations.NotNull) == FlowAnalysisAnnotations.NotNull)
            {
                return TypeWithState.Create(typeWithState.Type, NullableFlowState.NotNull);
            }

            return typeWithState;
        }

        // https://github.com/dotnet/roslyn/issues/29863 Record in the node whether type
        // arguments were implicit, to allow for cases where the syntax is not an
        // invocation (such as a synthesized call from a query interpretation).
        private static bool HasImplicitTypeArguments(BoundExpression node)
        {
            if (node is BoundCollectionElementInitializer { AddMethod: { TypeArgumentsWithAnnotations: { IsEmpty: false } } })
            {
                return true;
            }

            var syntax = node.Syntax;
            if (syntax.Kind() != SyntaxKind.InvocationExpression)
            {
                // Unexpected syntax kind.
                return false;
            }
            return HasImplicitTypeArguments(((InvocationExpressionSyntax)syntax).Expression);
        }

        private static bool HasImplicitTypeArguments(SyntaxNode syntax)
        {
            var nameSyntax = Binder.GetNameSyntax(syntax, out _);
            if (nameSyntax == null)
            {
                // Unexpected syntax kind.
                return false;
            }
            nameSyntax = nameSyntax.GetUnqualifiedName();
            return nameSyntax.Kind() != SyntaxKind.GenericName;
        }

        protected override void VisitArguments(ImmutableArray<BoundExpression> arguments, ImmutableArray<RefKind> refKindsOpt, MethodSymbol method)
        {
            // Callers should be using VisitArguments overload below.
            throw ExceptionUtilities.Unreachable;
        }

        private (MethodSymbol method, ImmutableArray<VisitArgumentResult> results, bool returnNotNull) VisitArguments(
            BoundExpression node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            MethodSymbol method,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded,
            bool invokedAsExtensionMethod)
        {
            return VisitArguments(node, arguments, refKindsOpt, method is null ? default : method.Parameters, argsToParamsOpt, expanded, invokedAsExtensionMethod, method);
        }

        private ImmutableArray<VisitArgumentResult> VisitArguments(
            BoundExpression node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            PropertySymbol property,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded)
        {
            return VisitArguments(node, arguments, refKindsOpt, property is null ? default : property.Parameters, argsToParamsOpt, expanded, invokedAsExtensionMethod: false).results;
        }

        /// <summary>
        /// If you pass in a method symbol, its type arguments will be re-inferred and the re-inferred method will be returned.
        /// </summary>
        private (MethodSymbol method, ImmutableArray<VisitArgumentResult> results, bool returnNotNull) VisitArguments(
            BoundExpression node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded,
            bool invokedAsExtensionMethod,
            MethodSymbol method = null)
        {
            Debug.Assert(!arguments.IsDefault);
            bool shouldReturnNotNull = false;

            (ImmutableArray<BoundExpression> argumentsNoConversions, ImmutableArray<Conversion> conversions) = RemoveArgumentConversions(arguments, refKindsOpt);

            // Visit the arguments and collect results
            ImmutableArray<VisitArgumentResult> results = VisitArgumentsEvaluate(argumentsNoConversions, refKindsOpt, parameters, argsToParamsOpt, expanded);

            // Re-infer method type parameters
            if ((object)method != null && method.IsGenericMethod)
            {
                if (HasImplicitTypeArguments(node))
                {
                    method = InferMethodTypeArguments(node, method, GetArgumentsForMethodTypeInference(argumentsNoConversions, results));
                    parameters = method.Parameters;
                }
                if (ConstraintsHelper.RequiresChecking(method))
                {
                    var syntax = node.Syntax;
                    CheckMethodConstraints((syntax as InvocationExpressionSyntax)?.Expression ?? syntax, method);
                }
            }

            bool parameterHasNotNullIfNotNull = !IsAnalyzingAttribute && !parameters.IsDefault && parameters.Any(p => !p.NotNullIfParameterNotNull.IsEmpty);
            var notNullParametersBuilder = parameterHasNotNullIfNotNull ? ArrayBuilder<ParameterSymbol>.GetInstance() : null;
            if (!node.HasErrors && !parameters.IsDefault)
            {
                // Visit conversions, inbound assignments including pre-conditions
                ImmutableHashSet<string> returnNotNullIfParameterNotNull = IsAnalyzingAttribute ? null : method?.ReturnNotNullIfParameterNotNull;
                for (int i = 0; i < argumentsNoConversions.Length; i++)
                {
                    (ParameterSymbol parameter, TypeWithAnnotations parameterType, FlowAnalysisAnnotations parameterAnnotations, bool isExpandedParamsArgument) =
                        GetCorrespondingParameter(i, parameters, argsToParamsOpt, expanded);
                    if (parameter is null)
                    {
                        continue;
                    }

                    var argumentNoConversion = argumentsNoConversions[i];
                    VisitArgumentConversionAndInboundAssignmentsAndPreConditions(
                        GetConversionIfApplicable(arguments[i], argumentNoConversion),
                        argumentNoConversion,
                        conversions.IsDefault ? Conversion.Identity : conversions[i],
                        GetRefKind(refKindsOpt, i),
                        parameter,
                        parameterType,
                        parameterAnnotations,
                        results[i],
                        invokedAsExtensionMethod && i == 0);

                    if (results[i].RValueType.IsNotNull || isExpandedParamsArgument)
                    {
                        notNullParametersBuilder?.Add(parameter);

                        if (returnNotNullIfParameterNotNull?.Contains(parameter.Name) == true)
                        {
                            shouldReturnNotNull = true;
                        }
                    }
                }
            }

            if (!node.HasErrors && !parameters.IsDefault)
            {
                // Visit outbound assignments and post-conditions
                // Note: the state may get split in this step
                for (int i = 0; i < argumentsNoConversions.Length; i++)
                {
                    (ParameterSymbol parameter, TypeWithAnnotations parameterType, FlowAnalysisAnnotations parameterAnnotations, _) = GetCorrespondingParameter(i, parameters, argsToParamsOpt, expanded);
                    if (parameter is null)
                    {
                        continue;
                    }

                    VisitArgumentOutboundAssignmentsAndPostConditions(
                        arguments[i],
                        GetRefKind(refKindsOpt, i),
                        parameter,
                        parameterType,
                        parameterAnnotations,
                        results[i],
                        notNullParametersBuilder);
                }
            }
            else
            {
                for (int i = 0; i < arguments.Length; i++)
                {
                    // We can hit this case when dynamic methods are involved, or when there are errors. In either case we have no information,
                    // so just assume that the conversions have the same nullability as the underlying result
                    var argument = arguments[i];
                    var result = results[i];
                    var argumentNoConversion = argumentsNoConversions[i];
                    TrackAnalyzedNullabilityThroughConversionGroup(TypeWithState.Create(argument.Type, result.RValueType.State), argument as BoundConversion, argumentNoConversion);
                }
            }

            if (method is object && (method.FlowAnalysisAnnotations & FlowAnalysisAnnotations.DoesNotReturn) == FlowAnalysisAnnotations.DoesNotReturn)
            {
                SetUnreachable();
            }

            notNullParametersBuilder?.Free();
            return (method, results, shouldReturnNotNull);
        }

        private ImmutableArray<VisitArgumentResult> VisitArgumentsEvaluate(ImmutableArray<BoundExpression> arguments, ImmutableArray<RefKind> refKindsOpt, ImmutableArray<ParameterSymbol> parameters, ImmutableArray<int> argsToParamsOpt, bool expanded)
        {
            Debug.Assert(!IsConditionalState);
            int n = arguments.Length;
            if (n == 0)
            {
                return ImmutableArray<VisitArgumentResult>.Empty;
            }
            var builder = ArrayBuilder<VisitArgumentResult>.GetInstance(n);
            for (int i = 0; i < n; i++)
            {
                FlowAnalysisAnnotations parameterAnnotations = GetCorrespondingParameter(i, parameters, argsToParamsOpt, expanded).Annotations;
                builder.Add(VisitArgumentEvaluate(arguments[i], GetRefKind(refKindsOpt, i), parameterAnnotations));
            }

            SetInvalidResult();
            return builder.ToImmutableAndFree();
        }

        private VisitArgumentResult VisitArgumentEvaluate(BoundExpression argument, RefKind refKind, FlowAnalysisAnnotations annotations)
        {
            Debug.Assert(!IsConditionalState);
            var savedState = (argument.Kind == BoundKind.Lambda) ? this.State.Clone() : default(Optional<LocalState>);
            // Note: DoesNotReturnIf is ineffective on ref/out parameters

            switch (refKind)
            {
                case RefKind.Ref:
                    Visit(argument);
                    Unsplit();
                    break;
                case RefKind.None:
                case RefKind.In:
                    switch (annotations & (FlowAnalysisAnnotations.DoesNotReturnIfTrue | FlowAnalysisAnnotations.DoesNotReturnIfFalse))
                    {
                        case FlowAnalysisAnnotations.DoesNotReturnIfTrue:
                            Visit(argument);
                            if (IsConditionalState)
                            {
                                SetState(StateWhenFalse);
                            }
                            break;

                        case FlowAnalysisAnnotations.DoesNotReturnIfFalse:
                            Visit(argument);
                            if (IsConditionalState)
                            {
                                SetState(StateWhenTrue);
                            }
                            break;

                        default:
                            VisitRvalue(argument);
                            break;
                    }
                    break;
                case RefKind.Out:
                    // As far as we can tell, there is no scenario relevant to nullability analysis
                    // where splitting an L-value (for instance with a ref conditional) would affect the result.
                    Visit(argument);

                    // We'll want to use the l-value type, rather than the result type, for method re-inference
                    UseLvalueOnly(argument);
                    break;
            }

            Debug.Assert(!IsConditionalState);
            return new VisitArgumentResult(_visitResult, savedState);
        }

        /// <summary>
        /// Verifies that an argument's nullability is compatible with its parameter's on the way in.
        /// </summary>
        private void VisitArgumentConversionAndInboundAssignmentsAndPreConditions(
            BoundConversion conversionOpt,
            BoundExpression argumentNoConversion,
            Conversion conversion,
            RefKind refKind,
            ParameterSymbol parameter,
            TypeWithAnnotations parameterType,
            FlowAnalysisAnnotations parameterAnnotations,
            VisitArgumentResult result,
            bool extensionMethodThisArgument)
        {
            Debug.Assert(!this.IsConditionalState);
            // Note: we allow for some variance in `in` and `out` cases. Unlike in binding, we're not
            // limited by CLR constraints.

            var resultType = result.RValueType;
            switch (refKind)
            {
                case RefKind.None:
                case RefKind.In:
                    {
                        // Note: for lambda arguments, they will be converted in the context/state we saved for that argument
                        var stateAfterConversion = VisitConversion(
                            conversionOpt: conversionOpt,
                            conversionOperand: argumentNoConversion,
                            conversion: conversion,
                            targetTypeWithNullability: ApplyLValueAnnotations(parameterType, parameterAnnotations),
                            operandType: resultType,
                            checkConversion: true,
                            fromExplicitCast: false,
                            useLegacyWarnings: false,
                            assignmentKind: AssignmentKind.Argument,
                            parameterOpt: parameter,
                            extensionMethodThisArgument: extensionMethodThisArgument,
                            stateForLambda: result.StateForLambda);

                        // If the parameter has annotations, we perform an additional check for nullable value types
                        CheckDisallowedNullAssignment(stateAfterConversion, parameterAnnotations, argumentNoConversion.Syntax.Location);
                        SetResultType(argumentNoConversion, stateAfterConversion, updateAnalyzedNullability: false);
                    }
                    break;
                case RefKind.Ref:
                    if (!argumentNoConversion.IsSuppressed)
                    {
                        var lvalueResultType = result.LValueType;
                        if (IsNullabilityMismatch(lvalueResultType.Type, parameterType.Type))
                        {
                            // declared types must match, ignoring top-level nullability
                            ReportNullabilityMismatchInRefArgument(argumentNoConversion, argumentType: lvalueResultType.Type, parameter, parameterType.Type);
                        }
                        else
                        {
                            // types match, but state would let a null in
                            ReportNullableAssignmentIfNecessary(argumentNoConversion, ApplyLValueAnnotations(parameterType, parameterAnnotations), resultType, useLegacyWarnings: false);
                            // If the parameter has annotations, we perform an additional check for nullable value types
                            CheckDisallowedNullAssignment(resultType, parameterAnnotations, argumentNoConversion.Syntax.Location);
                        }
                    }

                    break;
                case RefKind.Out:
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(refKind);
            }

            Debug.Assert(!this.IsConditionalState);
        }

        private void CheckDisallowedNullAssignment(TypeWithState state, FlowAnalysisAnnotations annotations, Location location)
        {
            if (((annotations & FlowAnalysisAnnotations.DisallowNull) != 0) && state.Type.IsNullableTypeOrTypeParameter() && state.MayBeNull)
            {
                ReportDiagnostic(ErrorCode.WRN_DisallowNullAttributeForbidsMaybeNullAssignment, location);
            }
        }

        /// <summary>
        /// Verifies that outbound assignments (from parameter to argument) are safe and
        /// tracks those assignments (or learns from post-condition attributes)
        /// </summary>
        private void VisitArgumentOutboundAssignmentsAndPostConditions(
            BoundExpression argument,
            RefKind refKind,
            ParameterSymbol parameter,
            TypeWithAnnotations parameterType,
            FlowAnalysisAnnotations parameterAnnotations,
            VisitArgumentResult result,
            ArrayBuilder<ParameterSymbol> notNullParametersOpt)
        {
            // Note: the state may be conditional if a previous argument involved a conditional post-condition
            // The WhenTrue/False states correspond to the invocation returning true/false

            switch (refKind)
            {
                case RefKind.None:
                case RefKind.In:
                    {
                        // learn from post-conditions [Maybe/NotNull, Maybe/NotNullWhen] without using an assignment
                        learnFromPostConditions(argument, parameterAnnotations);
                    }
                    break;
                case RefKind.Ref:
                    {
                        // assign from a fictional value from the parameter to the argument.
                        parameterAnnotations = notNullBasedOnParameters(parameterAnnotations, notNullParametersOpt, parameter);
                        var parameterWithState = TypeWithState.Create(parameterType, parameterAnnotations);
                        var parameterValue = new BoundParameter(argument.Syntax, parameter);
                        var lValueType = result.LValueType;
                        trackNullableStateForAssignment(parameterValue, lValueType, MakeSlot(argument), parameterWithState, argument.IsSuppressed, parameterAnnotations);

                        // check whether parameter would unsafely let a null out in the worse case
                        if (!argument.IsSuppressed)
                        {
                            ReportNullableAssignmentIfNecessary(parameterValue, lValueType, applyPostConditionsUnconditionally(parameterWithState, parameterAnnotations), UseLegacyWarnings(argument, result.LValueType));
                        }
                    }
                    break;
                case RefKind.Out:
                    {
                        // compute the fictional parameter state
                        parameterAnnotations = notNullBasedOnParameters(parameterAnnotations, notNullParametersOpt, parameter);
                        var parameterWithState = TypeWithState.Create(parameterType, parameterAnnotations);

                        // Adjust parameter state if MaybeNull or MaybeNullWhen are present (for `var` type and for assignment warnings)
                        var worstCaseParameterWithState = applyPostConditionsUnconditionally(parameterWithState, parameterAnnotations);

                        var lValueType = result.LValueType;
                        if (argument is BoundLocal local && local.DeclarationKind == BoundLocalDeclarationKind.WithInferredType)
                        {
                            var varType = worstCaseParameterWithState.ToTypeWithAnnotations();
                            _variableTypes[local.LocalSymbol] = varType;
                            lValueType = varType;
                        }
                        else if (argument is BoundDiscardExpression discard)
                        {
                            SetAnalyzedNullability(discard, new VisitResult(parameterWithState), isLvalue: true);
                        }

                        // track state by assigning from a fictional value from the parameter to the argument.
                        var parameterValue = new BoundParameter(argument.Syntax, parameter);
                        trackNullableStateForAssignment(parameterValue, lValueType, MakeSlot(argument), parameterWithState, argument.IsSuppressed, parameterAnnotations);

                        // report warnings if parameter would unsafely let a null out in the worst case
                        if (!argument.IsSuppressed)
                        {
                            ReportNullableAssignmentIfNecessary(parameterValue, lValueType, worstCaseParameterWithState, UseLegacyWarnings(argument, result.LValueType));

                            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                            if (!_conversions.HasIdentityOrImplicitReferenceConversion(parameterType.Type, lValueType.Type, ref useSiteDiagnostics))
                            {
                                ReportNullabilityMismatchInArgument(argument.Syntax, lValueType.Type, parameter, parameterType.Type, forOutput: true);
                            }
                        }
                    }
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(refKind);
            }

            FlowAnalysisAnnotations notNullBasedOnParameters(FlowAnalysisAnnotations parameterAnnotations, ArrayBuilder<ParameterSymbol> notNullParametersOpt, ParameterSymbol parameter)
            {
                if (!IsAnalyzingAttribute && notNullParametersOpt is object)
                {
                    var notNullIfParameterNotNull = parameter.NotNullIfParameterNotNull;
                    if (!notNullIfParameterNotNull.IsEmpty)
                    {
                        foreach (var notNullParameter in notNullParametersOpt)
                        {
                            if (notNullIfParameterNotNull.Contains(notNullParameter.Name))
                            {
                                return FlowAnalysisAnnotations.NotNull;
                            }
                        }
                    }
                }
                return parameterAnnotations;
            }

            void trackNullableStateForAssignment(BoundExpression parameterValue, TypeWithAnnotations lValueType, int targetSlot, TypeWithState parameterWithState, bool isSuppressed, FlowAnalysisAnnotations parameterAnnotations)
            {
                if (!IsConditionalState && !hasConditionalPostCondition(parameterAnnotations))
                {
                    TrackNullableStateForAssignment(parameterValue, lValueType, targetSlot, parameterWithState.WithSuppression(isSuppressed));
                }
                else
                {
                    Split();
                    var originalWhenFalse = StateWhenFalse.Clone();

                    SetState(StateWhenTrue);
                    // Note: the suppression applies over the post-condition attributes
                    TrackNullableStateForAssignment(parameterValue, lValueType, targetSlot, applyPostConditionsWhenTrue(parameterWithState, parameterAnnotations).WithSuppression(isSuppressed));
                    Debug.Assert(!IsConditionalState);
                    var newWhenTrue = State.Clone();

                    SetState(originalWhenFalse);
                    TrackNullableStateForAssignment(parameterValue, lValueType, targetSlot, applyPostConditionsWhenFalse(parameterWithState, parameterAnnotations).WithSuppression(isSuppressed));
                    Debug.Assert(!IsConditionalState);

                    SetConditionalState(newWhenTrue, whenFalse: State);
                }
            }

            static bool hasConditionalPostCondition(FlowAnalysisAnnotations annotations)
            {
                return (((annotations & FlowAnalysisAnnotations.MaybeNullWhenTrue) != 0) ^ ((annotations & FlowAnalysisAnnotations.MaybeNullWhenFalse) != 0)) ||
                    (((annotations & FlowAnalysisAnnotations.NotNullWhenTrue) != 0) ^ ((annotations & FlowAnalysisAnnotations.NotNullWhenFalse) != 0));
            }

            static TypeWithState applyPostConditionsUnconditionally(TypeWithState typeWithState, FlowAnalysisAnnotations annotations)
            {
                if ((annotations & FlowAnalysisAnnotations.MaybeNull) != 0)
                {
                    // MaybeNull and MaybeNullWhen
                    return TypeWithState.Create(typeWithState.Type, NullableFlowState.MaybeNull);
                }

                if ((annotations & FlowAnalysisAnnotations.NotNull) == FlowAnalysisAnnotations.NotNull)
                {
                    // NotNull
                    return TypeWithState.Create(typeWithState.Type, NullableFlowState.NotNull);
                }

                return typeWithState;
            }

            static TypeWithState applyPostConditionsWhenTrue(TypeWithState typeWithState, FlowAnalysisAnnotations annotations)
            {
                bool notNullWhenTrue = (annotations & FlowAnalysisAnnotations.NotNullWhenTrue) != 0;
                bool maybeNullWhenTrue = (annotations & FlowAnalysisAnnotations.MaybeNullWhenTrue) != 0;
                bool maybeNullWhenFalse = (annotations & FlowAnalysisAnnotations.MaybeNullWhenFalse) != 0;

                if (maybeNullWhenTrue && !(maybeNullWhenFalse && notNullWhenTrue))
                {
                    // [MaybeNull, NotNullWhen(true)] means [MaybeNullWhen(false)]
                    return TypeWithState.Create(typeWithState.Type, NullableFlowState.MaybeNull);
                }
                else if (notNullWhenTrue)
                {
                    return TypeWithState.Create(typeWithState.Type, NullableFlowState.NotNull);
                }

                return typeWithState;
            }

            static TypeWithState applyPostConditionsWhenFalse(TypeWithState typeWithState, FlowAnalysisAnnotations annotations)
            {
                bool notNullWhenFalse = (annotations & FlowAnalysisAnnotations.NotNullWhenFalse) != 0;
                bool maybeNullWhenTrue = (annotations & FlowAnalysisAnnotations.MaybeNullWhenTrue) != 0;
                bool maybeNullWhenFalse = (annotations & FlowAnalysisAnnotations.MaybeNullWhenFalse) != 0;

                if (maybeNullWhenFalse && !(maybeNullWhenTrue && notNullWhenFalse))
                {
                    // [MaybeNull, NotNullWhen(false)] means [MaybeNullWhen(true)]
                    return TypeWithState.Create(typeWithState.Type, NullableFlowState.MaybeNull);
                }
                else if (notNullWhenFalse)
                {
                    return TypeWithState.Create(typeWithState.Type, NullableFlowState.NotNull);
                }

                return typeWithState;
            }

            void learnFromPostConditions(BoundExpression argument, FlowAnalysisAnnotations parameterAnnotations)
            {
                // Note: NotNull = NotNullWhen(true) + NotNullWhen(false)
                bool notNullWhenTrue = (parameterAnnotations & FlowAnalysisAnnotations.NotNullWhenTrue) != 0;
                bool notNullWhenFalse = (parameterAnnotations & FlowAnalysisAnnotations.NotNullWhenFalse) != 0;

                // Note: MaybeNull = MaybeNullWhen(true) + MaybeNullWhen(false)
                bool maybeNullWhenTrue = (parameterAnnotations & FlowAnalysisAnnotations.MaybeNullWhenTrue) != 0;
                bool maybeNullWhenFalse = (parameterAnnotations & FlowAnalysisAnnotations.MaybeNullWhenFalse) != 0;

                if (maybeNullWhenTrue && maybeNullWhenFalse && !IsConditionalState && !(notNullWhenTrue && notNullWhenFalse))
                {
                    LearnFromNullTest(argument, ref State);
                }
                else if (notNullWhenTrue && notNullWhenFalse && !IsConditionalState && !(maybeNullWhenTrue && maybeNullWhenFalse))
                {
                    LearnFromNonNullTest(argument, ref State);
                }
                else if (notNullWhenTrue || notNullWhenFalse || maybeNullWhenTrue || maybeNullWhenFalse)
                {
                    Split();

                    if (notNullWhenTrue)
                    {
                        LearnFromNonNullTest(argument, ref StateWhenTrue);
                    }

                    if (notNullWhenFalse)
                    {
                        LearnFromNonNullTest(argument, ref StateWhenFalse);
                    }

                    if (maybeNullWhenTrue)
                    {
                        LearnFromNullTest(argument, ref StateWhenTrue);
                    }

                    if (maybeNullWhenFalse)
                    {
                        LearnFromNullTest(argument, ref StateWhenFalse);
                    }
                }
            }
        }

        private (ImmutableArray<BoundExpression> arguments, ImmutableArray<Conversion> conversions) RemoveArgumentConversions(
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt)
        {
            int n = arguments.Length;
            var conversions = default(ImmutableArray<Conversion>);
            if (n > 0)
            {
                var argumentsBuilder = ArrayBuilder<BoundExpression>.GetInstance(n);
                var conversionsBuilder = ArrayBuilder<Conversion>.GetInstance(n);
                bool includedConversion = false;
                for (int i = 0; i < n; i++)
                {
                    RefKind refKind = GetRefKind(refKindsOpt, i);
                    var argument = arguments[i];
                    var conversion = Conversion.Identity;
                    if (refKind == RefKind.None)
                    {
                        var before = argument;
                        (argument, conversion) = RemoveConversion(argument, includeExplicitConversions: false);
                        if (argument != before)
                        {
                            SnapshotWalkerThroughConversionGroup(before, argument);
                            includedConversion = true;
                        }
                    }
                    argumentsBuilder.Add(argument);
                    conversionsBuilder.Add(conversion);
                }
                if (includedConversion)
                {
                    arguments = argumentsBuilder.ToImmutable();
                    conversions = conversionsBuilder.ToImmutable();
                }
                argumentsBuilder.Free();
                conversionsBuilder.Free();
            }
            return (arguments, conversions);
        }

        private VariableState GetVariableState(Optional<LocalState> localState)
        {
            return new VariableState(
                _variableSlot.ToImmutableDictionary(),
                ImmutableArray.Create(variableBySlot, start: 0, length: nextVariableSlot),
                _variableTypes.ToImmutableDictionary(),
                localState.HasValue ? localState.Value : this.State.Clone());
        }

        private (ParameterSymbol Parameter, TypeWithAnnotations Type, FlowAnalysisAnnotations Annotations, bool isExpandedParamsArgument) GetCorrespondingParameter(
            int argumentOrdinal,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded)
        {
            if (parameters.IsDefault)
            {
                return default;
            }

            int n = parameters.Length;
            ParameterSymbol parameter;

            if (argsToParamsOpt.IsDefault)
            {
                if (argumentOrdinal < n)
                {
                    parameter = parameters[argumentOrdinal];
                }
                else if (expanded)
                {
                    parameter = parameters[n - 1];
                }
                else
                {
                    parameter = null;
                }
            }
            else
            {
                int parameterOrdinal = argsToParamsOpt[argumentOrdinal];

                if (parameterOrdinal < n)
                {
                    parameter = parameters[parameterOrdinal];
                }
                else
                {
                    parameter = null;
                    expanded = false;
                }
            }

            if (parameter is null)
            {
                Debug.Assert(!expanded);
                return default;
            }

            var type = parameter.TypeWithAnnotations;
            if (expanded && parameter.Ordinal == n - 1 && type.IsSZArray())
            {
                type = ((ArrayTypeSymbol)type.Type).ElementTypeWithAnnotations;
                return (parameter, type, FlowAnalysisAnnotations.None, isExpandedParamsArgument: true);
            }

            return (parameter, type, GetParameterAnnotations(parameter), isExpandedParamsArgument: false);
        }

        private MethodSymbol InferMethodTypeArguments(BoundExpression node, MethodSymbol method, ImmutableArray<BoundExpression> arguments)
        {
            return node switch
            {
                BoundCall call => InferMethodTypeArguments(
                    call.BinderOpt,
                    method,
                    arguments,
                    call.ArgumentRefKindsOpt,
                    call.ArgsToParamsOpt,
                    call.Expanded),
                BoundCollectionElementInitializer initializer => InferMethodTypeArguments(
                    initializer.BinderOpt,
                    method,
                    arguments,
                    argumentRefKindsOpt: default,
                    initializer.ArgsToParamsOpt,
                    initializer.Expanded),
                _ => throw ExceptionUtilities.UnexpectedValue(node.Kind)
            };
        }

        private MethodSymbol InferMethodTypeArguments(
            Binder binder,
            MethodSymbol method,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded)
        {
            Debug.Assert(binder != null);
            Debug.Assert(method.IsGenericMethod);

            // https://github.com/dotnet/roslyn/issues/27961 OverloadResolution.IsMemberApplicableInNormalForm and
            // IsMemberApplicableInExpandedForm use the least overridden method. We need to do the same here.
            var definition = method.ConstructedFrom;
            var refKinds = ArrayBuilder<RefKind>.GetInstance();
            if (argumentRefKindsOpt != null)
            {
                refKinds.AddRange(argumentRefKindsOpt);
            }

            // https://github.com/dotnet/roslyn/issues/27961 Do we really need OverloadResolution.GetEffectiveParameterTypes?
            // Aren't we doing roughly the same calculations in GetCorrespondingParameter?
            OverloadResolution.GetEffectiveParameterTypes(
                definition,
                arguments.Length,
                argsToParamsOpt,
                refKinds,
                isMethodGroupConversion: false,
                // https://github.com/dotnet/roslyn/issues/27961 `allowRefOmittedArguments` should be
                // false for constructors and several other cases (see Binder use). Should we
                // capture the original value in the BoundCall?
                allowRefOmittedArguments: true,
                binder: binder,
                expanded: expanded,
                parameterTypes: out ImmutableArray<TypeWithAnnotations> parameterTypes,
                parameterRefKinds: out ImmutableArray<RefKind> parameterRefKinds);
            refKinds.Free();

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var result = MethodTypeInferrer.Infer(
                binder,
                _conversions,
                definition.TypeParameters,
                definition.ContainingType,
                parameterTypes,
                parameterRefKinds,
                arguments,
                ref useSiteDiagnostics,
                new MethodInferenceExtensions(this));

            if (!result.Success)
            {
                return method;
            }

            return definition.Construct(result.InferredTypeArguments);
        }

        private sealed class MethodInferenceExtensions : MethodTypeInferrer.Extensions
        {
            private readonly NullableWalker _walker;

            internal MethodInferenceExtensions(NullableWalker walker)
            {
                _walker = walker;
            }

            internal override TypeWithAnnotations GetTypeWithAnnotations(BoundExpression expr)
            {
                return TypeWithAnnotations.Create(expr.Type, GetNullableAnnotation(expr));
            }

            /// <summary>
            /// Return top-level nullability for the expression. This method should be called on a limited
            /// set of expressions only. It should not be called on expressions tracked by flow analysis
            /// other than <see cref="BoundKind.ExpressionWithNullability"/> which is an expression
            /// specifically created in NullableWalker to represent the flow analysis state.
            /// </summary>
            private static NullableAnnotation GetNullableAnnotation(BoundExpression expr)
            {
                switch (expr.Kind)
                {
                    case BoundKind.DefaultLiteral:
                    case BoundKind.DefaultExpression:
                    case BoundKind.Literal:
                        return (expr.ConstantValue?.IsNull != false) ? NullableAnnotation.NotAnnotated : NullableAnnotation.Annotated;
                    case BoundKind.ExpressionWithNullability:
                        return ((BoundExpressionWithNullability)expr).NullableAnnotation;
                    case BoundKind.MethodGroup:
                    case BoundKind.UnboundLambda:
                        return NullableAnnotation.NotAnnotated;
                    default:
                        Debug.Assert(false); // unexpected value
                        return NullableAnnotation.Oblivious;
                }
            }

            internal override TypeWithAnnotations GetMethodGroupResultType(BoundMethodGroup group, MethodSymbol method)
            {
                if (_walker.TryGetMethodGroupReceiverNullability(group.ReceiverOpt, out TypeWithState receiverType))
                {
                    if (!method.IsStatic)
                    {
                        method = (MethodSymbol)AsMemberOfType(receiverType.Type, method);
                    }
                }
                return method.ReturnTypeWithAnnotations;
            }
        }

        private ImmutableArray<BoundExpression> GetArgumentsForMethodTypeInference(ImmutableArray<BoundExpression> arguments, ImmutableArray<VisitArgumentResult> argumentResults)
        {
            // https://github.com/dotnet/roslyn/issues/27961 MethodTypeInferrer.Infer relies
            // on the BoundExpressions for tuple element types and method groups.
            // By using a generic BoundValuePlaceholder, we're losing inference in those cases.
            // https://github.com/dotnet/roslyn/issues/27961 Inference should be based on
            // unconverted arguments. Consider cases such as `default`, lambdas, tuples.
            int n = arguments.Length;
            var builder = ArrayBuilder<BoundExpression>.GetInstance(n);
            for (int i = 0; i < n; i++)
            {
                var visitArgumentResult = argumentResults[i];
                var lambdaState = visitArgumentResult.StateForLambda;
                var argumentResult = visitArgumentResult.LValueType;
                if (!argumentResult.HasType)
                    argumentResult = visitArgumentResult.RValueType.ToTypeWithAnnotations();
                builder.Add(getArgumentForMethodTypeInference(arguments[i], argumentResult, lambdaState));
            }
            return builder.ToImmutableAndFree();

            BoundExpression getArgumentForMethodTypeInference(BoundExpression argument, TypeWithAnnotations argumentType, Optional<LocalState> lambdaState)
            {
                if (argument.Kind == BoundKind.Lambda)
                {
                    // MethodTypeInferrer must infer nullability for lambdas based on the nullability
                    // from flow analysis rather than the declared nullability. To allow that, we need
                    // to re-bind lambdas in MethodTypeInferrer.
                    return getUnboundLambda((BoundLambda)argument, GetVariableState(lambdaState));
                }
                if (!argumentType.HasType)
                {
                    return argument;
                }
                if (argument is BoundLocal local && local.DeclarationKind == BoundLocalDeclarationKind.WithInferredType)
                {
                    // 'out var' doesn't contribute to inference
                    return new BoundExpressionWithNullability(argument.Syntax, argument, NullableAnnotation.Oblivious, type: null);
                }
                return new BoundExpressionWithNullability(argument.Syntax, argument, argumentType.NullableAnnotation, argumentType.Type);
            }

            UnboundLambda getUnboundLambda(BoundLambda expr, VariableState variableState)
            {
                return expr.UnboundLambda.WithNullableState(expr.UnboundLambda.Data.Binder, variableState);
            }
        }

        private void CheckMethodConstraints(SyntaxNode syntax, MethodSymbol method)
        {
            if (_disableDiagnostics)
            {
                return;
            }

            var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            var nullabilityBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
            ConstraintsHelper.CheckMethodConstraints(
                method,
                _conversions,
                includeNullability: true,
                compilation,
                diagnosticsBuilder,
                nullabilityBuilder,
                ref useSiteDiagnosticsBuilder);
            foreach (var pair in nullabilityBuilder)
            {
                Diagnostics.Add(pair.DiagnosticInfo, syntax.Location);
            }
            useSiteDiagnosticsBuilder?.Free();
            nullabilityBuilder.Free();
            diagnosticsBuilder.Free();
        }

        private void ReplayReadsAndWrites(LocalFunctionSymbol localFunc,
                                  SyntaxNode syntax,
                                  bool writes)
        {
            // https://github.com/dotnet/roslyn/issues/27233 Support field initializers in local functions.
        }

        /// <summary>
        /// Returns the expression without the top-most conversion plus the conversion.
        /// If the expression is not a conversion, returns the original expression plus
        /// the Identity conversion. If `includeExplicitConversions` is true, implicit and
        /// explicit conversions are considered. If `includeExplicitConversions` is false
        /// only implicit conversions are considered and if the expression is an explicit
        /// conversion, the expression is returned as is, with the Identity conversion.
        /// (Currently, the only visit method that passes `includeExplicitConversions: true`
        /// is VisitConversion. All other callers are handling implicit conversions only.)
        /// </summary>
        private static (BoundExpression expression, Conversion conversion) RemoveConversion(BoundExpression expr, bool includeExplicitConversions)
        {
            ConversionGroup group = null;
            while (true)
            {
                if (expr.Kind != BoundKind.Conversion)
                {
                    break;
                }
                var conversion = (BoundConversion)expr;
                if (group != conversion.ConversionGroupOpt && group != null)
                {
                    // E.g.: (C)(B)a
                    break;
                }
                group = conversion.ConversionGroupOpt;
                Debug.Assert(group != null || !conversion.ExplicitCastInCode); // Explicit conversions should include a group.
                if (!includeExplicitConversions && group?.IsExplicitConversion == true)
                {
                    return (expr, Conversion.Identity);
                }
                expr = conversion.Operand;
                if (group == null)
                {
                    // Ungrouped conversion should not be followed by another ungrouped
                    // conversion. Otherwise, the conversions should have been grouped.
                    // https://github.com/dotnet/roslyn/issues/34919 This assertion does not always hold true for
                    // enum initializers
                    //Debug.Assert(expr.Kind != BoundKind.Conversion ||
                    //    ((BoundConversion)expr).ConversionGroupOpt != null ||
                    //    ((BoundConversion)expr).ConversionKind == ConversionKind.NoConversion);
                    return (expr, conversion.Conversion);
                }
            }
            return (expr, group?.Conversion ?? Conversion.Identity);
        }

        // See Binder.BindNullCoalescingOperator for initial binding.
        private Conversion GenerateConversionForConditionalOperator(BoundExpression sourceExpression, TypeSymbol sourceType, TypeSymbol destinationType, bool reportMismatch)
        {
            var conversion = GenerateConversion(_conversions, sourceExpression, sourceType, destinationType, fromExplicitCast: false, extensionMethodThisArgument: false);
            bool canConvertNestedNullability = conversion.Exists;
            if (!canConvertNestedNullability && reportMismatch && !sourceExpression.IsSuppressed)
            {
                ReportNullabilityMismatchInAssignment(sourceExpression.Syntax, GetTypeAsDiagnosticArgument(sourceType), destinationType);
            }
            return conversion;
        }

        private static Conversion GenerateConversion(Conversions conversions, BoundExpression sourceExpression, TypeSymbol sourceType, TypeSymbol destinationType, bool fromExplicitCast, bool extensionMethodThisArgument)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            bool useExpression = UseExpressionForConversion(sourceExpression);
            if (extensionMethodThisArgument)
            {
                return conversions.ClassifyImplicitExtensionMethodThisArgConversion(
                    useExpression ? sourceExpression : null,
                    sourceType,
                    destinationType,
                    ref useSiteDiagnostics);
            }
            return useExpression ?
                (fromExplicitCast ?
                    conversions.ClassifyConversionFromExpression(sourceExpression, destinationType, ref useSiteDiagnostics, forCast: true) :
                    conversions.ClassifyImplicitConversionFromExpression(sourceExpression, destinationType, ref useSiteDiagnostics)) :
                (fromExplicitCast ?
                    conversions.ClassifyConversionFromType(sourceType, destinationType, ref useSiteDiagnostics, forCast: true) :
                    conversions.ClassifyImplicitConversionFromType(sourceType, destinationType, ref useSiteDiagnostics));
        }

        /// <summary>
        /// Returns true if the expression should be used as the source when calculating
        /// a conversion from this expression, rather than using the type (with nullability)
        /// calculated by visiting this expression. Typically, that means expressions that
        /// do not have an explicit type but there are several other cases as well.
        /// (See expressions handled in ClassifyImplicitBuiltInConversionFromExpression.)
        /// </summary>
        private static bool UseExpressionForConversion(BoundExpression value)
        {
            if (value is null)
            {
                return false;
            }
            if (value.Type is null || value.Type.IsDynamic() || value.ConstantValue != null)
            {
                return true;
            }
            switch (value.Kind)
            {
                case BoundKind.InterpolatedString:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Adjust declared type based on inferred nullability at the point of reference.
        /// </summary>
        private TypeWithState GetAdjustedResult(TypeWithAnnotations type, int slot)
        {
            return GetAdjustedResult(type.ToTypeWithState(), slot);
        }

        private TypeWithState GetAdjustedResult(TypeWithState type, int slot)
        {
            if (slot > 0 && slot < this.State.Capacity)
            {
                NullableFlowState state = this.State[slot];
                return TypeWithState.Create(type.Type, state);
            }

            return type;
        }

        /// <summary>
        /// Gets the corresponding member for a symbol from initial binding to match an updated receiver type in NullableWalker.
        /// For instance, this will map from List&lt;string~&gt;.Add(string~) to List&lt;string?&gt;.Add(string?) in the following example:
        /// <example>
        /// string s = null;
        /// var list = new[] { s }.ToList();
        /// list.Add(null);
        /// </example>
        /// </summary>
        private static Symbol AsMemberOfType(TypeSymbol type, Symbol symbol)
        {
            Debug.Assert((object)symbol != null);

            var containingType = type as NamedTypeSymbol;
            if (containingType is null || containingType.IsErrorType() || symbol is ErrorMethodSymbol)
            {
                return symbol;
            }

            if (symbol.Kind == SymbolKind.Method)
            {
                if (((MethodSymbol)symbol).MethodKind == MethodKind.LocalFunction)
                {
                    // https://github.com/dotnet/roslyn/issues/27233 Handle type substitution for local functions.
                    return symbol;
                }
            }
            var symbolDef = symbol.OriginalDefinition;
            var symbolContainer = symbol.ContainingType;
            if (symbolContainer.IsTupleType)
            {
                return AsMemberOfTupleType((TupleTypeSymbol)containingType, symbol);
            }
            if (symbolContainer.IsAnonymousType)
            {
                int? memberIndex = symbol.Kind == SymbolKind.Property ? symbol.MemberIndexOpt : null;
                if (!memberIndex.HasValue)
                {
                    return symbol;
                }
                return AnonymousTypeManager.GetAnonymousTypeProperty(containingType, memberIndex.GetValueOrDefault());
            }
            if (!symbolContainer.IsGenericType)
            {
                Debug.Assert(symbol.ContainingType.IsDefinition);
                return symbol;
            }
            if (!containingType.IsGenericType && !containingType.IsTupleType)
            {
                return symbol;
            }
            if (symbolContainer.IsInterface)
            {
                if (tryAsMemberOfSingleType(containingType, out Symbol result))
                {
                    return result;
                }
                foreach (var @interface in containingType.AllInterfacesNoUseSiteDiagnostics)
                {
                    if (tryAsMemberOfSingleType(@interface, out result))
                    {
                        return result;
                    }
                }
            }
            else
            {
                while (true)
                {
                    if (tryAsMemberOfSingleType(containingType, out Symbol result))
                    {
                        return result;
                    }
                    containingType = containingType.BaseTypeNoUseSiteDiagnostics;
                    if ((object)containingType == null)
                    {
                        break;
                    }
                }
            }
            Debug.Assert(false); // If this assert fails, add an appropriate test.
            return symbol;

            bool tryAsMemberOfSingleType(NamedTypeSymbol singleType, out Symbol result)
            {
                if (!singleType.Equals(symbolContainer, TypeCompareKind.AllIgnoreOptions))
                {
                    result = null;
                    return false;
                }
                result = symbolDef.SymbolAsMember(singleType);
                if (result is MethodSymbol resultMethod && resultMethod.IsGenericMethod)
                {
                    result = resultMethod.Construct(((MethodSymbol)symbol).TypeArgumentsWithAnnotations);
                }
                return true;
            }
        }

        private static Symbol AsMemberOfTupleType(TupleTypeSymbol tupleType, Symbol symbol)
        {
            if (symbol.ContainingType.Equals(tupleType, SymbolEqualityComparer.ConsiderEverything.CompareKind))
            {
                return symbol;
            }
            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                    {
                        var index = ((FieldSymbol)symbol).TupleElementIndex;
                        if (index >= 0)
                        {
                            return tupleType.TupleElements[index];
                        }
                        return tupleType.GetTupleMemberSymbolForUnderlyingMember(((TupleFieldSymbol)symbol).UnderlyingField);
                    }
                case SymbolKind.Property:
                    return tupleType.GetTupleMemberSymbolForUnderlyingMember(((TuplePropertySymbol)symbol).UnderlyingProperty);
                case SymbolKind.Event:
                    return tupleType.GetTupleMemberSymbolForUnderlyingMember(((TupleEventSymbol)symbol).UnderlyingEvent);
                case SymbolKind.Method:
                    return tupleType.GetTupleMemberSymbolForUnderlyingMember(((TupleMethodSymbol)symbol).UnderlyingMethod);
                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        public override BoundNode VisitConversion(BoundConversion node)
        {
            // https://github.com/dotnet/roslyn/issues/35732: Assert VisitConversion is only used for explicit conversions.
            //Debug.Assert(node.ExplicitCastInCode);
            //Debug.Assert(node.ConversionGroupOpt != null);
            //Debug.Assert(node.ConversionGroupOpt.ExplicitType.HasType);

            TypeWithAnnotations explicitType = node.ConversionGroupOpt?.ExplicitType ?? default;
            bool fromExplicitCast = explicitType.HasType;
            TypeWithAnnotations targetType = fromExplicitCast ? explicitType : TypeWithAnnotations.Create(node.Type);
            Debug.Assert(targetType.HasType);

            (BoundExpression operand, Conversion conversion) = RemoveConversion(node, includeExplicitConversions: true);
            SnapshotWalkerThroughConversionGroup(node, operand);
            TypeWithState operandType = VisitRvalueWithState(operand);
            SetResultType(node,
                VisitConversion(
                    node,
                    operand,
                    conversion,
                    targetType,
                    operandType,
                    checkConversion: true,
                    fromExplicitCast: fromExplicitCast,
                    useLegacyWarnings: fromExplicitCast && !RequiresSafetyWarningWhenNullIntroduced(explicitType),
                    AssignmentKind.Assignment,
                    reportTopLevelWarnings: fromExplicitCast,
                    reportRemainingWarnings: true,
                    trackMembers: true));

            return null;
        }

        /// <summary>
        /// Visit an expression. If an explicit target type is provided, the expression is converted
        /// to that type. This method should be called whenever an expression may contain
        /// an implicit conversion, even if that conversion was omitted from the bound tree,
        /// so the conversion can be re-classified with nullability.
        /// </summary>
        private TypeWithState VisitOptionalImplicitConversion(BoundExpression expr, TypeWithAnnotations targetTypeOpt, bool useLegacyWarnings, bool trackMembers, AssignmentKind assignmentKind)
        {
            if (!targetTypeOpt.HasType)
            {
                return VisitRvalueWithState(expr);
            }

            (BoundExpression operand, Conversion conversion) = RemoveConversion(expr, includeExplicitConversions: false);
            SnapshotWalkerThroughConversionGroup(expr, operand);
            var operandType = VisitRvalueWithState(operand);
            // If an explicit conversion was used in place of an implicit conversion, the explicit
            // conversion was created by initial binding after reporting "error CS0266:
            // Cannot implicitly convert type '...' to '...'. An explicit conversion exists ...".
            // Since an error was reported, we don't need to report nested warnings as well.
            bool reportNestedWarnings = !conversion.IsExplicit;
            var resultType = VisitConversion(
                GetConversionIfApplicable(expr, operand),
                operand,
                conversion,
                targetTypeOpt,
                operandType,
                checkConversion: true,
                fromExplicitCast: false,
                useLegacyWarnings: useLegacyWarnings,
                assignmentKind,
                reportTopLevelWarnings: true,
                reportRemainingWarnings: reportNestedWarnings,
                trackMembers: trackMembers);

            return resultType;
        }

        private static bool AreNullableAndUnderlyingTypes(TypeSymbol nullableTypeOpt, TypeSymbol underlyingTypeOpt, out TypeWithAnnotations underlyingTypeWithAnnotations)
        {
            if (nullableTypeOpt?.IsNullableType() == true &&
                underlyingTypeOpt?.IsNullableType() == false)
            {
                var typeArg = nullableTypeOpt.GetNullableUnderlyingTypeWithAnnotations();
                if (typeArg.Type.Equals(underlyingTypeOpt, TypeCompareKind.AllIgnoreOptions))
                {
                    underlyingTypeWithAnnotations = typeArg;
                    return true;
                }
            }
            underlyingTypeWithAnnotations = default;
            return false;
        }

        public override BoundNode VisitTupleLiteral(BoundTupleLiteral node)
        {
            VisitTupleExpression(node);
            return null;
        }

        public override BoundNode VisitConvertedTupleLiteral(BoundConvertedTupleLiteral node)
        {
            // Visit the source tuple so that the semantic model can correctly report nullability for it
            // Disable diagnostics, as we don't want to duplicate any that are produced by visiting the converted literal below
            VisitWithoutDiagnostics(node.SourceTuple);

            VisitTupleExpression(node);
            return null;
        }

        private void VisitTupleExpression(BoundTupleExpression node)
        {
            var arguments = node.Arguments;
            ImmutableArray<TypeWithState> elementTypes = arguments.SelectAsArray((a, w) => w.VisitRvalueWithState(a), this);
            ImmutableArray<TypeWithAnnotations> elementTypesWithAnnotations = elementTypes.SelectAsArray(a => a.ToTypeWithAnnotations());
            var tupleOpt = (TupleTypeSymbol)node.Type;
            if (tupleOpt is null)
            {
                SetResultType(node, TypeWithState.Create(default, NullableFlowState.NotNull));
            }
            else
            {
                int slot = GetOrCreatePlaceholderSlot(node);
                if (slot > 0)
                {
                    this.State[slot] = NullableFlowState.NotNull;
                    TrackNullableStateOfTupleElements(slot, tupleOpt, arguments, elementTypes, useRestField: false);
                }

                tupleOpt = tupleOpt.WithElementTypes(elementTypesWithAnnotations);
                var locations = tupleOpt.TupleElements.SelectAsArray((element, location) => element.Locations.FirstOrDefault() ?? location, node.Syntax.Location);
                if (!_disableDiagnostics)
                {
                    tupleOpt.CheckConstraints(_conversions, includeNullability: true, node.Syntax, locations, compilation, diagnosticsOpt: null, nullabilityDiagnosticsOpt: Diagnostics);
                }

                SetResultType(node, TypeWithState.Create(tupleOpt, NullableFlowState.NotNull));
            }
        }

        /// <summary>
        /// Set the nullability of tuple elements for tuples at the point of construction.
        /// If <paramref name="useRestField"/> is true, the tuple was constructed with an explicit
        /// 'new ValueTuple' call, in which case the 8-th element, if any, represents the 'Rest' field.
        /// </summary>
        private void TrackNullableStateOfTupleElements(
            int slot,
            TupleTypeSymbol tupleType,
            ImmutableArray<BoundExpression> values,
            ImmutableArray<TypeWithState> types,
            bool useRestField)
        {
            Debug.Assert(values.Length == types.Length);
            Debug.Assert(values.Length == (useRestField ? Math.Min(tupleType.TupleElements.Length, TupleTypeSymbol.RestPosition) : tupleType.TupleElements.Length));

            if (slot > 0)
            {
                var tupleElements = tupleType.TupleElements;
                int n = values.Length;
                if (useRestField)
                {
                    n = Math.Min(n, TupleTypeSymbol.RestPosition - 1);
                }
                for (int i = 0; i < n; i++)
                {
                    trackState(values[i], tupleElements[i], types[i]);
                }
                if (useRestField && values.Length == TupleTypeSymbol.RestPosition)
                {
                    var restField = tupleType.GetMembers(TupleTypeSymbol.RestFieldName).FirstOrDefault() as FieldSymbol;
                    if ((object)restField != null)
                    {
                        trackState(values.Last(), restField, types.Last());
                    }
                }
            }

            void trackState(BoundExpression value, FieldSymbol field, TypeWithState valueType) =>
                TrackNullableStateForAssignment(value, field.TypeWithAnnotations, GetOrCreateSlot(field, slot), valueType, MakeSlot(value));
        }

        private void TrackNullableStateOfNullableValue(int containingSlot, TypeSymbol containingType, BoundExpression value, TypeWithState valueType, int valueSlot)
        {
            Debug.Assert(containingType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T);
            Debug.Assert(containingSlot > 0);
            Debug.Assert(valueSlot > 0);

            int targetSlot = GetNullableOfTValueSlot(containingType, containingSlot, out Symbol symbol);
            if (targetSlot > 0)
            {
                TrackNullableStateForAssignment(value, symbol.GetTypeOrReturnType(), targetSlot, valueType, valueSlot);
            }
        }

        private void TrackNullableStateOfNullableConversion(BoundConversion node)
        {
            Debug.Assert(node.ConversionKind == ConversionKind.ImplicitNullable || node.ConversionKind == ConversionKind.ExplicitNullable);

            var operand = node.Operand;
            var operandType = operand.Type;
            var convertedType = node.Type;
            if (AreNullableAndUnderlyingTypes(convertedType, operandType, out TypeWithAnnotations underlyingType))
            {
                // Conversion of T to Nullable<T> is equivalent to new Nullable<T>(t).
                TrackNullableStateOfNullableValue(node, operand, convertedType, underlyingType);
            }
        }

        private void TrackNullableStateOfNullableValue(BoundExpression node, BoundExpression operand, TypeSymbol convertedType, TypeWithAnnotations underlyingType)
        {
            int valueSlot = MakeSlot(operand);
            if (valueSlot > 0)
            {
                int containingSlot = GetOrCreatePlaceholderSlot(node);
                Debug.Assert(containingSlot > 0);
                TrackNullableStateOfNullableValue(containingSlot, convertedType, operand, underlyingType.ToTypeWithState(), valueSlot);
            }
        }

        private void TrackNullableStateOfTupleConversion(
            BoundConversion conversionOpt,
            BoundExpression convertedNode,
            Conversion conversion,
            TypeSymbol targetType,
            TypeSymbol operandType,
            int slot,
            int valueSlot,
            AssignmentKind assignmentKind,
            ParameterSymbol parameterOpt,
            bool reportWarnings)
        {
            Debug.Assert(conversion.Kind == ConversionKind.ImplicitTuple || conversion.Kind == ConversionKind.ExplicitTuple);
            Debug.Assert(slot > 0);
            Debug.Assert(valueSlot > 0);

            var valueTuple = operandType as TupleTypeSymbol;
            if (valueTuple is null)
            {
                return;
            }

            var conversions = conversion.UnderlyingConversions;
            var targetElements = ((TupleTypeSymbol)targetType).TupleElements;
            var valueElements = valueTuple.TupleElements;
            int n = valueElements.Length;
            for (int i = 0; i < n; i++)
            {
                trackConvertedValue(targetElements[i], conversions[i], valueElements[i]);
            }

            void trackConvertedValue(FieldSymbol targetField, Conversion conversion, FieldSymbol valueField)
            {
                switch (conversion.Kind)
                {
                    case ConversionKind.Identity:
                    case ConversionKind.NullLiteral:
                    case ConversionKind.DefaultLiteral:
                    case ConversionKind.ImplicitReference:
                    case ConversionKind.ExplicitReference:
                    case ConversionKind.Boxing:
                    case ConversionKind.Unboxing:
                        InheritNullableStateOfMember(slot, valueSlot, valueField, isDefaultValue: false, skipSlot: slot);
                        break;
                    case ConversionKind.ImplicitTupleLiteral:
                    case ConversionKind.ExplicitTupleLiteral:
                    case ConversionKind.ImplicitTuple:
                    case ConversionKind.ExplicitTuple:
                        {
                            int targetFieldSlot = GetOrCreateSlot(targetField, slot);
                            if (targetFieldSlot > 0)
                            {
                                this.State[targetFieldSlot] = NullableFlowState.NotNull;
                                int valueFieldSlot = GetOrCreateSlot(valueField, valueSlot);
                                if (valueFieldSlot > 0)
                                {
                                    TrackNullableStateOfTupleConversion(conversionOpt, convertedNode, conversion, targetField.Type, valueField.Type, targetFieldSlot, valueFieldSlot, assignmentKind, parameterOpt, reportWarnings);
                                }
                            }
                        }
                        break;
                    case ConversionKind.ImplicitNullable:
                    case ConversionKind.ExplicitNullable:
                        // Conversion of T to Nullable<T> is equivalent to new Nullable<T>(t).
                        if (AreNullableAndUnderlyingTypes(targetField.Type, valueField.Type, out _))
                        {
                            int targetFieldSlot = GetOrCreateSlot(targetField, slot);
                            if (targetFieldSlot > 0)
                            {
                                this.State[targetFieldSlot] = NullableFlowState.NotNull;
                                int valueFieldSlot = GetOrCreateSlot(valueField, valueSlot);
                                if (valueFieldSlot > 0)
                                {
                                    TrackNullableStateOfNullableValue(targetFieldSlot, targetField.Type, null, valueField.TypeWithAnnotations.ToTypeWithState(), valueFieldSlot);
                                }
                            }
                        }
                        break;
                    case ConversionKind.ImplicitUserDefined:
                    case ConversionKind.ExplicitUserDefined:
                        {
                            var convertedType = VisitUserDefinedConversion(
                                conversionOpt,
                                convertedNode,
                                conversion,
                                targetField.TypeWithAnnotations,
                                valueField.TypeWithAnnotations.ToTypeWithState(),
                                useLegacyWarnings: false,
                                assignmentKind,
                                parameterOpt,
                                reportTopLevelWarnings: reportWarnings,
                                reportRemainingWarnings: reportWarnings,
                                diagnosticLocation: (conversionOpt ?? convertedNode).Syntax.GetLocation());
                            int targetFieldSlot = GetOrCreateSlot(targetField, slot);
                            if (targetFieldSlot > 0)
                            {
                                this.State[targetFieldSlot] = convertedType.State;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        public override BoundNode VisitTupleBinaryOperator(BoundTupleBinaryOperator node)
        {
            base.VisitTupleBinaryOperator(node);
            SetNotNullResult(node);
            return null;
        }

        private void ReportNullabilityMismatchWithTargetDelegate(Location location, NamedTypeSymbol delegateType, MethodSymbol method, bool invokedAsExtensionMethod)
        {
            Debug.Assert((object)method != null);
            Debug.Assert(method.MethodKind != MethodKind.LambdaMethod);

            MethodSymbol invoke = delegateType?.DelegateInvokeMethod;
            if (invoke is null)
            {
                return;
            }

            if (IsNullabilityMismatch(method.ReturnTypeWithAnnotations, invoke.ReturnTypeWithAnnotations, requireIdentity: false))
            {
                ReportDiagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, location,
                    new FormattedSymbol(method, SymbolDisplayFormat.MinimallyQualifiedFormat),
                    delegateType);
            }

            int methodOffset = invokedAsExtensionMethod ? 1 : 0;
            int count = Math.Min(invoke.ParameterCount, method.ParameterCount - methodOffset);
            for (int i = 0; i < count; i++)
            {
                var invokeParameter = invoke.Parameters[i];
                var methodParameter = method.Parameters[i + methodOffset];

                var sourceParameter = invokeParameter;
                var destinationParameter = methodParameter;

                var invokeRefKind = invokeParameter.RefKind;
                if (invokeRefKind == RefKind.Out)
                {
                    // out parameters have inverted variance
                    (sourceParameter, destinationParameter) = (destinationParameter, sourceParameter);
                }

                if (IsNullabilityMismatch(sourceParameter.TypeWithAnnotations, destinationParameter.TypeWithAnnotations, requireIdentity: invokeRefKind == RefKind.Ref))
                {
                    ReportDiagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, location,
                        GetParameterAsDiagnosticArgument(methodParameter),
                        GetContainingSymbolAsDiagnosticArgument(methodParameter),
                        delegateType);
                }
            }
        }

        private void ReportNullabilityMismatchWithTargetDelegate(Location location, NamedTypeSymbol delegateType, UnboundLambda unboundLambda)
        {
            if (!unboundLambda.HasExplicitlyTypedParameterList)
            {
                return;
            }

            MethodSymbol invoke = delegateType?.DelegateInvokeMethod;
            if (invoke is null)
            {
                return;
            }

            int count = Math.Min(invoke.ParameterCount, unboundLambda.ParameterCount);
            for (int i = 0; i < count; i++)
            {
                var invokeParameter = invoke.Parameters[i];
                // Parameter nullability is expected to match exactly. This corresponds to the behavior of initial binding.
                //    Action<string> x = (object o) => { }; // error CS1661: Cannot convert lambda expression to delegate type 'Action<string>' because the parameter types do not match the delegate parameter types
                //    Action<object> y = (object? o) => { }; // warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'Action<object>'.
                // https://github.com/dotnet/roslyn/issues/35564: Consider relaxing and allow implicit conversions of nullability.
                // (Compare with method group conversions which pass `requireIdentity: false`.)
                if (IsNullabilityMismatch(invokeParameter.TypeWithAnnotations, unboundLambda.ParameterTypeWithAnnotations(i), requireIdentity: true))
                {
                    // Should the warning be reported using location of specific lambda parameter?
                    ReportDiagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, location,
                        unboundLambda.ParameterName(i),
                        unboundLambda.MessageID.Localize(),
                        delegateType);
                }
            }
        }

        private bool IsNullabilityMismatch(TypeWithAnnotations source, TypeWithAnnotations destination, bool requireIdentity)
        {
            if (!HasTopLevelNullabilityConversion(source, destination, requireIdentity))
            {
                return true;
            }
            if (requireIdentity)
            {
                return IsNullabilityMismatch(source, destination);
            }
            var sourceType = source.Type;
            var destinationType = destination.Type;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            return !_conversions.ClassifyImplicitConversionFromType(sourceType, destinationType, ref useSiteDiagnostics).Exists;
        }

        private bool HasTopLevelNullabilityConversion(TypeWithAnnotations source, TypeWithAnnotations destination, bool requireIdentity)
        {
            return requireIdentity ?
                _conversions.HasTopLevelNullabilityIdentityConversion(source, destination) :
                _conversions.HasTopLevelNullabilityImplicitConversion(source, destination);
        }

        /// <summary>
        /// Gets the conversion node for passing to <see cref="VisitConversion(BoundConversion, BoundExpression, Conversion, TypeWithAnnotations, TypeWithState, bool, bool, bool, AssignmentKind, ParameterSymbol, bool, bool, bool, Optional{LocalState}, bool, Location)"/>,
        /// if one should be passed.
        /// </summary>
        private static BoundConversion GetConversionIfApplicable(BoundExpression conversionOpt, BoundExpression convertedNode)
        {
            Debug.Assert(conversionOpt is null
                         || convertedNode == conversionOpt // Note that convertedNode itself can be a BoundConversion, so we do this check explicitly
                                                           // because the below calls to RemoveConversion could potentially strip that conversion.
                         || convertedNode == RemoveConversion(conversionOpt, includeExplicitConversions: false).expression
                         || convertedNode == RemoveConversion(conversionOpt, includeExplicitConversions: true).expression);
            return conversionOpt == convertedNode ? null : (BoundConversion)conversionOpt;
        }

        /// <summary>
        /// Apply the conversion to the type of the operand and return the resulting type. (If the
        /// operand does not have an explicit type, the operand expression is used for the type.)
        /// If `checkConversion` is set, the incoming conversion is assumed to be from binding and will be
        /// re-calculated, this time considering nullability. (Note that the conversion calculation considers
        /// nested nullability only. The caller is responsible for checking the top-level nullability of
        /// the type returned by this method.) `trackMembers` should be set if the nullability of any
        /// members of the operand should be copied to the converted result when possible.
        /// </summary>
        private TypeWithState VisitConversion(
            BoundConversion conversionOpt,
            BoundExpression conversionOperand,
            Conversion conversion,
            TypeWithAnnotations targetTypeWithNullability,
            TypeWithState operandType,
            bool checkConversion,
            bool fromExplicitCast,
            bool useLegacyWarnings,
            AssignmentKind assignmentKind,
            ParameterSymbol parameterOpt = null,
            bool reportTopLevelWarnings = true,
            bool reportRemainingWarnings = true,
            bool extensionMethodThisArgument = false,
            Optional<LocalState> stateForLambda = default,
            bool trackMembers = false,
            Location diagnosticLocationOpt = null)
        {
            Debug.Assert(!trackMembers || !IsConditionalState);
            Debug.Assert(conversionOperand != null);

            NullableFlowState resultState = NullableFlowState.NotNull;
            bool canConvertNestedNullability = true;
            bool isSuppressed = false;
            diagnosticLocationOpt ??= (conversionOpt ?? conversionOperand).Syntax.GetLocation();

            if (conversionOperand?.IsSuppressed == true)
            {
                reportTopLevelWarnings = false;
                reportRemainingWarnings = false;
                isSuppressed = true;
            }

            TypeSymbol targetType = targetTypeWithNullability.Type;
            switch (conversion.Kind)
            {
                case ConversionKind.MethodGroup:
                    {
                        var group = conversionOperand as BoundMethodGroup;
                        var delegateType = targetType.GetDelegateType();
                        var method = conversion.Method;
                        if (group != null)
                        {
                            method = CheckMethodGroupReceiverNullability(group, delegateType, method, conversion.IsExtensionMethod);
                        }
                        if (reportRemainingWarnings)
                        {
                            ReportNullabilityMismatchWithTargetDelegate(diagnosticLocationOpt, delegateType, method, conversion.IsExtensionMethod);
                        }
                    }
                    resultState = NullableFlowState.NotNull;
                    break;

                case ConversionKind.AnonymousFunction:
                    if (conversionOperand is BoundLambda lambda)
                    {
                        var delegateType = targetType.GetDelegateType();
                        var variableState = GetVariableState(stateForLambda);
                        VisitLambda(lambda, delegateType, Diagnostics, variableState);
                        if (reportRemainingWarnings)
                        {
                            ReportNullabilityMismatchWithTargetDelegate(diagnosticLocationOpt, delegateType, lambda.UnboundLambda);
                        }

                        TrackAnalyzedNullabilityThroughConversionGroup(targetTypeWithNullability.ToTypeWithState(), conversionOpt, conversionOperand);

                        return TypeWithState.Create(targetType, NullableFlowState.NotNull);
                    }
                    break;

                case ConversionKind.InterpolatedString:
                    resultState = NullableFlowState.NotNull;
                    break;

                case ConversionKind.SwitchExpression:
                    // The switch expression conversion is not represented as a separate conversion in the bound tree.
                    throw ExceptionUtilities.UnexpectedValue(conversion.Kind);

                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.ImplicitUserDefined:
                    return VisitUserDefinedConversion(conversionOpt, conversionOperand, conversion, targetTypeWithNullability, operandType, useLegacyWarnings, assignmentKind, parameterOpt, reportTopLevelWarnings, reportRemainingWarnings, diagnosticLocationOpt);

                case ConversionKind.ExplicitDynamic:
                case ConversionKind.ImplicitDynamic:
                case ConversionKind.Boxing:
                    resultState = operandType.State;
                    break;

                case ConversionKind.Unboxing:
                    if (targetType.IsNonNullableValueType())
                    {
                        if (operandType.MayBeNull && reportRemainingWarnings)
                        {
                            ReportDiagnostic(ErrorCode.WRN_UnboxPossibleNull, diagnosticLocationOpt);
                        }

                        LearnFromNonNullTest(conversionOperand, ref State);
                    }
                    else
                    {
                        resultState = operandType.State;
                    }
                    break;

                case ConversionKind.ImplicitThrow:
                    resultState = NullableFlowState.NotNull;
                    break;

                case ConversionKind.NoConversion:
                    resultState = operandType.State;
                    break;

                case ConversionKind.NullLiteral:
                case ConversionKind.DefaultLiteral:
                    if (checkConversion && RequiresSafetyWarningWhenNullIntroduced(targetTypeWithNullability) && !isSuppressed)
                    {
                        // For type parameters that cannot be annotated, the analysis must report those
                        // places where null values first sneak in, like `default`, `null`, and `GetFirstOrDefault`.
                        // This is one of those places.
                        ReportDiagnostic(ErrorCode.WRN_DefaultExpressionMayIntroduceNullT, diagnosticLocationOpt, GetTypeAsDiagnosticArgument(targetTypeWithNullability.Type));
                    }

                    checkConversion = false;
                    goto case ConversionKind.Identity;

                case ConversionKind.Identity:
                    // If the operand is an explicit conversion, and this identity conversion
                    // is converting to the same type including nullability, skip the conversion
                    // to avoid reporting redundant warnings. Also check useLegacyWarnings
                    // since that value was used when reporting warnings for the explicit cast.
                    // Don't skip the node when it's a user-defined conversion, as identity conversions
                    // on top of user-defined conversions means that we're coming in from VisitUserDefinedConversion
                    // and that any warnings caught by this recursive call of VisitConversion won't be redundant.
                    if (useLegacyWarnings && conversionOperand is BoundConversion operandConversion && !operandConversion.ConversionKind.IsUserDefinedConversion())
                    {
                        var explicitType = operandConversion.ConversionGroupOpt.ExplicitType;
                        if (explicitType.Equals(targetTypeWithNullability, TypeCompareKind.ConsiderEverything))
                        {
                            TrackAnalyzedNullabilityThroughConversionGroup(
                                calculateResultType(targetTypeWithNullability, fromExplicitCast, operandType.State, isSuppressed, targetType),
                                conversionOpt,
                                conversionOperand);
                            return operandType;
                        }
                    }
                    if (operandType.Type?.IsTupleType == true || conversionOperand.Kind == BoundKind.TupleLiteral)
                    {
                        goto case ConversionKind.ImplicitTuple;
                    }
                    goto case ConversionKind.ImplicitReference;

                case ConversionKind.ImplicitReference:
                    if (reportTopLevelWarnings &&
                        conversionOperand?.Kind == BoundKind.Literal &&
                        conversionOperand.ConstantValue?.IsNull == true &&
                        !isSuppressed &&
                        RequiresSafetyWarningWhenNullIntroduced(targetTypeWithNullability))
                    {
                        // For type parameters that cannot be annotated, the analysis must report those
                        // places where null values first sneak in, like `default`, `null`, and `GetFirstOrDefault`.
                        // This is one of those places.
                        ReportDiagnostic(ErrorCode.WRN_NullLiteralMayIntroduceNullT, diagnosticLocationOpt, targetType);
                    }
                    goto case ConversionKind.ExplicitReference;

                case ConversionKind.ExplicitReference:
                    // Inherit state from the operand.
                    if (checkConversion)
                    {
                        conversion = GenerateConversion(_conversions, conversionOperand, operandType.Type, targetType, fromExplicitCast, extensionMethodThisArgument);
                        canConvertNestedNullability = conversion.Exists;
                    }

                    resultState = operandType.State;
                    break;

                case ConversionKind.ImplicitNullable:
                    if (trackMembers)
                    {
                        Debug.Assert(conversionOperand != null);
                        if (AreNullableAndUnderlyingTypes(targetType, operandType.Type, out TypeWithAnnotations underlyingType))
                        {
                            // Conversion of T to Nullable<T> is equivalent to new Nullable<T>(t).
                            int valueSlot = MakeSlot(conversionOperand);
                            if (valueSlot > 0)
                            {
                                int containingSlot = GetOrCreatePlaceholderSlot(conversionOpt);
                                Debug.Assert(containingSlot > 0);
                                TrackNullableStateOfNullableValue(containingSlot, targetType, conversionOperand, underlyingType.ToTypeWithState(), valueSlot);
                            }
                        }
                    }

                    if (checkConversion)
                    {
                        conversion = GenerateConversion(_conversions, conversionOperand, operandType.Type, targetType, fromExplicitCast, extensionMethodThisArgument);
                        canConvertNestedNullability = conversion.Exists;
                    }

                    resultState = operandType.State;
                    break;

                case ConversionKind.ExplicitNullable:
                    if (operandType.Type?.IsNullableType() == true && !targetType.IsNullableType())
                    {
                        // Explicit conversion of Nullable<T> to T is equivalent to Nullable<T>.Value.
                        if (reportTopLevelWarnings && operandType.MayBeNull)
                        {
                            ReportDiagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, diagnosticLocationOpt);
                        }

                        // Mark the value as not nullable, regardless of whether it was known to be nullable,
                        // because the implied call to `.Value` will only succeed if not null.
                        if (conversionOperand != null)
                        {
                            LearnFromNonNullTest(conversionOperand, ref State);
                        }
                    }
                    goto case ConversionKind.ImplicitNullable;

                case ConversionKind.ImplicitTuple:
                case ConversionKind.ImplicitTupleLiteral:
                case ConversionKind.ExplicitTupleLiteral:
                case ConversionKind.ExplicitTuple:
                    if (trackMembers)
                    {
                        Debug.Assert(conversionOperand != null);
                        switch (conversion.Kind)
                        {
                            case ConversionKind.ImplicitTuple:
                            case ConversionKind.ExplicitTuple:
                                int valueSlot = MakeSlot(conversionOperand);
                                if (valueSlot > 0)
                                {
                                    int slot = GetOrCreatePlaceholderSlot(conversionOpt);
                                    if (slot > 0)
                                    {
                                        TrackNullableStateOfTupleConversion(conversionOpt, conversionOperand, conversion, targetType, operandType.Type, slot, valueSlot, assignmentKind, parameterOpt, reportWarnings: reportRemainingWarnings);
                                    }
                                }
                                break;
                        }
                    }

                    if (checkConversion && !targetType.IsErrorType())
                    {
                        // https://github.com/dotnet/roslyn/issues/29699: Report warnings for user-defined conversions on tuple elements.
                        conversion = GenerateConversion(_conversions, conversionOperand, operandType.Type, targetType, fromExplicitCast, extensionMethodThisArgument);
                        canConvertNestedNullability = conversion.Exists;
                    }
                    resultState = NullableFlowState.NotNull;
                    break;

                case ConversionKind.Deconstruction:
                    // Can reach here, with an error type, when the
                    // Deconstruct method is missing or inaccessible.
                    break;

                case ConversionKind.ExplicitEnumeration:
                    // Can reach here, with an error type.
                    break;

                default:
                    Debug.Assert(targetType.IsValueType || targetType.IsErrorType());
                    break;
            }

            TypeWithState resultType = calculateResultType(targetTypeWithNullability, fromExplicitCast, resultState, isSuppressed, targetType);

            if (operandType.Type?.IsErrorType() != true && !targetType.IsErrorType())
            {
                // Need to report all warnings that apply since the warnings can be suppressed individually.
                if (reportTopLevelWarnings)
                {
                    if (RequiresSafetyWarningWhenNullIntroduced(targetTypeWithNullability) && conversion.IsImplicit && !conversion.IsDynamic)
                    {
                        // For type parameters that cannot be annotated, the analysis must report those
                        // places where null values first sneak in, like `default`, `null`, and `GetFirstOrDefault`,
                        // as a safety diagnostic.  But we do not warn when such values flow through implicit conversion.
                    }
                    else
                    {
                        ReportNullableAssignmentIfNecessary(conversionOperand, targetTypeWithNullability, operandType, useLegacyWarnings, assignmentKind, parameterOpt, conversion, diagnosticLocationOpt);
                    }
                }
                if (reportRemainingWarnings && !canConvertNestedNullability)
                {
                    if (assignmentKind == AssignmentKind.Argument)
                    {
                        ReportNullabilityMismatchInArgument(diagnosticLocationOpt, operandType.Type, parameterOpt, targetType, forOutput: false);
                    }
                    else
                    {
                        ReportNullabilityMismatchInAssignment(diagnosticLocationOpt, GetTypeAsDiagnosticArgument(operandType.Type), targetType);
                    }
                }
            }

            TrackAnalyzedNullabilityThroughConversionGroup(resultType, conversionOpt, conversionOperand);

            return resultType;

            static TypeWithState calculateResultType(TypeWithAnnotations targetTypeWithNullability, bool fromExplicitCast, NullableFlowState resultState, bool isSuppressed, TypeSymbol targetType)
            {
                if (isSuppressed)
                {
                    resultState = NullableFlowState.NotNull;
                }
                else if (fromExplicitCast && targetTypeWithNullability.NullableAnnotation.IsAnnotated() && !targetType.IsNullableType())
                {
                    // An explicit cast to a nullable reference type introduces nullability
                    resultState = NullableFlowState.MaybeNull;
                }

                var resultType = TypeWithState.Create(targetType, resultState);
                return resultType;
            }
        }

        private TypeWithState VisitUserDefinedConversion(
            BoundConversion conversionOpt,
            BoundExpression conversionOperand,
            Conversion conversion,
            TypeWithAnnotations targetTypeWithNullability,
            TypeWithState operandType,
            bool useLegacyWarnings,
            AssignmentKind assignmentKind,
            ParameterSymbol parameterOpt,
            bool reportTopLevelWarnings,
            bool reportRemainingWarnings,
            Location diagnosticLocation)
        {
            Debug.Assert(!IsConditionalState);
            Debug.Assert(conversionOperand != null);
            Debug.Assert(targetTypeWithNullability.HasType);
            Debug.Assert(diagnosticLocation != null);
            Debug.Assert(conversion.Kind == ConversionKind.ExplicitUserDefined || conversion.Kind == ConversionKind.ImplicitUserDefined);

            TypeSymbol targetType = targetTypeWithNullability.Type;

            // cf. Binder.CreateUserDefinedConversion
            if (!conversion.IsValid)
            {
                var resultType = TypeWithState.Create(targetType, NullableFlowState.NotNull);
                TrackAnalyzedNullabilityThroughConversionGroup(resultType, conversionOpt, conversionOperand);
                return resultType;
            }

            // operand -> conversion "from" type
            // May be distinct from method parameter type for Nullable<T>.
            operandType = VisitConversion(
                conversionOpt,
                conversionOperand,
                conversion.UserDefinedFromConversion,
                TypeWithAnnotations.Create(conversion.BestUserDefinedConversionAnalysis.FromType),
                operandType,
                checkConversion: true,
                fromExplicitCast: false,
                useLegacyWarnings,
                assignmentKind,
                parameterOpt,
                reportTopLevelWarnings,
                reportRemainingWarnings,
                diagnosticLocationOpt: diagnosticLocation);

            // Update method based on operandType: see https://github.com/dotnet/roslyn/issues/29605.
            // (see NullableReferenceTypesTests.ImplicitConversions_07).
            var methodOpt = conversion.Method;
            Debug.Assert((object)methodOpt != null);
            Debug.Assert(methodOpt.ParameterCount == 1);
            var parameter = methodOpt.Parameters[0];
            var parameterType = parameter.TypeWithAnnotations;
            TypeWithState underlyingOperandType = default;
            bool isLiftedConversion = false;
            if (operandType.Type.IsNullableType() && !parameterType.IsNullableType())
            {
                var underlyingOperandTypeWithAnnotations = operandType.Type.GetNullableUnderlyingTypeWithAnnotations();
                underlyingOperandType = underlyingOperandTypeWithAnnotations.ToTypeWithState();
                isLiftedConversion = parameterType.Equals(underlyingOperandTypeWithAnnotations, TypeCompareKind.AllIgnoreOptions);
            }

            // conversion "from" type -> method parameter type
            NullableFlowState operandState = operandType.State;
            Location operandLocation = conversionOperand.Syntax.GetLocation();
            _ = ClassifyAndVisitConversion(
                conversionOperand,
                parameterType,
                isLiftedConversion ? underlyingOperandType : operandType,
                useLegacyWarnings,
                AssignmentKind.Argument,
                parameterOpt: parameter,
                reportWarnings: reportRemainingWarnings,
                fromExplicitCast: false,
                diagnosticLocation: operandLocation);

            // method parameter type -> method return type
            var methodReturnType = methodOpt.ReturnTypeWithAnnotations;
            operandType = GetLiftedReturnTypeIfNecessary(isLiftedConversion, methodReturnType, operandState);
            if (isLiftedConversion)
            {
                if (RequiresSafetyWarningWhenNullIntroduced(methodReturnType) && operandState == NullableFlowState.MaybeNull)
                {
                    ReportNullableAssignmentIfNecessary(conversionOpt, targetTypeWithNullability, operandType, useLegacyWarnings: useLegacyWarnings, assignmentKind, parameterOpt, conversion, diagnosticLocation);
                }
            }
            else
            {
                // Analyze operator call return value (honoring [Maybe|NotNull] attribute annotations) https://github.com/dotnet/roslyn/issues/32671
            }

            // method return type -> conversion "to" type
            // May be distinct from method return type for Nullable<T>.
            operandType = ClassifyAndVisitConversion(
                conversionOperand,
                TypeWithAnnotations.Create(conversion.BestUserDefinedConversionAnalysis.ToType),
                operandType,
                useLegacyWarnings,
                assignmentKind,
                parameterOpt,
                reportWarnings: reportRemainingWarnings,
                fromExplicitCast: false,
                diagnosticLocation: operandLocation);

            // conversion "to" type -> final type
            // We should only pass fromExplicitCast here. Given the following example:
            //
            // class A { public static explicit operator C(A a) => throw null!; }
            // class C
            // {
            //     void M() => var c = (C?)new A();
            // }
            //
            // This final conversion from the method return type "C" to the cast type "C?" is
            // where we will need to ensure that this is counted as an explicit cast, so that
            // the resulting operandType is nullable if that was introduced via cast.
            operandType = ClassifyAndVisitConversion(
                conversionOpt ?? conversionOperand,
                targetTypeWithNullability,
                operandType,
                useLegacyWarnings,
                assignmentKind,
                parameterOpt,
                reportWarnings: reportRemainingWarnings,
                fromExplicitCast: conversionOpt?.ExplicitCastInCode ?? false,
                diagnosticLocation);

            TrackAnalyzedNullabilityThroughConversionGroup(operandType, conversionOpt, conversionOperand);

            return operandType;
        }

        private void SnapshotWalkerThroughConversionGroup(BoundExpression conversionExpression, BoundExpression convertedNode)
        {
            if (_snapshotBuilderOpt is null)
            {
                return;
            }

            var conversionOpt = conversionExpression as BoundConversion;
            var conversionGroup = conversionOpt?.ConversionGroupOpt;
            while (conversionOpt != null &&
                   conversionOpt != convertedNode &&
                   conversionOpt.Syntax.SpanStart != convertedNode.Syntax.SpanStart)
            {
                Debug.Assert(conversionOpt.ConversionGroupOpt == conversionGroup);
                TakeIncrementalSnapshot(conversionOpt);
                conversionOpt = conversionOpt.Operand as BoundConversion;
            }
        }

        private void TrackAnalyzedNullabilityThroughConversionGroup(TypeWithState resultType, BoundConversion conversionOpt, BoundExpression convertedNode)
        {
            var visitResult = new VisitResult(resultType);
            var conversionGroup = conversionOpt?.ConversionGroupOpt;
            while (conversionOpt != null && conversionOpt != convertedNode)
            {
                Debug.Assert(conversionOpt.ConversionGroupOpt == conversionGroup);
                visitResult = withType(visitResult, conversionOpt.Type);
                SetAnalyzedNullability(conversionOpt, visitResult);
                conversionOpt = conversionOpt.Operand as BoundConversion;
            }

            static VisitResult withType(VisitResult visitResult, TypeSymbol newType) =>
                new VisitResult(TypeWithState.Create(newType, visitResult.RValueType.State),
                                TypeWithAnnotations.Create(newType, visitResult.LValueType.NullableAnnotation));
        }

        /// <summary>
        /// Return the return type for a lifted operator, given the nullability state of its operands.
        /// </summary>
        private TypeWithState GetLiftedReturnType(TypeWithAnnotations returnType, NullableFlowState operandState)
        {
            bool typeNeedsLifting = returnType.Type.IsNonNullableValueType();
            TypeSymbol type = typeNeedsLifting ? MakeNullableOf(returnType) : returnType.Type;
            NullableFlowState state = returnType.ToTypeWithState().State.Join(operandState);
            return TypeWithState.Create(type, state);
        }

        private static TypeWithState GetNullableUnderlyingTypeIfNecessary(bool isLifted, TypeWithState typeWithState)
        {
            if (isLifted)
            {
                var type = typeWithState.Type;
                if (type?.IsNullableType() == true)
                {
                    return type.GetNullableUnderlyingTypeWithAnnotations().ToTypeWithState();
                }
            }
            return typeWithState;
        }

        private TypeWithState GetLiftedReturnTypeIfNecessary(bool isLifted, TypeWithAnnotations returnType, NullableFlowState operandState)
        {
            return isLifted ?
                GetLiftedReturnType(returnType, operandState) :
                returnType.ToTypeWithState();
        }

        private TypeSymbol MakeNullableOf(TypeWithAnnotations underlying)
        {
            return compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(ImmutableArray.Create(underlying));
        }

        private TypeWithState ClassifyAndVisitConversion(
            BoundExpression node,
            TypeWithAnnotations targetType,
            TypeWithState operandType,
            bool useLegacyWarnings,
            AssignmentKind assignmentKind,
            ParameterSymbol parameterOpt,
            bool reportWarnings,
            bool fromExplicitCast,
            Location diagnosticLocation)
        {
            Debug.Assert(diagnosticLocation != null);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var conversion = _conversions.ClassifyStandardConversion(null, operandType.Type, targetType.Type, ref useSiteDiagnostics);
            if (reportWarnings && !conversion.Exists)
            {
                if (assignmentKind == AssignmentKind.Argument)
                {
                    ReportNullabilityMismatchInArgument(diagnosticLocation, operandType.Type, parameterOpt, targetType.Type, forOutput: false);
                }
                else
                {
                    ReportNullabilityMismatchInAssignment(diagnosticLocation, operandType.Type, targetType.Type);
                }
            }

            return VisitConversion(
                conversionOpt: null,
                conversionOperand: node,
                conversion,
                targetType,
                operandType,
                checkConversion: false,
                fromExplicitCast,
                useLegacyWarnings: useLegacyWarnings,
                assignmentKind,
                parameterOpt,
                reportTopLevelWarnings: reportWarnings,
                reportRemainingWarnings: !fromExplicitCast && reportWarnings,
                diagnosticLocationOpt: diagnosticLocation);
        }

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            Debug.Assert(node.Type.IsDelegateType());

            if (node.MethodOpt?.MethodKind == MethodKind.LocalFunction)
            {
                var syntax = node.Syntax;
                var localFunc = (LocalFunctionSymbol)node.MethodOpt.OriginalDefinition;
                ReplayReadsAndWrites(localFunc, syntax, writes: false);
            }

            var delegateType = (NamedTypeSymbol)node.Type;
            switch (node.Argument)
            {
                case BoundMethodGroup group:
                    {
                        VisitMethodGroup(group);
                        var method = node.MethodOpt;
                        if (method is object)
                        {
                            method = CheckMethodGroupReceiverNullability(group, delegateType, method, node.IsExtensionMethod);
                            if (!group.IsSuppressed)
                            {
                                ReportNullabilityMismatchWithTargetDelegate(group.Syntax.Location, delegateType, method, node.IsExtensionMethod);
                            }
                        }
                        SetAnalyzedNullability(group, default);
                    }
                    break;
                case BoundLambda lambda:
                    {
                        VisitLambda(lambda, delegateType, Diagnostics);
                        SetNotNullResult(lambda);
                        if (!lambda.IsSuppressed)
                        {
                            ReportNullabilityMismatchWithTargetDelegate(lambda.Symbol.DiagnosticLocation, delegateType, lambda.UnboundLambda);
                        }
                    }
                    break;
                case BoundExpression arg when arg.Type is { TypeKind: TypeKind.Delegate } argType:
                    {
                        var argTypeWithAnnotations = TypeWithAnnotations.Create(argType, NullableAnnotation.NotAnnotated);
                        var argState = VisitRvalueWithState(arg);
                        ReportNullableAssignmentIfNecessary(arg, argTypeWithAnnotations, argState, useLegacyWarnings: false);
                        if (!arg.IsSuppressed)
                        {
                            ReportNullabilityMismatchWithTargetDelegate(arg.Syntax.Location, delegateType, argType.DelegateInvokeMethod(), invokedAsExtensionMethod: false);
                        }

                        // Delegate creation will throw an exception if the argument is null
                        LearnFromNonNullTest(arg, ref State);
                    }
                    break;
                default:
                    VisitRvalue(node.Argument);
                    break;
            }

            SetNotNullResult(node);
            return null;
        }

        public override BoundNode VisitMethodGroup(BoundMethodGroup node)
        {
            Debug.Assert(!IsConditionalState);

            BoundExpression receiverOpt = node.ReceiverOpt;
            if (receiverOpt != null)
            {
                VisitRvalue(receiverOpt);
                // Receiver nullability is checked when applying the method group conversion,
                // when we have a specific method, to avoid reporting null receiver warnings
                // for extension method delegates. Here, store the receiver state for that check.
                SetMethodGroupReceiverNullability(receiverOpt, ResultType);
            }

            SetNotNullResult(node);
            return null;
        }

        private bool TryGetMethodGroupReceiverNullability(BoundExpression receiverOpt, out TypeWithState type)
        {
            if (receiverOpt != null &&
                _methodGroupReceiverMapOpt != null &&
                _methodGroupReceiverMapOpt.TryGetValue(receiverOpt, out type))
            {
                return true;
            }
            else
            {
                type = default;
                return false;
            }
        }

        private void SetMethodGroupReceiverNullability(BoundExpression receiver, TypeWithState type)
        {
            _methodGroupReceiverMapOpt ??= PooledDictionary<BoundExpression, TypeWithState>.GetInstance();
            _methodGroupReceiverMapOpt[receiver] = type;
        }

        private MethodSymbol CheckMethodGroupReceiverNullability(BoundMethodGroup group, NamedTypeSymbol delegateType, MethodSymbol method, bool invokedAsExtensionMethod)
        {
            var receiverOpt = group.ReceiverOpt;
            if (TryGetMethodGroupReceiverNullability(receiverOpt, out TypeWithState receiverType))
            {
                var syntax = group.Syntax;
                if (!invokedAsExtensionMethod)
                {
                    method = (MethodSymbol)AsMemberOfType(receiverType.Type, method);
                }
                if (method.IsGenericMethod && HasImplicitTypeArguments(group.Syntax))
                {
                    var arguments = ArrayBuilder<BoundExpression>.GetInstance();
                    if (invokedAsExtensionMethod)
                    {
                        arguments.Add(CreatePlaceholderIfNecessary(receiverOpt, receiverType.ToTypeWithAnnotations()));
                    }

                    // Create placeholders for the arguments. (See Conversions.GetDelegateArguments()
                    // which is used for that purpose in initial binding.)
                    foreach (var parameter in delegateType.DelegateInvokeMethod.Parameters)
                    {
                        var parameterType = parameter.TypeWithAnnotations;
                        arguments.Add(new BoundExpressionWithNullability(syntax, new BoundParameter(syntax, parameter), parameterType.NullableAnnotation, parameterType.Type));
                    }
                    method = InferMethodTypeArguments(_binder, method, arguments.ToImmutableAndFree(), argumentRefKindsOpt: default, argsToParamsOpt: default, expanded: false);
                }
                if (invokedAsExtensionMethod)
                {
                    CheckExtensionMethodThisNullability(receiverOpt, Conversion.Identity, method.Parameters[0], receiverType);
                }
                else
                {
                    CheckPossibleNullReceiver(receiverOpt, receiverType, checkNullableValueType: false);
                }
                if (ConstraintsHelper.RequiresChecking(method))
                {
                    CheckMethodConstraints(syntax, method);
                }
            }
            return method;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            // It's possible to reach VisitLambda without having analyzed the lambda body in error scenarios,
            // so we analyze for the purposes of determining top-level nullability. We don't want to report
            // any diagnostics from this analysis, as scenarios we want to have diagnostics for will have had
            // them reported through other analysis steps.

            // https://github.com/dotnet/roslyn/issues/35041: Can we make this conditional on whether or not we've already seen the node
            // or will that have no effect because this is always called first? It is for at least one lambda
            // conversion case, need to investigate others
            if (!_disableNullabilityAnalysis)
            {
                var bag = new DiagnosticBag();
                VisitLambda(node, node.Type.GetDelegateType(), bag);
                bag.Free();
            }
            SetNotNullResult(node);
            return null;
        }

        private void VisitLambda(BoundLambda node, NamedTypeSymbol delegateTypeOpt, DiagnosticBag diagnostics, VariableState initialState = null)
        {
            Debug.Assert(delegateTypeOpt?.IsDelegateType() != false);
            var analyzedNullabilityMap = _analyzedNullabilityMapOpt;
            var updatedSymbolMap = _updatedSymbolMapOpt;
            var snapshotBuilder = _snapshotBuilderOpt;
            if (_disableNullabilityAnalysis)
            {
                analyzedNullabilityMap = null;
                updatedSymbolMap = null;
                snapshotBuilder = null;
            }

            Analyze(
                compilation,
                node,
                _conversions,
                diagnostics,
                delegateTypeOpt?.DelegateInvokeMethod,
                initialState: initialState ?? GetVariableState(State.Clone()),
                analyzedNullabilityMap,
                updatedSymbolMap,
                snapshotBuilder,
                returnTypesOpt: null);
        }

        public override BoundNode VisitUnboundLambda(UnboundLambda node)
        {
            // The presence of this node suggests an error was detected in an earlier phase.
            // Analyze the body to report any additional warnings.
            var lambda = node.BindForErrorRecovery();
            VisitLambda(lambda, delegateTypeOpt: null, Diagnostics);
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            var body = node.Body;
            if (body != null)
            {
                var analyzedNullabilityMap = _analyzedNullabilityMapOpt;
                var updatedSymbolMap = _updatedSymbolMapOpt;
                var snapshotBuilder = _snapshotBuilderOpt;
                if (_disableNullabilityAnalysis)
                {
                    analyzedNullabilityMap = null;
                    updatedSymbolMap = null;
                    snapshotBuilder = null;
                }

                Analyze(compilation,
                        node.Symbol,
                        body,
                        _binder,
                        _conversions,
                        Diagnostics,
                        useMethodSignatureParameterTypes: false,
                        methodSignatureOpt: null,
                        initialState: GetVariableState(this.TopState()),
                        analyzedNullabilityMap,
                        updatedSymbolMap,
                        snapshotBuilder,
                        returnTypesOpt: null);
            }
            SetInvalidResult();
            return null;
        }

        public override BoundNode VisitThisReference(BoundThisReference node)
        {
            VisitThisOrBaseReference(node);
            return null;
        }

        private void VisitThisOrBaseReference(BoundExpression node)
        {
            var rvalueResult = TypeWithState.Create(node.Type, NullableFlowState.NotNull);
            var lvalueResult = TypeWithAnnotations.Create(node.Type, NullableAnnotation.NotAnnotated);
            SetResult(node, rvalueResult, lvalueResult);
        }

        public override BoundNode VisitParameter(BoundParameter node)
        {
            var parameter = node.ParameterSymbol;
            int slot = GetOrCreateSlot(parameter);
            var type = GetDeclaredParameterResult(parameter);
            SetResult(node, GetAdjustedResult(type, slot), type);
            return null;
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            Debug.Assert(!IsConditionalState);

            var left = node.Left;
            var right = node.Right;
            Visit(left);
            TypeWithAnnotations leftLValueType = LvalueResultType;
            FlowAnalysisAnnotations leftAnnotations = GetLValueAnnotations(left);
            leftLValueType = ApplyLValueAnnotations(leftLValueType, leftAnnotations);

            if (left.Kind == BoundKind.EventAccess && ((BoundEventAccess)left).EventSymbol.IsWindowsRuntimeEvent)
            {
                // Event assignment is a call to an Add method. (Note that assignment
                // of non-field-like events uses BoundEventAssignmentOperator
                // rather than BoundAssignmentOperator.)
                VisitRvalue(right);
                SetNotNullResult(node);
            }
            else
            {
                TypeWithState rightState;
                if (!node.IsRef)
                {
                    var discarded = left is BoundDiscardExpression;
                    rightState = VisitOptionalImplicitConversion(right, targetTypeOpt: discarded ? default : leftLValueType, UseLegacyWarnings(left, leftLValueType), trackMembers: true, AssignmentKind.Assignment);
                }
                else
                {
                    rightState = VisitRefExpression(right, leftLValueType);
                }

                // If the LHS has annotations, we perform an additional check for nullable value types
                CheckDisallowedNullAssignment(rightState, leftAnnotations, right.Syntax.Location);

                TrackNullableStateForAssignment(right, leftLValueType, MakeSlot(left), rightState, MakeSlot(right));
                if (left is BoundDiscardExpression)
                {
                    var lvalueType = rightState.ToTypeWithAnnotations();
                    SetResult(left, rightState, lvalueType, isLvalue: true);
                    SetResult(node, rightState, lvalueType);
                }
                else
                {
                    SetResult(node, TypeWithState.Create(leftLValueType.Type, rightState.State), leftLValueType);
                }
            }

            return null;
        }

        private FlowAnalysisAnnotations GetLValueAnnotations(BoundExpression expr)
        {
            // Annotations are ignored when binding an attribute to avoid cycles. (Members used
            // in attributes are error scenarios, so missing warnings should not be important.)
            if (IsAnalyzingAttribute)
            {
                return FlowAnalysisAnnotations.None;
            }

            var annotations = expr switch
            {
                BoundPropertyAccess property => getSetterAnnotations(property.PropertySymbol),
                BoundIndexerAccess indexer => getSetterAnnotations(indexer.Indexer),
                BoundFieldAccess field => field.FieldSymbol.FlowAnalysisAnnotations,
                _ => FlowAnalysisAnnotations.None
            };

            return annotations & (FlowAnalysisAnnotations.DisallowNull | FlowAnalysisAnnotations.AllowNull);

            static FlowAnalysisAnnotations getSetterAnnotations(PropertySymbol property)
                => property.GetOwnOrInheritedSetMethod()?.Parameters.Last()?.FlowAnalysisAnnotations ?? FlowAnalysisAnnotations.None;
        }

        private static bool UseLegacyWarnings(BoundExpression expr, TypeWithAnnotations exprType)
        {
            switch (expr.Kind)
            {
                case BoundKind.Local:
                    return expr.GetRefKind() == RefKind.None && !RequiresSafetyWarningWhenNullIntroduced(exprType);
                case BoundKind.Parameter:
                    RefKind kind = ((BoundParameter)expr).ParameterSymbol.RefKind;
                    return kind == RefKind.None && !RequiresSafetyWarningWhenNullIntroduced(exprType);
                default:
                    return false;
            }
        }

        public override BoundNode VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            return VisitDeconstructionAssignmentOperator(node, rightResultOpt: null);
        }

        private BoundNode VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node, TypeWithState? rightResultOpt = null)
        {
            var previousDisableNullabilityAnalysis = _disableNullabilityAnalysis;
            _disableNullabilityAnalysis = true;
            var left = node.Left;
            var right = node.Right;
            var variables = GetDeconstructionAssignmentVariables(left);

            if (node.HasErrors)
            {
                // In the case of errors, simply visit the right as an r-value to update
                // any nullability state even though deconstruction is skipped.
                VisitRvalue(right.Operand);
            }
            else
            {
                VisitDeconstructionArguments(variables, right.Conversion, right.Operand, rightResultOpt);
            }

            variables.FreeAll(v => v.NestedVariables);

            // https://github.com/dotnet/roslyn/issues/33011: Result type should be inferred and the constraints should
            // be re-verified. Even though the standard tuple type has no constraints we support that scenario. Constraints_78
            // has a test for this case that should start failing when this is fixed.
            SetNotNullResult(node);

            _disableNullabilityAnalysis = previousDisableNullabilityAnalysis;
            return null;
        }

        private void VisitDeconstructionArguments(ArrayBuilder<DeconstructionVariable> variables, Conversion conversion, BoundExpression right, TypeWithState? rightResultOpt = null)
        {
            Debug.Assert(conversion.Kind == ConversionKind.Deconstruction);

            int n = variables.Count;

            if (!conversion.DeconstructionInfo.IsDefault)
            {
                VisitRvalue(right);

                // If we were passed an explicit right result, use that rather than the visited result
                if (rightResultOpt.HasValue)
                {
                    SetResultType(right, rightResultOpt.Value);
                }
                var rightResult = ResultType;

                var invocation = conversion.DeconstructionInfo.Invocation as BoundCall;
                var deconstructMethod = invocation?.Method;

                if ((object)deconstructMethod != null)
                {
                    if (!invocation.InvokedAsExtensionMethod)
                    {
                        _ = CheckPossibleNullReceiver(right);

                        // update the deconstruct method with any inferred type parameters of the containing type
                        if (deconstructMethod.OriginalDefinition != deconstructMethod)
                        {
                            deconstructMethod = deconstructMethod.OriginalDefinition.AsMember((NamedTypeSymbol)rightResult.Type);
                        }
                    }
                    else
                    {
                        if (deconstructMethod.IsGenericMethod)
                        {
                            // re-infer the deconstruct parameters based on the 'this' parameter
                            ArrayBuilder<BoundExpression> placeholderArgs = ArrayBuilder<BoundExpression>.GetInstance(n + 1);
                            placeholderArgs.Add(CreatePlaceholderIfNecessary(right, rightResult.ToTypeWithAnnotations()));
                            for (int i = 0; i < n; i++)
                            {
                                placeholderArgs.Add(new BoundExpressionWithNullability(variables[i].Expression.Syntax, variables[i].Expression, NullableAnnotation.Oblivious, conversion.DeconstructionInfo.OutputPlaceholders[i].Type));
                            }
                            deconstructMethod = InferMethodTypeArguments(invocation, deconstructMethod, placeholderArgs.ToImmutableAndFree());

                            // check the constraints remain valid with the re-inferred parameter types
                            if (ConstraintsHelper.RequiresChecking(deconstructMethod))
                            {
                                CheckMethodConstraints(invocation.Syntax, deconstructMethod);
                            }
                        }
                    }

                    var parameters = deconstructMethod.Parameters;
                    int offset = invocation.InvokedAsExtensionMethod ? 1 : 0;
                    Debug.Assert(parameters.Length - offset == n);

                    if (invocation.InvokedAsExtensionMethod)
                    {
                        // Check nullability for `this` parameter
                        CheckExtensionMethodThisNullability(right, conversion, deconstructMethod.Parameters[0], rightResult);
                    }

                    for (int i = 0; i < n; i++)
                    {
                        var variable = variables[i];
                        var parameter = parameters[i + offset];
                        var underlyingConversion = conversion.UnderlyingConversions[i];
                        var nestedVariables = variable.NestedVariables;
                        if (nestedVariables != null)
                        {
                            var nestedRight = CreatePlaceholderIfNecessary(invocation.Arguments[i + offset], parameter.TypeWithAnnotations);
                            VisitDeconstructionArguments(nestedVariables, underlyingConversion, right: nestedRight);
                        }
                        else
                        {
                            VisitArgumentConversionAndInboundAssignmentsAndPreConditions(conversionOpt: null, variable.Expression, underlyingConversion, parameter.RefKind,
                                parameter, parameter.TypeWithAnnotations, GetParameterAnnotations(parameter), new VisitArgumentResult(new VisitResult(variable.Type.ToTypeWithState(), variable.Type), stateForLambda: default),
                                extensionMethodThisArgument: false);
                        }
                    }

                    for (int i = 0; i < n; i++)
                    {
                        var variable = variables[i];
                        var parameter = parameters[i + offset];
                        var nestedVariables = variable.NestedVariables;
                        if (nestedVariables == null)
                        {
                            VisitArgumentOutboundAssignmentsAndPostConditions(
                                variable.Expression, parameter.RefKind, parameter, parameter.TypeWithAnnotations, GetRValueAnnotations(parameter),
                                new VisitArgumentResult(new VisitResult(variable.Type.ToTypeWithState(), variable.Type), stateForLambda: default), notNullParametersOpt: null);
                        }
                    }
                }
            }
            else
            {
                var rightParts = GetDeconstructionRightParts(right);
                Debug.Assert(rightParts.Length == n);

                for (int i = 0; i < n; i++)
                {
                    var variable = variables[i];
                    var underlyingConversion = conversion.UnderlyingConversions[i];
                    var rightPart = rightParts[i];
                    var nestedVariables = variable.NestedVariables;
                    if (nestedVariables != null)
                    {
                        VisitDeconstructionArguments(nestedVariables, underlyingConversion, rightPart);
                    }
                    else
                    {
                        var targetType = variable.Type;
                        TypeWithState operandType;
                        TypeWithState valueType;
                        int valueSlot;
                        if (underlyingConversion.IsIdentity)
                        {
                            if ((variable.Expression as BoundLocal)?.DeclarationKind == BoundLocalDeclarationKind.WithInferredType)
                            {
                                // when the LHS is a var declaration, we can just visit the right part to infer the type
                                valueType = operandType = VisitRvalueWithState(rightPart);
                                _variableTypes[variable.Expression.ExpressionSymbol] = operandType.ToTypeWithAnnotations();
                            }
                            else
                            {
                                operandType = default;
                                valueType = VisitOptionalImplicitConversion(rightPart, targetType, useLegacyWarnings: true, trackMembers: true, AssignmentKind.Assignment);
                            }
                            valueSlot = MakeSlot(rightPart);
                        }
                        else
                        {
                            operandType = VisitRvalueWithState(rightPart);
                            valueType = VisitConversion(
                                conversionOpt: null,
                                rightPart,
                                underlyingConversion,
                                targetType,
                                operandType,
                                checkConversion: true,
                                fromExplicitCast: false,
                                useLegacyWarnings: true,
                                AssignmentKind.Assignment,
                                reportTopLevelWarnings: true,
                                reportRemainingWarnings: true,
                                trackMembers: false);
                            valueSlot = -1;
                        }

                        int targetSlot = MakeSlot(variable.Expression);
                        TrackNullableStateForAssignment(rightPart, targetType, targetSlot, valueType, valueSlot);

                        // Conversion of T to Nullable<T> is equivalent to new Nullable<T>(t).
                        if (targetSlot > 0 &&
                            underlyingConversion.Kind == ConversionKind.ImplicitNullable &&
                            AreNullableAndUnderlyingTypes(targetType.Type, operandType.Type, out TypeWithAnnotations underlyingType))
                        {
                            valueSlot = MakeSlot(rightPart);
                            if (valueSlot > 0)
                            {
                                var valueBeforeNullableWrapping = TypeWithState.Create(underlyingType.Type, NullableFlowState.NotNull);
                                TrackNullableStateOfNullableValue(targetSlot, targetType.Type, rightPart, valueBeforeNullableWrapping, valueSlot);
                            }
                        }
                    }
                }
            }
        }

        private readonly struct DeconstructionVariable
        {
            internal readonly BoundExpression Expression;
            internal readonly TypeWithAnnotations Type;
            internal readonly ArrayBuilder<DeconstructionVariable> NestedVariables;

            internal DeconstructionVariable(BoundExpression expression, TypeWithAnnotations type)
            {
                Expression = expression;
                Type = type;
                NestedVariables = null;
            }

            internal DeconstructionVariable(BoundExpression expression, ArrayBuilder<DeconstructionVariable> nestedVariables)
            {
                Expression = expression;
                Type = default;
                NestedVariables = nestedVariables;
            }
        }

        private ArrayBuilder<DeconstructionVariable> GetDeconstructionAssignmentVariables(BoundTupleExpression tuple)
        {
            var arguments = tuple.Arguments;
            var builder = ArrayBuilder<DeconstructionVariable>.GetInstance(arguments.Length);
            foreach (var argument in arguments)
            {
                builder.Add(getDeconstructionAssignmentVariable(argument));
            }
            return builder;

            DeconstructionVariable getDeconstructionAssignmentVariable(BoundExpression expr)
            {
                switch (expr.Kind)
                {
                    case BoundKind.TupleLiteral:
                    case BoundKind.ConvertedTupleLiteral:
                        return new DeconstructionVariable(expr, GetDeconstructionAssignmentVariables((BoundTupleExpression)expr));
                    default:
                        Visit(expr);
                        return new DeconstructionVariable(expr, LvalueResultType);
                }
            }
        }

        /// <summary>
        /// Return the sub-expressions for the righthand side of a deconstruction
        /// assignment. cf. LocalRewriter.GetRightParts.
        /// </summary>
        private ImmutableArray<BoundExpression> GetDeconstructionRightParts(BoundExpression expr)
        {
            switch (expr.Kind)
            {
                case BoundKind.TupleLiteral:
                case BoundKind.ConvertedTupleLiteral:
                    return ((BoundTupleExpression)expr).Arguments;
                case BoundKind.Conversion:
                    {
                        var conv = (BoundConversion)expr;
                        switch (conv.ConversionKind)
                        {
                            case ConversionKind.Identity:
                            case ConversionKind.ImplicitTupleLiteral:
                                return GetDeconstructionRightParts(conv.Operand);
                        }
                    }
                    break;
            }

            if (expr.Type is TupleTypeSymbol tupleType)
            {
                // https://github.com/dotnet/roslyn/issues/33011: Should include conversion.UnderlyingConversions[i].
                // For instance, Boxing conversions (see Deconstruction_ImplicitBoxingConversion_02) and
                // ImplicitNullable conversions (see Deconstruction_ImplicitNullableConversion_02).
                var fields = tupleType.TupleElements;
                return fields.SelectAsArray((f, e) => (BoundExpression)new BoundFieldAccess(e.Syntax, e, f, constantValueOpt: null), expr);
            }

            throw ExceptionUtilities.Unreachable;
        }

        public override BoundNode VisitIncrementOperator(BoundIncrementOperator node)
        {
            Debug.Assert(!IsConditionalState);

            var operandType = VisitRvalueWithState(node.Operand);
            var operandLvalue = LvalueResultType;
            bool setResult = false;

            if (this.State.Reachable)
            {
                // https://github.com/dotnet/roslyn/issues/29961 Update increment method based on operand type.
                MethodSymbol incrementOperator = (node.OperatorKind.IsUserDefined() && (object)node.MethodOpt != null && node.MethodOpt.ParameterCount == 1) ? node.MethodOpt : null;
                TypeWithAnnotations targetTypeOfOperandConversion;
                AssignmentKind assignmentKind = AssignmentKind.Assignment;
                ParameterSymbol parameter = null;

                // Analyze operator call properly (honoring [Disallow|Allow|Maybe|NotNull] attribute annotations) https://github.com/dotnet/roslyn/issues/32671
                // https://github.com/dotnet/roslyn/issues/29961 Update conversion method based on operand type.
                if (node.OperandConversion.IsUserDefined && (object)node.OperandConversion.Method != null && node.OperandConversion.Method.ParameterCount == 1)
                {
                    targetTypeOfOperandConversion = node.OperandConversion.Method.ReturnTypeWithAnnotations;
                }
                else if ((object)incrementOperator != null)
                {
                    targetTypeOfOperandConversion = incrementOperator.Parameters[0].TypeWithAnnotations;
                    assignmentKind = AssignmentKind.Argument;
                    parameter = incrementOperator.Parameters[0];
                }
                else
                {
                    // Either a built-in increment, or an error case.
                    targetTypeOfOperandConversion = default;
                }

                TypeWithState resultOfOperandConversionType;

                if (targetTypeOfOperandConversion.HasType)
                {
                    // https://github.com/dotnet/roslyn/issues/29961 Should something special be done for targetTypeOfOperandConversion for lifted case?
                    resultOfOperandConversionType = VisitConversion(
                        conversionOpt: null,
                        node.Operand,
                        node.OperandConversion,
                        targetTypeOfOperandConversion,
                        operandType,
                        checkConversion: true,
                        fromExplicitCast: false,
                        useLegacyWarnings: false,
                        assignmentKind,
                        parameter,
                        reportTopLevelWarnings: true,
                        reportRemainingWarnings: true);
                }
                else
                {
                    resultOfOperandConversionType = operandType;
                }

                TypeWithState resultOfIncrementType;
                if (incrementOperator is null)
                {
                    resultOfIncrementType = resultOfOperandConversionType;
                }
                else
                {
                    resultOfIncrementType = incrementOperator.ReturnTypeWithAnnotations.ToTypeWithState();
                }

                var operandTypeWithAnnotations = operandType.ToTypeWithAnnotations();
                resultOfIncrementType = VisitConversion(
                    conversionOpt: null,
                    node,
                    node.ResultConversion,
                    operandTypeWithAnnotations,
                    resultOfIncrementType,
                    checkConversion: true,
                    fromExplicitCast: false,
                    useLegacyWarnings: false,
                    AssignmentKind.Assignment);

                // https://github.com/dotnet/roslyn/issues/29961 Check node.Type.IsErrorType() instead?
                if (!node.HasErrors)
                {
                    var op = node.OperatorKind.Operator();
                    TypeWithState resultType = (op == UnaryOperatorKind.PrefixIncrement || op == UnaryOperatorKind.PrefixDecrement) ? resultOfIncrementType : operandType;
                    SetResultType(node, resultType);
                    setResult = true;

                    TrackNullableStateForAssignment(node, targetType: operandLvalue, targetSlot: MakeSlot(node.Operand), valueType: resultOfIncrementType);
                }
            }

            if (!setResult)
            {
                SetNotNullResult(node);
            }

            return null;
        }

        public override BoundNode VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            Visit(node.Left);
            TypeWithAnnotations leftLValueType = LvalueResultType;
            TypeWithState leftResultType = ResultType;

            Debug.Assert(!IsConditionalState);

            TypeWithState leftOnRightType = GetAdjustedResult(leftResultType, MakeSlot(node.Left));

            // https://github.com/dotnet/roslyn/issues/29962 Update operator based on inferred argument types.
            if ((object)node.Operator.LeftType != null)
            {
                // https://github.com/dotnet/roslyn/issues/29962 Ignoring top-level nullability of operator left parameter.
                leftOnRightType = VisitConversion(
                    conversionOpt: null,
                    node.Left,
                    node.LeftConversion,
                    TypeWithAnnotations.Create(node.Operator.LeftType),
                    leftOnRightType,
                    checkConversion: true,
                    fromExplicitCast: false,
                    useLegacyWarnings: false,
                    AssignmentKind.Assignment,
                    reportTopLevelWarnings: false,
                    reportRemainingWarnings: true);
            }
            else
            {
                leftOnRightType = default;
            }

            TypeWithState resultType;
            TypeWithState rightType = VisitRvalueWithState(node.Right);
            if ((object)node.Operator.ReturnType != null)
            {
                if (node.Operator.Kind.IsUserDefined() && (object)node.Operator.Method != null && node.Operator.Method.ParameterCount == 2)
                {
                    MethodSymbol method = node.Operator.Method;
                    VisitArguments(node, ImmutableArray.Create(node.Left, node.Right), method.ParameterRefKinds, method.Parameters, argsToParamsOpt: default,
                        expanded: true, invokedAsExtensionMethod: false, method);
                }

                resultType = InferResultNullability(node.Operator.Kind, node.Operator.Method, node.Operator.ReturnType, leftOnRightType, rightType);

                FlowAnalysisAnnotations leftAnnotations = GetLValueAnnotations(node.Left);
                leftLValueType = ApplyLValueAnnotations(leftLValueType, leftAnnotations);

                resultType = VisitConversion(
                    conversionOpt: null,
                    node,
                    node.FinalConversion,
                    leftLValueType,
                    resultType,
                    checkConversion: true,
                    fromExplicitCast: false,
                    useLegacyWarnings: false,
                    AssignmentKind.Assignment);

                // If the LHS has annotations, we perform an additional check for nullable value types
                CheckDisallowedNullAssignment(resultType, leftAnnotations, node.Syntax.Location);
            }
            else
            {
                resultType = TypeWithState.Create(node.Type, NullableFlowState.NotNull);
            }

            TrackNullableStateForAssignment(node, leftLValueType, MakeSlot(node.Left), resultType);
            SetResultType(node, resultType);
            return null;
        }

        public override BoundNode VisitFixedLocalCollectionInitializer(BoundFixedLocalCollectionInitializer node)
        {
            var initializer = node.Expression;
            if (initializer.Kind == BoundKind.AddressOfOperator)
            {
                initializer = ((BoundAddressOfOperator)initializer).Operand;
            }

            VisitRvalue(initializer);
            if (node.Expression.Kind == BoundKind.AddressOfOperator)
            {
                SetResultType(node.Expression, TypeWithState.Create(node.Expression.Type, ResultType.State));
            }
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode VisitAddressOfOperator(BoundAddressOfOperator node)
        {
            Visit(node.Operand);
            SetNotNullResult(node);
            return null;
        }

        private void ReportArgumentWarnings(BoundExpression argument, TypeWithState argumentType, ParameterSymbol parameter)
        {
            var paramType = parameter.TypeWithAnnotations;
            ReportNullableAssignmentIfNecessary(argument, paramType, argumentType, useLegacyWarnings: false, AssignmentKind.Argument, parameterOpt: parameter);

            if (!argumentType.HasNullType && IsNullabilityMismatch(paramType.Type, argumentType.Type))
            {
                ReportNullabilityMismatchInArgument(argument.Syntax, argumentType.Type, parameter, paramType.Type, forOutput: false);
            }
        }

        private void ReportNullabilityMismatchInRefArgument(BoundExpression argument, TypeSymbol argumentType, ParameterSymbol parameter, TypeSymbol parameterType)
        {
            ReportDiagnostic(ErrorCode.WRN_NullabilityMismatchInArgument,
                argument.Syntax, argumentType, parameterType,
                GetParameterAsDiagnosticArgument(parameter),
                GetContainingSymbolAsDiagnosticArgument(parameter));
        }

        /// <summary>
        /// Report warning passing argument where nested nullability does not match
        /// parameter (e.g.: calling `void F(object[] o)` with `F(new[] { maybeNull })`).
        /// </summary>
        private void ReportNullabilityMismatchInArgument(SyntaxNode argument, TypeSymbol argumentType, ParameterSymbol parameter, TypeSymbol parameterType, bool forOutput)
        {
            ReportNullabilityMismatchInArgument(argument.GetLocation(), argumentType, parameter, parameterType, forOutput);
        }

        private void ReportNullabilityMismatchInArgument(Location argument, TypeSymbol argumentType, ParameterSymbol parameterOpt, TypeSymbol parameterType, bool forOutput)
        {
            ReportDiagnostic(forOutput ? ErrorCode.WRN_NullabilityMismatchInArgumentForOutput : ErrorCode.WRN_NullabilityMismatchInArgument,
                argument, argumentType, parameterType,
                GetParameterAsDiagnosticArgument(parameterOpt),
                GetContainingSymbolAsDiagnosticArgument(parameterOpt));
        }

        private TypeWithAnnotations GetDeclaredLocalResult(LocalSymbol local)
        {
            return _variableTypes.TryGetValue(local, out TypeWithAnnotations type) ?
                type :
                local.TypeWithAnnotations;
        }

        private TypeWithAnnotations GetDeclaredParameterResult(ParameterSymbol parameter)
        {
            return _variableTypes.TryGetValue(parameter, out TypeWithAnnotations type) ?
                type :
                parameter.TypeWithAnnotations;
        }

        public override BoundNode VisitBaseReference(BoundBaseReference node)
        {
            VisitThisOrBaseReference(node);
            return null;
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            var updatedSymbol = VisitMemberAccess(node, node.ReceiverOpt, node.FieldSymbol);
            SetUpdatedSymbol(node, node.FieldSymbol, updatedSymbol);
            return null;
        }

        public override BoundNode VisitPropertyAccess(BoundPropertyAccess node)
        {
            var updatedMember = VisitMemberAccess(node, node.ReceiverOpt, node.PropertySymbol);
            SetUpdatedSymbol(node, node.PropertySymbol, updatedMember);
            return null;
        }

        public override BoundNode VisitIndexerAccess(BoundIndexerAccess node)
        {
            var receiverOpt = node.ReceiverOpt;
            var receiverType = VisitRvalueWithState(receiverOpt);
            // https://github.com/dotnet/roslyn/issues/30598: Mark receiver as not null
            // after indices have been visited, and only if the receiver has not changed.
            _ = CheckPossibleNullReceiver(receiverOpt);

            var indexer = node.Indexer;
            if (!receiverType.HasNullType)
            {
                // Update indexer based on inferred receiver type.
                indexer = (PropertySymbol)AsMemberOfType(receiverType.Type, indexer);
            }

            VisitArguments(node, node.Arguments, node.ArgumentRefKindsOpt, indexer, node.ArgsToParamsOpt, node.Expanded);

            var resultType = ApplyUnconditionalAnnotations(indexer.TypeWithAnnotations.ToTypeWithState(), GetRValueAnnotations(indexer));
            SetResult(node, resultType, indexer.TypeWithAnnotations);
            SetUpdatedSymbol(node, node.Indexer, indexer);
            return null;
        }

        public override BoundNode VisitIndexOrRangePatternIndexerAccess(BoundIndexOrRangePatternIndexerAccess node)
        {
            BoundExpression receiver = node.Receiver;
            var receiverType = VisitRvalueWithState(receiver);
            // https://github.com/dotnet/roslyn/issues/30598: Mark receiver as not null
            // after indices have been visited, and only if the receiver has not changed.
            _ = CheckPossibleNullReceiver(receiver);

            VisitRvalue(node.Argument);
            var patternSymbol = node.PatternSymbol;
            if (!receiverType.HasNullType)
            {
                patternSymbol = AsMemberOfType(receiverType.Type, patternSymbol);
            }

            SetLvalueResultType(node, patternSymbol.GetTypeOrReturnType());
            SetUpdatedSymbol(node, node.PatternSymbol, patternSymbol);
            return null;
        }

        public override BoundNode VisitEventAccess(BoundEventAccess node)
        {
            var updatedSymbol = VisitMemberAccess(node, node.ReceiverOpt, node.EventSymbol);
            SetUpdatedSymbol(node, node.EventSymbol, updatedSymbol);
            return null;
        }

        private Symbol VisitMemberAccess(BoundExpression node, BoundExpression receiverOpt, Symbol member)
        {
            Debug.Assert(!IsConditionalState);

            var receiverType = (receiverOpt != null) ? VisitRvalueWithState(receiverOpt) : default;

            SpecialMember? nullableOfTMember = null;
            if (member.RequiresInstanceReceiver())
            {
                member = AsMemberOfType(receiverType.Type, member);
                nullableOfTMember = GetNullableOfTMember(member);
                // https://github.com/dotnet/roslyn/issues/30598: For l-values, mark receiver as not null
                // after RHS has been visited, and only if the receiver has not changed.
                bool skipReceiverNullCheck = nullableOfTMember != SpecialMember.System_Nullable_T_get_Value;
                _ = CheckPossibleNullReceiver(receiverOpt, checkNullableValueType: !skipReceiverNullCheck);
            }

            var type = member.GetTypeOrReturnType();
            var resultType = type.ToTypeWithState();

            // We are supposed to track information for the node. Use whatever we managed to
            // accumulate so far.
            if (PossiblyNullableType(resultType.Type))
            {
                int slot = MakeMemberSlot(receiverOpt, member);
                if (slot > 0 && slot < this.State.Capacity)
                {
                    var state = this.State[slot];
                    resultType = TypeWithState.Create(resultType.Type, state);
                }
            }

            Debug.Assert(!IsConditionalState);
            if (nullableOfTMember == SpecialMember.System_Nullable_T_get_HasValue && !(receiverOpt is null))
            {
                int containingSlot = MakeSlot(receiverOpt);
                if (containingSlot > 0)
                {
                    Split();
                    this.StateWhenTrue[containingSlot] = NullableFlowState.NotNull;
                }
            }

            SetResult(node, resultType, type);
            return member;
        }

        private SpecialMember? GetNullableOfTMember(Symbol member)
        {
            if (member.Kind == SymbolKind.Property)
            {
                var getMethod = ((PropertySymbol)member.OriginalDefinition).GetMethod;
                if ((object)getMethod != null && getMethod.ContainingType.SpecialType == SpecialType.System_Nullable_T)
                {
                    if (getMethod == compilation.GetSpecialTypeMember(SpecialMember.System_Nullable_T_get_Value))
                    {
                        return SpecialMember.System_Nullable_T_get_Value;
                    }
                    if (getMethod == compilation.GetSpecialTypeMember(SpecialMember.System_Nullable_T_get_HasValue))
                    {
                        return SpecialMember.System_Nullable_T_get_HasValue;
                    }
                }
            }
            return null;
        }

        private int GetNullableOfTValueSlot(TypeSymbol containingType, int containingSlot, out Symbol valueProperty, bool forceSlotEvenIfEmpty = false)
        {
            Debug.Assert(containingType.IsNullableType());
            Debug.Assert(TypeSymbol.Equals(variableBySlot[containingSlot].Symbol.GetTypeOrReturnType().Type, containingType, TypeCompareKind.ConsiderEverything2));

            var getValue = (MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Nullable_T_get_Value);
            valueProperty = getValue?.AsMember((NamedTypeSymbol)containingType)?.AssociatedSymbol;
            return (valueProperty is null) ? -1 : GetOrCreateSlot(valueProperty, containingSlot, forceSlotEvenIfEmpty: forceSlotEvenIfEmpty);
        }

        protected override void VisitForEachExpression(BoundForEachStatement node)
        {
            if (node.Expression.Kind != BoundKind.Conversion)
            {
                // If we're in this scenario, there was a binding error, and we should suppress any further warnings.
                Debug.Assert(node.HasErrors);
                VisitRvalue(node.Expression);
                return;
            }

            var (expr, conversion) = RemoveConversion(node.Expression, includeExplicitConversions: false);
            SnapshotWalkerThroughConversionGroup(node.Expression, expr);

            // There are 7 ways that a foreach can be created:
            //    1. The collection type is an array type. For this, initial binding will generate an implicit reference conversion to
            //       IEnumerable, and we do not need to do any reinferring of enumerators here.
            //    2. The collection type is dynamic. For this we do the same as 1.
            //    3. The collection type implements the GetEnumerator pattern. For this, there is an identity conversion. Because
            //       this identity conversion uses nested types from initial binding, we cannot trust them and must instead use
            //       the type of the expression returned from VisitResult to reinfer the enumerator information.
            //    4. The collection type implements IEnumerable<T>. Only a few cases can hit this without being caught by number 3,
            //       such as a type with a private implementation of IEnumerable<T>, or a type parameter constrained to that type.
            //       In these cases, there will be an implicit conversion to IEnumerable<T>, but this will use types from
            //       initial binding. For this scenario, we need to look through the list of implemented interfaces on the type and
            //       find the version of IEnumerable<T> that it has after nullable analysis, as type substitution could have changed
            //       nested nullability of type paramters. See ForEach_22 for a concrete example of this.
            //    5. The collection type implements IEnumerable (non-generic). Because this version isn't generic, we don't need to
            //       do any reinference, and the existing conversion can stand as is.
            //    6. The target framework's System.String doesn't implement IEnumerable. This is a compat case: System.String normally
            //       does implement IEnumerable, but there are certain target frameworks where this isn't the case. The compiler will
            //       still emit code for foreach in these scenarios.
            //    7. Some binding error occurred, and some other error has already been reported. Usually this doesn't have any kind
            //       of conversion on top, but if there was an explicit conversion in code then we could get past the initial check
            //       for a BoundConversion node.

            var resultTypeWithState = VisitRvalueWithState(expr);
            SetAnalyzedNullability(expr, _visitResult);
            TypeWithAnnotations targetTypeWithAnnotations;

            if (conversion.IsIdentity ||
                (conversion.Kind == ConversionKind.ExplicitReference && resultTypeWithState.Type.SpecialType == SpecialType.System_String))
            {
                // This is case 3 or 6.
                targetTypeWithAnnotations = resultTypeWithState.ToTypeWithAnnotations();
            }
            else if (conversion.IsImplicit)
            {
                bool isAsync = node.AwaitOpt != null;
                if (node.Expression.Type.SpecialType == SpecialType.System_Collections_IEnumerable)
                {
                    // If this is a conversion to IEnumerable (non-generic), nothing to do. This is cases 1, 2, and 5.
                    targetTypeWithAnnotations = TypeWithAnnotations.Create(node.Expression.Type);
                }
                else if (ForEachLoopBinder.IsIEnumerableT(node.Expression.Type.OriginalDefinition, isAsync, compilation))
                {
                    // This is case 4. We need to look for the IEnumerable<T> that this reinferred expression implements,
                    // so that we pick up any nested type substitutions that could have occurred.
                    HashSet<DiagnosticInfo> ignoredUseSiteDiagnostics = null;
                    targetTypeWithAnnotations = TypeWithAnnotations.Create(ForEachLoopBinder.GetIEnumerableOfT(resultTypeWithState.Type, isAsync, compilation, ref ignoredUseSiteDiagnostics, out bool foundMultiple));
                    Debug.Assert(!foundMultiple);
                    Debug.Assert(targetTypeWithAnnotations.HasType);
                }
                else
                {
                    // This is case 7. There was not a successful binding, as a successful binding will _always_ generate one of the
                    // above conversions. Just return, as we want to suppress further errors.
                    return;
                }
            }
            else
            {
                // This is also case 7.
                return;
            }

            var convertedResult = VisitConversion(
                GetConversionIfApplicable(node.Expression, expr),
                expr,
                conversion,
                targetTypeWithAnnotations,
                resultTypeWithState,
                checkConversion: true,
                fromExplicitCast: false,
                useLegacyWarnings: false,
                AssignmentKind.Assignment);

            bool reportedDiagnostic = CheckPossibleNullReceiver(expr);

            SetResultType(node.Expression, convertedResult);

            if (resultTypeWithState.Type.IsArray() ||
                (conversion.Kind == ConversionKind.ExplicitReference &&
                 resultTypeWithState.Type.SpecialType == SpecialType.System_String))
            {
                // If we're iterating over an array type, VisitForEachIterationVariables will need the array type to calculate
                // the reinferred element type of the foreach, even though we use the IEnumerator pattern for arrays.
                // If we're iterating over a string that was explicitly converted, then it doesn't implement IEnumerable and
                // VisitForEachIterationVariables will need to explicitly set the type of the iteration variables to char.
                SetResultType(expression: null, resultTypeWithState);
            }

            if (reportedDiagnostic || node.EnumeratorInfoOpt == null || node.Expression is BoundConversion { Operand: { IsSuppressed: true } })
            {
                return;
            }

            var getEnumeratorMethod = (MethodSymbol)AsMemberOfType(convertedResult.Type, node.EnumeratorInfoOpt.GetEnumeratorMethod);
            var enumeratorReturnType = getEnumeratorMethod.ReturnTypeWithAnnotations.ToTypeWithState();
            if (enumeratorReturnType.State == NullableFlowState.MaybeNull)
            {
                ReportDiagnostic(ErrorCode.WRN_NullReferenceReceiver, expr.Syntax.GetLocation());
            }
        }

        public override void VisitForEachIterationVariables(BoundForEachStatement node)
        {
            TypeWithAnnotations sourceType;
            if (node.EnumeratorInfoOpt == null)
            {
                sourceType = default;
            }
            else
            {
                var inferredCollectionType = ResultType.Type;

                if (inferredCollectionType is ArrayTypeSymbol arrayType)
                {
                    // Even though arrays use the IEnumerator pattern, we use the array element type as the foreach target type, so
                    // directly get our source type from there instead of doing method reinference.
                    sourceType = arrayType.ElementTypeWithAnnotations;
                }
                else if (inferredCollectionType.SpecialType == SpecialType.System_String)
                {
                    // There are frameworks where System.String does not implement IEnumerable, but we still lower it to a for loop
                    // using the indexer over the individual characters anyway. So the type must be not annotated char.
                    sourceType = TypeWithAnnotations.Create(node.EnumeratorInfoOpt.ElementType, NullableAnnotation.NotAnnotated);
                }
                else
                {
                    // Reinfer the return type of the node.Expression.GetEnumerator().Current property, so that if
                    // the collection changed nested generic types we pick up those changes. ResultType should have been
                    // set by VisitForEachExpression, called just before this.
                    Debug.Assert(ResultType.Type.Equals(node.EnumeratorInfoOpt.CollectionType, TypeCompareKind.AllNullableIgnoreOptions));
                    var getEnumeratorMethod = (MethodSymbol)AsMemberOfType(inferredCollectionType, node.EnumeratorInfoOpt.GetEnumeratorMethod);
                    var currentProperty = (MethodSymbol)AsMemberOfType(getEnumeratorMethod.ReturnType, node.EnumeratorInfoOpt.CurrentPropertyGetter);
                    sourceType = currentProperty.ReturnTypeWithAnnotations;
                }
            }

            TypeWithState sourceState = sourceType.ToTypeWithState();

#pragma warning disable IDE0055 // Fix formatting
            var variableLocation = node.Syntax switch
            {
                ForEachStatementSyntax statement => statement.Identifier.GetLocation(),
                ForEachVariableStatementSyntax variableStatement => variableStatement.Variable.GetLocation(),
                _ => throw ExceptionUtilities.UnexpectedValue(node.Syntax)
            };
#pragma warning restore IDE0055 // Fix formatting

            if (node.DeconstructionOpt is object)
            {
                var assignment = node.DeconstructionOpt.DeconstructionAssignment;

                // Visit the assignment as a deconstruction with an explicit type
                VisitDeconstructionAssignmentOperator(assignment, sourceState);

                // https://github.com/dotnet/roslyn/issues/35010: if the iteration variable is a tuple deconstruction, we need to put something in the tree
                Visit(node.IterationVariableType);
            }
            else
            {
                Visit(node.IterationVariableType);
                foreach (var iterationVariable in node.IterationVariables)
                {
                    var state = NullableFlowState.NotNull;
                    if (!sourceState.HasNullType)
                    {
                        TypeWithAnnotations destinationType = iterationVariable.TypeWithAnnotations;
                        TypeWithState result = sourceState;
                        if (iterationVariable.IsRef)
                        {
                            // foreach (ref DestinationType variable in collection)
                            if (IsNullabilityMismatch(sourceType, destinationType))
                            {
                                var foreachSyntax = (ForEachStatementSyntax)node.Syntax;
                                ReportNullabilityMismatchInAssignment(foreachSyntax.Type, sourceType, destinationType);
                            }
                        }
                        else
                        {
                            // foreach (DestinationType variable in collection)
                            // foreach (var variable in collection)
                            // and asynchronous variants
                            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                            Conversion conversion = node.ElementConversion.Kind == ConversionKind.UnsetConversionKind
                                ? _conversions.ClassifyImplicitConversionFromType(sourceType.Type, destinationType.Type, ref useSiteDiagnostics)
                                : node.ElementConversion;
                            result = VisitConversion(
                                conversionOpt: null,
                                conversionOperand: node.IterationVariableType,
                                conversion,
                                destinationType,
                                sourceState,
                                checkConversion: true,
                                fromExplicitCast: !conversion.IsImplicit,
                                useLegacyWarnings: false,
                                AssignmentKind.ForEachIterationVariable,
                                reportTopLevelWarnings: true,
                                reportRemainingWarnings: true,
                                diagnosticLocationOpt: variableLocation);
                        }

                        // In non-error cases we'll only run this loop a single time. In error cases we'll set the nullability of the VariableType multiple times, but at least end up with something
                        SetAnalyzedNullability(node.IterationVariableType, new VisitResult(result, destinationType), isLvalue: true);
                        state = result.State;

                        // If we inferred the type of this variable, then record the inferred type of the variable for later use by the SemanticModel.
                        if (node.Syntax is ForEachStatementSyntax { Type: { IsVar: true } })
                        {
                            _variableTypes[iterationVariable] = result.ToTypeWithAnnotations();
                        }
                    }

                    int slot = GetOrCreateSlot(iterationVariable);
                    if (slot > 0)
                    {
                        this.State[slot] = state;
                    }
                }

            }
        }

        public override BoundNode VisitFromEndIndexExpression(BoundFromEndIndexExpression node)
        {
            var result = base.VisitFromEndIndexExpression(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode VisitObjectInitializerMember(BoundObjectInitializerMember node)
        {
            // Should be handled by VisitObjectCreationExpression.
            throw ExceptionUtilities.Unreachable;
        }

        public override BoundNode VisitDynamicObjectInitializerMember(BoundDynamicObjectInitializerMember node)
        {
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode VisitBadExpression(BoundBadExpression node)
        {
            var result = base.VisitBadExpression(node);
            var type = TypeWithAnnotations.Create(node.Type);
            SetLvalueResultType(node, type);
            return result;
        }

        public override BoundNode VisitTypeExpression(BoundTypeExpression node)
        {
            var result = base.VisitTypeExpression(node);

            if (node.BoundContainingTypeOpt != null)
            {
                VisitTypeExpression(node.BoundContainingTypeOpt);
            }

            SetNotNullResult(node);
            return result;
        }

        public override BoundNode VisitTypeOrValueExpression(BoundTypeOrValueExpression node)
        {
            // These should not appear after initial binding except in error cases.
            var result = base.VisitTypeOrValueExpression(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode VisitUnaryOperator(BoundUnaryOperator node)
        {
            Debug.Assert(!IsConditionalState);

            TypeWithState resultType;

            switch (node.OperatorKind)
            {
                case UnaryOperatorKind.BoolLogicalNegation:
                    VisitCondition(node.Operand);
                    SetConditionalState(StateWhenFalse, StateWhenTrue);
                    resultType = getResultType(ResultType);
                    break;
                case UnaryOperatorKind.DynamicTrue:
                    // We cannot use VisitCondition, because the operand is not of type bool.
                    // Yet we want to keep the result split if it was split.  So we simply visit.
                    Visit(node.Operand);
                    resultType = getResultType(ResultType);
                    break;
                case UnaryOperatorKind.DynamicLogicalNegation:
                    // We cannot use VisitCondition, because the operand is not of type bool.
                    // Yet we want to keep the result split if it was split.  So we simply visit.
                    Visit(node.Operand);
                    // If the state is split, the result is `bool` at runtime and we invert it here.
                    if (IsConditionalState)
                        SetConditionalState(StateWhenFalse, StateWhenTrue);
                    resultType = getResultType(ResultType);
                    break;
                default:
                    if (node.OperatorKind.IsUserDefined() &&
                        node.MethodOpt is MethodSymbol method &&
                        method.ParameterCount == 1)
                    {
                        var (operand, conversion) = RemoveConversion(node.Operand, includeExplicitConversions: false);
                        VisitRvalue(operand);
                        var operandResult = ResultType;
                        bool isLifted = node.OperatorKind.IsLifted();
                        var operandType = GetNullableUnderlyingTypeIfNecessary(isLifted, operandResult);
                        // Update method based on inferred operand type.
                        method = (MethodSymbol)AsMemberOfType(operandType.Type.StrippedType(), method);
                        // Analyze operator call properly (honoring [Disallow|Allow|Maybe|NotNull] attribute annotations) https://github.com/dotnet/roslyn/issues/32671
                        var parameter = method.Parameters[0];
                        _ = VisitConversion(
                            node.Operand as BoundConversion,
                            operand,
                            conversion,
                            parameter.TypeWithAnnotations,
                            operandType,
                            checkConversion: true,
                            fromExplicitCast: false,
                            useLegacyWarnings: false,
                            assignmentKind: AssignmentKind.Argument,
                            parameterOpt: parameter);
                        resultType = GetLiftedReturnTypeIfNecessary(isLifted, method.ReturnTypeWithAnnotations, operandResult.State);
                    }
                    else
                    {
                        VisitRvalue(node.Operand);
                        resultType = getResultType(ResultType);
                    }
                    break;
            }

            SetResultType(node, resultType);
            return null;

            TypeWithState getResultType(TypeWithState argumentResult) =>
                TypeWithState.Create(node.Type, node.OperatorKind.IsLifted() ? argumentResult.State : NullableFlowState.NotNull);
        }

        public override BoundNode VisitPointerIndirectionOperator(BoundPointerIndirectionOperator node)
        {
            var result = base.VisitPointerIndirectionOperator(node);
            var type = TypeWithAnnotations.Create(node.Type);
            SetLvalueResultType(node, type);
            return result;
        }

        public override BoundNode VisitPointerElementAccess(BoundPointerElementAccess node)
        {
            var result = base.VisitPointerElementAccess(node);
            var type = TypeWithAnnotations.Create(node.Type);
            SetLvalueResultType(node, type);
            return result;
        }

        public override BoundNode VisitRefTypeOperator(BoundRefTypeOperator node)
        {
            VisitRvalue(node.Operand);
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode VisitMakeRefOperator(BoundMakeRefOperator node)
        {
            var result = base.VisitMakeRefOperator(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode VisitRefValueOperator(BoundRefValueOperator node)
        {
            var result = base.VisitRefValueOperator(node);
            var type = TypeWithAnnotations.Create(node.Type, node.NullableAnnotation);
            SetLvalueResultType(node, type);
            return result;
        }

        private TypeWithState InferResultNullability(BoundUserDefinedConditionalLogicalOperator node)
        {
            if (node.OperatorKind.IsLifted())
            {
                // https://github.com/dotnet/roslyn/issues/33879 Conversions: Lifted operator
                // Should this use the updated flow type and state?  How should it compute nullability?
                return TypeWithState.Create(node.Type, NullableFlowState.NotNull);
            }

            // Update method based on inferred operand types: see https://github.com/dotnet/roslyn/issues/29605.
            // Analyze operator result properly (honoring [Maybe|NotNull] and [Maybe|NotNullWhen] attribute annotations) https://github.com/dotnet/roslyn/issues/32671
            if ((object)node.LogicalOperator != null && node.LogicalOperator.ParameterCount == 2)
            {
                return GetReturnTypeWithState(node.LogicalOperator);
            }
            else
            {
                return default;
            }
        }

        protected override void AfterLeftChildOfBinaryLogicalOperatorHasBeenVisited(BoundExpression node, BoundExpression right, bool isAnd, bool isBool, ref LocalState leftTrue, ref LocalState leftFalse)
        {
            Debug.Assert(!IsConditionalState);
            TypeWithState leftType = ResultType;
            // https://github.com/dotnet/roslyn/issues/29605 Update operator methods based on inferred operand types.
            MethodSymbol logicalOperator = null;
            MethodSymbol trueFalseOperator = null;
            BoundExpression left = null;

            switch (node.Kind)
            {
                case BoundKind.BinaryOperator:
                    Debug.Assert(!((BoundBinaryOperator)node).OperatorKind.IsUserDefined());
                    break;
                case BoundKind.UserDefinedConditionalLogicalOperator:
                    var binary = (BoundUserDefinedConditionalLogicalOperator)node;
                    if (binary.LogicalOperator != null && binary.LogicalOperator.ParameterCount == 2)
                    {
                        logicalOperator = binary.LogicalOperator;
                        left = binary.Left;
                        trueFalseOperator = isAnd ? binary.FalseOperator : binary.TrueOperator;

                        if ((object)trueFalseOperator != null && trueFalseOperator.ParameterCount != 1)
                        {
                            trueFalseOperator = null;
                        }
                    }
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind);
            }

            Debug.Assert(trueFalseOperator is null || ((object)logicalOperator != null && left != null));

            // Analyze operator call properly (honoring [Disallow|Allow|Maybe|NotNull] attribute annotations) https://github.com/dotnet/roslyn/issues/32671
            if ((object)trueFalseOperator != null)
            {
                ReportArgumentWarnings(left, leftType, trueFalseOperator.Parameters[0]);
            }

            if ((object)logicalOperator != null)
            {
                ReportArgumentWarnings(left, leftType, logicalOperator.Parameters[0]);
            }

            Visit(right);
            TypeWithState rightType = ResultType;

            SetResultType(node, InferResultNullabilityOfBinaryLogicalOperator(node, leftType, rightType));

            if ((object)logicalOperator != null)
            {
                ReportArgumentWarnings(right, rightType, logicalOperator.Parameters[1]);
            }

            AfterRightChildOfBinaryLogicalOperatorHasBeenVisited(node, right, isAnd, isBool, ref leftTrue, ref leftFalse);
        }

        private TypeWithState InferResultNullabilityOfBinaryLogicalOperator(BoundExpression node, TypeWithState leftType, TypeWithState rightType)
        {
            return node switch
            {
                BoundBinaryOperator binary => InferResultNullability(binary.OperatorKind, binary.MethodOpt, binary.Type, leftType, rightType),
                BoundUserDefinedConditionalLogicalOperator userDefined => InferResultNullability(userDefined),
                _ => throw ExceptionUtilities.UnexpectedValue(node)
            };
        }

        public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
        {
            var result = base.VisitAwaitExpression(node);
            _ = CheckPossibleNullReceiver(node.Expression);
            if (node.Type.IsValueType || node.HasErrors || node.AwaitableInfo.GetResult is null)
            {
                SetNotNullResult(node);
            }
            else
            {
                // Update method based on inferred receiver type: see https://github.com/dotnet/roslyn/issues/29605.
                SetResultType(node, node.AwaitableInfo.GetResult.ReturnTypeWithAnnotations.ToTypeWithState());
            }

            return result;
        }

        public override BoundNode VisitTypeOfOperator(BoundTypeOfOperator node)
        {
            var result = base.VisitTypeOfOperator(node);
            SetResultType(node, TypeWithState.Create(node.Type, NullableFlowState.NotNull));
            return result;
        }

        public override BoundNode VisitMethodInfo(BoundMethodInfo node)
        {
            var result = base.VisitMethodInfo(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode VisitFieldInfo(BoundFieldInfo node)
        {
            var result = base.VisitFieldInfo(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode VisitDefaultLiteral(BoundDefaultLiteral node)
        {
            // Can occur in error scenarios
            var result = base.VisitDefaultLiteral(node);
            SetUnknownResultNullability(node);
            return result;
        }

        public override BoundNode VisitDefaultExpression(BoundDefaultExpression node)
        {
            Debug.Assert(!this.IsConditionalState);

            var result = base.VisitDefaultExpression(node);
            TypeSymbol type = node.Type;
            if (EmptyStructTypeCache.IsTrackableStructType(type))
            {
                int slot = GetOrCreatePlaceholderSlot(node);
                if (slot > 0)
                {
                    this.State[slot] = NullableFlowState.NotNull;
                    InheritNullableStateOfTrackableStruct(type, slot, valueSlot: -1, isDefaultValue: true);
                }
            }

            // https://github.com/dotnet/roslyn/issues/33344: this fails to produce an updated tuple type for a default expression
            // (should produce nullable element types for those elements that are of reference types)
            SetResultType(node, TypeWithState.ForType(type));
            if (node.TargetType is object && RequiresSafetyWarningWhenNullIntroduced(node.Type) &&
                !node.IsSuppressed)
            {
                // For type parameters that cannot be annotated, the analysis must report those
                // places where null values first sneak in, like `default`, `null`, and `GetFirstOrDefault`.
                // This is one of those places.
                // `default(...)` is handled here to get a better behavior with an oblivious target type, while `default` is handled in VisitConversions.
                ReportDiagnostic(ErrorCode.WRN_DefaultExpressionMayIntroduceNullT, node.Syntax, GetTypeAsDiagnosticArgument(ResultType.Type));
            }

            return result;
        }

        public override BoundNode VisitIsOperator(BoundIsOperator node)
        {
            Debug.Assert(!this.IsConditionalState);

            var operand = node.Operand;
            var result = base.VisitIsOperator(node);
            Debug.Assert(node.Type.SpecialType == SpecialType.System_Boolean);

            var slotBuilder = ArrayBuilder<int>.GetInstance();
            GetSlotsToMarkAsNotNullable(operand, slotBuilder);
            if (slotBuilder.Count > 0)
            {
                Split();
                MarkSlotsAsNotNull(slotBuilder, ref StateWhenTrue);
            }
            slotBuilder.Free();

            VisitTypeExpression(node.TargetType);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode VisitAsOperator(BoundAsOperator node)
        {
            var argumentType = VisitRvalueWithState(node.Operand);
            NullableFlowState resultState = NullableFlowState.NotNull;
            var type = node.Type;

            if (type.CanContainNull())
            {
                switch (node.Conversion.Kind)
                {
                    case ConversionKind.Identity:
                    case ConversionKind.ImplicitReference:
                    case ConversionKind.Boxing:
                    case ConversionKind.ImplicitNullable:
                        resultState = argumentType.State;
                        break;

                    default:
                        resultState = NullableFlowState.MaybeNull;
                        if (RequiresSafetyWarningWhenNullIntroduced(node.TargetType.TypeWithAnnotations))
                        {
                            ReportDiagnostic(ErrorCode.WRN_AsOperatorMayReturnNull, node.Syntax, type);
                        }
                        break;
                }
            }

            VisitTypeExpression(node.TargetType);
            SetResultType(node, TypeWithState.Create(type, resultState));
            return null;
        }

        public override BoundNode VisitSizeOfOperator(BoundSizeOfOperator node)
        {
            var result = base.VisitSizeOfOperator(node);
            VisitTypeExpression(node.SourceType);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode VisitArgList(BoundArgList node)
        {
            var result = base.VisitArgList(node);
            Debug.Assert(node.Type.SpecialType == SpecialType.System_RuntimeArgumentHandle);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode VisitArgListOperator(BoundArgListOperator node)
        {
            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt, parameters: default, argsToParamsOpt: default, expanded: false);
            Debug.Assert(node.Type is null);
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode VisitLiteral(BoundLiteral node)
        {
            var result = base.VisitLiteral(node);

            Debug.Assert(!IsConditionalState);
            SetResultType(node, TypeWithState.Create(node.Type, node.Type?.CanContainNull() != false && node.ConstantValue?.IsNull == true ? NullableFlowState.MaybeNull : NullableFlowState.NotNull));

            if (node.ConstantValue?.IsBoolean == true)
            {
                Split();
                if (node.ConstantValue.BooleanValue)
                {
                    StateWhenFalse = UnreachableState();
                }
                else
                {
                    StateWhenTrue = UnreachableState();
                }
            }

            return result;
        }

        public override BoundNode VisitPreviousSubmissionReference(BoundPreviousSubmissionReference node)
        {
            var result = base.VisitPreviousSubmissionReference(node);
            Debug.Assert(node.WasCompilerGenerated);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode VisitHostObjectMemberReference(BoundHostObjectMemberReference node)
        {
            var result = base.VisitHostObjectMemberReference(node);
            Debug.Assert(node.WasCompilerGenerated);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode VisitPseudoVariable(BoundPseudoVariable node)
        {
            var result = base.VisitPseudoVariable(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode VisitRangeExpression(BoundRangeExpression node)
        {
            var result = base.VisitRangeExpression(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode VisitRangeVariable(BoundRangeVariable node)
        {
            VisitWithoutDiagnostics(node.Value);
            SetNotNullResult(node); // https://github.com/dotnet/roslyn/issues/29863 Need to review this
            return null;
        }

        public override BoundNode VisitLabel(BoundLabel node)
        {
            var result = base.VisitLabel(node);
            SetUnknownResultNullability(node);
            return result;
        }

        public override BoundNode VisitDynamicMemberAccess(BoundDynamicMemberAccess node)
        {
            var receiver = node.Receiver;
            VisitRvalue(receiver);
            _ = CheckPossibleNullReceiver(receiver);

            Debug.Assert(node.Type.IsDynamic());
            var result = TypeWithAnnotations.Create(node.Type);
            SetLvalueResultType(node, result);
            return null;
        }

        public override BoundNode VisitDynamicInvocation(BoundDynamicInvocation node)
        {
            var expr = node.Expression;
            VisitRvalue(expr);

            // If the expression was a MethodGroup, check nullability of receiver.
            var receiverOpt = (expr as BoundMethodGroup)?.ReceiverOpt;
            if (TryGetMethodGroupReceiverNullability(receiverOpt, out TypeWithState receiverType))
            {
                CheckPossibleNullReceiver(receiverOpt, receiverType, checkNullableValueType: false);
            }

            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt, parameters: default, argsToParamsOpt: default, expanded: false);
            Debug.Assert(node.Type.IsDynamic());
            Debug.Assert(node.Type.IsReferenceType);
            var result = TypeWithAnnotations.Create(node.Type, NullableAnnotation.Oblivious);
            SetLvalueResultType(node, result);
            return null;
        }

        public override BoundNode VisitEventAssignmentOperator(BoundEventAssignmentOperator node)
        {
            VisitRvalue(node.ReceiverOpt);
            Debug.Assert(!IsConditionalState);
            var receiverOpt = node.ReceiverOpt;
            var @event = node.Event;
            if (!@event.IsStatic)
            {
                @event = (EventSymbol)AsMemberOfType(ResultType.Type, @event);
                // https://github.com/dotnet/roslyn/issues/30598: Mark receiver as not null
                // after arguments have been visited, and only if the receiver has not changed.
                _ = CheckPossibleNullReceiver(receiverOpt);
                SetUpdatedSymbol(node, node.Event, @event);
            }
            VisitRvalue(node.Argument);
            // https://github.com/dotnet/roslyn/issues/31018: Check for delegate mismatch.
            SetNotNullResult(node); // https://github.com/dotnet/roslyn/issues/29969 Review whether this is the correct result
            return null;
        }

        public override BoundNode VisitDynamicObjectCreationExpression(BoundDynamicObjectCreationExpression node)
        {
            Debug.Assert(!IsConditionalState);
            var arguments = node.Arguments;
            var argumentResults = VisitArgumentsEvaluate(arguments, node.ArgumentRefKindsOpt, parameters: default, argsToParamsOpt: default, expanded: false);
            VisitObjectOrDynamicObjectCreation(node, arguments, argumentResults, node.InitializerExpressionOpt);
            return null;
        }

        public override BoundNode VisitObjectInitializerExpression(BoundObjectInitializerExpression node)
        {
            // Only reachable from bad expression. Otherwise handled in VisitObjectCreationExpression().
            // https://github.com/dotnet/roslyn/issues/35042: Do we need to analyze child expressions anyway for the public API?
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode VisitCollectionInitializerExpression(BoundCollectionInitializerExpression node)
        {
            // Only reachable from bad expression. Otherwise handled in VisitObjectCreationExpression().
            // https://github.com/dotnet/roslyn/issues/35042: Do we need to analyze child expressions anyway for the public API?
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode VisitDynamicCollectionElementInitializer(BoundDynamicCollectionElementInitializer node)
        {
            // Only reachable from bad expression. Otherwise handled in VisitObjectCreationExpression().
            // https://github.com/dotnet/roslyn/issues/35042: Do we need to analyze child expressions anyway for the public API?
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode VisitImplicitReceiver(BoundImplicitReceiver node)
        {
            var result = base.VisitImplicitReceiver(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode VisitAnonymousPropertyDeclaration(BoundAnonymousPropertyDeclaration node)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override BoundNode VisitNoPiaObjectCreationExpression(BoundNoPiaObjectCreationExpression node)
        {
            var result = base.VisitNoPiaObjectCreationExpression(node);
            SetResultType(node, TypeWithState.Create(node.Type, NullableFlowState.NotNull));
            return result;
        }

        public override BoundNode VisitNewT(BoundNewT node)
        {
            VisitObjectOrDynamicObjectCreation(node, ImmutableArray<BoundExpression>.Empty, ImmutableArray<VisitArgumentResult>.Empty, node.InitializerExpressionOpt);
            return null;
        }

        public override BoundNode VisitArrayInitialization(BoundArrayInitialization node)
        {
            var result = base.VisitArrayInitialization(node);
            SetNotNullResult(node);
            return result;
        }

        private void SetUnknownResultNullability(BoundExpression expression)
        {
            SetResultType(expression, TypeWithState.Create(expression.Type, default));
        }

        public override BoundNode VisitStackAllocArrayCreation(BoundStackAllocArrayCreation node)
        {
            var result = base.VisitStackAllocArrayCreation(node);
            Debug.Assert(node.Type is null || node.Type.IsPointerType() || node.Type.IsRefLikeType);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode VisitDynamicIndexerAccess(BoundDynamicIndexerAccess node)
        {
            var receiver = node.ReceiverOpt;
            VisitRvalue(receiver);
            // https://github.com/dotnet/roslyn/issues/30598: Mark receiver as not null
            // after indices have been visited, and only if the receiver has not changed.
            _ = CheckPossibleNullReceiver(receiver);
            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt, parameters: default, argsToParamsOpt: default, expanded: false);
            Debug.Assert(node.Type.IsDynamic());
            var result = TypeWithAnnotations.Create(node.Type, NullableAnnotation.Oblivious);
            SetLvalueResultType(node, result);
            return null;
        }

        private bool CheckPossibleNullReceiver(BoundExpression receiverOpt, bool checkNullableValueType = false)
        {
            return CheckPossibleNullReceiver(receiverOpt, ResultType, checkNullableValueType);
        }

        private bool CheckPossibleNullReceiver(BoundExpression receiverOpt, TypeWithState resultType, bool checkNullableValueType)
        {
            Debug.Assert(!this.IsConditionalState);
            bool reportedDiagnostic = false;
            if (receiverOpt != null && this.State.Reachable)
            {
                var resultTypeSymbol = resultType.Type;
                if (resultTypeSymbol is null)
                {
                    return false;
                }
#if DEBUG
                Debug.Assert(receiverOpt.Type is null || AreCloseEnough(receiverOpt.Type, resultTypeSymbol));
#endif
                if (!ReportPossibleNullReceiverIfNeeded(resultTypeSymbol, resultType.State, checkNullableValueType, receiverOpt.Syntax, out reportedDiagnostic))
                {
                    return reportedDiagnostic;
                }

                LearnFromNonNullTest(receiverOpt, ref this.State);
            }

            return reportedDiagnostic;
        }

        // Returns false if the type wasn't interesting
        private bool ReportPossibleNullReceiverIfNeeded(TypeSymbol type, NullableFlowState state, bool checkNullableValueType, SyntaxNode syntax, out bool reportedDiagnostic)
        {
            reportedDiagnostic = false;
            if (state.MayBeNull())
            {
                bool isValueType = type.IsValueType;
                if (isValueType && (!checkNullableValueType || !type.IsNullableTypeOrTypeParameter() || type.GetNullableUnderlyingType().IsErrorType()))
                {
                    return false;
                }

                ReportDiagnostic(isValueType ? ErrorCode.WRN_NullableValueTypeMayBeNull : ErrorCode.WRN_NullReferenceReceiver, syntax);
                reportedDiagnostic = true;
            }

            return true;
        }

        private void CheckExtensionMethodThisNullability(BoundExpression expr, Conversion conversion, ParameterSymbol parameter, TypeWithState result)
        {
            VisitArgumentConversionAndInboundAssignmentsAndPreConditions(
                conversionOpt: null,
                expr,
                conversion,
                parameter.RefKind,
                parameter,
                parameter.TypeWithAnnotations,
                GetParameterAnnotations(parameter),
                new VisitArgumentResult(new VisitResult(result),
                stateForLambda: default),
                extensionMethodThisArgument: true);
        }

        private static bool IsNullabilityMismatch(TypeWithAnnotations type1, TypeWithAnnotations type2)
        {
            // Note, when we are paying attention to nullability, we ignore oblivious mismatch.
            // See TypeCompareKind.ObliviousNullableModifierMatchesAny
            return type1.Equals(type2, TypeCompareKind.AllIgnoreOptions) &&
                !type1.Equals(type2, TypeCompareKind.AllIgnoreOptions & ~TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);
        }

        private static bool IsNullabilityMismatch(TypeSymbol type1, TypeSymbol type2)
        {
            // Note, when we are paying attention to nullability, we ignore oblivious mismatch.
            // See TypeCompareKind.ObliviousNullableModifierMatchesAny
            return type1.Equals(type2, TypeCompareKind.AllIgnoreOptions) &&
                !type1.Equals(type2, TypeCompareKind.AllIgnoreOptions & ~TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);
        }

        public override BoundNode VisitQueryClause(BoundQueryClause node)
        {
            var result = base.VisitQueryClause(node);
            SetNotNullResult(node); // https://github.com/dotnet/roslyn/issues/29863 Implement nullability analysis in LINQ queries
            return result;
        }

        public override BoundNode VisitNameOfOperator(BoundNameOfOperator node)
        {
            var result = base.VisitNameOfOperator(node);
            SetResultType(node, TypeWithState.Create(node.Type, NullableFlowState.NotNull));
            return result;
        }

        public override BoundNode VisitNamespaceExpression(BoundNamespaceExpression node)
        {
            var result = base.VisitNamespaceExpression(node);
            SetUnknownResultNullability(node);
            return result;
        }

        public override BoundNode VisitInterpolatedString(BoundInterpolatedString node)
        {
            var result = base.VisitInterpolatedString(node);
            SetResultType(node, TypeWithState.Create(node.Type, NullableFlowState.NotNull));
            return result;
        }

        public override BoundNode VisitStringInsert(BoundStringInsert node)
        {
            var result = base.VisitStringInsert(node);
            SetUnknownResultNullability(node);
            return result;
        }

        public override BoundNode VisitConvertedStackAllocExpression(BoundConvertedStackAllocExpression node)
        {
            var result = base.VisitConvertedStackAllocExpression(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode VisitDiscardExpression(BoundDiscardExpression node)
        {
            var result = TypeWithAnnotations.Create(node.Type);
            var rValueType = TypeWithState.ForType(node.Type);
            SetResult(node, rValueType, result);
            return null;
        }

        public override BoundNode VisitThrowExpression(BoundThrowExpression node)
        {
            VisitThrow(node.Expression);
            SetResultType(node, default);
            return null;
        }

        public override BoundNode VisitThrowStatement(BoundThrowStatement node)
        {
            VisitThrow(node.ExpressionOpt);
            return null;
        }

        private void VisitThrow(BoundExpression expr)
        {
            if (expr != null)
            {
                var result = VisitRvalueWithState(expr);
                // Cases:
                // null
                // null!
                // Other (typed) expression, including suppressed ones
                if (result.MayBeNull)
                {
                    ReportDiagnostic(ErrorCode.WRN_ThrowPossibleNull, expr.Syntax);
                }
            }
            SetUnreachable();
        }

        public override BoundNode VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            BoundExpression expr = node.Expression;
            if (expr == null)
            {
                return null;
            }
            var method = _methodSignatureOpt ?? (MethodSymbol)_symbol;
            TypeWithAnnotations elementType = InMethodBinder.GetIteratorElementTypeFromReturnType(compilation, RefKind.None,
                method.ReturnType, errorLocationNode: null, diagnostics: null).elementType;

            _ = VisitOptionalImplicitConversion(expr, elementType, useLegacyWarnings: false, trackMembers: false, AssignmentKind.Return);
            return null;
        }

        protected override void VisitCatchBlock(BoundCatchBlock node, ref LocalState finallyState)
        {
            TakeIncrementalSnapshot(node);
            if (node.Locals.Length > 0)
            {
                LocalSymbol local = node.Locals[0];
                if (local.DeclarationKind == LocalDeclarationKind.CatchVariable)
                {
                    int slot = GetOrCreateSlot(local);
                    if (slot > 0)
                        this.State[slot] = NullableFlowState.NotNull;
                }
            }

            if (node.ExceptionSourceOpt != null)
            {
                VisitWithoutDiagnostics(node.ExceptionSourceOpt);
            }

            base.VisitCatchBlock(node, ref finallyState);
        }

        public override BoundNode VisitLockStatement(BoundLockStatement node)
        {
            VisitRvalue(node.Argument);
            _ = CheckPossibleNullReceiver(node.Argument);
            VisitStatement(node.Body);
            return null;
        }

        public override BoundNode VisitAttribute(BoundAttribute node)
        {
            VisitArguments(node, node.ConstructorArguments, ImmutableArray<RefKind>.Empty, node.Constructor, argsToParamsOpt: node.ConstructorArgumentsToParamsOpt, expanded: node.ConstructorExpanded, invokedAsExtensionMethod: false);
            foreach (var assignment in node.NamedArguments)
            {
                Visit(assignment);
            }

            SetNotNullResult(node);
            return null;
        }

        public override BoundNode VisitExpressionWithNullability(BoundExpressionWithNullability node)
        {
            var typeWithAnnotations = TypeWithAnnotations.Create(node.Type, node.NullableAnnotation);
            SetResult(node.Expression, typeWithAnnotations.ToTypeWithState(), typeWithAnnotations);
            return null;
        }

        public override BoundNode VisitDeconstructValuePlaceholder(BoundDeconstructValuePlaceholder node)
        {
            SetNotNullResult(node);
            return null;
        }

        protected override string Dump(LocalState state)
        {
            if (!state.Reachable)
                return "unreachable";

            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;
            for (int i = 0; i < state.Capacity; i++)
            {
                if (nameForSlot(i) is string name)
                {
                    builder.Append(name);
                    builder.Append(state[i] == NullableFlowState.MaybeNull ? "?" : "!");
                }
            }

            return pooledBuilder.ToStringAndFree();

            string nameForSlot(int slot)
            {
                if (slot < 0)
                    return null;
                VariableIdentifier id = this.variableBySlot[slot];
                var name = id.Symbol?.Name;
                if (name == null)
                    return null;
                return nameForSlot(id.ContainingSlot) is string containingSlotName
                    ? containingSlotName + "." + name : name;
            }
        }

        protected override void Meet(ref LocalState self, ref LocalState other)
        {
            if (!self.Reachable)
                return;

            if (!other.Reachable)
            {
                self = other.Clone();
                return;
            }

            if (self.Capacity != other.Capacity)
            {
                Normalize(ref self);
                Normalize(ref other);
            }

            self.Meet(in other);
        }

        protected override bool Join(ref LocalState self, ref LocalState other)
        {
            if (!other.Reachable)
                return false;

            if (!self.Reachable)
            {
                self = other.Clone();
                return true;
            }

            if (self.Capacity != other.Capacity)
            {
                Normalize(ref self);
                Normalize(ref other);
            }

            return self.Join(in other);
        }

        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
#if REFERENCE_STATE
        internal class LocalState : ILocalState
#else
        internal struct LocalState : ILocalState
#endif
        {
            // The representation of a state is a bit vector.  We map false<->NotNull and true<->MayBeNull.
            // Slot 0 is used to represent whether the state is reachable (true) or not.
            private BitVector _state;

            private LocalState(BitVector state) => this._state = state;

            public bool Reachable => _state[0];

            public static LocalState ReachableState(int capacity)
            {
                if (capacity < 1)
                    capacity = 1;

                BitVector state = BitVector.Create(capacity);
                state[0] = true;
                return new LocalState(state);
            }

            public static LocalState UnreachableState
            {
                get
                {
                    BitVector state = BitVector.Create(1);
                    state[0] = false;
                    return new LocalState(state);
                }
            }

            public int Capacity => _state.Capacity;

            public void EnsureCapacity(int capacity) => _state.EnsureCapacity(capacity);

            public NullableFlowState this[int slot]
            {
                get => (slot < Capacity && this.Reachable && _state[slot]) ? NullableFlowState.MaybeNull : NullableFlowState.NotNull;

                // No states should be modified in unreachable code, as there is only one unreachable state.
                set => _ = !this.Reachable || (_state[slot] = (value == NullableFlowState.MaybeNull));
            }

            /// <summary>
            /// Produce a duplicate of this flow analysis state.
            /// </summary>
            /// <returns></returns>
            public LocalState Clone() => new LocalState(_state.Clone());

            public bool Join(in LocalState other) => _state.UnionWith(in other._state);

            public bool Meet(in LocalState other) => _state.IntersectWith(in other._state);

            internal string GetDebuggerDisplay()
            {
                var pooledBuilder = PooledStringBuilder.GetInstance();
                var builder = pooledBuilder.Builder;
                builder.Append(" ");
                for (int i = this.Capacity - 1; i >= 0; i--)
                    builder.Append(_state[i] ? '?' : '!');

                return pooledBuilder.ToStringAndFree();
            }
        }

#nullable enable
        private sealed class NullabilityInfoTypeComparer : IEqualityComparer<(NullabilityInfo info, TypeSymbol type)>
        {
            public static readonly NullabilityInfoTypeComparer Instance = new NullabilityInfoTypeComparer();

            public bool Equals((NullabilityInfo info, TypeSymbol type) x, (NullabilityInfo info, TypeSymbol type) y)
            {
                return x.info.Equals(y.info) &&
                       SymbolEqualityComparer.ConsiderEverything.Equals(x.type, y.type);
            }

            public int GetHashCode((NullabilityInfo info, TypeSymbol type) obj)
            {
                return obj.GetHashCode();
            }
        }

        private sealed class ExpressionAndSymbolEqualityComparer : IEqualityComparer<(BoundNode expr, Symbol sym)>
        {
            internal static readonly ExpressionAndSymbolEqualityComparer Instance = new ExpressionAndSymbolEqualityComparer();

            private ExpressionAndSymbolEqualityComparer() { }

            public bool Equals((BoundNode expr, Symbol sym) x, (BoundNode expr, Symbol sym) y)
            {
                Debug.Assert(x is ({ }, { }) && y is ({ }, { }));

                // We specifically use reference equality for the symbols here because the BoundNode should be immutable.
                // We should be storing and retrieving the exact same instance of the symbol, not just an "equivalent"
                // symbol.
                return x.expr == y.expr && (object)x.sym == y.sym;
            }

            public int GetHashCode((BoundNode expr, Symbol sym) obj)
            {
                Debug.Assert(obj is ({ }, { }));
                return Hash.Combine(obj.expr, obj.sym.GetHashCode());
            }
        }
    }
}
