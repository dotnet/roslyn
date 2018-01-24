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

        /// <summary>
        /// Perform data flow analysis, reporting all necessary diagnostics.
        /// </summary>
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

        private Result CheckImplicitConversion(BoundExpression value, TypeSymbol targetType, Result valueResult, Conversion originalConversion)
        {
            Debug.Assert(value != null);

            if (targetType is null)
            {
                return Result.Unset;
            }

            var valueType = valueResult.Type;
            if ((valueResult.Expression is null && valueType is null) ||
                valueType?.IsErrorType() == true || targetType.IsErrorType())
            {
                return Result.Create(TypeSymbolWithAnnotations.Create(targetType));
            }

            value = CreatePlaceholderExpressionIfNecessary(value, valueResult);

            var syntax = value.Syntax;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var conversion = _conversions.ClassifyImplicitConversionFromExpression(value, targetType, ref useSiteDiagnostics);

            Debug.Assert(originalConversion.Kind == ConversionKind.NoConversion ||
                conversion.Kind == ConversionKind.NoConversion ||
                (originalConversion.Kind == ConversionKind.ImplicitUserDefined) == (conversion.Kind == ConversionKind.ImplicitUserDefined));

            switch (conversion.Kind)
            {
                case ConversionKind.NoConversion:
                    // PROTOTYPE(NullableReferenceTypes): Not all scenarios are assignments.
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInAssignment, syntax, valueType.TypeSymbol, targetType);
                    return Result.Create(TypeSymbolWithAnnotations.Create(targetType, valueType?.IsNullable), valueResult.Slot);

                case ConversionKind.ImplicitUserDefined:
                    {
                        var conversionMethod = GetUserDefinedConversionMethodIfAny(conversion);
                        if ((object)conversionMethod != null)
                        {
                            WarnOnNullReferenceArgument(value, valueType, conversionMethod.Parameters[0], expanded: false);
                        }
                    }
                    break;

                case ConversionKind.AnonymousFunction:
                    switch (value.Kind)
                    {
                        case BoundKind.UnboundLambda:
                            {
                                var boundConversion = Binder.CreateAnonymousFunctionConversion(syntax, value, conversion, isCast: false, destination: targetType, diagnostics: Diagnostics);
                                var lambda = (BoundLambda)boundConversion.Operand;
                                ReportNullabilityMismatchWithTargetDelegate(syntax, targetType.GetDelegateType(), lambda.Symbol);
                                return boundConversion;
                            }
                        case BoundKind.Lambda:
                            throw ExceptionUtilities.UnexpectedValue(value.Kind);
                    }
                    break;

                case ConversionKind.MethodGroup:
                    ReportNullabilityMismatchWithTargetDelegate(syntax, targetType.GetDelegateType(), conversion.Method);
                    break;
            }

            return InferResultNullability(conversion, targetType, valueResult, allowImplicitConversions: true);
        }

        private static BoundExpression CreatePlaceholderExpressionIfNecessary(BoundExpression value, Result valueResult)
        {
            var valueType = valueResult.Type;
            return valueResult.Expression ?? new BoundValuePlaceholder(value.Syntax, valueType?.IsNullable, valueType?.TypeSymbol);
        }

        private static ImmutableArray<BoundExpression> CreatePlaceholderExpressionsIfNecessary(ImmutableArray<BoundExpression> values, ImmutableArray<Result> valueResults)
        {
            return valueResults.ZipAsArray(values, (r, v) => CreatePlaceholderExpressionIfNecessary(v, r));
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
                                InheritNullableStateOfTrackableType(targetSlot, valueSlot, isByRefTarget);
                            }
                        }
                    }
                }
                else if (targetSlot > 0 && EmptyStructTypeCache.IsTrackableStructType(targetType.TypeSymbol) &&
                        (value == null || targetType.TypeSymbol.Equals(valueType?.TypeSymbol, TypeCompareKind.ConsiderEverything)))
                {
                    InheritNullableStateOfTrackableStruct(targetType.TypeSymbol, targetSlot, valueSlot, IsByRefTarget(targetSlot));
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

        public override BoundNode VisitBlock(BoundBlock node)
        {
            DeclareVariables(node.Locals);

            VisitStatementsWithLocalFunctions(node);

            return null;
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
            {
                TypeSymbolWithAnnotations returnType = GetReturnType(this._currentMethodOrLambda);
                result = CheckImplicitConversion(expr, returnType.TypeSymbol, result, conversion);

                if (result.Type?.IsNullable == true)
                {
                    if ((object)returnType != null && returnType.IsReferenceType && returnType.IsNullable == false &&
                        !CheckNullAsNonNullableReference(expr))
                    {
                        ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceReturn, expr.Syntax);
                    }
                }
            }

            return null;
        }

        private TypeSymbolWithAnnotations GetReturnType(MethodSymbol method)
        {
            var returnType = method.ReturnType;
            return method.IsGenericTaskReturningAsync(compilation) ?
                ((NamedTypeSymbol)returnType.TypeSymbol).TypeArgumentsNoUseSiteDiagnostics[0] :
                returnType;
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
            SetUnknownResultNullability();
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

            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                _result = GetAdjustedResult(GetDeclaredLocalResult(node.LocalSymbol));
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
                initializer = RemoveImplicitConversion(initializer, out var conversion);
                var value = VisitRvalue(initializer);
                TypeSymbolWithAnnotations type = local.Type;

                if (node.DeclaredType.InferredType)
                {
                    _variableTypes[local] = value.Type;
                    type = value.Type;
                }

                value = CheckImplicitConversion(initializer, type.TypeSymbol, value, conversion);
                TrackNullableStateForAssignment(node, slot, type, initializer, value.Type, value.Slot);
            }

            return null;
        }

        protected override BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            Debug.Assert(!IsConditionalState);
            _result = _invalidType; // PROTOTYPE(NullableReferenceTypes): Move to `Visit` method?
            var result = base.VisitExpressionWithoutStackGuard(node);
#if DEBUG
            // Verify Visit method set ResultType.
            if (!IsConditionalState)
            {
                var resultType = _result.Type;
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
            return typeA.IsErrorType() ||
                typeB.IsErrorType() ||
                typeA.IsDynamic() ||
                typeB.IsDynamic() ||
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
                        InheritNullableStateOfTrackableStruct(type, slot, -1, false);
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
                    var result = VisitRvalue(node);
                    var type = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(containingSymbol);
                    TrackNullableStateForAssignment(node, containingSlot, type, node, result.Type, result.Slot);
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
                        int slot = (containingSlot < 0) ? -1 : GetOrCreateSlot(symbol, containingSlot);
                        VisitObjectCreationInitializer(symbol, slot, node.Right);
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

            VisitArguments(node, node.Arguments, default, default, node.AddMethod, node.ArgsToParamsOpt, node.Expanded);
            SetUnknownResultNullability();
        }

        private void SetResult(BoundExpression node)
        {
            Debug.Assert(!IsConditionalState);

            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                _result = node;
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
                int receiverSlot = -1;

                var arguments = node.Arguments;
                var constructor = node.Constructor;
                for (int i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];
                    // PROTOTYPE(NullableReferenceTypes): Unwrap implicit conversions and re-calculate.
                    Result argumentResult = VisitRvalue(argument);
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
            else
            {
                return base.VisitAnonymousObjectCreationExpression(node);
            }
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

            var elementBuilder = ArrayBuilder<BoundExpression>.GetInstance();
            GetArrayElements(node.InitializerOpt, elementBuilder);

            var resultBuilder = ArrayBuilder<Result>.GetInstance(elementBuilder.Count);
            for (int i = 0; i < elementBuilder.Count; i++)
            {
                var element = RemoveImplicitConversion(elementBuilder[i], out var conversion);
                elementBuilder[i] = element;
                resultBuilder.Add(VisitRvalue(element));
            }

            if (node.Syntax.Kind() == SyntaxKind.ImplicitArrayCreationExpression)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                var bestType = BestTypeInferrer.InferBestType(resultBuilder.SelectAsArray(r => r.Type?.TypeSymbol), _conversions, useSiteDiagnostics: ref useSiteDiagnostics);
                if (bestType is null)
                {
                    bestType = elementType.TypeSymbol;
                }
                var isNullable = GetIsNullable(resultBuilder);
                elementType = TypeSymbolWithAnnotations.Create(bestType, isNullable);
                arrayType = arrayType.WithElementType(elementType);
            }

            for (int i = 0; i < elementBuilder.Count; i++)
            {
                var element = elementBuilder[i];
                var result = CheckImplicitConversion(element, elementType?.TypeSymbol, resultBuilder[i], Conversion.NoConversion);
                if (elementType?.IsReferenceType == true)
                {
                    TrackNullableStateForAssignment(element, -1, elementType, element, result.Type, result.Slot);
                }
            }
            resultBuilder.Free();

            elementBuilder.Free();
            _result = _invalidType;
            return arrayType;
        }

        private static bool? GetIsNullable(ArrayBuilder<Result> results)
        {
            bool isNullable = false;
            foreach (var result in results)
            {
                var type = result.Type;
                if (type is null)
                {
                    // PROTOTYPE(NullableReferenceTypes): Should ignore untyped
                    // expressions such as unbound lambdas and typeless tuples.
                    // See StaticNullChecking.LocalVar_Array_02 test.
                    isNullable = true;
                    continue;
                }
                if (!type.IsReferenceType)
                {
                    return null;
                }
                switch (type.IsNullable)
                {
                    case null:
                        return null;
                    case true:
                        isNullable = true;
                        break;
                }
            }
            return isNullable;
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

            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                _result = type?.ElementType;
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
                    // PROTOTYPE(NullableReferenceTypes): Should return methodOpt.ReturnType
                    // since that type might include nested nullability inferred from this flow analysis.
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

        protected override void VisitBinaryOperatorChildren(ArrayBuilder<BoundBinaryOperator> stack)
        {
            var binary = stack.Peek();
            var left = RemoveImplicitConversion(binary.Left, out var conversion);
            var leftResult = VisitRvalue(left);
            leftResult = CheckImplicitConversion(left, binary.Left.Type, leftResult, conversion);

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
                    WarnOnNullReferenceArgument(binary.Left, leftType, binary.MethodOpt.Parameters[0], expanded: false);
                }

                var right = RemoveImplicitConversion(binary.Right, out var conversion);
                var rightResult = VisitRvalue(right);

                Debug.Assert(!IsConditionalState);
                rightResult = CheckImplicitConversion(right, binary.Right.Type, rightResult, conversion);

                // At this point, State.Reachable may be false for
                // invalid code such as `s + throw new Exception()`.
                TypeSymbolWithAnnotations rightType = rightResult.Type;

                if (warnOnNullReferenceArgument)
                {
                    WarnOnNullReferenceArgument(right, rightType, binary.MethodOpt.Parameters[1], expanded: false);
                }

                Debug.Assert(!IsConditionalState);
                var result = Result.Create(InferResultNullability(binary, leftType, rightType));
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

            var leftOperand = node.LeftOperand;
            var leftConversion = node.LeftConversion;
            var rightOperand = RemoveImplicitConversion(node.RightOperand, out var rightConversion);

            var leftResult = VisitRvalue(leftOperand);
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

            leftResult = CheckImplicitConversion(node.LeftOperand, node.Type, leftResult, leftConversion);
            rightResult = CheckImplicitConversion(node.RightOperand, node.Type, rightResult, rightConversion);

            TypeSymbolWithAnnotations resultType;

            if (node.Type.IsErrorType())
            {
                resultType = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: null);
            }
            else
            {
                var leftType = leftResult.Type;
                var rightType = rightResult.Type;

#if DEBUG
                Debug.Assert((object)leftType == null || AreCloseEnough(leftType.TypeSymbol, node.Type));
                Debug.Assert((object)rightType == null || AreCloseEnough(rightType.TypeSymbol, node.Type));
#endif

                // PROTOTYPE(NullableReferenceTypes): Capture in BindNullCoalescingOperator
                // which side provides type and use that to determine nullability.
                resultType = TypeSymbolWithAnnotations.Create((leftType ?? rightType)?.TypeSymbol, isNullableIfReferenceType: rightType?.IsNullable & leftType?.IsNullable);
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

            var consequence = node.Consequence;
            VisitConditionalOperand(consequenceState, node.Consequence, isByRef, out var consequenceConversion);
            Unsplit();
            consequenceState = this.State;
            var consequenceResult = _result;
            consequence = CreatePlaceholderExpressionIfNecessary(consequence, consequenceResult);

            var alternative = node.Alternative;
            VisitConditionalOperand(alternativeState, alternative, isByRef, out var alternativeConversion);
            Unsplit();
            var alternativeResult = _result;
            alternative = CreatePlaceholderExpressionIfNecessary(alternative, alternativeResult);

            if (IsConstantTrue(node.Condition))
            {
                SetState(consequenceState);
                _result = consequenceResult.Type;
            }
            else if (IsConstantFalse(node.Condition))
            {
                _result = alternativeResult.Type;
            }
            else
            {
                IntersectWith(ref this.State, ref consequenceState);

                TypeSymbol resultType = null;
                if (!node.HasErrors)
                {
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    var bestType = BestTypeInferrer.InferBestTypeForConditionalOperator(consequence, alternative, _conversions, out bool _, ref useSiteDiagnostics);
                    if (bestType is null)
                    {
                        object GetTypeAsDiagnosticArgument(TypeSymbol typeOpt) => typeOpt ?? (object)"<null>";
                        ReportStaticNullCheckingDiagnostics(
                            ErrorCode.WRN_NoBestNullabilityConditionalExpression,
                            node.Syntax,
                            GetTypeAsDiagnosticArgument(consequenceResult.Type?.TypeSymbol),
                            GetTypeAsDiagnosticArgument(alternativeResult.Type?.TypeSymbol));
                    }
                    else
                    {
                        consequenceResult = CheckImplicitConversion(consequence, bestType, consequenceResult, consequenceConversion);
                        alternativeResult = CheckImplicitConversion(alternative, bestType, alternativeResult, alternativeConversion);
                        resultType = bestType;
                    }
                }

                bool? GetIsNullable(Result result) => (object)result.Type == null ? true : result.Type.IsNullable;
                _result = TypeSymbolWithAnnotations.Create(resultType ?? node.Type, GetIsNullable(consequenceResult) | GetIsNullable(alternativeResult));
            }

            return null;
        }

        private void VisitConditionalOperand(LocalState state, BoundExpression operand, bool isByRef, out Conversion implicitConversion)
        {
            SetState(state);
            if (isByRef)
            {
                VisitLvalue(operand);
                implicitConversion = default;
            }
            else
            {
                operand = RemoveImplicitConversion(operand, out implicitConversion);
                VisitRvalue(operand);
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

            if (!node.HasErrors)
            {
                var names = node.ArgumentNamesOpt;
                var refKindsOpt = node.ArgumentRefKindsOpt;
                var argsToParamsOpt = node.ArgsToParamsOpt;
                var arguments = RemoveArgumentConversions(node.Arguments, refKindsOpt);
                ImmutableArray<Result> results = VisitArgumentsEvaluate(arguments, refKindsOpt, argsToParamsOpt, node.Expanded);
                ImmutableArray<BoundExpression> updatedArguments = CreatePlaceholderExpressionsIfNecessary(arguments, results);
                if (method.IsGenericMethod && !HasExplicitTypeArguments(node))
                {
                    method = InferMethod(node, method, updatedArguments);
                }
                var conversions = GetArgumentConversions(updatedArguments, names, refKindsOpt, method, node.Expanded, _binder);
                VisitArgumentsWarn(arguments, conversions, refKindsOpt, method.Parameters, argsToParamsOpt, node.Expanded, results);
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

        private static bool HasExplicitTypeArguments(BoundCall node)
        {
            var syntax = node.Syntax;
            if (syntax.Kind() != SyntaxKind.InvocationExpression)
            {
                return false;
            }
            syntax = ((Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax)syntax).Expression;
            if (syntax.Kind() == SyntaxKind.QualifiedName)
            {
                syntax = ((Microsoft.CodeAnalysis.CSharp.Syntax.QualifiedNameSyntax)syntax).Right;
            }
            return syntax.Kind() == SyntaxKind.GenericName;
        }

        protected override void VisitArguments(ImmutableArray<BoundExpression> arguments, ImmutableArray<RefKind> refKindsOpt, MethodSymbol method)
        {
            // Callers should be using VisitArguments overload below.
            throw ExceptionUtilities.Unreachable;
        }

        private void VisitArguments(BoundExpression node, ImmutableArray<BoundExpression> arguments, ImmutableArray<string> namesOpt, ImmutableArray<RefKind> refKindsOpt, MethodSymbol method, ImmutableArray<int> argsToParamsOpt, bool expanded)
        {
            if (node.HasErrors)
            {
                return;
            }
            arguments = RemoveArgumentConversions(arguments, refKindsOpt);
            ImmutableArray<Result> results = VisitArgumentsEvaluate(arguments, refKindsOpt, argsToParamsOpt, expanded);
            ImmutableArray<BoundExpression> updatedArguments = CreatePlaceholderExpressionsIfNecessary(arguments, results);
            ImmutableArray<Conversion> conversions = (method is null) ? default : GetArgumentConversions(updatedArguments, namesOpt, refKindsOpt, method, expanded, _binder);
            VisitArgumentsWarn(arguments, conversions, refKindsOpt, (method is null) ? default : method.Parameters, argsToParamsOpt, expanded, results);
        }

        private void VisitArguments(BoundExpression node, ImmutableArray<BoundExpression> arguments, ImmutableArray<string> namesOpt, ImmutableArray<RefKind> refKindsOpt, PropertySymbol property, ImmutableArray<int> argsToParamsOpt, bool expanded)
        {
            if (node.HasErrors)
            {
                return;
            }
            arguments = RemoveArgumentConversions(arguments, refKindsOpt);
            ImmutableArray<Result> results = VisitArgumentsEvaluate(arguments, refKindsOpt, argsToParamsOpt, expanded);
            ImmutableArray<BoundExpression> updatedArguments = CreatePlaceholderExpressionsIfNecessary(arguments, results);
            ImmutableArray<Conversion> conversions = GetArgumentConversions(property, property, default, updatedArguments, namesOpt, refKindsOpt, _binder, expanded);
            VisitArgumentsWarn(arguments, conversions, refKindsOpt, property.Parameters, argsToParamsOpt, expanded, results);
        }

        private ImmutableArray<Result> VisitArgumentsEvaluate(
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKindsOpt,
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
            ImmutableArray<Conversion> conversionsOpt,
            ImmutableArray<RefKind> refKindsOpt,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded,
            ImmutableArray<Result> results)
        {
            for (int i = 0; i < arguments.Length; i++)
            {
                RefKind refKind = GetRefKind(refKindsOpt, i);
                var argument = arguments[i];
                var parameter = GetCorrespondingParameter(i, parameters, argsToParamsOpt, ref expanded);
                var result = results[i];
                if (refKind != RefKind.None)
                {
                    TrackNullableStateForAssignment(argument, result.Slot, result.Type, null, parameter?.Type, -1);
                }
                if ((object)parameter != null)
                {
                    var parameterType = parameter.Type.TypeSymbol;
                    TypeSymbolWithAnnotations argumentTypeForMismatch = null;
                    if (conversionsOpt.IsDefault)
                    {
                        if ((object)result.Type != null && IsNullabilityMismatch(parameterType, result.Type.TypeSymbol))
                        {
                            argumentTypeForMismatch = result.Type;
                        }
                    }
                    else
                    {
                        var conversion = conversionsOpt[i];
                        if (conversion.Kind == ConversionKind.NoConversion && (object)result.Type != null)
                        {
                            argumentTypeForMismatch = result.Type;
                        }
                        result = InferResultNullability(conversion, parameterType, result, allowImplicitConversions: true);
                    }
                    if (refKind != RefKind.Out)
                    {
                        WarnOnNullReferenceArgument(argument, result.Type, parameter, expanded);
                    }
                    if ((object)argumentTypeForMismatch != null)
                    {
                        ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInArgument, argument.Syntax, argumentTypeForMismatch.TypeSymbol, parameterType,
                                new FormattedSymbol(parameter, SymbolDisplayFormat.ShortFormat),
                                new FormattedSymbol(parameter.ContainingSymbol, SymbolDisplayFormat.MinimallyQualifiedFormat));
                    }
                }
            }
        }

        private static ImmutableArray<BoundExpression> RemoveArgumentConversions(ImmutableArray<BoundExpression> arguments, ImmutableArray<RefKind> refKindsOpt)
        {
            int n = arguments.Length;
            if (n > 0)
            {
                var builder = ArrayBuilder<BoundExpression>.GetInstance(n);
                bool includedConversion = false;
                for (int i = 0; i < n; i++)
                {
                    RefKind refKind = GetRefKind(refKindsOpt, i);
                    var argument = arguments[i];
                    if (refKind == RefKind.None)
                    {
                        argument = RemoveImplicitConversion(argument, out var conversion);
                        if (argument != arguments[i])
                        {
                            includedConversion = true;
                        }
                    }
                    builder.Add(argument);
                }
                if (includedConversion)
                {
                    arguments = builder.ToImmutable();
                }
                builder.Free();
            }
            return arguments;
        }

        // PROTOTYPE(NullableReferenceTypes): Remove this Binder type and
        // instead change OverloadResolution to not depend on Binder.
        private sealed class BinderWithNullableConversions : Binder
        {
            internal BinderWithNullableConversions(Binder next) : base(next, next.Conversions.WithNullability())
            {
            }
        }

        private static ImmutableArray<Conversion> GetArgumentConversions(ImmutableArray<BoundExpression> arguments, ImmutableArray<string> namesOpt, ImmutableArray<RefKind> refKindsOpt, MethodSymbol method, bool expanded, Binder binder)
        {
            var constructedFrom = method.ConstructedFrom;
            var definition = GetLeastOverriddenMethod(constructedFrom);
            return GetArgumentConversions(constructedFrom, definition, method.TypeArguments, arguments, namesOpt, refKindsOpt, binder, expanded);
        }

        private static ImmutableArray<Conversion> GetArgumentConversions<TMember>(
            TMember member,
            TMember leastOverriddenMember,
            ImmutableArray<TypeSymbolWithAnnotations> typeArguments,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<string> namesOpt,
            ImmutableArray<RefKind> refKindsOpt,
            Binder binder,
            bool expanded)
            where TMember : Symbol
        {
            binder = new BinderWithNullableConversions(binder);
            ArrayBuilder<TypeSymbolWithAnnotations> typeArgumentsBuilder = null;
            if (!typeArguments.IsDefault)
            {
                typeArgumentsBuilder = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance(typeArguments.Length);
                typeArgumentsBuilder.AddRange(typeArguments);
            }
            var analyzedArguments = AnalyzedArguments.GetInstance();
            analyzedArguments.Arguments.AddRange(arguments);
            if (!namesOpt.IsDefault)
            {
                foreach (var name in namesOpt)
                {
                    analyzedArguments.Names.Add(name is null ? null : SyntaxFactory.IdentifierName(name));
                }
            }
            if (!refKindsOpt.IsDefault)
            {
                analyzedArguments.RefKinds.AddRange(refKindsOpt);
            }
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            bool allowRefOmittedArguments = true; // PROTOTYPE(NullableReferenceTypes): TODO
            bool completeResults = true; // PROTOTYPE(NullableReferenceTypes): See comment in OverloadResolution.IsApplicable regarding `completeResults` and avoiding extra work for lambda binding.
            var diagnostics = DiagnosticBag.GetInstance();
            var memberResolutionResult = expanded ?
                OverloadResolution.IsMemberApplicableInExpandedForm(
                    member,
                    leastOverriddenMember,
                    typeArgumentsBuilder,
                    analyzedArguments,
                    allowRefOmittedArguments: allowRefOmittedArguments,
                    completeResults: completeResults,
                    useSiteDiagnostics: ref useSiteDiagnostics,
                    binder: binder) :
            OverloadResolution.IsMemberApplicableInNormalForm(
                    member,
                    leastOverriddenMember,
                    typeArgumentsBuilder,
                    analyzedArguments,
                    isMethodGroupConversion: false, // PROTOTYPE(NullableReferenceTypes): TODO
                    allowRefOmittedArguments: allowRefOmittedArguments,
                    inferWithDynamic: true,
                    completeResults: completeResults,
                    useSiteDiagnostics: ref useSiteDiagnostics,
                    binder: binder);
            diagnostics.Free();
            analyzedArguments.Free();
            typeArgumentsBuilder?.Free();
            return memberResolutionResult.Result.ConversionsOpt;
        }

        private static MethodSymbol GetLeastOverriddenMethod(MethodSymbol method)
        {
            // PROTOTYPE(NullableReferenceTypes): OverloadResolution.IsMemberApplicableInNormalForm
            // and Binder.CoerceArguments expect the least overridden method.
            return method;
        }

        private static ParameterSymbol GetCorrespondingParameter(int argumentOrdinal, ImmutableArray<ParameterSymbol> parameters, ImmutableArray<int> argsToParamsOpt, ref bool expanded)
        {
            if (parameters.IsDefault)
            {
                expanded = false;
                return null;
            }

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

        private static BoundExpression RemoveImplicitConversion(BoundExpression expr, out Conversion conversion)
        {
            if (expr.Kind == BoundKind.Conversion)
            {
                var conv = (BoundConversion)expr;
                conversion = conv.Conversion;
                if (!conv.ExplicitCastInCode && conversion.IsImplicit)
                {
                    expr = conv.Operand;
                    Debug.Assert(expr.Kind != BoundKind.Conversion || ((BoundConversion)expr).ExplicitCastInCode);
                    return expr;
                }
            }
            conversion = default;
            return expr;
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
            if (symbol is null)
            {
                return null;
            }
            var containingType = _result.Type?.TypeSymbol as NamedTypeSymbol;
            if ((object)containingType == null || containingType.IsErrorType())
            {
                return symbol;
            }
            if (symbol.Kind == SymbolKind.Field)
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

            Visit(node.Operand);

            if (IsConditionalState)
            {
                var whenTrue = this.StateWhenTrue;
                var whenFalse = this.StateWhenFalse;

                SetState(whenTrue);
                VisitConversionNoConditionalState(node);
                Debug.Assert(!IsConditionalState);
                whenTrue = this.State;

                SetState(whenFalse);
                VisitConversionNoConditionalState(node);
                Debug.Assert(!IsConditionalState);
                whenFalse = this.State;

                SetConditionalState(whenTrue, whenFalse);
            }
            else
            {
                VisitConversionNoConditionalState(node);
            }

            return null;
        }

        private void VisitConversionNoConditionalState(BoundConversion node)
        {
            Debug.Assert(!IsConditionalState);
            var operand = node.Operand;

            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                switch (node.ConversionKind)
                {
                    case ConversionKind.ExplicitUserDefined:
                    case ConversionKind.ImplicitUserDefined:
                        {
                            var methodOpt = GetUnaryMethodIfAny(node.SymbolOpt);
                            if ((object)methodOpt != null)
                            {
                                WarnOnNullReferenceArgument(operand, _result.Type, methodOpt.Parameters[0], expanded: false);
                            }
                        }
                        break;
                }

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
                    result = InferResultNullability(node.Conversion, node.Type, operandResult, allowImplicitConversions: node.ExplicitCastInCode);
                }
                _result = result;
            }
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
            var tuple = (TupleTypeSymbol)node.Type;
            var elementTypes = arguments.SelectAsArray((a, w) => w.VisitRvalue(a).Type, this);
            var resultType = TupleTypeSymbol.Create(
                locationOpt: null,
                elementTypes: elementTypes,
                elementLocations: default,
                elementNames: tuple.TupleElementNames,
                compilation: compilation,
                shouldCheckConstraints: false,
                errorPositions: default,
                syntax: (CSharpSyntaxNode)node.Syntax);
            _result = TypeSymbolWithAnnotations.Create(resultType);
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

            bool IsNullabilityMismatch(TypeSymbol source, TypeSymbol destination)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                return !_conversions.ClassifyImplicitConversionFromType(source, destination, ref useSiteDiagnostics).Exists &&
                    ConversionsBase.HasIdentityConversion(source, destination);
            }

            var invokeReturnType = invoke.ReturnType;
            var methodReturnType = method.ReturnType;
            if (!ConversionsBase.HasTopLevelNullabilityImplicitConversion(methodReturnType, invokeReturnType) ||
                IsNullabilityMismatch(methodReturnType.TypeSymbol, invokeReturnType.TypeSymbol))
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, syntax,
                    new FormattedSymbol(method, SymbolDisplayFormat.MinimallyQualifiedFormat),
                    delegateType);
            }

            int count = Math.Min(invoke.ParameterCount, method.ParameterCount);

            for (int i = 0; i < count; i++)
            {
                var invokeParameterType = invoke.Parameters[i].Type;
                var methodParameterType = method.Parameters[i].Type;
                bool hasNullabilityConversion = (method.MethodKind == MethodKind.LambdaMethod) ?
                    ConversionsBase.HasTopLevelNullabilityIdentityConversion(invokeParameterType, methodParameterType) :
                    ConversionsBase.HasTopLevelNullabilityImplicitConversion(invokeParameterType, methodParameterType);
                if (!hasNullabilityConversion ||
                    IsNullabilityMismatch(invokeParameterType.TypeSymbol, methodParameterType.TypeSymbol))
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, syntax,
                        new FormattedSymbol(method.Parameters[i], SymbolDisplayFormat.ShortFormat),
                        new FormattedSymbol(method, SymbolDisplayFormat.MinimallyQualifiedFormat),
                        delegateType);
                }
            }
        }

        // PROTOTYPE(NullableReferenceTypes): Remove `allowImplicitConversions`
        // parameter when all implicit conversion callers have been updated.
        private Result InferResultNullability(Conversion conversion, TypeSymbol targetType, Result operand, bool allowImplicitConversions = false)
        {
            Debug.Assert(allowImplicitConversions || !conversion.IsImplicit);

            bool? isNullable = null;

            switch (conversion.Kind)
            {
                case ConversionKind.MethodGroup:
                case ConversionKind.AnonymousFunction:
                case ConversionKind.InterpolatedString:
                    isNullable = false;
                    break;

                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.ImplicitUserDefined:
                    {
                        var methodOpt = GetUnaryMethodIfAny(conversion.Method);
                        if ((object)methodOpt != null)
                        {
                            // PROTOTYPE(NullableReferenceTypes): Update method based on operandType.
                            return Result.Create(methodOpt.ReturnType);
                        }
                    }
                    break;

                case ConversionKind.Unboxing:
                case ConversionKind.ExplicitDynamic:
                case ConversionKind.ImplicitDynamic:
                case ConversionKind.NoConversion:
                case ConversionKind.ImplicitThrow:
                    break;

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
                            Debug.Assert(operandType?.IsReferenceType != true);
                        }
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
                    {
                        var expr = operand.Expression;
                        isNullable = (expr != null && (expr.IsLiteralNull() || expr.IsLiteralDefault())) ? true : operand.Type?.IsNullable;
                    }
                    break;

                case ConversionKind.Deconstruction:
                    // Can reach here, with an error type, when the
                    // Deconstruct method is missing or inaccessible.
                    break;

                case ConversionKind.ExplicitEnumeration:
                    // Can reach here, with an error type.
                    break;

                case ConversionKind.ImplicitTupleLiteral:
                case ConversionKind.ExplicitTupleLiteral:
                case ConversionKind.ImplicitTuple:
                case ConversionKind.ExplicitTuple:
                    {
                        var targetTuple = (TupleTypeSymbol)targetType;
                        var operandTuple = (TupleTypeSymbol)operand.Type.TypeSymbol;
                        int n = operandTuple.TupleElementTypes.Length;
                        var builder = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance(n);
                        var underlyingConversions = conversion.UnderlyingConversions;
                        for (int i = 0; i < n; i++)
                        {
                            // PROTOTYPE(NullableReferenceTypes): Should pass in a BoundExpression for tuple
                            // element rather than a Result. Another reason NullableWalker should be a rewriter.
                            builder.Add(InferResultNullability(underlyingConversions[i], targetTuple.TupleElementTypes[i].TypeSymbol, Result.Create(operandTuple.TupleElementTypes[i]), allowImplicitConversions: true).Type);
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
                var method = AsMemberOfResultType(node.LookupSymbolOpt);
                var methods = node.Methods.SelectAsArray((m, w) => (MethodSymbol)w.AsMemberOfResultType(m), this);
                _result = new BoundMethodGroup(node.Syntax, node.TypeArgumentsOpt, node.Name, methods, method, node.LookupError, node.Flags, node.ReceiverOpt, node.ResultKind, node.HasErrors);
            }

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
            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                _result = GetAdjustedResult(GetDeclaredParameterResult(node.ParameterSymbol));
            }

            return null;
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            Debug.Assert(!IsConditionalState);

            var left = node.Left;
            VisitLvalue(left);
            Result leftResult = _result;

            var right = RemoveImplicitConversion(node.Right, out var rightConversion);
            Result rightResult = VisitRvalue(right);

            // byref assignment is also a potential write
            if (node.RefKind != RefKind.None)
            {
                WriteArgument(right, node.RefKind, method: null);
            }

            rightResult = CheckImplicitConversion(right, leftResult.Type?.TypeSymbol, rightResult, rightConversion);
            TrackNullableStateForAssignment(left, leftResult.Slot, leftResult.Type, right, rightResult.Type, rightResult.Slot);

            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
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

            var right = RemoveImplicitConversion(node.Right, out var rightConversion);
            var rightResult = VisitRvalue(right);

            rightResult = CheckImplicitConversion(right, leftResult.Type?.TypeSymbol, rightResult, rightConversion);

            // PROTOTYPE(NullableReferenceTypes): Assign each of the deconstructed values.
            _result = rightResult;
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
            bool setResult = false;

            if (this.State.Reachable)
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

                Result operandConversionResult;

                if ((object)targetTypeOfOperandConversion != null)
                {
                    // PROTOTYPE(NullableReferenceTypes): Should something special be done for targetTypeOfOperandConversion for lifted case?
                    operandConversionResult = CheckImplicitConversion(node.Operand, targetTypeOfOperandConversion, operandResult, node.OperandConversion);
                }
                else
                {
                    operandConversionResult = operandResult;
                }

                Result incrementResult;
                if ((object)incrementOperator == null)
                {
                    incrementResult = operandConversionResult;
                }
                else
                {
                    WarnOnNullReferenceArgument(node.Operand, operandConversionResult.Type, incrementOperator.Parameters[0], expanded: false);

                    incrementResult = Result.Create(GetTypeOrReturnTypeWithAdjustedNullableAnnotations(incrementOperator));
                }

                incrementResult = CheckImplicitConversion(node, node.Type, incrementResult, node.ResultConversion);

                // PROTOTYPE(NullableReferenceTypes): Check node.Type.IsErrorType() instead?
                if (!node.HasErrors)
                {
                    var op = node.OperatorKind.Operator();
                    _result = (op == UnaryOperatorKind.PrefixIncrement || op == UnaryOperatorKind.PrefixDecrement) ? incrementResult : operandResult;
                    setResult = true;

                    TrackNullableStateForAssignment(node.Operand, operandResult.Slot, operandResult.Type, value: node, valueType: incrementResult.Type, valueSlot: -1);
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
            Result leftResult = _result;

            TypeSymbolWithAnnotations resultType;
            Debug.Assert(!IsConditionalState);

            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                Result leftOnRightResult = GetAdjustedResult(leftResult);

                // PROTOTYPE(NullableReferenceTypes): Update operator based on inferred argument types.
                leftOnRightResult = CheckImplicitConversion(node.Left, node.Operator.LeftType, leftOnRightResult, node.LeftConversion);

                var right = RemoveImplicitConversion(node.Right, out var rightConversion);
                Result rightResult = VisitRvalue(right);

                rightResult = CheckImplicitConversion(right, node.Operator.RightType, rightResult, rightConversion);

                if ((object)node.Operator.ReturnType != null)
                {
                    if (node.Operator.Kind.IsUserDefined() && (object)node.Operator.Method != null && node.Operator.Method.ParameterCount == 2)
                    {
                        WarnOnNullReferenceArgument(node.Left, leftOnRightResult.Type, node.Operator.Method.Parameters[0], expanded: false);
                        WarnOnNullReferenceArgument(node.Right, rightResult.Type, node.Operator.Method.Parameters[1], expanded: false);
                    }

                    resultType = InferResultNullability(node.Operator.Kind, node.Operator.Method, node.Operator.ReturnType, leftOnRightResult.Type, rightResult.Type);
                    resultType = CheckImplicitConversion(node, node.Type, Result.Create(resultType), node.FinalConversion).Type;
                }
                else
                {
                    resultType = null;
                }

                TrackNullableStateForAssignment(node, leftResult.Slot, leftResult.Type, node, resultType, -1);
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

            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                _result = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(node.Indexer);
            }

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

            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
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
            // Should be handled by VisitObjectCreationExpression.
            throw ExceptionUtilities.Unreachable;
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
            if (node.OperatorKind.IsUserDefined())
            {
                var operatorMethod = GetUnaryMethodIfAny(node.MethodOpt);
                if ((object)operatorMethod != null)
                {
                    WarnOnNullReferenceArgument(node.Operand, _result.Type, operatorMethod.Parameters[0], expanded: false);
                }
            }

            _result = InferResultNullability(node);
            return null;
        }

        private TypeSymbolWithAnnotations InferResultNullability(BoundUnaryOperator node)
        {
            if (node.OperatorKind.IsUserDefined())
            {
                // PROTOTYPE(NullableReferenceTypes): Update method based on inferred operand type.
                var operatorMethod = GetUnaryMethodIfAny(node.MethodOpt);
                if ((object)operatorMethod != null)
                {
                    return GetTypeOrReturnTypeWithAdjustedNullableAnnotations(node.MethodOpt);
                }
            }
            return TypeSymbolWithAnnotations.Create(node.Type);
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
                    WarnOnNullReferenceArgument(left, leftType, trueFalseOperator.Parameters[0], expanded: false);
                }

                if ((object)logicalOperator != null)
                {
                    // PROTOTYPE(NullableReferenceTypes): Unwrap implicit conversions and re-calculate.
                    WarnOnNullReferenceArgument(left, leftType, logicalOperator.Parameters[0], expanded: false);
                }

                // PROTOTYPE(NullableReferenceTypes): Unwrap implicit conversions and re-calculate.
                Visit(right);

                Debug.Assert(IsConditionalState ? (this.StateWhenFalse.Reachable || this.StateWhenTrue.Reachable) : this.State.Reachable);
                TypeSymbolWithAnnotations rightType = _result.Type;

                _result = InferResultNullabilityOfBinaryLogicalOperator(node, leftType, rightType);

                if ((object)logicalOperator != null)
                {
                    WarnOnNullReferenceArgument(right, rightType, logicalOperator.Parameters[1], expanded: false);
                }
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
                    throw ExceptionUtilities.UnexpectedValue(node.Kind);
            }
        }

        public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
        {
            var result = base.VisitAwaitExpression(node);

            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                if (!node.Type.IsReferenceType || node.HasErrors || (object)node.GetResult == null)
                {
                    SetResult(node);
                }
                else
                {
                    // PROTOTYPE(NullableReferenceTypes): Update method based on inferred receiver type.
                    _result = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(node.GetResult);
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

            Debug.Assert(!IsConditionalState);
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
            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt, argsToParamsOpt: default, expanded: false);
            Debug.Assert((object)node.Type == null);
            SetResult(node);
            return null;
        }

        public override BoundNode VisitLiteral(BoundLiteral node)
        {
            var constant = node.ConstantValue;
            bool? isNullableIfReference = (constant != null && ((object)node.Type != null ? true : constant.IsNull)) ?
                (bool?)constant.IsNull :
                null;
            var type = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReference);
            _result = Result.Create(node, type);
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
            VisitArgumentsEvaluate(node.Arguments, node.ArgumentRefKindsOpt, argsToParamsOpt: default, expanded: false);

            Debug.Assert(node.Type.IsDynamic());
            Debug.Assert(node.Type.IsReferenceType);
            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                // PROTOTYPE(NullableReferenceTypes): Update applicable members based on inferred argument types.
                bool? isNullable = InferResultNullabilityFromApplicableCandidates(StaticCast<Symbol>.From(node.ApplicableMethods));
                _result = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: isNullable);
            }

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

        // PROTOTYPE(NullableReferenceTypes): Some Visit methods call SetUnknownResultNullability,
        // some set ResultType = null directly. Use the same approach for all.
        private void SetUnknownResultNullability()
        {
            Debug.Assert(!IsConditionalState);
            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                _result = (BoundExpression)null;
            }
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
            Debug.Assert(!IsConditionalState);

            //if (this.State.Reachable) // PROTOTYPE(NullableReferenceTypes): Consider reachability?
            {
                // PROTOTYPE(NullableReferenceTypes): Update applicable members based on inferred argument types.
                bool? isNullable = (node.Type?.IsReferenceType == true) ?
                    InferResultNullabilityFromApplicableCandidates(StaticCast<Symbol>.From(node.ApplicableIndexers)) :
                    null;
                _result = TypeSymbolWithAnnotations.Create(node.Type, isNullableIfReferenceType: isNullable);
            }

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
            SetUnknownResultNullability();
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

            internal static Result Create(BoundExpression expr, TypeSymbolWithAnnotations type)
            {
                return new Result(expr, type, -1);
            }

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
