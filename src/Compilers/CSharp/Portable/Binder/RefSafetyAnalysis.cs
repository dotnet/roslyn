// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class RefSafetyAnalysis : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
    {
        internal static void Analyze(CSharpCompilation compilation, MethodSymbol symbol, BoundNode node, BindingDiagnosticBag diagnostics)
        {
            var visitor = new RefSafetyAnalysis(
                compilation,
                symbol,
                node,
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
        private readonly MethodSymbol _symbol;
        private readonly BoundNode _rootNode;
        private readonly bool _useUpdatedEscapeRules;
        private readonly BindingDiagnosticBag _diagnostics;
        private bool _inUnsafeRegion;
        private SafeContext _localScopeDepth;
        private Dictionary<LocalSymbol, (SafeContext RefEscapeScope, SafeContext ValEscapeScope)>? _localEscapeScopes;
        private Dictionary<BoundValuePlaceholderBase, SafeContextAndLocation>? _placeholderScopes;
        private SafeContext _patternInputValEscape;
#if DEBUG
        private const int MaxTrackVisited = 100; // Avoid tracking if too many expressions.
        private HashSet<BoundExpression>? _visited = new HashSet<BoundExpression>();
#endif

        private RefSafetyAnalysis(
            CSharpCompilation compilation,
            MethodSymbol symbol,
            BoundNode rootNode,
            bool inUnsafeRegion,
            bool useUpdatedEscapeRules,
            BindingDiagnosticBag diagnostics)
        {
            _compilation = compilation;
            _symbol = symbol;
            _rootNode = rootNode;
            _useUpdatedEscapeRules = useUpdatedEscapeRules;
            _diagnostics = diagnostics;
            _inUnsafeRegion = inUnsafeRegion;
            _localScopeDepth = SafeContext.CurrentMethod;
        }

        private ref struct LocalScope
        {
            private readonly RefSafetyAnalysis _analysis;
            private readonly ImmutableArray<LocalSymbol> _locals;
            private readonly bool _adjustDepth;

            /// <param name="adjustDepth">When true, narrows <see cref="_localScopeDepth"/> when the instance is created, and widens it when the instance is disposed.</param>
            public LocalScope(RefSafetyAnalysis analysis, ImmutableArray<LocalSymbol> locals, bool adjustDepth = true)
            {
                _analysis = analysis;
                _locals = locals;
                _adjustDepth = adjustDepth;
                if (adjustDepth)
                    _analysis._localScopeDepth = _analysis._localScopeDepth.Narrower();

                foreach (var local in locals)
                {
                    _analysis.AddLocalScopes(local, refEscapeScope: _analysis._localScopeDepth, valEscapeScope: SafeContext.CallingMethod);
                }
            }

            public void Dispose()
            {
                foreach (var local in _locals)
                {
                    _analysis.RemoveLocalScopes(local);
                }

                if (_adjustDepth)
                    _analysis._localScopeDepth = _analysis._localScopeDepth.Wider();
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
            private readonly SafeContext _previousInputValEscape;

            public PatternInput(RefSafetyAnalysis analysis, SafeContext patternInputValEscape)
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
            private readonly ArrayBuilder<(BoundValuePlaceholderBase, SafeContextAndLocation)> _placeholders;

            public PlaceholderRegion(RefSafetyAnalysis analysis, ArrayBuilder<(BoundValuePlaceholderBase, SafeContextAndLocation)> placeholders)
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

        private readonly struct SafeContextAndLocation
        {
            public static SafeContextAndLocation Create(SafeContext context
#if DEBUG
                , [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0
#endif
            )
            {
                return new SafeContextAndLocation(context
#if DEBUG
                    , filePath, lineNumber
#endif
                );
            }

            public readonly SafeContext Context;
#if DEBUG
            public readonly string FilePath;
            public readonly int LineNumber;
#endif

            private SafeContextAndLocation(SafeContext context
#if DEBUG
                , string filePath, int lineNumber
#endif
            )
            {
                Context = context;
#if DEBUG
                FilePath = filePath;
                LineNumber = lineNumber;
#endif
            }
        }

        private (SafeContext RefEscapeScope, SafeContext ValEscapeScope) GetLocalScopes(LocalSymbol local)
        {
            Debug.Assert(_localEscapeScopes?.ContainsKey(local) == true || _symbol != local.ContainingSymbol);

            return _localEscapeScopes?.TryGetValue(local, out var scopes) == true
                ? scopes
                : (SafeContext.CurrentMethod, SafeContext.CallingMethod);
        }

        private void SetLocalScopes(LocalSymbol local, SafeContext refEscapeScope, SafeContext valEscapeScope)
        {
            Debug.Assert(_localEscapeScopes?.ContainsKey(local) == true);

            AddOrSetLocalScopes(local, refEscapeScope, valEscapeScope);
        }

        private void AddPlaceholderScope(BoundValuePlaceholderBase placeholder, SafeContextAndLocation valEscapeScope)
        {
#if DEBUG
            if (_placeholderScopes?.TryGetValue(placeholder, out var existing) == true)
            {
                Debug.Fail($"Placeholder {placeholder} already has a scope: {existing.FilePath}:{existing.LineNumber}.");
            }
#endif

            // Consider not adding the placeholder to the dictionary if the escape scope is
            // CallingMethod, and simply fallback to that value in GetPlaceholderScope().

            _placeholderScopes ??= new Dictionary<BoundValuePlaceholderBase, SafeContextAndLocation>();
            _placeholderScopes[placeholder] = valEscapeScope;
        }

#pragma warning disable IDE0060
        private void RemovePlaceholderScope(BoundValuePlaceholderBase placeholder)
        {
            Debug.Assert(_placeholderScopes?.ContainsKey(placeholder) == true);

            // https://github.com/dotnet/roslyn/issues/65961: Currently, analysis may require subsequent calls
            // to GetRefEscape(), etc. for the same expression so we cannot remove placeholders eagerly.
            //_placeholderScopes.Remove(placeholder);
        }
#pragma warning restore IDE0060

        private SafeContext GetPlaceholderScope(BoundValuePlaceholderBase placeholder)
        {
            Debug.Assert(_placeholderScopes?.ContainsKey(placeholder) == true);

            return _placeholderScopes?.TryGetValue(placeholder, out var scope) == true
                ? scope.Context
                : SafeContext.CallingMethod;
        }

#if DEBUG
        private bool ContainsPlaceholderScope(BoundValuePlaceholderBase placeholder)
        {
            if (_placeholderScopes?.ContainsKey(placeholder) == true)
            {
                return true;
            }

            // _placeholderScopes should contain all placeholders that may be used by GetValEscape() or CheckValEscape().
            // The following placeholders are not needed by those methods and can be ignored, however.
            switch (placeholder)
            {
                case BoundObjectOrCollectionValuePlaceholder:
                    return true; // CheckValEscapeOfObjectInitializer() does not use BoundObjectOrCollectionValuePlaceholder.
                case BoundInterpolatedStringHandlerPlaceholder:
                    return true; // CheckInterpolatedStringHandlerConversionEscape() does not use BoundInterpolatedStringHandlerPlaceholder.
                case BoundImplicitIndexerValuePlaceholder:
                    return placeholder.Type?.SpecialType == SpecialType.System_Int32;
                case BoundCapturedReceiverPlaceholder:
                    return true; // BoundCapturedReceiverPlaceholder is created in GetInvocationArgumentsForEscape(), and was not part of the BoundNode tree.
                default:
                    return false;
            }
        }
#endif

        public override BoundNode? VisitBlock(BoundBlock node)
        {
            using var _1 = new UnsafeRegion(this, _inUnsafeRegion || node.HasUnsafeModifier);

            // Do not increase the depth if this is the top-level block of a method body.
            // This is needed to ensure that top-level locals have the same lifetime as by-value parameters, for example.
            bool adjustDepth = _rootNode switch
            {
                BoundConstructorMethodBody constructorBody => constructorBody.BlockBody != node && constructorBody.ExpressionBody != node,
                BoundNonConstructorMethodBody methodBody => methodBody.BlockBody != node && methodBody.ExpressionBody != node,
                BoundLambda lambda => lambda.Body != node,
                BoundLocalFunctionStatement localFunction => localFunction.Body != node,
                _ => true,
            };
            using var _2 = new LocalScope(this, node.Locals, adjustDepth);
            return base.VisitBlock(node);
        }

        public override BoundNode? Visit(BoundNode? node)
        {
#if DEBUG
            TrackVisit(node);
#endif
            return base.Visit(node);
        }

#if DEBUG
        protected override void BeforeVisitingSkippedBoundBinaryOperatorChildren(BoundBinaryOperator node)
        {
            TrackVisit(node);
        }

        protected override void BeforeVisitingSkippedBoundCallChildren(BoundCall node)
        {
            TrackVisit(node);
        }

        private void TrackVisit(BoundNode? node)
        {
            if (node is BoundValuePlaceholderBase placeholder)
            {
                Debug.Assert(ContainsPlaceholderScope(placeholder));
            }
            else if (node is BoundExpression expr)
            {
                if (_visited is { } && _visited.Count <= MaxTrackVisited)
                {
                    bool added = _visited.Add(expr);
                    RoslynDebug.Assert(added, $"Expression {expr} `{expr.Syntax}` visited more than once.");
                }
            }
        }

        private void AssertVisited(BoundExpression expr)
        {
            if (expr is BoundValuePlaceholderBase placeholder)
            {
                Debug.Assert(ContainsPlaceholderScope(placeholder));
            }
            else if (_visited is { } && _visited.Count <= MaxTrackVisited)
            {
                RoslynDebug.Assert(_visited.Contains(expr), $"Expected {expr} `{expr.Syntax}` to be visited.");
            }
        }
#endif

        public override BoundNode? VisitFieldEqualsValue(BoundFieldEqualsValue node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode? VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            var localFunction = (LocalFunctionSymbol)node.Symbol;
            var analysis = new RefSafetyAnalysis(_compilation, localFunction, node, _inUnsafeRegion || localFunction.IsUnsafe, _useUpdatedEscapeRules, _diagnostics);
            analysis.Visit(node.BlockBody);
            analysis.Visit(node.ExpressionBody);
            return null;
        }

        public override BoundNode? VisitLambda(BoundLambda node)
        {
            var lambda = node.Symbol;
            var analysis = new RefSafetyAnalysis(_compilation, lambda, node, _inUnsafeRegion, _useUpdatedEscapeRules, _diagnostics);
            analysis.Visit(node.Body);
            return null;
        }

        public override BoundNode? VisitConstructorMethodBody(BoundConstructorMethodBody node)
        {
            // Variables in a constructor initializer like `public MyType(int x) : this(M(out int y))` have the same scope as the parameters.
            using var _ = new LocalScope(this, node.Locals, adjustDepth: false);
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

            this.Visit(node.DeclarationsOpt);
            this.Visit(node.ExpressionOpt);

            var placeholders = ArrayBuilder<(BoundValuePlaceholderBase, SafeContextAndLocation)>.GetInstance();
            if (node.AwaitOpt is { } awaitableInfo)
            {
                SafeContext valEscapeScope = node.ExpressionOpt is { } expr
                    ? GetValEscape(expr, _localScopeDepth)
                    : _localScopeDepth;
                GetAwaitableInstancePlaceholders(placeholders, awaitableInfo, valEscapeScope);
            }

            using var region = new PlaceholderRegion(this, placeholders);
            this.Visit(node.AwaitOpt);
            this.Visit(node.Body);
            return null;
        }

        public override BoundNode? VisitUsingLocalDeclarations(BoundUsingLocalDeclarations node)
        {
            var placeholders = ArrayBuilder<(BoundValuePlaceholderBase, SafeContextAndLocation)>.GetInstance();
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
            this.Visit(node.Expression);
            using var _1 = new LocalScope(this, node.InnerLocals);
            using var _2 = new PatternInput(this, GetValEscape(node.Expression, _localScopeDepth));
            this.VisitList(node.SwitchSections);
            this.Visit(node.DefaultLabel);
            return null;
        }

        public override BoundNode? VisitConvertedSwitchExpression(BoundConvertedSwitchExpression node)
        {
            this.Visit(node.Expression);
            using var _ = new PatternInput(this, GetValEscape(node.Expression, _localScopeDepth));
            this.VisitList(node.SwitchArms);
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
                _symbol != node.LocalSymbol.ContainingSymbol);

            return base.VisitLocal(node);
        }

        private void AddLocalScopes(LocalSymbol local, SafeContext refEscapeScope, SafeContext valEscapeScope)
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
                    SafeContext.CurrentMethod;
                valEscapeScope = scopedModifier == ScopedKind.ScopedValue ?
                    _localScopeDepth :
                    SafeContext.CallingMethod;
            }

            Debug.Assert(_localEscapeScopes?.ContainsKey(local) != true);

            AddOrSetLocalScopes(local, refEscapeScope, valEscapeScope);
        }

        private void AddOrSetLocalScopes(LocalSymbol local, SafeContext refEscapeScope, SafeContext valEscapeScope)
        {
            _localEscapeScopes ??= new Dictionary<LocalSymbol, (SafeContext RefEscapeScope, SafeContext ValEscapeScope)>();
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
            base.VisitLocalDeclaration(node);

            if (node.InitializerOpt is { } initializer)
            {
                var localSymbol = (SourceLocalSymbol)node.LocalSymbol;
                (SafeContext refEscapeScope, SafeContext valEscapeScope) = GetLocalScopes(localSymbol);

                if (_useUpdatedEscapeRules && localSymbol.Scope != ScopedKind.None)
                {
                    // If the local has a scoped modifier, then the SafeContext is not inferred from
                    // the initializer. Validate the escape values for the initializer instead.

                    Debug.Assert(localSymbol.RefKind == RefKind.None ||
                        GetRefEscape(initializer, _localScopeDepth).IsConvertibleTo(refEscapeScope));

                    if (node.DeclaredTypeOpt?.Type.IsRefLikeOrAllowsRefLikeType() == true)
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
                ValidateEscape(expr, SafeContext.ReturnOnly, node.RefKind != RefKind.None, _diagnostics);
            }
            return null;
        }

        public override BoundNode? VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            base.VisitYieldReturnStatement(node);
            if (node.Expression is { Type: { } } expr)
            {
                ValidateEscape(expr, SafeContext.ReturnOnly, isByRef: false, _diagnostics);
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

        public override BoundNode? VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            base.VisitCompoundAssignmentOperator(node);

            if (!node.HasErrors && node.Operator.Method is { } compoundMethod)
            {
                var methodInvocationInfo = MethodInvocationInfo.FromCompoundAssignmentOperator(node);
                methodInvocationInfo = ReplaceWithExtensionImplementationIfNeeded(in methodInvocationInfo);
                CheckInvocationArgMixing(
                    node.Syntax,
                    in methodInvocationInfo,
                    _localScopeDepth,
                    symbolForReporting: compoundMethod,
                    _diagnostics);

                if (!compoundMethod.IsStatic)
                {
                    return null;
                }
            }

            ValidateAssignment(node.Syntax, node.Left, node, isRef: false, _diagnostics);
            return null;
        }

        public override BoundNode? VisitIsPatternExpression(BoundIsPatternExpression node)
        {
            this.Visit(node.Expression);
            using var _ = new PatternInput(this, GetValEscape(node.Expression, _localScopeDepth));
            this.Visit(node.Pattern);
            return null;
        }

        public override BoundNode? VisitDeclarationPattern(BoundDeclarationPattern node)
        {
            SetPatternLocalScopes(node);

            using var _ = new PatternInput(this, getDeclarationValEscape(node.DeclaredType, _patternInputValEscape));
            return base.VisitDeclarationPattern(node);

            static SafeContext getDeclarationValEscape(BoundTypeExpression typeExpression, SafeContext valEscape)
            {
                // https://github.com/dotnet/roslyn/issues/73551:
                // We do not have a test that demonstrates the statement below makes a difference
                // for ref like types. If 'SafeContext.CallingMethod' is always returned, not a single test fails.
                return typeExpression.Type.IsRefLikeOrAllowsRefLikeType() ? valEscape : SafeContext.CallingMethod;
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

            // If the equivalent `Deconstruct` call has unscoped receiver,
            // we narrow the escape scope of the pattern input
            // which flows as the escape scope of all ref-struct declaration subpatterns
            // and so the ref safety of the pattern is equivalent to a `Deconstruct(out var ...)` invocation
            // where "safe-context inference of declaration expressions" would have the same effect.
            if (node.DeconstructMethod is { } m &&
                tryGetReceiverParameter(m)?.EffectiveScope == ScopedKind.None)
            {
                using (new PatternInput(this, _localScopeDepth))
                {
                    return base.VisitRecursivePattern(node);
                }
            }

            return base.VisitRecursivePattern(node);

            static ParameterSymbol? tryGetReceiverParameter(MethodSymbol method)
            {
                if (method.IsExtensionMethod)
                {
                    return method.Parameters is [{ } firstParameter, ..] ? firstParameter : null;
                }
                else if (method.IsExtensionBlockMember())
                {
                    return method.ContainingType.ExtensionParameter;
                }

                return method.TryGetThisParameter(out var thisParameter) ? thisParameter : null;
            }
        }

        public override BoundNode? VisitPositionalSubpattern(BoundPositionalSubpattern node)
        {
            using var _ = new PatternInput(this, getPositionalValEscape(node.Symbol, _patternInputValEscape));
            return base.VisitPositionalSubpattern(node);

            static SafeContext getPositionalValEscape(Symbol? symbol, SafeContext valEscape)
            {
                return symbol is null
                    ? valEscape
                    : symbol.GetTypeOrReturnType().IsRefLikeOrAllowsRefLikeType() ? valEscape : SafeContext.CallingMethod;
            }
        }

        public override BoundNode? VisitPropertySubpattern(BoundPropertySubpattern node)
        {
            using var _ = new PatternInput(this, getMemberValEscape(node.Member, _patternInputValEscape));
            return base.VisitPropertySubpattern(node);

            static SafeContext getMemberValEscape(BoundPropertySubpatternMember? member, SafeContext valEscape)
            {
                if (member is null) return valEscape;
                valEscape = getMemberValEscape(member.Receiver, valEscape);
                return member.Type.IsRefLikeOrAllowsRefLikeType() ? valEscape : SafeContext.CallingMethod;
            }
        }

        private void SetPatternLocalScopes(BoundObjectPattern pattern)
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

        private void VisitArgumentsAndGetArgumentPlaceholders(BoundExpression? receiverOpt, ImmutableArray<BoundExpression> arguments, bool isNewExtensionMethod)
        {
            for (int i = 0; i < arguments.Length; i++)
            {
                var arg = arguments[i];
                if (arg is BoundConversion { ConversionKind: ConversionKind.InterpolatedStringHandler, Operand: BoundInterpolatedString or BoundBinaryOperator } conversion)
                {
                    var interpolationData = conversion.Operand.GetInterpolatedStringHandlerData();
                    var placeholders = ArrayBuilder<(BoundValuePlaceholderBase, SafeContextAndLocation)>.GetInstance();
                    GetInterpolatedStringPlaceholders(placeholders, interpolationData, receiverOpt, i, arguments, isNewExtensionMethod);
                    _ = new PlaceholderRegion(this, placeholders);
                }
                Visit(arg);
            }
        }

        public sealed override BoundNode? VisitCall(BoundCall node)
        {
            MethodInvocationInfo methodInvocationInfo = getInvocationInfo(node);
            if (methodInvocationInfo.Receiver is BoundCall receiver1)
            {
                var calls = ArrayBuilder<(BoundCall call, MethodInvocationInfo methodInvocationInfo)>.GetInstance();

                calls.Push((node, methodInvocationInfo));

                node = receiver1;
                methodInvocationInfo = getInvocationInfo(node);
                while (methodInvocationInfo.Receiver is BoundCall receiver2)
                {
                    BeforeVisitingSkippedBoundCallChildren(node);
                    calls.Push((node, methodInvocationInfo));
                    node = receiver2;
                    methodInvocationInfo = getInvocationInfo(node);
                }

                BeforeVisitingSkippedBoundCallChildren(node);

                visitReceiver(node, in methodInvocationInfo);

                var nodeAndInvocationInfo = (call: node, methodInvocationInfo);
                do
                {
                    visitArguments(nodeAndInvocationInfo.call, in nodeAndInvocationInfo.methodInvocationInfo);
                }
                while (calls.TryPop(out nodeAndInvocationInfo!));

                calls.Free();
            }
            else
            {
                visitReceiver(node, in methodInvocationInfo);
                visitArguments(node, in methodInvocationInfo);
            }

            return null;

            static MethodInvocationInfo getInvocationInfo(BoundCall node)
            {
                var methodInvocationInfo = MethodInvocationInfo.FromCall(node);

                if (!node.IsErroneousNode)
                {
                    methodInvocationInfo = ReplaceWithExtensionImplementationIfNeeded(in methodInvocationInfo);
                }

                return methodInvocationInfo;
            }

            void visitReceiver(BoundCall node, ref readonly MethodInvocationInfo methodInvocationInfo)
            {
                if (node.IsErroneousNode)
                {
                    Visit(node.ReceiverOpt);
                }
                else
                {
                    VisitReceiver(in methodInvocationInfo);
                }
            }

            void visitArguments(BoundCall node, ref readonly MethodInvocationInfo methodInvocationInfo)
            {
                if (node.IsErroneousNode)
                {
                    VisitList(node.Arguments);
                }
                else
                {
                    VisitArguments(node, in methodInvocationInfo);
                }
            }
        }

        protected override void VisitReceiver(BoundCall node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        private void VisitReceiver(ref readonly MethodInvocationInfo methodInvocationInfo)
        {
            if (methodInvocationInfo.Receiver is not null)
            {
                Visit(methodInvocationInfo.Receiver);
            }
        }

        protected override void VisitArguments(BoundCall node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        private void VisitArguments(BoundCall node, ref readonly MethodInvocationInfo methodInvocationInfo)
        {
            Debug.Assert(node.InitialBindingReceiverIsSubjectToCloning != ThreeState.Unknown);

            VisitArgumentsAndGetArgumentPlaceholders(methodInvocationInfo.Receiver, methodInvocationInfo.ArgsOpt, node.Method.IsExtensionBlockMember());

            if (!node.HasErrors)
            {
                CheckInvocationArgMixing(
                    node.Syntax,
                    in methodInvocationInfo,
                    _localScopeDepth,
                    node.Method,
                    _diagnostics);
            }
        }

        private void GetInterpolatedStringPlaceholders(
            ArrayBuilder<(BoundValuePlaceholderBase, SafeContextAndLocation)> placeholders,
            in InterpolatedStringHandlerData interpolationData,
            BoundExpression? receiver,
            int nArgumentsVisited,
            ImmutableArray<BoundExpression> arguments,
            bool isNewExtensionMethod)
        {
            Debug.Assert(interpolationData.ReceiverPlaceholder is not null);
            placeholders.Add((interpolationData.ReceiverPlaceholder, SafeContextAndLocation.Create(_localScopeDepth)));

            foreach (var placeholder in interpolationData.ArgumentPlaceholders)
            {
                SafeContext valEscapeScope;
                int argIndex = placeholder.ArgumentIndex;
                // In new extension form, the ref analysis visitor processes the arguments as if receiver is the first item in the argument list, like the old extension form. This means that all of
                // our placeholders will be off-by-one, with the extension receiver in the first position.
                var newExtensionFormOffset = isNewExtensionMethod ? 1 : 0;
                switch (argIndex)
                {
                    case BoundInterpolatedStringArgumentPlaceholder.InstanceParameter:
                        Debug.Assert(receiver != null);
                        if (receiver is null)
                        {
                            valEscapeScope = SafeContext.CallingMethod;
                        }
                        else
                        {
                            valEscapeScope = receiver.GetRefKind().IsWritableReference() ? GetRefEscape(receiver, _localScopeDepth) : GetValEscape(receiver, _localScopeDepth);
                        }
                        break;
                    case BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter:
                        Debug.Assert(placeholder.Type.SpecialType == SpecialType.System_Boolean);
                        // Escape scope of bool parameter is CallingMethod, which is the default for placeholders.
                        continue;
                    case BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter:
                        // Error condition, no need for additional ref safety errors.
                        continue;
                    case BoundInterpolatedStringArgumentPlaceholder.ExtensionReceiver:
                        Debug.Assert(isNewExtensionMethod);
                        valEscapeScope = getArgumentEscapeScope(nArgumentsVisited, arguments, 0);
                        break;
                    case >= 0:
                        valEscapeScope = getArgumentEscapeScope(nArgumentsVisited, arguments, argIndex + newExtensionFormOffset);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(placeholder.ArgumentIndex + newExtensionFormOffset);
                }
                placeholders.Add((placeholder, SafeContextAndLocation.Create(valEscapeScope)));
            }

            SafeContext getArgumentEscapeScope(int nArgumentsVisited, ImmutableArray<BoundExpression> arguments, int argIndex)
            {
                SafeContext valEscapeScope;
                if (argIndex < nArgumentsVisited)
                {
                    valEscapeScope = GetValEscape(arguments[argIndex], _localScopeDepth);
                }
                else
                {
                    // Error condition, see ERR_InterpolatedStringHandlerArgumentLocatedAfterInterpolatedString.
                    valEscapeScope = SafeContext.CallingMethod; // Consider skipping this placeholder entirely since SafeContext.CallingMethod is the fallback in GetPlaceholderScope().
                }

                return valEscapeScope;
            }
        }

        public override BoundNode? VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            VisitObjectCreationExpressionBase(node);
            return null;
        }

        public override BoundNode? VisitDynamicObjectCreationExpression(BoundDynamicObjectCreationExpression node)
        {
            VisitObjectCreationExpressionBase(node);
            return null;
        }

        public override BoundNode? VisitNewT(BoundNewT node)
        {
            VisitObjectCreationExpressionBase(node);
            return null;
        }

        public override BoundNode? VisitNoPiaObjectCreationExpression(BoundNoPiaObjectCreationExpression node)
        {
            VisitObjectCreationExpressionBase(node);
            return null;
        }

        private void VisitObjectCreationExpressionBase(BoundObjectCreationExpressionBase node)
        {
            if (node.Constructor is null)
            {
                VisitArgumentsAndGetArgumentPlaceholders(receiverOpt: null, node.Arguments, isNewExtensionMethod: false);
                Visit(node.InitializerExpressionOpt);
                return;
            }

            var methodInvocationInfo = MethodInvocationInfo.FromObjectCreation(node);
            methodInvocationInfo = ReplaceWithExtensionImplementationIfNeeded(in methodInvocationInfo);
            VisitArgumentsAndGetArgumentPlaceholders(receiverOpt: null, methodInvocationInfo.ArgsOpt, isNewExtensionMethod: node.Constructor.IsExtensionBlockMember());
            Visit(node.InitializerExpressionOpt);

            if (node.HasErrors)
            {
                return;
            }
            CheckInvocationArgMixing(
                node.Syntax,
                in methodInvocationInfo,
                _localScopeDepth,
                node.Constructor,
                _diagnostics);

            if (node.InitializerExpressionOpt is { })
            {
                // Object initializers are different than a normal constructor in that the 
                // scope of the receiver is determined by evaluating the inputs to the constructor
                // *and* all of the initializers. Another way of thinking about this is that
                // every argument in an initializer that can escape to the receiver is 
                // effectively an argument to the constructor. That means we need to do
                // a second mixing pass here where we consider the receiver escaping 
                // back into the ref parameters of the constructor.
                //
                // At the moment this is only a hypothetical problem. Because the language 
                // doesn't support ref field of ref struct mixing like this could not actually
                // happen in practice. At the same time we want to error on this now so that 
                // in a future when we do have ref field to ref struct this is not a breaking 
                // change. Customers can respond to failures like this by putting scoped on
                // such parameters in the constructor.
                var escapeValues = ArrayBuilder<EscapeValue>.GetInstance();
                var escapeFrom = GetValEscape(node.InitializerExpressionOpt, _localScopeDepth);
                GetEscapeValues(
                    in methodInvocationInfo,
                    ignoreArglistRefKinds: false,
                    mixableArguments: null,
                    escapeValues);

                foreach (var (parameter, argument, _, isRefEscape) in escapeValues)
                {
                    if (!isRefEscape)
                    {
                        continue;
                    }

                    if (parameter?.Type?.IsRefLikeOrAllowsRefLikeType() != true ||
                        !parameter.RefKind.IsWritableReference())
                    {
                        continue;
                    }

                    if (!escapeFrom.IsConvertibleTo(GetValEscape(argument, _localScopeDepth)))
                    {
                        Error(_diagnostics, ErrorCode.ERR_CallArgMixing, argument.Syntax, node.Constructor, parameter.Name);
                    }
                }

                escapeValues.Free();
            }
        }

        public override BoundNode? VisitPropertyAccess(BoundPropertyAccess node)
        {
            Debug.Assert(node.InitialBindingReceiverIsSubjectToCloning != ThreeState.Unknown);
            return base.VisitPropertyAccess(node);
        }

        public override BoundNode? VisitIndexerAccess(BoundIndexerAccess node)
        {
            Debug.Assert(node.InitialBindingReceiverIsSubjectToCloning != ThreeState.Unknown);
            var methodInvocationInfo = MethodInvocationInfo.FromIndexerAccess(node);
            methodInvocationInfo = ReplaceWithExtensionImplementationIfNeeded(in methodInvocationInfo);
            Visit(methodInvocationInfo.Receiver);
            VisitArgumentsAndGetArgumentPlaceholders(methodInvocationInfo.Receiver, methodInvocationInfo.ArgsOpt, node.Indexer.IsExtensionBlockMember());

            if (!node.HasErrors)
            {
                var indexer = node.Indexer;
                CheckInvocationArgMixing(
                    node.Syntax,
                    in methodInvocationInfo,
                    _localScopeDepth,
                    indexer,
                    _diagnostics);
            }

            return null;
        }

        public override BoundNode? VisitFunctionPointerInvocation(BoundFunctionPointerInvocation node)
        {
            VisitArgumentsAndGetArgumentPlaceholders(receiverOpt: null, node.Arguments, isNewExtensionMethod: false);

            if (!node.HasErrors)
            {
                var methodInvocationInfo = MethodInvocationInfo.FromFunctionPointerInvocation(node);
                CheckInvocationArgMixing(
                    node.Syntax,
                    in methodInvocationInfo,
                    _localScopeDepth,
                    node.FunctionPointer.Signature,
                    _diagnostics);
            }

            return null;
        }

        public override BoundNode? VisitAwaitExpression(BoundAwaitExpression node)
        {
            this.Visit(node.Expression);
            var placeholders = ArrayBuilder<(BoundValuePlaceholderBase, SafeContextAndLocation)>.GetInstance();
            GetAwaitableInstancePlaceholders(placeholders, node.AwaitableInfo, GetValEscape(node.Expression, _localScopeDepth));
            using var _ = new PlaceholderRegion(this, placeholders);
            this.Visit(node.AwaitableInfo);
            return null;
        }

        private void GetAwaitableInstancePlaceholders(ArrayBuilder<(BoundValuePlaceholderBase, SafeContextAndLocation)> placeholders, BoundAwaitableInfo awaitableInfo, SafeContext valEscapeScope)
        {
            if (awaitableInfo.AwaitableInstancePlaceholder is { } placeholder)
            {
                placeholders.Add((placeholder, SafeContextAndLocation.Create(valEscapeScope)));
            }

            if (awaitableInfo.RuntimeAsyncAwaitCallPlaceholder is { } runtimePlaceholder)
            {
                placeholders.Add((runtimePlaceholder, SafeContextAndLocation.Create(valEscapeScope)));
            }
        }

        public override BoundNode? VisitImplicitIndexerAccess(BoundImplicitIndexerAccess node)
        {
            // Verify we're only skipping placeholders for int values, where the escape scope is always CallingMethod.
            Debug.Assert(node.ArgumentPlaceholders.All(p => p is BoundImplicitIndexerValuePlaceholder { Type.SpecialType: SpecialType.System_Int32 }));

            base.VisitImplicitIndexerAccess(node);
            return null;
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

            if (invocation.IsErroneousNode || invocation.Method is null)
            {
                return;
            }

            var methodInvocationInfo = MethodInvocationInfo.FromCall(invocation);
            methodInvocationInfo = ReplaceWithExtensionImplementationIfNeeded(in methodInvocationInfo);

            var placeholders = ArrayBuilder<(BoundValuePlaceholderBase, SafeContextAndLocation)>.GetInstance();
            placeholders.Add((conversion.DeconstructionInfo.InputPlaceholder, SafeContextAndLocation.Create(GetValEscape(right, _localScopeDepth))));

            var parameters = methodInvocationInfo.Parameters;
            int n = variables.Count;
            int offset = invocation.InvokedAsExtensionMethod || invocation.Method.IsExtensionBlockMember() ? 1 : 0;
            Debug.Assert(parameters.Length - offset == n);

            for (int i = 0; i < n; i++)
            {
                var variable = variables[i];
                var nestedVariables = variable.NestedVariables;
                var arg = (BoundDeconstructValuePlaceholder)methodInvocationInfo.ArgsOpt[i + offset];
                SafeContext valEscape = nestedVariables is null
                    ? GetValEscape(variable.Expression, _localScopeDepth)
                    : _localScopeDepth;
                placeholders.Add((arg, SafeContextAndLocation.Create(valEscape)));
            }

            using var _ = new PlaceholderRegion(this, placeholders);

            if (offset == 0)
            {
                Visit(methodInvocationInfo.Receiver);
            }
            else
            {
                Debug.Assert(offset == 1);
                Debug.Assert(methodInvocationInfo.Receiver is null);
                Visit(methodInvocationInfo.ArgsOpt[0]);
            }

            CheckInvocationArgMixing(
                syntax,
                in methodInvocationInfo,
                _localScopeDepth,
                invocation.Method,
                _diagnostics);

            for (int i = 0; i < n; i++)
            {
                var variable = variables[i];
                var nestedVariables = variable.NestedVariables;
                if (nestedVariables != null)
                {
                    var (placeholder, placeholderConversion) = conversion.DeconstructConversionInfo[i];
                    var underlyingConversion = BoundNode.GetConversion(placeholderConversion, placeholder);
                    VisitDeconstructionArguments(nestedVariables, syntax, underlyingConversion, right: methodInvocationInfo.ArgsOpt[i + offset]);
                }
            }
        }

        private readonly struct DeconstructionVariable
        {
            internal readonly BoundExpression Expression;
            internal readonly SafeContext ValEscape;
            internal readonly ArrayBuilder<DeconstructionVariable>? NestedVariables;

            internal DeconstructionVariable(BoundExpression expression, SafeContext valEscape, ArrayBuilder<DeconstructionVariable>? nestedVariables)
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
                    ? new DeconstructionVariable(expr, valEscape: SafeContext.Empty, GetDeconstructionAssignmentVariables(tuple))
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
            this.Visit(node.Expression);
            SafeContext collectionEscape;

            if (node.EnumeratorInfoOpt is { InlineArraySpanType: not WellKnownType.Unknown and var spanType, InlineArrayUsedAsValue: false })
            {
                ImmutableArray<BoundExpression> arguments;
                ImmutableArray<RefKind> refKinds;

                SignatureOnlyMethodSymbol equivalentSignatureMethod = GetInlineArrayConversionEquivalentSignatureMethod(
                    // Strip identity conversion added by compiler on top of inline array.
                    inlineArray: node.Expression is not BoundConversion { Conversion.IsIdentity: true, ExplicitCastInCode: false, Operand: BoundExpression operand } ? node.Expression : operand,
                    resultType: node.EnumeratorInfoOpt.GetEnumeratorInfo.Method.ContainingType,
                    out arguments, out refKinds);

                collectionEscape = GetInvocationEscapeScope(
                    MethodInvocationInfo.FromInlineArrayConversion(equivalentSignatureMethod, arguments, refKinds, node.HasAnyErrors),
                    _localScopeDepth,
                    isRefEscape: false);
            }
            else
            {
                collectionEscape = GetValEscape(node.Expression, _localScopeDepth);
            }

            using var _ = new LocalScope(this, ImmutableArray<LocalSymbol>.Empty);

            foreach (var local in node.IterationVariables)
            {
                AddLocalScopes(local, refEscapeScope: local.RefKind == RefKind.None ? _localScopeDepth : collectionEscape, valEscapeScope: collectionEscape);
            }

            var placeholders = ArrayBuilder<(BoundValuePlaceholderBase, SafeContextAndLocation)>.GetInstance();
            if (node.DeconstructionOpt?.TargetPlaceholder is { } targetPlaceholder)
            {
                placeholders.Add((targetPlaceholder, SafeContextAndLocation.Create(collectionEscape)));
            }

            if (node.EnumeratorInfoOpt is { MoveNextAwaitableInfo: { } awaitableInfo })
            {
                GetAwaitableInstancePlaceholders(placeholders, awaitableInfo, collectionEscape);
            }
            else
            {
                awaitableInfo = null;
            }

            using var region = new PlaceholderRegion(this, placeholders);
            this.Visit(node.IterationVariableType);
            this.Visit(node.IterationErrorExpressionOpt);
            this.Visit(node.DeconstructionOpt);
            this.Visit(awaitableInfo);
            this.Visit(node.Body);

            foreach (var local in node.IterationVariables)
            {
                RemoveLocalScopes(local);
            }

            return null;
        }

        public override BoundNode? VisitCollectionExpression(BoundCollectionExpression node)
        {
            if (node.CollectionCreation is { } collectionCreation)
            {
                if (node.CollectionBuilderElementsPlaceholder is { } spanPlaceholder)
                {
                    var elementType = ((NamedTypeSymbol)spanPlaceholder.Type!).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
                    var safeContext = LocalRewriter.ShouldUseRuntimeHelpersCreateSpan(node, elementType.Type) ? SafeContext.ReturnOnly : _localScopeDepth;

                    var placeholders = ArrayBuilder<(BoundValuePlaceholderBase, SafeContextAndLocation)>.GetInstance();
                    placeholders.Add((spanPlaceholder, SafeContextAndLocation.Create(safeContext)));

                    using var _ = new PlaceholderRegion(this, placeholders);
                    Visit(collectionCreation);
                }
                else
                {
                    Visit(collectionCreation);
                }
            }

            VisitList(node.Elements);
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
