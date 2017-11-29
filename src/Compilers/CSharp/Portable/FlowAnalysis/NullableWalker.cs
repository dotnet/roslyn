// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if DEBUG
// See comment in DataFlowPass.
#define REFERENCE_STATE
#endif

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Implement C# data flow analysis (definite assignment).
    /// </summary>
    internal sealed partial class NullableWalker : DataFlowPassBase<NullableWalker.LocalState>
    {
        /// <summary>
        /// The inferred nullability at the point of declaration of var locals.
        /// </summary>
        // PROTOTYPE(NullableReferenceTypes): Does this need to
        // move to LocalState so it participates in merging?
        private readonly PooledDictionary<LocalSymbol, bool?> _variableIsNullable = PooledDictionary<LocalSymbol, bool?>.GetInstance();

        /// <summary>
        /// The current source assembly.
        /// </summary>
        private readonly SourceAssemblySymbol _sourceAssembly;

        /// <summary>
        /// Reflects the enclosing method or lambda at the current location (in the bound tree).
        /// </summary>
        private MethodSymbol _currentMethodOrLambda;

        private readonly bool _includeNonNullableWarnings;
        private PooledDictionary<BoundExpression, ObjectCreationPlaceholderLocal> _placeholderLocals;
        private LocalSymbol _implicitReceiver;

        protected override void Free()
        {
            _variableIsNullable.Free();
            _placeholderLocals?.Free();
            base.Free();
        }

        internal NullableWalker(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            bool includeNonNullableWarnings)
            : base(compilation, member, node, new EmptyStructTypeCache(compilation, dev12CompilerCompatibility: false), trackUnassignments: false)
        {
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
                    {
                        int containingSlot = variable.ContainingSlot;
                        if (variableBySlot[containingSlot].Symbol.GetTypeOrReturnType().TypeKind == TypeKind.Struct &&
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
                        member = eventSymbol.AssociatedField;
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
                        // PROTOTYPE(NullableReferenceTypes): Use backing field for struct property
                        // for now, to avoid cycles if the struct type contains a property of the struct type.
                        // Remove this and populate struct members lazily to match classes.
                        if (propSymbol.ContainingType.TypeKind == TypeKind.Struct)
                        {
                            var fieldName = GeneratedNames.MakeBackingFieldName(propSymbol.Name);
                            member = _emptyStructTypeCache.GetStructInstanceFields(propSymbol.ContainingType).FirstOrDefault(f => f.Name == fieldName);
                        }
                        else
                        {
                            member = propSymbol;
                        }
                        receiver = propAccess.ReceiverOpt;
                        break;
                    }
            }

            return (object)member != null &&
                (object)receiver != null &&
                receiver.Kind != BoundKind.TypeExpression &&
                (object)receiver.Type != null;
        }

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

        private void Assign(BoundNode node, BoundExpression value, bool? valueIsNotNull, RefKind refKind = RefKind.None, bool read = true)
        {
            AssignImpl(node, value, valueIsNotNull, written: true, refKind: refKind, read: read);
        }

        /// <summary>
        /// Mark a variable as assigned (or unassigned).
        /// </summary>
        /// <param name="node">Node being assigned to.</param>
        /// <param name="value">The value being assigned.</param>
        /// <param name="valueIsNotNull"/>
        /// <param name="written">True if target location is considered written to.</param>
        /// <param name="refKind">Target kind (by-ref or not).</param>
        /// <param name="read">True if target location is considered read from.</param>
        private void AssignImpl(BoundNode node, BoundExpression value, bool? valueIsNotNull, RefKind refKind, bool written, bool read)
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
                        if (written)
                        {
                            TrackNullableStateForAssignment(node, symbol, slot, value, valueIsNotNull, inferNullability: local.DeclaredType.InferredType);
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
                            if (written) VisitRvalue(local);

                            // PROTOTYPE(NullableReferenceTypes): StaticNullChecking?
                        }
                        else
                        {
                            int slot = MakeSlot(local);
                            if (written)
                            {
                                TrackNullableStateForAssignment(node, local.LocalSymbol, slot, value, valueIsNotNull);
                            }
                        }
                        break;
                    }

                case BoundKind.Parameter:
                    {
                        var parameter = (BoundParameter)node;
                        int slot = GetOrCreateSlot(parameter.ParameterSymbol);
                        if (written)
                        {
                            TrackNullableStateForAssignment(node, parameter.ParameterSymbol, slot, value, valueIsNotNull);
                        }
                        break;
                    }

                case BoundKind.FieldAccess:
                    {
                        var fieldAccess = (BoundFieldAccess)node;
                        int slot = MakeSlot(fieldAccess);
                        if (written)
                        {
                            TrackNullableStateForAssignment(node, fieldAccess.FieldSymbol, slot, value, valueIsNotNull);
                        }
                        break;
                    }

                case BoundKind.EventAccess:
                    {
                        var eventAccess = (BoundEventAccess)node;
                        int slot = MakeSlot(eventAccess);
                        if (written)
                        {
                            TrackNullableStateForAssignment(node, eventAccess.EventSymbol, slot, value, valueIsNotNull);
                        }
                        break;
                    }

                case BoundKind.PropertyAccess:
                    {
                        var propertyAccess = (BoundPropertyAccess)node;
                        int slot = MakeSlot(propertyAccess);
                        if (written)
                        {
                            TrackNullableStateForAssignment(node, propertyAccess.PropertySymbol, slot, value, valueIsNotNull);
                        }
                        break;
                    }

                case BoundKind.IndexerAccess:
                    {
                        if (written && this.State.Reachable)
                        {
                            var indexerAccess = (BoundIndexerAccess)node;
                            TrackNullableStateForAssignment(node, indexerAccess.Indexer, -1, value, valueIsNotNull);
                        }
                        break;
                    }

                case BoundKind.ArrayAccess:
                    {
                        if (written && this.State.Reachable)
                        {
                            var arrayAccess = (BoundArrayAccess)node;
                            TypeSymbolWithAnnotations elementType = (arrayAccess.Expression.Type as ArrayTypeSymbol)?.ElementType;

                            if ((object)elementType != null)
                            {
                                // Pass array type symbol as the target for the assignment. 
                                TrackNullableStateForAssignment(node, arrayAccess.Expression.Type, -1, value, valueIsNotNull);
                            }
                        }
                        break;
                    }

                case BoundKind.ObjectInitializerMember:
                    if (written && this.State.Reachable)
                    {
                        var initializerMember = (BoundObjectInitializerMember)node;
                        Symbol memberSymbol = initializerMember.MemberSymbol;

                        if ((object)memberSymbol != null)
                        {
                            int slot = -1;

                            if ((object)_implicitReceiver != null && !memberSymbol.IsStatic)
                            {
                                // PROTOTYPE(NullableReferenceTypes): Do we need to handle events?
                                switch (memberSymbol.Kind)
                                {
                                    case SymbolKind.Field:
                                    case SymbolKind.Property:
                                        slot = GetOrCreateSlot(memberSymbol, GetOrCreateSlot(_implicitReceiver));
                                        break;
                                }
                            }

                            TrackNullableStateForAssignment(node, memberSymbol, slot, value, valueIsNotNull);
                        }
                    }
                    break;

                case BoundKind.ThisReference:
                    {
                        var expression = (BoundThisReference)node;
                        int slot = MakeSlot(expression);
                        if (written)
                        {
                            ParameterSymbol thisParameter = MethodThisParameter;

                            if ((object)thisParameter != null)
                            {
                                TrackNullableStateForAssignment(node, thisParameter, slot, value, valueIsNotNull);
                            }
                        }
                        break;
                    }

                case BoundKind.RangeVariable:
                    // PROTOTYPE(NullableReferenceTypes): StaticNullChecking?
                    AssignImpl(((BoundRangeVariable)node).Value, value, valueIsNotNull, refKind, written, read);
                    break;

                case BoundKind.BadExpression:
                    {
                        // Sometimes a bad node is not so bad that we cannot analyze it at all.
                        var bad = (BoundBadExpression)node;
                        if (!bad.ChildBoundNodes.IsDefault && bad.ChildBoundNodes.Length == 1)
                        {
                            AssignImpl(bad.ChildBoundNodes[0], value, valueIsNotNull, refKind, written, read);
                        }
                        break;
                    }

                case BoundKind.TupleLiteral:
                    ((BoundTupleExpression)node).VisitAllElements((x, self) => self.Assign(x, value: null, valueIsNotNull: self.State.ResultIsNotNull, refKind: refKind), this);
                    break;

                default:
                    // Other kinds of left-hand-sides either represent things not tracked (e.g. array elements)
                    // or errors that have been reported earlier (e.g. assignment to a unary increment)
                    break;
            }
        }

        private void TrackNullableStateForAssignment(BoundNode node, Symbol assignmentTarget, int slot, BoundExpression value, bool? valueIsNotNull, bool inferNullability = false)
        {
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
                        _variableIsNullable[local] = !valueIsNotNull;
                    }

                    bool? targetIsNullable;
                    if ((object)local == null || !_variableIsNullable.TryGetValue(local, out targetIsNullable))
                    {
                        targetIsNullable = targetType.IsNullable;
                    }

                    bool isByRefTarget = IsByRefTarget(slot);

                    if (targetIsNullable == false)
                    {
                        if (valueIsNotNull == false && (value == null || !CheckNullAsNonNullableReference(value)))
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
                            (targetIsNullable == true) ? (bool?)false : null :
                            valueIsNotNull;
                    }

                    if (slot > 0)
                    {
                        // PROTOTYPE(NullableReferenceTypes): Might this clear state that
                        // should be copied in InheritNullableStateOfTrackableType?
                        InheritDefaultState(slot);

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
                            (value.Kind == BoundKind.ObjectCreationExpression || value.Kind == BoundKind.AnonymousObjectCreationExpression || targetType.TypeSymbol.IsAnonymousType) &&
                            targetType.TypeSymbol == value.Type) // PROTOTYPE(NullableReferenceTypes): Allow assignment to base type.
                        {
                            int valueSlot = GetValueSlotForAssignment(value);
                            if (valueSlot > 0)
                            {
                                InheritNullableStateOfTrackableType(slot, valueSlot, isByRefTarget);
                            }
                        }
                    }
                }
                else if (slot > 0 && EmptyStructTypeCache.IsTrackableStructType(targetType.TypeSymbol) &&
                        (value == null || targetType.TypeSymbol == value.Type))
                {
                    InheritNullableStateOfTrackableStruct(targetType.TypeSymbol, slot, GetValueSlotForAssignment(value), IsByRefTarget(slot));
                }

                if (value != null && (object)value.Type != null && IsNullabilityMismatch(targetType.TypeSymbol, value.Type))
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInAssignment, value.Syntax, value.Type, targetType.TypeSymbol);
                }
            }
        }

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

            // PROTOTYPE(NullableReferenceTypes): Handle properties not backed by fields.
            // See ModifyMembers_StructPropertyNoBackingField and PropertyCycle_Struct tests.
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
                    int valueSlot = VariableSlot(fieldOrProperty, valueContainerSlot);
                    if (valueSlot > 0)
                    {
                        int targetMemberSlot = GetOrCreateSlot(fieldOrProperty, targetContainerSlot);
                        InheritNullableStateOfTrackableType(targetMemberSlot, valueSlot, isByRefTarget);
                    }
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

        private void InheritNullableStateOfTrackableType(int targetSlot, int valueSlot, bool isByRefTarget)
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
                InheritNullableStateOfFieldOrProperty(targetSlot, valueSlot, member, isByRefTarget);
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
                    var paramType = parameter.Type.TypeSymbol;
                    if (EmptyStructTypeCache.IsTrackableStructType(paramType))
                    {
                        InheritNullableStateOfTrackableStruct(paramType, slot, -1, parameter.RefKind != RefKind.None);
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
                        Assign(pat, null, State.ResultIsNotNull, RefKind.None, false);
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

                if (this.State.ResultIsNotNull == false)
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
            if (this.State.Reachable)
            {
                this.State.ResultIsNotNull = IsResultNotNull(node, localSymbol);
            }

            return null;
        }

        public override BoundNode VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            var result = base.VisitLocalDeclaration(node);
            if (node.InitializerOpt != null)
            {
                Assign(node, node.InitializerOpt, this.State.ResultIsNotNull);
            }
            return result;
        }

        protected override BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            Debug.Assert(!IsConditionalState);
            this.State.ResultIsNotNull = null;
            return base.VisitExpressionWithoutStackGuard(node);
        }

        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            LocalSymbol saveImplicitReceiver = _implicitReceiver;
            _implicitReceiver = null;

            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                var type = node.Type;
                if ((object)type != null)
                {
                    bool isTrackableStructType = EmptyStructTypeCache.IsTrackableStructType(type);
                    if (type.IsReferenceType || isTrackableStructType)
                    {
                        _implicitReceiver = GetOrCreateObjectCreationPlaceholder(node);
                        var slot = MakeSlot(node);
                        if (slot > 0 && isTrackableStructType)
                        {
                            InheritNullableStateOfTrackableStruct(node.Type, slot, -1, false);
                        }
                    }
                }
            }

            var result = base.VisitObjectCreationExpression(node);

            SetResultIsNotNull(node);

            _implicitReceiver = saveImplicitReceiver;
            return result;
        }

        private void SetResultIsNotNull(BoundExpression node)
        {
            Debug.Assert(!IsConditionalState);

            // PROTOTYPE(NullableReferenceTypes): Is it necessary to check
            // this.State.Reachable for null checks, here or elsewhere?
            if (this.State.Reachable)
            {
                if (node.Type?.IsReferenceType == true)
                {
                    this.State.ResultIsNotNull = true;
                }
                else
                {
                    this.State.ResultIsNotNull = null;
                }
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
                    // explicitly-named properties only. For now, skip expressions
                    // with implicit names. See StaticNullChecking.AnonymousTypes_05.
                    if (node.Declarations.Length < arguments.Length)
                    {
                        continue;
                    }

                    PropertySymbol property = node.Declarations[i].Property;
                    if (receiverSlot <= 0)
                    {
                        receiverSlot = GetOrCreateSlot(implicitReceiver);
                    }

                    TrackNullableStateForAssignment(arguments[i], property, GetOrCreateSlot(property, receiverSlot), arguments[i], this.State.ResultIsNotNull);
                }

                this.State.ResultIsNotNull = true;
                return null;
            }
            else
            {
                return base.VisitAnonymousObjectCreationExpression(node);
            }
        }

        public override BoundNode VisitArrayCreation(BoundArrayCreation node)
        {
            var result = base.VisitArrayCreation(node);
            SetResultIsNotNull(node);
            return result;
        }

        protected override BoundNode VisitArrayElementInitializer(BoundArrayCreation arrayCreation, BoundExpression elementInitializer)
        {
            VisitRvalue(elementInitializer);

            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                TypeSymbolWithAnnotations elementType = (arrayCreation.Type as ArrayTypeSymbol)?.ElementType;

                if (elementType?.IsReferenceType == true)
                {
                    // Pass array type symbol as the target for the assignment. 
                    TrackNullableStateForAssignment(elementInitializer, arrayCreation.Type, -1, elementInitializer, this.State.ResultIsNotNull);
                }
            }

            return null;
        }

        public override BoundNode VisitArrayAccess(BoundArrayAccess node)
        {
            var result = base.VisitArrayAccess(node);

            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                bool? resultIsNotNull = null;
                TypeSymbolWithAnnotations elementType = (node.Expression.Type as ArrayTypeSymbol)?.ElementType;

                if (elementType?.IsReferenceType == true)
                {
                    resultIsNotNull = !elementType.IsNullable;
                }

                this.State.ResultIsNotNull = resultIsNotNull;
            }

            return result;
        }

        protected override void VisitArrayAccessTargetAsRvalue(BoundArrayAccess node)
        {
            base.VisitArrayAccessTargetAsRvalue(node);

            Debug.Assert(!IsConditionalState);
            // No need to check expression type since System.Array is a reference type.
            Debug.Assert(node.Expression.Type.IsReferenceType);
            CheckPossibleNullReceiver(node.Expression, checkType: false);
        }

        private bool? InferResultNullability(BoundBinaryOperator node, bool? leftIsNotNull, bool? rightIsNotNull)
        {
            return InferResultNullability(node.OperatorKind, node.MethodOpt, node.Type, leftIsNotNull, rightIsNotNull);
        }

        private bool? InferResultNullability(BinaryOperatorKind operatorKind, MethodSymbol methodOpt, TypeSymbol resultType, bool? leftIsNotNull, bool? rightIsNotNull)
        {
            if (operatorKind.IsUserDefined())
            {
                if ((object)methodOpt != null && methodOpt.ParameterCount == 2)
                {
                    return IsResultNotNull(methodOpt);
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
                        if (leftIsNotNull == true || rightIsNotNull == true)
                        {
                            return true;
                        }
                        else if (leftIsNotNull == false && rightIsNotNull == false)
                        {
                            return false;
                        }
                        else
                        {
                            Debug.Assert(leftIsNotNull == null || rightIsNotNull == null);
                            return null;
                        }

                    case BinaryOperatorKind.DelegateRemoval:
                        return false; // Delegate removal can produce null.
                }

                return true;
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
                bool? leftIsNotNull = this.State.ResultIsNotNull;
                bool warnOnNullReferenceArgument = (binary.OperatorKind.IsUserDefined() && (object)binary.MethodOpt != null && binary.MethodOpt.ParameterCount == 2);

                if (warnOnNullReferenceArgument)
                {
                    WarnOnNullReferenceArgument(binary.Left, leftIsNotNull, binary.MethodOpt.Parameters[0], expanded: false);
                }

                VisitRvalue(binary.Right);
                Debug.Assert(!IsConditionalState);
                // At this point, State.Reachable may be false for
                // invalid code such as `s + throw new Exception()`.
                bool? rightIsNotNull = this.State.ResultIsNotNull;

                if (warnOnNullReferenceArgument)
                {
                    WarnOnNullReferenceArgument(binary.Right, rightIsNotNull, binary.MethodOpt.Parameters[1], expanded: false);
                }

                AfterRightChildHasBeenVisited(binary);

                Debug.Assert(!IsConditionalState);
                this.State.ResultIsNotNull = InferResultNullability(binary, leftIsNotNull, rightIsNotNull);

                BinaryOperatorKind op = binary.OperatorKind.Operator();
                if (op == BinaryOperatorKind.Equal || op == BinaryOperatorKind.NotEqual)
                {
                    BoundExpression operandComparedToNull = null;
                    bool? operandComparedToNullIsNotNull = null;

                    if (binary.Right.ConstantValue?.IsNull == true)
                    {
                        operandComparedToNull = binary.Left;
                        operandComparedToNullIsNotNull = leftIsNotNull;
                    }
                    else if (binary.Left.ConstantValue?.IsNull == true)
                    {
                        operandComparedToNull = binary.Right;
                        operandComparedToNullIsNotNull = rightIsNotNull;
                    }

                    if (operandComparedToNull != null)
                    {
                        if (operandComparedToNullIsNotNull == true)
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

            if (!(this.State.Reachable) || node.LeftOperand.ConstantValue != null || node.LeftOperand.Type?.IsReferenceType != true)
            {
                return base.VisitNullCoalescingOperator(node);
            }

            VisitRvalue(node.LeftOperand);
            var savedState = this.State.Clone();

            BoundExpression operandComparedToNull = node.LeftOperand;

            if (savedState.ResultIsNotNull == true)
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.HDN_ExpressionIsProbablyNeverNull, node.LeftOperand.Syntax);
            }

            operandComparedToNull = SkipReferenceConversions(operandComparedToNull);
            int slot = MakeSlot(operandComparedToNull);

            VisitRvalue(node.RightOperand);
            bool? rightOperandIsNotNull = this.State.ResultIsNotNull;
            IntersectWith(ref this.State, ref savedState);
            Debug.Assert(!IsConditionalState);
            this.State.ResultIsNotNull = rightOperandIsNotNull | savedState.ResultIsNotNull;
            return null;
        }

        public override BoundNode VisitConditionalAccess(BoundConditionalAccess node)
        {
            Debug.Assert(!IsConditionalState);

            if (!(this.State.Reachable) || node.Receiver.ConstantValue != null || node.Receiver.Type?.IsReferenceType != true)
            {
                return base.VisitConditionalAccess(node);
            }

            VisitRvalue(node.Receiver);
            var savedState = this.State.Clone();

            BoundExpression operandComparedToNull = node.Receiver;

            if (savedState.ResultIsNotNull == true)
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
            IntersectWith(ref this.State, ref savedState);
            return null;
        }

        public override BoundNode VisitConditionalReceiver(BoundConditionalReceiver node)
        {
            var result = base.VisitConditionalReceiver(node);
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            // Always visit the arguments first
            var result = base.VisitCall(node);

            if (node.Method.MethodKind == MethodKind.LocalFunction)
            {
                var localFunc = (LocalFunctionSymbol)node.Method.OriginalDefinition;
                ReplayReadsAndWrites(localFunc, node.Syntax, writes: true);
            }

            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                this.State.ResultIsNotNull = IsResultNotNull(node.Method);
            }

            return result;
        }

        private void ReplayReadsAndWrites(LocalFunctionSymbol localFunc,
                                  SyntaxNode syntax,
                                  bool writes)
        {
            // PROTOTYPE(NullableReferenceTypes): Support field initializers in local functions.
        }

        private bool? IsResultNotNull(Symbol resultSymbol)
        {
            TypeSymbolWithAnnotations resultType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(resultSymbol);

            if (!resultType.IsVoid && resultType.IsReferenceType)
            {
                return !resultType.IsNullable;
            }
            else
            {
                return null;
            }
        }

        private bool? IsResultNotNull(BoundExpression node, Symbol resultSymbol)
        {
            TypeSymbolWithAnnotations resultType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(resultSymbol);

            if ((object)resultType != null && !resultType.IsVoid && resultType.IsReferenceType)
            {
                if (resultType.IsNullable == false)
                {
                    // Statically declared as not-nullable. This takes priority.
                    return true;
                }

                int slot = MakeSlot(node);

                if (slot > 0)
                {
                    // We are supposed to track information for the node. Use whatever we managed to
                    // accumulate so far.
                    return this.State[slot];
                }

                // The node is not trackable, use information from the declaration.
                Debug.Assert(resultType.IsNullable != false);
                return !resultType.IsNullable;
            }
            else
            {
                return null;
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

        protected override void VisitFieldReceiver(BoundExpression receiverOpt, FieldSymbol fieldSymbol, bool asLvalue)
        {
            base.VisitFieldReceiver(receiverOpt, fieldSymbol, asLvalue);

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
                            WarnOnNullReferenceArgument(node.Operand, this.State.ResultIsNotNull, node.SymbolOpt.Parameters[0], expanded: false);
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

                this.State.ResultIsNotNull = InferResultNullability(
                    node.Conversion,
                    node.Operand.Type,
                    node.Type,
                    this.State.ResultIsNotNull);
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

        private bool? InferResultNullability(Conversion conversion, TypeSymbol sourceTypeOpt, TypeSymbol targetType, bool? operandIsNotNull)
        {
            if (targetType.IsReferenceType)
            {
                switch (conversion.Kind)
                {
                    case ConversionKind.MethodGroup:
                    case ConversionKind.AnonymousFunction:
                    case ConversionKind.InterpolatedString:
                        return true;

                    case ConversionKind.ExplicitUserDefined:
                    case ConversionKind.ImplicitUserDefined:
                        var methodOpt = conversion.Method;
                        if ((object)methodOpt != null && methodOpt.ParameterCount == 1)
                        {
                            return IsResultNotNull(methodOpt);
                        }
                        else
                        {
                            return null;
                        }

                    case ConversionKind.Unboxing:
                    case ConversionKind.ExplicitDynamic:
                    case ConversionKind.ImplicitDynamic:
                    case ConversionKind.NoConversion:
                        return null;

                    case ConversionKind.Boxing:
                        if (sourceTypeOpt?.IsValueType == true)
                        {
                            if (sourceTypeOpt.IsNullableType())
                            {
                                // PROTOTYPE(NullableReferenceTypes): Should we worry about a pathological case of boxing nullable value known to be not null?
                                //       For example, new int?(0)
                                return false;
                            }
                            else
                            {
                                return true;
                            }
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
                        return operandIsNotNull;

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

            SetResultIsNotNull(node);

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

            if (this.State.Reachable)
            {
                this.State.ResultIsNotNull = null;
            }

            return null;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            var result = VisitLambdaOrLocalFunction(node);
            SetUnknownResultNullability();
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
            foreach (PendingBranch pending in pendingReturns)
            {
                this.State = pending.State;
                IntersectWith(ref finalState, ref this.State); // a no-op except in region analysis
            }

            this.State = finalState;

            this._currentMethodOrLambda = oldMethodOrLambda;
            return null;
        }

        public override BoundNode VisitThisReference(BoundThisReference node)
        {
            // TODO: in a struct constructor, "this" is not initially assigned.
            SetResultIsNotNull(node);
            return null;
        }

        public override BoundNode VisitParameter(BoundParameter node)
        {
            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                this.State.ResultIsNotNull = IsResultNotNull(node, node.ParameterSymbol);
            }

            return null;
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            base.VisitAssignmentOperator(node);
            Debug.Assert(!IsConditionalState);
            bool? valueIsNotNull = this.State.ResultIsNotNull;
            Assign(node.Left, node.Right, valueIsNotNull, refKind: node.RefKind);

            if (this.State.Reachable)
            {
                this.State.ResultIsNotNull = valueIsNotNull;
            }

            return null;
        }

        public override BoundNode VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            base.VisitDeconstructionAssignmentOperator(node);
            bool? valueIsNotNull = this.State.ResultIsNotNull;
            Assign(node.Left, node.Right, valueIsNotNull);
            return null;
        }

        public override BoundNode VisitIncrementOperator(BoundIncrementOperator node)
        {
            base.VisitIncrementOperator(node);

            bool? resultOfIncrementIsNotNull;

            Debug.Assert(!IsConditionalState);

            if (this.State.Reachable)
            {
                bool? operandIsNotNull;
                if (RegularPropertyAccess(node.Operand))
                {
                    PropertySymbol property = ((BoundPropertyAccess)node.Operand).PropertySymbol;
                    operandIsNotNull = IsResultNotNull(node, property);
                }
                else
                {
                    operandIsNotNull = this.State.ResultIsNotNull;
                }

                MethodSymbol incrementOperator = (node.OperatorKind.IsUserDefined() && (object)node.MethodOpt != null && node.MethodOpt.ParameterCount == 1) ? node.MethodOpt : null;
                TypeSymbol targetTypeOfOperandConversion;

                if (node.OperandConversion.IsUserDefined && (object)node.OperandConversion.Method != null && node.OperandConversion.Method.ParameterCount == 1)
                {
                    WarnOnNullReferenceArgument(node.Operand, operandIsNotNull, node.OperandConversion.Method.Parameters[0], expanded: false);
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

                bool? resultOfOperandConversionIsNotNull;

                if ((object)targetTypeOfOperandConversion != null)
                {
                    // PROTOTYPE(NullableReferenceTypes): Should something special be done for targetTypeOfOperandConversion for lifted case?
                    resultOfOperandConversionIsNotNull = InferResultNullability(node.OperandConversion,
                                                                                node.Operand.Type,
                                                                                targetTypeOfOperandConversion,
                                                                                operandIsNotNull);
                }
                else
                {
                    resultOfOperandConversionIsNotNull = null;
                }

                if ((object)incrementOperator == null)
                {
                    resultOfIncrementIsNotNull = null;
                }
                else 
                {
                    WarnOnNullReferenceArgument(node.Operand, 
                                                resultOfOperandConversionIsNotNull,
                                                incrementOperator.Parameters[0], expanded: false);

                    resultOfIncrementIsNotNull = IsResultNotNull(incrementOperator);
                }

                if (node.ResultConversion.IsUserDefined && (object)node.ResultConversion.Method != null && node.ResultConversion.Method.ParameterCount == 1)
                {
                    WarnOnNullReferenceArgument(node, resultOfIncrementIsNotNull, node.ResultConversion.Method.Parameters[0], expanded: false);
                }

                resultOfIncrementIsNotNull = InferResultNullability(node.ResultConversion,
                                                                    incrementOperator?.ReturnType.TypeSymbol,
                                                                    node.Type,
                                                                    resultOfIncrementIsNotNull);

                var op = node.OperatorKind.Operator();
                if (op == UnaryOperatorKind.PrefixIncrement || op == UnaryOperatorKind.PrefixDecrement)
                {
                    this.State.ResultIsNotNull = resultOfIncrementIsNotNull;
                }
                else
                {
                    this.State.ResultIsNotNull = operandIsNotNull;
                }
            }
            else
            {
                resultOfIncrementIsNotNull = null;
            }

            Assign(node.Operand, value: node, valueIsNotNull: resultOfIncrementIsNotNull); 
            return null;
        }

        public override BoundNode VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            VisitCompoundAssignmentTarget(node);

            bool? resultIsNotNull;
            Debug.Assert(!IsConditionalState);

            if (this.State.Reachable)
            {
                bool? leftIsNotNull;
                if (RegularPropertyAccess(node.Left))
                {
                    PropertySymbol property = ((BoundPropertyAccess)node.Left).PropertySymbol;
                    leftIsNotNull = IsResultNotNull(node, property);
                }
                else
                {
                    leftIsNotNull = this.State.ResultIsNotNull;
                }

                if (node.LeftConversion.IsUserDefined && (object)node.LeftConversion.Method != null && node.LeftConversion.Method.ParameterCount == 1)
                {
                    WarnOnNullReferenceArgument(node.Left, leftIsNotNull, node.LeftConversion.Method.Parameters[0], expanded: false);
                }

                bool? resultOfLeftConversionIsNotNull;

                if ((object)node.Operator.LeftType != null)
                {
                    resultOfLeftConversionIsNotNull = InferResultNullability(node.LeftConversion,
                                                                             node.Left.Type,
                                                                             node.Operator.LeftType,
                                                                             leftIsNotNull);
                }
                else
                {
                    resultOfLeftConversionIsNotNull = null;
                }

                VisitRvalue(node.Right);
                bool? rightIsNotNull = this.State.ResultIsNotNull;

                AfterRightHasBeenVisited(node);

                if ((object)node.Operator.ReturnType != null)
                {
                    if (node.Operator.Kind.IsUserDefined() && (object)node.Operator.Method != null && node.Operator.Method.ParameterCount == 2)
                    { 
                        WarnOnNullReferenceArgument(node.Left, resultOfLeftConversionIsNotNull, node.Operator.Method.Parameters[0], expanded: false);
                        WarnOnNullReferenceArgument(node.Right, rightIsNotNull, node.Operator.Method.Parameters[1], expanded: false);
                    }

                    resultIsNotNull = InferResultNullability(node.Operator.Kind, node.Operator.Method, node.Operator.ReturnType, leftIsNotNull, rightIsNotNull);

                    if (node.FinalConversion.IsUserDefined && (object)node.FinalConversion.Method != null && node.FinalConversion.Method.ParameterCount == 1)
                    {
                        WarnOnNullReferenceArgument(node, resultIsNotNull, node.FinalConversion.Method.Parameters[0], expanded: false);
                    }

                    resultIsNotNull = InferResultNullability(node.FinalConversion,
                                                             node.Operator.ReturnType,
                                                             node.Type,
                                                             resultIsNotNull);
                }
                else
                {
                    resultIsNotNull = null;
                }

                this.State.ResultIsNotNull = resultIsNotNull;
            }
            else
            {
                VisitRvalue(node.Right);
                AfterRightHasBeenVisited(node);
                resultIsNotNull = null;
            }

            Assign(node.Left, value: node, valueIsNotNull: resultIsNotNull); 
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

        protected override void VisitArgumentAsRvalue(BoundExpression argument, ParameterSymbol parameter, bool expanded)
        {
            base.VisitArgumentAsRvalue(argument, parameter, expanded);
            Debug.Assert(!IsConditionalState);
            if ((object)parameter != null && this.State.Reachable)
            {
                WarnOnNullReferenceArgument(argument, this.State.ResultIsNotNull, parameter, expanded);
            }
        }

        private void WarnOnNullReferenceArgument(BoundExpression argument, bool? argumentIsNotNull, ParameterSymbol parameter, bool expanded)
        {
            TypeSymbolWithAnnotations paramType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(parameter);

            if (argumentIsNotNull == false)
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
                    return ((LocalSymbol)symbol).Type;

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

        protected override void WriteArgument(BoundExpression arg, RefKind refKind, MethodSymbol method, ParameterSymbol parameter)
        {
            Debug.Assert(!IsConditionalState);
            Debug.Assert(refKind != RefKind.None);

            // Accessors are treated as not mutating `this`.
            if (method?.IsAccessor() == true && (object)parameter == null)
            {
                return;
            }

            bool? valueIsNotNull = null;
            BoundValuePlaceholder value = null;

            if ((object)parameter != null)
            {
                TypeSymbolWithAnnotations paramType;

                if (this.State.Reachable)
                {
                    paramType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(parameter);

                    if (paramType.IsReferenceType)
                    {
                        valueIsNotNull = !paramType.IsNullable;
                    }
                }
                else
                {
                    paramType = parameter.Type; 
                }

                value = new BoundValuePlaceholder(arg.Syntax, paramType.TypeSymbol) { WasCompilerGenerated = true };
            }

            Assign(arg, value, valueIsNotNull); 
        }

        public override BoundNode VisitBaseReference(BoundBaseReference node)
        {
            SetResultIsNotNull(node);
            return null;
        }

        protected override void VisitCatchBlock(BoundCatchBlock catchBlock, ref LocalState finallyState)
        {
            DeclareVariables(catchBlock.Locals);

            var exceptionSource = catchBlock.ExceptionSourceOpt;
            if (exceptionSource != null)
            {
                Assign(exceptionSource, value: null, read: false, valueIsNotNull: true);
            }

            base.VisitCatchBlock(catchBlock, ref finallyState);
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            var result = base.VisitFieldAccess(node);

            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                this.State.ResultIsNotNull = IsResultNotNull(node, node.FieldSymbol);
            }

            return result;
        }

        public override BoundNode VisitPropertyAccess(BoundPropertyAccess node)
        {
            var result = base.VisitPropertyAccess(node);

            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                this.State.ResultIsNotNull = IsResultNotNull(node, node.PropertySymbol);
            }

            return result;
        }

        public override BoundNode VisitIndexerAccess(BoundIndexerAccess node)
        {
            var result = base.VisitIndexerAccess(node);

            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                this.State.ResultIsNotNull = IsResultNotNull(node.Indexer);
            }

            return result;
        }

        public override BoundNode VisitEventAccess(BoundEventAccess node)
        {
            var result = base.VisitEventAccess(node);

            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                this.State.ResultIsNotNull = IsResultNotNull(node, node.EventSymbol);
            }

            return result;
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
            SetUnknownResultNullability();
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
                    this.StateWhenFalse.ResultIsNotNull = null;
                }

                if (this.StateWhenTrue.Reachable)
                {
                    this.StateWhenTrue.ResultIsNotNull = null;
                }
            }
            else if (this.State.Reachable)
            {
                if (node.OperatorKind.IsUserDefined() && (object)node.MethodOpt != null && node.MethodOpt.ParameterCount == 1)
                {
                    WarnOnNullReferenceArgument(node.Operand, this.State.ResultIsNotNull, node.MethodOpt.Parameters[0], expanded: false);
                }

                this.State.ResultIsNotNull = InferResultNullability(node);
            }

            return null;
        }

        private bool? InferResultNullability(BoundUnaryOperator node)
        {
            if (node.OperatorKind.IsUserDefined())
            {
                if ((object)node.MethodOpt != null && node.MethodOpt.ParameterCount == 1)
                {
                    return IsResultNotNull(node.MethodOpt);
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
            else if (node.Type?.IsReferenceType == true)
            {
                return true;
            }
            else
            {
                return null;
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
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitRefValueOperator(BoundRefValueOperator node)
        {
            var result = base.VisitRefValueOperator(node);
            SetUnknownResultNullability();
            return result;
        }

        private bool? InferResultNullability(BoundUserDefinedConditionalLogicalOperator node)
        {
            if ((object)node.LogicalOperator != null && node.LogicalOperator.ParameterCount == 2)
            {
                return IsResultNotNull(node.LogicalOperator);
            }
            else
            {
                return null;
            }
        }

        protected override void AfterLeftChildOfBinaryLogicalOperatorHasBeenVisited(BoundExpression node, BoundExpression right, bool isAnd, bool isBool, ref LocalState leftTrue, ref LocalState leftFalse)
        {
            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                bool? leftIsNotNull = this.State.ResultIsNotNull;
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
                    WarnOnNullReferenceArgument(left, leftIsNotNull, trueFalseOperator.Parameters[0], expanded: false);
                }

                if ((object)logicalOperator != null)
                {
                    WarnOnNullReferenceArgument(left, leftIsNotNull, logicalOperator.Parameters[0], expanded: false);
                }

                Visit(right);

                Debug.Assert(IsConditionalState ? (this.StateWhenFalse.Reachable || this.StateWhenTrue.Reachable) : this.State.Reachable);
                bool? rightIsNotNull = null;

                if (IsConditionalState)
                {
                    if (this.StateWhenFalse.Reachable)
                    {
                        rightIsNotNull = this.StateWhenFalse.ResultIsNotNull;
                        this.StateWhenFalse.ResultIsNotNull = InferResultNullabilityOfBinaryLogicalOperator(node, leftIsNotNull, rightIsNotNull);
                    }

                    if (this.StateWhenTrue.Reachable)
                    {
                        bool? saveRightIsNotNull = rightIsNotNull;
                        rightIsNotNull = this.StateWhenTrue.ResultIsNotNull;
                        this.StateWhenTrue.ResultIsNotNull = InferResultNullabilityOfBinaryLogicalOperator(node, leftIsNotNull, rightIsNotNull);

                        if (this.StateWhenFalse.Reachable)
                        {
                            rightIsNotNull &= saveRightIsNotNull;
                        }
                    }
                }
                else if (this.State.Reachable)
                {
                    rightIsNotNull = this.State.ResultIsNotNull;
                    this.State.ResultIsNotNull = InferResultNullabilityOfBinaryLogicalOperator(node, leftIsNotNull, rightIsNotNull);
                }

                if ((object)logicalOperator != null)
                {
                    WarnOnNullReferenceArgument(right, rightIsNotNull, logicalOperator.Parameters[1], expanded: false);
                }

                AfterRightChildOfBinaryLogicalOperatorHasBeenVisited(node, right, isAnd, isBool, ref leftTrue, ref leftFalse);
            }
            else
            {
                base.AfterLeftChildOfBinaryLogicalOperatorHasBeenVisited(node, right, isAnd, isBool, ref leftTrue, ref leftFalse);
            }
        }

        private bool? InferResultNullabilityOfBinaryLogicalOperator(BoundExpression node, bool? leftIsNotNull, bool? rightIsNotNull)
        {
            switch (node.Kind)
            {
                case BoundKind.BinaryOperator:
                    return InferResultNullability((BoundBinaryOperator)node, leftIsNotNull, rightIsNotNull);
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
            if (this.State.Reachable)
            {
                if (!node.Type.IsReferenceType || node.HasErrors || (object)node.GetResult == null)
                {
                    SetUnknownResultNullability();
                }
                else
                {
                    this.State.ResultIsNotNull = IsResultNotNull(node.GetResult);
                }
            }

            return result;
        }

        public override BoundNode VisitTypeOfOperator(BoundTypeOfOperator node)
        {
            var result = base.VisitTypeOfOperator(node);
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitMethodInfo(BoundMethodInfo node)
        {
            var result = base.VisitMethodInfo(node);
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitFieldInfo(BoundFieldInfo node)
        {
            var result = base.VisitFieldInfo(node);
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitDefaultExpression(BoundDefaultExpression node)
        {
            var result = base.VisitDefaultExpression(node);

            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                if ((object)node.Type != null && node.Type.IsReferenceType == true)
                {
                    this.State.ResultIsNotNull = false;
                }
                else
                {
                    this.State.ResultIsNotNull = null;
                }
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
            if (this.State.Reachable)
            {
                if (node.Type.IsReferenceType)
                {
                    switch (node.Conversion.Kind)
                    {
                        case ConversionKind.Identity:
                        case ConversionKind.ImplicitReference:
                            // Inherit nullability from the operand
                            break;

                        case ConversionKind.Boxing:
                            if (node.Operand.Type?.IsValueType == true)
                            {
                                if (node.Operand.Type.IsNullableType())
                                {
                                    // PROTOTYPE(NullableReferenceTypes): Should we worry about a pathological case of boxing nullable value known to be not null?
                                    //       For example, new int?(0)
                                    this.State.ResultIsNotNull = false;
                                }
                                else
                                {
                                    this.State.ResultIsNotNull = true;
                                }
                            }
                            else
                            {
                                Debug.Assert(node.Operand.Type?.IsReferenceType != true);
                                this.State.ResultIsNotNull = false;
                            }
                            break;

                        default:
                            this.State.ResultIsNotNull = false;
                            break;
                    }
                }
                else
                {
                    this.State.ResultIsNotNull = null;
                }
            }

            return result;
        }

        public override BoundNode VisitSuppressNullableWarningExpression(BoundSuppressNullableWarningExpression node)
        {
            var result = base.VisitSuppressNullableWarningExpression(node);

            Debug.Assert(!IsConditionalState);
            if (this.State.Reachable)
            {
                this.State.ResultIsNotNull = null;
            }

            return result;
        }

        public override BoundNode VisitSizeOfOperator(BoundSizeOfOperator node)
        {
            var result = base.VisitSizeOfOperator(node);
            SetResultIsNotNull(node);
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
            if (this.State.Reachable)
            {
                var constant = node.ConstantValue;

                if (constant != null &&
                    ((object)node.Type != null ? node.Type.IsReferenceType : constant.IsNull))
                {
                    this.State.ResultIsNotNull = !constant.IsNull;
                }
                else
                {
                    this.State.ResultIsNotNull = null;
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
            if (this.State.Reachable)
            {
                if (node.Type?.IsReferenceType == true)
                {
                    this.State.ResultIsNotNull = InferResultNullabilityFromApplicableCandidates(StaticCast<Symbol>.From(node.ApplicableMethods));
                }
                else
                {
                    this.State.ResultIsNotNull = null;
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
            SetResultIsNotNull(node);
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
            SetResultIsNotNull(node);
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
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitNewT(BoundNewT node)
        {
            var result = base.VisitNewT(node);
            SetResultIsNotNull(node);
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
            if (this.State.Reachable)
            {
                this.State.ResultIsNotNull = null;
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
            if (this.State.Reachable)
            {
                if (node.Type?.IsReferenceType == true)
                {
                    this.State.ResultIsNotNull = InferResultNullabilityFromApplicableCandidates(StaticCast<Symbol>.From(node.ApplicableIndexers));
                }
                else
                {
                    this.State.ResultIsNotNull = null;
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
                this.State.ResultIsNotNull == false)
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

            bool? resultIsNotNull = true;

            foreach (Symbol member in applicableMembers)
            {
                TypeSymbolWithAnnotations type = member.GetTypeOrReturnType();

                if (type.IsReferenceType)
                {
                    bool? memberResultIsNotNull = IsResultNotNull(member);
                    if (memberResultIsNotNull == false)
                    {
                        // At least one candidate can produce null, assume dynamic access can produce null as well
                        resultIsNotNull = false;
                        break;
                    }
                    else if (memberResultIsNotNull == null)
                    {
                        // At least one candidate can produce result of an unknow nullability.
                        // At best, dynamic access can produce result of an unknown nullability as well.
                        resultIsNotNull = null;
                    }
                }
                else if (!type.IsValueType)
                {
                    resultIsNotNull = null;
                }
            }

            return resultIsNotNull;
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
            SetResultIsNotNull(node);
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
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitStringInsert(BoundStringInsert node)
        {
            var result = base.VisitStringInsert(node);
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

            self.ResultIsNotNull |= other.ResultIsNotNull;
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

                bool? resultIsNotNull = self.ResultIsNotNull;
                self.ResultIsNotNull &= other.ResultIsNotNull;

                if (self.ResultIsNotNull != resultIsNotNull)
                {
                    result = true;
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

#if REFERENCE_STATE
        internal class LocalState : AbstractLocalState
#else
        internal struct LocalState : AbstractLocalState
#endif
        {
            private BitVector _knownNullState; // No diagnostics should be derived from a variable with a bit set to 0.
            private BitVector _notNull;
            internal bool? ResultIsNotNull;

            internal LocalState(BitVector unknownNullState, BitVector notNull, bool? resultIsNotNull)
            {
                Debug.Assert(!unknownNullState.IsNull);
                Debug.Assert(!notNull.IsNull);
                this._knownNullState = unknownNullState;
                this._notNull = notNull;
                ResultIsNotNull = resultIsNotNull;
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
                ResultIsNotNull = other.ResultIsNotNull;
            }

            /// <summary>
            /// Produce a duplicate of this flow analysis state.
            /// </summary>
            /// <returns></returns>
            public LocalState Clone()
            {
                return new LocalState(_knownNullState.Clone(), _notNull.Clone(), this.ResultIsNotNull);
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
