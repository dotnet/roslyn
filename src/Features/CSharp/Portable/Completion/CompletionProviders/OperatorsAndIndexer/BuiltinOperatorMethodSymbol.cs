// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class BuiltinOperatorMethodSymbol : IMethodSymbol
    {
        public BuiltinOperatorMethodSymbol(ITypeSymbol returnType, INamedTypeSymbol containingType)
        {
            ReturnType = returnType;
            Parameters = new IParameterSymbol[]
            {
                new BuiltinOperatorParameterSymbol(containingType, containingType),
            }.ToImmutableArray();
            ContainingType = containingType;
        }

        public ITypeSymbol ReturnType { get; }

        public INamedTypeSymbol ContainingType { get; }

        public ImmutableArray<IParameterSymbol> Parameters { get; }

        public string? GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default)
            => string.Format(@"
<summary>
Defines an explicit conversion of <see cref=""{0}"" /> to a <see cref=""{1}"" />.
</summary>
", ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

        public ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat? format = null)
            => SymbolDisplay.ToDisplayParts(this, format);

        public string ToDisplayString(SymbolDisplayFormat? format = null)
            => SymbolDisplay.ToDisplayString(this, format);

        public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat? format = null)
            => SymbolDisplay.ToMinimalDisplayParts(this, semanticModel, position, format);

        public string ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat? format = null)
            => SymbolDisplay.ToMinimalDisplayString(this, semanticModel, position, format);

        public bool Equals([NotNullWhen(true)] ISymbol? other, SymbolEqualityComparer equalityComparer)
        {
            if (other is BuiltinOperatorMethodSymbol otherMethod)
            {
                return ReturnType.Equals(otherMethod.ReturnType, equalityComparer) &&
                    otherMethod.Parameters.Length == 1 &&
                    Parameters[0].Equals(otherMethod.Parameters[0], equalityComparer);
            }

            return false;
        }

        public bool Equals([AllowNull] ISymbol? other)
            => Equals(other, SymbolEqualityComparer.Default);


        #region IMethodSymbol implementation returning constants
        public MethodKind MethodKind => MethodKind.BuiltinOperator;

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

        public ImmutableArray<INamedTypeSymbol> CallingConventionTypes => ImmutableArray<INamedTypeSymbol>.Empty;

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

        public void Accept(SymbolVisitor visitor)
            => visitor.VisitMethod(this);

        public TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            => visitor.VisitMethod(this);

        public IMethodSymbol Construct(params ITypeSymbol[] typeArguments)
            => throw new NotImplementedException();

        public IMethodSymbol Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<NullableAnnotation> typeArgumentNullableAnnotations)
            => throw new NotImplementedException();

        public ImmutableArray<AttributeData> GetAttributes() => ImmutableArray<AttributeData>.Empty;

        public DllImportData? GetDllImportData() => null;

        public string? GetDocumentationCommentId() => null;

        public ImmutableArray<AttributeData> GetReturnTypeAttributes() => ImmutableArray<AttributeData>.Empty;

        public ITypeSymbol? GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter)
            => throw new NotImplementedException();

        public IMethodSymbol? ReduceExtensionMethod(ITypeSymbol receiverType)
            => throw new NotImplementedException();
        #endregion
    }
}
