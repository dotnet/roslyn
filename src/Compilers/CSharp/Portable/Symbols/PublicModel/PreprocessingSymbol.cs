// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class PreprocessingSymbol : IPreprocessingSymbol
    {
        private readonly string _name;

        internal PreprocessingSymbol(string name)
        {
            _name = name;
        }

        ISymbol ISymbol.OriginalDefinition => this;

        ISymbol ISymbol.ContainingSymbol => null;

        INamedTypeSymbol ISymbol.ContainingType => null;

        public sealed override int GetHashCode()
        {
            return this._name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            PreprocessingSymbol other = obj as PreprocessingSymbol;

            return (object)other != null &&
                this._name.Equals(other._name);
        }

        bool IEquatable<ISymbol>.Equals(ISymbol other)
        {
            return this.Equals(other);
        }

        bool ISymbol.Equals(ISymbol other, CodeAnalysis.SymbolEqualityComparer equalityComparer)
        {
            return this.Equals(other);
        }

        ImmutableArray<Location> ISymbol.Locations => ImmutableArray<Location>.Empty;

        ImmutableArray<SyntaxReference> ISymbol.DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        ImmutableArray<AttributeData> ISymbol.GetAttributes() => ImmutableArray<AttributeData>.Empty;

        Accessibility ISymbol.DeclaredAccessibility => Accessibility.NotApplicable;

        void ISymbol.Accept(SymbolVisitor visitor) => throw new System.NotSupportedException();

        TResult ISymbol.Accept<TResult>(SymbolVisitor<TResult> visitor) => throw new System.NotSupportedException();

        string ISymbol.GetDocumentationCommentId() => null;

        string ISymbol.GetDocumentationCommentXml(CultureInfo preferredCulture, bool expandIncludes, CancellationToken cancellationToken) => null;

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
            return SymbolDisplay.ToMinimalDisplayString(this, Symbol.GetCSharpSemanticModel(semanticModel), position, format);
        }

        ImmutableArray<SymbolDisplayPart> ISymbol.ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat format)
        {
            return SymbolDisplay.ToMinimalDisplayParts(this, Symbol.GetCSharpSemanticModel(semanticModel), position, format);
        }

        SymbolKind ISymbol.Kind => SymbolKind.Preprocessing;

        string ISymbol.Language => LanguageNames.CSharp;

        string ISymbol.Name => _name;

        string ISymbol.MetadataName => _name;

        IAssemblySymbol ISymbol.ContainingAssembly => null;

        IModuleSymbol ISymbol.ContainingModule => null;

        INamespaceSymbol ISymbol.ContainingNamespace => null;

        bool ISymbol.IsDefinition => true;

        bool ISymbol.IsStatic => false;

        bool ISymbol.IsVirtual => false;

        bool ISymbol.IsOverride => false;

        bool ISymbol.IsAbstract => false;

        bool ISymbol.IsSealed => false;

        bool ISymbol.IsExtern => false;

        bool ISymbol.IsImplicitlyDeclared => false;

        bool ISymbol.CanBeReferencedByName => SyntaxFacts.IsValidIdentifier(_name) && !SyntaxFacts.ContainsDroppedIdentifierCharacters(_name);

        bool ISymbol.HasUnsupportedMetadata => false;

        public sealed override string ToString()
        {
            return SymbolDisplay.ToDisplayString(this);
        }
    }
}
