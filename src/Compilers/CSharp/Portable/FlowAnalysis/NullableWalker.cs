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
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Nullability flow analysis.
    /// </summary>
    internal sealed partial class NullableWalker : LocalDataFlowPass<NullableWalker.LocalState>
    {
        /// <summary>
        /// Used to copy variable slots and types from the NullableWalker for the containing method
        /// or lambda to the NullableWalker created for a nested lambda or local function.
        /// </summary>
        internal sealed class VariableState
        {
            // Consider referencing the collections directly from the original NullableWalker
            // rather than coping the collections. (Items are added to the collections
            // but never replaced so the collections are lazily populated but otherwise immutable.)
            internal readonly ImmutableDictionary<VariableIdentifier, int> VariableSlot;
            internal readonly ImmutableArray<VariableIdentifier> VariableBySlot;
            internal readonly ImmutableDictionary<Symbol, TypeSymbolWithAnnotations> VariableTypes;

            internal VariableState(
                ImmutableDictionary<VariableIdentifier, int> variableSlot,
                ImmutableArray<VariableIdentifier> variableBySlot,
                ImmutableDictionary<Symbol, TypeSymbolWithAnnotations> variableTypes)
            {
                VariableSlot = variableSlot;
                VariableBySlot = variableBySlot;
                VariableTypes = variableTypes;
            }
        }

        /// <summary>
        /// The inferred type at the point of declaration of var locals and parameters.
        /// </summary>
        private readonly PooledDictionary<Symbol, TypeSymbolWithAnnotations> _variableTypes = PooledDictionary<Symbol, TypeSymbolWithAnnotations>.GetInstance();

        private readonly Binder _binder;

        /// <summary>
        /// Conversions with nullability and unknown matching any.
        /// </summary>
        private readonly Conversions _conversions;

        /// <summary>
        /// Use the return type and nullability from _methodSignatureOpt to calculate return
        /// expression conversions. If false, the signature of _member is used instead.
        /// </summary>
        private readonly bool _useMethodSignatureReturnType;

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
        /// Types from return expressions. Used when inferring lambda return type in MethodTypeInferrer.
        /// </summary>
        private readonly ArrayBuilder<(RefKind, TypeSymbolWithAnnotations)> _returnTypesOpt;

        /// <summary>
        /// An optional callback for callers to receive notification of the inferred type and nullability
        /// of each expression in the method. Since the walker may require multiple passes, the callback
        /// may be invoked multiple times for a single expression, potentially with different nullability
        /// each time. The last call for each expression will include the final inferred type and nullability.
        /// </summary>
        private readonly Action<BoundExpression, TypeSymbolWithAnnotations> _callbackOpt;

        /// <summary>
        /// Invalid type, used only to catch Visit methods that do not set
        /// _result.Type. See VisitExpressionWithoutStackGuard.
        /// </summary>
        private static readonly TypeSymbolWithAnnotations _invalidType = TypeSymbolWithAnnotations.Create(ErrorTypeSymbol.UnknownResultType);

        private TypeSymbolWithAnnotations _resultType;

        /// <summary>
        /// Instances being constructed.
        /// </summary>
        private PooledDictionary<BoundExpression, ObjectCreationPlaceholderLocal> _placeholderLocalsOpt;

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

        protected override void Free()
        {
            _variableTypes.Free();
            _placeholderLocalsOpt?.Free();
            base.Free();
        }

        private NullableWalker(
            CSharpCompilation compilation,
            MethodSymbol method,
            bool useMethodSignatureReturnType,
            bool useMethodSignatureParameterTypes,
            MethodSymbol methodSignatureOpt,
            BoundNode node,
            ArrayBuilder<(RefKind, TypeSymbolWithAnnotations)> returnTypesOpt,
            VariableState initialState,
            Action<BoundExpression, TypeSymbolWithAnnotations> callbackOpt)
            : base(compilation, method, node, new EmptyStructTypeCache(compilation, dev12CompilerCompatibility: false), trackUnassignments: true)
        {
            _callbackOpt = callbackOpt;
            _binder = compilation.GetBinderFactory(node.SyntaxTree).GetBinder(node.Syntax);
            Debug.Assert(!_binder.Conversions.IncludeNullability);
            _conversions = (Conversions)_binder.Conversions.WithNullability(true);
            _useMethodSignatureReturnType = (object)methodSignatureOpt != null && useMethodSignatureReturnType;
            _useMethodSignatureParameterTypes = (object)methodSignatureOpt != null && useMethodSignatureParameterTypes;
            _methodSignatureOpt = methodSignatureOpt;
            _returnTypesOpt = returnTypesOpt;
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
                foreach (var pair in initialState.VariableTypes)
                {
                    _variableTypes.Add(pair.Key, pair.Value);
                }
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
            ParameterSymbol methodThisParameter = MethodThisParameter;
            this.State = TopState();                   // entry point is reachable
            this.regionPlace = RegionPlace.Before;
            EnterParameters();                               // with parameters assigned
            if ((object)methodThisParameter != null)
            {
                EnterParameter(methodThisParameter, methodThisParameter.Type);
            }

            ImmutableArray<PendingBranch> pendingReturns = base.Scan(ref badRegion);
            return pendingReturns;
        }

        internal static void Analyze(
            CSharpCompilation compilation,
            MethodSymbol method,
            BoundNode node,
            DiagnosticBag diagnostics,
            Action<BoundExpression, TypeSymbolWithAnnotations> callbackOpt = null)
        {
            if (method.IsImplicitlyDeclared)
            {
                return;
            }
            Analyze(compilation, method, node, diagnostics, useMethodSignatureReturnType: false, useMethodSignatureParameterTypes: false, methodSignatureOpt: null, returnTypes: null, initialState: null, callbackOpt);
        }

        internal static void Analyze(
            CSharpCompilation compilation,
            BoundLambda lambda,
            DiagnosticBag diagnostics,
            MethodSymbol delegateInvokeMethod,
            ArrayBuilder<(RefKind, TypeSymbolWithAnnotations)> returnTypes,
            VariableState initialState)
        {
            Analyze(
                compilation,
                lambda.Symbol,
                lambda.Body,
                diagnostics,
                useMethodSignatureReturnType: true,
                useMethodSignatureParameterTypes: !lambda.UnboundLambda.HasExplicitlyTypedParameterList,
                methodSignatureOpt: delegateInvokeMethod,
                returnTypes, initialState,
                callbackOpt: null);
        }

        private static void Analyze(
            CSharpCompilation compilation,
            MethodSymbol method,
            BoundNode node,
            DiagnosticBag diagnostics,
            bool useMethodSignatureReturnType,
            bool useMethodSignatureParameterTypes,
            MethodSymbol methodSignatureOpt,
            ArrayBuilder<(RefKind, TypeSymbolWithAnnotations)> returnTypes,
            VariableState initialState,
            Action<BoundExpression, TypeSymbolWithAnnotations> callbackOpt)
        {
            Debug.Assert(diagnostics != null);
            var walker = new NullableWalker(compilation, method, useMethodSignatureReturnType, useMethodSignatureParameterTypes, methodSignatureOpt, node, returnTypes, initialState, callbackOpt);
            try
            {
                bool badRegion = false;
                ImmutableArray<PendingBranch> returns = walker.Analyze(ref badRegion);
                diagnostics.AddRange(walker.Diagnostics);
                Debug.Assert(!badRegion);
            }
            catch (BoundTreeVisitor.CancelledByStackGuardException ex) when (diagnostics != null)
            {
                ex.AddAnError(diagnostics);
            }
            finally
            {
                walker.Free();
            }
        }

        protected override void Normalize(ref LocalState state)
        {
            int oldNext = state.Capacity;
            state.EnsureCapacity(nextVariableSlot);
            Populate(ref state, oldNext);
        }

        private void Populate(ref LocalState state, int start)
        {
            int capacity = state.Capacity;
            for (int slot = start; slot < capacity; slot++)
            {
                (NullableAnnotation value, bool assigned) = GetDefaultState(ref state, slot);
                state[slot] = value;
                state.SetAssigned(slot, assigned);
            }
        }

        private (NullableAnnotation annotation, bool assigned) GetDefaultState(ref LocalState state, int slot)
        {
            if (slot == 0)
            {
                return (NullableAnnotation.Unknown, false);
            }

            var variable = variableBySlot[slot];
            var symbol = variable.Symbol;

            switch (symbol.Kind)
            {
                case SymbolKind.Local:
                    return (NullableAnnotation.Unknown, false);
                case SymbolKind.Parameter:
                    {
                        var parameter = (ParameterSymbol)symbol;
                        if (parameter.RefKind == RefKind.Out)
                        {
                            return (NullableAnnotation.Unknown, false);
                        }
                        TypeSymbolWithAnnotations parameterType;
                        if (!_variableTypes.TryGetValue(parameter, out parameterType))
                        {
                            parameterType = parameter.Type;
                        }

                        return (parameterType.NullableAnnotation, true);
                    }
                case SymbolKind.Field:
                case SymbolKind.Property:
                case SymbolKind.Event:
                    {
                        int containingSlot = variable.ContainingSlot;
                        if (containingSlot > 0 &&
                            variableBySlot[containingSlot].Symbol.GetTypeOrReturnType().TypeKind == TypeKind.Struct &&
                            !state.IsAssigned(containingSlot))
                        {
                            return (NullableAnnotation.Unknown, false);
                        }

                        return (symbol.GetTypeOrReturnType().NullableAnnotation, true);
                    }
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
                        if (fieldSymbol.IsStatic || fieldSymbol.IsFixedSizeBuffer)
                        {
                            return false;
                        }
                        member = fieldSymbol;
                        receiver = fieldAccess.ReceiverOpt;
                        break;
                    }
                case BoundKind.EventAccess:
                    {
                        var eventAccess = (BoundEventAccess)expr;
                        var eventSymbol = eventAccess.EventSymbol;
                        if (eventSymbol.IsStatic)
                        {
                            return false;
                        }
                        // https://github.com/dotnet/roslyn/issues/29901 Use AssociatedField for field-like events?
                        member = eventSymbol;
                        receiver = eventAccess.ReceiverOpt;
                        break;
                    }
                case BoundKind.PropertyAccess:
                    {
                        var propAccess = (BoundPropertyAccess)expr;
                        var propSymbol = propAccess.PropertySymbol;
                        if (propSymbol.IsStatic)
                        {
                            return false;
                        }
                        member = GetBackingFieldIfStructProperty(propSymbol);
                        receiver = propAccess.ReceiverOpt;
                        break;
                    }
            }

            return (object)member != null &&
                (object)receiver != null &&
                receiver.Kind != BoundKind.TypeExpression &&
                (object)receiver.Type != null;
        }

        // https://github.com/dotnet/roslyn/issues/29619 Use backing field for struct property
        // for now, to avoid cycles if the struct type contains a property of the struct type.
        // Remove this and populate struct members lazily to match classes.
        private Symbol GetBackingFieldIfStructProperty(Symbol symbol)
        {
            if (symbol.Kind == SymbolKind.Property && !symbol.ContainingType.IsNullableType())
            {
                var property = (PropertySymbol)symbol;
                var containingType = property.ContainingType;
                if (containingType.TypeKind == TypeKind.Struct)
                {
                    // https://github.com/dotnet/roslyn/issues/29619 Relying on field name
                    // will not work for properties declared in other languages.
                    var fieldName = GeneratedNames.MakeBackingFieldName(property.Name);
                    return _emptyStructTypeCache.GetStructInstanceFields(containingType).FirstOrDefault(f => f.Name == fieldName);
                }
            }
            return symbol;
        }

        // https://github.com/dotnet/roslyn/issues/29619 Temporary, until we're using
        // properties on structs directly.
        private new int GetOrCreateSlot(Symbol symbol, int containingSlot = 0)
        {
            symbol = GetBackingFieldIfStructProperty(symbol);
            if ((object)symbol == null)
            {
                return -1;
            }
            return base.GetOrCreateSlot(symbol, containingSlot);
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
                        var conv = (BoundConversion)node;
                        if (conv.Conversion.Kind == ConversionKind.ExplicitNullable)
                        {
                            var operand = conv.Operand;
                            var operandType = operand.Type;
                            var convertedType = conv.Type;
                            if (operandType?.IsNullableType() == true &&
                                convertedType?.IsNullableType() == false &&
                                operandType.GetNullableUnderlyingType().Equals(convertedType, TypeCompareKind.AllIgnoreOptions))
                            {
                                // Explicit conversion of Nullable<T> to T is equivalent to Nullable<T>.Value.
                                // For instance, in the following, when evaluating `((A)a).B` we need to recognize
                                // the nullability of `(A)a` (not nullable) and the slot (the slot for `a.Value`).
                                //   struct A { B? B; }
                                //   struct B { }
                                //   if (a?.B != null) _ = ((A)a).B.Value; // no warning
                                int containingSlot = MakeSlot(operand);
                                return containingSlot < 0 ? -1 : GetNullableOfTValueSlot(operandType, containingSlot);
                            }
                        }
                    }
                    break;
                case BoundKind.ObjectCreationExpression:
                case BoundKind.DynamicObjectCreationExpression:
                case BoundKind.AnonymousObjectCreationExpression:
                    if (_placeholderLocalsOpt != null && _placeholderLocalsOpt.TryGetValue(node, out ObjectCreationPlaceholderLocal placeholder))
                    {
                        return GetOrCreateSlot(placeholder);
                    }
                    break;
                case BoundKind.ConditionalReceiver:
                    if (_lastConditionalAccessSlot != -1)
                    {
                        int slot = _lastConditionalAccessSlot;
                        _lastConditionalAccessSlot = -1;
                        return slot;
                    }
                    break;
            }
            return base.MakeSlot(node);

            MethodSymbol getTopLevelMethod(MethodSymbol method)
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

        private new void VisitLvalue(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundKind.Local:
                    _resultType = GetDeclaredLocalResult(((BoundLocal)node).LocalSymbol);
                    break;
                case BoundKind.Parameter:
                    _resultType = GetDeclaredParameterResult(((BoundParameter)node).ParameterSymbol);
                    break;
                case BoundKind.FieldAccess:
                    {
                        var fieldAccess = (BoundFieldAccess)node;
                        VisitMemberAccess(node, fieldAccess.ReceiverOpt, fieldAccess.FieldSymbol, asLvalue: true);
                    }
                    break;
                case BoundKind.PropertyAccess:
                    {
                        var propertyAccess = (BoundPropertyAccess)node;
                        VisitMemberAccess(node, propertyAccess.ReceiverOpt, propertyAccess.PropertySymbol, asLvalue: true);
                    }
                    break;
                case BoundKind.EventAccess:
                    {
                        var eventAccess = (BoundEventAccess)node;
                        VisitMemberAccess(node, eventAccess.ReceiverOpt, eventAccess.EventSymbol, asLvalue: true);
                    }
                    break;
                case BoundKind.ObjectInitializerMember:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind); // Should have been handled in VisitObjectCreationExpression().
                default:
                    VisitRvalue(node);
                    break;
            }

            if (_callbackOpt != null)
            {
                _callbackOpt(node, _resultType);
            }
        }

        private TypeSymbolWithAnnotations VisitRvalueWithResult(BoundExpression node)
        {
            base.VisitRvalue(node);
            return _resultType;
        }

        private static object GetTypeAsDiagnosticArgument(TypeSymbol typeOpt)
        {
            return typeOpt ?? (object)"<null>";
        }

        private enum AssignmentKind
        {
            Assignment,
            Return,
            Argument
        }

        /// <summary>
        /// Reports top-level nullability problem in assignment.
        /// </summary>
        private bool ReportNullableAssignmentIfNecessary(BoundExpression value, TypeSymbolWithAnnotations targetType, TypeSymbolWithAnnotations valueType, bool useLegacyWarnings, AssignmentKind assignmentKind = AssignmentKind.Assignment, Symbol target = null)
        {
            Debug.Assert((object)target != null || assignmentKind != AssignmentKind.Argument);

            if (value == null)
            {
                return false;
            }

            if (targetType.IsNull ||
                targetType.IsValueType ||
                !targetType.NullableAnnotation.IsAnyNotNullable() ||
                valueType.IsNull ||
                !valueType.NullableAnnotation.IsAnyNullable())
            {
                return false;
            }

            var unwrappedValue = SkipReferenceConversions(value);
            if (unwrappedValue.Kind == BoundKind.SuppressNullableWarningExpression)
            {
                return false;
            }

            if (reportNullLiteralAssignmentIfNecessary(value))
            {
                return true;
            }

            if (valueType.IsNull)
            {
                return false;
            }

            if (assignmentKind == AssignmentKind.Argument)
            {
                ReportSafetyDiagnostic(ErrorCode.WRN_NullReferenceArgument, value.Syntax,
                    new FormattedSymbol(target, SymbolDisplayFormat.ShortFormat),
                    new FormattedSymbol(target.ContainingSymbol, SymbolDisplayFormat.MinimallyQualifiedFormat));
            }
            else if (useLegacyWarnings)
            {
                ReportNonSafetyDiagnostic(value.Syntax);
            }
            else
            {
                ReportSafetyDiagnostic(assignmentKind == AssignmentKind.Return ? ErrorCode.WRN_NullReferenceReturn : ErrorCode.WRN_NullReferenceAssignment, value.Syntax);
            }

            return true;

            // Report warning converting null literal to non-nullable reference type.
            // target (e.g.: `object x = null;` or calling `void F(object y)` with `F(null)`).
            bool reportNullLiteralAssignmentIfNecessary(BoundExpression expr)
            {
                if (expr.ConstantValue?.IsNull != true && !isDefaultOfUnconstrainedTypeParameter(expr))
                {
                    return false;
                }

                if (useLegacyWarnings)
                {
                    ReportNonSafetyDiagnostic(expr.Syntax);
                }
                else
                {
                    ReportSafetyDiagnostic(assignmentKind == AssignmentKind.Return ? ErrorCode.WRN_NullReferenceReturn : ErrorCode.WRN_NullAsNonNullable, expr.Syntax);
                }
                return true;
            }

            bool isDefaultOfUnconstrainedTypeParameter(BoundExpression expr)
            {
                switch (expr.Kind)
                {
                    case BoundKind.Conversion:
                        {
                            var conversion = (BoundConversion)expr;
                            return conversion.Conversion.Kind == ConversionKind.DefaultOrNullLiteral &&
                                isDefaultOfUnconstrainedTypeParameter(conversion.Operand);
                        }
                    case BoundKind.DefaultExpression:
                        return IsUnconstrainedTypeParameter(expr.Type);
                    default:
                        return false;
                }
            }
        }

        // Maybe this method can be replaced by VisitOptionalImplicitConversion or ApplyConversion
        private void ReportAssignmentWarnings(BoundExpression value, TypeSymbolWithAnnotations targetType, TypeSymbolWithAnnotations valueType, bool useLegacyWarnings)
        {
            Debug.Assert(value != null);

            if (this.State.Reachable)
            {
                if (targetType.IsNull || valueType.IsNull)
                {
                    return;
                }

                // Report top-level nullability issues
                ReportNullableAssignmentIfNecessary(value, targetType, valueType, useLegacyWarnings, assignmentKind: AssignmentKind.Assignment);

                // Report nested nullability issues
                var sourceType = valueType.TypeSymbol;
                var destinationType = targetType.TypeSymbol;
                if ((object)sourceType != null && IsNullabilityMismatch(destinationType, sourceType))
                {
                    ReportSafetyDiagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, value.Syntax, sourceType, destinationType);
                }
            }
        }

        /// <summary>
        /// Update tracked value on assignment.
        /// </summary>
        private void TrackNullableStateForAssignment(BoundExpression value, TypeSymbolWithAnnotations targetType, int targetSlot, TypeSymbolWithAnnotations valueType, int valueSlot = -1)
        {
            Debug.Assert(value != null);
            Debug.Assert(!IsConditionalState);

            if (this.State.Reachable)
            {
                if (targetType.IsNull)
                {
                    return;
                }

                if (targetSlot <= 0)
                {
                    return;
                }

                bool isByRefTarget = IsByRefTarget(targetSlot);
                if (targetSlot >= this.State.Capacity) Normalize(ref this.State);

                // https://github.com/dotnet/roslyn/issues/29968 Remove isByRefTarget check?
                var newState = isByRefTarget ?
                    // Since reference can point to the heap, we cannot assume the value is not null after this assignment,
                    // regardless of what value is being assigned.
                    targetType.NullableAnnotation.IsAnyNullable() ? targetType.NullableAnnotation : NullableAnnotation.Unknown :
                    valueType.NullableAnnotation;
                this.State[targetSlot] = newState;
                if (newState.IsAnyNullable() && _tryState.HasValue)
                {
                    var state = _tryState.Value;
                    state[targetSlot] = NullableAnnotation.Nullable;
                    _tryState = state;
                }

                // https://github.com/dotnet/roslyn/issues/29968 Might this clear state that
                // should be copied in InheritNullableStateOfTrackableType?
                InheritDefaultState(targetSlot);

                if (targetType.IsReferenceType)
                {
                    // https://github.com/dotnet/roslyn/issues/31395,
                    // https://github.com/dotnet/roslyn/issues/29968 We should copy all tracked state from `value`,
                    // regardless of BoundNode type, but we'll need to handle cycles. (For instance, the
                    // assignment to C.F below. See also NullableReferenceTypesTests.Members_FieldCycle_01.)
                    // class C
                    // {
                    //     C? F;
                    //     C() { F = this; }
                    // }
                    // For now, we copy a limited set of BoundNode types that shouldn't contain cycles.
                    if ((value.Kind == BoundKind.ObjectCreationExpression || value.Kind == BoundKind.AnonymousObjectCreationExpression || value.Kind == BoundKind.DynamicObjectCreationExpression || targetType.TypeSymbol.IsAnonymousType) &&
                        targetType.TypeSymbol.Equals(valueType.TypeSymbol, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes)) // https://github.com/dotnet/roslyn/issues/29968 Allow assignment to base type.
                    {
                        if (valueSlot > 0)
                        {
                            InheritNullableStateOfTrackableType(targetSlot, valueSlot, isByRefTarget, slotWatermark: GetSlotWatermark());
                        }
                    }
                }
                else if (EmptyStructTypeCache.IsTrackableStructType(targetType.TypeSymbol) &&
                        targetType.TypeSymbol.Equals(valueType.TypeSymbol, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes))
                {
                    InheritNullableStateOfTrackableStruct(targetType.TypeSymbol, targetSlot, valueSlot, IsByRefTarget(targetSlot), slotWatermark: GetSlotWatermark());
                }
            }
        }

        private int GetSlotWatermark() => this.nextVariableSlot;

        private bool IsByRefTarget(int slot)
        {
            if (slot > 0)
            {
                Symbol associatedNonMemberSymbol = GetNonMemberSymbol(slot);

                switch (associatedNonMemberSymbol.Kind)
                {
                    case SymbolKind.Local:
                        return ((LocalSymbol)associatedNonMemberSymbol).RefKind != RefKind.None;
                    case SymbolKind.Parameter:
                        var parameter = (ParameterSymbol)associatedNonMemberSymbol;
                        return !parameter.IsThis && parameter.RefKind != RefKind.None;
                }
            }

            return false;
        }

        private void ReportNonSafetyDiagnostic(SyntaxNode syntax)
        {
            ReportNonSafetyDiagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, syntax);
        }

        private void ReportNonSafetyDiagnostic(ErrorCode errorCode, SyntaxNode syntax)
        {
            // All warnings should be in the `#pragma warning ... nullable` set.
            Debug.Assert(!ErrorFacts.NullableFlowAnalysisSafetyWarnings.Contains(MessageProvider.Instance.GetIdForErrorCode((int)errorCode)));
            Debug.Assert(ErrorFacts.NullableFlowAnalysisNonSafetyWarnings.Contains(MessageProvider.Instance.GetIdForErrorCode((int)errorCode)));
#pragma warning disable CS0618
            ReportDiagnostic(errorCode, syntax);
#pragma warning restore CS0618
        }

        private void ReportSafetyDiagnostic(ErrorCode errorCode, SyntaxNode syntaxNode, params object[] arguments)
        {
            // All warnings should be in the `#pragma warning ... nullable` set.
            Debug.Assert(ErrorFacts.NullableFlowAnalysisSafetyWarnings.Contains(MessageProvider.Instance.GetIdForErrorCode((int)errorCode)));
            Debug.Assert(!ErrorFacts.NullableFlowAnalysisNonSafetyWarnings.Contains(MessageProvider.Instance.GetIdForErrorCode((int)errorCode)));
#pragma warning disable CS0618
            ReportDiagnostic(errorCode, syntaxNode, arguments);
#pragma warning restore CS0618
        }

        [Obsolete("Use ReportSafetyDiagnostic/ReportNonSafetyDiagnostic instead", error: false)]
        private void ReportDiagnostic(ErrorCode errorCode, SyntaxNode syntaxNode, params object[] arguments)
        {
            if (!_disableDiagnostics)
            {
                Diagnostics.Add(errorCode, syntaxNode.GetLocation(), arguments);
            }
        }

        private void InheritNullableStateOfTrackableStruct(TypeSymbol targetType, int targetSlot, int valueSlot, bool isByRefTarget, int slotWatermark)
        {
            Debug.Assert(targetSlot > 0);
            Debug.Assert(EmptyStructTypeCache.IsTrackableStructType(targetType));

            // https://github.com/dotnet/roslyn/issues/29619 Handle properties not backed by fields.
            // See ModifyMembers_StructPropertyNoBackingField and PropertyCycle_Struct tests.
            foreach (var field in _emptyStructTypeCache.GetStructInstanceFields(targetType))
            {
                InheritNullableStateOfMember(targetSlot, valueSlot, field, isByRefTarget, slotWatermark);
            }
        }

        // 'slotWatermark' is used to avoid inheriting members from inherited members.
        private void InheritNullableStateOfMember(int targetContainerSlot, int valueContainerSlot, Symbol member, bool isByRefTarget, int slotWatermark)
        {
            Debug.Assert(valueContainerSlot <= slotWatermark);

            TypeSymbolWithAnnotations fieldOrPropertyType = member.GetTypeOrReturnType();

            // Nullable<T> is handled here rather than in InheritNullableStateOfTrackableStruct since that
            // method only clones auto-properties (see https://github.com/dotnet/roslyn/issues/29619).
            // When that issue is fixed, Nullable<T> should be handled there instead.
            if (fieldOrPropertyType.IsReferenceType || fieldOrPropertyType.IsNullableType())
            {
                int targetMemberSlot = GetOrCreateSlot(member, targetContainerSlot);
                NullableAnnotation value = fieldOrPropertyType.NullableAnnotation;
                // https://github.com/dotnet/roslyn/issues/29968 Remove isByRefTarget check?
                if (isByRefTarget)
                {
                    // This is a member access through a by ref entity and it isn't considered declared as not-nullable. 
                    // Since reference can point to the heap, we cannot assume the member doesn't have null value
                    // after this assignment, regardless of what value is being assigned.
                }
                else if (valueContainerSlot > 0)
                {
                    int valueMemberSlot = VariableSlot(member, valueContainerSlot);
                    value = valueMemberSlot > 0 && valueMemberSlot < this.State.Capacity ?
                        this.State[valueMemberSlot] :
                        NullableAnnotation.Unknown;
                }

                this.State[targetMemberSlot] = value;

                if (valueContainerSlot > 0)
                {
                    int valueMemberSlot = VariableSlot(member, valueContainerSlot);
                    if (valueMemberSlot > 0 && valueMemberSlot <= slotWatermark)
                    {
                        InheritNullableStateOfTrackableType(targetMemberSlot, valueMemberSlot, isByRefTarget, slotWatermark);
                    }
                }
            }
            else if (EmptyStructTypeCache.IsTrackableStructType(fieldOrPropertyType.TypeSymbol))
            {
                int targetMemberSlot = GetOrCreateSlot(member, targetContainerSlot);
                if (targetMemberSlot > 0)
                {
                    int valueMemberSlot = -1;
                    if (valueContainerSlot > 0)
                    {
                        int slot = GetOrCreateSlot(member, valueContainerSlot);
                        if (slot < slotWatermark)
                        {
                            valueMemberSlot = slot;
                        }
                    }
                    InheritNullableStateOfTrackableStruct(fieldOrPropertyType.TypeSymbol, targetMemberSlot, valueMemberSlot, isByRefTarget, slotWatermark);
                }
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
                this.State[slot] = variable.Symbol.GetTypeOrReturnType().NullableAnnotation;
                InheritDefaultState(slot);
            }
        }

        private void InheritNullableStateOfTrackableType(int targetSlot, int valueSlot, bool isByRefTarget, int slotWatermark)
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
                InheritNullableStateOfMember(targetSlot, valueSlot, member, isByRefTarget, slotWatermark);
            }
        }

        private TypeSymbol GetSlotType(int slot)
        {
            return VariableType(variableBySlot[slot].Symbol).TypeSymbol;
        }

        protected override LocalState TopState()
        {
            var state = new LocalState(reachable: true, BitVector.Create(nextVariableSlot), new ArrayBuilder<NullableAnnotation>(nextVariableSlot));
            Populate(ref state, start: 0);
            return state;
        }

        protected override LocalState UnreachableState()
        {
            return new LocalState(reachable: false, BitVector.Empty, null);
        }

        protected override LocalState ReachableBottomState()
        {
            // Create a reachable state in which all variables are known to be non-null.
            var builder = new ArrayBuilder<NullableAnnotation>(nextVariableSlot);
            builder.AddMany(NullableAnnotation.NotNullable, nextVariableSlot);
            return new LocalState(reachable: true, BitVector.AllSet(nextVariableSlot), builder);
        }

        private void EnterParameters()
        {
            var methodParameters = ((MethodSymbol)_symbol).Parameters;
            var signatureParameters = _useMethodSignatureParameterTypes ? _methodSignatureOpt.Parameters : methodParameters;
            Debug.Assert(signatureParameters.Length == methodParameters.Length);
            int n = methodParameters.Length;
            for (int i = 0; i < n; i++)
            {
                var parameter = methodParameters[i];
                var parameterType = signatureParameters[i].Type;
                EnterParameter(parameter, parameterType);
            }
        }

        private void EnterParameter(ParameterSymbol parameter, TypeSymbolWithAnnotations parameterType)
        {
            _variableTypes[parameter] = parameterType;
            int slot = GetOrCreateSlot(parameter);

            Debug.Assert(!IsConditionalState);
            if (slot > 0 && parameter.RefKind != RefKind.Out)
            {
                if (EmptyStructTypeCache.IsTrackableStructType(parameterType.TypeSymbol))
                {
                    InheritNullableStateOfTrackableStruct(parameterType.TypeSymbol, slot, valueSlot: -1, isByRefTarget: parameter.RefKind != RefKind.None, slotWatermark: GetSlotWatermark());
                }
            }
        }

        #region Visitors

        public override BoundNode VisitIsPatternExpression(BoundIsPatternExpression node)
        {
            VisitRvalue(node.Expression);
            var expressionResultType = this._resultType;

            VisitPattern(node.Expression, expressionResultType, node.Pattern);

            SetResult(node);
            return node;
        }

        /// <summary>
        /// Examples:
        /// `x is Point p`
        /// `switch (x) ... case Point p:` // https://github.com/dotnet/roslyn/issues/29873 not yet handled
        ///
        /// If the expression is trackable, we'll return with different null-states for that expression in the two conditional states.
        /// If the pattern is a `var` pattern, we'll also have re-inferred the `var` type with nullability and
        /// updated the state for that declared local.
        /// </summary>
        private void VisitPattern(BoundExpression expression, TypeSymbolWithAnnotations expressionResultType, BoundPattern pattern)
        {
            NullableAnnotation whenTrue = NullableAnnotation.Unknown; // the pattern tells us the expression (1) is null, (2) isn't null, or (3) we don't know.
            NullableAnnotation whenFalse = NullableAnnotation.Unknown;
            switch (pattern.Kind)
            {
                case BoundKind.ConstantPattern:
                    // If the constant is null, the pattern tells us the expression is null.
                    // If the constant is not null, the pattern tells us the expression is not null.
                    // If there is no constant, we don't know.
                    switch (((BoundConstantPattern)pattern).ConstantValue?.IsNull)
                    {
                        case true:
                            whenTrue = NullableAnnotation.Nullable;
                            whenFalse = NullableAnnotation.NotNullable;
                            break;
                        case false:
                            whenTrue = NullableAnnotation.NotNullable;
                            whenFalse = expressionResultType.NullableAnnotation;
                            break;
                    }
                    break;
                case BoundKind.DeclarationPattern:
                    var declarationPattern = (BoundDeclarationPattern)pattern;
                    if (declarationPattern.IsVar)
                    {
                        // The result type and state of the expression carry into the variable declared by var pattern
                        Symbol variable = declarationPattern.Variable;
                        // No variable declared for discard (`i is var _`)
                        if ((object)variable != null)
                        {
                            _variableTypes[variable] = expressionResultType;
                            TrackNullableStateForAssignment(expression, expressionResultType, GetOrCreateSlot(variable), expressionResultType);
                        }
                    }
                    else
                    {
                        whenTrue = NullableAnnotation.NotNullable; // the pattern tells us the expression is not null
                        whenFalse = expressionResultType.NullableAnnotation;
                    }
                    break;
            }

            Debug.Assert(!IsConditionalState);

            int mainSlot = -1;
            if (whenTrue != NullableAnnotation.Unknown)
            {
                // Create slot when the state is unconditional since EnsureCapacity should be
                // called on all fields and that is simpler if state is limited to this.State.
                mainSlot = MakeSlot(expression);
            }

            base.VisitPattern(expression, pattern); // note: splits
            Debug.Assert(IsConditionalState);

            // https://github.com/dotnet/roslyn/issues/29873 We should only report such
            // diagnostics for locals that are set or checked explicitly within this method.
            if (!expressionResultType.IsNull && expressionResultType.ValueCanBeNull() == false && whenTrue == NullableAnnotation.Nullable)
            {
                ReportNonSafetyDiagnostic(ErrorCode.HDN_NullCheckIsProbablyAlwaysFalse, pattern.Syntax);
            }

            if (mainSlot > 0)
            {
                Debug.Assert(whenTrue != NullableAnnotation.Unknown);
                this.StateWhenTrue[mainSlot] = whenTrue;
                this.StateWhenFalse[mainSlot] = whenFalse;
            }

            if (whenTrue == NullableAnnotation.NotNullable || whenFalse == NullableAnnotation.NotNullable)
            {
                var slotBuilder = ArrayBuilder<int>.GetInstance();
                GetSlotsToMarkAsNotNullable(expression, slotBuilder);

                // Set all nested conditional slots. For example in a?.b?.c we'll set a, b, and c.
                if (whenTrue == NullableAnnotation.NotNullable)
                {
                    MarkSlotsAsNotNullable(slotBuilder, ref StateWhenTrue);
                }
                else if (whenFalse == NullableAnnotation.NotNullable)
                {
                    MarkSlotsAsNotNullable(slotBuilder, ref StateWhenFalse);
                }

                slotBuilder.Free();
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

            if (_returnTypesOpt != null)
            {
                // Inferring return type. Should not convert to method return type.
                TypeSymbolWithAnnotations result = VisitRvalueWithResult(expr);
                _returnTypesOpt.Add((node.RefKind, result));
                return null;
            }

            TypeSymbolWithAnnotations returnType = GetReturnType();
            VisitOptionalImplicitConversion(expr, returnType, useLegacyWarnings: false, AssignmentKind.Return);
            return null;
        }

        private TypeSymbolWithAnnotations GetReturnType()
        {
            var method = (MethodSymbol)_symbol;
            var returnType = (_useMethodSignatureReturnType ? _methodSignatureOpt : method).ReturnType;
            Debug.Assert((object)returnType != LambdaSymbol.ReturnTypeIsBeingInferred);
            if (method.IsGenericTaskReturningAsync(compilation))
            {
                returnType = ((NamedTypeSymbol)returnType.TypeSymbol).TypeArgumentsNoUseSiteDiagnostics.Single();
            }

            return returnType;
        }

        private static bool IsUnconstrainedTypeParameter(TypeSymbol typeOpt)
        {
            return typeOpt?.IsUnconstrainedTypeParameter() == true;
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            var local = node.LocalSymbol;
            int slot = GetOrCreateSlot(local);
            var type = GetDeclaredLocalResult(local);
            _resultType = GetAdjustedResult(type, slot);
            return null;
        }

        public override BoundNode VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            var local = node.LocalSymbol;
            int slot = GetOrCreateSlot(local);

            var initializer = node.InitializerOpt;
            if (initializer is null)
            {
                return null;
            }

            bool inferredType = node.DeclaredType.InferredType;
            TypeSymbolWithAnnotations type = local.Type;
            TypeSymbolWithAnnotations valueType = VisitOptionalImplicitConversion(initializer, targetTypeOpt: inferredType ? default : type, useLegacyWarnings: true, AssignmentKind.Assignment);

            if (inferredType)
            {
                if (valueType.IsNull)
                {
                    Debug.Assert(type.IsErrorType());
                    valueType = type;
                }
                _variableTypes[local] = valueType;
                type = valueType;
            }

            TrackNullableStateForAssignment(initializer, type, slot, valueType, MakeSlot(initializer));
            return null;
        }

        protected override BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            Debug.Assert(!IsConditionalState);
            _resultType = _invalidType;
            var result = base.VisitExpressionWithoutStackGuard(node);
#if DEBUG
            // Verify Visit method set _result.
            TypeSymbolWithAnnotations resultType = _resultType;
            Debug.Assert((object)resultType.TypeSymbol != _invalidType.TypeSymbol);
            Debug.Assert(AreCloseEnough(resultType.TypeSymbol, node.Type));
#endif
            if (_callbackOpt != null)
            {
                _callbackOpt(node, _resultType);
            }
            return result;
        }

#if DEBUG
        // For asserts only.
        private static bool AreCloseEnough(TypeSymbol typeA, TypeSymbol typeB)
        {
            if ((object)typeA == typeB)
            {
                return true;
            }
            if (typeA is null || typeB is null)
            {
                return false;
            }
            bool canIgnoreType(TypeSymbol type) => (object)type.VisitType((t, unused1, unused2) => t.IsErrorType() || t.IsDynamic() || t.HasUseSiteError, (object)null) != null;
            return canIgnoreType(typeA) ||
                canIgnoreType(typeB) ||
                typeA.Equals(typeB, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes | TypeCompareKind.IgnoreDynamicAndTupleNames); // Ignore TupleElementNames (see https://github.com/dotnet/roslyn/issues/23651).
        }
#endif

        protected override void VisitStatement(BoundStatement statement)
        {
            _resultType = _invalidType;
            base.VisitStatement(statement);
            _resultType = _invalidType;
        }

        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            Debug.Assert(!IsConditionalState);
            VisitArguments(node, node.Arguments, node.ArgumentRefKindsOpt, node.Constructor, node.ArgsToParamsOpt, node.Expanded);
            VisitObjectOrDynamicObjectCreation(node, node.InitializerExpressionOpt);
            return null;
        }

        private void VisitObjectOrDynamicObjectCreation(BoundExpression node, BoundExpression initializerOpt)
        {
            Debug.Assert(node.Kind == BoundKind.ObjectCreationExpression || node.Kind == BoundKind.DynamicObjectCreationExpression);

            LocalSymbol receiver = null;
            int slot = -1;
            TypeSymbol type = node.Type;
            if ((object)type != null)
            {
                bool isTrackableStructType = EmptyStructTypeCache.IsTrackableStructType(type);
                if (!type.IsValueType || isTrackableStructType)
                {
                    receiver = GetOrCreateObjectCreationPlaceholder(node);
                    slot = GetOrCreateSlot(receiver);
                    if (slot > 0 && isTrackableStructType)
                    {
                        this.State[slot] = NullableAnnotation.NotNullable;
                        InheritNullableStateOfTrackableStruct(type, slot, valueSlot: -1, isByRefTarget: false, slotWatermark: GetSlotWatermark());
                    }
                }
            }

            if (initializerOpt != null)
            {
                VisitObjectCreationInitializer(receiver, slot, initializerOpt);
            }

            _resultType = TypeSymbolWithAnnotations.Create(type, NullableAnnotation.NotNullable);
        }

        private void VisitObjectCreationInitializer(Symbol containingSymbol, int containingSlot, BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundKind.ObjectInitializerExpression:
                    foreach (var initializer in ((BoundObjectInitializerExpression)node).Initializers)
                    {
                        switch (initializer.Kind)
                        {
                            case BoundKind.AssignmentOperator:
                                VisitObjectElementInitializer(containingSymbol, containingSlot, (BoundAssignmentOperator)initializer);
                                break;
                            default:
                                VisitRvalue(initializer);
                                break;
                        }
                    }
                    break;
                case BoundKind.CollectionInitializerExpression:
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
                    TypeSymbolWithAnnotations resultType = VisitRvalueWithResult(node);
                    if ((object)containingSymbol != null)
                    {
                        var type = containingSymbol.GetTypeOrReturnType();
                        ReportAssignmentWarnings(node, type, resultType, useLegacyWarnings: false);
                        TrackNullableStateForAssignment(node, type, containingSlot, resultType, MakeSlot(node));
                    }
                    break;
            }
        }

        private void VisitObjectElementInitializer(Symbol containingSymbol, int containingSlot, BoundAssignmentOperator node)
        {
            var left = node.Left;
            switch (left.Kind)
            {
                case BoundKind.ObjectInitializerMember:
                    {
                        var objectInitializer = (BoundObjectInitializerMember)left;
                        var symbol = objectInitializer.MemberSymbol;
                        if (!objectInitializer.Arguments.IsDefaultOrEmpty)
                        {
                            VisitArguments(objectInitializer, objectInitializer.Arguments, objectInitializer.ArgumentRefKindsOpt, (PropertySymbol)symbol, objectInitializer.ArgsToParamsOpt, objectInitializer.Expanded);
                        }
                        if ((object)symbol != null)
                        {
                            int slot = (containingSlot < 0) ? -1 : GetOrCreateSlot(symbol, containingSlot);
                            VisitObjectCreationInitializer(symbol, slot, node.Right);
                        }
                    }
                    break;
                default:
                    VisitLvalue(node);
                    break;
            }
        }

        private new void VisitCollectionElementInitializer(BoundCollectionElementInitializer node)
        {
            // Note: we analyze even omitted calls
            VisitArguments(node, node.Arguments, refKindsOpt: default, node.AddMethod, node.ArgsToParamsOpt, node.Expanded);
            SetUnknownResultNullability();
        }

        private void SetResult(BoundExpression node)
        {
            _resultType = TypeSymbolWithAnnotations.Create(node.Type);
        }

        private ObjectCreationPlaceholderLocal GetOrCreateObjectCreationPlaceholder(BoundExpression node)
        {
            ObjectCreationPlaceholderLocal placeholder;
            if (_placeholderLocalsOpt == null)
            {
                _placeholderLocalsOpt = PooledDictionary<BoundExpression, ObjectCreationPlaceholderLocal>.GetInstance();
                placeholder = null;
            }
            else
            {
                _placeholderLocalsOpt.TryGetValue(node, out placeholder);
            }

            if ((object)placeholder == null)
            {
                placeholder = new ObjectCreationPlaceholderLocal(_symbol, node);
                _placeholderLocalsOpt.Add(node, placeholder);
            }

            return placeholder;
        }

        public override BoundNode VisitAnonymousObjectCreationExpression(BoundAnonymousObjectCreationExpression node)
        {
            Debug.Assert(!IsConditionalState);

            int receiverSlot = -1;
            var arguments = node.Arguments;
            var constructor = node.Constructor;
            for (int i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                TypeSymbolWithAnnotations argumentType = VisitRvalueWithResult(argument);
                var parameter = constructor.Parameters[i];
                ReportArgumentWarnings(argument, argumentType, parameter);

                // https://github.com/dotnet/roslyn/issues/24018 node.Declarations includes
                // explicitly-named properties only. For now, skip expressions
                // with implicit names. See NullableReferenceTypesTests.AnonymousTypes_05.
                if (node.Declarations.Length < arguments.Length)
                {
                    continue;
                }

                PropertySymbol property = node.Declarations[i].Property;
                if (receiverSlot <= 0)
                {
                    ObjectCreationPlaceholderLocal implicitReceiver = GetOrCreateObjectCreationPlaceholder(node);
                    receiverSlot = GetOrCreateSlot(implicitReceiver);
                }

                TypeSymbolWithAnnotations propertyType = property.Type;
                ReportAssignmentWarnings(argument, propertyType, argumentType, useLegacyWarnings: false);
                TrackNullableStateForAssignment(argument, propertyType, GetOrCreateSlot(property, receiverSlot), argumentType, MakeSlot(argument));
            }

            // https://github.com/dotnet/roslyn/issues/24018 _result may need to be a new anonymous
            // type since the properties may have distinct nullability from original.
            // (See NullableReferenceTypesTests.AnonymousObjectCreation_02.)
            _resultType = TypeSymbolWithAnnotations.Create(node.Type, NullableAnnotation.NotNullable);
            return null;
        }

        public override BoundNode VisitArrayCreation(BoundArrayCreation node)
        {
            foreach (var expr in node.Bounds)
            {
                VisitRvalue(expr);
            }
            TypeSymbol resultType = (node.InitializerOpt == null) ? node.Type : VisitArrayInitializer(node);
            _resultType = TypeSymbolWithAnnotations.Create(resultType, NullableAnnotation.NotNullable);
            return null;
        }

        private ArrayTypeSymbol VisitArrayInitializer(BoundArrayCreation node)
        {
            var arrayType = (ArrayTypeSymbol)node.Type;
            var elementType = arrayType.ElementType;

            BoundArrayInitialization initialization = node.InitializerOpt;
            var elementBuilder = ArrayBuilder<BoundExpression>.GetInstance(initialization.Initializers.Length);
            GetArrayElements(initialization, elementBuilder);

            // https://github.com/dotnet/roslyn/issues/27961 Removing and recalculating conversions should not
            // be necessary for explicitly typed arrays. In those cases, VisitConversion should warn
            // on nullability mismatch (although we'll need to ensure we handle the case where
            // initial binding calculated an Identity conversion, even though nullability was distinct).
            int n = elementBuilder.Count;
            var conversionBuilder = ArrayBuilder<Conversion>.GetInstance(n);
            var resultBuilder = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance(n);
            for (int i = 0; i < n; i++)
            {
                (BoundExpression element, Conversion conversion) = RemoveConversion(elementBuilder[i], includeExplicitConversions: false);
                elementBuilder[i] = element;
                conversionBuilder.Add(conversion);
                resultBuilder.Add(VisitRvalueWithResult(element));
            }

            bool checkConversions = true;
            // Consider recording in the BoundArrayCreation
            // whether the array was implicitly typed, rather than relying on syntax.
            if (node.Syntax.Kind() == SyntaxKind.ImplicitArrayCreationExpression)
            {
                TypeSymbol bestType = null;
                if (!node.HasErrors)
                {
                    var placeholderBuilder = ArrayBuilder<BoundExpression>.GetInstance(n);
                    for (int i = 0; i < n; i++)
                    {
                        placeholderBuilder.Add(CreatePlaceholderIfNecessary(elementBuilder[i], resultBuilder[i]));
                    }
                    var placeholders = placeholderBuilder.ToImmutableAndFree();
                    bool hadNullabilityMismatch;
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    bestType = BestTypeInferrer.InferBestType(placeholders, _conversions, out hadNullabilityMismatch, ref useSiteDiagnostics);
                    if (hadNullabilityMismatch)
                    {
                        ReportSafetyDiagnostic(ErrorCode.WRN_NoBestNullabilityArrayElements, node.Syntax);
                        checkConversions = false;
                    }
                }
                if ((object)bestType == null)
                {
                    elementType = elementType.SetUnknownNullabilityForReferenceTypes();
                    checkConversions = false;
                }
                else
                {
                    elementType = TypeSymbolWithAnnotations.Create(bestType, BestTypeInferrer.GetNullableAnnotation(bestType, resultBuilder));
                }
                arrayType = arrayType.WithElementType(elementType);
            }

            if (checkConversions && !elementType.IsValueType)
            {
                for (int i = 0; i < n; i++)
                {
                    var conversion = conversionBuilder[i];
                    var element = elementBuilder[i];
                    var resultType = resultBuilder[i];
                    ApplyConversion(element, element, conversion, elementType, resultType, checkConversion: true, fromExplicitCast: false, useLegacyWarnings: false, AssignmentKind.Assignment);
                }
            }

            resultBuilder.Free();
            elementBuilder.Free();
            _resultType = _invalidType;
            return arrayType;
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

            VisitRvalue(node.Expression);

            Debug.Assert(!IsConditionalState);
            Debug.Assert(!node.Expression.Type.IsValueType);
            // https://github.com/dotnet/roslyn/issues/30598: Mark receiver as not null
            // after indices have been visited, and only if the receiver has not changed.
            CheckPossibleNullReceiver(node.Expression);

            var type = _resultType.TypeSymbol as ArrayTypeSymbol;

            foreach (var i in node.Indices)
            {
                VisitRvalue(i);
            }

            if (node.Indices.Length == 1 &&
                TypeSymbol.Equals(node.Indices[0].Type, compilation.GetWellKnownType(WellKnownType.System_Range), TypeCompareKind.ConsiderEverything2))
            {
                _resultType = TypeSymbolWithAnnotations.Create(type);
            }
            else
            {
                _resultType = type?.ElementType ?? default;
            }

            return null;
        }

        private TypeSymbolWithAnnotations InferResultNullability(BoundBinaryOperator node, TypeSymbolWithAnnotations leftType, TypeSymbolWithAnnotations rightType)
        {
            return InferResultNullability(node.OperatorKind, node.MethodOpt, node.Type, leftType, rightType);
        }

        private TypeSymbolWithAnnotations InferResultNullability(BinaryOperatorKind operatorKind, MethodSymbol methodOpt, TypeSymbol resultType, TypeSymbolWithAnnotations leftType, TypeSymbolWithAnnotations rightType)
        {
            NullableAnnotation nullableAnnotation = NullableAnnotation.Unknown;
            if (operatorKind.IsUserDefined())
            {
                if (operatorKind.IsLifted())
                {
                    // https://github.com/dotnet/roslyn/issues/29953 Conversions: Lifted operator
                    return TypeSymbolWithAnnotations.Create(resultType);
                }
                // Update method based on operand types: see https://github.com/dotnet/roslyn/issues/29605.
                if ((object)methodOpt != null && methodOpt.ParameterCount == 2)
                {
                    return methodOpt.ReturnType;
                }
            }
            else if (!operatorKind.IsDynamic() && !resultType.IsValueType)
            {
                switch (operatorKind.Operator() | operatorKind.OperandTypes())
                {
                    case BinaryOperatorKind.DelegateCombination:
                        {
                            NullableAnnotation left = leftType.GetValueNullableAnnotation();
                            NullableAnnotation right = rightType.GetValueNullableAnnotation();
                            if (left.IsAnyNotNullable() || right.IsAnyNotNullable())
                            {
                                nullableAnnotation = NullableAnnotation.NotNullable;
                            }
                            else if (left.IsAnyNullable() && right.IsAnyNullable())
                            {
                                nullableAnnotation = NullableAnnotation.Nullable;
                            }
                            else
                            {
                                Debug.Assert(left == NullableAnnotation.Unknown || right == NullableAnnotation.Unknown);
                            }
                        }
                        break;
                    case BinaryOperatorKind.DelegateRemoval:
                        nullableAnnotation = NullableAnnotation.Nullable; // Delegate removal can produce null.
                        break;
                    default:
                        nullableAnnotation = NullableAnnotation.NotNullable;
                        break;
                }
            }

            return TypeSymbolWithAnnotations.Create(resultType, nullableAnnotation);
        }

        protected override void AfterLeftChildHasBeenVisited(BoundBinaryOperator binary)
        {
            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable) // Consider reachability: see https://github.com/dotnet/roslyn/issues/28798
            {
                TypeSymbolWithAnnotations leftType = _resultType;
                bool warnOnNullReferenceArgument = (binary.OperatorKind.IsUserDefined() && (object)binary.MethodOpt != null && binary.MethodOpt.ParameterCount == 2);

                if (warnOnNullReferenceArgument)
                {
                    ReportArgumentWarnings(binary.Left, leftType, binary.MethodOpt.Parameters[0]);
                }

                VisitRvalue(binary.Right);
                Debug.Assert(!IsConditionalState);

                // At this point, State.Reachable may be false for
                // invalid code such as `s + throw new Exception()`.
                TypeSymbolWithAnnotations rightType = _resultType;

                if (warnOnNullReferenceArgument)
                {
                    ReportArgumentWarnings(binary.Right, rightType, binary.MethodOpt.Parameters[1]);
                }

                Debug.Assert(!IsConditionalState);
                _resultType = InferResultNullability(binary, leftType, rightType);

                BinaryOperatorKind op = binary.OperatorKind.Operator();
                if (op == BinaryOperatorKind.Equal || op == BinaryOperatorKind.NotEqual)
                {
                    BoundExpression operandComparedToNull = null;
                    TypeSymbolWithAnnotations operandComparedToNullType = default;

                    if (binary.Right.ConstantValue?.IsNull == true)
                    {
                        operandComparedToNull = binary.Left;
                        operandComparedToNullType = leftType;
                    }
                    else if (binary.Left.ConstantValue?.IsNull == true)
                    {
                        operandComparedToNull = binary.Right;
                        operandComparedToNullType = rightType;
                    }

                    if (operandComparedToNull != null)
                    {
                        // https://github.com/dotnet/roslyn/issues/29953: We should only report such
                        // diagnostics for locals that are set or checked explicitly within this method.
                        if (!operandComparedToNullType.IsNull && operandComparedToNullType.NullableAnnotation.IsAnyNotNullable())
                        {
                            ReportNonSafetyDiagnostic(op == BinaryOperatorKind.Equal ?
                                                                    ErrorCode.HDN_NullCheckIsProbablyAlwaysFalse :
                                                                    ErrorCode.HDN_NullCheckIsProbablyAlwaysTrue,
                                                                binary.Syntax);
                        }

                        // Skip reference conversions
                        operandComparedToNull = SkipReferenceConversions(operandComparedToNull);

                        // Set all nested conditional slots. For example in a?.b?.c we'll set a, b, and c.
                        var slotBuilder = ArrayBuilder<int>.GetInstance();
                        GetSlotsToMarkAsNotNullable(operandComparedToNull, slotBuilder);
                        if (slotBuilder.Count != 0)
                        {
                            Split();
                            ref LocalState stateToUpdate = ref (op == BinaryOperatorKind.Equal) ? ref this.StateWhenFalse : ref this.StateWhenTrue;
                            MarkSlotsAsNotNullable(slotBuilder, ref stateToUpdate);
                        }
                        slotBuilder.Free();
                    }
                }
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
            Debug.Assert(_lastConditionalAccessSlot == -1);

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
                        // https://github.com/dotnet/roslyn/issues/29953 Detect when conversion has a nullable operand
                        operand = ((BoundConversion)operand).Operand;
                        continue;
                    case BoundKind.ConditionalAccess:
                        var conditional = (BoundConditionalAccess)operand;

                        slot = MakeSlot(conditional.Receiver);
                        if (slot > 0)
                        {
                            // If we got a slot we must have processed the previous conditional receiver.
                            Debug.Assert(_lastConditionalAccessSlot == -1);

                            // We need to continue the walk regardless of whether the receiver should be updated.
                            var receiverType = conditional.Receiver.Type;
                            if (shouldUpdateType(receiverType))
                            {
                                slotBuilder.Add(slot);
                            }

                            if (receiverType.IsNullableType())
                            {
                                slot = GetNullableOfTValueSlot(receiverType, slot);
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

                        // https://github.com/dotnet/roslyn/issues/29953 When we handle unconditional access survival (ie after
                        // c.D has been invoked, c must be nonnull or we've thrown a NullRef), revisit whether
                        // we need more special handling here

                        slot = MakeSlot(operand);
                        if (slot > 0 && shouldUpdateType(operand.Type))
                        {
                            // If we got a slot then all previous BoundCondtionalReceivers must have been handled.
                            Debug.Assert(_lastConditionalAccessSlot == -1);

                            slotBuilder.Add(slot);
                        }

                        break;
                }

                // If we didn't get a slot, it's possible that the current _lastConditionalSlot was never processed,
                // so we reset before leaving the function.
                _lastConditionalAccessSlot = -1;

                return;
            }

            bool shouldUpdateType(TypeSymbol operandType)
                => !(operandType is null) && (!operandType.IsValueType || operandType.IsNullableType());
        }

        private static void MarkSlotsAsNotNullable(ArrayBuilder<int> slots, ref LocalState stateToUpdate)
        {
            if (slots is null)
            {
                return;
            }

            foreach (int slot in slots)
            {
                stateToUpdate[slot] = NullableAnnotation.NotNullable;
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

        // https://github.com/dotnet/roslyn/issues/30140: Track nullable state from ??=.
        public override BoundNode VisitNullCoalescingAssignmentOperator(BoundNullCoalescingAssignmentOperator node)
        {
            var result = base.VisitNullCoalescingAssignmentOperator(node);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitNullCoalescingOperator(BoundNullCoalescingOperator node)
        {
            Debug.Assert(!IsConditionalState);

            BoundExpression leftOperand = node.LeftOperand;
            BoundExpression rightOperand = node.RightOperand;

            TypeSymbolWithAnnotations leftResult = VisitRvalueWithResult(leftOperand);
            TypeSymbolWithAnnotations rightResult;

            if (IsConstantNull(leftOperand))
            {
                rightResult = VisitRvalueWithResult(rightOperand);
                // Should be able to use rightResult for the result of the operator but
                // binding may have generated a different result type in the case of errors.
                _resultType = TypeSymbolWithAnnotations.Create(node.Type, getNullableAnnotation(rightOperand, rightResult));
                return null;
            }

            var leftState = this.State.Clone();
            if (leftResult.ValueCanBeNull() == false)
            {
                ReportNonSafetyDiagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, leftOperand.Syntax);
            }

            bool leftIsConstant = leftOperand.ConstantValue != null;
            if (leftIsConstant)
            {
                SetUnreachable();
            }

            // https://github.com/dotnet/roslyn/issues/29955 For cases where the left operand determines
            // the type, we should unwrap the right conversion and re-apply.
            rightResult = VisitRvalueWithResult(rightOperand);
            Join(ref this.State, ref leftState);
            TypeSymbol resultType;
            var leftResultType = leftResult.TypeSymbol;
            var rightResultType = rightResult.TypeSymbol;
            switch (node.OperatorResultKind)
            {
                case BoundNullCoalescingOperatorResultKind.NoCommonType:
                    resultType = node.Type;
                    break;
                case BoundNullCoalescingOperatorResultKind.LeftType:
                    resultType = getLeftResultType(leftResultType, rightResultType);
                    break;
                case BoundNullCoalescingOperatorResultKind.LeftUnwrappedType:
                    resultType = getLeftResultType(leftResultType.StrippedType(), rightResultType);
                    break;
                case BoundNullCoalescingOperatorResultKind.RightType:
                    resultType = getRightResultType(leftResultType, rightResultType);
                    break;
                case BoundNullCoalescingOperatorResultKind.LeftUnwrappedRightType:
                    resultType = getRightResultType(leftResultType.StrippedType(), rightResultType);
                    break;
                case BoundNullCoalescingOperatorResultKind.RightDynamicType:
                    resultType = rightResultType;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.OperatorResultKind);
            }

            NullableAnnotation resultNullableAnnotation;

            // We want to check if the value can be nullable based on annotations and getValueNullableAnnotation might
            // adjust the value that it returns to emphasize the fact. However, we want the original annotation to flow through the system.
            if (getValueNullableAnnotation(leftOperand, leftResult).IsAnyNotNullable())
            {
                resultNullableAnnotation = getNullableAnnotation(leftOperand, leftResult);
            }
            else
            {
                resultNullableAnnotation = getNullableAnnotation(rightOperand, rightResult);
            }

            _resultType = TypeSymbolWithAnnotations.Create(resultType, resultNullableAnnotation);
            return null;

            NullableAnnotation getNullableAnnotation(BoundExpression e, TypeSymbolWithAnnotations t)
            {
                if (t.IsNull)
                {
                    return GetNullableAnnotation(e);
                }

                return t.NullableAnnotation;
            }

            NullableAnnotation getValueNullableAnnotation(BoundExpression e, TypeSymbolWithAnnotations t)
            {
                if (t.IsNull)
                {
                    return GetNullableAnnotation(e);
                }

                return t.GetValueNullableAnnotation();
            }

            TypeSymbol getLeftResultType(TypeSymbol leftType, TypeSymbol rightType)
            {
                // If there was an identity conversion between the two operands (in short, if there
                // is no implicit conversion on the right operand), then check nullable conversions
                // in both directions since it's possible the right operand is the better result type.
                if ((object)rightType != null &&
                    (node.RightOperand as BoundConversion)?.ExplicitCastInCode != false &&
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
                case BoundKind.DefaultExpression:
                case BoundKind.Literal:
                    {
                        var constant = expr.ConstantValue;
                        if (constant != null)
                        {
                            if (constant.IsNull)
                            {
                                return NullableAnnotation.Nullable;
                            }
                            if (expr.Type?.IsReferenceType == true)
                            {
                                return NullableAnnotation.NotNullable;
                            }
                        }
                        return NullableAnnotation.Unknown;
                    }
                case BoundKind.ExpressionWithNullability:
                    return ((BoundExpressionWithNullability)expr).NullableAnnotation;
                case BoundKind.MethodGroup:
                case BoundKind.UnboundLambda:
                    return NullableAnnotation.Unknown;
                default:
                    Debug.Assert(false); // unexpected value
                    return NullableAnnotation.Unknown;
            }
        }

        public override BoundNode VisitConditionalAccess(BoundConditionalAccess node)
        {
            Debug.Assert(!IsConditionalState);

            var receiver = node.Receiver;
            var receiverType = VisitRvalueWithResult(receiver);

            var receiverState = this.State.Clone();

            if (receiver.Type?.IsValueType == false)
            {
                if (receiverType.ValueCanBeNull() == false)
                {
                    ReportNonSafetyDiagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, receiver.Syntax);
                }

                int slot = MakeSlot(SkipReferenceConversions(receiver));
                if (slot > 0)
                {
                    if (slot >= this.State.Capacity) Normalize(ref this.State);
                    this.State[slot] = NullableAnnotation.NotNullable;
                }
            }

            if (IsConstantNull(node.Receiver))
            {
                SetUnreachable();
            }

            VisitRvalue(node.AccessExpression);
            Join(ref this.State, ref receiverState);

            TypeSymbol type = node.Type;
            NullableAnnotation resultAnnotation;

            // If receiver or the access can produce nullable value, the result can be nullable.
            // Otherwise, result is not nullable.

            if (type.SpecialType == SpecialType.System_Void || type.IsErrorType() || _resultType.IsNull)
            {
                resultAnnotation = NullableAnnotation.Unknown;
            }
            else if (_resultType.IsPossiblyNullableReferenceTypeTypeParameter())
            {
                Debug.Assert(TypeSymbol.Equals(_resultType.TypeSymbol, type, TypeCompareKind.ConsiderEverything2));
                Conversion conversion;
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;

                if (!receiverType.GetValueNullableAnnotation().IsAnyNullable())
                {
                    resultAnnotation = NullableAnnotation.NotAnnotated; // Inherit nullability of the access
                }
                else if (receiverType.IsPossiblyNullableReferenceTypeTypeParameter() &&
                    (conversion = _conversions.ClassifyConversionFromType(receiverType.TypeSymbol, _resultType.TypeSymbol, ref useSiteDiagnostics)).Exists &&
                    !conversion.IsUserDefined)
                {
                    // where T : U
                    // T?.U or U?.T

                    // T?.U
                    // If T is nullable, U is also nullable - result is nullable
                    // If T is not nullable - result is nullable if U is nullable
                    // If U is nullable - result is nullable
                    // If U is not nullable, T is also not nullable - result is not nullable
                    // So, nullability of the result can be inferred from nullability of U, and the type of the result is U.   

                    // U ?. T
                    // If U is nullable - result is nullable
                    // If U is not nullable, T is also not nullable - result is not nullable
                    // If T is nullable, U is also nullable - result is nullable
                    // If T is not nullable - result is nullable if U is nullable
                    // So, nullability of the result can be inferred from nullability of U, but the type of the result is T.   
                    // At the moment we don't have a way to represent this correlation, result type is one type parameter, but
                    // nullability comes from another. 
                    // Ideally, we would want to have the following behavior:
                    //     U x = U?.T - no warning
                    //     T y = U?.T - a warning
                    // But we can track the state only in the way when either both produce a warning, or none.
                    // It feels like it is reasonable to prefer the latter approach, i.e. produce no warnings 
                    // for both scenarios - no false diagnostics.
                    resultAnnotation = NullableAnnotation.NotAnnotated; // Inherit nullability of U
                }
                else
                {
                    resultAnnotation = NullableAnnotation.Nullable;
                }
            }
            else
            {
                NullableAnnotation receiverAnnotation = receiverType.GetValueNullableAnnotation();
                NullableAnnotation accessAnnotation = _resultType.GetValueNullableAnnotation();
                if (receiverAnnotation.IsAnyNullable() || accessAnnotation.IsAnyNullable())
                {
                    resultAnnotation = NullableAnnotation.Nullable;
                }
                else if (receiverAnnotation == NullableAnnotation.Unknown || accessAnnotation == NullableAnnotation.Unknown)
                {
                    resultAnnotation = NullableAnnotation.Unknown;
                }
                else
                {
                    resultAnnotation = NullableAnnotation.NotNullable;
                }
            }

            // https://github.com/dotnet/roslyn/issues/29956 Use flow analysis type rather than node.Type
            // so that nested nullability is inferred from flow analysis. See VisitConditionalOperator.
            _resultType = TypeSymbolWithAnnotations.Create(type, resultAnnotation);
            // https://github.com/dotnet/roslyn/issues/29956 Report conversion warnings.
            return null;
        }

        public override BoundNode VisitConditionalOperator(BoundConditionalOperator node)
        {
            var isByRef = node.IsRef;

            VisitCondition(node.Condition);
            var consequenceState = this.StateWhenTrue;
            var alternativeState = this.StateWhenFalse;

            BoundExpression consequence;
            BoundExpression alternative;
            Conversion consequenceConversion;
            Conversion alternativeConversion;
            TypeSymbolWithAnnotations consequenceResult;
            TypeSymbolWithAnnotations alternativeResult;

            bool isConstantTrue = IsConstantTrue(node.Condition);
            bool isConstantFalse = IsConstantFalse(node.Condition);
            if (isConstantTrue)
            {
                (alternative, alternativeConversion, alternativeResult) = visitConditionalOperand(alternativeState, node.Alternative);
                (consequence, consequenceConversion, consequenceResult) = visitConditionalOperand(consequenceState, node.Consequence);
            }
            else if (isConstantFalse)
            {
                (consequence, consequenceConversion, consequenceResult) = visitConditionalOperand(consequenceState, node.Consequence);
                (alternative, alternativeConversion, alternativeResult) = visitConditionalOperand(alternativeState, node.Alternative);
            }
            else
            {
                (consequence, consequenceConversion, consequenceResult) = visitConditionalOperand(consequenceState, node.Consequence);
                Unsplit();
                (alternative, alternativeConversion, alternativeResult) = visitConditionalOperand(alternativeState, node.Alternative);
                Unsplit();
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
                // For constant conditions, we could use the nested nullability of the particular
                // branch, but that requires using the nullability of the branch as it applies to the
                // target type. For instance, the result of the conditional in the following should
                // be `IEnumerable<object>` not `object[]`:
                //   object[] a = ...;
                //   IEnumerable<object?> b = ...;
                //   var c = true ? a : b;
                BoundExpression consequencePlaceholder = CreatePlaceholderIfNecessary(consequence, consequenceResult);
                BoundExpression alternativePlaceholder = CreatePlaceholderIfNecessary(alternative, alternativeResult);
                bool hadNullabilityMismatch;
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                // https://github.com/dotnet/roslyn/issues/30432: InferBestTypeForConditionalOperator should use node.IsRef.
                resultType = BestTypeInferrer.InferBestTypeForConditionalOperator(consequencePlaceholder, alternativePlaceholder, _conversions, out _, out hadNullabilityMismatch, ref useSiteDiagnostics);
                Debug.Assert((object)resultType != null);
                if (hadNullabilityMismatch)
                {
                    ReportSafetyDiagnostic(
                        ErrorCode.WRN_NoBestNullabilityConditionalExpression,
                        node.Syntax,
                        GetTypeAsDiagnosticArgument(consequenceResult.TypeSymbol),
                        GetTypeAsDiagnosticArgument(alternativeResult.TypeSymbol));
                }
            }

            if ((object)resultType != null)
            {
                // Let's pretend the result type is nullable, in order to avoid warnings reported for top level nullability by ApplyConversion.
                var resultTypeWithAnnotations = TypeSymbolWithAnnotations.Create(resultType, resultType.IsValueType ? NullableAnnotation.Unknown : NullableAnnotation.Nullable);
                TypeSymbolWithAnnotations convertedConsequenceResult = default;
                TypeSymbolWithAnnotations convertedAlternativeResult = default;

                if (!isConstantFalse)
                {
                    convertedConsequenceResult = ApplyConversion(
                        node.Consequence,
                        consequence,
                        consequenceConversion,
                        resultTypeWithAnnotations,
                        consequenceResult,
                        checkConversion: true,
                        fromExplicitCast: false,
                        useLegacyWarnings: false,
                        AssignmentKind.Assignment);
                }

                if (!isConstantTrue)
                {
                    convertedAlternativeResult = ApplyConversion(
                        node.Alternative,
                        alternative,
                        alternativeConversion,
                        resultTypeWithAnnotations,
                        alternativeResult,
                        checkConversion: true,
                        fromExplicitCast: false,
                        useLegacyWarnings: false,
                        AssignmentKind.Assignment);
                }

                if (convertedAlternativeResult.IsNull)
                {
                    Debug.Assert(!convertedConsequenceResult.IsNull);
                    _resultType = convertedConsequenceResult;
                }
                else if (convertedConsequenceResult.IsNull)
                {
                    Debug.Assert(!convertedAlternativeResult.IsNull);
                    _resultType = convertedAlternativeResult;
                }
                else
                {
                    _resultType = TypeSymbolWithAnnotations.Create(resultType,
                                                                   convertedConsequenceResult.NullableAnnotation.JoinForFlowAnalysisBranches(convertedAlternativeResult.NullableAnnotation,
                                                                                                                                             resultType,
                                                                                                                                             type => type.IsPossiblyNullableReferenceTypeTypeParameter()));
                }
            }
            else
            {
                NullableAnnotation resultNullableAnnotation;

                if (isConstantTrue)
                {
                    resultNullableAnnotation = getNullableAnnotation(consequence, consequenceResult);
                }
                else if (isConstantFalse)
                {
                    resultNullableAnnotation = getNullableAnnotation(alternative, alternativeResult);
                }
                else
                {
                    resultNullableAnnotation = getNullableAnnotation(consequence, consequenceResult).JoinForFlowAnalysisBranches(getNullableAnnotation(alternative, alternativeResult),
                                                                                                                                 node.Type,
                                                                                                                                 type => type.IsPossiblyNullableReferenceTypeTypeParameter());
                }

                _resultType = TypeSymbolWithAnnotations.Create(node.Type.SetUnknownNullabilityForReferenceTypes(), resultNullableAnnotation);
            }

            return null;

            NullableAnnotation getNullableAnnotation(BoundExpression expr, TypeSymbolWithAnnotations type)
            {
                if (!type.IsNull)
                {
                    return type.GetValueNullableAnnotation();
                }
                if (expr.IsLiteralNullOrDefault())
                {
                    return NullableAnnotation.Nullable;
                }
                return NullableAnnotation.Unknown;
            }

            (BoundExpression, Conversion, TypeSymbolWithAnnotations) visitConditionalOperand(LocalState state, BoundExpression operand)
            {
                Conversion conversion;
                SetState(state);
                if (isByRef)
                {
                    VisitLvalue(operand);
                    conversion = Conversion.Identity;
                }
                else
                {
                    (operand, conversion) = RemoveConversion(operand, includeExplicitConversions: false);
                    Visit(operand);
                }
                return (operand, conversion, _resultType);
            }
        }

        private static BoundExpression CreatePlaceholderIfNecessary(BoundExpression expr, TypeSymbolWithAnnotations type)
        {
            return type.IsNull ?
                expr :
                new BoundExpressionWithNullability(expr.Syntax, expr, type.NullableAnnotation, type.TypeSymbol);
        }

        public override BoundNode VisitConditionalReceiver(BoundConditionalReceiver node)
        {
            var result = base.VisitConditionalReceiver(node);
            // https://github.com/dotnet/roslyn/issues/29956 ConditionalReceiver does not
            // have a result type. Should this be moved to ConditionalAccess?
            _resultType = TypeSymbolWithAnnotations.Create(node.Type, NullableAnnotation.NotNullable);
            return result;
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            // Note: we analyze even omitted calls
            var method = node.Method;
            var receiverOpt = node.ReceiverOpt;
            TypeSymbolWithAnnotations receiverType = default;

            if (receiverOpt != null && method.MethodKind != MethodKind.Constructor)
            {
                receiverType = VisitRvalueWithResult(receiverOpt);
                // https://github.com/dotnet/roslyn/issues/30598: Mark receiver as not null
                // after arguments have been visited, and only if the receiver has not changed.
                CheckPossibleNullReceiver(receiverOpt);
            }

            // https://github.com/dotnet/roslyn/issues/29605 Can we handle some error cases?
            // (Compare with CSharpOperationFactory.CreateBoundCallOperation.)
            ImmutableArray<RefKind> refKindsOpt = node.ArgumentRefKindsOpt;
            (ImmutableArray<BoundExpression> arguments, ImmutableArray<Conversion> conversions) = RemoveArgumentConversions(node.Arguments, refKindsOpt);
            if (!receiverType.IsNull)
            {
                // Update method based on inferred receiver type.
                method = (MethodSymbol)AsMemberOfResultType(receiverType, method);
            }

            method = VisitArguments(node, arguments, refKindsOpt, method.Parameters, node.ArgsToParamsOpt,
                node.Expanded, node.InvokedAsExtensionMethod, conversions, method);

            if (method.MethodKind == MethodKind.LocalFunction)
            {
                var localFunc = (LocalFunctionSymbol)method.OriginalDefinition;
                ReplayReadsAndWrites(localFunc, node.Syntax, writes: true);
            }

            //if (this.State.Reachable) // Consider reachability: see https://github.com/dotnet/roslyn/issues/28798
            {
                _resultType = method.ReturnType;
            }

            return null;
        }

        /// <summary>
        /// For each argument, figure out if its corresponding parameter is annotated with NotNullWhenFalse or
        /// EnsuresNotNull.
        /// </summary>
        private static ImmutableArray<FlowAnalysisAnnotations> GetAnnotations(int numArguments,
            bool expanded, ImmutableArray<ParameterSymbol> parameters, ImmutableArray<int> argsToParamsOpt)
        {
            ArrayBuilder<FlowAnalysisAnnotations> builder = null;

            for (int i = 0; i < numArguments; i++)
            {
                (ParameterSymbol parameter, _) = GetCorrespondingParameter(i, parameters, argsToParamsOpt, expanded);
                FlowAnalysisAnnotations annotations = parameter?.FlowAnalysisAnnotations ?? FlowAnalysisAnnotations.None;

                annotations = removeInapplicableAnnotations(parameter, annotations);

                if (annotations != FlowAnalysisAnnotations.None && builder == null)
                {
                    builder = ArrayBuilder<FlowAnalysisAnnotations>.GetInstance(numArguments);
                    builder.AddMany(FlowAnalysisAnnotations.None, i);
                }

                if (builder != null)
                {
                    builder.Add(annotations);
                }
            }

            return builder == null ? default : builder.ToImmutableAndFree();

            FlowAnalysisAnnotations removeInapplicableAnnotations(ParameterSymbol parameter, FlowAnalysisAnnotations annotations)
            {
                // Ignore NotNullWhenTrue that is inapplicable
                annotations = removeInapplicableNotNullWhenSense(parameter, annotations, sense: true);

                // Ignore NotNullWhenFalse that is inapplicable
                annotations = removeInapplicableNotNullWhenSense(parameter, annotations, sense: false);

                const FlowAnalysisAnnotations both = FlowAnalysisAnnotations.AssertsTrue | FlowAnalysisAnnotations.AssertsFalse;
                if (parameter?.Type.SpecialType != SpecialType.System_Boolean)
                {
                    // AssertsTrue and AssertsFalse must be applied to a bool parameter
                    annotations &= ~both;
                }
                else if ((annotations & both) == both)
                {
                    // We'll ignore AssertsTrue and AssertsFalse if both set
                    annotations &= ~both;
                }

                return annotations;
            }

            FlowAnalysisAnnotations removeInapplicableNotNullWhenSense(ParameterSymbol parameter, FlowAnalysisAnnotations annotations, bool sense)
            {
                if (parameter is null)
                {
                    return annotations;
                }

                var whenSense = sense ? FlowAnalysisAnnotations.NotNullWhenTrue : FlowAnalysisAnnotations.NotNullWhenFalse;
                var whenNotSense = sense ? FlowAnalysisAnnotations.NotNullWhenFalse : FlowAnalysisAnnotations.NotNullWhenTrue;

                // NotNullWhenSense (without NotNullWhenNotSense) must be applied on a bool-returning member
                if ((annotations & whenSense) != 0 &&
                    (annotations & whenNotSense) == 0 &&
                    parameter.ContainingSymbol.GetTypeOrReturnType().SpecialType != SpecialType.System_Boolean)
                {
                    annotations &= ~whenSense;
                }

                // NotNullWhenSense must be applied to a reference type, a nullable value type, or an unconstrained generic type
                if ((annotations & whenSense) != 0 && parameter.Type.IsValueType && !parameter.Type.IsNullableType())
                {
                    annotations &= ~whenSense;
                }

                // NotNullWhenSense is inapplicable when argument corresponds to params parameter and we're in expanded form
                if ((annotations & whenSense) != 0 && expanded && ReferenceEquals(parameter, parameters.Last()))
                {
                    annotations &= ~whenSense;
                }

                return annotations;
            }
        }

        // https://github.com/dotnet/roslyn/issues/29863 Record in the node whether type
        // arguments were implicit, to allow for cases where the syntax is not an
        // invocation (such as a synthesized call from a query interpretation).
        private static bool HasImplicitTypeArguments(BoundExpression node)
        {
            var syntax = node.Syntax;
            if (syntax.Kind() != SyntaxKind.InvocationExpression)
            {
                // Unexpected syntax kind.
                return false;
            }
            var nameSyntax = Binder.GetNameSyntax(((InvocationExpressionSyntax)syntax).Expression, out var _);
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

        private void VisitArguments(
            BoundExpression node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            MethodSymbol method,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded)
        {
            ImmutableArray<Conversion> conversions;
            (arguments, conversions) = RemoveArgumentConversions(arguments, refKindsOpt);
            VisitArguments(node, arguments, refKindsOpt, method is null ? default : method.Parameters, argsToParamsOpt, expanded, invokedAsExtensionMethod: false, conversions);
        }

        private void VisitArguments(
            BoundExpression node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            PropertySymbol property,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded)
        {
            ImmutableArray<Conversion> conversions;
            (arguments, conversions) = RemoveArgumentConversions(arguments, refKindsOpt);
            VisitArguments(node, arguments, refKindsOpt, property is null ? default : property.Parameters, argsToParamsOpt, expanded, invokedAsExtensionMethod: false, conversions);
        }

        /// <summary>
        /// If you pass in a method symbol, its type arguments will be re-inferred and the re-inferred method will be returned.
        /// </summary>
        private MethodSymbol VisitArguments(
            BoundExpression node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded,
            bool invokedAsExtensionMethod,
            ImmutableArray<Conversion> conversions,
            MethodSymbol method = null)
        {
            Debug.Assert(!arguments.IsDefault);
            var savedState = this.State.Clone();

            // We do a first pass to work through the arguments without making any assumptions
            ImmutableArray<TypeSymbolWithAnnotations> results = VisitArgumentsEvaluate(arguments, refKindsOpt);

            if ((object)method != null && method.IsGenericMethod)
            {
                if (HasImplicitTypeArguments(node))
                {
                    method = InferMethodTypeArguments((BoundCall)node, method, GetArgumentsForMethodTypeInference(arguments, results));
                    parameters = method.Parameters;
                }
                if (ConstraintsHelper.RequiresChecking(method))
                {
                    var syntax = node.Syntax;
                    CheckMethodConstraints((syntax as InvocationExpressionSyntax)?.Expression ?? syntax, method);
                }
            }

            if (!node.HasErrors && !parameters.IsDefault)
            {
                VisitArgumentConversions(arguments, conversions, refKindsOpt, parameters, argsToParamsOpt, expanded, invokedAsExtensionMethod, results);
            }

            // We do a second pass through the arguments, ignoring any diagnostics produced, but honoring the annotations,
            // to get the proper result state.
            ImmutableArray<FlowAnalysisAnnotations> annotations = GetAnnotations(arguments.Length, expanded, parameters, argsToParamsOpt);

            if (!annotations.IsDefault)
            {
                this.SetState(savedState);

                bool saveDisableDiagnostics = _disableDiagnostics;
                _disableDiagnostics = true;
                if (!node.HasErrors && !parameters.IsDefault)
                {
                    VisitArgumentConversions(arguments, conversions, refKindsOpt, parameters, argsToParamsOpt, expanded, invokedAsExtensionMethod, results); // recompute out vars after state was reset
                }
                VisitArgumentsEvaluateHonoringAnnotations(arguments, refKindsOpt, annotations);

                _disableDiagnostics = saveDisableDiagnostics;
            }

            return method;
        }

        private ImmutableArray<TypeSymbolWithAnnotations> VisitArgumentsEvaluate(
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt)
        {
            Debug.Assert(!IsConditionalState);
            int n = arguments.Length;
            if (n == 0)
            {
                return ImmutableArray<TypeSymbolWithAnnotations>.Empty;
            }
            var builder = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance(n);
            for (int i = 0; i < n; i++)
            {
                VisitArgumentEvaluate(arguments, refKindsOpt, i, preserveConditionalState: false);
                builder.Add(_resultType);
            }

            _resultType = _invalidType;
            return builder.ToImmutableAndFree();
        }

        private void VisitArgumentEvaluate(ImmutableArray<BoundExpression> arguments, ImmutableArray<RefKind> refKindsOpt, int i, bool preserveConditionalState)
        {
            Debug.Assert(!IsConditionalState);
            RefKind refKind = GetRefKind(refKindsOpt, i);
            var argument = arguments[i];
            if (refKind != RefKind.Out)
            {
                // https://github.com/dotnet/roslyn/issues/29958 `ref` arguments should be treated as l-values
                // for assignment. See `ref x3` in NullableReferenceTypesTests.PassingParameters_01.
                if (preserveConditionalState)
                {
                    Visit(argument);
                    // No Unsplit
                }
                else
                {
                    VisitRvalue(argument);
                }
            }
            else
            {
                // As far as we can tell, there is no scenario relevant to nullability analysis
                // where splitting an L-value (for instance with a ref ternary) would affect the result.
                VisitLvalue(argument);
            }
        }

        /// <summary>
        /// Visit all the arguments for the purpose of computing the exit state of the method,
        /// given the annotations.
        /// If there is any [NotNullWhenTrue/False] annotation, then we'll return in a conditional state for the invocation.
        /// </summary>
        private void VisitArgumentsEvaluateHonoringAnnotations(
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            ImmutableArray<FlowAnalysisAnnotations> annotations)
        {
            Debug.Assert(!IsConditionalState);
            Debug.Assert(annotations.Length == arguments.Length);
            Debug.Assert(_disableDiagnostics);

            for (int i = 0; i < arguments.Length; i++)
            {
                FlowAnalysisAnnotations annotation = annotations[i];
                bool assertsTrue = (annotation & FlowAnalysisAnnotations.AssertsTrue) != 0;
                bool assertsFalse = (annotation & FlowAnalysisAnnotations.AssertsFalse) != 0;

                if (this.IsConditionalState)
                {
                    // We could be in a conditional state because of a conditional annotation (like NotNullWhenFalse)
                    // Then WhenTrue/False states correspond to the invocation returning true/false

                    // We'll first assume that we're in the unconditional state where the method returns true,
                    // then we'll repeat assuming the method returns false.

                    LocalState whenTrue = this.StateWhenTrue.Clone();
                    LocalState whenFalse = this.StateWhenFalse.Clone();

                    this.SetState(whenTrue);
                    visitArgumentEvaluateAndUnsplit(i, assertsTrue, assertsFalse);
                    Debug.Assert(!IsConditionalState);
                    whenTrue = this.State; // LocalState may be a struct

                    this.SetState(whenFalse);
                    visitArgumentEvaluateAndUnsplit(i, assertsTrue, assertsFalse);
                    Debug.Assert(!IsConditionalState);
                    whenFalse = this.State; // LocalState may be a struct

                    this.SetConditionalState(whenTrue, whenFalse);
                }
                else
                {
                    visitArgumentEvaluateAndUnsplit(i, assertsTrue, assertsFalse);
                }

                var argument = arguments[i];
                var argumentType = argument.Type;
                if ((object)argumentType == null || (argumentType.IsValueType && !argumentType.IsNullableType()))
                {
                    continue;
                }

                bool notNullWhenTrue = (annotation & FlowAnalysisAnnotations.NotNullWhenTrue) != 0;
                bool notNullWhenFalse = (annotation & FlowAnalysisAnnotations.NotNullWhenFalse) != 0;
                if (notNullWhenTrue || notNullWhenFalse)
                {
                    // The WhenTrue/False states correspond to the invocation returning true/false
                    bool wasPreviouslySplit = this.IsConditionalState;
                    Split();

                    var slotBuilder = ArrayBuilder<int>.GetInstance();
                    GetSlotsToMarkAsNotNullable(arguments[i], slotBuilder);

                    if (notNullWhenTrue)
                    {
                        MarkSlotsAsNotNullable(slotBuilder, ref StateWhenTrue);
                    }
                    if (notNullWhenFalse)
                    {
                        MarkSlotsAsNotNullable(slotBuilder, ref StateWhenFalse);
                        if (notNullWhenTrue && !wasPreviouslySplit) Unsplit();
                    }
                    slotBuilder.Free();
                }
            }

            _resultType = _invalidType;

            // Evaluate an argument, potentially producing a split state.
            // Then unsplit it based on [AssertsTrue] or [AssertsFalse] attributes, or default Unsplit otherwise.
            void visitArgumentEvaluateAndUnsplit(int argumentIndex, bool assertsTrue, bool assertsFalse)
            {
                Debug.Assert(!IsConditionalState);
                VisitArgumentEvaluate(arguments, refKindsOpt, argumentIndex, preserveConditionalState: true);

                if (!this.IsConditionalState)
                {
                    return;
                }
                else if (assertsTrue)
                {
                    this.SetState(this.StateWhenTrue);
                }
                else if (assertsFalse)
                {
                    this.SetState(this.StateWhenFalse);
                }
                else
                {
                    this.Unsplit();
                }
            }
        }

        private void VisitArgumentConversions(
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<Conversion> conversions,
            ImmutableArray<RefKind> refKindsOpt,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded,
            bool invokedAsExtensionMethod,
            ImmutableArray<TypeSymbolWithAnnotations> results)
        {
            for (int i = 0; i < arguments.Length; i++)
            {
                (ParameterSymbol parameter, TypeSymbolWithAnnotations parameterType) = GetCorrespondingParameter(i, parameters, argsToParamsOpt, expanded);
                if (parameter is null)
                {
                    continue;
                }
                VisitArgumentConversion(
                    arguments[i],
                    conversions.IsDefault ? Conversion.Identity : conversions[i],
                    GetRefKind(refKindsOpt, i),
                    parameter,
                    parameterType,
                    results[i],
                    invokedAsExtensionMethod && i == 0);
            }
        }

        /// <summary>
        /// Report warnings for an argument corresponding to a specific parameter.
        /// </summary>
        private void VisitArgumentConversion(
            BoundExpression argument,
            Conversion conversion,
            RefKind refKind,
            ParameterSymbol parameter,
            TypeSymbolWithAnnotations parameterType,
            TypeSymbolWithAnnotations resultType,
            bool extensionMethodThisArgument)
        {
            var argumentType = resultType.TypeSymbol;
            switch (refKind)
            {
                case RefKind.None:
                case RefKind.In:
                    {
                        ApplyConversion(
                            argument,
                            argument,
                            conversion,
                            parameterType,
                            resultType,
                            checkConversion: true,
                            fromExplicitCast: false,
                            useLegacyWarnings: false,
                            AssignmentKind.Argument,
                            target: parameter,
                            extensionMethodThisArgument: extensionMethodThisArgument);
                    }
                    break;
                case RefKind.Out:
                    {
                        if (argument is BoundLocal local && local.DeclarationKind == BoundLocalDeclarationKind.WithInferredType)
                        {
                            _variableTypes[local.LocalSymbol] = parameterType;
                            resultType = parameterType;
                        }

                        TypeSymbolWithAnnotations adjustedParameterType = adjustNullableAnnotationForNullabilityCheck(parameterType, resultType);

                        if (!ReportNullableAssignmentIfNecessary(argument, resultType, adjustedParameterType, useLegacyWarnings: UseLegacyWarnings(argument)))
                        {
                            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                            if (!_conversions.HasIdentityOrImplicitReferenceConversion(parameterType.TypeSymbol, argumentType, ref useSiteDiagnostics))
                            {
                                ReportNullabilityMismatchInArgument(argument, argumentType, parameter, parameterType.TypeSymbol);
                            }
                        }
                        // Set nullable state of argument to parameter type.
                        TrackNullableStateForAssignment(argument, resultType, MakeSlot(argument), parameterType);
                        break;
                    }
                case RefKind.Ref:
                    {
                        bool reportedWarning = false;
                        if (argument.Kind != BoundKind.SuppressNullableWarningExpression)
                        {
                            // Effect of this call is likely not observable due to https://github.com/dotnet/roslyn/issues/30946.
                            // See unit test NullabilityOfTypeParameters_080 for an attempt to see the effect.
                            TypeSymbolWithAnnotations adjustedResultType = adjustNullableAnnotationForNullabilityCheck(resultType, parameterType);

                            reportedWarning = ReportNullableAssignmentIfNecessary(argument, parameterType, adjustedResultType, useLegacyWarnings: false, assignmentKind: AssignmentKind.Argument, target: parameter);

                            if (!reportedWarning)
                            {
                                TypeSymbolWithAnnotations adjustedParameterType = adjustNullableAnnotationForNullabilityCheck(parameterType, resultType);

                                reportedWarning = ReportNullableAssignmentIfNecessary(argument, resultType, adjustedParameterType, useLegacyWarnings: UseLegacyWarnings(argument));
                            }
                        }

                        if (!reportedWarning)
                        {
                            if ((object)argumentType != null &&
                                IsNullabilityMismatch(argumentType, parameterType.TypeSymbol))
                            {
                                ReportNullabilityMismatchInArgument(argument, argumentType, parameter, parameterType.TypeSymbol);
                            }
                        }
                        // Set nullable state of argument to parameter type.
                        TrackNullableStateForAssignment(argument, resultType, MakeSlot(argument), parameterType);
                        break;
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(refKind);
            }

            TypeSymbolWithAnnotations adjustNullableAnnotationForNullabilityCheck(TypeSymbolWithAnnotations sourceType, TypeSymbolWithAnnotations destinationType)
            {
                if (sourceType.IsPossiblyNullableReferenceTypeTypeParameter() && !destinationType.IsPossiblyNullableReferenceTypeTypeParameter())
                {
                    return TypeSymbolWithAnnotations.Create(sourceType.TypeSymbol, NullableAnnotation.Nullable);
                }

                return sourceType;
            }
        }

        private static (ImmutableArray<BoundExpression> Arguments, ImmutableArray<Conversion> Conversions) RemoveArgumentConversions(
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
                    // https://github.com/dotnet/roslyn/issues/29958 Should `RefKind.In` be treated similarly to `RefKind.None`?
                    if (refKind == RefKind.None)
                    {
                        var before = argument;
                        (argument, conversion) = RemoveConversion(argument, includeExplicitConversions: false);
                        if (argument != before)
                        {
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

        private VariableState GetVariableState()
        {
            // https://github.com/dotnet/roslyn/issues/29617 To track nullability of captured variables inside and
            // outside a lambda, the lambda should be considered executed at the location the lambda
            // is converted to a delegate.
            return new VariableState(
                _variableSlot.ToImmutableDictionary(),
                ImmutableArray.Create(variableBySlot, start: 0, length: nextVariableSlot),
                _variableTypes.ToImmutableDictionary());
        }

        private UnboundLambda GetUnboundLambda(BoundLambda expr, VariableState variableState)
        {
            return expr.UnboundLambda.WithNullableState(_binder, variableState);
        }

        private static (ParameterSymbol Parameter, TypeSymbolWithAnnotations Type) GetCorrespondingParameter(
            int argumentOrdinal,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded)
        {
            if (parameters.IsDefault)
            {
                return (default, default);
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
                return (default, default);
            }

            var type = parameter.Type;
            if (expanded && parameter.Ordinal == n - 1 && type.IsSZArray())
            {
                type = ((ArrayTypeSymbol)type.TypeSymbol).ElementType;
            }

            return (parameter, type);
        }

        private MethodSymbol InferMethodTypeArguments(BoundCall node, MethodSymbol method, ImmutableArray<BoundExpression> arguments)
        {
            Debug.Assert(method.IsGenericMethod);
            // https://github.com/dotnet/roslyn/issues/27961 OverloadResolution.IsMemberApplicableInNormalForm and
            // IsMemberApplicableInExpandedForm use the least overridden method. We need to do the same here.
            var definition = method.ConstructedFrom;
            var refKinds = ArrayBuilder<RefKind>.GetInstance();
            if (node.ArgumentRefKindsOpt != null)
            {
                refKinds.AddRange(node.ArgumentRefKindsOpt);
            }
            // https://github.com/dotnet/roslyn/issues/27961 Do we really need OverloadResolution.GetEffectiveParameterTypes?
            // Aren't we doing roughly the same calculations in GetCorrespondingParameter?
            OverloadResolution.GetEffectiveParameterTypes(
                definition,
                arguments.Length,
                node.ArgsToParamsOpt,
                refKinds,
                isMethodGroupConversion: false,
                // https://github.com/dotnet/roslyn/issues/27961 `allowRefOmittedArguments` should be
                // false for constructors and several other cases (see Binder use). Should we
                // capture the original value in the BoundCall?
                allowRefOmittedArguments: true,
                binder: _binder,
                expanded: node.Expanded,
                parameterTypes: out ImmutableArray<TypeSymbolWithAnnotations> parameterTypes,
                parameterRefKinds: out ImmutableArray<RefKind> parameterRefKinds);
            refKinds.Free();
            bool hadNullabilityMismatch;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var result = MethodTypeInferrer.Infer(
                _binder,
                _conversions,
                definition.TypeParameters,
                definition.ContainingType,
                parameterTypes,
                parameterRefKinds,
                arguments,
                out hadNullabilityMismatch,
                ref useSiteDiagnostics,
                getNullableAnnotationOpt: expr => GetNullableAnnotation(expr));
            if (!result.Success)
            {
                return method;
            }
            if (hadNullabilityMismatch)
            {
                ReportSafetyDiagnostic(ErrorCode.WRN_CantInferNullabilityOfMethodTypeArgs, node.Syntax, definition);
            }
            return definition.Construct(result.InferredTypeArguments);
        }

        private ImmutableArray<BoundExpression> GetArgumentsForMethodTypeInference(ImmutableArray<BoundExpression> arguments, ImmutableArray<TypeSymbolWithAnnotations> argumentResults)
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
                builder.Add(getArgumentForMethodTypeInference(arguments[i], argumentResults[i]));
            }
            return builder.ToImmutableAndFree();

            BoundExpression getArgumentForMethodTypeInference(BoundExpression argument, TypeSymbolWithAnnotations argumentType)
            {
                if (argument.Kind == BoundKind.Lambda)
                {
                    // MethodTypeInferrer must infer nullability for lambdas based on the nullability
                    // from flow analysis rather than the declared nullability. To allow that, we need
                    // to re-bind lambdas in MethodTypeInferrer.
                    return GetUnboundLambda((BoundLambda)argument, GetVariableState());
                }
                if (argumentType.IsNull)
                {
                    return argument;
                }
                if (argument is BoundLocal local && local.DeclarationKind == BoundLocalDeclarationKind.WithInferredType)
                {
                    // 'out var' doesn't contribute to inference
                    return new BoundExpressionWithNullability(argument.Syntax, argument, NullableAnnotation.Unknown, type: null);
                }
                return new BoundExpressionWithNullability(argument.Syntax, argument, argumentType.NullableAnnotation, argumentType.TypeSymbol);
            }
        }

        private void CheckMethodConstraints(SyntaxNode syntax, MethodSymbol method)
        {
            var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            var warningsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
            ConstraintsHelper.CheckMethodConstraints(
                method,
                _conversions,
                compilation,
                diagnosticsBuilder,
                warningsBuilder,
                ref useSiteDiagnosticsBuilder);
            foreach (var pair in warningsBuilder)
            {
                Diagnostics.Add(pair.DiagnosticInfo, syntax.Location);
            }
            useSiteDiagnosticsBuilder?.Free();
            warningsBuilder.Free();
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
        private static (BoundExpression Expression, Conversion Conversion) RemoveConversion(BoundExpression expr, bool includeExplicitConversions)
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
                    Debug.Assert(expr.Kind != BoundKind.Conversion ||
                        ((BoundConversion)expr).ConversionGroupOpt != null ||
                        ((BoundConversion)expr).ConversionKind == ConversionKind.NoConversion);
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
            if (!canConvertNestedNullability && reportMismatch)
            {
                ReportSafetyDiagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, sourceExpression.Syntax, GetTypeAsDiagnosticArgument(sourceType), destinationType);
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
        private TypeSymbolWithAnnotations GetAdjustedResult(TypeSymbolWithAnnotations type, int slot)
        {
            if (slot > 0 && slot < this.State.Capacity)
            {
                NullableAnnotation annotation = this.State[slot];
                if (annotation != type.NullableAnnotation)
                {
                    return TypeSymbolWithAnnotations.Create(type.TypeSymbol, annotation);
                }
            }
            return type;
        }

        private static Symbol AsMemberOfResultType(TypeSymbolWithAnnotations resultType, Symbol symbol)
        {
            var containingType = resultType.TypeSymbol as NamedTypeSymbol;
            if ((object)containingType == null || containingType.IsErrorType())
            {
                return symbol;
            }
            return AsMemberOfType(containingType, symbol);
        }

        private static Symbol AsMemberOfType(NamedTypeSymbol containingType, Symbol symbol)
        {
            Debug.Assert((object)symbol != null);
            if (symbol.Kind == SymbolKind.Method)
            {
                if (((MethodSymbol)symbol).MethodKind == MethodKind.LocalFunction)
                {
                    // https://github.com/dotnet/roslyn/issues/27233 Handle type substitution for local functions.
                    return symbol;
                }
            }
            var symbolDef = symbol.OriginalDefinition;
            var symbolDefContainer = symbolDef.ContainingType;
            while (true)
            {
                if (containingType.OriginalDefinition.Equals(symbolDefContainer, TypeCompareKind.AllIgnoreOptions))
                {
                    if (symbolDefContainer.IsTupleType)
                    {
                        return AsMemberOfTupleType((TupleTypeSymbol)containingType, symbol);
                    }
                    var result = symbolDef.SymbolAsMember(containingType);
                    if (result is MethodSymbol resultMethod && resultMethod.IsGenericMethod)
                    {
                        return resultMethod.Construct(((MethodSymbol)symbol).TypeArguments);
                    }
                    return result;
                }
                containingType = containingType.BaseTypeNoUseSiteDiagnostics;
                if ((object)containingType == null)
                {
                    break;
                }
            }
            // https://github.com/dotnet/roslyn/issues/29967 Handle other cases such as interfaces.
            return symbol;
        }

        private static Symbol AsMemberOfTupleType(TupleTypeSymbol tupleType, Symbol symbol)
        {
            if (symbol.ContainingType.Equals(tupleType))
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
            // https://github.com/dotnet/roslyn/issues/29959 Assert VisitConversion is only used for explicit conversions.
            //Debug.Assert(node.ExplicitCastInCode);
            //Debug.Assert(node.ConversionGroupOpt != null);
            //Debug.Assert(!node.ConversionGroupOpt.ExplicitType.IsNull);

            TypeSymbolWithAnnotations explicitType = node.ConversionGroupOpt?.ExplicitType ?? default;
            bool fromExplicitCast = !explicitType.IsNull;
            TypeSymbolWithAnnotations targetType = fromExplicitCast ? explicitType : TypeSymbolWithAnnotations.Create(node.Type);
            Debug.Assert(!targetType.IsNull);

            (BoundExpression operand, Conversion conversion) = RemoveConversion(node, includeExplicitConversions: true);
            TypeSymbolWithAnnotations operandType = VisitRvalueWithResult(operand);
            _resultType = ApplyConversion(
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
                reportNestedWarnings: true);
            return null;
        }

        /// <summary>
        /// Visit an expression. If an explicit target type is provided, the expression is converted
        /// to that type. This method should be called whenever an expression may contain
        /// an implicit conversion, even if that conversion was omitted from the bound tree,
        /// so the conversion can be re-classified with nullability.
        /// </summary>
        private TypeSymbolWithAnnotations VisitOptionalImplicitConversion(BoundExpression expr, TypeSymbolWithAnnotations targetTypeOpt, bool useLegacyWarnings, AssignmentKind assignmentKind)
        {
            if (targetTypeOpt.IsNull)
            {
                return VisitRvalueWithResult(expr);
            }

            (BoundExpression operand, Conversion conversion) = RemoveConversion(expr, includeExplicitConversions: false);
            var operandType = VisitRvalueWithResult(operand);
            // If an explicit conversion was used in place of an implicit conversion, the explicit
            // conversion was created by initial binding after reporting "error CS0266:
            // Cannot implicitly convert type '...' to '...'. An explicit conversion exists ...".
            // Since an error was reported, we don't need to report nested warnings as well.
            bool reportNestedWarnings = !conversion.IsExplicit;
            return ApplyConversion(
                expr,
                operand,
                conversion,
                targetTypeOpt,
                operandType,
                checkConversion: true,
                fromExplicitCast: false,
                useLegacyWarnings: useLegacyWarnings,
                assignmentKind,
                reportTopLevelWarnings: true,
                reportNestedWarnings: reportNestedWarnings);
        }

        public override BoundNode VisitTupleLiteral(BoundTupleLiteral node)
        {
            VisitTupleExpression(node);
            return null;
        }

        public override BoundNode VisitConvertedTupleLiteral(BoundConvertedTupleLiteral node)
        {
            VisitTupleExpression(node);
            return null;
        }

        private void VisitTupleExpression(BoundTupleExpression node)
        {
            var arguments = node.Arguments;
            ImmutableArray<TypeSymbolWithAnnotations> elementTypes = arguments.SelectAsArray((a, w) => w.VisitRvalueWithResult(a), this);
            var tupleOpt = (TupleTypeSymbol)node.Type;
            _resultType = (tupleOpt is null) ?
                default :
                TypeSymbolWithAnnotations.Create(tupleOpt.WithElementTypes(elementTypes), NullableAnnotation.NotNullable);
        }

        public override BoundNode VisitTupleBinaryOperator(BoundTupleBinaryOperator node)
        {
            base.VisitTupleBinaryOperator(node);
            SetResult(node);
            return null;
        }

        private void ReportNullabilityMismatchWithTargetDelegate(SyntaxNode syntax, NamedTypeSymbol delegateType, MethodSymbol method)
        {
            Debug.Assert((object)method != null);
            Debug.Assert(method.MethodKind != MethodKind.LambdaMethod);

            MethodSymbol invoke = delegateType?.DelegateInvokeMethod;
            if (invoke is null)
            {
                return;
            }

            if (IsNullabilityMismatch(method.ReturnType, invoke.ReturnType, requireIdentity: false))
            {
                ReportSafetyDiagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, syntax,
                    new FormattedSymbol(method, SymbolDisplayFormat.MinimallyQualifiedFormat),
                    delegateType);
            }

            int count = Math.Min(invoke.ParameterCount, method.ParameterCount);
            for (int i = 0; i < count; i++)
            {
                var invokeParameter = invoke.Parameters[i];
                var methodParameter = method.Parameters[i];
                if (IsNullabilityMismatch(invokeParameter.Type, methodParameter.Type, requireIdentity: invokeParameter.RefKind != RefKind.None))
                {
                    ReportSafetyDiagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, syntax,
                        new FormattedSymbol(methodParameter, SymbolDisplayFormat.ShortFormat),
                        new FormattedSymbol(method, SymbolDisplayFormat.MinimallyQualifiedFormat),
                        delegateType);
                }
            }
        }

        private void ReportNullabilityMismatchWithTargetDelegate(SyntaxNode syntax, NamedTypeSymbol delegateType, UnboundLambda unboundLambda)
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
                // https://github.com/dotnet/roslyn/issues/29959 Consider relaxing and allow implicit conversions of nullability.
                // (Compare with method group conversions which pass `requireIdentity: false`.)
                if (IsNullabilityMismatch(invokeParameter.Type, unboundLambda.ParameterType(i), requireIdentity: true))
                {
                    // https://github.com/dotnet/roslyn/issues/29959 Consider using location of specific lambda parameter.
                    ReportSafetyDiagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, syntax,
                        unboundLambda.ParameterName(i),
                        unboundLambda.MessageID.Localize(),
                        delegateType);
                }
            }
        }

        private bool IsNullabilityMismatch(TypeSymbolWithAnnotations source, TypeSymbolWithAnnotations destination, bool requireIdentity)
        {
            if (!HasTopLevelNullabilityConversion(source, destination, requireIdentity))
            {
                return true;
            }
            if (requireIdentity)
            {
                return IsNullabilityMismatch(source, destination);
            }
            var sourceType = source.TypeSymbol;
            var destinationType = destination.TypeSymbol;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            return !_conversions.ClassifyImplicitConversionFromType(sourceType, destinationType, ref useSiteDiagnostics).Exists;
        }

        private bool HasTopLevelNullabilityConversion(TypeSymbolWithAnnotations source, TypeSymbolWithAnnotations destination, bool requireIdentity)
        {
            return requireIdentity ?
                _conversions.HasTopLevelNullabilityIdentityConversion(source, destination) :
                _conversions.HasTopLevelNullabilityImplicitConversion(source, destination);
        }

        /// <summary>
        /// Apply the conversion to the type of the operand and return the resulting type. (If the
        /// operand does not have an explicit type, the operand expression is used for the type.)
        /// If `checkConversion` is set, the incoming conversion is assumed to be from binding and will be
        /// re-calculated, this time considering nullability. (Note that the conversion calculation considers
        /// nested nullability only. The caller is responsible for checking the top-level nullability of
        /// the type returned by this method.) `canConvertNestedNullability` is set if the conversion
        /// considering nested nullability succeeded. `node` is used only for the location of diagnostics.
        /// </summary>
        private TypeSymbolWithAnnotations ApplyConversion(
            BoundExpression node,
            BoundExpression operandOpt,
            Conversion conversion,
            TypeSymbolWithAnnotations targetTypeWithNullability,
            TypeSymbolWithAnnotations operandType,
            bool checkConversion,
            bool fromExplicitCast,
            bool useLegacyWarnings,
            AssignmentKind assignmentKind,
            ParameterSymbol target = null,
            bool reportTopLevelWarnings = true,
            bool reportNestedWarnings = true,
            bool extensionMethodThisArgument = false)
        {
            Debug.Assert(node != null);
            Debug.Assert(operandOpt != null || !operandType.IsNull);
            Debug.Assert(!targetTypeWithNullability.IsNull);
            Debug.Assert((object)target != null || assignmentKind != AssignmentKind.Argument);

            NullableAnnotation resultAnnotation = NullableAnnotation.Unknown;
            bool forceOperandAnnotationForResult = false;
            bool canConvertNestedNullability = true;

            TypeSymbol targetType = targetTypeWithNullability.TypeSymbol;
            switch (conversion.Kind)
            {
                case ConversionKind.MethodGroup:
                    if (!fromExplicitCast)
                    {
                        ReportNullabilityMismatchWithTargetDelegate(node.Syntax, targetType.GetDelegateType(), conversion.Method);
                    }
                    resultAnnotation = NullableAnnotation.NotNullable;
                    break;

                case ConversionKind.AnonymousFunction:
                    if (operandOpt.Kind == BoundKind.Lambda)
                    {
                        var lambda = (BoundLambda)operandOpt;
                        var delegateType = targetType.GetDelegateType();
                        var methodSignatureOpt = lambda.UnboundLambda.HasExplicitlyTypedParameterList ? null : delegateType?.DelegateInvokeMethod;
                        var variableState = GetVariableState();
                        Analyze(compilation, lambda, Diagnostics, delegateInvokeMethod: delegateType?.DelegateInvokeMethod, returnTypes: null, initialState: variableState);
                        var unboundLambda = GetUnboundLambda(lambda, variableState);
                        var boundLambda = unboundLambda.Bind(delegateType);
                        if (!fromExplicitCast)
                        {
                            ReportNullabilityMismatchWithTargetDelegate(node.Syntax, delegateType, unboundLambda);
                        }
                        return TypeSymbolWithAnnotations.Create(targetType, NullableAnnotation.NotNullable);
                    }
                    break;

                case ConversionKind.InterpolatedString:
                    resultAnnotation = NullableAnnotation.NotNullable;
                    break;

                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.ImplicitUserDefined:
                    // cf. Binder.CreateUserDefinedConversion
                    {
                        if (!conversion.IsValid)
                        {
                            break;
                        }

                        // operand -> conversion "from" type
                        // May be distinct from method parameter type for Nullable<T>.
                        operandType = ApplyConversion(
                            node,
                            operandOpt,
                            conversion.UserDefinedFromConversion,
                            TypeSymbolWithAnnotations.Create(conversion.BestUserDefinedConversionAnalysis.FromType),
                            operandType,
                            checkConversion: true,
                            fromExplicitCast: false,
                            useLegacyWarnings,
                            assignmentKind,
                            target);

                        // Update method based on operandType: see https://github.com/dotnet/roslyn/issues/29605.
                        // (see NullableReferenceTypesTests.ImplicitConversions_07).
                        var methodOpt = conversion.Method;
                        Debug.Assert((object)methodOpt != null);
                        Debug.Assert(methodOpt.ParameterCount == 1);
                        var parameter = methodOpt.Parameters[0];
                        var parameterType = parameter.Type;
                        TypeSymbolWithAnnotations underlyingOperandType = default;
                        bool isLiftedConversion =
                            operandType.IsNullableType() &&
                            !parameterType.IsNullableType() &&
                            parameterType.Equals(underlyingOperandType = operandType.GetNullableUnderlyingType(), TypeCompareKind.AllIgnoreOptions);

                        // conversion "from" type -> method parameter type
                        NullableAnnotation operandAnnotation = operandType.NullableAnnotation;
                        operandType = ClassifyAndApplyConversion(operandOpt ?? node, parameterType, isLiftedConversion ? underlyingOperandType : operandType, useLegacyWarnings, AssignmentKind.Argument, target: parameter);

                        // method parameter type -> method return type
                        var methodReturnType = methodOpt.ReturnType;
                        if (isLiftedConversion)
                        {
                            operandType = TypeSymbolWithAnnotations.Create(
                                methodReturnType.IsValueType && !methodReturnType.IsNullableType() ?
                                    compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(ImmutableArray.Create(methodReturnType)) :
                                    methodReturnType.TypeSymbol,
                                methodReturnType.NullableAnnotation.IsAnyNullable() || operandAnnotation.IsAnyNullable() ?
                                    NullableAnnotation.Nullable :
                                    (methodReturnType.IsPossiblyNullableReferenceTypeTypeParameter() ? methodReturnType.NullableAnnotation : NullableAnnotation.NotNullable));
                        }
                        else
                        {
                            operandType = methodReturnType;
                        }

                        // method return type -> conversion "to" type
                        // May be distinct from method return type for Nullable<T>.
                        operandType = ClassifyAndApplyConversion(operandOpt ?? node, TypeSymbolWithAnnotations.Create(conversion.BestUserDefinedConversionAnalysis.ToType), operandType, useLegacyWarnings, assignmentKind, target);

                        // conversion "to" type -> final type
                        // https://github.com/dotnet/roslyn/issues/29959 If the original conversion was
                        // explicit, this conversion should not report nested nullability mismatches.
                        // (see NullableReferenceTypesTests.ExplicitCast_UserDefined_02).
                        operandType = ClassifyAndApplyConversion(node, targetTypeWithNullability, operandType, useLegacyWarnings, assignmentKind, target);
                        return operandType;
                    }

                case ConversionKind.ExplicitDynamic:
                case ConversionKind.ImplicitDynamic:
                    resultAnnotation = operandType.IsNull ? NullableAnnotation.Unknown : operandType.NullableAnnotation;
                    if (resultAnnotation == NullableAnnotation.NotAnnotated && targetType.IsTypeParameter())
                    {
                        resultAnnotation = NullableAnnotation.NotNullable;
                    }
                    else if (targetType.IsValueType)
                    {
                        Debug.Assert(!operandType.IsNull); // If assert fails, add a test that verifies resulting type is nullable.
                        resultAnnotation = (targetType.IsNullableType() && (operandType.IsNull || operandType.NullableAnnotation.IsAnyNullable())) ? NullableAnnotation.Nullable : NullableAnnotation.NotNullable;
                    }
                    break;

                case ConversionKind.ImplicitThrow:
                    break;

                case ConversionKind.Unboxing:
                    if (targetType.IsValueType)
                    {
                        Debug.Assert(!operandType.IsNull); // If assert fails, add a test that verifies resulting type is nullable.
                        resultAnnotation = (targetType.IsNullableTypeOrTypeParameter() && (operandType.IsNull || operandType.NullableAnnotation.IsAnyNullable())) ? NullableAnnotation.Nullable : NullableAnnotation.NotNullable;
                    }
                    else if (!operandType.IsNull && targetType.IsTypeParameter())
                    {
                        resultAnnotation = operandType.GetValueNullableAnnotation();

                        if (resultAnnotation == NullableAnnotation.NotAnnotated)
                        {
                            resultAnnotation = NullableAnnotation.NotNullable;
                        }
                    }
                    break;

                case ConversionKind.Boxing:
                    if (!operandType.IsNull)
                    {
                        if (operandType.IsValueType)
                        {
                            resultAnnotation = (operandType.IsNullableTypeOrTypeParameter() && operandType.GetValueNullableAnnotation().IsAnyNullable()) ? NullableAnnotation.Nullable : NullableAnnotation.NotNullable;
                            break;
                        }
                        else if (IsUnconstrainedTypeParameter(operandType.TypeSymbol))
                        {
                            if (operandType.IsPossiblyNullableReferenceTypeTypeParameter() && !targetTypeWithNullability.IsPossiblyNullableReferenceTypeTypeParameter())
                            {
                                resultAnnotation = NullableAnnotation.Nullable;
                                forceOperandAnnotationForResult = targetType.IsPossiblyNullableReferenceTypeTypeParameter();
                            }
                            else
                            {
                                resultAnnotation = operandType.NullableAnnotation;
                            }
                            break;
                        }
                    }
                    Debug.Assert(operandType.IsNull ||
                        !operandType.IsReferenceType ||
                        operandType.SpecialType == SpecialType.System_ValueType ||
                        operandType.TypeKind == TypeKind.Interface ||
                        operandType.TypeKind == TypeKind.Dynamic);
                    break;

                case ConversionKind.NoConversion:
                case ConversionKind.DefaultOrNullLiteral:
                    checkConversion = false;
                    goto case ConversionKind.Identity;

                case ConversionKind.Identity:
                    // If the operand is an explicit conversion, and this identity conversion
                    // is converting to the same type including nullability, skip the conversion
                    // to avoid reporting redundant warnings. Also check useLegacyWarnings
                    // since that value was used when reporting warnings for the explicit cast.
                    if (useLegacyWarnings && operandOpt?.Kind == BoundKind.Conversion)
                    {
                        var operandConversion = (BoundConversion)operandOpt;
                        var explicitType = operandConversion.ConversionGroupOpt.ExplicitType;
                        if (!explicitType.IsNull && explicitType.Equals(targetTypeWithNullability, TypeCompareKind.IgnoreInsignificantNullableModifiersDifference))
                        {
                            return operandType;
                        }
                    }
                    goto case ConversionKind.ImplicitReference;

                case ConversionKind.ImplicitReference:
                case ConversionKind.ExplicitReference:
                    if (operandType.IsNull && operandOpt.IsLiteralNullOrDefault())
                    {
                        resultAnnotation = NullableAnnotation.Nullable;
                    }
                    else
                    {
                        // Inherit state from the operand.
                        if (checkConversion)
                        {
                            // https://github.com/dotnet/roslyn/issues/29959 Assert conversion is similar to original.
                            conversion = GenerateConversion(_conversions, operandOpt, operandType.TypeSymbol, targetType, fromExplicitCast, extensionMethodThisArgument);
                            canConvertNestedNullability = conversion.Exists;
                        }

                        if (operandType.IsNull)
                        {
                            resultAnnotation = NullableAnnotation.Unknown;
                        }
                        else if (operandType.IsPossiblyNullableReferenceTypeTypeParameter())
                        {
                            if (conversion.Kind == ConversionKind.ExplicitReference)
                            {
                                resultAnnotation = NullableAnnotation.Nullable;
                            }
                            else if (!targetTypeWithNullability.IsPossiblyNullableReferenceTypeTypeParameter())
                            {
                                resultAnnotation = NullableAnnotation.Nullable;
                                forceOperandAnnotationForResult = targetType.IsPossiblyNullableReferenceTypeTypeParameter();
                            }
                            else
                            {
                                resultAnnotation = operandType.NullableAnnotation;
                            }
                        }
                        else
                        {
                            resultAnnotation = operandType.NullableAnnotation;
                            if (resultAnnotation == NullableAnnotation.NotAnnotated && targetType.IsTypeParameter())
                            {
                                resultAnnotation = NullableAnnotation.NotNullable;
                            }
                        }
                    }
                    break;

                case ConversionKind.ImplicitNullable:
                    if (checkConversion)
                    {
                        conversion = GenerateConversion(_conversions, operandOpt, operandType.TypeSymbol, targetType, fromExplicitCast, extensionMethodThisArgument);
                        canConvertNestedNullability = conversion.Exists;
                    }
                    if ((targetType.IsValueType && !targetType.IsNullableType()) ||
                        (operandType.IsValueType && !operandType.IsNullableType()))
                    {
                        resultAnnotation = NullableAnnotation.NotNullable;
                    }
                    else
                    {
                        resultAnnotation = operandType.NullableAnnotation.IsAnyNullable() ? NullableAnnotation.Nullable : NullableAnnotation.NotNullable;
                    }
                    break;

                case ConversionKind.ExplicitNullable:
                    if (operandType.TypeSymbol?.IsNullableType() == true &&
                        !targetType.IsNullableType())
                    {
                        // Explicit conversion of Nullable<T> to T is equivalent to Nullable<T>.Value.
                        if (reportTopLevelWarnings && operandType.NullableAnnotation.IsAnyNullable())
                        {
                            ReportSafetyDiagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, node.Syntax);
                        }
                        // Mark the value as not nullable, regardless of whether it was known to be nullable,
                        // because the implied call to `.Value` will only succeed if not null.
                        if (operandOpt != null)
                        {
                            int slot = MakeSlot(operandOpt);
                            if (slot > 0)
                            {
                                this.State[slot] = NullableAnnotation.NotNullable;
                            }
                        }
                    }
                    goto case ConversionKind.ImplicitNullable;

                case ConversionKind.ImplicitTupleLiteral:
                case ConversionKind.ImplicitTuple:
                case ConversionKind.ExplicitTupleLiteral:
                case ConversionKind.ExplicitTuple:
                    if (checkConversion)
                    {
                        // https://github.com/dotnet/roslyn/issues/29699: Report warnings for user-defined conversions on tuple elements.
                        conversion = GenerateConversion(_conversions, operandOpt, operandType.TypeSymbol, targetType, fromExplicitCast, extensionMethodThisArgument);
                        canConvertNestedNullability = conversion.Exists;
                    }
                    resultAnnotation = NullableAnnotation.NotNullable;
                    break;

                case ConversionKind.Deconstruction:
                    // Can reach here, with an error type, when the
                    // Deconstruct method is missing or inaccessible.
                    break;

                case ConversionKind.ExplicitEnumeration:
                    // Can reach here, with an error type.
                    break;

                default:
                    Debug.Assert(targetType.IsValueType);
                    break;
            }

            var resultType = TypeSymbolWithAnnotations.Create(targetType, resultAnnotation);

            if (operandType.TypeSymbol?.IsErrorType() != true && !targetType.IsErrorType())
            {
                // Need to report all warnings that apply since the warnings can be suppressed individually.
                if (reportTopLevelWarnings)
                {
                    ReportNullableAssignmentIfNecessary(node, targetTypeWithNullability, resultType, useLegacyWarnings: useLegacyWarnings, assignmentKind, target);
                }
                if (reportNestedWarnings && !canConvertNestedNullability)
                {
                    if (assignmentKind == AssignmentKind.Argument)
                    {
                        ReportNullabilityMismatchInArgument(node, operandType.TypeSymbol, target, targetType);
                    }
                    else
                    {
                        ReportSafetyDiagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, node.Syntax, GetTypeAsDiagnosticArgument(operandType.TypeSymbol), targetType);
                    }
                }
            }

            if (forceOperandAnnotationForResult)
            {
                resultType = TypeSymbolWithAnnotations.Create(targetType, operandType.NullableAnnotation);
            }

            return resultType;
        }

        private TypeSymbolWithAnnotations ClassifyAndApplyConversion(BoundExpression node, TypeSymbolWithAnnotations targetType, TypeSymbolWithAnnotations operandType, bool useLegacyWarnings, AssignmentKind assignmentKind, ParameterSymbol target)
        {
            Debug.Assert((object)target != null || assignmentKind != AssignmentKind.Argument);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var conversion = _conversions.ClassifyStandardConversion(null, operandType.TypeSymbol, targetType.TypeSymbol, ref useSiteDiagnostics);
            if (!conversion.Exists)
            {
                if (assignmentKind == AssignmentKind.Argument)
                {
                    ReportNullabilityMismatchInArgument(node, operandType.TypeSymbol, target, targetType.TypeSymbol);
                }
                else
                {
                    ReportSafetyDiagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, node.Syntax, operandType.TypeSymbol, targetType.TypeSymbol);
                }
            }
            return ApplyConversion(
                node,
                operandOpt: null,
                conversion,
                targetType,
                operandType,
                checkConversion: false,
                fromExplicitCast: false,
                useLegacyWarnings: useLegacyWarnings,
                assignmentKind,
                target);
        }

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            if (node.MethodOpt?.MethodKind == MethodKind.LocalFunction)
            {
                var syntax = node.Syntax;
                var localFunc = (LocalFunctionSymbol)node.MethodOpt.OriginalDefinition;
                ReplayReadsAndWrites(localFunc, syntax, writes: false);
            }

            base.VisitDelegateCreationExpression(node);
            _resultType = TypeSymbolWithAnnotations.Create(node.Type, NullableAnnotation.NotNullable);
            return null;
        }

        public override BoundNode VisitMethodGroup(BoundMethodGroup node)
        {
            Debug.Assert(!IsConditionalState);

            BoundExpression receiverOpt = node.ReceiverOpt;
            if (receiverOpt != null)
            {
                VisitRvalue(receiverOpt);
                // https://github.com/dotnet/roslyn/issues/30563: Should not check receiver here.
                // That check should be handled when applying the method group conversion,
                // when we have a specific method, to avoid reporting null receiver warnings
                // for extension method delegates.
                CheckPossibleNullReceiver(receiverOpt);
            }

            //if (this.State.Reachable) // Consider reachability: see https://github.com/dotnet/roslyn/issues/28798
            {
                _resultType = default;
            }

            return null;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            SetResult(node);
            return null;
        }

        public override BoundNode VisitUnboundLambda(UnboundLambda node)
        {
            // The presence of this node suggests an error was detected in an earlier phase.
            // Analyze the body to report any additional warnings.
            var lambda = node.BindForErrorRecovery();
            Analyze(compilation, lambda, Diagnostics, delegateInvokeMethod: null, returnTypes: null, initialState: GetVariableState());
            SetResult(node);
            return null;
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            var body = node.Body;
            if (body != null)
            {
                Analyze(
                    compilation,
                    node.Symbol,
                    body,
                    Diagnostics,
                    useMethodSignatureReturnType: false,
                    useMethodSignatureParameterTypes: false,
                    methodSignatureOpt: null,
                    returnTypes: null,
                    initialState: GetVariableState(),
                    callbackOpt: _callbackOpt);
            }
            _resultType = _invalidType;
            return null;
        }

        public override BoundNode VisitThisReference(BoundThisReference node)
        {
            VisitThisOrBaseReference(node);
            return null;
        }

        private void VisitThisOrBaseReference(BoundExpression node)
        {
            _resultType = TypeSymbolWithAnnotations.Create(node.Type, NullableAnnotation.NotNullable);
        }

        public override BoundNode VisitParameter(BoundParameter node)
        {
            var parameter = node.ParameterSymbol;
            int slot = GetOrCreateSlot(parameter);
            var type = GetDeclaredParameterResult(parameter);
            _resultType = GetAdjustedResult(type, slot);
            return null;
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            Debug.Assert(!IsConditionalState);

            var left = node.Left;
            var right = node.Right;
            VisitLvalue(left);
            TypeSymbolWithAnnotations leftType = _resultType;

            if (left.Kind == BoundKind.EventAccess && ((BoundEventAccess)left).EventSymbol.IsWindowsRuntimeEvent)
            {
                // Event assignment is a call to an Add method. (Note that assignment
                // of non-field-like events uses BoundEventAssignmentOperator
                // rather than BoundAssignmentOperator.)
                VisitRvalue(right);
                SetResult(node);
            }
            else
            {
                TypeSymbolWithAnnotations rightType = VisitOptionalImplicitConversion(right, leftType, UseLegacyWarnings(left), AssignmentKind.Assignment);
                TrackNullableStateForAssignment(right, leftType, MakeSlot(left), rightType, MakeSlot(right));
                // https://github.com/dotnet/roslyn/issues/30066 Check node.Type.IsErrorType() instead?
                _resultType = node.HasErrors ? TypeSymbolWithAnnotations.Create(node.Type) : rightType;
            }

            return null;
        }

        private static bool UseLegacyWarnings(BoundExpression expr)
        {
            switch (expr.Kind)
            {
                case BoundKind.Local:
                    return true;
                case BoundKind.Parameter:
                    RefKind kind = ((BoundParameter)expr).ParameterSymbol.RefKind;
                    return kind == RefKind.None;
                default:
                    return false;
            }
        }

        public override BoundNode VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            // https://github.com/dotnet/roslyn/issues/29618: Assign each of the deconstructed values,
            // and handle deconstruction conversion for node.Right.
            VisitLvalue(node.Left);
            VisitRvalue(node.Right.Operand);
            SetResult(node);
            return null;
        }

        public override BoundNode VisitIncrementOperator(BoundIncrementOperator node)
        {
            Debug.Assert(!IsConditionalState);

            VisitRvalue(node.Operand);
            var operandType = _resultType;
            bool setResult = false;

            if (this.State.Reachable)
            {
                // https://github.com/dotnet/roslyn/issues/29961 Update increment method based on operand type.
                MethodSymbol incrementOperator = (node.OperatorKind.IsUserDefined() && (object)node.MethodOpt != null && node.MethodOpt.ParameterCount == 1) ? node.MethodOpt : null;
                TypeSymbolWithAnnotations targetTypeOfOperandConversion;
                AssignmentKind assignmentKind = AssignmentKind.Assignment;
                ParameterSymbol target = null;

                // https://github.com/dotnet/roslyn/issues/29961 Update conversion method based on operand type.
                if (node.OperandConversion.IsUserDefined && (object)node.OperandConversion.Method != null && node.OperandConversion.Method.ParameterCount == 1)
                {
                    targetTypeOfOperandConversion = node.OperandConversion.Method.ReturnType;
                }
                else if ((object)incrementOperator != null)
                {
                    targetTypeOfOperandConversion = incrementOperator.Parameters[0].Type;
                    assignmentKind = AssignmentKind.Argument;
                    target = incrementOperator.Parameters[0];
                }
                else
                {
                    // Either a built-in increment, or an error case.
                    targetTypeOfOperandConversion = default;
                }

                TypeSymbolWithAnnotations resultOfOperandConversionType;

                if (!targetTypeOfOperandConversion.IsNull)
                {
                    // https://github.com/dotnet/roslyn/issues/29961 Should something special be done for targetTypeOfOperandConversion for lifted case?
                    resultOfOperandConversionType = ApplyConversion(
                        node.Operand,
                        node.Operand,
                        node.OperandConversion,
                        targetTypeOfOperandConversion,
                        operandType,
                        checkConversion: true,
                        fromExplicitCast: false,
                        useLegacyWarnings: false,
                        assignmentKind,
                        target,
                        reportTopLevelWarnings: true,
                        reportNestedWarnings: true);
                }
                else
                {
                    resultOfOperandConversionType = operandType;
                }

                TypeSymbolWithAnnotations resultOfIncrementType;
                if ((object)incrementOperator == null)
                {
                    resultOfIncrementType = resultOfOperandConversionType;
                }
                else
                {
                    resultOfIncrementType = incrementOperator.ReturnType;
                }

                resultOfIncrementType = ApplyConversion(
                    node,
                    node,
                    node.ResultConversion,
                    operandType,
                    resultOfIncrementType,
                    checkConversion: true,
                    fromExplicitCast: false,
                    useLegacyWarnings: false,
                    AssignmentKind.Assignment);

                // https://github.com/dotnet/roslyn/issues/29961 Check node.Type.IsErrorType() instead?
                if (!node.HasErrors)
                {
                    var op = node.OperatorKind.Operator();
                    _resultType = (op == UnaryOperatorKind.PrefixIncrement || op == UnaryOperatorKind.PrefixDecrement) ? resultOfIncrementType : operandType;
                    setResult = true;

                    TrackNullableStateForAssignment(node, operandType, MakeSlot(node.Operand), valueType: resultOfIncrementType);
                }
            }

            if (!setResult)
            {
                this.SetResult(node);
            }

            return null;
        }

        public override BoundNode VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            VisitLvalue(node.Left); // https://github.com/dotnet/roslyn/issues/29962 Method should be called VisitValue rather than VisitLvalue.
            TypeSymbolWithAnnotations leftType = _resultType;

            TypeSymbolWithAnnotations resultType;
            Debug.Assert(!IsConditionalState);

            //if (this.State.Reachable) // Consider reachability: see https://github.com/dotnet/roslyn/issues/28798
            {
                TypeSymbolWithAnnotations leftOnRightType = GetAdjustedResult(leftType, MakeSlot(node.Left));

                // https://github.com/dotnet/roslyn/issues/29962 Update operator based on inferred argument types.
                if ((object)node.Operator.LeftType != null)
                {
                    // https://github.com/dotnet/roslyn/issues/29962 Ignoring top-level nullability of operator left parameter.
                    leftOnRightType = ApplyConversion(
                        node.Left,
                        node.Left,
                        node.LeftConversion,
                        TypeSymbolWithAnnotations.Create(node.Operator.LeftType),
                        leftOnRightType,
                        checkConversion: true,
                        fromExplicitCast: false,
                        useLegacyWarnings: false,
                        AssignmentKind.Assignment,
                        reportTopLevelWarnings: false,
                        reportNestedWarnings: false);
                }
                else
                {
                    leftOnRightType = default;
                }

                VisitRvalue(node.Right);
                TypeSymbolWithAnnotations rightType = _resultType;

                if ((object)node.Operator.ReturnType != null)
                {
                    if (node.Operator.Kind.IsUserDefined() && (object)node.Operator.Method != null && node.Operator.Method.ParameterCount == 2)
                    {
                        ReportArgumentWarnings(node.Left, leftOnRightType, node.Operator.Method.Parameters[0]);
                        ReportArgumentWarnings(node.Right, rightType, node.Operator.Method.Parameters[1]);
                    }

                    resultType = InferResultNullability(node.Operator.Kind, node.Operator.Method, node.Operator.ReturnType, leftOnRightType, rightType);
                    resultType = ApplyConversion(
                        node,
                        node,
                        node.FinalConversion,
                        leftType,
                        resultType,
                        checkConversion: true,
                        fromExplicitCast: false,
                        useLegacyWarnings: false,
                        AssignmentKind.Assignment);
                }
                else
                {
                    resultType = TypeSymbolWithAnnotations.Create(node.Type);
                }

                TrackNullableStateForAssignment(node, leftType, MakeSlot(node.Left), resultType);
                _resultType = resultType;
            }
            //else
            //{   // https://github.com/dotnet/roslyn/issues/29962 code should be restored?
            //    VisitRvalue(node.Right);
            //    AfterRightHasBeenVisited(node);
            //    resultType = null;
            //}

            return null;
        }

        public override BoundNode VisitFixedLocalCollectionInitializer(BoundFixedLocalCollectionInitializer node)
        {
            var initializer = node.Expression;
            if (initializer.Kind == BoundKind.AddressOfOperator)
            {
                initializer = ((BoundAddressOfOperator)initializer).Operand;
            }

            this.VisitRvalue(initializer);
            SetResult(node);
            return null;
        }

        public override BoundNode VisitAddressOfOperator(BoundAddressOfOperator node)
        {
            SetResult(node);
            return null;
        }

        private void ReportArgumentWarnings(BoundExpression argument, TypeSymbolWithAnnotations argumentType, ParameterSymbol parameter)
        {
            var paramType = parameter.Type;
            ReportNullableAssignmentIfNecessary(argument, paramType, argumentType, useLegacyWarnings: false, assignmentKind: AssignmentKind.Argument, target: parameter);

            if (!argumentType.IsNull && IsNullabilityMismatch(paramType.TypeSymbol, argumentType.TypeSymbol))
            {
                ReportNullabilityMismatchInArgument(argument, argumentType.TypeSymbol, parameter, paramType.TypeSymbol);
            }
        }

        /// <summary>
        /// Report warning passing argument where nested nullability does not match
        /// parameter (e.g.: calling `void F(object[] o)` with `F(new[] { maybeNull })`).
        /// </summary>
        private void ReportNullabilityMismatchInArgument(BoundExpression argument, TypeSymbol argumentType, ParameterSymbol parameter, TypeSymbol parameterType)
        {
            ReportSafetyDiagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, argument.Syntax, argumentType, parameterType,
                new FormattedSymbol(parameter, SymbolDisplayFormat.ShortFormat),
                new FormattedSymbol(parameter.ContainingSymbol, SymbolDisplayFormat.MinimallyQualifiedFormat));
        }

        private TypeSymbolWithAnnotations GetDeclaredLocalResult(LocalSymbol local)
        {
            return _variableTypes.TryGetValue(local, out TypeSymbolWithAnnotations type) ?
                type :
                local.Type;
        }

        private TypeSymbolWithAnnotations GetDeclaredParameterResult(ParameterSymbol parameter)
        {
            return _variableTypes.TryGetValue(parameter, out TypeSymbolWithAnnotations type) ?
                type :
                parameter.Type;
        }

        public override BoundNode VisitBaseReference(BoundBaseReference node)
        {
            VisitThisOrBaseReference(node);
            return null;
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            VisitMemberAccess(node, node.ReceiverOpt, node.FieldSymbol, asLvalue: false);
            return null;
        }

        public override BoundNode VisitPropertyAccess(BoundPropertyAccess node)
        {
            VisitMemberAccess(node, node.ReceiverOpt, node.PropertySymbol, asLvalue: false);
            return null;
        }

        public override BoundNode VisitIndexerAccess(BoundIndexerAccess node)
        {
            var receiverOpt = node.ReceiverOpt;
            VisitRvalue(receiverOpt);
            // https://github.com/dotnet/roslyn/issues/30598: Mark receiver as not null
            // after indices have been visited, and only if the receiver has not changed.
            CheckPossibleNullReceiver(receiverOpt);

            // https://github.com/dotnet/roslyn/issues/29964 Update indexer based on inferred receiver type.
            VisitArguments(node, node.Arguments, node.ArgumentRefKindsOpt, node.Indexer, node.ArgsToParamsOpt, node.Expanded);

            // https://github.com/dotnet/roslyn/issues/30620 remove before shipping dev16
            if (node.Arguments.Length == 1 &&
                TypeSymbol.Equals(node.Arguments[0].Type, compilation.GetWellKnownType(WellKnownType.System_Range), TypeCompareKind.ConsiderEverything2))
            {
                _resultType = TypeSymbolWithAnnotations.Create(node.Type);
            }
            else
            {
                _resultType = node.Indexer.Type;
            }
            return null;
        }

        public override BoundNode VisitEventAccess(BoundEventAccess node)
        {
            VisitMemberAccess(node, node.ReceiverOpt, node.EventSymbol, asLvalue: false);
            return null;
        }

        private void VisitMemberAccess(BoundExpression node, BoundExpression receiverOpt, Symbol member, bool asLvalue)
        {
            Debug.Assert(!IsConditionalState);

            //if (this.State.Reachable) // Consider reachability: see https://github.com/dotnet/roslyn/issues/28798
            {
                VisitRvalue(receiverOpt);

                SpecialMember? nullableOfTMember = null;
                if (!member.IsStatic)
                {
                    member = AsMemberOfResultType(_resultType, member);
                    nullableOfTMember = GetNullableOfTMember(compilation, member);
                    // https://github.com/dotnet/roslyn/issues/30598: For l-values, mark receiver as not null
                    // after RHS has been visited, and only if the receiver has not changed.
                    bool allowValueType = nullableOfTMember == SpecialMember.System_Nullable_T_get_Value;
                    CheckPossibleNullReceiver(receiverOpt, allowValueType, (allowValueType ? node : (receiverOpt ?? node)).Syntax);
                }

                var resultType = member.GetTypeOrReturnType();

                if (!asLvalue)
                {
                    // We are supposed to track information for the node. Use whatever we managed to
                    // accumulate so far.
                    if (!resultType.IsValueType || resultType.IsNullableType())
                    {
                        int containingSlot = getReceiverSlot();
                        int slot = (containingSlot < 0) ? -1 : GetOrCreateSlot(member, containingSlot);
                        if (slot > 0 && slot < this.State.Capacity)
                        {
                            var annotation = this.State[slot];
                            if (annotation != resultType.NullableAnnotation)
                            {
                                resultType = TypeSymbolWithAnnotations.Create(resultType.TypeSymbol, annotation);
                            }
                        }
                    }

                    Debug.Assert(!IsConditionalState);
                    if (nullableOfTMember == SpecialMember.System_Nullable_T_get_HasValue)
                    {
                        int containingSlot = getReceiverSlot();
                        if (containingSlot > 0)
                        {
                            // https://github.com/dotnet/roslyn/issues/31516: Report HDN_NullCheckIsProbablyAlwaysTrue/False
                            // when HasValue check is unnecessary.
                            Split();
                            this.StateWhenTrue[containingSlot] = NullableAnnotation.NotNullable;
                        }
                    }
                }

                _resultType = resultType;
            }

            int getReceiverSlot() => (receiverOpt is null) ? -1 : MakeSlot(receiverOpt);
        }

        private static SpecialMember? GetNullableOfTMember(CSharpCompilation compilation, Symbol member)
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

        private int GetNullableOfTValueSlot(TypeSymbol containingType, int containingSlot)
        {
            Debug.Assert(containingType.IsNullableType());
            Debug.Assert(TypeSymbol.Equals(GetSlotType(containingSlot), containingType, TypeCompareKind.ConsiderEverything2));

            var getValue = (MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_Nullable_T_get_Value);
            var member = getValue?.AsMember((NamedTypeSymbol)containingType)?.AssociatedSymbol;
            return (member is null) ? -1 : GetOrCreateSlot(member, containingSlot);
        }

        protected override void VisitForEachExpression(BoundForEachStatement node)
        {
            var expr = node.Expression;
            VisitRvalue(expr);
            CheckPossibleNullReceiver(expr);
        }

        public override void VisitForEachIterationVariables(BoundForEachStatement node)
        {
            // declare and assign all iteration variables
            foreach (var iterationVariable in node.IterationVariables)
            {
                TypeSymbolWithAnnotations sourceType = node.EnumeratorInfoOpt?.ElementType ?? default;
                NullableAnnotation annotation = NullableAnnotation.Unknown;
                if (!sourceType.IsNull)
                {
                    TypeSymbolWithAnnotations destinationType = iterationVariable.Type;
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    Conversion conversion = _conversions.ClassifyImplicitConversionFromType(sourceType.TypeSymbol, destinationType.TypeSymbol, ref useSiteDiagnostics);
                    TypeSymbolWithAnnotations result = ApplyConversion(
                        node.IterationVariableType,
                        operandOpt: null,
                        conversion,
                        destinationType,
                        sourceType,
                        checkConversion: false,
                        fromExplicitCast: true,
                        useLegacyWarnings: false,
                        AssignmentKind.Assignment,
                        reportTopLevelWarnings: false,
                        reportNestedWarnings: false);
                    if (destinationType.IsReferenceType && destinationType.NullableAnnotation.IsAnyNotNullable() && sourceType.NullableAnnotation.IsAnyNullable())
                    {
                        ReportNonSafetyDiagnostic(node.IterationVariableType.Syntax);
                    }
                    annotation = result.NullableAnnotation;
                }

                int slot = GetOrCreateSlot(iterationVariable);
                if (slot > 0)
                {
                    this.State[slot] = annotation;
                }
            }
        }

        public override BoundNode VisitFromEndIndexExpression(BoundFromEndIndexExpression node)
        {
            var result = base.VisitFromEndIndexExpression(node);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitObjectInitializerMember(BoundObjectInitializerMember node)
        {
            // Should be handled by VisitObjectCreationExpression.
            throw ExceptionUtilities.Unreachable;
        }

        public override BoundNode VisitDynamicObjectInitializerMember(BoundDynamicObjectInitializerMember node)
        {
            SetResult(node);
            return null;
        }

        public override BoundNode VisitBadExpression(BoundBadExpression node)
        {
            var result = base.VisitBadExpression(node);
            _resultType = TypeSymbolWithAnnotations.Create(node.Type);
            return result;
        }

        public override BoundNode VisitTypeExpression(BoundTypeExpression node)
        {
            var result = base.VisitTypeExpression(node);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitTypeOrValueExpression(BoundTypeOrValueExpression node)
        {
            var result = base.VisitTypeOrValueExpression(node);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitUnaryOperator(BoundUnaryOperator node)
        {
            Debug.Assert(!IsConditionalState);

            var result = base.VisitUnaryOperator(node);
            TypeSymbolWithAnnotations resultType = default;

            // Update method based on inferred operand type: see https://github.com/dotnet/roslyn/issues/29605.
            if (node.OperatorKind.IsUserDefined())
            {
                if (node.OperatorKind.IsLifted())
                {
                    // https://github.com/dotnet/roslyn/issues/29953 Conversions: Lifted operator
                }
                else if ((object)node.MethodOpt != null && node.MethodOpt.ParameterCount == 1)
                {
                    ReportArgumentWarnings(node.Operand, _resultType, node.MethodOpt.Parameters[0]);
                    resultType = node.MethodOpt.ReturnType;
                }
            }

            _resultType = resultType.IsNull ? TypeSymbolWithAnnotations.Create(node.Type) : resultType;
            return null;
        }

        public override BoundNode VisitPointerIndirectionOperator(BoundPointerIndirectionOperator node)
        {
            var result = base.VisitPointerIndirectionOperator(node);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitPointerElementAccess(BoundPointerElementAccess node)
        {
            var result = base.VisitPointerElementAccess(node);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitRefTypeOperator(BoundRefTypeOperator node)
        {
            VisitRvalue(node.Operand);
            SetResult(node);
            return null;
        }

        public override BoundNode VisitMakeRefOperator(BoundMakeRefOperator node)
        {
            var result = base.VisitMakeRefOperator(node);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitRefValueOperator(BoundRefValueOperator node)
        {
            var result = base.VisitRefValueOperator(node);
            SetResult(node);
            return result;
        }

        private TypeSymbolWithAnnotations InferResultNullability(BoundUserDefinedConditionalLogicalOperator node)
        {
            if (node.OperatorKind.IsLifted())
            {
                // https://github.com/dotnet/roslyn/issues/29953 Conversions: Lifted operator
                return TypeSymbolWithAnnotations.Create(node.Type);
            }
            // Update method based on inferred operand types: see https://github.com/dotnet/roslyn/issues/29605.
            if ((object)node.LogicalOperator != null && node.LogicalOperator.ParameterCount == 2)
            {
                return node.LogicalOperator.ReturnType;
            }
            else
            {
                return default;
            }
        }

        protected override void AfterLeftChildOfBinaryLogicalOperatorHasBeenVisited(BoundExpression node, BoundExpression right, bool isAnd, bool isBool, ref LocalState leftTrue, ref LocalState leftFalse)
        {
            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable) // Consider reachability: see https://github.com/dotnet/roslyn/issues/28798
            {
                TypeSymbolWithAnnotations leftType = _resultType;
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

                Debug.Assert((object)trueFalseOperator == null || ((object)logicalOperator != null && left != null));

                if ((object)trueFalseOperator != null)
                {
                    ReportArgumentWarnings(left, leftType, trueFalseOperator.Parameters[0]);
                }

                if ((object)logicalOperator != null)
                {
                    ReportArgumentWarnings(left, leftType, logicalOperator.Parameters[0]);
                }

                Visit(right);
                TypeSymbolWithAnnotations rightType = _resultType;

                _resultType = InferResultNullabilityOfBinaryLogicalOperator(node, leftType, rightType);

                if ((object)logicalOperator != null)
                {
                    ReportArgumentWarnings(right, rightType, logicalOperator.Parameters[1]);
                }
            }

            AfterRightChildOfBinaryLogicalOperatorHasBeenVisited(node, right, isAnd, isBool, ref leftTrue, ref leftFalse);
        }

        private TypeSymbolWithAnnotations InferResultNullabilityOfBinaryLogicalOperator(BoundExpression node, TypeSymbolWithAnnotations leftType, TypeSymbolWithAnnotations rightType)
        {
            switch (node.Kind)
            {
                case BoundKind.BinaryOperator:
                    return InferResultNullability((BoundBinaryOperator)node, leftType, rightType);
                case BoundKind.UserDefinedConditionalLogicalOperator:
                    return InferResultNullability((BoundUserDefinedConditionalLogicalOperator)node);
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind);
            }
        }

        public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
        {
            var result = base.VisitAwaitExpression(node);
            if (node.Type.IsValueType || node.HasErrors || (object)node.AwaitableInfo.GetResult == null)
            {
                SetResult(node);
            }
            else
            {
                // Update method based on inferred receiver type: see https://github.com/dotnet/roslyn/issues/29605.
                _resultType = node.AwaitableInfo.GetResult.ReturnType;
            }
            return result;
        }

        public override BoundNode VisitTypeOfOperator(BoundTypeOfOperator node)
        {
            var result = base.VisitTypeOfOperator(node);
            _resultType = TypeSymbolWithAnnotations.Create(node.Type, NullableAnnotation.NotNullable);
            return result;
        }

        public override BoundNode VisitMethodInfo(BoundMethodInfo node)
        {
            var result = base.VisitMethodInfo(node);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitFieldInfo(BoundFieldInfo node)
        {
            var result = base.VisitFieldInfo(node);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitDefaultExpression(BoundDefaultExpression node)
        {
            var result = base.VisitDefaultExpression(node);
            TypeSymbol type = node.Type;
            _resultType = TypeSymbolWithAnnotations.Create(type, (type is null || type.IsNullableType() || !type.IsValueType) ? NullableAnnotation.Nullable : NullableAnnotation.Unknown);
            return result;
        }

        public override BoundNode VisitIsOperator(BoundIsOperator node)
        {
            Debug.Assert(!this.IsConditionalState);

            var operand = node.Operand;
            var result = base.VisitIsOperator(node);
            Debug.Assert(node.Type.SpecialType == SpecialType.System_Boolean);

            if (operand.Type?.IsValueType == false)
            {
                var slotBuilder = ArrayBuilder<int>.GetInstance();
                GetSlotsToMarkAsNotNullable(operand, slotBuilder);
                if (slotBuilder.Count > 0)
                {
                    Split();
                    MarkSlotsAsNotNullable(slotBuilder, ref StateWhenTrue);
                }
                slotBuilder.Free();
            }

            SetResult(node);
            return result;
        }

        public override BoundNode VisitAsOperator(BoundAsOperator node)
        {
            VisitRvalue(node.Operand);

            //if (this.State.Reachable) // Consider reachability: see https://github.com/dotnet/roslyn/issues/28798
            {
                NullableAnnotation nullableAnnotation = NullableAnnotation.Unknown;
                var type = node.Type;

                if (!type.IsValueType || type.IsNullableType())
                {
                    var operandType = _resultType;
                    switch (node.Conversion.Kind)
                    {
                        case ConversionKind.Identity:
                            // Inherit nullability from the operand
                            nullableAnnotation = operandType.NullableAnnotation;
                            break;

                        case ConversionKind.ImplicitReference:
                            // Inherit nullability from the operand
                            if (!operandType.IsNull && operandType.IsPossiblyNullableReferenceTypeTypeParameter())
                            {
                                if (!type.IsPossiblyNullableReferenceTypeTypeParameter())
                                {
                                    nullableAnnotation = NullableAnnotation.Nullable;
                                }
                                else
                                {
                                    nullableAnnotation = NullableAnnotation.NotAnnotated;
                                }
                            }
                            else
                            {
                                nullableAnnotation = operandType.NullableAnnotation;
                                if (nullableAnnotation == NullableAnnotation.NotAnnotated && type.IsTypeParameter())
                                {
                                    nullableAnnotation = NullableAnnotation.NotNullable;
                                }
                            }
                            break;

                        case ConversionKind.Boxing:
                            if (operandType.TypeSymbol?.IsValueType == true)
                            {
                                nullableAnnotation = (operandType.TypeSymbol.IsNullableType() && operandType.NullableAnnotation.IsAnyNullable()) ? NullableAnnotation.Nullable : NullableAnnotation.NotNullable;
                            }
                            else
                            {
                                Debug.Assert(operandType.TypeSymbol?.IsReferenceType != true);

                                if (!operandType.IsNull)
                                {
                                    if (operandType.IsPossiblyNullableReferenceTypeTypeParameter() && type.IsPossiblyNullableReferenceTypeTypeParameter())
                                    {
                                        nullableAnnotation = NullableAnnotation.NotAnnotated;
                                    }
                                    else
                                    {
                                        nullableAnnotation = operandType.GetValueNullableAnnotation();
                                    }
                                }
                                else
                                {
                                    nullableAnnotation = NullableAnnotation.Nullable;
                                }
                            }
                            break;

                        case ConversionKind.ImplicitNullable:
                            nullableAnnotation = (operandType.IsNullableType() && operandType.NullableAnnotation.IsAnyNullable()) ? NullableAnnotation.Nullable : NullableAnnotation.NotNullable;
                            break;

                        default:
                            nullableAnnotation = NullableAnnotation.Nullable;
                            break;
                    }
                }

                _resultType = TypeSymbolWithAnnotations.Create(type, nullableAnnotation);
            }

            return null;
        }

        public override BoundNode VisitSuppressNullableWarningExpression(BoundSuppressNullableWarningExpression node)
        {
            base.VisitSuppressNullableWarningExpression(node);

            //if (this.State.Reachable) // Consider reachability: see https://github.com/dotnet/roslyn/issues/28798
            {
                _resultType = _resultType.IsNull ? default : _resultType.WithTopLevelNonNullability();
            }

            return null;
        }

        public override BoundNode VisitSizeOfOperator(BoundSizeOfOperator node)
        {
            var result = base.VisitSizeOfOperator(node);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitArgList(BoundArgList node)
        {
            var result = base.VisitArgList(node);
            Debug.Assert(node.Type.SpecialType == SpecialType.System_RuntimeArgumentHandle);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitArgListOperator(BoundArgListOperator node)
        {
            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt);
            Debug.Assert((object)node.Type == null);
            SetResult(node);
            return null;
        }

        public override BoundNode VisitLiteral(BoundLiteral node)
        {
            var result = base.VisitLiteral(node);

            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable) // Consider reachability: see https://github.com/dotnet/roslyn/issues/28798
            {
                var constant = node.ConstantValue;

                if (constant != null &&
                    ((object)node.Type != null ? node.Type.IsReferenceType : constant.IsNull))
                {
                    _resultType = TypeSymbolWithAnnotations.Create(node.Type, constant.IsNull ? NullableAnnotation.Nullable : NullableAnnotation.NotNullable);
                }
                else
                {
                    SetResult(node);
                }
            }

            return result;
        }

        public override BoundNode VisitPreviousSubmissionReference(BoundPreviousSubmissionReference node)
        {
            var result = base.VisitPreviousSubmissionReference(node);
            Debug.Assert(node.WasCompilerGenerated);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitHostObjectMemberReference(BoundHostObjectMemberReference node)
        {
            var result = base.VisitHostObjectMemberReference(node);
            Debug.Assert(node.WasCompilerGenerated);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitPseudoVariable(BoundPseudoVariable node)
        {
            var result = base.VisitPseudoVariable(node);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitRangeExpression(BoundRangeExpression node)
        {
            var result = base.VisitRangeExpression(node);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitRangeVariable(BoundRangeVariable node)
        {
            var result = base.VisitRangeVariable(node);
            SetResult(node); // https://github.com/dotnet/roslyn/issues/29863 Need to review this
            return result;
        }

        public override BoundNode VisitLabel(BoundLabel node)
        {
            var result = base.VisitLabel(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitDynamicMemberAccess(BoundDynamicMemberAccess node)
        {
            var receiver = node.Receiver;
            VisitRvalue(receiver);
            CheckPossibleNullReceiver(receiver);

            Debug.Assert(node.Type.IsDynamic());
            _resultType = TypeSymbolWithAnnotations.Create(node.Type);
            return null;
        }

        public override BoundNode VisitDynamicInvocation(BoundDynamicInvocation node)
        {
            VisitRvalue(node.Expression);
            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt);

            Debug.Assert(node.Type.IsDynamic());
            Debug.Assert(node.Type.IsReferenceType);

            // https://github.com/dotnet/roslyn/issues/29893 Update applicable members based on inferred argument types.
            NullableAnnotation nullableAnnotation = InferResultNullabilityFromApplicableCandidates(StaticCast<Symbol>.From(node.ApplicableMethods));
            _resultType = TypeSymbolWithAnnotations.Create(node.Type, nullableAnnotation);
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
                @event = (EventSymbol)AsMemberOfResultType(_resultType, @event);
                // https://github.com/dotnet/roslyn/issues/30598: Mark receiver as not null
                // after arguments have been visited, and only if the receiver has not changed.
                CheckPossibleNullReceiver(receiverOpt);
            }
            VisitRvalue(node.Argument);
            // https://github.com/dotnet/roslyn/issues/31018: Check for delegate mismatch.
            SetResult(node); // https://github.com/dotnet/roslyn/issues/29969 Review whether this is the correct result
            return null;
        }

        public override BoundNode VisitDynamicObjectCreationExpression(BoundDynamicObjectCreationExpression node)
        {
            Debug.Assert(!IsConditionalState);
            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt);
            VisitObjectOrDynamicObjectCreation(node, node.InitializerExpressionOpt);
            return null;
        }

        public override BoundNode VisitObjectInitializerExpression(BoundObjectInitializerExpression node)
        {
            // Only reachable from bad expression. Otherwise handled in VisitObjectCreationExpression().
            SetResult(node);
            return null;
        }

        public override BoundNode VisitCollectionInitializerExpression(BoundCollectionInitializerExpression node)
        {
            // Only reachable from bad expression. Otherwise handled in VisitObjectCreationExpression().
            SetResult(node);
            return null;
        }

        public override BoundNode VisitDynamicCollectionElementInitializer(BoundDynamicCollectionElementInitializer node)
        {
            // Only reachable from bad expression. Otherwise handled in VisitObjectCreationExpression().
            SetResult(node);
            return null;
        }

        public override BoundNode VisitImplicitReceiver(BoundImplicitReceiver node)
        {
            var result = base.VisitImplicitReceiver(node);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitAnonymousPropertyDeclaration(BoundAnonymousPropertyDeclaration node)
        {
            var result = base.VisitAnonymousPropertyDeclaration(node);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitNoPiaObjectCreationExpression(BoundNoPiaObjectCreationExpression node)
        {
            var result = base.VisitNoPiaObjectCreationExpression(node);
            _resultType = TypeSymbolWithAnnotations.Create(node.Type, NullableAnnotation.NotNullable);
            return result;
        }

        public override BoundNode VisitNewT(BoundNewT node)
        {
            var result = base.VisitNewT(node);
            _resultType = TypeSymbolWithAnnotations.Create(node.Type, NullableAnnotation.NotNullable);
            return result;
        }

        public override BoundNode VisitArrayInitialization(BoundArrayInitialization node)
        {
            var result = base.VisitArrayInitialization(node);
            SetResult(node);
            return result;
        }

        private void SetUnknownResultNullability()
        {
            _resultType = default;
        }

        public override BoundNode VisitStackAllocArrayCreation(BoundStackAllocArrayCreation node)
        {
            var result = base.VisitStackAllocArrayCreation(node);
            Debug.Assert((object)node.Type == null || node.Type.IsPointerType() || node.Type.IsByRefLikeType);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitDynamicIndexerAccess(BoundDynamicIndexerAccess node)
        {
            var receiver = node.ReceiverOpt;
            VisitRvalue(receiver);
            // https://github.com/dotnet/roslyn/issues/30598: Mark receiver as not null
            // after indices have been visited, and only if the receiver has not changed.
            CheckPossibleNullReceiver(receiver);
            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt);

            Debug.Assert(node.Type.IsDynamic());

            // https://github.com/dotnet/roslyn/issues/29893 Update applicable members based on inferred argument types.
            NullableAnnotation nullableAnnotation = (object)node.Type != null && !node.Type.IsValueType ?
                InferResultNullabilityFromApplicableCandidates(StaticCast<Symbol>.From(node.ApplicableIndexers)) :
                NullableAnnotation.Unknown;
            _resultType = TypeSymbolWithAnnotations.Create(node.Type, nullableAnnotation);
            return null;
        }

        private void CheckPossibleNullReceiver(BoundExpression receiverOpt, bool checkNullableValueType = false, SyntaxNode syntaxOpt = null)
        {
            Debug.Assert(!this.IsConditionalState);
            if (receiverOpt != null && this.State.Reachable)
            {
#if DEBUG
                Debug.Assert(receiverOpt.Type is null || _resultType.TypeSymbol is null || AreCloseEnough(receiverOpt.Type, _resultType.TypeSymbol));
#endif
                var resultType = _resultType.TypeSymbol;
                if ((object)resultType != null &&
                    _resultType.GetValueNullableAnnotation().IsAnyNullable())
                {
                    bool isValueType = resultType.IsValueType;
                    if (isValueType && (!checkNullableValueType || !resultType.IsNullableType()))
                    {
                        return;
                    }
                    ReportSafetyDiagnostic(isValueType ? ErrorCode.WRN_NullableValueTypeMayBeNull : ErrorCode.WRN_NullReferenceReceiver, syntaxOpt ?? receiverOpt.Syntax);
                    int slot = MakeSlot(receiverOpt);
                    if (slot > 0)
                    {
                        this.State[slot] = NullableAnnotation.NotNullable;
                    }
                }
            }
        }

        private static bool IsNullabilityMismatch(TypeSymbolWithAnnotations type1, TypeSymbolWithAnnotations type2)
        {
            // Note, when we are paying attention to nullability, we ignore insignificant differences and oblivious mismatch. 
            // See TypeCompareKind.UnknownNullableModifierMatchesAny and TypeCompareKind.IgnoreInsignificantNullableModifiersDifference
            return type1.Equals(type2, TypeCompareKind.AllIgnoreOptions) &&
                !type1.Equals(type2, TypeCompareKind.AllIgnoreOptions & ~TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);
        }

        private static bool IsNullabilityMismatch(TypeSymbol type1, TypeSymbol type2)
        {
            // Note, when we are paying attention to nullability, we ignore insignificant differences and oblivious mismatch. 
            // See TypeCompareKind.UnknownNullableModifierMatchesAny and TypeCompareKind.IgnoreInsignificantNullableModifiersDifference
            return type1.Equals(type2, TypeCompareKind.AllIgnoreOptions) &&
                !type1.Equals(type2, TypeCompareKind.AllIgnoreOptions & ~TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);
        }

        private NullableAnnotation InferResultNullabilityFromApplicableCandidates(ImmutableArray<Symbol> applicableMembers)
        {
            if (applicableMembers.IsDefaultOrEmpty)
            {
                return NullableAnnotation.Unknown;
            }

            NullableAnnotation result = NullableAnnotation.NotNullable;

            foreach (Symbol member in applicableMembers)
            {
                TypeSymbolWithAnnotations type = member.GetTypeOrReturnType();

                if (type.IsReferenceType)
                {
                    NullableAnnotation memberResult = type.GetValueNullableAnnotation();
                    if (memberResult.IsAnyNullable())
                    {
                        // At least one candidate can produce null, assume dynamic access can produce null as well
                        result = NullableAnnotation.Nullable;
                        break;
                    }
                    else if (memberResult == NullableAnnotation.Unknown)
                    {
                        // At least one candidate can produce result of an unknown nullability.
                        // At best, dynamic access can produce result of an unknown nullability as well.
                        result = NullableAnnotation.Unknown;
                    }
                }
                else if (!type.IsValueType)
                {
                    result = NullableAnnotation.Unknown;
                }
            }

            return result;
        }

        public override BoundNode VisitQueryClause(BoundQueryClause node)
        {
            var result = base.VisitQueryClause(node);
            SetResult(node); // https://github.com/dotnet/roslyn/issues/29863 Implement nullability analysis in LINQ queries
            return result;
        }

        public override BoundNode VisitNameOfOperator(BoundNameOfOperator node)
        {
            var result = base.VisitNameOfOperator(node);
            _resultType = TypeSymbolWithAnnotations.Create(node.Type, NullableAnnotation.NotNullable);
            return result;
        }

        public override BoundNode VisitNamespaceExpression(BoundNamespaceExpression node)
        {
            var result = base.VisitNamespaceExpression(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitInterpolatedString(BoundInterpolatedString node)
        {
            var result = base.VisitInterpolatedString(node);
            _resultType = TypeSymbolWithAnnotations.Create(node.Type, NullableAnnotation.NotNullable);
            return result;
        }

        public override BoundNode VisitStringInsert(BoundStringInsert node)
        {
            var result = base.VisitStringInsert(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitConvertedStackAllocExpression(BoundConvertedStackAllocExpression node)
        {
            var result = base.VisitConvertedStackAllocExpression(node);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitDiscardExpression(BoundDiscardExpression node)
        {
            SetResult(node);
            return null;
        }

        public override BoundNode VisitThrowExpression(BoundThrowExpression node)
        {
            var result = base.VisitThrowExpression(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            BoundExpression expr = node.Expression;
            if (expr == null)
            {
                return null;
            }
            var method = (MethodSymbol)_symbol;
            TypeSymbolWithAnnotations elementType = InMethodBinder.GetIteratorElementTypeFromReturnType(compilation, RefKind.None, method.ReturnType.TypeSymbol, errorLocationNode: null, diagnostics: null);
            VisitOptionalImplicitConversion(expr, elementType, useLegacyWarnings: false, AssignmentKind.Return);
            return null;
        }

        #endregion Visitors

        protected override string Dump(LocalState state)
        {
            return string.Empty;
        }

        protected override void Meet(ref LocalState self, ref LocalState other)
        {
            if (self.Capacity != other.Capacity)
            {
                Normalize(ref self);
                Normalize(ref other);
            }

            for (int slot = 1; slot < self.Capacity; slot++)
            {
                NullableAnnotation selfAnnotation = self[slot];
                NullableAnnotation otherAnnotation = other[slot];
                NullableAnnotation union = selfAnnotation.MeetForFlowAnalysisFinally(otherAnnotation);

                if (selfAnnotation != union)
                {
                    self[slot] = union;
                }

                bool selfIsAssigned = self.IsAssigned(slot);
                bool isAssigned = selfIsAssigned || other.IsAssigned(slot);
                if (selfIsAssigned != isAssigned)
                {
                    self.SetAssigned(slot, isAssigned);
                }
            }
        }

        protected override bool Join(ref LocalState self, ref LocalState other)
        {
            if (self.Reachable == other.Reachable)
            {
                bool result = false;

                if (self.Capacity != other.Capacity)
                {
                    Normalize(ref self);
                    Normalize(ref other);
                }

                for (int slot = 1; slot < self.Capacity; slot++)
                {
                    NullableAnnotation selfAnnotation = self[slot];
                    NullableAnnotation intersection = selfAnnotation.JoinForFlowAnalysisBranches(other[slot], (slot, this), IsPossiblyNullableReferenceTypeTypeParameterDelegate);
                    if (selfAnnotation != intersection)
                    {
                        self[slot] = intersection;
                        result = true;
                    }

                    bool selfIsAssigned = self.IsAssigned(slot);
                    bool isAssigned = selfIsAssigned && other.IsAssigned(slot);
                    if (selfIsAssigned != isAssigned)
                    {
                        self.SetAssigned(slot, isAssigned);
                        result = true;
                    }
                }

                return result;
            }
            else if (!self.Reachable)
            {
                self = other.Clone();
                return true;
            }
            else
            {
                Debug.Assert(!other.Reachable);
                return false;
            }
        }

        private readonly static Func<(int slot, NullableWalker self), bool> IsPossiblyNullableReferenceTypeTypeParameterDelegate = args =>
        {
            Symbol symbol = args.self.variableBySlot[args.slot].Symbol;
            return (object)symbol != null && VariableType(symbol).TypeSymbol?.IsPossiblyNullableReferenceTypeTypeParameter() == true;
        };

        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
#if REFERENCE_STATE
        internal class LocalState : ILocalState
#else
        internal struct LocalState : ILocalState
#endif
        {
            private BitVector _assigned;
            private ArrayBuilder<NullableAnnotation> _state;
            public bool Reachable { get; }

            internal LocalState(bool reachable, BitVector assigned, ArrayBuilder<NullableAnnotation> state)
            {
                Debug.Assert(!assigned.IsNull);
                this.Reachable = reachable;
                this._assigned = assigned;
                this._state = state;
            }

            internal int Capacity => _state?.Count ?? 0;

            internal void EnsureCapacity(int capacity)
            {
                _assigned.EnsureCapacity(capacity);

                if (_state == null)
                {
                    _state = new ArrayBuilder<NullableAnnotation>(capacity);
                }

                if (_state.Count < capacity)
                {
                    _state.Count = capacity;
                }
            }

            internal NullableAnnotation this[int slot]
            {
                get
                {
                    if (slot < Capacity)
                    {
                        return _state[slot];
                    }

                    return NullableAnnotation.Unknown;
                }
                set
                {
                    EnsureCapacity(slot + 1);
                    _state[slot] = value;
                    SetAssigned(slot, true);
                }
            }

            internal void SetAssigned(int slot, bool value)
            {
                _assigned[slot] = value;
            }

            internal bool IsAssigned(int slot)
            {
                return _assigned[slot];
            }

            /// <summary>
            /// Produce a duplicate of this flow analysis state.
            /// </summary>
            /// <returns></returns>
            public LocalState Clone()
            {
                ArrayBuilder<NullableAnnotation> clone;

                if (_state == null)
                {
                    clone = null;
                }
                else
                {
                    clone = new ArrayBuilder<NullableAnnotation>(_state.Count);
                    clone.Count = 0;
                    clone.AddRange(_state);
                }

                return new LocalState(Reachable, _assigned.Clone(), clone);
            }

            internal string GetDebuggerDisplay()
            {
                var pooledBuilder = PooledStringBuilder.GetInstance();
                var builder = pooledBuilder.Builder;
                builder.Append(" ");
                for (int i = this.Capacity - 1; i >= 0; i--)
                {
                    string append;

                    switch (_state[i])
                    {
                        case NullableAnnotation.Unknown:
                            append = "__";
                            break;

                        case NullableAnnotation.Annotated:
                            append = "?-";
                            break;

                        case NullableAnnotation.Nullable:
                            append = "?+";
                            break;

                        case NullableAnnotation.NotNullable:
                            append = "!+";
                            break;

                        case NullableAnnotation.NotAnnotated:
                            append = "!-";
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(_state[i]);
                    }

                    builder.Append(append);
                }

                return pooledBuilder.ToStringAndFree();
            }
        }
    }
}
