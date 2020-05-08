// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
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

            public IAssemblySymbol ContainingAssembly => FirstContainer<IAssemblySymbol>();
            public IModuleSymbol ContainingModule => FirstContainer<IModuleSymbol>();
            public INamedTypeSymbol ContainingType => FirstContainer<INamedTypeSymbol>();
            public INamespaceSymbol ContainingNamespace => FirstContainer<INamespaceSymbol>();

            private TSymbol FirstContainer<TSymbol>() where TSymbol : ISymbol
            {
                for (var current = this.ContainingSymbol; current != null; current = current.ContainingSymbol)
                {
                    if (current is TSymbol symbol)
                        return symbol;
                }

                return default;
            }

            #region default implementation

            public virtual bool CanBeReferencedByName => throw new NotImplementedException();
            public virtual bool Equals(ISymbol other, SymbolEqualityComparer equalityComparer) => throw new NotImplementedException();
            public virtual bool Equals(ISymbol other) => throw new NotImplementedException();
            public virtual bool HasUnsupportedMetadata => throw new NotImplementedException();
            public virtual bool IsDefinition => throw new NotImplementedException();
            public virtual bool IsImplicitlyDeclared => throw new NotImplementedException();
            public virtual ImmutableArray<AttributeData> GetAttributes() => throw new NotImplementedException();
            public virtual ImmutableArray<Location> Locations => throw new NotImplementedException();
            public virtual ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat format = null) => throw new NotImplementedException();
            public virtual ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null) => throw new NotImplementedException();
            public virtual ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => throw new NotImplementedException();
            public virtual ISymbol ContainingSymbol => throw new NotImplementedException();
            public virtual ISymbol OriginalDefinition => throw new NotImplementedException();
            public virtual string GetDocumentationCommentId() => throw new NotImplementedException();
            public virtual string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public virtual string MetadataName => throw new NotImplementedException();
            public virtual string Name => throw new NotImplementedException();
            public virtual string ToDisplayString(SymbolDisplayFormat format = null) => throw new NotImplementedException();
            public virtual string ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null) => throw new NotImplementedException();

            #endregion
        }
    }
}
