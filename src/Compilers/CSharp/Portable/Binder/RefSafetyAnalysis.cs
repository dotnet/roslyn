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
    internal sealed partial class RefSafetyAnalysis : BoundTreeWalker
    {
        internal static void Analyze(CSharpCompilation compilation, Symbol symbol, BoundNode node, BindingDiagnosticBag diagnostics)
        {
            var visitor = new RefSafetyAnalysis(
                compilation,
                symbol,
                inUnsafeRegion: InUnsafeMethod(symbol),
                useUpdatedEscapeRules: symbol.ContainingModule.UseUpdatedEscapeRules,
                diagnostics);
            visitor.Visit(node);
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
                visitor.VisitFieldOrPropertyInitializer(initializer);
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
        private Dictionary<BoundValuePlaceholderBase, uint>? _placeholders;
        private uint _switchGoverningValEscape;

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

        protected override BoundExpression? VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        private ref struct LocalScope
        {
            private readonly RefSafetyAnalysis _analysis;

            public LocalScope(RefSafetyAnalysis analysis)
            {
                _analysis = analysis;
                _analysis._localScopeDepth++;
            }

            public void Dispose()
            {
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

        private (uint RefEscapeScope, uint ValEscapeScope) GetLocalScopes(LocalSymbol local)
        {
            if (_localEscapeScopes?.TryGetValue(local, out var scopes) != true)
            {
                throw ExceptionUtilities.UnexpectedValue(local);
            }
            return scopes;
        }

        // PROTOTYPE: When we leave the current scope, we should remove locals from this dictionary.
        private void SetLocalScopes(LocalSymbol local, uint refEscapeScope, uint valEscapeScope)
        {
            _localEscapeScopes ??= new Dictionary<LocalSymbol, (uint RefEscapeScope, uint ValEscapeScope)>();
            // PROTOTYPE: Should only allow overwriting if the current value is the init value.
            _localEscapeScopes[local] = (refEscapeScope, valEscapeScope);
        }

        private void AddPlaceholder(BoundValuePlaceholderBase placeholder, uint valEscapeScope)
        {
            _placeholders ??= new Dictionary<BoundValuePlaceholderBase, uint>();
            _placeholders.Add(placeholder, valEscapeScope);
        }

        private void RemovePlaceholder(BoundValuePlaceholderBase placeholder)
        {
            _placeholders!.Remove(placeholder);
        }

        private uint GetPlaceholder(BoundValuePlaceholderBase placeholder)
        {
            return _placeholders![placeholder];
        }

        public override BoundNode? VisitBlock(BoundBlock node)
        {
            var inUnsafeRegion = node.InUnsafeRegion;
            using var region = new UnsafeRegion(this, inUnsafeRegion.HasValue() ? inUnsafeRegion.Value() :  _inUnsafeRegion);
            using var _ = new LocalScope(this);
            AddLocals(node.Locals);
            return base.VisitBlock(node);
        }

        public override BoundNode Visit(BoundNode? node)
        {
#if DEBUG
            // PROTOTYPE: Ensure _localScopeDepth matches expected depth
            // based on number of BoundNodes above with Locals fields.
#endif
            return base.Visit(node);
        }

        private void VisitFieldOrPropertyInitializer(BoundInitializer initializer)
        {
            var fieldEqualsValue = (BoundFieldEqualsValue)initializer;

            using var _ = new LocalScope(this);
            AddLocals(fieldEqualsValue.Locals);

            var field = fieldEqualsValue.Field;
            bool isByRef = field.RefKind != RefKind.None;
            if (isByRef || field.Type.IsRefLikeType)
            {
                ValidateEscape(fieldEqualsValue.Value, Binder.CallingMethodScope, isByRef: isByRef, _diagnostics);
            }

            base.Visit(fieldEqualsValue.Value);
        }

        public override BoundNode? VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            // PROTOTYPE: Test all combinations of { safe, unsafe } for { container, local function }.
            var localFunction = node.Symbol;
            // PROTOTYPE: Test local function within unsafe block.
            // PROTOTYPE: It's not clear we should be reusing _localEscapeScopes across nested local functions
            // (or lambdas in VisitLambda) because _localScopeDepth is reset when entering the local function
            // (or lambda) so the scopes are unrelated across the outer and inner methods.
            // See https://github.com/dotnet/roslyn/issues/65353 for a related bug.
            var analysis = new RefSafetyAnalysis(_compilation, localFunction, localFunction.IsUnsafe, _useUpdatedEscapeRules, _diagnostics, _localEscapeScopes);
            analysis.Visit(node.BlockBody);
            analysis.Visit(node.ExpressionBody);
            return null;
        }

        public override BoundNode? VisitLambda(BoundLambda node)
        {
            var lambda = node.Symbol;
            var analysis = new RefSafetyAnalysis(_compilation, lambda, _inUnsafeRegion, _useUpdatedEscapeRules, _diagnostics, _localEscapeScopes);
            analysis.Visit(node.Body);
            return null;
        }

        public override BoundNode? VisitConstructorMethodBody(BoundConstructorMethodBody node)
        {
            using var _ = new LocalScope(this);
            AddLocals(node.Locals);
            return base.VisitConstructorMethodBody(node);
        }

        public override BoundNode? VisitForStatement(BoundForStatement node)
        {
            using var _ = new LocalScope(this);
            AddLocals(node.OuterLocals);
            AddLocals(node.InnerLocals);
            return base.VisitForStatement(node);
        }

        public override BoundNode? VisitUsingStatement(BoundUsingStatement node)
        {
            using var _ = new LocalScope(this);
            AddLocals(node.Locals);
            return base.VisitUsingStatement(node);
        }

        public override BoundNode? VisitFixedStatement(BoundFixedStatement node)
        {
            using var _ = new LocalScope(this);
            AddLocals(node.Locals);
            return base.VisitFixedStatement(node);
        }

        public override BoundNode? VisitDoStatement(BoundDoStatement node)
        {
            using var _ = new LocalScope(this);
            AddLocals(node.Locals);
            return base.VisitDoStatement(node);
        }

        public override BoundNode? VisitWhileStatement(BoundWhileStatement node)
        {
            using var _ = new LocalScope(this);
            AddLocals(node.Locals);
            return base.VisitWhileStatement(node);
        }

        public override BoundNode? VisitSwitchStatement(BoundSwitchStatement node)
        {
            using var _ = new LocalScope(this);
            // PROTOTYPE: Do we need the same for switch expressions?
            // PROTOTYPE: Shouldn't be tracking this value in a field. We should be explicitly walking
            // the SwitchSections here, similar to how this was created in Binder.BuildSwitchLabels().
            var previousValEscape = _switchGoverningValEscape;
            _switchGoverningValEscape = GetValEscape(node.Expression, _localScopeDepth);
            AddLocals(node.InnerLocals);
            base.VisitSwitchStatement(node);
            _switchGoverningValEscape = previousValEscape;
            return null;
        }

        public override BoundNode? VisitSwitchSection(BoundSwitchSection node)
        {
            using var _ = new LocalScope(this);
            AddLocals(node.Locals);
            return base.VisitSwitchSection(node);
        }

        public override BoundNode? VisitSwitchExpressionArm(BoundSwitchExpressionArm node)
        {
            using var _ = new LocalScope(this);
            AddLocals(node.Locals);
            return base.VisitSwitchExpressionArm(node);
        }

        public override BoundNode? VisitCatchBlock(BoundCatchBlock node)
        {
            using var _ = new LocalScope(this);
            AddLocals(node.Locals);
            return base.VisitCatchBlock(node);
        }

        public override BoundNode? VisitLocal(BoundLocal node)
        {
            Debug.Assert(_localEscapeScopes?.ContainsKey(node.LocalSymbol) == true ||
                (node.LocalSymbol.ContainingSymbol is SynthesizedSimpleProgramEntryPointSymbol entryPoint && _symbol != entryPoint));

            return base.VisitLocal(node);
        }

        // PROTOTYPE: This needs to be called from every BoundNode that includes Locals.
        private void AddLocals(ImmutableArray<LocalSymbol> locals)
        {
            foreach (var local in locals)
            {
                AddLocal(local, refEscapeScope: _localScopeDepth, valEscapeScope: Binder.CallingMethodScope);
            }
        }

        private void AddLocal(LocalSymbol local, uint refEscapeScope, uint valEscapeScope)
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

            SetLocalScopes(local, refEscapeScope, valEscapeScope);
        }

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
            if (node.ExpressionOpt is { Type: { } } expr)
            {
                ValidateEscape(expr, Binder.ReturnOnlyScope, node.RefKind != RefKind.None, _diagnostics);
            }
            return base.VisitReturnStatement(node);
        }

        public override BoundNode? VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            if (node.Expression is { Type: { } } expr)
            {
                ValidateEscape(expr, Binder.ReturnOnlyScope, isByRef: false, _diagnostics);
            }
            return base.VisitYieldReturnStatement(node);
        }

        public override BoundNode? VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            ValidateAssignment(node.Syntax, node.Left, node.Right, node.IsRef, _diagnostics);
            return base.VisitAssignmentOperator(node);
        }

        public override BoundNode? VisitIsPatternExpression(BoundIsPatternExpression node)
        {
            if (node.Pattern is BoundObjectPattern { Variable: LocalSymbol local })
            {
                uint valEscape = GetValEscape(node.Expression, _localScopeDepth);
                SetLocalScopes(local, _localScopeDepth, valEscape);
            }

            return base.VisitIsPatternExpression(node);
        }

        public override BoundNode? VisitDeclarationPattern(BoundDeclarationPattern node)
        {
            if (node.Variable is LocalSymbol local)
            {
                SetLocalScopes(local, _localScopeDepth, _switchGoverningValEscape);
            }
            return base.VisitDeclarationPattern(node);
        }

        public override BoundNode? VisitConditionalOperator(BoundConditionalOperator node)
        {
            if (node.IsRef)
            {
                ValidateRefConditionalOperator(node.Syntax, node.Consequence, node.Alternative, _diagnostics);
            }
            return base.VisitConditionalOperator(node);
        }

        public override BoundNode? VisitCall(BoundCall node)
        {
            base.VisitCall(node);

            if (!node.HasErrors)
            {
                var method = node.Method;
                // PROTOTYPE: Should this substitution surround base.VisitCall(node) above?
                // PROTOTYPE: Need placeholder substitution whenever we visit any call,
                // and regardless of whether it's Get, Check, Val, Ref, or ArgMixing.
                var placeholders = ArrayBuilder<(BoundInterpolatedStringArgumentPlaceholder, uint)>.GetInstance();
                for (int i = 0; i < node.Arguments.Length; i++)
                {
                    getInterpolatedStringPlaceholders(i, node.Arguments[i], method.Parameters, node.ArgumentRefKindsOpt, node.ArgsToParamsOpt, placeholders);
                }
                foreach (var (placeholder, valEscapeScope) in placeholders)
                {
                    AddPlaceholder(placeholder, valEscapeScope);
                }
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
                foreach (var (placeholder, _) in placeholders)
                {
                    RemovePlaceholder(placeholder);
                }
                placeholders.Free();
            }

            return null;

            void getInterpolatedStringPlaceholders(
                int argIndex,
                BoundExpression argument,
                ImmutableArray<ParameterSymbol> parameters,
                ImmutableArray<RefKind> argRefKindsOpt,
                ImmutableArray<int> argsToParamsOpt,
                ArrayBuilder<(BoundInterpolatedStringArgumentPlaceholder, uint)> placeholders)
            {
                if (argument is BoundConversion { ConversionKind: ConversionKind.InterpolatedStringHandler, Operand: BoundInterpolatedString or BoundBinaryOperator } conversion)
                {
                    var interpolationData = conversion.Operand.GetInterpolatedStringHandlerData();
                    foreach (var placeholder in interpolationData.ArgumentPlaceholders)
                    {
                        BoundExpression expr;
                        switch (placeholder.ArgumentIndex)
                        {
                            case BoundInterpolatedStringArgumentPlaceholder.InstanceParameter:
                                Debug.Assert(node.ReceiverOpt is { });
                                expr = node.ReceiverOpt;
                                break;
                            case >= 0:
                                var paramIndex = argsToParamsOpt.IsDefault ? argIndex : argsToParamsOpt[argIndex];
                                RefKind argRefKind = argRefKindsOpt.RefKinds(argIndex);
                                RefKind paramRefKind = parameters[paramIndex].RefKind;
                                Debug.Assert(paramRefKind == RefKind.None); // PROTOTYPE: Handle other cases, including value passed to RefKind.In. See addInterpolationPlaceholderReplacements().
                                expr = argument;
                                break;
                            case BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter:
                                continue;
                            default:
                                throw ExceptionUtilities.UnexpectedValue(placeholder.ArgumentIndex);
                        }
                        placeholders.Add((placeholder, GetValEscape(expr, _localScopeDepth)));
                    }
                }
            }
        }

        public override BoundNode? VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
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

        public override BoundNode? VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            base.VisitDeconstructionAssignmentOperator(node);

            // PROTOTYPE: Should this substitution surround base.VisitDeconstructionAssignmentOperator(node) above?
            // PROTOTYPE: Do we need this placeholder substitution even if we have a deconstruction assignment within a Get, Check, Val, Ref call?
            var right = node.Right;
            var conversion = right.Conversion;
            Debug.Assert(conversion.Kind == ConversionKind.Deconstruction);
            var deconstructionInfo = conversion.DeconstructionInfo;
            if (deconstructionInfo.Invocation is BoundCall deconstruct)
            {
                var placeholders = ArrayBuilder<(BoundDeconstructValuePlaceholder, uint)>.GetInstance();
                getPlaceholders(node, placeholders);
                foreach (var (placeholder, valEscapeScope) in placeholders)
                {
                    AddPlaceholder(placeholder, valEscapeScope);
                }
                // PROTOTYPE: Handle nested deconstruction. See NullableWalker for instance.
                CheckInvocationArgMixing(
                    right.Syntax,
                    deconstruct.Method,
                    deconstruct.ReceiverOpt,
                    deconstruct.Method.Parameters,
                    deconstruct.Arguments,
                    deconstruct.ArgumentRefKindsOpt,
                    deconstruct.ArgsToParamsOpt,
                    _localScopeDepth,
                    _diagnostics);
                foreach (var (placeholder, _) in placeholders)
                {
                    RemovePlaceholder(placeholder);
                }
                placeholders.Free();
            }
            return null;

            void getPlaceholders(BoundDeconstructionAssignmentOperator node, ArrayBuilder<(BoundDeconstructValuePlaceholder, uint)> placeholders)
            {
                var left = node.Left;
                var right = node.Right;
                var conversion = right.Conversion;
                Debug.Assert(conversion.Kind == ConversionKind.Deconstruction);

                var deconstructionInfo = conversion.DeconstructionInfo;
                placeholders.Add((deconstructionInfo.InputPlaceholder, GetValEscape(right.Operand, _localScopeDepth)));

                // PROTOTYPE: Handle nested deconstruction. See NullableWalker for instance.
                var arguments = left.Arguments;
                for (int i = 0; i < arguments.Length; i++)
                {
                    placeholders.Add((deconstructionInfo.OutputPlaceholders[i], GetValEscape(arguments[i], _localScopeDepth)));
                }
            }
        }

        public override BoundNode? VisitForEachStatement(BoundForEachStatement node)
        {
            using var _ = new LocalScope(this);
            var placeholder = node.DeconstructionOpt?.TargetPlaceholder;
            uint collectionEscape = GetValEscape(node.Expression, _localScopeDepth);
            foreach (var local in node.IterationVariables)
            {
                AddLocal(
                    local,
                    refEscapeScope: local.RefKind == RefKind.None ? _localScopeDepth : collectionEscape,
                    valEscapeScope: collectionEscape);
            }
            if (placeholder is { })
            {
                AddPlaceholder(placeholder, collectionEscape);
            }
            base.VisitForEachStatement(node);
            if (placeholder is { })
            {
                RemovePlaceholder(placeholder);
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
