// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class LocalFunctionSymbol : LocalFunctionOrSourceMemberMethodSymbol
    {
        private readonly Binder _binder;
        private readonly Symbol _containingSymbol;
        private readonly DeclarationModifiers _declarationModifiers;
        private readonly ImmutableArray<SourceMethodTypeParameterSymbol> _typeParameters;
        private readonly RefKind _refKind;

        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private bool _lazyIsVarArg;
        // Initialized in two steps. Hold a copy if accessing during initialization.
        private ImmutableArray<ImmutableArray<TypeWithAnnotations>> _lazyTypeParameterConstraintTypes;
        private ImmutableArray<TypeParameterConstraintKind> _lazyTypeParameterConstraintKinds;
        private TypeWithAnnotations.Boxed? _lazyReturnType;

        // Lock for initializing lazy fields and registering their diagnostics
        // Acquire this lock when initializing lazy objects to guarantee their declaration
        // diagnostics get added to the store exactly once
        private readonly DiagnosticBag _declarationDiagnostics;
        private readonly HashSet<AssemblySymbol> _declarationDependencies;

        public LocalFunctionSymbol(
            Binder binder,
            Symbol containingSymbol,
            LocalFunctionStatementSyntax syntax)
            : base(syntax.GetReference(), isIterator: SyntaxFacts.HasYieldOperations(syntax.Body))
        {
            Debug.Assert(containingSymbol.DeclaringCompilation == binder.Compilation);
            _containingSymbol = containingSymbol;

            _declarationDiagnostics = new DiagnosticBag();
            _declarationDependencies = new HashSet<AssemblySymbol>();

            _declarationModifiers =
                DeclarationModifiers.Private |
                syntax.Modifiers.ToDeclarationModifiers(isForTypeDeclaration: false, diagnostics: _declarationDiagnostics);

            var diagnostics = BindingDiagnosticBag.GetInstance();
            Debug.Assert(diagnostics.DiagnosticBag is { });
            Debug.Assert(diagnostics.DependenciesBag is { });

            this.CheckUnsafeModifier(_declarationModifiers, diagnostics);

            ScopeBinder = binder;

            binder = binder.WithUnsafeRegionIfNecessary(syntax.Modifiers);

            if (syntax.TypeParameterList != null)
            {
                _typeParameters = MakeTypeParameters(diagnostics);
            }
            else
            {
                _typeParameters = ImmutableArray<SourceMethodTypeParameterSymbol>.Empty;
                ReportErrorIfHasConstraints(syntax.ConstraintClauses, _declarationDiagnostics);
            }

            if (IsExtensionMethod)
            {
                _declarationDiagnostics.Add(ErrorCode.ERR_BadExtensionAgg, GetFirstLocation());
            }

            foreach (var param in syntax.ParameterList.Parameters)
            {
                ReportAttributesDisallowed(param.AttributeLists, diagnostics);
            }

            syntax.ReturnType.SkipRefInLocalOrReturn(diagnostics, out _refKind);

            _declarationDiagnostics.AddRange(diagnostics.DiagnosticBag);
            _declarationDependencies.AddAll(diagnostics.DependenciesBag);
            diagnostics.Free();

            _binder = binder;
        }

        /// <summary>
        /// Binder that owns the scope for the local function symbol, namely the scope where the
        /// local function is declared.
        /// </summary>
        internal Binder ScopeBinder { get; }

        internal override Binder OuterBinder => _binder;

        internal override Binder WithTypeParametersBinder
            => _typeParameters.IsEmpty ? _binder : new WithMethodTypeParametersBinder(this, _binder);

        internal LocalFunctionStatementSyntax Syntax => (LocalFunctionStatementSyntax)syntaxReferenceOpt.GetSyntax();

        internal void GetDeclarationDiagnostics(BindingDiagnosticBag addTo)
        {
            // Force complete type parameters
            foreach (var typeParam in _typeParameters)
            {
                typeParam.ForceComplete(null, filter: null, default(CancellationToken));
            }

            // force lazy init
            ComputeParameters();

            foreach (var p in _lazyParameters)
            {
                // Force complete parameters to retrieve all diagnostics
                p.ForceComplete(null, filter: null, default(CancellationToken));
            }

            ComputeReturnType();

            GetAttributes();
            GetReturnTypeAttributes();

            var compilation = DeclaringCompilation;
            ParameterHelpers.EnsureRefKindAttributesExist(compilation, Parameters, addTo, modifyCompilation: false);
            // Not emitting ParamCollectionAttribute/ParamArrayAttribute for local functions
            ParameterHelpers.EnsureNativeIntegerAttributeExists(compilation, Parameters, addTo, modifyCompilation: false);
            ParameterHelpers.EnsureScopedRefAttributeExists(compilation, Parameters, addTo, modifyCompilation: false);
            ParameterHelpers.EnsureNullableAttributeExists(compilation, this, Parameters, addTo, modifyCompilation: false);

            addTo.AddRange(_declarationDiagnostics);
            addTo.AddDependencies((IReadOnlyCollection<AssemblySymbol>)_declarationDependencies);

            AsyncMethodChecks(addTo);

            var diagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: false, withDependencies: addTo.AccumulatesDependencies);
            if (IsEntryPointCandidate && !IsGenericMethod &&
                ContainingSymbol is SynthesizedSimpleProgramEntryPointSymbol &&
                compilation.HasEntryPointSignature(this, diagnostics).IsCandidate)
            {
                addTo.Add(ErrorCode.WRN_MainIgnored, Syntax.Identifier.GetLocation(), this);
            }

            addTo.AddRangeAndFree(diagnostics);
        }

        internal override void AddDeclarationDiagnostics(BindingDiagnosticBag diagnostics)
        {
            if (diagnostics.DiagnosticBag is { } diagnosticBag)
            {
                _declarationDiagnostics.AddRange(diagnosticBag);
            }

            if (diagnostics.DependenciesBag is { } dependenciesBag)
            {
                _declarationDependencies.AddAll(dependenciesBag);
            }
        }

        public override bool RequiresInstanceReceiver => false;

        public override bool IsVararg
        {
            get
            {
                ComputeParameters();
                return _lazyIsVarArg;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                ComputeParameters();
                return _lazyParameters;
            }
        }

        private void ComputeParameters()
        {
            if (_lazyParameters != null)
            {
                return;
            }

            SyntaxToken arglistToken;
            var diagnostics = BindingDiagnosticBag.GetInstance();
            Debug.Assert(diagnostics.DiagnosticBag is { });
            Debug.Assert(diagnostics.DependenciesBag is { });

            var parameters = ParameterHelpers.MakeParameters(
                WithTypeParametersBinder,
                this,
                this.Syntax.ParameterList,
                arglistToken: out arglistToken,
                allowRefOrOut: true,
                allowThis: true,
                addRefReadOnlyModifier: false,
                diagnostics: diagnostics).Cast<SourceParameterSymbol, ParameterSymbol>();

            // Note: we don't need to warn on annotations used in #nullable disable context for local functions, as this is handled in binding already

            var isVararg = arglistToken.Kind() == SyntaxKind.ArgListKeyword;
            if (isVararg)
            {
                diagnostics.Add(ErrorCode.ERR_IllegalVarArgs, arglistToken.GetLocation());
            }

            lock (_declarationDiagnostics)
            {
                if (_lazyParameters != null)
                {
                    diagnostics.Free();
                    return;
                }

                _declarationDiagnostics.AddRange(diagnostics.DiagnosticBag);
                _declarationDependencies.AddAll(diagnostics.DependenciesBag);
                diagnostics.Free();
                _lazyIsVarArg = isVararg;
                _lazyParameters = parameters;
            }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                ComputeReturnType();
                return _lazyReturnType!.Value;
            }
        }

        public override RefKind RefKind => _refKind;

        internal void ComputeReturnType()
        {
            if (_lazyReturnType is object)
            {
                return;
            }

            var diagnostics = BindingDiagnosticBag.GetInstance();
            Debug.Assert(diagnostics.DiagnosticBag is { });
            Debug.Assert(diagnostics.DependenciesBag is { });

            TypeSyntax returnTypeSyntax = Syntax.ReturnType;
            Debug.Assert(returnTypeSyntax is not ScopedTypeSyntax);
            TypeWithAnnotations returnType = WithTypeParametersBinder.BindType(returnTypeSyntax.SkipScoped(out _).SkipRef(), diagnostics);

            var compilation = DeclaringCompilation;

            // Skip some diagnostics when the local function is not associated with a compilation
            // (specifically, local functions nested in expressions in the EE).
            if (compilation is object)
            {
                Location? location = null;
                if (_refKind == RefKind.RefReadOnly)
                {
                    compilation.EnsureIsReadOnlyAttributeExists(diagnostics, location ??= returnTypeSyntax.Location, modifyCompilation: false);
                }

                if (compilation.ShouldEmitNativeIntegerAttributes(returnType.Type))
                {
                    compilation.EnsureNativeIntegerAttributeExists(diagnostics, location ??= returnTypeSyntax.Location, modifyCompilation: false);
                }

                if (compilation.ShouldEmitNullableAttributes(this) &&
                    returnType.NeedsNullableAttribute())
                {
                    compilation.EnsureNullableAttributeExists(diagnostics, location ??= returnTypeSyntax.Location, modifyCompilation: false);
                    // Note: we don't need to warn on annotations used in #nullable disable context for local functions, as this is handled in binding already
                }
            }

            // span-like types are returnable in general
            if (returnType.IsRestrictedType(ignoreSpanLikeTypes: true))
            {
                // The return type of a method, delegate, or function pointer cannot be '{0}'
                diagnostics.Add(ErrorCode.ERR_MethodReturnCantBeRefAny, returnTypeSyntax.Location, returnType.Type);
            }

            Debug.Assert(_refKind == RefKind.None
                || !returnType.IsVoidType()
                || returnTypeSyntax.HasErrors);

            lock (_declarationDiagnostics)
            {
                if (_lazyReturnType is object)
                {
                    diagnostics.Free();
                    return;
                }

                _declarationDiagnostics.AddRange(diagnostics.DiagnosticBag);
                _declarationDependencies.AddAll(diagnostics.DependenciesBag);
                diagnostics.Free();
                Interlocked.CompareExchange(ref _lazyReturnType, new TypeWithAnnotations.Boxed(returnType), null);
            }
        }

        public override bool ReturnsVoid => ReturnType.IsVoidType();

        public override int Arity => TypeParameters.Length;

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => GetTypeParametersAsTypeArguments();

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
            => _typeParameters.Cast<SourceMethodTypeParameterSymbol, TypeParameterSymbol>();

        public override bool IsExtensionMethod
        {
            get
            {
                // It is an error to be an extension method, but we need to compute it to report it
                var firstParam = Syntax.ParameterList.Parameters.FirstOrDefault();
                return firstParam != null &&
                    !firstParam.IsArgList &&
                    firstParam.Modifiers.Any(SyntaxKind.ThisKeyword);
            }
        }

        public override MethodKind MethodKind => MethodKind.LocalFunction;

        public sealed override Symbol ContainingSymbol => _containingSymbol;

        public override string Name => Syntax.Identifier.ValueText ?? "";

        public SyntaxToken NameToken => Syntax.Identifier;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        public override ImmutableArray<Location> Locations => ImmutableArray.Create(Syntax.Identifier.GetLocation());

        public override Location TryGetFirstLocation() => Syntax.Identifier.GetLocation();

        internal override bool GenerateDebugInfo => true;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        internal override CallingConvention CallingConvention => CallingConvention.Default;

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            return OneOrMany.Create(Syntax.AttributeLists);
        }

        protected override void NoteAttributesComplete(bool forReturnType) { }

        public override Symbol? AssociatedSymbol => null;

        public override Accessibility DeclaredAccessibility => ModifierUtils.EffectiveAccessibility(_declarationModifiers);

        public override bool IsAsync => (_declarationModifiers & DeclarationModifiers.Async) != 0;

        public override bool IsStatic => (_declarationModifiers & DeclarationModifiers.Static) != 0;

        public override bool IsVirtual => (_declarationModifiers & DeclarationModifiers.Virtual) != 0;

        public override bool IsOverride => (_declarationModifiers & DeclarationModifiers.Override) != 0;

        public override bool IsAbstract => (_declarationModifiers & DeclarationModifiers.Abstract) != 0;

        public override bool IsSealed => (_declarationModifiers & DeclarationModifiers.Sealed) != 0;

        public override bool IsExtern => (_declarationModifiers & DeclarationModifiers.Extern) != 0;

        public bool IsUnsafe => (_declarationModifiers & DeclarationModifiers.Unsafe) != 0;

        internal bool IsExpressionBodied => Syntax is { Body: null, ExpressionBody: object _ };

        internal override bool IsDeclaredReadOnly => false;

        internal override bool IsInitOnly => false;

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool TryGetThisParameter(out ParameterSymbol? thisParameter)
        {
            // Local function symbols have no "this" parameter
            thisParameter = null;
            return true;
        }

        private void ReportAttributesDisallowed(SyntaxList<AttributeListSyntax> attributes, BindingDiagnosticBag diagnostics)
        {
            var diagnosticInfo = MessageID.IDS_FeatureLocalFunctionAttributes.GetFeatureAvailabilityDiagnosticInfo((CSharpParseOptions)syntaxReferenceOpt.SyntaxTree.Options);
            if (diagnosticInfo is object)
            {
                foreach (var attrList in attributes)
                {
                    diagnostics.Add(diagnosticInfo, attrList.Location);
                }
            }
        }

        private ImmutableArray<SourceMethodTypeParameterSymbol> MakeTypeParameters(BindingDiagnosticBag diagnostics)
        {
            var result = ArrayBuilder<SourceMethodTypeParameterSymbol>.GetInstance();
            var typeParameters = Syntax.TypeParameterList?.Parameters ?? default;
            for (int ordinal = 0; ordinal < typeParameters.Count; ordinal++)
            {
                var parameter = typeParameters[ordinal];
                if (parameter.VarianceKeyword.Kind() != SyntaxKind.None)
                {
                    diagnostics.Add(ErrorCode.ERR_IllegalVarianceSyntax, parameter.VarianceKeyword.GetLocation());
                }

                ReportAttributesDisallowed(parameter.AttributeLists, diagnostics);

                var identifier = parameter.Identifier;
                var location = identifier.GetLocation();
                var name = identifier.ValueText ?? "";

                foreach (var @param in result)
                {
                    if (name == @param.Name)
                    {
                        diagnostics.Add(ErrorCode.ERR_DuplicateTypeParameter, location, name);
                        break;
                    }
                }

                SourceMemberContainerTypeSymbol.ReportReservedTypeName(identifier.Text, this.DeclaringCompilation, diagnostics.DiagnosticBag, location);

                var tpEnclosing = ContainingSymbol.FindEnclosingTypeParameter(name);
                if ((object?)tpEnclosing != null)
                {
                    ErrorCode typeError;
                    if (tpEnclosing.ContainingSymbol.Kind == SymbolKind.Method)
                    {
                        // Type parameter '{0}' has the same name as the type parameter from outer method '{1}'
                        typeError = ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter;
                    }
                    else
                    {
                        Debug.Assert(tpEnclosing.ContainingSymbol.Kind == SymbolKind.NamedType);
                        // Type parameter '{0}' has the same name as the type parameter from outer type '{1}'
                        typeError = ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter;
                    }
                    diagnostics.Add(typeError, location, name, tpEnclosing.ContainingSymbol);
                }

                var typeParameter = new SourceMethodTypeParameterSymbol(
                        this,
                        name,
                        ordinal,
                        ImmutableArray.Create(location),
                        ImmutableArray.Create(parameter.GetReference()));

                result.Add(typeParameter);
            }

            return result.ToImmutableAndFree();
        }

        public override ImmutableArray<ImmutableArray<TypeWithAnnotations>> GetTypeParameterConstraintTypes()
        {
            if (_lazyTypeParameterConstraintTypes.IsDefault)
            {
                GetTypeParameterConstraintKinds();

                var syntax = Syntax;
                var diagnostics = BindingDiagnosticBag.GetInstance();
                Debug.Assert(diagnostics.DiagnosticBag is { });
                Debug.Assert(diagnostics.DependenciesBag is { });

                var constraints = this.MakeTypeParameterConstraintTypes(
                    WithTypeParametersBinder,
                    TypeParameters,
                    syntax.TypeParameterList,
                    syntax.ConstraintClauses,
                    diagnostics);
                lock (_declarationDiagnostics)
                {
                    if (_lazyTypeParameterConstraintTypes.IsDefault)
                    {
                        _declarationDiagnostics.AddRange(diagnostics.DiagnosticBag);
                        _declarationDependencies.AddAll(diagnostics.DependenciesBag);
                        _lazyTypeParameterConstraintTypes = constraints;
                    }
                }
                diagnostics.Free();
            }

            return _lazyTypeParameterConstraintTypes;
        }

        public override ImmutableArray<TypeParameterConstraintKind> GetTypeParameterConstraintKinds()
        {
            if (_lazyTypeParameterConstraintKinds.IsDefault)
            {
                var syntax = Syntax;
                var constraints = this.MakeTypeParameterConstraintKinds(
                    WithTypeParametersBinder,
                    TypeParameters,
                    syntax.TypeParameterList,
                    syntax.ConstraintClauses);

                ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeParameterConstraintKinds, constraints);
            }

            return _lazyTypeParameterConstraintKinds;
        }

        internal override bool IsNullableAnalysisEnabled() => throw ExceptionUtilities.Unreachable();

        public override int GetHashCode()
        {
            // this is what lambdas do (do not use hashes of other fields)
            return Syntax.GetHashCode();
        }

        public sealed override bool Equals(Symbol symbol, TypeCompareKind compareKind)
        {
            if ((object)this == symbol) return true;

            var localFunction = symbol as LocalFunctionSymbol;
            return localFunction?.Syntax == Syntax;
        }
    }
}
