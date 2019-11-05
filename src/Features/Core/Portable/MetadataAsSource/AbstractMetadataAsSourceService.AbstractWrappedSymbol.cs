// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal partial class AbstractMetadataAsSourceService
    {
        private abstract class AbstractWrappedSymbol : ISymbol
        {
            private readonly ISymbol _symbol;
            protected readonly bool CanImplementImplicitly;
            protected readonly IDocumentationCommentFormattingService DocCommentFormattingService;

            protected AbstractWrappedSymbol(ISymbol symbol, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService)
            {
                _symbol = symbol;
                CanImplementImplicitly = canImplementImplicitly;
                DocCommentFormattingService = docCommentFormattingService;
            }

            public bool CanBeReferencedByName => _symbol.CanBeReferencedByName;

            public IAssemblySymbol ContainingAssembly => _symbol.ContainingAssembly;

            public IModuleSymbol ContainingModule => _symbol.ContainingModule;

            public INamespaceSymbol ContainingNamespace => _symbol.ContainingNamespace;

            public ISymbol ContainingSymbol => _symbol.ContainingSymbol;

            public INamedTypeSymbol ContainingType => _symbol.ContainingType;

            public Accessibility DeclaredAccessibility => _symbol.DeclaredAccessibility;

            public ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => _symbol.DeclaringSyntaxReferences;

            public bool IsAbstract => _symbol.IsAbstract;

            public bool IsDefinition => _symbol.IsDefinition;

            public bool IsExtern => _symbol.IsExtern;

            public bool IsImplicitlyDeclared => _symbol.IsImplicitlyDeclared;

            public bool IsOverride => _symbol.IsOverride;

            public bool IsSealed => _symbol.IsSealed;

            public bool IsStatic => _symbol.IsStatic;

            public bool IsVirtual => _symbol.IsVirtual;

            public SymbolKind Kind => _symbol.Kind;

            public string Language => _symbol.Language;

            public ImmutableArray<Location> Locations => _symbol.Locations;

            public string MetadataName => _symbol.MetadataName;

            public string Name => _symbol.Name;

            public ISymbol OriginalDefinition => _symbol.OriginalDefinition;

            public bool HasUnsupportedMetadata => _symbol.HasUnsupportedMetadata;

            public void Accept(SymbolVisitor visitor)
            {
                _symbol.Accept(visitor);
            }

            public TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            {
                return _symbol.Accept<TResult>(visitor);
            }

            public ImmutableArray<AttributeData> GetAttributes()
            {
                return _symbol.GetAttributes();
            }

            public string GetDocumentationCommentId()
            {
                return _symbol.GetDocumentationCommentId();
            }

            public string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default)
            {
                return _symbol.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
            }

            public ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat format = null)
            {
                return _symbol.ToDisplayParts(format);
            }

            public string ToDisplayString(SymbolDisplayFormat format = null)
            {
                return _symbol.ToDisplayString(format);
            }

            public string ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
            {
                return _symbol.ToMinimalDisplayString(semanticModel, position, format);
            }

            public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
            {
                return _symbol.ToMinimalDisplayParts(semanticModel, position, format);
            }

            public bool Equals(ISymbol other)
            {
                return Equals((object)other);
            }

            public bool Equals(ISymbol other, SymbolEqualityComparer equalityComparer)
            {
                return Equals(other);
            }
        }
    }
}
