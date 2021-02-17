﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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

            public VisitResult(TypeWithState rValueType, TypeWithAnnotations lValueType)
            {
                RValueType = rValueType;
                LValueType = lValueType;
                // https://github.com/dotnet/roslyn/issues/34993: Doesn't hold true for Tuple_Assignment_10. See if we can make it hold true
                //Debug.Assert((RValueType.Type is null && LValueType.TypeSymbol is null) ||
                //             RValueType.Type.Equals(LValueType.TypeSymbol, TypeCompareKind.ConsiderEverything | TypeCompareKind.AllIgnoreOptions));
            }

            public VisitResult(TypeSymbol? type, NullableAnnotation annotation, NullableFlowState state)
            {
                RValueType = TypeWithState.Create(type, state);
                LValueType = TypeWithAnnotations.Create(type, annotation);
                Debug.Assert(TypeSymbol.Equals(RValueType.Type, LValueType.Type, TypeCompareKind.ConsiderEverything));
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
        /// If true, the parameter types and nullability from _delegateInvokeMethod is used for
        /// initial parameter state. If false, the signature of CurrentSymbol is used instead.
        /// </summary>
        private bool _useDelegateInvokeParameterTypes;

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
        private static readonly TypeWithState _invalidType = TypeWithState.Create(ErrorTypeSymbol.UnknownResultType, NullableFlowState.NotNull);

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

        /// <summary>
        /// State of awaitable expressions, for substitution in placeholders within GetAwaiter calls.
        /// </summary>
        private PooledDictionary<BoundAwaitableValuePlaceholder, (BoundExpression AwaitableExpression, VisitResult Result)>? _awaitablePlaceholdersOpt;

        /// <summary>
        /// Variables instances for each lambda or local function defined within the analyzed region.
        /// </summary>
        private PooledDictionary<MethodSymbol, Variables>? _nestedFunctionVariables;

        /// <summary>
        /// True if we're analyzing speculative code. This turns off some initialization steps
        /// that would otherwise be taken.
        /// </summary>
        private readonly bool _isSpeculative;

        /// <summary>
        /// True if this walker was created using an initial state.
        /// </summary>
        private readonly bool _hasInitialState;

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

        /// <summary>
        /// Force the inference of the LValueResultType from ResultType.
        /// </summary>
        private void UseRvalueOnly(BoundExpression? expression)
        {
            SetResult(expression, ResultType, ResultType.ToTypeWithAnnotations(compilation), isLvalue: false);
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
        private void SetAnalyzedNullability(BoundExpression? expr, VisitResult result, bool? isLvalue = null)
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
            _nestedFunctionVariables?.Free();
            _awaitablePlaceholdersOpt?.Free();
            _methodGroupReceiverMapOpt?.Free();
            _placeholderLocalsOpt?.Free();
            _variables.Free();
            base.Free();
        }

        private NullableWalker(
            CSharpCompilation compilation,
            Symbol? symbol,
            bool useConstructorExitWarnings,
            bool useDelegateInvokeParameterTypes,
            MethodSymbol? delegateInvokeMethodOpt,
            BoundNode node,
            Binder binder,
            Conversions conversions,
            Variables? variables,
            ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)>? returnTypesOpt,
            ImmutableDictionary<BoundExpression, (NullabilityInfo, TypeSymbol?)>.Builder? analyzedNullabilityMapOpt,
            SnapshotManager.Builder? snapshotBuilderOpt,
            bool isSpeculative = false)
            : base(compilation, symbol, node, EmptyStructTypeCache.CreatePrecise(), trackUnassignments: true)
        {
            Debug.Assert(!useDelegateInvokeParameterTypes || delegateInvokeMethodOpt is object);

            _variables = variables ?? Variables.Create(symbol);
            _binder = binder;
            _conversions = (Conversions)conversions.WithNullability(true);
            _useConstructorExitWarnings = useConstructorExitWarnings;
            _useDelegateInvokeParameterTypes = useDelegateInvokeParameterTypes;
            _delegateInvokeMethod = delegateInvokeMethodOpt;
            _analyzedNullabilityMapOpt = analyzedNullabilityMapOpt;
            _returnTypesOpt = returnTypesOpt;
            _snapshotBuilderOpt = snapshotBuilderOpt;
            _isSpeculative = isSpeculative;
            _hasInitialState = variables is { };
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

        protected override bool TryGetVariable(VariableIdentifier identifier, out int slot)
        {
            return _variables.TryGetValue(identifier, out slot);
        }

        protected override int AddVariable(VariableIdentifier identifier)
        {
            return _variables.Add(identifier);
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

                makeNotNullMembersMaybeNull();
                // We need to create a snapshot even of the first node, because we want to have the state of the initial parameters.
                _snapshotBuilderOpt?.TakeIncrementalSnapshot(methodMainNode, State);
            }

            ImmutableArray<PendingBranch> pendingReturns = base.Scan(ref badRegion);
            if ((_symbol as MethodSymbol)?.IsConstructor() != true || _useConstructorExitWarnings)
            {
                EnforceDoesNotReturn(syntaxOpt: null);
                enforceMemberNotNull(syntaxOpt: null, this.State);
                enforceNotNull(null, this.State);

                foreach (var pendingReturn in pendingReturns)
                {
                    enforceMemberNotNull(syntaxOpt: pendingReturn.Branch.Syntax, pendingReturn.State);

                    if (pendingReturn.Branch is BoundReturnStatement returnStatement)
                    {
                        enforceNotNull(returnStatement.Syntax, pendingReturn.State);
                        enforceNotNullWhenForPendingReturn(pendingReturn, returnStatement);
                        enforceMemberNotNullWhenForPendingReturn(pendingReturn, returnStatement);
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
                        if (method.RequiresInstanceReceiver)
                        {
                            method.TryGetThisParameter(out var thisParameter);
                            Debug.Assert(thisParameter is object);
                            thisSlot = GetOrCreateSlot(thisParameter);
                        }
                        // https://github.com/dotnet/roslyn/issues/46718: give diagnostics on return points, not constructor signature
                        var exitLocation = method.DeclaringSyntaxReferences.IsEmpty ? null : method.Locations.FirstOrDefault();
                        foreach (var member in method.ContainingType.GetMembersUnordered())
                        {
                            checkMemberStateOnConstructorExit(method, member, state, thisSlot, exitLocation);
                        }
                    }
                    else
                    {
                        do
                        {
                            foreach (var memberName in method.NotNullMembers)
                            {
                                enforceMemberNotNullOnMember(syntaxOpt, state, method, memberName);
                            }

                            method = method.OverriddenMethod;
                        }
                        while (method != null);
                    }
                }
            }

            void checkMemberStateOnConstructorExit(MethodSymbol constructor, Symbol member, LocalState state, int thisSlot, Location? exitLocation)
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
                if (HasInitializer(member))
                {
                    return;
                }

                TypeWithAnnotations fieldType;
                FieldSymbol? field;
                Symbol symbol;
                switch (member)
                {
                    case FieldSymbol f:
                        fieldType = f.TypeWithAnnotations;
                        field = f;
                        symbol = (Symbol?)(f.AssociatedSymbol as PropertySymbol) ?? f;
                        break;
                    case EventSymbol e:
                        fieldType = e.TypeWithAnnotations;
                        field = e.AssociatedField;
                        symbol = e;
                        if (field is null)
                        {
                            return;
                        }
                        break;
                    default:
                        return;
                }
                if (field.IsConst)
                {
                    return;
                }
                if (fieldType.Type.IsValueType || fieldType.Type.IsErrorType())
                {
                    return;
                }
                var annotations = symbol.GetFlowAnalysisAnnotations();
                if ((annotations & FlowAnalysisAnnotations.AllowNull) != 0)
                {
                    // We assume that if a member has AllowNull then the user
                    // does not care that we exit at a point where the member might be null.
                    return;
                }
                fieldType = ApplyUnconditionalAnnotations(fieldType, annotations);
                if (!fieldType.NullableAnnotation.IsNotAnnotated())
                {
                    return;
                }
                var slot = GetOrCreateSlot(symbol, thisSlot);
                if (slot < 0)
                {
                    return;
                }

                var memberState = state[slot];
                var badState = fieldType.Type.IsPossiblyNullableReferenceTypeTypeParameter() && (annotations & FlowAnalysisAnnotations.NotNull) == 0
                    ? NullableFlowState.MaybeDefault
                    : NullableFlowState.MaybeNull;
                if (memberState >= badState) // is 'memberState' as bad as or worse than 'badState'?
                {
                    Diagnostics.Add(ErrorCode.WRN_UninitializedNonNullableField, exitLocation ?? symbol.Locations.FirstOrNone(), symbol.Kind.Localize(), symbol.Name);
                }
            }

            void enforceMemberNotNullOnMember(SyntaxNode? syntaxOpt, LocalState state, MethodSymbol method, string memberName)
            {
                foreach (var member in method.ContainingType.GetMembers(memberName))
                {
                    if (memberHasBadState(member, state))
                    {
                        // Member '{name}' must have a non-null value when exiting.
                        Diagnostics.Add(ErrorCode.WRN_MemberNotNull, syntaxOpt?.GetLocation() ?? methodMainNode.Syntax.GetLastToken().GetLocation(), member.Name);
                    }
                }
            }

            void enforceMemberNotNullWhenForPendingReturn(PendingBranch pendingReturn, BoundReturnStatement returnStatement)
            {
                if (pendingReturn.IsConditionalState)
                {
                    if (returnStatement.ExpressionOpt is { ConstantValue: { IsBoolean: true, BooleanValue: bool value } })
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
                            enforceMemberNotNullWhenIfAffected(returnStatement.Syntax, sense: true, method.ContainingType.GetMembers(memberName), pendingReturn.StateWhenTrue, pendingReturn.StateWhenFalse);
                        }

                        foreach (var memberName in method.NotNullWhenFalseMembers)
                        {
                            enforceMemberNotNullWhenIfAffected(returnStatement.Syntax, sense: false, method.ContainingType.GetMembers(memberName), pendingReturn.StateWhenFalse, pendingReturn.StateWhenTrue);
                        }
                    }
                }
                else if (returnStatement.ExpressionOpt is { ConstantValue: { IsBoolean: true, BooleanValue: bool value } })
                {
                    enforceMemberNotNullWhen(returnStatement.Syntax, sense: value, pendingReturn.State);
                }
            }

            void enforceMemberNotNullWhenIfAffected(SyntaxNode? syntaxOpt, bool sense, ImmutableArray<Symbol> members, LocalState state, LocalState otherState)
            {
                foreach (var member in members)
                {
                    // For non-constant values, only complain if we were able to analyze a difference for this member between two branches
                    if (memberHasBadState(member, state) != memberHasBadState(member, otherState))
                    {
                        reportMemberIfBadConditionalState(syntaxOpt, sense, member, state);
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
                            reportMemberIfBadConditionalState(syntaxOpt, sense, member, state);
                        }
                    }
                }
            }

            void reportMemberIfBadConditionalState(SyntaxNode? syntaxOpt, bool sense, Symbol member, LocalState state)
            {
                if (memberHasBadState(member, state))
                {
                    // Member '{name}' must have a non-null value when exiting with '{sense}'.
                    Diagnostics.Add(ErrorCode.WRN_MemberNotNullWhen, syntaxOpt?.GetLocation() ?? methodMainNode.Syntax.GetLastToken().GetLocation(), member.Name, sense ? "true" : "false");
                }
            }

            bool memberHasBadState(Symbol member, LocalState state)
            {
                switch (member.Kind)
                {
                    case SymbolKind.Field:
                    case SymbolKind.Property:
                        if (getSlotForFieldOrPropertyOrEvent(member) is int memberSlot &&
                            memberSlot > 0)
                        {
                            var parameterState = state[memberSlot];
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

            void makeNotNullMembersMaybeNull()
            {
                if (_symbol is MethodSymbol method)
                {
                    if (method.IsConstructor())
                    {
                        if (needsDefaultInitialStateForMembers())
                        {
                            foreach (var member in method.ContainingType.GetMembersUnordered())
                            {
                                if (member.IsStatic != method.IsStatic)
                                {
                                    continue;
                                }

                                var memberToInitialize = member;
                                switch (member)
                                {
                                    case PropertySymbol:
                                        // skip any manually implemented properties.
                                        continue;
                                    case FieldSymbol { IsConst: true }:
                                        continue;
                                    case FieldSymbol { AssociatedSymbol: PropertySymbol prop }:
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
                                var memberSlot = getSlotForFieldOrPropertyOrEvent(memberToInitialize);
                                if (memberSlot > 0)
                                {
                                    var type = memberToInitialize.GetTypeOrReturnType();
                                    if (!type.NullableAnnotation.IsOblivious())
                                    {
                                        this.State[memberSlot] = type.Type.IsPossiblyNullableReferenceTypeTypeParameter() ? NullableFlowState.MaybeDefault : NullableFlowState.MaybeNull;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        do
                        {
                            makeMembersMaybeNull(method, method.NotNullMembers);
                            makeMembersMaybeNull(method, method.NotNullWhenTrueMembers);
                            makeMembersMaybeNull(method, method.NotNullWhenFalseMembers);
                            method = method.OverriddenMethod;
                        }
                        while (method != null);
                    }
                }

                bool needsDefaultInitialStateForMembers()
                {
                    if (_hasInitialState)
                    {
                        return false;
                    }

                    // We don't use a default initial state for value type instance constructors without `: this()` because
                    // any usages of uninitialized fields will get definite assignment errors anyway.
                    if (!method.HasThisConstructorInitializer() && (!method.ContainingType.IsValueType || method.IsStatic))
                    {
                        return true;
                    }

                    return methodMainNode is BoundConstructorMethodBody ctorBody
                        && ctorBody.Initializer?.Expression.ExpressionSymbol is MethodSymbol delegatedCtor
                        && delegatedCtor.IsDefaultValueTypeConstructor();
                }
            }

            void makeMembersMaybeNull(MethodSymbol method, ImmutableArray<string> members)
            {
                foreach (var memberName in members)
                {
                    makeMemberMaybeNull(method, memberName);
                }
            }

            void makeMemberMaybeNull(MethodSymbol method, string memberName)
            {
                var type = method.ContainingType;
                foreach (var member in type.GetMembers(memberName))
                {
                    if (getSlotForFieldOrPropertyOrEvent(member) is int memberSlot &&
                        memberSlot > 0)
                    {
                        this.State[memberSlot] = NullableFlowState.MaybeNull;
                    }
                }
            }

            void enforceNotNullWhenForPendingReturn(PendingBranch pendingReturn, BoundReturnStatement returnStatement)
            {
                var parameters = this.MethodParameters;

                if (!parameters.IsEmpty)
                {
                    if (pendingReturn.IsConditionalState)
                    {
                        if (returnStatement.ExpressionOpt is { ConstantValue: { IsBoolean: true, BooleanValue: bool value } })
                        {
                            enforceParameterNotNullWhen(returnStatement.Syntax, parameters, sense: value, stateWhen: pendingReturn.State);
                            return;
                        }

                        if (!pendingReturn.StateWhenTrue.Reachable || !pendingReturn.StateWhenFalse.Reachable)
                        {
                            return;
                        }

                        foreach (var parameter in parameters)
                        {
                            // For non-constant values, only complain if we were able to analyze a difference for this parameter between two branches
                            if (GetOrCreateSlot(parameter) is > 0 and var slot && pendingReturn.StateWhenTrue[slot] != pendingReturn.StateWhenFalse[slot])
                            {
                                reportParameterIfBadConditionalState(returnStatement.Syntax, parameter, sense: true, stateWhen: pendingReturn.StateWhenTrue);
                                reportParameterIfBadConditionalState(returnStatement.Syntax, parameter, sense: false, stateWhen: pendingReturn.StateWhenFalse);
                            }
                        }
                    }
                    else if (returnStatement.ExpressionOpt is { ConstantValue: { IsBoolean: true, BooleanValue: bool value } })
                    {
                        // example: return (bool)true;
                        enforceParameterNotNullWhen(returnStatement.Syntax, parameters, sense: value, stateWhen: pendingReturn.State);
                        return;
                    }
                }
            }

            void reportParameterIfBadConditionalState(SyntaxNode syntax, ParameterSymbol parameter, bool sense, LocalState stateWhen)
            {
                if (parameterHasBadConditionalState(parameter, sense, stateWhen))
                {
                    // Parameter '{name}' must have a non-null value when exiting with '{sense}'.
                    Diagnostics.Add(ErrorCode.WRN_ParameterConditionallyDisallowsNull, syntax.Location, parameter.Name, sense ? "true" : "false");
                }
            }

            void enforceNotNull(SyntaxNode? syntaxOpt, LocalState state)
            {
                if (!state.Reachable)
                {
                    return;
                }

                foreach (var parameter in this.MethodParameters)
                {
                    var slot = GetOrCreateSlot(parameter);
                    if (slot <= 0)
                    {
                        continue;
                    }

                    var annotations = parameter.FlowAnalysisAnnotations;
                    var hasNotNull = (annotations & FlowAnalysisAnnotations.NotNull) == FlowAnalysisAnnotations.NotNull;
                    var parameterState = state[slot];
                    if (hasNotNull && parameterState.MayBeNull())
                    {
                        // Parameter '{name}' must have a non-null value when exiting.
                        Diagnostics.Add(ErrorCode.WRN_ParameterDisallowsNull, syntaxOpt?.GetLocation() ?? methodMainNode.Syntax.GetLastToken().GetLocation(), parameter.Name);
                    }
                    else
                    {
                        EnforceNotNullIfNotNull(syntaxOpt, state, this.MethodParameters, parameter.NotNullIfParameterNotNull, parameterState, parameter);
                    }
                }
            }

            void enforceParameterNotNullWhen(SyntaxNode syntax, ImmutableArray<ParameterSymbol> parameters, bool sense, LocalState stateWhen)
            {
                if (!stateWhen.Reachable)
                {
                    return;
                }

                foreach (var parameter in parameters)
                {
                    reportParameterIfBadConditionalState(syntax, parameter, sense, stateWhen);
                }
            }

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
                    var parameterState = stateWhen[slot];

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

            int getSlotForFieldOrPropertyOrEvent(Symbol member)
            {
                if (member.Kind != SymbolKind.Field &&
                    member.Kind != SymbolKind.Property &&
                    member.Kind != SymbolKind.Event)
                {
                    return -1;
                }

                int containingSlot = 0;
                if (!member.IsStatic)
                {
                    if (MethodThisParameter is null)
                    {
                        return -1;
                    }
                    containingSlot = GetOrCreateSlot(MethodThisParameter);
                    if (containingSlot < 0)
                    {
                        return -1;
                    }
                    Debug.Assert(containingSlot > 0);
                }

                return GetOrCreateSlot(member, containingSlot);
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
                    && state[inputSlot].IsNotNull())
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
            // DoesNotReturn is only supported in member methods
            if (CurrentSymbol is MethodSymbol { ContainingSymbol: TypeSymbol _ } method &&
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
            out VariableState? finalNullableState)
        {
            if (!HasRequiredLanguageVersion(compilation) || !compilation.IsNullableAnalysisEnabledIn(method))
            {
                if (compilation.IsNullableAnalysisEnabledAlways)
                {
                    // Once we address https://github.com/dotnet/roslyn/issues/46579 we should also always pass `getFinalNullableState: true` in debug mode.
                    // We will likely always need to write a 'null' out for the out parameter in this code path, though, because
                    // we don't want to introduce behavior differences between debug and release builds
                    Analyze(compilation, method, node, new DiagnosticBag(), useConstructorExitWarnings: false, initialNullableState: null, getFinalNullableState: false, out _, requiresAnalysis: false);
                }
                finalNullableState = null;
                return;
            }

            Analyze(compilation, method, node, diagnostics, useConstructorExitWarnings, initialNullableState, getFinalNullableState, out finalNullableState);
        }

        private static void Analyze(
            CSharpCompilation compilation,
            MethodSymbol method,
            BoundNode node,
            DiagnosticBag diagnostics,
            bool useConstructorExitWarnings,
            VariableState? initialNullableState,
            bool getFinalNullableState,
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
                useDelegateInvokeParameterTypes: false,
                delegateInvokeMethodOpt: null,
                initialState: initialNullableState,
                analyzedNullabilityMapOpt: null,
                snapshotBuilderOpt: null,
                returnTypesOpt: null,
                getFinalNullableState,
                finalNullableState: out finalNullableState,
                requiresAnalysis);
        }

        /// <summary>
        /// Gets the "after initializers state" which should be used at the beginning of nullable analysis
        /// of certain constructors. Only used for semantic model and debug verification.
        /// </summary>
        internal static VariableState? GetAfterInitializersState(CSharpCompilation compilation, Symbol? symbol)
        {
            if (symbol is MethodSymbol method
                && method.IncludeFieldInitializersInBody()
                && method.ContainingType is SourceMemberContainerTypeSymbol containingType)
            {
                var unusedDiagnostics = DiagnosticBag.GetInstance();

                Binder.ProcessedFieldInitializers initializers = default;
                Binder.BindFieldInitializers(compilation, null, method.IsStatic ? containingType.StaticInitializers : containingType.InstanceInitializers, BindingDiagnosticBag.Discarded, ref initializers);
                NullableWalker.AnalyzeIfNeeded(
                    compilation,
                    method,
                    InitializerRewriter.RewriteConstructor(initializers.BoundInitializers, method),
                    unusedDiagnostics,
                    useConstructorExitWarnings: false,
                    initialNullableState: null,
                    getFinalNullableState: true,
                    out var afterInitializersState);

                unusedDiagnostics.Free();

                return afterInitializersState;
            }

            return null;
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
            _ = AnalyzeWithSemanticInfo(compilation, symbol, node, binder, initialState: GetAfterInitializersState(compilation, symbol), diagnostics, createSnapshots, requiresAnalysis: false);
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
                useDelegateInvokeParameterTypes: false,
                delegateInvokeMethodOpt: null,
                initialState,
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
                return compilation.SyntaxTrees.Any(tree => ((CSharpSyntaxTree)tree).IsNullableAnalysisEnabled(new Text.TextSpan(0, tree.Length)) == true);
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
                useDelegateInvokeParameterTypes: false,
                delegateInvokeMethodOpt: null,
                node,
                binder,
                binder.Conversions,
                Variables.Create(variables),
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
            DiagnosticBag diagnostics)
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
                symbol: null,
                node,
                binder,
                binder.Conversions,
                diagnostics,
                useConstructorExitWarnings: false,
                useDelegateInvokeParameterTypes: false,
                delegateInvokeMethodOpt: null,
                initialState: null,
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
            ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)>? returnTypesOpt)
        {
            var symbol = lambda.Symbol;
            var variables = Variables.Create(initialState.Variables).CreateNestedMethodScope(symbol);
            var walker = new NullableWalker(
                compilation,
                symbol,
                useConstructorExitWarnings: false,
                useDelegateInvokeParameterTypes: UseDelegateInvokeParameterTypes(lambda, delegateInvokeMethodOpt),
                delegateInvokeMethodOpt: delegateInvokeMethodOpt,
                lambda.Body,
                lambda.Binder,
                conversions,
                variables,
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
            bool useDelegateInvokeParameterTypes,
            MethodSymbol? delegateInvokeMethodOpt,
            VariableState? initialState,
            ImmutableDictionary<BoundExpression, (NullabilityInfo, TypeSymbol?)>.Builder? analyzedNullabilityMapOpt,
            SnapshotManager.Builder? snapshotBuilderOpt,
            ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)>? returnTypesOpt,
            bool getFinalNullableState,
            out VariableState? finalNullableState,
            bool requiresAnalysis = true)
        {
            Debug.Assert(diagnostics != null);
            var walker = new NullableWalker(compilation,
                                            symbol,
                                            useConstructorExitWarnings,
                                            useDelegateInvokeParameterTypes,
                                            delegateInvokeMethodOpt,
                                            node,
                                            binder,
                                            conversions,
                                            initialState is null ? null : Variables.Create(initialState.Variables),
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
            try
            {
                bool badRegion = false;
                var previousSlot = snapshotBuilderOpt?.EnterNewWalker(symbol!) ?? -1;
                ImmutableArray<PendingBranch> returns = walker.Analyze(ref badRegion, initialState);
                snapshotBuilderOpt?.ExitWalker(walker.SaveSharedState(), previousSlot);
                diagnostics?.AddRange(walker.Diagnostics);
                Debug.Assert(!badRegion);
            }
            catch (CancelledByStackGuardException ex) when (diagnostics != null)
            {
                ex.AddAnError(diagnostics);
            }

            walker.RecordNullableAnalysisData(symbol, requiresAnalysis);
        }

        private void RecordNullableAnalysisData(Symbol? symbol, bool requiredAnalysis)
        {
            if (compilation.NullableAnalysisData is { } state)
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
            Debug.Assert(AreCloseEnough(originalSymbol, updatedSymbol), $"Attempting to set {node.Syntax} from {originalSymbol.ToDisplayString()} to {updatedSymbol.ToDisplayString()}");
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
                Debug.Assert(node.HasErrors || nodeType!.IsErrorType() ||
                       conversionsWithoutNullability.HasIdentityOrImplicitReferenceConversion(slotType, nodeType, ref discardedUseSiteInfo) ||
                       conversionsWithoutNullability.HasBoxingConversion(slotType, nodeType, ref discardedUseSiteInfo));
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

            if (value == null ||
                // This prevents us from giving undesired warnings for synthesized arguments for optional parameters,
                // but allows us to give warnings for synthesized assignments to record properties and for parameter default values at the declaration site.
                (value.WasCompilerGenerated && assignmentKind == AssignmentKind.Argument) ||
                !ShouldReportNullableAssignment(targetType, valueType.State))
            {
                return;
            }

            location ??= value.Syntax.GetLocation();
            var unwrappedValue = SkipReferenceConversions(value);
            if (unwrappedValue.IsSuppressed)
            {
                return;
            }

            if (value.ConstantValue?.IsNull == true && !useLegacyWarnings)
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

            if (refKind == RefKind.None || refKind == RefKind.In)
            {
                // pre-condition attributes
                // Check whether we can assign a value from overridden parameter to overriding
                var valueState = GetParameterState(overriddenType, overriddenAnnotations);
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

                if (!this.State.HasValue(targetSlot)) Normalize(ref this.State);

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
                        value = this.State.HasValue(valueMemberSlot) ?
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

        private TypeSymbol NominalSlotType(int slot)
        {
            return _variables[slot].Symbol.GetTypeOrReturnType().Type;
        }

        /// <summary>
        /// Whenever assigning a variable, and that variable is not declared at the point the state is being set,
        /// and the new state is not <see cref="NullableFlowState.NotNull"/>, this method should be called to perform the
        /// state setting and to ensure the mutation is visible outside the finally block when the mutation occurs in a
        /// finally block.
        /// </summary>
        private void SetStateAndTrackForFinally(ref LocalState state, int slot, NullableFlowState newState)
        {
            state[slot] = newState;
            if (newState != NullableFlowState.NotNull && NonMonotonicState.HasValue)
            {
                var tryState = NonMonotonicState.Value;
                if (tryState.HasVariable(slot))
                {
                    tryState[slot] = newState.Join(tryState[slot]);
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

            // Reset the state of any members of the target.
            var members = ArrayBuilder<(VariableIdentifier, int)>.GetInstance();
            _variables.GetMembers(members, targetSlot);
            foreach (var (variable, slot) in members)
            {
                var symbol = AsMemberOfType(targetType, variable.Symbol);
                SetStateAndTrackForFinally(ref this.State, slot, GetDefaultState(symbol));
                InheritDefaultState(symbol.GetTypeOrReturnType().Type, slot);
            }
            members.Free();
        }

        private NullableFlowState GetDefaultState(Symbol symbol)
            => ApplyUnconditionalAnnotations(symbol.GetTypeOrReturnType().ToTypeWithState(), GetRValueAnnotations(symbol)).State;

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
            return LocalState.ReachableState(_variables);
        }

        private void EnterParameters()
        {
            if (!(CurrentSymbol is MethodSymbol methodSymbol))
            {
                return;
            }

            if (methodSymbol is SynthesizedRecordConstructor)
            {
                if (_hasInitialState)
                {
                    // A record primary constructor's parameters are entered before analyzing initializers.
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

            // The partial definition part may include optional parameters whose default values we want to simulate assigning at the beginning of the method
            methodSymbol = methodSymbol.PartialDefinitionPart ?? methodSymbol;

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
            int slot = GetOrCreateSlot(parameter);

            Debug.Assert(!IsConditionalState);
            if (slot > 0)
            {
                var state = GetParameterState(parameterType, parameter.FlowAnalysisAnnotations).State;
                this.State[slot] = state;
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

            // If the LHS has annotations, we perform an additional check for nullable value types
            CheckDisallowedNullAssignment(resultType, parameterAnnotations, equalsValue.Value.Syntax.Location);

            return null;
        }

        private static TypeWithState GetParameterState(TypeWithAnnotations parameterType, FlowAnalysisAnnotations parameterAnnotations)
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
                    CheckDisallowedNullAssignment(returnState, ToInwardAnnotations(returnAnnotations), node.Syntax.Location, boundValueOpt: expr);
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
                EnforceNotNullIfNotNull(node.Syntax, this.State, method.Parameters, method.ReturnNotNullIfParameterNotNull, ResultType.State, outputParam: null);
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

            var delegateOrMethod = _delegateInvokeMethod ?? method;
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
                annotations = delegateOrMethod.ReturnTypeFlowAnalysisAnnotations;
                type = ApplyUnconditionalAnnotations(returnType, annotations);
                return true;
            }

            if (returnType.Type.IsGenericTaskType(compilation))
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
            if (!TrackingRegions && !block.LocalFunctions.IsDefaultOrEmpty)
            {
                // First visit everything else
                foreach (var stmt in block.Statements)
                {
                    if (stmt.Kind != BoundKind.LocalFunctionStatement)
                    {
                        VisitStatement(stmt);
                    }
                }

                // Now visit the local function bodies
                foreach (var stmt in block.Statements)
                {
                    if (stmt is BoundLocalFunctionStatement localFunc)
                    {
                        VisitLocalFunctionStatement(localFunc);
                    }
                }
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
            var localFunc = node.Symbol;
            var localFunctionState = GetOrCreateLocalFuncUsages(localFunc);
            // The starting state is the top state, but with captured
            // variables set according to Joining the state at all the
            // local function use sites
            var state = TopState();
            var startingState = localFunctionState.StartingState;
            startingState.ForEach(
                (slot, variables) =>
                {
                    var symbol = variables[variables.RootSlot(slot)].Symbol;
                    if (Symbol.IsCaptured(symbol, localFunc))
                    {
                        state[slot] = startingState[slot];
                    }
                },
                _variables);
            localFunctionState.Visited = true;

            AnalyzeLocalFunctionOrLambda(
                node,
                localFunc,
                state,
                delegateInvokeMethod: null,
                useDelegateInvokeParameterTypes: false);

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
            bool useDelegateInvokeParameterTypes)
        {
            var oldSymbol = this.CurrentSymbol;
            this.CurrentSymbol = lambdaOrFunctionSymbol;

            Debug.Assert(!useDelegateInvokeParameterTypes || delegateInvokeMethod is object);
            var oldDelegateInvokeMethod = _delegateInvokeMethod;
            _delegateInvokeMethod = delegateInvokeMethod;
            var oldUseDelegateInvokeParameterTypes = _useDelegateInvokeParameterTypes;
            _useDelegateInvokeParameterTypes = useDelegateInvokeParameterTypes;

            var oldReturnTypes = _returnTypesOpt;
            _returnTypesOpt = null;

            var oldState = this.State;
            _variables = GetOrCreateNestedFunctionVariables(_variables, lambdaOrFunctionSymbol);
            this.State = state.CreateNestedMethodState(_variables);
            var previousSlot = _snapshotBuilderOpt?.EnterNewWalker(lambdaOrFunctionSymbol) ?? -1;

            var oldPending = SavePending();

            EnterParameters();

            var oldPending2 = SavePending();

            // If this is an iterator, there's an implicit branch before the first statement
            // of the function where the enumerable is returned.
            if (lambdaOrFunctionSymbol.IsIterator)
            {
                PendingBranches.Add(new PendingBranch(null, this.State, null));
            }

            VisitAlways(lambdaOrFunction.Body);
            RestorePending(oldPending2); // process any forward branches within the lambda body
            ImmutableArray<PendingBranch> pendingReturns = RemoveReturns();
            RestorePending(oldPending);

            _snapshotBuilderOpt?.ExitWalker(this.SaveSharedState(), previousSlot);
            _variables = _variables.Container!;
            this.State = oldState;
            _returnTypesOpt = oldReturnTypes;
            _useDelegateInvokeParameterTypes = oldUseDelegateInvokeParameterTypes;
            _delegateInvokeMethod = oldDelegateInvokeMethod;
            this.CurrentSymbol = oldSymbol;
        }

        protected override void VisitLocalFunctionUse(
            LocalFunctionSymbol symbol,
            LocalFunctionState localFunctionState,
            SyntaxNode syntax,
            bool isCall)
        {
            // Do not use this overload in NullableWalker. Use the overload below instead.
            throw ExceptionUtilities.Unreachable;
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

            var resultType = withExpr.CloneMethod?.ReturnTypeWithAnnotations ?? ResultType.ToTypeWithAnnotations(compilation);
            var resultState = ApplyUnconditionalAnnotations(resultType.ToTypeWithState(), GetRValueAnnotations(withExpr.CloneMethod));
            var resultSlot = GetOrCreatePlaceholderSlot(withExpr);
            // carry over the null state of members of 'receiver' to the result of the with-expression.
            TrackNullableStateForAssignment(receiver, resultType, resultSlot, resultState, MakeSlot(receiver));
            // use the declared nullability of Clone() for the top-level nullability of the result of the with-expression.
            SetResult(withExpr, resultState, resultType);
            VisitObjectCreationInitializer(containingSymbol: null, resultSlot, withExpr.InitializerExpression, FlowAnalysisAnnotations.None);

            // Note: this does not account for the scenario where `Clone()` returns maybe-null and the with-expression has no initializers.
            // Tracking in https://github.com/dotnet/roslyn/issues/44759
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
                    this.State[slot] = GetDefaultState(ref this.State, slot);
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

        protected override BoundExpression? VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            Debug.Assert(!IsConditionalState);
            SetInvalidResult();
            _ = base.VisitExpressionWithoutStackGuard(node);
            TypeWithState resultType = ResultType;

#if DEBUG
            // Verify Visit method set _result.
            Debug.Assert((object?)resultType.Type != _invalidType.Type);
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

            bool canIgnoreAnyType(TypeSymbol type)
            {
                return type.VisitType((t, unused1, unused2) => canIgnoreType(t), (object?)null) is object;
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
            return invokeMethod.Parameters.SequenceEqual(l.Parameters,
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

        private BoundNode Visit(BoundNode? node, bool expressionIsRead)
        {
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
            Debug.Assert(!IsConditionalState);
            var arguments = node.Arguments;
            var argumentResults = VisitArguments(node, arguments, node.ArgumentRefKindsOpt, node.Constructor, node.ArgsToParamsOpt, node.DefaultArguments, node.Expanded, invokedAsExtensionMethod: false).results;
            VisitObjectOrDynamicObjectCreation(node, arguments, argumentResults, node.InitializerExpressionOpt);
            return null;
        }

        public override BoundNode? VisitUnconvertedObjectCreationExpression(BoundUnconvertedObjectCreationExpression node)
        {
            // This method is only involved in method inference with unbound lambdas.
            // The diagnostics on arguments are reported by VisitObjectCreationExpression.
            SetResultType(node, TypeWithState.Create(null, NullableFlowState.NotNull));
            return null;
        }

        private void VisitObjectOrDynamicObjectCreation(
            BoundExpression node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<VisitArgumentResult> argumentResults,
            BoundExpression? initializerOpt)
        {
            Debug.Assert(node.Kind == BoundKind.ObjectCreationExpression ||
                node.Kind == BoundKind.DynamicObjectCreationExpression ||
                node.Kind == BoundKind.NewT);
            var argumentTypes = argumentResults.SelectAsArray(ar => ar.RValueType);

            int slot = -1;
            var type = node.Type;
            var resultState = NullableFlowState.NotNull;
            if (type is object)
            {
                slot = GetOrCreatePlaceholderSlot(node);
                if (slot > 0)
                {
                    var boundObjectCreationExpression = node as BoundObjectCreationExpression;
                    var constructor = boundObjectCreationExpression?.Constructor;
                    bool isDefaultValueTypeConstructor = constructor?.IsDefaultValueTypeConstructor() == true;

                    if (EmptyStructTypeCache.IsTrackableStructType(type))
                    {
                        var containingType = constructor?.ContainingType;
                        if (containingType?.IsTupleType == true && !isDefaultValueTypeConstructor)
                        {
                            // new System.ValueTuple<T1, ..., TN>(e1, ..., eN)
                            TrackNullableStateOfTupleElements(slot, containingType, arguments, argumentTypes, boundObjectCreationExpression!.ArgsToParamsOpt, useRestField: true);
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

                    this.State[slot] = resultState;
                }
            }

            if (initializerOpt != null)
            {
                VisitObjectCreationInitializer(containingSymbol: null, slot, initializerOpt, leftAnnotations: FlowAnalysisAnnotations.None);
            }

            SetResultType(node, TypeWithState.Create(type, resultState));
        }

        private void VisitObjectCreationInitializer(Symbol? containingSymbol, int containingSlot, BoundExpression node, FlowAnalysisAnnotations leftAnnotations)
        {
            TakeIncrementalSnapshot(node);
            switch (node)
            {
                case BoundObjectInitializerExpression objectInitializer:
                    checkImplicitReceiver(objectInitializer);
                    foreach (var initializer in objectInitializer.Initializers)
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
                    SetNotNullResult(objectInitializer.Placeholder);
                    break;
                case BoundCollectionInitializerExpression collectionInitializer:
                    checkImplicitReceiver(collectionInitializer);
                    foreach (var initializer in collectionInitializer.Initializers)
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
                    SetNotNullResult(collectionInitializer.Placeholder);
                    break;
                default:
                    Debug.Assert(containingSymbol is object);
                    if (containingSymbol is object)
                    {
                        var type = ApplyLValueAnnotations(containingSymbol.GetTypeOrReturnType(), leftAnnotations);
                        TypeWithState resultType = VisitOptionalImplicitConversion(node, type, useLegacyWarnings: false, trackMembers: true, AssignmentKind.Assignment);
                        TrackNullableStateForAssignment(node, type, containingSlot, resultType, MakeSlot(node));
                    }
                    break;
            }

            void checkImplicitReceiver(BoundObjectInitializerExpressionBase node)
            {
                if (containingSlot >= 0 && !node.Initializers.IsEmpty)
                {
                    if (!node.Type.IsValueType && State[containingSlot].MayBeNull())
                    {
                        if (containingSymbol is null)
                        {
                            ReportDiagnostic(ErrorCode.WRN_NullReferenceReceiver, node.Syntax);
                        }
                        else
                        {
                            ReportDiagnostic(ErrorCode.WRN_NullReferenceInitializer, node.Syntax, containingSymbol);
                        }
                    }
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
                            VisitArguments(objectInitializer, objectInitializer.Arguments, objectInitializer.ArgumentRefKindsOpt, (PropertySymbol?)symbol, objectInitializer.ArgsToParamsOpt, objectInitializer.DefaultArguments, objectInitializer.Expanded);
                        }

                        if (symbol is object)
                        {
                            int slot = (containingSlot < 0 || !IsSlotMember(containingSlot, symbol)) ? -1 : GetOrCreateSlot(symbol, containingSlot);
                            VisitObjectCreationInitializer(symbol, slot, node.Right, GetLValueAnnotations(node.Left));
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
            (var reinferredMethod, _, _) = VisitArguments(node, node.Arguments, refKindsOpt: default, node.AddMethod, node.ArgsToParamsOpt, node.DefaultArguments, node.Expanded, node.InvokedAsExtensionMethod);
            Debug.Assert(reinferredMethod is object);
            if (node.ImplicitReceiverOpt != null)
            {
                Debug.Assert(node.ImplicitReceiverOpt.Kind == BoundKind.ObjectOrCollectionValuePlaceholder);
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

            TakeIncrementalSnapshot(initialization);
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
                    placeholderBuilder.Add(CreatePlaceholderIfNecessary(expressionNoConversion, resultType.ToTypeWithAnnotations(compilation)));
                }

                var placeholders = placeholderBuilder.ToImmutableAndFree();

                TypeSymbol? bestType = null;
                if (!node.HasErrors)
                {
                    var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    bestType = BestTypeInferrer.InferBestType(placeholders, _conversions, ref discardedUseSiteInfo);
                }

                TypeWithAnnotations inferredType = (bestType is null)
                    ? elementType.SetUnknownNullabilityForReferenceTypes()
                    : TypeWithAnnotations.Create(bestType);

                if (bestType is object)
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
                    inferredType = TypeWithState.Create(inferredType.Type, elementState).ToTypeWithAnnotations(compilation);

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
            SetResultType(node, TypeWithState.Create(arrayType, NullableFlowState.NotNull));
            return null;
        }

        /// <summary>
        /// Applies analysis similar to <see cref="VisitArrayCreation"/>.
        /// The expressions returned from a lambda are not converted though, so we'll have to classify fresh conversions.
        /// Note: even if some conversions fail, we'll proceed to infer top-level nullability. That is reasonable in common cases.
        /// </summary>
        internal static TypeWithAnnotations BestTypeForLambdaReturns(
            ArrayBuilder<(BoundExpression, TypeWithAnnotations)> returns,
            Binder binder,
            BoundNode node,
            Conversions conversions)
        {
            var walker = new NullableWalker(binder.Compilation,
                                            symbol: null,
                                            useConstructorExitWarnings: false,
                                            useDelegateInvokeParameterTypes: false,
                                            delegateInvokeMethodOpt: null,
                                            node,
                                            binder,
                                            conversions: conversions,
                                            variables: null,
                                            returnTypesOpt: null,
                                            analyzedNullabilityMapOpt: null,
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

            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            var placeholders = placeholdersBuilder.ToImmutableAndFree();
            TypeSymbol? bestType = BestTypeInferrer.InferBestType(placeholders, walker._conversions, ref discardedUseSiteInfo);

            TypeWithAnnotations inferredType;
            if (bestType is { })
            {
                // Note: so long as we have a best type, we can proceed.
                var bestTypeWithObliviousAnnotation = TypeWithAnnotations.Create(bestType);
                ConversionsBase conversionsWithoutNullability = walker._conversions.WithNullability(false);
                for (int i = 0; i < n; i++)
                {
                    BoundExpression placeholder = placeholders[i];
                    Conversion conversion = conversionsWithoutNullability.ClassifyConversionFromExpression(placeholder, bestType, ref discardedUseSiteInfo);
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
            Visit(leftOperand);

            while (true)
            {
                if (!learnFromBooleanConstantTest())
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

            // learn from true/false constant
            bool learnFromBooleanConstantTest()
            {
                if (!IsConditionalState)
                {
                    return false;
                }

                if (!leftConversion.IsIdentity)
                {
                    return false;
                }

                BinaryOperatorKind op = binary.OperatorKind.Operator();
                if (op != BinaryOperatorKind.Equal && op != BinaryOperatorKind.NotEqual)
                {
                    return false;
                }

                bool isSense;
                if (binary.Right.ConstantValue?.IsBoolean == true)
                {
                    UseRvalueOnly(leftOperand); // record result for the left

                    var stateWhenTrue = this.StateWhenTrue.Clone();
                    var stateWhenFalse = this.StateWhenFalse.Clone();

                    Unsplit();
                    Visit(binary.Right);
                    UseRvalueOnly(binary.Right); // record result for the right

                    SetConditionalState(stateWhenTrue, stateWhenFalse);
                    isSense = (op == BinaryOperatorKind.Equal) == binary.Right.ConstantValue.BooleanValue;
                }
                else if (binary.Left.ConstantValue?.IsBoolean == true)
                {
                    Unsplit();
                    UseRvalueOnly(leftOperand); // record result for the left

                    Visit(binary.Right);
                    UseRvalueOnly(binary.Right); // record result for the right
                    isSense = (op == BinaryOperatorKind.Equal) == binary.Left.ConstantValue.BooleanValue;
                }
                else
                {
                    return false;
                }

                if (!isSense && IsConditionalState)
                {
                    SetConditionalState(StateWhenFalse, StateWhenTrue);
                }

                // record result for the binary
                Debug.Assert(binary.Type.SpecialType == SpecialType.System_Boolean);
                SetResult(binary, TypeWithState.ForType(binary.Type), TypeWithAnnotations.Create(binary.Type));
                return true;
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
                TypeSymbol? asMemberOfType = getTypeIfContainingType(methodContainer, leftUnderlyingType.Type) ??
                    getTypeIfContainingType(methodContainer, rightUnderlyingType.Type);
                if (asMemberOfType is object)
                {
                    method = (MethodSymbol)AsMemberOfType(asMemberOfType, method);
                }

                // Analyze operator call properly (honoring [Disallow|Allow|Maybe|NotNull] attribute annotations) https://github.com/dotnet/roslyn/issues/32671
                var parameters = method.Parameters;
                visitOperandConversion(binary.Left, leftOperand, leftConversion, parameters[0], leftUnderlyingType);
                visitOperandConversion(binary.Right, rightOperand, rightConversion, parameters[1], rightUnderlyingType);
                SetUpdatedSymbol(binary, binary.MethodOpt!, method);

                void visitOperandConversion(
                    BoundExpression expr,
                    BoundExpression operand,
                    Conversion conversion,
                    ParameterSymbol parameter,
                    TypeWithState operandType)
                {
                    TypeWithAnnotations targetTypeWithNullability = parameter.TypeWithAnnotations;

                    if (isLifted && targetTypeWithNullability.Type.IsNonNullableValueType())
                    {
                        targetTypeWithNullability = TypeWithAnnotations.Create(MakeNullableOf(targetTypeWithNullability));
                    }

                    _ = VisitConversion(
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
            SetResult(binary, inferredResult, inferredResult.ToTypeWithAnnotations(compilation));

            BinaryOperatorKind op = binary.OperatorKind.Operator();

            if (op == BinaryOperatorKind.Equal || op == BinaryOperatorKind.NotEqual)
            {
                // learn from null constant
                BoundExpression? operandComparedToNull = null;
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

            TypeSymbol? getTypeIfContainingType(TypeSymbol baseType, TypeSymbol? derivedType)
            {
                if (derivedType is null)
                {
                    return null;
                }
                derivedType = derivedType.StrippedType();
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                var conversion = _conversions.ClassifyBuiltInConversion(derivedType, baseType, ref discardedUseSiteInfo);
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

        private static void MarkSlotsAsNotNull(ArrayBuilder<int> slots, ref LocalState stateToUpdate)
        {
            foreach (int slot in slots)
            {
                stateToUpdate[slot] = NullableFlowState.NotNull;
            }
        }

        private void LearnFromNonNullTest(BoundExpression expression, ref LocalState state)
        {
            if (expression.Kind == BoundKind.AwaitableValuePlaceholder)
            {
                if (_awaitablePlaceholdersOpt != null && _awaitablePlaceholdersOpt.TryGetValue((BoundAwaitableValuePlaceholder)expression, out var value))
                {
                    expression = value.AwaitableExpression;
                }
                else
                {
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
            state[slot] = NullableFlowState.NotNull;
        }

        private void LearnFromNullTest(BoundExpression expression, ref LocalState state)
        {
            // nothing to learn about a constant
            if (expression.ConstantValue != null)
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
                if (state[slot] == NullableFlowState.NotNull)
                {
                    // Note: We leave a MaybeDefault state as-is
                    state[slot] = NullableFlowState.MaybeNull;
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
                        state[childSlot] = NullableFlowState.NotNull;
                        MarkDependentSlotsNotNull(childSlot, member.GetTypeOrReturnType().Type, ref state, depth - 1);
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

        public override BoundNode? VisitNullCoalescingOperator(BoundNullCoalescingOperator node)
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
                BoundNullCoalescingOperatorResultKind.LeftType => getLeftResultType(leftResultType!, rightResultType!),
                BoundNullCoalescingOperatorResultKind.LeftUnwrappedType => getLeftResultType(leftResultType!.StrippedType(), rightResultType!),
                BoundNullCoalescingOperatorResultKind.RightType => getRightResultType(leftResultType!, rightResultType!),
                BoundNullCoalescingOperatorResultKind.LeftUnwrappedRightType => getRightResultType(leftResultType!.StrippedType(), rightResultType!),
                BoundNullCoalescingOperatorResultKind.RightDynamicType => rightResultType!,
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

        public override BoundNode? VisitConditionalAccess(BoundConditionalAccess node)
        {
            Debug.Assert(!IsConditionalState);

            var receiver = node.Receiver;
            _ = VisitRvalueWithState(receiver);
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
                LearnFromNullTest(receiver, ref receiverState);
                LearnFromNonNullTest(receiver, ref this.State);
                var nextConditionalAccessSlot = MakeSlot(receiver);
                if (nextConditionalAccessSlot > 0 && receiver.Type?.IsNullableType() == true)
                    nextConditionalAccessSlot = GetNullableOfTValueSlot(receiver.Type, nextConditionalAccessSlot, out _);

                _lastConditionalAccessSlot = nextConditionalAccessSlot;
            }

            var accessTypeWithAnnotations = VisitLvalueWithAnnotations(node.AccessExpression);
            TypeSymbol accessType = accessTypeWithAnnotations.Type;
            Join(ref this.State, ref receiverState);

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
                TypeWithAnnotations consequenceLValue;
                TypeWithAnnotations alternativeLValue;

                (consequenceLValue, consequenceRValue) = visitConditionalRefOperand(consequenceState, originalConsequence);
                consequenceState = this.State;
                (alternativeLValue, alternativeRValue) = visitConditionalRefOperand(alternativeState, originalAlternative);
                Join(ref this.State, ref consequenceState);

                TypeSymbol? refResultType = node.Type?.SetUnknownNullabilityForReferenceTypes();
                if (IsNullabilityMismatch(consequenceLValue, alternativeLValue))
                {
                    // l-value types must match
                    ReportNullabilityMismatchInAssignment(node.Syntax, consequenceLValue, alternativeLValue);
                }
                else if (!node.HasErrors)
                {
                    refResultType = consequenceRValue.Type!.MergeEquivalentTypes(alternativeRValue.Type, VarianceKind.None);
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
                (alternative, alternativeConversion, alternativeRValue) = visitConditionalOperand(alternativeState, originalAlternative);
                (consequence, consequenceConversion, consequenceRValue) = visitConditionalOperand(consequenceState, originalConsequence);
                alternativeEndReachable = false;
                consequenceEndReachable = IsReachable();
            }
            else if (!consequenceState.Reachable)
            {
                (consequence, consequenceConversion, consequenceRValue) = visitConditionalOperand(consequenceState, originalConsequence);
                (alternative, alternativeConversion, alternativeRValue) = visitConditionalOperand(alternativeState, originalAlternative);
                consequenceEndReachable = false;
                alternativeEndReachable = IsReachable();
            }
            else
            {
                (consequence, consequenceConversion, consequenceRValue) = visitConditionalOperand(consequenceState, originalConsequence);
                Unsplit();
                consequenceState = this.State;
                consequenceEndReachable = consequenceState.Reachable;

                (alternative, alternativeConversion, alternativeRValue) = visitConditionalOperand(alternativeState, originalAlternative);
                Unsplit();
                alternativeEndReachable = this.State.Reachable;
                Join(ref this.State, ref consequenceState);
            }

            TypeSymbol? resultType;
            if (node.HasErrors || node is BoundConditionalOperator { WasTargetTyped: true })
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
                BoundExpression consequencePlaceholder = CreatePlaceholderIfNecessary(consequence, consequenceRValue.ToTypeWithAnnotations(compilation));
                BoundExpression alternativePlaceholder = CreatePlaceholderIfNecessary(alternative, alternativeRValue.ToTypeWithAnnotations(compilation));
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                resultType = BestTypeInferrer.InferBestTypeForConditionalOperator(consequencePlaceholder, alternativePlaceholder, _conversions, out _, ref discardedUseSiteInfo);
            }

            NullableFlowState resultState;
            if (resultType is null)
            {
                resultType = node.Type?.SetUnknownNullabilityForReferenceTypes();
                resultState = consequenceRValue.State.Join(alternativeRValue.State);

                var resultTypeWithState = TypeWithState.Create(resultType, resultState);

                if (consequence != originalConsequence)
                {
                    TrackAnalyzedNullabilityThroughConversionGroup(resultTypeWithState, (BoundConversion)originalConsequence, consequence);
                }

                if (alternative != originalAlternative)
                {
                    TrackAnalyzedNullabilityThroughConversionGroup(resultTypeWithState, (BoundConversion)originalAlternative, alternative);
                }
            }
            else
            {
                var resultTypeWithAnnotations = TypeWithAnnotations.Create(resultType);

                TypeWithState convertedConsequenceResult = convertResult(
                    originalConsequence,
                    consequence,
                    consequenceConversion,
                    resultTypeWithAnnotations,
                    consequenceRValue,
                    consequenceState,
                    consequenceEndReachable);

                TypeWithState convertedAlternativeResult = convertResult(
                    originalAlternative,
                    alternative,
                    alternativeConversion,
                    resultTypeWithAnnotations,
                    alternativeRValue,
                    alternativeState,
                    alternativeEndReachable);

                resultState = convertedConsequenceResult.State.Join(convertedAlternativeResult.State);
            }

            SetResultType(node, TypeWithState.Create(resultType, resultState));
            return null;

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

            TypeWithState convertResult(
                BoundExpression node,
                BoundExpression operand,
                Conversion conversion,
                TypeWithAnnotations targetType,
                TypeWithState operandType,
                LocalState state,
                bool isReachable)
            {
                var savedState = this.State;
                this.State = state;

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
                this.State = savedState;

                return resultType;
            }
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
            (method, results, returnNotNull) = VisitArguments(node, node.Arguments, refKindsOpt, method!.Parameters, node.ArgsToParamsOpt, node.DefaultArguments,
                node.Expanded, node.InvokedAsExtensionMethod, method);

            ApplyMemberPostConditions(node.ReceiverOpt, method);

            LearnFromEqualsMethod(method, node, receiverType, results);

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
            public readonly ImmutableArray<VisitArgumentResult> Results;
            public readonly ImmutableArray<int> ArgsToParamsOpt;

            public CompareExchangeInfo(ImmutableArray<BoundExpression> arguments, ImmutableArray<VisitArgumentResult> results, ImmutableArray<int> argsToParamsOpt)
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
            if (comparand.ConstantValue?.IsNull == true)
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
            return IsAnalyzingAttribute ? FlowAnalysisAnnotations.None : parameter.FlowAnalysisAnnotations;
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
            if (node is BoundCollectionElementInitializer { AddMethod: { TypeArgumentsWithAnnotations: { IsEmpty: false } } })
            {
                return true;
            }

            if (node is BoundForEachStatement { EnumeratorInfoOpt: { GetEnumeratorInfo: { Method: { TypeArgumentsWithAnnotations: { IsEmpty: false } } } } })
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

        private (MethodSymbol? method, ImmutableArray<VisitArgumentResult> results, bool returnNotNull) VisitArguments(
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

        private ImmutableArray<VisitArgumentResult> VisitArguments(
            BoundExpression node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            PropertySymbol? property,
            ImmutableArray<int> argsToParamsOpt,
            BitVector defaultArguments,
            bool expanded)
        {
            return VisitArguments(node, arguments, refKindsOpt, property is null ? default : property.Parameters, argsToParamsOpt, defaultArguments, expanded, invokedAsExtensionMethod: false).results;
        }

        /// <summary>
        /// If you pass in a method symbol, its type arguments will be re-inferred and the re-inferred method will be returned.
        /// </summary>
        private (MethodSymbol? method, ImmutableArray<VisitArgumentResult> results, bool returnNotNull) VisitArguments(
            BoundNode node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            ImmutableArray<ParameterSymbol> parametersOpt,
            ImmutableArray<int> argsToParamsOpt,
            BitVector defaultArguments,
            bool expanded,
            bool invokedAsExtensionMethod,
            MethodSymbol? method = null)
        {
            Debug.Assert(!arguments.IsDefault);
            bool shouldReturnNotNull = false;

            (ImmutableArray<BoundExpression> argumentsNoConversions, ImmutableArray<Conversion> conversions) = RemoveArgumentConversions(arguments, refKindsOpt);

            // Visit the arguments and collect results
            ImmutableArray<VisitArgumentResult> results = VisitArgumentsEvaluate(node.Syntax, argumentsNoConversions, refKindsOpt, parametersOpt, argsToParamsOpt, defaultArguments, expanded);

            // Re-infer method type parameters
            if (method?.IsGenericMethod == true)
            {
                if (HasImplicitTypeArguments(node))
                {
                    method = InferMethodTypeArguments(method, GetArgumentsForMethodTypeInference(results, argumentsNoConversions), refKindsOpt, argsToParamsOpt, expanded);
                    parametersOpt = method.Parameters;
                }
                if (ConstraintsHelper.RequiresChecking(method))
                {
                    var syntax = node.Syntax;
                    CheckMethodConstraints(
                        syntax switch
                        {
                            InvocationExpressionSyntax { Expression: var expression } => expression,
                            ForEachStatementSyntax { Expression: var expression } => expression,
                            _ => syntax
                        },
                        method);
                }
            }

            bool parameterHasNotNullIfNotNull = !IsAnalyzingAttribute && !parametersOpt.IsDefault && parametersOpt.Any(p => !p.NotNullIfParameterNotNull.IsEmpty);
            var notNullParametersBuilder = parameterHasNotNullIfNotNull ? ArrayBuilder<ParameterSymbol>.GetInstance() : null;
            if (!node.HasErrors && !parametersOpt.IsDefault)
            {
                // Visit conversions, inbound assignments including pre-conditions
                ImmutableHashSet<string>? returnNotNullIfParameterNotNull = IsAnalyzingAttribute ? null : method?.ReturnNotNullIfParameterNotNull;
                for (int i = 0; i < results.Length; i++)
                {
                    (ParameterSymbol? parameter, TypeWithAnnotations parameterType, FlowAnalysisAnnotations parameterAnnotations, bool isExpandedParamsArgument) =
                        GetCorrespondingParameter(i, parametersOpt, argsToParamsOpt, expanded);
                    if (parameter is null)
                    {
                        continue;
                    }

                    var argumentNoConversion = argumentsNoConversions[i];
                    var argument = i < arguments.Length ? arguments[i] : argumentsNoConversions[i];
                    VisitArgumentConversionAndInboundAssignmentsAndPreConditions(
                        GetConversionIfApplicable(argument, argumentNoConversion),
                        argumentNoConversion,
                        conversions.IsDefault || i >= conversions.Length ? Conversion.Identity : conversions[i],
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
            else
            {
                // Normally we delay visiting the lambda until we can visit it along with its conversion.
                // Since we can't visit its conversion here, or it doesn't have one, we dig back in and
                // visit the lambda here to ensure all nodes have nullability info for public API
                for (int i = 0; i < results.Length; i++)
                {
                    if (argumentsNoConversions[i] is BoundLambda lambda)
                    {
                        VisitLambda(lambda, delegateTypeOpt: null, results[i].StateForLambda);
                    }
                }
            }

            if (node is BoundCall { Method: { OriginalDefinition: LocalFunctionSymbol localFunction } })
            {
                VisitLocalFunctionUse(localFunction);
            }

            if (!node.HasErrors && !parametersOpt.IsDefault)
            {
                // For CompareExchange method we need more context to determine the state of outbound assignment
                CompareExchangeInfo compareExchangeInfo = IsCompareExchangeMethod(method) ? new CompareExchangeInfo(arguments, results, argsToParamsOpt) : default;

                // Visit outbound assignments and post-conditions
                // Note: the state may get split in this step
                for (int i = 0; i < arguments.Length; i++)
                {
                    (ParameterSymbol? parameter, TypeWithAnnotations parameterType, FlowAnalysisAnnotations parameterAnnotations, _) =
                        GetCorrespondingParameter(i, parametersOpt, argsToParamsOpt, expanded);
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

            if (!IsAnalyzingAttribute && method is object && (method.FlowAnalysisAnnotations & FlowAnalysisAnnotations.DoesNotReturn) == FlowAnalysisAnnotations.DoesNotReturn)
            {
                SetUnreachable();
            }

            notNullParametersBuilder?.Free();
            return (method, results, shouldReturnNotNull);
        }

        private void ApplyMemberPostConditions(BoundExpression? receiverOpt, MethodSymbol? method)
        {
            if (method is null)
            {
                return;
            }

            int receiverSlot =
                method.IsStatic ? 0 :
                receiverOpt is null ? -1 :
                MakeSlot(receiverOpt);

            if (receiverSlot < 0)
            {
                return;
            }

            ApplyMemberPostConditions(receiverSlot, method);
        }

        private void ApplyMemberPostConditions(int receiverSlot, MethodSymbol method)
        {
            Debug.Assert(receiverSlot >= 0);
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

                if (!notNullWhenTrueMembers.IsEmpty || !notNullWhenFalseMembers.IsEmpty)
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
                        receiverSlot = 0;
                    }

                    switch (member.Kind)
                    {
                        case SymbolKind.Field:
                        case SymbolKind.Property:
                            if (GetOrCreateSlot(member, receiverSlot) is int memberSlot &&
                                memberSlot > 0)
                            {
                                state[memberSlot] = NullableFlowState.NotNull;
                            }
                            break;
                        case SymbolKind.Event:
                        case SymbolKind.Method:
                            break;
                    }
                }
            }
        }

        private ImmutableArray<VisitArgumentResult> VisitArgumentsEvaluate(
            SyntaxNode syntax,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            ImmutableArray<ParameterSymbol> parametersOpt,
            ImmutableArray<int> argsToParamsOpt,
            BitVector defaultArguments,
            bool expanded)
        {
            Debug.Assert(!IsConditionalState);
            int n = arguments.Length;
            if (n == 0 && parametersOpt.IsDefaultOrEmpty)
            {
                return ImmutableArray<VisitArgumentResult>.Empty;
            }

            var visitedParameters = PooledHashSet<ParameterSymbol?>.GetInstance();
            var resultsBuilder = ArrayBuilder<VisitArgumentResult>.GetInstance(n);
            var previousDisableDiagnostics = _disableDiagnostics;
            for (int i = 0; i < n; i++)
            {
                var (parameter, _, parameterAnnotations, _) = GetCorrespondingParameter(i, parametersOpt, argsToParamsOpt, expanded);

                // we disable nullable warnings on default arguments
                _disableDiagnostics = defaultArguments[i] || previousDisableDiagnostics;
                resultsBuilder.Add(VisitArgumentEvaluate(arguments[i], GetRefKind(refKindsOpt, i), parameterAnnotations));
                visitedParameters.Add(parameter);
            }
            _disableDiagnostics = previousDisableDiagnostics;

            SetInvalidResult();
            visitedParameters.Free();
            return resultsBuilder.ToImmutableAndFree();
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
            BoundConversion? conversionOpt,
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
                        if (conversion is { Kind: ConversionKind.ImplicitUserDefined })
                        {
                            var argumentResultType = resultType.Type;
                            conversion = GenerateConversion(_conversions, argumentNoConversion, argumentResultType, parameterType.Type, fromExplicitCast: false, extensionMethodThisArgument: false);
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
                            stateForLambda: result.StateForLambda);

                        // If the parameter has annotations, we perform an additional check for nullable value types
                        if (CheckDisallowedNullAssignment(stateAfterConversion, parameterAnnotations, argumentNoConversion.Syntax.Location))
                        {
                            LearnFromNonNullTest(argumentNoConversion, ref State);
                        }
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

        /// <summary>Returns <see langword="true"/> if this is an assignment forbidden by DisallowNullAttribute, otherwise <see langword="false"/>.</summary>
        private bool CheckDisallowedNullAssignment(TypeWithState state, FlowAnalysisAnnotations annotations, Location location, BoundExpression? boundValueOpt = null)
        {
            if (boundValueOpt is { WasCompilerGenerated: true })
            {
                // We need to skip `return backingField;` in auto-prop getters
                return false;
            }

            // We do this extra check for types whose non-nullable version cannot be represented
            if (IsDisallowedNullAssignment(state, annotations))
            {
                ReportDiagnostic(ErrorCode.WRN_DisallowNullAttributeForbidsMaybeNullAssignment, location);
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
            VisitArgumentResult result,
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
                        learnFromPostConditions(argument, parameterType, parameterAnnotations);
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
                                UseLegacyWarnings(argument, result.LValueType));
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
                        else if (argument is BoundDiscardExpression discard)
                        {
                            SetAnalyzedNullability(discard, new VisitResult(parameterWithState, parameterWithState.ToTypeWithAnnotations(compilation)), isLvalue: true);
                        }

                        // track state by assigning from a fictional value from the parameter to the argument.
                        var parameterValue = new BoundParameter(argument.Syntax, parameter);

                        // If the argument type has annotations, we perform an additional check for nullable value types
                        CheckDisallowedNullAssignment(parameterWithState, leftAnnotations, argument.Syntax.Location);

                        AdjustSetValue(argument, ref parameterWithState);
                        trackNullableStateForAssignment(parameterValue, lValueType, MakeSlot(argument), parameterWithState, argument.IsSuppressed, parameterAnnotations);

                        // report warnings if parameter would unsafely let a null out in the worst case
                        if (!argument.IsSuppressed)
                        {
                            ReportNullableAssignmentIfNecessary(parameterValue, lValueType, worstCaseParameterWithState, UseLegacyWarnings(argument, result.LValueType));

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

            void learnFromPostConditions(BoundExpression argument, TypeWithAnnotations parameterType, FlowAnalysisAnnotations parameterAnnotations)
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
            bool expanded)
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
            if (expanded && parameter.Ordinal == parametersOpt.Length - 1 && type.IsSZArray())
            {
                type = ((ArrayTypeSymbol)type.Type).ElementTypeWithAnnotations;
                return (parameter, type, FlowAnalysisAnnotations.None, isExpandedParamsArgument: true);
            }

            return (parameter, type, GetParameterAnnotations(parameter), isExpandedParamsArgument: false);
        }

        private MethodSymbol InferMethodTypeArguments(
            MethodSymbol method,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded)
        {
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
                binder: _binder,
                expanded: expanded,
                parameterTypes: out ImmutableArray<TypeWithAnnotations> parameterTypes,
                parameterRefKinds: out ImmutableArray<RefKind> parameterRefKinds);
            refKinds.Free();

            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            var result = MethodTypeInferrer.Infer(
                _binder,
                _conversions,
                definition.TypeParameters,
                definition.ContainingType,
                parameterTypes,
                parameterRefKinds,
                arguments,
                ref discardedUseSiteInfo,
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
                    case BoundKind.UnconvertedObjectCreationExpression:
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

        private ImmutableArray<BoundExpression> GetArgumentsForMethodTypeInference(ImmutableArray<VisitArgumentResult> argumentResults, ImmutableArray<BoundExpression> arguments)
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
                var lambdaState = visitArgumentResult.StateForLambda;
                // Note: for `out` arguments, the argument result contains the declaration type (see `VisitArgumentEvaluate`)
                var argumentResult = visitArgumentResult.RValueType.ToTypeWithAnnotations(compilation);
                builder.Add(getArgumentForMethodTypeInference(arguments[i], argumentResult, lambdaState));
            }
            return builder.ToImmutableAndFree();

            BoundExpression getArgumentForMethodTypeInference(BoundExpression argument, TypeWithAnnotations argumentType, Optional<LocalState> lambdaState)
            {
                if (argument.Kind == BoundKind.Lambda)
                {
                    Debug.Assert(lambdaState.HasValue);
                    // MethodTypeInferrer must infer nullability for lambdas based on the nullability
                    // from flow analysis rather than the declared nullability. To allow that, we need
                    // to re-bind lambdas in MethodTypeInferrer.
                    return getUnboundLambda((BoundLambda)argument, GetVariableState(_variables, lambdaState.Value));
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

            static UnboundLambda getUnboundLambda(BoundLambda expr, VariableState variableState)
            {
                return expr.UnboundLambda.WithNullableState(variableState);
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
        private Conversion GenerateConversionForConditionalOperator(BoundExpression sourceExpression, TypeSymbol? sourceType, TypeSymbol destinationType, bool reportMismatch)
        {
            var conversion = GenerateConversion(_conversions, sourceExpression, sourceType, destinationType, fromExplicitCast: false, extensionMethodThisArgument: false);
            bool canConvertNestedNullability = conversion.Exists;
            if (!canConvertNestedNullability && reportMismatch && !sourceExpression.IsSuppressed)
            {
                ReportNullabilityMismatchInAssignment(sourceExpression.Syntax, GetTypeAsDiagnosticArgument(sourceType), destinationType);
            }
            return conversion;
        }

        private static Conversion GenerateConversion(Conversions conversions, BoundExpression? sourceExpression, TypeSymbol? sourceType, TypeSymbol destinationType, bool fromExplicitCast, bool extensionMethodThisArgument)
        {
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            bool useExpression = sourceType is null || UseExpressionForConversion(sourceExpression);
            if (extensionMethodThisArgument)
            {
                return conversions.ClassifyImplicitExtensionMethodThisArgConversion(
                    useExpression ? sourceExpression : null,
                    sourceType,
                    destinationType,
                    ref discardedUseSiteInfo);
            }
            return useExpression ?
                (fromExplicitCast ?
                    conversions.ClassifyConversionFromExpression(sourceExpression, destinationType, ref discardedUseSiteInfo, forCast: true) :
                    conversions.ClassifyImplicitConversionFromExpression(sourceExpression, destinationType, ref discardedUseSiteInfo)) :
                (fromExplicitCast ?
                    conversions.ClassifyConversionFromType(sourceType, destinationType, ref discardedUseSiteInfo, forCast: true) :
                    conversions.ClassifyImplicitConversionFromType(sourceType, destinationType, ref discardedUseSiteInfo));
        }

        /// <summary>
        /// Returns true if the expression should be used as the source when calculating
        /// a conversion from this expression, rather than using the type (with nullability)
        /// calculated by visiting this expression. Typically, that means expressions that
        /// do not have an explicit type but there are several other cases as well.
        /// (See expressions handled in ClassifyImplicitBuiltInConversionFromExpression.)
        /// </summary>
        private static bool UseExpressionForConversion([NotNullWhen(true)] BoundExpression? value)
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
        private TypeWithState GetAdjustedResult(TypeWithState type, int slot)
        {
            if (this.State.HasValue(slot))
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
        private static Symbol AsMemberOfType(TypeSymbol? type, Symbol symbol)
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
            Debug.Assert(false); // If this assert fails, add an appropriate test.
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
                    useLegacyWarnings: fromExplicitCast,
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
                    this.State[slot] = NullableFlowState.NotNull;
                    TrackNullableStateOfTupleElements(slot, tupleOpt, arguments, elementTypes, argsToParamsOpt: default, useRestField: false);
                }

                tupleOpt = tupleOpt.WithElementTypes(elementTypesWithAnnotations);
                if (!_disableDiagnostics)
                {
                    var locations = tupleOpt.TupleElements.SelectAsArray((element, location) => element.Locations.FirstOrDefault() ?? location, node.Syntax.Location);
                    tupleOpt.CheckConstraints(new ConstraintsHelper.CheckConstraintsArgs(compilation, _conversions, includeNullability: true, node.Syntax.Location, diagnostics: null),
                                              typeSyntax: node.Syntax, locations, nullabilityDiagnosticsOpt: new BindingDiagnosticBag(Diagnostics));
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
                    var argOrdinal = GetArgumentOrdinalFromParameterOrdinal(i);
                    trackState(values[argOrdinal], tupleElements[i], types[argOrdinal]);
                }
                if (useRestField &&
                    values.Length == NamedTypeSymbol.ValueTupleRestPosition &&
                    tupleType.GetMembers(NamedTypeSymbol.ValueTupleRestFieldName).FirstOrDefault() is FieldSymbol restField)
                {
                    var argOrdinal = GetArgumentOrdinalFromParameterOrdinal(NamedTypeSymbol.ValueTupleRestPosition - 1);
                    trackState(values[argOrdinal], restField, types[argOrdinal]);
                }
            }

            void trackState(BoundExpression value, FieldSymbol field, TypeWithState valueType)
            {
                int targetSlot = GetOrCreateSlot(field, slot);
                TrackNullableStateForAssignment(value, field.TypeWithAnnotations, targetSlot, valueType, MakeSlot(value));
            }

            int GetArgumentOrdinalFromParameterOrdinal(int parameterOrdinal)
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
                TrackNullableStateForAssignment(value, symbol!.GetTypeOrReturnType(), targetSlot, valueType, valueSlot);
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

        public override BoundNode? VisitTupleBinaryOperator(BoundTupleBinaryOperator node)
        {
            base.VisitTupleBinaryOperator(node);
            SetNotNullResult(node);
            return null;
        }

        private void ReportNullabilityMismatchWithTargetDelegate(Location location, TypeSymbol targetType, MethodSymbol targetInvokeMethod, MethodSymbol sourceInvokeMethod, bool invokedAsExtensionMethod)
        {
            SourceMemberContainerTypeSymbol.CheckValidNullableMethodOverride(
                compilation,
                targetInvokeMethod,
                sourceInvokeMethod,
                new BindingDiagnosticBag(Diagnostics),
                reportBadDelegateReturn,
                reportBadDelegateParameter,
                extraArgument: (targetType, location),
                invokedAsExtensionMethod: invokedAsExtensionMethod);

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

        private void ReportNullabilityMismatchWithTargetDelegate(Location location, NamedTypeSymbol delegateType, UnboundLambda unboundLambda)
        {
            if (!unboundLambda.HasExplicitlyTypedParameterList)
            {
                return;
            }

            var invoke = delegateType?.DelegateInvokeMethod;
            if (invoke is null)
            {
                return;
            }

            Debug.Assert(delegateType is object);
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
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            return !_conversions.ClassifyImplicitConversionFromType(sourceType, destinationType, ref discardedUseSiteInfo).Exists;
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
        /// Apply the conversion to the type of the operand and return the resulting type. (If the
        /// operand does not have an explicit type, the operand expression is used for the type.)
        /// If `checkConversion` is set, the incoming conversion is assumed to be from binding and will be
        /// re-calculated, this time considering nullability. (Note that the conversion calculation considers
        /// nested nullability only. The caller is responsible for checking the top-level nullability of
        /// the type returned by this method.) `trackMembers` should be set if the nullability of any
        /// members of the operand should be copied to the converted result when possible.
        /// </summary>
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
            bool extensionMethodThisArgument = false,
            Optional<LocalState> stateForLambda = default,
            bool trackMembers = false,
            Location? diagnosticLocationOpt = null)
        {
            Debug.Assert(!trackMembers || !IsConditionalState);
            Debug.Assert(conversionOperand != null);

            NullableFlowState resultState = NullableFlowState.NotNull;
            bool canConvertNestedNullability = true;
            bool isSuppressed = false;
            diagnosticLocationOpt ??= (conversionOpt ?? conversionOperand).Syntax.GetLocation();

            if (conversionOperand.IsSuppressed == true)
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
                        if (reportRemainingWarnings)
                        {
                            ReportNullabilityMismatchWithTargetDelegate(diagnosticLocationOpt, targetType, invokeSignature, method, conversion.IsExtensionMethod);
                        }
                    }
                    resultState = NullableFlowState.NotNull;
                    break;

                    static (MethodSymbol invokeSignature, ImmutableArray<ParameterSymbol>) getDelegateOrFunctionPointerInfo(TypeSymbol targetType)
                        => targetType switch
                        {
                            NamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { Parameters: { } parameters } signature } => (signature, parameters),
                            FunctionPointerTypeSymbol { Signature: { Parameters: { } parameters } signature } => (signature, parameters),
                            _ => throw ExceptionUtilities.UnexpectedValue(targetType)
                        };

                case ConversionKind.AnonymousFunction:
                    if (conversionOperand is BoundLambda lambda)
                    {
                        var delegateType = targetType.GetDelegateType();
                        VisitLambda(lambda, delegateType, stateForLambda);
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

                case ConversionKind.ObjectCreation:
                case ConversionKind.SwitchExpression:
                case ConversionKind.ConditionalExpression:
                    return operandType;

                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.ImplicitUserDefined:
                    return VisitUserDefinedConversion(conversionOpt, conversionOperand, conversion, targetTypeWithNullability, operandType, useLegacyWarnings, assignmentKind, parameterOpt, reportTopLevelWarnings, reportRemainingWarnings, diagnosticLocationOpt);

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
                            ReportDiagnostic(ErrorCode.WRN_UnboxPossibleNull, diagnosticLocationOpt);
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
                        conversion = GenerateConversion(_conversions, conversionOperand, operandType.Type, targetType, fromExplicitCast, extensionMethodThisArgument);
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
                    ReportNullableAssignmentIfNecessary(conversionOperand, targetTypeWithNullability, resultType, useLegacyWarnings, assignmentKind, parameterOpt, diagnosticLocationOpt);
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
                diagnosticLocationOpt: diagnosticLocation);

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
            if (!isLiftedConversion && CheckDisallowedNullAssignment(operandType, parameterAnnotations, conversionOperand.Syntax.Location))
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
            ParameterSymbol? parameterOpt,
            bool reportWarnings,
            bool fromExplicitCast,
            Location diagnosticLocation)
        {
            Debug.Assert(operandType.Type is object);
            Debug.Assert(diagnosticLocation != null);
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            var conversion = _conversions.ClassifyStandardConversion(null, operandType.Type, targetType.Type, ref discardedUseSiteInfo);
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

        public override BoundNode? VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            Debug.Assert(node.Type.IsDelegateType());

            if (node.MethodOpt?.OriginalDefinition is LocalFunctionSymbol localFunc)
            {
                VisitLocalFunctionUse(localFunc);
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
                            method = CheckMethodGroupReceiverNullability(group, delegateType.DelegateInvokeMethod.Parameters, method, node.IsExtensionMethod);
                            if (!group.IsSuppressed)
                            {
                                ReportNullabilityMismatchWithTargetDelegate(group.Syntax.Location, delegateType, delegateType.DelegateInvokeMethod, method, node.IsExtensionMethod);
                            }
                        }
                        SetAnalyzedNullability(group, default);
                    }
                    break;
                case BoundLambda lambda:
                    {
                        VisitLambda(lambda, delegateType);
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
                            ReportNullabilityMismatchWithTargetDelegate(arg.Syntax.Location, delegateType, delegateType.DelegateInvokeMethod, argType.DelegateInvokeMethod(), invokedAsExtensionMethod: false);
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
                    method = InferMethodTypeArguments(method, arguments.ToImmutableAndFree(), argumentRefKindsOpt: default, argsToParamsOpt: default, expanded: false);
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

        public override BoundNode? VisitLambda(BoundLambda node)
        {
            // Note: actual lambda analysis happens after this call (primarily in VisitConversion).
            // Here we just indicate that a lambda expression produces a non-null value.
            SetNotNullResult(node);
            return null;
        }

        private void VisitLambda(BoundLambda node, NamedTypeSymbol? delegateTypeOpt, Optional<LocalState> initialState = default)
        {
            Debug.Assert(delegateTypeOpt?.IsDelegateType() != false);

            var delegateInvokeMethod = delegateTypeOpt?.DelegateInvokeMethod;
            bool useDelegateInvokeParameterTypes = UseDelegateInvokeParameterTypes(node, delegateInvokeMethod);
            if (useDelegateInvokeParameterTypes && _snapshotBuilderOpt is object)
            {
                SetUpdatedSymbol(node, node.Symbol, delegateTypeOpt!);
            }

            AnalyzeLocalFunctionOrLambda(
                node,
                node.Symbol,
                initialState.HasValue ? initialState.Value : State.Clone(),
                delegateInvokeMethod,
                useDelegateInvokeParameterTypes);
        }

        private static bool UseDelegateInvokeParameterTypes(BoundLambda lambda, MethodSymbol? delegateInvokeMethod)
        {
            return delegateInvokeMethod is object && !lambda.UnboundLambda.HasExplicitlyTypedParameterList;
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
            switch (left)
            {
                // when binding initializers, we treat assignments to auto-properties or field-like events as direct assignments to the underlying field.
                // in order to track member state based on these initializers, we need to see the assignment in terms of the associated member
                case BoundFieldAccess { ExpressionSymbol: FieldSymbol { AssociatedSymbol: PropertySymbol autoProperty } } fieldAccess:
                    left = new BoundPropertyAccess(fieldAccess.Syntax, fieldAccess.ReceiverOpt, autoProperty, LookupResultKind.Viable, autoProperty.Type, fieldAccess.HasErrors);
                    break;
                case BoundFieldAccess { ExpressionSymbol: FieldSymbol { AssociatedSymbol: EventSymbol @event } } fieldAccess:
                    left = new BoundEventAccess(fieldAccess.Syntax, fieldAccess.ReceiverOpt, @event, isUsableAsField: true, LookupResultKind.Viable, @event.Type, fieldAccess.HasErrors);
                    break;
            }

            var right = node.Right;
            VisitLValue(left);
            // we may enter a conditional state for error scenarios on the LHS.
            Unsplit();

            FlowAnalysisAnnotations leftAnnotations = GetLValueAnnotations(left);
            TypeWithAnnotations declaredType = LvalueResultType;
            TypeWithAnnotations leftLValueType = ApplyLValueAnnotations(declaredType, leftAnnotations);

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

                AdjustSetValue(left, ref rightState);
                TrackNullableStateForAssignment(right, leftLValueType, MakeSlot(left), rightState, MakeSlot(right));

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
                BoundFieldAccess field => getFieldAnnotations(field.FieldSymbol),
                BoundObjectInitializerMember { MemberSymbol: PropertySymbol prop } => prop.GetFlowAnalysisAnnotations(),
                BoundObjectInitializerMember { MemberSymbol: FieldSymbol field } => getFieldAnnotations(field),
                BoundParameter { ParameterSymbol: ParameterSymbol parameter }
                    => ToInwardAnnotations(GetParameterAnnotations(parameter) & ~FlowAnalysisAnnotations.NotNull), // NotNull is enforced upon method exit
                _ => FlowAnalysisAnnotations.None
            };

            return annotations & (FlowAnalysisAnnotations.DisallowNull | FlowAnalysisAnnotations.AllowNull);

            static FlowAnalysisAnnotations getFieldAnnotations(FieldSymbol field)
            {
                return field.AssociatedSymbol is PropertySymbol property ?
                    property.GetFlowAnalysisAnnotations() :
                    field.FlowAnalysisAnnotations;
            }
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

        private static bool UseLegacyWarnings(BoundExpression expr, TypeWithAnnotations exprType)
        {
            switch (expr.Kind)
            {
                case BoundKind.Local:
                    return expr.GetRefKind() == RefKind.None;
                case BoundKind.Parameter:
                    RefKind kind = ((BoundParameter)expr).ParameterSymbol.RefKind;
                    return kind == RefKind.None;
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
                VisitTupleDeconstructionArguments(variables, conversion.UnderlyingConversions, right);
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
            var deconstructMethod = invocation?.Method;

            if (deconstructMethod is object)
            {
                Debug.Assert(invocation is object);
                Debug.Assert(rightResult.Type is object);

                int n = variables.Count;
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
                        placeholderArgs.Add(CreatePlaceholderIfNecessary(right, rightResult.ToTypeWithAnnotations(compilation)));
                        for (int i = 0; i < n; i++)
                        {
                            placeholderArgs.Add(new BoundExpressionWithNullability(variables[i].Expression.Syntax, variables[i].Expression, NullableAnnotation.Oblivious, conversion.DeconstructionInfo.OutputPlaceholders[i].Type));
                        }
                        deconstructMethod = InferMethodTypeArguments(deconstructMethod, placeholderArgs.ToImmutableAndFree(), invocation.ArgumentRefKindsOpt, invocation.ArgsToParamsOpt, invocation.Expanded);

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
                    var argConversion = RemoveConversion(invocation.Arguments[0], includeExplicitConversions: false).conversion;
                    CheckExtensionMethodThisNullability(right, argConversion, deconstructMethod.Parameters[0], rightResult);
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
                            new VisitArgumentResult(new VisitResult(variable.Type.ToTypeWithState(), variable.Type), stateForLambda: default),
                            notNullParametersOpt: null, compareExchangeInfoOpt: default);
                    }
                }
            }
        }

        private void VisitTupleDeconstructionArguments(ArrayBuilder<DeconstructionVariable> variables, ImmutableArray<Conversion> conversions, BoundExpression right)
        {
            int n = variables.Count;
            var rightParts = GetDeconstructionRightParts(right);
            Debug.Assert(rightParts.Length == n);

            for (int i = 0; i < n; i++)
            {
                var variable = variables[i];
                var underlyingConversion = conversions[i];
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
                    CheckDisallowedNullAssignment(valueType, leftAnnotations, right.Syntax.Location);

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

            if (expr.Type is NamedTypeSymbol { IsTupleType: true } tupleType)
            {
                // https://github.com/dotnet/roslyn/issues/33011: Should include conversion.UnderlyingConversions[i].
                // For instance, Boxing conversions (see Deconstruction_ImplicitBoxingConversion_02) and
                // ImplicitNullable conversions (see Deconstruction_ImplicitNullableConversion_02).
                var fields = tupleType.TupleElements;
                return fields.SelectAsArray((f, e) => (BoundExpression)new BoundFieldAccess(e.Syntax, e, f, constantValueOpt: null), expr);
            }

            throw ExceptionUtilities.Unreachable;
        }

        public override BoundNode? VisitIncrementOperator(BoundIncrementOperator node)
        {
            Debug.Assert(!IsConditionalState);

            var operandType = VisitRvalueWithState(node.Operand);
            var operandLvalue = LvalueResultType;
            bool setResult = false;

            if (this.State.Reachable)
            {
                // https://github.com/dotnet/roslyn/issues/29961 Update increment method based on operand type.
                MethodSymbol? incrementOperator = (node.OperatorKind.IsUserDefined() && node.MethodOpt?.ParameterCount == 1) ? node.MethodOpt : null;
                TypeWithAnnotations targetTypeOfOperandConversion;
                AssignmentKind assignmentKind = AssignmentKind.Assignment;
                ParameterSymbol? parameter = null;

                // Analyze operator call properly (honoring [Disallow|Allow|Maybe|NotNull] attribute annotations) https://github.com/dotnet/roslyn/issues/32671
                // https://github.com/dotnet/roslyn/issues/29961 Update conversion method based on operand type.
                if (node.OperandConversion.IsUserDefined && node.OperandConversion.Method?.ParameterCount == 1)
                {
                    targetTypeOfOperandConversion = node.OperandConversion.Method.ReturnTypeWithAnnotations;
                }
                else if (incrementOperator is object)
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

                var operandTypeWithAnnotations = operandType.ToTypeWithAnnotations(compilation);
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

        public override BoundNode? VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            var left = node.Left;
            var right = node.Right;
            Visit(left);
            TypeWithAnnotations declaredType = LvalueResultType;
            TypeWithAnnotations leftLValueType = declaredType;
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
            TypeWithState rightType = VisitRvalueWithState(right);
            if ((object)node.Operator.ReturnType != null)
            {
                if (node.Operator.Kind.IsUserDefined() && (object)node.Operator.Method != null && node.Operator.Method.ParameterCount == 2)
                {
                    MethodSymbol method = node.Operator.Method;
                    VisitArguments(node, ImmutableArray.Create(node.Left, right), method.ParameterRefKinds, method.Parameters, argsToParamsOpt: default, defaultArguments: default,
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

            AdjustSetValue(left, ref resultType);
            TrackNullableStateForAssignment(node, leftLValueType, MakeSlot(node.Left), resultType);

            SetResultType(node, resultType);
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

        private void ReportArgumentWarnings(BoundExpression argument, TypeWithState argumentType, ParameterSymbol parameter)
        {
            var paramType = parameter.TypeWithAnnotations;
            ReportNullableAssignmentIfNecessary(argument, paramType, argumentType, useLegacyWarnings: false, AssignmentKind.Argument, parameterOpt: parameter);

            if (argumentType.Type is { } argType && IsNullabilityMismatch(paramType.Type, argType))
            {
                ReportNullabilityMismatchInArgument(argument.Syntax, argType, parameter, paramType.Type, forOutput: false);
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

        private void SplitIfBooleanConstant(BoundExpression node)
        {
            if (node.ConstantValue is { IsBoolean: true, BooleanValue: bool booleanValue })
            {
                Split();
                if (booleanValue)
                {
                    StateWhenFalse = UnreachableState();
                }
                else
                {
                    StateWhenTrue = UnreachableState();
                }
            }
        }

        public override BoundNode? VisitFieldAccess(BoundFieldAccess node)
        {
            var updatedSymbol = VisitMemberAccess(node, node.ReceiverOpt, node.FieldSymbol);

            SplitIfBooleanConstant(node);
            SetUpdatedSymbol(node, node.FieldSymbol, updatedSymbol);
            return null;
        }

        public override BoundNode? VisitPropertyAccess(BoundPropertyAccess node)
        {
            var property = node.PropertySymbol;
            var updatedMember = VisitMemberAccess(node, node.ReceiverOpt, property);

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

            SetUpdatedSymbol(node, property, updatedMember);
            return null;
        }

        public override BoundNode? VisitIndexerAccess(BoundIndexerAccess node)
        {
            var receiverOpt = node.ReceiverOpt;
            var receiverType = VisitRvalueWithState(receiverOpt).Type;
            // https://github.com/dotnet/roslyn/issues/30598: Mark receiver as not null
            // after indices have been visited, and only if the receiver has not changed.
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

        public override BoundNode? VisitIndexOrRangePatternIndexerAccess(BoundIndexOrRangePatternIndexerAccess node)
        {
            BoundExpression receiver = node.Receiver;
            var receiverType = VisitRvalueWithState(receiver).Type;
            // https://github.com/dotnet/roslyn/issues/30598: Mark receiver as not null
            // after indices have been visited, and only if the receiver has not changed.
            _ = CheckPossibleNullReceiver(receiver);

            VisitRvalue(node.Argument);
            var patternSymbol = node.PatternSymbol;
            if (receiverType is object)
            {
                patternSymbol = AsMemberOfType(receiverType, patternSymbol);
            }

            SetLvalueResultType(node, patternSymbol.GetTypeOrReturnType());
            SetUpdatedSymbol(node, node.PatternSymbol, patternSymbol);
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
            var memberAnnotations = GetRValueAnnotations(member);
            var resultType = ApplyUnconditionalAnnotations(type.ToTypeWithState(), memberAnnotations);

            // We are supposed to track information for the node. Use whatever we managed to
            // accumulate so far.
            if (PossiblyNullableType(resultType.Type))
            {
                int slot = MakeMemberSlot(receiverOpt, member);
                if (this.State.HasValue(slot))
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

        private int GetNullableOfTValueSlot(TypeSymbol containingType, int containingSlot, out Symbol? valueProperty, bool forceSlotEvenIfEmpty = false)
        {
            Debug.Assert(containingType.IsNullableType());
            Debug.Assert(TypeSymbol.Equals(NominalSlotType(containingSlot), containingType, TypeCompareKind.ConsiderEverything2));

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

            if (node.EnumeratorInfoOpt?.GetEnumeratorInfo is { Method: { IsExtensionMethod: true, Parameters: var parameters } } enumeratorMethodInfo)
            {
                // this is case 7
                // We do not need to do this same analysis for non-extension methods because they do not have generic parameters that
                // can be inferred from usage like extension methods can. We don't warn about default arguments at the call site, so
                // there's nothing that can be learned from the non-extension case.
                var (method, results, _) = VisitArguments(
                    node,
                    enumeratorMethodInfo.Arguments,
                    refKindsOpt: default,
                    parameters,
                    argsToParamsOpt: enumeratorMethodInfo.ArgsToParamsOpt,
                    defaultArguments: enumeratorMethodInfo.DefaultArguments,
                    expanded: false,
                    invokedAsExtensionMethod: true,
                    enumeratorMethodInfo.Method);

                targetTypeWithAnnotations = results[0].LValueType;
                reinferredGetEnumeratorMethod = method;
            }
            else if (conversion.IsIdentity ||
                (conversion.Kind == ConversionKind.ExplicitReference && resultType.SpecialType == SpecialType.System_String))
            {
                // This is case 3 or 6.
                targetTypeWithAnnotations = resultTypeWithState.ToTypeWithAnnotations(compilation);
            }
            else if (conversion.IsImplicit)
            {
                bool isAsync = node.AwaitOpt != null;
                if (node.Expression.Type!.SpecialType == SpecialType.System_Collections_IEnumerable)
                {
                    // If this is a conversion to IEnumerable (non-generic), nothing to do. This is cases 1, 2, and 5.
                    targetTypeWithAnnotations = TypeWithAnnotations.Create(node.Expression.Type);
                }
                else if (ForEachLoopBinder.IsIEnumerableT(node.Expression.Type.OriginalDefinition, isAsync, compilation))
                {
                    // This is case 4. We need to look for the IEnumerable<T> that this reinferred expression implements,
                    // so that we pick up any nested type substitutions that could have occurred.
                    var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    targetTypeWithAnnotations = TypeWithAnnotations.Create(ForEachLoopBinder.GetIEnumerableOfT(resultType, isAsync, compilation, ref discardedUseSiteInfo, out bool foundMultiple));
                    Debug.Assert(!foundMultiple);
                    Debug.Assert(targetTypeWithAnnotations.HasType);
                }
                else
                {
                    // This is case 8. There was not a successful binding, as a successful binding will _always_ generate one of the
                    // above conversions. Just return, as we want to suppress further errors.
                    return;
                }
            }
            else
            {
                // This is also case 8.
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

            bool reportedDiagnostic = node.EnumeratorInfoOpt?.GetEnumeratorInfo.Method is { IsExtensionMethod: true }
                ? false
                : CheckPossibleNullReceiver(expr);

            SetAnalyzedNullability(node.Expression, new VisitResult(convertedResult, convertedResult.ToTypeWithAnnotations(compilation)));

            TypeWithState currentPropertyGetterTypeWithState;

            if (node.EnumeratorInfoOpt is null)
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
                    TypeWithAnnotations.Create(node.EnumeratorInfoOpt.ElementType, NullableAnnotation.NotAnnotated).ToTypeWithState();
            }
            else
            {
                // Reinfer the return type of the node.Expression.GetEnumerator().Current property, so that if
                // the collection changed nested generic types we pick up those changes.
                reinferredGetEnumeratorMethod ??= (MethodSymbol)AsMemberOfType(convertedResult.Type, node.EnumeratorInfoOpt.GetEnumeratorInfo.Method);
                var enumeratorReturnType = GetReturnTypeWithState(reinferredGetEnumeratorMethod);

                if (enumeratorReturnType.State != NullableFlowState.NotNull)
                {
                    if (!reportedDiagnostic && !(node.Expression is BoundConversion { Operand: { IsSuppressed: true } }))
                    {
                        ReportDiagnostic(ErrorCode.WRN_NullReferenceReceiver, expr.Syntax.GetLocation());
                    }
                }

                var currentPropertyGetter = (MethodSymbol)AsMemberOfType(enumeratorReturnType.Type, node.EnumeratorInfoOpt.CurrentPropertyGetter);

                currentPropertyGetterTypeWithState = ApplyUnconditionalAnnotations(
                    currentPropertyGetter.ReturnTypeWithAnnotations.ToTypeWithState(),
                    currentPropertyGetter.ReturnTypeFlowAnalysisAnnotations);

                // Analyze `await MoveNextAsync()`
                if (node.AwaitOpt is { AwaitableInstancePlaceholder: BoundAwaitableValuePlaceholder moveNextPlaceholder } awaitMoveNextInfo)
                {
                    var moveNextAsyncMethod = (MethodSymbol)AsMemberOfType(reinferredGetEnumeratorMethod.ReturnType, node.EnumeratorInfoOpt.MoveNextInfo.Method);

                    EnsureAwaitablePlaceholdersInitialized();
                    var result = new VisitResult(GetReturnTypeWithState(moveNextAsyncMethod), moveNextAsyncMethod.ReturnTypeWithAnnotations);
                    _awaitablePlaceholdersOpt.Add(moveNextPlaceholder, (moveNextPlaceholder, result));
                    Visit(awaitMoveNextInfo);
                    _awaitablePlaceholdersOpt.Remove(moveNextPlaceholder);
                }

                // Analyze `await DisposeAsync()`
                if (node.EnumeratorInfoOpt is { NeedsDisposal: true, DisposeAwaitableInfo: BoundAwaitableInfo awaitDisposalInfo })
                {
                    var disposalPlaceholder = awaitDisposalInfo.AwaitableInstancePlaceholder;
                    bool addedPlaceholder = false;
                    if (node.EnumeratorInfoOpt.PatternDisposeInfo is { Method: var originalDisposeMethod }) // no statically known Dispose method if doing a runtime check
                    {
                        Debug.Assert(disposalPlaceholder is not null);
                        var disposeAsyncMethod = (MethodSymbol)AsMemberOfType(reinferredGetEnumeratorMethod.ReturnType, originalDisposeMethod);
                        EnsureAwaitablePlaceholdersInitialized();
                        var result = new VisitResult(GetReturnTypeWithState(disposeAsyncMethod), disposeAsyncMethod.ReturnTypeWithAnnotations);
                        _awaitablePlaceholdersOpt.Add(disposalPlaceholder, (disposalPlaceholder, result));
                        addedPlaceholder = true;
                    }

                    Visit(awaitDisposalInfo);

                    if (addedPlaceholder)
                    {
                        _awaitablePlaceholdersOpt!.Remove(disposalPlaceholder!);
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
                            if (IsNullabilityMismatch(sourceType, destinationType))
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
                            Conversion conversion = node.ElementConversion.Kind == ConversionKind.UnsetConversionKind
                                ? _conversions.ClassifyImplicitConversionFromType(sourceType.Type, destinationType.Type, ref discardedUseSiteInfo)
                                : node.ElementConversion;
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
                                diagnosticLocationOpt: variableLocation);
                        }

                        // In non-error cases we'll only run this loop a single time. In error cases we'll set the nullability of the VariableType multiple times, but at least end up with something
                        SetAnalyzedNullability(node.IterationVariableType, new VisitResult(resultForType, destinationType), isLvalue: true);
                        state = result.State;
                    }

                    int slot = GetOrCreateSlot(iterationVariable);
                    if (slot > 0)
                    {
                        this.State[slot] = state;
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
            throw ExceptionUtilities.Unreachable;
        }

        public override BoundNode? VisitDynamicObjectInitializerMember(BoundDynamicObjectInitializerMember node)
        {
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode? VisitBadExpression(BoundBadExpression node)
        {
            var result = base.VisitBadExpression(node);
            var type = TypeWithAnnotations.Create(node.Type);
            SetLvalueResultType(node, type);
            return result;
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
                        method = (MethodSymbol)AsMemberOfType(operandType.Type!.StrippedType(), method);
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
            MethodSymbol? logicalOperator = null;
            MethodSymbol? trueFalseOperator = null;
            BoundExpression? left = null;

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

            Debug.Assert(trueFalseOperator is null || logicalOperator is object);
            Debug.Assert(logicalOperator is null || left is object);

            // Analyze operator call properly (honoring [Disallow|Allow|Maybe|NotNull] attribute annotations) https://github.com/dotnet/roslyn/issues/32671
            if (trueFalseOperator is object)
            {
                ReportArgumentWarnings(left!, leftType, trueFalseOperator.Parameters[0]);
            }

            if (logicalOperator is object)
            {
                ReportArgumentWarnings(left!, leftType, logicalOperator.Parameters[0]);
            }

            Visit(right);
            TypeWithState rightType = ResultType;

            SetResultType(node, InferResultNullabilityOfBinaryLogicalOperator(node, leftType, rightType));

            if (logicalOperator is object)
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

        public override BoundNode? VisitAwaitExpression(BoundAwaitExpression node)
        {
            var result = base.VisitAwaitExpression(node);
            var awaitableInfo = node.AwaitableInfo;
            var placeholder = awaitableInfo.AwaitableInstancePlaceholder;
            Debug.Assert(placeholder is object);

            EnsureAwaitablePlaceholdersInitialized();
            _awaitablePlaceholdersOpt.Add(placeholder, (node.Expression, _visitResult));
            Visit(awaitableInfo);
            _awaitablePlaceholdersOpt.Remove(placeholder);

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
                    this.State[slot] = NullableFlowState.NotNull;
                    InheritNullableStateOfTrackableStruct(type, slot, valueSlot: -1, isDefaultValue: true);
                }
            }

            // https://github.com/dotnet/roslyn/issues/33344: this fails to produce an updated tuple type for a default expression
            // (should produce nullable element types for those elements that are of reference types)
            SetResultType(node, TypeWithState.ForType(type));
            return result;
        }

        public override BoundNode? VisitIsOperator(BoundIsOperator node)
        {
            Debug.Assert(!this.IsConditionalState);

            var operand = node.Operand;
            var typeExpr = node.TargetType;

            var result = base.VisitIsOperator(node);
            Debug.Assert(node.Type.SpecialType == SpecialType.System_Boolean);

            Split();
            LearnFromNonNullTest(operand, ref StateWhenTrue);
            if (typeExpr.Type?.SpecialType == SpecialType.System_Object)
            {
                LearnFromNullTest(operand, ref StateWhenFalse);
            }

            VisitTypeExpression(typeExpr);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode? VisitAsOperator(BoundAsOperator node)
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
            VisitArgumentsEvaluate(node.Syntax, node.Arguments, node.ArgumentRefKindsOpt, parametersOpt: default, argsToParamsOpt: default, defaultArguments: default, expanded: false);
            Debug.Assert(node.Type is null);
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode? VisitLiteral(BoundLiteral node)
        {
            var result = base.VisitLiteral(node);

            Debug.Assert(!IsConditionalState);
            SetResultType(node, TypeWithState.Create(node.Type, node.Type?.CanContainNull() != false && node.ConstantValue?.IsNull == true ? NullableFlowState.MaybeDefault : NullableFlowState.NotNull));

            SplitIfBooleanConstant(node);
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

            VisitArgumentsEvaluate(node.Syntax, node.Arguments, node.ArgumentRefKindsOpt, parametersOpt: default, argsToParamsOpt: default, defaultArguments: default, expanded: false);
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
            if (node.Argument.ConstantValue?.IsNull != true
                && MakeMemberSlot(receiverOpt, @event) is > 0 and var memberSlot)
            {
                this.State[memberSlot] = node.IsAddition
                    ? this.State[memberSlot].Meet(ResultType.State)
                    : NullableFlowState.MaybeNull;
            }
            SetNotNullResult(node); // https://github.com/dotnet/roslyn/issues/29969 Review whether this is the correct result
            return null;
        }

        public override BoundNode? VisitDynamicObjectCreationExpression(BoundDynamicObjectCreationExpression node)
        {
            Debug.Assert(!IsConditionalState);
            var arguments = node.Arguments;
            var argumentResults = VisitArgumentsEvaluate(node.Syntax, arguments, node.ArgumentRefKindsOpt, parametersOpt: default, argsToParamsOpt: default, defaultArguments: default, expanded: false);
            VisitObjectOrDynamicObjectCreation(node, arguments, argumentResults, node.InitializerExpressionOpt);
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
            throw ExceptionUtilities.Unreachable;
        }

        public override BoundNode? VisitNoPiaObjectCreationExpression(BoundNoPiaObjectCreationExpression node)
        {
            var result = base.VisitNoPiaObjectCreationExpression(node);
            SetResultType(node, TypeWithState.Create(node.Type, NullableFlowState.NotNull));
            return result;
        }

        public override BoundNode? VisitNewT(BoundNewT node)
        {
            VisitObjectOrDynamicObjectCreation(node, ImmutableArray<BoundExpression>.Empty, ImmutableArray<VisitArgumentResult>.Empty, node.InitializerExpressionOpt);
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

        public override BoundNode? VisitStackAllocArrayCreation(BoundStackAllocArrayCreation node)
        {
            var result = base.VisitStackAllocArrayCreation(node);
            Debug.Assert(node.Type is null || node.Type.IsErrorType() || node.Type.IsRefLikeType);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode? VisitDynamicIndexerAccess(BoundDynamicIndexerAccess node)
        {
            var receiver = node.Receiver;
            VisitRvalue(receiver);
            // https://github.com/dotnet/roslyn/issues/30598: Mark receiver as not null
            // after indices have been visited, and only if the receiver has not changed.
            _ = CheckPossibleNullReceiver(receiver);
            VisitArgumentsEvaluate(node.Syntax, node.Arguments, node.ArgumentRefKindsOpt, parametersOpt: default, argsToParamsOpt: default, defaultArguments: default, expanded: false);
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
                new VisitArgumentResult(new VisitResult(result, result.ToTypeWithAnnotations(compilation)), stateForLambda: default),
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

        public override BoundNode? VisitInterpolatedString(BoundInterpolatedString node)
        {
            var result = base.VisitInterpolatedString(node);
            SetResultType(node, TypeWithState.Create(node.Type, NullableFlowState.NotNull));
            return result;
        }

        public override BoundNode? VisitStringInsert(BoundStringInsert node)
        {
            var result = base.VisitStringInsert(node);
            SetUnknownResultNullability(node);
            return result;
        }

        public override BoundNode? VisitConvertedStackAllocExpression(BoundConvertedStackAllocExpression node)
        {
            var result = base.VisitConvertedStackAllocExpression(node);
            SetNotNullResult(node);
            return result;
        }

        public override BoundNode? VisitDiscardExpression(BoundDiscardExpression node)
        {
            var result = TypeWithAnnotations.Create(node.Type);
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

        public override BoundNode? VisitLockStatement(BoundLockStatement node)
        {
            VisitRvalue(node.Argument);
            _ = CheckPossibleNullReceiver(node.Argument);
            VisitStatement(node.Body);
            return null;
        }

        public override BoundNode? VisitAttribute(BoundAttribute node)
        {
            VisitArguments(node, node.ConstructorArguments, ImmutableArray<RefKind>.Empty, node.Constructor, argsToParamsOpt: node.ConstructorArgumentsToParamsOpt, defaultArguments: default, expanded: node.ConstructorExpanded, invokedAsExtensionMethod: false);
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
            SetNotNullResult(node);
            return null;
        }

        public override BoundNode? VisitObjectOrCollectionValuePlaceholder(BoundObjectOrCollectionValuePlaceholder node)
        {
            SetNotNullResult(node);
            return null;
        }

        [MemberNotNull(nameof(_awaitablePlaceholdersOpt))]
        private void EnsureAwaitablePlaceholdersInitialized()
        {
            _awaitablePlaceholdersOpt ??= PooledDictionary<BoundAwaitableValuePlaceholder, (BoundExpression AwaitableExpression, VisitResult Result)>.GetInstance();
        }

        public override BoundNode? VisitAwaitableValuePlaceholder(BoundAwaitableValuePlaceholder node)
        {
            if (_awaitablePlaceholdersOpt != null && _awaitablePlaceholdersOpt.TryGetValue(node, out var value))
            {
                var result = value.Result;
                SetResult(node, result.RValueType, result.LValueType);
            }
            else
            {
                SetNotNullResult(node);
            }
            return null;
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

            private static LocalState CreateReachableOrUnreachableState(Variables variables, bool reachable)
            {
                var container = variables.Container is null ?
                    null :
                    new Boxed(CreateReachableOrUnreachableState(variables.Container, reachable));
                int capacity = reachable ? variables.NextAvailableIndex : 1;
                return new LocalState(variables.Id, container, CreateBitVector(capacity, reachable));
            }

            public LocalState CreateNestedMethodState(Variables variables)
            {
                Debug.Assert(Id == variables.Container!.Id);
                return new LocalState(variables.Id, container: new Boxed(this), CreateBitVector(capacity: variables.NextAvailableIndex, reachable: true));
            }

            private static BitVector CreateBitVector(int capacity, bool reachable)
            {
                if (capacity < 1)
                    capacity = 1;

                BitVector state = BitVector.Create(capacity * 2);
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
                return HasVariable(id, index);
            }

            private bool HasVariable(int id, int index)
            {
                if (Id > id)
                {
                    return _container!.Value.HasValue(id, index);
                }
                else
                {
                    return Id == id;
                }
            }

            public bool HasValue(int slot)
            {
                if (slot <= 0)
                {
                    return false;
                }
                (int id, int index) = Variables.DeconstructSlot(slot);
                return HasValue(id, index);
            }

            private bool HasValue(int id, int index)
            {
                if (Id != id)
                {
                    Debug.Assert(Id > id);
                    return _container!.Value.HasValue(id, index);
                }
                else
                {
                    return index < Capacity;
                }
            }

            public void Normalize(NullableWalker walker, Variables variables)
            {
                if (Id != variables.Id)
                {
                    Debug.Assert(Id < variables.Id);
                    Normalize(walker, variables.Container!);
                }
                else
                {
                    _container?.Value.Normalize(walker, variables.Container!);
                    int start = Capacity;
                    EnsureCapacity(variables.NextAvailableIndex);
                    Populate(walker, start);
                }
            }

            public void PopulateAll(NullableWalker walker)
            {
                _container?.Value.PopulateAll(walker);
                Populate(walker, start: 1);
            }

            private void Populate(NullableWalker walker, int start)
            {
                int capacity = Capacity;
                for (int index = start; index < capacity; index++)
                {
                    int slot = Variables.ConstructSlot(Id, index);
                    SetValue(Id, index, walker.GetDefaultState(ref this, slot));
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
                else
                {
                    if (index < Capacity && this.Reachable)
                    {
                        index *= 2;
                        var result = (_state[index + 1], _state[index]) switch
                        {
                            (false, false) => NullableFlowState.NotNull,
                            (false, true) => NullableFlowState.MaybeNull,
                            (true, false) => throw ExceptionUtilities.UnexpectedValue(index),
                            (true, true) => NullableFlowState.MaybeDefault
                        };
                        return result;
                    }
                    return NullableFlowState.NotNull;
                }
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
                    // No states should be modified in unreachable code, as there is only one unreachable state.
                    if (!this.Reachable) return;
                    index *= 2;
                    _state[index] = (value != NullableFlowState.NotNull);
                    _state[index + 1] = (value == NullableFlowState.MaybeDefault);
                }
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
                bool result = false;
                if (_container is { } && _container.Value.Join(in other._container!.Value))
                {
                    result = true;
                }
                if (_state.UnionWith(in other._state))
                {
                    result = true;
                }
                return result;
            }

            public bool Meet(in LocalState other)
            {
                Debug.Assert(Id == other.Id);
                bool result = false;
                if (_container is { } && _container.Value.Meet(in other._container!.Value))
                {
                    result = true;
                }
                if (_state.IntersectWith(in other._state))
                {
                    result = true;
                }
                return result;
            }

            internal string GetDebuggerDisplay()
            {
                var pooledBuilder = PooledStringBuilder.GetInstance();
                var builder = pooledBuilder.Builder;
                builder.Append(" ");
                int n = Math.Min(Capacity, 8);
                for (int i = n - 1; i >= 0; i--)
                    builder.Append(_state[i * 2] ? '?' : '!');

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
                : base(unreachableState.Clone(), unreachableState.Clone())
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
