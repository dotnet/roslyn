// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        private abstract class Symbol : ISymbol
        {
            public abstract SymbolKind Kind { get; }
            public abstract void Accept(SymbolVisitor visitor);
            public abstract TResult Accept<TResult>(SymbolVisitor<TResult> visitor);

            public string Language => "SourceGenerator";

            public virtual Accessibility DeclaredAccessibility => Accessibility.NotApplicable;
            public virtual SymbolModifiers Modifiers => SymbolModifiers.None;

            public virtual bool IsStatic => (Modifiers & SymbolModifiers.Static) != 0;
            public virtual bool IsVirtual => (Modifiers & SymbolModifiers.Virtual) != 0;
            public virtual bool IsOverride => (Modifiers & SymbolModifiers.Override) != 0;
            public virtual bool IsAbstract => (Modifiers & SymbolModifiers.Abstract) != 0;
            public virtual bool IsSealed => (Modifiers & SymbolModifiers.Sealed) != 0;
            public virtual bool IsExtern => (Modifiers & SymbolModifiers.Extern) != 0;

            #region default implementation

            public virtual string Name => throw new System.NotImplementedException();

            public virtual string MetadataName => throw new System.NotImplementedException();

            public virtual ISymbol ContainingSymbol => throw new System.NotImplementedException();

            public virtual IAssemblySymbol ContainingAssembly => throw new System.NotImplementedException();

            public virtual IModuleSymbol ContainingModule => throw new System.NotImplementedException();

            public virtual INamedTypeSymbol ContainingType => throw new System.NotImplementedException();

            public virtual INamespaceSymbol ContainingNamespace => throw new System.NotImplementedException();

            public virtual bool IsDefinition => throw new System.NotImplementedException();

            public virtual bool IsImplicitlyDeclared => throw new System.NotImplementedException();

            public virtual bool CanBeReferencedByName => throw new System.NotImplementedException();

            public virtual ImmutableArray<Location> Locations => throw new System.NotImplementedException();

            public virtual ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => throw new System.NotImplementedException();

            public virtual ISymbol OriginalDefinition => throw new System.NotImplementedException();

            public virtual bool HasUnsupportedMetadata => throw new System.NotImplementedException();

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

            #endregion
        }
    }
}
