// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class LambdaSymbol : SourceMethodSymbol
    {
        private readonly Binder _binder;
        private readonly Symbol _containingSymbol;
        private readonly MessageID _messageID;
        private readonly SyntaxNode _syntax;
        private readonly ImmutableArray<ParameterSymbol> _parameters;
        private RefKind _refKind;
        private ImmutableArray<CustomModifier> _refCustomModifiers;
        private TypeWithAnnotations _returnType;
        private readonly bool _isSynthesized;
        private readonly bool _isAsync;
        private readonly bool _isStatic;
        private readonly DiagnosticBag _declarationDiagnostics;
        private readonly HashSet<AssemblySymbol> _declarationDependencies;

        /// <summary>
        /// This symbol is used as the return type of a LambdaSymbol when we are interpreting
        /// lambda's body in order to infer its return type.
        /// </summary>
        internal static readonly TypeSymbol ReturnTypeIsBeingInferred = new UnsupportedMetadataTypeSymbol();

        /// <summary>
        /// This symbol is used as the return type of a LambdaSymbol when we failed to infer its return type.
        /// </summary>
        internal static readonly TypeSymbol InferenceFailureReturnType = new UnsupportedMetadataTypeSymbol();

        public LambdaSymbol(
            Binder binder,
            CSharpCompilation compilation,
            Symbol containingSymbol,
            UnboundLambda unboundLambda,
            ImmutableArray<TypeWithAnnotations> parameterTypes,
            ImmutableArray<RefKind> parameterRefKinds,
            RefKind refKind,
            ImmutableArray<CustomModifier> refCustomModifiers,
            TypeWithAnnotations returnType) :
            base(unboundLambda.Syntax.GetReference())
        {
            Debug.Assert(syntaxReferenceOpt is not null);
            Debug.Assert(containingSymbol.DeclaringCompilation == compilation);

            _binder = binder;
            _containingSymbol = containingSymbol;
            _messageID = unboundLambda.Data.MessageID;
            _syntax = unboundLambda.Syntax;
            if (!unboundLambda.HasExplicitReturnType(out _refKind, out _refCustomModifiers, out _returnType))
            {
                _refKind = refKind;
                _refCustomModifiers = refCustomModifiers;
                _returnType = !returnType.HasType ? TypeWithAnnotations.Create(ReturnTypeIsBeingInferred) : returnType;
            }
            _isSynthesized = unboundLambda.WasCompilerGenerated;
            _isAsync = unboundLambda.IsAsync;
            _isStatic = unboundLambda.IsStatic;
            // No point in making this lazy. We are always going to need these soon after creation of the symbol.
            _parameters = MakeParameters(compilation, unboundLambda, parameterTypes, parameterRefKinds);
            _declarationDiagnostics = new DiagnosticBag();
            _declarationDependencies = new HashSet<AssemblySymbol>();
        }

        public MessageID MessageID { get { return _messageID; } }

        public override MethodKind MethodKind
        {
            get { return MethodKind.AnonymousFunction; }
        }

        public override bool IsExtern
        {
            get { return false; }
        }

        public override bool IsSealed
        {
            get { return false; }
        }

        public override bool IsAbstract
        {
            get { return false; }
        }

        public override bool IsVirtual
        {
            get { return false; }
        }

        public override bool IsOverride
        {
            get { return false; }
        }

        public override bool IsStatic => _isStatic;

        public override bool IsAsync
        {
            get { return _isAsync; }
        }

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal sealed override bool IsMetadataVirtual(IsMetadataVirtualOption option = IsMetadataVirtualOption.None)
        {
            return false;
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return false;
            }
        }

        public override bool IsVararg
        {
            get { return false; }
        }

        internal override bool HasSpecialName
        {
            get { return false; }
        }

        public override bool ReturnsVoid
        {
            get { return this.ReturnTypeWithAnnotations.HasType && this.ReturnType.IsVoidType(); }
        }

        public override RefKind RefKind
        {
            get { return _refKind; }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return _refCustomModifiers; }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get { return _returnType; }
        }

        // In error recovery and type inference scenarios we do not know the return type
        // until after the body is bound, but the symbol is created before the body
        // is bound.  Fill in the return type post hoc in these scenarios; the
        // IDE might inspect the symbol and want to know the return type.
        internal void SetInferredReturnType(RefKind refKind, TypeWithAnnotations inferredReturnType)
        {
            Debug.Assert(inferredReturnType.HasType);
            Debug.Assert(_returnType.Type.IsErrorType());
            Debug.Assert(refKind != RefKind.RefReadOnly);
            _refKind = refKind;
            _refCustomModifiers = [];
            _returnType = inferredReturnType;
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return false; }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray<MethodSymbol>.Empty; }
        }

        public override Symbol? AssociatedSymbol
        {
            get { return null; }
        }

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get { return ImmutableArray<TypeWithAnnotations>.Empty; }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return ImmutableArray<TypeParameterSymbol>.Empty; }
        }

        public override int Arity
        {
            get { return 0; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return _parameters; }
        }

        internal override bool TryGetThisParameter(out ParameterSymbol? thisParameter)
        {
            // Lambda symbols have no "this" parameter
            thisParameter = null;
            return true;
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Private; }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray.Create<Location>(_syntax.Location);
            }
        }

        /// <summary>
        /// GetFirstLocation() on lambda symbols covers the entire syntax, which is inconvenient but remains for compatibility.
        /// For better diagnostics quality, use the DiagnosticLocation instead, which points to the "delegate" or the "=>".
        /// </summary>
        internal Location DiagnosticLocation
        {
            get
            {
                return _syntax switch
                {
                    AnonymousMethodExpressionSyntax syntax => syntax.DelegateKeyword.GetLocation(),
                    LambdaExpressionSyntax syntax => syntax.ArrowToken.GetLocation(),
                    _ => GetFirstLocation()
                };
            }
        }

        private bool HasExplicitReturnType => _syntax is ParenthesizedLambdaExpressionSyntax { ReturnType: not null };

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray.Create<SyntaxReference>(syntaxReferenceOpt);
            }
        }

        public override Symbol ContainingSymbol
        {
            get { return _containingSymbol; }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get { return Microsoft.Cci.CallingConvention.Default; }
        }

        public override bool IsExtensionMethod
        {
            get { return false; }
        }

        internal override Binder OuterBinder => _binder;

        internal override Binder WithTypeParametersBinder => _binder;

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            return _syntax is LambdaExpressionSyntax lambdaSyntax ?
                OneOrMany.Create(lambdaSyntax.AttributeLists) :
                default;
        }

        internal void GetDeclarationDiagnostics(BindingDiagnosticBag addTo)
        {
            foreach (var parameter in _parameters)
            {
                parameter.ForceComplete(locationOpt: null, filter: null, cancellationToken: default);
            }

            GetAttributes();
            GetReturnTypeAttributes();

            var diagnostics = BindingDiagnosticBag.GetInstance();
            Debug.Assert(diagnostics.DiagnosticBag is { });
            Debug.Assert(diagnostics.DependenciesBag is { });

            AsyncMethodChecks(verifyReturnType: HasExplicitReturnType, DiagnosticLocation, diagnostics);
            if (!HasExplicitReturnType && this.HasAsyncMethodBuilderAttribute(out _))
            {
                addTo.Add(ErrorCode.ERR_BuilderAttributeDisallowed, DiagnosticLocation);
            }

            _declarationDiagnostics.AddRange(diagnostics.DiagnosticBag);
            _declarationDependencies.AddAll(diagnostics.DependenciesBag);
            diagnostics.Free();

            addTo.AddRange(_declarationDiagnostics);
            addTo.AddDependencies((IReadOnlyCollection<AssemblySymbol>)_declarationDependencies);
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

        private ImmutableArray<ParameterSymbol> MakeParameters(
            CSharpCompilation compilation,
            UnboundLambda unboundLambda,
            ImmutableArray<TypeWithAnnotations> parameterTypes,
            ImmutableArray<RefKind> parameterRefKinds)
        {
            Debug.Assert(parameterTypes.Length == parameterRefKinds.Length);

            if (!unboundLambda.HasSignature || unboundLambda.ParameterCount == 0)
            {
                // The parameters may be omitted in source, but they are still present on the symbol.
                return parameterTypes.SelectAsArray((type, ordinal, arg) =>
                                                        SynthesizedParameterSymbol.Create(
                                                            arg.owner,
                                                            type,
                                                            ordinal,
                                                            arg.refKinds[ordinal],
                                                            GeneratedNames.LambdaCopyParameterName(ordinal)), // Make sure nothing binds to this.
                                                     (owner: this, refKinds: parameterRefKinds));
            }

            var builder = ArrayBuilder<ParameterSymbol>.GetInstance(unboundLambda.ParameterCount);
            var hasExplicitlyTypedParameterList = unboundLambda.HasExplicitlyTypedParameterList;
            var numDelegateParameters = parameterTypes.Length;

            for (int p = 0; p < unboundLambda.ParameterCount; ++p)
            {
                var refKind = unboundLambda.RefKind(p);
                var scope = unboundLambda.DeclaredScope(p);
                var paramSyntax = unboundLambda.ParameterSyntax(p);

                // If there are no types given in the lambda then use the delegate type.
                // If the lambda is typed then the types probably match the delegate types;
                // if they do not, use the lambda types for binding. Either way, if we 
                // can, then we use the lambda types. (Whatever you do, do not use the names 
                // in the delegate parameters; they are not in scope!)
                var type = hasExplicitlyTypedParameterList
                    ? unboundLambda.ParameterTypeWithAnnotations(p)
                    : p < numDelegateParameters
                        ? parameterTypes[p]
                        : TypeWithAnnotations.Create(new ExtendedErrorTypeSymbol(compilation, name: string.Empty, arity: 0, errorInfo: null));

                var attributeLists = unboundLambda.ParameterAttributes(p);
                var name = unboundLambda.ParameterName(p);
                var location = unboundLambda.ParameterLocation(p);
                var isParams = paramSyntax?.Modifiers.Any(static m => m.IsKind(SyntaxKind.ParamsKeyword)) ?? false;

                var parameter = new LambdaParameterSymbol(owner: this, paramSyntax?.GetReference(), attributeLists, type, ordinal: p, refKind, scope, name, unboundLambda.ParameterIsDiscard(p), isParams, location);
                builder.Add(parameter);
            }

            var result = builder.ToImmutableAndFree();

            return result;
        }

        public sealed override bool Equals(Symbol symbol, TypeCompareKind compareKind)
        {
            if ((object)this == symbol) return true;

            return symbol is LambdaSymbol lambda
                && lambda._syntax == _syntax
                && lambda._refKind == _refKind
                && lambda._refCustomModifiers.SequenceEqual(_refCustomModifiers)
                && TypeSymbol.Equals(lambda.ReturnType, this.ReturnType, compareKind)
                && ParameterTypesWithAnnotations.SequenceEqual(lambda.ParameterTypesWithAnnotations, compareKind,
                                                               (p1, p2, compareKind) => p1.Equals(p2, compareKind))
                && lambda.ContainingSymbol.Equals(ContainingSymbol, compareKind);
        }

        public override int GetHashCode()
        {
            return _syntax.GetHashCode();
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return _isSynthesized;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get { return true; }
        }

        internal override bool IsDeclaredReadOnly => false;

        internal override bool IsInitOnly => false;

        internal override bool IsUnsafe => false;

        public override ImmutableArray<ImmutableArray<TypeWithAnnotations>> GetTypeParameterConstraintTypes() => ImmutableArray<ImmutableArray<TypeWithAnnotations>>.Empty;

        public override ImmutableArray<TypeParameterConstraintKind> GetTypeParameterConstraintKinds() => ImmutableArray<TypeParameterConstraintKind>.Empty;

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool IsNullableAnalysisEnabled() => throw ExceptionUtilities.Unreachable();

        protected override void NoteAttributesComplete(bool forReturnType)
        {
        }
    }
}
