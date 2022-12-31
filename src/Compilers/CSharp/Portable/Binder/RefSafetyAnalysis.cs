// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class RefSafetyAnalysis : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
    {
        internal static void Analyze(CSharpCompilation compilation, Symbol symbol, BoundNode node, BindingDiagnosticBag diagnostics)
        {
            var visitor = new RefSafetyAnalysis(
                compilation,
                symbol,
                inUnsafeRegion: InUnsafeMethod(symbol),
                useUpdatedEscapeRules: symbol.ContainingModule.UseUpdatedEscapeRules,
                diagnostics);
            try
            {
                visitor.Visit(node);
            }
            catch (CancelledByStackGuardException e)
            {
                e.AddAnError(diagnostics);
            }
        }

        internal static void Analyze(CSharpCompilation compilation, Symbol symbol, ImmutableArray<BoundInitializer> fieldAndPropertyInitializers, BindingDiagnosticBag diagnostics)
        {
            var visitor = new RefSafetyAnalysis(
                compilation,
                symbol,
                inUnsafeRegion: InUnsafeMethod(symbol),
                useUpdatedEscapeRules: symbol.ContainingModule.UseUpdatedEscapeRules,
                diagnostics);
            foreach (var initializer in fieldAndPropertyInitializers)
            {
                try
                {
                    visitor.VisitFieldOrPropertyInitializer(initializer);
                }
                catch (CancelledByStackGuardException e)
                {
                    e.AddAnError(diagnostics);
                }
            }
        }

        private static bool InUnsafeMethod(Symbol symbol)
        {
            if (symbol is SourceMemberMethodSymbol { IsUnsafe: true })
            {
                return true;
            }

            var type = symbol.ContainingType;
            while (type is { })
            {
                var def = type.OriginalDefinition;
                if (def is SourceMemberContainerTypeSymbol { IsUnsafe: true })
                {
                    return true;
                }
                type = def.ContainingType;
            }

            return false;
        }

        private readonly CSharpCompilation _compilation;
        private readonly Symbol _symbol;
        private readonly bool _useUpdatedEscapeRules;
        private readonly BindingDiagnosticBag _diagnostics;
        private bool _inUnsafeRegion;
        private uint _localScopeDepth;
        private Dictionary<LocalSymbol, (uint RefEscapeScope, uint ValEscapeScope)>? _localEscapeScopes;
        private Dictionary<BoundValuePlaceholderBase, uint>? _placeholderScopes;
        private uint _patternInputValEscape;

        private RefSafetyAnalysis(
            CSharpCompilation compilation,
            Symbol symbol,
            bool inUnsafeRegion,
            bool useUpdatedEscapeRules,
            BindingDiagnosticBag diagnostics,
            Dictionary<LocalSymbol, (uint RefEscapeScope, uint ValEscapeScope)>? localEscapeScopes = null)
        {
            _compilation = compilation;
            _symbol = symbol;
            _useUpdatedEscapeRules = useUpdatedEscapeRules;
            _diagnostics = diagnostics;
            _inUnsafeRegion = inUnsafeRegion;
            // _localScopeDepth is incremented at each block in the method, including the
            // outermost. To ensure that locals in the outermost block are considered at
            // the same depth as parameters, _localScopeDepth is initialized to one less.
            _localScopeDepth = CurrentMethodScope - 1;
            _localEscapeScopes = localEscapeScopes;
        }

        private ref struct LocalScope
        {
            private readonly RefSafetyAnalysis _analysis;
            private readonly ImmutableArray<LocalSymbol> _locals;

            public LocalScope(RefSafetyAnalysis analysis, ImmutableArray<LocalSymbol> locals)
            {
                _analysis = analysis;
                _locals = locals;
                _analysis._localScopeDepth++;
                foreach (var local in locals)
                {
                    _analysis.AddLocalScopes(local, refEscapeScope: _analysis._localScopeDepth, valEscapeScope: CallingMethodScope);
                }
            }

            public void Dispose()
            {
                foreach (var local in _locals)
                {
                    _analysis.RemoveLocalScopes(local);
                }
                _analysis._localScopeDepth--;
            }
        }

        private ref struct UnsafeRegion
        {
            private readonly RefSafetyAnalysis _analysis;
            private readonly bool _previousRegion;

            public UnsafeRegion(RefSafetyAnalysis analysis, bool inUnsafeRegion)
            {
                _analysis = analysis;
                _previousRegion = analysis._inUnsafeRegion;
                _analysis._inUnsafeRegion = inUnsafeRegion;
            }

            public void Dispose()
            {
                _analysis._inUnsafeRegion = _previousRegion;
            }
        }

        private ref struct PatternInput
        {
            private readonly RefSafetyAnalysis _analysis;
            private readonly uint _previousInputValEscape;

            public PatternInput(RefSafetyAnalysis analysis, uint patternInputValEscape)
            {
                _analysis = analysis;
                _previousInputValEscape = analysis._patternInputValEscape;
                _analysis._patternInputValEscape = patternInputValEscape;
            }

            public void Dispose()
            {
                _analysis._patternInputValEscape = _previousInputValEscape;
            }
        }

        private ref struct PlaceholderRegion
        {
            private readonly RefSafetyAnalysis _analysis;
            private readonly ArrayBuilder<(BoundValuePlaceholderBase, uint)> _placeholders;

            public PlaceholderRegion(RefSafetyAnalysis analysis, ArrayBuilder<(BoundValuePlaceholderBase, uint)> placeholders)
            {
                _analysis = analysis;
                _placeholders = placeholders;
                foreach (var (placeholder, valEscapeScope) in placeholders)
                {
                    _analysis.AddPlaceholderScope(placeholder, valEscapeScope);
                }
            }

            public void Dispose()
            {
                foreach (var (placeholder, _) in _placeholders)
                {
                    _analysis.RemovePlaceholderScope(placeholder);
                }
                _placeholders.Free();
            }
        }

        private (uint RefEscapeScope, uint ValEscapeScope) GetLocalScopes(LocalSymbol local)
        {
            Debug.Assert(_localEscapeScopes is { });
            return _localEscapeScopes[local];
        }

        private void SetLocalScopes(LocalSymbol local, uint refEscapeScope, uint valEscapeScope)
        {
            Debug.Assert(_localEscapeScopes is { });
            _localEscapeScopes[local] = (refEscapeScope, valEscapeScope);
        }

        private void AddPlaceholderScope(BoundValuePlaceholderBase placeholder, uint valEscapeScope)
        {
            _placeholderScopes ??= new Dictionary<BoundValuePlaceholderBase, uint>();
            // PROTOTYPE: Several placeholder kinds may be included multiple times currently.
            if (placeholder is BoundInterpolatedStringHandlerPlaceholder or BoundInterpolatedStringArgumentPlaceholder or BoundDeconstructValuePlaceholder)
            {
                if (_placeholderScopes.TryGetValue(placeholder, out var value))
                {
                    Debug.Assert(value == valEscapeScope);
                    return;
                }
            }
            _placeholderScopes.Add(placeholder, valEscapeScope);
        }

#pragma warning disable IDE0060
        private void RemovePlaceholderScope(BoundValuePlaceholderBase placeholder)
        {
            Debug.Assert(_placeholderScopes is { });
            // https://github.com/dotnet/roslyn/issues/65961: Currently, analysis may require subsequent calls
            // to GetRefEscape(), etc. for the same expression so we cannot remove placeholders eagerly.
            //_placeholderScopes.Remove(placeholder);
        }
#pragma warning restore IDE0060

        private uint GetPlaceholderScope(BoundValuePlaceholderBase placeholder)
        {
            // PROTOTYPE: BoundInterpolatedStringArgumentPlaceholder is not always included currently. (See also assert in Visit() method below.)
            if (placeholder is BoundInterpolatedStringArgumentPlaceholder)
            {
                return _placeholderScopes?.TryGetValue(placeholder, out uint value) == true
                    ? value
                    : CallingMethodScope;
            }

            Debug.Assert(_placeholderScopes is { });
            return _placeholderScopes[placeholder];
        }

        public override BoundNode? VisitBlock(BoundBlock node)
        {
            using var _1 = new UnsafeRegion(this, _inUnsafeRegion || node.HasUnsafeModifier);
            using var _2 = new LocalScope(this, node.Locals);
            return base.VisitBlock(node);
        }

        public override BoundNode? Visit(BoundNode? node)
        {
#if DEBUG
            if (node is BoundValuePlaceholderBase placeholder
                // CheckValEscapeOfObjectInitializer() does not use BoundObjectOrCollectionValuePlaceholder.
                // CheckInterpolatedStringHandlerConversionEscape() does not use BoundInterpolatedStringHandlerPlaceholder.
                // PROTOTYPE: BoundInterpolatedStringArgumentPlaceholder is temporarily ignored, but should be included for the assert.
                && node is not (BoundObjectOrCollectionValuePlaceholder or BoundInterpolatedStringHandlerPlaceholder or BoundInterpolatedStringArgumentPlaceholder))
            {
                Debug.Assert(_placeholderScopes?.ContainsKey(placeholder) == true);
            }
#endif
            if (node is BoundExpression expr)
            {
                VisitExpression(expr);
                return null;
            }
            return base.Visit(node);
        }

        private void VisitFieldOrPropertyInitializer(BoundInitializer initializer)
        {
            var fieldEqualsValue = (BoundFieldEqualsValue)initializer;
            var field = fieldEqualsValue.Field;
            var value = fieldEqualsValue.Value;

            using var _ = new LocalScope(this, fieldEqualsValue.Locals);

            var result = VisitExpression(value, checkingReceiver: false, isRef: field.RefKind != RefKind.None);
            ReportRefEscapeErrors(result, CallingMethodScope);
        }

        public override BoundNode? VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            var localFunction = node.Symbol;
            // https://github.com/dotnet/roslyn/issues/65353: We should not reuse _localEscapeScopes
            // across nested local functions or lambdas because _localScopeDepth is reset when entering
            // the function or lambda so the scopes across the methods are unrelated.
            var analysis = new RefSafetyAnalysis(_compilation, localFunction, _inUnsafeRegion || localFunction.IsUnsafe, _useUpdatedEscapeRules, _diagnostics, _localEscapeScopes);
            analysis.Visit(node.BlockBody);
            analysis.Visit(node.ExpressionBody);
            return null;
        }

        public override BoundNode? VisitLambda(BoundLambda node)
        {
            var lambda = node.Symbol;
            // https://github.com/dotnet/roslyn/issues/65353: We should not reuse _localEscapeScopes
            // across nested local functions or lambdas because _localScopeDepth is reset when entering
            // the function or lambda so the scopes across the methods are unrelated.
            var analysis = new RefSafetyAnalysis(_compilation, lambda, _inUnsafeRegion, _useUpdatedEscapeRules, _diagnostics, _localEscapeScopes);
            analysis.Visit(node.Body);
            return null;
        }

        public override BoundNode? VisitConstructorMethodBody(BoundConstructorMethodBody node)
        {
            using var _ = new LocalScope(this, node.Locals);
            return base.VisitConstructorMethodBody(node);
        }

        public override BoundNode? VisitForStatement(BoundForStatement node)
        {
            using var outerLocals = new LocalScope(this, node.OuterLocals);
            using var innerLocals = new LocalScope(this, node.InnerLocals);
            return base.VisitForStatement(node);
        }

        public override BoundNode? VisitUsingStatement(BoundUsingStatement node)
        {
            using var _ = new LocalScope(this, node.Locals);

            var placeholders = ArrayBuilder<(BoundValuePlaceholderBase, uint)>.GetInstance();
            if (node.AwaitOpt is { } awaitableInfo)
            {
                uint valEscapeScope = node.ExpressionOpt is { } expr
                    ? GetValEscape(expr).EscapeScope
                    : _localScopeDepth;
                GetAwaitableInstancePlaceholders(placeholders, awaitableInfo, valEscapeScope);
            }

            using var region = new PlaceholderRegion(this, placeholders);
            return base.VisitUsingStatement(node);
        }

        public override BoundNode? VisitUsingLocalDeclarations(BoundUsingLocalDeclarations node)
        {
            var placeholders = ArrayBuilder<(BoundValuePlaceholderBase, uint)>.GetInstance();
            if (node.AwaitOpt is { } awaitableInfo)
            {
                GetAwaitableInstancePlaceholders(placeholders, awaitableInfo, _localScopeDepth);
            }

            using var _ = new PlaceholderRegion(this, placeholders);
            return base.VisitUsingLocalDeclarations(node);
        }

        public override BoundNode? VisitFixedStatement(BoundFixedStatement node)
        {
            using var _ = new LocalScope(this, node.Locals);
            return base.VisitFixedStatement(node);
        }

        public override BoundNode? VisitDoStatement(BoundDoStatement node)
        {
            using var _ = new LocalScope(this, node.Locals);
            return base.VisitDoStatement(node);
        }

        public override BoundNode? VisitWhileStatement(BoundWhileStatement node)
        {
            using var _ = new LocalScope(this, node.Locals);
            return base.VisitWhileStatement(node);
        }

        public override BoundNode? VisitSwitchStatement(BoundSwitchStatement node)
        {
            using var _1 = new LocalScope(this, node.InnerLocals);
            using var _2 = new PatternInput(this, GetValEscape(node.Expression).EscapeScope);
            base.VisitSwitchStatement(node);
            return null;
        }

        private Result VisitSwitchExpression(BoundSwitchExpression node, uint escapeTo, bool isRef)
        {
            using var _ = new PatternInput(this, VisitExpression(node.Expression).EscapeScope);
            var escapeScope = Result.Create(node.Syntax, node, checkingReceiver: false, isRef, CallingMethodScope);
            foreach (var arm in node.SwitchArms)
            {
                escapeScope = Result.Max(escapeScope, VisitSwitchExpressionArm(arm, escapeTo, isRef));
            }
            return escapeScope;
        }

        public override BoundNode? VisitSwitchSection(BoundSwitchSection node)
        {
            using var _ = new LocalScope(this, node.Locals);
            return base.VisitSwitchSection(node);
        }

        private Result VisitSwitchExpressionArm(BoundSwitchExpressionArm node, uint escapeTo, bool isRef)
        {
            using var _ = new LocalScope(this, node.Locals);
            Visit(node.Pattern);
            Visit(node.WhenClause);
            return VisitExpression(node.Value, escapeTo, isRef: isRef);
        }

        // PROTOTYPE: Remove this.
        private Result DefaultVisit(BoundExpression node, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            // Visit sub-expressions.
            base.Visit(node);

            uint escapeScope = isRef
                ? _localScopeDepth
                : CallingMethodScope;
            return Result.Create(node.Syntax, node, checkingReceiver, isRef, escapeScope);
        }

        private Result VisitThisReference(SyntaxNode node, BoundThisReference expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            var thisParam = GetThisParameter((MethodSymbol)_symbol);
            uint escapeScope = thisParam is null
                ? CallingMethodScope
                : (isRef ? GetParameterRefEscape(thisParam) : GetParameterValEscape(thisParam));
            return Result.Create(node, expr, checkingReceiver, isRef, escapeScope);
        }

        private Result VisitParameter(SyntaxNode node, BoundParameter expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            var parameter = expr.ParameterSymbol;
            uint refEscapeScope = GetParameterRefEscape(parameter);
            uint valEscapeScope = GetParameterValEscape(parameter);
            return Result.Create(node, expr, checkingReceiver, isRef, refEscapeScope, valEscapeScope, hasErrors: false);
        }

        private Result VisitLocal(SyntaxNode node, BoundLocal expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            var scopes = GetLocalScopes(expr.LocalSymbol);
            return Result.Create(node, expr, checkingReceiver, isRef, scopes.RefEscapeScope, scopes.ValEscapeScope, hasErrors: false);
        }

        private Result VisitTupleExpression(SyntaxNode node, BoundTupleExpression expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                return CheckValEscape(expr, expr.Arguments, escapeTo, _diagnostics);
            }
            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitRefValueOperator(SyntaxNode node, BoundRefValueOperator expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            // The undocumented __refvalue(tr, T) expression results in an lvalue of type T.
            // for compat reasons it is not ref-returnable (since TypedReference is not val-returnable)
            if (isRef)
            {
                if (escapeTo is CallingMethodScope or ReturnOnlyScope)
                {
                    return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
                }

                // it can, however, ref-escape to any other level (since TypedReference can val-escape to any other level)
                return Result.Create(node, expr, checkingReceiver, isRef, CurrentMethodScope);
            }
            else
            {
                // for compat reasons
                return Result.Create(node, expr, checkingReceiver, isRef, CallingMethodScope);
            }
        }

        private Result VisitValuePlaceholderBase(SyntaxNode syntax, BoundValuePlaceholderBase expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                var escapeScope = GetPlaceholderScope(expr);
                return Result.Create(syntax, expr, checkingReceiver, isRef, escapeScope);
            }
            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitCapturedReceiverPlaceholder(SyntaxNode node, BoundCapturedReceiverPlaceholder expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (isRef)
            {
                // Equivalent to a non-ref local with the underlying receiver as an initializer provided at declaration 
                var escapeScope = expr.LocalScopeDepth;
                if (escapeScope <= escapeTo)
                {
                    return Result.Create(node, expr, checkingReceiver, isRef, escapeScope);
                }
                return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
            }
            else
            {
                // Equivalent to a non-ref local with the underlying receiver as an initializer provided at declaration 
                BoundExpression underlyingReceiver = expr.Receiver;
                return CheckValEscape(underlyingReceiver.Syntax, underlyingReceiver, escapeTo, checkingReceiver, _diagnostics);
            }
        }

        private Result VisitStackAllocArrayCreationBase(SyntaxNode syntax, BoundStackAllocArrayCreationBase expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                uint escapeScope = expr.Type?.IsRefLikeType != true
                    ? CallingMethodScope
                    : CurrentMethodScope;
                return Result.Create(syntax, expr, checkingReceiver, isRef, escapeScope);
            }
            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitUnconvertedConditionalOperator(SyntaxNode node, BoundUnconvertedConditionalOperator expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                return Result.Max(CheckValEscape(expr.Consequence.Syntax, expr.Consequence, escapeTo, checkingReceiver: false, diagnostics: _diagnostics),
                    CheckValEscape(expr.Alternative.Syntax, expr.Alternative, escapeTo, checkingReceiver: false, diagnostics: _diagnostics));
            }
            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitConditionalOperator(SyntaxNode node, BoundConditionalOperator expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            Result whenTrueResult = VisitExpression(expr.Consequence, escapeTo, checkingReceiver: false, isRef);
            Result whenFalseResult = VisitExpression(expr.Alternative, escapeTo, checkingReceiver: false, isRef);

            if (expr.IsRef)
            {
                ValidateRefConditionalOperator(node, whenTrueResult, whenFalseResult);
            }

            return Result.Max(whenTrueResult, whenFalseResult);
        }

        private Result VisitNullCoalescingOperator(BoundNullCoalescingOperator expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                return Result.Max(CheckValEscape(expr.LeftOperand.Syntax, expr.LeftOperand, escapeTo, checkingReceiver, _diagnostics),
                    CheckValEscape(expr.RightOperand.Syntax, expr.RightOperand, escapeTo, checkingReceiver, _diagnostics));
            }
            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitFieldAccess(SyntaxNode node, BoundFieldAccess expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (isRef)
            {
                return CheckFieldRefEscape(node, expr, escapeTo, _diagnostics);
            }
            else
            {
                var fieldSymbol = expr.FieldSymbol;

                if (fieldSymbol.IsStatic || !expr.Type.IsRefLikeType || !fieldSymbol.ContainingType.IsRefLikeType)
                {
                    // PROTOTYPE: Need to visit receiver.

                    // Already an error state.
                    return Result.Create(node, expr, checkingReceiver, isRef, CallingMethodScope);
                }

                // for ref-like fields defer to the receiver.
                return CheckValEscape(node, expr.ReceiverOpt, escapeTo, true, _diagnostics);
            }
        }

        private Result VisitEventAccess(SyntaxNode node, BoundEventAccess expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (isRef)
            {
                // not field-like events are RValues
                if (expr.IsUsableAsField)
                {
                    return CheckFieldLikeEventRefEscape(node, expr, escapeTo, _diagnostics);
                }
            }
            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitImplicitIndexerAccess(SyntaxNode node, BoundImplicitIndexerAccess expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            // Note: the Argument and LengthOrCountAccess use is purely local

            switch (expr.IndexerOrSliceAccess)
            {
                case BoundIndexerAccess indexerAccess:
                    var indexerSymbol = indexerAccess.Indexer;

                    if (isRef && indexerSymbol.RefKind == RefKind.None)
                    {
                        break;
                    }

                    return CheckInvocationEscape(
                        indexerAccess.Syntax,
                        indexerAccess,
                        indexerSymbol,
                        new InvocationArguments(this), // PROTOTYPE: Replace with using statement.
                        expr.Receiver,
                        indexerSymbol.Parameters,
                        indexerAccess.Arguments,
                        indexerAccess.ArgumentRefKindsOpt,
                        indexerAccess.ArgsToParamsOpt,
                        checkingReceiver,
                        escapeTo,
                        _diagnostics,
                        isRef);

                case BoundArrayAccess:
                    return isRef
                        // array elements are readwrite variables
                        ? Result.Create(node, expr, checkingReceiver, isRef, CallingMethodScope)
                        : CreateDefaultValScope(node, expr, checkingReceiver);

                case BoundCall call:
                    var methodSymbol = call.Method;
                    if (isRef && methodSymbol.RefKind == RefKind.None)
                    {
                        break;
                    }

                    return CheckInvocationEscape(
                        call.Syntax,
                        call,
                        methodSymbol,
                        new InvocationArguments(this), // PROTOTYPE: Replace with using statement.
                        expr.Receiver,
                        methodSymbol.Parameters,
                        call.Arguments,
                        call.ArgumentRefKindsOpt,
                        call.ArgsToParamsOpt,
                        checkingReceiver,
                        escapeTo,
                        _diagnostics,
                        isRef);

                default:
                    throw ExceptionUtilities.UnexpectedValue(expr.IndexerOrSliceAccess.Kind);
            }

            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitPropertyAccess(SyntaxNode node, BoundPropertyAccess expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            var propertySymbol = expr.PropertySymbol;

            if (isRef && propertySymbol.RefKind == RefKind.None)
            {
                return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
            }

            // not passing any arguments/parameters
            return CheckInvocationEscape(
                node,
                expr,
                propertySymbol,
                new InvocationArguments(this), // PROTOTYPE: Replace with using statement.
                expr.ReceiverOpt,
                default,
                default,
                default,
                default,
                checkingReceiver,
                escapeTo,
                _diagnostics,
                isRef);
        }

        private Result VisitObjectCreationExpression(SyntaxNode node, BoundObjectCreationExpression expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            using var _ = GetArgumentPlaceholders(receiverOpt: null, expr.Arguments);
            VisitList(expr.Arguments);

            var constructor = expr.Constructor;
            var escapeScope = CheckInvocationEscape(
                node,
                expr,
                constructor,
                new InvocationArguments(this), // PROTOTYPE: Replace with using statement.
                null,
                constructor.Parameters,
                expr.Arguments,
                expr.ArgumentRefKindsOpt,
                expr.ArgsToParamsOpt,
                checkingReceiver,
                escapeTo,
                _diagnostics,
                isRefEscape: false);

            escapeScope = Result.Max(escapeScope, VisitObjectCreationExpressionBase(expr));

            if (!expr.HasErrors)
            {
                CheckInvocationArgMixing(
                    node,
                    constructor,
                    new InvocationArguments(this), // PROTOTYPE: Replace with using statement.
                    receiverOpt: null,
                    constructor.Parameters,
                    expr.Arguments,
                    expr.ArgumentRefKindsOpt,
                    expr.ArgsToParamsOpt,
                    _diagnostics);
            }

            if (isRef)
            {
                return Result.Create(node, expr, checkingReceiver, isRef, _localScopeDepth);
            }

            if (!expr.Type.IsRefLikeType)
            {
                return Result.Create(node, expr, checkingReceiver, isRef, CallingMethodScope);
            }

            return escapeScope;
        }

        private Result VisitWithExpression(SyntaxNode node, BoundWithExpression expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                var escape = (Result)CheckValEscape(node, expr.Receiver, escapeTo, checkingReceiver: false, _diagnostics);

                var initializerExpr = expr.InitializerExpression;
                escape = Result.Max(escape, CheckValEscape(initializerExpr.Syntax, initializerExpr, escapeTo, checkingReceiver: false, diagnostics: _diagnostics));

                return escape;
            }

            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitUnaryOperator(SyntaxNode node, BoundUnaryOperator expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                return CheckValEscape(node, expr.Operand, escapeTo, checkingReceiver: false, diagnostics: _diagnostics);
            }
            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitConversion(SyntaxNode node, BoundConversion expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (isRef)
            {
                if (expr.Conversion == Conversion.ImplicitThrow)
                {
                    return CheckRefEscape(node, expr.Operand, escapeTo, checkingReceiver, _diagnostics);
                }
                return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
            }
            else
            {
                Debug.Assert(expr.ConversionKind != ConversionKind.StackAllocToSpanType, "StackAllocToSpanType unexpected");

                if (expr.ConversionKind == ConversionKind.InterpolatedStringHandler)
                {
                    return CheckInterpolatedStringHandlerConversionEscape(expr.Operand, escapeTo, _diagnostics);
                }

                var result = CheckValEscape(node, expr.Operand, escapeTo, checkingReceiver: false, diagnostics: _diagnostics);
                if (expr.Type?.IsRefLikeType != true)
                {
                    return Result.Create(node, expr, checkingReceiver, isRef: false, CallingMethodScope);
                }
                return result;
            }
        }

        private Result VisitIncrementOperator(SyntaxNode node, BoundIncrementOperator expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                return CheckValEscape(node, expr.Operand, escapeTo, checkingReceiver: false, diagnostics: _diagnostics);
            }
            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                return Result.Max(CheckValEscape(expr.Left.Syntax, expr.Left, escapeTo, checkingReceiver: false, diagnostics: _diagnostics),
                    CheckValEscape(expr.Right.Syntax, expr.Right, escapeTo, checkingReceiver: false, diagnostics: _diagnostics));
            }
            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitBinaryOperator(SyntaxNode node, BoundBinaryOperator expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (isRef)
            {
                return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
            }

            if (expr.OperatorKind == BinaryOperatorKind.Utf8Addition)
            {
                return Result.Create(node, expr, checkingReceiver, isRef, CallingMethodScope);
            }

            var stack = ArrayBuilder<BoundExpression>.GetInstance();
            stack.Push(expr.Right);

            BoundExpression current = expr.Left;
            while (current.Kind == BoundKind.BinaryOperator)
            {
                expr = (BoundBinaryOperator)current;
                stack.Push(expr.Right);
                current = expr.Left;
            }

            Result result = VisitExpression(current, escapeTo, checkingReceiver);
            while (stack.Count > 0)
            {
                current = stack.Pop();
                result = Result.Max(result, VisitExpression(current, escapeTo, checkingReceiver));
            }

            stack.Free();
            return result;
        }

        private Result VisitRangeExpression(SyntaxNode node, BoundRangeExpression expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                var escapeScope = Result.Create(node, expr, checkingReceiver, isRef: false, CallingMethodScope);
                if (expr.LeftOperandOpt is { } left)
                {
                    escapeScope = Result.Max(escapeScope, CheckValEscape(left.Syntax, left, escapeTo, checkingReceiver: false, diagnostics: _diagnostics));
                }
                if (expr.RightOperandOpt is { } right)
                {
                    escapeScope = Result.Max(escapeScope, CheckValEscape(right.Syntax, right, escapeTo, checkingReceiver: false, diagnostics: _diagnostics));
                }
                return escapeScope;
            }

            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitUserDefinedConditionalLogicalOperator(SyntaxNode node, BoundUserDefinedConditionalLogicalOperator expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                return Result.Max(CheckValEscape(expr.Left.Syntax, expr.Left, escapeTo, checkingReceiver: false, diagnostics: _diagnostics),
                    CheckValEscape(expr.Right.Syntax, expr.Right, escapeTo, checkingReceiver: false, diagnostics: _diagnostics));
            }

            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitQueryClause(SyntaxNode node, BoundQueryClause expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                return CheckValEscape(expr.Value.Syntax, expr.Value, escapeTo, checkingReceiver: false, diagnostics: _diagnostics);
            }
            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitRangeVariable(SyntaxNode node, BoundRangeVariable expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                return CheckValEscape(expr.Value.Syntax, expr.Value, escapeTo, checkingReceiver: false, diagnostics: _diagnostics);
            }
            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitObjectInitializerExpression(SyntaxNode node, BoundObjectInitializerExpression expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                return CheckValEscapeOfObjectInitializer(expr, escapeTo, _diagnostics);
            }
            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitInterpolatedStringHandlerPlaceholder(SyntaxNode node, BoundInterpolatedStringHandlerPlaceholder expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                // The handler placeholder cannot escape out of the current expression, as it's a compiler-synthesized
                // location.
                return Result.Create(node, expr, checkingReceiver, isRef, _localScopeDepth);
            }
            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitDisposableValuePlaceholder(SyntaxNode node, BoundDisposableValuePlaceholder expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                // Disposable value placeholder is only ever used to lookup a pattern dispose method
                // then immediately discarded. The actual expression will be generated during lowering 
                return Result.Create(node, expr, checkingReceiver, isRef, _localScopeDepth);
            }
            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitPointerElementAccess(SyntaxNode node, BoundPointerElementAccess expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (isRef)
            {
                // pointer dereferencing are readwrite variables
                return Result.Create(node, expr, checkingReceiver, isRef, CallingMethodScope);
            }
            else
            {
                return CheckValEscape(expr.Expression.Syntax, expr.Expression, escapeTo, checkingReceiver, _diagnostics);
            }
        }

        private Result VisitPointerIndirectionOperator(SyntaxNode node, BoundPointerIndirectionOperator expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (isRef)
            {
                // pointer dereferencing are readwrite variables
                return Result.Create(node, expr, checkingReceiver, isRef, CallingMethodScope);
            }
            else
            {
                return CheckValEscape(expr.Operand.Syntax, expr.Operand, escapeTo, checkingReceiver, _diagnostics);
            }
        }

        private Result VisitArrayAccess(SyntaxNode node, BoundArrayAccess expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (isRef)
            {
                // array elements are readwrite variables
                return Result.Create(node, expr, checkingReceiver, isRef, CallingMethodScope);
            }
            return CreateDefaultValScope(node, expr, checkingReceiver);
        }

        private Result VisitAsOperator(SyntaxNode node, BoundAsOperator expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                return CreateDefaultValScope(node, expr, checkingReceiver);
            }
            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitAwaitExpression(SyntaxNode node, BoundAwaitExpression expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                return CreateDefaultValScope(node, expr, checkingReceiver);
            }
            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result VisitConditionalAccess(SyntaxNode node, BoundConditionalAccess expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            if (!isRef)
            {
                return CreateDefaultValScope(node, expr, checkingReceiver);
            }
            return DefaultVisit(expr, escapeTo, checkingReceiver, isRef);
        }

        private Result CreateDefaultValScope(SyntaxNode node, BoundExpression expr, bool checkingReceiver)
        {
            if (expr.Type?.IsRefLikeType != true)
            {
                return Result.Create(node, expr, checkingReceiver, isRef: false, CallingMethodScope);
            }
            // only possible in error cases (if possible at all)
            return Result.Create(node, expr, checkingReceiver, isRef: false, refEscapeScope: UndefinedScope, _localScopeDepth, hasErrors: true);
        }

        private Result VisitThrowExpression(SyntaxNode node, BoundThrowExpression expr, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            return Result.Create(node, expr, checkingReceiver, isRef, CallingMethodScope);
        }

        public override BoundNode? VisitCatchBlock(BoundCatchBlock node)
        {
            using var _ = new LocalScope(this, node.Locals);
            return base.VisitCatchBlock(node);
        }

        public override BoundNode? VisitLocal(BoundLocal node)
        {
            // _localEscapeScopes may be null for locals in top-level statements.
            Debug.Assert(_localEscapeScopes?.ContainsKey(node.LocalSymbol) == true ||
                (node.LocalSymbol.ContainingSymbol is SynthesizedSimpleProgramEntryPointSymbol entryPoint && _symbol != entryPoint));

            return base.VisitLocal(node);
        }

        private void AddLocalScopes(LocalSymbol local, uint refEscapeScope, uint valEscapeScope)
        {
            // From https://github.com/dotnet/csharplang/blob/main/csharp-11.0/proposals/low-level-struct-improvements.md:
            //
            // | Parameter or Local     | ref-safe-to-escape | safe-to-escape |
            // |------------------------|--------------------|----------------|
            // | Span<int> s            | current method     | calling method |
            // | scoped Span<int> s     | current method     | current method |
            // | ref Span<int> s        | calling method     | calling method |
            // | scoped ref Span<int> s | current method     | calling method |

            var scopedModifier = _useUpdatedEscapeRules ? local.Scope : ScopedKind.None;
            if (scopedModifier != ScopedKind.None)
            {
                refEscapeScope = scopedModifier == ScopedKind.ScopedRef ?
                    _localScopeDepth :
                    CurrentMethodScope;
                valEscapeScope = scopedModifier == ScopedKind.ScopedValue ?
                    _localScopeDepth :
                    CallingMethodScope;
            }

            _localEscapeScopes ??= new Dictionary<LocalSymbol, (uint RefEscapeScope, uint ValEscapeScope)>();
            // PROTOTYPE: Should use .Add(..., ...) but we're currently traversing nodes multiple times.
            _localEscapeScopes[local] = (refEscapeScope, valEscapeScope);
        }

#pragma warning disable IDE0060
        private void RemoveLocalScopes(LocalSymbol local)
        {
            Debug.Assert(_localEscapeScopes is { });
            // https://github.com/dotnet/roslyn/issues/65961: Currently, analysis may require subsequent calls
            // to GetRefEscape(), etc. for the same expression so we cannot remove locals eagerly.
            //_localEscapeScopes.Remove(local);
        }
#pragma warning restore IDE0060

        public override BoundNode? VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            if (node.InitializerOpt is { } initializer)
            {
                var localSymbol = (SourceLocalSymbol)node.LocalSymbol;
                (uint refEscapeScope, uint valEscapeScope) = GetLocalScopes(localSymbol);

                if (_useUpdatedEscapeRules && localSymbol.Scope != ScopedKind.None)
                {
                    // If the local has a scoped modifier, then the lifetime is not inferred from
                    // the initializer. Validate the escape values for the initializer instead.

                    Debug.Assert(localSymbol.RefKind == RefKind.None ||
                        refEscapeScope >= GetRefEscape(initializer).EscapeScope);

                    if (node.DeclaredTypeOpt?.Type.IsRefLikeType == true)
                    {
                        ValidateEscape(initializer, valEscapeScope, isByRef: false, _diagnostics);
                    }
                }
                else
                {
                    // default to the current scope in case we need to handle self-referential error cases.
                    SetLocalScopes(localSymbol, _localScopeDepth, _localScopeDepth);

                    bool isRef = localSymbol.RefKind != RefKind.None;
                    var result = VisitExpression(initializer, isRef: isRef);
                    valEscapeScope = result.ValEscapeScope;
                    if (isRef)
                    {
                        refEscapeScope = result.RefEscapeScope;
                    }

                    SetLocalScopes(localSymbol, refEscapeScope, valEscapeScope);
                }
            }

            return null;
        }

        public override BoundNode? VisitReturnStatement(BoundReturnStatement node)
        {
            if (node.ExpressionOpt is { Type: { } } expr)
            {
                ValidateEscape(expr, ReturnOnlyScope, node.RefKind != RefKind.None, _diagnostics);
            }
            return null;
        }

        public override BoundNode? VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            base.VisitYieldReturnStatement(node);
            if (node.Expression is { Type: { } } expr)
            {
                ValidateEscape(expr, ReturnOnlyScope, isByRef: false, _diagnostics);
            }
            return null;
        }

        private Result VisitAssignmentOperator(BoundAssignmentOperator node, uint escapeTo, bool isRef)
        {
            // PROTOTYPE: We need a single code path where we visit left and right,
            // rather than visiting some here, some in ValidateAssignment.
            if (node.Left.Kind == BoundKind.DiscardExpression)
            {
                VisitExpression(node.Right, isRef: isRef);
            }
            else
            {
                ValidateAssignment(node.Syntax, node.Left, node.Right, node.IsRef, _diagnostics);
            }
            // PROTOTYPE: Previously we had a specific check for !assignment.IsRef (see also
            // "Only ref-assignments can be LValues"). Where is that now?
            return VisitExpression(node.Left, escapeTo, isRef: isRef, node: node.Syntax);
        }

        public override BoundNode? VisitIsPatternExpression(BoundIsPatternExpression node)
        {
            using var _ = new PatternInput(this, GetValEscape(node.Expression).EscapeScope);
            return base.VisitIsPatternExpression(node);
        }

        public override BoundNode? VisitDeclarationPattern(BoundDeclarationPattern node)
        {
            SetPatternLocalScopes(node);

            using var _ = new PatternInput(this, getDeclarationValEscape(node.DeclaredType, _patternInputValEscape));
            return base.VisitDeclarationPattern(node);

            static uint getDeclarationValEscape(BoundTypeExpression typeExpression, uint valEscape)
            {
                return typeExpression.Type.IsRefLikeType ? valEscape : CallingMethodScope;
            }
        }

        public override BoundNode? VisitListPattern(BoundListPattern node)
        {
            SetPatternLocalScopes(node);
            return base.VisitListPattern(node);
        }

        public override BoundNode? VisitRecursivePattern(BoundRecursivePattern node)
        {
            SetPatternLocalScopes(node);
            return base.VisitRecursivePattern(node);
        }

        public override BoundNode? VisitPositionalSubpattern(BoundPositionalSubpattern node)
        {
            using var _ = new PatternInput(this, getPositionalValEscape(node.Symbol, _patternInputValEscape));
            return base.VisitPositionalSubpattern(node);

            static uint getPositionalValEscape(Symbol? symbol, uint valEscape)
            {
                return symbol is null
                    ? valEscape
                    : symbol.GetTypeOrReturnType().IsRefLikeType() ? valEscape : CallingMethodScope;
            }
        }

        public override BoundNode? VisitPropertySubpattern(BoundPropertySubpattern node)
        {
            using var _ = new PatternInput(this, getMemberValEscape(node.Member, _patternInputValEscape));
            return base.VisitPropertySubpattern(node);

            static uint getMemberValEscape(BoundPropertySubpatternMember? member, uint valEscape)
            {
                if (member is null) return valEscape;
                valEscape = getMemberValEscape(member.Receiver, valEscape);
                return member.Type.IsRefLikeType ? valEscape : CallingMethodScope;
            }
        }

        private void SetPatternLocalScopes(BoundObjectPattern pattern)
        {
            if (pattern.Variable is LocalSymbol local)
            {
                SetLocalScopes(local, _localScopeDepth, _patternInputValEscape);
            }
        }

        private PlaceholderRegion GetArgumentPlaceholders(BoundExpression? receiverOpt, ImmutableArray<BoundExpression> arguments)
        {
            var placeholders = ArrayBuilder<(BoundValuePlaceholderBase, uint)>.GetInstance();
            foreach (var arg in arguments)
            {
                if (arg is BoundConversion { ConversionKind: ConversionKind.InterpolatedStringHandler, Operand: BoundInterpolatedString or BoundBinaryOperator } conversion)
                {
                    var interpolationData = conversion.Operand.GetInterpolatedStringHandlerData();
                    GetInterpolatedStringPlaceholders(placeholders, interpolationData, receiverOpt, arguments);
                }
            }
            return new PlaceholderRegion(this, placeholders);
        }

        private ref struct InvocationArguments
        {
            private readonly RefSafetyAnalysis _analysis;
            private readonly PooledDictionary<(BoundExpression Expression, bool IsRef), Result> _argumentResults;

            public InvocationArguments(RefSafetyAnalysis analysis)
            {
                _analysis = analysis;
                _argumentResults = PooledDictionary<(BoundExpression Expression, bool IsRef), Result>.GetInstance();
            }

            // PROTOTYPE: Remove overload.
            internal Result VisitExpressionIfNecessary(BoundExpression expr, uint escapeTo, bool isRef, bool addIfMissing)
            {
                return VisitExpressionIfNecessary(expr, isRef, addIfMissing);
            }

            internal Result VisitExpressionIfNecessary(BoundExpression expr, bool isRef, bool addIfMissing)
            {
                Result? result;
                if (!_argumentResults.TryGetValue((expr, isRef), out result))
                {
                    result = _analysis.VisitExpression(expr, checkingReceiver: false, isRef: isRef);
                    if (addIfMissing)
                    {
                        _argumentResults.Add((expr, isRef), result);
                    }
                }
                return result;
            }

            public void Dispose()
            {
                _argumentResults.Free();
            }
        }

        private static ParameterSymbol? GetThisParameter(MethodSymbol? method)
        {
            return method?.TryGetThisParameter(out var thisParameter) == true
                ? thisParameter
                : null;
        }

        private Result VisitCall(BoundCall node, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            var method = node.Method;
            return VisitInvocation(
                node,
                method,
                method.RefKind,
                GetThisParameter(method),
                node.ReceiverOpt,
                method.Parameters,
                node.Arguments,
                node.ArgumentRefKindsOpt,
                node.ArgsToParamsOpt,
                checkingReceiver,
                escapeTo,
                isRef);
        }

        private Result VisitInvocation(
            BoundExpression node,
            Symbol symbol,
            RefKind refKind, // PROTOTYPE: RefKind and ThisParameter is already handled in GetInvocationArgumentsForEscape(). Share code with that method.
            ParameterSymbol? thisParameter,
            BoundExpression? receiver,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<BoundExpression> argsOpt,
            ImmutableArray<RefKind> argRefKindsOpt,
            ImmutableArray<int> argsToParamsOpt,
            bool checkingReceiver,
            uint escapeTo,
            bool isRef)
        {
            using var _ = GetArgumentPlaceholders(receiver, argsOpt);
            using var args = new InvocationArguments(this);

            Result result;

            if (node.HasAnyErrors)
            {
                base.Visit(node);
                uint escapeScope = isRef
                    ? ReturnOnlyScope
                    : CallingMethodScope;
                return Result.Create(node.Syntax, node, checkingReceiver, isRef, escapeScope);
            }
            else if (isRef && refKind == RefKind.None)
            {
                base.Visit(node);
                return Result.Create(node.Syntax, node, checkingReceiver, isRef, _localScopeDepth);
            }
            else
            {
                result = CheckInvocationEscape(
                    node.Syntax,
                    node,
                    symbol,
                    args,
                    receiver,
                    parameters,
                    argsOpt,
                    argRefKindsOpt,
                    argsToParamsOpt,
                    checkingReceiver,
                    escapeTo,
                    _diagnostics,
                    isRef);

                CheckInvocationArgMixing(
                    node.Syntax,
                    symbol,
                    args,
                    receiver,
                    parameters,
                    argsOpt,
                    argRefKindsOpt,
                    argsToParamsOpt,
                    _diagnostics);

                // PROTOTYPE: Check all callers to CheckInvocationEscape(). All callers should return
                // CallingMethodScope when the return type is not IsRefLikeType. Or better yet,
                // CheckInvocationEscape() should return CallingMethodScope in that case!
                if (!isRef && symbol.GetTypeOrReturnType().Type?.IsRefLikeType != true)
                {
                    return Result.Create(node.Syntax, node, checkingReceiver, isRef, CallingMethodScope);
                }
            }

            // Visit any arguments that weren't visited above.
            // PROTOTYPE: These calls to VisitExpressionIfNecessary() are not correct. We should only visit
            // the sub-expression if we haven't visited it already for any escapeTo value, not uint.MaxValue.
            if (receiver is { } && symbol.RequiresInstanceReceiver())
            {
                // PROTOTYPE: What is the correct 'isRef'?
                // PROTOTYPE: RefKind and ThisParameter is already handled in GetInvocationArgumentsForEscape(). Share code with that method.
                bool receiverIsRef = thisParameter is { } && thisParameter.RefKind != RefKind.None;
                args.VisitExpressionIfNecessary(receiver, escapeTo: uint.MaxValue, receiverIsRef, addIfMissing: false);
            }

            if (!argsOpt.IsDefault)
            {
                for (int i = 0; i < argsOpt.Length; i++)
                {
                    var arg = argsOpt[i];
                    // PROTOTYPE: What is the correct 'isRef'? Need to include implicit 'in' from parameter.
                    bool argIsRef = argRefKindsOpt.IsDefault ? false : argRefKindsOpt[i] != RefKind.None;
                    args.VisitExpressionIfNecessary(arg, escapeTo: uint.MaxValue, argIsRef, addIfMissing: false);
                }
            }

            return result;
        }

        private void GetInterpolatedStringPlaceholders(
            ArrayBuilder<(BoundValuePlaceholderBase, uint)> placeholders,
            in InterpolatedStringHandlerData interpolationData,
            BoundExpression? receiver,
            ImmutableArray<BoundExpression> arguments)
        {
            placeholders.Add((interpolationData.ReceiverPlaceholder, _localScopeDepth));

            foreach (var placeholder in interpolationData.ArgumentPlaceholders)
            {
                uint valEscapeScope;
                int argIndex = placeholder.ArgumentIndex;
                switch (argIndex)
                {
                    case BoundInterpolatedStringArgumentPlaceholder.InstanceParameter:
                        Debug.Assert(receiver != null);
                        valEscapeScope = (receiver.GetRefKind().IsWritableReference() ? GetRefEscape(receiver) : GetValEscape(receiver)).EscapeScope;
                        break;
                    case BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter:
                        // PROTOTYPE: What does this parameter represent, and what is the correct escape scope?
                        valEscapeScope = CallingMethodScope;
                        break;
                    case BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter:
                        continue;
                    case >= 0:
                        valEscapeScope = GetValEscape(arguments[argIndex]).EscapeScope;
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(placeholder.ArgumentIndex);
                }
                placeholders.Add((placeholder, valEscapeScope));
            }
        }

        // PROTOTYPE: Remove or replace all the "public override BoundNode? Visit*()" methods for
        // expressions since all BoundExpression cases should go through VisitExpression() instead.

        public override BoundNode? VisitDynamicObjectCreationExpression(BoundDynamicObjectCreationExpression node)
        {
            using var _ = GetArgumentPlaceholders(receiverOpt: null, node.Arguments);
            VisitList(node.Arguments);

            // PROTOTYPE: Test.
            VisitObjectCreationExpressionBase(node);
            return null;
        }

        public override BoundNode? VisitNoPiaObjectCreationExpression(BoundNoPiaObjectCreationExpression node)
        {
            // PROTOTYPE: Test.
            VisitObjectCreationExpressionBase(node);
            return null;
        }

        public override BoundNode? VisitNewT(BoundNewT node)
        {
            VisitObjectCreationExpressionBase(node);
            return null;
        }

        public override BoundNode? VisitWithExpression(BoundWithExpression node)
        {
            Visit(node.Receiver);
            VisitObjectCreationInitializer(node.InitializerExpression);
            return null;
        }

        private Result VisitObjectCreationExpressionBase(BoundObjectCreationExpressionBase node)
        {
            if (node.InitializerExpressionOpt is { } initializer)
            {
                return VisitObjectCreationInitializer(initializer);
            }
            return Result.Create(node.Syntax, node, checkingReceiver: false, isRef: false, CallingMethodScope);
        }

        private Result VisitObjectCreationInitializer(BoundObjectInitializerExpressionBase node)
        {
            switch (node)
            {
                case BoundObjectInitializerExpression objectInitializer:
                    return CheckValEscapeOfObjectInitializer(objectInitializer, escapeTo: UndefinedScope, _diagnostics);

                case BoundCollectionInitializerExpression collectionInitializer:
                    foreach (var initializer in collectionInitializer.Initializers)
                    {
                        switch (initializer)
                        {
                            case BoundCollectionElementInitializer:
                                // PROTOTYPE: Check Add() method.
                                goto default;
                            default:
                                // PROTOTYPE: Include initializer escape scope in result.
                                Visit(initializer);
                                break;
                        }
                    }
                    break;
            }

            // PROTOTYPE: Should include results above.
            return Result.Create(node.Syntax, node, checkingReceiver: false, isRef: false, CallingMethodScope);
        }

        private Result VisitIndexerAccess(BoundIndexerAccess node, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            var indexer = node.Indexer;
            return VisitInvocation(
                node,
                indexer,
                indexer.RefKind,
                GetThisParameter(indexer.GetMethod),
                node.ReceiverOpt,
                indexer.Parameters,
                node.Arguments,
                node.ArgumentRefKindsOpt,
                node.ArgsToParamsOpt,
                checkingReceiver,
                escapeTo,
                isRef);
        }

        private Result VisitFunctionPointerInvocation(BoundFunctionPointerInvocation node, uint escapeTo, bool checkingReceiver, bool isRef)
        {
            var method = node.FunctionPointer.Signature;
            return VisitInvocation(
                node,
                method,
                method.RefKind,
                thisParameter: null,
                receiver: null,
                method.Parameters,
                node.Arguments,
                node.ArgumentRefKindsOpt,
                argsToParamsOpt: default,
                checkingReceiver,
                escapeTo,
                isRef);
        }

        public override BoundNode? VisitAwaitExpression(BoundAwaitExpression node)
        {
            var placeholders = ArrayBuilder<(BoundValuePlaceholderBase, uint)>.GetInstance();
            GetAwaitableInstancePlaceholders(placeholders, node.AwaitableInfo, GetValEscape(node.Expression).EscapeScope);
            using var _ = new PlaceholderRegion(this, placeholders);
            base.VisitAwaitExpression(node);
            return null;
        }

        private void GetAwaitableInstancePlaceholders(ArrayBuilder<(BoundValuePlaceholderBase, uint)> placeholders, BoundAwaitableInfo awaitableInfo, uint valEscapeScope)
        {
            if (awaitableInfo.AwaitableInstancePlaceholder is { } placeholder)
            {
                placeholders.Add((placeholder, valEscapeScope));
            }
        }

        // Based on NullableWalker.VisitDeconstructionAssignmentOperator().
        public override BoundNode? VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            base.VisitDeconstructionAssignmentOperator(node);

            var left = node.Left;
            var right = node.Right;
            var variables = GetDeconstructionAssignmentVariables(left);
            VisitDeconstructionArguments(variables, right.Syntax, right.Conversion, right.Operand);
            variables.FreeAll(v => v.NestedVariables);
            return null;
        }

        private void VisitDeconstructionArguments(ArrayBuilder<DeconstructionVariable> variables, SyntaxNode syntax, Conversion conversion, BoundExpression right)
        {
            Debug.Assert(conversion.Kind == ConversionKind.Deconstruction);

            // We only need to visit the right side when deconstruction uses a Deconstruct() method call
            // (when !DeconstructionInfo.IsDefault), not when the right side is a tuple, because ref structs
            // cannot be used as tuple type arguments.
            if (conversion.DeconstructionInfo.IsDefault)
            {
                return;
            }

            var invocation = conversion.DeconstructionInfo.Invocation as BoundCall;
            if (invocation is null)
            {
                return;
            }

            var deconstructMethod = invocation.Method;
            if (deconstructMethod is null)
            {
                return;
            }

            var placeholders = ArrayBuilder<(BoundValuePlaceholderBase, uint)>.GetInstance();
            placeholders.Add((conversion.DeconstructionInfo.InputPlaceholder, GetValEscape(right).EscapeScope));

            var parameters = deconstructMethod.Parameters;
            int n = variables.Count;
            int offset = invocation.InvokedAsExtensionMethod ? 1 : 0;
            Debug.Assert(parameters.Length - offset == n);

            for (int i = 0; i < n; i++)
            {
                var variable = variables[i];
                var nestedVariables = variable.NestedVariables;
                var arg = (BoundDeconstructValuePlaceholder)invocation.Arguments[i + offset];
                uint valEscape = nestedVariables is null
                    ? GetValEscape(variable.Expression).EscapeScope
                    : _localScopeDepth;
                placeholders.Add((arg, valEscape));
            }

            using var _ = new PlaceholderRegion(this, placeholders);

            CheckInvocationArgMixing(
                syntax,
                deconstructMethod,
                new InvocationArguments(this), // PROTOTYPE: Replace with using statement.
                invocation.ReceiverOpt,
                parameters,
                invocation.Arguments,
                invocation.ArgumentRefKindsOpt,
                invocation.ArgsToParamsOpt,
                _diagnostics);

            for (int i = 0; i < n; i++)
            {
                var variable = variables[i];
                var nestedVariables = variable.NestedVariables;
                if (nestedVariables != null)
                {
                    var (placeholder, placeholderConversion) = conversion.DeconstructConversionInfo[i];
                    var underlyingConversion = BoundNode.GetConversion(placeholderConversion, placeholder);
                    VisitDeconstructionArguments(nestedVariables, syntax, underlyingConversion, right: invocation.Arguments[i + offset]);
                }
            }
        }

        private readonly struct DeconstructionVariable
        {
            internal readonly BoundExpression Expression;
            internal readonly uint ValEscape;
            internal readonly ArrayBuilder<DeconstructionVariable>? NestedVariables;

            internal DeconstructionVariable(BoundExpression expression, uint valEscape, ArrayBuilder<DeconstructionVariable>? nestedVariables)
            {
                Expression = expression;
                ValEscape = valEscape;
                NestedVariables = nestedVariables;
            }
        }

        private ArrayBuilder<DeconstructionVariable> GetDeconstructionAssignmentVariables(BoundTupleExpression tuple)
        {
            var arguments = tuple.Arguments;
            var builder = ArrayBuilder<DeconstructionVariable>.GetInstance(arguments.Length);
            foreach (var arg in arguments)
            {
                builder.Add(getDeconstructionAssignmentVariable(arg));
            }
            return builder;

            DeconstructionVariable getDeconstructionAssignmentVariable(BoundExpression expr)
            {
                return expr is BoundTupleExpression tuple
                    ? new DeconstructionVariable(expr, valEscape: uint.MaxValue, GetDeconstructionAssignmentVariables(tuple))
                    : new DeconstructionVariable(expr, GetValEscape(expr).EscapeScope, null);
            }
        }

        private static ImmutableArray<BoundExpression> GetDeconstructionRightParts(BoundExpression expr)
        {
            switch (expr)
            {
                case BoundTupleExpression tuple:
                    return tuple.Arguments;
                case BoundConversion conv:
                    switch (conv.ConversionKind)
                    {
                        case ConversionKind.Identity:
                        case ConversionKind.ImplicitTupleLiteral:
                            return GetDeconstructionRightParts(conv.Operand);
                    }
                    break;
            }

            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode? VisitForEachStatement(BoundForEachStatement node)
        {
            uint collectionEscape = GetValEscape(node.Expression).EscapeScope;
            using var _ = new LocalScope(this, ImmutableArray<LocalSymbol>.Empty);

            foreach (var local in node.IterationVariables)
            {
                AddLocalScopes(local, refEscapeScope: local.RefKind == RefKind.None ? _localScopeDepth : collectionEscape, valEscapeScope: collectionEscape);
            }

            var placeholders = ArrayBuilder<(BoundValuePlaceholderBase, uint)>.GetInstance();
            if (node.DeconstructionOpt?.TargetPlaceholder is { } targetPlaceholder)
            {
                placeholders.Add((targetPlaceholder, collectionEscape));
            }
            if (node.AwaitOpt is { } awaitableInfo)
            {
                GetAwaitableInstancePlaceholders(placeholders, awaitableInfo, collectionEscape);
            }

            using var region = new PlaceholderRegion(this, placeholders);
            base.VisitForEachStatement(node);

            foreach (var local in node.IterationVariables)
            {
                RemoveLocalScopes(local);
            }

            return null;
        }

        private bool ReportRefEscapeErrors(Result result, uint escapeTo)
        {
            uint escapeScope = result.EscapeScope;
            if (escapeScope <= escapeTo)
            {
                return true;
            }

            var inUnsafeRegion = _inUnsafeRegion;

            while (result is ParameterResult parameterResult)
            {
                if (!inUnsafeRegion)
                {
                    ReportInvocationEscapeError(result.Syntax!, parameterResult.ContainingSymbol, parameterResult.Parameter, parameterResult.CheckingReceiver, _diagnostics);
                }
                result = parameterResult.ArgumentResult;
            }

            var expressionResult = (ExpressionResult)result;
            var expr = expressionResult.Expression;

            if (expressionResult.IsOriginallyRef)
            {
                bool checkingReceiver = result.CheckingReceiver;
                switch (expr.Kind)
                {
                    case BoundKind.Local:
                        {
                            var localSymbol = ((BoundLocal)expr).LocalSymbol;
                            if (escapeTo is CallingMethodScope or ReturnOnlyScope)
                            {
                                if (localSymbol.RefKind == RefKind.None)
                                {
                                    if (checkingReceiver)
                                    {
                                        Error(_diagnostics, inUnsafeRegion ? ErrorCode.WRN_RefReturnLocal2 : ErrorCode.ERR_RefReturnLocal2, expr.Syntax, localSymbol);
                                    }
                                    else
                                    {
                                        Error(_diagnostics, inUnsafeRegion ? ErrorCode.WRN_RefReturnLocal : ErrorCode.ERR_RefReturnLocal, result.Syntax, localSymbol);
                                    }
                                }
                                else if (checkingReceiver)
                                {
                                    Error(_diagnostics, inUnsafeRegion ? ErrorCode.WRN_RefReturnNonreturnableLocal2 : ErrorCode.ERR_RefReturnNonreturnableLocal2, expr.Syntax, localSymbol);
                                }
                                else
                                {
                                    Error(_diagnostics, inUnsafeRegion ? ErrorCode.WRN_RefReturnNonreturnableLocal : ErrorCode.ERR_RefReturnNonreturnableLocal, result.Syntax, localSymbol);
                                }
                            }
                            else
                            {
                                Error(_diagnostics, inUnsafeRegion ? ErrorCode.WRN_EscapeVariable : ErrorCode.ERR_EscapeVariable, result.Syntax, localSymbol);
                            }
                        }
                        break;

                    case BoundKind.ThisReference:
                        Error(_diagnostics, inUnsafeRegion ? ErrorCode.WRN_RefReturnStructThis : ErrorCode.ERR_RefReturnStructThis, result.Syntax);
                        break;

                    case BoundKind.Parameter:
                        {
                            var parameterSymbol = ((BoundParameter)expr).ParameterSymbol;
                            bool isRefScoped = parameterSymbol.EffectiveScope == ScopedKind.ScopedRef;
                            Debug.Assert(parameterSymbol.RefKind == RefKind.None || isRefScoped || escapeScope == ReturnOnlyScope);
#pragma warning disable format
                            var (errorCode, syntax) = (checkingReceiver, isRefScoped, inUnsafeRegion, escapeScope) switch
                            {
                                (checkingReceiver: true,  isRefScoped: true,  inUnsafeRegion: false, _)                      => (ErrorCode.ERR_RefReturnScopedParameter2, expr.Syntax),
                                (checkingReceiver: true,  isRefScoped: true,  inUnsafeRegion: true,  _)                      => (ErrorCode.WRN_RefReturnScopedParameter2, expr.Syntax),
                                (checkingReceiver: true,  isRefScoped: false, inUnsafeRegion: false, ReturnOnlyScope) => (ErrorCode.ERR_RefReturnOnlyParameter2,   expr.Syntax),
                                (checkingReceiver: true,  isRefScoped: false, inUnsafeRegion: true,  ReturnOnlyScope) => (ErrorCode.WRN_RefReturnOnlyParameter2,   expr.Syntax),
                                (checkingReceiver: true,  isRefScoped: false, inUnsafeRegion: false, _)                      => (ErrorCode.ERR_RefReturnParameter2,       expr.Syntax),
                                (checkingReceiver: true,  isRefScoped: false, inUnsafeRegion: true,  _)                      => (ErrorCode.WRN_RefReturnParameter2,       expr.Syntax),
                                (checkingReceiver: false, isRefScoped: true,  inUnsafeRegion: false, _)                      => (ErrorCode.ERR_RefReturnScopedParameter,  result.Syntax),
                                (checkingReceiver: false, isRefScoped: true,  inUnsafeRegion: true,  _)                      => (ErrorCode.WRN_RefReturnScopedParameter,  result.Syntax),
                                (checkingReceiver: false, isRefScoped: false, inUnsafeRegion: false, ReturnOnlyScope) => (ErrorCode.ERR_RefReturnOnlyParameter,    result.Syntax),
                                (checkingReceiver: false, isRefScoped: false, inUnsafeRegion: true,  ReturnOnlyScope) => (ErrorCode.WRN_RefReturnOnlyParameter,    result.Syntax),
                                (checkingReceiver: false, isRefScoped: false, inUnsafeRegion: false, _)                      => (ErrorCode.ERR_RefReturnParameter,        result.Syntax),
                                (checkingReceiver: false, isRefScoped: false, inUnsafeRegion: true,  _)                      => (ErrorCode.WRN_RefReturnParameter,        result.Syntax)
                            };
#pragma warning restore format
                            Error(_diagnostics, errorCode, syntax, parameterSymbol.Name);
                        }
                        break;

                    default:
                        // PROTOTYPE: Re-enable assert if useful.
                        //Debug.Assert(expr is BoundDiscardExpression
                        //    or BoundLiteral { ConstantValueOpt: { } }
                        //    or BoundConversion
                        //    or BoundObjectCreationExpression
                        //    or BoundStackAllocArrayCreation
                        //    or BoundConvertedStackAllocExpression
                        //    or BoundRefValueOperator
                        //    or BoundDynamicMemberAccess
                        //    or BoundDynamicIndexerAccess);
                        // At this point we should have covered all the possible cases for anything that is not a strict RValue.
                        if (escapeTo is CallingMethodScope or ReturnOnlyScope)
                        {
                            Error(_diagnostics, ErrorCode.ERR_RefReturnLvalueExpected, expr.Syntax);
                        }
                        else
                        {
                            Error(_diagnostics, ErrorCode.ERR_EscapeOther, expr.Syntax);
                        }
                        break;
                }
            }
            else if (!result.HasErrors)
            {
                switch (expr)
                {
                    case BoundLocal local:
                        Error(_diagnostics, inUnsafeRegion ? ErrorCode.WRN_EscapeVariable : ErrorCode.ERR_EscapeVariable, result.Syntax, local.LocalSymbol);
                        break;

                    case BoundParameter parameter:
                        Error(_diagnostics, inUnsafeRegion ? ErrorCode.WRN_EscapeVariable : ErrorCode.ERR_EscapeVariable, result.Syntax, parameter.ParameterSymbol);
                        break;

                    case BoundThisReference:
                        Error(_diagnostics, inUnsafeRegion ? ErrorCode.WRN_EscapeVariable : ErrorCode.ERR_EscapeVariable, result.Syntax, ((MethodSymbol)_symbol).ThisParameter);
                        break;

                    case BoundValuePlaceholderBase:
                        Error(_diagnostics, inUnsafeRegion ? ErrorCode.WRN_EscapeVariable : ErrorCode.ERR_EscapeVariable, result.Syntax, expr.Syntax);
                        break;

                    case BoundStackAllocArrayCreationBase:
                        Error(_diagnostics, inUnsafeRegion ? ErrorCode.WRN_EscapeStackAlloc : ErrorCode.ERR_EscapeStackAlloc, result.Syntax, expr.Type!);
                        break;

                    default:
                        Debug.Assert(false);
                        break;
                }
            }

            return inUnsafeRegion;
        }

        private static void Error(BindingDiagnosticBag diagnostics, ErrorCode code, SyntaxNodeOrToken syntax, params object[] args)
        {
            var location = syntax.GetLocation();
            RoslynDebug.Assert(location is object);
            Error(diagnostics, code, location, args);
        }

        private static void Error(BindingDiagnosticBag diagnostics, ErrorCode code, Location location, params object[] args)
        {
            diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(code, args), location));
        }
    }
}
