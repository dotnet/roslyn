// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class PreprocessingSymbol : IPreprocessingSymbol
    {
        private readonly string _name;
        private readonly IAssemblySymbol _assembly;
        private readonly IModuleSymbol _module;

        internal PreprocessingSymbol(string name, IAssemblySymbol assembly, IModuleSymbol module)
        {
            _name = name;
            _assembly = assembly;
            _module = module;
        }

        ISymbol ISymbol.OriginalDefinition => this;

        ISymbol? ISymbol.ContainingSymbol => null;

        INamedTypeSymbol? ISymbol.ContainingType => null;

        public sealed override int GetHashCode()
        {
            return this._name.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is not IPreprocessingSymbol other)
            {
                return false;
            }

            if (obj is PreprocessingSymbol csharpPreprocessingSymbol)
            {
                return _name == csharpPreprocessingSymbol._name;
            }

            // If we do not encounter a C# preprocessing symbol, we still
            // compare against the symbol's name directly.
            return _name == other.Name;
        }

        bool IEquatable<ISymbol?>.Equals(ISymbol? other)
        {
            return this.Equals(other);
        }

        bool ISymbol.Equals(ISymbol? other, CodeAnalysis.SymbolEqualityComparer equalityComparer)
        {
            return this.Equals(other);
        }

        ImmutableArray<Location> ISymbol.Locations => ImmutableArray<Location>.Empty;

        ImmutableArray<SyntaxReference> ISymbol.DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        ImmutableArray<AttributeData> ISymbol.GetAttributes() => ImmutableArray<AttributeData>.Empty;

        Accessibility ISymbol.DeclaredAccessibility => Accessibility.NotApplicable;

        void ISymbol.Accept(SymbolVisitor visitor)
        {
            throw new NotSupportedException();
        }

        TResult ISymbol.Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            throw new NotSupportedException();
        }

        TResult ISymbol.Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            throw new NotSupportedException();
        }

        string? ISymbol.GetDocumentationCommentId() => null;

        string? ISymbol.GetDocumentationCommentXml(CultureInfo? preferredCulture, bool expandIncludes, CancellationToken cancellationToken) => null;

        string ISymbol.ToDisplayString(SymbolDisplayFormat? format)
        {
            return _name;
        }

        ImmutableArray<SymbolDisplayPart> ISymbol.ToDisplayParts(SymbolDisplayFormat? format)
        {
            return ToDisplayParts();
        }

        string ISymbol.ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat? format)
        {
            return _name;
        }

        ImmutableArray<SymbolDisplayPart> ISymbol.ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat? format)
        {
            return ToDisplayParts();
        }

        private ImmutableArray<SymbolDisplayPart> ToDisplayParts()
        {
            var part = new SymbolDisplayPart(SymbolDisplayPartKind.PreprocessingName, this, _name);
            return ImmutableArray.Create(part);
        }

        SymbolKind ISymbol.Kind => SymbolKind.Preprocessing;

        string ISymbol.Language => LanguageNames.CSharp;

        string ISymbol.Name => _name;

        string ISymbol.MetadataName => _name;

        int ISymbol.MetadataToken => 0;

        IAssemblySymbol? ISymbol.ContainingAssembly => _assembly;

        IModuleSymbol? ISymbol.ContainingModule => _module;

        INamespaceSymbol? ISymbol.ContainingNamespace => null;

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
