// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if DEBUG
// See comment in DataFlowPass.
#define REFERENCE_STATE
#endif

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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

        private readonly TypeSymbolWithAnnotations _invalidType;

        /// <summary>
        /// Reflects the enclosing method or lambda at the current location (in the bound tree).
        /// </summary>
        private MethodSymbol _currentMethodOrLambda;

        private readonly bool _includeNonNullableWarnings;
        private PooledDictionary<BoundExpression, ObjectCreationPlaceholderLocal> _placeholderLocals;
        private LocalSymbol _implicitReceiver;

        protected override void Free()
        {
            _variableTypes.Free();
            _placeholderLocals?.Free();
            base.Free();
        }

        internal NullableWalker(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            bool includeNonNullableWarnings)
            : base(compilation, member, node, new EmptyStructTypeCache(compilation, !compilation.FeatureStrictEnabled), trackUnassignments: false, trackClassFields: false)
        {
            _invalidType = TypeSymbolWithAnnotations.Create(new ExtendedErrorTypeSymbol(compilation, "Invalid", 0, null));
            _sourceAssembly = ((object)member == null) ? null : (SourceAssemblySymbol)member.ContainingAssembly;
            this._currentMethodOrLambda = member as MethodSymbol;
            _includeNonNullableWarnings = includeNonNullableWarnings;
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

        /// <summary>
        /// Perform data flow analysis, reporting all necessary diagnostics.
        /// </summary>
        public static void Analyze(CSharpCompilation compilation, Symbol member, BoundNode node, DiagnosticBag diagnostics)
        {
            Debug.Assert(diagnostics != null);

            var flags = ((CSharpParseOptions)node.SyntaxTree.Options).GetNullableReferenceFlags();
            var walker = new NullableWalker(
                compilation,
                member,
                node,
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
            state.EnsureCapacity(nextVariableSlot);
        }

        protected override bool TryGetReceiverAndMember(BoundExpression expr, out BoundExpression receiver, out Symbol member)
        {
            if (expr.Kind == BoundKind.PropertyAccess)
            {
                var propAccess = (BoundPropertyAccess)expr;
                if (!Binder.AccessingAutoPropertyFromConstructor(propAccess, this._currentMethodOrLambda))
                {
                    var propSymbol = propAccess.PropertySymbol;
                    if (IsTrackableAnonymousTypeProperty(propSymbol))
                    {
                        Debug.Assert(!propSymbol.Type.IsReferenceType || propSymbol.Type.IsNullable == true);
                        receiver = propAccess.ReceiverOpt;
                        if ((object)receiver != null && receiver.Kind != BoundKind.TypeExpression)
                        {
                            member = propSymbol;
                            return true;
                        }
                        receiver = null;
                        member = null;
                        return false;
                    }
                }
            }
            return base.TryGetReceiverAndMember(expr, out receiver, out member);
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

        private bool IsTrackableAnonymousTypeProperty(PropertySymbol propSymbol)
        {
            return !propSymbol.IsStatic &&
                   propSymbol.IsReadOnly &&
                   propSymbol.ContainingType.IsAnonymousType &&
                   (propSymbol.Type.IsReferenceType || EmptyStructTypeCache.IsTrackableStructType(propSymbol.Type.TypeSymbol));
        }

        private Symbol GetNonFieldSymbol(int slot)
        {
            VariableIdentifier variableId = variableBySlot[slot];
            while (variableId.ContainingSlot > 0)
            {
                Debug.Assert(variableId.Symbol.Kind == SymbolKind.Field || (variableId.Symbol.Kind == SymbolKind.Property));
                variableId = variableBySlot[variableId.ContainingSlot];
            }
            return variableId.Symbol;
        }

        // PROTOTYPE(NullableReferenceTypes): Use TypeAndSlot instead.
        private void Assign(BoundNode node, BoundExpression value, TypeSymbolWithAnnotations valueType, RefKind refKind = RefKind.None)
        {
            AssignImpl(node, value, valueType, refKind: refKind);
        }

        // PROTOTYPE(NullableReferenceTypes): Use TypeAndSlot instead.
        /// <summary>
        /// Mark a variable as assigned (or unassigned).
        /// </summary>
        /// <param name="node">Node being assigned to.</param>
        /// <param name="value">The value being assigned.</param>
        /// <param name="valueType"/>
        /// <param name="refKind">Target kind (by-ref or not).</param>
        private void AssignImpl(BoundNode node, BoundExpression value, TypeSymbolWithAnnotations valueType, RefKind refKind)
        {
            Debug.Assert(!IsConditionalState);

            switch (node.Kind)
            {
                case BoundKind.LocalDeclaration:
                    {
                        var local = (BoundLocalDeclaration)node;
                        Debug.Assert(local.InitializerOpt == value || value == null);
                        LocalSymbol symbol = local.LocalSymbol;
                        int slot = GetOrCreateSlot(symbol);
                        //if (written)
                        {
                            TrackNullableStateForAssignment(node, symbol, slot, value, valueType, inferNullability: local.DeclaredType.InferredType);
                        }
                        break;
                    }

                case BoundKind.Local:
                    {
                        var local = (BoundLocal)node;
                        if (local.LocalSymbol.RefKind != refKind)
                        {
                            // Writing through the (reference) value of a reference local
                            // requires us to read the reference itself.
                            //if (written)
                            {
                                VisitRvalue(local);
                            }

                            // PROTOTYPE(NullableReferenceTypes): StaticNullChecking?
                        }
                        else
                        {
                            int slot = MakeSlot(local);
                            //if (written)
                            {
                                TrackNullableStateForAssignment(node, local.LocalSymbol, slot, value, valueType);
                            }
                        }
                        break;
                    }

                case BoundKind.Parameter:
                    {
                        var parameter = (BoundParameter)node;
                        int slot = GetOrCreateSlot(parameter.ParameterSymbol);
                        //if (written)
                        {
                            TrackNullableStateForAssignment(node, parameter.ParameterSymbol, slot, value, valueType);
                        }
                        break;
                    }

                case BoundKind.FieldAccess:
                    {
                        var fieldAccess = (BoundFieldAccess)node;
                        int slot = MakeSlot(fieldAccess);
                        //if (written)
                        {
                            TrackNullableStateForAssignment(node, fieldAccess.FieldSymbol, slot, value, valueType);
                        }
                        break;
                    }

                case BoundKind.EventAccess:
                    {
                        var eventAccess = (BoundEventAccess)node;
                        int slot = MakeSlot(eventAccess);
                        //if (written)
                        {
                            TrackNullableStateForAssignment(node, eventAccess.EventSymbol, slot, value, valueType);
                        }
                        break;
                    }

                case BoundKind.PropertyAccess:
                    {
                        var propertyAccess = (BoundPropertyAccess)node;
                        int slot = MakeSlot(propertyAccess);
                        //if (written)
                        {
                            TrackNullableStateForAssignment(node, propertyAccess.PropertySymbol, slot, value, valueType);
                        }
                        break;
                    }

                case BoundKind.IndexerAccess:
                    {
                        //if (written && this.State.Reachable)
                        {
                            var indexerAccess = (BoundIndexerAccess)node;
                            TrackNullableStateForAssignment(node, indexerAccess.Indexer, -1, value, valueType);
                        }
                        break;
                    }

                case BoundKind.ArrayAccess:
                    {
                        //if (written && this.State.Reachable)
                        {
                            var arrayAccess = (BoundArrayAccess)node;
                            TypeSymbolWithAnnotations elementType = (arrayAccess.Expression.Type as ArrayTypeSymbol)?.ElementType;

                            if ((object)elementType != null)
                            {
                                // Pass array type symbol as the target for the assignment. 
                                TrackNullableStateForAssignment(node, arrayAccess.Expression.Type, -1, value, valueType);
                            }
                        }
                        break;
                    }

                case BoundKind.ObjectInitializerMember:
                    //if (written && this.State.Reachable)
                    {
                        var initializerMember = (BoundObjectInitializerMember)node;
                        Symbol memberSymbol = initializerMember.MemberSymbol;

                        if ((object)memberSymbol != null)
                        {
                            int slot = -1;

                            if ((object)_implicitReceiver != null && !memberSymbol.IsStatic)
                            {
                                // PROTOTYPE(NullableReferenceTypes): Do we need to handle events?
                                if (memberSymbol.Kind == SymbolKind.Field)
                                {
                                    slot = GetOrCreateSlot(memberSymbol, GetOrCreateSlot(_implicitReceiver));
                                }
                            }

                            TrackNullableStateForAssignment(node, memberSymbol, slot, value, valueType);
                        }
                    }
                    break;

                case BoundKind.ThisReference:
                    {
                        var expression = (BoundThisReference)node;
                        int slot = MakeSlot(expression);
                        //if (written)
                        {
                            ParameterSymbol thisParameter = MethodThisParameter;

                            if ((object)thisParameter != null)
                            {
                                TrackNullableStateForAssignment(node, thisParameter, slot, value, valueType);
                            }
                        }
                        break;
                    }

                case BoundKind.RangeVariable:
                    // PROTOTYPE(NullableReferenceTypes): StaticNullChecking?
                    AssignImpl(((BoundRangeVariable)node).Value, value, valueType, refKind);
                    break;

                case BoundKind.BadExpression:
                    {
                        // Sometimes a bad node is not so bad that we cannot analyze it at all.
                        var bad = (BoundBadExpression)node;
                        if (!bad.ChildBoundNodes.IsDefault && bad.ChildBoundNodes.Length == 1)
                        {
                            AssignImpl(bad.ChildBoundNodes[0], value, valueType, refKind);
                        }
                        break;
                    }

                case BoundKind.TupleLiteral:
                    ((BoundTupleExpression)node).VisitAllElements((x, self) => self.Assign(x, value: null, valueType: self.State.ResultType, refKind: refKind), this);
                    break;

                default:
                    // Other kinds of left-hand-sides either represent things not tracked (e.g. array elements)
                    // or errors that have been reported earlier (e.g. assignment to a unary increment)
                    break;
            }
        }

        private void TrackNullableStateForAssignment(BoundNode node, int slot, TypeSymbolWithAnnotations targetType, TypeAndSlot value, bool inferNullability = false)
        {
            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                if (targetType.IsReferenceType)
                {
                    LocalSymbol local = null;
                    if (inferNullability)
                    {
                        local = (LocalSymbol)variableBySlot[slot].Symbol;
                        _variableTypes[local] = value.Type;
                    }

                    if ((object)local != null && _variableTypes.TryGetValue(local, out var variableType))
                    {
                        targetType = variableType;
                    }

                    bool isByRefTarget = IsByRefTarget(slot);

                    if (targetType.IsNullable == false)
                    {
                        if (value.Type?.IsNullable == true && (value.IsNull || !CheckNullAsNonNullableReference(value)))
                        {
                            ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceAssignment, value.Syntax ?? node.Syntax);
                        }
                    }
                    else if (slot > 0)
                    {
                        if (slot >= this.State.Capacity) Normalize(ref this.State);

                        this.State[slot] = isByRefTarget ?
                            // Since reference can point to the heap, we cannot assume the value is not null after this assignment,
                            // regardless of what value is being assigned. 
                            (targetType.IsNullable == true) ? (bool?)false : null :
                            !value.Type?.IsNullable;
                    }

                    if (slot > 0 && targetType.TypeSymbol.IsAnonymousType && targetType.TypeSymbol.IsClassType() &&
                        (value.IsNull || targetType.TypeSymbol == value.Type.TypeSymbol))
                    {
                        InheritNullableStateOfAnonymousTypeInstance(targetType.TypeSymbol, slot, value.Slot, isByRefTarget);
                    }
                }
                else if (slot > 0 && EmptyStructTypeCache.IsTrackableStructType(targetType.TypeSymbol) &&
                        (value.IsNull || targetType.TypeSymbol == value.Type.TypeSymbol))
                {
                    InheritNullableStateOfTrackableStruct(targetType.TypeSymbol, slot, value.Slot, IsByRefTarget(slot));
                }

                if (!value.IsNull && (object)value.Type?.TypeSymbol != null && IsNullabilityMismatch(targetType.TypeSymbol, value.Type.TypeSymbol))
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInAssignment, value.Syntax, value.Type.TypeSymbol, targetType.TypeSymbol);
                }
            }
        }

        // PROTOTYPE(NullableReferenceTypes): Use TypeAndSlot instead.
        private void TrackNullableStateForAssignment(BoundNode node, Symbol assignmentTarget, int slot, BoundExpression value, TypeSymbolWithAnnotations valueType, bool inferNullability = false)
        {
#if false
            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                // Specially handle array types as assignment targets, the assignment is happening to an array element.
                TypeSymbolWithAnnotations targetType = assignmentTarget.Kind == SymbolKind.ArrayType ?
                                                           ((ArrayTypeSymbol)assignmentTarget).ElementType :
                                                           GetTypeOrReturnTypeWithAdjustedNullableAnnotations(assignmentTarget);

                if (targetType.IsReferenceType)
                {
                    var local = assignmentTarget as LocalSymbol;
                    if (inferNullability)
                    {
                        Debug.Assert((object)local != null);
                        _variableTypes[local] = valueType;
                    }

                    if ((object)local != null && _variableTypes.TryGetValue(local, out var variableType))
                    {
                        targetType = variableType;
                    }

                    bool isByRefTarget = IsByRefTarget(slot);

                    if (targetType.IsNullable == false)
                    {
                        if (valueType?.IsNullable == true && (value == null || !CheckNullAsNonNullableReference(value)))
                        {
                            ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceAssignment, (value ?? node).Syntax);
                        }
                    }
                    else if (slot > 0)
                    {
                        if (slot >= this.State.Capacity) Normalize(ref this.State);

                        this.State[slot] = isByRefTarget ?
                            // Since reference can point to the heap, we cannot assume the value is not null after this assignment,
                            // regardless of what value is being assigned. 
                            (targetType.IsNullable == true) ? (bool?)false : null :
                            !valueType?.IsNullable;
                    }

                    if (slot > 0 && targetType.TypeSymbol.IsAnonymousType && targetType.TypeSymbol.IsClassType() &&
                        (value == null || targetType.TypeSymbol == valueType.TypeSymbol))
                    {
                        InheritNullableStateOfAnonymousTypeInstance(targetType.TypeSymbol, slot, GetValueSlotForAssignment(value), isByRefTarget);
                    }
                }
                else if (slot > 0 && EmptyStructTypeCache.IsTrackableStructType(targetType.TypeSymbol) &&
                        (value == null || targetType.TypeSymbol == valueType.TypeSymbol))
                {
                    InheritNullableStateOfTrackableStruct(targetType.TypeSymbol, slot, GetValueSlotForAssignment(value), IsByRefTarget(slot));
                }

                if (value != null && (object)valueType?.TypeSymbol != null && IsNullabilityMismatch(targetType.TypeSymbol, valueType.TypeSymbol))
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInAssignment, value.Syntax, valueType.TypeSymbol, targetType.TypeSymbol);
                }
            }
#else
            throw new NotSupportedException();
#endif
        }

        private bool IsByRefTarget(int slot)
        {
            if (slot > 0)
            {
                Symbol associatedNonMemberSymbol = GetNonFieldSymbol(slot);

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

        private int GetValueSlotForAssignment(BoundExpression value)
        {
            if (value != null)
            {
                return MakeSlot(value);
            }

            return -1;
        }

        private void ReportStaticNullCheckingDiagnostics(ErrorCode errorCode, SyntaxNode syntaxNode, params object[] arguments)
        {
            Diagnostics.Add(errorCode, syntaxNode.GetLocation(), arguments);
        }

        private void InheritNullableStateOfTrackableStruct(TypeSymbol targetType, int targetSlot, int valueSlot, bool isByRefTarget)
        {
            Debug.Assert(targetSlot > 0);
            Debug.Assert(EmptyStructTypeCache.IsTrackableStructType(targetType));

            foreach (var field in _emptyStructTypeCache.GetStructInstanceFields(targetType))
            {
                InheritNullableStateOfFieldOrProperty(targetSlot, valueSlot, field, isByRefTarget);
            }
        }

        private void InheritNullableStateOfFieldOrProperty(int targetContainerSlot, int valueContainerSlot, Symbol fieldOrProperty, bool isByRefTarget)
        {
            TypeSymbolWithAnnotations fieldOrPropertyType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(fieldOrProperty);

            if (fieldOrPropertyType.IsReferenceType)
            {
                // If statically declared as not-nullable, no need to adjust the tracking info. 
                // Declaration information takes priority.
                if (fieldOrPropertyType.IsNullable != false)
                {
                    int targetMemberSlot = GetOrCreateSlot(fieldOrProperty, targetContainerSlot);
                    if (targetMemberSlot >= this.State.Capacity) Normalize(ref this.State);

                    if (isByRefTarget)
                    {
                        // This is a property/field acesses through a by ref entity and it isn't considered declared as not-nullable. 
                        // Since reference can point to the heap, we cannot assume the property/field doesn't have null value after this assignment,
                        // regardless of what value is being assigned.
                        this.State[targetMemberSlot] = (fieldOrPropertyType.IsNullable == true) ? (bool?)false : null;
                    }
                    else if (valueContainerSlot > 0)
                    {
                        int valueMemberSlot = VariableSlot(fieldOrProperty, valueContainerSlot);
                        this.State[targetMemberSlot] = valueMemberSlot > 0 && valueMemberSlot < this.State.Capacity ?
                            this.State[valueMemberSlot] :
                            null;
                    }
                    else
                    {
                        // No tracking information for the value. We need to fill tracking state for the target
                        // with information inferred from the declaration. 
                        Debug.Assert(fieldOrPropertyType.IsNullable != false);

                        this.State[targetMemberSlot] = (fieldOrPropertyType.IsNullable == true) ? (bool?)false : null;
                    }
                }

                if (fieldOrPropertyType.TypeSymbol.IsAnonymousType && fieldOrPropertyType.TypeSymbol.IsClassType())
                {
                    InheritNullableStateOfAnonymousTypeInstance(fieldOrPropertyType.TypeSymbol,
                                                                GetOrCreateSlot(fieldOrProperty, targetContainerSlot),
                                                                valueContainerSlot > 0 ? GetOrCreateSlot(fieldOrProperty, valueContainerSlot) : -1, isByRefTarget);
                }
            }
            else if (EmptyStructTypeCache.IsTrackableStructType(fieldOrPropertyType.TypeSymbol))
            {
                var slot = GetOrCreateSlot(fieldOrProperty, targetContainerSlot);
                if (slot > 0)
                {
                    InheritNullableStateOfTrackableStruct(fieldOrPropertyType.TypeSymbol,
                                                          slot,
                                                          valueContainerSlot > 0 ? GetOrCreateSlot(fieldOrProperty, valueContainerSlot) : -1, isByRefTarget);
                }
            }
        }

        private void InheritNullableStateOfAnonymousTypeInstance(TypeSymbol targetType, int targetSlot, int valueSlot, bool isByRefTarget)
        {
            Debug.Assert(targetSlot > 0);
            Debug.Assert(targetType.IsAnonymousType && targetType.IsClassType());

            foreach (var member in targetType.GetMembersUnordered())
            {
                if (member.Kind != SymbolKind.Property)
                {
                    continue;
                }

                var propertySymbol = (PropertySymbol)member;

                if (!IsTrackableAnonymousTypeProperty(propertySymbol))
                {
                    continue;
                }

                InheritNullableStateOfFieldOrProperty(targetSlot, valueSlot, propertySymbol, isByRefTarget);
            }
        }

        protected override LocalState ReachableState()
        {
            return new LocalState(BitVector.Create(nextVariableSlot), BitVector.Create(nextVariableSlot), null);
        }

        protected override LocalState UnreachableState()
        {
            return new LocalState(BitVector.Empty, BitVector.Empty, null);
        }

        protected override LocalState AllBitsSet()
        {
            return new LocalState(BitVector.Create(nextVariableSlot), BitVector.Create(nextVariableSlot), null);
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
                    TypeSymbolWithAnnotations paramType = parameter.Type;

                    if (paramType.IsReferenceType)
                    {
                        if (paramType.IsNullable != false)
                        {
                            if (slot >= this.State.Capacity) Normalize(ref this.State);

                            this.State[slot] = (paramType.IsNullable == true) ? (bool?)false : null;
                        }

                        if (paramType.TypeSymbol.IsAnonymousType && paramType.TypeSymbol.IsClassType())
                        {
                            InheritNullableStateOfAnonymousTypeInstance(paramType.TypeSymbol, slot, -1, parameter.RefKind != RefKind.None);
                        }
                    }
                    else if (EmptyStructTypeCache.IsTrackableStructType(paramType.TypeSymbol))
                    {
                        InheritNullableStateOfTrackableStruct(paramType.TypeSymbol, slot, -1, parameter.RefKind != RefKind.None);
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

            return result;
        }

        public override void VisitPattern(BoundExpression expression, BoundPattern pattern)
        {
            base.VisitPattern(expression, pattern);
            var whenFail = StateWhenFalse;
            SetState(StateWhenTrue);
            AssignPatternVariables(pattern);
            SetConditionalState(this.State, whenFail);
        }

        private void AssignPatternVariables(BoundPattern pattern)
        {
            switch (pattern.Kind)
            {
                case BoundKind.DeclarationPattern:
                    {
                        var pat = (BoundDeclarationPattern)pattern;
                        Assign(pat, null, State.ResultType, RefKind.None);
                        break;
                    }
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

        public override BoundNode VisitBlock(BoundBlock node)
        {
            DeclareVariables(node.Locals);

            VisitStatementsWithLocalFunctions(node);

            return null;
        }

        protected override BoundNode VisitReturnStatementNoAdjust(BoundReturnStatement node)
        {
            var result = base.VisitReturnStatementNoAdjust(node);

            Debug.Assert(!IsConditionalState);
            if (node.ExpressionOpt != null && this.State.Reachable)
            {
                TypeSymbolWithAnnotations returnType = this._currentMethodOrLambda?.ReturnType;

                if (this.State.ResultIsNullable == true)
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

        // PROTOTYPE(NullableReferenceTypes): Move some of the Visit
        // methods to the base class, to share with DataFlowPass.
        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            DeclareVariables(node.InnerLocals);
            var result = base.VisitSwitchStatement(node);
            return result;
        }

        public override BoundNode VisitPatternSwitchStatement(BoundPatternSwitchStatement node)
        {
            DeclareVariables(node.InnerLocals);
            var result = base.VisitPatternSwitchStatement(node);
            return result;
        }

        protected override void VisitPatternSwitchSection(BoundPatternSwitchSection node, BoundExpression switchExpression, bool isLastSection)
        {
            DeclareVariables(node.Locals);
            base.VisitPatternSwitchSection(node, switchExpression, isLastSection);
        }

        public override BoundNode VisitForStatement(BoundForStatement node)
        {
            DeclareVariables(node.OuterLocals);
            DeclareVariables(node.InnerLocals);
            var result = base.VisitForStatement(node);
            return result;
        }

        public override BoundNode VisitDoStatement(BoundDoStatement node)
        {
            DeclareVariables(node.Locals);
            var result = base.VisitDoStatement(node);
            return result;
        }

        public override BoundNode VisitWhileStatement(BoundWhileStatement node)
        {
            DeclareVariables(node.Locals);
            var result = base.VisitWhileStatement(node);
            return result;
        }

        /// <remarks>
        /// Variables declared in a using statement are always considered used, so this is just an assert.
        /// </remarks>
        public override BoundNode VisitUsingStatement(BoundUsingStatement node)
        {
            var localsOpt = node.Locals;
            DeclareVariables(localsOpt);
            var result = base.VisitUsingStatement(node);
            return result;
        }

        public override BoundNode VisitFixedStatement(BoundFixedStatement node)
        {
            DeclareVariables(node.Locals);
            return base.VisitFixedStatement(node);
        }

        public override BoundNode VisitSequence(BoundSequence node)
        {
            DeclareVariables(node.Locals);
            var result = base.VisitSequence(node);

            if (node.Value == null)
            {
                SetUnknownResultNullability();
            }

            return result;
        }

        // PROTOTYPE(NullableReferenceTypes): Remove if not needed.
        private void DeclareVariables(ImmutableArray<LocalSymbol> locals)
        {
            foreach (var symbol in locals)
            {
                DeclareVariable(symbol);
            }
        }

        private void DeclareVariable(LocalSymbol symbol)
        {
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            // Note: the caller should avoid allowing this to be called for the left-hand-side of
            // an assignment (if a simple variable or this-qualified or deconstruction variables) or an out parameter.
            // That's because this code assumes the variable is being read, not written.
            LocalSymbol localSymbol = node.LocalSymbol;

            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable)
            {
                this.State.Result = GetResult(node, localSymbol);
            }

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
                var value = this.State.Result;

                TrackNullableStateForAssignment(node, slot, local.Type, value, inferNullability: node.DeclaredType.InferredType);

            }
            return null;
        }

        protected override BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            Debug.Assert(!IsConditionalState);
            this.State.ResultType = _invalidType;
            var result = base.VisitExpressionWithoutStackGuard(node);
            // Verify Visit method set ResultType.
            Debug.Assert(IsConditionalState || (object)this.State.ResultType != _invalidType);
            return result;
        }

        protected override void VisitStatement(BoundStatement statement)
        {
            base.VisitStatement(statement);
            this.State.ResultType = null;
        }

        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            LocalSymbol saveImplicitReceiver = _implicitReceiver;
            _implicitReceiver = null;

            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable &&
                EmptyStructTypeCache.IsTrackableStructType(node.Type))
            {
                _implicitReceiver = GetOrCreateObjectCreationPlaceholder(node);
                var slot = MakeSlot(node);
                if (slot > 0)
                {
                    InheritNullableStateOfTrackableStruct(node.Type, slot, -1, false);
                }
            }

            var result = base.VisitObjectCreationExpression(node);
            SetResult(node);

            _implicitReceiver = saveImplicitReceiver;
            return result;
        }

        private void SetResult(BoundExpression node)
        {
            Debug.Assert(!IsConditionalState);

            // PROTOTYPE(NullableReferenceTypes): Is it necessary to check
            // this.State.Reachable for null checks, here or elsewhere?
            //if (this.State.Reachable)
            {
                this.State.Result = new TypeAndSlot(node, TypeSymbolWithAnnotations.Create(node.Type));
            }
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
            if (this.State.Reachable)
            {
                ObjectCreationPlaceholderLocal implicitReceiver = GetOrCreateObjectCreationPlaceholder(node);
                int receiverSlot = -1;

                //  visit arguments as r-values
                var arguments = node.Arguments;
                var constructor = node.Constructor;
                for (int i = 0; i < arguments.Length; i++)
                {
                    VisitArgumentAsRvalue(arguments[i], constructor.Parameters[i], expanded: false);

                    // PROTOTYPE(NullableReferenceTypes): node.Declarations includes
                    // explicitly -named properties only. For now, skip expressions
                    // with implicit names. See StaticNullChecking.AnonymousTypes_05.
                    if (node.Declarations.Length < arguments.Length)
                    {
                        continue;
                    }

                    PropertySymbol property = node.Declarations[i].Property;

                    if (IsTrackableAnonymousTypeProperty(property))
                    {
                        if (receiverSlot <= 0)
                        {
                            receiverSlot = GetOrCreateSlot(implicitReceiver);
                        }

                        TrackNullableStateForAssignment(arguments[i], property, GetOrCreateSlot(property, receiverSlot), arguments[i], this.State.ResultType);
                    }
                }

                if (_trackExceptions) NotePossibleException(node);

                this.State.ResultType = TypeSymbolWithAnnotations.Create(node.Type);
                return null;
            }
            else
            {
                return base.VisitAnonymousObjectCreationExpression(node);
            }
        }

        protected override void VisitArrayInitializer(BoundArrayCreation arrayCreation, BoundArrayInitialization node)
        {
            this.State.ResultType = null;
            bool isImplicitArrayCreation = arrayCreation.Syntax.Kind() == SyntaxKind.ImplicitArrayCreationExpression;
            var builder = isImplicitArrayCreation ? ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance() : null;

            foreach (var child in node.Initializers)
            {
                if (child.Kind == BoundKind.ArrayInitialization)
                {
                    VisitArrayInitializer(arrayCreation, (BoundArrayInitialization)child);
                }
                else
                {
                    VisitRvalue(child);
                    Debug.Assert(!IsConditionalState);
                    if (this.State.Reachable)
                    {
                        TypeSymbolWithAnnotations elementType = (arrayCreation.Type as ArrayTypeSymbol)?.ElementType;
                        if (!isImplicitArrayCreation && elementType?.IsReferenceType == true)
                        {
                            // Pass array type symbol as the target for the assignment. 
                            TrackNullableStateForAssignment(child, arrayCreation.Type, -1, child, this.State.ResultType);
                        }
                    }
                }
                if (builder != null)
                {
                    builder.Add(this.State.ResultType);
                }
            }

            var arrayType = (ArrayTypeSymbol)arrayCreation.Type;
            if (builder != null)
            {
                var initializerTypes = builder.ToImmutableAndFree();
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                var elementType = BestTypeInferrer.InferBestType(initializerTypes, compilation.Conversions, includeNullability: true, useSiteDiagnostics: ref useSiteDiagnostics);
                for (int i = 0; i < initializerTypes.Length; i++)
                {
                    ReportNullabilityMismatchIfAny(node.Initializers[i], elementType, initializerTypes[i]);
                }
                arrayType = arrayType.WithElementType(elementType);
            }

            this.State.ResultType = TypeSymbolWithAnnotations.Create(arrayType, isNullableIfReferenceType: false);
        }

        public override BoundNode VisitArrayAccess(BoundArrayAccess node)
        {
            Debug.Assert(!IsConditionalState);

            VisitRvalue(node.Expression);

            Debug.Assert(!IsConditionalState);
            // No need to check expression type since System.Array is a reference type.
            Debug.Assert(node.Expression.Type.IsReferenceType);
            CheckPossibleNullReceiver(node.Expression, checkType: false);

            var type = this.State.ResultType?.TypeSymbol as ArrayTypeSymbol;

            foreach (var i in node.Indices)
            {
                VisitRvalue(i);
            }

            //if (this.State.Reachable)
            {
                this.State.ResultType = type?.ElementType;
            }

            return null;
        }

        private TypeSymbolWithAnnotations InferResultNullability(BoundBinaryOperator node, TypeSymbolWithAnnotations leftType, TypeSymbolWithAnnotations rightType)
        {
            return InferResultNullability(node.OperatorKind, node.MethodOpt, node.Type, leftType, rightType);
        }

        private TypeSymbolWithAnnotations InferResultNullability(BinaryOperatorKind operatorKind, MethodSymbol methodOpt, TypeSymbol resultType, TypeSymbolWithAnnotations leftType, TypeSymbolWithAnnotations rightType)
        {
            bool? isNullable = InferResultNullability(operatorKind, methodOpt, resultType, leftType?.IsNullable, rightType?.IsNullable);
            return TypeSymbolWithAnnotations.Create(resultType, isNullable);
        }

        private bool? InferResultNullability(BinaryOperatorKind operatorKind, MethodSymbol methodOpt, TypeSymbol resultType, bool? leftIsNullable, bool? rightIsNullable)
        {
            if (operatorKind.IsUserDefined())
            {
                if ((object)methodOpt != null && methodOpt.ParameterCount == 2)
                {
                    return IsResultNullable(methodOpt);
                }
                else
                {
                    return null;
                }
            }
            else if (operatorKind.IsDynamic())
            {
                return null;
            }
            else if (resultType.IsReferenceType == true)
            {
                switch (operatorKind.Operator() | operatorKind.OperandTypes())
                {
                    case BinaryOperatorKind.DelegateCombination:
                        if (leftIsNullable == false || rightIsNullable == false)
                        {
                            return false;
                        }
                        else if (leftIsNullable == true && rightIsNullable == true)
                        {
                            return true;
                        }
                        else
                        {
                            Debug.Assert(leftIsNullable == null || rightIsNullable == null);
                            return null;
                        }

                    case BinaryOperatorKind.DelegateRemoval:
                        return true; // Delegate removal can produce null.
                }

                return false;
            }
            else
            {
                return null;
            }
        }

        protected override void AfterLeftChildHasBeenVisited(BoundBinaryOperator binary)
        {
            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                TypeSymbolWithAnnotations leftType = this.State.ResultType;
                bool warnOnNullReferenceArgument = (binary.OperatorKind.IsUserDefined() && (object)binary.MethodOpt != null && binary.MethodOpt.ParameterCount == 2);

                if (warnOnNullReferenceArgument)
                {
                    WarnOnNullReferenceArgument(binary.Left, leftType?.IsNullable, binary.MethodOpt.Parameters[0], expanded: false);
                }

                VisitRvalue(binary.Right);
                Debug.Assert(!IsConditionalState);
                // At this point, State.Reachable may be false for
                // invalid code such as `s + throw new Exception()`.
                TypeSymbolWithAnnotations rightType = this.State.ResultType;

                if (warnOnNullReferenceArgument)
                {
                    WarnOnNullReferenceArgument(binary.Right, rightType?.IsNullable, binary.MethodOpt.Parameters[1], expanded: false);
                }

                AfterRightChildHasBeenVisited(binary);

                Debug.Assert(!IsConditionalState);
                this.State.ResultType = InferResultNullability(binary, leftType, rightType);

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
            else
            {
                base.AfterLeftChildHasBeenVisited(binary);
                Debug.Assert(!IsConditionalState);
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
            if (leftState.ResultType?.IsNullable == false)
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.HDN_ExpressionIsProbablyNeverNull, node.LeftOperand.Syntax);
            }

            bool leftIsConstant = node.LeftOperand.ConstantValue != null;
            if (leftIsConstant)
            {
                SetUnreachable();
            }
            VisitRvalue(node.RightOperand);

            var leftType = InferResultNullability(node.LeftConversion, node.LeftOperand.Type, node.Type, leftState.ResultType);
            var rightType = this.State.ResultType;

            IntersectWith(ref this.State, ref leftState);

            if (node.HasErrors)
            {
                this.State.ResultType = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: null);
            }
            else
            {
                Debug.Assert((object)leftType == null || leftType.TypeSymbol.Equals(node.Type, TypeCompareKind.ConsiderEverything));
                Debug.Assert((object)rightType == null || rightType.TypeSymbol.Equals(node.Type, TypeCompareKind.ConsiderEverything));

                var resultType = TypeSymbolWithAnnotations.Create((leftType ?? rightType)?.TypeSymbol, isNullableIfReferenceType: rightType?.IsNullable & leftType?.IsNullable);
                this.State.ResultType = resultType;

                ReportNullabilityMismatchIfAny(node.LeftOperand, resultType, leftType);
                ReportNullabilityMismatchIfAny(node.RightOperand, resultType, rightType);
            }
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

            if (!(this.State.Reachable) || node.Receiver.ConstantValue != null || node.Receiver.Type?.IsReferenceType != true)
            {
                // PROTOTYPE(NullableReferenceTypes): Are we setting this.State.ResultType to T? if receiver == null?
                return base.VisitConditionalAccess(node);
            }

            VisitRvalue(node.Receiver);
            var receiverState = this.State.Clone();

            BoundExpression operandComparedToNull = node.Receiver;

            if (receiverState.ResultIsNullable == false)
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.HDN_ExpressionIsProbablyNeverNull, node.Receiver.Syntax);
            }

            operandComparedToNull = SkipReferenceConversions(operandComparedToNull);
            int slot = MakeSlot(operandComparedToNull);

            if (slot > 0)
            {
                if (slot >= this.State.Capacity) Normalize(ref this.State);

                this.State[slot] = true;
            }

            VisitRvalue(node.AccessExpression);

            IntersectWith(ref this.State, ref receiverState);
            // PROTOTYPE(NullableReferenceTypes): Use flow analysis type rather than node.Type
            // so that nested nullability is inferred from flow analysis. See VisitConditionalOperator.
            this.State.ResultType = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: receiverState.ResultType?.IsNullable | this.State.ResultType?.IsNullable);
            // PROTOTYPE(NullableReferenceTypes): Report conversion warnings.
            return null;
        }

        public override BoundNode VisitConditionalOperator(BoundConditionalOperator node)
        {
            var isByRef = node.IsByRef;

            VisitCondition(node.Condition);
            var consequenceState = this.StateWhenTrue;
            var alternativeState = this.StateWhenFalse;
            if (IsConstantTrue(node.Condition))
            {
                VisitConditionalOperand(alternativeState, node.Alternative, isByRef);
                VisitConditionalOperand(consequenceState, node.Consequence, isByRef);
                // it may be a boolean state at this point.
            }
            else if (IsConstantFalse(node.Condition))
            {
                VisitConditionalOperand(consequenceState, node.Consequence, isByRef);
                VisitConditionalOperand(alternativeState, node.Alternative, isByRef);
                // it may be a boolean state at this point.
            }
            else
            {
                VisitConditionalOperand(consequenceState, node.Consequence, isByRef);
                Unsplit();
                consequenceState = this.State;
                VisitConditionalOperand(alternativeState, node.Alternative, isByRef);
                Unsplit();
                IntersectWith(ref this.State, ref consequenceState);
                var consequenceType = consequenceState.ResultType;
                var alternativeType = this.State.ResultType;
                this.State.ResultType = (object)consequenceType == null || (object)alternativeType == null ?
                    TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: true) :
                    GetMoreNullableType(consequenceType, alternativeType);
                // it may not be a boolean state at this point (5.3.3.28)
                // PROTOTYPE(NullableReferenceTypes): Report conversion warnings.
            }

            return null;
        }

        // Return the type that is "more nullable". Assumes types are equal, ignoring nullability.
        private static TypeSymbolWithAnnotations GetMoreNullableType(TypeSymbolWithAnnotations typeA, TypeSymbolWithAnnotations typeB)
        {
            Debug.Assert(typeA.Equals(typeB, TypeCompareKind.ConsiderEverything));
            bool? isNullableA = typeA.IsNullable;
            bool? isNullableB = typeB.IsNullable;
            if (isNullableA == true) return typeA;
            if (isNullableB == true) return typeB;
            if (isNullableA == null) return typeA;
            if (isNullableB == null) return typeB;
            return typeA;
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

            // PROTOTYPE(NullableReferenceTypes): Should skip state set in
            // arguments of omitted call. See PreciseAbstractFlowPass.VisitCall.
            Debug.Assert(!method.CallsAreOmitted(node.SyntaxTree));

            VisitReceiverBeforeCall(node.ReceiverOpt, method);

            var arguments = VisitArguments(node.Arguments, node.ArgumentRefKindsOpt, method, node.ArgsToParamsOpt, node.Expanded);
            if (method.IsGenericMethod)
            {
                method = InferMethod(node.Syntax, method, arguments.SelectAsArray(t => t.Type));
            }

            bool expanded = node.Expanded;
            for (int i = 0; i < arguments.Length; i++)
            {
                var parameter = GetCorrespondingParameter(i, method, node.ArgsToParamsOpt, ref expanded);
                if ((object)parameter != null)
                {
                    WarnOnNullReferenceArgument(arguments[i], parameter, expanded);
                }
            }

            UpdateStateForCall(node);
            VisitReceiverAfterCall(node.ReceiverOpt, method);

            if (method.MethodKind == MethodKind.LocalFunction)
            {
                var localFunc = (LocalFunctionSymbol)method.OriginalDefinition;
                ReplayReadsAndWrites(localFunc, node.Syntax, writes: true);
            }

            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable)
            {
                this.State.Result = new TypeAndSlot(node, GetTypeOrReturnTypeWithAdjustedNullableAnnotations(method));
            }

            return null;
        }

        private ImmutableArray<TypeAndSlot> VisitArguments(ImmutableArray<BoundExpression> arguments, ImmutableArray<RefKind> refKindsOpt, MethodSymbol method, ImmutableArray<int> argsToParamsOpt, bool expanded)
        {
            Debug.Assert(!IsConditionalState);

            // PROTOTYPE(NullableReferenceTypes): Handle ref and out
            // parameters. See PreciseAbstractFlowPass.VisitArguments.
            Debug.Assert(refKindsOpt.IsDefault);

            var builder = ArrayBuilder<TypeAndSlot>.GetInstance();
            for (int i = 0; i < arguments.Length; i++)
            {
                VisitArgumentAsRvalue(arguments, i, method, argsToParamsOpt, expanded);
                builder.Add(this.State.Result);
            }
            return builder.ToImmutableAndFree();
        }

        private MethodSymbol InferMethod(SyntaxNode syntax, MethodSymbol method, ImmutableArray<TypeSymbolWithAnnotations> argumentTypes)
        {
            Debug.Assert(method.IsGenericMethod);
            var definition = method.ConstructedFrom;
            // PROTOTYPE(NullableReferenceTypes): MethodTypeInferrer.Infer relies
            // on the BoundExpressions for tuple element types and method groups.
            // By using a generic BoundValuePlaceholder, we're losing inference in those cases.
            var arguments = argumentTypes.SelectAsArray((t, s) => (BoundExpression)new BoundValuePlaceholder(s, t.IsNullable, t.TypeSymbol), syntax);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var result = MethodTypeInferrer.Infer(
                null, // PROTOTYPE(NullableReferenceTypes): Binder.
                compilation.Conversions,
                definition.TypeParameters,
                definition.ContainingType,
                definition.ParameterTypes,
                definition.Parameters.SelectAsArray(p => p.RefKind),
                arguments,
                ref useSiteDiagnostics,
                includeNullability: true);
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

        private bool? IsResultNullable(Symbol resultSymbol)
        {
            TypeSymbolWithAnnotations resultType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(resultSymbol);

            if (!resultType.IsVoid && resultType.IsReferenceType)
            {
                return resultType.IsNullable;
            }
            else
            {
                return null;
            }
        }

        // PROTOTYPE(NullableReferenceTypes): Argument shouldn't be a node, it should be a containing slot.
        private TypeAndSlot GetResult(BoundExpression node, Symbol resultSymbol)
        {
            resultSymbol = AsMemberOfResultType(resultSymbol);
            TypeSymbolWithAnnotations resultType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(resultSymbol);

            if ((object)resultType != null && !resultType.IsVoid && resultType.IsReferenceType)
            {
                // If the symbol is statically declared as not-nullable
                // or null-oblivious, ignore flow state.
                if (resultType.IsNullable != true)
                {
                    return new TypeAndSlot(node, resultType);
                }

                int slot = MakeSlot(node);

                // We are supposed to track information for the node. Use whatever we managed to
                // accumulate so far.
                if (slot > 0 && slot < this.State.Capacity)
                {
                    var isNullable = !this.State[slot];
                    if (isNullable != resultType.IsNullable)
                    {
                        resultType = TypeSymbolWithAnnotations.Create(resultType.TypeSymbol, isNullable);
                    }
                    return new TypeAndSlot(node, resultType, slot);
                }
            }

            return new TypeAndSlot(node, resultType);
        }

        private Symbol AsMemberOfResultType(Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                case SymbolKind.Property:
                case SymbolKind.Event:
                case SymbolKind.Method:
                case SymbolKind.NamedType:
                    var containingType = (NamedTypeSymbol)this.State.Result.Type.TypeSymbol;
                    return symbol.OriginalDefinition.SymbolAsMember(containingType);
                default:
                    return symbol;
            }
        }

        protected override void VisitReceiverBeforeCall(BoundExpression receiverOpt, MethodSymbol method)
        {
            base.VisitReceiverBeforeCall(receiverOpt, method);

            Debug.Assert(!IsConditionalState);
            if ((object)method != null && !method.IsStatic && method.MethodKind != MethodKind.Constructor)
            {
                CheckPossibleNullReceiver(receiverOpt);
            }
        }

        protected override void VisitFieldReceiverAsRvalue(BoundExpression receiverOpt, FieldSymbol fieldSymbol)
        {
            base.VisitFieldReceiverAsRvalue(receiverOpt, fieldSymbol);

            Debug.Assert(!IsConditionalState);
            if ((object)fieldSymbol != null && !fieldSymbol.IsStatic)
            {
                CheckPossibleNullReceiver(receiverOpt);
            }
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

            return base.VisitConversion(node);
        }

        protected override void VisitConversionNoConditionalState(BoundConversion node)
        {
            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                switch (node.ConversionKind)
                {
                    case ConversionKind.ExplicitUserDefined:
                    case ConversionKind.ImplicitUserDefined:
                        if ((object)node.SymbolOpt != null && node.SymbolOpt.ParameterCount == 1)
                        {
                            WarnOnNullReferenceArgument(node.Operand, this.State.ResultIsNullable, node.SymbolOpt.Parameters[0], expanded: false);
                        }
                        break;

                    case ConversionKind.AnonymousFunction:
                        if (!node.ExplicitCastInCode && node.Operand.Kind == BoundKind.Lambda)
                        {
                            var lambda = (BoundLambda)node.Operand;
                            ReportNullabilityMismatchWithTargetDelegate(node.Operand.Syntax, node.Type.GetDelegateType(), lambda.Symbol);
                        }
                        break;

                    case ConversionKind.MethodGroup:
                        if (!node.ExplicitCastInCode)
                        {
                            ReportNullabilityMismatchWithTargetDelegate(node.Operand.Syntax, node.Type.GetDelegateType(), node.SymbolOpt);
                        }
                        break;
                }
            }

            var operand = node.Operand;
            if (operand.Kind == BoundKind.Literal &&
                (object)operand.Type == null &&
                operand.ConstantValue.IsNull)
            {
                this.State.Result = new TypeAndSlot(node, TypeSymbolWithAnnotations.Create(node.Type, true));
            }
            else
            {
                this.State.ResultType = InferResultNullability(
                    node.Conversion,
                    operand.Type,
                    node.Type,
                    this.State.ResultType);
            }
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

        private TypeSymbolWithAnnotations InferResultNullability(Conversion conversion, TypeSymbol sourceTypeOpt, TypeSymbol targetType, TypeSymbolWithAnnotations operandType)
        {
            bool? isNullable = InferResultNullability(conversion, sourceTypeOpt, targetType, operandType?.IsNullable);
            return TypeSymbolWithAnnotations.Create(targetType, isNullable);
        }

        private bool? InferResultNullability(Conversion conversion, TypeSymbol sourceTypeOpt, TypeSymbol targetType, bool? operandIsNullable)
        {
            if (targetType.IsReferenceType)
            {
                switch (conversion.Kind)
                {
                    case ConversionKind.MethodGroup:
                    case ConversionKind.AnonymousFunction:
                    case ConversionKind.InterpolatedString:
                        return false;

                    case ConversionKind.ExplicitUserDefined:
                    case ConversionKind.ImplicitUserDefined:
                        var methodOpt = conversion.Method;
                        if ((object)methodOpt != null && methodOpt.ParameterCount == 1)
                        {
                            return IsResultNullable(methodOpt);
                        }
                        else
                        {
                            return null;
                        }

                    case ConversionKind.Unboxing:
                    case ConversionKind.ExplicitDynamic:
                    case ConversionKind.ImplicitDynamic:
                    case ConversionKind.NoConversion:
                    case ConversionKind.ImplicitThrow:
                        return null;

                    case ConversionKind.Boxing:
                        if (sourceTypeOpt?.IsValueType == true)
                        {
                            // PROTOTYPE(NullableReferenceTypes): Should we worry about a pathological case of boxing nullable value known to be not null?
                            //       For example, new int?(0)
                            return sourceTypeOpt.IsNullableType();
                        }
                        else
                        {
                            Debug.Assert(sourceTypeOpt?.IsReferenceType != true);
                            return null;
                        }

                    case ConversionKind.DefaultOrNullLiteral:
                    case ConversionKind.Identity:
                    case ConversionKind.ImplicitReference:
                    case ConversionKind.ExplicitReference:
                        // Inherit state from the operand
                        return operandIsNullable;

                    case ConversionKind.Deconstruction:
                        // Can reach here, with an error type, when the
                        // Deconstruct method is missing or inaccessible.
                        return null;

                    case ConversionKind.ExplicitEnumeration:
                        // Can reach here, with an error type.
                        return null;

                    default:
                        Debug.Assert(false);
                        return null;
                }
            }
            else
            {
                return null;
            }
        }

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            if (node.MethodOpt?.MethodKind == MethodKind.LocalFunction)
            {
                var syntax = node.Syntax;
                var localFunc = (LocalFunctionSymbol)node.MethodOpt.OriginalDefinition;
                ReplayReadsAndWrites(localFunc, syntax, writes: false);
            }

            SetResult(node);

            return base.VisitDelegateCreationExpression(node);
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

            //if (this.State.Reachable)
            {
                this.State.ResultType = TypeSymbolWithAnnotations.Create(node.Type);
            }

            return null;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            var result = VisitLambdaOrLocalFunction(node);
            this.State.ResultType = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: null);
            return result;
        }

        public override BoundNode VisitUnboundLambda(UnboundLambda node)
        {
            var result = base.VisitUnboundLambda(node);
            SetUnknownResultNullability();
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
            this.State.ResultType = null;
            foreach (PendingBranch pending in pendingReturns)
            {
                this.State = pending.State;
                IntersectWith(ref finalState, ref this.State); // a no-op except in region analysis
                this.State.ResultType = null;
            }

            this.State = finalState;

            this._currentMethodOrLambda = oldMethodOrLambda;
            return null;
        }

        public override BoundNode VisitThisReference(BoundThisReference node)
        {
            // TODO: in a struct constructor, "this" is not initially assigned.
            SetResult(node);
            return null;
        }

        public override BoundNode VisitParameter(BoundParameter node)
        {
            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable)
            {
                this.State.Result = GetResult(node, node.ParameterSymbol);
            }

            return null;
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            Debug.Assert(!IsConditionalState);

            VisitLvalue(node.Left);
            var left = this.State.Result;

            VisitRvalue(node.Right);

            // byref assignment is also a potential write
            if (node.RefKind != RefKind.None)
            {
                WriteArgument(node.Right, node.RefKind, method: null, parameter: null);
            }

            var right = this.State.Result;
            TrackNullableStateForAssignment(node.Left, left.Slot, left.Type, right);

            //if (this.State.Reachable)
            {
                this.State.Result = right;
            }

            return null;
        }

        protected override void VisitLvalue(BoundLocal node)
        {
            var symbol = ((BoundLocal)node).LocalSymbol;
            int slot = GetOrCreateSlot(symbol);
            var type = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(symbol);
            this.State.Result = new TypeAndSlot(node, type, slot);
        }

        private void VisitLvalue2(BoundExpression node)
        {
            BoundExpression receiverOpt = null;
            Symbol symbol;
            switch (node.Kind)
            {
                case BoundKind.Local:
                    symbol = ((BoundLocal)node).LocalSymbol;
                    break;
                case BoundKind.Parameter:
                    symbol = ((BoundParameter)node).ParameterSymbol;
                    break;
                case BoundKind.FieldAccess:
                    {
                        var fieldAccess = (BoundFieldAccess)node;
                        symbol = fieldAccess.FieldSymbol;
                        receiverOpt = fieldAccess.ReceiverOpt;
                    }
                    break;
                case BoundKind.PropertyAccess:
                    {
                        var propertyAccess = (BoundPropertyAccess)node;
                        symbol = propertyAccess.PropertySymbol;
                        receiverOpt = propertyAccess.ReceiverOpt;
                    }
                    break;
                default:
                    VisitRvalue(node);
                    return;
            }

            if (receiverOpt != null)
            {
                VisitRvalue(receiverOpt);
                symbol = AsMemberOfResultType(symbol);
            }
            int slot = GetOrCreateSlot(symbol);
            var type = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(symbol);
            this.State.Result = new TypeAndSlot(node, type, slot);
        }

        public override BoundNode VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            base.VisitDeconstructionAssignmentOperator(node);
            var resultType = this.State.ResultType;
            Assign(node.Left, node.Right, resultType);
            return null;
        }

        public override BoundNode VisitIncrementOperator(BoundIncrementOperator node)
        {
            VisitRvalue(node.Operand);
            var operandResult = this.State.Result;

            TypeSymbolWithAnnotations resultOfIncrementType;

            Debug.Assert(!IsConditionalState);

            if (this.State.Reachable)
            {
                MethodSymbol incrementOperator = (node.OperatorKind.IsUserDefined() && (object)node.MethodOpt != null && node.MethodOpt.ParameterCount == 1) ? node.MethodOpt : null;
                TypeSymbol targetTypeOfOperandConversion;

                if (node.OperandConversion.IsUserDefined && (object)node.OperandConversion.Method != null && node.OperandConversion.Method.ParameterCount == 1)
                {
                    WarnOnNullReferenceArgument(operandResult, node.OperandConversion.Method.Parameters[0], expanded: false);
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
                    resultOfOperandConversionType = InferResultNullability(node.OperandConversion,
                                                                                node.Operand.Type,
                                                                                targetTypeOfOperandConversion,
                                                                                operandResult.Type);
                }
                else
                {
                    resultOfOperandConversionType = null;
                }

                if ((object)incrementOperator == null)
                {
                    resultOfIncrementType = null;
                }
                else 
                {
                    WarnOnNullReferenceArgument(node.Operand, 
                                                resultOfOperandConversionType?.IsNullable,
                                                incrementOperator.Parameters[0], expanded: false);

                    resultOfIncrementType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(incrementOperator);
                }

                if (node.ResultConversion.IsUserDefined && (object)node.ResultConversion.Method != null && node.ResultConversion.Method.ParameterCount == 1)
                {
                    WarnOnNullReferenceArgument(node, resultOfIncrementType?.IsNullable, node.ResultConversion.Method.Parameters[0], expanded: false);
                }

                resultOfIncrementType = InferResultNullability(node.ResultConversion,
                                                                    incrementOperator?.ReturnType.TypeSymbol,
                                                                    node.Type,
                                                                    resultOfIncrementType);

                var op = node.OperatorKind.Operator();
                if (op == UnaryOperatorKind.PrefixIncrement || op == UnaryOperatorKind.PrefixDecrement)
                {
                    this.State.ResultType = resultOfIncrementType;
                }
                else
                {
                    this.State.Result = operandResult;
                }
            }
            else
            {
                resultOfIncrementType = null;
            }

            TrackNullableStateForAssignment(node, operandResult.Slot, resultOfIncrementType, operandResult);
            return null;
        }

        public override BoundNode VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            VisitCompoundAssignmentTarget(node);

            TypeSymbolWithAnnotations resultType;
            Debug.Assert(!IsConditionalState);

            if (this.State.Reachable)
            {
                TypeSymbolWithAnnotations leftType;
                if (RegularPropertyAccess(node.Left))
                {
                    PropertySymbol property = ((BoundPropertyAccess)node.Left).PropertySymbol;
                    leftType = GetResult(node, property).Type;
                }
                else
                {
                    leftType = this.State.ResultType;
                }

                if (node.LeftConversion.IsUserDefined && (object)node.LeftConversion.Method != null && node.LeftConversion.Method.ParameterCount == 1)
                {
                    WarnOnNullReferenceArgument(node.Left, leftType?.IsNullable, node.LeftConversion.Method.Parameters[0], expanded: false);
                }

                TypeSymbolWithAnnotations resultOfLeftConversionType;

                if ((object)node.Operator.LeftType != null)
                {
                    resultOfLeftConversionType = InferResultNullability(node.LeftConversion,
                                                                             node.Left.Type,
                                                                             node.Operator.LeftType,
                                                                             leftType);
                }
                else
                {
                    resultOfLeftConversionType = null;
                }

                VisitRvalue(node.Right);
                TypeSymbolWithAnnotations rightType = this.State.ResultType;

                AfterRightHasBeenVisited(node);

                if ((object)node.Operator.ReturnType != null)
                {
                    if (node.Operator.Kind.IsUserDefined() && (object)node.Operator.Method != null && node.Operator.Method.ParameterCount == 2)
                    { 
                        WarnOnNullReferenceArgument(node.Left, resultOfLeftConversionType?.IsNullable, node.Operator.Method.Parameters[0], expanded: false);
                        WarnOnNullReferenceArgument(node.Right, rightType?.IsNullable, node.Operator.Method.Parameters[1], expanded: false);
                    }

                    resultType = InferResultNullability(node.Operator.Kind, node.Operator.Method, node.Operator.ReturnType, leftType, rightType);

                    if (node.FinalConversion.IsUserDefined && (object)node.FinalConversion.Method != null && node.FinalConversion.Method.ParameterCount == 1)
                    {
                        WarnOnNullReferenceArgument(node, resultType?.IsNullable, node.FinalConversion.Method.Parameters[0], expanded: false);
                    }

                    resultType = InferResultNullability(node.FinalConversion,
                                                             node.Operator.ReturnType,
                                                             node.Type,
                                                             resultType);
                }
                else
                {
                    resultType = null;
                }

                this.State.ResultType = resultType;
            }
            else
            {
                VisitRvalue(node.Right);
                AfterRightHasBeenVisited(node);
                resultType = null;
            }

            Assign(node.Left, value: node, valueType: resultType); 
            return null;
        }

        public override BoundNode VisitFixedLocalCollectionInitializer(BoundFixedLocalCollectionInitializer node)
        {
            var initializer = node.Expression;

            if (initializer.Kind == BoundKind.AddressOfOperator)
            {
                initializer = ((BoundAddressOfOperator)initializer).Operand;
            }

            // If the node is a fixed statement address-of operator (e.g. fixed(int *p = &...)),
            // then we don't need to consider it for membership in unsafeAddressTakenVariables,
            // because it is either not a local/parameter/range variable (if the variable is
            // non-moveable) or it is and it has a RefKind other than None, in which case it can't
            // be referred to in a lambda (i.e. can't be captured).
            VisitAddressOfOperand(initializer, shouldReadOperand: false);
            return null;
        }

        public override BoundNode VisitAddressOfOperator(BoundAddressOfOperator node)
        {
            SetUnknownResultNullability();
            return null;
        }

        //protected override void VisitArgumentAsRvalue(BoundExpression argument, ParameterSymbol parameter, bool expanded)
        //{
        //    base.VisitArgumentAsRvalue(argument, parameter, expanded);
        //    Debug.Assert(!IsConditionalState);
        //    if ((object)parameter != null && this.State.Reachable)
        //    {
        //        WarnOnNullReferenceArgument(argument, this.State.ResultIsNullable, parameter, expanded);
        //    }
        //}

        private void WarnOnNullReferenceArgument(TypeAndSlot argument, ParameterSymbol parameter, bool expanded)
        {
            TypeSymbolWithAnnotations paramType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(parameter);

            if (argument.Type.IsNullable == true)
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

            if ((object)argument.Type != null && IsNullabilityMismatch(paramType.TypeSymbol, argument.Type.TypeSymbol))
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInArgument, argument.Syntax, argument.Type, paramType.TypeSymbol,
                        new FormattedSymbol(parameter, SymbolDisplayFormat.ShortFormat),
                        new FormattedSymbol(parameter.ContainingSymbol, SymbolDisplayFormat.MinimallyQualifiedFormat));
            }
        }

        // PROTOTYPE(NullableReferenceTypes): Use TypeAndSlot instead.
        private void WarnOnNullReferenceArgument(BoundExpression argument, bool? argumentIsNullable, ParameterSymbol parameter, bool expanded)
        {
            TypeSymbolWithAnnotations paramType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(parameter);

            if (argumentIsNullable == true)
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

            if ((object)argument.Type != null && IsNullabilityMismatch(paramType.TypeSymbol, argument.Type))
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInArgument, argument.Syntax, argument.Type, paramType.TypeSymbol,
                        new FormattedSymbol(parameter, SymbolDisplayFormat.ShortFormat),
                        new FormattedSymbol(parameter.ContainingSymbol, SymbolDisplayFormat.MinimallyQualifiedFormat));
            }
        }

        private TypeSymbolWithAnnotations GetTypeOrReturnTypeWithAdjustedNullableAnnotations(Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Local:
                    var local = (LocalSymbol)symbol;
                    if (_variableTypes.TryGetValue(local, out var type))
                    {
                        return type;
                    }
                    return local.Type;

                case SymbolKind.Parameter:
                    var parameter = (ParameterSymbol)symbol;
                    if (parameter.IsThis)
                    {
                        return parameter.Type;
                    }

                    goto default;

                default:
                    return compilation.GetTypeOrReturnTypeWithAdjustedNullableAnnotations(symbol);
            }
        }

        // PROTOTYPE(NullableReferenceTypes): Remove.
        protected override void WriteArgument(BoundExpression arg, RefKind refKind, MethodSymbol method, ParameterSymbol parameter)
        {
#if false
            Debug.Assert(!IsConditionalState);
            TypeSymbolWithAnnotations valueType = null;
            BoundValuePlaceholder value = null;

            if ((object)parameter != null)
            {
                TypeSymbolWithAnnotations paramType;

                if (this.State.Reachable)
                {
                    paramType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(parameter);
                }
                else
                {
                    paramType = parameter.Type; 
                }

                value = new BoundValuePlaceholder(arg.Syntax, paramType.IsNullable, paramType.TypeSymbol) { WasCompilerGenerated = true };
                valueType = paramType;
            }

            Assign(arg, value, valueType);
#else
            throw new NotSupportedException();
#endif
        }

        public override BoundNode VisitBaseReference(BoundBaseReference node)
        {
            SetResult(node);
            return null;
        }

        protected override void VisitCatchBlock(BoundCatchBlock catchBlock, ref LocalState finallyState)
        {
            DeclareVariables(catchBlock.Locals);

            var exceptionSource = catchBlock.ExceptionSourceOpt;
            if (exceptionSource != null)
            {
                Assign(exceptionSource, value: null, valueType: TypeSymbolWithAnnotations.Create(exceptionSource.Type));
            }

            base.VisitCatchBlock(catchBlock, ref finallyState);
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            var result = base.VisitFieldAccess(node);

            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable)
            {
                this.State.Result = GetResult(node, node.FieldSymbol);
            }

            return result;
        }

        public override BoundNode VisitPropertyAccess(BoundPropertyAccess node)
        {
            var result = base.VisitPropertyAccess(node);

            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable)
            {
                var property = node.PropertySymbol;
                this.State.Result = GetResult(node, property);
            }

            return result;
        }

        public override BoundNode VisitIndexerAccess(BoundIndexerAccess node)
        {
            var result = base.VisitIndexerAccess(node);

            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable)
            {
                this.State.ResultType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(node.Indexer);
            }

            return result;
        }

        public override BoundNode VisitEventAccess(BoundEventAccess node)
        {
            var result = base.VisitEventAccess(node);
            // special definite assignment behavior for events of struct local variables.

            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable)
            {
                this.State.Result = GetResult(node, node.EventSymbol);
            }

            return result;
        }

        // PROTOTYPE(NullableReferenceTypes): Remove if not needed.
        public override void VisitForEachIterationVariables(BoundForEachStatement node)
        {
            // declare and assign all iteration variables
            foreach (var iterationVariable in node.IterationVariables)
            {
            }
        }

        public override BoundNode VisitObjectInitializerMember(BoundObjectInitializerMember node)
        {
            var result = base.VisitObjectInitializerMember(node);

            if ((object)_sourceAssembly != null && node.MemberSymbol != null && node.MemberSymbol.Kind == SymbolKind.Field)
            {
                _sourceAssembly.NoteFieldAccess((FieldSymbol)node.MemberSymbol.OriginalDefinition, read: false, write: true);
            }

            SetUnknownResultNullability();

            return result;
        }

        public override BoundNode VisitDynamicObjectInitializerMember(BoundDynamicObjectInitializerMember node)
        {
            SetUnknownResultNullability();
            return null;
        }

        public override BoundNode VisitBadExpression(BoundBadExpression node)
        {
            var result = base.VisitBadExpression(node);
            this.State.ResultType = TypeSymbolWithAnnotations.Create(node.Type);
            return result;
        }

        public override BoundNode VisitTypeExpression(BoundTypeExpression node)
        {
            var result = base.VisitTypeExpression(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitTypeOrValueExpression(BoundTypeOrValueExpression node)
        {
            var result = base.VisitTypeOrValueExpression(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitUnaryOperator(BoundUnaryOperator node)
        {
            Debug.Assert(!IsConditionalState);

            var result = base.VisitUnaryOperator(node);

            Debug.Assert(!IsConditionalState || node.OperatorKind == UnaryOperatorKind.BoolLogicalNegation);
            if (IsConditionalState)
            {
                if (this.StateWhenFalse.Reachable)
                {
                    this.StateWhenFalse.ResultType = null;
                }

                if (this.StateWhenTrue.Reachable)
                {
                    this.StateWhenTrue.ResultType = null;
                }
            }
            else
            {
                if (this.State.Reachable)
                {
                    if (node.OperatorKind.IsUserDefined() && (object)node.MethodOpt != null && node.MethodOpt.ParameterCount == 1)
                    {
                        WarnOnNullReferenceArgument(node.Operand, this.State.ResultIsNullable, node.MethodOpt.Parameters[0], expanded: false);
                    }
                }

                this.State.ResultType = InferResultNullability(node);
            }

            return null;
        }

        private TypeSymbolWithAnnotations InferResultNullability(BoundUnaryOperator node)
        {
            if (node.OperatorKind.IsUserDefined())
            {
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
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitPointerElementAccess(BoundPointerElementAccess node)
        {
            var result = base.VisitPointerElementAccess(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitRefTypeOperator(BoundRefTypeOperator node)
        {
            // Inherit nullable state from the argument.
            return base.VisitRefTypeOperator(node);
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
            SetUnknownResultNullability();
            return result;
        }

        private TypeSymbolWithAnnotations InferResultNullability(BoundUserDefinedConditionalLogicalOperator node)
        {
            if ((object)node.LogicalOperator != null && node.LogicalOperator.ParameterCount == 2)
            {
                return GetTypeOrReturnTypeWithAdjustedNullableAnnotations(node.LogicalOperator);
            }
            else
            {
                return null;
            }
        }

        protected override void VisitCondition(BoundExpression node, bool inExpression = false)
        {
            base.VisitCondition(node, inExpression);

            if (!inExpression)
            {
                if (IsConditionalState)
                {
                    this.StateWhenFalse.ResultType = null;
                    this.StateWhenTrue.ResultType = null;
                }
                else
                {
                    this.State.ResultType = null;
                }
            }
        }

        protected override void AfterLeftChildOfBinaryLogicalOperatorHasBeenVisited(BoundExpression node, BoundExpression right, bool isAnd, bool isBool, ref LocalState leftTrue, ref LocalState leftFalse)
        {
            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                TypeSymbolWithAnnotations leftType = this.State.ResultType;
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
                        throw ExceptionUtilities.Unreachable;
                }

                Debug.Assert((object)trueFalseOperator == null || ((object)logicalOperator != null && left != null));

                if ((object)trueFalseOperator != null)
                {
                    WarnOnNullReferenceArgument(left, leftType?.IsNullable, trueFalseOperator.Parameters[0], expanded: false);
                }

                if ((object)logicalOperator != null)
                {
                    WarnOnNullReferenceArgument(left, leftType?.IsNullable, logicalOperator.Parameters[0], expanded: false);
                }

                Visit(right);

                Debug.Assert(IsConditionalState ? (this.StateWhenFalse.Reachable || this.StateWhenTrue.Reachable) : this.State.Reachable);
                TypeSymbolWithAnnotations rightType = null;

                if (IsConditionalState)
                {
                    if (this.StateWhenFalse.Reachable)
                    {
                        rightType = this.StateWhenFalse.ResultType;
                        this.StateWhenFalse.ResultType = InferResultNullabilityOfBinaryLogicalOperator(node, leftType, rightType);
                    }

                    if (this.StateWhenTrue.Reachable)
                    {
                        var saveRightType = rightType;
                        rightType = this.StateWhenTrue.ResultType;
                        this.StateWhenTrue.ResultType = InferResultNullabilityOfBinaryLogicalOperator(node, leftType, rightType);

                        if (this.StateWhenFalse.Reachable)
                        {
                            // PROTOTYPE(NullableReferenceTypes): ...
                            //rightType &= saveRightType;
                        }
                    }
                }
                else if (this.State.Reachable)
                {
                    rightType = this.State.ResultType;
                    this.State.ResultType = InferResultNullabilityOfBinaryLogicalOperator(node, leftType, rightType);
                }

                if ((object)logicalOperator != null)
                {
                    WarnOnNullReferenceArgument(right, rightType?.IsNullable, logicalOperator.Parameters[1], expanded: false);
                }

                AfterRightChildOfBinaryLogicalOperatorHasBeenVisited(node, right, isAnd, isBool, ref leftTrue, ref leftFalse);
            }
            else
            {
                base.AfterLeftChildOfBinaryLogicalOperatorHasBeenVisited(node, right, isAnd, isBool, ref leftTrue, ref leftFalse);
            }
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
                    throw ExceptionUtilities.Unreachable;
            }
        }

        public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
        {
            var result = base.VisitAwaitExpression(node);

            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable)
            {
                if (!node.Type.IsReferenceType || node.HasErrors || (object)node.GetResult == null)
                {
                    SetUnknownResultNullability();
                }
                else
                {
                    this.State.ResultType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(node.GetResult);
                }
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

            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable)
            {
                this.State.ResultType = (object)node.Type == null ?
                    null :
                    TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: true);
            }

            return result;
        }

        public override BoundNode VisitIsOperator(BoundIsOperator node)
        {
            var result = base.VisitIsOperator(node);
            Debug.Assert(node.Type.SpecialType == SpecialType.System_Boolean);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitAsOperator(BoundAsOperator node)
        {
            var result = base.VisitAsOperator(node);

            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable)
            {
                bool? isNullable = null;
                if (node.Type.IsReferenceType)
                {
                    switch (node.Conversion.Kind)
                    {
                        case ConversionKind.Identity:
                        case ConversionKind.ImplicitReference:
                            // Inherit nullability from the operand
                            isNullable = this.State.ResultIsNullable;
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
                this.State.ResultType = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: isNullable);
            }

            return result;
        }

        public override BoundNode VisitSuppressNullableWarningExpression(BoundSuppressNullableWarningExpression node)
        {
            var result = base.VisitSuppressNullableWarningExpression(node);

            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable)
            {
                this.State.ResultType = this.State.ResultType?.SetUnknownNullabilityForReferenceTypes();
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
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitArgListOperator(BoundArgListOperator node)
        {
            var result = base.VisitArgListOperator(node);
            Debug.Assert((object)node.Type == null);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitLiteral(BoundLiteral node)
        {
            var result = base.VisitLiteral(node);

            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable)
            {
                var constant = node.ConstantValue;

                if (constant != null &&
                    ((object)node.Type != null ? node.Type.IsReferenceType : constant.IsNull))
                {
                    this.State.Result = new TypeAndSlot(node, TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: constant.IsNull));
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
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitHostObjectMemberReference(BoundHostObjectMemberReference node)
        {
            var result = base.VisitHostObjectMemberReference(node);
            Debug.Assert(node.WasCompilerGenerated);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitPseudoVariable(BoundPseudoVariable node)
        {
            var result = base.VisitPseudoVariable(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitRangeVariable(BoundRangeVariable node)
        {
            var result = base.VisitRangeVariable(node);
            SetUnknownResultNullability(); // PROTOTYPE(NullableReferenceTypes)
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
            var result = base.VisitDynamicMemberAccess(node);

            Debug.Assert(node.Type.IsDynamic());
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitDynamicInvocation(BoundDynamicInvocation node)
        {
            var result = base.VisitDynamicInvocation(node);

            Debug.Assert(node.Type.IsDynamic());
            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable)
            {
                if (node.Type?.IsReferenceType == true)
                {
                    bool? isNullable = InferResultNullabilityFromApplicableCandidates(StaticCast<Symbol>.From(node.ApplicableMethods));
                    this.State.ResultType = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: isNullable);
                }
                else
                {
                    this.State.ResultType = null;
                }
            }

            return result;
        }

        public override BoundNode VisitEventAssignmentOperator(BoundEventAssignmentOperator node)
        {
            var result = base.VisitEventAssignmentOperator(node);
            SetUnknownResultNullability();
            return result;
        }

        protected override void VisitReceiverOfEventAssignmentAsRvalue(BoundEventAssignmentOperator node)
        {
            base.VisitReceiverOfEventAssignmentAsRvalue(node);

            Debug.Assert(!IsConditionalState);
            var receiverOpt = node.ReceiverOpt;
            if (!node.Event.IsStatic)
            {
                CheckPossibleNullReceiver(receiverOpt);
            }
        }

        public override BoundNode VisitDynamicObjectCreationExpression(BoundDynamicObjectCreationExpression node)
        {
            var result = base.VisitDynamicObjectCreationExpression(node);
            SetResult(node);
            return result;
        }

        public override BoundNode VisitObjectInitializerExpression(BoundObjectInitializerExpression node)
        {
            var result = base.VisitObjectInitializerExpression(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitCollectionInitializerExpression(BoundCollectionInitializerExpression node)
        {
            var result = base.VisitCollectionInitializerExpression(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitCollectionElementInitializer(BoundCollectionElementInitializer node)
        {
            var result = base.VisitCollectionElementInitializer(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitDynamicCollectionElementInitializer(BoundDynamicCollectionElementInitializer node)
        {
            var result = base.VisitDynamicCollectionElementInitializer(node);
            SetUnknownResultNullability();
            return result;
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
            SetUnknownResultNullability();
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
            SetUnknownResultNullability();
            return result;
        }

        private void SetUnknownResultNullability()
        {
            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable)
            {
                this.State.ResultType = null;
            }
        }

        public override BoundNode VisitStackAllocArrayCreation(BoundStackAllocArrayCreation node)
        {
            var result = base.VisitStackAllocArrayCreation(node);
            Debug.Assert((object)node.Type == null || node.Type.IsPointerType() || node.Type.IsByRefLikeType);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitDynamicIndexerAccess(BoundDynamicIndexerAccess node)
        {
            var result = base.VisitDynamicIndexerAccess(node);

            Debug.Assert(node.Type.IsDynamic());
            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable)
            {
                if (node.Type?.IsReferenceType == true)
                {
                    bool? isNullable = InferResultNullabilityFromApplicableCandidates(StaticCast<Symbol>.From(node.ApplicableIndexers));
                    this.State.ResultType = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: isNullable);
                }
                else
                {
                    this.State.ResultType = null;
                }
            }

            return result;
        }

        protected override void VisitReceiverOfDynamicAccessAsRvalue(BoundExpression receiverOpt)
        {
            base.VisitReceiverOfDynamicAccessAsRvalue(receiverOpt);

            Debug.Assert(!IsConditionalState);
            CheckPossibleNullReceiver(receiverOpt);
        }

        private void CheckPossibleNullReceiver(BoundExpression receiverOpt, bool checkType = true)
        {
            if (receiverOpt != null &&
                (!checkType || ((object)receiverOpt.Type != null && receiverOpt.Type.IsReferenceType)) &&
                this.State.Reachable &&
                this.State.ResultIsNullable == true)
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceReceiver, receiverOpt.Syntax);
            }
        }

        private bool CheckNullAsNonNullableReference(TypeAndSlot value)
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

        // PROTOTYPE(NullableReferenceTypes): Use TypeAndSlot instead.
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
                    bool? memberResultIsNullable = IsResultNullable(member);
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
            SetUnknownResultNullability(); // PROTOTYPE(NullableReferenceTypes)
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

#endregion Visitors

        // PROTOTYPE(NullableReferenceTypes): Implement.
        protected override string Dump(LocalState state)
        {
            return string.Empty;
        }

        private void AppendBitNames(BitVector a, StringBuilder builder)
        {
            bool any = false;
            foreach (int bit in a.TrueBits())
            {
                if (any) builder.Append(", ");
                any = true;
                AppendBitName(bit, builder);
            }
        }

        private void AppendBitName(int bit, StringBuilder builder)
        {
            VariableIdentifier id = variableBySlot[bit];
            if (id.ContainingSlot > 0)
            {
                AppendBitName(id.ContainingSlot, builder);
                builder.Append(".");
            }

            builder.Append(
                bit == 0 ? "<unreachable>" :
                string.IsNullOrEmpty(id.Symbol.Name) ? "<anon>" + id.Symbol.GetHashCode() :
                id.Symbol.Name);
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

            self.ResultType = _invalidType;
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

                //self.ResultType = _invalidType;
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
        internal struct TypeAndSlot
        {
            internal readonly BoundExpression Expr;
            internal readonly TypeSymbolWithAnnotations Type;
            internal readonly int Slot;

            internal TypeAndSlot(BoundExpression expr, TypeSymbolWithAnnotations type, int slot = -1)
            {
                Expr = expr;
                Type = type;
                Slot = slot;
            }

            internal bool IsNull => (object)Type == null;

            internal SyntaxNode Syntax => Expr?.Syntax;

            internal ConstantValue ConstantValue => Expr?.ConstantValue;

            internal string GetDebuggerDisplay()
            {
                return $"Slot={Slot}, Type={Type}, Expr={Expr?.Kind}";
            }
        }

#if REFERENCE_STATE
        internal class LocalState : AbstractLocalState
#else
        internal struct LocalState : AbstractLocalState
#endif
        {
            private BitVector _knownNullState; // No diagnostics should be derived from a variable with a bit set to 0.
            private BitVector _notNull;
            // PROTOTYPE(NullableReferenceTypes): Should be the return
            // value from the visitor, not mutable state.
            internal TypeAndSlot Result;

            // PROTOTYPE(NullableReferenceTypes): Remove.
            internal TypeSymbolWithAnnotations ResultType
            {
                get => Result.Type;
                set => Result = new TypeAndSlot(null, value);
            }

            internal bool? ResultIsNullable => (object)ResultType == null ? null : ResultType.IsNullable;

            internal LocalState(BitVector unknownNullState, BitVector notNull, TypeSymbolWithAnnotations resultType)
            {
                Debug.Assert(!unknownNullState.IsNull);
                Debug.Assert(!notNull.IsNull);
                this._knownNullState = unknownNullState;
                this._notNull = notNull;
                ResultType = resultType;
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
                ResultType = other.ResultType;
            }

            /// <summary>
            /// Produce a duplicate of this flow analysis state.
            /// </summary>
            /// <returns></returns>
            public LocalState Clone()
            {
                return new LocalState( _knownNullState.Clone(), _notNull.Clone(), ResultType);
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
