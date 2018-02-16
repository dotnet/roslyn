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
            bool includeNonNullableWarnings)
            : base(compilation, member, node, new EmptyStructTypeCache(compilation, dev12CompilerCompatibility: false), trackUnassignments: false)
        {
            _sourceAssembly = ((object)member == null) ? null : (SourceAssemblySymbol)member.ContainingAssembly;
            this._currentMethodOrLambda = member;
            _includeNonNullableWarnings = includeNonNullableWarnings;
            _binder = compilation.GetBinderFactory(node.SyntaxTree).GetBinder(node.Syntax);
            Debug.Assert(!_binder.Conversions.IncludeNullability);
            _conversions = _binder.Conversions.WithNullability();
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

        public static void Analyze(CSharpCompilation compilation, MethodSymbol member, BoundNode node, DiagnosticBag diagnostics)
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

        private new Result VisitRvalue(BoundExpression node)
        {
            base.VisitRvalue(node);
            return _result;
        }

        private Result ApplyConversion(BoundExpression value, Conversion conversion, TypeSymbol targetType, Result valueResult)
        {
            if (targetType is null)
            {
                Debug.Assert(conversion.Kind == ConversionKind.NoConversion || conversion.Kind == ConversionKind.Identity);
                return Result.Unset;
            }
            var result = ApplyConversion(value, conversion, targetType, valueResult, checkConversion: true, requireIdentity: false, canConvert: out bool canConvert);
            if (!canConvert)
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInAssignment, value.Syntax, GetTypeAsDiagnosticArgument(valueResult.Type?.TypeSymbol), targetType);
            }
            return result;
        }

        private static object GetTypeAsDiagnosticArgument(TypeSymbol typeOpt)
        {
            // PROTOTYPE(NullableReferenceTypes): Avoid hardcoded string.
            return typeOpt ?? (object)"<null>";
        }

        private static BoundExpression CreatePlaceholderExpressionIfNecessary(SyntaxNode syntax, Result value)
        {
            var valueType = value.Type;
            return value.Expression ?? new BoundValuePlaceholder(syntax, valueType?.IsNullable, valueType?.TypeSymbol);
        }

        private static ImmutableArray<BoundExpression> CreatePlaceholderExpressionsIfNecessary(ImmutableArray<BoundExpression> values, ImmutableArray<Result> valueResults)
        {
            return valueResults.ZipAsArray(values, (r, v) => CreatePlaceholderExpressionIfNecessary(v.Syntax, r));
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
                var targetMemberSlot = GetOrCreateSlot(fieldOrProperty, targetContainerSlot);
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

            _result = TypeSymbolWithAnnotations.Create(node.Type);
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
            Debug.Assert(!IsConditionalState);

            var expr = node.ExpressionOpt;
            if (expr == null)
            {
                return null;
            }

            expr = RemoveImplicitConversion(expr, out var conversion);
            var result = VisitRvalue(expr);

            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            if (expr.Type?.IsErrorType() == true)
            {
                return null;
            }

            TypeSymbolWithAnnotations returnType = GetReturnType(compilation, _currentMethodOrLambda);
            if ((object)returnType != null)
            {
                var unconvertedType = result.Type;
                result = ApplyConversion(expr, conversion, returnType.TypeSymbol, result, checkConversion: true, requireIdentity: false, canConvert: out bool canConvert);

                if ((object)returnType != null && !ConversionsBase.HasTopLevelNullabilityImplicitConversion(result.Type, returnType))
                {
                    if (!CheckNullAsNonNullableReference(expr))
                    {
                        ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceReturn, expr.Syntax);
                    }
                }
                else if (!canConvert)
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInAssignment, expr.Syntax, GetTypeAsDiagnosticArgument(unconvertedType?.TypeSymbol), returnType.TypeSymbol);
                }
            }

            return null;
        }

        private static TypeSymbolWithAnnotations GetReturnType(CSharpCompilation compilation, MethodSymbol method)
        {
            var returnType = method.ReturnType;
            if (returnType is null)
            {
                return null;
            }
            return method.IsGenericTaskReturningAsync(compilation) ?
                ((NamedTypeSymbol)returnType.TypeSymbol).TypeArgumentsNoUseSiteDiagnostics[0] :
                returnType;
        }

        protected override void VisitPatternSwitchSection(BoundPatternSwitchSection node, BoundExpression switchExpression, bool isLastSection)
        {
            base.VisitPatternSwitchSection(node, switchExpression, isLastSection);
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            _result = GetAdjustedResult(GetDeclaredLocalResult(node.LocalSymbol));
            return null;
        }

        public override BoundNode VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            var initializer = node.InitializerOpt;
            if (initializer is null)
            {
                return node;
            }

            var local = node.LocalSymbol;
            int slot = GetOrCreateSlot(local);

            initializer = RemoveImplicitConversion(initializer, out var conversion);
            Result value = VisitRvalue(initializer);
            TypeSymbolWithAnnotations type = local.Type;

            if (node.DeclaredType.InferredType)
            {
                Debug.Assert(conversion.Kind == ConversionKind.Identity);
                _variableTypes[local] = value.Type;
                type = value.Type;
            }
            else
            {
                TypeSymbolWithAnnotations valueType = value.Type;
                value = ApplyConversion(initializer, conversion, type.TypeSymbol, value, checkConversion: true, requireIdentity: false, canConvert: out bool canConvert);
                if (!canConvert)
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInAssignment, initializer.Syntax, valueType.TypeSymbol, type.TypeSymbol);
                }
            }

            TrackNullableStateForAssignment(node, slot, type, initializer, value.Type, value.Slot);
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
            base.VisitStatement(statement);
            _result = _invalidType;
        }

        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            Debug.Assert(!IsConditionalState);
            VisitArguments(node, node.Arguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, node.Constructor, node.ArgsToParamsOpt, node.Expanded);
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
                    // PROTOTYPE(NullableReferenceTypes): Unwrap implicit conversions and re-calculate.
                    Result result = VisitRvalue(node);
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
                            VisitArguments(objectInitializer, objectInitializer.Arguments, objectInitializer.ArgumentNamesOpt, objectInitializer.ArgumentRefKindsOpt, (PropertySymbol)symbol, objectInitializer.ArgsToParamsOpt, objectInitializer.Expanded);
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

            VisitArguments(node, node.Arguments, namesOpt: default, refKindsOpt: default, method: node.AddMethod, argsToParamsOpt: node.ArgsToParamsOpt, expanded: node.Expanded);
            SetUnknownResultNullability();
        }

        private void SetResult(BoundExpression node)
        {
            _result = node;
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
                // PROTOTYPE(NullableReferenceTypes): Unwrap implicit conversions and re-calculate.
                Result argumentResult = VisitRvalue(argument);
                var parameter = constructor.Parameters[i];
                WarnOnNullReferenceArgument(argument, argumentResult.Type, parameter);

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

            var initialization = node.InitializerOpt;
            var elementBuilder = ArrayBuilder<BoundExpression>.GetInstance(initialization.Initializers.Length);
            GetArrayElements(initialization, elementBuilder);

            int n = elementBuilder.Count;
            var conversionsBuilder = ArrayBuilder<Conversion>.GetInstance(n);
            var resultBuilder = ArrayBuilder<Result>.GetInstance(n);
            for (int i = 0; i < n; i++)
            {
                var element = RemoveImplicitConversion(elementBuilder[i], out var conversion);
                conversionsBuilder.Add(conversion);
                elementBuilder[i] = element;
                resultBuilder.Add(VisitRvalue(element));
            }

            if (node.Syntax.Kind() == SyntaxKind.ImplicitArrayCreationExpression)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                var resultTypes = resultBuilder.SelectAsArray(r => r.Type);
                // PROTOTYPE(NullableReferenceType): Initial binding calls InferBestType(ImmutableArray<BoundExpression>, ...)
                // overload. Why are we calling InferBestType(ImmutableArray<TypeSymbolWithAnnotations>, ...) here?
                // PROTOTYPE(NullableReferenceType): InferBestType(ImmutableArray<BoundExpression>, ...)
                // uses a HashSet<TypeSymbol> to reduce the candidates to the unique types before comparing.
                // Should do the same here.
                var bestType = BestTypeInferrer.InferBestType(resultTypes.SelectAsArray(r => r?.TypeSymbol), _conversions, useSiteDiagnostics: ref useSiteDiagnostics);
                var isNullable = BestTypeInferrer.GetIsNullable(resultTypes);
                elementType = TypeSymbolWithAnnotations.Create(bestType ?? elementType.TypeSymbol, isNullable);
                arrayType = arrayType.WithElementType(elementType);
            }

            for (int i = 0; i < n; i++)
            {
                var element = elementBuilder[i];
                var result = ApplyConversion(element, conversionsBuilder[i], elementType?.TypeSymbol, resultBuilder[i]);
                if (elementType?.IsReferenceType == true)
                {
                    TrackNullableStateForAssignment(element, -1, elementType, element, result.Type, result.Slot);
                }
            }
            resultBuilder.Free();
            conversionsBuilder.Free();

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

        private TypeSymbolWithAnnotations InferResultNullability(BinaryOperatorKind operatorKind, MethodSymbol methodOpt, TypeSymbol resultType, TypeSymbolWithAnnotations leftType, TypeSymbolWithAnnotations rightType)
        {
            bool? isNullable = null;
            if (operatorKind.IsUserDefined())
            {
                if ((object)methodOpt != null && methodOpt.ParameterCount == 2)
                {
                    bool hasLiftedReturnType(BinaryOperatorKind opKind)
                    {
                        if (!opKind.IsLifted())
                        {
                            return false;
                        }
                        switch (opKind.Operator())
                        {
                            case BinaryOperatorKind.Equal:
                            case BinaryOperatorKind.NotEqual:
                            case BinaryOperatorKind.GreaterThan:
                            case BinaryOperatorKind.LessThan:
                            case BinaryOperatorKind.GreaterThanOrEqual:
                            case BinaryOperatorKind.LessThanOrEqual:
                                return false;
                            default:
                                return true;
                        }
                    }
                    return ConstructNullableIfNecessary(methodOpt.ReturnType, useNullable: hasLiftedReturnType(operatorKind));
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

        protected override void VisitBinaryOperatorChildren(ArrayBuilder<BoundBinaryOperator> stack)
        {
            var binary = stack.Peek();
            var left = RemoveImplicitConversion(binary.Left, out var conversion);
            var leftResult = VisitRvalue(left);
            leftResult = ApplyConversion(left, conversion, binary.Left.Type, leftResult);

            while (true)
            {
                binary = stack.Pop();
                leftResult = AfterLeftChildOfBinaryOperatorHasBeenVisited(binary, leftResult);

                if (stack.Count == 0)
                {
                    break;
                }

                Unsplit(); // VisitRvalue does this
            }
        }

        private Result AfterLeftChildOfBinaryOperatorHasBeenVisited(BoundBinaryOperator binary, Result leftResult)
        {
            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                TypeSymbolWithAnnotations leftType = leftResult.Type;
                bool warnOnNullReferenceArgument = (binary.OperatorKind.IsUserDefined() && (object)binary.MethodOpt != null && binary.MethodOpt.ParameterCount == 2);

                if (warnOnNullReferenceArgument)
                {
                    WarnOnNullReferenceArgument(binary.Left, leftType, binary.MethodOpt.Parameters[0]);
                }

                BoundExpression right = RemoveImplicitConversion(binary.Right, out var conversion);
                Result rightResult = VisitRvalue(right);

                Debug.Assert(!IsConditionalState);
                rightResult = ApplyConversion(right, conversion, binary.Right.Type, rightResult);

                // At this point, State.Reachable may be false for
                // invalid code such as `s + throw new Exception()`.
                TypeSymbolWithAnnotations rightType = rightResult.Type;

                if (warnOnNullReferenceArgument)
                {
                    WarnOnNullReferenceArgument(right, rightType, binary.MethodOpt.Parameters[1]);
                }

                Debug.Assert(!IsConditionalState);
                // PROTOTYPE(NullableReferenceTypes): Update method based on inferred operand type.
                var result = Result.Create(InferResultNullability(binary.OperatorKind, binary.MethodOpt, binary.Type, leftType, rightType));
                _result = result;

                BinaryOperatorKind op = binary.OperatorKind.Operator();
                if (op == BinaryOperatorKind.Equal || op == BinaryOperatorKind.NotEqual)
                {
                    BoundExpression operandComparedToNull = null;
                    TypeSymbolWithAnnotations operandComparedToNullType = null;

                    if (right.ConstantValue?.IsNull == true)
                    {
                        operandComparedToNull = binary.Left;
                        operandComparedToNullType = leftType;
                    }
                    else if (binary.Left.ConstantValue?.IsNull == true)
                    {
                        operandComparedToNull = right;
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

                return result;
            }
        }

        // PROTOTYPE(NullableReferenceTypes): Can this be replaced with RemoveImplicitConversion?
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
            BoundExpression rightOperand = RemoveImplicitConversion(node.RightOperand, out var rightConversion);

            Result leftResult = VisitRvalue(leftOperand);
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

            var rightResult = VisitRvalue(rightOperand);
            IntersectWith(ref this.State, ref leftState);

            bool? getResultNullable(Result a, Result b) => a.IsNullable & b.IsNullable;
            TypeSymbolWithAnnotations resultType;

            switch (node.OperatorResultKind)
            {
                case BoundNullCoalescingOperatorResultKind.LeftType:
                case BoundNullCoalescingOperatorResultKind.LeftUnwrappedType:
                    leftResult = ApplyConversion(node.LeftOperand, node.LeftConversion, node.Type, leftResult);
                    rightResult = ApplyConversion(node.RightOperand, rightConversion, leftResult.Type.TypeSymbol, rightResult);
                    resultType = TypeSymbolWithAnnotations.Create(leftResult.Type.TypeSymbol, getResultNullable(leftResult, rightResult));
                    break;
                case BoundNullCoalescingOperatorResultKind.RightType:
                    rightResult = ApplyConversion(node.RightOperand, rightConversion, node.Type, rightResult);
                    leftResult = ApplyConversion(node.LeftOperand, node.LeftConversion, rightResult.Type.TypeSymbol, leftResult);
                    resultType = TypeSymbolWithAnnotations.Create(rightResult.Type.TypeSymbol, getResultNullable(leftResult, rightResult));
                    break;
                case BoundNullCoalescingOperatorResultKind.ErrorType:
                    resultType = TypeSymbolWithAnnotations.Create(node.Type);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.OperatorResultKind);
            }

            _result = resultType;
            return null;
        }

        public override BoundNode VisitConditionalAccess(BoundConditionalAccess node)
        {
            Debug.Assert(!IsConditionalState);

            var receiver = node.Receiver;
            var resultType = VisitRvalue(receiver).Type;

            var receiverState = this.State.Clone();

            if (receiver.Type?.IsReferenceType == true)
            {
                if (resultType?.IsNullable == false)
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
            _result = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: resultType?.IsNullable | _result.Type?.IsNullable);
            // PROTOTYPE(NullableReferenceTypes): Report conversion warnings.
            return null;
        }

        public override BoundNode VisitConditionalOperator(BoundConditionalOperator node)
        {
            var isByRef = node.IsByRef;

            VisitCondition(node.Condition);
            var consequenceState = this.StateWhenTrue;
            var alternativeState = this.StateWhenFalse;

            void visitConditionalOperand(LocalState state, BoundExpression operand, out Conversion conversion)
            {
                SetState(state);
                if (isByRef)
                {
                    conversion = Conversion.Identity;
                    VisitLvalue(operand);
                }
                else
                {
                    operand = RemoveImplicitConversion(operand, out conversion);
                    VisitRvalue(operand);
                }
            }

            var consequence = node.Consequence;
            visitConditionalOperand(consequenceState, consequence, out var consequenceConversion);
            Unsplit();
            consequenceState = this.State;
            var consequenceResult = _result;
            consequence = CreatePlaceholderExpressionIfNecessary(consequence.Syntax, consequenceResult);

            var alternative = node.Alternative;
            visitConditionalOperand(alternativeState, alternative, out var alternativeConversion);
            Unsplit();
            var alternativeResult = _result;
            alternative = CreatePlaceholderExpressionIfNecessary(alternative.Syntax, alternativeResult);

            TypeSymbol resultType = null;
            if (!node.HasErrors)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                var bestType = BestTypeInferrer.InferBestTypeForConditionalOperator(consequence, alternative, _conversions, out bool _, ref useSiteDiagnostics);
                if (bestType is null)
                {
                    ReportStaticNullCheckingDiagnostics(
                        ErrorCode.WRN_NoBestNullabilityConditionalExpression,
                        node.Syntax,
                        GetTypeAsDiagnosticArgument(consequenceResult.Type?.TypeSymbol),
                        GetTypeAsDiagnosticArgument(alternativeResult.Type?.TypeSymbol));
                }
                else
                {
                    consequenceResult = ApplyConversion(consequence, consequenceConversion, bestType, consequenceResult);
                    alternativeResult = ApplyConversion(alternative, alternativeConversion, bestType, alternativeResult);
                    resultType = bestType;
                }
            }

            bool? getIsNullable(Result result) => (object)result.Type == null ? true : result.Type.IsNullable;
            bool? resultIsNullable;

            if (IsConstantTrue(node.Condition))
            {
                SetState(consequenceState);
                resultIsNullable = getIsNullable(consequenceResult);
            }
            else if (IsConstantFalse(node.Condition))
            {
                resultIsNullable = getIsNullable(alternativeResult);
            }
            else
            {
                IntersectWith(ref this.State, ref consequenceState);
                resultIsNullable = (getIsNullable(consequenceResult) | getIsNullable(alternativeResult));
            }

            _result = TypeSymbolWithAnnotations.Create(resultType ?? node.Type, resultIsNullable);
            return null;
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

            if (!node.HasErrors)
            {
                var refKindsOpt = node.ArgumentRefKindsOpt;
                ImmutableArray<Conversion> conversions;
                var arguments = RemoveArgumentConversions(node.Arguments, refKindsOpt, out conversions);
                ImmutableArray<Result> results = VisitArgumentsEvaluate(arguments, refKindsOpt, node.Expanded);
                if (method.IsGenericMethod && HasImplicitTypeArguments(node))
                {
                    ImmutableArray<BoundExpression> updatedArguments = CreatePlaceholderExpressionsIfNecessary(arguments, results);
                    method = InferMethod(node, method, updatedArguments);
                }
                VisitArgumentsWarn(arguments, conversions, refKindsOpt, method.Parameters, node.ArgsToParamsOpt, node.Expanded, results);
            }

            UpdateStateForCall(node);

            if (method.MethodKind == MethodKind.LocalFunction)
            {
                var localFunc = (LocalFunctionSymbol)method.OriginalDefinition;
                ReplayReadsAndWrites(localFunc, node.Syntax, writes: true);
            }

            _result = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(method);
            return null;
        }

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
            ImmutableArray<string> namesOpt,
            ImmutableArray<RefKind> refKindsOpt,
            MethodSymbol method,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded)
        {
            VisitArguments(node, arguments, namesOpt, refKindsOpt, (method is null) ? default : method.Parameters, argsToParamsOpt, expanded);
        }

        private void VisitArguments(
            BoundExpression node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<string> namesOpt,
            ImmutableArray<RefKind> refKindsOpt,
            PropertySymbol property,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded)
        {
            VisitArguments(node, arguments, namesOpt, refKindsOpt, (property is null) ? default : property.Parameters, argsToParamsOpt, expanded);
        }

        private void VisitArguments(
            BoundExpression node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<string> namesOpt,
            ImmutableArray<RefKind> refKindsOpt,
            ImmutableArray<ParameterSymbol> parametersOpt,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded)
        {
            if (node.HasErrors)
            {
                return;
            }
            arguments = RemoveArgumentConversions(arguments, refKindsOpt, out var conversions);
            ImmutableArray<Result> results = VisitArgumentsEvaluate(arguments, refKindsOpt, expanded);
            if (!parametersOpt.IsDefault)
            {
                VisitArgumentsWarn(arguments, conversions, refKindsOpt, parametersOpt, argsToParamsOpt, expanded, results);
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
            ImmutableArray<Conversion> conversionsOpt,
            ImmutableArray<RefKind> refKindsOpt,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded,
            ImmutableArray<Result> results)
        {
            for (int i = 0; i < arguments.Length; i++)
            {
                bool expandedParameter = expanded;
                var parameter = GetCorrespondingParameter(i, parameters, argsToParamsOpt, ref expandedParameter);
                if (parameter is null)
                {
                    continue;
                }
                VisitArgumentWarn(
                    arguments[i],
                    conversionsOpt.IsDefault ? Conversion.Identity : conversionsOpt[i],
                    GetRefKind(refKindsOpt, i),
                    parameter,
                    expandedParameter,
                    results[i]);
            }
        }

        private void VisitArgumentWarn(
            BoundExpression argument,
            Conversion conversion,
            RefKind refKind,
            ParameterSymbol parameter,
            bool expandedParameter,
            Result result)
        {
            var parameterType = GetParameterType(parameter, expandedParameter);
            var unconvertedType = result.Type;
            result = ApplyConversion(
                argument,
                conversion,
                parameterType.TypeSymbol,
                result,
                checkConversion: true,
                requireIdentity: refKind != RefKind.None,
                canConvert: out bool canConvert);
            bool reported = false;
            if (refKind != RefKind.Out)
            {
                reported = WarnOnNullReferenceArgument(argument, result.Type, parameter, parameterType);
            }
            if (!reported &&
                (!canConvert ||
                    (refKind == RefKind.None && !ConversionsBase.HasTopLevelNullabilityImplicitConversion(result.Type, parameterType))))
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInArgument, argument.Syntax, unconvertedType?.TypeSymbol, parameterType.TypeSymbol,
                        new FormattedSymbol(parameter, SymbolDisplayFormat.ShortFormat),
                        new FormattedSymbol(parameter.ContainingSymbol, SymbolDisplayFormat.MinimallyQualifiedFormat));
            }
            if (refKind != RefKind.None)
            {
                TrackNullableStateForAssignment(argument, result.Slot, result.Type, null, parameterType, -1);
            }
        }

        private static ImmutableArray<BoundExpression> RemoveArgumentConversions(ImmutableArray<BoundExpression> arguments, ImmutableArray<RefKind> refKindsOpt, out ImmutableArray<Conversion> conversions)
        {
            int n = arguments.Length;
            conversions = default;
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
                        argument = RemoveImplicitConversion(argument, out conversion);
                        if (argument != arguments[i])
                        {
                            includedConversion = true;
                        }
                    }
                    argumentsBuilder.Add(argument);
                    conversionsBuilder.Add(conversion);
                }
                if (includedConversion)
                {
                    conversions = conversionsBuilder.ToImmutable();
                    arguments = argumentsBuilder.ToImmutable();
                }
                conversionsBuilder.Free();
                argumentsBuilder.Free();
            }
            return arguments;
        }

        private static MethodSymbol GetLeastOverriddenMethod(MethodSymbol method)
        {
            // PROTOTYPE(NullableReferenceTypes): OverloadResolution.IsMemberApplicableInNormalForm
            // and Binder.CoerceArguments expect the least overridden method.
            return method;
        }

        private static ParameterSymbol GetCorrespondingParameter(int argumentOrdinal, ImmutableArray<ParameterSymbol> parameters, ImmutableArray<int> argsToParamsOpt, ref bool expanded)
        {
            Debug.Assert(!parameters.IsDefault);

            ParameterSymbol parameter;

            if (argsToParamsOpt.IsDefault)
            {
                if (argumentOrdinal < parameters.Length)
                {
                    parameter = parameters[argumentOrdinal];
                }
                else if (expanded)
                {
                    parameter = parameters[parameters.Length - 1];
                }
                else
                {
                    parameter = null;
                }
            }
            else
            {
                int parameterOrdinal = argsToParamsOpt[argumentOrdinal];

                if (parameterOrdinal < parameters.Length)
                {
                    parameter = parameters[parameterOrdinal];
                }
                else
                {
                    parameter = null;
                    expanded = false;
                }
            }

            Debug.Assert((object)parameter != null || !expanded);
            if (expanded && (parameter.Ordinal < parameters.Length - 1 || !parameter.Type.IsSZArray()))
            {
                expanded = false;
            }

            return parameter;
        }

        private static TypeSymbolWithAnnotations GetParameterType(ParameterSymbol parameter, bool expanded)
        {
            var type = parameter.Type;
            return (expanded && parameter.IsParams) ? ((ArrayTypeSymbol)type.TypeSymbol).ElementType : type;
        }

        private MethodSymbol InferMethod(BoundCall node, MethodSymbol method, ImmutableArray<BoundExpression> arguments)
        {
            Debug.Assert(method.IsGenericMethod);
            // Use least overridden method, matching OverloadResolution.IsMemberApplicableInNormalForm
            // and IsMemberApplicableInExpandedForm.
            var definition = GetLeastOverriddenMethod(method.ConstructedFrom);
            var refKinds = ArrayBuilder<RefKind>.GetInstance();
            if (node.ArgumentRefKindsOpt != null)
            {
                refKinds.AddRange(node.ArgumentRefKindsOpt);
            }
            // PROTOTYPE(NullableReferenceTypes): Can we use GetCorrespondingParameter
            // rather than OverloadResolution.GetEffectiveParameterTypes?
            OverloadResolution.GetEffectiveParameterTypes(
                definition,
                node.Arguments.Length,
                node.ArgsToParamsOpt,
                refKinds,
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

        // PROTOTYPE(NullableReferenceTypes): Can we avoid removing and re-applying
        // conversions and instead simply call VisitConversion in most cases?
        private static BoundExpression RemoveImplicitConversion(BoundExpression expr, out Conversion conversion)
        {
            BoundConversion asImplicitConversion(BoundExpression e)
            {
                if (e.Kind == BoundKind.Conversion)
                {
                    var c = (BoundConversion)e;
                    if (!c.ExplicitCastInCode)
                    {
                        return c;
                    }
                }
                return null;
            }

            conversion = Conversion.Identity;

            var conv = asImplicitConversion(expr);
            if (conv == null)
            {
                return expr;
            }

            switch (conv.Conversion.Kind)
            {
                case ConversionKind.Identity:
                case ConversionKind.DefaultOrNullLiteral:
                case ConversionKind.ImplicitReference:
                case ConversionKind.ImplicitTuple:
                case ConversionKind.ImplicitTupleLiteral:
                case ConversionKind.ImplicitUserDefined:
                case ConversionKind.MethodGroup:
                    break;
                default:
                    return expr;
            }

            conversion = conv.Conversion;
            expr = conv.Operand;

            while (true)
            {
                if (conversion.Kind == ConversionKind.ImplicitUserDefined)
                {
                    switch (conversion.UserDefinedFromConversion.Kind)
                    {
                        case ConversionKind.NoConversion:
                        case ConversionKind.Identity:
                        case ConversionKind.ImplicitTupleLiteral:
                            return expr;
                    }
                }

                conv = asImplicitConversion(expr);
                if (conv == null)
                {
                    return expr;
                }

                var next = conv.Conversion;
                expr = conv.Operand;

                if (conversion.Kind == ConversionKind.ImplicitUserDefined)
                {
                    Debug.Assert(conversion.UserDefinedFromConversion == next);
                    break;
                }

                Debug.Assert(conversion.Kind == ConversionKind.Identity ||
                    (next.Kind == ConversionKind.ImplicitUserDefined && next.UserDefinedToConversion == conversion));
                conversion = next;
            }

            return expr;
        }

        private void ReplayReadsAndWrites(LocalFunctionSymbol localFunc,
                                  SyntaxNode syntax,
                                  bool writes)
        {
            // PROTOTYPE(NullableReferenceTypes): Support field initializers in local functions.
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
            return AsMemberOfType(containingType, symbol);
        }

        private static Symbol AsMemberOfType(NamedTypeSymbol containingType, Symbol symbol)
        {
            if (symbol is null)
            {
                return null;
            }
            if (containingType.IsTupleType)
            {
                return AsMemberOfTupleType((TupleTypeSymbol)containingType, symbol);
            }
            if (symbol.Kind == SymbolKind.Method)
            {
                if (((MethodSymbol)symbol).MethodKind == MethodKind.LocalFunction)
                {
                    // PROTOTYPE(NullableReferenceTypes): Handle type substitution for local functions.
                    return symbol;
                }
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

        private static Symbol AsMemberOfTupleType(TupleTypeSymbol tupleType, Symbol symbol)
        {
            if (symbol.ContainingType.Equals(tupleType, TypeCompareKind.CompareNullableModifiersForReferenceTypes))
            {
                return symbol;
            }
            var underlyingType = tupleType.UnderlyingNamedType;
            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                    {
                        var index = ((FieldSymbol)symbol).TupleElementIndex;
                        if (index >= 0)
                        {
                            return tupleType.TupleElements[index];
                        }
                        return new TupleFieldSymbol(tupleType, (FieldSymbol)AsMemberOfType(underlyingType, ((TupleFieldSymbol)symbol).UnderlyingField), index);
                    }
                case SymbolKind.Property:
                    return new TuplePropertySymbol(tupleType, (PropertySymbol)AsMemberOfType(underlyingType, ((TuplePropertySymbol)symbol).UnderlyingProperty));
                case SymbolKind.Event:
                    return new TupleEventSymbol(tupleType, (EventSymbol)AsMemberOfType(underlyingType, ((TupleEventSymbol)symbol).UnderlyingEvent));
                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
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

            var operand = node.Operand;
            Visit(operand);

            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                var operandResult = _result;
                Result result;
                if (operand.Kind == BoundKind.Literal && (object)operand.Type == null && operand.ConstantValue.IsNull)
                {
                    result = Result.Create(TypeSymbolWithAnnotations.Create(node.Type, true));
                }
                else if (node.ConversionKind == ConversionKind.Identity && !node.ExplicitCastInCode)
                {
                    result = operandResult;
                }
                else
                {
                    result = ApplyConversion(node, node.Conversion, node.Type, operandResult, checkConversion: !node.ExplicitCastInCode, requireIdentity: false, canConvert: out bool _);
                }
                _result = result;
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
            var elementTypes = arguments.SelectAsArray((a, w) => w.VisitRvalue(a).Type, this);
            var tupleOpt = (TupleTypeSymbol)node.Type;
            _result = tupleOpt is null ?
                (Result)node :
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

            bool IsNullabilityMismatch(TypeSymbolWithAnnotations source, TypeSymbolWithAnnotations destination, bool requireIdentity)
            {
                if (!HasTopLevelNullabilityConversion(source, destination, requireIdentity))
                {
                    return true;
                }
                var sourceType = source.TypeSymbol;
                var destinationType = destination.TypeSymbol;
                if (requireIdentity)
                {
                    return source.Equals(destination, TypeCompareKind.AllIgnoreOptions) &&
                        !source.Equals(destination, TypeCompareKind.AllIgnoreOptions | TypeCompareKind.CompareNullableModifiersForReferenceTypes | TypeCompareKind.UnknownNullableModifierMatchesAny);
                }
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                return !_conversions.ClassifyImplicitConversionFromType(sourceType, destinationType, ref useSiteDiagnostics).Exists;
            }

            if (IsNullabilityMismatch(method.ReturnType, invoke.ReturnType, requireIdentity: false))
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, syntax,
                    new FormattedSymbol(method, SymbolDisplayFormat.MinimallyQualifiedFormat),
                    delegateType);
            }

            int count = Math.Min(invoke.ParameterCount, method.ParameterCount);
            for (int i = 0; i < count; i++)
            {
                if (IsNullabilityMismatch(invoke.Parameters[i].Type, method.Parameters[i].Type, requireIdentity: method.MethodKind == MethodKind.LambdaMethod))
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, syntax,
                        new FormattedSymbol(method.Parameters[i], SymbolDisplayFormat.ShortFormat),
                        new FormattedSymbol(method, SymbolDisplayFormat.MinimallyQualifiedFormat),
                        delegateType);
                }
            }
        }

        private Result ApplyConversion(BoundExpression expr, Conversion conversion, TypeSymbol targetType, Result operand, bool checkConversion, bool requireIdentity, out bool canConvert)
        {
            bool? isNullable = null;
            canConvert = true;

            switch (conversion.Kind)
            {
                case ConversionKind.NoConversion:
                    canConvert = false;
                    break;

                case ConversionKind.MethodGroup:
                    if (checkConversion)
                    {
                        conversion = ClassifyImplicitConversion(operand, targetType);
                        canConvert = conversion.Exists;
                        ReportNullabilityMismatchWithTargetDelegate(expr.Syntax, targetType.GetDelegateType(), conversion.Method);
                    }
                    isNullable = false;
                    break;

                case ConversionKind.AnonymousFunction:
                case ConversionKind.InterpolatedString:
                    isNullable = false;
                    break;

                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.ImplicitUserDefined:
                    {
                        var methodOpt = GetUnaryMethodIfAny(conversion.Method);
                        if (methodOpt is null)
                        {
                            canConvert = false;
                        }
                        else
                        {
                            var parameter = methodOpt.Parameters[0];
                            operand = ApplyConversion(expr, conversion.UserDefinedFromConversion, parameter.Type.TypeSymbol, operand, checkConversion, requireIdentity: false, canConvert: out canConvert);
                            if (canConvert)
                            {
                                WarnOnNullReferenceArgument(expr, operand.Type, parameter);
                                var returnType = methodOpt.ReturnType;
                                operand = ApplyConversion(expr, conversion.UserDefinedToConversion, targetType, returnType, checkConversion, requireIdentity: false, canConvert: out canConvert);
                                isNullable = returnType.IsNullable;
                            }
                        }
                    }
                    break;

                case ConversionKind.Unboxing:
                case ConversionKind.ExplicitDynamic:
                case ConversionKind.ImplicitDynamic:
                    break;

                case ConversionKind.ImplicitThrow:
                    Debug.Assert(operand.Expression.Kind == BoundKind.ThrowExpression);
                    return operand;

                case ConversionKind.Boxing:
                    {
                        var operandType = operand.Type;
                        if (operandType?.IsValueType == true)
                        {
                            // PROTOTYPE(NullableReferenceTypes): Should we worry about a pathological case of boxing nullable value known to be not null?
                            //       For example, new int?(0)
                            isNullable = operandType.IsNullableType();
                        }
                        else
                        {
                            Debug.Assert(operandType?.IsReferenceType != true ||
                                operandType.SpecialType == SpecialType.System_ValueType ||
                                operandType.TypeKind == TypeKind.Interface ||
                                operandType.TypeKind == TypeKind.Dynamic);
                        }
                    }
                    break;

                case ConversionKind.Identity:
                    {
                        var op = operand.Expression;
                        if (op != null && (op.IsLiteralNull() || op.IsLiteralDefault()))
                        {
                            isNullable = true;
                            break;
                        }
                        goto case ConversionKind.ImplicitReference;
                    }
                case ConversionKind.DefaultOrNullLiteral:
                case ConversionKind.ImplicitReference:
                case ConversionKind.ExplicitReference:
                    // Inherit state from the operand
                    // PROTOTYPE(NullableReferenceTypes): Should an explicit cast cast away
                    // outermost nullability? For instance, is `s` a `string!` or `string?`?
                    // object? obj = ...; var s = (string)obj;
                    if (checkConversion)
                    {
                        conversion = ClassifyImplicitConversion(operand, targetType);
                        canConvert = requireIdentity ? (conversion.Kind == ConversionKind.Identity) : conversion.Exists;
                    }
                    isNullable = operand.IsNullable;
                    break;

                case ConversionKind.ExplicitEnumeration:
                    // Can reach here, with an error type.
                    break;

                case ConversionKind.ImplicitTupleLiteral:
                case ConversionKind.ExplicitTupleLiteral:
                    // TupleLiteral conversions are identity conversions. The actual tuple
                    // element conversions are within the ConvertedTupleLiteral operand.
                    isNullable = false;
                    break;

                case ConversionKind.Deconstruction:
                case ConversionKind.ImplicitTuple:
                case ConversionKind.ExplicitTuple:
                    {
                        var operandTuple = operand.Type?.TypeSymbol as TupleTypeSymbol;
                        var targetTuple = targetType as TupleTypeSymbol;
                        if (operandTuple is null || targetTuple is null)
                        {
                            // May be null in error cases, at least for deconstruction.
                            break;
                        }
                        int n = operandTuple.TupleElementTypes.Length;
                        var builder = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance(n);
                        var underlyingConversions = conversion.UnderlyingConversions;
                        var targetElementTypes = targetTuple.TupleElementTypes;
                        var operandElementTypes = operandTuple.TupleElementTypes;
                        for (int i = 0; i < n; i++)
                        {
                            var targetElementType = targetElementTypes[i];
                            var operandElementType = operandElementTypes[i];
                            var elementResult = ApplyConversion(
                                expr,
                                underlyingConversions[i],
                                targetElementType.TypeSymbol,
                                Result.Create(operandElementType),
                                checkConversion,
                                requireIdentity,
                                out bool canConvertElement);
                            if (!canConvertElement || !HasTopLevelNullabilityConversion(operandElementType, targetElementType, requireIdentity))
                            {
                                canConvert = false;
                            }
                            builder.Add(elementResult.Type);
                        }
                        return Result.Create(TypeSymbolWithAnnotations.Create(TupleTypeSymbol.Create(
                            default,
                            builder.ToImmutableAndFree(),
                            elementLocations: default,
                            elementNames: targetTuple.TupleElementNames,
                            compilation: compilation,
                            shouldCheckConstraints: false,
                            errorPositions: default)));
                    }

                default:
                    Debug.Assert(targetType?.IsReferenceType != true);
                    break;
            }

            return Result.Create(TypeSymbolWithAnnotations.Create(targetType, isNullable), operand.Slot);
        }

        private static bool HasTopLevelNullabilityConversion(TypeSymbolWithAnnotations source, TypeSymbolWithAnnotations destination, bool requireIdentity)
        {
            return requireIdentity ?
                ConversionsBase.HasTopLevelNullabilityIdentityConversion(source, destination) :
                ConversionsBase.HasTopLevelNullabilityImplicitConversion(source, destination);
        }

        private Conversion ClassifyImplicitConversion(Result source, TypeSymbol destination)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            return (source.Expression is null) ?
                _conversions.ClassifyImplicitConversionFromType(source.Type.TypeSymbol, destination, ref useSiteDiagnostics) :
                _conversions.ClassifyImplicitConversionFromExpression(source.Expression, destination, ref useSiteDiagnostics);
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

            var method = AsMemberOfResultType(node.LookupSymbolOpt);
            var methods = node.Methods.SelectAsArray((m, w) => (MethodSymbol)w.AsMemberOfResultType(m), this);
            _result = new BoundMethodGroup(node.Syntax, node.TypeArgumentsOpt, node.Name, methods, method, node.LookupError, node.Flags, node.ReceiverOpt, node.ResultKind, node.HasErrors);
            return null;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            var result = VisitLambdaOrLocalFunction(node);
            SetResult(node); // PROTOTYPE(NullableReferenceTypes)
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

            var left = node.Left;
            VisitLvalue(left);
            Result leftResult = _result;

            var right = RemoveImplicitConversion(node.Right, out var conversion);
            Result rightResult = VisitRvalue(right);

            // byref assignment is also a potential write
            if (node.RefKind != RefKind.None)
            {
                WriteArgument(right, node.RefKind, method: null);
            }

            rightResult = ApplyConversion(right, conversion, leftResult.Type?.TypeSymbol, rightResult);

            if (left.Kind == BoundKind.EventAccess && ((BoundEventAccess)left).EventSymbol.IsWindowsRuntimeEvent)
            {
                // Event assignment is a call to an Add method.
                _result = TypeSymbolWithAnnotations.Create(node.Type);
            }
            else
            {
                TrackNullableStateForAssignment(left, leftResult.Slot, leftResult.Type, right, rightResult.Type, rightResult.Slot);
                // PROTOTYPE(NullableReferenceTypes): Check node.Type.IsErrorType() instead?
                _result = node.HasErrors ? Result.Create(TypeSymbolWithAnnotations.Create(node.Type)) : rightResult;
            }

            return null;
        }

        public override BoundNode VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            var left = node.Left;
            VisitLvalue(left);
            var leftResult = _result;

            var right = RemoveImplicitConversion(node.Right, out var conversion);
            var rightResult = VisitRvalue(right);

            rightResult = ApplyConversion(right, conversion, leftResult.Type?.TypeSymbol, rightResult);

            // PROTOTYPE(NullableReferenceTypes): Assign each of the deconstructed values.
            // See IdentityConversion_DeconstructionAssignment test.
            _result = node.HasErrors ? TypeSymbolWithAnnotations.Create(node.Type) : rightResult;
            return null;
        }

        private static MethodSymbol GetUnaryMethodIfAny(MethodSymbol methodOpt)
        {
            return (object)methodOpt != null && methodOpt.ParameterCount == 1 ? methodOpt : null;
        }

        private static MethodSymbol GetUserDefinedConversionMethodIfAny(Conversion conversion)
        {
            return conversion.IsUserDefined ? GetUnaryMethodIfAny(conversion.Method) : null;
        }

        public override BoundNode VisitIncrementOperator(BoundIncrementOperator node)
        {
            Debug.Assert(!IsConditionalState);

            var operandResult = VisitRvalue(node.Operand);
            Result incrementResult;

            if (node.OperatorKind.IsDynamic())
            {
                incrementResult = operandResult;
            }
            else
            {
                // PROTOTYPE(NullableReferenceTypes): Update increment method based on operand type.
                MethodSymbol incrementOperator = node.OperatorKind.IsUserDefined() ? GetUnaryMethodIfAny(node.MethodOpt) : null;
                TypeSymbol targetTypeOfOperandConversion;
                var conversionMethod = GetUserDefinedConversionMethodIfAny(node.OperandConversion);
                if ((object)conversionMethod != null)
                {
                    targetTypeOfOperandConversion = conversionMethod.ReturnType.TypeSymbol;
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

                Result operandConversionResult = (targetTypeOfOperandConversion is null) ?
                    operandResult :
                    ApplyConversion(node.Operand, node.OperandConversion, targetTypeOfOperandConversion, operandResult);

                if ((object)incrementOperator == null)
                {
                    incrementResult = operandConversionResult;
                }
                else
                {
                    WarnOnNullReferenceArgument(node.Operand, operandConversionResult.Type, incrementOperator.Parameters[0]);

                    incrementResult = Result.Create(GetTypeOrReturnTypeWithAdjustedNullableAnnotations(incrementOperator));
                }

                incrementResult = ApplyConversion(node, node.ResultConversion, node.Type, incrementResult);
            }

            // PROTOTYPE(NullableReferenceTypes): Check node.Type.IsErrorType() instead?
            if (node.HasErrors)
            {
                this.SetResult(node);
            }
            else
            {
                var op = node.OperatorKind.Operator();
                _result = (op == UnaryOperatorKind.PrefixIncrement || op == UnaryOperatorKind.PrefixDecrement) ? incrementResult : operandResult;
                TrackNullableStateForAssignment(node.Operand, operandResult.Slot, operandResult.Type, value: node, valueType: incrementResult.Type, valueSlot: -1);
            }

            return null;
        }

        public override BoundNode VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            VisitLvalue(node.Left); // PROTOTYPE(NullableReferenceTypes): Method should be called VisitValue rather than VisitLvalue.
            Result leftResult = _result;

            Debug.Assert(!IsConditionalState);

            Result leftOnRightResult = GetAdjustedResult(leftResult);

            if (!node.Operator.Kind.IsDynamic())
            {
                // PROTOTYPE(NullableReferenceTypes): Update operator based on inferred argument types.
                leftOnRightResult = ApplyConversion(node.Left, node.LeftConversion, node.Operator.LeftType, leftOnRightResult);
            }

            var right = RemoveImplicitConversion(node.Right, out var rightConversion);
            Result rightResult = VisitRvalue(right);

            rightResult = ApplyConversion(right, rightConversion, node.Operator.RightType, rightResult);

            TypeSymbolWithAnnotations resultType;
            if ((object)node.Operator.ReturnType != null)
            {
                if (node.Operator.Kind.IsUserDefined() && (object)node.Operator.Method != null && node.Operator.Method.ParameterCount == 2)
                {
                    WarnOnNullReferenceArgument(node.Left, leftOnRightResult.Type, node.Operator.Method.Parameters[0]);
                    WarnOnNullReferenceArgument(node.Right, rightResult.Type, node.Operator.Method.Parameters[1]);
                }

                // PROTOTYPE(NullableReferenceTypes): Update method based on inferred operand type.
                resultType = InferResultNullability(node.Operator.Kind, node.Operator.Method, node.Operator.ReturnType, leftOnRightResult.Type, rightResult.Type);
                resultType = ApplyConversion(node, node.FinalConversion, node.Type, Result.Create(resultType)).Type;
            }
            else
            {
                resultType = null;
            }

            TrackNullableStateForAssignment(node, leftResult.Slot, leftResult.Type, node, resultType, -1);
            _result = resultType;
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

        private bool WarnOnNullReferenceArgument(BoundExpression argument, TypeSymbolWithAnnotations argumentType, ParameterSymbol parameter, TypeSymbolWithAnnotations paramType = null)
        {
            if (argumentType?.IsNullable == true)
            {
                if (paramType is null)
                {
                    Debug.Assert(!parameter.IsParams);
                    paramType = parameter.Type;
                }
                if (paramType.IsReferenceType && paramType.IsNullable == false)
                {
                    if (!CheckNullAsNonNullableReference(argument))
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
            VisitArguments(node, node.Arguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, method, node.ArgsToParamsOpt, node.Expanded);

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

            Result receiverResult = VisitRvalue(receiverOpt);

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
            TypeSymbolWithAnnotations resultType = null;

            // PROTOTYPE(NullableReferenceTypes): Update method based on inferred operand type.
            if (node.OperatorKind.IsUserDefined())
            {
                var operatorMethod = GetUnaryMethodIfAny(node.MethodOpt);
                if ((object)operatorMethod != null)
                {
                    WarnOnNullReferenceArgument(node.Operand, _result.Type, operatorMethod.Parameters[0]);
                    resultType = ConstructNullableIfNecessary(operatorMethod.ReturnType, useNullable: node.OperatorKind.IsLifted());
                }
            }

            _result = resultType ?? TypeSymbolWithAnnotations.Create(node.Type);
            return result;
        }

        private TypeSymbolWithAnnotations ConstructNullableIfNecessary(TypeSymbolWithAnnotations type, bool useNullable)
        {
            if (useNullable)
            {
                Debug.Assert(type.IsValueType);
                return TypeSymbolWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(type.TypeSymbol));
            }
            return type;
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

        protected override void VisitBinaryLogicalOperatorChildren(ArrayBuilder<BoundExpression> stack)
        {
            var child = GetBinaryLogicalOperatorLeft(stack.Peek());
            VisitCondition(child);

            while (true)
            {
                var binary = stack.Pop();
                BinaryOperatorKind kind;
                BoundExpression right = GetBinaryLogicalOperatorRight(binary, out kind);

                var op = kind.Operator();
                var isAnd = op == BinaryOperatorKind.And;
                var isBool = kind.OperandTypes() == BinaryOperatorKind.Bool;

                Debug.Assert(isAnd || op == BinaryOperatorKind.Or);

                var leftTrue = this.StateWhenTrue;
                var leftFalse = this.StateWhenFalse;
                SetState(isAnd ? leftTrue : leftFalse);

                AfterLeftChildOfBinaryLogicalOperatorHasBeenVisited(binary, right, isAnd);
                AfterRightChildOfBinaryLogicalOperatorHasBeenVisited(binary, right, isAnd, isBool, ref leftTrue, ref leftFalse);

                if (stack.Count == 0)
                {
                    break;
                }

                AdjustConditionalState(binary);
            }
        }

        private void AfterLeftChildOfBinaryLogicalOperatorHasBeenVisited(BoundExpression node, BoundExpression right, bool isAnd)
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
                    // PROTOTYPE(NullableReferenceTypes): Unwrap implicit conversions and re-calculate.
                    WarnOnNullReferenceArgument(left, leftType, trueFalseOperator.Parameters[0]);
                }

                if ((object)logicalOperator != null)
                {
                    // PROTOTYPE(NullableReferenceTypes): Unwrap implicit conversions and re-calculate.
                    WarnOnNullReferenceArgument(left, leftType, logicalOperator.Parameters[0]);
                }

                // PROTOTYPE(NullableReferenceTypes): Unwrap implicit conversions and re-calculate.
                Visit(right);
                TypeSymbolWithAnnotations rightType = _result.Type;

                _result = InferResultNullabilityOfBinaryLogicalOperator(node, leftType, rightType);

                if ((object)logicalOperator != null)
                {
                    WarnOnNullReferenceArgument(right, rightType, logicalOperator.Parameters[1]);
                }
            }
        }

        private TypeSymbolWithAnnotations InferResultNullabilityOfBinaryLogicalOperator(BoundExpression node, TypeSymbolWithAnnotations leftType, TypeSymbolWithAnnotations rightType)
        {
            switch (node.Kind)
            {
                case BoundKind.BinaryOperator:
                    {
                        var binary = (BoundBinaryOperator)node;
                        // PROTOTYPE(NullableReferenceTypes): Update method based on inferred operand type.
                        return InferResultNullability(binary.OperatorKind, binary.MethodOpt, binary.Type, leftType, rightType);
                    }
                case BoundKind.UserDefinedConditionalLogicalOperator:
                    {
                        var binary = (BoundUserDefinedConditionalLogicalOperator)node;
                        // PROTOTYPE(NullableReferenceTypes): Update method based on inferred operand types.
                        var method = binary.LogicalOperator;
                        if ((object)method != null && method.ParameterCount == 2)
                        {
                            return ConstructNullableIfNecessary(method.ReturnType, useNullable: binary.OperatorKind.IsLifted());
                        }
                        return TypeSymbolWithAnnotations.Create(node.Type);
                    }
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
            SetResult(node);
            return null;
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
            return result;
        }

        public override BoundNode VisitSuppressNullableWarningExpression(BoundSuppressNullableWarningExpression node)
        {
            VisitRvalue(node.Expression);
            _result = node;
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
            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt, expanded: false);
            Debug.Assert((object)node.Type == null);
            SetResult(node);
            return null;
        }

        public override BoundNode VisitLiteral(BoundLiteral node)
        {
            SetResult(node);
            return null;
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
            VisitArguments(node, node.Arguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, (MethodSymbol)null, argsToParamsOpt: default, expanded: false);
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
            VisitArguments(node, node.Arguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, (MethodSymbol)null, argsToParamsOpt: default, expanded: false);

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
            base.VisitThrowExpression(node);
            SetResult(node);
            return null;
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

        // PROTOTYPE(NullableReferenceTypes): Replace Result with BoundNode, making
        // NullableWalker a proper rewriter instead, returning the result (the rewritten
        // BoundNode) from each Visit method rather than storing in _result.
        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        internal struct Result
        {
            internal readonly BoundExpression Expression;
            internal readonly TypeSymbolWithAnnotations Type;
            internal readonly int Slot;

            internal static readonly Result Unset = new Result(null, null);

            internal static Result Create(TypeSymbolWithAnnotations type, int slot = -1)
            {
                return new Result(null, type, slot);
            }

            // PROTOTYPE(NullableReferenceTypes): Consider replacing implicit cast operators with
            // explicit methods - either Result.Create() overloads or ToResult() extension methods.
            public static implicit operator Result(BoundExpression expr)
            {
                var type = expr?.GetTypeAndNullability(includeNullability: true);
                return new Result(expr, type);
            }

            public static implicit operator Result(TypeSymbolWithAnnotations type)
            {
                return new Result(null, type);
            }

            internal bool? IsNullable => (Expression is null) ? Type?.IsNullable : Expression.IsNullable();

            private Result(BoundExpression expr, TypeSymbolWithAnnotations type, int slot = -1)
            {
                Expression = expr;
                Type = type;
                Slot = slot;
            }

            private string GetDebuggerDisplay()
            {
                var expr = (object)Expression == null ? "<null>" : Expression.GetDebuggerDisplay();
                var type = (object)Type == null ? "<null>" : Type.GetDebuggerDisplay();
                return $"Expression={expr}, Type={type}, Slot={Slot}";
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
