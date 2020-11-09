// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Metadata;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    /// <summary>
    /// Describes a conversion operator method from containingType to returnType as if it were defined like so:
    /// <code>
    /// public class ContainingType
    /// {
    ///     public static explicit operator ReturnType(ContainingType value) => ...;
    /// }
    /// </code>
    /// </summary>
    internal class BuiltinOperatorMethodSymbol : IMethodSymbol
    {
        private static readonly SymbolDisplayFormat s_displayFormat = new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        public ITypeSymbol ReturnType { get; }
        public INamedTypeSymbol ContainingType { get; }
        public ImmutableArray<IParameterSymbol> Parameters { get; }

        public BuiltinOperatorMethodSymbol(ITypeSymbol returnType, INamedTypeSymbol containingType)
        {
            ReturnType = returnType;
            Parameters = ImmutableArray.Create<IParameterSymbol>(new BuiltinOperatorParameterSymbol(containingType));
            ContainingType = containingType;
        }

        public void Accept(SymbolVisitor visitor)
            => visitor.VisitMethod(this);

        [return: MaybeNull]
        public TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            => visitor.VisitMethod(this);

        public string? GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default)
        {
            // Explicit conversion of <see cref="T:{0}"/> to <see cref="T:{1}"/>.
            var template = @$"
<summary>
    {string.Format(CSharpFeaturesResources.Explicit_conversion_of_0_to_1, CreateSeeTag(ContainingType), CreateSeeTag(ReturnType))}
</summary>
";

            return template;

            static string CreateSeeTag(ISymbol symbol)
            {
                return $@"<see cref=""T:{symbol.ToDisplayParts(s_displayFormat)}""/>";
            }
        }

        public string? GetDocumentationCommentId() => null;

        public ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat? format = null)
            => SymbolDisplay.ToDisplayParts(this, format);

        public string ToDisplayString(SymbolDisplayFormat? format = null)
            => SymbolDisplay.ToDisplayString(this, format);

        public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat? format = null)
            => SymbolDisplay.ToMinimalDisplayParts(this, semanticModel, position, format);

        public string ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat? format = null)
            => SymbolDisplay.ToMinimalDisplayString(this, semanticModel, position, format);

        public bool Equals([NotNullWhen(true)] ISymbol? other, SymbolEqualityComparer equalityComparer)
            => other is BuiltinOperatorMethodSymbol otherMethod &&
               ReturnType.Equals(otherMethod.ReturnType, equalityComparer) &&
               Parameters[0].Equals(otherMethod.Parameters[0], equalityComparer);

        public bool Equals([AllowNull] ISymbol? other)
            => Equals(other, SymbolEqualityComparer.Default);

        #region IMethodSymbol implementation returning constants

        public MethodKind MethodKind => MethodKind.Conversion;

        public int Arity => 0;

        public bool IsGenericMethod => false;

        public bool IsExtensionMethod => false;

        public bool IsAsync => false;

        public bool IsVararg => false;

        public bool IsCheckedBuiltin => true;

        public bool HidesBaseMethodsByName => false;

        public bool ReturnsVoid => false;

        public bool ReturnsByRef => false;

        public bool ReturnsByRefReadonly => false;

        public RefKind RefKind => RefKind.None;

        public NullableAnnotation ReturnNullableAnnotation => NullableAnnotation.None;

        public ImmutableArray<ITypeSymbol> TypeArguments => ImmutableArray<ITypeSymbol>.Empty;

        public ImmutableArray<NullableAnnotation> TypeArgumentNullableAnnotations => ImmutableArray<NullableAnnotation>.Empty;

        public ImmutableArray<ITypeParameterSymbol> TypeParameters => ImmutableArray<ITypeParameterSymbol>.Empty;

        public IMethodSymbol ConstructedFrom => this;

        public bool IsReadOnly => false;

        public bool IsInitOnly => false;

        public IMethodSymbol OriginalDefinition => this;

        public IMethodSymbol? OverriddenMethod => null;

        public ITypeSymbol? ReceiverType => ContainingType;

        public NullableAnnotation ReceiverNullableAnnotation => NullableAnnotation.None;

        public IMethodSymbol? ReducedFrom => null;

        public ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<IMethodSymbol>.Empty;

        public ImmutableArray<CustomModifier> ReturnTypeCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public SignatureCallingConvention CallingConvention => SignatureCallingConvention.Default;

        public ISymbol? AssociatedSymbol => null;

        public IMethodSymbol? PartialDefinitionPart => null;

        public IMethodSymbol? PartialImplementationPart => null;

        public INamedTypeSymbol? AssociatedAnonymousDelegate => null;

        public bool IsConditional => false;

        public SymbolKind Kind => SymbolKind.Method;

        public string Language => LanguageNames.CSharp;

        public string Name => WellKnownMemberNames.ExplicitConversionName;

        public string MetadataName => WellKnownMemberNames.ExplicitConversionName;

        public ISymbol ContainingSymbol => ContainingType;

        public IAssemblySymbol ContainingAssembly => ContainingType.ContainingAssembly;

        public IModuleSymbol ContainingModule => ContainingType.ContainingModule;

        public INamespaceSymbol ContainingNamespace => ContainingType.ContainingNamespace;

        public bool IsDefinition => true;

        public bool IsStatic => true;

        public bool IsVirtual => false;

        public bool IsOverride => false;

        public bool IsAbstract => false;

        public bool IsSealed => false;

        public bool IsExtern => false;

        public bool IsImplicitlyDeclared => false;

        public bool CanBeReferencedByName => false;

        public ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;

        public ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public Accessibility DeclaredAccessibility => Accessibility.Public;

        public bool HasUnsupportedMetadata => false;

        ISymbol ISymbol.OriginalDefinition => this;

        public DllImportData? GetDllImportData() => null;

        #endregion

        #region not implemented yet

        public ImmutableArray<INamedTypeSymbol> UnmanagedCallingConventionTypes => throw new NotImplementedException();

        public IMethodSymbol Construct(params ITypeSymbol[] typeArguments)
            => throw new NotImplementedException();

        public IMethodSymbol Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<NullableAnnotation> typeArgumentNullableAnnotations)
            => throw new NotImplementedException();

        public ImmutableArray<AttributeData> GetAttributes() => ImmutableArray<AttributeData>.Empty;

        public ImmutableArray<AttributeData> GetReturnTypeAttributes() => ImmutableArray<AttributeData>.Empty;

        public ITypeSymbol? GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter)
            => throw new NotImplementedException();

        public IMethodSymbol? ReduceExtensionMethod(ITypeSymbol receiverType)
            => throw new NotImplementedException();

        #endregion
    }
}
