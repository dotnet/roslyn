// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if DEBUG
// See comment in DataFlowPass.
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
    internal sealed partial class NullableWalker : DataFlowPassBase<NullableWalker.LocalState>
    {
        /// <summary>
        /// The inferred type at the point of declaration of var locals.
        /// </summary>
        // PROTOTYPE(NullableReferenceTypes): Does this need to
        // move to LocalState so it participates in merging?
        private readonly PooledDictionary<LocalSymbol, TypeSymbolWithAnnotations> _variableTypes = PooledDictionary<LocalSymbol, TypeSymbolWithAnnotations>.GetInstance();

        /// <summary>
        /// The current source assembly.
        /// </summary>
        private readonly SourceAssemblySymbol _sourceAssembly;

        // PROTOTYPE(NullableReferenceTypes): Remove the Binder if possible. 
        private readonly Binder _binder;

        private readonly Conversions _conversions;

        private readonly Action<BoundExpression, TypeSymbolWithAnnotations> _callbackOpt;

        /// <summary>
        /// Invalid type, used only to catch Visit methods that do not set
        /// _result.Type. See VisitExpressionWithoutStackGuard.
        /// </summary>
        private static readonly TypeSymbolWithAnnotations _invalidType = TypeSymbolWithAnnotations.Create(ErrorTypeSymbol.UnknownResultType);

        private Result _result; // PROTOTYPE(NullableReferenceTypes): Should be return value from the visitor, not mutable state.

        /// <summary>
        /// Reflects the enclosing method or lambda at the current location (in the bound tree).
        /// </summary>
        private MethodSymbol _currentMethodOrLambda;

        /// <summary>
        /// Instances being constructed.
        /// </summary>
        private PooledDictionary<BoundExpression, ObjectCreationPlaceholderLocal> _placeholderLocals;

        protected override void Free()
        {
            _variableTypes.Free();
            _placeholderLocals?.Free();
            base.Free();
        }

        private NullableWalker(
            CSharpCompilation compilation,
            MethodSymbol member,
            BoundNode node,
            Action<BoundExpression, TypeSymbolWithAnnotations> callbackOpt)
            : base(compilation, member, node, new EmptyStructTypeCache(compilation, dev12CompilerCompatibility: false), trackUnassignments: false)
        {
            _sourceAssembly = ((object)member == null) ? null : (SourceAssemblySymbol)member.ContainingAssembly;
            this._currentMethodOrLambda = member;
            _callbackOpt = callbackOpt;
            // PROTOTYPE(NullableReferenceTypes): Do we really need a Binder?
            // If so, are we interested in an InMethodBinder specifically?
            _binder = compilation.GetBinderFactory(node.SyntaxTree).GetBinder(node.Syntax);
            Debug.Assert(!_binder.Conversions.IncludeNullability);
            _conversions = _binder.Conversions.WithNullability(true);
        }

        protected override bool ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()
        {
            return true;
        }

        protected override ImmutableArray<PendingBranch> Scan(ref bool badRegion)
        {
            this.Diagnostics.Clear();
            ImmutableArray<ParameterSymbol> methodParameters = MethodParameters;
            ParameterSymbol methodThisParameter = MethodThisParameter;
            this.State = ReachableState();                   // entry point is reachable
            this.regionPlace = RegionPlace.Before;
            EnterParameters(methodParameters);               // with parameters assigned
            if ((object)methodThisParameter != null)
            {
                EnterParameter(methodThisParameter);
            }

            ImmutableArray<PendingBranch> pendingReturns = base.Scan(ref badRegion);
            return pendingReturns;
        }

        public static void Analyze(CSharpCompilation compilation, MethodSymbol member, BoundNode node, DiagnosticBag diagnostics, Action<BoundExpression, TypeSymbolWithAnnotations> callbackOpt = null)
        {
            Debug.Assert(diagnostics != null);

            if (member.IsImplicitlyDeclared)
            {
                return;
            }

            var flags = ((CSharpParseOptions)node.SyntaxTree.Options).GetNullableReferenceFlags();
            var walker = new NullableWalker(
                compilation,
                member,
                node,
                callbackOpt);

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
                var value = GetDefaultState(ref state, slot);
                state[slot] = value;
            }
        }

        private bool? GetDefaultState(ref LocalState state, int slot)
        {
            if (slot == 0)
            {
                return null;
            }

            var variable = variableBySlot[slot];
            var symbol = variable.Symbol;

            switch (symbol.Kind)
            {
                case SymbolKind.Local:
                    return null;
                case SymbolKind.Parameter:
                    {
                        var parameter = (ParameterSymbol)symbol;
                        return (parameter.RefKind == RefKind.Out) ?
                            null :
                            !parameter.Type.IsNullable;
                    }
                case SymbolKind.Field:
                case SymbolKind.Property:
                case SymbolKind.Event:
                    {
                        // PROTOTYPE(NullableReferenceTypes): State of containing struct should not be important.
                        int containingSlot = variable.ContainingSlot;
                        if (containingSlot > 0 &&
                            variableBySlot[containingSlot].Symbol.GetTypeOrReturnType().TypeKind == TypeKind.Struct &&
                            state[containingSlot] == null)
                        {
                            return null;
                        }
                        return !symbol.GetTypeOrReturnType().IsNullable;
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
                        if (fieldSymbol.IsStatic || fieldSymbol.IsFixed)
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
                        // PROTOTYPE(NullableReferenceTypes): Use AssociatedField for field-like events?
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

        // PROTOTYPE(NullableReferenceTypes): Use backing field for struct property
        // for now, to avoid cycles if the struct type contains a property of the struct type.
        // Remove this and populate struct members lazily to match classes.
        private Symbol GetBackingFieldIfStructProperty(Symbol symbol)
        {
            if (symbol.Kind == SymbolKind.Property)
            {
                var property = (PropertySymbol)symbol;
                var containingType = property.ContainingType;
                if (containingType.TypeKind == TypeKind.Struct)
                {
                    // PROTOTYPE(NullableReferenceTypes): Relying on field name
                    // will not work for properties declared in other languages.
                    var fieldName = GeneratedNames.MakeBackingFieldName(property.Name);
                    return _emptyStructTypeCache.GetStructInstanceFields(containingType).FirstOrDefault(f => f.Name == fieldName);
                }
            }
            return symbol;
        }

        // PROTOTYPE(NullableReferenceTypes): Temporary, until we're using
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

        // PROTOTYPE(NullableReferenceTypes): Remove use of MakeSlot.
        protected override int MakeSlot(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundKind.ObjectCreationExpression:
                case BoundKind.AnonymousObjectCreationExpression:
                    if (_placeholderLocals != null && _placeholderLocals.TryGetValue(node, out ObjectCreationPlaceholderLocal placeholder))
                    {
                        return GetOrCreateSlot(placeholder);
                    }
                    break;
            }
            return base.MakeSlot(node);
        }

        private new void VisitLvalue(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundKind.Local:
                    _result = GetDeclaredLocalResult(((BoundLocal)node).LocalSymbol);
                    break;
                case BoundKind.Parameter:
                    _result = GetDeclaredParameterResult(((BoundParameter)node).ParameterSymbol);
                    break;
                case BoundKind.FieldAccess:
                    {
                        var fieldAccess = (BoundFieldAccess)node;
                        VisitMemberAccess(fieldAccess.ReceiverOpt, fieldAccess.FieldSymbol, asLvalue: true);
                    }
                    break;
                case BoundKind.PropertyAccess:
                    {
                        var propertyAccess = (BoundPropertyAccess)node;
                        VisitMemberAccess(propertyAccess.ReceiverOpt, propertyAccess.PropertySymbol, asLvalue: true);
                    }
                    break;
                case BoundKind.EventAccess:
                    {
                        var eventAccess = (BoundEventAccess)node;
                        VisitMemberAccess(eventAccess.ReceiverOpt, eventAccess.EventSymbol, asLvalue: true);
                    }
                    break;
                case BoundKind.ObjectInitializerMember:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind); // Should have been handled in VisitObjectCreationExpression().
                default:
                    VisitRvalue(node);
                    break;
            }
        }

        private Result VisitRvalueWithResult(BoundExpression node)
        {
            base.VisitRvalue(node);
            return _result;
        }

        private static object GetTypeAsDiagnosticArgument(TypeSymbol typeOpt)
        {
            // PROTOTYPE(NullableReferenceTypes): Avoid hardcoded string.
            return typeOpt ?? (object)"<null>";
        }

        private bool ReportNullReferenceAssignmentIfNecessary(BoundExpression value, TypeSymbolWithAnnotations targetType, TypeSymbolWithAnnotations valueType, bool useLegacyWarnings)
        {
            Debug.Assert(value != null);
            Debug.Assert(!IsConditionalState);

            if (targetType is null || valueType is null)
            {
                return false;
            }

            if (targetType.IsReferenceType && targetType.IsNullable == false && valueType.IsNullable == true)
            {
                if (useLegacyWarnings)
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_ConvertingNullableToNonNullable, value.Syntax);
                }
                else if (!ReportNullAsNonNullableReferenceIfNecessary(value))
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceAssignment, value.Syntax);
                }
                return true;
            }

            return false;
        }

        private void ReportAssignmentWarnings(BoundExpression value, TypeSymbolWithAnnotations targetType, TypeSymbolWithAnnotations valueType, bool useLegacyWarnings)
        {
            Debug.Assert(value != null);
            Debug.Assert(!IsConditionalState);

            if (this.State.Reachable)
            {
                if (targetType is null || valueType is null)
                {
                    return;
                }

                ReportNullReferenceAssignmentIfNecessary(value, targetType, valueType, useLegacyWarnings);
                ReportNullabilityMismatchInAssignmentIfNecessary(value, valueType.TypeSymbol, targetType.TypeSymbol);
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
                if ((object)targetType == null)
                {
                    return;
                }

                if (targetSlot <= 0)
                {
                    return;
                }

                bool isByRefTarget = IsByRefTarget(targetSlot);
                if (targetSlot >= this.State.Capacity) Normalize(ref this.State);

                this.State[targetSlot] = isByRefTarget ?
                    // Since reference can point to the heap, we cannot assume the value is not null after this assignment,
                    // regardless of what value is being assigned. 
                    (targetType.IsNullable == true) ? (bool?)false : null :
                    !valueType?.IsNullable;

                // PROTOTYPE(NullableReferenceTypes): Might this clear state that
                // should be copied in InheritNullableStateOfTrackableType?
                InheritDefaultState(targetSlot);

                if (targetType.IsReferenceType)
                {
                    // PROTOTYPE(NullableReferenceTypes): We should copy all tracked state from `value`,
                    // regardless of BoundNode type, but we'll need to handle cycles. (For instance, the
                    // assignment to C.F below. See also StaticNullChecking_Members.FieldCycle_01.)
                    // class C
                    // {
                    //     C? F;
                    //     C() { F = this; }
                    // }
                    // For now, we copy a limited set of BoundNode types that shouldn't contain cycles.
                    if ((value.Kind == BoundKind.ObjectCreationExpression || value.Kind == BoundKind.AnonymousObjectCreationExpression || value.Kind == BoundKind.DynamicObjectCreationExpression || targetType.TypeSymbol.IsAnonymousType) &&
                        targetType.TypeSymbol.Equals(valueType?.TypeSymbol, TypeCompareKind.ConsiderEverything)) // PROTOTYPE(NullableReferenceTypes): Allow assignment to base type.
                    {
                        if (valueSlot > 0)
                        {
                            InheritNullableStateOfTrackableType(targetSlot, valueSlot, isByRefTarget, slotWatermark: GetSlotWatermark());
                        }
                    }
                }
                else if (EmptyStructTypeCache.IsTrackableStructType(targetType.TypeSymbol) &&
                        targetType.TypeSymbol.Equals(valueType?.TypeSymbol, TypeCompareKind.ConsiderEverything))
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

        private void ReportStaticNullCheckingDiagnostics(ErrorCode errorCode, SyntaxNode syntaxNode, params object[] arguments)
        {
            Diagnostics.Add(errorCode, syntaxNode.GetLocation(), arguments);
        }

        private void InheritNullableStateOfTrackableStruct(TypeSymbol targetType, int targetSlot, int valueSlot, bool isByRefTarget, int slotWatermark)
        {
            Debug.Assert(targetSlot > 0);
            Debug.Assert(EmptyStructTypeCache.IsTrackableStructType(targetType));

            // PROTOTYPE(NullableReferenceTypes): Handle properties not backed by fields.
            // See ModifyMembers_StructPropertyNoBackingField and PropertyCycle_Struct tests.
            foreach (var field in _emptyStructTypeCache.GetStructInstanceFields(targetType))
            {
                InheritNullableStateOfFieldOrProperty(targetSlot, valueSlot, field, isByRefTarget, slotWatermark);
            }
        }

        // 'slotWatermark' is used to avoid inheriting members from inherited members.
        private void InheritNullableStateOfFieldOrProperty(int targetContainerSlot, int valueContainerSlot, Symbol fieldOrProperty, bool isByRefTarget, int slotWatermark)
        {
            Debug.Assert(valueContainerSlot <= slotWatermark);

            TypeSymbolWithAnnotations fieldOrPropertyType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(fieldOrProperty);

            if (fieldOrPropertyType.IsReferenceType)
            {
                // If statically declared as not-nullable, no need to adjust the tracking info. 
                // Declaration information takes priority.
                if (fieldOrPropertyType.IsNullable != false)
                {
                    int targetMemberSlot = GetOrCreateSlot(fieldOrProperty, targetContainerSlot);
                    bool? value = !fieldOrPropertyType.IsNullable;
                    if (isByRefTarget)
                    {
                        // This is a property/field access through a by ref entity and it isn't considered declared as not-nullable. 
                        // Since reference can point to the heap, we cannot assume the property/field doesn't have null value after this assignment,
                        // regardless of what value is being assigned.
                    }
                    else if (valueContainerSlot > 0)
                    {
                        int valueMemberSlot = VariableSlot(fieldOrProperty, valueContainerSlot);
                        value = valueMemberSlot > 0 && valueMemberSlot < this.State.Capacity ?
                            this.State[valueMemberSlot] :
                            null;
                    }

                    this.State[targetMemberSlot] = value;
                }

                if (valueContainerSlot > 0)
                {
                    int valueMemberSlot = VariableSlot(fieldOrProperty, valueContainerSlot);
                    if (valueMemberSlot > 0 && valueMemberSlot <= slotWatermark)
                    {
                        int targetMemberSlot = GetOrCreateSlot(fieldOrProperty, targetContainerSlot);
                        InheritNullableStateOfTrackableType(targetMemberSlot, valueMemberSlot, isByRefTarget, slotWatermark);
                    }
                }
            }
            else if (EmptyStructTypeCache.IsTrackableStructType(fieldOrPropertyType.TypeSymbol))
            {
                int targetMemberSlot = GetOrCreateSlot(fieldOrProperty, targetContainerSlot);
                if (targetMemberSlot > 0)
                {
                    int valueMemberSlot = -1;
                    if (valueContainerSlot > 0)
                    {
                        int slot = GetOrCreateSlot(fieldOrProperty, valueContainerSlot);
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
                this.State[slot] = !variable.Symbol.GetTypeOrReturnType().IsNullable;
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
                Debug.Assert(member.Kind == SymbolKind.Field || member.Kind == SymbolKind.Property);
                InheritNullableStateOfFieldOrProperty(targetSlot, valueSlot, member, isByRefTarget, slotWatermark);
            }
        }

        protected override LocalState ReachableState()
        {
            var state = new LocalState(BitVector.Create(nextVariableSlot), BitVector.Create(nextVariableSlot));
            Populate(ref state, start: 0);
            return state;
        }

        protected override LocalState UnreachableState()
        {
            return new LocalState(BitVector.Empty, BitVector.Empty);
        }

        protected override LocalState AllBitsSet()
        {
            return new LocalState(BitVector.Create(nextVariableSlot), BitVector.Create(nextVariableSlot));
        }

        private void EnterParameters(ImmutableArray<ParameterSymbol> parameters)
        {
            // label out parameters as not assigned.
            foreach (var parameter in parameters)
            {
                EnterParameter(parameter);
            }
        }

        private void EnterParameter(ParameterSymbol parameter)
        {
            int slot = GetOrCreateSlot(parameter);
            Debug.Assert(!IsConditionalState);
            if (slot > 0 && parameter.RefKind != RefKind.Out)
            {
                var paramType = parameter.Type.TypeSymbol;
                if (EmptyStructTypeCache.IsTrackableStructType(paramType))
                {
                    InheritNullableStateOfTrackableStruct(paramType, slot, valueSlot: -1, isByRefTarget: parameter.RefKind != RefKind.None, slotWatermark: GetSlotWatermark());
                }
            }
        }

        #region Visitors

        public override BoundNode VisitIsPatternExpression(BoundIsPatternExpression node)
        {
            // PROTOTYPE(NullableReferenceTypes): Move these asserts to base class.
            Debug.Assert(!IsConditionalState);

            // Create slot when the state is unconditional since EnsureCapacity should be
            // called on all fields and that is simpler if state is limited to this.State.
            int slot = -1;
            if (this.State.Reachable)
            {
                var pattern = node.Pattern;
                // PROTOTYPE(NullableReferenceTypes): Handle patterns that ensure x is not null:
                // x is T y // where T is not inferred via var
                // x is K // where K is a constant other than null
                if (pattern.Kind == BoundKind.ConstantPattern && ((BoundConstantPattern)pattern).ConstantValue?.IsNull == true)
                {
                    slot = MakeSlot(node.Expression);
                    if (slot > 0)
                    {
                        Normalize(ref this.State);
                    }
                }
            }

            var result = base.VisitIsPatternExpression(node);

            Debug.Assert(IsConditionalState);
            if (slot > 0)
            {
                this.StateWhenTrue[slot] = false;
                this.StateWhenFalse[slot] = true;
            }

            SetResult(node);
            return result;
        }

        public override void VisitPattern(BoundExpression expression, BoundPattern pattern)
        {
            base.VisitPattern(expression, pattern);
            var whenFail = StateWhenFalse;
            SetState(StateWhenTrue);
            AssignPatternVariables(pattern);
            SetConditionalState(this.State, whenFail);
            SetUnknownResultNullability();
        }

        private void AssignPatternVariables(BoundPattern pattern)
        {
            switch (pattern.Kind)
            {
                case BoundKind.DeclarationPattern:
                    // PROTOTYPE(NullableReferenceTypes): Handle.
                    break;
                case BoundKind.WildcardPattern:
                    break;
                case BoundKind.ConstantPattern:
                    {
                        var pat = (BoundConstantPattern)pattern;
                        this.VisitRvalue(pat.Value);
                        break;
                    }
                default:
                    break;
            }
        }

        protected override BoundNode VisitReturnStatementNoAdjust(BoundReturnStatement node)
        {
            var result = base.VisitReturnStatementNoAdjust(node);

            Debug.Assert(!IsConditionalState);
            if (node.ExpressionOpt != null && this.State.Reachable)
            {
                TypeSymbolWithAnnotations returnType = this._currentMethodOrLambda?.ReturnType;
                bool returnTypeIsNonNullable = IsNonNullable(returnType);
                bool returnTypeIsUnconstrainedTypeParameter = IsUnconstrainedTypeParameter(returnType?.TypeSymbol);
                bool reportedNullable = false;
                if (returnTypeIsNonNullable || returnTypeIsUnconstrainedTypeParameter)
                {
                    reportedNullable = ReportNullAsNonNullableReferenceIfNecessary(node.ExpressionOpt);
                }
                if (!reportedNullable)
                {
                    TypeSymbolWithAnnotations resultType = _result.Type;
                    if (IsNullable(resultType) && (returnTypeIsNonNullable || returnTypeIsUnconstrainedTypeParameter) ||
                        IsUnconstrainedTypeParameter(resultType?.TypeSymbol) && returnTypeIsNonNullable)
                    {
                        ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceReturn, node.ExpressionOpt.Syntax);
                    }
                }

                ReportNullabilityMismatchInAssignmentIfNecessary(node.ExpressionOpt, node.ExpressionOpt.Type, returnType.TypeSymbol);
            }

            return result;
        }

        private static bool IsNullable(TypeSymbolWithAnnotations typeOpt)
        {
            return typeOpt?.IsNullable == true;
        }

        private static bool IsNonNullable(TypeSymbolWithAnnotations typeOpt)
        {
            return typeOpt?.IsNullable == false && typeOpt.IsReferenceType;
        }

        private static bool IsUnconstrainedTypeParameter(TypeSymbol typeOpt)
        {
            return typeOpt?.IsUnconstrainedTypeParameter() == true;
        }

        /// <summary>
        /// Report warning assigning value where nested nullability does not match
        /// target (e.g.: `object[] a = new[] { maybeNull }`).
        /// </summary>
        private void ReportNullabilityMismatchInAssignmentIfNecessary(BoundExpression node, TypeSymbol sourceType, TypeSymbol destinationType)
        {
            if ((object)sourceType != null && IsNullabilityMismatch(destinationType, sourceType))
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInAssignment, node.Syntax, sourceType, destinationType);
            }
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            _result = GetAdjustedResult(GetDeclaredLocalResult(node.LocalSymbol));
            return null;
        }

        public override BoundNode VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            var local = node.LocalSymbol;
            int slot = GetOrCreateSlot(local);

            var initializer = node.InitializerOpt;
            if (initializer != null)
            {
                VisitRvalue(initializer);
                Result value = _result;
                TypeSymbolWithAnnotations type = local.Type;
                TypeSymbolWithAnnotations valueType = value.Type;

                if (type.IsReferenceType && node.DeclaredType.InferredType && (object)valueType != null)
                {
                    _variableTypes[local] = valueType;
                    type = valueType;
                }

                ReportAssignmentWarnings(initializer, type, valueType, useLegacyWarnings: true);
                TrackNullableStateForAssignment(initializer, type, slot, valueType, value.Slot);
            }

            return null;
        }

        protected override BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            Debug.Assert(!IsConditionalState);
            _result = _invalidType; // PROTOTYPE(NullableReferenceTypes): Move to `Visit` method?
            var result = base.VisitExpressionWithoutStackGuard(node);
#if DEBUG
            // Verify Visit method set _result.
            if (!IsConditionalState)
            {
                TypeSymbolWithAnnotations resultType = _result.Type;
                Debug.Assert((object)resultType != _invalidType);
                Debug.Assert((object)resultType == null || AreCloseEnough(resultType.TypeSymbol, node.Type));
            }
#endif
            if (_callbackOpt != null)
            {
                _callbackOpt(node, _result.Type);
            }
            return result;
        }

#if DEBUG
        // For asserts only.
        private static bool AreCloseEnough(TypeSymbol typeA, TypeSymbol typeB)
        {
            bool canIgnoreType(TypeSymbol type) => (object)type.VisitType((t, unused1, unused2) => t.IsErrorType() || t.IsDynamic() || t.HasUseSiteError, (object)null) != null;
            return canIgnoreType(typeA) ||
                canIgnoreType(typeB) ||
                typeA.Equals(typeB, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.IgnoreDynamicAndTupleNames); // Ignore TupleElementNames (see https://github.com/dotnet/roslyn/issues/23651).
        }
#endif

        protected override void VisitStatement(BoundStatement statement)
        {
            _result = _invalidType;
            base.VisitStatement(statement);
            _result = _invalidType;
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
                if (type.IsReferenceType || isTrackableStructType)
                {
                    receiver = GetOrCreateObjectCreationPlaceholder(node);
                    slot = GetOrCreateSlot(receiver);
                    if (slot > 0 && isTrackableStructType)
                    {
                        this.State[slot] = true;
                        InheritNullableStateOfTrackableStruct(type, slot, valueSlot: -1, isByRefTarget: false, slotWatermark: GetSlotWatermark());
                    }
                }
            }

            if (initializerOpt != null)
            {
                VisitObjectCreationInitializer(receiver, slot, initializerOpt);
            }

            _result = Result.Create(TypeSymbolWithAnnotations.Create(type), slot);
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
                    Result result = VisitRvalueWithResult(node);
                    if ((object)containingSymbol != null)
                    {
                        var type = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(containingSymbol);
                        ReportAssignmentWarnings(node, type, result.Type, useLegacyWarnings: false);
                        TrackNullableStateForAssignment(node, type, containingSlot, result.Type, result.Slot);
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
            if (node.AddMethod.CallsAreOmitted(node.SyntaxTree))
            {
                // PROTOTYPE(NullableReferenceTypes): Should skip state set in arguments
                // of omitted call. See PreciseAbstractFlowPass.VisitCollectionElementInitializer.
            }

            VisitArguments(node, node.Arguments, default(ImmutableArray<RefKind>), node.AddMethod, node.ArgsToParamsOpt, node.Expanded);
            SetUnknownResultNullability();
        }

        private void SetResult(BoundExpression node)
        {
            _result = TypeSymbolWithAnnotations.Create(node.Type);
        }

        private ObjectCreationPlaceholderLocal GetOrCreateObjectCreationPlaceholder(BoundExpression node)
        {
            ObjectCreationPlaceholderLocal placeholder;
            if (_placeholderLocals == null)
            {
                _placeholderLocals = PooledDictionary<BoundExpression, ObjectCreationPlaceholderLocal>.GetInstance();
                placeholder = null;
            }
            else
            {
                _placeholderLocals.TryGetValue(node, out placeholder);
            }

            if ((object)placeholder == null)
            {
                placeholder = new ObjectCreationPlaceholderLocal(_member, node);
                _placeholderLocals.Add(node, placeholder);
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
                Result argumentResult = VisitRvalueWithResult(argument);
                var parameter = constructor.Parameters[i];
                ReportArgumentWarnings(argument, argumentResult.Type, parameter);

                // PROTOTYPE(NullableReferenceTypes): node.Declarations includes
                // explicitly-named properties only. For now, skip expressions
                // with implicit names. See StaticNullChecking.AnonymousTypes_05.
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

                ReportAssignmentWarnings(argument, property.Type, argumentResult.Type, useLegacyWarnings: false);
                TrackNullableStateForAssignment(argument, property.Type, GetOrCreateSlot(property, receiverSlot), argumentResult.Type, argumentResult.Slot);
            }

            // PROTOTYPE(NullableReferenceType): Result.Type may need to be a new anonymous
            // type since the properties may have distinct nullability from original.
            // (See StaticNullChecking_FlowAnalysis.AnonymousObjectCreation_02.)
            _result = Result.Create(TypeSymbolWithAnnotations.Create(node.Type), receiverSlot);
            return null;
        }

        public override BoundNode VisitArrayCreation(BoundArrayCreation node)
        {
            foreach (var expr in node.Bounds)
            {
                VisitRvalue(expr);
            }
            TypeSymbol resultType = (node.InitializerOpt == null) ? node.Type : VisitArrayInitializer(node);
            _result = TypeSymbolWithAnnotations.Create(resultType);
            return null;
        }

        private ArrayTypeSymbol VisitArrayInitializer(BoundArrayCreation node)
        {
            var arrayType = (ArrayTypeSymbol)node.Type;
            var elementType = arrayType.ElementType;

            BoundArrayInitialization initialization = node.InitializerOpt;
            var elementBuilder = ArrayBuilder<BoundExpression>.GetInstance(initialization.Initializers.Length);
            GetArrayElements(initialization, elementBuilder);

            // PROTOTYPE(NullableReferenceType): Removing and recalculating conversions should not
            // be necessary for explicitly typed arrays. In those cases, VisitConversion should warn
            // on nullability mismatch (although we'll need to ensure we handle the case where
            // initial binding calculated an Identity conversion, even though nullability was distinct.
            int n = elementBuilder.Count;
            var resultBuilder = ArrayBuilder<Result>.GetInstance(n);
            for (int i = 0; i < n; i++)
            {
                BoundExpression element = RemoveImplicitConversions(elementBuilder[i]);
                elementBuilder[i] = element;
                resultBuilder.Add(VisitRvalueWithResult(element));
            }

            // PROTOTYPE(NullableReferenceType): Record in the BoundArrayCreation
            // whether the array was implicitly typed, rather than relying on syntax.
            if (node.Syntax.Kind() == SyntaxKind.ImplicitArrayCreationExpression)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                var resultTypes = resultBuilder.SelectAsArray(r => r.Type);
                // PROTOTYPE(NullableReferenceType): Initial binding calls InferBestType(ImmutableArray<BoundExpression>, ...)
                // overload. Why are we calling InferBestType(ImmutableArray<TypeSymbolWithAnnotations>, ...) here?
                // PROTOTYPE(NullableReferenceType): InferBestType(ImmutableArray<BoundExpression>, ...)
                // uses a HashSet<TypeSymbol> to reduce the candidates to the unique types before comparing.
                // Should do the same here.
                var bestType = BestTypeInferrer.InferBestType(resultTypes, _conversions, useSiteDiagnostics: ref useSiteDiagnostics);
                // PROTOTYPE(NullableReferenceType): Report a special ErrorCode.WRN_NoBestNullabilityArrayElements
                // when InferBestType fails, and avoid reporting conversion warnings for each element in those cases.
                // (See similar code for conditional expressions: ErrorCode.WRN_NoBestNullabilityConditionalExpression.)
                if ((object)bestType != null)
                {
                    elementType = bestType;
                }
                arrayType = arrayType.WithElementType(elementType);
            }

            if ((object)elementType != null)
            {
                TypeSymbol destinationType = elementType.TypeSymbol;
                bool elementTypeIsReferenceType = elementType.IsReferenceType == true;
                for (int i = 0; i < n; i++)
                {
                    var element = elementBuilder[i];
                    var resultType = resultBuilder[i].Type;
                    // See Binder.GenerateConversionForAssignment for initial binding.
                    var sourceType = resultType?.TypeSymbol;
                    Conversion conversion = GenerateConversion(_conversions, element, sourceType, destinationType);
                    if (!conversion.Exists)
                    {
                        ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInAssignment, element.Syntax, GetTypeAsDiagnosticArgument(sourceType), destinationType);
                        continue;
                    }
                    if (elementTypeIsReferenceType)
                    {
                        resultType = InferResultNullability(element, conversion, destinationType, resultType);
                        ReportAssignmentWarnings(element, elementType, resultType, useLegacyWarnings: false);
                    }
                }
            }
            resultBuilder.Free();

            elementBuilder.Free();
            _result = _invalidType;
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
            // No need to check expression type since System.Array is a reference type.
            Debug.Assert(node.Expression.Type.IsReferenceType);
            CheckPossibleNullReceiver(node.Expression, checkType: false);

            var type = _result.Type?.TypeSymbol as ArrayTypeSymbol;

            foreach (var i in node.Indices)
            {
                VisitRvalue(i);
            }

            _result = type?.ElementType;
            return null;
        }

        private TypeSymbolWithAnnotations InferResultNullability(BoundBinaryOperator node, TypeSymbolWithAnnotations leftType, TypeSymbolWithAnnotations rightType)
        {
            return InferResultNullability(node.OperatorKind, node.MethodOpt, node.Type, leftType, rightType);
        }

        private TypeSymbolWithAnnotations InferResultNullability(BinaryOperatorKind operatorKind, MethodSymbol methodOpt, TypeSymbol resultType, TypeSymbolWithAnnotations leftType, TypeSymbolWithAnnotations rightType)
        {
            bool? isNullable = null;
            if (operatorKind.IsUserDefined())
            {
                if (operatorKind.IsLifted())
                {
                    // PROTOTYPE(NullableReferenceTypes): Conversions: Lifted operator
                    return TypeSymbolWithAnnotations.Create(resultType, isNullableIfReferenceType: null);
                }
                // PROTOTYPE(NullableReferenceTypes): Update method based on operand types.
                if ((object)methodOpt != null && methodOpt.ParameterCount == 2)
                {
                    return methodOpt.ReturnType;
                }
            }
            else if (!operatorKind.IsDynamic() && resultType.IsReferenceType == true)
            {
                switch (operatorKind.Operator() | operatorKind.OperandTypes())
                {
                    case BinaryOperatorKind.DelegateCombination:
                        {
                            bool? leftIsNullable = leftType?.IsNullable;
                            bool? rightIsNullable = rightType?.IsNullable;
                            if (leftIsNullable == false || rightIsNullable == false)
                            {
                                isNullable = false;
                            }
                            else if (leftIsNullable == true && rightIsNullable == true)
                            {
                                isNullable = true;
                            }
                            else
                            {
                                Debug.Assert(leftIsNullable == null || rightIsNullable == null);
                            }
                        }
                        break;
                    case BinaryOperatorKind.DelegateRemoval:
                        isNullable = true; // Delegate removal can produce null.
                        break;
                    default:
                        isNullable = false;
                        break;
                }
            }
            return TypeSymbolWithAnnotations.Create(resultType, isNullable);
        }

        protected override void AfterLeftChildHasBeenVisited(BoundBinaryOperator binary)
        {
            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                TypeSymbolWithAnnotations leftType = _result.Type;
                bool warnOnNullReferenceArgument = (binary.OperatorKind.IsUserDefined() && (object)binary.MethodOpt != null && binary.MethodOpt.ParameterCount == 2);

                if (warnOnNullReferenceArgument)
                {
                    ReportArgumentWarnings(binary.Left, leftType, binary.MethodOpt.Parameters[0]);
                }

                VisitRvalue(binary.Right);
                Debug.Assert(!IsConditionalState);

                // At this point, State.Reachable may be false for
                // invalid code such as `s + throw new Exception()`.
                TypeSymbolWithAnnotations rightType = _result.Type;

                if (warnOnNullReferenceArgument)
                {
                    ReportArgumentWarnings(binary.Right, rightType, binary.MethodOpt.Parameters[1]);
                }

                Debug.Assert(!IsConditionalState);
                _result = InferResultNullability(binary, leftType, rightType);

                BinaryOperatorKind op = binary.OperatorKind.Operator();
                if (op == BinaryOperatorKind.Equal || op == BinaryOperatorKind.NotEqual)
                {
                    BoundExpression operandComparedToNull = null;
                    TypeSymbolWithAnnotations operandComparedToNullType = null;

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
                        // PROTOTYPE(NullableReferenceTypes): This check is incorrect since it compares declared
                        // nullability rather than tracked nullability. Moreover, we should only report such
                        // diagnostics for locals that are set or checked explicitly within this method.
                        if (operandComparedToNullType?.IsNullable == false)
                        {
                            ReportStaticNullCheckingDiagnostics(op == BinaryOperatorKind.Equal ?
                                                                    ErrorCode.HDN_NullCheckIsProbablyAlwaysFalse :
                                                                    ErrorCode.HDN_NullCheckIsProbablyAlwaysTrue,
                                                                binary.Syntax);
                        }

                        // Skip reference conversions
                        operandComparedToNull = SkipReferenceConversions(operandComparedToNull);

                        if (operandComparedToNull.Type?.IsReferenceType == true)
                        {
                            int slot = MakeSlot(operandComparedToNull);

                            if (slot > 0)
                            {
                                if (slot >= this.State.Capacity) Normalize(ref this.State);

                                Split();

                                if (op == BinaryOperatorKind.Equal)
                                {
                                    this.StateWhenFalse[slot] = true;
                                }
                                else
                                {
                                    this.StateWhenTrue[slot] = true;
                                }
                            }
                        }
                    }
                }
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

        public override BoundNode VisitNullCoalescingOperator(BoundNullCoalescingOperator node)
        {
            Debug.Assert(!IsConditionalState);

            BoundExpression leftOperand = node.LeftOperand;
            BoundExpression rightOperand = node.RightOperand;

            Result leftResult = VisitRvalueWithResult(leftOperand);
            if (IsConstantNull(leftOperand))
            {
                VisitRvalue(rightOperand);
                return null;
            }

            var leftState = this.State.Clone();
            if (leftResult.Type?.IsNullable == false)
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.HDN_ExpressionIsProbablyNeverNull, leftOperand.Syntax);
            }

            bool leftIsConstant = leftOperand.ConstantValue != null;
            if (leftIsConstant)
            {
                SetUnreachable();
            }

            // PROTOTYPE(NullableReferenceTypes): For cases where the left operand determines
            // the type, we should unwrap the right conversion and re-apply.
            var rightResult = VisitRvalueWithResult(rightOperand);
            IntersectWith(ref this.State, ref leftState);

            TypeSymbol resultType;
            switch (node.OperatorResultKind)
            {
                case BoundNullCoalescingOperatorResultKind.NoCommonType:
                    resultType = node.Type;
                    break;
                case BoundNullCoalescingOperatorResultKind.LeftType:
                    resultType = getLeftResultType(leftResult.Type.TypeSymbol, rightResult.Type?.TypeSymbol);
                    break;
                case BoundNullCoalescingOperatorResultKind.LeftUnwrappedType:
                    resultType = getLeftResultType(leftResult.Type.TypeSymbol.StrippedType(), rightResult.Type?.TypeSymbol);
                    break;
                case BoundNullCoalescingOperatorResultKind.RightType:
                    resultType = getRightResultType(leftResult.Type.TypeSymbol, rightResult.Type.TypeSymbol);
                    break;
                case BoundNullCoalescingOperatorResultKind.LeftUnwrappedRightType:
                    resultType = getRightResultType(leftResult.Type.TypeSymbol.StrippedType(), rightResult.Type.TypeSymbol);
                    break;
                case BoundNullCoalescingOperatorResultKind.RightDynamicType:
                    resultType = rightResult.Type.TypeSymbol;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.OperatorResultKind);
            }

            bool? resultIsNullable = getIsNullable(leftOperand, leftResult) == false ? false : getIsNullable(rightOperand, rightResult);
            _result = TypeSymbolWithAnnotations.Create(resultType, resultIsNullable);
            return null;

            bool? getIsNullable(BoundExpression e, Result r) => (r.Type is null) ? e.IsNullable() : r.Type.IsNullable;
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

        public override BoundNode VisitConditionalAccess(BoundConditionalAccess node)
        {
            Debug.Assert(!IsConditionalState);

            var receiver = node.Receiver;
            var receiverType = VisitRvalueWithResult(receiver).Type;

            var receiverState = this.State.Clone();

            if (receiver.Type?.IsReferenceType == true)
            {
                if (receiverType?.IsNullable == false)
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.HDN_ExpressionIsProbablyNeverNull, receiver.Syntax);
                }

                int slot = MakeSlot(SkipReferenceConversions(receiver));
                if (slot > 0)
                {
                    if (slot >= this.State.Capacity) Normalize(ref this.State);
                    this.State[slot] = true;
                }
            }

            if (IsConstantNull(node.Receiver))
            {
                SetUnreachable();
            }

            VisitRvalue(node.AccessExpression);
            IntersectWith(ref this.State, ref receiverState);

            // PROTOTYPE(NullableReferenceTypes): Use flow analysis type rather than node.Type
            // so that nested nullability is inferred from flow analysis. See VisitConditionalOperator.
            _result = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: receiverType?.IsNullable | _result.Type?.IsNullable);
            // PROTOTYPE(NullableReferenceTypes): Report conversion warnings.
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
            Result consequenceResult;
            Result alternativeResult;
            bool? isNullableIfReferenceType;

            if (IsConstantTrue(node.Condition))
            {
                (alternative, alternativeResult) = visitConditionalOperand(alternativeState, node.Alternative);
                (consequence, consequenceResult) = visitConditionalOperand(consequenceState, node.Consequence);
                isNullableIfReferenceType = getIsNullableIfReferenceType(consequence, consequenceResult);
            }
            else if (IsConstantFalse(node.Condition))
            {
                (consequence, consequenceResult) = visitConditionalOperand(consequenceState, node.Consequence);
                (alternative, alternativeResult) = visitConditionalOperand(alternativeState, node.Alternative);
                isNullableIfReferenceType = getIsNullableIfReferenceType(alternative, alternativeResult);
            }
            else
            {
                (consequence, consequenceResult) = visitConditionalOperand(consequenceState, node.Consequence);
                Unsplit();
                (alternative, alternativeResult) = visitConditionalOperand(alternativeState, node.Alternative);
                Unsplit();
                IntersectWith(ref this.State, ref consequenceState);
                isNullableIfReferenceType = (getIsNullableIfReferenceType(consequence, consequenceResult) | getIsNullableIfReferenceType(alternative, alternativeResult));
            }

            TypeSymbolWithAnnotations resultType;
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
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                resultType = BestTypeInferrer.InferBestTypeForConditionalOperator(
                    createPlaceholderIfNecessary(consequence, consequenceResult),
                    createPlaceholderIfNecessary(alternative, alternativeResult),
                    _conversions,
                    out _,
                    ref useSiteDiagnostics);
                if (resultType is null)
                {
                    ReportStaticNullCheckingDiagnostics(
                        ErrorCode.WRN_NoBestNullabilityConditionalExpression,
                        node.Syntax,
                        GetTypeAsDiagnosticArgument(consequenceResult.Type?.TypeSymbol),
                        GetTypeAsDiagnosticArgument(alternativeResult.Type?.TypeSymbol));
                }
            }
            resultType = TypeSymbolWithAnnotations.Create(resultType?.TypeSymbol ?? node.Type, isNullableIfReferenceType);

            _result = resultType;
            return null;

            bool? getIsNullableIfReferenceType(BoundExpression expr, Result result)
            {
                var type = result.Type;
                if ((object)type != null)
                {
                    return type.IsNullable;
                }
                if (expr.IsLiteralNullOrDefault())
                {
                    return true;
                }
                return null;
            }

            BoundExpression createPlaceholderIfNecessary(BoundExpression expr, Result result)
            {
                var type = result.Type;
                return type is null ?
                    expr :
                    new BoundValuePlaceholder(expr.Syntax, type.IsNullable, type.TypeSymbol);
            }

            (BoundExpression, Result) visitConditionalOperand(LocalState state, BoundExpression operand)
            {
                SetState(state);
                if (isByRef)
                {
                    VisitLvalue(operand);
                }
                else
                {
                    operand = RemoveImplicitConversions(operand);
                    Visit(operand);
                }
                return (operand, _result);
            }
        }

        public override BoundNode VisitConditionalReceiver(BoundConditionalReceiver node)
        {
            var result = base.VisitConditionalReceiver(node);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            var method = node.Method;

            if (method.CallsAreOmitted(node.SyntaxTree))
            {
                // PROTOTYPE(NullableReferenceTypes): Should skip state set in
                // arguments of omitted call. See PreciseAbstractFlowPass.VisitCall.
            }

            var receiverOpt = node.ReceiverOpt;
            if (receiverOpt != null && method.MethodKind != MethodKind.Constructor)
            {
                VisitRvalue(receiverOpt);
                CheckPossibleNullReceiver(receiverOpt);
                // PROTOTYPE(NullableReferenceTypes): Update method based on inferred receiver type.
            }

            // PROTOTYPE(NullableReferenceTypes): Can we handle some error cases?
            // (Compare with CSharpOperationFactory.CreateBoundCallOperation.)
            if (!node.HasErrors)
            {
                ImmutableArray<RefKind> refKindsOpt = node.ArgumentRefKindsOpt;
                ImmutableArray<BoundExpression> arguments = RemoveArgumentConversions(node.Arguments, refKindsOpt);
                ImmutableArray<Result> results = VisitArgumentsEvaluate(arguments, refKindsOpt, node.Expanded);
                if (method.IsGenericMethod && HasImplicitTypeArguments(node))
                {
                    method = InferMethod(node, method, results.SelectAsArray(r => r.Type));
                }
                VisitArgumentsWarn(arguments, refKindsOpt, method.Parameters, node.ArgsToParamsOpt, node.Expanded, results);
            }

            UpdateStateForCall(node);

            if (method.MethodKind == MethodKind.LocalFunction)
            {
                var localFunc = (LocalFunctionSymbol)method.OriginalDefinition;
                ReplayReadsAndWrites(localFunc, node.Syntax, writes: true);
            }

            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                _result = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(method);
            }

            return null;
        }

        // PROTOTYPE(NullableReferenceTypes): Record in the node whether type
        // arguments were implicit, to allow for cases where the syntax is not an
        // invocation (such as a synthesized call from a query interpretation).
        private static bool HasImplicitTypeArguments(BoundCall node)
        {
            var syntax = node.Syntax;
            if (syntax.Kind() != SyntaxKind.InvocationExpression)
            {
                // Unexpected syntax kind.
                return false;
            }
            var nameSyntax = Binder.GetNameSyntax(((InvocationExpressionSyntax)syntax).Expression, out var _);
            if (nameSyntax  == null)
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
            VisitArguments(node, arguments, refKindsOpt, method is null ? default : method.Parameters, argsToParamsOpt, expanded);
        }

        private void VisitArguments(
            BoundExpression node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            PropertySymbol property,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded)
        {
            VisitArguments(node, arguments, refKindsOpt, property is null ? default : property.Parameters, argsToParamsOpt, expanded);
        }

        private void VisitArguments(
            BoundExpression node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded)
        {
            Debug.Assert(!arguments.IsDefault);
            ImmutableArray<Result> results = VisitArgumentsEvaluate(arguments, refKindsOpt, expanded);
            // PROTOTYPE(NullableReferenceTypes): Can we handle some error cases?
            // (Compare with CSharpOperationFactory.CreateBoundCallOperation.)
            if (!node.HasErrors && !parameters.IsDefault)
            {
                VisitArgumentsWarn(arguments, refKindsOpt, parameters, argsToParamsOpt, expanded, results);
            }
        }

        private ImmutableArray<Result> VisitArgumentsEvaluate(
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            bool expanded)
        {
            Debug.Assert(!IsConditionalState);
            int n = arguments.Length;
            if (n == 0)
            {
                return ImmutableArray<Result>.Empty;
            }
            var builder = ArrayBuilder<Result>.GetInstance(n);
            for (int i = 0; i < n; i++)
            {
                RefKind refKind = GetRefKind(refKindsOpt, i);
                var argument = arguments[i];
                if (refKind != RefKind.Out)
                {
                    // PROTOTYPE(NullReferenceTypes): `ref` arguments should be treated as l-values
                    // for assignment. See `ref x3` in StaticNullChecking.PassingParameters_01.
                    VisitRvalue(argument);
                }
                else
                {
                    VisitLvalue(argument);
                }
                builder.Add(_result);
            }
            _result = _invalidType;
            return builder.ToImmutableAndFree();
        }

        private void VisitArgumentsWarn(
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded,
            ImmutableArray<Result> results)
        {
            for (int i = 0; i < arguments.Length; i++)
            {
                (ParameterSymbol parameter, TypeSymbolWithAnnotations parameterType) = GetCorrespondingParameter(i, parameters, argsToParamsOpt, expanded);
                if (parameter is null)
                {
                    continue;
                }
                VisitArgumentWarn(
                    arguments[i],
                    GetRefKind(refKindsOpt, i),
                    parameter,
                    parameterType,
                    results[i]);
            }
        }

        /// <summary>
        /// Report warnings for an argument corresponding to a specific parameter.
        /// </summary>
        private void VisitArgumentWarn(
            BoundExpression argument,
            RefKind refKind,
            ParameterSymbol parameter,
            TypeSymbolWithAnnotations parameterType,
            Result result)
        {
            TypeSymbolWithAnnotations resultType = result.Type;
            var argumentType = resultType?.TypeSymbol;
            switch (refKind)
            {
                case RefKind.None:
                case RefKind.In:
                    {
                        Conversion conversion = GenerateConversion(_conversions, argument, argumentType, parameterType.TypeSymbol);
                        resultType = InferResultNullability(argument, conversion, parameterType.TypeSymbol, resultType);
                        if (!ReportNullReferenceArgumentIfNecessary(argument, resultType, parameter, parameterType) &&
                            !conversion.Exists)
                        {
                            ReportNullabilityMismatchInArgument(argument, argumentType, parameter, parameterType.TypeSymbol);
                        }
                    }
                    break;
                case RefKind.Out:
                    if (!ReportNullReferenceAssignmentIfNecessary(argument, resultType, parameterType, useLegacyWarnings: UseLegacyWarnings(argument)))
                    {
                        HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                        if (!_conversions.HasIdentityOrImplicitReferenceConversion(parameterType.TypeSymbol, argumentType, ref useSiteDiagnostics))
                        {
                            ReportNullabilityMismatchInArgument(argument, argumentType, parameter, parameterType.TypeSymbol);
                        }
                    }
                    // Set nullable state of argument to parameter type.
                    TrackNullableStateForAssignment(argument, resultType, result.Slot, parameterType);
                    break;
                case RefKind.Ref:
                    if (!ReportNullReferenceArgumentIfNecessary(argument, resultType, parameter, parameterType) &&
                        !ReportNullReferenceAssignmentIfNecessary(argument, resultType, parameterType, useLegacyWarnings: UseLegacyWarnings(argument)))
                    {
                        if ((object)argumentType != null && IsNullabilityMismatch(argumentType, parameterType.TypeSymbol))
                        {
                            ReportNullabilityMismatchInArgument(argument, argumentType, parameter, parameterType.TypeSymbol);
                        }
                    }
                    // Set nullable state of argument to parameter type.
                    TrackNullableStateForAssignment(argument, resultType, result.Slot, parameterType);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(refKind);
            }
        }

        private static ImmutableArray<BoundExpression> RemoveArgumentConversions(ImmutableArray<BoundExpression> arguments, ImmutableArray<RefKind> refKindsOpt)
        {
            int n = arguments.Length;
            if (n > 0)
            {
                var argumentsBuilder = ArrayBuilder<BoundExpression>.GetInstance(n);
                bool includedConversion = false;
                for (int i = 0; i < n; i++)
                {
                    RefKind refKind = GetRefKind(refKindsOpt, i);
                    var argument = arguments[i];
                    if (refKind == RefKind.None)
                    {
                        argument = RemoveImplicitConversions(argument);
                        if (argument != arguments[i])
                        {
                            includedConversion = true;
                        }
                    }
                    argumentsBuilder.Add(argument);
                }
                if (includedConversion)
                {
                    arguments = argumentsBuilder.ToImmutable();
                }
                argumentsBuilder.Free();
            }
            return arguments;
        }

        private static (ParameterSymbol, TypeSymbolWithAnnotations) GetCorrespondingParameter(int argumentOrdinal, ImmutableArray<ParameterSymbol> parameters, ImmutableArray<int> argsToParamsOpt, bool expanded)
        {
            Debug.Assert(!parameters.IsDefault);

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
                return (null, null);
            }

            var type = parameter.Type;
            if (expanded && parameter.Ordinal == n - 1 && parameter.Type.IsSZArray())
            {
                type = ((ArrayTypeSymbol)type.TypeSymbol).ElementType;
            }

            return (parameter, type);
        }

        private MethodSymbol InferMethod(BoundCall node, MethodSymbol method, ImmutableArray<TypeSymbolWithAnnotations> argumentTypes)
        {
            Debug.Assert(method.IsGenericMethod);
            // PROTOTYPE(NullableReferenceTypes): OverloadResolution.IsMemberApplicableInNormalForm and
            // IsMemberApplicableInExpandedForm use the least overridden method. We need to do the same here.
            var definition = method.ConstructedFrom;
            // PROTOTYPE(NullableReferenceTypes): MethodTypeInferrer.Infer relies
            // on the BoundExpressions for tuple element types and method groups.
            // By using a generic BoundValuePlaceholder, we're losing inference in those cases.
            // PROTOTYPE(NullableReferenceTypes): Inference should be based on
            // unconverted arguments. Consider cases such as `default`, lambdas, tuples.
            ImmutableArray<BoundExpression> arguments = argumentTypes.ZipAsArray(node.Arguments, (t, a) => ((object)t == null) ? a : new BoundValuePlaceholder(a.Syntax, t.IsNullable, t.TypeSymbol));
            var refKinds = ArrayBuilder<RefKind>.GetInstance();
            if (node.ArgumentRefKindsOpt != null)
            {
                refKinds.AddRange(node.ArgumentRefKindsOpt);
            }
            OverloadResolution.GetEffectiveParameterTypes(
                definition,
                node.Arguments.Length,
                node.ArgsToParamsOpt,
                refKinds,
                isMethodGroupConversion: false,
                // PROTOTYPE(NullableReferenceTypes): `allowRefOmittedArguments` should be
                // false for constructors and several other cases (see Binder use). Should we
                // capture the original value in the BoundCall?
                allowRefOmittedArguments: true,
                binder: _binder,
                expanded: node.Expanded,
                parameterTypes: out ImmutableArray<TypeSymbolWithAnnotations> parameterTypes,
                parameterRefKinds: out ImmutableArray<RefKind> parameterRefKinds);
            refKinds.Free();
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var result = MethodTypeInferrer.Infer(
                _binder,
                _conversions,
                definition.TypeParameters,
                definition.ContainingType,
                parameterTypes,
                parameterRefKinds,
                arguments,
                ref useSiteDiagnostics);
            if (result.Success)
            {
                // PROTOTYPE(NullableReferenceTypes): Report conversion warnings.
                return definition.Construct(result.InferredTypeArguments);
            }
            return method;
        }

        private void ReplayReadsAndWrites(LocalFunctionSymbol localFunc,
                                  SyntaxNode syntax,
                                  bool writes)
        {
            // PROTOTYPE(NullableReferenceTypes): Support field initializers in local functions.
        }

        private static BoundExpression RemoveImplicitConversions(BoundExpression expr)
        {
            // PROTOTYPE(NullableReferenceTypes): The loop is necessary to handle
            // implicit conversions that have multiple parts (for instance, user-defined
            // conversions). Should check for those cases explicitly.
            while (true)
            {
                if (expr.Kind != BoundKind.Conversion)
                {
                    break;
                }
                var conversion = (BoundConversion)expr;
                if (conversion.ExplicitCastInCode || !conversion.Conversion.Exists)
                {
                    break;
                }
                expr = conversion.Operand;
            }
            return expr;
        }

        // See Binder.BindNullCoalescingOperator for initial binding.
        private Conversion GenerateConversionForConditionalOperator(BoundExpression sourceExpression, TypeSymbol sourceType, TypeSymbol destinationType, bool reportMismatch)
        {
            var conversion = GenerateConversion(_conversions, sourceExpression, sourceType, destinationType);
            bool canConvert = conversion.Exists;
            if (!canConvert && reportMismatch)
            {
                Debug.Assert(GenerateConversion(_conversions.WithNullability(false), sourceExpression, sourceType, destinationType).Exists);
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInAssignment, sourceExpression.Syntax, GetTypeAsDiagnosticArgument(sourceType), destinationType);
            }
            return conversion;
        }

        private static Conversion GenerateConversion(Conversions conversions, BoundExpression sourceExpression, TypeSymbol sourceType, TypeSymbol destinationType)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            return useExpressionForConversion(sourceExpression) ?
                conversions.ClassifyImplicitConversionFromExpression(sourceExpression, destinationType, ref useSiteDiagnostics) :
                conversions.ClassifyImplicitConversionFromType(sourceType, destinationType, ref useSiteDiagnostics);

            bool useExpressionForConversion(BoundExpression value) => value.Type is null || value.ConstantValue != null;
        }

        /// <summary>
        /// Adjust declared type based on inferred nullability at the point of reference.
        /// </summary>
        private Result GetAdjustedResult(Result pair)
        {
            var type = pair.Type;
            var slot = pair.Slot;
            if (slot > 0 && slot < this.State.Capacity)
            {
                bool? isNullable = !this.State[slot];
                if (isNullable != type.IsNullable)
                {
                    return Result.Create(TypeSymbolWithAnnotations.Create(type.TypeSymbol, isNullable), slot);
                }
            }
            return pair;
        }

        private Symbol AsMemberOfResultType(Symbol symbol)
        {
            var containingType = _result.Type?.TypeSymbol as NamedTypeSymbol;
            if ((object)containingType == null || containingType.IsErrorType())
            {
                return symbol;
            }
            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                    {
                        var field = (FieldSymbol)symbol;
                        var index = field.TupleElementIndex;
                        if (index >= 0)
                        {
                            // PROTOTYPE(NullableReferenceTypes): Handle other members of
                            // tuple type (such as TuplePropertySymbol), perhaps using
                            // TupleTypeSymbol.GetTupleMemberSymbolForUnderlyingMember
                            return containingType.TupleElements[index];
                        }
                    }
                    break;
                case SymbolKind.Property:
                case SymbolKind.Event:
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
            var symbolDef = symbol.OriginalDefinition;
            var symbolDefContainer = symbolDef.ContainingType;
            while (true)
            {
                if (containingType.OriginalDefinition.Equals(symbolDefContainer, TypeCompareKind.ConsiderEverything))
                {
                    return symbolDef.SymbolAsMember(containingType);
                }
                containingType = containingType.BaseTypeNoUseSiteDiagnostics;
                if ((object)containingType == null)
                {
                    break;
                }
            }
            // PROTOTYPE(NullableReferenceTypes): Handle other cases such as interfaces.
            Debug.Assert(symbolDefContainer.IsInterface);
            return symbol;
        }

        public override BoundNode VisitConversion(BoundConversion node)
        {
            if (node.ConversionKind == ConversionKind.MethodGroup
                && node.SymbolOpt?.MethodKind == MethodKind.LocalFunction)
            {
                var localFunc = (LocalFunctionSymbol)node.SymbolOpt.OriginalDefinition;
                var syntax = node.Syntax;
                ReplayReadsAndWrites(localFunc, syntax, writes: false);
            }

            var operand = node.Operand;
            Visit(operand);
            var operandType = _result.Type;
            var targetType = node.Type;

            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                switch (node.ConversionKind)
                {
                    case ConversionKind.ExplicitUserDefined:
                    case ConversionKind.ImplicitUserDefined:
                        if ((object)node.SymbolOpt != null && node.SymbolOpt.ParameterCount == 1)
                        {
                            ReportArgumentWarnings(operand, operandType, node.SymbolOpt.Parameters[0]);
                        }
                        break;

                    case ConversionKind.AnonymousFunction:
                        if (!node.ExplicitCastInCode && operand.Kind == BoundKind.Lambda)
                        {
                            var lambda = (BoundLambda)operand;
                            ReportNullabilityMismatchWithTargetDelegate(operand.Syntax, targetType.GetDelegateType(), lambda.Symbol);
                        }
                        break;

                    case ConversionKind.MethodGroup:
                        if (!node.ExplicitCastInCode)
                        {
                            ReportNullabilityMismatchWithTargetDelegate(operand.Syntax, targetType.GetDelegateType(), node.SymbolOpt);
                        }
                        break;
                }

                if (node.ExplicitCastInCode && !node.IsExplicitlyNullable)
                {
                    bool reportNullable = false;
                    if (targetType.IsReferenceType && IsUnconstrainedTypeParameter(operandType?.TypeSymbol))
                    {
                        reportNullable = true;
                    }
                    else if ((targetType.IsReferenceType || IsUnconstrainedTypeParameter(targetType)) &&
                        (operandType?.IsNullable == true || (operandType is null && operand.IsLiteralNullOrDefault())))
                    {
                        reportNullable = true;
                    }
                    if (reportNullable)
                    {
                        // PROTOTYPE(NullableReferenceTypes): Should not report warning for explicit
                        // user-defined conversion if the operator is defined from nullable to
                        // non-nullable. See StaticNullChecking.ExplicitCast_UserDefined.
                        ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_ConvertingNullableToNonNullable, node.Syntax);
                    }
                }

                TypeSymbolWithAnnotations resultType;
                if (operand.Kind == BoundKind.Literal && (object)operand.Type == null && operand.ConstantValue.IsNull)
                {
                    resultType = TypeSymbolWithAnnotations.Create(targetType, true);
                }
                else if (node.ConversionKind == ConversionKind.Identity && !node.ExplicitCastInCode)
                {
                    resultType = operandType;
                }
                else
                {
                    resultType = InferResultNullability(operand, node.Conversion, targetType, operandType, fromConversionNode: true);
                }
                _result = resultType;
            }

            return null;
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
            ImmutableArray<TypeSymbolWithAnnotations> elementTypes = arguments.SelectAsArray((a, w) => w.VisitRvalueWithResult(a).Type, this);
            var tupleOpt = (TupleTypeSymbol)node.Type;
            _result = (tupleOpt is null) ?
                null :
                TypeSymbolWithAnnotations.Create(tupleOpt.WithElementTypes(elementTypes));
        }

        private void ReportNullabilityMismatchWithTargetDelegate(SyntaxNode syntax, NamedTypeSymbol delegateType, MethodSymbol method)
        {
            if ((object)delegateType == null || (object)method == null)
            {
                return;
            }

            MethodSymbol invoke = delegateType.DelegateInvokeMethod;

            if ((object)invoke == null)
            {
                return;
            }

            if (IsNullabilityMismatch(invoke.ReturnType, method.ReturnType))
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, syntax,
                    new FormattedSymbol(method, SymbolDisplayFormat.MinimallyQualifiedFormat),
                    delegateType);
            }

            int count = Math.Min(invoke.ParameterCount, method.ParameterCount);

            for (int i = 0; i < count; i++)
            {
                if (IsNullabilityMismatch(invoke.Parameters[i].Type, method.Parameters[i].Type))
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, syntax,
                        new FormattedSymbol(method.Parameters[i], SymbolDisplayFormat.ShortFormat),
                        new FormattedSymbol(method, SymbolDisplayFormat.MinimallyQualifiedFormat),
                        delegateType);
                }
            }
        }

        private static TypeSymbolWithAnnotations InferResultNullability(BoundExpression operand, Conversion conversion, TypeSymbol targetType, TypeSymbolWithAnnotations operandType, bool fromConversionNode = false)
        {
            bool? isNullableIfReferenceType = null;

            switch (conversion.Kind)
            {
                case ConversionKind.MethodGroup:
                case ConversionKind.AnonymousFunction:
                case ConversionKind.InterpolatedString:
                    isNullableIfReferenceType = false;
                    break;

                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.ImplicitUserDefined:
                    var methodOpt = conversion.Method;
                    if ((object)methodOpt != null && methodOpt.ParameterCount == 1)
                    {
                        if (conversion.BestUserDefinedConversionAnalysis.Kind == UserDefinedConversionAnalysisKind.ApplicableInLiftedForm)
                        {
                            // PROTOTYPE(NullableReferenceTypes): Conversions: Lifted operator
                            break;
                        }
                        // PROTOTYPE(NullableReferenceTypes): Update method based on operandType.
                        var resultType = methodOpt.ReturnType;
                        // Ensure converted type matches expected target type, specifically for lifted
                        // conversions. See LocalRewriter.MakeConversionNode for similar handling
                        // based on conversion.BestUserDefinedConversionAnalysis.ToType.
                        if (!fromConversionNode)
                        {
                            if (resultType.TypeSymbol != targetType)
                            {
                                // PROTOTYPE(NullableReferenceTypes): Should handle nested nullability
                                // and top-level nullability. Conversion from resultType to targetType
                                // should be treated as a built-in conversion.
                                resultType = TypeSymbolWithAnnotations.Create(targetType);
                            }
                        }
                        return resultType;
                    }
                    break;

                case ConversionKind.Unboxing:
                case ConversionKind.ExplicitDynamic:
                case ConversionKind.ImplicitDynamic:
                case ConversionKind.NoConversion:
                case ConversionKind.ImplicitThrow:
                    break;

                case ConversionKind.Boxing:
                    if (operandType?.IsValueType == true)
                    {
                        // PROTOTYPE(NullableReferenceTypes): Should we worry about a pathological case of boxing nullable value known to be not null?
                        //       For example, new int?(0)
                        isNullableIfReferenceType = operandType.IsNullableType();
                    }
                    else if (IsUnconstrainedTypeParameter(operandType?.TypeSymbol))
                    {
                        isNullableIfReferenceType = true;
                    }
                    else
                    {
                        Debug.Assert(operandType?.IsReferenceType != true ||
                            operandType.SpecialType == SpecialType.System_ValueType ||
                            operandType.TypeKind == TypeKind.Interface ||
                            operandType.TypeKind == TypeKind.Dynamic);
                    }
                    break;

                case ConversionKind.Identity:
                case ConversionKind.DefaultOrNullLiteral:
                case ConversionKind.ImplicitReference:
                case ConversionKind.ExplicitReference:
                    // Inherit state from the operand
                    // PROTOTYPE(NullableReferenceTypes): Should an explicit cast cast away
                    // outermost nullability? For instance, is `s` a `string!` or `string?`?
                    // object? obj = ...; var s = (string)obj;
                    isNullableIfReferenceType = (operandType is null) ? operand.IsLiteralNullOrDefault() : operandType.IsNullable;
                    break;

                case ConversionKind.Deconstruction:
                    // Can reach here, with an error type, when the
                    // Deconstruct method is missing or inaccessible.
                    break;

                case ConversionKind.ExplicitEnumeration:
                    // Can reach here, with an error type.
                    break;

                default:
                    Debug.Assert(targetType?.IsReferenceType != true);
                    break;
            }

            // PROTOTYPE(NullableReferenceTypes): Include nested nullability?
            return TypeSymbolWithAnnotations.Create(targetType, isNullableIfReferenceType);
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
            SetResult(node);
            return null;
        }

        public override BoundNode VisitMethodGroup(BoundMethodGroup node)
        {
            Debug.Assert(!IsConditionalState);

            BoundExpression receiverOpt = node.ReceiverOpt;
            if (receiverOpt != null)
            {
                // An explicit or implicit receiver, for example in an expression such as (x.Foo is Action, or Foo is Action), is considered to be read.
                VisitRvalue(receiverOpt);

                CheckPossibleNullReceiver(receiverOpt);
            }

            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                _result = null;
            }

            return null;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            var result = VisitLambdaOrLocalFunction(node);
            SetResult(node); // PROTOTYPE(NullableReferenceTypes): Conversions: Lamba
            return result;
        }

        public override BoundNode VisitUnboundLambda(UnboundLambda node)
        {
            var result = base.VisitUnboundLambda(node);
            SetResult(node);
            return result;
        }

        private BoundNode VisitLambdaOrLocalFunction(IBoundLambdaOrFunction node)
        {
            var oldMethodOrLambda = this._currentMethodOrLambda;
            this._currentMethodOrLambda = node.Symbol;

            var oldPending = SavePending(); // we do not support branches into a lambda
            LocalState finalState = this.State;
            this.State = this.State.Reachable ? this.State.Clone() : AllBitsSet();
            if (!node.WasCompilerGenerated) EnterParameters(node.Symbol.Parameters);
            var oldPending2 = SavePending();
            VisitAlways(node.Body);
            RestorePending(oldPending2); // process any forward branches within the lambda body
            ImmutableArray<PendingBranch> pendingReturns = RemoveReturns();
            RestorePending(oldPending);
            IntersectWith(ref finalState, ref this.State); // a no-op except in region analysis
            _result = _invalidType;
            foreach (PendingBranch pending in pendingReturns)
            {
                this.State = pending.State;
                IntersectWith(ref finalState, ref this.State); // a no-op except in region analysis
                _result = _invalidType;
            }

            this.State = finalState;

            this._currentMethodOrLambda = oldMethodOrLambda;
            return null;
        }

        public override BoundNode VisitThisReference(BoundThisReference node)
        {
            VisitThisOrBaseReference(node);
            return null;
        }

        private void VisitThisOrBaseReference(BoundExpression node)
        {
            var thisParameter = MethodThisParameter;
            int slot = (object)thisParameter == null ? -1 : GetOrCreateSlot(thisParameter);
            _result = Result.Create(TypeSymbolWithAnnotations.Create(node.Type), slot);
        }

        public override BoundNode VisitParameter(BoundParameter node)
        {
            _result = GetAdjustedResult(GetDeclaredParameterResult(node.ParameterSymbol));
            return null;
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            Debug.Assert(!IsConditionalState);

            VisitLvalue(node.Left);
            Result left = _result;

            VisitRvalue(node.Right);
            Result right = _result;

            // byref assignment is also a potential write
            if (node.IsRef)
            {
                WriteArgument(node.Right, node.Left.GetRefKind(), method: null);
            }

            if (node.Left.Kind == BoundKind.EventAccess && ((BoundEventAccess)node.Left).EventSymbol.IsWindowsRuntimeEvent)
            {
                // Event assignment is a call to an Add method. (Note that assignment
                // of non-field-like events uses BoundEventAssignmentOperator
                // rather than BoundAssignmentOperator.)
                SetResult(node);
            }
            else
            {
                ReportAssignmentWarnings(node.Right, left.Type, right.Type, useLegacyWarnings: UseLegacyWarnings(node.Left));
                TrackNullableStateForAssignment(node.Right, left.Type, left.Slot, right.Type, right.Slot);
                // PROTOTYPE(NullableReferenceTypes): Check node.Type.IsErrorType() instead?
                _result = node.HasErrors ? Result.Create(TypeSymbolWithAnnotations.Create(node.Type)) : left;
            }

            return null;
        }

        private static bool UseLegacyWarnings(BoundExpression expr)
        {
            switch (expr.Kind)
            {
                case BoundKind.Local:
                case BoundKind.Parameter:
                    // PROTOTYPE(NullableReferenceTypes): Warnings when assigning to `ref`
                    // or `out` parameters should be regular warnings. Warnings assigning to
                    // other parameters should be W warnings.
                    return true;
                default:
                    return false;
            }
        }

        public override BoundNode VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            base.VisitDeconstructionAssignmentOperator(node);
            // PROTOTYPE(NullableReferenceTypes): Assign each of the deconstructed values.
            SetResult(node);
            return null;
        }

        public override BoundNode VisitIncrementOperator(BoundIncrementOperator node)
        {
            Debug.Assert(!IsConditionalState);

            VisitRvalue(node.Operand);
            var operandResult = _result;
            bool setResult = false;

            if (this.State.Reachable)
            {
                // PROTOTYPE(NullableReferenceTypes): Update increment method based on operand type.
                MethodSymbol incrementOperator = (node.OperatorKind.IsUserDefined() && (object)node.MethodOpt != null && node.MethodOpt.ParameterCount == 1) ? node.MethodOpt : null;
                TypeSymbol targetTypeOfOperandConversion;

                // PROTOTYPE(NullableReferenceTypes): Update conversion method based on operand type.
                if (node.OperandConversion.IsUserDefined && (object)node.OperandConversion.Method != null && node.OperandConversion.Method.ParameterCount == 1)
                {
                    ReportArgumentWarnings(node.Operand, operandResult.Type, node.OperandConversion.Method.Parameters[0]);
                    targetTypeOfOperandConversion = node.OperandConversion.Method.ReturnType.TypeSymbol;
                }
                else if ((object)incrementOperator != null)
                {
                    targetTypeOfOperandConversion = incrementOperator.Parameters[0].Type.TypeSymbol;
                }
                else
                {
                    // Either a built-in increment, or an error case.
                    targetTypeOfOperandConversion = null;
                }

                TypeSymbolWithAnnotations resultOfOperandConversionType;

                if ((object)targetTypeOfOperandConversion != null)
                {
                    // PROTOTYPE(NullableReferenceTypes): Should something special be done for targetTypeOfOperandConversion for lifted case?
                    resultOfOperandConversionType = InferResultNullability(node.Operand, node.OperandConversion, targetTypeOfOperandConversion, operandResult.Type);
                }
                else
                {
                    resultOfOperandConversionType = operandResult.Type;
                }

                TypeSymbolWithAnnotations resultOfIncrementType;
                if ((object)incrementOperator == null)
                {
                    resultOfIncrementType = resultOfOperandConversionType;
                }
                else
                {
                    ReportArgumentWarnings(node.Operand, resultOfOperandConversionType, incrementOperator.Parameters[0]);

                    resultOfIncrementType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(incrementOperator);
                }

                if (node.ResultConversion.IsUserDefined && (object)node.ResultConversion.Method != null && node.ResultConversion.Method.ParameterCount == 1)
                {
                    ReportArgumentWarnings(node, resultOfIncrementType, node.ResultConversion.Method.Parameters[0]);
                }

                resultOfIncrementType = InferResultNullability(node, node.ResultConversion, node.Type, resultOfIncrementType);

                // PROTOTYPE(NullableReferenceTypes): Check node.Type.IsErrorType() instead?
                if (!node.HasErrors)
                {
                    var op = node.OperatorKind.Operator();
                    _result = (op == UnaryOperatorKind.PrefixIncrement || op == UnaryOperatorKind.PrefixDecrement) ? resultOfIncrementType : operandResult;
                    setResult = true;

                    ReportAssignmentWarnings(node, operandResult.Type, valueType: resultOfIncrementType, useLegacyWarnings: false);
                    TrackNullableStateForAssignment(node, operandResult.Type, operandResult.Slot, valueType: resultOfIncrementType);
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
            VisitLvalue(node.Left); // PROTOTYPE(NullableReferenceTypes): Method should be called VisitValue rather than VisitLvalue.
            Result left = _result;

            TypeSymbolWithAnnotations resultType;
            Debug.Assert(!IsConditionalState);

            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                Result leftOnRight = GetAdjustedResult(left);

                // PROTOTYPE(NullableReferenceTypes): Update conversion method based on inferred operand type.
                if (node.LeftConversion.IsUserDefined && (object)node.LeftConversion.Method != null && node.LeftConversion.Method.ParameterCount == 1)
                {
                    ReportArgumentWarnings(node.Left, leftOnRight.Type, node.LeftConversion.Method.Parameters[0]);
                }

                TypeSymbolWithAnnotations leftOnRightType;

                // PROTOTYPE(NullableReferenceTypes): Update operator based on inferred argument types.
                if ((object)node.Operator.LeftType != null)
                {
                    leftOnRightType = InferResultNullability(node.Left, node.LeftConversion, node.Operator.LeftType, leftOnRight.Type);
                }
                else
                {
                    leftOnRightType = null;
                }

                VisitRvalue(node.Right);
                TypeSymbolWithAnnotations rightType = _result.Type;

                if ((object)node.Operator.ReturnType != null)
                {
                    if (node.Operator.Kind.IsUserDefined() && (object)node.Operator.Method != null && node.Operator.Method.ParameterCount == 2)
                    {
                        ReportArgumentWarnings(node.Left, leftOnRightType, node.Operator.Method.Parameters[0]);
                        ReportArgumentWarnings(node.Right, rightType, node.Operator.Method.Parameters[1]);
                    }

                    resultType = InferResultNullability(node.Operator.Kind, node.Operator.Method, node.Operator.ReturnType, leftOnRightType, rightType);

                    // PROTOTYPE(NullableReferenceTypes): Update final conversion based on inferred operand type.
                    if (node.FinalConversion.IsUserDefined && (object)node.FinalConversion.Method != null && node.FinalConversion.Method.ParameterCount == 1)
                    {
                        ReportArgumentWarnings(node, resultType, node.FinalConversion.Method.Parameters[0]);
                    }

                    resultType = InferResultNullability(node, node.FinalConversion, node.Type, resultType);
                }
                else
                {
                    resultType = null;
                }

                ReportAssignmentWarnings(node, left.Type, resultType, useLegacyWarnings: false);
                TrackNullableStateForAssignment(node, left.Type, left.Slot, resultType);
                _result = resultType;
            }
            //else
            //{
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

        /// <summary>
        /// Report warning passing nullable argument to non-nullable parameter
        /// (e.g.: calling `void F(string s)` with `F(maybeNull)`).
        /// </summary>
        private bool ReportNullReferenceArgumentIfNecessary(BoundExpression argument, TypeSymbolWithAnnotations argumentType, ParameterSymbol parameter, TypeSymbolWithAnnotations paramType)
        {
            if (argumentType?.IsNullable == true)
            {
                if (paramType.IsReferenceType && paramType.IsNullable == false)
                {
                    if (!ReportNullAsNonNullableReferenceIfNecessary(argument))
                    {
                        ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceArgument, argument.Syntax,
                            new FormattedSymbol(parameter, SymbolDisplayFormat.ShortFormat),
                            new FormattedSymbol(parameter.ContainingSymbol, SymbolDisplayFormat.MinimallyQualifiedFormat));
                    }
                    return true;
                }
            }
            return false;
        }

        private void ReportArgumentWarnings(BoundExpression argument, TypeSymbolWithAnnotations argumentType, ParameterSymbol parameter)
        {
            var paramType = parameter.Type;

            ReportNullReferenceArgumentIfNecessary(argument, argumentType, parameter, paramType);

            if ((object)argumentType != null && IsNullabilityMismatch(paramType.TypeSymbol, argumentType.TypeSymbol))
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
            ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInArgument, argument.Syntax, argumentType, parameterType,
                new FormattedSymbol(parameter, SymbolDisplayFormat.ShortFormat),
                new FormattedSymbol(parameter.ContainingSymbol, SymbolDisplayFormat.MinimallyQualifiedFormat));
        }

        // PROTOTYPE(NullableReferenceTypes): If support for [NullableOptOut] or [NullableOptOutForAssembly]
        // is re-enabled, we'll need to call this helper for method symbols before inferring nullability of
        // arguments to avoid warnings when nullability checking of the method is suppressed.
        // (See all uses of this helper for method symbols.)
        private TypeSymbolWithAnnotations GetTypeOrReturnTypeWithAdjustedNullableAnnotations(Symbol symbol)
        {
            Debug.Assert(symbol.Kind != SymbolKind.Local); // Handled in VisitLocal.
            Debug.Assert(symbol.Kind != SymbolKind.Parameter); // Handled in VisitParameter.

            return compilation.GetTypeOrReturnTypeWithAdjustedNullableAnnotations(symbol);
        }

        private Result GetDeclaredLocalResult(LocalSymbol local)
        {
            var slot = GetOrCreateSlot(local);
            TypeSymbolWithAnnotations type;
            if (!_variableTypes.TryGetValue(local, out type))
            {
                type = local.Type;
            }
            return Result.Create(type, slot);
        }

        private Result GetDeclaredParameterResult(ParameterSymbol parameter)
        {
            var slot = GetOrCreateSlot(parameter);
            return Result.Create(parameter.Type, slot);
        }

        public override BoundNode VisitBaseReference(BoundBaseReference node)
        {
            VisitThisOrBaseReference(node);
            return null;
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            VisitMemberAccess(node.ReceiverOpt, node.FieldSymbol, asLvalue: false);
            return null;
        }

        public override BoundNode VisitPropertyAccess(BoundPropertyAccess node)
        {
            VisitMemberAccess(node.ReceiverOpt, node.PropertySymbol, asLvalue: false);
            return null;
        }

        public override BoundNode VisitIndexerAccess(BoundIndexerAccess node)
        {
            var receiverOpt = node.ReceiverOpt;
            VisitRvalue(receiverOpt);
            CheckPossibleNullReceiver(receiverOpt);

            // PROTOTYPE(NullableReferenceTypes): Update indexer based on inferred receiver type.
            VisitArguments(node, node.Arguments, node.ArgumentRefKindsOpt, node.Indexer, node.ArgsToParamsOpt, node.Expanded);

            _result = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(node.Indexer);
            return null;
        }

        public override BoundNode VisitEventAccess(BoundEventAccess node)
        {
            VisitMemberAccess(node.ReceiverOpt, node.EventSymbol, asLvalue: false);
            return null;
        }

        private void VisitMemberAccess(BoundExpression receiverOpt, Symbol member, bool asLvalue)
        {
            Debug.Assert(!IsConditionalState);

            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                Result receiverResult = VisitRvalueWithResult(receiverOpt);

                if (!member.IsStatic)
                {
                    member = AsMemberOfResultType(member);
                    CheckPossibleNullReceiver(receiverOpt);
                }

                int containingSlot = receiverResult.Slot;
                int slot = (containingSlot < 0) ? -1 : GetOrCreateSlot(member, containingSlot);
                var resultType = member.GetTypeOrReturnType();

                if (!asLvalue)
                {
                    // We are supposed to track information for the node. Use whatever we managed to
                    // accumulate so far.
                    if (resultType.IsReferenceType && slot > 0 && slot < this.State.Capacity)
                    {
                        var isNullable = !this.State[slot];
                        if (isNullable != resultType.IsNullable)
                        {
                            resultType = TypeSymbolWithAnnotations.Create(resultType.TypeSymbol, isNullable);
                        }
                    }
                }

                _result = Result.Create(resultType, slot);
            }
        }

        public override void VisitForEachIterationVariables(BoundForEachStatement node)
        {
            // declare and assign all iteration variables
            foreach (var iterationVariable in node.IterationVariables)
            {
                // PROTOTYPE(NullableReferenceTypes): Mark as assigned.
            }
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
            _result = TypeSymbolWithAnnotations.Create(node.Type);
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

            // PROTOTYPE(NullableReferenceTypes): Update method based on inferred operand type.
            if (node.OperatorKind.IsUserDefined() && (object)node.MethodOpt != null && node.MethodOpt.ParameterCount == 1)
            {
                ReportArgumentWarnings(node.Operand, _result.Type, node.MethodOpt.Parameters[0]);
            }

            _result = InferResultNullability(node);
            return null;
        }

        private TypeSymbolWithAnnotations InferResultNullability(BoundUnaryOperator node)
        {
            if (node.OperatorKind.IsUserDefined())
            {
                if (node.OperatorKind.IsLifted())
                {
                    // PROTOTYPE(NullableReferenceTypes): Conversions: Lifted operator
                    return TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: null);
                }
                // PROTOTYPE(NullableReferenceTypes): Update method based on inferred operand type.
                if ((object)node.MethodOpt != null && node.MethodOpt.ParameterCount == 1)
                {
                    return GetTypeOrReturnTypeWithAdjustedNullableAnnotations(node.MethodOpt);
                }
                else
                {
                    return null;
                }
            }
            else if (node.OperatorKind.IsDynamic())
            {
                return null;
            }
            else
            {
                return TypeSymbolWithAnnotations.Create(node.Type);
            }
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
                // PROTOTYPE(NullableReferenceTypes): Conversions: Lifted operator
                return TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: null);
            }
            // PROTOTYPE(NullableReferenceTypes): Update method based on inferred operand types.
            if ((object)node.LogicalOperator != null && node.LogicalOperator.ParameterCount == 2)
            {
                return GetTypeOrReturnTypeWithAdjustedNullableAnnotations(node.LogicalOperator);
            }
            else
            {
                return null;
            }
        }

        protected override void AfterLeftChildOfBinaryLogicalOperatorHasBeenVisited(BoundExpression node, BoundExpression right, bool isAnd, bool isBool, ref LocalState leftTrue, ref LocalState leftFalse)
        {
            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                TypeSymbolWithAnnotations leftType = _result.Type;
                // PROTOTYPE(NullableReferenceTypes): Update operator methods based on inferred operand types.
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
                TypeSymbolWithAnnotations rightType = _result.Type;

                _result = InferResultNullabilityOfBinaryLogicalOperator(node, leftType, rightType);

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
            if (!node.Type.IsReferenceType || node.HasErrors || (object)node.GetResult == null)
            {
                SetResult(node);
            }
            else
            {
                // PROTOTYPE(NullableReferenceTypes): Update method based on inferred receiver type.
                _result = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(node.GetResult);
            }
            return result;
        }

        public override BoundNode VisitTypeOfOperator(BoundTypeOfOperator node)
        {
            var result = base.VisitTypeOfOperator(node);
            SetResult(node);
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
            _result = (object)node.Type == null ?
                null :
                TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: true);
            return result;
        }

        public override BoundNode VisitIsOperator(BoundIsOperator node)
        {
            var result = base.VisitIsOperator(node);
            Debug.Assert(node.Type.SpecialType == SpecialType.System_Boolean);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitAsOperator(BoundAsOperator node)
        {
            var result = base.VisitAsOperator(node);

            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                bool? isNullable = null;
                if (node.Type.IsReferenceType)
                {
                    switch (node.Conversion.Kind)
                    {
                        case ConversionKind.Identity:
                        case ConversionKind.ImplicitReference:
                            // Inherit nullability from the operand
                            isNullable = _result.Type?.IsNullable;
                            break;

                        case ConversionKind.Boxing:
                            var operandType = node.Operand.Type;
                            if (operandType?.IsValueType == true)
                            {
                                // PROTOTYPE(NullableReferenceTypes): Should we worry about a pathological case of boxing nullable value known to be not null?
                                //       For example, new int?(0)
                                isNullable = operandType.IsNullableType();
                            }
                            else
                            {
                                Debug.Assert(operandType?.IsReferenceType != true);
                                isNullable = true;
                            }
                            break;

                        default:
                            isNullable = true;
                            break;
                    }
                }
                _result = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: isNullable);
            }

            return result;
        }

        public override BoundNode VisitSuppressNullableWarningExpression(BoundSuppressNullableWarningExpression node)
        {
            var result = base.VisitSuppressNullableWarningExpression(node);

            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                _result = _result.Type?.SetUnknownNullabilityForReferenceTypes();
            }

            return result;
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
            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt, expanded: false);
            Debug.Assert((object)node.Type == null);
            SetResult(node);
            return null;
        }

        public override BoundNode VisitLiteral(BoundLiteral node)
        {
            var result = base.VisitLiteral(node);

            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                var constant = node.ConstantValue;

                if (constant != null &&
                    ((object)node.Type != null ? node.Type.IsReferenceType : constant.IsNull))
                {
                    _result = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: constant.IsNull);
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

        public override BoundNode VisitRangeVariable(BoundRangeVariable node)
        {
            var result = base.VisitRangeVariable(node);
            SetResult(node); // PROTOTYPE(NullableReferenceTypes)
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
            _result = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: null);
            return null;
        }

        public override BoundNode VisitDynamicInvocation(BoundDynamicInvocation node)
        {
            VisitRvalue(node.Expression);
            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt, expanded: false);

            Debug.Assert(node.Type.IsDynamic());
            Debug.Assert(node.Type.IsReferenceType);

            // PROTOTYPE(NullableReferenceTypes): Update applicable members based on inferred argument types.
            bool? isNullable = InferResultNullabilityFromApplicableCandidates(StaticCast<Symbol>.From(node.ApplicableMethods));
            _result = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: isNullable);
            return null;
        }

        public override BoundNode VisitEventAssignmentOperator(BoundEventAssignmentOperator node)
        {
            VisitRvalue(node.ReceiverOpt);
            Debug.Assert(!IsConditionalState);
            var receiverOpt = node.ReceiverOpt;
            if (!node.Event.IsStatic)
            {
                CheckPossibleNullReceiver(receiverOpt);
            }
            VisitRvalue(node.Argument);
            SetResult(node); // PROTOTYPE(NullableReferenceTypes)
            return null;
        }

        public override BoundNode VisitDynamicObjectCreationExpression(BoundDynamicObjectCreationExpression node)
        {
            Debug.Assert(!IsConditionalState);
            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt, expanded: false);
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
            SetResult(node);
            return result;
        }

        public override BoundNode VisitNewT(BoundNewT node)
        {
            var result = base.VisitNewT(node);
            SetResult(node);
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
            _result = Result.Unset;
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
            CheckPossibleNullReceiver(receiver);
            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt, expanded: false);

            Debug.Assert(node.Type.IsDynamic());

            // PROTOTYPE(NullableReferenceTypes): Update applicable members based on inferred argument types.
            bool? isNullable = (node.Type?.IsReferenceType == true) ?
                InferResultNullabilityFromApplicableCandidates(StaticCast<Symbol>.From(node.ApplicableIndexers)) :
                null;
            _result = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: isNullable);
            return null;
        }

        private void CheckPossibleNullReceiver(BoundExpression receiverOpt, bool checkType = true)
        {
            if (receiverOpt != null && this.State.Reachable)
            {
#if DEBUG
                Debug.Assert(receiverOpt.Type is null || _result.Type?.TypeSymbol is null || AreCloseEnough(receiverOpt.Type, _result.Type.TypeSymbol));
#endif
                TypeSymbol receiverType = receiverOpt.Type ?? _result.Type?.TypeSymbol;
                if ((object)receiverType != null &&
                    (!checkType || receiverType.IsReferenceType || receiverType.IsUnconstrainedTypeParameter()) &&
                    (_result.Type?.IsNullable == true || receiverType.IsUnconstrainedTypeParameter()))
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceReceiver, receiverOpt.Syntax);
                }
            }
        }

        /// <summary>
        /// Report warning converting null literal to non-nullable reference type.
        /// target (e.g.: `object x = null;` or calling `void F(object y)` with `F(null)`).
        /// </summary>
        private bool ReportNullAsNonNullableReferenceIfNecessary(BoundExpression value)
        {
            if (value.ConstantValue?.IsNull != true && !IsDefaultOfUnconstrainedTypeParameter(value))
            {
                return false;
            }
            ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullAsNonNullable, value.Syntax);
            return true;
        }

        private static bool IsDefaultOfUnconstrainedTypeParameter(BoundExpression expr)
        {
            switch (expr.Kind)
            {
                case BoundKind.Conversion:
                    {
                        var conversion = (BoundConversion)expr;
                        // PROTOTYPE(NullableReferenceTypes): Check target type is unconstrained type parameter?
                        return conversion.Conversion.Kind == ConversionKind.DefaultOrNullLiteral &&
                            IsDefaultOfUnconstrainedTypeParameter(conversion.Operand);
                    }
                case BoundKind.DefaultExpression:
                    return IsUnconstrainedTypeParameter(expr.Type);
                default:
                    return false;
            }
        }

        private static bool IsNullabilityMismatch(TypeSymbolWithAnnotations type1, TypeSymbolWithAnnotations type2)
        {
            return type1.Equals(type2, TypeCompareKind.AllIgnoreOptions) &&
                !type1.Equals(type2, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes | TypeCompareKind.UnknownNullableModifierMatchesAny);
        }

        private static bool IsNullabilityMismatch(TypeSymbol type1, TypeSymbol type2)
        {
            return type1.Equals(type2, TypeCompareKind.AllIgnoreOptions) &&
                !type1.Equals(type2, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes | TypeCompareKind.UnknownNullableModifierMatchesAny);
        }

        private bool? InferResultNullabilityFromApplicableCandidates(ImmutableArray<Symbol> applicableMembers)
        {
            if (applicableMembers.IsDefaultOrEmpty)
            {
                return null;
            }

            bool? resultIsNullable = false;

            foreach (Symbol member in applicableMembers)
            {
                TypeSymbolWithAnnotations type = member.GetTypeOrReturnType();

                if (type.IsReferenceType)
                {
                    bool? memberResultIsNullable = type.IsNullable;
                    if (memberResultIsNullable == true)
                    {
                        // At least one candidate can produce null, assume dynamic access can produce null as well
                        resultIsNullable = true;
                        break;
                    }
                    else if (memberResultIsNullable == null)
                    {
                        // At least one candidate can produce result of an unknown nullability.
                        // At best, dynamic access can produce result of an unknown nullability as well.
                        resultIsNullable = null;
                    }
                }
                else if (!type.IsValueType)
                {
                    resultIsNullable = null;
                }
            }

            return resultIsNullable;
        }

        public override BoundNode VisitQueryClause(BoundQueryClause node)
        {
            var result = base.VisitQueryClause(node);
            SetResult(node); // PROTOTYPE(NullableReferenceTypes)
            return result;
        }

        public override BoundNode VisitNameOfOperator(BoundNameOfOperator node)
        {
            var result = base.VisitNameOfOperator(node);
            SetResult(node);
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
            SetResult(node);
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

#endregion Visitors

        protected override string Dump(LocalState state)
        {
            return string.Empty;
        }

        protected override void UnionWith(ref LocalState self, ref LocalState other)
        {
            if (self.Capacity != other.Capacity)
            {
                Normalize(ref self);
                Normalize(ref other);
            }

            for (int slot = 1; slot < self.Capacity; slot++)
            {
                bool? selfSlotIsNotNull = self[slot];
                bool? union = selfSlotIsNotNull | other[slot];
                if (selfSlotIsNotNull != union)
                {
                    self[slot] = union;
                }
            }
        }

        protected override bool IntersectWith(ref LocalState self, ref LocalState other)
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
                    bool? selfSlotIsNotNull = self[slot];
                    bool? intersection = selfSlotIsNotNull & other[slot];
                    if (selfSlotIsNotNull != intersection)
                    {
                        self[slot] = intersection;
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

        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        internal struct Result
        {
            internal readonly TypeSymbolWithAnnotations Type;
            internal readonly int Slot;

            internal static readonly Result Unset = new Result(type: null, slot: -1);

            internal static Result Create(TypeSymbolWithAnnotations type, int slot = -1)
            {
                return new Result(type, slot);
            }

            // PROTOTYPE(NullableReferenceTypes): Consider replacing implicit cast operators with
            // explicit methods - either Result.Create() overloads or ToResult() extension methods.
            public static implicit operator Result(TypeSymbolWithAnnotations type)
            {
                return Result.Create(type);
            }

            private Result(TypeSymbolWithAnnotations type, int slot)
            {
                Type = type;
                Slot = slot;
            }

            private string GetDebuggerDisplay()
            {
                var type = (object)Type == null ? "<null>" : Type.GetDebuggerDisplay();
                return $"Type={type}, Slot={Slot}";
            }
        }

#if REFERENCE_STATE
        internal class LocalState : AbstractLocalState
#else
        internal struct LocalState : AbstractLocalState
#endif
        {
            // PROTOTYPE(NullableReferenceTypes): Consider storing nullability rather than non-nullability
            // or perhaps expose as nullability from `this[int]` even if stored differently.
            private BitVector _knownNullState; // No diagnostics should be derived from a variable with a bit set to 0.
            private BitVector _notNull;

            internal LocalState(BitVector knownNullState, BitVector notNull)
            {
                Debug.Assert(!knownNullState.IsNull);
                Debug.Assert(!notNull.IsNull);
                this._knownNullState = knownNullState;
                this._notNull = notNull;
            }

            internal int Capacity => _knownNullState.Capacity;

            internal void EnsureCapacity(int capacity)
            {
                _knownNullState.EnsureCapacity(capacity);
                _notNull.EnsureCapacity(capacity);
            }

            internal bool? this[int slot]
            {
                get
                {
                    return _knownNullState[slot] ? _notNull[slot] : (bool?)null;
                }
                set
                {
                    _knownNullState[slot] = value.HasValue;
                    _notNull[slot] = value.GetValueOrDefault();
                }
            }

            /// <summary>
            /// Produce a duplicate of this flow analysis state.
            /// </summary>
            /// <returns></returns>
            public LocalState Clone()
            {
                return new LocalState(_knownNullState.Clone(), _notNull.Clone());
            }

            public bool Reachable
            {
                get
                {
                    return _knownNullState.Capacity > 0;
                }
            }
        }
    }
}
