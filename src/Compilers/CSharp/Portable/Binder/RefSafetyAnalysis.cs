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
            _localScopeDepth = Binder.CurrentMethodScope - 1;
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
                    _analysis.AddLocalScopes(local, refEscapeScope: _analysis._localScopeDepth, valEscapeScope: Binder.CallingMethodScope);
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
            private readonly ArrayBuilder<BoundValuePlaceholderBase> _placeholders;

            public PlaceholderRegion(RefSafetyAnalysis analysis, ArrayBuilder<(BoundValuePlaceholderBase, uint)> placeholders)
            {
                _analysis = analysis;
                _placeholders = ArrayBuilder<BoundValuePlaceholderBase>.GetInstance(placeholders.Count);
                foreach (var (placeholder, _) in placeholders)
                {
                    _placeholders.Add(placeholder);
                }
                _analysis.AddPlaceholderScopes(placeholders);
            }

            public void Dispose()
            {
                _analysis.RemovePlaceholderScopes(_placeholders);
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

        private void AddPlaceholderScopes(ArrayBuilder<(BoundValuePlaceholderBase, uint)> placeholders)
        {
            _placeholderScopes ??= new Dictionary<BoundValuePlaceholderBase, uint>();
            foreach (var (placeholder, valEscapeScope) in placeholders)
            {
                _placeholderScopes.Add(placeholder, valEscapeScope);
            }
        }

#pragma warning disable IDE0060
        private void RemovePlaceholderScopes(ArrayBuilder<BoundValuePlaceholderBase> placeholders)
        {
            Debug.Assert(_placeholderScopes is { });
            // https://github.com/dotnet/roslyn/issues/65961: Currently, analysis may require subsequent calls
            // to GetRefEscape(), etc. for the same expression so we cannot remove placeholders eagerly.
            //foreach (var placeholder in placeholders)
            //{
            //    _placeholderScopes.Remove(placeholder);
            //}
        }
#pragma warning restore IDE0060

        private uint GetPlaceholderScope(BoundValuePlaceholderBase placeholder)
        {
            Debug.Assert(_placeholderScopes is { });
            return _placeholderScopes[placeholder];
        }

        public override BoundNode? VisitBlock(BoundBlock node)
        {
            var inUnsafeRegion = node.InUnsafeRegion;
            using var region = new UnsafeRegion(this, inUnsafeRegion.HasValue() ? inUnsafeRegion.Value() : _inUnsafeRegion);
            using var _ = new LocalScope(this, node.Locals);
            return base.VisitBlock(node);
        }

        public override BoundNode? Visit(BoundNode? node)
        {
#if DEBUG
            if (node is BoundValuePlaceholderBase placeholder
                && node is not (BoundObjectOrCollectionValuePlaceholder or BoundInterpolatedStringHandlerPlaceholder))
            {
                Debug.Assert(_placeholderScopes?.ContainsKey(placeholder) == true);
            }
#endif
            return base.Visit(node);
        }

        private void VisitFieldOrPropertyInitializer(BoundInitializer initializer)
        {
            var fieldEqualsValue = (BoundFieldEqualsValue)initializer;

            using var _ = new LocalScope(this, fieldEqualsValue.Locals);

            base.Visit(fieldEqualsValue.Value);

            var field = fieldEqualsValue.Field;
            bool isByRef = field.RefKind != RefKind.None;
            if (isByRef || field.Type.IsRefLikeType)
            {
                ValidateEscape(fieldEqualsValue.Value, Binder.CallingMethodScope, isByRef: isByRef, _diagnostics);
            }
        }

        public override BoundNode? VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            var localFunction = node.Symbol;
            // https://github.com/dotnet/roslyn/issues/65353: We should not reuse _localEscapeScopes
            // across nested local functions or lambdas because _localScopeDepth is reset when entering
            // the function or lambda so the scopes across the methods are unrelated.
            var analysis = new RefSafetyAnalysis(_compilation, localFunction, localFunction.IsUnsafe, _useUpdatedEscapeRules, _diagnostics, _localEscapeScopes);
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
                    ? GetValEscape(expr, _localScopeDepth)
                    : _localScopeDepth;
                GetAwaitableInstancePlaceholders(placeholders, awaitableInfo, valEscapeScope);
            }

            using var region = new PlaceholderRegion(this, placeholders);
            placeholders.Free();

            return base.VisitUsingStatement(node);
        }

        public override BoundNode? VisitUsingLocalDeclarations(BoundUsingLocalDeclarations node)
        {
            var placeholders = ArrayBuilder<(BoundValuePlaceholderBase, uint)>.GetInstance();
            if (node.AwaitOpt is { } awaitableInfo)
            {
                GetAwaitableInstancePlaceholders(placeholders, awaitableInfo, _localScopeDepth);
            }

            using var region = new PlaceholderRegion(this, placeholders);
            placeholders.Free();

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
            using var _ = new LocalScope(this, node.InnerLocals);
            using var patternInput = new PatternInput(this, GetValEscape(node.Expression, _localScopeDepth));
            base.VisitSwitchStatement(node);
            return null;
        }

        public override BoundNode? VisitConvertedSwitchExpression(BoundConvertedSwitchExpression node)
        {
            using var patternInput = new PatternInput(this, GetValEscape(node.Expression, _localScopeDepth));
            base.VisitConvertedSwitchExpression(node);
            return null;
        }

        public override BoundNode? VisitSwitchSection(BoundSwitchSection node)
        {
            using var _ = new LocalScope(this, node.Locals);
            return base.VisitSwitchSection(node);
        }

        public override BoundNode? VisitSwitchExpressionArm(BoundSwitchExpressionArm node)
        {
            using var _ = new LocalScope(this, node.Locals);
            return base.VisitSwitchExpressionArm(node);
        }

        public override BoundNode? VisitCatchBlock(BoundCatchBlock node)
        {
            using var _ = new LocalScope(this, node.Locals);
            return base.VisitCatchBlock(node);
        }

        public override BoundNode? VisitLocal(BoundLocal node)
        {
            Debug.Assert(_localEscapeScopes?.ContainsKey(node.LocalSymbol) == true ||
                (node.LocalSymbol.ContainingSymbol is SynthesizedSimpleProgramEntryPointSymbol entryPoint && _symbol != entryPoint));

            return base.VisitLocal(node);
        }

        private void AddLocalScopes(LocalSymbol local, uint refEscapeScope, uint valEscapeScope)
        {
            var scope = _useUpdatedEscapeRules ? local.Scope : DeclarationScope.Unscoped;
            if (scope != DeclarationScope.Unscoped)
            {
                // From https://github.com/dotnet/csharplang/blob/main/csharp-11.0/proposals/low-level-struct-improvements.md:
                //
                // | Parameter or Local     | ref-safe-to-escape | safe-to-escape |
                // |------------------------|--------------------|----------------|
                // | Span<int> s            | current method     | calling method |
                // | scoped Span<int> s     | current method     | current method |
                // | ref Span<int> s        | calling method     | calling method |
                // | scoped ref Span<int> s | current method     | calling method |

                refEscapeScope = scope == DeclarationScope.RefScoped ?
                    _localScopeDepth :
                    Binder.CurrentMethodScope;
                valEscapeScope = scope == DeclarationScope.ValueScoped ?
                    _localScopeDepth :
                    Binder.CallingMethodScope;
            }

            _localEscapeScopes ??= new Dictionary<LocalSymbol, (uint RefEscapeScope, uint ValEscapeScope)>();
            _localEscapeScopes.Add(local, (refEscapeScope, valEscapeScope));
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
            base.VisitLocalDeclaration(node);

            var localSymbol = (SourceLocalSymbol)node.LocalSymbol;
            var scope = _useUpdatedEscapeRules ? localSymbol.Scope : DeclarationScope.Unscoped;

            if (node.InitializerOpt is { } initializer)
            {
                (uint refEscapeScope, uint valEscapeScope) = GetLocalScopes(localSymbol);

                if (scope != DeclarationScope.Unscoped)
                {
                    // If the local has a scoped modifier, then the lifetime is not inferred from
                    // the initializer. Validate the escape values for the initializer instead.

                    Debug.Assert(localSymbol.RefKind == RefKind.None ||
                        refEscapeScope >= GetRefEscape(initializer, _localScopeDepth));

                    if (node.DeclaredTypeOpt?.Type.IsRefLikeType == true)
                    {
                        ValidateEscape(initializer, valEscapeScope, isByRef: false, _diagnostics);
                    }
                }
                else
                {
                    // default to the current scope in case we need to handle self-referential error cases.
                    SetLocalScopes(localSymbol, _localScopeDepth, _localScopeDepth);

                    valEscapeScope = GetValEscape(initializer, _localScopeDepth);
                    if (localSymbol.RefKind != RefKind.None)
                    {
                        refEscapeScope = GetRefEscape(initializer, _localScopeDepth);
                    }

                    SetLocalScopes(localSymbol, refEscapeScope, valEscapeScope);
                }
            }

            return null;
        }

        public override BoundNode? VisitReturnStatement(BoundReturnStatement node)
        {
            base.VisitReturnStatement(node);
            if (node.ExpressionOpt is { Type: { } } expr)
            {
                ValidateEscape(expr, Binder.ReturnOnlyScope, node.RefKind != RefKind.None, _diagnostics);
            }
            return null;
        }

        public override BoundNode? VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            base.VisitYieldReturnStatement(node);
            if (node.Expression is { Type: { } } expr)
            {
                ValidateEscape(expr, Binder.ReturnOnlyScope, isByRef: false, _diagnostics);
            }
            return null;
        }

        public override BoundNode? VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            base.VisitAssignmentOperator(node);
            if (node.Left.Kind != BoundKind.DiscardExpression)
            {
                ValidateAssignment(node.Syntax, node.Left, node.Right, node.IsRef, _diagnostics);
            }
            return null;
        }

        public override BoundNode? VisitIsPatternExpression(BoundIsPatternExpression node)
        {
            using var _ = new PatternInput(this, GetValEscape(node.Expression, _localScopeDepth));
            return base.VisitIsPatternExpression(node);
        }

        public override BoundNode? VisitDeclarationPattern(BoundDeclarationPattern node)
        {
            SetLocalScopes(node);

            using var _ = new PatternInput(this, getDeclarationValEscape(node.DeclaredType, _patternInputValEscape));
            return base.VisitDeclarationPattern(node);

            static uint getDeclarationValEscape(BoundTypeExpression typeExpression, uint valEscape)
            {
                return typeExpression.Type.IsRefLikeType ? valEscape : Binder.CallingMethodScope;
            }
        }

        public override BoundNode? VisitListPattern(BoundListPattern node)
        {
            SetLocalScopes(node);
            return base.VisitListPattern(node);
        }

        public override BoundNode? VisitRecursivePattern(BoundRecursivePattern node)
        {
            SetLocalScopes(node);
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
                    : symbol.GetTypeOrReturnType().IsRefLikeType() ? valEscape : Binder.CallingMethodScope;
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
                return member.Type.IsRefLikeType ? valEscape : Binder.CallingMethodScope;
            }
        }

        private void SetLocalScopes(BoundObjectPattern pattern)
        {
            if (pattern.Variable is LocalSymbol local)
            {
                SetLocalScopes(local, _localScopeDepth, _patternInputValEscape);
            }
        }

        public override BoundNode? VisitConditionalOperator(BoundConditionalOperator node)
        {
            base.VisitConditionalOperator(node);
            if (node.IsRef)
            {
                ValidateRefConditionalOperator(node.Syntax, node.Consequence, node.Alternative, _diagnostics);
            }
            return null;
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
            var placeholderRegion = new PlaceholderRegion(this, placeholders);
            placeholders.Free();
            return placeholderRegion;
        }

        public override BoundNode? VisitCall(BoundCall node)
        {
            using var _ = GetArgumentPlaceholders(node.ReceiverOpt, node.Arguments);
            base.VisitCall(node);

            if (!node.HasErrors)
            {
                var method = node.Method;
                CheckInvocationArgMixing(
                    node.Syntax,
                    method,
                    node.ReceiverOpt,
                    method.Parameters,
                    node.Arguments,
                    node.ArgumentRefKindsOpt,
                    node.ArgsToParamsOpt,
                    _localScopeDepth,
                    _diagnostics);
            }

            return null;
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
                        valEscapeScope = receiver.GetRefKind().IsWritableReference() ? GetRefEscape(receiver, _localScopeDepth) : GetValEscape(receiver, _localScopeDepth);
                        break;
                    case BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter:
                    case BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter:
                        continue;
                    case >= 0:
                        valEscapeScope = GetValEscape(arguments[argIndex], _localScopeDepth);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(placeholder.ArgumentIndex);
                }
                placeholders.Add((placeholder, valEscapeScope));
            }
        }

        public override BoundNode? VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            using var _ = GetArgumentPlaceholders(receiverOpt: null, node.Arguments);
            base.VisitObjectCreationExpression(node);

            if (!node.HasErrors)
            {
                var constructor = node.Constructor;
                CheckInvocationArgMixing(
                    node.Syntax,
                    constructor,
                    receiverOpt: null,
                    constructor.Parameters,
                    node.Arguments,
                    node.ArgumentRefKindsOpt,
                    node.ArgsToParamsOpt,
                    _localScopeDepth,
                    _diagnostics);
            }

            return null;
        }

        public override BoundNode? VisitIndexerAccess(BoundIndexerAccess node)
        {
            using var _ = GetArgumentPlaceholders(node.ReceiverOpt, node.Arguments);
            base.VisitIndexerAccess(node);

            if (!node.HasErrors)
            {
                var indexer = node.Indexer;
                CheckInvocationArgMixing(
                    node.Syntax,
                    indexer,
                    node.ReceiverOpt,
                    indexer.Parameters,
                    node.Arguments,
                    node.ArgumentRefKindsOpt,
                    node.ArgsToParamsOpt,
                    _localScopeDepth,
                    _diagnostics);
            }

            return null;
        }

        public override BoundNode? VisitFunctionPointerInvocation(BoundFunctionPointerInvocation node)
        {
            using var _ = GetArgumentPlaceholders(receiverOpt: null, node.Arguments);
            base.VisitFunctionPointerInvocation(node);

            if (!node.HasErrors)
            {
                var method = node.FunctionPointer.Signature;
                CheckInvocationArgMixing(
                    node.Syntax,
                    method,
                    receiverOpt: null,
                    method.Parameters,
                    node.Arguments,
                    node.ArgumentRefKindsOpt,
                    argsToParamsOpt: default,
                    _localScopeDepth,
                    _diagnostics);
            }

            return null;
        }

        public override BoundNode? VisitAwaitExpression(BoundAwaitExpression node)
        {
            var placeholders = ArrayBuilder<(BoundValuePlaceholderBase, uint)>.GetInstance();
            GetAwaitableInstancePlaceholders(placeholders, node.AwaitableInfo, GetValEscape(node.Expression, _localScopeDepth));
            using var _ = new PlaceholderRegion(this, placeholders);
            placeholders.Free();
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
            if (!conversion.DeconstructionInfo.IsDefault)
            {
                VisitDeconstructionMethodArguments(variables, syntax, conversion, right);
            }
        }

        private void VisitDeconstructionMethodArguments(ArrayBuilder<DeconstructionVariable> variables, SyntaxNode syntax, Conversion conversion, BoundExpression right)
        {
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
            placeholders.Add((conversion.DeconstructionInfo.InputPlaceholder, GetValEscape(right, _localScopeDepth)));

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
                    ? GetValEscape(variable.Expression, _localScopeDepth)
                    : _localScopeDepth;
                placeholders.Add((arg, valEscape));
            }

            using var _ = new PlaceholderRegion(this, placeholders);
            placeholders.Free();

            CheckInvocationArgMixing(
                syntax,
                deconstructMethod,
                invocation.ReceiverOpt,
                parameters,
                invocation.Arguments,
                invocation.ArgumentRefKindsOpt,
                invocation.ArgsToParamsOpt,
                _localScopeDepth,
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
                    : new DeconstructionVariable(expr, GetValEscape(expr, _localScopeDepth), null);
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
            uint collectionEscape = GetValEscape(node.Expression, _localScopeDepth);
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
            placeholders.Free();

            base.VisitForEachStatement(node);

            foreach (var local in node.IterationVariables)
            {
                RemoveLocalScopes(local);
            }

            return null;
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
