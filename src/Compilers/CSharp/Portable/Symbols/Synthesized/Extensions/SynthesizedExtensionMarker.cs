// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// This method encodes the information needed to round-trip extension types
    /// through metadata.
    ///
    /// It encodes:
    /// - whether the extension type is implicit or explicit (PROTOTYPE)
    /// - the underlying type (first parameter type)
    /// - the base extensions (subsequent parameter types)
    /// </summary>
    internal sealed class SynthesizedExtensionMarker : SourceOrdinaryMethodSymbolBase
    {
        private readonly TypeSymbol _underlyingType;
        private readonly ImmutableArray<NamedTypeSymbol> _baseExtensionTypes;

        internal SynthesizedExtensionMarker(SourceMemberContainerTypeSymbol containingType,
            TypeSymbol underlyingType, ImmutableArray<NamedTypeSymbol> baseExtensionTypes, BindingDiagnosticBag diagnostics)
            : base(containingType, WellKnownMemberNames.ExtensionMarkerMethodName, containingType.Locations[0],
                (CSharpSyntaxNode)containingType.SyntaxReferences[0].GetSyntax(), MethodKind.Ordinary,
                isIterator: false, isExtensionMethod: false, isReadOnly: false, hasBody: true,
                isNullableAnalysisEnabled: false, diagnostics)
        {
            _underlyingType = underlyingType;
            _baseExtensionTypes = baseExtensionTypes;
        }

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters, bool IsVararg, ImmutableArray<TypeParameterConstraintClause> DeclaredConstraintsForOverrideOrImplementation)
            MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics)
        {
            var returnType = TypeWithAnnotations.Create(isNullableEnabled: false, DeclaringCompilation.GetSpecialType(SpecialType.System_Void));

            var parameters = ArrayBuilder<ParameterSymbol>.GetInstance(_baseExtensionTypes.Length);
            parameters.Add(makeParameter(0, _underlyingType, locations));
            for (int i = 0; i < _baseExtensionTypes.Length; i++)
            {
                parameters.Add(makeParameter(i + 1, _baseExtensionTypes[i], locations));
            }

            return (ReturnType: returnType,
                    Parameters: parameters.ToImmutableAndFree(),
                    IsVararg: false,
                    DeclaredConstraintsForOverrideOrImplementation: ImmutableArray<TypeParameterConstraintClause>.Empty);

            SourceSimpleParameterSymbol makeParameter(int ordinal, TypeSymbol parameterType, ImmutableArray<Location> locations)
            {
                return new SourceSimpleParameterSymbol(
                    this,
                    TypeWithAnnotations.Create(isNullableEnabled: false, parameterType),
                    ordinal,
                    RefKind.None,
                    ScopedKind.None,
                    name: "",
                    locations);
            }
        }

        protected override DeclarationModifiers MakeDeclarationModifiers(DeclarationModifiers allowedModifiers, BindingDiagnosticBag diagnostics)
            => DeclarationModifiers.Private | DeclarationModifiers.Static;

        protected override int GetParameterCountFromSyntax() => _baseExtensionTypes.Length + 1;

        internal sealed override bool SynthesizesLoweredBoundBody => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CloseMethod(F.Return());
        }

        protected override ImmutableArray<TypeParameterSymbol> MakeTypeParameters(CSharpSyntaxNode node, BindingDiagnosticBag diagnostics)
            => ImmutableArray<TypeParameterSymbol>.Empty;

        protected override void CompleteAsyncMethodChecksBetweenStartAndFinish()
        {
        }

        public override string? GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default)
            => null;

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
            => OneOrMany.Create(default(SyntaxList<AttributeListSyntax>));

        protected override void ExtensionMethodChecks(BindingDiagnosticBag diagnostics)
        {
        }

        protected override MethodSymbol? FindExplicitlyImplementedMethod(BindingDiagnosticBag diagnostics)
            => null;

        protected override void CheckConstraintsForExplicitInterfaceType(ConversionsBase conversions, BindingDiagnosticBag diagnostics)
        {
        }

        protected override void PartialMethodChecks(BindingDiagnosticBag diagnostics)
        {
        }

        public override ImmutableArray<ImmutableArray<TypeWithAnnotations>> GetTypeParameterConstraintTypes()
            => ImmutableArray<ImmutableArray<TypeWithAnnotations>>.Empty;

        public override ImmutableArray<TypeParameterConstraintKind> GetTypeParameterConstraintKinds()
            => ImmutableArray<TypeParameterConstraintKind>.Empty;

        internal override ExecutableCodeBinder TryGetBodyBinder(BinderFactory? binderFactoryOpt = null, bool ignoreAccessibility = false)
            => throw ExceptionUtilities.Unreachable();

        protected override bool HasAnyBody => true;

        protected override SourceMemberMethodSymbol? BoundAttributesSource => null;

        protected override Location ReturnTypeLocation => Locations[0];

        protected override TypeSymbol? ExplicitInterfaceType => null;

        internal override bool IsExpressionBodied => false;

        public override bool IsVararg => false;

        public override RefKind RefKind => RefKind.None;

        internal override bool GenerateDebugInfo => false;

        public sealed override bool IsImplicitlyDeclared => true;
    }
}
