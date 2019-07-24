// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ReferencedSymbolTests : TestBase
    {
        [Fact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public void DebuggerDisplay_OneReference()
        {
            var referencedSymbol = CreateReferencedSymbol("Goo", 1);

            Assert.Equal("Goo, 1 ref", referencedSymbol.GetTestAccessor().GetDebuggerDisplay());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public void DebuggerDisplay_NoReferences()
        {
            var referencedSymbol = CreateReferencedSymbol("Goo", 0);

            Assert.Equal("Goo, 0 refs", referencedSymbol.GetTestAccessor().GetDebuggerDisplay());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public void DebuggerDisplay_TwoReferences()
        {
            var referencedSymbol = CreateReferencedSymbol("Goo", 2);

            Assert.Equal("Goo, 2 refs", referencedSymbol.GetTestAccessor().GetDebuggerDisplay());
        }

        private static ReferencedSymbol CreateReferencedSymbol(
            string symbolName, int referenceCount)
        {
            var symbol = new StubSymbol(symbolName);

            var locations = new List<ReferenceLocation>(capacity: referenceCount);
            for (var i = 0; i < referenceCount; i++)
            {
                locations.Add(new ReferenceLocation());
            }

            var referencedSymbol = new ReferencedSymbol(
                SymbolAndProjectId.Create(symbol, projectId: null), locations);
            return referencedSymbol;
        }

        private class StubSymbol : ISymbol
        {
            private readonly string _name;

            public StubSymbol(string name)
            {
                _name = name;
            }

            public bool CanBeReferencedByName
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public IAssemblySymbol ContainingAssembly
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public IModuleSymbol ContainingModule
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public INamespaceSymbol ContainingNamespace
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public ISymbol ContainingSymbol
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public INamedTypeSymbol ContainingType
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public Accessibility DeclaredAccessibility
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool HasUnsupportedMetadata
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsAbstract
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsDefinition
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsExtern
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsImplicitlyDeclared
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsOverride
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsSealed
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsStatic
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsVirtual
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public SymbolKind Kind
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string Language
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public ImmutableArray<Location> Locations
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string MetadataName
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string Name
            {
                get
                {
                    return _name;
                }
            }

            public ISymbol OriginalDefinition
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public void Accept(SymbolVisitor visitor)
            {
                throw new NotImplementedException();
            }

            public TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            {
                throw new NotImplementedException();
            }

            public ImmutableArray<AttributeData> GetAttributes()
            {
                throw new NotImplementedException();
            }

            public string GetDocumentationCommentId()
            {
                throw new NotImplementedException();
            }

            public string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public SymbolKey GetSymbolId()
            {
                throw new NotImplementedException();
            }

            public ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat format = null)
            {
                throw new NotImplementedException();
            }

            public string ToDisplayString(SymbolDisplayFormat format = null)
            {
                throw new NotImplementedException();
            }

            public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
            {
                throw new NotImplementedException();
            }

            public string ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
            {
                throw new NotImplementedException();
            }

            public bool Equals(ISymbol other)
            {
                return this.Equals((object)other);
            }

            public bool Equals(ISymbol other, SymbolEqualityComparer equalityComparer)
            {
                return equalityComparer.Equals(this, other);
            }
        }
    }
}
