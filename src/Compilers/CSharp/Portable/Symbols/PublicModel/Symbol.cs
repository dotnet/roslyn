// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal abstract class Symbol : ISymbol
    {
        internal abstract CSharp.Symbol UnderlyingSymbol { get; }

        protected static ImmutableArray<TypeWithAnnotations> ConstructTypeArguments(ITypeSymbol[] typeArguments)
        {
            var builder = ArrayBuilder<TypeWithAnnotations>.GetInstance(typeArguments.Length);
            foreach (var typeArg in typeArguments)
            {
                var type = typeArg.EnsureCSharpSymbolOrNull(nameof(typeArguments));
                builder.Add(TypeWithAnnotations.Create(type, (typeArg?.NullableAnnotation.ToInternalAnnotation()).GetValueOrDefault()));
            }

            return builder.ToImmutableAndFree();
        }

        protected static ImmutableArray<TypeWithAnnotations> ConstructTypeArguments(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<CodeAnalysis.NullableAnnotation> typeArgumentNullableAnnotations)
        {
            if (typeArguments.IsDefault)
            {
                throw new ArgumentException(nameof(typeArguments));
            }

            int n = typeArguments.Length;
            if (!typeArgumentNullableAnnotations.IsDefault && typeArgumentNullableAnnotations.Length != n)
            {
                throw new ArgumentException(nameof(typeArgumentNullableAnnotations));
            }

            var builder = ArrayBuilder<TypeWithAnnotations>.GetInstance(n);
            for (int i = 0; i < n; i++)
            {
                var type = typeArguments[i].EnsureCSharpSymbolOrNull(nameof(typeArguments));
                var annotation = typeArgumentNullableAnnotations.IsDefault ? NullableAnnotation.Oblivious : typeArgumentNullableAnnotations[i].ToInternalAnnotation();
                builder.Add(TypeWithAnnotations.Create(type, annotation));
            }

            return builder.ToImmutableAndFree();
        }

        ISymbol ISymbol.OriginalDefinition
        {
            get
            {
                return UnderlyingSymbol.OriginalDefinition.GetPublicSymbol();
            }
        }

        ISymbol ISymbol.ContainingSymbol
        {
            get
            {
                return UnderlyingSymbol.ContainingSymbol.GetPublicSymbol();
            }
        }

        INamedTypeSymbol ISymbol.ContainingType
        {
            get
            {
                return UnderlyingSymbol.ContainingType.GetPublicSymbol();
            }
        }

        public sealed override int GetHashCode()
        {
            return UnderlyingSymbol.GetHashCode();
        }

        public sealed override bool Equals(object obj)
        {
            return this.Equals(obj as Symbol, CodeAnalysis.SymbolEqualityComparer.Default);
        }

        bool IEquatable<ISymbol>.Equals(ISymbol other)
        {
            return this.Equals(other as Symbol, CodeAnalysis.SymbolEqualityComparer.Default);
        }

        bool ISymbol.Equals(ISymbol other, CodeAnalysis.SymbolEqualityComparer equalityComparer)
        {
            return this.Equals(other as Symbol, equalityComparer);
        }

        protected bool Equals(Symbol other, CodeAnalysis.SymbolEqualityComparer equalityComparer)
        {
            return other is object && UnderlyingSymbol.Equals(other.UnderlyingSymbol, equalityComparer.CompareKind);
        }

        ImmutableArray<Location> ISymbol.Locations
        {
            get
            {
                return UnderlyingSymbol.Locations;
            }
        }

        ImmutableArray<SyntaxReference> ISymbol.DeclaringSyntaxReferences
        {
            get
            {
                return UnderlyingSymbol.DeclaringSyntaxReferences;
            }
        }

        ImmutableArray<AttributeData> ISymbol.GetAttributes()
        {
            return StaticCast<AttributeData>.From(UnderlyingSymbol.GetAttributes());
        }

        Accessibility ISymbol.DeclaredAccessibility
        {
            get
            {
                return UnderlyingSymbol.DeclaredAccessibility;
            }
        }

        void ISymbol.Accept(SymbolVisitor visitor)
        {
            Accept(visitor);
        }

        protected abstract void Accept(SymbolVisitor visitor);

        TResult ISymbol.Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return Accept(visitor);
        }

        protected abstract TResult Accept<TResult>(SymbolVisitor<TResult> visitor);

        string ISymbol.GetDocumentationCommentId()
        {
            return UnderlyingSymbol.GetDocumentationCommentId();
        }

        string ISymbol.GetDocumentationCommentXml(CultureInfo preferredCulture, bool expandIncludes, CancellationToken cancellationToken)
        {
            return UnderlyingSymbol.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        string ISymbol.ToDisplayString(SymbolDisplayFormat format)
        {
            return SymbolDisplay.ToDisplayString(this, format);
        }

        ImmutableArray<SymbolDisplayPart> ISymbol.ToDisplayParts(SymbolDisplayFormat format)
        {
            return SymbolDisplay.ToDisplayParts(this, format);
        }

        string ISymbol.ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat format)
        {
            return SymbolDisplay.ToMinimalDisplayString(this, GetCSharpSemanticModel(semanticModel), position, format);
        }

        ImmutableArray<SymbolDisplayPart> ISymbol.ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat format)
        {
            return SymbolDisplay.ToMinimalDisplayParts(this, GetCSharpSemanticModel(semanticModel), position, format);
        }

        internal static CSharpSemanticModel GetCSharpSemanticModel(SemanticModel semanticModel)
        {
            var csharpModel = semanticModel as CSharpSemanticModel;
            if (csharpModel == null)
            {
                throw new ArgumentException(CSharpResources.WrongSemanticModelType, LanguageNames.CSharp);
            }

            return csharpModel;
        }

        SymbolKind ISymbol.Kind => UnderlyingSymbol.Kind;

        string ISymbol.Language => LanguageNames.CSharp;

        string ISymbol.Name => UnderlyingSymbol.Name;

        string ISymbol.MetadataName => UnderlyingSymbol.MetadataName;

        IAssemblySymbol ISymbol.ContainingAssembly => UnderlyingSymbol.ContainingAssembly.GetPublicSymbol();

        IModuleSymbol ISymbol.ContainingModule => UnderlyingSymbol.ContainingModule.GetPublicSymbol();

        INamespaceSymbol ISymbol.ContainingNamespace => UnderlyingSymbol.ContainingNamespace.GetPublicSymbol();

        bool ISymbol.IsDefinition => UnderlyingSymbol.IsDefinition;

        bool ISymbol.IsStatic
        {
            get { return UnderlyingSymbol.IsStatic; }
        }

        bool ISymbol.IsVirtual
        {
            get { return UnderlyingSymbol.IsVirtual; }
        }

        bool ISymbol.IsOverride
        {
            get { return UnderlyingSymbol.IsOverride; }
        }

        bool ISymbol.IsAbstract
        {
            get
            {
                return UnderlyingSymbol.IsAbstract;
            }
        }

        bool ISymbol.IsSealed
        {
            get
            {
                return UnderlyingSymbol.IsSealed;
            }
        }

        bool ISymbol.IsExtern => UnderlyingSymbol.IsExtern;

        bool ISymbol.IsImplicitlyDeclared => UnderlyingSymbol.IsImplicitlyDeclared;

        bool ISymbol.CanBeReferencedByName => UnderlyingSymbol.CanBeReferencedByName;

        bool ISymbol.HasUnsupportedMetadata => UnderlyingSymbol.HasUnsupportedMetadata;

        public sealed override string ToString()
        {
            return SymbolDisplay.ToDisplayString(this);
        }
    }
}
