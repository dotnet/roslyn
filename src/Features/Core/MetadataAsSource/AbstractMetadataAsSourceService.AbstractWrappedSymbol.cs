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
            private readonly ISymbol symbol;
            protected readonly bool CanImplementImplicitly;
            protected readonly IDocumentationCommentFormattingService DocCommentFormattingService;

            protected AbstractWrappedSymbol(ISymbol symbol, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService)
            {
                this.symbol = symbol;
                this.CanImplementImplicitly = canImplementImplicitly;
                this.DocCommentFormattingService = docCommentFormattingService;
            }

            public bool CanBeReferencedByName
            {
                get
                {
                    return this.symbol.CanBeReferencedByName;
                }
            }

            public IAssemblySymbol ContainingAssembly
            {
                get
                {
                    return this.symbol.ContainingAssembly;
                }
            }

            public IModuleSymbol ContainingModule
            {
                get
                {
                    return this.symbol.ContainingModule;
                }
            }

            public INamespaceSymbol ContainingNamespace
            {
                get
                {
                    return this.symbol.ContainingNamespace;
                }
            }

            public ISymbol ContainingSymbol
            {
                get
                {
                    return this.symbol.ContainingSymbol;
                }
            }

            public INamedTypeSymbol ContainingType
            {
                get
                {
                    return this.symbol.ContainingType;
                }
            }

            public Accessibility DeclaredAccessibility
            {
                get
                {
                    return this.symbol.DeclaredAccessibility;
                }
            }

            public ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            {
                get
                {
                    return this.symbol.DeclaringSyntaxReferences;
                }
            }

            public bool IsAbstract
            {
                get
                {
                    return this.symbol.IsAbstract;
                }
            }

            public bool IsDefinition
            {
                get
                {
                    return this.symbol.IsDefinition;
                }
            }

            public bool IsExtern
            {
                get
                {
                    return this.symbol.IsExtern;
                }
            }

            public bool IsImplicitlyDeclared
            {
                get
                {
                    return this.symbol.IsImplicitlyDeclared;
                }
            }

            public bool IsOverride
            {
                get
                {
                    return this.symbol.IsOverride;
                }
            }

            public bool IsSealed
            {
                get
                {
                    return this.symbol.IsSealed;
                }
            }

            public bool IsStatic
            {
                get
                {
                    return this.symbol.IsStatic;
                }
            }

            public bool IsVirtual
            {
                get
                {
                    return this.symbol.IsVirtual;
                }
            }

            public SymbolKind Kind
            {
                get
                {
                    return this.symbol.Kind;
                }
            }

            public string Language
            {
                get
                {
                    return this.symbol.Language;
                }
            }

            public ImmutableArray<Location> Locations
            {
                get
                {
                    return this.symbol.Locations;
                }
            }

            public string MetadataName
            {
                get
                {
                    return this.symbol.MetadataName;
                }
            }

            public string Name
            {
                get
                {
                    return this.symbol.Name;
                }
            }

            public ISymbol OriginalDefinition
            {
                get
                {
                    return this.symbol.OriginalDefinition;
                }
            }

            public bool HasUnsupportedMetadata
            {
                get
                {
                    return this.symbol.HasUnsupportedMetadata;
                }
            }

            public void Accept(SymbolVisitor visitor)
            {
                this.symbol.Accept(visitor);
            }

            public TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            {
                return this.symbol.Accept<TResult>(visitor);
            }

            public ImmutableArray<AttributeData> GetAttributes()
            {
                return this.symbol.GetAttributes();
            }

            public string GetDocumentationCommentId()
            {
                return this.symbol.GetDocumentationCommentId();
            }

            public string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
            {
                return this.symbol.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
            }

            public ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat format = null)
            {
                return this.symbol.ToDisplayParts(format);
            }

            public string ToDisplayString(SymbolDisplayFormat format = null)
            {
                return this.symbol.ToDisplayString(format);
            }

            public string ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
            {
                return this.symbol.ToMinimalDisplayString(semanticModel, position, format);
            }

            public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
            {
                return this.symbol.ToMinimalDisplayParts(semanticModel, position, format);
            }

            public bool Equals(ISymbol other)
            {
                return this.Equals((object)other);
            }
        }
    }
}
