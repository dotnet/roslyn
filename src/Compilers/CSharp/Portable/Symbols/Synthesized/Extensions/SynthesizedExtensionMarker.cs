// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
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
    internal sealed class SynthesizedExtensionMarker : SourceMemberMethodSymbol
    {
        private readonly TypeSymbol _returnType;
        private readonly ImmutableArray<ParameterSymbol> _parameters;

        internal SynthesizedExtensionMarker(SourceMemberContainerTypeSymbol containingType,
            TypeSymbol underlyingType, ImmutableArray<NamedTypeSymbol> baseExtensionTypes, BindingDiagnosticBag diagnostics)
            : base(containingType, syntaxReferenceOpt: containingType.SyntaxReferences[0], containingType.Locations[0], isIterator: false)
        {
            this.MakeFlags(
                MethodKind.Ordinary,
                DeclarationModifiers.Static | DeclarationModifiers.Private,
                returnsVoid: true,
                isExtensionMethod: false,
                isNullableAnalysisEnabled: false,
                isMetadataVirtualIgnoringModifiers: false);

            _returnType = Binder.GetSpecialType(DeclaringCompilation, SpecialType.System_Void, containingType.Locations[0], diagnostics);

            var parameters = ArrayBuilder<ParameterSymbol>.GetInstance(baseExtensionTypes.Length);
            parameters.Add(makeParameter(0, underlyingType));
            for (int i = 0; i < baseExtensionTypes.Length; i++)
            {
                parameters.Add(makeParameter(i + 1, baseExtensionTypes[i]));
            }

            _parameters = parameters.ToImmutableAndFree();

            return;

            ParameterSymbol makeParameter(int ordinal, TypeSymbol parameterType)
            {
                return SynthesizedParameterSymbol.Create(container: this,
                    TypeWithAnnotations.Create(isNullableEnabled: false, parameterType),
                    ordinal, RefKind.None);
            }
        }

        public override string Name => WellKnownMemberNames.ExtensionMarkerMethodName;

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes => default;

        public override bool IsVararg => false;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        internal override int ParameterCount => _parameters.Length;

        public override ImmutableArray<ParameterSymbol> Parameters => _parameters;

        public override RefKind RefKind => RefKind.None;

        public override TypeWithAnnotations ReturnTypeWithAnnotations => TypeWithAnnotations.Create(_returnType);

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public sealed override bool IsImplicitlyDeclared
            => throw ExceptionUtilities.Unreachable(); // PROTOTYPE

        internal sealed override bool GenerateDebugInfo => false;

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
            => throw ExceptionUtilities.Unreachable(); // PROTOTYPE

        protected override void MethodChecks(BindingDiagnosticBag diagnostics)
            => throw ExceptionUtilities.Unreachable();

        internal override bool IsExpressionBodied
            => throw ExceptionUtilities.Unreachable();

        public override ImmutableArray<ImmutableArray<TypeWithAnnotations>> GetTypeParameterConstraintTypes()
            => throw ExceptionUtilities.Unreachable();

        public override ImmutableArray<TypeParameterConstraintKind> GetTypeParameterConstraintKinds()
            => throw ExceptionUtilities.Unreachable();

        protected override object MethodChecksLockObject
            => throw ExceptionUtilities.Unreachable();

        internal override ExecutableCodeBinder TryGetBodyBinder(BinderFactory? binderFactoryOpt = null, bool ignoreAccessibility = false)
            => throw ExceptionUtilities.Unreachable();

        internal override bool IsDefinedInSourceTree(SyntaxTree tree, TextSpan? definedWithinSpan, CancellationToken cancellationToken)
            => throw new System.NotImplementedException("PROTOTYPE"); // PROTOTYPE

        internal sealed override bool SynthesizesLoweredBoundBody
            => throw ExceptionUtilities.Unreachable(); // PROTOTYPE

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CloseMethod(F.Return());
        }

        public override string? GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default)
            => null;

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
            => OneOrMany.Create(default(SyntaxList<AttributeListSyntax>));

        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, BindingDiagnosticBag diagnostics)
            => throw ExceptionUtilities.Unreachable();
    }
}
