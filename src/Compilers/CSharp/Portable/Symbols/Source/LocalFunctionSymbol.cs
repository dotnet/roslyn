// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class LocalFunctionSymbol : SourceMethodSymbol
    {
        private readonly Binder _binder;
        private readonly LocalFunctionStatementSyntax _syntax;
        private readonly Symbol _containingSymbol;
        private readonly DeclarationModifiers _declarationModifiers;
        private readonly ImmutableArray<SourceMethodTypeParameterSymbol> _typeParameters;
        private readonly RefKind _refKind;

        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private bool _lazyIsVarArg;
        // Initialized in two steps. Hold a copy if accessing during initialization.
        private ImmutableArray<TypeParameterConstraintClause> _lazyTypeParameterConstraints;
        private TypeWithAnnotations.Boxed _lazyReturnType;
        private TypeWithAnnotations.Boxed _lazyIteratorElementType;

        // Lock for initializing lazy fields and registering their diagnostics
        // Acquire this lock when initializing lazy objects to guarantee their declaration
        // diagnostics get added to the store exactly once
        private readonly DiagnosticBag _declarationDiagnostics;

        public LocalFunctionSymbol(
            Binder binder,
            Symbol containingSymbol,
            LocalFunctionStatementSyntax syntax)
        {
            _syntax = syntax;
            _containingSymbol = containingSymbol;

            _declarationDiagnostics = new DiagnosticBag();

            _declarationModifiers =
                DeclarationModifiers.Private |
                syntax.Modifiers.ToDeclarationModifiers(diagnostics: _declarationDiagnostics);

            this.CheckUnsafeModifier(_declarationModifiers, _declarationDiagnostics);

            ScopeBinder = binder;

            binder = binder.WithUnsafeRegionIfNecessary(syntax.Modifiers);

            if (_syntax.TypeParameterList != null)
            {
                binder = new WithMethodTypeParametersBinder(this, binder);
                _typeParameters = MakeTypeParameters(_declarationDiagnostics);
            }
            else
            {
                _typeParameters = ImmutableArray<SourceMethodTypeParameterSymbol>.Empty;
                ReportErrorIfHasConstraints(_syntax.ConstraintClauses, _declarationDiagnostics);
            }

            if (IsExtensionMethod)
            {
                _declarationDiagnostics.Add(ErrorCode.ERR_BadExtensionAgg, Locations[0]);
            }

            foreach (var param in syntax.ParameterList.Parameters)
            {
                ReportAttributesDisallowed(param.AttributeLists, _declarationDiagnostics);
            }

            if (syntax.ReturnType.Kind() == SyntaxKind.RefType)
            {
                var returnType = (RefTypeSyntax)syntax.ReturnType;
                if (returnType.ReadOnlyKeyword.Kind() == SyntaxKind.ReadOnlyKeyword)
                {
                    _refKind = RefKind.RefReadOnly;
                }
                else
                {
                    _refKind = RefKind.Ref;
                }
            }
            else
            {
                _refKind = RefKind.None;
            }

            _binder = binder;
        }

        /// <summary>
        /// Binder that owns the scope for the local function symbol, namely the scope where the
        /// local function is declared.
        /// </summary>
        internal Binder ScopeBinder { get; }

        public Binder ParameterBinder => _binder;

        internal void GetDeclarationDiagnostics(DiagnosticBag addTo)
        {
            // Force complete type parameters
            foreach (var typeParam in _typeParameters)
            {
                typeParam.ForceComplete(null, default(CancellationToken));
            }

            // force lazy init
            ComputeParameters();

            foreach (var p in _lazyParameters)
            {
                // Force complete parameters to retrieve all diagnostics
                p.ForceComplete(null, default(CancellationToken));
            }

            ComputeReturnType();

            addTo.AddRange(_declarationDiagnostics);
        }

        internal override void AddDeclarationDiagnostics(DiagnosticBag diagnostics)
            => _declarationDiagnostics.AddRange(diagnostics);

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
            var diagnostics = DiagnosticBag.GetInstance();

            var parameters = ParameterHelpers.MakeParameters(
                _binder,
                this,
                _syntax.ParameterList,
                arglistToken: out arglistToken,
                allowRefOrOut: true,
                allowThis: true,
                addRefReadOnlyModifier: false,
                diagnostics: diagnostics);

            var compilation = DeclaringCompilation;
            ParameterHelpers.EnsureIsReadOnlyAttributeExists(compilation, parameters, diagnostics, modifyCompilation: false);
            ParameterHelpers.EnsureNullableAttributeExists(compilation, this, parameters, diagnostics, modifyCompilation: false);
            // Note: we don't need to warn on annotations used in #nullable disable context for local functions, as this is handled in binding already

            var isVararg = arglistToken.Kind() == SyntaxKind.ArgListKeyword;
            if (isVararg)
            {
                diagnostics.Add(ErrorCode.ERR_IllegalVarArgs, arglistToken.GetLocation());
            }

            if (IsAsync)
            {
                SourceOrdinaryMethodSymbol.ReportAsyncParameterErrors(parameters, diagnostics, this.Locations[0]);
            }

            lock (_declarationDiagnostics)
            {
                if (_lazyParameters != null)
                {
                    diagnostics.Free();
                    return;
                }

                _declarationDiagnostics.AddRangeAndFree(diagnostics);
                _lazyIsVarArg = isVararg;
                _lazyParameters = parameters;
            }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                ComputeReturnType();
                return _lazyReturnType.Value;
            }
        }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override RefKind RefKind => _refKind;

        internal void ComputeReturnType()
        {
            if (_lazyReturnType is object)
            {
                return;
            }

            var diagnostics = DiagnosticBag.GetInstance();
            TypeSyntax returnTypeSyntax = _syntax.ReturnType;
            TypeWithAnnotations returnType = _binder.BindType(returnTypeSyntax.SkipRef(), diagnostics);

            var compilation = DeclaringCompilation;

            // Skip some diagnostics when the local function is not associated with a compilation
            // (specifically, local functions nested in expressions in the EE).
            if (compilation is object)
            {
                if (this.IsAsync)
                {
                    if (this.RefKind != RefKind.None)
                    {
                        ReportBadRefToken(returnTypeSyntax, diagnostics);
                    }
                    else if (returnType.Type.IsBadAsyncReturn(compilation))
                    {
                        diagnostics.Add(ErrorCode.ERR_BadAsyncReturn, this.Locations[0]);
                    }
                }

                var location = _syntax.ReturnType.Location;
                if (_refKind == RefKind.RefReadOnly)
                {
                    compilation.EnsureIsReadOnlyAttributeExists(diagnostics, location, modifyCompilation: false);
                }

                if (compilation.ShouldEmitNullableAttributes(this) &&
                    returnType.NeedsNullableAttribute())
                {
                    compilation.EnsureNullableAttributeExists(diagnostics, location, modifyCompilation: false);
                    // Note: we don't need to warn on annotations used in #nullable disable context for local functions, as this is handled in binding already
                }
            }

            // span-like types are returnable in general
            if (returnType.IsRestrictedType(ignoreSpanLikeTypes: true))
            {
                // Method or delegate cannot return type '{0}'
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

                _declarationDiagnostics.AddRangeAndFree(diagnostics);
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
                var firstParam = _syntax.ParameterList.Parameters.FirstOrDefault();
                return firstParam != null &&
                    !firstParam.IsArgList &&
                    firstParam.Modifiers.Any(SyntaxKind.ThisKeyword);
            }
        }

        internal override TypeWithAnnotations IteratorElementTypeWithAnnotations
        {
            get
            {
                return _lazyIteratorElementType?.Value ?? default;
            }
            set
            {
                Debug.Assert(_lazyIteratorElementType == null || TypeSymbol.Equals(_lazyIteratorElementType.Value.Type, value.Type, TypeCompareKind.ConsiderEverything2));
                Interlocked.CompareExchange(ref _lazyIteratorElementType, new TypeWithAnnotations.Boxed(value), null);
            }
        }

        public override MethodKind MethodKind => MethodKind.LocalFunction;

        public sealed override Symbol ContainingSymbol => _containingSymbol;

        public override string Name => _syntax.Identifier.ValueText;

        public SyntaxToken NameToken => _syntax.Identifier;

        public Binder SignatureBinder => _binder;

        internal override bool HasSpecialName => false;

        public override bool HidesBaseMethodsByName => false;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        public override ImmutableArray<Location> Locations => ImmutableArray.Create(_syntax.Identifier.GetLocation());

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray.Create(_syntax.GetReference());

        internal override bool GenerateDebugInfo => true;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        internal override MethodImplAttributes ImplementationAttributes => default(MethodImplAttributes);

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation => null;

        internal override CallingConvention CallingConvention => CallingConvention.Default;

        internal override bool HasDeclarativeSecurity => false;

        internal override bool RequiresSecurityObject => false;

        public override Symbol AssociatedSymbol => null;

        public override Accessibility DeclaredAccessibility => ModifierUtils.EffectiveAccessibility(_declarationModifiers);

        public override bool IsAsync => (_declarationModifiers & DeclarationModifiers.Async) != 0;

        public override bool IsStatic => (_declarationModifiers & DeclarationModifiers.Static) != 0;

        public override bool IsVirtual => (_declarationModifiers & DeclarationModifiers.Virtual) != 0;

        public override bool IsOverride => (_declarationModifiers & DeclarationModifiers.Override) != 0;

        public override bool IsAbstract => (_declarationModifiers & DeclarationModifiers.Abstract) != 0;

        public override bool IsSealed => (_declarationModifiers & DeclarationModifiers.Sealed) != 0;

        public override bool IsExtern => (_declarationModifiers & DeclarationModifiers.Extern) != 0;

        public bool IsUnsafe => (_declarationModifiers & DeclarationModifiers.Unsafe) != 0;

        internal bool IsExpressionBodied => _syntax.Body == null && _syntax.ExpressionBody != null;

        internal override bool IsDeclaredReadOnly => false;

        public override DllImportData GetDllImportData() => null;

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => ImmutableArray<string>.Empty;

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override bool TryGetThisParameter(out ParameterSymbol thisParameter)
        {
            // Local function symbols have no "this" parameter
            thisParameter = null;
            return true;
        }

        private void ReportAttributesDisallowed(SyntaxList<AttributeListSyntax> attributes, DiagnosticBag diagnostics)
        {
            var diagnosticInfo = MessageID.IDS_FeatureLocalFunctionAttributes.GetFeatureAvailabilityDiagnosticInfoOpt(this.DeclaringCompilation);
            if (diagnosticInfo is object)
            {
                foreach (var attrList in attributes)
                {
                    diagnostics.Add(diagnosticInfo, attrList.Location);
                }
            }
        }

        private ImmutableArray<SourceMethodTypeParameterSymbol> MakeTypeParameters(DiagnosticBag diagnostics)
        {
            var result = ArrayBuilder<SourceMethodTypeParameterSymbol>.GetInstance();
            var typeParameters = _syntax.TypeParameterList.Parameters;
            for (int ordinal = 0; ordinal < typeParameters.Count; ordinal++)
            {
                var parameter = typeParameters[ordinal];
                if (parameter.VarianceKeyword.Kind() != SyntaxKind.None)
                {
                    diagnostics.Add(ErrorCode.ERR_IllegalVarianceSyntax, parameter.VarianceKeyword.GetLocation());
                }

                // Attributes are currently disallowed on local function type parameters
                ReportAttributesDisallowed(parameter.AttributeLists, diagnostics);

                var identifier = parameter.Identifier;
                var location = identifier.GetLocation();
                var name = identifier.ValueText;

                foreach (var @param in result)
                {
                    if (name == @param.Name)
                    {
                        diagnostics.Add(ErrorCode.ERR_DuplicateTypeParameter, location, name);
                        break;
                    }
                }

                var tpEnclosing = ContainingSymbol.FindEnclosingTypeParameter(name);
                if ((object)tpEnclosing != null)
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

        public override ImmutableArray<TypeParameterConstraintClause> GetTypeParameterConstraintClauses()
        {
            if (_lazyTypeParameterConstraints.IsDefault)
            {
                var diagnostics = DiagnosticBag.GetInstance();
                var constraints = this.MakeTypeParameterConstraints(
                    _binder,
                    TypeParameters,
                    _syntax.TypeParameterList,
                    _syntax.ConstraintClauses,
                    _syntax.Identifier.GetLocation(),
                    diagnostics);
                lock (_declarationDiagnostics)
                {
                    if (_lazyTypeParameterConstraints.IsDefault)
                    {
                        _declarationDiagnostics.AddRange(diagnostics);
                        _lazyTypeParameterConstraints = constraints;
                    }
                }
                diagnostics.Free();
            }

            return _lazyTypeParameterConstraints;
        }

        public override int GetHashCode()
        {
            // this is what lambdas do (do not use hashes of other fields)
            return _syntax.GetHashCode();
        }

        public sealed override bool Equals(Symbol symbol, TypeCompareKind compareKind)
        {
            if ((object)this == symbol) return true;

            var localFunction = symbol as LocalFunctionSymbol;
            return (object)localFunction != null
                && localFunction._syntax == _syntax;
        }
    }
}
