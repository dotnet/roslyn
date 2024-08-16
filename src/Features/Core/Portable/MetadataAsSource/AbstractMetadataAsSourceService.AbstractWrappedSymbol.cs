// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

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

        public int MetadataToken => _symbol.MetadataToken;

        public string Name => _symbol.Name;

        public ISymbol OriginalDefinition => _symbol.OriginalDefinition;

        public bool HasUnsupportedMetadata => _symbol.HasUnsupportedMetadata;

        public void Accept(SymbolVisitor visitor)
            => _symbol.Accept(visitor);

        public TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            => _symbol.Accept(visitor);

        public TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
            => _symbol.Accept(visitor, argument);

        public ImmutableArray<AttributeData> GetAttributes()
            => _symbol.GetAttributes();

        public string GetDocumentationCommentId()
            => _symbol.GetDocumentationCommentId();

        public string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default)
            => _symbol.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);

        public ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat format = null)
            => _symbol.ToDisplayParts(format);

        public string ToDisplayString(SymbolDisplayFormat format = null)
            => _symbol.ToDisplayString(format);

        public string ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
            => _symbol.ToMinimalDisplayString(semanticModel, position, format);

        public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
            => _symbol.ToMinimalDisplayParts(semanticModel, position, format);

        public bool Equals(ISymbol other)
            => Equals((object)other);

        public bool Equals(ISymbol other, SymbolEqualityComparer equalityComparer)
            => Equals(other);
    }
}
