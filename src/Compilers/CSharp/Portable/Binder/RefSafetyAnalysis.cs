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
            using var region = new UnsafeRegion(this, inUnsafeRegion.HasValue() ? inUnsafeRegion.Value() : _inUnsafeRegion);
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
            using var patternInput = new PatternInput(this, GetValEscape(node.Expression, _localScopeDepth));
            AddLocals(node.InnerLocals);
            base.VisitSwitchStatement(node);
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
            using var _ = new PatternInput(this, GetValEscape(node.Expression, _localScopeDepth));
            SetLocalScopes(node.Pattern); // PROTOTYPE: Remove this call. It should occur within base.VisitIsPatternExpression() below.
            return base.VisitIsPatternExpression(node);
        }

        public override BoundNode? VisitDeclarationPattern(BoundDeclarationPattern node)
        {
            SetLocalScopes(node);
            return base.VisitDeclarationPattern(node);
        }

        public override BoundNode? VisitRecursivePattern(BoundRecursivePattern node)
        {
            SetLocalScopes(node);
            return base.VisitRecursivePattern(node);
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

        private void SetLocalScopes(BoundPattern pattern)
        {
            // PROTOTYPE: Assert we're calling this for all types derived from BoundObjectPattern.
            if (pattern is BoundObjectPattern { Variable: LocalSymbol local })
            {
                SetLocalScopes(local, _localScopeDepth, _patternInputValEscape);
            }
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
                foreach (var arg in node.Arguments)
                {
                    if (arg is BoundConversion { ConversionKind: ConversionKind.InterpolatedStringHandler, Operand: BoundInterpolatedString or BoundBinaryOperator } conversion)
                    {
                        var interpolationData = conversion.Operand.GetInterpolatedStringHandlerData();
                        getInterpolatedStringPlaceholders(interpolationData, node.ReceiverOpt, node.Arguments, placeholders);
                    }
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
                in InterpolatedStringHandlerData interpolationData,
                BoundExpression? receiver,
                ImmutableArray<BoundExpression> arguments,
                ArrayBuilder<(BoundInterpolatedStringArgumentPlaceholder, uint)> placeholders)
            {
                foreach (var placeholder in interpolationData.ArgumentPlaceholders)
                {
                    BoundExpression expr;
                    int argIndex = placeholder.ArgumentIndex;
                    switch (argIndex)
                    {
                        case BoundInterpolatedStringArgumentPlaceholder.InstanceParameter:
                            Debug.Assert(receiver is { });
                            expr = receiver;
                            break;
                        case >= 0:
                            // PROTOTYPE: BindInterpolatedStringHandlerInMemberCall() was ignoring parameters[paramIndex].RefKind and
                            // using GetValEscape() unconditionally for this argument. But if the parameter is by ref, couldn't the ref be captured?
                            // (See addInterpolationPlaceholderReplacements() which does use parameters[paramIndex].RefKind.)
                            expr = arguments[argIndex];
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

        // Based on NullableWalker.VisitDeconstructionAssignmentOperator().
        public override BoundNode? VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            base.VisitDeconstructionAssignmentOperator(node);

            // PROTOTYPE: Should this substitution surround base.VisitDeconstructionAssignmentOperator(node) above?
            // PROTOTYPE: Do we need this placeholder substitution even if we have a deconstruction assignment within a Get, Check, Val, Ref call?
            var left = node.Left;
            var right = node.Right;
            var variables = GetDeconstructionAssignmentVariables(left); // PROTOTYPE: Can we avoid creating nested ArrayBuilder<> instances, and instead recurse through node.Left in VisitDeconstructionArguments()?
            // PROTOTYPE: Remove placeholders added (perhaps recursively) in VisitDeconstructionArguments().
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

            AddPlaceholder(conversion.DeconstructionInfo.InputPlaceholder, GetValEscape(right, _localScopeDepth));

            var parameters = deconstructMethod.Parameters; // PROTOTYPE: Remove if not needed.
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
                AddPlaceholder(arg, valEscape);
            }

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

            // PROTOTYPE: Remove any placeholders added above.

            for (int i = 0; i < n; i++)
            {
                var variable = variables[i];
                var nestedVariables = variable.NestedVariables;
                if (nestedVariables != null)
                {
                    var (placeholder, placeholderConversion) = conversion.DeconstructConversionInfo[i];
                    var underlyingConversion = BoundNode.GetConversion(placeholderConversion, placeholder);
                    // PROTOTYPE: Add placeholder for the temporary.
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
