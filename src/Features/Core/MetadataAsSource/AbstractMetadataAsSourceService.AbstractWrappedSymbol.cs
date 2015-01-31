// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DocumentationCommentFormatting;

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
                this.CanImplementImplicitly = canImplementImplicitly;
                this.DocCommentFormattingService = docCommentFormattingService;
            }

            public bool CanBeReferencedByName
            {
                get
                {
                    return _symbol.CanBeReferencedByName;
                }
            }

            public IAssemblySymbol ContainingAssembly
            {
                get
                {
                    return _symbol.ContainingAssembly;
                }
            }

            public IModuleSymbol ContainingModule
            {
                get
                {
                    return _symbol.ContainingModule;
                }
            }

            public INamespaceSymbol ContainingNamespace
            {
                get
                {
                    return _symbol.ContainingNamespace;
                }
            }

            public ISymbol ContainingSymbol
            {
                get
                {
                    return _symbol.ContainingSymbol;
                }
            }

            public INamedTypeSymbol ContainingType
            {
                get
                {
                    return _symbol.ContainingType;
                }
            }

            public Accessibility DeclaredAccessibility
            {
                get
                {
                    return _symbol.DeclaredAccessibility;
                }
            }

            public ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            {
                get
                {
                    return _symbol.DeclaringSyntaxReferences;
                }
            }

            public bool IsAbstract
            {
                get
                {
                    return _symbol.IsAbstract;
                }
            }

            public bool IsDefinition
            {
                get
                {
                    return _symbol.IsDefinition;
                }
            }

            public bool IsExtern
            {
                get
                {
                    return _symbol.IsExtern;
                }
            }

            public bool IsImplicitlyDeclared
            {
                get
                {
                    return _symbol.IsImplicitlyDeclared;
                }
            }

            public bool IsOverride
            {
                get
                {
                    return _symbol.IsOverride;
                }
            }

            public bool IsSealed
            {
                get
                {
                    return _symbol.IsSealed;
                }
            }

            public bool IsStatic
            {
                get
                {
                    return _symbol.IsStatic;
                }
            }

            public bool IsVirtual
            {
                get
                {
                    return _symbol.IsVirtual;
                }
            }

            public SymbolKind Kind
            {
                get
                {
                    return _symbol.Kind;
                }
            }

            public string Language
            {
                get
                {
                    return _symbol.Language;
                }
            }

            public ImmutableArray<Location> Locations
            {
                get
                {
                    return _symbol.Locations;
                }
            }

            public string MetadataName
            {
                get
                {
                    return _symbol.MetadataName;
                }
            }

            public string Name
            {
                get
                {
                    return _symbol.Name;
                }
            }

            public ISymbol OriginalDefinition
            {
                get
                {
                    return _symbol.OriginalDefinition;
                }
            }

            public bool HasUnsupportedMetadata
            {
                get
                {
                    return _symbol.HasUnsupportedMetadata;
                }
            }

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

            public string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
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
                return this.Equals((object)other);
            }
        }
    }
}
