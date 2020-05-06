// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal static partial class CodeGenerator
    {
        private class Symbol : ISymbol
        {
            public virtual SymbolKind Kind => throw new System.NotImplementedException();

            public virtual string Language => throw new System.NotImplementedException();

            public virtual string Name => throw new System.NotImplementedException();

            public virtual string MetadataName => throw new System.NotImplementedException();

            public virtual ISymbol ContainingSymbol => throw new System.NotImplementedException();

            public virtual IAssemblySymbol ContainingAssembly => throw new System.NotImplementedException();

            public virtual IModuleSymbol ContainingModule => throw new System.NotImplementedException();

            public virtual INamedTypeSymbol ContainingType => throw new System.NotImplementedException();

            public virtual INamespaceSymbol ContainingNamespace => throw new System.NotImplementedException();

            public virtual bool IsDefinition => throw new System.NotImplementedException();

            public virtual bool IsStatic => throw new System.NotImplementedException();

            public virtual bool IsVirtual => throw new System.NotImplementedException();

            public virtual bool IsOverride => throw new System.NotImplementedException();

            public virtual bool IsAbstract => throw new System.NotImplementedException();

            public virtual bool IsSealed => throw new System.NotImplementedException();

            public virtual bool IsExtern => throw new System.NotImplementedException();

            public virtual bool IsImplicitlyDeclared => throw new System.NotImplementedException();

            public virtual bool CanBeReferencedByName => throw new System.NotImplementedException();

            public virtual ImmutableArray<Location> Locations => throw new System.NotImplementedException();

            public virtual ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => throw new System.NotImplementedException();

            public virtual Accessibility DeclaredAccessibility => throw new System.NotImplementedException();

            public virtual ISymbol OriginalDefinition => throw new System.NotImplementedException();

            public virtual bool HasUnsupportedMetadata => throw new System.NotImplementedException();

            public virtual void Accept(SymbolVisitor visitor)
            {
                throw new System.NotImplementedException();
            }

            public virtual TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            {
                throw new System.NotImplementedException();
            }

            public virtual bool Equals([NotNullWhen(true)] ISymbol other, SymbolEqualityComparer equalityComparer)
            {
                throw new System.NotImplementedException();
            }

            public virtual bool Equals([AllowNull] ISymbol other)
            {
                throw new System.NotImplementedException();
            }

            public virtual ImmutableArray<AttributeData> GetAttributes()
            {
                throw new System.NotImplementedException();
            }

            public virtual string GetDocumentationCommentId()
            {
                throw new System.NotImplementedException();
            }

            public virtual string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default)
            {
                throw new System.NotImplementedException();
            }

            public virtual ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat format = null)
            {
                throw new System.NotImplementedException();
            }

            public virtual string ToDisplayString(SymbolDisplayFormat format = null)
            {
                throw new System.NotImplementedException();
            }

            public virtual ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
            {
                throw new System.NotImplementedException();
            }

            public virtual string ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
