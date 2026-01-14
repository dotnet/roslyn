// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
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
    internal sealed partial class NullableWalker
        : LocalDataFlowPass<NullableWalker.LocalState, NullableWalker.LocalFunctionState>
    {
        /// <summary>
        /// Nullable analysis data for methods, parameter default values, and attributes
        /// stored on the Compilation during testing only.
        /// The key is a symbol for methods or parameters, and syntax for attributes.
        /// </summary>
        internal sealed class NullableAnalysisData
        {
            internal readonly int MaxRecursionDepth;
            internal readonly ConcurrentDictionary<object, NullableWalker.Data> Data;

            internal NullableAnalysisData(int maxRecursionDepth = -1)
            {
                MaxRecursionDepth = maxRecursionDepth;
                Data = new ConcurrentDictionary<object, NullableWalker.Data>();
            }
        }

        /// <summary>
        /// Additional info for getter null resilience analysis.
        /// When this value is passed down through 'Analyze()', it means we are in the process of inferring the nullable annotation of 'field'.
        /// The 'assumedAnnotation' should be used as the field's nullable annotation for this analysis pass.
        /// See https://github.com/dotnet/csharplang/blob/229406aa6dc51c1e37b98e90eb868d979ec6d195/proposals/csharp-14.0/field-keyword.md#nullability-of-the-backing-field
        /// </summary>
        /// <param name="assumedAnnotation">Indicates whether the *not-annotated* pass or the *annotated* pass is being performed.</param>
        internal readonly struct GetterNullResilienceData(SynthesizedBackingFieldSymbol field, NullableAnnotation assumedAnnotation)
        {
            public readonly SynthesizedBackingFieldSymbol field = field;
            public readonly NullableAnnotation assumedAnnotation = assumedAnnotation;

            public void Deconstruct(out SynthesizedBackingFieldSymbol field, out NullableAnnotation assumedAnnotation)
            {
                field = this.field;
                assumedAnnotation = this.assumedAnnotation;
            }
        }

        /// <summary>
        /// Used to copy variable slots and types from the NullableWalker for the containing method
        /// or lambda to the NullableWalker created for a nested lambda or local function.
        /// </summary>
        internal sealed class VariableState
        {
            // Consider referencing the Variables instance directly from the original NullableWalker
            // rather than cloning. (Items are added to the collections but never replaced so the
            // collections are lazily populated but otherwise immutable. We'd probably want a
            // clone when analyzing from speculative semantic model though.)
            internal readonly VariablesSnapshot Variables;

            // The nullable state of all variables captured at the point where the function or lambda appeared.
            internal readonly LocalStateSnapshot VariableNullableStates;

            internal VariableState(VariablesSnapshot variables, LocalStateSnapshot variableNullableStates)
            {
                Debug.Assert(variables.Id == variableNullableStates.Id);
                Variables = variables;
                VariableNullableStates = variableNullableStates;
            }
        }

        /// <summary>
        /// Data recorded for a particular analysis run.
        /// </summary>
        internal readonly struct Data
        {
            /// <summary>
            /// Number of entries tracked during analysis.
            /// </summary>
            internal readonly int TrackedEntries;

            /// <summary>
            /// True if analysis was required; false if analysis was optional and results dropped.
            /// </summary>
            internal readonly bool RequiredAnalysis;

            internal Data(int trackedEntries, bool requiredAnalysis)
            {
                TrackedEntries = trackedEntries;
                RequiredAnalysis = requiredAnalysis;
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

            // For lambda expressions, we save the state when the lambda is analyzed,
            // so we can use it when analyzing the lambda body as part of a delegate conversion.
            public readonly Optional<LocalState> StateForLambda;

            // For expressions that contain nested expressions (such as collection expressions),
            // we store the results of visiting those nested expressions.
            // Note: we cannot use ImmutableArray<VisitResult> because that yields a TypeLoad exception on .NET Framework.
            public readonly VisitResult[]? NestedVisitResults;

            public VisitResult(TypeWithState rValueType, TypeWithAnnotations lValueType)
            {
                RValueType = rValueType;
                LValueType = lValueType;
                // https://github.com/dotnet/roslyn/issues/34993: Doesn't hold true for Tuple_Assignment_10. See if we can make it hold true
                //Debug.Assert((RValueType.Type is null && LValueType.TypeSymbol is null) ||
                //             RValueType.Type.Equals(LValueType.TypeSymbol, TypeCompareKind.ConsiderEverything | TypeCompareKind.AllIgnoreOptions));
            }

            public VisitResult(TypeWithState rValueType, TypeWithAnnotations lValueType, Optional<LocalState> stateForLambda)
                : this(rValueType, lValueType)
            {
                StateForLambda = stateForLambda;
            }

            public VisitResult(TypeSymbol? type, NullableAnnotation annotation, NullableFlowState state)
            {
                RValueType = TypeWithState.Create(type, state);
                LValueType = TypeWithAnnotations.Create(type, annotation);
                Debug.Assert(TypeSymbol.Equals(RValueType.Type, LValueType.Type, TypeCompareKind.ConsiderEverything));
            }

            /// <summary>
            /// For expressions whose constituent parts contribute to method type inference (such as collection expressions),
            /// we need to keep track of the visit results for those parts.
            /// </summary>
            public VisitResult(TypeWithState rValueType, TypeWithAnnotations lValueType, VisitResult[] nestedVisitResults)
                : this(rValueType, lValueType)
            {
                NestedVisitResults = nestedVisitResults;
            }

            internal VisitResult WithLValueType(TypeWithAnnotations lvalueType)
            {
                if (NestedVisitResults is not null)
                {
                    Debug.Assert(!StateForLambda.HasValue);
                    return new VisitResult(RValueType, lvalueType, NestedVisitResults);
                }

                return new VisitResult(RValueType, lvalueType, StateForLambda);
            }

            internal string GetDebuggerDisplay()
            {
                if (NestedVisitResults is null)
                {
                    return $"{{LValue: {LValueType.GetDebuggerDisplay()}, RValue: {RValueType.GetDebuggerDisplay()}}}";
                }

                return $$"""Collection: {{string.Join(", ", NestedVisitResults.Select(r => r.GetDebuggerDisplay()))}}""";
            }
        }

        private Variables _variables;

        /// <summary>
        /// Binder for symbol being analyzed.
        /// </summary>
        private readonly Binder _binder;

        /// <summary>
        /// Conversions with nullability and unknown matching any.
        /// </summary>
        private readonly Conversions _conversions;

        /// <summary>
        /// 'true' if non-nullable member warnings should be issued at return points.
        /// One situation where this is 'false' is when we are analyzing field initializers and there is a constructor symbol in the type.
        /// </summary>
        private readonly bool _useConstructorExitWarnings;

        /// <summary>
        /// Non-null if we are performing the 'null-resilience' analysis of a getter which uses the 'field' keyword.
        /// In this case, the inferred nullable annotation of the backing field must not be used, as we are currently in the process of inferring it.
        /// </summary>
        private readonly GetterNullResilienceData? _getterNullResilienceData;

        /// <summary>
        /// If true, the parameter types and nullability from _delegateInvokeMethod is used for
        /// initial parameter state. If false, the signature of CurrentSymbol is used instead.
        /// </summary>
        private bool _useDelegateInvokeParameterTypes;

        /// <summary>
        /// If true, the return type and nullability from _delegateInvokeMethod is used.
        /// If false, the signature of CurrentSymbol is used instead.
        /// </summary>
        private bool _useDelegateInvokeReturnType;

        /// <summary>
        /// Method signature used for return or parameter types. Distinct from CurrentSymbol signature
        /// when CurrentSymbol is a lambda and type is inferred from MethodTypeInferrer.
        /// </summary>
        private MethodSymbol? _delegateInvokeMethod;

        /// <summary>
        /// Return statements and the result types from analyzing the returned expressions. Used when inferring lambda return type in MethodTypeInferrer.
        /// </summary>
        private ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)>? _returnTypesOpt;

        /// <summary>
        /// Invalid type, used only to catch Visit methods that do not set
        /// _result.Type. See VisitExpressionWithoutStackGuard.
        /// </summary>
        private static readonly TypeWithState _invalidType = TypeWithState.Create(new UnsupportedMetadataTypeSymbol(), NullableFlowState.NotNull);

        /// <summary>
        /// Contains the map of expressions to inferred nullabilities and types used by the optional rewriter phase of the
        /// compiler.
        /// </summary>
        private readonly ImmutableDictionary<BoundExpression, (NullabilityInfo Info, TypeSymbol? Type)>.Builder? _analyzedNullabilityMapOpt;

        /// <summary>
        /// Manages creating snapshots of the walker as appropriate. Null if we're not taking snapshots of
        /// this walker.
        /// </summary>
        private readonly SnapshotManager.Builder? _snapshotBuilderOpt;

        // https://github.com/dotnet/roslyn/issues/35043: remove this when all expression are supported
        private bool _disableNullabilityAnalysis;

        /// <summary>
        /// State of method group receivers, used later when analyzing the conversion to a delegate.
        /// (Could be replaced by _analyzedNullabilityMapOpt if that map is always available.)
        /// </summary>
        private PooledDictionary<BoundExpression, TypeWithState>? _methodGroupReceiverMapOpt;

        private PooledDictionary<BoundValuePlaceholderBase, (BoundExpression? Replacement, VisitResult Result)>? _resultForPlaceholdersOpt;

        /// <summary>
        /// Variables instances for each lambda or local function defined within the analyzed region.
        /// </summary>
        private PooledDictionary<MethodSymbol, Variables>? _nestedFunctionVariables;
#if DEBUG
        private bool _completingTargetTypedExpression;
#endif
        private PooledDictionary<BoundExpression, Func<TypeWithAnnotations, TypeWithState>>? _targetTypedAnalysisCompletionOpt;

        /// <summary>
        /// Map from a target-typed expression (such as a target-typed conditional, switch or new) to the delegate
        /// that completes analysis once the target type is known.
        /// The delegate is invoked by <see cref="VisitConversion(BoundConversion, BoundExpression, Conversion, TypeWithAnnotations, TypeWithState, bool, bool, bool, AssignmentKind, ParameterSymbol, bool, bool, bool, bool, Optional&lt;LocalState&gt;,bool, Location, ArrayBuilder&lt;VisitResult&gt;)"/>.
        /// </summary>
        private PooledDictionary<BoundExpression, Func<TypeWithAnnotations, TypeWithState>> TargetTypedAnalysisCompletion
            => _targetTypedAnalysisCompletionOpt ??= PooledDictionary<BoundExpression, Func<TypeWithAnnotations, TypeWithState>>.GetInstance();

        /// <summary>
        /// True if we're analyzing speculative code. This turns off some initialization steps
        /// that would otherwise be taken.
        /// </summary>
        private readonly bool _isSpeculative;

        /// <summary>
        /// True if this walker was created using an initial state.
        /// </summary>
        private readonly bool _hasInitialState;

        private readonly MethodSymbol? _baseOrThisInitializer;

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

        private void SetResultType(BoundExpression? expression, TypeWithState type, bool updateAnalyzedNullability = true)
        {
            SetResult(expression, resultType: type, lvalueType: type.ToTypeWithAnnotations(compilation), updateAnalyzedNullability: updateAnalyzedNullability);
        }

        private void SetAnalyzedNullability(BoundExpression? expression, TypeWithState type)
        {
            SetAnalyzedNullability(expression, resultType: type, lvalueType: type.ToTypeWithAnnotations(compilation));
        }

        /// <summary>
        /// Force the inference of the LValueResultType from ResultType.
        /// </summary>
        private void UseRvalueOnly(BoundExpression? expression)
        {
            VisitResult visitResult = _visitResult.WithLValueType(ResultType.ToTypeWithAnnotations(compilation));
            SetResult(expression, visitResult, updateAnalyzedNullability: true, isLvalue: false);
        }

        private TypeWithAnnotations LvalueResultType
        {
            get => _visitResult.LValueType;
        }

        private void SetLvalueResultType(BoundExpression? expression, TypeWithAnnotations type)
        {
            SetResult(expression, resultType: type.ToTypeWithState(), lvalueType: type);
        }

        /// <summary>
        /// Force the inference of the ResultType from LValueResultType.
        /// </summary>
        private void UseLvalueOnly(BoundExpression? expression)
        {
            SetResult(expression, LvalueResultType.ToTypeWithState(), LvalueResultType, isLvalue: true);
        }

        private void SetInvalidResult() => SetResult(expression: null, _invalidType, _invalidType.ToTypeWithAnnotations(compilation), updateAnalyzedNullability: false);

        private void SetResult(BoundExpression? expression, TypeWithState resultType, TypeWithAnnotations lvalueType, bool updateAnalyzedNullability = true, bool? isLvalue = null)
        {
            SetResult(expression, new VisitResult(resultType, lvalueType), updateAnalyzedNullability, isLvalue);
        }

        private void SetResult(BoundExpression? expression, VisitResult visitResult, bool updateAnalyzedNullability, bool? isLvalue)
        {
            // As a general rule, the state should only be conditional for expressions of type bool,
            // although there are a few exceptions.
            Debug.Assert(TypeAllowsConditionalState(visitResult.RValueType.Type)
                || !IsConditionalState
                || expression is BoundTypeExpression);

            _visitResult = visitResult;
            if (updateAnalyzedNullability)
            {
                SetAnalyzedNullability(expression, _visitResult, isLvalue);
            }
        }

        private void SetAnalyzedNullability(BoundExpression? expression, TypeWithState resultType, TypeWithAnnotations lvalueType, bool? isLvalue = null)
        {
            SetAnalyzedNullability(expression, new VisitResult(resultType, lvalueType), isLvalue);
        }

        /// <summary>
        /// Sets the analyzed nullability of the expression to be the given result.
        /// </summary>
        private void SetAnalyzedNullability(BoundExpression? expr, VisitResult result, bool? isLvalue = null)
        {
            if (expr == null
                // BoundExpressionWithNullability is not produced by the binder but is used within nullability analysis to pass information to internal components.
                || expr.Kind == BoundKind.ExpressionWithNullability
                || _disableNullabilityAnalysis)
            {
                return;
            }

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
        private PooledDictionary<object, PlaceholderLocal>? _placeholderLocalsOpt;

        /// <summary>
        /// For methods with annotations, we'll need to visit the arguments twice.
        /// Once for diagnostics and once for result state (but disabling diagnostics).
        /// </summary>
        private bool _disableDiagnostics = false;

        /// <summary>
        /// Whether we are going to read the currently visited expression.
        /// </summary>
        private bool _expressionIsRead = true;

        /// <summary>
        /// Used to allow <see cref="MakeSlot(BoundExpression)"/> to substitute the correct slot for a <see cref="BoundConditionalReceiver"/> when
        /// it's encountered.
        /// </summary>
        private int _lastConditionalAccessSlot = -1;

        private bool IsAnalyzingAttribute => methodMainNode.Kind == BoundKind.Attribute;

        protected override void Free()
        {
            AssertNoPlaceholderReplacements();

            _nestedFunctionVariables?.Free();
            _resultForPlaceholdersOpt?.Free();
            _methodGroupReceiverMapOpt?.Free();
            _placeholderLocalsOpt?.Free();
            _variables.Free();
            Debug.Assert(_targetTypedAnalysisCompletionOpt is null or { Count: 0 });
            _targetTypedAnalysisCompletionOpt?.Free();
            base.Free();
        }

        private NullableWalker(
            CSharpCompilation compilation,
            Symbol? symbol,
            bool useConstructorExitWarnings,
            GetterNullResilienceData? getterNullResilienceData,
            bool useDelegateInvokeParameterTypes,
            bool useDelegateInvokeReturnType,
            MethodSymbol? delegateInvokeMethodOpt,
            BoundNode node,
            Binder binder,
            Conversions conversions,
            Variables? variables,
            MethodSymbol? baseOrThisInitializer,
            ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)>? returnTypesOpt,
            ImmutableDictionary<BoundExpression, (NullabilityInfo, TypeSymbol?)>.Builder? analyzedNullabilityMapOpt,
            SnapshotManager.Builder? snapshotBuilderOpt,
            bool isSpeculative = false)
            : base(compilation, symbol, node, EmptyStructTypeCache.CreatePrecise(), trackUnassignments: true)
        {
            Debug.Assert(!TrackingRegions);
            Debug.Assert(!useDelegateInvokeParameterTypes || delegateInvokeMethodOpt is object);
            Debug.Assert(baseOrThisInitializer is null or { MethodKind: MethodKind.Constructor });

            _variables = variables ?? Variables.Create(symbol);
            _binder = binder;
            _conversions = (Conversions)conversions.WithNullability(true);
            _useConstructorExitWarnings = useConstructorExitWarnings;
            _getterNullResilienceData = getterNullResilienceData;
            _useDelegateInvokeParameterTypes = useDelegateInvokeParameterTypes;
            _useDelegateInvokeReturnType = useDelegateInvokeReturnType;
            _delegateInvokeMethod = delegateInvokeMethodOpt;
            _analyzedNullabilityMapOpt = analyzedNullabilityMapOpt;
            _returnTypesOpt = returnTypesOpt;
            _snapshotBuilderOpt = snapshotBuilderOpt;
            _isSpeculative = isSpeculative;
            _hasInitialState = variables is { };
            _baseOrThisInitializer = baseOrThisInitializer;
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

        protected override void EnsureSufficientExecutionStack(int recursionDepth)
        {
            if (recursionDepth > StackGuard.MaxUncheckedRecursionDepth &&
                compilation.TestOnlyCompilationData is NullableAnalysisData { MaxRecursionDepth: var depth } &&
                depth > 0 &&
                recursionDepth > depth)
            {
                throw new InsufficientExecutionStackException();
            }

            base.EnsureSufficientExecutionStack(recursionDepth);
        }

        protected override bool ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()
        {
            return true;
        }

        protected override bool TryGetVariable(VariableIdentifier identifier, out int slot)
        {
            return _variables.TryGetValue(identifier, out slot);
        }

        protected override int AddVariable(VariableIdentifier identifier)
        {
            return _variables.Add(identifier);
        }

        [Conditional("DEBUG")]
        private void AssertNoPlaceholderReplacements()
        {
            if (_resultForPlaceholdersOpt is not null)
            {
                Debug.Assert(_resultForPlaceholdersOpt.Count == 0);
            }
        }

        private void AddPlaceholderReplacement(BoundValuePlaceholderBase placeholder, BoundExpression? expression, VisitResult result)
        {
#if DEBUG
            Debug.Assert(AreCloseEnough(placeholder.Type, result.RValueType.Type));
            Debug.Assert(expression != null || placeholder.Kind == BoundKind.InterpolatedStringArgumentPlaceholder);
#endif

            _resultForPlaceholdersOpt ??= PooledDictionary<BoundValuePlaceholderBase, (BoundExpression? Replacement, VisitResult Result)>.GetInstance();
            _resultForPlaceholdersOpt.Add(placeholder, (expression, result));
        }

        private void RemovePlaceholderReplacement(BoundValuePlaceholderBase placeholder)
        {
            Debug.Assert(_resultForPlaceholdersOpt is { });
            bool removed = _resultForPlaceholdersOpt.Remove(placeholder);
            Debug.Assert(removed);
        }

        [Conditional("DEBUG")]
        private static void AssertPlaceholderAllowedWithoutRegistration(BoundValuePlaceholderBase placeholder)
        {
            Debug.Assert(placeholder is { });

            switch (placeholder.Kind)
            {
                case BoundKind.DeconstructValuePlaceholder:
                case BoundKind.InterpolatedStringHandlerPlaceholder:
                case BoundKind.InterpolatedStringArgumentPlaceholder:
                case BoundKind.ObjectOrCollectionValuePlaceholder:
                case BoundKind.AwaitableValuePlaceholder:
                    return;

                case BoundKind.ImplicitIndexerValuePlaceholder:
                    // Since such placeholders are always not-null, we skip adding them to the map.
                    return;

                default:
                    // Newer placeholders are expected to follow placeholder discipline, namely that
                    // they must be added to the map before they are visited, then removed.
                    throw ExceptionUtilities.UnexpectedValue(placeholder.Kind);
            }
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

                if (_symbol.TryGetInstanceExtensionParameter(out ParameterSymbol? extensionParameter))
                {
                    EnterParameter(extensionParameter, extensionParameter.TypeWithAnnotations);
                }

                makeNotNullMembersMaybeNull();
                // We need to create a snapshot even of the first node, because we want to have the state of the initial parameters.
                _snapshotBuilderOpt?.TakeIncrementalSnapshot(methodMainNode, State);
            }

            ImmutableArray<PendingBranch> pendingReturns = base.Scan(ref badRegion);
            if ((_symbol as MethodSymbol)?.IsConstructor() != true || _useConstructorExitWarnings)
            {
                EnforceDoesNotReturn(syntaxOpt: null);
                enforceMemberNotNull(syntaxOpt: null, this.State);
                EnforceParameterNotNullOnExit(syntaxOpt: null, this.State);

                foreach (var pendingReturn in pendingReturns)
                {
                    enforceMemberNotNull(syntaxOpt: pendingReturn.Branch.Syntax, pendingReturn.State);

                    if (pendingReturn.Branch is BoundReturnStatement returnStatement)
                    {
                        EnforceParameterNotNullOnExit(returnStatement.Syntax, pendingReturn.State);
                        EnforceNotNullWhenForPendingReturn(pendingReturn, returnStatement);
                        EnforceMemberNotNullWhenForPendingReturn(pendingReturn, returnStatement);
                    }
                }
            }

            return pendingReturns;

            void enforceMemberNotNull(SyntaxNode? syntaxOpt, LocalState state)
            {
                if (!state.Reachable)
                {
                    return;
                }

                var method = _symbol as MethodSymbol;
                if (method is object)
                {
                    if (method.IsConstructor())
                    {
                        Debug.Assert(_useConstructorExitWarnings);
                        var thisSlot = 0;
                        if (method.TryGetThisParameter(out var thisParameter) && thisParameter is object)
                        {
                            thisSlot = GetOrCreateSlot(thisParameter);
                        }
                        var exitLocation = method is SynthesizedPrimaryConstructor || method.DeclaringSyntaxReferences.IsEmpty ? null : method.TryGetFirstLocation();
                        bool constructorEnforcesRequiredMembers = method.ShouldCheckRequiredMembers();

                        // Required properties can be attributed MemberNotNull, indicating that if the property is set, the field will be set as well.
                        // If we're enforcing required members (ie, the constructor is not attributed with SetsRequiredMembers), we also want to
                        // not warn for members named in such attributes.
                        var membersWithStateEnforcedByRequiredMembers = constructorEnforcesRequiredMembers
                            ? method.ContainingType.GetMembersUnordered().SelectManyAsArray(
                                predicate: member => member is PropertySymbol { IsRequired: true },
                                selector: member =>
                                {
                                    var property = (PropertySymbol)member;
                                    return property.SetMethod?.NotNullMembers ?? property.NotNullMembers;
                                })
                            : ImmutableArray<string>.Empty;

                        var alreadyWarnedMembers = PooledHashSet<Symbol>.GetInstance();
                        foreach (var member in method.ContainingType.GetMembersUnordered())
                        {
                            // If this constructor has `SetsRequiredMembers`, then we need to check the state of _all_ required properties, regardless of whether they are auto-properties or not.
                            // For auto-properties, `GetMembersUnordered()` will return the backing field, and `checkStateOnConstructorExit` will follow that to the property itself, so we only need
                            // to force property analysis if the member is required and _does not_ have a backing field.
                            var shouldForcePropertyAnalysis = !constructorEnforcesRequiredMembers && member is not SourcePropertySymbolBase { BackingField: not null } && member.IsRequired();
                            checkMemberStateOnConstructorExit(method, member, state, thisSlot, exitLocation, membersWithStateEnforcedByRequiredMembers, forcePropertyAnalysis: shouldForcePropertyAnalysis);
                        }

                        // If this constructor is adding `SetsRequiredMembers` and the base or this constructor did not have it, we need
                        // to restore the nullable warnings for all members that were not initialized in this constructor, including those
                        // from base types that were expected to have been initialized by the consumer at the construction site.

                        var chainedConstructorEnforcesRequiredMembers = GetBaseOrThisInitializer()?.ShouldCheckRequiredMembers() ?? false;

                        if (chainedConstructorEnforcesRequiredMembers && !constructorEnforcesRequiredMembers && method.ContainingType.BaseTypeNoUseSiteDiagnostics is { } baseType)
                        {
                            // Members of the current type were checked above. We need to grab all the required members from the base
                            // type and enforce them as well. We don't need to check the non-required members: those warnings would have
                            // been reported in constructor of the type that defined them.
                            foreach (var (_, member) in baseType.AllRequiredMembers)
                            {
                                checkMemberStateOnConstructorExit(method, member, state, thisSlot, exitLocation, membersWithStateEnforcedByRequiredMembers: ImmutableArray<string>.Empty, forcePropertyAnalysis: true);
                            }
                        }

                        alreadyWarnedMembers.Free();
                    }
                    else
                    {
                        do
                        {
                            foreach (var memberName in method.NotNullMembers)
                            {
                                EnforceMemberNotNullOnMember(syntaxOpt, state, method, memberName);
                            }

                            method = method.OverriddenMethod;
                        }
                        while (method != null);
                    }
                }
            }

            void checkMemberStateOnConstructorExit(MethodSymbol constructor, Symbol member, LocalState state, int thisSlot, Location? exitLocation, ImmutableArray<string> membersWithStateEnforcedByRequiredMembers, bool forcePropertyAnalysis)
            {
                var isStatic = !constructor.RequiresInstanceReceiver();
                if (member.IsStatic != isStatic)
                {
                    return;
                }

                // This is not required for correctness, but in the case where the member has
                // an initializer, we know we've assigned to the member and
                // have given any applicable warnings about a bad value going in.
                // Therefore we skip this check when the member has an initializer to reduce noise.
                if (HasInitializer(member) && constructor.IncludeFieldInitializersInBody())
                {
                    return;
                }

                TypeWithAnnotations symbolType;
                FieldSymbol? field;
                Symbol symbol;
                switch (member)
                {
                    case FieldSymbol f:
                        symbolType = GetTypeOrReturnTypeWithAnnotations(f);
                        field = f;
                        symbol = (Symbol?)(f.AssociatedSymbol as PropertySymbol) ?? f;
                        break;
                    case EventSymbol e:
                        symbolType = e.TypeWithAnnotations;
                        field = e.AssociatedField;
                        symbol = e;
                        if (field is null)
                        {
                            return;
                        }
                        break;
                    case PropertySymbol p when forcePropertyAnalysis:
                        symbolType = p.TypeWithAnnotations;
                        field = null;
                        symbol = p;
                        break;
                    default:
                        return;
                }
                if (field?.IsConst ?? false)
                {
                    return;
                }
                if (symbolType.Type.IsValueType || symbolType.Type.IsErrorType())
                {
                    return;
                }

                if ((symbol.IsRequired() || membersWithStateEnforcedByRequiredMembers.Contains(symbol.Name)) && constructor.ShouldCheckRequiredMembers())
                {
                    return;
                }

                // If 'field' keyword is explicitly used by 'symbol', then use FlowAnalysisAnnotations from the backing field.
                // Otherwise, use the FlowAnalysisAnnotations from the user-declared symbol (property or ordinary field).
                var usesFieldKeyword = symbol is SourcePropertySymbolBase { UsesFieldKeyword: true };
                var annotations = usesFieldKeyword ? field!.FlowAnalysisAnnotations : symbol.GetFlowAnalysisAnnotations();
                if ((annotations & FlowAnalysisAnnotations.AllowNull) != 0)
                {
                    // We assume that if a member has AllowNull then the user
                    // does not care that we exit at a point where the member might be null.
                    return;
                }
                symbolType = ApplyUnconditionalAnnotations(symbolType, annotations);
                if (!symbolType.NullableAnnotation.IsNotAnnotated())
                {
                    return;
                }
                var slot = GetOrCreateSlot(symbol, thisSlot);
                if (slot < 0)
                {
                    return;
                }

                var memberState = GetState(ref state, slot);
                var badState = symbolType.Type.IsPossiblyNullableReferenceTypeTypeParameter() && (annotations & FlowAnalysisAnnotations.NotNull) == 0
                    ? NullableFlowState.MaybeDefault
                    : NullableFlowState.MaybeNull;
                if (memberState >= badState) // is 'memberState' as bad as or worse than 'badState'?
                {
                    var errorCode = usesFieldKeyword ? ErrorCode.WRN_UninitializedNonNullableBackingField : ErrorCode.WRN_UninitializedNonNullableField;
                    var info = new CSDiagnosticInfo(errorCode, new object[] { symbol.Kind.Localize(), symbol.Name }, ImmutableArray<Symbol>.Empty, additionalLocations: symbol.Locations);
                    Diagnostics.Add(info, exitLocation ?? symbol.GetFirstLocationOrNone());
                }
            }

            void makeNotNullMembersMaybeNull()
            {
                if (_symbol is MethodSymbol method)
                {
                    if (method.IsConstructor())
                    {
                        foreach (var member in getMembersNeedingDefaultInitialState())
                        {
                            if (member.IsStatic != method.IsStatic)
                            {
                                continue;
                            }

                            var memberToInitialize = member;
                            switch (member)
                            {
                                case PropertySymbol { IsRequired: true }:
                                    break;
                                case PropertySymbol:
                                    // skip any manually implemented non-required properties.
                                    continue;
                                case FieldSymbol { OriginalDefinition: SynthesizedPrimaryConstructorParameterBackingFieldSymbol }:
                                    // Skip primary constructor capture fields, compiler initializes them with parameters' values
                                    continue;
                                case FieldSymbol { IsConst: true }:
                                    continue;
                                case FieldSymbol { AssociatedSymbol: SourcePropertySymbolBase { UsesFieldKeyword: false } prop }:
                                    // this is a property where assigning 'default' causes us to simply update
                                    // the state to the output state of the property
                                    // thus we skip setting an initial state for it here
                                    if (IsPropertyOutputMoreStrictThanInput(prop))
                                    {
                                        continue;
                                    }

                                    // We want to initialize auto-property state to the default state, but not computed properties.
                                    memberToInitialize = prop;
                                    break;
                                default:
                                    break;
                            }
                            var memberSlot = GetSlotForMemberPostCondition(memberToInitialize);
                            if (memberSlot > 0)
                            {
                                var type = GetTypeOrReturnTypeWithAnnotations(memberToInitialize);
                                if (!type.NullableAnnotation.IsOblivious())
                                {
                                    SetState(ref this.State, memberSlot, type.Type.IsPossiblyNullableReferenceTypeTypeParameter() ? NullableFlowState.MaybeDefault : NullableFlowState.MaybeNull);
                                }
                            }
                        }
                    }
                    else
                    {
                        do
                        {
                            MakeMembersMaybeNull(method, method.NotNullMembers);
                            MakeMembersMaybeNull(method, method.NotNullWhenTrueMembers);
                            MakeMembersMaybeNull(method, method.NotNullWhenFalseMembers);
                            method = method.OverriddenMethod;
                        }
                        while (method != null);
                    }
                }

                return;

                ImmutableArray<Symbol> getMembersNeedingDefaultInitialState()
                {
                    if (_hasInitialState)
                    {
                        return ImmutableArray<Symbol>.Empty;
                    }

                    bool includeCurrentTypeRequiredMembers = true;
                    bool includeBaseRequiredMembers = true;
                    bool hasThisConstructorInitializer = false;

                    if (method is SourceMemberMethodSymbol { SyntaxNode: ConstructorDeclarationSyntax { Initializer: { RawKind: var initializerKind } } })
                    {
                        var baseOrThisInitializer = GetBaseOrThisInitializer();
                        // If there's an error in the base or this initializer, presume that we should set all required members to default.
                        includeBaseRequiredMembers = baseOrThisInitializer?.ShouldCheckRequiredMembers() ?? true;
                        if (initializerKind == (int)SyntaxKind.ThisConstructorInitializer)
                        {
                            hasThisConstructorInitializer = true;
                            // If we chained to a `this` constructor, a SetsRequiredMembers attribute applies to both the current type's required members and the base type's required members.
                            includeCurrentTypeRequiredMembers = includeBaseRequiredMembers;
                        }
                        else if (initializerKind == (int)SyntaxKind.BaseConstructorInitializer)
                        {
                            // If we chained to a `base` constructor, a SetsRequiredMembers attribute applies to the base type's required members only, and the current type's required members
                            // are not assumed to be initialized.
                            includeCurrentTypeRequiredMembers = true;
                        }
                    }

                    // Pre-C# 11, we don't use a default initial state for value type instance constructors without `: this()`
                    // because any usages of uninitialized fields will get definite assignment errors anyway.
                    if (!hasThisConstructorInitializer
                        && (!method.ContainingType.IsValueType
                            || method.IsStatic
                            || compilation.IsFeatureEnabled(MessageID.IDS_FeatureAutoDefaultStructs)))
                    {
                        return membersToBeInitialized(method.ContainingType, includeAllMembers: true, includeCurrentTypeRequiredMembers, includeBaseRequiredMembers);
                    }

                    // We want to presume all required members of the type are uninitialized, and in addition we want to set all fields to
                    // default if we can get to this constructor by doing so (ie, : this() in a value type).
                    return membersToBeInitialized(method.ContainingType, includeAllMembers: method.IncludeFieldInitializersInBody(), includeCurrentTypeRequiredMembers, includeBaseRequiredMembers);

                    static ImmutableArray<Symbol> membersToBeInitialized(NamedTypeSymbol containingType, bool includeAllMembers, bool includeCurrentTypeRequiredMembers, bool includeBaseRequiredMembers)
                    {
                        return (includeAllMembers, includeCurrentTypeRequiredMembers, includeBaseRequiredMembers) switch
                        {
                            (includeAllMembers: false, includeCurrentTypeRequiredMembers: false, includeBaseRequiredMembers: false)
                                => ImmutableArray<Symbol>.Empty,

                            (includeAllMembers: false, includeCurrentTypeRequiredMembers: true, includeBaseRequiredMembers: false)
                                => containingType.GetMembersUnordered().SelectManyAsArray(
                                    predicate: SymbolExtensions.IsRequired,
                                    selector: symbol => getAllMembersToBeDefaulted(symbol, filterOverridingProperties: true)),

                            (includeAllMembers: false, includeCurrentTypeRequiredMembers: true, includeBaseRequiredMembers: true)
                                => containingType.AllRequiredMembers.SelectManyAsArray(static kvp => getAllMembersToBeDefaulted(kvp.Value, filterOverridingProperties: true)),

                            (includeAllMembers: true, includeCurrentTypeRequiredMembers: _, includeBaseRequiredMembers: false)
                                => containingType.GetMembersUnordered().SelectManyAsArray(
                                    selector: symbol =>
                                    {
                                        var symbolToInitialize = getFieldSymbolToBeInitialized(symbol);
                                        var prop = symbolToInitialize as PropertySymbol ?? (symbolToInitialize as FieldSymbol)?.AssociatedSymbol as PropertySymbol;
                                        if (prop is not null && isFilterableOverrideOfAbstractProperty(prop))
                                        {
                                            return OneOrMany<Symbol>.Empty;
                                        }
                                        else
                                        {
                                            return OneOrMany.Create(symbolToInitialize);
                                        }
                                    }),

                            (includeAllMembers: true, includeCurrentTypeRequiredMembers: true, includeBaseRequiredMembers: true)
                                => getAllTypeAndRequiredMembers(containingType),

                            (includeAllMembers: _, includeCurrentTypeRequiredMembers: false, includeBaseRequiredMembers: true)
                                => throw ExceptionUtilities.Unreachable(),
                        };

                        static ImmutableArray<Symbol> getAllTypeAndRequiredMembers(TypeSymbol containingType)
                        {
                            var members = containingType.GetMembersUnordered();
                            var baseRequiredMembers = containingType.BaseTypeNoUseSiteDiagnostics?.AllRequiredMembers ?? ImmutableSegmentedDictionary<string, Symbol>.Empty;

                            if (baseRequiredMembers.IsEmpty)
                            {
                                return members;
                            }

                            var builder = ArrayBuilder<Symbol>.GetInstance(members.Length + baseRequiredMembers.Count);
                            builder.AddRange(members);
                            foreach (var (_, requiredMember) in baseRequiredMembers)
                            {
                                // We want to assume that all required members were _not_ set by the chained constructor

                                // We exclude any members that are abstract and have an implementation in the current type, when that implementation passes the same heuristics
                                // we use in the other other cases (annotations match up across overrides). This is because the chained constructor, which as SetsRequiredMembers as well,
                                // will have already set the abstract member to non-null, and there isn't a separate slot for that abstract member.
                                if (requiredMember is PropertySymbol { IsAbstract: true } abstractProperty)
                                {
                                    if (members.FirstOrDefault(static (thisMember, baseMember) => thisMember.IsOverride && (object)thisMember.GetOverriddenMember() == baseMember, requiredMember) is { } overridingMember
                                        && isFilterableOverrideOfAbstractProperty((PropertySymbol)overridingMember))
                                    {
                                        continue;
                                    }
                                }

                                builder.AddRange(getAllMembersToBeDefaulted(requiredMember, filterOverridingProperties: false));
                            }

                            return builder.ToImmutableAndFree();
                        }

                        static OneOrMany<Symbol> getAllMembersToBeDefaulted(Symbol requiredMember, bool filterOverridingProperties)
                        {
                            Debug.Assert(requiredMember.IsRequired());

                            if (requiredMember is FieldSymbol)
                            {
                                return OneOrMany.Create(requiredMember);
                            }
                            else
                            {
                                var property = (PropertySymbol)requiredMember;

                                if (filterOverridingProperties && isFilterableOverrideOfAbstractProperty(property))
                                {
                                    return OneOrMany<Symbol>.Empty;
                                }

                                var @return = OneOrMany.Create(getFieldSymbolToBeInitialized(property));

                                // If the set method is null (ie missing), that's an error, but we'll recover as best we can
                                foreach (var notNullMemberName in (property.SetMethod?.NotNullMembers ?? property.NotNullMembers))
                                {
                                    foreach (var member in property.ContainingType.GetMembers(notNullMemberName))
                                    {
                                        @return = @return.Add(getFieldSymbolToBeInitialized(member));
                                    }
                                }

                                return @return;
                            }
                        }

                        static Symbol getFieldSymbolToBeInitialized(Symbol requiredMember)
                            => requiredMember is SourcePropertySymbolBase { BackingField: { } backingField } ? backingField : requiredMember;

                        static bool isFilterableOverrideOfAbstractProperty(PropertySymbol property)
                        {
                            // If this is an override of an abstract property, and the overridden property has the same nullable
                            // annotation as us, we can skip default-initializing the property because the chained constructor
                            // will have done so
                            if (property.OverriddenProperty is not { IsAbstract: true } overriddenProperty)
                            {
                                return false;
                            }

                            var symbolAnnotations = property is SourcePropertySymbolBase { UsesFieldKeyword: true, BackingField: { } field }
                                ? field!.FlowAnalysisAnnotations
                                : property.GetFlowAnalysisAnnotations();
                            var symbolType = ApplyUnconditionalAnnotations(property.TypeWithAnnotations, symbolAnnotations);
                            if (!symbolType.NullableAnnotation.IsNotAnnotated())
                            {
                                return false;
                            }

                            var overriddenAnnotations = overriddenProperty.GetFlowAnalysisAnnotations();
                            var overriddenType = ApplyUnconditionalAnnotations(overriddenProperty.TypeWithAnnotations, overriddenAnnotations);
                            return overriddenType.NullableAnnotation == symbolType.NullableAnnotation;
                        }
                    }
                }
            }
        }

        private void EnforceMemberNotNullOnMember(SyntaxNode? syntaxOpt, LocalState state, MethodSymbol method, string memberName)
        {
            foreach (var member in method.ContainingType.GetMembers(memberName))
            {
                if (FailsMemberNotNullExpectation(member, state))
                {
                    SyntaxNodeOrToken syntax = syntaxOpt switch
                    {
                        BlockSyntax blockSyntax => blockSyntax.CloseBraceToken,
                        LocalFunctionStatementSyntax localFunctionSyntax => localFunctionSyntax.GetLastToken(),
                        _ => syntaxOpt ?? (SyntaxNodeOrToken)methodMainNode.Syntax.GetLastToken()
                    };

                    // Member '{name}' must have a non-null value when exiting.
                    Diagnostics.Add(ErrorCode.WRN_MemberNotNull, syntax.GetLocation(), member.Name);
                }
            }
        }

        private void EnforceMemberNotNullWhenForPendingReturn(PendingBranch pendingReturn, BoundReturnStatement returnStatement)
        {
            if (pendingReturn.IsConditionalState)
            {
                if (returnStatement.ExpressionOpt is { ConstantValueOpt: { IsBoolean: true, BooleanValue: bool value } })
                {
                    enforceMemberNotNullWhen(returnStatement.Syntax, sense: value, pendingReturn.State);
                    return;
                }

                if (!pendingReturn.StateWhenTrue.Reachable || !pendingReturn.StateWhenFalse.Reachable)
                {
                    return;
                }

                if (_symbol is MethodSymbol method)
                {
                    foreach (var memberName in method.NotNullWhenTrueMembers)
                    {
                        enforceMemberNotNullWhenIfAffected(returnStatement.Syntax, sense: true, members: method.ContainingType.GetMembers(memberName), state: pendingReturn.StateWhenTrue, otherState: pendingReturn.StateWhenFalse);
                    }

                    foreach (var memberName in method.NotNullWhenFalseMembers)
                    {
                        enforceMemberNotNullWhenIfAffected(returnStatement.Syntax, sense: false, members: method.ContainingType.GetMembers(memberName), state: pendingReturn.StateWhenFalse, otherState: pendingReturn.StateWhenTrue);
                    }
                }
            }

            return;

            void enforceMemberNotNullWhenIfAffected(SyntaxNode? syntaxOpt, bool sense, ImmutableArray<Symbol> members, LocalState state, LocalState otherState)
            {
                foreach (var member in members)
                {
                    // For non-constant values, only complain if we were able to analyze a difference for this member between two branches
                    if (FailsMemberNotNullExpectation(member, state) != FailsMemberNotNullExpectation(member, otherState))
                    {
                        ReportFailedMemberNotNullIfNeeded(syntaxOpt, sense, member, state);
                    }
                }
            }

            void enforceMemberNotNullWhen(SyntaxNode? syntaxOpt, bool sense, LocalState state)
            {
                if (_symbol is MethodSymbol method)
                {
                    var notNullMembers = sense ? method.NotNullWhenTrueMembers : method.NotNullWhenFalseMembers;
                    foreach (var memberName in notNullMembers)
                    {
                        foreach (var member in method.ContainingType.GetMembers(memberName))
                        {
                            ReportFailedMemberNotNullIfNeeded(syntaxOpt, sense, member, state);
                        }
                    }
                }
            }
        }

        private void ReportFailedMemberNotNullIfNeeded(SyntaxNode? syntaxOpt, bool sense, Symbol member, LocalState state)
        {
            if (FailsMemberNotNullExpectation(member, state))
            {
                // Member '{name}' must have a non-null value when exiting with '{sense}'.
                Diagnostics.Add(ErrorCode.WRN_MemberNotNullWhen, syntaxOpt?.GetLocation() ?? methodMainNode.Syntax.GetLastToken().GetLocation(), member.Name, sense ? "true" : "false");
            }
        }

        private bool FailsMemberNotNullExpectation(Symbol member, LocalState state)
        {
            switch (member.Kind)
            {
                case SymbolKind.Field:
                case SymbolKind.Property:
                    if (GetSlotForMemberPostCondition(member) is int memberSlot &&
                        memberSlot > 0)
                    {
                        var parameterState = GetState(ref state, memberSlot);
                        return !parameterState.IsNotNull();
                    }
                    else
                    {
                        return false;
                    }

                case SymbolKind.Event:
                case SymbolKind.Method:
                    break;
            }

            return false;
        }

        private void MakeMembersMaybeNull(MethodSymbol method, ImmutableArray<string> members)
        {
            foreach (var memberName in members)
            {
                makeMemberMaybeNull(method, memberName);
            }
            return;

            void makeMemberMaybeNull(MethodSymbol method, string memberName)
            {
                var type = method.ContainingType;
                foreach (var member in type.GetMembers(memberName))
                {
                    if (GetSlotForMemberPostCondition(member) is int memberSlot &&
                        memberSlot > 0)
                    {
                        SetState(ref this.State, memberSlot, NullableFlowState.MaybeNull);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a slot for a static member, or a member of 'this', which is being referenced by a postcondition.
        /// Used for "declaration-site" analysis of MemberNotNullAttributes.
        /// </summary>
        private int GetSlotForMemberPostCondition(Symbol member)
        {
            if (member.Kind != SymbolKind.Field &&
                member.Kind != SymbolKind.Property &&
                member.Kind != SymbolKind.Event)
            {
                return -1;
            }

            int containingSlot;
            if (member.IsStatic)
            {
                containingSlot = 0;
            }
            else
            {
                containingSlot = GetReceiverSlotForMemberPostConditions(_symbol as MethodSymbol);
                if (containingSlot <= 0)
                {
                    // Either trying to access an instance member from a static context,
                    // or an invalid slot (-1) was returned
                    return -1;
                }
            }

            return GetOrCreateSlot(member, containingSlot);
        }

        /// <summary>
        /// We have multiple ways of entering the nullable walker: we could be just analyzing the initializers, with a BoundStatementList body and _baseOrThisInitializer
        /// having been provided, or we could be analyzing the body of a constructor, with a BoundConstructorBody body and _baseOrThisInitializer being null.
        /// </summary>
        private MethodSymbol? GetBaseOrThisInitializer()
        {
            return (_baseOrThisInitializer ?? GetConstructorThisOrBaseSymbol(this.methodMainNode));
        }

        private void EnforceNotNullWhenForPendingReturn(PendingBranch pendingReturn, BoundReturnStatement returnStatement)
        {
            if (_symbol is not MethodSymbol method)
            {
                return;
            }

            ImmutableArray<ParameterSymbol> parameters = method.GetParametersIncludingExtensionParameter(skipExtensionIfStatic: true);

            if (!parameters.IsEmpty)
            {
                if (pendingReturn.IsConditionalState)
                {
                    if (returnStatement.ExpressionOpt is { ConstantValueOpt: { IsBoolean: true, BooleanValue: bool value } })
                    {
                        EnforceParameterNotNullWhenOnExit(returnStatement.Syntax, parameters, sense: value, stateWhen: pendingReturn.State);
                        return;
                    }

                    if (!pendingReturn.StateWhenTrue.Reachable || !pendingReturn.StateWhenFalse.Reachable)
                    {
                        return;
                    }

                    foreach (var parameter in parameters)
                    {
                        // For non-constant values, only complain if we were able to analyze a difference for this parameter between two branches
                        if (GetOrCreateSlot(parameter) is > 0 and var slot && GetState(ref pendingReturn.StateWhenTrue, slot) != GetState(ref pendingReturn.StateWhenFalse, slot))
                        {
                            ReportParameterIfBadConditionalState(returnStatement.Syntax, parameter, sense: true, stateWhen: pendingReturn.StateWhenTrue);
                            ReportParameterIfBadConditionalState(returnStatement.Syntax, parameter, sense: false, stateWhen: pendingReturn.StateWhenFalse);
                        }
                    }
                }
                else if (returnStatement.ExpressionOpt is { ConstantValueOpt: { IsBoolean: true, BooleanValue: bool value } })
                {
                    // example: return (bool)true;
                    EnforceParameterNotNullWhenOnExit(returnStatement.Syntax, parameters, sense: value, stateWhen: pendingReturn.State);
                    return;
                }
            }
        }

        private void EnforceParameterNotNullOnExit(SyntaxNode? syntaxOpt, LocalState state)
        {
            if (!state.Reachable)
            {
                return;
            }

            if (_symbol is not MethodSymbol method)
            {
                return;
            }

            ImmutableArray<ParameterSymbol> parameters = method.GetParametersIncludingExtensionParameter(skipExtensionIfStatic: true);
            foreach (var parameter in parameters)
            {
                var slot = GetOrCreateSlot(parameter);
                if (slot <= 0)
                {
                    continue;
                }

                var annotations = parameter.FlowAnalysisAnnotations;
                var hasNotNull = (annotations & FlowAnalysisAnnotations.NotNull) == FlowAnalysisAnnotations.NotNull;
                var parameterState = GetState(ref state, slot);
                if (hasNotNull && parameterState.MayBeNull())
                {
                    Location location;
                    if (syntaxOpt is BlockSyntax blockSyntax)
                    {
                        location = blockSyntax.CloseBraceToken.GetLocation();
                    }
                    else
                    {
                        location = syntaxOpt?.GetLocation() ?? methodMainNode.Syntax.GetLastToken().GetLocation();
                    }

                    // Parameter '{name}' must have a non-null value when exiting.
                    Diagnostics.Add(ErrorCode.WRN_ParameterDisallowsNull, location, parameter.Name);
                }
                else
                {
                    EnforceNotNullIfNotNull(syntaxOpt, state, parameters, parameter.NotNullIfParameterNotNull, parameterState, parameter);
                }
            }
        }

        private void EnforceParameterNotNullWhenOnExit(SyntaxNode syntax, ImmutableArray<ParameterSymbol> parameters, bool sense, LocalState stateWhen)
        {
            if (!stateWhen.Reachable)
            {
                return;
            }

            foreach (var parameter in parameters)
            {
                ReportParameterIfBadConditionalState(syntax, parameter, sense, stateWhen);
            }
        }

        private void ReportParameterIfBadConditionalState(SyntaxNode syntax, ParameterSymbol parameter, bool sense, LocalState stateWhen)
        {
            if (parameterHasBadConditionalState(parameter, sense, stateWhen))
            {
                // Parameter '{name}' must have a non-null value when exiting with '{sense}'.
                Diagnostics.Add(ErrorCode.WRN_ParameterConditionallyDisallowsNull, syntax.Location, parameter.Name, sense ? "true" : "false");
            }
            return;

            bool parameterHasBadConditionalState(ParameterSymbol parameter, bool sense, LocalState stateWhen)
            {
                var refKind = parameter.RefKind;
                if (refKind != RefKind.Out && refKind != RefKind.Ref)
                {
                    return false;
                }

                var slot = GetOrCreateSlot(parameter);
                if (slot > 0)
                {
                    var parameterState = GetState(ref stateWhen, slot);

                    // On a parameter marked with MaybeNullWhen, we would have not reported an assignment warning.
                    // We should only check if an assignment warning would have been warranted ignoring the MaybeNullWhen.
                    FlowAnalysisAnnotations annotations = parameter.FlowAnalysisAnnotations;
                    if (sense)
                    {
                        bool hasNotNullWhenTrue = (annotations & FlowAnalysisAnnotations.NotNull) == FlowAnalysisAnnotations.NotNullWhenTrue;
                        bool hasMaybeNullWhenFalse = (annotations & FlowAnalysisAnnotations.MaybeNull) == FlowAnalysisAnnotations.MaybeNullWhenFalse;

                        return (hasNotNullWhenTrue && parameterState.MayBeNull()) ||
                            (hasMaybeNullWhenFalse && ShouldReportNullableAssignment(parameter.TypeWithAnnotations, parameterState));
                    }
                    else
                    {
                        bool hasNotNullWhenFalse = (annotations & FlowAnalysisAnnotations.NotNull) == FlowAnalysisAnnotations.NotNullWhenFalse;
                        bool hasMaybeNullWhenTrue = (annotations & FlowAnalysisAnnotations.MaybeNull) == FlowAnalysisAnnotations.MaybeNullWhenTrue;

                        return (hasNotNullWhenFalse && parameterState.MayBeNull()) ||
                            (hasMaybeNullWhenTrue && ShouldReportNullableAssignment(parameter.TypeWithAnnotations, parameterState));
                    }
                }

                return false;
            }
        }

        private void EnforceNotNullIfNotNull(SyntaxNode? syntaxOpt, LocalState state, ImmutableArray<ParameterSymbol> parameters, ImmutableHashSet<string> inputParamNames, NullableFlowState outputState, ParameterSymbol? outputParam)
        {
            if (inputParamNames.IsEmpty || outputState.IsNotNull())
            {
                return;
            }

            foreach (var inputParam in parameters)
            {
                if (inputParamNames.Contains(inputParam.Name)
                    && GetOrCreateSlot(inputParam) is > 0 and int inputSlot
                    && GetState(ref state, inputSlot).IsNotNull())
                {
                    var location = syntaxOpt?.GetLocation() ?? methodMainNode.Syntax.GetLastToken().GetLocation();
                    if (outputParam is object)
                    {
                        // Parameter '{0}' must have a non-null value when exiting because parameter '{1}' is non-null.
                        Diagnostics.Add(ErrorCode.WRN_ParameterNotNullIfNotNull, location, outputParam.Name, inputParam.Name);
                    }
                    else if (CurrentSymbol is MethodSymbol { IsAsync: false })
                    {
                        // Return value must be non-null because parameter '{0}' is non-null.
                        Diagnostics.Add(ErrorCode.WRN_ReturnNotNullIfNotNull, location, inputParam.Name);
                    }
                    break;
                }
            }
        }

        private void EnforceDoesNotReturn(SyntaxNode? syntaxOpt)
        {
            if (CurrentSymbol is MethodSymbol method &&
                ((method.FlowAnalysisAnnotations & FlowAnalysisAnnotations.DoesNotReturn) == FlowAnalysisAnnotations.DoesNotReturn) &&
                this.IsReachable())
            {
                // A method marked [DoesNotReturn] should not return.
                ReportDiagnostic(ErrorCode.WRN_ShouldNotReturn, syntaxOpt?.GetLocation() ?? methodMainNode.Syntax.GetLastToken().GetLocation());
            }
        }

        /// <summary>
        /// Analyzes a method body if settings indicate we should.
        /// </summary>
        internal static void AnalyzeIfNeeded(
            CSharpCompilation compilation,
            MethodSymbol method,
            BoundNode node,
            DiagnosticBag diagnostics,
            bool useConstructorExitWarnings,
            VariableState? initialNullableState,
            bool getFinalNullableState,
            MethodSymbol? baseOrThisInitializer,
            out VariableState? finalNullableState)
        {
            if (!HasRequiredLanguageVersion(compilation) || !compilation.IsNullableAnalysisEnabledIn(method))
            {
                if (compilation.IsNullableAnalysisEnabledAlways)
                {
                    // Once we address https://github.com/dotnet/roslyn/issues/46579 we should also always pass `getFinalNullableState: true` in debug mode.
                    // We will likely always need to write a 'null' out for the out parameter in this code path, though, because
                    // we don't want to introduce behavior differences between debug and release builds
                    Analyze(compilation, method, node, new DiagnosticBag(), useConstructorExitWarnings: false, initialNullableState: null, getFinalNullableState: false, baseOrThisInitializer, out _, requiresAnalysis: false);
                }
                finalNullableState = null;
                return;
            }

            Analyze(compilation, method, node, diagnostics, useConstructorExitWarnings, initialNullableState, getFinalNullableState, baseOrThisInitializer, out finalNullableState);
        }

        private static void Analyze(
            CSharpCompilation compilation,
            MethodSymbol method,
            BoundNode node,
            DiagnosticBag diagnostics,
            bool useConstructorExitWarnings,
            VariableState? initialNullableState,
            bool getFinalNullableState,
            MethodSymbol? baseOrThisInitializer,
            out VariableState? finalNullableState,
            bool requiresAnalysis = true)
        {
            if (method.IsImplicitlyDeclared && !method.IsImplicitConstructor && !method.IsScriptInitializer)
            {
                finalNullableState = null;
                return;
            }
            Debug.Assert(node.SyntaxTree is object);
            var binder = method is SynthesizedSimpleProgramEntryPointSymbol entryPoint ?
                             entryPoint.GetBodyBinder(ignoreAccessibility: false) :
                             compilation.GetBinderFactory(node.SyntaxTree).GetBinder(node.Syntax);
            var conversions = binder.Conversions;
            Analyze(compilation,
                method,
                node,
                binder,
                conversions,
                diagnostics,
                useConstructorExitWarnings,
                getterNullResilienceData: null,
                useDelegateInvokeParameterTypes: false,
                useDelegateInvokeReturnType: false,
                delegateInvokeMethodOpt: null,
                initialState: initialNullableState,
                baseOrThisInitializer,
                analyzedNullabilityMapOpt: null,
                snapshotBuilderOpt: null,
                returnTypesOpt: null,
                getFinalNullableState,
                finalNullableState: out finalNullableState,
                requiresAnalysis);
        }

        internal static VariableState? GetAfterInitializersState(CSharpCompilation compilation, Symbol? symbol, BoundNode constructorBody)
        {
            if (symbol is MethodSymbol method
                && method.IncludeFieldInitializersInBody()
                && method.ContainingType is SourceMemberContainerTypeSymbol containingType)
            {
                Binder.ProcessedFieldInitializers discardedInitializers = default;
                Binder.BindFieldInitializers(compilation, null, method.IsStatic ? containingType.StaticInitializers : containingType.InstanceInitializers, BindingDiagnosticBag.Discarded, ref discardedInitializers);
                return GetAfterInitializersState(compilation, method, InitializerRewriter.RewriteConstructor(discardedInitializers.BoundInitializers, method), constructorBody, diagnostics: BindingDiagnosticBag.Discarded);
            }

            return null;
        }

        /// <summary>
        /// Gets the "after initializers state" which should be used at the beginning of nullable analysis
        /// of certain constructors.
        /// </summary>
        internal static VariableState? GetAfterInitializersState(CSharpCompilation compilation, MethodSymbol method, BoundNode nodeToAnalyze, BoundNode? constructorBody, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(method.IsConstructor());
            bool ownsDiagnostics;
            DiagnosticBag diagnosticsBag;
            if (diagnostics.DiagnosticBag == null)
            {
                diagnostics = BindingDiagnosticBag.Discarded;
                diagnosticsBag = DiagnosticBag.GetInstance();
                ownsDiagnostics = true;
            }
            else
            {
                diagnosticsBag = diagnostics.DiagnosticBag;
                ownsDiagnostics = false;
            }

            MethodSymbol? baseOrThisInitializer = GetConstructorThisOrBaseSymbol(constructorBody);

            NullableWalker.AnalyzeIfNeeded(
                compilation,
                method,
                nodeToAnalyze,
                diagnosticsBag,
                useConstructorExitWarnings: false,
                initialNullableState: null,
                getFinalNullableState: true,
                baseOrThisInitializer,
                out var afterInitializersState);

            if (ownsDiagnostics)
            {
                diagnosticsBag.Free();
            }

            return afterInitializersState;
        }

        private static MethodSymbol? GetConstructorThisOrBaseSymbol(BoundNode? constructorBody)
        {
            return constructorBody is BoundConstructorMethodBody { Initializer: BoundExpressionStatement { Expression: BoundCall { Method: { MethodKind: MethodKind.Constructor } initializerMethod } } }
                ? initializerMethod
                : null;
        }

        /// <summary>
        /// Analyzes a set of bound nodes, recording updated nullability information. This method is only
        /// used when nullable is explicitly enabled for all methods but disabled otherwise to verify that
        /// correct semantic information is being recorded for all bound nodes. The results are thrown away.
        /// </summary>
        internal static void AnalyzeWithoutRewrite(
            CSharpCompilation compilation,
            Symbol? symbol,
            BoundNode node,
            Binder binder,
            DiagnosticBag diagnostics,
            bool createSnapshots)
        {
            _ = AnalyzeWithSemanticInfo(compilation, symbol, node, binder, initialState: GetAfterInitializersState(compilation, symbol, node), diagnostics, createSnapshots, requiresAnalysis: false);
        }

        /// <summary>
        /// Analyzes a set of bound nodes, recording updated nullability information, and returns an
        /// updated BoundNode with the information populated.
        /// </summary>
        internal static BoundNode AnalyzeAndRewrite(
            CSharpCompilation compilation,
            Symbol? symbol,
            BoundNode node,
            Binder binder,
            VariableState? initialState,
            DiagnosticBag diagnostics,
            bool createSnapshots,
            out SnapshotManager? snapshotManager,
            ref ImmutableDictionary<Symbol, Symbol>? remappedSymbols)
        {
            ImmutableDictionary<BoundExpression, (NullabilityInfo, TypeSymbol?)> analyzedNullabilitiesMap;
            (snapshotManager, analyzedNullabilitiesMap) = AnalyzeWithSemanticInfo(compilation, symbol, node, binder, initialState, diagnostics, createSnapshots, requiresAnalysis: true);
            return Rewrite(analyzedNullabilitiesMap, snapshotManager, node, ref remappedSymbols);
        }

        private static (SnapshotManager?, ImmutableDictionary<BoundExpression, (NullabilityInfo, TypeSymbol?)>) AnalyzeWithSemanticInfo(
            CSharpCompilation compilation,
            Symbol? symbol,
            BoundNode node,
            Binder binder,
            VariableState? initialState,
            DiagnosticBag diagnostics,
            bool createSnapshots,
            bool requiresAnalysis)
        {
            var analyzedNullabilities = ImmutableDictionary.CreateBuilder<BoundExpression, (NullabilityInfo, TypeSymbol?)>(EqualityComparer<BoundExpression>.Default, NullabilityInfoTypeComparer.Instance);

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
                useConstructorExitWarnings: true,
                getterNullResilienceData: null,
                useDelegateInvokeParameterTypes: false,
                useDelegateInvokeReturnType: false,
                delegateInvokeMethodOpt: null,
                initialState,
                baseOrThisInitializer: null,
                analyzedNullabilities,
                snapshotBuilder,
                returnTypesOpt: null,
                getFinalNullableState: false,
                out _,
                requiresAnalysis);

            var analyzedNullabilitiesMap = analyzedNullabilities.ToImmutable();
            var snapshotManager = snapshotBuilder?.ToManagerAndFree();

#if DEBUG
            // https://github.com/dotnet/roslyn/issues/34993 Enable for all calls
            if (isNullableAnalysisEnabledAnywhere(compilation))
            {
                DebugVerifier.Verify(analyzedNullabilitiesMap, snapshotManager, node);
            }

            static bool isNullableAnalysisEnabledAnywhere(CSharpCompilation compilation)
            {
                if (compilation.Options.NullableContextOptions != NullableContextOptions.Disable)
                {
                    return true;
                }
                return compilation.SyntaxTrees.Any(static tree => ((CSharpSyntaxTree)tree).IsNullableAnalysisEnabled(new Text.TextSpan(0, tree.Length)) == true);
            }
#endif

            return (snapshotManager, analyzedNullabilitiesMap);
        }

        internal static BoundNode AnalyzeAndRewriteSpeculation(
            int position,
            BoundNode node,
            Binder binder,
            SnapshotManager originalSnapshots,
            out SnapshotManager newSnapshots,
            ref ImmutableDictionary<Symbol, Symbol>? remappedSymbols)
        {
            var analyzedNullabilities = ImmutableDictionary.CreateBuilder<BoundExpression, (NullabilityInfo, TypeSymbol?)>(EqualityComparer<BoundExpression>.Default, NullabilityInfoTypeComparer.Instance);
            var newSnapshotBuilder = new SnapshotManager.Builder();
            var (variables, localState) = originalSnapshots.GetSnapshot(position);
            var symbol = variables.Symbol;
            var walker = new NullableWalker(
                binder.Compilation,
                symbol,
                useConstructorExitWarnings: false,
                getterNullResilienceData: null,
                useDelegateInvokeParameterTypes: false,
                useDelegateInvokeReturnType: false,
                delegateInvokeMethodOpt: null,
                node,
                binder,
                binder.Conversions,
                Variables.Create(variables),
                baseOrThisInitializer: null,
                returnTypesOpt: null,
                analyzedNullabilities,
                newSnapshotBuilder,
                isSpeculative: true);
            try
            {
                Analyze(walker, symbol, diagnostics: null, LocalState.Create(localState), snapshotBuilderOpt: newSnapshotBuilder);
            }
            finally
            {
                walker.Free();
            }

            var analyzedNullabilitiesMap = analyzedNullabilities.ToImmutable();
            newSnapshots = newSnapshotBuilder.ToManagerAndFree();

#if DEBUG
            DebugVerifier.Verify(analyzedNullabilitiesMap, newSnapshots, node);
#endif

            return Rewrite(analyzedNullabilitiesMap, newSnapshots, node, ref remappedSymbols);
        }

        private static BoundNode Rewrite(ImmutableDictionary<BoundExpression, (NullabilityInfo, TypeSymbol?)> updatedNullabilities, SnapshotManager? snapshotManager, BoundNode node, ref ImmutableDictionary<Symbol, Symbol>? remappedSymbols)
        {
            var remappedSymbolsBuilder = ImmutableDictionary.CreateBuilder<Symbol, Symbol>(Symbols.SymbolEqualityComparer.ConsiderEverything, Symbols.SymbolEqualityComparer.ConsiderEverything);
            if (remappedSymbols is object)
            {
                // When we're rewriting for the speculative model, there will be a set of originally-mapped symbols, and we need to
                // use them in addition to any symbols found during this pass of the walker.
                remappedSymbolsBuilder.AddRange(remappedSymbols);
            }
            var rewriter = new NullabilityRewriter(updatedNullabilities, snapshotManager, remappedSymbolsBuilder);
            var rewrittenNode = rewriter.Visit(node);
            remappedSymbols = remappedSymbolsBuilder.ToImmutable();
            return rewrittenNode;
        }

        private static bool HasRequiredLanguageVersion(CSharpCompilation compilation)
        {
            return compilation.LanguageVersion >= MessageID.IDS_FeatureNullableReferenceTypes.RequiredVersion();
        }

        /// <summary>
        /// Returns true if the nullable analysis is needed for the region represented by <paramref name="syntaxNode"/>.
        /// The syntax node is used to determine the overall nullable context for the region.
        /// </summary>
        internal static bool NeedsAnalysis(CSharpCompilation compilation, SyntaxNode syntaxNode)
        {
            return HasRequiredLanguageVersion(compilation) &&
                (compilation.IsNullableAnalysisEnabledIn(syntaxNode) || compilation.IsNullableAnalysisEnabledAlways);
        }

        /// <summary>Analyzes a node in a "one-off" context, such as for attributes or parameter default values.</summary>
        /// <remarks><paramref name="syntax"/> is the syntax span used to determine the overall nullable context.</remarks>
        internal static void AnalyzeIfNeeded(
            Binder binder,
            BoundNode node,
            SyntaxNode syntax,
            DiagnosticBag diagnostics,
            (SourcePropertyAccessorSymbol symbol, GetterNullResilienceData getterNullResilienceData)? symbolAndGetterNullResilienceData = null)
        {
            bool requiresAnalysis = true;
            var compilation = binder.Compilation;
            if (!HasRequiredLanguageVersion(compilation) || !compilation.IsNullableAnalysisEnabledIn(syntax))
            {
                if (!compilation.IsNullableAnalysisEnabledAlways)
                {
                    return;
                }
                diagnostics = new DiagnosticBag();
                requiresAnalysis = false;
            }

            Analyze(
                compilation,
                symbol: symbolAndGetterNullResilienceData?.symbol,
                node,
                binder,
                binder.Conversions,
                diagnostics,
                useConstructorExitWarnings: false,
                getterNullResilienceData: symbolAndGetterNullResilienceData?.getterNullResilienceData,
                useDelegateInvokeParameterTypes: false,
                useDelegateInvokeReturnType: false,
                delegateInvokeMethodOpt: null,
                initialState: null,
                baseOrThisInitializer: null,
                analyzedNullabilityMapOpt: null,
                snapshotBuilderOpt: null,
                returnTypesOpt: null,
                getFinalNullableState: false,
                out _,
                requiresAnalysis);
        }

        internal static void Analyze(
            CSharpCompilation compilation,
            BoundLambda lambda,
            Conversions conversions,
            DiagnosticBag diagnostics,
            MethodSymbol? delegateInvokeMethodOpt,
            VariableState initialState,
            ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)>? returnTypesOpt,
            GetterNullResilienceData? getterNullResilienceData)
        {
            var symbol = lambda.Symbol;
            var variables = Variables.Create(initialState.Variables).CreateNestedMethodScope(symbol);
            UseDelegateInvokeParameterAndReturnTypes(lambda, delegateInvokeMethodOpt, out bool useDelegateInvokeParameterTypes, out bool useDelegateInvokeReturnType);
            var walker = new NullableWalker(
                compilation,
                symbol,
                useConstructorExitWarnings: false,
                getterNullResilienceData: getterNullResilienceData,
                useDelegateInvokeParameterTypes: useDelegateInvokeParameterTypes,
                useDelegateInvokeReturnType: useDelegateInvokeReturnType,
                delegateInvokeMethodOpt: delegateInvokeMethodOpt,
                lambda.Body,
                lambda.Binder,
                conversions,
                variables,
                baseOrThisInitializer: null,
                returnTypesOpt,
                analyzedNullabilityMapOpt: null,
                snapshotBuilderOpt: null);
            try
            {
                var localState = LocalState.Create(initialState.VariableNullableStates).CreateNestedMethodState(variables);
                Analyze(walker, symbol, diagnostics, localState, snapshotBuilderOpt: null);
            }
            finally
            {
                walker.Free();
            }
        }

        private static void Analyze(
            CSharpCompilation compilation,
            Symbol? symbol,
            BoundNode node,
            Binder binder,
            Conversions conversions,
            DiagnosticBag diagnostics,
            bool useConstructorExitWarnings,
            GetterNullResilienceData? getterNullResilienceData,
            bool useDelegateInvokeParameterTypes,
            bool useDelegateInvokeReturnType,
            MethodSymbol? delegateInvokeMethodOpt,
            VariableState? initialState,
            MethodSymbol? baseOrThisInitializer,
            ImmutableDictionary<BoundExpression, (NullabilityInfo, TypeSymbol?)>.Builder? analyzedNullabilityMapOpt,
            SnapshotManager.Builder? snapshotBuilderOpt,
            ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)>? returnTypesOpt,
            bool getFinalNullableState,
            out VariableState? finalNullableState,
            bool requiresAnalysis = true)
        {
            Debug.Assert(diagnostics != null);
            Debug.Assert(getterNullResilienceData is null || symbol is SourcePropertyAccessorSymbol { MethodKind: MethodKind.PropertyGet });
            Debug.Assert(getterNullResilienceData is null || !useConstructorExitWarnings);

            var walker = new NullableWalker(compilation,
                                            symbol,
                                            useConstructorExitWarnings,
                                            getterNullResilienceData,
                                            useDelegateInvokeParameterTypes,
                                            useDelegateInvokeReturnType,
                                            delegateInvokeMethodOpt,
                                            node,
                                            binder,
                                            conversions,
                                            initialState is null ? null : Variables.Create(initialState.Variables),
                                            baseOrThisInitializer,
                                            returnTypesOpt,
                                            analyzedNullabilityMapOpt,
                                            snapshotBuilderOpt);

            finalNullableState = null;
            try
            {
                Analyze(walker, symbol, diagnostics, initialState is null ? (Optional<LocalState>)default : LocalState.Create(initialState.VariableNullableStates), snapshotBuilderOpt, requiresAnalysis);
                if (getFinalNullableState)
                {
                    Debug.Assert(!walker.IsConditionalState);
                    finalNullableState = GetVariableState(walker._variables, walker.State);
                }
            }
            finally
            {
                walker.Free();
            }
        }

        private static void Analyze(
            NullableWalker walker,
            Symbol? symbol,
            DiagnosticBag? diagnostics,
            Optional<LocalState> initialState,
            SnapshotManager.Builder? snapshotBuilderOpt,
            bool requiresAnalysis = true)
        {
            Debug.Assert(snapshotBuilderOpt is null || symbol is object);
            var previousSlot = snapshotBuilderOpt?.EnterNewWalker(symbol!) ?? -1;
            try
            {
#if DEBUG
                if (initialState.HasValue)
                {
                    Debug.Assert(walker._variables.Id == initialState.Value.Id);
                }
#endif
                bool badRegion = false;
                ImmutableArray<PendingBranch> returns = walker.Analyze(ref badRegion, initialState);
                diagnostics?.AddRange(walker.Diagnostics);
                Debug.Assert(!badRegion);
            }
            catch (CancelledByStackGuardException ex) when (diagnostics != null)
            {
                ex.AddAnError(diagnostics);
            }
            finally
            {
                snapshotBuilderOpt?.ExitWalker(walker.SaveSharedState(), previousSlot);
            }

            walker.RecordNullableAnalysisData(symbol, requiresAnalysis);
        }

        private void RecordNullableAnalysisData(Symbol? symbol, bool requiredAnalysis)
        {
            if (compilation.TestOnlyCompilationData is NullableAnalysisData { Data: { } state })
            {
                var key = (object?)symbol ?? methodMainNode.Syntax;
                if (state.TryGetValue(key, out var result))
                {
                    Debug.Assert(result.RequiredAnalysis == requiredAnalysis);
                }
                else
                {
                    state.TryAdd(key, new Data(_variables.GetTotalVariableCount(), requiredAnalysis));
                }
            }
        }

        private SharedWalkerState SaveSharedState()
        {
            return new SharedWalkerState(_variables.CreateSnapshot());
        }

        private void TakeIncrementalSnapshot(BoundNode? node)
        {
            Debug.Assert(!IsConditionalState);
            _snapshotBuilderOpt?.TakeIncrementalSnapshot(node, State);
        }

        private void SetUpdatedSymbol(BoundNode node, Symbol originalSymbol, Symbol updatedSymbol)
        {
            if (_snapshotBuilderOpt is null)
            {
                return;
            }

            var lambdaIsExactMatch = false;
            if (node is BoundLambda boundLambda && originalSymbol is LambdaSymbol l && updatedSymbol is NamedTypeSymbol n)
            {
                if (!AreLambdaAndNewDelegateSimilar(l, n))
                {
                    return;
                }

                lambdaIsExactMatch = updatedSymbol.Equals(boundLambda.Type!.GetDelegateType(), TypeCompareKind.ConsiderEverything);
            }

#if DEBUG
            Debug.Assert(node is object);
            RoslynDebug.Assert(AreCloseEnough(originalSymbol, updatedSymbol), $"Attempting to set {node.Syntax} from {originalSymbol.ToDisplayString()} to {updatedSymbol.ToDisplayString()}");
#endif

            if (lambdaIsExactMatch || Symbol.Equals(originalSymbol, updatedSymbol, TypeCompareKind.ConsiderEverything))
            {
                // If the symbol is reset, remove the updated symbol so we don't needlessly update the
                // bound node later on. We do this unconditionally, as Remove will just return false
                // if the key wasn't in the dictionary.
                _snapshotBuilderOpt.RemoveSymbolIfPresent(node, originalSymbol);
            }
            else
            {
                _snapshotBuilderOpt.SetUpdatedSymbol(node, originalSymbol, updatedSymbol);
            }
        }

        private NullableFlowState GetState(ref LocalState state, int slot)
        {
            if (!state.Reachable)
                return NullableFlowState.NotNull;

            NormalizeIfNeeded(ref state, slot, useNotNullsAsDefault: false);
            return state[slot];
        }

        private void SetState(ref LocalState state, int slot, NullableFlowState value, bool useNotNullsAsDefault = false)
        {
            if (!state.Reachable)
                return;

            NormalizeIfNeeded(ref state, slot, useNotNullsAsDefault);
            state[slot] = value;
        }

        private void NormalizeIfNeeded(ref LocalState state, int slot, bool useNotNullsAsDefault)
        {
            state.NormalizeIfNeeded(slot, this, _variables, useNotNullsAsDefault);
        }

        protected override void Normalize(ref LocalState state)
        {
            if (!state.Reachable)
                return;

            state.Normalize(this, _variables);
        }

        private NullableFlowState GetDefaultState(ref LocalState state, int slot)
        {
            Debug.Assert(slot > 0);

            if (!state.Reachable)
                return NullableFlowState.NotNull;

            var variable = _variables[slot];
            var symbol = variable.Symbol;

            switch (symbol.Kind)
            {
                case SymbolKind.Local:
                    {
                        var local = (LocalSymbol)symbol;
                        if (!_variables.TryGetType(local, out TypeWithAnnotations localType))
                        {
                            localType = local.TypeWithAnnotations;
                        }
                        return localType.ToTypeWithState().State;
                    }
                case SymbolKind.Parameter:
                    {
                        var parameter = (ParameterSymbol)symbol;
                        if (!_variables.TryGetType(parameter, out TypeWithAnnotations parameterType))
                        {
                            parameterType = parameter.TypeWithAnnotations;
                        }
                        return GetParameterState(parameterType, parameter.FlowAnalysisAnnotations).State;
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

        protected override bool TryGetReceiverAndMember(BoundExpression expr, out BoundExpression? receiver, [NotNullWhen(true)] out Symbol? member)
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

            return member is object &&
                receiver is object &&
                receiver.Kind != BoundKind.TypeExpression &&
                receiver.Type is object;
        }

        protected override int MakeSlot(BoundExpression node)
        {
            int result = makeSlot(node);
#if DEBUG
            if (result != -1)
            {
                // Check that the slot represents a value of an equivalent type to the node
                TypeSymbol slotType = NominalSlotType(result);
                TypeSymbol? nodeType = node.Type;
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                var conversionsWithoutNullability = this.compilation.Conversions;
                Debug.Assert(node.HasErrors ||
                    (nodeType is { } &&
                        (nodeType.IsErrorType() ||
                        conversionsWithoutNullability.HasIdentityOrImplicitReferenceConversion(slotType, nodeType, ref discardedUseSiteInfo) ||
                        conversionsWithoutNullability.HasBoxingConversion(slotType, nodeType, ref discardedUseSiteInfo))));
            }
#endif
            return result;

            int makeSlot(BoundExpression node)
            {
                switch (node.Kind)
                {
                    case BoundKind.ThisReference:
                    case BoundKind.BaseReference:
                        {
                            var method = getTopLevelMethod(_symbol as MethodSymbol);
                            var thisParameter = method?.ThisParameter;
                            return thisParameter is object ? GetOrCreateSlot(thisParameter) : -1;
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

                                case ConversionKind.ConditionalExpression or ConversionKind.SwitchExpression or ConversionKind.ObjectCreation when
                                         IsTargetTypedExpression(conv.Operand) &&
                                         TypeSymbol.Equals(conv.Type, conv.Operand.Type, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes):
                                case ConversionKind.Identity:
                                case ConversionKind.DefaultLiteral:
                                case ConversionKind.ImplicitReference:
                                case ConversionKind.ImplicitTupleLiteral:
                                case ConversionKind.Boxing:
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
                    case BoundKind.ConditionalAccess:
                        return getPlaceholderSlot(node);
                    case BoundKind.ConditionalReceiver:
                        return _lastConditionalAccessSlot;
                    default:
                        {
                            int slot = getPlaceholderSlot(node);
                            return (slot > 0) ? slot : base.MakeSlot(node);
                        }
                }

                return -1;
            }

            int getPlaceholderSlot(BoundExpression expr)
            {
                if (_placeholderLocalsOpt != null && _placeholderLocalsOpt.TryGetValue(expr, out var placeholder))
                {
                    return GetOrCreateSlot(placeholder);
                }
                return -1;
            }

            static MethodSymbol? getTopLevelMethod(MethodSymbol? method)
            {
                while (method is object)
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

        protected override int GetOrCreateSlot(Symbol symbol, int containingSlot = 0, bool forceSlotEvenIfEmpty = false, bool createIfMissing = true)
        {
            if (containingSlot > 0 && !IsSlotMember(containingSlot, symbol))
                return -1;

            // Primary constructor parameter and its backing field share the slot when
            // we are dealing with 'this' instance.
            if (symbol is ParameterSymbol { ContainingSymbol: SynthesizedPrimaryConstructor primaryConstructor } parameter &&
                primaryConstructor.GetCapturedParameters().TryGetValue(parameter, out FieldSymbol? field))
            {
                Debug.Assert(containingSlot == 0);

                var enclosingMemberMethod = _symbol as MethodSymbol;

                while (enclosingMemberMethod?.MethodKind is MethodKind.AnonymousFunction or MethodKind.LocalFunction)
                {
                    enclosingMemberMethod = enclosingMemberMethod.ContainingSymbol as MethodSymbol;
                }

                if (enclosingMemberMethod?.TryGetThisParameter(out ParameterSymbol? methodThisParameter) == true &&
                    methodThisParameter?.ContainingSymbol.ContainingSymbol == (object)primaryConstructor.ContainingSymbol &&
                    GetOrCreateSlot(methodThisParameter) is >= 0 and var thisSlot)
                {
                    symbol = field;
                    containingSlot = thisSlot;
                }
            }

            // Share a slot between backing field and associated property/event in the context of a constructor which owns initialization of that backing field.
            if (this._symbol is MethodSymbol constructor
                && constructor.IsConstructor()
                && constructor.IsStatic == symbol.IsStatic)
            {
                if ((constructor.IsStatic && containingSlot == 0 && constructor.ContainingType.Equals(symbol.ContainingType))
                    || (!constructor.IsStatic && containingSlot > 0 && _variables[containingSlot].Symbol is ThisParameterSymbol))
                {
                    // If symbol is a backing field, but property does not use the field keyword,
                    // then use the property to determine initial state and to own the slot.
                    // Example scenarios:
                    // - property initializer on normal auto-property
                    // - property assignment on getter-only auto-property.
                    // Example test: NullableReferenceTypesTests.ConstructorUsesStateFromInitializers will fail without this.
                    if (symbol is SynthesizedBackingFieldSymbol { AssociatedSymbol: SourcePropertySymbolBase { UsesFieldKeyword: false } property })
                        symbol = property;
                    // If symbol is a property that uses field keyword, then use field to determine initial state and to own the slot.
                    else if (symbol is SourcePropertySymbolBase { UsesFieldKeyword: true, BackingField: { } backingField })
                        symbol = backingField;
                    else if (symbol is SourceEventFieldSymbol eventField)
                        symbol = eventField.AssociatedSymbol;
                }
            }

            return base.GetOrCreateSlot(symbol, containingSlot, forceSlotEvenIfEmpty, createIfMissing);
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

        private void VisitWithoutDiagnostics(BoundNode? node)
        {
            var previousDiagnostics = _disableDiagnostics;
            _disableDiagnostics = true;
            Visit(node);
            _disableDiagnostics = previousDiagnostics;
        }

        protected override void VisitRvalue(BoundExpression? node, bool isKnownToBeAnLvalue = false)
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
        private void VisitRvalueEpilogue(BoundExpression? node)
        {
            Unsplit();
            UseRvalueOnly(node); // drop lvalue part
        }

        private TypeWithState VisitRvalueWithState(BoundExpression? node)
        {
            VisitRvalue(node);
            return ResultType;
        }

        private TypeWithAnnotations VisitLvalueWithAnnotations(BoundExpression node)
        {
            VisitLValue(node);
            Unsplit();
            return LvalueResultType;
        }

        private static object GetTypeAsDiagnosticArgument(TypeSymbol? typeOpt)
        {
            return typeOpt ?? (object)"<null>";
        }

        private static object GetParameterAsDiagnosticArgument(ParameterSymbol? parameterOpt)
        {
            return parameterOpt is null ?
                (object)"" :
                new FormattedSymbol(parameterOpt, SymbolDisplayFormat.ShortFormat);
        }

        private static object GetContainingSymbolAsDiagnosticArgument(ParameterSymbol? parameterOpt)
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
        /// Should we warn for assigning this state into this type?
        ///
        /// This should often be checked together with <seealso cref="IsDisallowedNullAssignment(TypeWithState, FlowAnalysisAnnotations)"/>
        /// It catches putting a `null` into a `[DisallowNull]int?` for example, which cannot simply be represented as a non-nullable target type.
        /// </summary>
        private static bool ShouldReportNullableAssignment(TypeWithAnnotations type, NullableFlowState state)
        {
            if (!type.HasType ||
                type.Type.IsValueType)
            {
                return false;
            }

            switch (type.NullableAnnotation)
            {
                case NullableAnnotation.Oblivious:
                case NullableAnnotation.Annotated:
                    return false;
            }

            switch (state)
            {
                case NullableFlowState.NotNull:
                    return false;
                case NullableFlowState.MaybeNull:
                    if (type.Type.IsTypeParameterDisallowingAnnotationInCSharp8() && !(type.Type is TypeParameterSymbol { IsNotNullable: true }))
                    {
                        return false;
                    }
                    break;
            }

            return true;
        }

        /// <summary>
        /// Reports top-level nullability problem in assignment.
        /// Any conversion of the value should have been applied.
        /// </summary>
        private void ReportNullableAssignmentIfNecessary(
            BoundExpression? value,
            TypeWithAnnotations targetType,
            TypeWithState valueType,
            bool useLegacyWarnings,
            AssignmentKind assignmentKind = AssignmentKind.Assignment,
            ParameterSymbol? parameterOpt = null,
            Location? location = null)
        {
            // Callers should apply any conversions before calling this method
            // (see https://github.com/dotnet/roslyn/issues/39867).
            if (targetType.HasType &&
                !targetType.Type.Equals(valueType.Type, TypeCompareKind.AllIgnoreOptions))
            {
                return;
            }

            if (value == null || !ShouldReportNullableAssignment(targetType, valueType.State))
            {
                return;
            }

            location ??= value.Syntax.GetLocation();
            var unwrappedValue = SkipReferenceConversions(value);
            if (unwrappedValue.IsSuppressed)
            {
                return;
            }

            if (value.ConstantValueOpt?.IsNull == true && !useLegacyWarnings)
            {
                // Report warning converting null literal to non-nullable reference type.
                // target (e.g.: `object F() => null;` or calling `void F(object y)` with `F(null)`).
                ReportDiagnostic(assignmentKind == AssignmentKind.Return ? ErrorCode.WRN_NullReferenceReturn : ErrorCode.WRN_NullAsNonNullable, location);
            }
            else if (assignmentKind == AssignmentKind.Argument)
            {
                ReportDiagnostic(ErrorCode.WRN_NullReferenceArgument, location,
                    GetParameterAsDiagnosticArgument(parameterOpt),
                    GetContainingSymbolAsDiagnosticArgument(parameterOpt));

                LearnFromNonNullTest(value, ref State);
            }
            else if (useLegacyWarnings)
            {
                if (isMaybeDefaultValue(valueType) && !allowUnconstrainedTypeParameterAnnotations(compilation))
                {
                    // No W warning reported assigning or casting [MaybeNull]T value to T
                    // because there is no syntax for declaring the target type as [MaybeNull]T.
                    return;
                }
                ReportNonSafetyDiagnostic(location);
            }
            else
            {
                ReportDiagnostic(assignmentKind == AssignmentKind.Return ? ErrorCode.WRN_NullReferenceReturn : ErrorCode.WRN_NullReferenceAssignment, location);
            }

            static bool isMaybeDefaultValue(TypeWithState valueType)
            {
                return valueType.Type?.TypeKind == TypeKind.TypeParameter &&
                    valueType.State == NullableFlowState.MaybeDefault;
            }

            static bool allowUnconstrainedTypeParameterAnnotations(CSharpCompilation compilation)
            {
                // Check IDS_FeatureDefaultTypeParameterConstraint feature since `T?` and `where ... : default`
                // are treated as a single feature, even though the errors reported for the two cases are distinct.
                var requiredVersion = MessageID.IDS_FeatureDefaultTypeParameterConstraint.RequiredVersion();
                return requiredVersion <= compilation.LanguageVersion;
            }
        }

        internal static bool AreParameterAnnotationsCompatible(
            RefKind refKind,
            TypeWithAnnotations overriddenType,
            FlowAnalysisAnnotations overriddenAnnotations,
            TypeWithAnnotations overridingType,
            FlowAnalysisAnnotations overridingAnnotations,
            bool forRef = false)
        {
            // We've already checked types and annotations, let's check nullability attributes as well
            // Return value is treated as an `out` parameter (or `ref` if it is a `ref` return)

            if (refKind == RefKind.Ref)
            {
                // ref variables are invariant
                return AreParameterAnnotationsCompatible(RefKind.None, overriddenType, overriddenAnnotations, overridingType, overridingAnnotations, forRef: true) &&
                    AreParameterAnnotationsCompatible(RefKind.Out, overriddenType, overriddenAnnotations, overridingType, overridingAnnotations);
            }

            if (refKind is RefKind.None or RefKind.In or RefKind.RefReadOnlyParameter)
            {
                // pre-condition attributes
                // Check whether we can assign a value from overridden parameter to overriding
                var valueState = GetParameterState(
                    overriddenType,
                    overriddenAnnotations);
                if (isBadAssignment(valueState, overridingType, overridingAnnotations))
                {
                    return false;
                }

                // unconditional post-condition attributes on inputs
                bool overridingHasNotNull = (overridingAnnotations & FlowAnalysisAnnotations.NotNull) == FlowAnalysisAnnotations.NotNull;
                bool overriddenHasNotNull = (overriddenAnnotations & FlowAnalysisAnnotations.NotNull) == FlowAnalysisAnnotations.NotNull;
                if (overriddenHasNotNull && !overridingHasNotNull && !forRef)
                {
                    // Overriding doesn't conform to contract of overridden (ie. promise not to return if parameter is null)
                    return false;
                }

                bool overridingHasMaybeNull = (overridingAnnotations & FlowAnalysisAnnotations.MaybeNull) == FlowAnalysisAnnotations.MaybeNull;
                bool overriddenHasMaybeNull = (overriddenAnnotations & FlowAnalysisAnnotations.MaybeNull) == FlowAnalysisAnnotations.MaybeNull;
                if (overriddenHasMaybeNull && !overridingHasMaybeNull && !forRef)
                {
                    // Overriding doesn't conform to contract of overridden (ie. promise to only return if parameter is null)
                    return false;
                }
            }

            if (refKind == RefKind.Out)
            {
                // post-condition attributes (`Maybe/NotNull` and `Maybe/NotNullWhen`)
                if (!canAssignOutputValueWhen(true) || !canAssignOutputValueWhen(false))
                {
                    return false;
                }
            }

            return true;

            bool canAssignOutputValueWhen(bool sense)
            {
                var valueWhen = ApplyUnconditionalAnnotations(
                    overridingType.ToTypeWithState(),
                    makeUnconditionalAnnotation(overridingAnnotations, sense));

                var destAnnotationsWhen = ToInwardAnnotations(makeUnconditionalAnnotation(overriddenAnnotations, sense));
                if (isBadAssignment(valueWhen, overriddenType, destAnnotationsWhen))
                {
                    // Can't assign value from overriding to overridden in 'sense' case
                    return false;
                }

                return true;
            }

            static bool isBadAssignment(TypeWithState valueState, TypeWithAnnotations destinationType, FlowAnalysisAnnotations destinationAnnotations)
            {
                if (ShouldReportNullableAssignment(
                    ApplyLValueAnnotations(destinationType, destinationAnnotations),
                    valueState.State))
                {
                    return true;
                }

                if (IsDisallowedNullAssignment(valueState, destinationAnnotations))
                {
                    return true;
                }

                return false;
            }

            // Convert both conditional annotations to unconditional ones or nothing
            static FlowAnalysisAnnotations makeUnconditionalAnnotation(FlowAnalysisAnnotations annotations, bool sense)
            {
                if (sense)
                {
                    var unconditionalAnnotationWhenTrue = makeUnconditionalAnnotationCore(annotations, FlowAnalysisAnnotations.NotNullWhenTrue, FlowAnalysisAnnotations.NotNull);
                    return makeUnconditionalAnnotationCore(unconditionalAnnotationWhenTrue, FlowAnalysisAnnotations.MaybeNullWhenTrue, FlowAnalysisAnnotations.MaybeNull);
                }

                var unconditionalAnnotationWhenFalse = makeUnconditionalAnnotationCore(annotations, FlowAnalysisAnnotations.NotNullWhenFalse, FlowAnalysisAnnotations.NotNull);
                return makeUnconditionalAnnotationCore(unconditionalAnnotationWhenFalse, FlowAnalysisAnnotations.MaybeNullWhenFalse, FlowAnalysisAnnotations.MaybeNull);
            }

            // Convert Maybe/NotNullWhen into Maybe/NotNull or nothing
            static FlowAnalysisAnnotations makeUnconditionalAnnotationCore(FlowAnalysisAnnotations annotations, FlowAnalysisAnnotations conditionalAnnotation, FlowAnalysisAnnotations replacementAnnotation)
            {
                if ((annotations & conditionalAnnotation) != 0)
                {
                    return annotations | replacementAnnotation;
                }

                return annotations & ~replacementAnnotation;
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
            BoundExpression? valueOpt,
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

                var newState = valueType.State;
                SetStateAndTrackForFinally(ref this.State, targetSlot, newState);
                InheritDefaultState(targetType.Type, targetSlot);

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

        private bool IsSlotMember(int slot, Symbol possibleMember)
        {
            TypeSymbol possibleBase = possibleMember.ContainingType;
            TypeSymbol possibleDerived = NominalSlotType(slot);
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            var conversionsWithoutNullability = _conversions.WithNullability(false);
            return
                conversionsWithoutNullability.HasIdentityOrImplicitReferenceConversion(possibleDerived, possibleBase, ref discardedUseSiteInfo) ||
                conversionsWithoutNullability.HasBoxingConversion(possibleDerived, possibleBase, ref discardedUseSiteInfo);
        }

        // 'skipSlot' is the original target slot that should be skipped in case of cycles.
        private void InheritNullableStateOfMember(int targetContainerSlot, int valueContainerSlot, Symbol member, bool isDefaultValue, int skipSlot)
        {
            Debug.Assert(targetContainerSlot > 0);
            Debug.Assert(skipSlot > 0);

            // Ensure member is valid for target and value.
            if (!IsSlotMember(targetContainerSlot, member))
                return;

            if (member is SynthesizedBackingFieldSymbol backingField && !isUsable(backingField))
                return;

            TypeWithAnnotations fieldOrPropertyType = GetTypeOrReturnTypeWithAnnotations(member);

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
                        value = valueMemberSlot > 0 ?
                            GetState(ref this.State, valueMemberSlot) :
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

            // Decide if the given 'backingField' can be used in the context of '_symbol'.
            // Filtering on this basis helps us avoid cycles across nullable inference of backing fields.
            bool isUsable(SynthesizedBackingFieldSymbol backingField)
            {
                if (_symbol is not MethodSymbol method)
                    return false;

                if (method.IsConstructor() && method.IsStatic == backingField.IsStatic)
                    return true;

                if (method is SourcePropertyAccessorSymbol { AssociatedSymbol: PropertySymbol prop } && (object)backingField.AssociatedSymbol == prop)
                    return true;

                return false;
            }
        }

        private TypeSymbol NominalSlotType(int slot)
        {
            return GetTypeOrReturnType(_variables[slot].Symbol);
        }

        /// <summary>
        /// Whenever assigning a variable, and that variable is not declared at the point the state is being set,
        /// and the new state is not <see cref="NullableFlowState.NotNull"/>, this method should be called to perform the
        /// state setting and to ensure the mutation is visible outside the finally block when the mutation occurs in a
        /// finally block.
        /// </summary>
        private void SetStateAndTrackForFinally(ref LocalState state, int slot, NullableFlowState newState)
        {
            Debug.Assert(slot > 0);
            SetState(ref state, slot, newState);
            if (newState != NullableFlowState.NotNull && NonMonotonicState.HasValue)
            {
                var tryState = NonMonotonicState.Value;
                if (tryState.HasVariable(slot))
                {
                    SetState(ref tryState, slot, newState.Join(GetState(ref tryState, slot)), useNotNullsAsDefault: true);
                    NonMonotonicState = tryState;
                }
            }
        }

        protected override void JoinTryBlockState(ref LocalState self, ref LocalState other)
        {
            var tryState = other.GetStateForVariables(self.Id);
            Join(ref self, ref tryState);
        }

        private void InheritDefaultState(TypeSymbol targetType, int targetSlot)
        {
            Debug.Assert(targetSlot > 0);

#if DEBUG
            var actualType = GetTypeOrReturnType(_variables[targetSlot].Symbol);
            Debug.Assert(actualType is { });

            if (!actualType.ContainsErrorType() &&
                !targetType.ContainsErrorType())
            {
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                var conversionsWithoutNullability = _conversions.WithNullability(false);
                var conversion = conversionsWithoutNullability.ClassifyImplicitConversionFromType(actualType, targetType, ref discardedUseSiteInfo);
                Debug.Assert(conversion.Kind is ConversionKind.Identity or ConversionKind.ImplicitReference);
            }
#endif

            // Reset the state of any members of the target.
            var members = ArrayBuilder<(VariableIdentifier, int)>.GetInstance();
            _variables.GetMembers(members, targetSlot);
            foreach (var (variable, slot) in members)
            {
                var symbol = AsMemberOfType(targetType, variable.Symbol);
                SetStateAndTrackForFinally(ref this.State, slot, GetDefaultState(symbol));
                InheritDefaultState(GetTypeOrReturnType(symbol), slot);
            }
            members.Free();
        }

        private static TypeSymbol GetTypeOrReturnType(Symbol symbol) => symbol.GetTypeOrReturnType().Type;

        /// <summary>Gets the TypeWithAnnotations of a symbol, possibly using the inferred nullable annotation for backing fields.</summary>
        private TypeWithAnnotations GetTypeOrReturnTypeWithAnnotations(Symbol symbol)
        {
            var typeWithAnnotations = symbol.GetTypeOrReturnType();
            if (symbol is SynthesizedBackingFieldSymbol { InfersNullableAnnotation: true } backingField)
            {
                NullableAnnotation nullableAnnotation;
                if (_getterNullResilienceData is var (analyzedField, assumedNullableAnnotation))
                {
                    if ((object)analyzedField != backingField)
                    {
                        // If we find a usage of a different backing field, than the one we are currently doing a null resilience analysis on,
                        // we must not call 'GetInferredNullableAnnotation' on it. Doing that could cause a cycle across inference of multiple fields.
                        // We generally don't want this code path to be hit. However, it's difficult to guarantee that it will never happen, and isn't worth crashing the retail compiler when it happens.
                        // In retail builds, we should proceed by using the non-inferred property nullability associated with the field.
                        Debug.Assert(false);
                        nullableAnnotation = backingField.TypeWithAnnotations.NullableAnnotation;
                    }
                    else
                    {
                        // Currently in the process of inferring the nullable annotation for 'backingField'.
                        // Therefore don't try to access the inferred nullable annotation, use a temporary assumedNullableAnnotation instead.
                        nullableAnnotation = assumedNullableAnnotation;
                    }
                }
                else
                {
                    nullableAnnotation = backingField.GetInferredNullableAnnotation();
                }

                typeWithAnnotations = TypeWithAnnotations.Create(typeWithAnnotations.Type, nullableAnnotation);
            }

            return typeWithAnnotations;
        }

        private NullableFlowState GetDefaultState(Symbol symbol)
        {
            return ApplyUnconditionalAnnotations(GetTypeOrReturnTypeWithAnnotations(symbol).ToTypeWithState(), GetRValueAnnotations(symbol)).State;
        }

        private void InheritNullableStateOfTrackableType(int targetSlot, int valueSlot, int skipSlot)
        {
            Debug.Assert(targetSlot > 0);
            Debug.Assert(valueSlot > 0);

            // Clone the state for members that have been set on the value.
            var members = ArrayBuilder<(VariableIdentifier, int)>.GetInstance();
            _variables.GetMembers(members, valueSlot);
            foreach (var (variable, slot) in members)
            {
                var member = variable.Symbol;
                Debug.Assert(member.Kind == SymbolKind.Field || member.Kind == SymbolKind.Property || member.Kind == SymbolKind.Event);
                InheritNullableStateOfMember(targetSlot, valueSlot, member, isDefaultValue: false, skipSlot);
            }
            members.Free();
        }

        protected override LocalState TopState()
        {
            var state = LocalState.ReachableState(_variables);
            state.PopulateAll(this);
            return state;
        }

        protected override LocalState UnreachableState()
        {
            return LocalState.UnreachableState(_variables);
        }

        protected override LocalState ReachableBottomState()
        {
            // Create a reachable state in which all variables are known to be non-null.
            return LocalState.ReachableStateWithNotNulls(_variables);
        }

        private void EnterParameters()
        {
            if (!(CurrentSymbol is MethodSymbol methodSymbol))
            {
                return;
            }

            if (methodSymbol is SynthesizedPrimaryConstructor)
            {
                if (_hasInitialState)
                {
                    // Primary constructor's parameters are entered before analyzing initializers.
                    // On the second pass, the correct parameter states (potentially modified by initializers)
                    // are contained in the initial state.
                    return;
                }
            }
            else if (methodSymbol.IsConstructor())
            {
                if (!_hasInitialState)
                {
                    // For most constructors, we only enter parameters after analyzing initializers.
                    return;
                }
            }

            var methodParameters = methodSymbol.Parameters;
            var signatureParameters = (_useDelegateInvokeParameterTypes ? _delegateInvokeMethod! : methodSymbol).Parameters;

            // save a state representing the possibility that parameter default values were not assigned to the parameters.
            var parameterDefaultsNotAssignedState = State.Clone();
            for (int i = 0; i < methodParameters.Length; i++)
            {
                var parameter = methodParameters[i];
                // In error scenarios, the method can potentially have more parameters than the signature. If so, use the parameter type for those
                // errored parameters
                var parameterType = i >= signatureParameters.Length ? parameter.TypeWithAnnotations : signatureParameters[i].TypeWithAnnotations;
                EnterParameter(parameter, parameterType);
            }
            Join(ref State, ref parameterDefaultsNotAssignedState);
        }

        private void EnterParameter(ParameterSymbol parameter, TypeWithAnnotations parameterType)
        {
            _variables.SetType(parameter, parameterType);

            if (parameter.RefKind != RefKind.Out)
            {
                int slot = GetOrCreateSlot(parameter);

                Debug.Assert(!IsConditionalState);
                if (slot > 0)
                {
                    var state = GetParameterState(parameterType, parameter.FlowAnalysisAnnotations).State;
                    SetState(ref this.State, slot, state);
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
        }

        public override BoundNode? VisitParameterEqualsValue(BoundParameterEqualsValue equalsValue)
        {
            var parameter = equalsValue.Parameter;
            var parameterAnnotations = GetParameterAnnotations(parameter);
            var parameterLValueType = ApplyLValueAnnotations(parameter.TypeWithAnnotations, parameterAnnotations);

            var resultType = VisitOptionalImplicitConversion(
                equalsValue.Value,
                parameterLValueType,
                useLegacyWarnings: false,
                trackMembers: false,
                assignmentKind: AssignmentKind.Assignment);
            Unsplit();

            // If the LHS has annotations, we perform an additional check for nullable value types
            CheckDisallowedNullAssignment(resultType, parameterAnnotations, equalsValue.Value.Syntax);

            return null;
        }

        internal static TypeWithState GetParameterState(TypeWithAnnotations parameterType, FlowAnalysisAnnotations parameterAnnotations)
        {
            if ((parameterAnnotations & FlowAnalysisAnnotations.AllowNull) != 0)
            {
                return TypeWithState.Create(parameterType.Type, NullableFlowState.MaybeDefault);
            }

            if ((parameterAnnotations & FlowAnalysisAnnotations.DisallowNull) != 0)
            {
                return TypeWithState.Create(parameterType.Type, NullableFlowState.NotNull);
            }

            return parameterType.ToTypeWithState();
        }

        public sealed override BoundNode? VisitReturnStatement(BoundReturnStatement node)
        {
            Debug.Assert(!IsConditionalState);

            var expr = node.ExpressionOpt;
            if (expr == null)
            {
                EnforceDoesNotReturn(node.Syntax);
                PendingBranches.Add(new PendingBranch(node, this.State, label: null));
                SetUnreachable();
                return null;
            }

            // Should not convert to method return type when inferring return type (when _returnTypesOpt != null).
            if (_returnTypesOpt == null &&
                TryGetReturnType(out TypeWithAnnotations returnType, out FlowAnalysisAnnotations returnAnnotations))
            {
                if (node.RefKind == RefKind.None &&
                    returnType.Type.SpecialType == SpecialType.System_Boolean)
                {
                    // visit the expression without unsplitting, then check parameters marked with flow analysis attributes
                    Visit(expr);
                }
                else
                {
                    TypeWithState returnState;
                    if (node.RefKind == RefKind.None)
                    {
                        returnState = VisitOptionalImplicitConversion(expr, returnType, useLegacyWarnings: false, trackMembers: false, AssignmentKind.Return);
                    }
                    else
                    {
                        // return ref expr;
                        returnState = VisitRefExpression(expr, returnType);
                    }

                    // If the return has annotations, we perform an additional check for nullable value types
                    CheckDisallowedNullAssignment(returnState, ToInwardAnnotations(returnAnnotations), node.Syntax, boundValueOpt: expr);
                }
            }
            else
            {
                var result = VisitRvalueWithState(expr);
                if (_returnTypesOpt != null)
                {
                    _returnTypesOpt.Add((node, result.ToTypeWithAnnotations(compilation)));
                }
            }

            EnforceDoesNotReturn(node.Syntax);

            if (IsConditionalState)
            {
                var joinedState = this.StateWhenTrue.Clone();
                Join(ref joinedState, ref this.StateWhenFalse);
                PendingBranches.Add(new PendingBranch(node, joinedState, label: null, this.IsConditionalState, this.StateWhenTrue, this.StateWhenFalse));
            }
            else
            {
                PendingBranches.Add(new PendingBranch(node, this.State, label: null));
            }

            Unsplit();
            if (CurrentSymbol is MethodSymbol method)
            {
                ImmutableArray<ParameterSymbol> parameters = method.GetParametersIncludingExtensionParameter(skipExtensionIfStatic: true);
                EnforceNotNullIfNotNull(node.Syntax, this.State, parameters, method.ReturnNotNullIfParameterNotNull, ResultType.State, outputParam: null);
            }

            SetUnreachable();

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

        private bool TryGetReturnType(out TypeWithAnnotations type, out FlowAnalysisAnnotations annotations)
        {
            var method = CurrentSymbol as MethodSymbol;
            if (method is null)
            {
                type = default;
                annotations = FlowAnalysisAnnotations.None;
                return false;
            }

            var delegateOrMethod = _useDelegateInvokeReturnType ? _delegateInvokeMethod! : method;
            var returnType = delegateOrMethod.ReturnTypeWithAnnotations;
            Debug.Assert((object)returnType != LambdaSymbol.ReturnTypeIsBeingInferred);

            if (returnType.IsVoidType())
            {
                type = default;
                annotations = FlowAnalysisAnnotations.None;
                return false;
            }

            if (!method.IsAsync)
            {
                annotations = method.ReturnTypeFlowAnalysisAnnotations;
                type = ApplyUnconditionalAnnotations(returnType, annotations);
                return true;
            }

            if (method.IsAsyncEffectivelyReturningGenericTask(compilation))
            {
                type = ((NamedTypeSymbol)returnType.Type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Single();
                annotations = FlowAnalysisAnnotations.None;
                return true;
            }

            type = default;
            annotations = FlowAnalysisAnnotations.None;
            return false;
        }

        public override BoundNode? VisitLocal(BoundLocal node)
        {
            // Ignore var self-references (e.g., the RHS of `var x = x;`) to avoid cycles.
            // While inferring the type of a more complex construct (like lambda),
            // nullability analysis could be triggered against a reference of the local being inferred,
            // querying its type and hence starting the same type inference recursively.
            if (node.Type == (object)this.compilation.ImplicitlyTypedVariableUsedInForbiddenZoneType)
            {
                SetResultType(node, TypeWithState.ForType(node.Type));
                return null;
            }

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

            SetResult(node, GetAdjustedResult(type.ToTypeWithState(), slot), type);
            SplitIfBooleanConstant(node);
            return null;
        }

        public override BoundNode? VisitBlock(BoundBlock node)
        {
            DeclareLocals(node.Locals);
            VisitStatementsWithLocalFunctions(node);

            return null;
        }

        private void VisitStatementsWithLocalFunctions(BoundBlock block)
        {
            // Since the nullable flow state affects type information, and types can be queried by
            // the semantic model, there needs to be a single flow state input to a local function
            // that cannot be path-dependent. To decide the local starting state we Meet the state
            // of captured variables from all the uses of the local function, computing the
            // conservative combination of all potential starting states.
            //
            // For performance we split the analysis into two phases: the first phase where we
            // analyze everything except the local functions, hoping to visit all of the uses of the
            // local function, and then a pass where we visit the local functions. If there's no
            // recursion or calls between the local functions, the starting state of the local
            // function should be stable and we don't need a second pass.
            if (!block.LocalFunctions.IsDefaultOrEmpty)
            {
                Debug.Assert(!TrackingRegions);

                // First visit everything else
                var localFuncs = ArrayBuilder<BoundLocalFunctionStatement?>.GetInstance();
                foreach (var stmt in block.Statements)
                {
                    if (stmt is BoundLocalFunctionStatement localFunc)
                    {
                        localFuncs.Add(localFunc);
                    }
                    else
                    {
                        VisitStatement(stmt);
                    }
                }

                // Visited local functions will be set to null.
                // We keep count of unvisited (non-null) local functions.
                var unvisitedLocalFuncs = localFuncs.Count;

                // Now visit the local function bodies.
                // We first visit those we have seen called (hence we know their starting state),
                // repeating this while visiting new bodies (which might contain more call sites).
                // This avoids unnecessary passes and incorrect starting states of reachable bodies
                // (which could be captured in loop head state for example).
                for (bool newBodiesVisited = true; newBodiesVisited;)
                {
                    newBodiesVisited = false;

                    for (int i = 0; unvisitedLocalFuncs != 0 && i < localFuncs.Count; i++)
                    {
                        // We visit the body only if the function's usages state has been created
                        // which happens when we visit the function's call site or its body.
                        // In the first pass of the nullable walker, existence of usages here means that a call site has been visited.
                        // In subsequent nullable walker passes, the starting state is preserved from previous passes.
                        // In any case, existence of usages means that we have a good starting state.
                        if (localFuncs[i] is { } localFunc && HasLocalFuncUsagesCreated((LocalFunctionSymbol)localFunc.Symbol))
                        {
                            localFuncs[i] = null;
                            unvisitedLocalFuncs--;

                            // The body of this local function might contain calls to other local functions,
                            // hence we will rerun the outer loop to visit bodies of newly-called functions if any.
                            newBodiesVisited = true;

                            TakeIncrementalSnapshot(localFunc);
                            VisitLocalFunctionStatement(localFunc);
                        }
                    }

                    // If we haven't visited new bodies in this iteration, visit an unreachable function if any.
                    // This might make other functions reachable, so we will continue with the outer loop.
                    if (!newBodiesVisited && unvisitedLocalFuncs != 0)
                    {
                        for (int i = 0; i < localFuncs.Count; i++)
                        {
                            if (localFuncs[i] is { } localFunc)
                            {
                                localFuncs[i] = null;
                                unvisitedLocalFuncs--;
                                newBodiesVisited = true;
                                TakeIncrementalSnapshot(localFunc);
                                VisitLocalFunctionStatement(localFunc);
                                break;
                            }
                        }

                        Debug.Assert(newBodiesVisited);
                    }
                }

                localFuncs.Free();
            }
            else
            {
                foreach (var stmt in block.Statements)
                {
                    VisitStatement(stmt);
                }
            }
        }

        public override BoundNode? VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            var localFunc = (LocalFunctionSymbol)node.Symbol;

            // Usages state is created when we visit the function's call site or its body.
            // In the first pass of the nullable walker, existence of usages here means that a call site has been visited.
            // In subsequent nullable walker passes, the starting state is preserved from previous passes.
            // In any case, existence of usages means that we have a good starting state.
            var hasGoodStartingState = HasLocalFuncUsagesCreated(localFunc);

            var localFunctionState = GetOrCreateLocalFuncUsages(localFunc);

            // The state for the function's body analysis starts as the top state ("maybe null").
            var state = TopState();

            if (!hasGoodStartingState)
            {
                // For unreachable local functions, top-level captured variables are set to "not null",
                // ignoring nested slots to avoid depending on slot allocation order
                // (e.g., whether we have seen a class field or not already).
                state.Normalize(this, _variables);
                state.ForEach(
                    (slot, variables) =>
                    {
                        if (Symbol.IsCaptured(variables[slot].Symbol, localFunc))
                        {
                            SetState(ref state, slot, NullableFlowState.NotNull);
                        }
                    },
                    _variables);

                // In subsequent passes, we will use the starting state,
                // so make sure it's set correctly for unreachable functions from now on.
                localFunctionState.StartingState = state.Clone();
            }
            else
            {
                // Captured variables are joined with the state
                // from visited call sites of the local function.
                var startingState = localFunctionState.StartingState;
                startingState.ForEach(
                    (slot, variables) =>
                    {
                        var symbol = variables[variables.RootSlot(slot)].Symbol;
                        if (Symbol.IsCaptured(symbol, localFunc))
                        {
                            SetState(ref state, slot, GetState(ref startingState, slot));
                        }
                    },
                    _variables);
            }

            localFunctionState.Visited = true;

            AnalyzeLocalFunctionOrLambda(
                node,
                localFunc,
                state,
                delegateInvokeMethod: null,
                useDelegateInvokeParameterTypes: false,
                useDelegateInvokeReturnType: false);

            SetInvalidResult();

            return null;
        }

        private Variables GetOrCreateNestedFunctionVariables(Variables container, MethodSymbol lambdaOrLocalFunction)
        {
            _nestedFunctionVariables ??= PooledDictionary<MethodSymbol, Variables>.GetInstance();
            if (!_nestedFunctionVariables.TryGetValue(lambdaOrLocalFunction, out var variables))
            {
                variables = container.CreateNestedMethodScope(lambdaOrLocalFunction);
                _nestedFunctionVariables.Add(lambdaOrLocalFunction, variables);
            }
            Debug.Assert((object?)variables.Container == container);
            return variables;
        }

        private void AnalyzeLocalFunctionOrLambda(
            IBoundLambdaOrFunction lambdaOrFunction,
            MethodSymbol lambdaOrFunctionSymbol,
            LocalState state,
            MethodSymbol? delegateInvokeMethod,
            bool useDelegateInvokeParameterTypes,
            bool useDelegateInvokeReturnType)
        {
            Debug.Assert(!useDelegateInvokeParameterTypes || delegateInvokeMethod is object);
            Debug.Assert(!useDelegateInvokeReturnType || delegateInvokeMethod is object);

            var oldSymbol = this._symbol;
            this._symbol = lambdaOrFunctionSymbol;
            var oldCurrentSymbol = this.CurrentSymbol;
            this.CurrentSymbol = lambdaOrFunctionSymbol;

            var oldDelegateInvokeMethod = _delegateInvokeMethod;
            _delegateInvokeMethod = delegateInvokeMethod;
            var oldUseDelegateInvokeParameterTypes = _useDelegateInvokeParameterTypes;
            _useDelegateInvokeParameterTypes = useDelegateInvokeParameterTypes;
            var oldUseDelegateInvokeReturnType = _useDelegateInvokeReturnType;
            _useDelegateInvokeReturnType = useDelegateInvokeReturnType;

            var oldReturnTypes = _returnTypesOpt;
            _returnTypesOpt = null;
#if DEBUG
            var oldCompletingTargetTypedExpression = _completingTargetTypedExpression;
            _completingTargetTypedExpression = false;
#endif
            var oldState = this.State;
            _variables = GetOrCreateNestedFunctionVariables(_variables, lambdaOrFunctionSymbol);
            this.State = state.CreateNestedMethodState(_variables);
            var previousSlot = _snapshotBuilderOpt?.EnterNewWalker(lambdaOrFunctionSymbol) ?? -1;

            try
            {
                var oldPending = SavePending();

                EnterParameters();

                bool isLocalFunction = lambdaOrFunctionSymbol is LocalFunctionSymbol;
                if (isLocalFunction)
                {
                    MakeMembersMaybeNull(lambdaOrFunctionSymbol, lambdaOrFunctionSymbol.NotNullMembers);
                    MakeMembersMaybeNull(lambdaOrFunctionSymbol, lambdaOrFunctionSymbol.NotNullWhenTrueMembers);
                    MakeMembersMaybeNull(lambdaOrFunctionSymbol, lambdaOrFunctionSymbol.NotNullWhenFalseMembers);
                }

                var oldPending2 = SavePending();

                // If this is an iterator, there's an implicit branch before the first statement
                // of the function where the enumerable is returned.
                if (lambdaOrFunctionSymbol.IsIterator)
                {
                    PendingBranches.Add(new PendingBranch(null, this.State, null));
                }

                VisitAlways(lambdaOrFunction.Body);
                EnforceDoesNotReturn(syntaxOpt: null);
                if (isLocalFunction)
                {
                    enforceMemberNotNull(((LocalFunctionSymbol)lambdaOrFunctionSymbol).Syntax, this.State);
                }
                EnforceParameterNotNullOnExit(null, this.State);

                RestorePending(oldPending2); // process any forward branches within the lambda body

                ImmutableArray<PendingBranch> pendingReturns = RemoveReturns();
                foreach (var pendingReturn in pendingReturns)
                {
                    if (isLocalFunction)
                    {
                        enforceMemberNotNull(syntax: pendingReturn.Branch?.Syntax, pendingReturn.State);
                    }

                    if (pendingReturn.Branch is BoundReturnStatement returnStatement)
                    {
                        EnforceParameterNotNullOnExit(returnStatement.Syntax, pendingReturn.State);
                        EnforceNotNullWhenForPendingReturn(pendingReturn, returnStatement);
                        if (isLocalFunction)
                        {
                            EnforceMemberNotNullWhenForPendingReturn(pendingReturn, returnStatement);
                        }
                    }
                }

                RestorePending(oldPending);
            }
            finally
            {
                _snapshotBuilderOpt?.ExitWalker(this.SaveSharedState(), previousSlot);
            }

            _variables = _variables.Container!;
            this.State = oldState;
#if DEBUG
            _completingTargetTypedExpression = oldCompletingTargetTypedExpression;
#endif
            _returnTypesOpt = oldReturnTypes;
            _useDelegateInvokeReturnType = oldUseDelegateInvokeReturnType;
            _useDelegateInvokeParameterTypes = oldUseDelegateInvokeParameterTypes;
            _delegateInvokeMethod = oldDelegateInvokeMethod;
            this.CurrentSymbol = oldCurrentSymbol;
            this._symbol = oldSymbol;
            return;

            void enforceMemberNotNull(SyntaxNode? syntax, LocalState state)
            {
                if (!state.Reachable)
                    return;

                var method = (LocalFunctionSymbol)_symbol;
                foreach (var memberName in method.NotNullMembers)
                {
                    EnforceMemberNotNullOnMember(syntax, state, method, memberName);
                }
            }
        }

        protected override void VisitLocalFunctionUse(
            LocalFunctionSymbol symbol,
            LocalFunctionState localFunctionState,
            SyntaxNode syntax,
            bool isCall)
        {
            // Do not use this overload in NullableWalker. Use the overload below instead.
            throw ExceptionUtilities.Unreachable();
        }

        private void VisitLocalFunctionUse(LocalFunctionSymbol symbol)
        {
            Debug.Assert(!IsConditionalState);
            var localFunctionState = GetOrCreateLocalFuncUsages(symbol);
            var state = State.GetStateForVariables(localFunctionState.StartingState.Id);
            if (Join(ref localFunctionState.StartingState, ref state) &&
                localFunctionState.Visited)
            {
                // If the starting state of the local function has changed and we've already visited
                // the local function, we need another pass
                stateChangedAfterUse = true;
            }
        }

        public override BoundNode? VisitDoStatement(BoundDoStatement node)
        {
            DeclareLocals(node.Locals);
            return base.VisitDoStatement(node);
        }

        public override BoundNode? VisitWhileStatement(BoundWhileStatement node)
        {
            DeclareLocals(node.Locals);
            return base.VisitWhileStatement(node);
        }

        public override BoundNode? VisitWithExpression(BoundWithExpression withExpr)
        {
            Debug.Assert(!IsConditionalState);

            var receiver = withExpr.Receiver;
            VisitRvalue(receiver);
            _ = CheckPossibleNullReceiver(receiver);

            var resultType = ResultType.ToTypeWithAnnotations(compilation);
            var resultState = ApplyUnconditionalAnnotations(resultType.ToTypeWithState(), GetRValueAnnotations(withExpr.CloneMethod));
            var resultSlot = GetOrCreatePlaceholderSlot(withExpr);
            // carry over the null state of members of 'receiver' to the result of the with-expression.
            TrackNullableStateForAssignment(receiver, resultType, resultSlot, resultState, MakeSlot(receiver));
            // use the declared nullability of Clone() for the top-level nullability of the result of the with-expression.
            SetResult(withExpr, resultState, resultType);
            VisitObjectCreationInitializer(resultSlot, resultType.Type, withExpr.InitializerExpression, delayCompletionForType: false);

            return null;
        }

        public override BoundNode? VisitForStatement(BoundForStatement node)
        {
            DeclareLocals(node.OuterLocals);
            DeclareLocals(node.InnerLocals);
            return base.VisitForStatement(node);
        }

        public override BoundNode? VisitForEachStatement(BoundForEachStatement node)
        {
            DeclareLocals(node.IterationVariables);
            return base.VisitForEachStatement(node);
        }

        public override BoundNode? VisitUsingStatement(BoundUsingStatement node)
        {
            DeclareLocals(node.Locals);
            Visit(node.AwaitOpt);
            return base.VisitUsingStatement(node);
        }

        public override BoundNode? VisitUsingLocalDeclarations(BoundUsingLocalDeclarations node)
        {
            Visit(node.AwaitOpt);
            return base.VisitUsingLocalDeclarations(node);
        }

        public override BoundNode? VisitFixedStatement(BoundFixedStatement node)
        {
            DeclareLocals(node.Locals);
            return base.VisitFixedStatement(node);
        }

        public override BoundNode? VisitConstructorMethodBody(BoundConstructorMethodBody node)
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
                    SetState(ref this.State, slot, GetDefaultState(ref this.State, slot));
                    InheritDefaultState(GetDeclaredLocalResult(local).Type, slot);
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

        public override BoundNode? VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            var local = node.LocalSymbol;
            int slot = GetOrCreateSlot(local);

            // We need visit the optional arguments so that we can return nullability information
            // about them, but we don't want to communicate any information about anything underneath.
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
                Unsplit();
            }

            if (inferredType)
            {
                if (valueType.HasNullType)
                {
                    Debug.Assert(type.Type.IsErrorType());
                    valueType = type.ToTypeWithState();
                }

                type = valueType.ToAnnotatedTypeWithAnnotations(compilation);
                _variables.SetType(local, type);

                if (node.DeclaredTypeOpt != null)
                {
                    SetAnalyzedNullability(node.DeclaredTypeOpt, new VisitResult(type.ToTypeWithState(), type), true);
                }
            }

            TrackNullableStateForAssignment(initializer, type, slot, valueType, MakeSlot(initializer));
            return null;
        }

        protected override BoundNode? VisitExpressionOrPatternWithoutStackGuard(BoundNode node)
        {
            Debug.Assert(node is BoundExpression or BoundPattern);
            Debug.Assert(!IsConditionalState);
            SetInvalidResult();
            _ = base.VisitExpressionOrPatternWithoutStackGuard(node);
            if (node is BoundExpression expr)
            {
                VisitExpressionWithoutStackGuardEpilogue(expr);
            }

            return null;
        }

        private void VisitExpressionWithoutStackGuardEpilogue(BoundExpression node)
        {
            TypeWithState resultType = ResultType;

#if DEBUG
            // Verify Visit method set _result.
            Debug.Assert((object?)resultType.Type != _invalidType.Type);
            Debug.Assert(AreCloseEnough(resultType.Type, node.Type));
#endif

            if (shouldMakeNotNullRvalue(node) && _visitResult.NestedVisitResults is null && !_visitResult.StateForLambda.HasValue)
            {
                var result = resultType.WithNotNullState();
                SetResult(node, result, LvalueResultType);
            }
            return;

            bool shouldMakeNotNullRvalue(BoundExpression node) => node.IsSuppressed || node.HasAnyErrors || !IsReachable();
        }

#if DEBUG
        // For asserts only.
        private static bool AreCloseEnough(TypeSymbol? typeA, TypeSymbol? typeB)
        {
            // https://github.com/dotnet/roslyn/issues/34993: We should be able to tighten this to ensure that we're actually always returning the same type,
            // not error if one is null or ignoring certain types
            if ((object?)typeA == typeB)
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

            static bool canIgnoreAnyType(TypeSymbol type)
            {
                return type.VisitType((t, unused1, unused2) => canIgnoreType(t), (object?)null) is object;
            }
            static bool canIgnoreType(TypeSymbol type)
            {
                return type.IsErrorType() || type.IsDynamic() || type.HasUseSiteError || (type.IsAnonymousType && canIgnoreAnonymousType((NamedTypeSymbol)type));
            }
            static bool canIgnoreAnonymousType(NamedTypeSymbol type)
            {
                return AnonymousTypeManager.GetAnonymousTypeFieldTypes(type).Any(static t => canIgnoreAnyType(t.Type));
            }
        }

        private static bool AreCloseEnough(Symbol original, Symbol updated)
        {
            // When https://github.com/dotnet/roslyn/issues/38195 is fixed, this workaround needs to be removed
            if (original is ConstructedMethodSymbol || updated is ConstructedMethodSymbol)
            {
                return AreCloseEnough(original.OriginalDefinition, updated.OriginalDefinition);
            }

            return (original, updated) switch
            {
                (LambdaSymbol l, NamedTypeSymbol n) _ when n.IsDelegateType() => AreLambdaAndNewDelegateSimilar(l, n),
                (FieldSymbol { ContainingType: { IsTupleType: true }, TupleElementIndex: var oi } originalField, FieldSymbol { ContainingType: { IsTupleType: true }, TupleElementIndex: var ui } updatedField) =>
                    originalField.Type.Equals(updatedField.Type, TypeCompareKind.AllNullableIgnoreOptions | TypeCompareKind.IgnoreTupleNames) && oi == ui,
                _ => original.Equals(updated, TypeCompareKind.AllNullableIgnoreOptions | TypeCompareKind.IgnoreTupleNames)
            };
        }
#endif

        private static bool AreLambdaAndNewDelegateSimilar(LambdaSymbol l, NamedTypeSymbol n)
        {
            var invokeMethod = n.DelegateInvokeMethod;
            return invokeMethod!.Parameters.SequenceEqual(l.Parameters,
                        (p1, p2) => p1.Type.Equals(p2.Type, TypeCompareKind.AllNullableIgnoreOptions | TypeCompareKind.IgnoreTupleNames)) &&
                   invokeMethod.ReturnType.Equals(l.ReturnType, TypeCompareKind.AllNullableIgnoreOptions | TypeCompareKind.IgnoreTupleNames);
        }

        public override BoundNode? Visit(BoundNode? node)
        {
            return Visit(node, expressionIsRead: true);
        }

        private BoundNode VisitLValue(BoundNode node)
        {
            return Visit(node, expressionIsRead: false);
        }

        private static bool TypeAllowsConditionalState(TypeSymbol? type)
        {
            return type is not null
                && (type.SpecialType == SpecialType.System_Boolean || type.IsDynamic() || type.IsErrorType());
        }

        private void UnsplitIfNeeded(TypeSymbol? type)
        {
            if (!TypeAllowsConditionalState(type))
            {
                Unsplit();
            }
        }

        private BoundNode Visit(BoundNode? node, bool expressionIsRead)
        {
#if DEBUG
            Debug.Assert(!_completingTargetTypedExpression);
#endif
            bool originalExpressionIsRead = _expressionIsRead;
            _expressionIsRead = expressionIsRead;

            TakeIncrementalSnapshot(node);
            var result = base.Visit(node);

            _expressionIsRead = originalExpressionIsRead;
            return result;
        }

        protected override void VisitStatement(BoundStatement statement)
        {
            SetInvalidResult();
            base.VisitStatement(statement);
            SetInvalidResult();
        }

        public override BoundNode? VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            VisitObjectCreationExpressionBase(node);
            return null;
        }

        public override BoundNode? VisitUnconvertedObjectCreationExpression(BoundUnconvertedObjectCreationExpression node)
        {
            // This method is only involved in method inference with unbound lambdas.
            // The diagnostics on arguments are reported by VisitObjectCreationExpression.
            SetResultType(node, TypeWithState.Create(null, NullableFlowState.NotNull));
            return null;
        }

        public override BoundNode? VisitUnconvertedCollectionExpression(BoundUnconvertedCollectionExpression node)
        {
            // This method is only involved in method inference with unbound lambdas.
            var result = base.VisitUnconvertedCollectionExpression(node);
            SetResultType(node, TypeWithState.Create(null, NullableFlowState.NotNull));
            return result;
        }

        public override BoundNode? VisitCollectionExpression(BoundCollectionExpression node)
        {
            // When the collection is target-typed, we can initially only visit the elements and update the state.
            // When the target-typing conversion is processed, the completion continuation will be given a target-type and
            // we'll be able to process the element conversions and compute the final visit result.

            // Walk into the creation side first (generally, this corresponds to the 'with(...)' elements if present.
            // This will ensure any nullable changes in those arguments are reflected before we walk into the elements.
            // Note: the creation side may reference a placeholder representing the actual elements to add to the
            // collection.  For example `SomeCollection.Create(withArg1, withArg2, <placeholder_for_elements>)`. In this
            // case, we know the type of that place holder and that it is not nullable (it is a ReadOnlySpan).  Populate
            // the right maps so walking into the creation understands this.

            var collectionCreationCompletion = visitCollectionCreationArguments(node);

            var (collectionKind, targetElementType) = getCollectionDetails(node, node.Type);

            var resultBuilder = ArrayBuilder<VisitResult>.GetInstance(node.Elements.Length);
            var elementConversionCompletions = ArrayBuilder<Action<TypeWithAnnotations /*targetElementType*/, TypeSymbol /*targetCollectionType*/>>.GetInstance();
            foreach (var element in node.Elements)
            {
                visitElement(element, node, targetElementType, elementConversionCompletions);
                resultBuilder.Add(_visitResult);
            }

            if (node.WasTargetTyped)
            {
                // We're in the context of a conversion, so the analysis of element conversions and the final visit result
                // will be completed later (when that conversion is processed).
                TargetTypedAnalysisCompletion[node] =
                    (TypeWithAnnotations resultTypeWithAnnotations) => convertCollection(
                        node, resultTypeWithAnnotations, collectionCreationCompletion, elementConversionCompletions);
            }
            else
            {
                // We're not in the context of a conversion, so don't expect any target-type information to be provided later,
                // so we're done. For example, `[1, 2].ToString()`.
                elementConversionCompletions.Free();
            }

            var resultType = TypeWithAnnotations.Create(node.Type);
            var visitResult = new VisitResult(TypeWithState.Create(resultType), resultType,
                nestedVisitResults: resultBuilder.ToArrayAndFree());

            SetResult(node, visitResult, updateAnalyzedNullability: !node.WasTargetTyped, isLvalue: false);
            return null;

            Action<TypeSymbol>? visitCollectionCreationArguments(BoundCollectionExpression node)
            {
                var collectionCreation = node.GetUnconvertedCollectionCreation();
                if (collectionCreation is BoundObjectCreationExpression objectCreation &&
                    node.CollectionTypeKind == CollectionExpressionTypeKind.ImplementsIEnumerable)
                {
                    // Walk into the arguments of the object creation, passing in 'delayCompletionForTargetMember: true'
                    // so that we reprocess the nullability of the arguments when we have the target-type for the
                    // collection expression.
                    var (_, argumentResults, _, completion) = this.VisitArguments(
                        objectCreation,
                        objectCreation.Arguments,
                        objectCreation.ArgumentRefKindsOpt,
                        objectCreation.Constructor.Parameters,
                        objectCreation.ArgsToParamsOpt,
                        objectCreation.DefaultArguments,
                        objectCreation.Expanded,
                        invokedAsExtensionMethod: false,
                        objectCreation.Constructor,
                        delayCompletionForTargetMember: true);
                    Debug.Assert(completion != null);

                    return collectionFinalType =>
                    {
                        // Find the actual constructor we are calling into now that we know the real target-type of the
                        // collection expression.
                        var constructor = (MethodSymbol)AsMemberOfType(collectionFinalType, objectCreation.Constructor);
                        completion(argumentResults, constructor.Parameters, constructor);
                    };
                }
                else if (collectionCreation is BoundCall call)
                {
                    var collectionBuilderElementsPlaceholder = node.CollectionBuilderElementsPlaceholder;
                    Debug.Assert(collectionBuilderElementsPlaceholder != null);

                    AddPlaceholderReplacement(
                        collectionBuilderElementsPlaceholder,
                        collectionBuilderElementsPlaceholder,
                        result: new VisitResult(
                            collectionBuilderElementsPlaceholder.Type,
                            NullableAnnotation.NotAnnotated,
                            NullableFlowState.NotNull));

                    var (_, argumentResults, _, completion) = this.VisitArguments(
                        call,
                        call.Arguments,
                        call.ArgumentRefKindsOpt,
                        call.Method.Parameters,
                        call.ArgsToParamsOpt,
                        call.DefaultArguments,
                        call.Expanded,
                        call.InvokedAsExtensionMethod,
                        call.Method,
                        delayCompletionForTargetMember: true);
                    Debug.Assert(completion != null);

                    RemovePlaceholderReplacement(collectionBuilderElementsPlaceholder);

                    return collectionFinalType =>
                    {
                        // BoundCalls are calls to CollectionBuilder factory methods.  In the case where they are generic,
                        // our first pass will have substituted oblivious types in the type signature for the type
                        // arguments.  So we'll have a signature like:
                        //
                        //      SomeCollection<X~,Y~> Create<X~,Y~>(...X~..., ReadOnlySpan<ElementType> t).
                        //
                        // Now that we have determined the real final collection type (say `SomeCollection<X!, Y?>`) we need
                        // to go back through and reanalyze the arguments to the BoundCall with the proper substitutions for
                        // X and Y.
                        //
                        // Note: the language requires (and our conversion logic ensures) that the creation method has the
                        // same arity as the collection type.  So in order to get the final creation method we just need to
                        // take the final collection type and use its type arguments to re-construct the creation method.

                        var allTypeArguments = ((NamedTypeSymbol)collectionFinalType).GetAllTypeArgumentsNoUseSiteDiagnostics();
                        Debug.Assert(allTypeArguments.Length == call.Method.Arity, "Guaranteed by GetCollectionBuilderMethods");

                        var constructed = call.Method.Arity == 0 ? call.Method : call.Method.ConstructedFrom.Construct(allTypeArguments);
                        completion(argumentResults, constructed.Parameters, constructed);
                    };
                }
                else
                {
                    // Only the CollectionBuilder and ImplementsIEnumerable kinds can end up with a target type that
                    // would make us want to reanalyze the collection creation arguments.  This is because these are the
                    // types where the factory-method or constructor (respectively), could be generic and then have type
                    // parameters which are reinferred to be more specific nullable types that will affect the analysis
                    // of the arguments passed into them.  The other kinds are:
                    //
                    // 1. Arrays/Spans.  These can't have CollectionCreation expressions at all.
                    // 2. Array interfaces (e.g. IList<T>).  These only have two supported constructors that are called
                    //    (List<T>() and List<T>(int)), and neither of these involve generics, so they do not need
                    //    reanalysis.
                    //
                    // So for all these uninteresting cases, all we do is visit the collection creation once and be
                    // done. This at least ensures we visit any *arguments* passed into the 'with', which themselves
                    // could affect nullability state.  For example `[with(a = "")]`.
                    Visit(node.CollectionCreation);
                    return null;
                }
            }

            void visitElement(BoundNode element, BoundCollectionExpression node, TypeWithAnnotations targetElementType, ArrayBuilder<Action<TypeWithAnnotations, TypeSymbol>> elementConversionCompletions)
            {
                switch (element)
                {
                    case BoundCollectionElementInitializer initializer:
                        // The initializer generally represents a call to an Add method.
                        // We do not analyze the full call or all the arguments.
                        // We only analyze the single argument that represents the collection-expression element.
                        SetUnknownResultNullability(initializer);
                        Debug.Assert(node.Placeholder is { });
                        SetUnknownResultNullability(node.Placeholder);

                        var argIndex = initializer.AddMethod.IsExtensionMethod ? 1 : 0;
                        Debug.Assert(initializer.ArgsToParamsOpt.IsDefault);
                        var addArgument = initializer.Arguments[argIndex];
                        VisitRvalue(addArgument);
                        var addArgumentResult = _visitResult;

                        elementConversionCompletions.Add((_, targetCollectionType) =>
                        {
                            // Reinfer the addMethod signature and convert the argument to parameter type
                            var addMethod = initializer.AddMethod;
                            MethodSymbol reinferredAddMethod;
                            if (!addMethod.IsExtensionMethod && !addMethod.IsExtensionBlockMember())
                            {
                                reinferredAddMethod = (MethodSymbol)AsMemberOfType(targetCollectionType, addMethod);
                            }
                            else
                            {
                                // https://github.com/dotnet/roslyn/issues/68786: reinfer type arguments of a generic extension Add method
                                reinferredAddMethod = addMethod;
                            }

                            var reinferredParameter = reinferredAddMethod.Parameters[argIndex];
                            VisitConversion(
                                conversionOpt: null,
                                addArgument,
                                Conversion.Identity, // as only a nullable reinference is being done we expect an identity conversion
                                reinferredParameter.TypeWithAnnotations,
                                addArgumentResult.RValueType,
                                checkConversion: true,
                                fromExplicitCast: false,
                                useLegacyWarnings: false,
                                parameterOpt: reinferredParameter,
                                assignmentKind: AssignmentKind.Argument,
                                reportTopLevelWarnings: true,
                                reportRemainingWarnings: true,
                                trackMembers: false);
                        });

                        break;
                    case BoundCollectionExpressionSpreadElement spread:
                        Visit(spread);
                        if (targetElementType.HasType &&
                            spread.ElementPlaceholder is { } elementPlaceholder &&
                            spread.IteratorBody is { })
                        {
                            var itemResult = spread.EnumeratorInfoOpt == null ? default : _visitResult;
                            var iteratorBody = ((BoundExpressionStatement)spread.IteratorBody).Expression;

                            // Consider a collection expression like List<TElem> x = [y, ..z]
                            // Lowering is comparable to the following:
                            // List<TElem> __tmp = new(...);
                            // __tmp.Add(y);
                            // foreach (var z1 in z)
                            //     __tmp.Add(z1);
                            //
                            // In other words, the spread contains a BoundCollectionElementInitializer which needs to be further deconstructed and 'z1' converted to 'TElem'.
                            AddPlaceholderReplacement(elementPlaceholder, expression: elementPlaceholder, itemResult);
                            visitElement(iteratorBody, node, targetElementType, elementConversionCompletions);
                            RemovePlaceholderReplacement(elementPlaceholder);
                        }
                        break;
                    default:
                        var elementExpr = (BoundExpression)element;
                        if (!targetElementType.HasType)
                        {
                            VisitRvalueWithState(elementExpr);
                        }
                        else
                        {
                            var completion = VisitOptionalImplicitConversion(elementExpr, targetElementType,
                                useLegacyWarnings: false, trackMembers: false, AssignmentKind.Assignment, delayCompletionForTargetType: true).completion;

                            Debug.Assert(completion is not null);
                            elementConversionCompletions.Add((elementType, _) => completion(elementType));
                        }
                        break;
                }
            }

            TypeWithState convertCollection(
                BoundCollectionExpression node,
                TypeWithAnnotations targetCollectionType,
                Action<TypeSymbol>? collectionCreationCompletion,
                ArrayBuilder<Action<TypeWithAnnotations, TypeSymbol>> completions)
            {
                var strippedTargetCollectionType = targetCollectionType.Type.StrippedType();
                Debug.Assert(TypeSymbol.Equals(strippedTargetCollectionType, node.Type, TypeCompareKind.AllIgnoreOptions));

                // https://github.com/dotnet/roslyn/issues/68786: Use inferInitialObjectState() to set the initial
                // state of the instance: see the call to InheritNullableStateOfTrackableStruct() in particular.

                // Process the element conversions now that we have the target-type
                var (collectionKind, targetElementType) = getCollectionDetails(node, strippedTargetCollectionType);

                // Now that we know the final type of the collection, use that to properly reanalyze the arguments
                // passed to a with(...) element if present.
                collectionCreationCompletion?.Invoke(strippedTargetCollectionType);

                foreach (var completion in completions)
                {
                    completion(targetElementType, strippedTargetCollectionType);
                }
                completions.Free();

                // Record the final state
                NullableFlowState resultState = getResultState(node, collectionKind);
                var resultTypeWithState = TypeWithState.Create(strippedTargetCollectionType, resultState);

                var collectionCreation = node.CollectionCreation;
                while (collectionCreation is BoundConversion conversion)
                {
                    SetAnalyzedNullability(collectionCreation, resultTypeWithState);
                    collectionCreation = conversion.Operand;
                }

                SetAnalyzedNullability(collectionCreation, resultTypeWithState);

                SetAnalyzedNullability(node, resultTypeWithState);
                return resultTypeWithState;
            }

            static NullableFlowState getResultState(BoundCollectionExpression node, CollectionExpressionTypeKind collectionKind)
            {
                if (collectionKind is CollectionExpressionTypeKind.CollectionBuilder)
                {
                    var createMethod = node.CollectionBuilderMethod;
                    if (createMethod is not null)
                    {
                        var annotations = createMethod.GetFlowAnalysisAnnotations();
                        return ApplyUnconditionalAnnotations(createMethod.ReturnTypeWithAnnotations, annotations).ToTypeWithState().State;
                    }
                }

                return NullableFlowState.NotNull;
            }

            (CollectionExpressionTypeKind, TypeWithAnnotations) getCollectionDetails(BoundCollectionExpression node, TypeSymbol collectionType)
            {
                var collectionKind = ConversionsBase.GetCollectionExpressionTypeKind(this.compilation, collectionType, out var targetElementType);
                if (collectionKind is CollectionExpressionTypeKind.CollectionBuilder)
                {
                    var createMethod = node.CollectionBuilderMethod;
                    if (createMethod is not null)
                    {
                        var foundIterationType = _binder.TryGetCollectionIterationType((ExpressionSyntax)node.Syntax, collectionType, out targetElementType);
                        Debug.Assert(foundIterationType);
                    }
                }
                else if (collectionKind is CollectionExpressionTypeKind.ImplementsIEnumerable)
                {
                    Debug.Assert(!targetElementType.HasType);
                    _binder.TryGetCollectionIterationType(node.Syntax, collectionType, out targetElementType);
                }

                return (collectionKind, targetElementType);
            }
        }

        public override BoundNode? VisitCollectionExpressionSpreadElement(BoundCollectionExpressionSpreadElement node)
        {
            VisitRvalue(node.Expression);

            if (node.Conversion is BoundConversion { Conversion: var conversion })
            {
                Debug.Assert(node.ExpressionPlaceholder is { });
                Debug.Assert(node.EnumeratorInfoOpt is { });
                AddPlaceholderReplacement(node.ExpressionPlaceholder, node.Expression, _visitResult);
                VisitForEachExpression(
                    node,
                    node.Conversion,
                    conversion,
                    node.ExpressionPlaceholder,
                    node.EnumeratorInfoOpt);
                RemovePlaceholderReplacement(node.ExpressionPlaceholder);
            }
            else
            {
                Debug.Assert(node.HasErrors);
                Debug.Assert(node.Conversion is null);
                Debug.Assert(node.EnumeratorInfoOpt is null);
            }

            return null;
        }

        private void VisitObjectCreationExpressionBase(BoundObjectCreationExpressionBase node)
        {
            Debug.Assert(!IsConditionalState);
            bool isTargetTyped = node.WasTargetTyped;
            MethodSymbol? constructor = getConstructor(node, node.Type);
            var arguments = node.Arguments;

            (_, ImmutableArray<VisitResult> argumentResults, _, ArgumentsCompletionDelegate<MethodSymbol>? argumentsCompletion) =
                VisitArguments(
                           node, arguments, node.ArgumentRefKindsOpt, constructor?.Parameters ?? default,
                           node.ArgsToParamsOpt, node.DefaultArguments, node.Expanded, invokedAsExtensionMethod: false,
                           constructor, delayCompletionForTargetMember: isTargetTyped);
            Debug.Assert(isTargetTyped == argumentsCompletion is not null);

            var type = node.Type;
            var initializerOpt = node.InitializerExpressionOpt;
            (int slot, NullableFlowState resultState, Func<TypeSymbol, MethodSymbol?, int>? initialStateInferenceCompletion) =
                inferInitialObjectState(node, type, constructor, arguments, argumentResults, isTargetTyped, hasObjectInitializer: initializerOpt is { });

            Action<int, TypeSymbol>? initializerCompletion = null;
            if (initializerOpt != null)
            {
                initializerCompletion = VisitObjectCreationInitializer(slot, type, initializerOpt, delayCompletionForType: isTargetTyped);
            }

            TypeWithState result = setAnalyzedNullability(node, type, argumentResults, argumentsCompletion, initialStateInferenceCompletion, initializerCompletion, resultState, isTargetTyped);
            SetResultType(node, result, updateAnalyzedNullability: false);
            return;

            TypeWithState setAnalyzedNullability(
                BoundObjectCreationExpressionBase node,
                TypeSymbol? type,
                ImmutableArray<VisitResult> argumentResults,
                ArgumentsCompletionDelegate<MethodSymbol>? argumentsCompletion,
                Func<TypeSymbol, MethodSymbol?, int>? initialStateInferenceCompletion,
                Action<int, TypeSymbol>? initializerCompletion,
                NullableFlowState resultState,
                bool isTargetTyped)
            {
                var result = TypeWithState.Create(type, resultState);

                if (isTargetTyped)
                {
                    Debug.Assert(argumentsCompletion is not null);
                    Debug.Assert(initialStateInferenceCompletion is not null);
                    setAnalyzedNullabilityAsContinuation(node, argumentResults, argumentsCompletion, initialStateInferenceCompletion, initializerCompletion, resultState);
                }
                else
                {
                    Debug.Assert(argumentsCompletion is null);
                    Debug.Assert(initialStateInferenceCompletion is null);
                    Debug.Assert(initializerCompletion is null);

                    SetAnalyzedNullability(node, result);
                }

                return result;
            }

            void setAnalyzedNullabilityAsContinuation(
                BoundObjectCreationExpressionBase node,
                ImmutableArray<VisitResult> argumentResults,
                ArgumentsCompletionDelegate<MethodSymbol> argumentsCompletion,
                Func<TypeSymbol, MethodSymbol?, int> initialStateInferenceCompletion,
                Action<int, TypeSymbol>? initializerCompletion,
                NullableFlowState resultState)
            {
                Debug.Assert(resultState == NullableFlowState.NotNull);

                TargetTypedAnalysisCompletion[node] =
                    (TypeWithAnnotations resultTypeWithAnnotations) =>
                    {
                        Debug.Assert(TypeSymbol.Equals(resultTypeWithAnnotations.Type, node.Type, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

                        var type = resultTypeWithAnnotations.Type;
                        MethodSymbol? constructor = getConstructor(node, type);

                        argumentsCompletion(argumentResults, constructor?.Parameters ?? default, constructor);
                        int slot = initialStateInferenceCompletion(type, constructor);
                        initializerCompletion?.Invoke(slot, type);

                        return setAnalyzedNullability(
                                   node, type, argumentResults,
                                   argumentsCompletion: null, initialStateInferenceCompletion: null, initializerCompletion: null, resultState, isTargetTyped: false);
                    };
            }

            static MethodSymbol? getConstructor(BoundObjectCreationExpressionBase node, TypeSymbol type)
            {
                var constructor = node.Constructor;

                if (constructor is not null && !type.IsInterfaceType())
                {
                    constructor = (MethodSymbol)AsMemberOfType(type, constructor);
                }

                return constructor;
            }

            (int slot, NullableFlowState resultState, Func<TypeSymbol, MethodSymbol?, int>? completion) inferInitialObjectState(
                BoundExpression node, TypeSymbol type, MethodSymbol? constructor,
                ImmutableArray<BoundExpression> arguments, ImmutableArray<VisitResult> argumentResults,
                bool isTargetTyped,
                bool hasObjectInitializer)
            {
                if (isTargetTyped)
                {
                    return (-1, NullableFlowState.NotNull, inferInitialObjectStateAsContinuation(node, arguments, argumentResults, hasObjectInitializer));
                }

                Debug.Assert(node.Kind is BoundKind.ObjectCreationExpression or BoundKind.DynamicObjectCreationExpression or BoundKind.NewT or BoundKind.NoPiaObjectCreationExpression);

                int slot = -1;
                var resultState = NullableFlowState.NotNull;
                if (type is object &&
                    (hasObjectInitializer || type.IsStructType()))
                {
                    slot = GetOrCreatePlaceholderSlot(node);
                    if (slot > 0)
                    {
                        bool isDefaultValueTypeConstructor = constructor?.IsDefaultValueTypeConstructor() == true;

                        if (EmptyStructTypeCache.IsTrackableStructType(type))
                        {
                            var containingType = constructor?.ContainingType;
                            if (containingType?.IsTupleType == true && !isDefaultValueTypeConstructor)
                            {
                                // new System.ValueTuple<T1, ..., TN>(e1, ..., eN)
                                var argumentTypes = argumentResults.SelectAsArray(ar => ar.RValueType);
                                TrackNullableStateOfTupleElements(slot, containingType, arguments, argumentTypes, ((BoundObjectCreationExpression)node).ArgsToParamsOpt, useRestField: true);
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
                            else if (constructor?.ParameterCount == 1)
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

                        SetState(ref this.State, slot, resultState);
                    }
                }

                return (slot, resultState, null);
            }

            Func<TypeSymbol, MethodSymbol?, int> inferInitialObjectStateAsContinuation(
                BoundExpression node,
                ImmutableArray<BoundExpression> arguments,
                ImmutableArray<VisitResult> argumentResults,
                bool hasObjectInitializer)
            {
                return (TypeSymbol type, MethodSymbol? constructor) =>
                {
                    var (slot, resultState, completion) = inferInitialObjectState(node, type, constructor, arguments, argumentResults, isTargetTyped: false, hasObjectInitializer);
                    Debug.Assert(completion is null);
                    Debug.Assert(resultState == NullableFlowState.NotNull);
                    return slot;
                };
            }
        }

        /// <summary>
        /// If <paramref name="delayCompletionForType"/>, <paramref name="containingSlot"/> is known only within returned delegate.
        /// </summary>
        /// <returns>A delegate to complete the initializer analysis.</returns>
        private Action<int, TypeSymbol>? VisitObjectCreationInitializer(int containingSlot, TypeSymbol containingType, BoundObjectInitializerExpressionBase node, bool delayCompletionForType)
        {
            Debug.Assert(!delayCompletionForType || containingSlot == -1);
            Action<int, TypeSymbol>? completion = null;

            TakeIncrementalSnapshot(node);
            switch (node)
            {
                case BoundObjectInitializerExpression objectInitializer:
                    foreach (var initializer in objectInitializer.Initializers)
                    {
                        switch (initializer.Kind)
                        {
                            case BoundKind.AssignmentOperator:
                                completion += VisitObjectElementInitializer(containingSlot, containingType, (BoundAssignmentOperator)initializer, delayCompletionForType);
                                break;
                            default:
                                VisitRvalue(initializer);
                                break;
                        }
                    }
                    SetNotNullResult(objectInitializer.Placeholder);
                    break;
                case BoundCollectionInitializerExpression collectionInitializer:
                    foreach (var initializer in collectionInitializer.Initializers)
                    {
                        switch (initializer.Kind)
                        {
                            case BoundKind.CollectionElementInitializer:
                                completion += VisitCollectionElementInitializer((BoundCollectionElementInitializer)initializer, containingType, delayCompletionForType);
                                break;
                            default:
                                VisitRvalue(initializer);
                                break;
                        }
                    }
                    SetNotNullResult(collectionInitializer.Placeholder);
                    break;
                default:
                    ExceptionUtilities.UnexpectedValue(node.Kind);
                    break;
            }

            return completion;
        }

        /// <summary>
        /// If <paramref name="delayCompletionForType"/>, <paramref name="containingSlot"/> is known only within returned delegate.
        /// </summary>
        /// <returns>A delegate to complete the element initializer analysis.</returns>
        private Action<int, TypeSymbol>? VisitObjectElementInitializer(int containingSlot, TypeSymbol containingType, BoundAssignmentOperator node, bool delayCompletionForType)
        {
            Debug.Assert(!delayCompletionForType || containingSlot == -1);

            TakeIncrementalSnapshot(node);
            var left = node.Left;
            switch (left.Kind)
            {
                case BoundKind.ObjectInitializerMember:
                    {
                        TakeIncrementalSnapshot(left);
                        return visitMemberInitializer(containingSlot, containingType, node, delayCompletionForType);
                    }

                default:
                    VisitRvalue(node);
                    return null;
            }

            Action<int, TypeSymbol>? visitMemberInitializer(int containingSlot, TypeSymbol containingType, BoundAssignmentOperator node, bool delayCompletionForType)
            {
                var objectInitializer = (BoundObjectInitializerMember)node.Left;
                Symbol? symbol = getTargetMember(containingType, objectInitializer);

                ImmutableArray<VisitResult> argumentResults = default;
                ArgumentsCompletionDelegate<Symbol>? argumentsCompletion = null;
                if (!objectInitializer.Arguments.IsDefaultOrEmpty)
                {
                    // It is an error for an interpolated string to use the receiver of an object initializer indexer here, so we just use
                    // a default visit result
                    (_, argumentResults, _, argumentsCompletion) =
                        VisitArguments(
                            objectInitializer, objectInitializer.Arguments, objectInitializer.ArgumentRefKindsOpt,
                            ((PropertySymbol?)symbol)?.Parameters ?? default, objectInitializer.ArgsToParamsOpt,
                            objectInitializer.DefaultArguments, objectInitializer.Expanded,
                            invokedAsExtensionMethod: false, member: (Symbol?)null, delayCompletionForTargetMember: delayCompletionForType);
                }

                Action<int, Symbol>? initializationCompletion = null;

                if (symbol is object)
                {
                    if (node.Right is BoundObjectInitializerExpressionBase initializer)
                    {
                        initializationCompletion = visitNestedInitializer(containingSlot, containingType, symbol, initializer, delayCompletionForType);
                    }
                    else
                    {
                        TakeIncrementalSnapshot(node.Right);
                        initializationCompletion = visitMemberAssignment(node, containingSlot, symbol, delayCompletionForType);
                    }
                    // https://github.com/dotnet/roslyn/issues/35040: Should likely be setting _resultType in VisitObjectCreationInitializer
                    // and using that value instead of reconstructing here
                }

                return setAnalyzedNullability(node, argumentResults, argumentsCompletion, initializationCompletion, delayCompletionForType);
            }

            Action<int, TypeSymbol>? setAnalyzedNullability(
                BoundAssignmentOperator node,
                ImmutableArray<VisitResult> argumentResults,
                ArgumentsCompletionDelegate<Symbol>? argumentsCompletion,
                Action<int, Symbol>? initializationCompletion,
                bool delayCompletionForType)
            {
                if (delayCompletionForType)
                {
                    return setAnalyzedNullabilityAsContinuation(node, argumentResults, argumentsCompletion, initializationCompletion);
                }

                Debug.Assert(argumentsCompletion is null);
                Debug.Assert(initializationCompletion is null);
                var objectInitializer = (BoundObjectInitializerMember)node.Left;

                var result = new VisitResult(objectInitializer.Type, NullableAnnotation.NotAnnotated, NullableFlowState.NotNull);
                SetAnalyzedNullability(objectInitializer, result);
                SetAnalyzedNullability(node, result);
                return null;
            }

            Action<int, TypeSymbol>? setAnalyzedNullabilityAsContinuation(
                BoundAssignmentOperator node,
                ImmutableArray<VisitResult> argumentResults,
                ArgumentsCompletionDelegate<Symbol>? argumentsCompletion,
                Action<int, Symbol>? initializationCompletion)
            {
                return (int containingSlot, TypeSymbol containingType) =>
                {
                    Symbol? symbol = getTargetMember(containingType, (BoundObjectInitializerMember)node.Left);

                    Debug.Assert(initializationCompletion is null || symbol is not null);

                    argumentsCompletion?.Invoke(argumentResults, ((PropertySymbol?)symbol)?.Parameters ?? default, null);
                    initializationCompletion?.Invoke(containingSlot, symbol!);

                    var result = setAnalyzedNullability(node, argumentResults, argumentsCompletion: null, initializationCompletion: null, delayCompletionForType: false);
                    Debug.Assert(result is null);
                };
            }

            Symbol? getTargetMember(TypeSymbol containingType, BoundObjectInitializerMember objectInitializer)
            {
                var symbol = objectInitializer.MemberSymbol;
                if (symbol == null)
                    return null;

                if (!symbol.IsExtensionBlockMember())
                {
                    Debug.Assert(TypeSymbol.Equals(objectInitializer.Type, GetTypeOrReturnType(symbol), TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));
                    return AsMemberOfType(containingType, symbol);
                }

                var extension = symbol.OriginalDefinition.ContainingType;
                if (extension.Arity == 0)
                {
                    return symbol;
                }

                if (symbol is not PropertySymbol { IsStatic: false } property
                    || extension.ExtensionParameter is not { } extensionParameter)
                {
                    Debug.Assert(objectInitializer.HasErrors);
                    return symbol;
                }

                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;

                // This may be incorrect for extension indexers.
                // At that point we would maybe just want to gather arguments and visit the memberInitializer as an invocation of the indexer setter.
                var inferenceResult = MethodTypeInferrer.Infer(
                    _binder,
                    _conversions,
                    extension.TypeParameters,
                    extension,
                    formalParameterTypes: [extensionParameter.TypeWithAnnotations],
                    formalParameterRefKinds: [extensionParameter.RefKind],
                    [new BoundExpressionWithNullability(objectInitializer.Syntax, objectInitializer, nullableAnnotation: NullableAnnotation.NotAnnotated, containingType)],
                    ref discardedUseSiteInfo,
                    new MethodInferenceExtensions(this),
                    ordinals: null);

                if (inferenceResult.Success)
                {
                    extension = extension.Construct(inferenceResult.InferredTypeArguments);
                    property = property.OriginalDefinition.AsMember(extension);
                    SetUpdatedSymbol(objectInitializer, symbol, property);
                }

                return property;
            }

            int getOrCreateSlot(int containingSlot, Symbol symbol)
            {
                return (containingSlot < 0 || !IsSlotMember(containingSlot, symbol)) ? -1 : GetOrCreateSlot(symbol, containingSlot);
            }

            Action<int, Symbol>? visitNestedInitializer(int containingSlot, TypeSymbol containingType, Symbol symbol, BoundObjectInitializerExpressionBase initializer, bool delayCompletionForType)
            {
                int slot = getOrCreateSlot(containingSlot, symbol);
                Debug.Assert(!delayCompletionForType || slot == -1);

                Action<int, TypeSymbol>? nestedCompletion = VisitObjectCreationInitializer(slot, GetTypeOrReturnType(symbol), initializer, delayCompletionForType);

                return completeNestedInitializerAnalysis(symbol, initializer, slot, nestedCompletion, delayCompletionForType);
            }

            Action<int, Symbol>? completeNestedInitializerAnalysis(
                Symbol symbol, BoundObjectInitializerExpressionBase initializer, int slot, Action<int, TypeSymbol>? nestedCompletion,
                bool delayCompletionForType)
            {
                if (delayCompletionForType)
                {
                    return completeNestedInitializerAnalysisAsContinuation(initializer, nestedCompletion);
                }

                Debug.Assert(nestedCompletion is null);

                if (slot >= 0 && !initializer.Initializers.IsEmpty)
                {
                    if (!initializer.Type.IsValueType && GetState(ref State, slot).MayBeNull())
                    {
                        ReportDiagnostic(ErrorCode.WRN_NullReferenceInitializer, initializer.Syntax, symbol);
                    }
                }

                return null;
            }

            Action<int, Symbol>? completeNestedInitializerAnalysisAsContinuation(BoundObjectInitializerExpressionBase initializer, Action<int, TypeSymbol>? nestedCompletion)
            {
                return (int containingSlot, Symbol symbol) =>
                {
                    int slot = getOrCreateSlot(containingSlot, symbol);
                    completeNestedInitializerAnalysis(symbol, initializer, slot, nestedCompletion: null, delayCompletionForType: false);
                    nestedCompletion?.Invoke(slot, GetTypeOrReturnType(symbol));
                };
            }

            Action<int, Symbol>? visitMemberAssignment(BoundAssignmentOperator node, int containingSlot, Symbol symbol, bool delayCompletionForType, Func<TypeWithAnnotations, TypeWithState>? conversionCompletion = null)
            {
                Debug.Assert(!delayCompletionForType || conversionCompletion is null);

                if (!delayCompletionForType && conversionCompletion is null)
                {
                    TakeIncrementalSnapshot(node.Right);
                }

                Debug.Assert(GetTypeOrReturnTypeWithAnnotations(symbol).HasType);

                var type = ApplyLValueAnnotations(GetTypeOrReturnTypeWithAnnotations(symbol), GetObjectInitializerMemberLValueAnnotations(symbol));

                (TypeWithState resultType, conversionCompletion) =
                    conversionCompletion is not null ?
                        (conversionCompletion(type), null) :
                        VisitOptionalImplicitConversion(node.Right, type, useLegacyWarnings: false, trackMembers: true, AssignmentKind.Assignment, delayCompletionForType);
                Unsplit();

                if (delayCompletionForType)
                {
                    Debug.Assert(conversionCompletion is not null);
                    return visitMemberAssignmentAsContinuation(node, conversionCompletion);
                }

                Debug.Assert(conversionCompletion is null);

                int slot = getOrCreateSlot(containingSlot, symbol);
                TrackNullableStateForAssignment(node.Right, type, slot, resultType, MakeSlot(node.Right));

                return null;
            }

            Action<int, Symbol>? visitMemberAssignmentAsContinuation(BoundAssignmentOperator node, Func<TypeWithAnnotations, TypeWithState> conversionCompletion)
            {
                return (int containingSlot, Symbol symbol) =>
                {
                    var result = visitMemberAssignment(node, containingSlot, symbol, delayCompletionForType: false, conversionCompletion);
                    Debug.Assert(result is null);
                };
            }
        }

        [Obsolete("Use VisitCollectionElementInitializer(BoundCollectionElementInitializer node, TypeSymbol containingType, bool delayCompletionForType) instead.", true)]
#pragma warning disable IDE0051 // Remove unused private members
        private new void VisitCollectionElementInitializer(BoundCollectionElementInitializer node)
#pragma warning restore IDE0051 // Remove unused private members
        {
            throw ExceptionUtilities.Unreachable();
        }

        private Action<int, TypeSymbol>? VisitCollectionElementInitializer(BoundCollectionElementInitializer node, TypeSymbol containingType, bool delayCompletionForType)
        {
            ImmutableArray<VisitResult> argumentResults = default;
            MethodSymbol addMethod = addMethodAsMemberOfContainingType(node, containingType, ref argumentResults);

            // Note: we analyze even omitted calls
            (MethodSymbol? reinferredMethod,
             argumentResults,
             _,
             ArgumentsCompletionDelegate<MethodSymbol>? visitArgumentsCompletion) =
                VisitArguments(
                    node,
                    node.Arguments,
                    refKindsOpt: default,
                    addMethod.Parameters,
                    node.ArgsToParamsOpt,
                    node.DefaultArguments,
                    node.Expanded,
                    node.InvokedAsExtensionMethod,
                    addMethod,
                    delayCompletionForTargetMember: delayCompletionForType);

#if DEBUG
            if (node.InvokedAsExtensionMethod)
            {
                VisitResult receiverResult = argumentResults[0];
                Debug.Assert(TypeSymbol.Equals(containingType, receiverResult.RValueType.Type, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));
                Debug.Assert(TypeSymbol.Equals(containingType, receiverResult.LValueType.Type, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));
            }
#endif

            return setUpdatedSymbol(node, containingType, reinferredMethod, argumentResults, visitArgumentsCompletion, delayCompletionForType);

            Action<int, TypeSymbol>? setUpdatedSymbol(
                BoundCollectionElementInitializer node,
                TypeSymbol containingType,
                MethodSymbol? reinferredMethod,
                ImmutableArray<VisitResult> argumentResults,
                ArgumentsCompletionDelegate<MethodSymbol>? visitArgumentsCompletion,
                bool delayCompletionForType)
            {
                if (delayCompletionForType)
                {
                    Debug.Assert(visitArgumentsCompletion is not null);
                    return setUpdatedSymbolAsContinuation(node, argumentResults, visitArgumentsCompletion);
                }

                Debug.Assert(visitArgumentsCompletion is null);
                Debug.Assert(reinferredMethod is object);
                if (node.ImplicitReceiverOpt != null)
                {
                    //Debug.Assert(node.ImplicitReceiverOpt.Kind == BoundKind.ObjectOrCollectionValuePlaceholder); // Tracked by https://github.com/dotnet/roslyn/issues/78828 : the receiver may be converted now
                    SetAnalyzedNullability(node.ImplicitReceiverOpt, new VisitResult(node.ImplicitReceiverOpt.Type, NullableAnnotation.NotAnnotated, NullableFlowState.NotNull));
                }
                SetUnknownResultNullability(node);
                SetUpdatedSymbol(node, node.AddMethod, reinferredMethod);

                return null;
            }

            Action<int, TypeSymbol>? setUpdatedSymbolAsContinuation(
                BoundCollectionElementInitializer node,
                ImmutableArray<VisitResult> argumentResults,
                ArgumentsCompletionDelegate<MethodSymbol> visitArgumentsCompletion)
            {
                return (int containingSlot, TypeSymbol containingType) =>
                {
                    MethodSymbol addMethod = addMethodAsMemberOfContainingType(node, containingType, ref argumentResults);

                    setUpdatedSymbol(
                        node, containingType, visitArgumentsCompletion.Invoke(argumentResults, addMethod.Parameters, addMethod).member,
                        argumentResults, visitArgumentsCompletion: null, delayCompletionForType: false);
                };
            }

            static MethodSymbol addMethodAsMemberOfContainingType(BoundCollectionElementInitializer node, TypeSymbol containingType, ref ImmutableArray<VisitResult> argumentResults)
            {
                var method = node.AddMethod;

                if (node.InvokedAsExtensionMethod)
                {
                    if (!argumentResults.IsDefault)
                    {
                        VisitResult receiverResult = argumentResults[0];
                        Debug.Assert(TypeSymbol.Equals(containingType, receiverResult.RValueType.Type, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));
                        Debug.Assert(TypeSymbol.Equals(containingType, receiverResult.LValueType.Type, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

                        var builder = ArrayBuilder<VisitResult>.GetInstance(argumentResults.Length);
                        builder.Add(
                            new VisitResult(
                                TypeWithState.Create(containingType, receiverResult.RValueType.State),
                                receiverResult.LValueType.WithType(containingType),
                                receiverResult.StateForLambda));

                        builder.AddRange(argumentResults, 1, argumentResults.Length - 1);
                        argumentResults = builder.ToImmutableAndFree();
                    }
                }
                else if (!method.IsExtensionBlockMember())
                {
                    // Tracked by https://github.com/dotnet/roslyn/issues/78828: Do we need to do anything special for new extensions here?
                    method = (MethodSymbol)AsMemberOfType(containingType, method);
                }

                return method;
            }
        }

        private void SetNotNullResult(BoundExpression node)
        {
            SetResultType(node, TypeWithState.Create(node.Type, NullableFlowState.NotNull));
        }

        private void SetNotNullResultForLambda(BoundExpression node, LocalState stateForLambda)
        {
            var resultType = TypeWithState.Create(node.Type, NullableFlowState.NotNull);
            var lvalueType = resultType.ToTypeWithAnnotations(compilation);
            SetResult(node, new VisitResult(resultType, lvalueType, stateForLambda), updateAnalyzedNullability: true, isLvalue: null);
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

            if (type.SpecialType.CanOptimizeBehavior())
            {
                return true;
            }

            var members = ((NamedTypeSymbol)type).GetMembersUnordered();

            // EmptyStructTypeCache.IsEmptyStructType() returned true. If there are fields,
            // at least one of those fields must be cyclic, so treat the type as empty.
            if (members.Any(static m => m.Kind == SymbolKind.Field))
            {
                return true;
            }

            // If there are properties, the type is not empty.
            if (members.Any(static m => m.Kind == SymbolKind.Property))
            {
                return false;
            }

            return true;
        }

        private int GetOrCreatePlaceholderSlot(BoundExpression node)
        {
            Debug.Assert(node.Type is object);
            if (IsEmptyStructType(node.Type))
            {
                return -1;
            }

            return GetOrCreatePlaceholderSlot(node, TypeWithAnnotations.Create(node.Type, NullableAnnotation.NotAnnotated));
        }

        private int GetOrCreatePlaceholderSlot(object identifier, TypeWithAnnotations type)
        {
            _placeholderLocalsOpt ??= PooledDictionary<object, PlaceholderLocal>.GetInstance();
            if (!_placeholderLocalsOpt.TryGetValue(identifier, out var placeholder))
            {
                placeholder = new PlaceholderLocal(CurrentSymbol, identifier, type);
                _placeholderLocalsOpt.Add(identifier, placeholder);
            }

            Debug.Assert((object)placeholder != null);
            return GetOrCreateSlot(placeholder, forceSlotEvenIfEmpty: true);
        }

        public override BoundNode? VisitAnonymousObjectCreationExpression(BoundAnonymousObjectCreationExpression node)
        {
            Debug.Assert(!IsConditionalState);
            Debug.Assert(node.Type.IsAnonymousType);

            var anonymousType = (NamedTypeSymbol)node.Type;
            var arguments = node.Arguments;
            var argumentTypes = arguments.SelectAsArray((arg, self) =>
                self.VisitRvalueWithState(arg), this);
            var argumentsWithAnnotations = argumentTypes.SelectAsArray(arg =>
                arg.ToTypeWithAnnotations(compilation));

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
                    if (property.Type.SpecialType != SpecialType.System_Void)
                    {
                        // A void element results in an error type in the anonymous type but not in the property's container!
                        // To avoid failing an assertion later, we skip them.
                        var slot = GetOrCreateSlot(property, receiverSlot);
                        TrackNullableStateForAssignment(argument, property.TypeWithAnnotations, slot, argumentType, MakeSlot(argument));

                        var currentDeclaration = getDeclaration(node, property, ref currentDeclarationIndex);
                        if (currentDeclaration is object)
                        {
                            TakeIncrementalSnapshot(currentDeclaration);
                            SetAnalyzedNullability(currentDeclaration, new VisitResult(argumentType, property.TypeWithAnnotations));
                        }
                    }
                }
            }

            SetResultType(node, TypeWithState.Create(anonymousType, NullableFlowState.NotNull));
            return null;

            static BoundAnonymousPropertyDeclaration? getDeclaration(BoundAnonymousObjectCreationExpression node, PropertySymbol currentProperty, ref int currentDeclarationIndex)
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

        public override BoundNode? VisitArrayCreation(BoundArrayCreation node)
        {
            foreach (var expr in node.Bounds)
            {
                VisitRvalue(expr);
            }

            var initialization = node.InitializerOpt;
            if (initialization is null)
            {
                SetResultType(node, TypeWithState.Create(node.Type, NullableFlowState.NotNull));
                return null;
            }

            var arrayType = VisitArrayInitialization(node.Type, initialization, node.HasErrors);
            SetResultType(node, TypeWithState.Create(arrayType, NullableFlowState.NotNull));
            return null;
        }

        private TypeSymbol VisitArrayInitialization(TypeSymbol type, BoundArrayInitialization initialization, bool hasErrors)
        {
            TakeIncrementalSnapshot(initialization);
            var expressions = ArrayBuilder<BoundExpression>.GetInstance(initialization.Initializers.Length);
            GetArrayElements(initialization, expressions);
            int n = expressions.Count;

            // Consider recording in the BoundArrayCreation
            // whether the array was implicitly typed, rather than relying on syntax.
            var elementType = type switch
            {
                ArrayTypeSymbol arrayType => arrayType.ElementTypeWithAnnotations,
                PointerTypeSymbol pointerType => pointerType.PointedAtTypeWithAnnotations,
                NamedTypeSymbol spanType => getSpanElementType(spanType),
                _ => throw ExceptionUtilities.UnexpectedValue(type.TypeKind)
            };

            var resultType = type;
            if (!initialization.IsInferred)
            {
                foreach (var expr in expressions)
                {
                    _ = VisitOptionalImplicitConversion(expr, elementType, useLegacyWarnings: false, trackMembers: false, AssignmentKind.Assignment);
                    Unsplit();
                }
            }
            else
            {
                var expressionsNoConversions = ArrayBuilder<BoundExpression>.GetInstance(n);
                var conversions = ArrayBuilder<Conversion>.GetInstance(n);
                var expressionTypes = ArrayBuilder<TypeWithState>.GetInstance(n);
                var placeholderBuilder = ArrayBuilder<BoundExpression>.GetInstance(n);
                foreach (var expression in expressions)
                {
                    // collect expressions, conversions and result types
                    (BoundExpression expressionNoConversion, Conversion conversion) = RemoveConversion(expression, includeExplicitConversions: false);
                    expressionsNoConversions.Add(expressionNoConversion);
                    conversions.Add(conversion);
                    SnapshotWalkerThroughConversionGroup(expression, expressionNoConversion);
                    var expressionType = VisitRvalueWithState(expressionNoConversion);
                    expressionTypes.Add(expressionType);

                    if (!IsTargetTypedExpression(expressionNoConversion))
                    {
                        placeholderBuilder.Add(CreatePlaceholderIfNecessary(expressionNoConversion, expressionType.ToTypeWithAnnotations(compilation)));
                    }
                }

                var placeholders = placeholderBuilder.ToImmutableAndFree();

                TypeSymbol? bestType = null;
                if (!hasErrors)
                {
                    var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    bestType = BestTypeInferrer.InferBestType(placeholders, _conversions, ref discardedUseSiteInfo, out _);
                }

                TypeWithAnnotations inferredType = (bestType is null)
                    ? elementType.SetUnknownNullabilityForReferenceTypes()
                    : TypeWithAnnotations.Create(bestType);

                // Convert elements to best type to determine element top-level nullability and to report nested nullability warnings
                for (int i = 0; i < n; i++)
                {
                    var expressionNoConversion = expressionsNoConversions[i];
                    var expression = GetConversionIfApplicable(expressions[i], expressionNoConversion);
                    expressionTypes[i] = VisitConversion(expression, expressionNoConversion, conversions[i], inferredType, expressionTypes[i], checkConversion: true,
                        fromExplicitCast: false, useLegacyWarnings: false, AssignmentKind.Assignment, reportRemainingWarnings: true, reportTopLevelWarnings: false);
                    Unsplit();
                }

                // Set top-level nullability on inferred element type
                var elementState = BestTypeInferrer.GetNullableState(expressionTypes);
                inferredType = TypeWithState.Create(inferredType.Type, elementState).ToTypeWithAnnotations(compilation);

                for (int i = 0; i < n; i++)
                {
                    // Report top-level warnings
                    _ = VisitConversion(conversionOpt: null, conversionOperand: expressionsNoConversions[i], Conversion.Identity, targetTypeWithNullability: inferredType, operandType: expressionTypes[i],
                        checkConversion: true, fromExplicitCast: false, useLegacyWarnings: false, AssignmentKind.Assignment, reportRemainingWarnings: false);
                }

                expressionsNoConversions.Free();
                conversions.Free();
                expressionTypes.Free();

                resultType = type switch
                {
                    ArrayTypeSymbol arrayType => arrayType.WithElementType(inferredType),
                    PointerTypeSymbol pointerType => pointerType.WithPointedAtType(inferredType),
                    NamedTypeSymbol spanType => setSpanElementType(spanType, inferredType),
                    _ => throw ExceptionUtilities.UnexpectedValue(type.TypeKind)
                };
            }

            expressions.Free();
            return resultType;

            static TypeWithAnnotations getSpanElementType(NamedTypeSymbol namedType)
            {
                Debug.Assert(namedType.Name == "Span");
                Debug.Assert(namedType.OriginalDefinition.Arity == 1);
                return namedType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
            }

            static TypeSymbol setSpanElementType(NamedTypeSymbol namedType, TypeWithAnnotations elementType)
            {
                Debug.Assert(namedType.Name == "Span");
                Debug.Assert(namedType.OriginalDefinition.Arity == 1);
                return namedType.OriginalDefinition.Construct(ImmutableArray.Create(elementType));
            }
        }

        /// <summary>
        /// For target-typed expressions, we first visit the constituent parts to determine the effect on the State,
        /// but the final VisitResult isn't determined and the conversions on the constituent parts are not analyzed 
        /// until the target-type is known and the containing conversion is processed.
        /// This is done using <see cref="TargetTypedAnalysisCompletion"/>. All registered completions must be processed
        /// (ie. analyzed via some conversion) before the nullable analysis completes.
        /// </summary>
        internal static bool IsTargetTypedExpression(BoundExpression node)
        {
            return node is BoundConditionalOperator { WasTargetTyped: true } or
                           BoundConvertedSwitchExpression { WasTargetTyped: true } or
                           BoundObjectCreationExpressionBase { WasTargetTyped: true } or
                           BoundDelegateCreationExpression { WasTargetTyped: true } or
                           BoundCollectionExpression { WasTargetTyped: true };
        }

        /// <summary>
        /// Applies analysis similar to <see cref="VisitArrayCreation"/>.
        /// The expressions returned from a lambda are not converted though, so we'll have to classify fresh conversions.
        /// Note: even if some conversions fail, we'll proceed to infer top-level nullability. That is reasonable in common cases.
        /// </summary>
        internal static TypeWithAnnotations BestTypeForLambdaReturns(
            ArrayBuilder<(BoundExpression expr, TypeWithAnnotations resultType, bool isChecked)> returns,
            Binder binder,
            BoundNode node,
            Conversions conversions,
            out bool inferredFromFunctionType)
        {
            var walker = new NullableWalker(binder.Compilation,
                                            symbol: null,
                                            useConstructorExitWarnings: false,
                                            getterNullResilienceData: null,
                                            useDelegateInvokeParameterTypes: false,
                                            useDelegateInvokeReturnType: false,
                                            delegateInvokeMethodOpt: null,
                                            node,
                                            binder,
                                            conversions: conversions,
                                            variables: null,
                                            baseOrThisInitializer: null,
                                            returnTypesOpt: null,
                                            analyzedNullabilityMapOpt: null,
                                            snapshotBuilderOpt: null);

            int n = returns.Count;
            var resultTypes = ArrayBuilder<TypeWithAnnotations>.GetInstance(n);
            var placeholdersBuilder = ArrayBuilder<BoundExpression>.GetInstance(n);
            for (int i = 0; i < n; i++)
            {
                var (returnExpr, resultType, _) = returns[i];
                resultTypes.Add(resultType);
                placeholdersBuilder.Add(CreatePlaceholderIfNecessary(returnExpr, resultType));
            }

            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            var placeholders = placeholdersBuilder.ToImmutableAndFree();
            TypeSymbol? bestType = BestTypeInferrer.InferBestType(placeholders, walker._conversions, ref discardedUseSiteInfo, out inferredFromFunctionType);

            TypeWithAnnotations inferredType;
            if (bestType is { })
            {
                // Note: so long as we have a best type, we can proceed.
                var bestTypeWithObliviousAnnotation = TypeWithAnnotations.Create(bestType);
                Conversions conversionsWithoutNullability = walker._conversions.WithNullability(false);
                for (int i = 0; i < n; i++)
                {
                    BoundExpression placeholder = placeholders[i];
                    Conversion conversion = conversionsWithoutNullability.ClassifyConversionFromExpression(placeholder, bestType, isChecked: returns[i].isChecked, ref discardedUseSiteInfo);
                    resultTypes[i] = walker.VisitConversion(conversionOpt: null, placeholder, conversion, bestTypeWithObliviousAnnotation, resultTypes[i].ToTypeWithState(),
                        checkConversion: false, fromExplicitCast: false, useLegacyWarnings: false, AssignmentKind.Return,
                        reportRemainingWarnings: false, reportTopLevelWarnings: false).ToTypeWithAnnotations(binder.Compilation);
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

        public override BoundNode? VisitArrayAccess(BoundArrayAccess node)
        {
            Debug.Assert(!IsConditionalState);

            Visit(node.Expression);

            Debug.Assert(!IsConditionalState);
            Debug.Assert(!node.Expression.Type!.IsValueType);
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

        public override BoundNode? VisitInlineArrayAccess(BoundInlineArrayAccess node)
        {
            Debug.Assert(!IsConditionalState);

            var expressionType = VisitRvalueWithState(node.Expression).Type;

            Debug.Assert(!IsConditionalState);
            Debug.Assert(expressionType is not null);
            Debug.Assert(expressionType.IsValueType);

            VisitRvalue(node.Argument);

            TypeWithAnnotations type = expressionType.TryGetInlineArrayElementField()!.TypeWithAnnotations;

            if (node.GetItemOrSliceHelper is WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int or WellKnownMember.System_Span_T__Slice_Int_Int)
            {
                type = TypeWithAnnotations.Create(((NamedTypeSymbol)node.Type).OriginalDefinition.Construct(ImmutableArray.Create(type)));
            }

            SetResult(node, type.ToTypeWithState(), type);
            return null;
        }

        private TypeWithState InferResultNullability(BinaryOperatorKind operatorKind, MethodSymbol? methodOpt, TypeSymbol resultType, TypeWithState leftType, TypeWithState rightType)
        {
            NullableFlowState resultState = NullableFlowState.NotNull;
            if (operatorKind.IsUserDefined())
            {
                if (methodOpt?.ParameterCount == 2)
                {
                    if (operatorKind.IsLifted() && !operatorKind.IsComparison())
                    {
                        return GetLiftedReturnType(methodOpt.ReturnTypeWithAnnotations, leftType.State.Join(rightType.State));
                    }

                    var resultTypeWithState = GetReturnTypeWithState(methodOpt);
                    if ((leftType.IsNotNull && methodOpt.ReturnNotNullIfParameterNotNull.Contains(methodOpt.Parameters[0].Name)) ||
                        (rightType.IsNotNull && methodOpt.ReturnNotNullIfParameterNotNull.Contains(methodOpt.Parameters[1].Name)))
                    {
                        resultTypeWithState = resultTypeWithState.WithNotNullState();
                    }

                    return resultTypeWithState;
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

            // Only the leftmost operator of a left-associative binary operator chain can learn from a conditional access on the left
            // For simplicity, we just special case it here.
            // For example, `a?.b(out x) == true` has a conditional access on the left of the operator,
            // but `expr == a?.b(out x) == true` has a conditional access on the right of the operator
            if (VisitPossibleConditionalAccess(leftOperand, out var conditionalStateWhenNotNull)
                && CanPropagateStateWhenNotNull(leftConversion)
                && binary.OperatorKind.Operator() is BinaryOperatorKind.Equal or BinaryOperatorKind.NotEqual)
            {
                Debug.Assert(!IsConditionalState);
                var leftType = ResultType;
                var (rightOperand, rightConversion) = RemoveConversion(binary.Right, includeExplicitConversions: false);

                // Consider the following two scenarios:
                // `a?.b(x = new object()) == c.d(x = null)` // `x` is maybe-null after expression
                // `a?.b(x = null) == c.d(x = new object())` // `x` is not-null after expression

                // In this scenario, we visit the RHS twice:
                // (1) Once using the single "stateWhenNotNull" after the LHS, in order to update the "stateWhenNotNull" with the effects of the RHS
                // (2) Once using the "worst case" state after the LHS for diagnostics and public API

                // After the two visits of the RHS, we may set a conditional state using the state after (1) as the StateWhenTrue and the state after (2) as the StateWhenFalse.
                // Depending on whether `==` or `!=` was used, and depending on the value of the RHS, we may then swap the StateWhenTrue with the StateWhenFalse.

                var oldDisableDiagnostics = _disableDiagnostics;
                _disableDiagnostics = true;

                var stateAfterLeft = this.State;
                SetState(getUnconditionalStateWhenNotNull(rightOperand, conditionalStateWhenNotNull));
                VisitRvalue(rightOperand);
                var stateWhenNotNull = this.State;

                _disableDiagnostics = oldDisableDiagnostics;

                // Now visit the right side for public API and diagnostics using the worst-case state from the LHS.
                // Note that we do this visit last to try and make sure that the "visit for public API" overwrites walker state recorded during previous visits where possible.
                SetState(stateAfterLeft);
                var rightType = VisitRvalueWithState(rightOperand);
                ReinferBinaryOperatorAndSetResult(leftOperand, leftConversion, leftType, rightOperand, rightConversion, rightType, binary);
                if (isKnownNullOrNotNull(rightOperand, rightType))
                {
                    var isNullConstant = rightOperand.ConstantValueOpt?.IsNull == true;
                    SetConditionalState(isNullConstant == isEquals(binary)
                        ? (State, stateWhenNotNull)
                        : (stateWhenNotNull, State));
                }

                if (stack.Count == 0)
                {
                    return;
                }

                leftOperand = binary;
                leftConversion = Conversion.Identity;
                binary = stack.Pop();
            }

            while (true)
            {
                if (!learnFromConditionalAccessOrBoolConstant())
                {
                    Unsplit(); // VisitRvalue does this
                    UseRvalueOnly(leftOperand); // drop lvalue part

                    AfterLeftChildHasBeenVisited(leftOperand, leftConversion, binary);
                }

                if (stack.Count == 0)
                {
                    break;
                }

                leftOperand = binary;
                leftConversion = Conversion.Identity;
                binary = stack.Pop();
            }

            static bool isEquals(BoundBinaryOperator binary)
                => binary.OperatorKind.Operator() == BinaryOperatorKind.Equal;

            static bool isKnownNullOrNotNull(BoundExpression expr, TypeWithState resultType)
            {
                return resultType.State.IsNotNull()
                    || expr.ConstantValueOpt is object;
            }

            LocalState getUnconditionalStateWhenNotNull(BoundExpression otherOperand, PossiblyConditionalState conditionalStateWhenNotNull)
            {
                LocalState stateWhenNotNull;
                if (!conditionalStateWhenNotNull.IsConditionalState)
                {
                    stateWhenNotNull = conditionalStateWhenNotNull.State;
                }
                else if (isEquals(binary) && otherOperand.ConstantValueOpt is { IsBoolean: true, BooleanValue: var boolValue })
                {
                    // can preserve conditional state from `.TryGetValue` in `dict?.TryGetValue(key, out value) == true`,
                    // but not in `dict?.TryGetValue(key, out value) != false`
                    stateWhenNotNull = boolValue ? conditionalStateWhenNotNull.StateWhenTrue : conditionalStateWhenNotNull.StateWhenFalse;
                }
                else
                {
                    stateWhenNotNull = conditionalStateWhenNotNull.StateWhenTrue;
                    Join(ref stateWhenNotNull, ref conditionalStateWhenNotNull.StateWhenFalse);
                }
                return stateWhenNotNull;
            }

            // Returns true if `binary.Right` was visited by the call.
            bool learnFromConditionalAccessOrBoolConstant()
            {
                if (binary.OperatorKind.Operator() is not (BinaryOperatorKind.Equal or BinaryOperatorKind.NotEqual))
                {
                    return false;
                }

                var leftResult = ResultType;
                var (rightOperand, rightConversion) = RemoveConversion(binary.Right, includeExplicitConversions: false);
                // `true == a?.b(out x)`
                if (isKnownNullOrNotNull(leftOperand, leftResult)
                    && CanPropagateStateWhenNotNull(rightConversion)
                    && TryVisitConditionalAccess(rightOperand, out var conditionalStateWhenNotNull))
                {
                    ReinferBinaryOperatorAndSetResult(leftOperand, leftConversion, leftResult, rightOperand, rightConversion, rightType: ResultType, binary);

                    var stateWhenNotNull = getUnconditionalStateWhenNotNull(leftOperand, conditionalStateWhenNotNull);
                    var isNullConstant = leftOperand.ConstantValueOpt?.IsNull == true;
                    SetConditionalState(isNullConstant == isEquals(binary)
                        ? (State, stateWhenNotNull)
                        : (stateWhenNotNull, State));

                    return true;
                }

                // can only learn from a bool constant operand here if it's using the built in `bool operator ==(bool left, bool right)`
                if (binary.OperatorKind.IsUserDefined())
                {
                    return false;
                }

                // `(x != null) == true`
                if (IsConditionalState && binary.Right.ConstantValueOpt is { IsBoolean: true } rightConstant)
                {
                    var (stateWhenTrue, stateWhenFalse) = (StateWhenTrue.Clone(), StateWhenFalse.Clone());
                    Unsplit();
                    Visit(binary.Right);
                    UseRvalueOnly(binary.Right); // record result for the right
                    SetConditionalState(isEquals(binary) == rightConstant.BooleanValue
                        ? (stateWhenTrue, stateWhenFalse)
                        : (stateWhenFalse, stateWhenTrue));
                }
                // `true == (x != null)`
                else if (binary.Left.ConstantValueOpt is { IsBoolean: true } leftConstant)
                {
                    Unsplit();
                    Visit(binary.Right);
                    UseRvalueOnly(binary.Right);
                    if (IsConditionalState && isEquals(binary) != leftConstant.BooleanValue)
                    {
                        SetConditionalState(StateWhenFalse, StateWhenTrue);
                    }
                }
                else
                {
                    return false;
                }

                // record result for the binary
                Debug.Assert(binary.Type.SpecialType == SpecialType.System_Boolean);
                SetResult(binary, TypeWithState.ForType(binary.Type), TypeWithAnnotations.Create(binary.Type));
                return true;
            }
        }

        private void ReinferBinaryOperatorAndSetResult(
            BoundExpression leftOperand,
            Conversion leftConversion,
            TypeWithState leftType,
            BoundExpression rightOperand,
            Conversion rightConversion,
            TypeWithState rightType,
            BoundBinaryOperator binary)
        {
            var inferredResult = ReinferAndVisitBinaryOperator(binary, binary.OperatorKind, binary.BinaryOperatorMethod, binary.Type, binary.Left, leftOperand, leftConversion, leftType, binary.Right, rightOperand, rightConversion, rightType);
            SetResult(binary, inferredResult, inferredResult.ToTypeWithAnnotations(compilation));
        }

        private TypeWithState ReinferAndVisitBinaryOperator(
            BoundExpression binary,
            BinaryOperatorKind operatorKind,
            MethodSymbol? method,
            TypeSymbol returnType,
            BoundExpression left,
            BoundExpression leftOperand,
            Conversion leftConversion,
            TypeWithState leftType,
            BoundExpression right,
            BoundExpression rightOperand,
            Conversion rightConversion,
            TypeWithState rightType)
        {
            Debug.Assert(!IsConditionalState);
            // At this point, State.Reachable may be false for
            // invalid code such as `s + throw new Exception()`.

            if (operatorKind.IsUserDefined() &&
                method?.ParameterCount == 2)
            {
                bool isLifted = operatorKind.IsLifted();
                TypeWithState leftUnderlyingType = GetNullableUnderlyingTypeIfNecessary(isLifted, leftType);
                TypeWithState rightUnderlyingType = GetNullableUnderlyingTypeIfNecessary(isLifted, rightType);

                // Update method based on inferred operand type.
                MethodSymbol reinferredMethod = ReInferBinaryOperator(binary.Syntax, method, leftOperand, rightOperand, leftUnderlyingType, rightUnderlyingType);

                SetUpdatedSymbol(binary, method, reinferredMethod);
                method = reinferredMethod;

                var parameters = method.Parameters;
                VisitBinaryOperatorOperandConversionAndPostConditions(left, leftOperand, leftConversion, parameters[0], leftUnderlyingType, isLifted);
                VisitBinaryOperatorOperandConversionAndPostConditions(right, rightOperand, rightConversion, parameters[1], rightUnderlyingType, isLifted);
            }
            else
            {
                // Assume this is a built-in operator in which case the parameter types are unannotated.
                visitOperandConversion(left, leftOperand, leftConversion, leftType);
                visitOperandConversion(right, rightOperand, rightConversion, rightType);

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
            if (operatorKind.IsLifted()
                && operatorKind.Operator() is BinaryOperatorKind.GreaterThan or BinaryOperatorKind.GreaterThanOrEqual or BinaryOperatorKind.LessThan or BinaryOperatorKind.LessThanOrEqual)
            {
                Debug.Assert(binary.Type!.SpecialType == SpecialType.System_Boolean);
                SplitAndLearnFromNonNullTest(left, whenTrue: true);
                SplitAndLearnFromNonNullTest(right, whenTrue: true);
            }

            // For nested binary operators, this can be the only time they're visited due to explicit stack used in AbstractFlowPass.VisitBinaryOperator,
            // so we need to set the flow-analyzed type here.
            var inferredResult = InferResultNullability(operatorKind, method, returnType, leftType, rightType);

            return inferredResult;
        }

        private MethodSymbol ReInferBinaryOperator(
            SyntaxNode syntax,
            MethodSymbol method,
            BoundExpression leftOperand,
            BoundExpression rightOperand,
            TypeWithState leftUnderlyingType,
            TypeWithState rightUnderlyingType)
        {
            TypeSymbol methodContainer = method.ContainingType;
            MethodSymbol reinferredMethod;

            if (!method.IsExtensionBlockMember())
            {
                TypeSymbol asMemberOfType = getTypeIfContainingType(methodContainer, leftUnderlyingType.Type, leftOperand) ??
                    getTypeIfContainingType(methodContainer, rightUnderlyingType.Type, rightOperand) ?? methodContainer;
                reinferredMethod = (MethodSymbol)AsMemberOfType(asMemberOfType, method);
            }
            else if (method.ContainingType.Arity != 0)
            {
                NamedTypeSymbol extension = method.OriginalDefinition.ContainingType;
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;

                var inferenceResult = MethodTypeInferrer.Infer(
                    _binder,
                    _conversions,
                    extension.TypeParameters,
                    extension,
                    method.OriginalDefinition.ParameterTypesWithAnnotations,
                    method.OriginalDefinition.ParameterRefKinds,
                    // https://github.com/dotnet/roslyn/issues/78828: https://github.com/dotnet/roslyn/pull/79103#discussion_r2162657025
                    //            In analysis of invocations (`VisitCall`/`VisitArguments`), we use `GetArgumentsForMethodTypeInference` to get inputs to `MethodTypeInferrer.Infer`.
                    //            Do we need the same thing here (it has extra cases to deal with lambda, collection expressions and typeless expressions)?
                    [new BoundExpressionWithNullability(leftOperand.Syntax, leftOperand, leftUnderlyingType.ToTypeWithAnnotations(compilation).NullableAnnotation, leftUnderlyingType.Type),
                     new BoundExpressionWithNullability(rightOperand.Syntax, rightOperand, rightUnderlyingType.ToTypeWithAnnotations(compilation).NullableAnnotation, rightUnderlyingType.Type)],
                    ref discardedUseSiteInfo,
                    new MethodInferenceExtensions(this),
                    ordinals: null);

                if (inferenceResult.Success)
                {
                    extension = extension.Construct(inferenceResult.InferredTypeArguments);
                    method = method.OriginalDefinition.AsMember(extension);
                }

                CheckMethodConstraints(syntax, method);
                reinferredMethod = method;
            }
            else
            {
                reinferredMethod = method;
            }

            return reinferredMethod;

            TypeSymbol? getTypeIfContainingType(TypeSymbol baseType, TypeSymbol? derivedType, BoundExpression operand)
            {
                if (derivedType is null || IsTargetTypedExpression(operand))
                {
                    return null;
                }
                derivedType = derivedType.StrippedType();
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                var conversion = _conversions.ClassifyBuiltInConversion(derivedType, baseType, isChecked: false, ref discardedUseSiteInfo);
                if (conversion.Kind is ConversionKind.Identity or ConversionKind.ImplicitReference)
                {
                    return derivedType;
                }
                return null;
            }
        }

        private TypeWithState VisitBinaryOperatorOperandConversion(
            BoundExpression expr, BoundExpression operand, Conversion conversion, ParameterSymbol parameter, TypeWithState operandType, bool isLifted,
            out FlowAnalysisAnnotations parameterAnnotations)
        {
            parameterAnnotations = GetParameterAnnotations(parameter);
            var targetTypeWithNullability = ApplyLValueAnnotations(parameter.TypeWithAnnotations, parameterAnnotations);

            if (isLifted && targetTypeWithNullability.Type.IsNonNullableValueType())
            {
                targetTypeWithNullability = TypeWithAnnotations.Create(MakeNullableOf(targetTypeWithNullability));
            }

            return VisitConversion(
                expr as BoundConversion,
                operand,
                conversion,
                targetTypeWithNullability,
                operandType,
                checkConversion: true,
                fromExplicitCast: false,
                useLegacyWarnings: false,
                AssignmentKind.Argument,
                parameter);
        }

        private void VisitBinaryOperatorOperandConversionAndPostConditions(BoundExpression expr, BoundExpression operand, Conversion conversion, ParameterSymbol parameter, TypeWithState operandType, bool isLifted)
        {
            FlowAnalysisAnnotations parameterAnnotations;
            TypeWithState resultType = VisitBinaryOperatorOperandConversion(expr, operand, conversion, parameter, operandType, isLifted, out parameterAnnotations);

            if (CheckDisallowedNullAssignment(resultType, parameterAnnotations, expr.Syntax, operand))
            {
                LearnFromNonNullTest(operand, ref State);
            }

            LearnFromPostConditions(operand, parameterAnnotations);
        }

        private void AfterLeftChildHasBeenVisited(
            BoundExpression leftOperand,
            Conversion leftConversion,
            BoundBinaryOperator binary)
        {
            Debug.Assert(!IsConditionalState);
            var leftType = ResultType;

            var (rightOperand, rightConversion) = RemoveConversion(binary.Right, includeExplicitConversions: false);
            VisitRvalue(rightOperand);

            var rightType = ResultType;
            ReinferBinaryOperatorAndSetResult(leftOperand, leftConversion, leftType, rightOperand, rightConversion, rightType, binary);

            BinaryOperatorKind op = binary.OperatorKind.Operator();

            if (op == BinaryOperatorKind.Equal || op == BinaryOperatorKind.NotEqual)
            {
                // learn from null constant
                BoundExpression? operandComparedToNull = null;
                if (binary.Right.ConstantValueOpt?.IsNull == true)
                {
                    operandComparedToNull = binary.Left;
                }
                else if (binary.Left.ConstantValueOpt?.IsNull == true)
                {
                    operandComparedToNull = binary.Right;
                }

                if (operandComparedToNull != null)
                {
                    // Set all nested conditional slots. For example in a?.b?.c we'll set a, b, and c.
                    bool nonNullCase = op != BinaryOperatorKind.Equal; // true represents WhenTrue
                    SplitAndLearnFromNonNullTest(operandComparedToNull, whenTrue: nonNullCase);

                    // `x == null` and `x != null` are pure null tests so update the null-state in the alternative branch too
                    LearnFromNullTest(operandComparedToNull, ref nonNullCase ? ref StateWhenFalse : ref StateWhenTrue);
                    return;
                }
            }

            // learn from comparison between non-null and maybe-null, possibly updating maybe-null to non-null
            BoundExpression? operandComparedToNonNull = null;
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
                        SplitAndLearnFromNonNullTest(operandComparedToNonNull, whenTrue: true);
                        return;
                    case BinaryOperatorKind.NotEqual:
                        operandComparedToNonNull = SkipReferenceConversions(operandComparedToNonNull);
                        SplitAndLearnFromNonNullTest(operandComparedToNonNull, whenTrue: false);
                        return;
                }
            }
        }

        private void SplitAndLearnFromNonNullTest(BoundExpression operandComparedToNonNull, bool whenTrue)
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

        protected override bool VisitInterpolatedStringHandlerParts(BoundInterpolatedStringBase node, bool usesBoolReturns, bool firstPartIsConditional, ref LocalState shortCircuitState)
        {
            var result = base.VisitInterpolatedStringHandlerParts(node, usesBoolReturns, firstPartIsConditional, ref shortCircuitState);
            SetNotNullResult(node);
            return result;
        }

        protected override void VisitInterpolatedStringBinaryOperatorNode(BoundBinaryOperator node)
        {
            SetNotNullResult(node);
        }

        /// <summary>
        /// If we learn that the operand is non-null, we can infer that certain
        /// sub-expressions were also non-null.
        /// Get all nested conditional slots for those sub-expressions. For example in a?.b?.c we'll set a, b, and c.
        /// Only returns slots for tracked expressions.
        /// </summary>
        /// <remarks>https://github.com/dotnet/roslyn/issues/53397 This method should potentially be removed.</remarks>
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
                        case BoundKind.AsOperator:
                            operand = ((BoundAsOperator)operand).Operand;
                            continue;
                        case BoundKind.ConditionalAccess:
                            var conditional = (BoundConditionalAccess)operand;

                            GetSlotsToMarkAsNotNullable(conditional.Receiver, slotBuilder);
                            slot = MakeSlot(conditional.Receiver);
                            if (slot > 0)
                            {
                                // We need to continue the walk regardless of whether the receiver should be updated.
                                var receiverType = conditional.Receiver.Type!;
                                if (receiverType.IsNullableType())
                                    slot = GetNullableOfTValueSlot(receiverType, slot, out _);
                            }

                            if (slot > 0)
                            {
                                // When MakeSlot is called on the nested AccessExpression, it will recurse through receivers
                                // until it gets to the BoundConditionalReceiver associated with this node. In our override
                                // of MakeSlot, we substitute this slot when we encounter a BoundConditionalReceiver, and reset the
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

        private static bool PossiblyNullableType([NotNullWhen(true)] TypeSymbol? operandType) => operandType?.CanContainNull() == true;

        private void MarkSlotsAsNotNull(ArrayBuilder<int> slots, ref LocalState stateToUpdate)
        {
            foreach (int slot in slots)
            {
                SetState(ref stateToUpdate, slot, NullableFlowState.NotNull);
            }
        }

        private void LearnFromNonNullTest(BoundExpression expression, ref LocalState state)
        {
            if (expression is BoundValuePlaceholderBase placeholder)
            {
                if (_resultForPlaceholdersOpt != null &&
                    _resultForPlaceholdersOpt.TryGetValue(placeholder, out var value) &&
                    value.Replacement != null)
                {
                    expression = value.Replacement;
                }
                else
                {
                    AssertPlaceholderAllowedWithoutRegistration(placeholder);
                    return;
                }
            }

            var slotBuilder = ArrayBuilder<int>.GetInstance();
            GetSlotsToMarkAsNotNullable(expression, slotBuilder);
            MarkSlotsAsNotNull(slotBuilder, ref state);
            slotBuilder.Free();
        }

        private void LearnFromNonNullTest(int slot, ref LocalState state)
        {
            SetState(ref state, slot, NullableFlowState.NotNull);
        }

        private void LearnFromNullTest(BoundExpression expression, ref LocalState state)
        {
            // nothing to learn about a constant
            if (expression.ConstantValueOpt != null)
                return;

            // We should not blindly strip conversions here. Tracked by https://github.com/dotnet/roslyn/issues/36164
            var expressionWithoutConversion = RemoveConversion(expression, includeExplicitConversions: true).expression;
            var slot = MakeSlot(expressionWithoutConversion);

            // Since we know for sure the slot is null (we just tested it), we know that dependent slots are not
            // reachable and therefore can be treated as not null.  However, we have not computed the proper
            // (inferred) type for the expression, so we cannot compute the correct symbols for the member slots here
            // (using the incorrect symbols would result in computing an incorrect default state for them).
            // Therefore we do not mark dependent slots not null.  See https://github.com/dotnet/roslyn/issues/39624
            LearnFromNullTest(slot, expressionWithoutConversion.Type, ref state, markDependentSlotsNotNull: false);
        }

        private void LearnFromNullTest(int slot, TypeSymbol? expressionType, ref LocalState state, bool markDependentSlotsNotNull)
        {
            if (slot > 0 && PossiblyNullableType(expressionType))
            {
                if (GetState(ref state, slot) == NullableFlowState.NotNull)
                {
                    // Note: We leave a MaybeDefault state as-is
                    SetState(ref state, slot, NullableFlowState.MaybeNull);
                }

                if (markDependentSlotsNotNull)
                {
                    MarkDependentSlotsNotNull(slot, expressionType, ref state);
                }
            }
        }

        // If we know for sure that a slot contains a null value, then we know for sure that dependent slots
        // are "unreachable" so we might as well treat them as not null.  That way when this state is merged
        // with another state, those dependent states won't pollute values from the other state.
        private void MarkDependentSlotsNotNull(int slot, TypeSymbol expressionType, ref LocalState state, int depth = 2)
        {
            if (depth <= 0)
                return;

            foreach (var member in getMembers(expressionType))
            {
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                var containingType = this._symbol?.ContainingType;
                if ((member is PropertySymbol { IsIndexedProperty: false } || member.Kind == SymbolKind.Field) &&
                    member.RequiresInstanceReceiver() &&
                    (containingType is null || AccessCheck.IsSymbolAccessible(member, containingType, ref discardedUseSiteInfo)))
                {
                    int childSlot = GetOrCreateSlot(member, slot, forceSlotEvenIfEmpty: true, createIfMissing: false);
                    if (childSlot > 0)
                    {
                        SetState(ref state, childSlot, NullableFlowState.NotNull);
                        MarkDependentSlotsNotNull(childSlot, GetTypeOrReturnType(member), ref state, depth - 1);
                    }
                }
            }

            static IEnumerable<Symbol> getMembers(TypeSymbol type)
            {
                // First, return the direct members
                foreach (var member in type.GetMembers())
                    yield return member;

                // All types inherit members from their effective bases
                for (NamedTypeSymbol baseType = effectiveBase(type); !(baseType is null); baseType = baseType.BaseTypeNoUseSiteDiagnostics)
                    foreach (var member in baseType.GetMembers())
                        yield return member;

                // Interfaces and type parameters inherit from their effective interfaces
                foreach (NamedTypeSymbol interfaceType in inheritedInterfaces(type))
                    foreach (var member in interfaceType.GetMembers())
                        yield return member;

                yield break;

                static NamedTypeSymbol effectiveBase(TypeSymbol type) => type switch
                {
                    TypeParameterSymbol tp => tp.EffectiveBaseClassNoUseSiteDiagnostics,
                    var t => t.BaseTypeNoUseSiteDiagnostics,
                };

                static ImmutableArray<NamedTypeSymbol> inheritedInterfaces(TypeSymbol type) => type switch
                {
                    TypeParameterSymbol tp => tp.AllEffectiveInterfacesNoUseSiteDiagnostics,
                    { TypeKind: TypeKind.Interface } => type.AllInterfacesNoUseSiteDiagnostics,
                    _ => ImmutableArray<NamedTypeSymbol>.Empty,
                };
            }
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

        public override BoundNode? VisitNullCoalescingAssignmentOperator(BoundNullCoalescingAssignmentOperator node)
        {
            BoundExpression leftOperand = node.LeftOperand;
            BoundExpression rightOperand = node.RightOperand;
            int leftSlot = MakeSlot(leftOperand);

            TypeWithAnnotations targetType = VisitLvalueWithAnnotations(leftOperand);
            var leftState = this.State.Clone();
            LearnFromNonNullTest(leftOperand, ref leftState);
            LearnFromNullTest(leftOperand, ref this.State);

            // If we are assigning to a nullable value type variable, set the top-level state of
            // the LHS first, then change the slot to the Value property of the LHS to simulate
            // assignment of the RHS and update the nullable state of the underlying value type.
            if (node.IsNullableValueTypeAssignment)
            {
                Debug.Assert(targetType.Type.ContainsErrorType() ||
                    node.Type?.ContainsErrorType() == true ||
                    TypeSymbol.Equals(targetType.Type.GetNullableUnderlyingType(), node.Type, TypeCompareKind.AllIgnoreOptions));
                if (leftSlot > 0)
                {
                    SetState(ref this.State, leftSlot, NullableFlowState.NotNull);
                    leftSlot = GetNullableOfTValueSlot(targetType.Type, leftSlot, out _);
                }
                targetType = TypeWithAnnotations.Create(node.Type, NullableAnnotation.NotAnnotated);
            }

            TypeWithState rightResult = VisitOptionalImplicitConversion(rightOperand, targetType, useLegacyWarnings: UseLegacyWarnings(leftOperand), trackMembers: false, AssignmentKind.Assignment);
            Debug.Assert(TypeSymbol.Equals(targetType.Type, rightResult.Type, TypeCompareKind.AllIgnoreOptions));
            TrackNullableStateForAssignment(rightOperand, targetType, leftSlot, rightResult, MakeSlot(rightOperand));

            Join(ref this.State, ref leftState);
            TypeWithState resultType = TypeWithState.Create(targetType.Type, rightResult.State);
            SetResultType(node, resultType);
            return null;
        }

        public override BoundNode? VisitNullCoalescingOperator(BoundNullCoalescingOperator node)
        {
            Debug.Assert(!IsConditionalState);

            BoundExpression leftOperand = node.LeftOperand;
            BoundExpression rightOperand = node.RightOperand;

            if (IsConstantNull(leftOperand))
            {
                VisitRvalue(leftOperand);
                Visit(rightOperand);
                var rightUnconditionalResult = ResultType;
                // Should be able to use rightResult for the result of the operator but
                // binding may have generated a different result type in the case of errors.
                SetResultType(node, TypeWithState.Create(node.Type, rightUnconditionalResult.State));
                return null;
            }

            VisitPossibleConditionalAccess(leftOperand, out var whenNotNull);
            TypeWithState leftResult = ResultType;
            Unsplit();
            LearnFromNullTest(leftOperand, ref this.State);

            bool leftIsConstant = leftOperand.ConstantValueOpt != null;
            if (leftIsConstant)
            {
                SetUnreachable();
            }

            Visit(rightOperand);
            TypeWithState rightResult = ResultType;

            Join(ref whenNotNull);

            var leftResultType = leftResult.Type;
            var rightResultType = rightResult.Type;

            var (resultType, leftState) = node.OperatorResultKind switch
            {
                BoundNullCoalescingOperatorResultKind.NoCommonType => (node.Type, NullableFlowState.NotNull),
                BoundNullCoalescingOperatorResultKind.LeftType => getLeftResultType(leftResultType!, rightResultType!),
                BoundNullCoalescingOperatorResultKind.LeftUnwrappedType => getLeftResultType(leftResultType!.StrippedType(), rightResultType!),
                BoundNullCoalescingOperatorResultKind.RightType => getResultStateWithRightType(leftResultType!, rightResultType!),
                BoundNullCoalescingOperatorResultKind.LeftUnwrappedRightType => getResultStateWithRightType(leftResultType!.StrippedType(), rightResultType!),
                BoundNullCoalescingOperatorResultKind.RightDynamicType => (rightResultType!, NullableFlowState.NotNull),
                _ => throw ExceptionUtilities.UnexpectedValue(node.OperatorResultKind),
            };

            SetResultType(node, TypeWithState.Create(resultType, rightResult.State.Join(leftState)));
            return null;

            (TypeSymbol ResultType, NullableFlowState LeftState) getLeftResultType(TypeSymbol leftType, TypeSymbol rightType)
            {
                Debug.Assert(rightType is object);
                // If there was an identity conversion between the two operands (in short, if there
                // is no implicit conversion on the right operand), then check nullable conversions
                // in both directions since it's possible the right operand is the better result type.
                if ((node.RightOperand as BoundConversion)?.ExplicitCastInCode != false &&
                    GenerateConversionForConditionalOperator(node.LeftOperand, leftType, rightType, reportMismatch: false, isChecked: node.Checked) is { Exists: true } conversion)
                {
                    Debug.Assert(!conversion.IsUserDefined);
                    return (rightType, NullableFlowState.NotNull);
                }

                conversion = GenerateConversionForConditionalOperator(node.RightOperand, rightType, leftType, reportMismatch: true, isChecked: node.Checked);
                Debug.Assert(!conversion.IsUserDefined);
                return (leftType, NullableFlowState.NotNull);
            }

            (TypeSymbol ResultType, NullableFlowState LeftState) getResultStateWithRightType(TypeSymbol leftType, TypeSymbol rightType)
            {
                var conversion = GenerateConversionForConditionalOperator(node.LeftOperand, leftType, rightType, reportMismatch: true, isChecked: node.Checked);
                if (conversion.IsUserDefined)
                {
                    var conversionResult = VisitConversion(
                        conversionOpt: null,
                        node.LeftOperand,
                        conversion,
                        TypeWithAnnotations.Create(rightType),
                        // When considering the conversion on the left node, it can only occur in the case where the underlying
                        // execution returned non-null
                        TypeWithState.Create(leftType, NullableFlowState.NotNull),
                        checkConversion: false,
                        fromExplicitCast: false,
                        useLegacyWarnings: false,
                        AssignmentKind.Assignment,
                        reportTopLevelWarnings: false,
                        reportRemainingWarnings: false);
                    Debug.Assert(conversionResult.Type is not null);
                    return (conversionResult.Type!, conversionResult.State);
                }

                return (rightType, NullableFlowState.NotNull);
            }
        }

        /// <summary>
        /// Visits a node only if it is a conditional access.
        /// Returns 'true' if and only if the node was visited.
        /// </summary>
        private bool TryVisitConditionalAccess(BoundExpression node, out PossiblyConditionalState stateWhenNotNull)
        {
            var (operand, conversion) = RemoveConversion(node, includeExplicitConversions: true);
            if (operand is not BoundConditionalAccess access || !CanPropagateStateWhenNotNull(conversion))
            {
                stateWhenNotNull = default;
                return false;
            }

            Unsplit();
            VisitConditionalAccess(access, out stateWhenNotNull);
            if (node is BoundConversion boundConversion)
            {
                var operandType = ResultType;
                TypeWithAnnotations explicitType = boundConversion.ConversionGroupOpt?.ExplicitType ?? default;
                bool fromExplicitCast = explicitType.HasType;
                TypeWithAnnotations targetType = fromExplicitCast ? explicitType : TypeWithAnnotations.Create(boundConversion.Type);
                Debug.Assert(targetType.HasType);
                var result = VisitConversion(boundConversion,
                    access,
                    conversion,
                    targetType,
                    operandType,
                    checkConversion: true,
                    fromExplicitCast,
                    useLegacyWarnings: true,
                    assignmentKind: AssignmentKind.Assignment);
                SetResultType(boundConversion, result);
            }
            Debug.Assert(!IsConditionalState);
            return true;
        }

        /// <summary>
        /// Unconditionally visits an expression and returns the "state when not null" for the expression.
        /// </summary>
        private bool VisitPossibleConditionalAccess(BoundExpression node, out PossiblyConditionalState stateWhenNotNull)
        {
            if (TryVisitConditionalAccess(node, out stateWhenNotNull))
            {
                return true;
            }

            // in case we *didn't* have a conditional access, the only thing we learn in the "state when not null"
            // is that the top-level expression was non-null.
            Visit(node);
            stateWhenNotNull = PossiblyConditionalState.Create(this);

            (node, _) = RemoveConversion(node, includeExplicitConversions: true);
            var slot = MakeSlot(node);
            if (slot > -1)
            {
                if (IsConditionalState)
                {
                    LearnFromNonNullTest(slot, ref stateWhenNotNull.StateWhenTrue);
                    LearnFromNonNullTest(slot, ref stateWhenNotNull.StateWhenFalse);
                }
                else
                {
                    LearnFromNonNullTest(slot, ref stateWhenNotNull.State);
                }
            }
            return false;
        }

        private void VisitConditionalAccess(BoundConditionalAccess node, out PossiblyConditionalState stateWhenNotNull)
        {
            Debug.Assert(!IsConditionalState);

            var receiver = node.Receiver;

            // handle scenarios like `(a?.b)?.c()`
            VisitPossibleConditionalAccess(receiver, out stateWhenNotNull);
            Unsplit();

            _currentConditionalReceiverVisitResult = _visitResult;
            var previousConditionalAccessSlot = _lastConditionalAccessSlot;

            if (receiver.ConstantValueOpt is { IsNull: false })
            {
                // Consider a scenario like `"a"?.M0(x = 1)?.M0(y = 1)`.
                // We can "know" that `.M0(x = 1)` was evaluated unconditionally but not `M0(y = 1)`.
                // Therefore we do a VisitPossibleConditionalAccess here which unconditionally includes the "after receiver" state in State
                // and includes the "after subsequent conditional accesses" in stateWhenNotNull
                VisitPossibleConditionalAccess(node.AccessExpression, out stateWhenNotNull);
                Unsplit();
            }
            else
            {
                var savedState = this.State.Clone();
                if (IsConstantNull(receiver))
                {
                    SetUnreachable();
                    _lastConditionalAccessSlot = -1;
                }
                else
                {
                    // In the when-null branch, the receiver is known to be maybe-null.
                    // In the other branch, the receiver is known to be non-null.
                    LearnFromNullTest(receiver, ref savedState);
                    makeAndAdjustReceiverSlot(receiver);
                    SetPossiblyConditionalState(stateWhenNotNull);
                }

                // We want to preserve stateWhenNotNull from accesses in the same "chain":
                // a?.b(out x)?.c(out y); // expected to preserve stateWhenNotNull from both ?.b(out x) and ?.c(out y)
                // but not accesses in nested expressions:
                // a?.b(out x, c?.d(out y)); // expected to preserve stateWhenNotNull from a?.b(out x, ...) but not from c?.d(out y)
                BoundExpression expr = node.AccessExpression;
                while (expr is BoundConditionalAccess innerCondAccess)
                {
                    // we assume that non-conditional accesses can never contain conditional accesses from the same "chain".
                    // that is, we never have to dig through non-conditional accesses to find and handle conditional accesses.
                    Debug.Assert(innerCondAccess.Receiver is not (BoundConditionalAccess or BoundConversion));
                    VisitRvalue(innerCondAccess.Receiver);
                    _currentConditionalReceiverVisitResult = _visitResult;
                    makeAndAdjustReceiverSlot(innerCondAccess.Receiver);

                    // The savedState here represents the scenario where 0 or more of the access expressions could have been evaluated.
                    // e.g. after visiting `a?.b(x = null)?.c(x = new object())`, the "state when not null" of `x` is NotNull, but the "state when maybe null" of `x` is MaybeNull.
                    Join(ref savedState, ref State);

                    expr = innerCondAccess.AccessExpression;
                }

                Debug.Assert(expr is BoundExpression);
                Visit(expr);

                expr = node.AccessExpression;
                while (expr is BoundConditionalAccess innerCondAccess)
                {
                    // The resulting nullability of each nested conditional access is the same as the resulting nullability of the rightmost access.
                    SetAnalyzedNullability(innerCondAccess, _visitResult);
                    expr = innerCondAccess.AccessExpression;
                }
                Debug.Assert(expr is BoundExpression);
                var slot = MakeSlot(expr);
                if (slot > -1)
                {
                    if (IsConditionalState)
                    {
                        LearnFromNonNullTest(slot, ref StateWhenTrue);
                        LearnFromNonNullTest(slot, ref StateWhenFalse);
                    }
                    else
                    {
                        LearnFromNonNullTest(slot, ref State);
                    }
                }

                stateWhenNotNull = PossiblyConditionalState.Create(this);
                Unsplit();
                Join(ref this.State, ref savedState);
            }

            var accessTypeWithAnnotations = LvalueResultType;
            TypeSymbol accessType = accessTypeWithAnnotations.Type;
            var oldType = node.Type;
            var resultType =
                oldType.IsVoidType() || oldType.IsErrorType() ? oldType :
                oldType.IsNullableType() && !accessType.IsNullableType() ? MakeNullableOf(accessTypeWithAnnotations) :
                accessType;

            // Per LDM 2019-02-13 decision, the result of a conditional access "may be null" even if
            // both the receiver and right-hand-side are believed not to be null.
            SetResultType(node, TypeWithState.Create(resultType, NullableFlowState.MaybeDefault));
            _currentConditionalReceiverVisitResult = default;
            _lastConditionalAccessSlot = previousConditionalAccessSlot;

            void makeAndAdjustReceiverSlot(BoundExpression receiver)
            {
                var slot = MakeSlot(receiver);
                if (slot > -1)
                    LearnFromNonNullTest(slot, ref State);

                // given `a?.b()`, when `a` is a nullable value type,
                // the conditional receiver for `?.b()` must be linked to `a.Value`, not to `a`.
                if (slot > 0 && receiver.Type?.IsNullableType() == true)
                    slot = GetNullableOfTValueSlot(receiver.Type, slot, out _);

                _lastConditionalAccessSlot = slot;
            }
        }

        public override BoundNode? VisitConditionalAccess(BoundConditionalAccess node)
        {
            VisitConditionalAccess(node, out _);
            return null;
        }

        protected override BoundNode? VisitConditionalOperatorCore(
            BoundExpression node,
            bool isRef,
            BoundExpression condition,
            BoundExpression originalConsequence,
            BoundExpression originalAlternative)
        {
            VisitCondition(condition);
            var consequenceState = this.StateWhenTrue;
            var alternativeState = this.StateWhenFalse;

            TypeWithState consequenceRValue;
            TypeWithState alternativeRValue;

            if (isRef)
            {
                Debug.Assert(node is not BoundConditionalOperator { WasTargetTyped: true }, """
                    Unexpected ref target typed conditional operator.
                    Should not do type inference below in this case.
                    """);

                TypeWithAnnotations consequenceLValue;
                TypeWithAnnotations alternativeLValue;

                (consequenceLValue, consequenceRValue) = visitConditionalRefOperand(consequenceState, originalConsequence);
                consequenceState = this.State;
                (alternativeLValue, alternativeRValue) = visitConditionalRefOperand(alternativeState, originalAlternative);
                Join(ref this.State, ref consequenceState);

                var lValueAnnotation = consequenceLValue.NullableAnnotation.EnsureCompatible(alternativeLValue.NullableAnnotation);
                var rValueState = consequenceRValue.State.Join(alternativeRValue.State);

                TypeSymbol? refResultType = node.Type?.SetUnknownNullabilityForReferenceTypes();
                if (IsNullabilityMismatch(consequenceLValue, alternativeLValue))
                {
                    // If there is a mismatch between the operands, use type inference to determine the target type.
                    BoundExpression consequencePlaceholder = CreatePlaceholderIfNecessary(originalConsequence, consequenceLValue);
                    BoundExpression alternativePlaceholder = CreatePlaceholderIfNecessary(originalAlternative, alternativeLValue);
                    var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    refResultType = BestTypeInferrer.InferBestTypeForConditionalOperator(consequencePlaceholder, alternativePlaceholder, _conversions, out _, ref discardedUseSiteInfo);

                    // Report warning for each operand that is not convertible to the target type.
                    var refResultTypeWithAnnotations = TypeWithAnnotations.Create(refResultType, lValueAnnotation);
                    reportMismatchIfNecessary(originalConsequence, consequenceLValue, refResultTypeWithAnnotations);
                    reportMismatchIfNecessary(originalAlternative, alternativeLValue, refResultTypeWithAnnotations);
                }
                else if (!node.HasErrors)
                {
                    refResultType = consequenceRValue.Type!.MergeEquivalentTypes(alternativeRValue.Type, VarianceKind.None);
                }

                SetResult(node, TypeWithState.Create(refResultType, rValueState), TypeWithAnnotations.Create(refResultType, lValueAnnotation));
                return null;
            }

            (var consequence, var consequenceConversion, consequenceRValue) = visitConditionalOperand(consequenceState, originalConsequence);
            var consequenceConditionalState = PossiblyConditionalState.Create(this);
            consequenceState = CloneAndUnsplit(ref consequenceConditionalState);
            var consequenceEndReachable = consequenceState.Reachable;

            (var alternative, var alternativeConversion, alternativeRValue) = visitConditionalOperand(alternativeState, originalAlternative);
            var alternativeConditionalState = PossiblyConditionalState.Create(this);
            alternativeState = CloneAndUnsplit(ref alternativeConditionalState);
            var alternativeEndReachable = alternativeState.Reachable;

            SetPossiblyConditionalState(in consequenceConditionalState);
            Join(ref alternativeConditionalState);

            TypeSymbol? resultType;
            bool wasTargetTyped = node is BoundConditionalOperator { WasTargetTyped: true };
            if (wasTargetTyped)
            {
                resultType = null;
            }
            else if (IsTargetTypedExpression(consequence))
            {
                resultType = alternativeRValue.Type;
            }
            else if (IsTargetTypedExpression(alternative))
            {
                resultType = consequenceRValue.Type;
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
                BoundExpression consequencePlaceholder = CreatePlaceholderIfNecessary(consequence, consequenceRValue.ToTypeWithAnnotations(compilation));
                BoundExpression alternativePlaceholder = CreatePlaceholderIfNecessary(alternative, alternativeRValue.ToTypeWithAnnotations(compilation));
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                resultType = BestTypeInferrer.InferBestTypeForConditionalOperator(consequencePlaceholder, alternativePlaceholder, _conversions, out _, ref discardedUseSiteInfo);
            }

            resultType ??= node.Type?.SetUnknownNullabilityForReferenceTypes();

            UnsplitIfNeeded(resultType);

            TypeWithAnnotations resultTypeWithAnnotations;

            if (resultType is null)
            {
                Debug.Assert(!wasTargetTyped);
                if (!wasTargetTyped)
                {
                    // This can happen when we're inferring the return type of a lambda or visiting a node without diagnostics like
                    // BoundConvertedTupleLiteral.SourceTuple. For these cases, we don't need to do any work,
                    // the unconverted conditional operator can't contribute info. The conversion that should be on top of this
                    // can add or remove nullability, and nested nodes aren't being publicly exposed by the semantic model.
                    Debug.Assert(node is BoundUnconvertedConditionalOperator);
                    Debug.Assert(_returnTypesOpt is not null || _disableDiagnostics);
                    SetResultType(node, TypeWithState.Create(resultType, default));
                    return null;
                }

                resultTypeWithAnnotations = default;
            }
            else
            {
                resultTypeWithAnnotations = TypeWithAnnotations.Create(resultType);
            }

            TypeWithState typeWithState = convertArms(
                                                node, originalConsequence, originalAlternative, consequenceState, alternativeState, consequenceRValue, alternativeRValue,
                                                consequence, consequenceConversion, consequenceEndReachable, alternative, alternativeConversion, alternativeEndReachable,
                                                resultTypeWithAnnotations, wasTargetTyped);
            SetResultType(node, typeWithState, updateAnalyzedNullability: false);
            return null;

            TypeWithState convertArms(
                BoundExpression node, BoundExpression originalConsequence, BoundExpression originalAlternative, LocalState consequenceState, LocalState alternativeState,
                TypeWithState consequenceRValue, TypeWithState alternativeRValue, BoundExpression consequence, Conversion consequenceConversion, bool consequenceEndReachable,
                BoundExpression alternative, Conversion alternativeConversion, bool alternativeEndReachable, TypeWithAnnotations resultTypeWithAnnotations, bool wasTargetTyped)
            {
                NullableFlowState resultState;
                if (!wasTargetTyped)
                {
                    TypeWithState convertedConsequenceResult = ConvertConditionalOperandOrSwitchExpressionArmResult(
                        originalConsequence,
                        consequence,
                        consequenceConversion,
                        resultTypeWithAnnotations,
                        consequenceRValue,
                        consequenceState,
                        consequenceEndReachable);

                    TypeWithState convertedAlternativeResult = ConvertConditionalOperandOrSwitchExpressionArmResult(
                        originalAlternative,
                        alternative,
                        alternativeConversion,
                        resultTypeWithAnnotations,
                        alternativeRValue,
                        alternativeState,
                        alternativeEndReachable);

                    resultState = convertedConsequenceResult.State.Join(convertedAlternativeResult.State);
                    var typeWithState = TypeWithState.Create(resultTypeWithAnnotations.Type, resultState);
                    SetAnalyzedNullability(node, typeWithState);
                    return typeWithState;
                }
                else
                {
                    addConvertArmsAsCompletion(
                                  node, originalConsequence, originalAlternative, consequenceState, alternativeState,
                                  consequenceRValue, alternativeRValue,
                                  consequence, consequenceConversion, consequenceEndReachable,
                                  alternative, alternativeConversion, alternativeEndReachable);

                    resultState = consequenceRValue.State.Join(alternativeRValue.State);
                    return TypeWithState.Create(resultTypeWithAnnotations.Type, resultState);
                }
            }

            void addConvertArmsAsCompletion(
                BoundExpression node,
                BoundExpression originalConsequence,
                BoundExpression originalAlternative,
                LocalState consequenceState,
                LocalState alternativeState,
                TypeWithState consequenceRValue,
                TypeWithState alternativeRValue,
                BoundExpression consequence,
                Conversion consequenceConversion,
                bool consequenceEndReachable,
                BoundExpression alternative,
                Conversion alternativeConversion,
                bool alternativeEndReachable)
            {
                TargetTypedAnalysisCompletion[node] =
                    (TypeWithAnnotations resultTypeWithAnnotations) =>
                    {
                        return convertArms(
                                   node, originalConsequence, originalAlternative, consequenceState, alternativeState, consequenceRValue, alternativeRValue,
                                   consequence, consequenceConversion, consequenceEndReachable, alternative, alternativeConversion, alternativeEndReachable,
                                   resultTypeWithAnnotations, wasTargetTyped: false);
                    };
            }

            (BoundExpression, Conversion, TypeWithState) visitConditionalOperand(LocalState state, BoundExpression operand)
            {
                Conversion conversion;
                SetState(state);
                Debug.Assert(!isRef);

                BoundExpression operandNoConversion;
                (operandNoConversion, conversion) = RemoveConversion(operand, includeExplicitConversions: false);
                SnapshotWalkerThroughConversionGroup(operand, operandNoConversion);
                Visit(operandNoConversion);
                return (operandNoConversion, conversion, ResultType);
            }

            (TypeWithAnnotations LValueType, TypeWithState RValueType) visitConditionalRefOperand(LocalState state, BoundExpression operand)
            {
                SetState(state);
                Debug.Assert(isRef);
                TypeWithAnnotations lValueType = VisitLvalueWithAnnotations(operand);
                return (lValueType, ResultType);
            }

            void reportMismatchIfNecessary(BoundExpression node, TypeWithAnnotations source, TypeWithAnnotations destination)
            {
                if (!node.IsSuppressed && IsNullabilityMismatch(source, destination))
                {
                    ReportNullabilityMismatchInAssignment(node.Syntax, source, destination);
                }
            }
        }

        private TypeWithState ConvertConditionalOperandOrSwitchExpressionArmResult(
            BoundExpression node,
            BoundExpression operand,
            Conversion conversion,
            TypeWithAnnotations targetType,
            TypeWithState operandType,
            LocalState state,
            bool isReachable)
        {
            var savedState = PossiblyConditionalState.Create(this);
            this.SetState(state);

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
            this.SetPossiblyConditionalState(in savedState);

            return resultType;
        }

        private bool IsReachable()
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

        public override BoundNode? VisitConditionalReceiver(BoundConditionalReceiver node)
        {
            var rvalueType = _currentConditionalReceiverVisitResult.RValueType.Type;
            if (rvalueType?.IsNullableType() == true)
            {
                rvalueType = rvalueType.GetNullableUnderlyingType();
            }
            SetResultType(node, TypeWithState.Create(rvalueType, NullableFlowState.NotNull));
            return null;
        }

        public override BoundNode? VisitCall(BoundCall node)
        {
            // Note: we analyze even omitted calls
            if (tryGetReceiver(node, out BoundCall? receiver))
            {
                // Handle long call chain of both instance and extension method invocations.
                var calls = ArrayBuilder<BoundCall>.GetInstance();

                calls.Push(node);
                node = receiver;

                bool originalExpressionIsRead = _expressionIsRead;
                _expressionIsRead = true;

                while (tryGetReceiver(node, out BoundCall? receiver2))
                {
                    TakeIncrementalSnapshot(node); // Visit does this before visiting each node
                    calls.Push(node);
                    node = receiver2;
                }

                TakeIncrementalSnapshot(node); // Visit does this before visiting each node
                TypeWithState receiverType = visitAndCheckReceiver(node);

                VisitResult? extensionReceiverResult = null;
                while (true)
                {
                    reinferMethodAndVisitArguments(node, receiverType, firstArgumentResult: extensionReceiverResult);

                    receiver = node;
                    if (!calls.TryPop(out node!))
                    {
                        break;
                    }

                    VisitExpressionWithoutStackGuardEpilogue(receiver); // VisitExpressionWithoutStackGuard does this after visiting each node

                    if (node.IsErroneousNode)
                    {
                        VisitRvalueEpilogue(receiver); // VisitRvalue does this after visiting each node

                        if (node.ReceiverOpt is not null)
                        {
                            receiverType = ResultType;
                            extensionReceiverResult = null;
                        }
                        else
                        {
                            Debug.Assert(node.InvokedAsExtensionMethod);
                            extensionReceiverResult = _visitResult;
                            receiverType = default;
                        }
                    }
                    else
                    {
                        bool isExtensionBlockMethod = node.Method.IsExtensionBlockMember();

                        // Only instance receivers go through VisitRvalue; arguments go through VisitArgumentEvaluate.
                        if (node.ReceiverOpt is not null && !isExtensionBlockMethod)
                        {
                            VisitRvalueEpilogue(receiver); // VisitRvalue does this after visiting each node
                            receiverType = ResultType;
                            CheckCallReceiver(receiver, receiverType, node.Method);
                            extensionReceiverResult = null;
                        }
                        else
                        {
                            // The receiver for new extension methods is analyzed as an argument
                            Debug.Assert(node.InvokedAsExtensionMethod || isExtensionBlockMethod);

                            var refKind = isExtensionBlockMethod ? GetExtensionReceiverRefKind(node.Method) : GetRefKind(node.ArgumentRefKindsOpt, 0);

                            FlowAnalysisAnnotations annotations;
                            if (isExtensionBlockMethod)
                            {
                                Debug.Assert(node.Method.ContainingType.ExtensionParameter is not null);
                                annotations = node.Method.ContainingType.ExtensionParameter.FlowAnalysisAnnotations;
                            }
                            else
                            {
                                TypeWithAnnotations paramsIterationType = default;
                                annotations = GetCorrespondingParameter(0, node.Method.Parameters, node.ArgsToParamsOpt, node.Expanded, ref paramsIterationType).Annotations;
                            }

                            extensionReceiverResult = VisitArgumentEvaluateEpilogue(receiver, refKind, annotations);
                            receiverType = default;
                        }
                    }
                }

                _expressionIsRead = originalExpressionIsRead;

                calls.Free();
            }
            else
            {
                TypeWithState receiverType = visitAndCheckReceiver(node);
                reinferMethodAndVisitArguments(node, receiverType);
            }

            return null;

            // Gets the instance or extension invocation receiver if any.
            bool tryGetReceiver(BoundCall node, [MaybeNullWhen(returnValue: false)] out BoundCall receiver)
            {
                if (node.ReceiverOpt is BoundCall instanceReceiver)
                {
                    receiver = instanceReceiver;
                    return true;
                }

                if (node.InvokedAsExtensionMethod && node.Arguments is [BoundCall extensionReceiver, ..] &&
                    // Exclude arguments that need saving state before visiting (only lambdas currently).
                    !VisitArgumentEvaluateNeedsCloningState(extensionReceiver))
                {
                    Debug.Assert(node.ReceiverOpt is null);
                    receiver = extensionReceiver;
                    return true;
                }

                receiver = null;
                return false;
            }

            TypeWithState visitAndCheckReceiver(BoundCall node)
            {
                if (node.IsErroneousNode)
                {
                    return VisitBadExpressionChild(node.ReceiverOpt);
                }

                return VisitAndCheckReceiver(node.ReceiverOpt, node.Method);
            }

            void reinferMethodAndVisitArguments(BoundCall node, TypeWithState receiverType, VisitResult? firstArgumentResult = null)
            {
                if (node.IsErroneousNode)
                {
                    for (int i = 0; i < node.Arguments.Length; i++)
                    {
                        if (i == 0 && firstArgumentResult is { })
                        {
                            continue;
                        }

                        BoundExpression? child = node.Arguments[i];
                        VisitBadExpressionChild(child);
                    }

                    var type = TypeWithAnnotations.Create(node.Type);
                    SetLvalueResultType(node, type);
                    SetUpdatedSymbol(node, node.Method, node.Method);
                    return;
                }

                (MethodSymbol method, ImmutableArray<VisitResult> results, bool returnNotNull) = ReInferMethodAndVisitArguments(
                    node,
                    node.ReceiverOpt,
                    receiverType,
                    node.Method,
                    node.Arguments,
                    node.ArgumentRefKindsOpt,
                    node.ArgsToParamsOpt,
                    node.DefaultArguments,
                    node.Expanded,
                    node.InvokedAsExtensionMethod,
                    firstArgumentResult);

                LearnFromEqualsMethod(method, node, receiverType, results);

                var returnState = GetReturnTypeWithState(method);
                if (returnNotNull)
                {
                    returnState = returnState.WithNotNullState();
                }

                SetResult(node, returnState, method.ReturnTypeWithAnnotations);
                SetUpdatedSymbol(node, node.Method, method);
            }
        }

        private TypeWithState VisitAndCheckReceiver(BoundExpression? receiverOpt, MethodSymbol method)
        {
            TypeWithState receiverType = default;

            // The receiver for new extension methods will be analyzed as an argument
            if (receiverOpt is { } receiver && !method.IsExtensionBlockMember())
            {
                receiverType = VisitRvalueWithState(receiver);
                CheckCallReceiver(receiver, receiverType, method);
            }

            return receiverType;
        }

        private (MethodSymbol method, ImmutableArray<VisitResult> results, bool returnNotNull) ReInferMethodAndVisitArguments(
            BoundNode node,
            BoundExpression? receiverOpt,
            TypeWithState receiverType,
            MethodSymbol method,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            ImmutableArray<int> argsToParamsOpt,
            BitVector defaultArguments,
            bool expanded,
            bool invokedAsExtensionMethod,
            VisitResult? firstArgumentResult = null)
        {
            bool adjustForExtensionBlockMember = method.IsExtensionBlockMember();

            refKindsOpt = GetArgumentRefKinds(refKindsOpt, adjustForExtensionBlockMember, method, arguments.Length);

            if (!method.IsExtensionBlockMember() && !receiverType.HasNullType)
            {
                // Update method based on inferred receiver type.
                method = (MethodSymbol)AsMemberOfType(receiverType.Type, method);
            }

            arguments = getArguments(arguments, adjustForExtensionBlockMember, receiverOpt);
            ImmutableArray<ParameterSymbol> parameters = getParameters(method.Parameters, adjustForExtensionBlockMember, method);
            argsToParamsOpt = GetArgsToParamsOpt(argsToParamsOpt, adjustForExtensionBlockMember);

            ImmutableArray<VisitResult> results;
            bool returnNotNull;
            (var newMethod, results, returnNotNull) = VisitArguments(node, arguments, refKindsOpt, parameters, argsToParamsOpt, defaultArguments,
                expanded, invokedAsExtensionMethod, method, firstArgumentResult: firstArgumentResult);

            Debug.Assert(newMethod is not null);
            ApplyMemberPostConditions(receiverOpt, newMethod);

            return (newMethod, results, returnNotNull);

            static ImmutableArray<BoundExpression> getArguments(ImmutableArray<BoundExpression> arguments, bool isExtensionBlockMethod, BoundExpression? receiver)
            {
                // In error cases, the receiver may have already been stored as an argument
                if (isExtensionBlockMethod && receiver is not null)
                {
                    return [receiver, .. arguments];
                }

                return arguments;
            }

            static ImmutableArray<ParameterSymbol> getParameters(ImmutableArray<ParameterSymbol> parameters, bool isExtensionBlockMethod, MethodSymbol method)
            {
                if (!isExtensionBlockMethod)
                {
                    return parameters;
                }

                ParameterSymbol? extensionParameter = method.ContainingType.ExtensionParameter;
                Debug.Assert(extensionParameter is not null);

                return [extensionParameter, .. parameters];
            }
        }

        internal static ImmutableArray<RefKind> GetArgumentRefKinds(ImmutableArray<RefKind> argumentRefKindsOpt, bool adjustForExtensionBlockMethod,
            MethodSymbol method, int argumentCount)
        {
            if (!adjustForExtensionBlockMethod)
            {
                return argumentRefKindsOpt;
            }

            Debug.Assert(method.IsExtensionBlockMember());
            RefKind receiverRefKind = GetExtensionReceiverRefKind(method);

            if (argumentRefKindsOpt.IsDefault)
            {
                if (receiverRefKind == RefKind.None)
                {
                    return argumentRefKindsOpt;
                }

                var builder = ArrayBuilder<RefKind>.GetInstance(argumentCount + 1, fillWithValue: RefKind.None);
                builder[0] = receiverRefKind;
                return builder.ToImmutableAndFree();
            }

            return [receiverRefKind, .. argumentRefKindsOpt];
        }

        private static RefKind GetExtensionReceiverRefKind(MethodSymbol method)
        {
            ParameterSymbol? extensionParameter = method.ContainingType.ExtensionParameter;
            Debug.Assert(extensionParameter is not null);
            // See "OverloadResolution.IsApplicable": we only give an implicit `ref` on the receiver for a `ref` parameter
            // For `ref readonly` or `in`, we use "none"
            // `out` is a declaration error
            return extensionParameter.RefKind == RefKind.Ref ? RefKind.Ref : RefKind.None;
        }

        private static ImmutableArray<int> GetArgsToParamsOpt(ImmutableArray<int> argsToParamsOpt, bool isExtensionBlockMethod)
        {
            if (!isExtensionBlockMethod)
            {
                return argsToParamsOpt;
            }

            if (argsToParamsOpt.IsDefault)
            {
                return argsToParamsOpt;
            }

            var builder = ArrayBuilder<int>.GetInstance(argsToParamsOpt.Length + 1);
            builder.Add(0);
            for (int i = 0; i < argsToParamsOpt.Length; i++)
            {
                builder.Add(argsToParamsOpt[i] + 1);
            }

            return builder.ToImmutableAndFree();
        }

        private void LearnFromEqualsMethod(MethodSymbol method, BoundCall node, TypeWithState receiverType, ImmutableArray<VisitResult> results)
        {
            // easy out
            var parameterCount = method.ParameterCount;
            var arguments = node.Arguments;
            if (node.HasErrors
                || (parameterCount != 1 && parameterCount != 2)
                || parameterCount != arguments.Length
                || method.MethodKind != MethodKind.Ordinary
                || method.ReturnType.SpecialType != SpecialType.System_Boolean
                || (method.Name != SpecialMembers.GetDescriptor(SpecialMember.System_Object__Equals).Name
                    && method.Name != SpecialMembers.GetDescriptor(SpecialMember.System_Object__ReferenceEquals).Name
                    && !anyOverriddenMethodHasExplicitImplementation(method)))
            {
                return;
            }

            var isStaticEqualsMethod = method.Equals(compilation.GetSpecialTypeMember(SpecialMember.System_Object__EqualsObjectObject))
                    || method.Equals(compilation.GetSpecialTypeMember(SpecialMember.System_Object__ReferenceEquals));
            if (isStaticEqualsMethod ||
                isWellKnownEqualityMethodOrImplementation(compilation, method, receiverType.Type, WellKnownMember.System_Collections_Generic_IEqualityComparer_T__Equals))
            {
                Debug.Assert(arguments.Length == 2);
                learnFromEqualsMethodArguments(arguments[0], results[0].RValueType, arguments[1], results[1].RValueType);
                return;
            }

            var isObjectEqualsMethodOrOverride = method.GetLeastOverriddenMethod(accessingTypeOpt: null)
                .Equals(compilation.GetSpecialTypeMember(SpecialMember.System_Object__Equals));
            if (node.ReceiverOpt is BoundExpression receiver &&
                    (isObjectEqualsMethodOrOverride ||
                     isWellKnownEqualityMethodOrImplementation(compilation, method, receiverType.Type, WellKnownMember.System_IEquatable_T__Equals)))
            {
                Debug.Assert(arguments.Length == 1);
                learnFromEqualsMethodArguments(receiver, receiverType, arguments[0], results[0].RValueType);
                return;
            }

            static bool anyOverriddenMethodHasExplicitImplementation(MethodSymbol method)
            {
                for (var overriddenMethod = method; overriddenMethod is object; overriddenMethod = overriddenMethod.OverriddenMethod)
                {
                    if (overriddenMethod.IsExplicitInterfaceImplementation)
                    {
                        return true;
                    }
                }

                return false;
            }

            static bool isWellKnownEqualityMethodOrImplementation(CSharpCompilation compilation, MethodSymbol method, TypeSymbol? receiverType, WellKnownMember wellKnownMember)
            {
                var wellKnownMethod = (MethodSymbol?)compilation.GetWellKnownTypeMember(wellKnownMember);
                if (wellKnownMethod is null || receiverType is null)
                {
                    return false;
                }

                var wellKnownType = wellKnownMethod.ContainingType;
                var parameterType = method.Parameters[0].TypeWithAnnotations;
                var constructedType = wellKnownType.Construct(ImmutableArray.Create(parameterType));
                var constructedMethod = wellKnownMethod.AsMember(constructedType);

                // FindImplementationForInterfaceMember doesn't check if this method is itself the interface method we're looking for
                if (constructedMethod.Equals(method))
                {
                    return true;
                }

                // check whether 'method', when called on this receiver, is an implementation of 'constructedMethod'.
                for (var baseType = receiverType; baseType is object && method is object; baseType = baseType.BaseTypeNoUseSiteDiagnostics)
                {
                    var implementationMethod = baseType.FindImplementationForInterfaceMember(constructedMethod);
                    if (implementationMethod is null)
                    {
                        // we know no base type will implement this interface member either
                        return false;
                    }

                    if (implementationMethod.ContainingType.IsInterface)
                    {
                        // this method cannot be called directly from source because an interface can only explicitly implement a method from its base interface.
                        return false;
                    }

                    // could be calling an override of a method that implements the interface method
                    for (var overriddenMethod = method; overriddenMethod is object; overriddenMethod = overriddenMethod.OverriddenMethod)
                    {
                        if (overriddenMethod.Equals(implementationMethod))
                        {
                            return true;
                        }
                    }

                    // the Equals method being called isn't the method that implements the interface method in this type.
                    // it could be a method that implements the interface on a base type, so check again with the base type of 'implementationMethod.ContainingType'

                    // e.g. in this hierarchy:
                    // class A -> B -> C -> D
                    // method virtual B.Equals -> override D.Equals
                    //
                    // we would potentially check:
                    // 1. D.Equals when called on D, then B.Equals when called on D
                    // 2. B.Equals when called on C
                    // 3. B.Equals when called on B
                    // 4. give up when checking A, since B.Equals is not overriding anything in A

                    // we know that implementationMethod.ContainingType is the same type or a base type of 'baseType',
                    // and that the implementation method will be the same between 'baseType' and 'implementationMethod.ContainingType'.
                    // we step through the intermediate bases in order to skip unnecessary override methods.
                    while (!baseType.Equals(implementationMethod.ContainingType) && method is object)
                    {
                        if (baseType.Equals(method.ContainingType))
                        {
                            // since we're about to move on to the base of 'method.ContainingType',
                            // we know the implementation could only be an overridden method of 'method'.
                            method = method.OverriddenMethod;
                        }

                        baseType = baseType.BaseTypeNoUseSiteDiagnostics;
                        // the implementation method must be contained in this 'baseType' or one of its bases.
                        Debug.Assert(baseType is object);
                    }

                    // now 'baseType == implementationMethod.ContainingType', so if 'method' is
                    // contained in that same type we should advance 'method' one more time.
                    if (method is object && baseType.Equals(method.ContainingType))
                    {
                        method = method.OverriddenMethod;
                    }
                }

                return false;
            }

            void learnFromEqualsMethodArguments(BoundExpression left, TypeWithState leftType, BoundExpression right, TypeWithState rightType)
            {
                // comparing anything to a null literal gives maybe-null when true and not-null when false
                // comparing a maybe-null to a not-null gives us not-null when true, nothing learned when false
                if (left.ConstantValueOpt?.IsNull == true)
                {
                    Split();
                    LearnFromNullTest(right, ref StateWhenTrue);
                    LearnFromNonNullTest(right, ref StateWhenFalse);
                }
                else if (right.ConstantValueOpt?.IsNull == true)
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

        private bool IsCompareExchangeMethod(MethodSymbol? method)
        {
            if (method is null)
            {
                return false;
            }

            return method.Equals(compilation.GetWellKnownTypeMember(WellKnownMember.System_Threading_Interlocked__CompareExchange), SymbolEqualityComparer.ConsiderEverything.CompareKind)
                || method.OriginalDefinition.Equals(compilation.GetWellKnownTypeMember(WellKnownMember.System_Threading_Interlocked__CompareExchange_T), SymbolEqualityComparer.ConsiderEverything.CompareKind);
        }

        private readonly struct CompareExchangeInfo
        {
            public readonly ImmutableArray<BoundExpression> Arguments;
            public readonly ImmutableArray<VisitResult> Results;
            public readonly ImmutableArray<int> ArgsToParamsOpt;

            public CompareExchangeInfo(ImmutableArray<BoundExpression> arguments, ImmutableArray<VisitResult> results, ImmutableArray<int> argsToParamsOpt)
            {
                Arguments = arguments;
                Results = results;
                ArgsToParamsOpt = argsToParamsOpt;
            }

            public bool IsDefault => Arguments.IsDefault || Results.IsDefault;
        }

        private NullableFlowState LearnFromCompareExchangeMethod(in CompareExchangeInfo compareExchangeInfo)
        {
            Debug.Assert(!compareExchangeInfo.IsDefault);

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

            if (compareExchangeInfo.Arguments.Length != 3)
            {
                // This can occur if CompareExchange has optional arguments.
                // Since none of the main runtimes have optional arguments,
                // we bail to avoid an exception but don't bother actually calculating the FlowState.
                return NullableFlowState.NotNull;
            }

            var argsToParamsOpt = compareExchangeInfo.ArgsToParamsOpt;
            Debug.Assert(argsToParamsOpt is { IsDefault: true } or { Length: 3 });
            var (comparandIndex, valueIndex, locationIndex) = argsToParamsOpt.IsDefault
                ? (2, 1, 0)
                : (argsToParamsOpt.IndexOf(2), argsToParamsOpt.IndexOf(1), argsToParamsOpt.IndexOf(0));

            var comparand = compareExchangeInfo.Arguments[comparandIndex];
            var valueFlowState = compareExchangeInfo.Results[valueIndex].RValueType.State;
            if (comparand.ConstantValueOpt?.IsNull == true)
            {
                // If location contained a null, then the write `location = value` definitely occurred
            }
            else
            {
                var locationFlowState = compareExchangeInfo.Results[locationIndex].RValueType.State;
                // A write may have occurred
                valueFlowState = valueFlowState.Join(locationFlowState);
            }

            return valueFlowState;
        }

        private void CheckCallReceiver(BoundExpression? receiverOpt, TypeWithState receiverType, MethodSymbol method)
        {
            if (method.IsExtensionBlockMember())
            {
                return;
            }

            // methods which are members of Nullable<T> (ex: ToString, GetHashCode) can be invoked on null receiver.
            // However, inherited methods (ex: GetType) are invoked on a boxed value (since base types are reference types)
            // and therefore in those cases nullable receivers should be checked for nullness.
            bool checkNullableValueType = false;

            var type = receiverType.Type;
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
            _ = CheckPossibleNullReceiver(receiverOpt, receiverType, checkNullableValueType);
        }

        private TypeWithState GetReturnTypeWithState(MethodSymbol method)
        {
            return TypeWithState.Create(method.ReturnTypeWithAnnotations, GetRValueAnnotations(method));
        }

        private FlowAnalysisAnnotations GetRValueAnnotations(Symbol? symbol)
        {
            // Annotations are ignored when binding an attribute to avoid cycles. (Members used
            // in attributes are error scenarios, so missing warnings should not be important.)
            if (IsAnalyzingAttribute)
            {
                return FlowAnalysisAnnotations.None;
            }

            var annotations = symbol.GetFlowAnalysisAnnotations();
            return annotations & (FlowAnalysisAnnotations.MaybeNull | FlowAnalysisAnnotations.NotNull);
        }

        private FlowAnalysisAnnotations GetParameterAnnotations(ParameterSymbol parameter)
        {
            // Annotations are ignored when binding an attribute to avoid cycles. (Members used
            // in attributes are error scenarios, so missing warnings should not be important.)
            if (IsAnalyzingAttribute)
                return FlowAnalysisAnnotations.None;

            var annotations = parameter.FlowAnalysisAnnotations;

            // Conditional annotations are ignored on parameters of non-boolean members.
            if (!parameter.IsExtensionParameter()
                && GetTypeOrReturnType(parameter.ContainingSymbol).SpecialType != SpecialType.System_Boolean)
            {
                // NotNull = NotNullWhenTrue + NotNullWhenFalse
                bool hasNotNullWhenTrue = (annotations & FlowAnalysisAnnotations.NotNull) == FlowAnalysisAnnotations.NotNullWhenTrue;
                bool hasNotNullWhenFalse = (annotations & FlowAnalysisAnnotations.NotNull) == FlowAnalysisAnnotations.NotNullWhenFalse;
                if (hasNotNullWhenTrue ^ hasNotNullWhenFalse)
                {
                    annotations &= ~FlowAnalysisAnnotations.NotNull;
                }

                // MaybeNull = MaybeNullWhenTrue + MaybeNullWhenFalse
                bool hasMaybeNullWhenTrue = (annotations & FlowAnalysisAnnotations.MaybeNull) == FlowAnalysisAnnotations.MaybeNullWhenTrue;
                bool hasMaybeNullWhenFalse = (annotations & FlowAnalysisAnnotations.MaybeNull) == FlowAnalysisAnnotations.MaybeNullWhenFalse;
                if (hasMaybeNullWhenTrue ^ hasMaybeNullWhenFalse)
                {
                    annotations &= ~FlowAnalysisAnnotations.MaybeNull;
                }
            }

            return annotations;
        }

        /// <summary>
        /// Fix a TypeWithAnnotations based on Allow/DisallowNull annotations prior to a conversion or assignment.
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
            if ((annotations & FlowAnalysisAnnotations.NotNull) == FlowAnalysisAnnotations.NotNull)
            {
                return TypeWithState.Create(typeWithState.Type, NullableFlowState.NotNull);
            }

            if ((annotations & FlowAnalysisAnnotations.MaybeNull) == FlowAnalysisAnnotations.MaybeNull)
            {
                return TypeWithState.Create(typeWithState.Type, NullableFlowState.MaybeDefault);
            }

            return typeWithState;
        }

        private static TypeWithAnnotations ApplyUnconditionalAnnotations(TypeWithAnnotations declaredType, FlowAnalysisAnnotations annotations)
        {
            if ((annotations & FlowAnalysisAnnotations.MaybeNull) == FlowAnalysisAnnotations.MaybeNull)
            {
                return declaredType.AsAnnotated();
            }

            if ((annotations & FlowAnalysisAnnotations.NotNull) == FlowAnalysisAnnotations.NotNull)
            {
                return declaredType.AsNotAnnotated();
            }

            return declaredType;
        }

        // https://github.com/dotnet/roslyn/issues/29863 Record in the node whether type
        // arguments were implicit, to allow for cases where the syntax is not an
        // invocation (such as a synthesized call from a query interpretation).
        private static bool HasImplicitTypeArguments(BoundNode node)
        {
            if (node is BoundCollectionElementInitializer
                or BoundForEachStatement
                or BoundPropertyAccess
                or BoundIncrementOperator
                or BoundCompoundAssignmentOperator
                or BoundDagPropertyEvaluation)
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

        protected override void VisitArguments(ImmutableArray<BoundExpression> arguments, ImmutableArray<RefKind> refKindsOpt, MethodSymbol method, ImmutableArray<int> argsToParamsOpt, bool expanded)
        {
            // Callers should be using VisitArguments overload below.
            throw ExceptionUtilities.Unreachable();
        }

        private (MethodSymbol? method, ImmutableArray<VisitResult> results, bool returnNotNull) VisitArguments(
            BoundExpression node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            MethodSymbol? method,
            ImmutableArray<int> argsToParamsOpt,
            BitVector defaultArguments,
            bool expanded,
            bool invokedAsExtensionMethod)
        {
            return VisitArguments(node, arguments, refKindsOpt, method is null ? default : method.Parameters, argsToParamsOpt, defaultArguments, expanded, invokedAsExtensionMethod, method);
        }

        private ImmutableArray<VisitResult> VisitArguments(
            BoundExpression node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            PropertySymbol? property,
            ImmutableArray<int> argsToParamsOpt,
            BitVector defaultArguments,
            bool expanded)
        {
            return VisitArguments<PropertySymbol>(node, arguments, refKindsOpt, parametersOpt: property is null ? default : property.Parameters, argsToParamsOpt, defaultArguments, expanded, invokedAsExtensionMethod: false).results;
        }

        // Returns the re-inferred member and the results of the arguments.
        private (TMember? member, ImmutableArray<VisitResult> results, bool returnNotNull) VisitArguments<TMember>(
            BoundNode node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            ImmutableArray<ParameterSymbol> parametersOpt,
            ImmutableArray<int> argsToParamsOpt,
            BitVector defaultArguments,
            bool expanded,
            bool invokedAsExtensionMethod,
            TMember? member = null,
            VisitResult? firstArgumentResult = null)
            where TMember : Symbol
        {
            var result = VisitArguments(node, arguments, refKindsOpt, parametersOpt, argsToParamsOpt, defaultArguments, expanded, invokedAsExtensionMethod, member, delayCompletionForTargetMember: false, firstArgumentResult: firstArgumentResult);
            Debug.Assert(result.completion is null);

            return (result.member, result.results, result.returnNotNull);
        }

        private delegate (TMember? member, bool returnNotNull) ArgumentsCompletionDelegate<TMember>(ImmutableArray<VisitResult> argumentResults, ImmutableArray<ParameterSymbol> parametersOpt, TMember? member) where TMember : Symbol;

        private (TMember? member, ImmutableArray<VisitResult> results, bool returnNotNull, ArgumentsCompletionDelegate<TMember>? completion)
        VisitArguments<TMember>(
            BoundNode node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            ImmutableArray<ParameterSymbol> parametersOpt,
            ImmutableArray<int> argsToParamsOpt,
            BitVector defaultArguments,
            bool expanded,
            bool invokedAsExtensionMethod,
            TMember? member,
            bool delayCompletionForTargetMember,
            VisitResult? firstArgumentResult = null)
            where TMember : Symbol
        {
            Debug.Assert(!arguments.IsDefault);
            Debug.Assert(!expanded || !parametersOpt.IsDefault);
            Debug.Assert(refKindsOpt.IsDefaultOrEmpty || refKindsOpt.Length == arguments.Length);
            Debug.Assert(argsToParamsOpt.IsDefault || argsToParamsOpt.Length == arguments.Length);

            if (expanded)
            {
                expandParamsCollection(ref arguments, ref refKindsOpt, parametersOpt, ref argsToParamsOpt, ref defaultArguments);
            }
            else
            {
                Debug.Assert(!arguments.Any(a => a.IsParamsArrayOrCollection));
            }

            (ImmutableArray<BoundExpression> argumentsNoConversions, ImmutableArray<Conversion> conversions) = RemoveArgumentConversions(arguments, refKindsOpt);

            // Visit the arguments and collect results
            ImmutableArray<VisitResult> results = VisitArgumentsEvaluate(argumentsNoConversions, refKindsOpt, GetParametersAnnotations(arguments, parametersOpt, argsToParamsOpt, expanded), defaultArguments, firstArgumentResult: firstArgumentResult);

            return visitArguments(
                node, arguments, argumentsNoConversions, conversions, results, refKindsOpt,
                parametersOpt, argsToParamsOpt, defaultArguments, expanded, invokedAsExtensionMethod,
                member, delayCompletionForTargetMember);

            (TMember? member, ImmutableArray<VisitResult> results, bool returnNotNull, ArgumentsCompletionDelegate<TMember>? completion)
            visitArguments(
                BoundNode node,
                ImmutableArray<BoundExpression> arguments,
                ImmutableArray<BoundExpression> argumentsNoConversions,
                ImmutableArray<Conversion> conversions,
                ImmutableArray<VisitResult> results,
                ImmutableArray<RefKind> refKindsOpt,
                ImmutableArray<ParameterSymbol> parametersOpt,
                ImmutableArray<int> argsToParamsOpt,
                BitVector defaultArguments,
                bool expanded,
                bool invokedAsExtensionMethod,
                TMember? member,
                bool delayCompletionForTargetMember)
            {
                bool shouldReturnNotNull = false;

                if (delayCompletionForTargetMember)
                {
                    return (member, results, shouldReturnNotNull, visitArgumentsAsContinuation(
                                                                      node, arguments, argumentsNoConversions, conversions, refKindsOpt,
                                                                      argsToParamsOpt, defaultArguments, expanded, invokedAsExtensionMethod));
                }

                // Re-infer method type arguments
                if (member?.GetMemberArityIncludingExtension() > 0)
                {
                    if (HasImplicitTypeArguments(node))
                    {
                        member = InferMemberTypeArguments(member, GetArgumentsForMethodTypeInference(results, argumentsNoConversions), refKindsOpt, argsToParamsOpt, expanded);
                        parametersOpt = member.GetParametersIncludingExtensionParameter(skipExtensionIfStatic: false);
                    }

                    var syntaxForConstraintCheck = node.Syntax switch
                    {
                        InvocationExpressionSyntax { Expression: var expression } => expression,
                        ForEachStatementSyntax { Expression: var expression } => expression,
                        _ => node.Syntax
                    };

                    if (member is MethodSymbol reinferredMethod)
                    {
                        if (ConstraintsHelper.RequiresChecking(reinferredMethod))
                        {
                            CheckMethodConstraints(syntaxForConstraintCheck, reinferredMethod);
                        }
                    }
                    else if (member.IsExtensionBlockMember())
                    {
                        if (member.ContainingType is { } extension && ConstraintsHelper.RequiresChecking(extension))
                        {
                            CheckExtensionConstraints(syntaxForConstraintCheck, extension);
                        }
                    }
                    else
                    {
                        throw ExceptionUtilities.UnexpectedValue(member);
                    }
                }

                var method = member as MethodSymbol;
                bool parameterHasNotNullIfNotNull = !IsAnalyzingAttribute && !parametersOpt.IsDefault && parametersOpt.Any(static p => !p.NotNullIfParameterNotNull.IsEmpty);
                var notNullParametersBuilder = parameterHasNotNullIfNotNull ? ArrayBuilder<ParameterSymbol>.GetInstance() : null;
                var conversionResultsBuilder = ArrayBuilder<VisitResult>.GetInstance(results.Length);
                if (!parametersOpt.IsDefault)
                {
                    // Visit conversions, inbound assignments including pre-conditions
                    TypeWithAnnotations paramsIterationType = default;
                    ImmutableHashSet<string>? returnNotNullIfParameterNotNull = IsAnalyzingAttribute ? null : method?.ReturnNotNullIfParameterNotNull;
                    for (int i = 0; i < results.Length; i++)
                    {
                        var argumentNoConversion = argumentsNoConversions[i];
                        var argument = i < arguments.Length ? arguments[i] : argumentNoConversion;

                        (ParameterSymbol? parameter, TypeWithAnnotations parameterType, FlowAnalysisAnnotations parameterAnnotations, bool isExpandedParamsArgument) =
                            GetCorrespondingParameter(i, parametersOpt, argsToParamsOpt, expanded, ref paramsIterationType);

                        // This is known to happen for certain error scenarios, because
                        // the parameter matching logic above is not as flexible as the one we use in `Binder.BuildArgumentsForErrorRecovery`
                        // so we may end up with a pending conversion completion for an argument apparently without a corresponding parameter.
                        if (parameter is null)
                        {
                            if (tryShortCircuitTargetTypedExpression(argument, argumentNoConversion))
                            {
                                Debug.Assert(method is ErrorMethodSymbol);
                            }
                            continue;
                        }

                        // In error recovery with named arguments, target-typing cannot work as we can get a different parameter type
                        // from our GetCorrespondingParameter logic than Binder.BuildArgumentsForErrorRecovery does.
                        if (node is BoundCall { HasErrors: true, ArgumentNamesOpt.IsDefaultOrEmpty: false, ArgsToParamsOpt.IsDefault: true } &&
                            tryShortCircuitTargetTypedExpression(argument, argumentNoConversion))
                        {
                            continue;
                        }

                        // We disable diagnostics when:
                        // 1. the containing call has errors (to reduce cascading diagnostics)
                        // 2. on implicit default arguments (since that's really only an issue with the declaration)
                        var previousDisableDiagnostics = _disableDiagnostics;
                        _disableDiagnostics |= node.HasErrors || defaultArguments[i];

                        VisitArgumentConversionAndInboundAssignmentsAndPreConditions(
                            GetConversionIfApplicable(argument, argumentNoConversion),
                            argumentNoConversion,
                            conversions.IsDefault || i >= conversions.Length ? Conversion.Identity : conversions[i],
                            GetRefKind(refKindsOpt, i),
                            parameter,
                            parameterType,
                            parameterAnnotations,
                            results[i],
                            conversionResultsBuilder,
                            invokedAsExtensionMethod && i == 0);

                        _disableDiagnostics = previousDisableDiagnostics;

                        bool isStaticExtensionReceiver = member?.IsExtensionBlockMember() == true && member.IsStatic && i == 0;
                        if (!isStaticExtensionReceiver)
                        {
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
                }

                conversionResultsBuilder.Free();

                if (node is BoundCall { Method: { OriginalDefinition: LocalFunctionSymbol localFunction } })
                {
                    VisitLocalFunctionUse(localFunction);
                }

                if (!node.HasErrors && !parametersOpt.IsDefault)
                {
                    // For CompareExchange method we need more context to determine the state of outbound assignment
                    CompareExchangeInfo compareExchangeInfo = IsCompareExchangeMethod(method) ? new CompareExchangeInfo(arguments, results, argsToParamsOpt) : default;
                    TypeWithAnnotations paramsIterationType = default;

                    // Visit outbound assignments and post-conditions
                    // Note: the state may get split in this step
                    for (int i = 0; i < arguments.Length; i++)
                    {
                        (ParameterSymbol? parameter, TypeWithAnnotations parameterType, FlowAnalysisAnnotations parameterAnnotations, _) =
                            GetCorrespondingParameter(i, parametersOpt, argsToParamsOpt, expanded, ref paramsIterationType);
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
                            notNullParametersBuilder,
                            (!compareExchangeInfo.IsDefault && parameter.Ordinal == 0) ? compareExchangeInfo : default);
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

                if (!IsAnalyzingAttribute && method is not null && (method.FlowAnalysisAnnotations & FlowAnalysisAnnotations.DoesNotReturn) == FlowAnalysisAnnotations.DoesNotReturn)
                {
                    SetUnreachable();
                }

                notNullParametersBuilder?.Free();
                return (member, results, shouldReturnNotNull, null);
            }

            ArgumentsCompletionDelegate<TMember> visitArgumentsAsContinuation(
                BoundNode node,
                ImmutableArray<BoundExpression> arguments,
                ImmutableArray<BoundExpression> argumentsNoConversions,
                ImmutableArray<Conversion> conversions,
                ImmutableArray<RefKind> refKindsOpt,
                ImmutableArray<int> argsToParamsOpt,
                BitVector defaultArguments,
                bool expanded,
                bool invokedAsExtensionMethod)
            {
                return (ImmutableArray<VisitResult> results, ImmutableArray<ParameterSymbol> parametersOpt, TMember? member) =>
                       {
                           var result = visitArguments(
                                           node, arguments, argumentsNoConversions, conversions, results, refKindsOpt,
                                           parametersOpt, argsToParamsOpt, defaultArguments, expanded, invokedAsExtensionMethod,
                                           member, delayCompletionForTargetMember: false);
                           Debug.Assert(result.completion is null);

                           return (result.member, result.returnNotNull);
                       };
            }

            static void expandParamsCollection(ref ImmutableArray<BoundExpression> arguments, ref ImmutableArray<RefKind> refKindsOpt, ImmutableArray<ParameterSymbol> parametersOpt, ref ImmutableArray<int> argsToParamsOpt, ref BitVector defaultArguments)
            {
                // It looks like in some error scenarios we can get here without params array created.
                // At the moment, there is only one test that gets here like that - Microsoft.CodeAnalysis.CSharp.UnitTests.AttributeTests.TestBadParamsCtor.
                // And we get here for the erroneous attribute application, constructor is inaccessible. 
                // Perhaps that shouldn't cancel the default values / params array processing?
                Debug.Assert(arguments.Count(a => a.IsParamsArrayOrCollection) <= 1);

                for (int a = 0; a < arguments.Length; ++a)
                {
                    BoundExpression argument = arguments[a];
                    if (argument.IsParamsArrayOrCollection)
                    {
                        Debug.Assert(parametersOpt.IsDefault || arguments.Length == parametersOpt.Length);
                        ImmutableArray<BoundExpression> elements;

                        if (argument is BoundArrayCreation array)
                        {
                            elements = array.InitializerOpt!.Initializers;
                        }
                        else
                        {
                            elements = ((BoundCollectionExpression)((BoundConversion)argument).Operand).UnconvertedCollectionExpression.Elements.CastArray<BoundExpression>();
                        }

                        if (elements.Length == 0)
                        {
                            arguments = arguments.RemoveAt(a);

                            if (!argsToParamsOpt.IsDefault)
                            {
                                argsToParamsOpt = argsToParamsOpt.RemoveAt(a);
                            }

                            if (!refKindsOpt.IsDefaultOrEmpty)
                            {
                                refKindsOpt = refKindsOpt.RemoveAt(a);
                            }
                        }
                        else
                        {
                            Debug.Assert(defaultArguments.IsNull || elements.Length == 1);
                            Debug.Assert(elements.Length == 1 || a == arguments.Length - 1);
                            var argumentsBuilder = ArrayBuilder<BoundExpression>.GetInstance(arguments.Length + elements.Length - 1);
                            argumentsBuilder.AddRange(arguments, a);
                            argumentsBuilder.AddRange(elements);
                            argumentsBuilder.AddRange(arguments, a + 1, arguments.Length - (a + 1));
                            Debug.Assert(argumentsBuilder.Count == arguments.Length + elements.Length - 1);

                            if (!argsToParamsOpt.IsDefault)
                            {
                                var argsToParamsBuilder = ArrayBuilder<int>.GetInstance(argsToParamsOpt.Length + elements.Length - 1);
                                argsToParamsBuilder.AddRange(argsToParamsOpt, a);
                                argsToParamsBuilder.AddMany(arguments.Length - 1, elements.Length);
                                argsToParamsBuilder.AddRange(argsToParamsOpt, a + 1, argsToParamsOpt.Length - (a + 1));
                                argsToParamsOpt = argsToParamsBuilder.ToImmutableAndFree();
                            }

                            if (!refKindsOpt.IsDefaultOrEmpty)
                            {
                                var refKindsBuilder = ArrayBuilder<RefKind>.GetInstance(refKindsOpt.Length + elements.Length - 1);
                                refKindsBuilder.AddRange(refKindsOpt, a);
                                refKindsBuilder.AddMany(RefKind.None, elements.Length);
                                refKindsBuilder.AddRange(refKindsOpt, a + 1, refKindsOpt.Length - (a + 1));
                                refKindsOpt = refKindsBuilder.ToImmutableAndFree();
                            }

                            arguments = argumentsBuilder.ToImmutableAndFree();
                        }

                        break;
                    }
                }
            }

            bool tryShortCircuitTargetTypedExpression(BoundExpression argument, BoundExpression argumentNoConversion)
            {
                if (IsTargetTypedExpression(argumentNoConversion) && _targetTypedAnalysisCompletionOpt?.TryGetValue(argumentNoConversion, out var completion) is true)
                {
                    // We've done something wrong if we have a target-typed expression and registered an analysis continuation for it
                    // (we won't be able to complete that continuation)
                    // We flush the completion with a plausible/dummy type and remove it.
                    completion(TypeWithAnnotations.Create(argument.Type));
                    TargetTypedAnalysisCompletion.Remove(argumentNoConversion);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Applies the member postconditions of <paramref name="method"/> to members of <paramref name="receiverOpt"/> or to the appropriate static members.
        /// Used for the "use-site" analysis of MemberNotNullAttributes.
        /// </summary>
        private void ApplyMemberPostConditions(BoundExpression? receiverOpt, MethodSymbol? method)
        {
            if (method is null)
            {
                return;
            }

            int receiverSlot = receiverOpt is not null && !method.IsStatic
                ? MakeSlot(receiverOpt)
                : GetReceiverSlotForMemberPostConditions(method);

            if (receiverSlot < 0)
            {
                return;
            }

            ApplyMemberPostConditions(receiverSlot, method);
        }

        /// <summary>
        /// Returns -1 when a null method is passed. In this case there are definitely no member postconditions to apply.
        /// Returns the slot for the `this` parameter for an instance method, or a non-static local function in an instance method.
        /// Otherwise, returns 0, because postconditions applying to static members of the containing type could be present.
        /// </summary>
        private int GetReceiverSlotForMemberPostConditions(MethodSymbol? method)
        {
            if (method is null)
            {
                return -1;
            }

            if (method.IsStatic)
            {
                return 0;
            }

            MethodSymbol? current = method;
            while (current.ContainingSymbol is MethodSymbol container)
            {
                current = container;
                if (container.IsStatic)
                {
                    return 0;
                }
            }

            if (current.TryGetThisParameter(out var thisParameter) && thisParameter is not null)
            {
                return GetOrCreateSlot(thisParameter);
            }

            return 0;
        }

        private void ApplyMemberPostConditions(int receiverSlot, MethodSymbol method)
        {
            Debug.Assert(receiverSlot >= 0);
            if (method.IsExtensionBlockMember())
            {
                // Tracked by https://github.com/dotnet/roslyn/issues/78828 : should we extend member post-conditions to work with extension members?
                return;
            }

            do
            {
                var type = method.ContainingType;
                var notNullMembers = method.NotNullMembers;
                var notNullWhenTrueMembers = method.NotNullWhenTrueMembers;
                var notNullWhenFalseMembers = method.NotNullWhenFalseMembers;

                if (IsConditionalState)
                {
                    applyMemberPostConditions(receiverSlot, type, notNullMembers, ref StateWhenTrue);
                    applyMemberPostConditions(receiverSlot, type, notNullMembers, ref StateWhenFalse);
                }
                else
                {
                    applyMemberPostConditions(receiverSlot, type, notNullMembers, ref State);
                }

                if (method.ReturnType.SpecialType == SpecialType.System_Boolean
                    && !(notNullWhenTrueMembers.IsEmpty && notNullWhenFalseMembers.IsEmpty))
                {
                    Split();
                    applyMemberPostConditions(receiverSlot, type, notNullWhenTrueMembers, ref StateWhenTrue);
                    applyMemberPostConditions(receiverSlot, type, notNullWhenFalseMembers, ref StateWhenFalse);
                }

                method = method.OverriddenMethod;
            }
            while (method != null);

            void applyMemberPostConditions(int receiverSlot, TypeSymbol type, ImmutableArray<string> members, ref LocalState state)
            {
                if (members.IsEmpty)
                {
                    return;
                }

                foreach (var memberName in members)
                {
                    markMembersAsNotNull(receiverSlot, type, memberName, ref state);
                }
            }

            void markMembersAsNotNull(int receiverSlot, TypeSymbol type, string memberName, ref LocalState state)
            {
                foreach (Symbol member in type.GetMembers(memberName))
                {
                    if (member.IsStatic)
                    {
                        // Trying to access a static member from a non-static context
                        receiverSlot = 0;
                    }
                    else if (receiverSlot == 0)
                    {
                        // Trying to access an instance member from a static context
                        continue;
                    }

                    switch (member.Kind)
                    {
                        case SymbolKind.Field:
                        case SymbolKind.Property:
                            if (GetOrCreateSlot(member, receiverSlot) is int memberSlot &&
                                memberSlot > 0)
                            {
                                SetState(ref state, memberSlot, NullableFlowState.NotNull);
                            }
                            break;
                        case SymbolKind.Event:
                        case SymbolKind.Method:
                            break;
                    }
                }
            }
        }

        private ImmutableArray<VisitResult> VisitArgumentsEvaluate(
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            ImmutableArray<FlowAnalysisAnnotations> parameterAnnotationsOpt,
            BitVector defaultArguments,
            VisitResult? firstArgumentResult = null)
        {
            Debug.Assert(!IsConditionalState);
            int n = arguments.Length;
            if (n == 0 && parameterAnnotationsOpt.IsDefaultOrEmpty)
            {
                return ImmutableArray<VisitResult>.Empty;
            }

            var resultsBuilder = ArrayBuilder<VisitResult>.GetInstance(n);
            var previousDisableDiagnostics = _disableDiagnostics;
            for (int i = 0; i < n; i++)
            {
                // we disable nullable warnings on default arguments
                _disableDiagnostics = defaultArguments[i] || previousDisableDiagnostics;
                if (i == 0 && firstArgumentResult is { } result)
                {
                    resultsBuilder.Add(result);
                }
                else
                {
                    resultsBuilder.Add(VisitArgumentEvaluate(arguments[i], GetRefKind(refKindsOpt, i), parameterAnnotationsOpt.IsDefault ? default : parameterAnnotationsOpt[i]));
                }
            }
            _disableDiagnostics = previousDisableDiagnostics;

            SetInvalidResult();
            return resultsBuilder.ToImmutableAndFree();
        }

        private ImmutableArray<FlowAnalysisAnnotations> GetParametersAnnotations(ImmutableArray<BoundExpression> arguments, ImmutableArray<ParameterSymbol> parametersOpt, ImmutableArray<int> argsToParamsOpt, bool expanded)
        {
            ImmutableArray<FlowAnalysisAnnotations> parameterAnnotationsOpt = default;

            if (!parametersOpt.IsDefault)
            {
                if (expanded)
                {
                    TypeWithAnnotations paramsIterationType = default;
                    parameterAnnotationsOpt = arguments.SelectAsArray(
                        (argument, i, arg) => arg.self.GetCorrespondingParameter(i, arg.parametersOpt, arg.argsToParamsOpt, expanded: true, ref paramsIterationType).Annotations,
                        (self: this, parametersOpt, argsToParamsOpt));
                }
                else
                {
                    parameterAnnotationsOpt = arguments.SelectAsArray(
                        static (argument, i, arg) =>
                        {
                            TypeWithAnnotations paramsIterationType = default;
                            return arg.self.GetCorrespondingParameter(i, arg.parametersOpt, arg.argsToParamsOpt, expanded: false, ref paramsIterationType).Annotations;
                        },
                        (self: this, parametersOpt, argsToParamsOpt));
                }
            }

            return parameterAnnotationsOpt;
        }

        private VisitResult VisitArgumentEvaluate(BoundExpression argument, RefKind refKind, FlowAnalysisAnnotations annotations)
        {
            Visit(argument);
            return VisitArgumentEvaluateEpilogue(argument, refKind, annotations);
        }

        private bool VisitArgumentEvaluateNeedsCloningState(BoundExpression argument)
        {
            Debug.Assert(!IsConditionalState);
            return (argument.Kind == BoundKind.Lambda);
        }

        private VisitResult VisitArgumentEvaluateEpilogue(BoundExpression argument, RefKind refKind, FlowAnalysisAnnotations annotations)
        {
            // Note: DoesNotReturnIf is ineffective on ref/out parameters

            switch (refKind)
            {
                case RefKind.Ref:
                    Unsplit();
                    break;
                case RefKind.None:
                case RefKind.In:
                    switch (annotations & (FlowAnalysisAnnotations.DoesNotReturnIfTrue | FlowAnalysisAnnotations.DoesNotReturnIfFalse))
                    {
                        case FlowAnalysisAnnotations.DoesNotReturnIfTrue:
                            if (IsConditionalState)
                            {
                                SetState(StateWhenFalse);
                            }
                            break;

                        case FlowAnalysisAnnotations.DoesNotReturnIfFalse:
                            if (IsConditionalState)
                            {
                                SetState(StateWhenTrue);
                            }
                            break;

                        default:
                            VisitRvalueEpilogue(argument);
                            break;
                    }
                    break;
                case RefKind.Out:
                    // As far as we can tell, there is no scenario relevant to nullability analysis
                    // where splitting an L-value (for instance with a ref conditional) would affect the result.
                    Unsplit();

                    // We'll want to use the l-value type, rather than the result type, for method re-inference
                    UseLvalueOnly(argument);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(refKind);
            }

            Debug.Assert(!IsConditionalState);
            return _visitResult;
        }

        /// <summary>
        /// Verifies that an argument's nullability is compatible with its parameter's on the way in.
        /// </summary>
        private void VisitArgumentConversionAndInboundAssignmentsAndPreConditions(
            BoundConversion? conversionOpt,
            BoundExpression argumentNoConversion,
            Conversion conversion,
            RefKind refKind,
            ParameterSymbol parameter,
            TypeWithAnnotations parameterType,
            FlowAnalysisAnnotations parameterAnnotations,
            VisitResult result,
            ArrayBuilder<VisitResult>? conversionResultsBuilder,
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
                        if (conversion is { IsValid: true, Kind: ConversionKind.ImplicitUserDefined })
                        {
                            var argumentResultType = resultType.Type;
                            conversion = GenerateConversion(_conversions, argumentNoConversion, argumentResultType, parameterType.Type, fromExplicitCast: false, extensionMethodThisArgument: false, isChecked: conversionOpt?.Checked ?? false);
                            if (!conversion.Exists && !argumentNoConversion.IsSuppressed)
                            {
                                Debug.Assert(argumentResultType is not null);
                                ReportNullabilityMismatchInArgument(argumentNoConversion.Syntax, argumentResultType, parameter, parameterType.Type, forOutput: false);
                            }
                        }

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
                            stateForLambda: result.StateForLambda,
                            previousArgumentConversionResults: conversionResultsBuilder);

                        // If the parameter has annotations, we perform an additional check for nullable value types
                        if (CheckDisallowedNullAssignment(stateAfterConversion, parameterAnnotations, argumentNoConversion.Syntax))
                        {
                            LearnFromNonNullTest(argumentNoConversion, ref State);
                        }
                        SetResultType(argumentNoConversion, stateAfterConversion, updateAnalyzedNullability: false);
                        conversionResultsBuilder?.Add(_visitResult);
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
                            CheckDisallowedNullAssignment(resultType, parameterAnnotations, argumentNoConversion.Syntax);
                        }
                    }

                    conversionResultsBuilder?.Add(result);
                    break;
                case RefKind.Out:
                    conversionResultsBuilder?.Add(result);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(refKind);
            }

            Debug.Assert(!this.IsConditionalState);
        }

        /// <summary>Returns <see langword="true"/> if this is an assignment forbidden by DisallowNullAttribute, otherwise <see langword="false"/>.</summary>
        private bool CheckDisallowedNullAssignment(TypeWithState state, FlowAnalysisAnnotations annotations, SyntaxNode node, BoundExpression? boundValueOpt = null)
        {
            if (boundValueOpt is { WasCompilerGenerated: true })
            {
                // We need to skip `return backingField;` in auto-prop getters
                return false;
            }

            // We do this extra check for types whose non-nullable version cannot be represented
            if (IsDisallowedNullAssignment(state, annotations))
            {
                ReportDiagnostic(ErrorCode.WRN_DisallowNullAttributeForbidsMaybeNullAssignment, node.Location);
                return true;
            }

            return false;
        }

        private static bool IsDisallowedNullAssignment(TypeWithState valueState, FlowAnalysisAnnotations targetAnnotations)
        {
            return ((targetAnnotations & FlowAnalysisAnnotations.DisallowNull) != 0) &&
                hasNoNonNullableCounterpart(valueState.Type) &&
                valueState.MayBeNull;

            static bool hasNoNonNullableCounterpart(TypeSymbol? type)
            {
                if (type is null)
                {
                    return false;
                }

                // Some types that could receive a maybe-null value have a NotNull counterpart:
                // [NotNull]string? -> string
                // [NotNull]string -> string
                // [NotNull]TClass -> TClass
                // [NotNull]TClass? -> TClass
                //
                // While others don't:
                // [NotNull]int? -> X
                // [NotNull]TNullable -> X
                // [NotNull]TStruct? -> X
                // [NotNull]TOpen -> X
                return (type.Kind == SymbolKind.TypeParameter && !type.IsReferenceType) || type.IsNullableTypeOrTypeParameter();
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
            VisitResult result,
            ArrayBuilder<ParameterSymbol>? notNullParametersOpt,
            CompareExchangeInfo compareExchangeInfoOpt)
        {
            // Note: the state may be conditional if a previous argument involved a conditional post-condition
            // The WhenTrue/False states correspond to the invocation returning true/false

            switch (refKind)
            {
                case RefKind.None:
                case RefKind.In:
                    {
                        // learn from post-conditions [Maybe/NotNull, Maybe/NotNullWhen] without using an assignment
                        LearnFromPostConditions(argument, parameterAnnotations);
                    }
                    break;
                case RefKind.Ref:
                    {
                        // assign from a fictional value from the parameter to the argument.
                        parameterAnnotations = notNullBasedOnParameters(parameterAnnotations, notNullParametersOpt, parameter);
                        var parameterWithState = TypeWithState.Create(parameterType, parameterAnnotations);
                        if (!compareExchangeInfoOpt.IsDefault)
                        {
                            var adjustedState = LearnFromCompareExchangeMethod(in compareExchangeInfoOpt);
                            parameterWithState = TypeWithState.Create(parameterType.Type, adjustedState);
                        }

                        var parameterValue = new BoundParameter(argument.Syntax, parameter);
                        var lValueType = result.LValueType;
                        trackNullableStateForAssignment(parameterValue, lValueType, MakeSlot(argument), parameterWithState, argument.IsSuppressed, parameterAnnotations);

                        // check whether parameter would unsafely let a null out in the worse case
                        if (!argument.IsSuppressed)
                        {
                            var leftAnnotations = GetLValueAnnotations(argument);
                            ReportNullableAssignmentIfNecessary(
                                parameterValue,
                                targetType: ApplyLValueAnnotations(lValueType, leftAnnotations),
                                valueType: applyPostConditionsUnconditionally(parameterWithState, parameterAnnotations),
                                UseLegacyWarnings(argument));
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

                        var declaredType = result.LValueType;
                        var leftAnnotations = GetLValueAnnotations(argument);
                        var lValueType = ApplyLValueAnnotations(declaredType, leftAnnotations);
                        if (argument is BoundLocal local && local.DeclarationKind == BoundLocalDeclarationKind.WithInferredType)
                        {
                            var varType = worstCaseParameterWithState.ToAnnotatedTypeWithAnnotations(compilation);
                            _variables.SetType(local.LocalSymbol, varType);
                            lValueType = varType;
                        }
                        else if (argument is BoundDiscardExpression discard && discard.IsInferred)
                        {
                            SetAnalyzedNullability(discard, new VisitResult(parameterWithState, parameterWithState.ToTypeWithAnnotations(compilation)), isLvalue: true);
                        }

                        // track state by assigning from a fictional value from the parameter to the argument.
                        var parameterValue = new BoundParameter(argument.Syntax, parameter);

                        // If the argument type has annotations, we perform an additional check for nullable value types
                        CheckDisallowedNullAssignment(parameterWithState, leftAnnotations, argument.Syntax);

                        AdjustSetValue(argument, ref parameterWithState);
                        trackNullableStateForAssignment(parameterValue, lValueType, MakeSlot(argument), parameterWithState, argument.IsSuppressed, parameterAnnotations);

                        // report warnings if parameter would unsafely let a null out in the worst case
                        if (!argument.IsSuppressed)
                        {
                            ReportNullableAssignmentIfNecessary(parameterValue, lValueType, worstCaseParameterWithState, UseLegacyWarnings(argument));

                            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                            if (!_conversions.HasIdentityOrImplicitReferenceConversion(parameterType.Type, lValueType.Type, ref discardedUseSiteInfo))
                            {
                                ReportNullabilityMismatchInArgument(argument.Syntax, lValueType.Type, parameter, parameterType.Type, forOutput: true);
                            }
                        }
                    }
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(refKind);
            }

            FlowAnalysisAnnotations notNullBasedOnParameters(FlowAnalysisAnnotations parameterAnnotations, ArrayBuilder<ParameterSymbol>? notNullParametersOpt, ParameterSymbol parameter)
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
                    return TypeWithState.Create(typeWithState.Type, NullableFlowState.MaybeDefault);
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
                    return TypeWithState.Create(typeWithState.Type, NullableFlowState.MaybeDefault);
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
                    return TypeWithState.Create(typeWithState.Type, NullableFlowState.MaybeDefault);
                }
                else if (notNullWhenFalse)
                {
                    return TypeWithState.Create(typeWithState.Type, NullableFlowState.NotNull);
                }

                return typeWithState;
            }
        }

        /// <summary>
        /// Learn from postconditions on a by-value or 'in' argument.
        /// </summary>
        private void LearnFromPostConditions(BoundExpression argument, FlowAnalysisAnnotations parameterAnnotations)
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
            else if (notNullWhenTrue && notNullWhenFalse
                && !IsConditionalState
                && !(maybeNullWhenTrue || maybeNullWhenFalse))
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

        private static VariableState GetVariableState(Variables variables, LocalState localState)
        {
            Debug.Assert(variables.Id == localState.Id);
            return new VariableState(variables.CreateSnapshot(), localState.CreateSnapshot());
        }

        private (ParameterSymbol? Parameter, TypeWithAnnotations Type, FlowAnalysisAnnotations Annotations, bool isExpandedParamsArgument) GetCorrespondingParameter(
            int argumentOrdinal,
            ImmutableArray<ParameterSymbol> parametersOpt,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded,
            ref TypeWithAnnotations paramsIterationType)
        {
            if (parametersOpt.IsDefault)
            {
                return default;
            }

            var parameter = Binder.GetCorrespondingParameter(argumentOrdinal, parametersOpt, argsToParamsOpt, expanded);
            if (parameter is null)
            {
                Debug.Assert(!expanded);
                return default;
            }

            var type = parameter.TypeWithAnnotations;
            if (expanded && (object)parameter == parametersOpt[^1])
            {
                if (!paramsIterationType.HasType)
                {
                    OverloadResolution.TryInferParamsCollectionIterationType(_binder, type.Type, out paramsIterationType);
                    Debug.Assert(paramsIterationType.HasType);
                }

                return (parameter, paramsIterationType, FlowAnalysisAnnotations.None, isExpandedParamsArgument: true);
            }

            return (parameter, type, GetParameterAnnotations(parameter), isExpandedParamsArgument: false);
        }

        private TMember InferMemberTypeArguments<TMember>(
            TMember member,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded)
            where TMember : Symbol
        {
            Debug.Assert(member.GetMemberArityIncludingExtension() != 0);

            // https://github.com/dotnet/roslyn/issues/27961 OverloadResolution.IsMemberApplicableInNormalForm and
            // IsMemberApplicableInExpandedForm use the least overridden method. We need to do the same here.
            var definition = member.IsExtensionBlockMember() ? member.OriginalDefinition : member.ConstructedFrom();
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
                binder: _binder,
                expanded: expanded,
                parameterTypes: out ImmutableArray<TypeWithAnnotations> parameterTypes,
                parameterRefKinds: out ImmutableArray<RefKind> parameterRefKinds);
            refKinds.Free();

            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            var typeParameters = definition.GetTypeParametersIncludingExtension();
            Dictionary<TypeParameterSymbol, int>? ordinals = definition.MakeAdjustedTypeParameterOrdinalsIfNeeded(typeParameters);

            var result = MethodTypeInferrer.Infer(
                _binder,
                _conversions,
                typeParameters,
                definition.ContainingType,
                parameterTypes,
                parameterRefKinds,
                arguments,
                ref discardedUseSiteInfo,
                new MethodInferenceExtensions(this),
                ordinals);

            if (!result.Success)
            {
                return member;
            }

            return (TMember)(object)definition.ConstructIncludingExtension(result.InferredTypeArguments);
        }

        /// <remarks>
        /// <para>This type assists with nullable reinference of generic types used in expressions.</para>
        ///
        /// <para>
        /// "Type argument inference" is the step we do during initial binding,
        /// when producing the bound tree used as input to nullable analysis, lowering, and a bunch of other steps.
        /// This inference is flow-independent, so, an expression used at any point in a method,
        /// would get the same type arguments for the calls in it, assuming the stuff the expression is using is still in scope.
        /// </para>
        ///
        /// <para>
        /// "Reinference" is done during nullable analysis, in order to enrich the results of
        /// the initial type argument inference, based on the flow state of expressions at the particular point of usage.
        /// </para>
        ///
        /// <para>What it comes down to is scenarios like the following:</para>
        ///
        /// <code>
        /// var str = GetStringOrNull();
        ///
        /// var arr1 = ImmutableArray.Create(str);
        /// arr1[0].ToString(); // warning: possible null dereference
        ///
        /// if (str == null)
        ///     return;
        ///
        /// var arr2 = ImmutableArray.Create(str);
        /// arr2[0].ToString(); // ok
        /// </code>
        ///
        /// <para>
        /// For both calls to ImmutableArray.Create, initial binding will do a flow-independent type argument inference,
        /// and both will receive type argument `string` (oblivious).
        /// </para>
        ///
        /// <para>
        /// During nullable analysis, we will do a reinference of the call. The first call will get type argument `string?`.
        /// The second call will get type argument `string` (non-nullable), based on the flow state of `str` at that point.
        /// That needs to propagate to the next points in the control flow and be surfaced in public API for the types of the expressions and so on.
        /// </para>
        ///
        /// <para>
        /// Reinference needs to be done on pretty much any expression which can represent a usage of either a generic method or a member of a generic type.
        /// That ends up including calls (obviously) but also things like binary/unary operators, compound assignments, foreach statements, await-exprs, collection expression elements and so on.
        /// </para>
        /// </remarks>
        private sealed class MethodInferenceExtensions : MethodTypeInferrer.Extensions
        {
            private readonly NullableWalker _walker;

            internal MethodInferenceExtensions(NullableWalker walker)
            {
                _walker = walker;
            }

            internal override TypeWithAnnotations GetTypeWithAnnotations(BoundExpression expr)
            {
                return TypeWithAnnotations.Create(expr.GetTypeOrFunctionType(), GetNullableAnnotation(expr));
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
                        return expr.ConstantValueOpt == ConstantValue.NotAvailable || !expr.ConstantValueOpt.IsNull || expr.IsSuppressed ? NullableAnnotation.NotAnnotated : NullableAnnotation.Annotated;
                    case BoundKind.ExpressionWithNullability:
                        return ((BoundExpressionWithNullability)expr).NullableAnnotation;
                    case BoundKind.MethodGroup:
                    case BoundKind.UnboundLambda:
                    case BoundKind.UnconvertedObjectCreationExpression:
                    case BoundKind.ConvertedTupleLiteral:
                    case BoundKind.UnconvertedCollectionExpression:
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

        private ImmutableArray<BoundExpression> GetArgumentsForMethodTypeInference(ImmutableArray<VisitResult> argumentResults, ImmutableArray<BoundExpression> arguments)
        {
            // https://github.com/dotnet/roslyn/issues/27961 MethodTypeInferrer.Infer relies
            // on the BoundExpressions for tuple element types and method groups.
            // By using a generic BoundValuePlaceholder, we're losing inference in those cases.
            // https://github.com/dotnet/roslyn/issues/27961 Inference should be based on
            // unconverted arguments. Consider cases such as `default`, lambdas, tuples.
            int n = argumentResults.Length;
            var builder = ArrayBuilder<BoundExpression>.GetInstance(n);
            for (int i = 0; i < n; i++)
            {
                var visitArgumentResult = argumentResults[i];
                builder.Add(getArgumentForMethodTypeInference(arguments[i], visitArgumentResult));
            }
            return builder.ToImmutableAndFree();

            BoundExpression getArgumentForMethodTypeInference(BoundExpression argument, VisitResult visitResult)
            {
                var lambdaState = visitResult.StateForLambda;
                if (argument.Kind == BoundKind.Lambda)
                {
                    Debug.Assert(lambdaState.HasValue);
                    // MethodTypeInferrer must infer nullability for lambdas based on the nullability
                    // from flow analysis rather than the declared nullability. To allow that, we need
                    // to re-bind lambdas in MethodTypeInferrer.
                    return getUnboundLambda((BoundLambda)argument, GetVariableState(_variables, lambdaState.Value), _getterNullResilienceData);
                }

                if (argument.Kind == BoundKind.CollectionExpression)
                {
                    // MethodTypeInferrer must infer types using the elements of an unconverted collection expression
                    var collectionExpressionVisitResults = visitResult.NestedVisitResults;
                    var collection = (BoundCollectionExpression)argument;

                    Debug.Assert(collectionExpressionVisitResults is not null);
                    Debug.Assert(collectionExpressionVisitResults.Length == collection.Elements.Length);

                    var elementsBuilder = ArrayBuilder<BoundNode>.GetInstance(collectionExpressionVisitResults.Length);
                    for (int i = 0; i < collectionExpressionVisitResults.Length; i++)
                    {
                        if (collection.Elements[i] is BoundExpression elementExpression)
                        {
                            var (elementNoConversion, _) = RemoveConversion(elementExpression, includeExplicitConversions: false);
                            elementsBuilder.Add(getArgumentForMethodTypeInference(elementNoConversion, collectionExpressionVisitResults[i]));
                        }
                        else
                        {
                            elementsBuilder.Add(collection.Elements[i]);
                        }
                    }

                    // Note: the 'with(...)' element in a collection expression does not contribute to method type
                    // inference (just like 'new(...)' in an argument position does not.  Instead, once method type
                    // inference is done, the final target type will be used to bind and determine what 'with(...)'
                    // and 'new(...)' mean.
                    //
                    // So in this case, just pass 'null' for this as they do not contribute to inference and it's 
                    // the same as if the user did not provide any.
                    return new BoundUnconvertedCollectionExpression(
                        collection.Syntax, withElement: null, elements: elementsBuilder.ToImmutableAndFree())
                    {
                        WasCompilerGenerated = true
                    };
                }

                // Note: for `out` arguments, the argument result contains the declaration type (see `VisitArgumentEvaluate`)
                var argumentType = visitResult.RValueType.ToTypeWithAnnotations(compilation);

                if (!argumentType.HasType)
                {
                    return argument;
                }

                if (argument is BoundLocal { DeclarationKind: BoundLocalDeclarationKind.WithInferredType } || IsTargetTypedExpression(argument))
                {
                    // target-typed contexts don't contribute to nullability
                    return new BoundExpressionWithNullability(argument.Syntax, argument, NullableAnnotation.Oblivious, type: null);
                }

                return new BoundExpressionWithNullability(argument.Syntax, argument, argumentType.NullableAnnotation, argumentType.Type);
            }

            static UnboundLambda getUnboundLambda(BoundLambda expr, VariableState variableState, GetterNullResilienceData? getterNullResilienceData)
            {
                return expr.UnboundLambda.WithNullabilityInfo(variableState, getterNullResilienceData);
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
            ArrayBuilder<TypeParameterDiagnosticInfo>? useSiteDiagnosticsBuilder = null;

            ConstraintsHelper.CheckMethodConstraints(
                method,
                new ConstraintsHelper.CheckConstraintsArgs(compilation, _conversions, includeNullability: true, NoLocation.Singleton, diagnostics: null, template: CompoundUseSiteInfo<AssemblySymbol>.Discarded),
                diagnosticsBuilder,
                nullabilityBuilder,
                ref useSiteDiagnosticsBuilder);

            foreach (var pair in nullabilityBuilder)
            {
                if (pair.UseSiteInfo.DiagnosticInfo is object)
                {
                    Diagnostics.Add(pair.UseSiteInfo.DiagnosticInfo, syntax.Location);
                }
            }

            useSiteDiagnosticsBuilder?.Free();
            nullabilityBuilder.Free();
            diagnosticsBuilder.Free();
        }

        private void CheckExtensionConstraints(SyntaxNode syntax, NamedTypeSymbol extension)
        {
            if (_disableDiagnostics)
            {
                return;
            }

            var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            var nullabilityBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            ArrayBuilder<TypeParameterDiagnosticInfo>? useSiteDiagnosticsBuilder = null;

            var constraintsArgs = new ConstraintsHelper.CheckConstraintsArgs(compilation, _conversions, includeNullability: false, location: NoLocation.Singleton, diagnostics: null, template: CompoundUseSiteInfo<AssemblySymbol>.Discarded);

            ConstraintsHelper.CheckConstraints(extension, in constraintsArgs,
                extension.TypeSubstitution, extension.TypeParameters, extension.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics,
                diagnosticsBuilder, nullabilityDiagnosticsBuilderOpt: nullabilityBuilder, ref useSiteDiagnosticsBuilder);

            foreach (var pair in nullabilityBuilder)
            {
                if (pair.UseSiteInfo.DiagnosticInfo is object)
                {
                    Diagnostics.Add(pair.UseSiteInfo.DiagnosticInfo, syntax.Location);
                }
            }

            useSiteDiagnosticsBuilder?.Free();
            nullabilityBuilder.Free();
            diagnosticsBuilder.Free();
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
            ConversionGroup? group = null;
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
        private Conversion GenerateConversionForConditionalOperator(BoundExpression sourceExpression, TypeSymbol? sourceType, TypeSymbol destinationType, bool reportMismatch, bool isChecked)
        {
            var conversion = GenerateConversion(_conversions, sourceExpression, sourceType, destinationType, fromExplicitCast: false, extensionMethodThisArgument: false, isChecked: isChecked);
            bool canConvertNestedNullability = conversion.Exists;
            if (!canConvertNestedNullability && reportMismatch && !sourceExpression.IsSuppressed)
            {
                ReportNullabilityMismatchInAssignment(sourceExpression.Syntax, GetTypeAsDiagnosticArgument(sourceType), destinationType);
            }
            return conversion;
        }

        private Conversion GenerateConversion(Conversions conversions, BoundExpression? sourceExpression, TypeSymbol? sourceType, TypeSymbol destinationType, bool fromExplicitCast, bool extensionMethodThisArgument, bool isChecked)
        {
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            bool useExpression = sourceType is null || UseExpressionForConversion(sourceExpression);
            if (extensionMethodThisArgument)
            {
                return conversions.ClassifyImplicitExtensionMethodThisArgConversion(
                    useExpression ? sourceExpression : null,
                    sourceType,
                    destinationType,
                    ref discardedUseSiteInfo,
                    isMethodGroupConversion: false);
            }
            return useExpression ?
                (fromExplicitCast ?
                    conversions.ClassifyConversionFromExpression(sourceExpression, destinationType, isChecked: isChecked, ref discardedUseSiteInfo, forCast: true) :
                    conversions.ClassifyImplicitConversionFromExpression(sourceExpression!, destinationType, ref discardedUseSiteInfo)) :
                (fromExplicitCast ?
                    conversions.ClassifyConversionFromType(sourceType, destinationType, isChecked: isChecked, ref discardedUseSiteInfo, forCast: true) :
                    conversions.ClassifyImplicitConversionFromType(sourceType!, destinationType, ref discardedUseSiteInfo));
        }

        /// <summary>
        /// Returns true if the expression should be used as the source when calculating
        /// a conversion from this expression, rather than using the type (with nullability)
        /// calculated by visiting this expression. Typically, that means expressions that
        /// do not have an explicit type but there are several other cases as well.
        /// (See expressions handled in ClassifyImplicitBuiltInConversionFromExpression.)
        /// </summary>
        private bool UseExpressionForConversion([NotNullWhen(true)] BoundExpression? value)
        {
            if (value is null)
            {
                return false;
            }
            if (value.Type is null || value.Type.IsDynamic() || value.ConstantValueOpt != null)
            {
                return true;
            }
            switch (value.Kind)
            {
                case BoundKind.InterpolatedString:
                    return true;
                default:
                    if (!_binder.InAttributeArgument && !_binder.InParameterDefaultValue && // These checks prevent cycles caused by attribute binding when HasInlineArrayAttribute check triggers that.
                        value.Type.HasInlineArrayAttribute(out _) == true &&
                        value.Type.TryGetInlineArrayElementField() is not null)
                    {
                        return true;
                    }

                    return false;
            }
        }

        /// <summary>
        /// Adjust declared type based on inferred nullability at the point of reference.
        /// </summary>
        private TypeWithState GetAdjustedResult(TypeWithState type, int slot)
        {
            if (slot > 0)
            {
                NullableFlowState state = GetState(ref this.State, slot);
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
        private static Symbol AsMemberOfType(TypeSymbol? type, Symbol symbol)
        {
            Debug.Assert((object)symbol != null);
            Debug.Assert(!symbol.IsExtensionBlockMember(), symbol.ToDisplayString());

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

            if (symbol is TupleElementFieldSymbol or TupleErrorFieldSymbol)
            {
                return symbol.SymbolAsMember(containingType);
            }

            var symbolContainer = symbol.ContainingType;
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
            if (!containingType.IsGenericType)
            {
                return symbol;
            }
            if (symbolContainer.IsInterface)
            {
                if (tryAsMemberOfSingleType(containingType, out var result))
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
                    if (tryAsMemberOfSingleType(containingType, out var result))
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

            Debug.Assert(false);
            return symbol;

            bool tryAsMemberOfSingleType(NamedTypeSymbol singleType, [NotNullWhen(true)] out Symbol? result)
            {
                if (!singleType.Equals(symbolContainer, TypeCompareKind.AllIgnoreOptions))
                {
                    result = null;
                    return false;
                }
                var symbolDef = symbol.OriginalDefinition;
                result = symbolDef.SymbolAsMember(singleType);
                if (result is MethodSymbol resultMethod && resultMethod.IsGenericMethod)
                {
                    result = resultMethod.Construct(((MethodSymbol)symbol).TypeArgumentsWithAnnotations);
                }
                return true;
            }
        }

        public override BoundNode? VisitConversion(BoundConversion node)
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
            if (TypeAllowsConditionalState(targetType.Type) && TypeAllowsConditionalState(operand.Type))
            {
                Visit(operand); // don't Unsplit
            }
            else
            {
                VisitRvalue(operand);
            }

            SetResultType(node,
                VisitConversion(
                    node,
                    operand,
                    conversion,
                    targetType,
                    ResultType,
                    checkConversion: true,
                    fromExplicitCast: fromExplicitCast,
                    useLegacyWarnings: fromExplicitCast,
                    AssignmentKind.Assignment,
                    reportTopLevelWarnings: fromExplicitCast,
                    reportRemainingWarnings: true,
                    trackMembers: !IsConditionalState));

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

            var (resultType, completion) = VisitOptionalImplicitConversion(expr, targetTypeOpt, useLegacyWarnings, trackMembers, assignmentKind, delayCompletionForTargetType: false);
            Debug.Assert(completion is null);
            return resultType;
        }

        private (TypeWithState resultType, Func<TypeWithAnnotations, TypeWithState>? completion) VisitOptionalImplicitConversion(
            BoundExpression expr,
            TypeWithAnnotations targetTypeOpt,
            bool useLegacyWarnings,
            bool trackMembers,
            AssignmentKind assignmentKind,
            bool delayCompletionForTargetType)
        {
            (BoundExpression operand, Conversion conversion) = RemoveConversion(expr, includeExplicitConversions: false);
            SnapshotWalkerThroughConversionGroup(expr, operand);
            var operandType = VisitRvalueWithState(operand);

            return visitConversion(expr, targetTypeOpt, useLegacyWarnings, trackMembers, assignmentKind, operand, conversion, operandType, delayCompletionForTargetType);

            (TypeWithState resultType, Func<TypeWithAnnotations, TypeWithState>? completion) visitConversion(
                BoundExpression expr,
                TypeWithAnnotations targetTypeOpt,
                bool useLegacyWarnings, bool trackMembers, AssignmentKind assignmentKind,
                BoundExpression operand,
                Conversion conversion, TypeWithState operandType,
                bool delayCompletionForTargetType)
            {
                if (delayCompletionForTargetType)
                {
                    return (TypeWithState.Create(targetTypeOpt), visitConversionAsContinuation(expr, useLegacyWarnings, trackMembers, assignmentKind, operand, conversion, operandType));
                }

                Debug.Assert(targetTypeOpt.HasType);

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

                return (resultType, null);
            }

            Func<TypeWithAnnotations, TypeWithState> visitConversionAsContinuation(BoundExpression expr, bool useLegacyWarnings, bool trackMembers, AssignmentKind assignmentKind, BoundExpression operand, Conversion conversion, TypeWithState operandType)
            {
                return (TypeWithAnnotations targetTypeOpt) =>
                {
                    var result = visitConversion(expr, targetTypeOpt, useLegacyWarnings, trackMembers, assignmentKind, operand, conversion, operandType, delayCompletionForTargetType: false);
                    Debug.Assert(result.completion is null);
                    return result.resultType;
                };
            }
        }

        private static bool AreNullableAndUnderlyingTypes([NotNullWhen(true)] TypeSymbol? nullableTypeOpt, [NotNullWhen(true)] TypeSymbol? underlyingTypeOpt, out TypeWithAnnotations underlyingTypeWithAnnotations)
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

        public override BoundNode? VisitTupleLiteral(BoundTupleLiteral node)
        {
            VisitTupleExpression(node);
            return null;
        }

        public override BoundNode? VisitConvertedTupleLiteral(BoundConvertedTupleLiteral node)
        {
            Debug.Assert(!IsConditionalState);
            var savedState = this.State.Clone();
            // Visit the source tuple so that the semantic model can correctly report nullability for it
            // Disable diagnostics, as we don't want to duplicate any that are produced by visiting the converted literal below
            VisitWithoutDiagnostics(node.SourceTuple);

            this.SetState(savedState);
            VisitTupleExpression(node);

            return null;
        }

        private void VisitTupleExpression(BoundTupleExpression node)
        {
            var arguments = node.Arguments;
            ImmutableArray<TypeWithState> elementTypes = arguments.SelectAsArray((a, w) => w.VisitRvalueWithState(a), this);
            ImmutableArray<TypeWithAnnotations> elementTypesWithAnnotations = elementTypes.SelectAsArray(a => a.ToTypeWithAnnotations(compilation));
            var tupleOpt = (NamedTypeSymbol?)node.Type;
            if (tupleOpt is null)
            {
                SetResultType(node, TypeWithState.Create(null, NullableFlowState.NotNull));
            }
            else
            {
                int slot = GetOrCreatePlaceholderSlot(node);
                if (slot > 0)
                {
                    SetState(ref this.State, slot, NullableFlowState.NotNull);
                    TrackNullableStateOfTupleElements(slot, tupleOpt, arguments, elementTypes, argsToParamsOpt: default, useRestField: false);
                }

                tupleOpt = tupleOpt.WithElementTypes(elementTypesWithAnnotations);
                if (!_disableDiagnostics)
                {
                    var locations = tupleOpt.TupleElements.SelectAsArray((element, location) => element.TryGetFirstLocation() ?? location, node.Syntax.Location);
                    var diagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
                    Debug.Assert(diagnostics.DiagnosticBag is { });

                    tupleOpt.CheckConstraints(new ConstraintsHelper.CheckConstraintsArgs(compilation, _conversions, includeNullability: true, node.Syntax.Location, diagnostics: null),
                                              typeSyntax: node.Syntax, locations, nullabilityDiagnosticsOpt: diagnostics);

                    Diagnostics.AddRange(diagnostics.DiagnosticBag);
                    diagnostics.Free();
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
            NamedTypeSymbol tupleType,
            ImmutableArray<BoundExpression> values,
            ImmutableArray<TypeWithState> types,
            ImmutableArray<int> argsToParamsOpt,
            bool useRestField)
        {
            Debug.Assert(tupleType.IsTupleType);
            Debug.Assert(values.Length == types.Length);
            Debug.Assert(values.Length == (useRestField ? Math.Min(tupleType.TupleElements.Length, NamedTypeSymbol.ValueTupleRestPosition) : tupleType.TupleElements.Length));

            if (slot > 0)
            {
                var tupleElements = tupleType.TupleElements;
                int n = values.Length;
                if (useRestField)
                {
                    n = Math.Min(n, NamedTypeSymbol.ValueTupleRestPosition - 1);
                }
                for (int i = 0; i < n; i++)
                {
                    var argOrdinal = getArgumentOrdinalFromParameterOrdinal(i);
                    trackState(values[argOrdinal], tupleElements[i], types[argOrdinal]);
                }
                if (useRestField &&
                    values.Length == NamedTypeSymbol.ValueTupleRestPosition &&
                    tupleType.GetMembers(NamedTypeSymbol.ValueTupleRestFieldName).FirstOrDefault() is FieldSymbol restField)
                {
                    var argOrdinal = getArgumentOrdinalFromParameterOrdinal(NamedTypeSymbol.ValueTupleRestPosition - 1);
                    trackState(values[argOrdinal], restField, types[argOrdinal]);
                }
            }

            void trackState(BoundExpression value, FieldSymbol field, TypeWithState valueType)
            {
                int targetSlot = GetOrCreateSlot(field, slot);
                TrackNullableStateForAssignment(value, field.TypeWithAnnotations, targetSlot, valueType, MakeSlot(value));
            }

            int getArgumentOrdinalFromParameterOrdinal(int parameterOrdinal)
            {
                var index = argsToParamsOpt.IsDefault ? parameterOrdinal : argsToParamsOpt.IndexOf(parameterOrdinal);
                Debug.Assert(index != -1);
                return index;
            }
        }

        private void TrackNullableStateOfNullableValue(int containingSlot, TypeSymbol containingType, BoundExpression? value, TypeWithState valueType, int valueSlot)
        {
            Debug.Assert(containingType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T);
            Debug.Assert(containingSlot > 0);
            Debug.Assert(valueSlot > 0);

            int targetSlot = GetNullableOfTValueSlot(containingType, containingSlot, out Symbol? symbol);
            if (targetSlot > 0)
            {
                TrackNullableStateForAssignment(value, GetTypeOrReturnTypeWithAnnotations(symbol!), targetSlot, valueType, valueSlot);
            }
        }

        private void TrackNullableStateOfTupleConversion(
            BoundConversion? conversionOpt,
            BoundExpression convertedNode,
            Conversion conversion,
            TypeSymbol targetType,
            TypeSymbol operandType,
            int slot,
            int valueSlot,
            AssignmentKind assignmentKind,
            ParameterSymbol? parameterOpt,
            bool reportWarnings)
        {
            Debug.Assert(conversion.Kind == ConversionKind.ImplicitTuple || conversion.Kind == ConversionKind.ExplicitTuple);
            Debug.Assert(slot > 0);
            Debug.Assert(valueSlot > 0);

            var valueTuple = operandType as NamedTypeSymbol;
            if (valueTuple is null || !valueTuple.IsTupleType)
            {
                return;
            }

            var conversions = conversion.UnderlyingConversions;
            var targetElements = ((NamedTypeSymbol)targetType).TupleElements;
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
                                SetState(ref this.State, targetFieldSlot, NullableFlowState.NotNull);
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
                                SetState(ref this.State, targetFieldSlot, NullableFlowState.NotNull);
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
                                SetState(ref this.State, targetFieldSlot, convertedType.State);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        public override BoundNode? VisitTupleBinaryOperator(BoundTupleBinaryOperator node)
        {
            base.VisitTupleBinaryOperator(node);
            SetNotNullResult(node);
            return null;
        }

        private void ReportNullabilityMismatchWithTargetDelegate(Location location, TypeSymbol targetType, MethodSymbol targetInvokeMethod, MethodSymbol sourceInvokeMethod, bool invokedAsExtensionMethod)
        {
            var diagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
            Debug.Assert(diagnostics.DiagnosticBag is { });

            SourceMemberContainerTypeSymbol.CheckValidNullableMethodOverride(
                compilation,
                targetInvokeMethod,
                sourceInvokeMethod,
                diagnostics,
                reportBadDelegateReturn,
                reportBadDelegateParameter,
                extraArgument: (targetType, location),
                invokedAsExtensionMethod: invokedAsExtensionMethod);

            Diagnostics.AddRange(diagnostics.DiagnosticBag);
            diagnostics.Free();

            void reportBadDelegateReturn(BindingDiagnosticBag bag, MethodSymbol targetInvokeMethod, MethodSymbol sourceInvokeMethod, bool topLevel, (TypeSymbol targetType, Location location) arg)
            {
                ReportDiagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, arg.location,
                    new FormattedSymbol(sourceInvokeMethod, SymbolDisplayFormat.MinimallyQualifiedFormat),
                    arg.targetType);
            }

            void reportBadDelegateParameter(BindingDiagnosticBag bag, MethodSymbol sourceInvokeMethod, MethodSymbol targetInvokeMethod, ParameterSymbol parameter, bool topLevel, (TypeSymbol targetType, Location location) arg)
            {
                ReportDiagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, arg.location,
                    GetParameterAsDiagnosticArgument(parameter),
                    GetContainingSymbolAsDiagnosticArgument(parameter),
                    arg.targetType);
            }
        }

        private void ReportNullabilityMismatchWithTargetDelegate(Location location, NamedTypeSymbol delegateType, BoundLambda lambda)
        {
            MethodSymbol? targetInvokeMethod = delegateType.DelegateInvokeMethod;
            LambdaSymbol sourceMethod = (LambdaSymbol)lambda.Symbol;
            UnboundLambda unboundLambda = lambda.UnboundLambda;

            if (targetInvokeMethod is null ||
                targetInvokeMethod.ParameterCount != sourceMethod.ParameterCount)
            {
                return;
            }

            // Parameter nullability is expected to match exactly. This corresponds to the behavior of initial binding.
            //    Action<string> x = (object o) => { }; // error CS1661: Cannot convert lambda expression to delegate type 'Action<string>' because the parameter types do not match the delegate parameter types
            //    Action<object> y = (object? o) => { }; // warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'Action<object>'.
            // We check that by calling CheckValidNullableMethodOverride in both directions.
            // https://github.com/dotnet/roslyn/issues/35564: Consider relaxing and allow implicit conversions of nullability (as we do for method group conversions).

            if (lambda.Syntax is LambdaExpressionSyntax lambdaSyntax)
            {
                int start = lambdaSyntax.SpanStart;
                location = Location.Create(lambdaSyntax.SyntaxTree, new Text.TextSpan(start, lambdaSyntax.ArrowToken.Span.End - start));
            }

            var diagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
            Debug.Assert(diagnostics.DiagnosticBag is { });

            if (SourceMemberContainerTypeSymbol.CheckValidNullableMethodOverride(
                compilation,
                targetInvokeMethod,
                sourceMethod,
                diagnostics,
                reportBadDelegateReturn,
                reportBadDelegateParameter,
                extraArgument: location,
                invokedAsExtensionMethod: false))
            {
                Diagnostics.AddRange(diagnostics.DiagnosticBag);
                diagnostics.Free();
                return;
            }

            SourceMemberContainerTypeSymbol.CheckValidNullableMethodOverride(
                compilation,
                sourceMethod,
                targetInvokeMethod,
                diagnostics,
                reportBadDelegateReturn,
                reportBadDelegateParameter,
                extraArgument: location,
                invokedAsExtensionMethod: false);

            Diagnostics.AddRange(diagnostics.DiagnosticBag);
            diagnostics.Free();

            void reportBadDelegateReturn(BindingDiagnosticBag bag, MethodSymbol targetInvokeMethod, MethodSymbol sourceInvokeMethod, bool topLevel, Location location)
            {
                ReportDiagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, location,
                    unboundLambda.MessageID.Localize(),
                    delegateType);
            }

            void reportBadDelegateParameter(BindingDiagnosticBag bag, MethodSymbol sourceInvokeMethod, MethodSymbol targetInvokeMethod, ParameterSymbol parameterSymbol, bool topLevel, Location location)
            {
                // For anonymous functions with implicit parameters, no need to report this since the parameters can't be referenced
                if (unboundLambda.HasSignature)
                {
                    ReportDiagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, location,
                        unboundLambda.ParameterName(parameterSymbol.Ordinal),
                        unboundLambda.MessageID.Localize(),
                        delegateType);
                }
            }
        }

        /// <summary>
        /// Gets the conversion node for passing to VisitConversion, if one should be passed.
        /// </summary>
        private static BoundConversion? GetConversionIfApplicable(BoundExpression? conversionOpt, BoundExpression convertedNode)
        {
            Debug.Assert(conversionOpt is null
                         || convertedNode == conversionOpt // Note that convertedNode itself can be a BoundConversion, so we do this check explicitly
                                                           // because the below calls to RemoveConversion could potentially strip that conversion.
                         || convertedNode == RemoveConversion(conversionOpt, includeExplicitConversions: false).expression
                         || convertedNode == RemoveConversion(conversionOpt, includeExplicitConversions: true).expression);
            return conversionOpt == convertedNode ? null : (BoundConversion?)conversionOpt;
        }

        /// <summary>
        /// Apply the conversion to the type of the operand and return the resulting type.
        /// If the operand does not have an explicit type, the operand expression is used.
        /// </summary>
        /// <param name="checkConversion">
        /// If <see langword="true"/>, the incoming conversion is assumed to be from binding
        /// and will be re-calculated, this time considering nullability.
        /// Note that the conversion calculation considers nested nullability only.
        /// The caller is responsible for checking the top-level nullability of
        /// the type returned by this method.
        /// </param>
        /// <param name="trackMembers">
        /// If <see langword="true"/>, the nullability of any members of the operand
        /// will be copied to the converted result when possible.
        /// </param>
        /// <param name="useLegacyWarnings">
        /// If <see langword="true"/>, indicates that the "non-safety" diagnostic <see cref="ErrorCode.WRN_ConvertingNullableToNonNullable"/>
        /// should be given for an invalid conversion.
        /// </param>
        private TypeWithState VisitConversion(
            BoundConversion? conversionOpt,
            BoundExpression conversionOperand,
            Conversion conversion,
            TypeWithAnnotations targetTypeWithNullability,
            TypeWithState operandType,
            bool checkConversion,
            bool fromExplicitCast,
            bool useLegacyWarnings,
            AssignmentKind assignmentKind,
            ParameterSymbol? parameterOpt = null,
            bool reportTopLevelWarnings = true,
            bool reportRemainingWarnings = true,
            bool isSuppressed = false,
            bool extensionMethodThisArgument = false,
            Optional<LocalState> stateForLambda = default,
            bool trackMembers = false,
            Location? diagnosticLocation = null,
            ArrayBuilder<VisitResult>? previousArgumentConversionResults = null)
        {
            Debug.Assert(!trackMembers || !IsConditionalState);
            Debug.Assert(conversionOperand != null);

            if (IsTargetTypedExpression(conversionOperand))
            {
                if (TargetTypedAnalysisCompletion.TryGetValue(conversionOperand, out Func<TypeWithAnnotations, TypeWithState>? completion))
                {
                    TargetTypedAnalysisCompletion.Remove(conversionOperand);
#if DEBUG
                    bool save_completingTargetTypedExpression = _completingTargetTypedExpression;
                    _completingTargetTypedExpression = true;
#endif
                    if (conversionOperand is BoundObjectCreationExpressionBase && targetTypeWithNullability.IsNullableType())
                    {
                        operandType = completion(targetTypeWithNullability.Type.GetNullableUnderlyingTypeWithAnnotations());
                        conversion = Conversion.MakeNullableConversion(ConversionKind.ImplicitNullable, Conversion.Identity);
                    }
                    else
                    {
                        operandType = completion(targetTypeWithNullability);
                    }
#if DEBUG
                    _completingTargetTypedExpression = save_completingTargetTypedExpression;
#endif
                }
                else
                {
                    Debug.Assert(conversionOpt is null);
                }
            }

            NullableFlowState resultState = NullableFlowState.NotNull;
            bool canConvertNestedNullability = true;

            if (isSuppressed || conversionOperand.IsSuppressed)
            {
                reportTopLevelWarnings = false;
                reportRemainingWarnings = false;
                isSuppressed = true;
            }
#nullable disable

            TypeSymbol targetType = targetTypeWithNullability.Type;
            switch (conversion.Kind)
            {
                case ConversionKind.MethodGroup:
                    {
                        var group = conversionOperand as BoundMethodGroup;
                        var (invokeSignature, parameters) = getDelegateOrFunctionPointerInfo(targetType);
                        var method = conversion.Method;
                        if (group != null)
                        {
                            if (method?.OriginalDefinition is LocalFunctionSymbol localFunc)
                            {
                                VisitLocalFunctionUse(localFunc);
                            }
                            method = CheckMethodGroupReceiverNullability(group, parameters, method, conversion.IsExtensionMethod);
                        }
                        if (reportRemainingWarnings && invokeSignature != null)
                        {
                            ReportNullabilityMismatchWithTargetDelegate(getDiagnosticLocation(), targetType, invokeSignature, method, conversion.IsExtensionMethod);
                        }
                    }
                    resultState = NullableFlowState.NotNull;
                    break;

                    static (MethodSymbol invokeSignature, ImmutableArray<ParameterSymbol>) getDelegateOrFunctionPointerInfo(TypeSymbol targetType)
                        => targetType switch
                        {
                            NamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { Parameters: { } parameters } signature } => (signature, parameters),
                            FunctionPointerTypeSymbol { Signature: { Parameters: { } parameters } signature } => (signature, parameters),
                            _ => (null, ImmutableArray<ParameterSymbol>.Empty),
                        };

#nullable enable
                case ConversionKind.AnonymousFunction:
                    if (conversionOperand is BoundLambda lambda)
                    {
                        var delegateType = targetType.GetDelegateType();
                        VisitLambda(lambda, delegateType, stateForLambda);
                        if (reportRemainingWarnings && delegateType is not null)
                        {
                            ReportNullabilityMismatchWithTargetDelegate(getDiagnosticLocation(), delegateType, lambda);
                        }

                        TrackAnalyzedNullabilityThroughConversionGroup(targetTypeWithNullability.ToTypeWithState(), conversionOpt, conversionOperand);

                        return TypeWithState.Create(targetType, NullableFlowState.NotNull);
                    }
                    break;
#nullable disable

                case ConversionKind.FunctionType:
                    resultState = NullableFlowState.NotNull;
                    break;

                case ConversionKind.InterpolatedString:
                    resultState = NullableFlowState.NotNull;
                    break;

                case ConversionKind.InterpolatedStringHandler:
                    visitInterpolatedStringHandlerConstructor();
                    resultState = NullableFlowState.NotNull;
                    break;

                case ConversionKind.ObjectCreation:
                case ConversionKind.CollectionExpression:
                case ConversionKind.SwitchExpression:
                case ConversionKind.ConditionalExpression:
                    resultState = getConversionResultState(operandType);
                    break;

                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.ImplicitUserDefined:
                    return VisitUserDefinedConversion(conversionOpt, conversionOperand, conversion, targetTypeWithNullability, operandType, useLegacyWarnings, assignmentKind, parameterOpt, reportTopLevelWarnings, reportRemainingWarnings, getDiagnosticLocation());

                case ConversionKind.ExplicitDynamic:
                case ConversionKind.ImplicitDynamic:
                    resultState = getConversionResultState(operandType);
                    break;

                case ConversionKind.Boxing:
                    resultState = getBoxingConversionResultState(targetTypeWithNullability, operandType);
                    break;

                case ConversionKind.Unboxing:
                    if (targetType.IsNonNullableValueType())
                    {
                        if (!operandType.IsNotNull && reportRemainingWarnings)
                        {
                            ReportDiagnostic(ErrorCode.WRN_UnboxPossibleNull, getDiagnosticLocation());
                        }

                        LearnFromNonNullTest(conversionOperand, ref State);
                    }
                    else
                    {
                        resultState = getUnboxingConversionResultState(operandType);
                    }
                    break;

                case ConversionKind.ImplicitThrow:
                    resultState = NullableFlowState.NotNull;
                    break;

                case ConversionKind.NoConversion:
                    resultState = getConversionResultState(operandType);
                    break;

                case ConversionKind.NullLiteral:
                case ConversionKind.DefaultLiteral:
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
                        var explicitType = operandConversion.ConversionGroupOpt?.ExplicitType;
                        if (explicitType?.Equals(targetTypeWithNullability, TypeCompareKind.ConsiderEverything) == true)
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
                case ConversionKind.ExplicitReference:
                    // Inherit state from the operand.
                    if (checkConversion)
                    {
                        conversion = GenerateConversion(_conversions, conversionOperand, operandType.Type, targetType, fromExplicitCast, extensionMethodThisArgument, isChecked: conversionOpt?.Checked ?? false);
                        canConvertNestedNullability = conversion.Exists;
                    }

                    resultState = conversion.IsReference ? getReferenceConversionResultState(targetTypeWithNullability, operandType) : operandType.State;
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
                        conversion = GenerateConversion(_conversions, conversionOperand, operandType.Type, targetType, fromExplicitCast, extensionMethodThisArgument, isChecked: conversionOpt?.Checked ?? false);
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
                            ReportDiagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, getDiagnosticLocation());
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
                        conversion = GenerateConversion(_conversions, conversionOperand, operandType.Type, targetType, fromExplicitCast, extensionMethodThisArgument, isChecked: conversionOpt?.Checked ?? false);
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

                case ConversionKind.InlineArray:
                    if (checkConversion)
                    {
                        conversion = GenerateConversion(_conversions, conversionOperand, operandType.Type, targetType, fromExplicitCast, extensionMethodThisArgument, isChecked: conversionOpt?.Checked ?? false);
                        canConvertNestedNullability = conversion.Exists;
                    }
                    break;

                case ConversionKind.ImplicitSpan:
                case ConversionKind.ExplicitSpan:
                    if (checkConversion)
                    {
                        var previousKind = conversion.Kind;
                        conversion = GenerateConversion(_conversions, conversionOperand, operandType.Type, targetType, fromExplicitCast, extensionMethodThisArgument, isChecked: conversionOpt?.Checked ?? false);
                        // We do not want user-defined conversions to relax nullability, so we consider only span conversions.
                        canConvertNestedNullability = conversion.Exists && conversion.IsSpan;
                    }
                    break;

                default:
                    Debug.Assert(targetType.IsValueType || targetType.IsErrorType());
                    break;
            }

            TypeWithState resultType = calculateResultType(targetTypeWithNullability, fromExplicitCast, resultState, isSuppressed, targetType);

            if (!conversionOperand.HasErrors && !targetType.IsErrorType())
            {
                // Need to report all warnings that apply since the warnings can be suppressed individually.
                if (reportTopLevelWarnings)
                {
                    ReportNullableAssignmentIfNecessary(conversionOperand, targetTypeWithNullability, resultType, useLegacyWarnings, assignmentKind, parameterOpt, getDiagnosticLocation());
                }
                if (reportRemainingWarnings && !canConvertNestedNullability)
                {
                    if (assignmentKind == AssignmentKind.Argument)
                    {
                        ReportNullabilityMismatchInArgument(getDiagnosticLocation(), operandType.Type, parameterOpt, targetType, forOutput: false);
                    }
                    else
                    {
                        ReportNullabilityMismatchInAssignment(getDiagnosticLocation(), GetTypeAsDiagnosticArgument(operandType.Type), targetType);
                    }
                }
            }

            TrackAnalyzedNullabilityThroughConversionGroup(resultType, conversionOpt, conversionOperand);

            return resultType;

            // Avoid realizing the diagnostic location until needed.
            Location getDiagnosticLocation()
            {
                diagnosticLocation ??= (conversionOpt ?? conversionOperand).Syntax.GetLocation();
                return diagnosticLocation;
            }

#nullable enable
            static TypeWithState calculateResultType(TypeWithAnnotations targetTypeWithNullability, bool fromExplicitCast, NullableFlowState resultState, bool isSuppressed, TypeSymbol targetType)
            {
                if (isSuppressed)
                {
                    resultState = NullableFlowState.NotNull;
                }
                else if (fromExplicitCast && targetTypeWithNullability.NullableAnnotation.IsAnnotated() && !targetType.IsNullableType())
                {
                    // An explicit cast to a nullable reference type introduces nullability
                    resultState = targetType?.IsTypeParameterDisallowingAnnotationInCSharp8() == true ? NullableFlowState.MaybeDefault : NullableFlowState.MaybeNull;
                }

                var resultType = TypeWithState.Create(targetType, resultState);
                return resultType;
            }

            static NullableFlowState getReferenceConversionResultState(TypeWithAnnotations targetType, TypeWithState operandType)
            {
                var state = operandType.State;
                switch (state)
                {
                    case NullableFlowState.MaybeNull:
                        if (targetType.Type?.IsTypeParameterDisallowingAnnotationInCSharp8() == true)
                        {
                            var type = operandType.Type;
                            if (type is null || !type.IsTypeParameterDisallowingAnnotationInCSharp8())
                            {
                                return NullableFlowState.MaybeDefault;
                            }
                            else if (targetType.NullableAnnotation.IsNotAnnotated() &&
                                type is TypeParameterSymbol typeParameter1 &&
                                dependsOnTypeParameter(typeParameter1, (TypeParameterSymbol)targetType.Type, NullableAnnotation.NotAnnotated, out var annotation))
                            {
                                return (annotation == NullableAnnotation.Annotated) ? NullableFlowState.MaybeDefault : NullableFlowState.MaybeNull;
                            }
                        }
                        break;
                    case NullableFlowState.MaybeDefault:
                        if (targetType.Type?.IsTypeParameterDisallowingAnnotationInCSharp8() == false)
                        {
                            return NullableFlowState.MaybeNull;
                        }
                        break;
                }
                return state;
            }

            // Converting to a less-derived type (object, interface, type parameter).
            // If the operand is MaybeNull, the result should be
            // MaybeNull (if the target type allows) or MaybeDefault otherwise.
            static NullableFlowState getBoxingConversionResultState(TypeWithAnnotations targetType, TypeWithState operandType)
            {
                var state = operandType.State;
                if (state == NullableFlowState.MaybeNull)
                {
                    var type = operandType.Type;
                    if (type is null || !type.IsTypeParameterDisallowingAnnotationInCSharp8())
                    {
                        return NullableFlowState.MaybeDefault;
                    }
                    else if (targetType.NullableAnnotation.IsNotAnnotated() &&
                        type is TypeParameterSymbol typeParameter1 &&
                        targetType.Type is TypeParameterSymbol typeParameter2)
                    {
                        bool dependsOn = dependsOnTypeParameter(typeParameter1, typeParameter2, NullableAnnotation.NotAnnotated, out var annotation);
                        Debug.Assert(dependsOn); // If this case fails, add a corresponding test.
                        if (dependsOn)
                        {
                            return (annotation == NullableAnnotation.Annotated) ? NullableFlowState.MaybeDefault : NullableFlowState.MaybeNull;
                        }
                    }
                }
                return state;
            }

            // Converting to a more-derived type (struct, class, type parameter).
            // If the operand is MaybeNull or MaybeDefault, the result should be
            // MaybeDefault.
            static NullableFlowState getUnboxingConversionResultState(TypeWithState operandType)
            {
                var state = operandType.State;
                if (state == NullableFlowState.MaybeNull)
                {
                    return NullableFlowState.MaybeDefault;
                }
                return state;
            }

            static NullableFlowState getConversionResultState(TypeWithState operandType)
            {
                var state = operandType.State;
                if (state == NullableFlowState.MaybeNull)
                {
                    return NullableFlowState.MaybeDefault;
                }
                return state;
            }

            // If type parameter 1 depends on type parameter 2 (that is, if type parameter 2 appears
            // in the constraint types of type parameter 1), returns the effective annotation on
            // type parameter 2 in the constraints of type parameter 1.
            static bool dependsOnTypeParameter(TypeParameterSymbol typeParameter1, TypeParameterSymbol typeParameter2, NullableAnnotation typeParameter1Annotation, out NullableAnnotation annotation)
            {
                if (typeParameter1.Equals(typeParameter2, TypeCompareKind.AllIgnoreOptions))
                {
                    annotation = typeParameter1Annotation;
                    return true;
                }
                bool dependsOn = false;
                var combinedAnnotation = NullableAnnotation.Annotated;
                foreach (var constraintType in typeParameter1.ConstraintTypesNoUseSiteDiagnostics)
                {
                    if (constraintType.Type is TypeParameterSymbol constraintTypeParameter &&
                        dependsOnTypeParameter(constraintTypeParameter, typeParameter2, constraintType.NullableAnnotation, out var constraintAnnotation))
                    {
                        dependsOn = true;
                        combinedAnnotation = combinedAnnotation.Meet(constraintAnnotation);
                    }
                }
                if (dependsOn)
                {
                    annotation = combinedAnnotation.Join(typeParameter1Annotation);
                    return true;
                }
                annotation = default;
                return false;
            }

            void visitInterpolatedStringHandlerConstructor()
            {
                var handlerData = conversionOperand.GetInterpolatedStringHandlerData(throwOnMissing: false);
                if (handlerData.IsDefault)
                {
                    return;
                }

                if (previousArgumentConversionResults == null)
                {
                    Debug.Assert(handlerData.ArgumentPlaceholders.IsEmpty
                                 || handlerData.ArgumentPlaceholders.Single().ArgumentIndex == BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter);
                    visitHandlerConstruction(handlerData);
                    return;
                }

                // In new extension form, the nullable rewriter processes the arguments as if receiver is the first item in the argument list, like the old extension form. This means that all of
                // our placeholders will be off-by-one, with the extension receiver in the first position.
                var extensionBlockFormOffset = parameterOpt?.ContainingType.IsExtension is true ? 1 : 0;
                bool addedPlaceholders = false;
                foreach (var placeholder in handlerData.ArgumentPlaceholders)
                {
                    switch (placeholder.ArgumentIndex)
                    {
                        case BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter:
                        case BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter:
                        // We presume that all instance parameters were dereferenced by calling the instance method this handler was passed to. This isn't strictly
                        // true: the handler constructor will be run before the receiver is dereferenced. However, if the dereference isn't safe, that will be a
                        // much better error to report than a mismatched argument nullability error.
                        case BoundInterpolatedStringArgumentPlaceholder.InstanceParameter:
                            break;
                        case BoundInterpolatedStringArgumentPlaceholder.ExtensionReceiver:
                            Debug.Assert(extensionBlockFormOffset == 1);
                            AddPlaceholderReplacement(placeholder, expression: null, previousArgumentConversionResults[0]);
                            addedPlaceholders = true;
                            break;
                        default:
                            if (previousArgumentConversionResults.Count > placeholder.ArgumentIndex)
                            {
                                // We intentionally do not give a replacement bound node for this placeholder, as we do not propagate any post conditions from the constructor
                                // to the original location of the node. This is because the nullable walker is not a true evaluation-order walker, and doing so would cause
                                // us to miss real warnings.
                                AddPlaceholderReplacement(placeholder, expression: null, previousArgumentConversionResults[placeholder.ArgumentIndex + extensionBlockFormOffset]);
                                addedPlaceholders = true;
                            }
                            break;
                    }
                }

                visitHandlerConstruction(handlerData);

                if (addedPlaceholders)
                {
                    foreach (var placeholder in handlerData.ArgumentPlaceholders)
                    {
                        if (placeholder.ArgumentIndex < previousArgumentConversionResults.Count && placeholder.ArgumentIndex is >= 0 or BoundInterpolatedStringArgumentPlaceholder.ExtensionReceiver)
                        {
                            RemovePlaceholderReplacement(placeholder);
                        }
                    }
                }
            }

            void visitHandlerConstruction(InterpolatedStringHandlerData handlerData)
            {
#if DEBUG
                bool save_completingTargetTypedExpression = _completingTargetTypedExpression;
                _completingTargetTypedExpression = false;
#endif
                VisitRvalue(handlerData.Construction);
#if DEBUG
                _completingTargetTypedExpression = save_completingTargetTypedExpression;
#endif
            }
        }

        private TypeWithState VisitUserDefinedConversion(
            BoundConversion? conversionOpt,
            BoundExpression conversionOperand,
            Conversion conversion,
            TypeWithAnnotations targetTypeWithNullability,
            TypeWithState operandType,
            bool useLegacyWarnings,
            AssignmentKind assignmentKind,
            ParameterSymbol? parameterOpt,
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
                TypeWithAnnotations.Create(conversion.BestUserDefinedConversionAnalysis!.FromType),
                operandType,
                checkConversion: true,
                fromExplicitCast: false,
                useLegacyWarnings,
                assignmentKind,
                parameterOpt,
                reportTopLevelWarnings,
                reportRemainingWarnings,
                diagnosticLocation: diagnosticLocation);

            // Update method based on operandType: see https://github.com/dotnet/roslyn/issues/29605.
            // (see NullableReferenceTypesTests.ImplicitConversions_07).
            var method = conversion.Method;
            Debug.Assert(method is object);
            Debug.Assert(method.ParameterCount == 1);
            Debug.Assert(operandType.Type is object);

            var parameter = method.Parameters[0];
            var parameterAnnotations = GetParameterAnnotations(parameter);
            var parameterType = ApplyLValueAnnotations(parameter.TypeWithAnnotations, parameterAnnotations);
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

            // in the case of a lifted conversion, we assume that the call to the operator occurs only if the argument is not-null
            if (!isLiftedConversion && CheckDisallowedNullAssignment(operandType, parameterAnnotations, conversionOperand.Syntax))
            {
                LearnFromNonNullTest(conversionOperand, ref State);
            }

            // method parameter type -> method return type
            var methodReturnType = method.ReturnTypeWithAnnotations;
            operandType = GetLiftedReturnTypeIfNecessary(isLiftedConversion, methodReturnType, operandState);
            if (!isLiftedConversion || operandState.IsNotNull())
            {
                var returnNotNull = operandState.IsNotNull() && method.ReturnNotNullIfParameterNotNull.Contains(parameter.Name);
                if (returnNotNull)
                {
                    operandType = operandType.WithNotNullState();
                }
                else
                {
                    operandType = ApplyUnconditionalAnnotations(operandType, GetRValueAnnotations(method));
                }
            }

            // method return type -> conversion "to" type
            // May be distinct from method return type for Nullable<T>.
            operandType = ClassifyAndVisitConversion(
                conversionOperand,
                TypeWithAnnotations.Create(conversion.BestUserDefinedConversionAnalysis!.ToType),
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

            LearnFromPostConditions(conversionOperand, parameterAnnotations);

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

        private void TrackAnalyzedNullabilityThroughConversionGroup(TypeWithState resultType, BoundConversion? conversionOpt, BoundExpression convertedNode)
        {
            var visitResult = new VisitResult(resultType, resultType.ToTypeWithAnnotations(compilation));
            var conversionGroup = conversionOpt?.ConversionGroupOpt;
            while (conversionOpt != null && conversionOpt != convertedNode)
            {
                Debug.Assert(conversionOpt.ConversionGroupOpt == conversionGroup);

                // https://github.com/dotnet/roslyn/issues/35046
                // SetAnalyzedNullability will drop the type if the visitResult.RValueType.Type differs from conversionOpt.Type.
                // (It will use the top-level nullability from visitResult, though.)
                //
                // Here, the visitResult represents the result of visiting the operand.
                // Ideally, we would use the visitResult to reinfer the types of the containing conversions, and store those results here.
                SetAnalyzedNullability(conversionOpt, visitResult);

                conversionOpt = conversionOpt.Operand as BoundConversion;
            }
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
            ParameterSymbol? parameterOpt,
            bool reportWarnings,
            bool fromExplicitCast,
            Location diagnosticLocation)
        {
            Debug.Assert(operandType.Type is object);
            Debug.Assert(diagnosticLocation != null);
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            var conversion = _conversions.ClassifyStandardConversion(operandType.Type, targetType.Type, ref discardedUseSiteInfo);
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
                diagnosticLocation: diagnosticLocation);
        }

        public override BoundNode? VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            Debug.Assert(node.Type.IsDelegateType());

            if (node.MethodOpt?.OriginalDefinition is LocalFunctionSymbol localFunc)
            {
                VisitLocalFunctionUse(localFunc);
            }

            Action<NamedTypeSymbol>? analysisCompletion;

            var delegateType = (NamedTypeSymbol)node.Type;
            switch (node.Argument)
            {
                case BoundMethodGroup group:
                    {
                        analysisCompletion = visitMethodGroupArgument(node, delegateType, group);
                    }
                    break;
                case BoundLambda lambda:
                    {
                        analysisCompletion = visitLambdaArgument(delegateType, lambda, node.WasTargetTyped);
                    }
                    break;
                case BoundExpression arg when arg.Type is { TypeKind: TypeKind.Delegate }:
                    {
                        analysisCompletion = visitDelegateArgument(delegateType, arg, node.WasTargetTyped);
                    }
                    break;
                default:
                    VisitRvalue(node.Argument);
                    analysisCompletion = null;
                    break;
            }

            TypeWithState result = setAnalyzedNullability(node, delegateType, analysisCompletion, node.WasTargetTyped);
            SetResultType(node, result, updateAnalyzedNullability: false);
            return null;

            TypeWithState setAnalyzedNullability(BoundDelegateCreationExpression node, NamedTypeSymbol delegateType, Action<NamedTypeSymbol>? analysisCompletion, bool isTargetTyped)
            {
                var result = TypeWithState.Create(delegateType, NullableFlowState.NotNull);

                if (isTargetTyped)
                {
                    setAnalyzedNullabilityAsContinuation(node, analysisCompletion);
                }
                else
                {
                    Debug.Assert(analysisCompletion is null);
                    SetAnalyzedNullability(node, result);
                }

                return result;
            }

            void setAnalyzedNullabilityAsContinuation(BoundDelegateCreationExpression node, Action<NamedTypeSymbol>? analysisCompletion)
            {
                TargetTypedAnalysisCompletion[node] =
                    (TypeWithAnnotations resultTypeWithAnnotations) =>
                    {
                        Debug.Assert(TypeSymbol.Equals(resultTypeWithAnnotations.Type, node.Type, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));
                        var delegateType = (NamedTypeSymbol)resultTypeWithAnnotations.Type;

                        analysisCompletion?.Invoke(delegateType);

                        return setAnalyzedNullability(node, delegateType, analysisCompletion: null, isTargetTyped: false);
                    };
            }

            Action<NamedTypeSymbol>? visitMethodGroupArgument(BoundDelegateCreationExpression node, NamedTypeSymbol delegateType, BoundMethodGroup group)
            {
                VisitMethodGroup(group);
                SetAnalyzedNullability(group, default);

                return analyzeMethodGroupConversion(node, delegateType, group, node.WasTargetTyped);
            }

            Action<NamedTypeSymbol>? analyzeMethodGroupConversion(BoundDelegateCreationExpression node, NamedTypeSymbol delegateType, BoundMethodGroup group, bool isTargetTyped)
            {
                if (isTargetTyped)
                {
                    return analyzeMethodGroupConversionAsContinuation(node, group);
                }

                var method = node.MethodOpt;
                if (method is object &&
                    delegateType.DelegateInvokeMethod is { } delegateInvokeMethod)
                {
                    method = CheckMethodGroupReceiverNullability(group, delegateInvokeMethod.Parameters, method, node.IsExtensionMethod);
                    if (!group.IsSuppressed)
                    {
                        ReportNullabilityMismatchWithTargetDelegate(group.Syntax.Location, delegateType, delegateInvokeMethod, method, node.IsExtensionMethod);
                    }
                }

                return null;
            }

            Action<NamedTypeSymbol>? analyzeMethodGroupConversionAsContinuation(BoundDelegateCreationExpression node, BoundMethodGroup group)
            {
                return (NamedTypeSymbol delegateType) =>
                {
                    analyzeMethodGroupConversion(node, delegateType, group, isTargetTyped: false);
                };
            }

            Action<NamedTypeSymbol>? visitLambdaArgument(NamedTypeSymbol delegateType, BoundLambda lambda, bool isTargetTyped)
            {
                SetNotNullResult(lambda);

                return analyzeLambdaConversion(delegateType, lambda, isTargetTyped);
            }

            Action<NamedTypeSymbol>? analyzeLambdaConversion(NamedTypeSymbol delegateType, BoundLambda lambda, bool isTargetTyped)
            {
                if (isTargetTyped)
                {
                    return analyzeLambdaConversionAsContinuation(lambda);
                }

                VisitLambda(lambda, delegateType);

                if (!lambda.IsSuppressed)
                {
                    ReportNullabilityMismatchWithTargetDelegate(((LambdaSymbol)lambda.Symbol).DiagnosticLocation, delegateType, lambda);
                }

                return null;
            }

            Action<NamedTypeSymbol> analyzeLambdaConversionAsContinuation(BoundLambda lambda)
            {
                return (NamedTypeSymbol delegateType) => analyzeLambdaConversion(delegateType, lambda, isTargetTyped: false);
            }

            Action<NamedTypeSymbol>? visitDelegateArgument(NamedTypeSymbol delegateType, BoundExpression arg, bool isTargetTyped)
            {
                Debug.Assert(arg.Type is not null);
                TypeSymbol argType = arg.Type;
                var argTypeWithAnnotations = TypeWithAnnotations.Create(argType, NullableAnnotation.NotAnnotated);
                var argState = VisitRvalueWithState(arg);
                ReportNullableAssignmentIfNecessary(arg, argTypeWithAnnotations, argState, useLegacyWarnings: false);

                // Delegate creation will throw an exception if the argument is null
                LearnFromNonNullTest(arg, ref State);

                return analyzeDelegateConversion(delegateType, arg, isTargetTyped);
            }

            Action<NamedTypeSymbol>? analyzeDelegateConversion(NamedTypeSymbol delegateType, BoundExpression arg, bool isTargetTyped)
            {
                if (isTargetTyped)
                {
                    return analyzeDelegateConversionAsContinuation(arg);
                }

                Debug.Assert(arg.Type is not null);
                TypeSymbol argType = arg.Type;

                if (!arg.IsSuppressed &&
                    delegateType.DelegateInvokeMethod is { } delegateInvokeMethod &&
                    argType.DelegateInvokeMethod() is { } argInvokeMethod)
                {
                    ReportNullabilityMismatchWithTargetDelegate(arg.Syntax.Location, delegateType, delegateInvokeMethod, argInvokeMethod, invokedAsExtensionMethod: false);
                }

                return null;
            }

            Action<NamedTypeSymbol> analyzeDelegateConversionAsContinuation(BoundExpression arg)
            {
                return (NamedTypeSymbol delegateType) => analyzeDelegateConversion(delegateType, arg, isTargetTyped: false);
            }
        }

        public override BoundNode? VisitMethodGroup(BoundMethodGroup node)
        {
            Debug.Assert(!IsConditionalState);

            var receiverOpt = node.ReceiverOpt;
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

        private bool TryGetMethodGroupReceiverNullability([NotNullWhen(true)] BoundExpression? receiverOpt, out TypeWithState type)
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

        private MethodSymbol CheckMethodGroupReceiverNullability(BoundMethodGroup group, ImmutableArray<ParameterSymbol> parameters, MethodSymbol method, bool invokedAsExtensionMethod)
        {
            var receiverOpt = group.ReceiverOpt;
            bool isExtensionBlockMethod = method.IsExtensionBlockMember();

            if (TryGetMethodGroupReceiverNullability(receiverOpt, out TypeWithState receiverType))
            {
                var syntax = group.Syntax;
                if (!invokedAsExtensionMethod && !isExtensionBlockMethod)
                {
                    method = (MethodSymbol)AsMemberOfType(receiverType.Type, method);
                }

                if (method.GetMemberArityIncludingExtension() != 0 && HasImplicitTypeArguments(group.Syntax))
                {
                    var arguments = ArrayBuilder<BoundExpression>.GetInstance();
                    if (invokedAsExtensionMethod || isExtensionBlockMethod)
                    {
                        arguments.Add(CreatePlaceholderIfNecessary(receiverOpt, receiverType.ToTypeWithAnnotations(compilation)));
                    }

                    // Create placeholders for the arguments. (See Conversions.GetDelegateArguments()
                    // which is used for that purpose in initial binding.)
                    foreach (var parameter in parameters)
                    {
                        var parameterType = parameter.TypeWithAnnotations;
                        arguments.Add(new BoundExpressionWithNullability(syntax, new BoundParameter(syntax, parameter), parameterType.NullableAnnotation, parameterType.Type));
                    }

                    Debug.Assert(_binder is object);
                    method = InferMemberTypeArguments(method, arguments.ToImmutableAndFree(), argumentRefKindsOpt: default, argsToParamsOpt: default, expanded: false);
                }

                if (!isExtensionBlockMethod || !method.IsStatic)
                {
                    if (invokedAsExtensionMethod || isExtensionBlockMethod)
                    {
                        ParameterSymbol? receiverParameter = isExtensionBlockMethod ? method.ContainingType.ExtensionParameter : method.Parameters[0];
                        Debug.Assert(receiverParameter is not null);

                        CheckExtensionMethodThisNullability(receiverOpt, Conversion.Identity, receiverParameter, receiverType);
                    }
                    else
                    {
                        CheckPossibleNullReceiver(receiverOpt, receiverType, checkNullableValueType: false);
                    }
                }

                if (ConstraintsHelper.RequiresChecking(method))
                {
                    CheckMethodConstraints(syntax, method);
                }
            }
            return method;
        }

        public override BoundNode? VisitLambda(BoundLambda node)
        {
            var stateForLambda = this.State.Clone();
            // Lambda bodies are usually visited in VisitConversion (we need to know the target delegate type),
            // but in erroneous code, the lambda-to-delegate conversion might be missing, then we visit the lambda here.
            if (!node.InAnonymousFunctionConversion)
            {
                VisitLambda(node, delegateTypeOpt: null);
            }

            SetNotNullResultForLambda(node, stateForLambda);
            return null;
        }

        private void VisitLambda(BoundLambda node, NamedTypeSymbol? delegateTypeOpt, Optional<LocalState> initialState = default)
        {
            Debug.Assert(delegateTypeOpt?.IsDelegateType() != false);

            var delegateInvokeMethod = delegateTypeOpt?.DelegateInvokeMethod;
            UseDelegateInvokeParameterAndReturnTypes(node, delegateInvokeMethod, out bool useDelegateInvokeParameterTypes, out bool useDelegateInvokeReturnType);
            if (useDelegateInvokeParameterTypes && _snapshotBuilderOpt is object)
            {
                SetUpdatedSymbol(node, node.Symbol, delegateTypeOpt!);
            }

            AnalyzeLocalFunctionOrLambda(
                node,
                node.Symbol,
                initialState.HasValue ? initialState.Value : State.Clone(),
                delegateInvokeMethod,
                useDelegateInvokeParameterTypes,
                useDelegateInvokeReturnType);
        }

        private static void UseDelegateInvokeParameterAndReturnTypes(BoundLambda lambda, MethodSymbol? delegateInvokeMethod, out bool useDelegateInvokeParameterTypes, out bool useDelegateInvokeReturnType)
        {
            if (delegateInvokeMethod is null)
            {
                useDelegateInvokeParameterTypes = false;
                useDelegateInvokeReturnType = false;
            }
            else
            {
                var unboundLambda = lambda.UnboundLambda;
                useDelegateInvokeParameterTypes = !unboundLambda.HasExplicitlyTypedParameterList;
                useDelegateInvokeReturnType = !unboundLambda.HasExplicitReturnType(out _, out _, out _);
            }
        }

        public override BoundNode? VisitUnboundLambda(UnboundLambda node)
        {
            // The presence of this node suggests an error was detected in an earlier phase.
            // Analyze the body to report any additional warnings.
            var lambda = node.BindForErrorRecovery();
            VisitLambda(lambda, delegateTypeOpt: null);
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode? VisitThisReference(BoundThisReference node)
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

        public override BoundNode? VisitParameter(BoundParameter node)
        {
            var parameter = node.ParameterSymbol;
            int slot = GetOrCreateSlot(parameter);
            var parameterType = GetDeclaredParameterResult(parameter);
            var typeWithState = GetParameterState(parameterType, parameter.FlowAnalysisAnnotations);
            SetResult(node, GetAdjustedResult(typeWithState, slot), parameterType);
            return null;
        }

        public override BoundNode? VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            Debug.Assert(!IsConditionalState);

            var left = node.Left;
            var right = node.Right;
            VisitLValue(left);
            // we may enter a conditional state for error scenarios on the LHS.
            Unsplit();

            // When a getter-only prop is assigned in a constructor, it is bound as
            // an assignment of the property even though it is really an assignment of the backing field.
            // When such a property also uses the field keyword, we want the field's annotations+attributes
            // to decide the validity of the assignment and the ones on the property itself to be ignored.
            TypeWithAnnotations leftLValueType;
            FlowAnalysisAnnotations leftAnnotations;
            if (left is BoundPropertyAccess { PropertySymbol: SourcePropertySymbolBase { SetMethod: null, UsesFieldKeyword: true } property })
            {
                var field = property.BackingField;
                leftAnnotations = field.FlowAnalysisAnnotations;
                leftLValueType = ApplyLValueAnnotations(GetTypeOrReturnTypeWithAnnotations(field), leftAnnotations);
            }
            else
            {
                leftAnnotations = GetLValueAnnotations(left);
                leftLValueType = ApplyLValueAnnotations(LvalueResultType, leftAnnotations);
            }

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
                    rightState = VisitOptionalImplicitConversion(right, targetTypeOpt: discarded ? default : leftLValueType, UseLegacyWarnings(left), trackMembers: true, AssignmentKind.Assignment);
                    Unsplit();
                }
                else
                {
                    rightState = VisitRefExpression(right, leftLValueType);
                }

                // If the LHS has annotations, we perform an additional check for nullable value types
                CheckDisallowedNullAssignment(rightState, leftAnnotations, right.Syntax);

                if (left is BoundDiscardExpression)
                {
                    var lvalueType = rightState.ToTypeWithAnnotations(compilation);
                    SetResult(left, rightState, lvalueType, isLvalue: true);
                    SetResult(node, rightState, lvalueType);
                }
                else
                {
                    SetResult(node, TypeWithState.Create(leftLValueType.Type, rightState.State), leftLValueType);
                }

                AdjustSetValue(left, ref rightState);
                TrackNullableStateForAssignment(right, leftLValueType, MakeSlot(left), rightState, MakeSlot(right));
            }

            return null;
        }

        private bool IsPropertyOutputMoreStrictThanInput(PropertySymbol property)
        {
            var type = property.TypeWithAnnotations;
            var annotations = IsAnalyzingAttribute ? FlowAnalysisAnnotations.None : property.GetFlowAnalysisAnnotations();
            var lValueType = ApplyLValueAnnotations(type, annotations);
            if (lValueType.NullableAnnotation.IsOblivious() || !lValueType.CanBeAssignedNull)
            {
                return false;
            }

            var rValueType = ApplyUnconditionalAnnotations(type.ToTypeWithState(), annotations);
            return rValueType.IsNotNull;
        }

        /// <summary>
        /// When the allowed output of a property/indexer is not-null but the allowed input is maybe-null, we store a not-null value instead.
        /// This way, assignment of a legal input value results in a legal output value.
        /// This adjustment doesn't apply to oblivious properties/indexers.
        /// </summary>
        private void AdjustSetValue(BoundExpression left, ref TypeWithState rightState)
        {
            var property = left switch
            {
                BoundPropertyAccess propAccess => propAccess.PropertySymbol,
                BoundIndexerAccess indexerAccess => indexerAccess.Indexer,
                _ => null
            };

            if (property is not null && IsPropertyOutputMoreStrictThanInput(property))
            {
                rightState = rightState.WithNotNullState();
            }
        }

        private FlowAnalysisAnnotations GetLValueAnnotations(BoundExpression expr)
        {
            Debug.Assert(expr is not BoundObjectInitializerMember);

            // Annotations are ignored when binding an attribute to avoid cycles. (Members used
            // in attributes are error scenarios, so missing warnings should not be important.)
            if (IsAnalyzingAttribute)
            {
                return FlowAnalysisAnnotations.None;
            }

            var annotations = expr switch
            {
                BoundPropertyAccess property => property.PropertySymbol.GetFlowAnalysisAnnotations(),
                BoundIndexerAccess indexer => indexer.Indexer.GetFlowAnalysisAnnotations(),
                BoundFieldAccess field => GetFieldAnnotations(field.FieldSymbol),
                BoundParameter { ParameterSymbol: ParameterSymbol parameter }
                    => ToInwardAnnotations(GetParameterAnnotations(parameter) & ~FlowAnalysisAnnotations.NotNull), // NotNull is enforced upon method exit
                _ => FlowAnalysisAnnotations.None
            };

            return annotations & (FlowAnalysisAnnotations.DisallowNull | FlowAnalysisAnnotations.AllowNull);
        }

        private static FlowAnalysisAnnotations GetFieldAnnotations(FieldSymbol field)
        {
            return field.AssociatedSymbol is SourcePropertySymbolBase { UsesFieldKeyword: false } property ?
                property.GetFlowAnalysisAnnotations() :
                field.FlowAnalysisAnnotations;
        }

        private FlowAnalysisAnnotations GetObjectInitializerMemberLValueAnnotations(Symbol memberSymbol)
        {
            // Annotations are ignored when binding an attribute to avoid cycles. (Members used
            // in attributes are error scenarios, so missing warnings should not be important.)
            if (IsAnalyzingAttribute)
            {
                return FlowAnalysisAnnotations.None;
            }

            var annotations = memberSymbol switch { PropertySymbol prop => prop.GetFlowAnalysisAnnotations(), FieldSymbol field => GetFieldAnnotations(field), _ => FlowAnalysisAnnotations.None };

            return annotations & (FlowAnalysisAnnotations.DisallowNull | FlowAnalysisAnnotations.AllowNull);
        }

        private static FlowAnalysisAnnotations ToInwardAnnotations(FlowAnalysisAnnotations outwardAnnotations)
        {
            var annotations = FlowAnalysisAnnotations.None;
            if ((outwardAnnotations & FlowAnalysisAnnotations.MaybeNull) != 0)
            {
                // MaybeNull and MaybeNullWhen count as MaybeNull
                annotations |= FlowAnalysisAnnotations.AllowNull;
            }
            if ((outwardAnnotations & FlowAnalysisAnnotations.NotNull) == FlowAnalysisAnnotations.NotNull)
            {
                // NotNullWhenTrue and NotNullWhenFalse don't count on their own. Only NotNull (ie. both flags) matters.
                annotations |= FlowAnalysisAnnotations.DisallowNull;
            }
            return annotations;
        }

        private static bool UseLegacyWarnings(BoundExpression expr)
        {
            switch (expr)
            {
                case BoundLocal { LocalSymbol.RefKind: RefKind.None }:
                case BoundParameter { ParameterSymbol: { RefKind: RefKind.None } parameter } when
                         parameter.ContainingSymbol is not SynthesizedPrimaryConstructor primaryConstructor || !primaryConstructor.GetCapturedParameters().ContainsKey(parameter):
                    return true;
                default:
                    return false;
            }
        }

        public override BoundNode? VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            return VisitDeconstructionAssignmentOperator(node, rightResultOpt: null);
        }

        private BoundNode? VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node, TypeWithState? rightResultOpt)
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

            if (!conversion.DeconstructionInfo.IsDefault)
            {
                VisitDeconstructMethodArguments(variables, conversion, right, rightResultOpt);
            }
            else
            {
                VisitTupleDeconstructionArguments(variables, conversion.DeconstructConversionInfo, right, rightResultOpt);
            }
        }

        private void VisitDeconstructMethodArguments(ArrayBuilder<DeconstructionVariable> variables, Conversion conversion, BoundExpression right, TypeWithState? rightResultOpt)
        {
            VisitRvalue(right);

            // If we were passed an explicit right result, use that rather than the visited result
            if (rightResultOpt.HasValue)
            {
                SetResultType(right, rightResultOpt.Value);
            }
            var rightResult = ResultType;
            var invocation = conversion.DeconstructionInfo.Invocation as BoundCall;

            if (invocation is { Method: { } deconstructMethod, IsErroneousNode: false })
            {
                Debug.Assert(invocation is object);
                Debug.Assert(rightResult.Type is object);

                int n = variables.Count;
                bool isExtensionBlockMethod = deconstructMethod.IsExtensionBlockMember();
                if (!invocation.InvokedAsExtensionMethod && !isExtensionBlockMethod)
                {
                    _ = CheckPossibleNullReceiver(right);

                    // update the deconstruct method with any inferred type parameters of the containing type
                    if (deconstructMethod.OriginalDefinition != deconstructMethod)
                    {
                        deconstructMethod = (MethodSymbol)AsMemberOfType(rightResult.Type, deconstructMethod);
                    }
                }
                else if (deconstructMethod.GetMemberArityIncludingExtension() != 0)
                {
                    // re-infer the deconstruct parameters based on the 'this' parameter
                    ArrayBuilder<BoundExpression> placeholderArgs = ArrayBuilder<BoundExpression>.GetInstance(n + 1);
                    placeholderArgs.Add(CreatePlaceholderIfNecessary(right, rightResult.ToTypeWithAnnotations(compilation)));
                    for (int i = 0; i < n; i++)
                    {
                        placeholderArgs.Add(new BoundExpressionWithNullability(variables[i].Expression.Syntax, variables[i].Expression, NullableAnnotation.Oblivious, conversion.DeconstructionInfo.OutputPlaceholders[i].Type));
                    }

                    var argumentRefKinds = GetArgumentRefKinds(invocation.ArgumentRefKindsOpt, isExtensionBlockMethod, deconstructMethod, argumentCount: n);
                    var argsToParams = GetArgsToParamsOpt(invocation.ArgsToParamsOpt, isExtensionBlockMethod);
                    deconstructMethod = InferMemberTypeArguments(deconstructMethod, placeholderArgs.ToImmutableAndFree(), argumentRefKinds, argsToParams, invocation.Expanded);

                    // check the constraints remain valid with the re-inferred parameter types
                    if (ConstraintsHelper.RequiresChecking(deconstructMethod))
                    {
                        CheckMethodConstraints(invocation.Syntax, deconstructMethod);
                    }
                }

                var parameters = deconstructMethod.Parameters;
                int offset = invocation.InvokedAsExtensionMethod ? 1 : 0;
                Debug.Assert(parameters.Length - offset == n);

                if (invocation.InvokedAsExtensionMethod || isExtensionBlockMethod)
                {
                    // Check nullability for `this` parameter
                    var argConversion = RemoveConversion(invocation.Arguments[0], includeExplicitConversions: false).conversion;
                    var receiverParameter = isExtensionBlockMethod ? deconstructMethod.ContainingType.ExtensionParameter : deconstructMethod.Parameters[0];
                    Debug.Assert(receiverParameter is not null);
                    CheckExtensionMethodThisNullability(right, argConversion, receiverParameter, rightResult);
                }

                for (int i = 0; i < n; i++)
                {
                    var variable = variables[i];
                    var parameter = parameters[i + offset];
                    var (placeholder, placeholderConversion) = conversion.DeconstructConversionInfo[i];
                    var underlyingConversion = BoundNode.GetConversion(placeholderConversion, placeholder);
                    var nestedVariables = variable.NestedVariables;
                    if (nestedVariables != null)
                    {
                        var nestedRight = CreatePlaceholderIfNecessary(invocation.Arguments[i + offset], parameter.TypeWithAnnotations);
                        VisitDeconstructionArguments(nestedVariables, underlyingConversion, right: nestedRight);
                    }
                    else
                    {
                        VisitArgumentConversionAndInboundAssignmentsAndPreConditions(conversionOpt: null, variable.Expression, underlyingConversion, parameter.RefKind,
                            parameter, parameter.TypeWithAnnotations, GetParameterAnnotations(parameter), new VisitResult(variable.Type.ToTypeWithState(), variable.Type),
                            conversionResultsBuilder: null, extensionMethodThisArgument: false);
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
                            new VisitResult(variable.Type.ToTypeWithState(), variable.Type),
                            notNullParametersOpt: null, compareExchangeInfoOpt: default);
                    }
                }
            }
        }

        private void VisitTupleDeconstructionArguments(ArrayBuilder<DeconstructionVariable> variables, ImmutableArray<(BoundValuePlaceholder? placeholder, BoundExpression? conversion)> deconstructConversionInfo, BoundExpression right, TypeWithState? rightResultOpt)
        {
            int n = variables.Count;
            var rightParts = GetDeconstructionRightParts(right, rightResultOpt);
            Debug.Assert(rightParts.Length == n);

            for (int i = 0; i < n; i++)
            {
                var variable = variables[i];
                var (placeholder, placeholderConversion) = deconstructConversionInfo[i];
                var underlyingConversion = BoundNode.GetConversion(placeholderConversion, placeholder);
                var rightPart = rightParts[i];
                var nestedVariables = variable.NestedVariables;
                if (nestedVariables != null)
                {
                    VisitDeconstructionArguments(nestedVariables, underlyingConversion, rightPart);
                }
                else
                {
                    var lvalueType = variable.Type;
                    var leftAnnotations = GetLValueAnnotations(variable.Expression);
                    lvalueType = ApplyLValueAnnotations(lvalueType, leftAnnotations);

                    TypeWithState operandType;
                    TypeWithState valueType;
                    int valueSlot;
                    if (underlyingConversion.IsIdentity)
                    {
                        if (variable.Expression is BoundLocal { DeclarationKind: BoundLocalDeclarationKind.WithInferredType } local)
                        {
                            // when the LHS is a var declaration, we can just visit the right part to infer the type
                            valueType = operandType = VisitRvalueWithState(rightPart);
                            _variables.SetType(local.LocalSymbol, operandType.ToAnnotatedTypeWithAnnotations(compilation));
                        }
                        else
                        {
                            operandType = default;
                            valueType = VisitOptionalImplicitConversion(rightPart, lvalueType, useLegacyWarnings: true, trackMembers: true, AssignmentKind.Assignment);
                            Unsplit();
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
                            lvalueType,
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

                    // If the LHS has annotations, we perform an additional check for nullable value types
                    CheckDisallowedNullAssignment(valueType, leftAnnotations, right.Syntax);

                    int targetSlot = MakeSlot(variable.Expression);
                    AdjustSetValue(variable.Expression, ref valueType);
                    TrackNullableStateForAssignment(rightPart, lvalueType, targetSlot, valueType, valueSlot);

                    // Conversion of T to Nullable<T> is equivalent to new Nullable<T>(t).
                    if (targetSlot > 0 &&
                        underlyingConversion.Kind == ConversionKind.ImplicitNullable &&
                        AreNullableAndUnderlyingTypes(lvalueType.Type, operandType.Type, out TypeWithAnnotations underlyingType))
                    {
                        valueSlot = MakeSlot(rightPart);
                        if (valueSlot > 0)
                        {
                            var valueBeforeNullableWrapping = TypeWithState.Create(underlyingType.Type, NullableFlowState.NotNull);
                            TrackNullableStateOfNullableValue(targetSlot, lvalueType.Type, rightPart, valueBeforeNullableWrapping, valueSlot);
                        }
                    }
                }
            }
        }

        private readonly struct DeconstructionVariable
        {
            internal readonly BoundExpression Expression;
            internal readonly TypeWithAnnotations Type;
            internal readonly ArrayBuilder<DeconstructionVariable>? NestedVariables;

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
                        VisitLValue(expr);
                        return new DeconstructionVariable(expr, LvalueResultType);
                }
            }
        }

        /// <summary>
        /// Return the sub-expressions for the righthand side of a deconstruction
        /// assignment. cf. LocalRewriter.GetRightParts.
        /// </summary>
        private ImmutableArray<BoundExpression> GetDeconstructionRightParts(BoundExpression expr, TypeWithState? rightResultOpt)
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
                                return GetDeconstructionRightParts(conv.Operand, null);
                        }
                    }
                    break;
            }
            if (rightResultOpt is { } rightResult)
            {
                expr = CreatePlaceholderIfNecessary(expr, rightResult.ToTypeWithAnnotations(compilation));
            }

            if (expr.Type is NamedTypeSymbol { IsTupleType: true } tupleType)
            {
                // https://github.com/dotnet/roslyn/issues/33011: Should include conversion.UnderlyingConversions[i].
                // For instance, Boxing conversions (see Deconstruction_ImplicitBoxingConversion_02) and
                // ImplicitNullable conversions (see Deconstruction_ImplicitNullableConversion_02).
                var fields = tupleType.TupleElements;
                return fields.SelectAsArray((f, e) => (BoundExpression)new BoundFieldAccess(e.Syntax, e, f, constantValueOpt: null), expr);
            }

            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode? VisitIncrementOperator(BoundIncrementOperator node)
        {
            Debug.Assert(!IsConditionalState);

            if (node.MethodOpt is { } method ?
                    !method.IsStatic :
                    (!node.OriginalUserDefinedOperatorsOpt.IsDefaultOrEmpty && !node.OriginalUserDefinedOperatorsOpt[0].IsStatic))
            {
                if (node.MethodOpt is { } instanceMethod)
                {
                    int extensionReceiverSlot = -1;

                    if (instanceMethod.IsExtensionBlockMember())
                    {
                        extensionReceiverSlot = MakeSlot(node.Operand) is > 0 and int slot ? slot : GetOrCreatePlaceholderSlot(node.Operand);
                    }

                    TypeWithState receiverType = VisitAndCheckReceiver(node.Operand, instanceMethod);
                    (instanceMethod, ImmutableArray<VisitResult> results, bool returnNotNull) = ReInferMethodAndVisitArguments(
                        node,
                        node.Operand,
                        receiverType,
                        instanceMethod,
                        arguments: [],
                        refKindsOpt: default,
                        argsToParamsOpt: default,
                        defaultArguments: default,
                        expanded: false,
                        invokedAsExtensionMethod: false);

                    if (node.Type.IsVoidType())
                    {
                        SetNotNullResult(node);
                    }
                    else if (!instanceMethod.IsExtensionBlockMember())
                    {
                        SetResultType(node, TypeWithState.Create(receiverType.Type, NullableFlowState.NotNull));
                    }
                    else if (extensionReceiverSlot > 0)
                    {
                        SetResultType(node, TypeWithState.Create(results[0].RValueType.Type, GetState(ref State, extensionReceiverSlot)));
                    }
                    else
                    {
                        SetResult(node, results[0], updateAnalyzedNullability: true, isLvalue: false);
                    }

                    SetUpdatedSymbol(node, node.MethodOpt, instanceMethod);
                }
                else
                {
                    // An error case
                    var opType = VisitRvalueWithState(node.Operand);

                    if (node.Type.IsVoidType())
                    {
                        SetNotNullResult(node);
                    }
                    else
                    {
                        SetResultType(node, TypeWithState.Create(opType.Type, NullableFlowState.NotNull));
                    }
                }

                return null;
            }

            var operandType = VisitRvalueWithState(node.Operand);
            var operandLvalue = LvalueResultType;
            bool setResult = false;

            if (this.State.Reachable)
            {
                bool isLifted = node.OperatorKind.IsLifted();
                MethodSymbol? incrementOperator = (node.OperatorKind.IsUserDefined() && node.MethodOpt?.ParameterCount == 1) ? node.MethodOpt : null;

                // Update increment method based on operand type.
                if (incrementOperator is not null)
                {
                    incrementOperator = ReInferUnaryOperator(node.Syntax, incrementOperator, node.Operand, GetNullableUnderlyingTypeIfNecessary(isLifted, operandType));
                    SetUpdatedSymbol(node, node.MethodOpt!, incrementOperator);
                }

                TypeWithAnnotations targetTypeOfOperandConversion;
                AssignmentKind assignmentKind = AssignmentKind.Assignment;
                ParameterSymbol? parameter = null;

                // Analyze operator call properly (honoring [Disallow|Allow|Maybe|NotNull] attribute annotations) https://github.com/dotnet/roslyn/issues/32671
                // https://github.com/dotnet/roslyn/issues/29961 Update conversion method based on operand type.
                if (node.OperandConversion is BoundConversion { Conversion: var operandConversion } && operandConversion.IsUserDefined && operandConversion.Method?.ParameterCount == 1)
                {
                    targetTypeOfOperandConversion = operandConversion.Method.ReturnTypeWithAnnotations;
                }
                else if (incrementOperator is object)
                {
                    targetTypeOfOperandConversion = incrementOperator.Parameters[0].TypeWithAnnotations;

                    if (isLifted)
                    {
                        targetTypeOfOperandConversion = TypeWithAnnotations.Create(MakeNullableOf(targetTypeOfOperandConversion));
                    }

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
                        BoundNode.GetConversion(node.OperandConversion, node.OperandPlaceholder),
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
                    resultOfIncrementType = GetLiftedReturnTypeIfNecessary(isLifted, incrementOperator.ReturnTypeWithAnnotations, operandType.State);
                }

                var operandTypeWithAnnotations = operandType.ToTypeWithAnnotations(compilation);
                resultOfIncrementType = VisitConversion(
                    conversionOpt: null,
                    node,
                    BoundNode.GetConversion(node.ResultConversion, node.ResultPlaceholder),
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

        public override BoundNode? VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            Debug.Assert(!IsConditionalState);

            if (node.Operator.Method is { } method ?
                    !method.IsStatic :
                    (!node.OriginalUserDefinedOperatorsOpt.IsDefaultOrEmpty && !node.OriginalUserDefinedOperatorsOpt[0].IsStatic))
            {
                if (node.Operator.Method is { } instanceMethod)
                {
                    int extensionReceiverSlot = -1;

                    if (instanceMethod.IsExtensionBlockMember())
                    {
                        extensionReceiverSlot = MakeSlot(node.Left) is > 0 and int slot ? slot : GetOrCreatePlaceholderSlot(node.Left);
                    }

                    TypeWithState receiverType = VisitAndCheckReceiver(node.Left, instanceMethod);
                    (instanceMethod, ImmutableArray<VisitResult> results, bool returnNotNull) = ReInferMethodAndVisitArguments(
                        node,
                        node.Left,
                        receiverType,
                        instanceMethod,
                        arguments: [node.Right],
                        refKindsOpt: default,
                        argsToParamsOpt: default,
                        defaultArguments: default,
                        expanded: false,
                        invokedAsExtensionMethod: false);

                    if (node.Type.IsVoidType())
                    {
                        SetNotNullResult(node);
                    }
                    else if (!instanceMethod.IsExtensionBlockMember())
                    {
                        SetResultType(node, TypeWithState.Create(receiverType.Type, NullableFlowState.NotNull));
                    }
                    else if (extensionReceiverSlot > 0)
                    {
                        SetResultType(node, TypeWithState.Create(results[0].RValueType.Type, GetState(ref State, extensionReceiverSlot)));
                    }
                    else
                    {
                        SetResult(node, results[0], updateAnalyzedNullability: true, isLvalue: false);
                    }

                    SetUpdatedSymbol(node, node.Operator.Method, instanceMethod);
                }
                else
                {
                    // An error case
                    Visit(node.Left);
                    var opType = ResultType;
                    Unsplit();

                    VisitRvalue(node.Right);

                    if (node.Type.IsVoidType())
                    {
                        SetNotNullResult(node);
                    }
                    else
                    {
                        SetResultType(node, TypeWithState.Create(opType.Type, NullableFlowState.NotNull));
                    }
                }

                return null;
            }

            // visit 'x'
            Visit(node.Left);
            Unsplit();
            var leftTypeWithState = ResultType;
            var leftLvalueType = LvalueResultType;

            // note: LHS keeps a conversion+placeholder separately, because it is used
            // for both a read and a write, unlike the right side, which is only read.
            var (rightConversionOperand, rightConversion) = RemoveConversion(node.Right, includeExplicitConversions: false);

            // visit 'y'
            var rightTypeWithState = VisitRvalueWithState(rightConversionOperand);

            // for an operator like: 'TResult operator op(TLeftParam left, TRightParam right);'
            // and usage like: 'x op= y;'
            // expansion is (roughly): 'x = (TLeftArg)((TLeftParam)x op (TRightParam)y);'

            // visit '(TLeftParam)x op (TRightParam)y'
            var resultTypeWithState = ReinferAndVisitBinaryOperator(
                node,
                node.Operator.Kind,
                node.Operator.Method,
                node.Operator.ReturnType ?? node.Type,
                node.LeftConversion as BoundConversion ?? node.Left,
                node.Left,
                BoundNode.GetConversion(node.LeftConversion, node.LeftPlaceholder),
                leftTypeWithState,
                node.Right,
                rightConversionOperand,
                rightConversion,
                rightTypeWithState);

            var leftArgumentAnnotations = GetLValueAnnotations(node.Left);
            leftLvalueType = ApplyLValueAnnotations(leftLvalueType, leftArgumentAnnotations);

            // visit '(TLeftArg)((TLeftParam)x op (TRightParam)y)'
            resultTypeWithState = VisitConversion(
                conversionOpt: null,
                conversionOperand: node,
                BoundNode.GetConversion(node.FinalConversion, node.FinalPlaceholder),
                targetTypeWithNullability: leftLvalueType,
                operandType: resultTypeWithState,
                checkConversion: true,
                fromExplicitCast: false,
                useLegacyWarnings: false,
                assignmentKind: AssignmentKind.Assignment);

            // visit 'x = (TLeftArg)((TLeftParam)x op (TRightParam)y)'
            // Handle `[DisallowNull]` on LHS operand (final assignment target).
            CheckDisallowedNullAssignment(resultTypeWithState, leftArgumentAnnotations, node.Syntax);

            SetResultType(node, resultTypeWithState);

            AdjustSetValue(node.Left, ref resultTypeWithState);
            Debug.Assert(MakeSlot(node) == -1);
            TrackNullableStateForAssignment(node, leftLvalueType, MakeSlot(node.Left), resultTypeWithState);

            return null;
        }

        public override BoundNode? VisitFixedLocalCollectionInitializer(BoundFixedLocalCollectionInitializer node)
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

        public override BoundNode? VisitAddressOfOperator(BoundAddressOfOperator node)
        {
            Visit(node.Operand);
            SetNotNullResult(node);
            return null;
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

        private void ReportNullabilityMismatchInArgument(Location argumentLocation, TypeSymbol argumentType, ParameterSymbol? parameterOpt, TypeSymbol parameterType, bool forOutput)
        {
            ReportDiagnostic(forOutput ? ErrorCode.WRN_NullabilityMismatchInArgumentForOutput : ErrorCode.WRN_NullabilityMismatchInArgument,
                argumentLocation, argumentType,
                parameterOpt?.Type.IsNonNullableValueType() == true && parameterType.IsNullableType() ? parameterOpt.Type : parameterType, // Compensate for operator lifting
                GetParameterAsDiagnosticArgument(parameterOpt),
                GetContainingSymbolAsDiagnosticArgument(parameterOpt));
        }

        private TypeWithAnnotations GetDeclaredLocalResult(LocalSymbol local)
        {
            return _variables.TryGetType(local, out TypeWithAnnotations type) ?
                type :
                local.TypeWithAnnotations;
        }

        private TypeWithAnnotations GetDeclaredParameterResult(ParameterSymbol parameter)
        {
            return _variables.TryGetType(parameter, out TypeWithAnnotations type) ?
                type :
                parameter.TypeWithAnnotations;
        }

        public override BoundNode? VisitBaseReference(BoundBaseReference node)
        {
            VisitThisOrBaseReference(node);
            return null;
        }

        public override BoundNode? VisitFieldAccess(BoundFieldAccess node)
        {
            var updatedSymbol = VisitMemberAccess(node, node.ReceiverOpt, node.FieldSymbol);

            SplitIfBooleanConstant(node);
            SetUpdatedSymbol(node, node.FieldSymbol, updatedSymbol);
            return null;
        }

        private (PropertySymbol updatedProperty, bool returnNotNull) ReInferAndVisitExtensionPropertyAccess(BoundNode node, PropertySymbol property, BoundExpression receiver)
        {
            Debug.Assert(property.IsExtensionBlockMember());
            ImmutableArray<BoundExpression> arguments = [receiver];

            var extensionParameter = property.ContainingType.ExtensionParameter;
            Debug.Assert(extensionParameter is not null);
            ImmutableArray<ParameterSymbol> parameters = [extensionParameter];

            ImmutableArray<RefKind> refKindsOpt = extensionParameter.RefKind == RefKind.Ref ? [RefKind.Ref] : default;

            // Tracked by https://github.com/dotnet/roslyn/issues/37238 : properties/indexers should account for NotNullIfNotNull
            var (updatedProperty, _, returnNotNull) = VisitArguments(node, arguments, refKindsOpt, parameters, default, defaultArguments: default,
                expanded: false, invokedAsExtensionMethod: false, property, firstArgumentResult: null);

            Debug.Assert(updatedProperty is not null);
            return (updatedProperty, returnNotNull);
        }

        public override BoundNode? VisitPropertyAccess(BoundPropertyAccess node)
        {
            var property = node.PropertySymbol;
            Symbol? updatedProperty;

            if (property.IsExtensionBlockMember())
            {
                Debug.Assert(node.ReceiverOpt is not null);
                (updatedProperty, _) = ReInferAndVisitExtensionPropertyAccess(node, property, node.ReceiverOpt);

                TypeWithAnnotations typeWithAnnotations = GetTypeOrReturnTypeWithAnnotations(updatedProperty);
                FlowAnalysisAnnotations memberAnnotations = GetRValueAnnotations(updatedProperty);
                TypeWithState typeWithState = ApplyUnconditionalAnnotations(typeWithAnnotations.ToTypeWithState(), memberAnnotations);

                SetResult(node, typeWithState, typeWithAnnotations);
            }
            else
            {
                updatedProperty = VisitMemberAccess(node, node.ReceiverOpt, property);
            }

            if (!IsAnalyzingAttribute)
            {
                if (_expressionIsRead)
                {
                    ApplyMemberPostConditions(node.ReceiverOpt, property.GetMethod);
                }
                else
                {
                    ApplyMemberPostConditions(node.ReceiverOpt, property.SetMethod);
                }
            }

            SetUpdatedSymbol(node, property, updatedProperty);
            return null;
        }

        public override BoundNode? VisitIndexerAccess(BoundIndexerAccess node)
        {
            var receiverOpt = node.ReceiverOpt;
            var receiverType = VisitRvalueWithState(receiverOpt).Type;
            // https://github.com/dotnet/roslyn/issues/30598: Mark receiver as not null
            // after indices have been visited, and only if the receiver has not changed.
            // Tracked by https://github.com/dotnet/roslyn/issues/78829: add support for indexers
            _ = CheckPossibleNullReceiver(receiverOpt);

            var indexer = node.Indexer;
            if (receiverType is object)
            {
                // Update indexer based on inferred receiver type.
                indexer = (PropertySymbol)AsMemberOfType(receiverType, indexer);
            }

            VisitArguments(node, node.Arguments, node.ArgumentRefKindsOpt, indexer, node.ArgsToParamsOpt, node.DefaultArguments, node.Expanded);

            var resultType = ApplyUnconditionalAnnotations(indexer.TypeWithAnnotations.ToTypeWithState(), GetRValueAnnotations(indexer));
            SetResult(node, resultType, indexer.TypeWithAnnotations);
            SetUpdatedSymbol(node, node.Indexer, indexer);
            return null;
        }

        public override BoundNode? VisitImplicitIndexerAccess(BoundImplicitIndexerAccess node)
        {
            VisitRvalue(node.Receiver);
            var receiverResult = _visitResult;
            VisitRvalue(node.Argument);

            AddPlaceholderReplacement(node.ReceiverPlaceholder, node.Receiver, receiverResult);
            VisitRvalue(node.IndexerOrSliceAccess);
            RemovePlaceholderReplacement(node.ReceiverPlaceholder);

            SetResult(node, ResultType, LvalueResultType);
            return null;
        }

        public override BoundNode? VisitImplicitIndexerValuePlaceholder(BoundImplicitIndexerValuePlaceholder node)
        {
            // These placeholders don't need to be replaced because we know they are always not-null
            AssertPlaceholderAllowedWithoutRegistration(node);
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode? VisitImplicitIndexerReceiverPlaceholder(BoundImplicitIndexerReceiverPlaceholder node)
        {
            VisitPlaceholderWithReplacement(node);
            return null;
        }

        public override BoundNode? VisitCollectionExpressionSpreadExpressionPlaceholder(BoundCollectionExpressionSpreadExpressionPlaceholder node)
        {
            VisitPlaceholderWithReplacement(node);
            return null;
        }

        public override BoundNode? VisitValuePlaceholder(BoundValuePlaceholder node)
        {
            VisitPlaceholderWithReplacement(node);
            return null;
        }

        public override BoundNode? VisitCollectionBuilderElementsPlaceholder(BoundCollectionBuilderElementsPlaceholder node)
        {
            VisitPlaceholderWithReplacement(node);
            return null;
        }

        public override BoundNode? VisitEventAccess(BoundEventAccess node)
        {
            var updatedSymbol = VisitMemberAccess(node, node.ReceiverOpt, node.EventSymbol);
            SetUpdatedSymbol(node, node.EventSymbol, updatedSymbol);
            return null;
        }

        private Symbol VisitMemberAccess(BoundExpression node, BoundExpression? receiverOpt, Symbol member)
        {
            Debug.Assert(!IsConditionalState);
            Debug.Assert(!member.IsExtensionBlockMember());

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

            var type = GetTypeOrReturnTypeWithAnnotations(member);
            var memberAnnotations = GetRValueAnnotations(member);
            var resultType = ApplyUnconditionalAnnotations(type.ToTypeWithState(), memberAnnotations);

            // We are supposed to track information for the node. Use whatever we managed to
            // accumulate so far.
            if (PossiblyNullableType(resultType.Type))
            {
                int slot = MakeMemberSlot(receiverOpt, member);
                if (slot > 0)
                {
                    var state = GetState(ref this.State, slot);
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
                    SetState(ref this.StateWhenTrue, containingSlot, NullableFlowState.NotNull);
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

        private int GetNullableOfTValueSlot(TypeSymbol containingType, int containingSlot, out Symbol? valueProperty, bool forceSlotEvenIfEmpty = false)
        {
            Debug.Assert(containingType.IsNullableType());
            Debug.Assert(TypeSymbol.Equals(NominalSlotType(containingSlot), containingType, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

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
                Visit(node.EnumeratorInfoOpt?.MoveNextAwaitableInfo);
                return;
            }

            var (expr, conversion) = RemoveConversion(node.Expression, includeExplicitConversions: false);
            SnapshotWalkerThroughConversionGroup(node.Expression, expr);

            VisitForEachExpression(
                node,
                node.Expression,
                conversion,
                expr,
                node.EnumeratorInfoOpt);
        }

        private void VisitForEachExpression(
            BoundNode node,
            BoundExpression collectionExpression,
            Conversion conversion,
            BoundExpression expr,
            ForEachEnumeratorInfo? enumeratorInfoOpt)
        {
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
            //       nested nullability of type parameters. See ForEach_22 for a concrete example of this.
            //    5. The collection type implements IEnumerable (non-generic). Because this version isn't generic, we don't need to
            //       do any reinference, and the existing conversion can stand as is.
            //    6. The target framework's System.String doesn't implement IEnumerable. This is a compat case: System.String normally
            //       does implement IEnumerable, but there are certain target frameworks where this isn't the case. The compiler will
            //       still emit code for foreach in these scenarios.
            //    7. The collection type implements the GetEnumerator pattern via an extension GetEnumerator. For this, there will be
            //       conversion to the parameter of the extension method.
            //    8. Some binding error occurred, and some other error has already been reported. Usually this doesn't have any kind
            //       of conversion on top, but if there was an explicit conversion in code then we could get past the initial check
            //       for a BoundConversion node.

            var resultTypeWithState = VisitRvalueWithState(expr);
            var resultType = resultTypeWithState.Type;
            Debug.Assert(resultType is object);

            SetAnalyzedNullability(expr, _visitResult);
            TypeWithAnnotations targetTypeWithAnnotations;

            MethodSymbol? reinferredGetEnumeratorMethod = null;

            if (enumeratorInfoOpt?.GetEnumeratorInfo is { } enumeratorMethodInfo
                && (enumeratorMethodInfo.Method.IsExtensionMethod || enumeratorMethodInfo.Method.IsExtensionBlockMember()))
            {
                // this is case 7
                // We do not need to do this same analysis for non-extension methods because they do not have generic parameters that
                // can be inferred from usage like extension methods can. We don't warn about default arguments at the call site, so
                // there's nothing that can be learned from the non-extension case.
                (reinferredGetEnumeratorMethod, var results, _) = ReInferMethodAndVisitArguments(
                    node: node,
                    receiverOpt: expr,
                    receiverType: resultTypeWithState,
                    method: enumeratorMethodInfo.Method,
                    arguments: enumeratorMethodInfo.Arguments,
                    refKindsOpt: default,
                    argsToParamsOpt: default,
                    defaultArguments: enumeratorMethodInfo.DefaultArguments,
                    expanded: enumeratorMethodInfo.Expanded,
                    invokedAsExtensionMethod: true,
                    firstArgumentResult: _visitResult);

                targetTypeWithAnnotations = results[0].LValueType;
            }
            else if (conversion.IsIdentity ||
                (conversion.Kind == ConversionKind.ExplicitReference && resultType.SpecialType == SpecialType.System_String))
            {
                // This is case 3 or 6.
                targetTypeWithAnnotations = resultTypeWithState.ToTypeWithAnnotations(compilation);
            }
            else if (conversion.IsImplicit)
            {
                bool isAsync = enumeratorInfoOpt?.MoveNextAwaitableInfo != null;
                if (collectionExpression.Type!.SpecialType == SpecialType.System_Collections_IEnumerable)
                {
                    // If this is a conversion to IEnumerable (non-generic), nothing to do. This is cases 1, 2, and 5.
                    targetTypeWithAnnotations = TypeWithAnnotations.Create(collectionExpression.Type);
                }
                else if (ForEachLoopBinder.IsIEnumerableT(collectionExpression.Type.OriginalDefinition, isAsync, compilation))
                {
                    // This is case 4. We need to look for the IEnumerable<T> that this reinferred expression implements,
                    // so that we pick up any nested type substitutions that could have occurred.
                    var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    targetTypeWithAnnotations = TypeWithAnnotations.Create(ForEachLoopBinder.GetIEnumerableOfT(resultType, isAsync, compilation, ref discardedUseSiteInfo, out bool foundMultiple, needSupportForRefStructInterfaces: out _));
                    Debug.Assert(!foundMultiple);
                    Debug.Assert(targetTypeWithAnnotations.HasType);
                }
                else
                {
                    // This is case 8. There was not a successful binding, as a successful binding will _always_ generate one of the
                    // above conversions. Just return, as we want to suppress further errors.
                    Debug.Assert(node.HasErrors);
                    return;
                }
            }
            else
            {
                // This is also case 8.
                Debug.Assert(node.HasErrors);
                return;
            }

            var convertedResult = VisitConversion(
                GetConversionIfApplicable(collectionExpression, expr),
                expr,
                conversion,
                targetTypeWithAnnotations,
                resultTypeWithState,
                checkConversion: true,
                fromExplicitCast: false,
                useLegacyWarnings: false,
                AssignmentKind.Assignment);

            bool reportedDiagnostic = enumeratorInfoOpt?.GetEnumeratorInfo.Method is { } getEnumeratorMethod
                    && (getEnumeratorMethod.IsExtensionMethod || getEnumeratorMethod.IsExtensionBlockMember())
                ? false
                : CheckPossibleNullReceiver(expr);

            SetAnalyzedNullability(collectionExpression, new VisitResult(convertedResult, convertedResult.ToTypeWithAnnotations(compilation)));

            TypeWithState currentPropertyGetterTypeWithState;

            if (enumeratorInfoOpt is null)
            {
                currentPropertyGetterTypeWithState = default;
            }
            else if (resultType is ArrayTypeSymbol arrayType)
            {
                // Even though arrays use the IEnumerator pattern, we use the array element type as the foreach target type, so
                // directly get our source type from there instead of doing method reinference.
                currentPropertyGetterTypeWithState = arrayType.ElementTypeWithAnnotations.ToTypeWithState();
            }
            else if (resultType.SpecialType == SpecialType.System_String)
            {
                // There are frameworks where System.String does not implement IEnumerable, but we still lower it to a for loop
                // using the indexer over the individual characters anyway. So the type must be not annotated char.
                currentPropertyGetterTypeWithState =
                    TypeWithAnnotations.Create(enumeratorInfoOpt.ElementType, NullableAnnotation.NotAnnotated).ToTypeWithState();
            }
            else
            {
                // Reinfer the return type of the collectionExpression.GetEnumerator().Current property, so that if
                // the collection changed nested generic types we pick up those changes.
                if (reinferredGetEnumeratorMethod is null)
                {
                    TypeSymbol? getEnumeratorType;

                    if (enumeratorInfoOpt is { InlineArraySpanType: not WellKnownType.Unknown and var wellKnownSpan })
                    {
                        Debug.Assert(wellKnownSpan is WellKnownType.System_Span_T or WellKnownType.System_ReadOnlySpan_T);
                        NamedTypeSymbol spanType = compilation.GetWellKnownType(wellKnownSpan);
                        getEnumeratorType = spanType.Construct(ImmutableArray.Create(convertedResult.Type!.TryGetInlineArrayElementField()!.TypeWithAnnotations));
                    }
                    else
                    {
                        getEnumeratorType = convertedResult.Type;
                    }

                    reinferredGetEnumeratorMethod = (MethodSymbol)AsMemberOfType(getEnumeratorType, enumeratorInfoOpt.GetEnumeratorInfo.Method);
                }

                var enumeratorReturnType = GetReturnTypeWithState(reinferredGetEnumeratorMethod);

                if (enumeratorReturnType.State != NullableFlowState.NotNull)
                {
                    if (!reportedDiagnostic && !(collectionExpression is BoundConversion { Operand: { IsSuppressed: true } }))
                    {
                        ReportDiagnostic(ErrorCode.WRN_NullReferenceReceiver, expr.Syntax.GetLocation());
                    }
                }

                var currentPropertyGetter = (MethodSymbol)AsMemberOfType(enumeratorReturnType.Type, enumeratorInfoOpt.CurrentPropertyGetter);

                currentPropertyGetterTypeWithState = ApplyUnconditionalAnnotations(
                    currentPropertyGetter.ReturnTypeWithAnnotations.ToTypeWithState(),
                    currentPropertyGetter.ReturnTypeFlowAnalysisAnnotations);

                // Analyze `await MoveNextAsync()`
                if (enumeratorInfoOpt is { MoveNextAwaitableInfo: { AwaitableInstancePlaceholder: BoundAwaitableValuePlaceholder moveNextPlaceholder } awaitMoveNextInfo })
                {
                    var moveNextAsyncMethod = (MethodSymbol)AsMemberOfType(reinferredGetEnumeratorMethod.ReturnType, enumeratorInfoOpt.MoveNextInfo.Method);

                    var result = new VisitResult(GetReturnTypeWithState(moveNextAsyncMethod), moveNextAsyncMethod.ReturnTypeWithAnnotations);
                    AddPlaceholderReplacement(moveNextPlaceholder, moveNextPlaceholder, result);
                    Visit(awaitMoveNextInfo);
                    RemovePlaceholderReplacement(moveNextPlaceholder);
                }

                // Analyze `await DisposeAsync()`
                if (enumeratorInfoOpt is { NeedsDisposal: true, DisposeAwaitableInfo: BoundAwaitableInfo awaitDisposalInfo })
                {
                    var disposalPlaceholder = awaitDisposalInfo.AwaitableInstancePlaceholder;
                    bool addedPlaceholder = false;
                    if (enumeratorInfoOpt.PatternDisposeInfo is { Method: var originalDisposeMethod }) // no statically known Dispose method if doing a runtime check
                    {
                        Debug.Assert(disposalPlaceholder is not null);
                        var disposeAsyncMethod = (MethodSymbol)AsMemberOfType(reinferredGetEnumeratorMethod.ReturnType, originalDisposeMethod);
                        var result = new VisitResult(GetReturnTypeWithState(disposeAsyncMethod), disposeAsyncMethod.ReturnTypeWithAnnotations);
                        AddPlaceholderReplacement(disposalPlaceholder, disposalPlaceholder, result);
                        addedPlaceholder = true;
                    }

                    Visit(awaitDisposalInfo);

                    if (addedPlaceholder)
                    {
                        RemovePlaceholderReplacement(disposalPlaceholder!);
                    }
                }
            }

            SetResultType(expression: null, currentPropertyGetterTypeWithState);
        }

        public override void VisitForEachIterationVariables(BoundForEachStatement node)
        {
            // ResultType should have been set by VisitForEachExpression, called just before this.
            var sourceState = node.EnumeratorInfoOpt == null ? default : ResultType;
            TypeWithAnnotations sourceType = sourceState.ToTypeWithAnnotations(compilation);

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
                VisitDeconstructionAssignmentOperator(assignment, sourceState.HasNullType ? (TypeWithState?)null : sourceState);

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
                        TypeWithState resultForType = sourceState;
                        if (iterationVariable.IsRef)
                        {
                            // foreach (ref DestinationType variable in collection)
                            if (node.Expression is not BoundConversion { Operand.IsSuppressed: true } &&
                                IsNullabilityMismatch(sourceType, destinationType))
                            {
                                var foreachSyntax = (ForEachStatementSyntax)node.Syntax;
                                ReportNullabilityMismatchInAssignment(foreachSyntax.Type, sourceType, destinationType);
                            }
                        }
                        else if (iterationVariable is SourceLocalSymbol { IsVar: true })
                        {
                            // foreach (var variable in collection)
                            destinationType = sourceState.ToAnnotatedTypeWithAnnotations(compilation);
                            _variables.SetType(iterationVariable, destinationType);
                            resultForType = destinationType.ToTypeWithState();
                        }
                        else
                        {
                            // foreach (DestinationType variable in collection)
                            // and asynchronous variants
                            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                            Conversion conversion = BoundNode.GetConversion(node.ElementConversion, node.ElementPlaceholder);
                            if (conversion.Kind == ConversionKind.NoConversion)
                            {
                                conversion = _conversions.ClassifyImplicitConversionFromType(sourceType.Type, destinationType.Type, ref discardedUseSiteInfo);
                            }

                            result = VisitConversion(
                                conversionOpt: null,
                                conversionOperand: node.IterationVariableType,
                                conversion,
                                destinationType,
                                sourceState,
                                checkConversion: true,
                                fromExplicitCast: !conversion.IsImplicit,
                                useLegacyWarnings: true,
                                AssignmentKind.ForEachIterationVariable,
                                reportTopLevelWarnings: true,
                                reportRemainingWarnings: true,
                                isSuppressed: node.Expression is BoundConversion { Operand.IsSuppressed: true },
                                diagnosticLocation: variableLocation);
                        }

                        // In non-error cases we'll only run this loop a single time. In error cases we'll set the nullability of the VariableType multiple times, but at least end up with something
                        SetAnalyzedNullability(node.IterationVariableType, new VisitResult(resultForType, destinationType), isLvalue: true);
                        state = result.State;
                    }

                    int slot = GetOrCreateSlot(iterationVariable);
                    if (slot > 0)
                    {
                        SetState(ref this.State, slot, state);
                    }
                }

            }
        }

        public override BoundNode? VisitFromEndIndexExpression(BoundFromEndIndexExpression node)
        {
            var result = base.VisitFromEndIndexExpression(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode? VisitObjectInitializerMember(BoundObjectInitializerMember node)
        {
            // Should be handled by VisitObjectCreationExpression.
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode? VisitDynamicObjectInitializerMember(BoundDynamicObjectInitializerMember node)
        {
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode? VisitBadExpression(BoundBadExpression node)
        {
            foreach (var child in node.ChildBoundNodes)
            {
                VisitBadExpressionChild(child);
            }

            var type = TypeWithAnnotations.Create(node.Type);
            SetLvalueResultType(node, type);
            return null;
        }

        private TypeWithState VisitBadExpressionChild(BoundExpression? child)
        {
            // https://github.com/dotnet/roslyn/issues/35042, we need to implement similar workarounds for object, collection, and dynamic initializers.
            if (child is BoundLambda lambda)
            {
                TakeIncrementalSnapshot(lambda);
                VisitLambda(lambda, delegateTypeOpt: null);
                VisitRvalueEpilogue(lambda);
            }
            else
            {
                VisitRvalue(child as BoundExpression);
            }

            return ResultType;
        }

        public override BoundNode? VisitTypeExpression(BoundTypeExpression node)
        {
            var result = base.VisitTypeExpression(node);

            if (node.BoundContainingTypeOpt != null)
            {
                VisitTypeExpression(node.BoundContainingTypeOpt);
            }

            SetNotNullResult(node);
            return result;
        }

        public override BoundNode? VisitTypeOrValueExpression(BoundTypeOrValueExpression node)
        {
            // These should not appear after initial binding except in error cases.
            var result = base.VisitTypeOrValueExpression(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode? VisitUnaryOperator(BoundUnaryOperator node)
        {
            Debug.Assert(!IsConditionalState);

            TypeWithState resultType;

            switch (node.OperatorKind)
            {
                case UnaryOperatorKind.BoolLogicalNegation:
                    Visit(node.Operand);
                    if (IsConditionalState)
                        SetConditionalState(StateWhenFalse, StateWhenTrue);
                    resultType = adjustForLifting(ResultType);
                    break;
                case UnaryOperatorKind.DynamicTrue:
                    // We cannot use VisitCondition, because the operand is not of type bool.
                    // Yet we want to keep the result split if it was split.  So we simply visit.
                    Visit(node.Operand);
                    resultType = adjustForLifting(ResultType);
                    break;
                case UnaryOperatorKind.DynamicLogicalNegation:
                    // We cannot use VisitCondition, because the operand is not of type bool.
                    // Yet we want to keep the result split if it was split.  So we simply visit.
                    Visit(node.Operand);
                    // If the state is split, the result is `bool` at runtime and we invert it here.
                    if (IsConditionalState)
                        SetConditionalState(StateWhenFalse, StateWhenTrue);
                    resultType = adjustForLifting(ResultType);
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
                        method = ReInferUnaryOperator(node.Syntax, method, operand, operandType);

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
                        SetUpdatedSymbol(node, node.MethodOpt, method);
                    }
                    else
                    {
                        VisitRvalue(node.Operand);
                        resultType = adjustForLifting(ResultType);
                    }
                    break;
            }

            SetResultType(node, resultType);
            return null;

            TypeWithState adjustForLifting(TypeWithState argumentResult) =>
                TypeWithState.Create(node.Type, node.OperatorKind.IsLifted() ? argumentResult.State : NullableFlowState.NotNull);
        }

        /// <summary>
        /// The <paramref name="operandType"/> must be stripped of Nullable if we are dealing with lifted form of a unary operator.
        /// </summary>
        private MethodSymbol ReInferUnaryOperator(SyntaxNode syntax, MethodSymbol method, BoundExpression operand, TypeWithState operandType)
        {
            if (!method.IsExtensionBlockMember())
            {
                method = (MethodSymbol)AsMemberOfType(operandType.Type!.StrippedType(), method);
            }
            else if (method.ContainingType.Arity != 0)
            {
                NamedTypeSymbol extension = method.OriginalDefinition.ContainingType;
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;

                var inferenceResult = MethodTypeInferrer.Infer(
                    _binder,
                    _conversions,
                    extension.TypeParameters,
                    extension,
                    method.OriginalDefinition.ParameterTypesWithAnnotations,
                    method.OriginalDefinition.ParameterRefKinds,
                    [new BoundExpressionWithNullability(operand.Syntax, operand, operandType.ToTypeWithAnnotations(compilation).NullableAnnotation, operandType.Type)],
                    ref discardedUseSiteInfo,
                    new MethodInferenceExtensions(this),
                    ordinals: null);

                if (inferenceResult.Success)
                {
                    extension = extension.Construct(inferenceResult.InferredTypeArguments);
                    method = method.OriginalDefinition.AsMember(extension);
                }

                CheckMethodConstraints(syntax, method);
            }

            return method;
        }

        public override BoundNode? VisitPointerIndirectionOperator(BoundPointerIndirectionOperator node)
        {
            var result = base.VisitPointerIndirectionOperator(node);
            var type = TypeWithAnnotations.Create(node.Type);
            SetLvalueResultType(node, type);
            return result;
        }

        public override BoundNode? VisitPointerElementAccess(BoundPointerElementAccess node)
        {
            var result = base.VisitPointerElementAccess(node);
            var type = TypeWithAnnotations.Create(node.Type);
            SetLvalueResultType(node, type);
            return result;
        }

        public override BoundNode? VisitRefTypeOperator(BoundRefTypeOperator node)
        {
            VisitRvalue(node.Operand);
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode? VisitMakeRefOperator(BoundMakeRefOperator node)
        {
            var result = base.VisitMakeRefOperator(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode? VisitRefValueOperator(BoundRefValueOperator node)
        {
            var result = base.VisitRefValueOperator(node);
            var type = TypeWithAnnotations.Create(node.Type, node.NullableAnnotation);
            SetLvalueResultType(node, type);
            return result;
        }

        protected override void VisitBinaryLogicalOperatorChildren(ArrayBuilder<BoundExpression> stack)
        {
            BoundExpression binary;
            Debug.Assert(stack.Count > 0);

            binary = stack.Pop();

            BoundExpression? leftOperand = null;
            Conversion leftConversion = Conversion.Identity;

            switch (binary)
            {
                case BoundBinaryOperator binOp:
                    VisitCondition(binOp.Left);
                    break;
                case BoundUserDefinedConditionalLogicalOperator udBinOp:
                    (leftOperand, leftConversion) = RemoveConversion(udBinOp.Left, includeExplicitConversions: false);
                    Visit(leftOperand);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(binary.Kind);
            }

            while (true)
            {
                switch (binary)
                {
                    case BoundBinaryOperator binOp:
                        afterLeftChildOfBoundBinaryOperatorHasBeenVisited(binOp);
                        break;
                    case BoundUserDefinedConditionalLogicalOperator udBinOp:
                        Debug.Assert(leftOperand is not null);
                        afterLeftChildOfBoundUserDefinedConditionalLogicalOperatorHasBeenVisited(udBinOp, leftOperand, leftConversion);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(binary.Kind);
                }

                if (stack.Count == 0)
                {
                    break;
                }

                AdjustConditionalState(binary);

                leftOperand = binary;
                leftConversion = Conversion.Identity;

                binary = stack.Pop();
            }

            static void getBinaryConditionalOperatorInfo(BinaryOperatorKind kind, out bool isAnd, out bool isBool)
            {
                BinaryOperatorKind op = kind.Operator();
                isAnd = op == BinaryOperatorKind.And;
                isBool = kind.OperandTypes() == BinaryOperatorKind.Bool;
                Debug.Assert(isAnd || op == BinaryOperatorKind.Or);
            }

            void afterLeftChildOfBoundBinaryOperatorHasBeenVisited(BoundBinaryOperator node)
            {
                Debug.Assert(IsConditionalState);
                TypeWithState leftType = ResultType;

                getBinaryConditionalOperatorInfo(node.OperatorKind, out bool isAnd, out bool isBool);

                var leftTrue = this.StateWhenTrue;
                var leftFalse = this.StateWhenFalse;
                SetState(isAnd ? leftTrue : leftFalse);

                Visit(node.Right);
                TypeWithState rightType = ResultType;
                SetResultType(node, InferResultNullability(node.OperatorKind, node.BinaryOperatorMethod, node.Type, leftType, rightType));
                AfterRightChildOfBinaryLogicalOperatorHasBeenVisited(node.Right, isAnd, isBool, ref leftTrue, ref leftFalse);
            }

            void afterLeftChildOfBoundUserDefinedConditionalLogicalOperatorHasBeenVisited(BoundUserDefinedConditionalLogicalOperator binary, BoundExpression leftOperand, Conversion leftConversion)
            {
                TypeWithState leftType = ResultType;

                Unsplit();
                Split();

                var leftTrue = this.StateWhenTrue;
                var leftFalse = this.StateWhenFalse;

                getBinaryConditionalOperatorInfo(binary.OperatorKind, out bool isAnd, out bool isBool);
                Debug.Assert(!isBool);
                SetState(isAnd ? leftTrue : leftFalse);

                var (rightOperand, rightConversion) = RemoveConversion(binary.Right, includeExplicitConversions: false);

                VisitRvalue(rightOperand);
                var rightType = ResultType;
                Unsplit();

                var rightState = State.Clone();

                // Analyze operator calls properly (honoring [Disallow|Allow|Maybe|NotNull] attribute annotations) https://github.com/dotnet/roslyn/issues/32671

                bool isLifted = binary.OperatorKind.IsLifted();
                TypeWithState leftUnderlyingType = GetNullableUnderlyingTypeIfNecessary(isLifted, leftType);
                TypeWithState rightUnderlyingType = GetNullableUnderlyingTypeIfNecessary(isLifted, rightType);

                MethodSymbol logicalOperator = binary.LogicalOperator;

                // Update method based on inferred operand type.
                MethodSymbol reInferredMethod = ReInferBinaryOperator(binary.Syntax, logicalOperator, leftOperand, rightOperand, leftUnderlyingType, rightUnderlyingType);

                SetUpdatedSymbol(binary, logicalOperator, reInferredMethod);
                logicalOperator = reInferredMethod;

                // Conversion of the left operand is actually done before the split (before the true/false check)
                var leftState = isAnd ? leftFalse : leftTrue;
                SetState(leftState);

                var parameters = logicalOperator.Parameters;
                leftType = VisitBinaryOperatorOperandConversion(binary.Left, leftOperand, leftConversion, parameters[0], leftUnderlyingType, isLifted, out _);

                // True/False call is done before the split, short-circuiting happens based on its result
                MethodSymbol trueFalseOperator = isAnd ? binary.FalseOperator : binary.TrueOperator;

                // Update operator method based on inferred operand type.
                var updatedTrueFalseOperator = ReInferUnaryOperator(leftOperand.Syntax, trueFalseOperator, binary.Left, leftType);
                SetUpdatedSymbol(binary, trueFalseOperator, updatedTrueFalseOperator);
                trueFalseOperator = updatedTrueFalseOperator;

                var trueFalseParameter = trueFalseOperator.Parameters[0];
                _ = VisitConversion(
                    conversionOpt: null,
                    binary.Left,
                    BoundNode.GetConversion(binary.TrueFalseOperandConversion, binary.TrueFalseOperandPlaceholder),
                    trueFalseParameter.TypeWithAnnotations,
                    leftType,
                    checkConversion: true,
                    fromExplicitCast: false,
                    useLegacyWarnings: false,
                    assignmentKind: AssignmentKind.Argument,
                    parameterOpt: trueFalseParameter);

                SetState(rightState);

                rightType = VisitBinaryOperatorOperandConversion(binary.Right, rightOperand, rightConversion, parameters[1], rightUnderlyingType, isLifted, out _);

                SetResultType(binary, InferResultNullability(binary.OperatorKind, logicalOperator, binary.Type, leftType, rightType));

                AfterRightChildOfBinaryLogicalOperatorHasBeenVisited(binary.Right, isAnd, isBool, ref leftTrue, ref leftFalse);
            }
        }

        protected override void AfterLeftChildOfBinaryLogicalOperatorHasBeenVisited(BoundExpression node, BoundExpression right, bool isAnd, bool isBool, ref LocalState leftTrue, ref LocalState leftFalse)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode? VisitAwaitExpression(BoundAwaitExpression node)
        {
            var result = base.VisitAwaitExpression(node);
            var awaitableInfo = node.AwaitableInfo;
            var placeholder = awaitableInfo.AwaitableInstancePlaceholder;
            Debug.Assert(placeholder is object);

            AddPlaceholderReplacement(placeholder, node.Expression, _visitResult);
            Visit(awaitableInfo);
            RemovePlaceholderReplacement(placeholder);

            if (node.Type.IsValueType || node.HasErrors || awaitableInfo.GetResult is null)
            {
                SetNotNullResult(node);
            }
            else
            {
                // It is possible for the awaiter type returned from GetAwaiter to not be a named type. e.g. it could be a type parameter.
                // Proper handling of this is additional work which only benefits a very uncommon scenario,
                // so we will just use the originally bound GetResult method in this case.
                var getResult = awaitableInfo.GetResult;
                var reinferredGetResult = _visitResult.RValueType.Type is NamedTypeSymbol taskAwaiterType
                    ? getResult.OriginalDefinition.AsMember(taskAwaiterType)
                    : getResult;

                SetResultType(node, reinferredGetResult.ReturnTypeWithAnnotations.ToTypeWithState());
            }

            return result;
        }

        public override BoundNode? VisitTypeOfOperator(BoundTypeOfOperator node)
        {
            var result = base.VisitTypeOfOperator(node);
            SetResultType(node, TypeWithState.Create(node.Type, NullableFlowState.NotNull));
            return result;
        }

        public override BoundNode? VisitMethodInfo(BoundMethodInfo node)
        {
            var result = base.VisitMethodInfo(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode? VisitFieldInfo(BoundFieldInfo node)
        {
            var result = base.VisitFieldInfo(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode? VisitDefaultLiteral(BoundDefaultLiteral node)
        {
            // Can occur in error scenarios and lambda scenarios
            var result = base.VisitDefaultLiteral(node);
            SetResultType(node, TypeWithState.Create(node.Type, NullableFlowState.MaybeDefault));
            return result;
        }

        public override BoundNode? VisitDefaultExpression(BoundDefaultExpression node)
        {
            Debug.Assert(!this.IsConditionalState);

            var result = base.VisitDefaultExpression(node);
            TypeSymbol type = node.Type;
            if (EmptyStructTypeCache.IsTrackableStructType(type))
            {
                int slot = GetOrCreatePlaceholderSlot(node);
                if (slot > 0)
                {
                    SetState(ref this.State, slot, NullableFlowState.NotNull);
                    InheritNullableStateOfTrackableStruct(type, slot, valueSlot: -1, isDefaultValue: true);
                }
            }

            SetResultType(node, TypeWithState.ForType(type));
            return result;
        }

        public override BoundNode? VisitIsOperator(BoundIsOperator node)
        {
            Debug.Assert(!this.IsConditionalState);

            var operand = node.Operand;
            var typeExpr = node.TargetType;

            VisitPossibleConditionalAccess(operand, out var conditionalStateWhenNotNull);
            Unsplit();

            LocalState stateWhenNotNull;
            if (!conditionalStateWhenNotNull.IsConditionalState)
            {
                stateWhenNotNull = conditionalStateWhenNotNull.State;
            }
            else
            {
                stateWhenNotNull = conditionalStateWhenNotNull.StateWhenTrue;
                Join(ref stateWhenNotNull, ref conditionalStateWhenNotNull.StateWhenFalse);
            }

            Debug.Assert(node.Type.SpecialType == SpecialType.System_Boolean);

            SetConditionalState(stateWhenNotNull, State);
            if (typeExpr.Type?.SpecialType == SpecialType.System_Object)
            {
                LearnFromNullTest(operand, ref StateWhenFalse);
            }

            VisitTypeExpression(typeExpr);
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode? VisitAsOperator(BoundAsOperator node)
        {
            var argumentType = VisitRvalueWithState(node.Operand);
            NullableFlowState resultState = NullableFlowState.NotNull;
            var type = node.Type;

            if (type.CanContainNull())
            {
                switch (BoundNode.GetConversion(node.OperandConversion, node.OperandPlaceholder).Kind)
                {
                    case ConversionKind.Identity:
                    case ConversionKind.ImplicitReference:
                    case ConversionKind.Boxing:
                    case ConversionKind.ImplicitNullable:
                        resultState = argumentType.State;
                        break;

                    default:
                        resultState = NullableFlowState.MaybeDefault;
                        break;
                }
            }

            VisitTypeExpression(node.TargetType);
            SetResultType(node, TypeWithState.Create(type, resultState));
            return null;
        }

        public override BoundNode? VisitSizeOfOperator(BoundSizeOfOperator node)
        {
            var result = base.VisitSizeOfOperator(node);
            VisitTypeExpression(node.SourceType);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode? VisitArgList(BoundArgList node)
        {
            var result = base.VisitArgList(node);
            Debug.Assert(node.Type.SpecialType == SpecialType.System_RuntimeArgumentHandle);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode? VisitArgListOperator(BoundArgListOperator node)
        {
            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt, parameterAnnotationsOpt: default, defaultArguments: default);
            Debug.Assert(node.Type is null);
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode? VisitLiteral(BoundLiteral node)
        {
            Debug.Assert(!IsConditionalState);
            var result = base.VisitLiteral(node);
            SetResultType(node, TypeWithState.Create(node.Type, node.Type?.CanContainNull() != false && node.ConstantValueOpt?.IsNull == true ? NullableFlowState.MaybeDefault : NullableFlowState.NotNull));
            return result;
        }

        public override BoundNode? VisitUtf8String(BoundUtf8String node)
        {
            Debug.Assert(!IsConditionalState);
            var result = base.VisitUtf8String(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode? VisitPreviousSubmissionReference(BoundPreviousSubmissionReference node)
        {
            var result = base.VisitPreviousSubmissionReference(node);
            Debug.Assert(node.WasCompilerGenerated);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode? VisitHostObjectMemberReference(BoundHostObjectMemberReference node)
        {
            var result = base.VisitHostObjectMemberReference(node);
            Debug.Assert(node.WasCompilerGenerated);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode? VisitPseudoVariable(BoundPseudoVariable node)
        {
            var result = base.VisitPseudoVariable(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode? VisitRangeExpression(BoundRangeExpression node)
        {
            var result = base.VisitRangeExpression(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode? VisitRangeVariable(BoundRangeVariable node)
        {
            VisitWithoutDiagnostics(node.Value);
            SetNotNullResult(node); // https://github.com/dotnet/roslyn/issues/29863 Need to review this
            return null;
        }

        public override BoundNode? VisitLabel(BoundLabel node)
        {
            var result = base.VisitLabel(node);
            SetUnknownResultNullability(node);
            return result;
        }

        public override BoundNode? VisitDynamicMemberAccess(BoundDynamicMemberAccess node)
        {
            var receiver = node.Receiver;
            VisitRvalue(receiver);
            _ = CheckPossibleNullReceiver(receiver);

            Debug.Assert(node.Type.IsDynamic());
            var result = TypeWithAnnotations.Create(node.Type);
            SetLvalueResultType(node, result);
            return null;
        }

        public override BoundNode? VisitDynamicInvocation(BoundDynamicInvocation node)
        {
            var expr = node.Expression;
            VisitRvalue(expr);

            // If the expression was a MethodGroup, check nullability of receiver.
            var receiverOpt = (expr as BoundMethodGroup)?.ReceiverOpt;
            if (TryGetMethodGroupReceiverNullability(receiverOpt, out TypeWithState receiverType))
            {
                CheckPossibleNullReceiver(receiverOpt, receiverType, checkNullableValueType: false);
            }

            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt, parameterAnnotationsOpt: default, defaultArguments: default);
            Debug.Assert(node.Type.IsDynamic());
            Debug.Assert(node.Type.IsReferenceType);
            var result = TypeWithAnnotations.Create(node.Type, NullableAnnotation.Oblivious);
            SetLvalueResultType(node, result);
            return null;
        }

        public override BoundNode? VisitEventAssignmentOperator(BoundEventAssignmentOperator node)
        {
            var receiverOpt = node.ReceiverOpt;
            VisitRvalue(receiverOpt);
            Debug.Assert(!IsConditionalState);
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
            if (node.Argument.ConstantValueOpt?.IsNull != true
                && MakeMemberSlot(receiverOpt, @event) is > 0 and var memberSlot)
            {
                SetState(ref this.State, memberSlot,
                    node.IsAddition ? GetState(ref this.State, memberSlot).Meet(ResultType.State) : NullableFlowState.MaybeNull);
            }
            SetNotNullResult(node); // https://github.com/dotnet/roslyn/issues/29969 Review whether this is the correct result
            return null;
        }

        public override BoundNode? VisitDynamicObjectCreationExpression(BoundDynamicObjectCreationExpression node)
        {
            VisitObjectCreationExpressionBase(node);
            return null;
        }

        public override BoundNode? VisitObjectInitializerExpression(BoundObjectInitializerExpression node)
        {
            // Only reachable from bad expression. Otherwise handled in VisitObjectCreationExpression().
            // https://github.com/dotnet/roslyn/issues/35042: Do we need to analyze child expressions anyway for the public API?
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode? VisitCollectionInitializerExpression(BoundCollectionInitializerExpression node)
        {
            // Only reachable from bad expression. Otherwise handled in VisitObjectCreationExpression().
            // https://github.com/dotnet/roslyn/issues/35042: Do we need to analyze child expressions anyway for the public API?
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode? VisitDynamicCollectionElementInitializer(BoundDynamicCollectionElementInitializer node)
        {
            // Only reachable from bad expression. Otherwise handled in VisitObjectCreationExpression().
            // https://github.com/dotnet/roslyn/issues/35042: Do we need to analyze child expressions anyway for the public API?
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode? VisitImplicitReceiver(BoundImplicitReceiver node)
        {
            var result = base.VisitImplicitReceiver(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode? VisitAnonymousPropertyDeclaration(BoundAnonymousPropertyDeclaration node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode? VisitNoPiaObjectCreationExpression(BoundNoPiaObjectCreationExpression node)
        {
            VisitObjectCreationExpressionBase(node);
            return null;
        }

        public override BoundNode? VisitNewT(BoundNewT node)
        {
            VisitObjectCreationExpressionBase(node);
            return null;
        }

        public override BoundNode? VisitArrayInitialization(BoundArrayInitialization node)
        {
            var result = base.VisitArrayInitialization(node);
            SetNotNullResult(node);
            return result;
        }

        private void SetUnknownResultNullability(BoundExpression expression)
        {
            SetResultType(expression, TypeWithState.Create(expression.Type, default));
        }

        public override BoundNode? VisitDynamicIndexerAccess(BoundDynamicIndexerAccess node)
        {
            var receiver = node.Receiver;
            VisitRvalue(receiver);
            // https://github.com/dotnet/roslyn/issues/30598: Mark receiver as not null
            // after indices have been visited, and only if the receiver has not changed.
            _ = CheckPossibleNullReceiver(receiver);
            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt, parameterAnnotationsOpt: default, defaultArguments: default);
            Debug.Assert(node.Type.IsDynamic());
            var result = TypeWithAnnotations.Create(node.Type, NullableAnnotation.Oblivious);
            SetLvalueResultType(node, result);
            return null;
        }

        private bool CheckPossibleNullReceiver(BoundExpression? receiverOpt, bool checkNullableValueType = false)
        {
            return CheckPossibleNullReceiver(receiverOpt, ResultType, checkNullableValueType);
        }

        private bool CheckPossibleNullReceiver(BoundExpression? receiverOpt, TypeWithState resultType, bool checkNullableValueType)
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
                new VisitResult(result, result.ToTypeWithAnnotations(compilation)),
                conversionResultsBuilder: null,
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

        public override BoundNode? VisitQueryClause(BoundQueryClause node)
        {
            var result = base.VisitQueryClause(node);
            SetNotNullResult(node); // https://github.com/dotnet/roslyn/issues/29863 Implement nullability analysis in LINQ queries
            return result;
        }

        public override BoundNode? VisitNameOfOperator(BoundNameOfOperator node)
        {
            var result = base.VisitNameOfOperator(node);
            SetResultType(node, TypeWithState.Create(node.Type, NullableFlowState.NotNull));
            return result;
        }

        public override BoundNode? VisitNamespaceExpression(BoundNamespaceExpression node)
        {
            var result = base.VisitNamespaceExpression(node);
            SetUnknownResultNullability(node);
            return result;
        }

        public override BoundNode? VisitUnconvertedInterpolatedString(BoundUnconvertedInterpolatedString node)
        {
            // This is only involved with unbound lambdas or when visiting the source of a converted tuple literal
            var result = base.VisitUnconvertedInterpolatedString(node);
            SetResultType(node, TypeWithState.Create(node.Type, NullableFlowState.NotNull));
            return result;
        }

        public override BoundNode? VisitStringInsert(BoundStringInsert node)
        {
            var result = base.VisitStringInsert(node);
            SetUnknownResultNullability(node);
            return result;
        }

        protected override void VisitInterpolatedStringHandlerConstructor(BoundExpression? constructor)
        {
            // We skip visiting the constructor at this stage. We will visit it manually when VisitConversion is
            // called on the interpolated string handler
        }

        public override BoundNode? VisitInterpolatedStringHandlerPlaceholder(BoundInterpolatedStringHandlerPlaceholder node)
        {
            // These placeholders don't yet follow proper placeholder discipline
            AssertPlaceholderAllowedWithoutRegistration(node);
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode? VisitInterpolatedStringArgumentPlaceholder(BoundInterpolatedStringArgumentPlaceholder node)
        {
            VisitPlaceholderWithReplacement(node);
            return null;
        }

        public override BoundNode? VisitStackAllocArrayCreation(BoundStackAllocArrayCreation node)
        {
            Debug.Assert(node.Type is null || node.Type.IsErrorType() || node.Type.IsRefLikeType);
            return VisitStackAllocArrayCreationBase(node);
        }

        public override BoundNode? VisitConvertedStackAllocExpression(BoundConvertedStackAllocExpression node)
        {
            return VisitStackAllocArrayCreationBase(node);
        }

        private BoundNode? VisitStackAllocArrayCreationBase(BoundStackAllocArrayCreationBase node)
        {
            VisitRvalue(node.Count);

            var initialization = node.InitializerOpt;
            if (initialization is null)
            {
                SetResultType(node, TypeWithState.Create(node.Type, NullableFlowState.NotNull));
                return null;
            }

            Debug.Assert(node.Type is not null);
            var type = VisitArrayInitialization(node.Type, initialization, node.HasErrors);
            SetResultType(node, TypeWithState.Create(type, NullableFlowState.NotNull));
            return null;
        }

        public override BoundNode? VisitDiscardExpression(BoundDiscardExpression node)
        {
            var result = TypeWithAnnotations.Create(node.Type, node.IsInferred ? NullableAnnotation.Annotated : node.NullableAnnotation);
            var rValueType = TypeWithState.ForType(node.Type);
            SetResult(node, rValueType, result);
            return null;
        }

        public override BoundNode? VisitThrowExpression(BoundThrowExpression node)
        {
            VisitThrow(node.Expression);
            SetResultType(node, default);
            return null;
        }

        public override BoundNode? VisitThrowStatement(BoundThrowStatement node)
        {
            VisitThrow(node.ExpressionOpt);
            return null;
        }

        private void VisitThrow(BoundExpression? expr)
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

        public override BoundNode? VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            BoundExpression expr = node.Expression;
            if (expr == null)
            {
                return null;
            }
            var method = (MethodSymbol)CurrentSymbol;
            TypeWithAnnotations elementType = InMethodBinder.GetIteratorElementTypeFromReturnType(compilation, RefKind.None,
                method.ReturnType, errorLocation: null, diagnostics: null);

            _ = VisitOptionalImplicitConversion(expr, elementType, useLegacyWarnings: false, trackMembers: false, AssignmentKind.Return);
            Unsplit();
            return null;
        }

        public override BoundNode? VisitCatchBlock(BoundCatchBlock node)
        {
            TakeIncrementalSnapshot(node);
            if (node.Locals.Length > 0)
            {
                LocalSymbol local = node.Locals[0];
                if (local.DeclarationKind == LocalDeclarationKind.CatchVariable)
                {
                    int slot = GetOrCreateSlot(local);
                    if (slot > 0)
                        SetState(ref this.State, slot, NullableFlowState.NotNull);
                }
            }

            if (node.ExceptionSourceOpt != null)
            {
                VisitWithoutDiagnostics(node.ExceptionSourceOpt);
            }

            base.VisitCatchBlock(node);

            return null;
        }

        public override BoundNode? VisitLockStatement(BoundLockStatement node)
        {
            VisitRvalue(node.Argument);
            _ = CheckPossibleNullReceiver(node.Argument);
            VisitStatement(node.Body);
            return null;
        }

        public override BoundNode? VisitAttribute(BoundAttribute node)
        {
            VisitArguments(node, node.ConstructorArguments, ImmutableArray<RefKind>.Empty, node.Constructor, argsToParamsOpt: node.ConstructorArgumentsToParamsOpt, defaultArguments: node.ConstructorDefaultArguments,
                expanded: node.ConstructorExpanded, invokedAsExtensionMethod: false);
            foreach (var assignment in node.NamedArguments)
            {
                Visit(assignment);
            }

            SetNotNullResult(node);
            return null;
        }

        public override BoundNode? VisitExpressionWithNullability(BoundExpressionWithNullability node)
        {
            var typeWithAnnotations = TypeWithAnnotations.Create(node.Type, node.NullableAnnotation);
            SetResult(node.Expression, typeWithAnnotations.ToTypeWithState(), typeWithAnnotations);
            return null;
        }

        public override BoundNode? VisitDeconstructValuePlaceholder(BoundDeconstructValuePlaceholder node)
        {
            // These placeholders don't yet follow proper placeholder discipline
            AssertPlaceholderAllowedWithoutRegistration(node);
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode? VisitObjectOrCollectionValuePlaceholder(BoundObjectOrCollectionValuePlaceholder node)
        {
            // These placeholders don't yet follow proper placeholder discipline
            AssertPlaceholderAllowedWithoutRegistration(node);
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode? VisitAwaitableValuePlaceholder(BoundAwaitableValuePlaceholder node)
        {
            // These placeholders don't always follow proper placeholder discipline yet
            AssertPlaceholderAllowedWithoutRegistration(node);
            VisitPlaceholderWithReplacement(node);
            return null;
        }

        private void VisitPlaceholderWithReplacement(BoundValuePlaceholderBase node)
        {
            if (_resultForPlaceholdersOpt != null &&
                _resultForPlaceholdersOpt.TryGetValue(node, out var value))
            {
                var result = value.Result;
                SetResult(node, result.RValueType, result.LValueType);
            }
            else
            {
                AssertPlaceholderAllowedWithoutRegistration(node);
                SetNotNullResult(node);
            }
        }

        public override BoundNode? VisitAwaitableInfo(BoundAwaitableInfo node)
        {
            Visit(node.AwaitableInstancePlaceholder);
            Visit(node.GetAwaiter);
            return null;
        }

        public override BoundNode? VisitFunctionPointerInvocation(BoundFunctionPointerInvocation node)
        {
            _ = Visit(node.InvokedExpression);
            Debug.Assert(ResultType is TypeWithState { Type: FunctionPointerTypeSymbol { }, State: NullableFlowState.NotNull });
            _ = VisitArguments(
                node,
                node.Arguments,
                node.ArgumentRefKindsOpt,
                node.FunctionPointer.Signature,
                argsToParamsOpt: default,
                defaultArguments: default,
                expanded: false,
                invokedAsExtensionMethod: false);

            var returnTypeWithAnnotations = node.FunctionPointer.Signature.ReturnTypeWithAnnotations;
            SetResult(node, returnTypeWithAnnotations.ToTypeWithState(), returnTypeWithAnnotations);

            return null;
        }

        protected override string Dump(LocalState state)
        {
            return state.Dump(_variables);
        }

        protected override bool Meet(ref LocalState self, ref LocalState other)
        {
            if (!self.Reachable)
                return false;

            if (!other.Reachable)
            {
                self = other.Clone();
                return true;
            }

            Normalize(ref self);
            Normalize(ref other);

            return self.Meet(in other);
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

            Normalize(ref self);
            Normalize(ref other);

            return self.Join(in other);
        }

        private void Join(ref PossiblyConditionalState other)
        {
            var otherIsConditional = other.IsConditionalState;
            if (otherIsConditional)
            {
                Split();
            }

            if (IsConditionalState)
            {
                Join(ref StateWhenTrue, ref otherIsConditional ? ref other.StateWhenTrue : ref other.State);
                Join(ref StateWhenFalse, ref otherIsConditional ? ref other.StateWhenFalse : ref other.State);
            }
            else
            {
                Debug.Assert(!otherIsConditional);
                Join(ref State, ref other.State);
            }
        }

        private LocalState CloneAndUnsplit(ref PossiblyConditionalState conditionalState)
        {
            if (!conditionalState.IsConditionalState)
            {
                return conditionalState.State.Clone();
            }

            var state = conditionalState.StateWhenTrue.Clone();
            Join(ref state, ref conditionalState.StateWhenFalse);
            return state;
        }

        private void SetPossiblyConditionalState(in PossiblyConditionalState conditionalState)
        {
            if (!conditionalState.IsConditionalState)
            {
                SetState(conditionalState.State);
            }
            else
            {
                SetConditionalState(conditionalState.StateWhenTrue, conditionalState.StateWhenFalse);
            }
        }

        internal sealed class LocalStateSnapshot
        {
            internal readonly int Id;
            internal readonly LocalStateSnapshot? Container;
            internal readonly BitVector State;

            internal LocalStateSnapshot(int id, LocalStateSnapshot? container, BitVector state)
            {
                Id = id;
                Container = container;
                State = state;
            }
        }

        /// <summary>
        /// A bit array containing the nullability of variables associated with a method scope. If the method is a
        /// nested function (a lambda or a local function), there is a reference to the corresponding instance for
        /// the containing method scope. The instances in the chain are associated with a corresponding
        /// <see cref="Variables"/> chain, and the <see cref="Id"/> field in this type matches <see cref="Variables.Id"/>.
        /// </summary>
        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        internal struct LocalState : ILocalDataFlowState
        {
            private sealed class Boxed
            {
                internal LocalState Value;

                internal Boxed(LocalState value)
                {
                    Value = value;
                }
            }

            internal readonly int Id;
            private readonly Boxed? _container;

            // The representation of a state is a bit vector with two bits per slot:
            // (false, false) => NotNull, (false, true) => MaybeNull, (true, true) => MaybeDefault.
            // Slot 0 is used to represent whether the state is reachable (true) or not.
            private BitVector _state;

            private LocalState(int id, Boxed? container, BitVector state)
            {
                Id = id;
                _container = container;
                _state = state;
            }

            internal static LocalState Create(LocalStateSnapshot snapshot)
            {
                var container = snapshot.Container is null ? null : new Boxed(Create(snapshot.Container));
                return new LocalState(snapshot.Id, container, snapshot.State.Clone());
            }

            internal LocalStateSnapshot CreateSnapshot()
            {
                return new LocalStateSnapshot(Id, _container?.Value.CreateSnapshot(), _state.Clone());
            }

            public bool Reachable => _state[0];

            public bool NormalizeToBottom => false;

            public static LocalState ReachableState(Variables variables)
            {
                return CreateReachableOrUnreachableState(variables, reachable: true);
            }

            public static LocalState UnreachableState(Variables variables)
            {
                return CreateReachableOrUnreachableState(variables, reachable: false);
            }

            public static LocalState ReachableStateWithNotNulls(Variables variables)
            {
                var container = variables.Container is null ?
                    null :
                    new Boxed(ReachableStateWithNotNulls(variables.Container));

                int capacity = variables.NextAvailableIndex;
                return new LocalState(variables.Id, container, createBitVectorWithNotNulls(capacity, reachable: true));

                static BitVector createBitVectorWithNotNulls(int capacity, bool reachable)
                {
                    BitVector state = BitVector.Create(capacity * 2);
                    state[0] = reachable;

                    for (int i = 1; i < capacity; i++)
                    {
                        var index = i * 2;
                        state[index] = true;
                        state[index + 1] = true;
                    }
                    return state;
                }
            }

            private static LocalState CreateReachableOrUnreachableState(Variables variables, bool reachable)
            {
                var container = variables.Container is null ?
                    null :
                    new Boxed(CreateReachableOrUnreachableState(variables.Container, reachable));

                return new LocalState(variables.Id, container, CreateBitVector(reachable));
            }

            public LocalState CreateNestedMethodState(Variables variables)
            {
                Debug.Assert(Id == variables.Container!.Id);
                return new LocalState(variables.Id, container: new Boxed(this), CreateBitVector(reachable: true));
            }

            private static BitVector CreateBitVector(bool reachable)
            {
                BitVector state = BitVector.Create(2);
                state[0] = reachable;
                return state;
            }

            private int Capacity => _state.Capacity / 2;

            private void EnsureCapacity(int capacity)
            {
                _state.EnsureCapacity(capacity * 2);
            }

            public bool HasVariable(int slot)
            {
                if (slot <= 0)
                {
                    return false;
                }
                (int id, int index) = Variables.DeconstructSlot(slot);
                return hasVariableCore(ref this, id, index);

                bool hasVariableCore(ref LocalState state, int id, int index)
                {
                    if (state.Id > id)
                    {
                        return hasVariableCore(ref state._container!.Value, id, index);
                    }
                    else
                    {
                        return state.Id == id;
                    }
                }
            }

            public void NormalizeIfNeeded(int slot, NullableWalker walker, Variables variables, bool useNotNullsAsDefault = false)
            {
                if (!hasValue(ref this, slot))
                    Normalize(walker, variables, useNotNullsAsDefault);

                static bool hasValue(ref LocalState state, int slot)
                {
                    if (slot <= 0)
                    {
                        return false;
                    }
                    (int id, int index) = Variables.DeconstructSlot(slot);
                    return hasValueCore(ref state, id, index);
                }

                static bool hasValueCore(ref LocalState state, int id, int index)
                {
                    if (state.Id != id)
                    {
                        Debug.Assert(state.Id > id);
                        return hasValueCore(ref state._container!.Value, id, index);
                    }
                    else
                    {
                        return index < state.Capacity;
                    }
                }
            }

            public void Normalize(NullableWalker walker, Variables variables, bool useNotNullsAsDefault = false)
            {
                if (Id != variables.Id)
                {
                    Debug.Assert(Id < variables.Id);
                    Normalize(walker, variables.Container!, useNotNullsAsDefault);
                }
                else
                {
                    _container?.Value.Normalize(walker, variables.Container!, useNotNullsAsDefault);
                    int start = Capacity;
                    EnsureCapacity(variables.NextAvailableIndex);
                    Populate(walker, start, useNotNullsAsDefault);
                }
            }

            public void PopulateAll(NullableWalker walker)
            {
                _container?.Value.PopulateAll(walker);
                Populate(walker, start: 1, useNotNullsAsDefault: false);
            }

            private void Populate(NullableWalker walker, int start, bool useNotNullsAsDefault)
            {
                int capacity = Capacity;
                for (int index = start; index < capacity; index++)
                {
                    int slot = Variables.ConstructSlot(Id, index);
                    SetValue(Id, index, useNotNullsAsDefault ? NullableFlowState.NotNull : walker.GetDefaultState(ref this, slot));
                }
            }

            public NullableFlowState this[int slot]
            {
                get
                {
                    (int id, int index) = Variables.DeconstructSlot(slot);
                    return GetValue(id, index);
                }
                set
                {
                    (int id, int index) = Variables.DeconstructSlot(slot);
                    SetValue(id, index, value);
                }
            }

            private NullableFlowState GetValue(int id, int index)
            {
                if (Id != id)
                {
                    Debug.Assert(Id > id);
                    return _container!.Value.GetValue(id, index);
                }
                return GetValue(index);
            }

            private NullableFlowState GetValue(int index)
            {
                if (!this.Reachable)
                {
                    return NullableFlowState.NotNull;
                }

                Debug.Assert(index < Capacity);
                index *= 2;
                Debug.Assert((_state[index], _state[index + 1]) != (false, false));

                var result = (_state[index], _state[index + 1]) switch
                {
                    (false, false) => NullableFlowState.NotNull, // Should not be reachable
                    (true, false) => NullableFlowState.MaybeNull,
                    (false, true) => NullableFlowState.MaybeDefault,
                    (true, true) => NullableFlowState.NotNull
                };

                return result;
            }

            private void SetValue(int id, int index, NullableFlowState value)
            {
                if (Id != id)
                {
                    Debug.Assert(Id > id);
                    _container!.Value.SetValue(id, index, value);
                }
                else
                {
                    SetValue(index, value);
                }
            }

            private void SetValue(int index, NullableFlowState value)
            {
                // No states should be modified in unreachable code, as there is only one unreachable state.
                if (!this.Reachable) return;
                index *= 2;

                (_state[index], _state[index + 1]) = value switch
                {
                    NullableFlowState.MaybeNull => (true, false),
                    NullableFlowState.MaybeDefault => (false, true),
                    NullableFlowState.NotNull => (true, true),
                    _ => throw ExceptionUtilities.Unreachable()
                };
            }

            internal void ForEach<TArg>(Action<int, TArg> action, TArg arg)
            {
                _container?.Value.ForEach(action, arg);
                for (int index = 1; index < Capacity; index++)
                {
                    action(Variables.ConstructSlot(Id, index), arg);
                }
            }

            internal LocalState GetStateForVariables(int id)
            {
                var state = this;
                while (state.Id != id)
                {
                    state = state._container!.Value;
                }
                return state;
            }

            /// <summary>
            /// Produce a duplicate of this flow analysis state.
            /// </summary>
            /// <returns></returns>
            public LocalState Clone()
            {
                var container = _container is null ? null : new Boxed(_container.Value.Clone());
                return new LocalState(Id, container, _state.Clone());
            }

            public bool Join(in LocalState other)
            {
                Debug.Assert(Id == other.Id);
                bool hasChanged = false;
                if (_container is { } && _container.Value.Join(in other._container!.Value))
                {
                    hasChanged = true;
                }

                Debug.Assert(_state.Capacity == other._state.Capacity);
                var oldReachable = Reachable;
                var newReachable = oldReachable | other.Reachable;
                _state[0] = newReachable;
                hasChanged |= (oldReachable != newReachable);

                for (int i = 1; i < Capacity; i++)
                {
                    // An unreachable state may contain uninitialized `(false, false)` values,
                    // but it can be turned into a reachable state during a join (see above).
                    // During that process we can't call GetValue on it.
                    var oldValue = oldReachable ? GetValue(i) : NullableFlowState.NotNull;
                    var newValue = oldValue.Join(other.GetValue(i));
                    SetValue(i, newValue);
                    hasChanged |= (oldValue != newValue);
                }

                return hasChanged;
            }

            public bool Meet(in LocalState other)
            {
                Debug.Assert(Id == other.Id);
                bool hasChanged = false;
                if (_container is { } && _container.Value.Meet(in other._container!.Value))
                {
                    hasChanged = true;
                }

                Debug.Assert(_state.Capacity == other._state.Capacity);
                var oldReachable = Reachable;
                var newReachable = oldReachable & other.Reachable;
                _state[0] = newReachable;
                hasChanged |= (oldReachable != newReachable);

                for (int i = 1; i < Capacity; i++)
                {
                    var oldValue = GetValue(i);
                    var newValue = oldValue.Meet(other.GetValue(i));
                    SetValue(i, newValue);
                    hasChanged |= (oldValue != newValue);
                }

                return hasChanged;
            }

            internal string GetDebuggerDisplay()
            {
                var pooledBuilder = PooledStringBuilder.GetInstance();
                var builder = pooledBuilder.Builder;
                builder.Append(' ');
                int n = Math.Min(Capacity, 8);
                for (int i = n - 1; i >= 0; i--)
                {
                    var mayBeNull = GetValue(i) is NullableFlowState.MaybeNull or NullableFlowState.MaybeDefault;
                    builder.Append(mayBeNull ? '?' : '!');
                }

                return pooledBuilder.ToStringAndFree();
            }

            internal string Dump(Variables variables)
            {
                if (!this.Reachable)
                    return "unreachable";

                if (Id != variables.Id)
                    return "invalid";

                var builder = PooledStringBuilder.GetInstance();
                Dump(builder, variables);
                return builder.ToStringAndFree();
            }

            private void Dump(StringBuilder builder, Variables variables)
            {
                _container?.Value.Dump(builder, variables.Container!);

                for (int index = 1; index < Capacity; index++)
                {
                    if (getName(Variables.ConstructSlot(Id, index)) is string name)
                    {
                        builder.Append(name);
                        var annotation = GetValue(Id, index) switch
                        {
                            NullableFlowState.MaybeNull => "?",
                            NullableFlowState.MaybeDefault => "??",
                            _ => "!"
                        };
                        builder.Append(annotation);
                    }
                }

                string? getName(int slot)
                {
                    VariableIdentifier id = variables[slot];
                    var name = id.Symbol.Name;
                    int containingSlot = id.ContainingSlot;
                    return containingSlot > 0 ?
                        getName(containingSlot) + "." + name :
                        name;
                }
            }
        }

        internal sealed class LocalFunctionState : AbstractLocalFunctionState
        {
            /// <summary>
            /// Defines the starting state used in the local function body to
            /// produce diagnostics and determine types.
            /// </summary>
            public LocalState StartingState;

            public LocalFunctionState(LocalState unreachableState)
                // Note: these states are not used in nullable analysis.
                : base(stateFromBottom: unreachableState.Clone(), stateFromTop: unreachableState.Clone())
            {
                StartingState = unreachableState;
            }
        }

        protected override LocalFunctionState CreateLocalFunctionState(LocalFunctionSymbol symbol)
        {
            var variables = (symbol.ContainingSymbol is MethodSymbol containingMethod ? _variables.GetVariablesForMethodScope(containingMethod) : null) ??
                _variables.GetRootScope();
            return new LocalFunctionState(LocalState.UnreachableState(variables));
        }

        private sealed class NullabilityInfoTypeComparer : IEqualityComparer<(NullabilityInfo info, TypeSymbol? type)>
        {
            public static readonly NullabilityInfoTypeComparer Instance = new NullabilityInfoTypeComparer();

            public bool Equals((NullabilityInfo info, TypeSymbol? type) x, (NullabilityInfo info, TypeSymbol? type) y)
            {
                return x.info.Equals(y.info) &&
                       Symbols.SymbolEqualityComparer.ConsiderEverything.Equals(x.type, y.type);
            }

            public int GetHashCode((NullabilityInfo info, TypeSymbol? type) obj)
            {
                return obj.GetHashCode();
            }
        }

        private sealed class ExpressionAndSymbolEqualityComparer : IEqualityComparer<(BoundNode? expr, Symbol symbol)>
        {
            internal static readonly ExpressionAndSymbolEqualityComparer Instance = new ExpressionAndSymbolEqualityComparer();

            private ExpressionAndSymbolEqualityComparer() { }

            public bool Equals((BoundNode? expr, Symbol symbol) x, (BoundNode? expr, Symbol symbol) y)
            {
                RoslynDebug.Assert(x.symbol is object);
                RoslynDebug.Assert(y.symbol is object);

                // We specifically use reference equality for the symbols here because the BoundNode should be immutable.
                // We should be storing and retrieving the exact same instance of the symbol, not just an "equivalent"
                // symbol.
                return x.expr == y.expr && (object)x.symbol == y.symbol;
            }

            public int GetHashCode((BoundNode? expr, Symbol symbol) obj)
            {
                RoslynDebug.Assert(obj.symbol is object);
                return Hash.Combine(obj.expr, obj.symbol.GetHashCode());
            }
        }
    }
}
