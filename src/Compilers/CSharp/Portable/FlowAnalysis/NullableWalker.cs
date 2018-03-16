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

        private readonly bool _includeNonNullableWarnings;

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
            Action<BoundExpression, TypeSymbolWithAnnotations> callbackOpt,
            bool includeNonNullableWarnings)
            : base(compilation, member, node, new EmptyStructTypeCache(compilation, dev12CompilerCompatibility: false), trackUnassignments: false)
        {
            _sourceAssembly = ((object)member == null) ? null : (SourceAssemblySymbol)member.ContainingAssembly;
            this._currentMethodOrLambda = member;
            _callbackOpt = callbackOpt;
            _includeNonNullableWarnings = includeNonNullableWarnings;
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
                callbackOpt,
                includeNonNullableWarnings: (flags & NullableReferenceFlags.AllowNullAsNonNull) == 0);

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
            for (int slot = oldNext; slot < nextVariableSlot; slot++)
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

        /// <summary>
        /// Report nullable mismatch warnings and optionally update tracked value on assignment.
        /// </summary>
        private void TrackNullableStateForAssignment(BoundNode node, int targetSlot, TypeSymbolWithAnnotations targetType, BoundExpression value, TypeSymbolWithAnnotations valueType, int valueSlot)
        {
            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                if ((object)targetType == null)
                {
                    return;
                }

                if (targetType.IsReferenceType)
                {
                    bool isByRefTarget = IsByRefTarget(targetSlot);

                    if (targetType.IsNullable == false)
                    {
                        if (valueType?.IsNullable == true && (value == null || !CheckNullAsNonNullableReference(value)))
                        {
                            ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceAssignment, (value ?? node).Syntax);
                        }
                    }
                    else if (targetSlot > 0)
                    {
                        if (targetSlot >= this.State.Capacity) Normalize(ref this.State);

                        this.State[targetSlot] = isByRefTarget ?
                            // Since reference can point to the heap, we cannot assume the value is not null after this assignment,
                            // regardless of what value is being assigned. 
                            (targetType.IsNullable == true) ? (bool?)false : null :
                            !valueType?.IsNullable;
                    }

                    if (targetSlot > 0)
                    {
                        // PROTOTYPE(NullableReferenceTypes): Might this clear state that
                        // should be copied in InheritNullableStateOfTrackableType?
                        InheritDefaultState(targetSlot);

                        // PROTOTYPE(NullableReferenceTypes): We should copy all tracked state from `value`,
                        // regardless of BoundNode type, but we'll need to handle cycles. (For instance, the
                        // assignment to C.F below. See also StaticNullChecking_Members.FieldCycle_01.)
                        // class C
                        // {
                        //     C? F;
                        //     C() { F = this; }
                        // }
                        // For now, we copy a limited set of BoundNode types that shouldn't contain cycles.
                        if (value != null &&
                            (value.Kind == BoundKind.ObjectCreationExpression || value.Kind == BoundKind.AnonymousObjectCreationExpression || value.Kind == BoundKind.DynamicObjectCreationExpression || targetType.TypeSymbol.IsAnonymousType) &&
                            targetType.TypeSymbol.Equals(valueType?.TypeSymbol, TypeCompareKind.ConsiderEverything)) // PROTOTYPE(NullableReferenceTypes): Allow assignment to base type.
                        {
                            if (valueSlot > 0)
                            {
                                InheritNullableStateOfTrackableType(targetSlot, valueSlot, isByRefTarget, slotWatermark: GetSlotWatermark());
                            }
                        }
                    }
                }
                else if (targetSlot > 0 && EmptyStructTypeCache.IsTrackableStructType(targetType.TypeSymbol) &&
                        (value == null || targetType.TypeSymbol.Equals(valueType?.TypeSymbol, TypeCompareKind.ConsiderEverything)))
                {
                    InheritNullableStateOfTrackableStruct(targetType.TypeSymbol, targetSlot, valueSlot, IsByRefTarget(targetSlot), slotWatermark: GetSlotWatermark());
                }

                if ((object)valueType?.TypeSymbol != null && IsNullabilityMismatch(targetType.TypeSymbol, valueType.TypeSymbol))
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInAssignment, (value ?? node).Syntax, valueType.TypeSymbol, targetType.TypeSymbol);
                }
            }
        }

        private int GetSlotWatermark() => this.State.Capacity;

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
            return new LocalState(BitVector.Create(nextVariableSlot), BitVector.Create(nextVariableSlot));
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
            if (parameter.RefKind == RefKind.Out && !this._currentMethodOrLambda.IsAsync) // out parameters not allowed in async
            {
            }
            else
            {
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

                if (_result.Type?.IsNullable == true)
                {
                    if ((object)returnType != null && returnType.IsReferenceType && returnType.IsNullable == false &&
                        !CheckNullAsNonNullableReference(node.ExpressionOpt))
                    {
                        ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceReturn, node.ExpressionOpt.Syntax);
                    }
                }

                if ((object)node.ExpressionOpt.Type != null && IsNullabilityMismatch(returnType.TypeSymbol, node.ExpressionOpt.Type))
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInAssignment, node.ExpressionOpt.Syntax, node.ExpressionOpt.Type, returnType.TypeSymbol);
                }
            }

            return result;
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

                TrackNullableStateForAssignment(node, slot, type, initializer, valueType, value.Slot);
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
            VisitArguments(node.Arguments, node.ArgumentRefKindsOpt, node.Constructor, node.ArgsToParamsOpt, node.Expanded);
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
                        TrackNullableStateForAssignment(node, containingSlot, type, node, result.Type, result.Slot);
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
                            var method = ((PropertySymbol)symbol).GetOwnOrInheritedSetMethod();
                            VisitArguments(objectInitializer.Arguments, objectInitializer.ArgumentRefKindsOpt, method, objectInitializer.ArgsToParamsOpt, objectInitializer.Expanded);
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

            VisitArguments(node.Arguments, default(ImmutableArray<RefKind>), node.AddMethod, node.ArgsToParamsOpt, node.Expanded);
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
                WarnOnNullReferenceArgument(argument, argumentResult.Type, parameter, expanded: false);

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

                TrackNullableStateForAssignment(argument, GetOrCreateSlot(property, receiverSlot), property.Type, argument, argumentResult.Type, argumentResult.Slot);
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
                if ((object)bestType != null)
                {
                    elementType = bestType;
                }
                arrayType = arrayType.WithElementType(elementType);
            }

            TypeSymbol elementTypeSymbol = elementType?.TypeSymbol;
            bool elementTypeIsReferenceType = elementType?.IsReferenceType == true;
            for (int i = 0; i < n; i++)
            {
                var element = elementBuilder[i];
                var result = GenerateConversionForAssignment(element, elementTypeSymbol, resultBuilder[i]);
                if (elementTypeIsReferenceType)
                {
                    TrackNullableStateForAssignment(element, -1, elementType, element, result.Type, result.Slot);
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
                    WarnOnNullReferenceArgument(binary.Left, leftType, binary.MethodOpt.Parameters[0], expanded: false);
                }

                VisitRvalue(binary.Right);
                Debug.Assert(!IsConditionalState);

                // At this point, State.Reachable may be false for
                // invalid code such as `s + throw new Exception()`.
                TypeSymbolWithAnnotations rightType = _result.Type;

                if (warnOnNullReferenceArgument)
                {
                    WarnOnNullReferenceArgument(binary.Right, rightType, binary.MethodOpt.Parameters[1], expanded: false);
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

            VisitRvalue(node.LeftOperand);
            if (IsConstantNull(node.LeftOperand))
            {
                VisitRvalue(node.RightOperand);
                return null;
            }

            var leftState = this.State.Clone();
            var leftType = _result.Type;
            if (leftType?.IsNullable == false)
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.HDN_ExpressionIsProbablyNeverNull, node.LeftOperand.Syntax);
            }

            bool leftIsConstant = node.LeftOperand.ConstantValue != null;
            if (leftIsConstant)
            {
                SetUnreachable();
            }

            leftType = InferResultNullability(node.LeftOperand, node.LeftConversion, node.Type, leftType);

            VisitRvalue(node.RightOperand);
            var rightType = _result.Type;

            IntersectWith(ref this.State, ref leftState);

            TypeSymbolWithAnnotations resultType;

            if (node.Type.IsErrorType())
            {
                resultType = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: null);
            }
            else
            {
                // PROTOTYPE(NullableReferenceTypes): Conversions: NullCoalescingOperator
#if DEBUG
                //Debug.Assert((object)leftType == null || AreCloseEnough(leftType.TypeSymbol, node.Type));
                //Debug.Assert((object)rightType == null || AreCloseEnough(rightType.TypeSymbol, node.Type));
#endif

                // PROTOTYPE(NullableReferenceTypes): Capture in BindNullCoalescingOperator
                // which side provides type and use that to determine nullability.
                resultType = TypeSymbolWithAnnotations.Create((leftType ?? rightType)?.TypeSymbol, isNullableIfReferenceType: rightType?.IsNullable & leftType?.IsNullable);

                ReportNullabilityMismatchIfAny(node.LeftOperand, resultType, leftType);
                ReportNullabilityMismatchIfAny(node.RightOperand, resultType, rightType);
            }

            // PROTOTYPE(NullableReferenceTypes): Conversions: NullCoalescingOperator
            //_result = resultType;
            SetResult(node);
            return null;
        }

        private void ReportNullabilityMismatchIfAny(BoundExpression node, TypeSymbolWithAnnotations expectedType, TypeSymbolWithAnnotations actualType)
        {
            if ((object)expectedType != null &&
                (object)actualType != null &&
                IsNullabilityMismatch(expectedType.TypeSymbol, actualType.TypeSymbol))
            {
                // PROTOTYPE(NullableReferenceTypes): Create a distinct warning rather than using WRN_NullabilityMismatchInAssignment.
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInAssignment, node.Syntax, actualType.TypeSymbol, expectedType.TypeSymbol);
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

            ImmutableArray<Result> results = VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt, method, node.ArgsToParamsOpt, node.Expanded);
            if (method.IsGenericMethod && HasImplicitTypeArguments(node))
            {
                method = InferMethod(node, method, results.SelectAsArray(r => r.Type));
            }
            VisitArgumentsWarn(node.Arguments, node.ArgumentRefKindsOpt, method, node.ArgsToParamsOpt, node.Expanded, results);

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

        private void VisitArguments(ImmutableArray<BoundExpression> arguments, ImmutableArray<RefKind> refKindsOpt, MethodSymbol method, ImmutableArray<int> argsToParamsOpt, bool expanded)
        {
            ImmutableArray<Result> results = VisitArgumentsEvaluate(arguments, refKindsOpt, method, argsToParamsOpt, expanded);
            VisitArgumentsWarn(arguments, refKindsOpt, method, argsToParamsOpt, expanded, results);
        }

        private ImmutableArray<Result> VisitArgumentsEvaluate(
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
            MethodSymbol method,
            ImmutableArray<int> argsToParamsOpt,
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
            MethodSymbol method,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded,
            ImmutableArray<Result> results)
        {
            for (int i = 0; i < arguments.Length; i++)
            {
                RefKind refKind = GetRefKind(refKindsOpt, i);
                var argument = arguments[i];
                var parameter = GetCorrespondingParameter(i, method, argsToParamsOpt, ref expanded);
                var result = results[i];
                if (refKind != RefKind.None)
                {
                    TrackNullableStateForAssignment(argument, result.Slot, result.Type, null, parameter?.Type, -1);
                }
                if (refKind != RefKind.Out && (object)parameter != null)
                {
                    WarnOnNullReferenceArgument(argument, result.Type, parameter, expanded);
                }
            }
        }

        private static ParameterSymbol GetCorrespondingParameter(int argumentOrdinal, MethodSymbol method, ImmutableArray<int> argsToParamsOpt, ref bool expanded)
        {
            if ((object)method == null)
            {
                expanded = false;
                return null;
            }

            ParameterSymbol parameter;

            if (argsToParamsOpt.IsDefault)
            {
                if (argumentOrdinal < method.ParameterCount)
                {
                    parameter = method.Parameters[argumentOrdinal];
                }
                else if (expanded)
                {
                    parameter = method.Parameters[method.ParameterCount - 1];
                }
                else
                {
                    parameter = null;
                }
            }
            else
            {
                int parameterOrdinal = argsToParamsOpt[argumentOrdinal];

                if (parameterOrdinal < method.ParameterCount)
                {
                    parameter = method.Parameters[parameterOrdinal];
                }
                else
                {
                    parameter = null;
                    expanded = false;
                }
            }

            Debug.Assert((object)parameter != null || !expanded);
            if (expanded && (parameter.Ordinal < method.ParameterCount - 1 || !parameter.Type.IsSZArray()))
            {
                expanded = false;
            }

            return parameter;
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

        // See Binder.GenerateConversionForAssignment for corresponding approach in initial binding.
        private Result GenerateConversionForAssignment(BoundExpression value, TypeSymbol targetType, Result valueResult)
        {
            if (targetType is null)
            {
                return Result.Unset;
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            TypeSymbol sourceType = valueResult.Type?.TypeSymbol;
            var conversion = (sourceType is null) ?
                _conversions.ClassifyImplicitConversionFromExpression(value, targetType, ref useSiteDiagnostics) :
                _conversions.ClassifyImplicitConversionFromType(sourceType, targetType, ref useSiteDiagnostics);
            if (!conversion.Exists)
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInAssignment, value.Syntax, GetTypeAsDiagnosticArgument(valueResult.Type?.TypeSymbol), targetType);
            }

            return InferResultNullability(value, conversion, targetType, valueResult.Type);
        }

        /// <summary>
        /// Adjust declared type based on inferred nullability at the point of reference.
        /// </summary>
        private Result GetAdjustedResult(Result pair)
        {
            var type = pair.Type;
            var slot = pair.Slot;
            if (type.IsNullable != false && slot > 0 && slot < this.State.Capacity)
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

            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                switch (node.ConversionKind)
                {
                    case ConversionKind.ExplicitUserDefined:
                    case ConversionKind.ImplicitUserDefined:
                        if ((object)node.SymbolOpt != null && node.SymbolOpt.ParameterCount == 1)
                        {
                            WarnOnNullReferenceArgument(operand, _result.Type, node.SymbolOpt.Parameters[0], expanded: false);
                        }
                        break;

                    case ConversionKind.AnonymousFunction:
                        if (!node.ExplicitCastInCode && operand.Kind == BoundKind.Lambda)
                        {
                            var lambda = (BoundLambda)operand;
                            ReportNullabilityMismatchWithTargetDelegate(operand.Syntax, node.Type.GetDelegateType(), lambda.Symbol);
                        }
                        break;

                    case ConversionKind.MethodGroup:
                        if (!node.ExplicitCastInCode)
                        {
                            ReportNullabilityMismatchWithTargetDelegate(operand.Syntax, node.Type.GetDelegateType(), node.SymbolOpt);
                        }
                        break;
                }

                var operandType = _result.Type;
                TypeSymbolWithAnnotations resultType;
                if (operand.Kind == BoundKind.Literal && (object)operand.Type == null && operand.ConstantValue.IsNull)
                {
                    resultType = TypeSymbolWithAnnotations.Create(node.Type, true);
                }
                else if (node.ConversionKind == ConversionKind.Identity && !node.ExplicitCastInCode)
                {
                    resultType = operandType;
                }
                else
                {
                    resultType = InferResultNullability(operand, node.Conversion, node.Type, operandType, fromConversionNode: true);
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
                TrackNullableStateForAssignment(node.Left, left.Slot, left.Type, node.Right, right.Type, right.Slot);
                // PROTOTYPE(NullableReferenceTypes): Check node.Type.IsErrorType() instead?
                _result = node.HasErrors ? Result.Create(TypeSymbolWithAnnotations.Create(node.Type)) : left;
            }

            return null;
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
                    WarnOnNullReferenceArgument(node.Operand, operandResult.Type, node.OperandConversion.Method.Parameters[0], expanded: false);
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
                    WarnOnNullReferenceArgument(node.Operand, resultOfOperandConversionType, incrementOperator.Parameters[0], expanded: false);

                    resultOfIncrementType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(incrementOperator);
                }

                if (node.ResultConversion.IsUserDefined && (object)node.ResultConversion.Method != null && node.ResultConversion.Method.ParameterCount == 1)
                {
                    WarnOnNullReferenceArgument(node, resultOfIncrementType, node.ResultConversion.Method.Parameters[0], expanded: false);
                }

                resultOfIncrementType = InferResultNullability(node, node.ResultConversion, node.Type, resultOfIncrementType);

                // PROTOTYPE(NullableReferenceTypes): Check node.Type.IsErrorType() instead?
                if (!node.HasErrors)
                {
                    var op = node.OperatorKind.Operator();
                    _result = (op == UnaryOperatorKind.PrefixIncrement || op == UnaryOperatorKind.PrefixDecrement) ? resultOfIncrementType : operandResult;
                    setResult = true;

                    TrackNullableStateForAssignment(node.Operand, operandResult.Slot, operandResult.Type, value: node, valueType: resultOfIncrementType, valueSlot: -1);
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
                    WarnOnNullReferenceArgument(node.Left, leftOnRight.Type, node.LeftConversion.Method.Parameters[0], expanded: false);
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
                        WarnOnNullReferenceArgument(node.Left, leftOnRightType, node.Operator.Method.Parameters[0], expanded: false);
                        WarnOnNullReferenceArgument(node.Right, rightType, node.Operator.Method.Parameters[1], expanded: false);
                    }

                    resultType = InferResultNullability(node.Operator.Kind, node.Operator.Method, node.Operator.ReturnType, leftOnRightType, rightType);

                    // PROTOTYPE(NullableReferenceTypes): Update final conversion based on inferred operand type.
                    if (node.FinalConversion.IsUserDefined && (object)node.FinalConversion.Method != null && node.FinalConversion.Method.ParameterCount == 1)
                    {
                        WarnOnNullReferenceArgument(node, resultType, node.FinalConversion.Method.Parameters[0], expanded: false);
                    }

                    resultType = InferResultNullability(node, node.FinalConversion, node.Type, resultType);
                }
                else
                {
                    resultType = null;
                }

                TrackNullableStateForAssignment(node, left.Slot, left.Type, node, resultType, -1);
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

        private void WarnOnNullReferenceArgument(BoundExpression argument, TypeSymbolWithAnnotations argumentType, ParameterSymbol parameter, bool expanded)
        {
            var paramType = parameter.Type;

            if (argumentType?.IsNullable == true)
            {
                if (expanded)
                {
                    paramType = ((ArrayTypeSymbol)parameter.Type.TypeSymbol).ElementType;
                }

                if (paramType.IsReferenceType && paramType.IsNullable == false && !CheckNullAsNonNullableReference(argument))
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceArgument, argument.Syntax,
                        new FormattedSymbol(parameter, SymbolDisplayFormat.ShortFormat),
                        new FormattedSymbol(parameter.ContainingSymbol, SymbolDisplayFormat.MinimallyQualifiedFormat));
                }
            }

            if ((object)argumentType != null && IsNullabilityMismatch(paramType.TypeSymbol, argumentType.TypeSymbol))
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInArgument, argument.Syntax, argumentType.TypeSymbol, paramType.TypeSymbol,
                        new FormattedSymbol(parameter, SymbolDisplayFormat.ShortFormat),
                        new FormattedSymbol(parameter.ContainingSymbol, SymbolDisplayFormat.MinimallyQualifiedFormat));
            }
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

            // PROTOTYPE(NullableReferenceTypes): Update method based on inferred receiver type.
            var method = node.Indexer.GetOwnOrInheritedGetMethod();
            VisitArguments(node.Arguments, node.ArgumentRefKindsOpt, method, node.ArgsToParamsOpt, node.Expanded);

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
                    // If the symbol is statically declared as not-nullable
                    // or null-oblivious, ignore flow state.
                    if (resultType.IsNullable == true && resultType.IsReferenceType)
                    {
                        // We are supposed to track information for the node. Use whatever we managed to
                        // accumulate so far.
                        if (slot > 0 && slot < this.State.Capacity)
                        {
                            var isNullable = !this.State[slot];
                            if (isNullable != resultType.IsNullable)
                            {
                                resultType = TypeSymbolWithAnnotations.Create(resultType.TypeSymbol, isNullable);
                            }
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
                WarnOnNullReferenceArgument(node.Operand, _result.Type, node.MethodOpt.Parameters[0], expanded: false);
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
                    WarnOnNullReferenceArgument(left, leftType, trueFalseOperator.Parameters[0], expanded: false);
                }

                if ((object)logicalOperator != null)
                {
                    WarnOnNullReferenceArgument(left, leftType, logicalOperator.Parameters[0], expanded: false);
                }

                Visit(right);
                TypeSymbolWithAnnotations rightType = _result.Type;

                _result = InferResultNullabilityOfBinaryLogicalOperator(node, leftType, rightType);

                if ((object)logicalOperator != null)
                {
                    WarnOnNullReferenceArgument(right, rightType, logicalOperator.Parameters[1], expanded: false);
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
            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt, null, default(ImmutableArray<int>), expanded: false);
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
            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt, null, default(ImmutableArray<int>), expanded: false);

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
            VisitArguments(node.Arguments, node.ArgumentRefKindsOpt, null, default(ImmutableArray<int>), expanded: false);
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
            VisitArguments(node.Arguments, node.ArgumentRefKindsOpt, null, default(ImmutableArray<int>), expanded: false);

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
            if (receiverOpt != null &&
                (!checkType || ((object)receiverOpt.Type != null && receiverOpt.Type.IsReferenceType)) &&
                this.State.Reachable &&
                _result.Type?.IsNullable == true)
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceReceiver, receiverOpt.Syntax);
            }
        }

        private bool CheckNullAsNonNullableReference(BoundExpression value)
        {
            if (value.ConstantValue?.IsNull != true)
            {
                return false;
            }
            if (_includeNonNullableWarnings)
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullAsNonNullable, value.Syntax);
            }
            return true;
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
                self.Clone(other);
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
            // PROTOTYPE(NullableReferenceTypes): Consider storing nullability rather than non-nullability.
            private BitVector _knownNullState; // No diagnostics should be derived from a variable with a bit set to 0.
            private BitVector _notNull;

            internal LocalState(BitVector unknownNullState, BitVector notNull)
            {
                Debug.Assert(!unknownNullState.IsNull);
                Debug.Assert(!notNull.IsNull);
                this._knownNullState = unknownNullState;
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

            internal void Clone(LocalState other)
            {
                _knownNullState = other._knownNullState.Clone();
                _notNull = other._notNull.Clone();
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
