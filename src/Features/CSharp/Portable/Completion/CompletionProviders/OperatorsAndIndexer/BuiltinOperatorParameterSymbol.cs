// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class BuiltinOperatorParameterSymbol : IParameterSymbol
    {
        public BuiltinOperatorParameterSymbol(ITypeSymbol type, ISymbol containingSymbol)
        {
            Type = type;
            ContainingSymbol = containingSymbol;
        }

        public ITypeSymbol Type { get; }

        public ISymbol ContainingSymbol { get; }

        public bool Equals([NotNullWhen(true)] ISymbol? other, SymbolEqualityComparer equalityComparer)
        {
            if (other is BuiltinOperatorParameterSymbol otherParameter)
            {
                return Type.Equals(otherParameter.Type, equalityComparer);
            }

            return false;
        }

        public bool Equals([AllowNull] ISymbol other)
            => Equals(other, SymbolEqualityComparer.Default);

        public ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat? format = null)
            => SymbolDisplay.ToDisplayParts(this, format);

        public string ToDisplayString(SymbolDisplayFormat? format = null)
            => SymbolDisplay.ToDisplayString(this, format);

        public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat? format = null)
            => SymbolDisplay.ToMinimalDisplayParts(this, semanticModel, position, format);

        public string ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat? format = null)
            => SymbolDisplay.ToMinimalDisplayString(this, semanticModel, position, format);

        #region IParameterSymbol implementation returning constants
        public RefKind RefKind => RefKind.None;

        public bool IsParams => false;

        public bool IsOptional => false;

        public bool IsThis => false;

        public bool IsDiscard => false;

        public NullableAnnotation NullableAnnotation => NullableAnnotation.None;

        public ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public int Ordinal => 0;

        public bool HasExplicitDefaultValue => false;

        public object ExplicitDefaultValue => throw new InvalidOperationException();

        public IParameterSymbol OriginalDefinition => this;

        public SymbolKind Kind => SymbolKind.Parameter;

        public string Language => LanguageNames.CSharp;

        public string Name => "value";

        public string MetadataName => "value";

        public IAssemblySymbol ContainingAssembly => ContainingSymbol.ContainingAssembly;

        public IModuleSymbol ContainingModule => ContainingSymbol.ContainingModule;

        public INamedTypeSymbol ContainingType => ContainingSymbol.ContainingType;

        public INamespaceSymbol ContainingNamespace => ContainingSymbol.ContainingNamespace;

        public bool IsDefinition => true;

        public bool IsStatic => false;

        public bool IsVirtual => false;

        public bool IsOverride => false;

        public bool IsAbstract => false;

        public bool IsSealed => false;

        public bool IsExtern => false;

        public bool IsImplicitlyDeclared => false;

        public bool CanBeReferencedByName => true;

        public ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;

        public ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public Accessibility DeclaredAccessibility => Accessibility.NotApplicable;

        public bool HasUnsupportedMetadata => false;

        ISymbol ISymbol.OriginalDefinition => this;

        public void Accept(SymbolVisitor visitor)
            => visitor.VisitParameter(this);

        public TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            => visitor.VisitParameter(this);

        public ImmutableArray<AttributeData> GetAttributes() => ImmutableArray<AttributeData>.Empty;

        public string GetDocumentationCommentId()
            => "";

        public string? GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default)
            => "";

        #endregion
    }
}
