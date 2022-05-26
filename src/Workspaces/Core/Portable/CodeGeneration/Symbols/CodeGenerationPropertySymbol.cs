// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationPropertySymbol : CodeGenerationSymbol, IPropertySymbol
    {
        private readonly RefKind _refKind;
        public ITypeSymbol Type { get; }
        public NullableAnnotation NullableAnnotation => Type.NullableAnnotation;
        public bool IsIndexer { get; }

        public ImmutableArray<IParameterSymbol> Parameters { get; }
        public ImmutableArray<IPropertySymbol> ExplicitInterfaceImplementations { get; }

        public IMethodSymbol GetMethod { get; }
        public IMethodSymbol SetMethod { get; }

        public CodeGenerationPropertySymbol(
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol type,
            RefKind refKind,
            ImmutableArray<IPropertySymbol> explicitInterfaceImplementations,
            string name,
            bool isIndexer,
            ImmutableArray<IParameterSymbol> parametersOpt,
            IMethodSymbol getMethod,
            IMethodSymbol setMethod)
            : base(containingType?.ContainingAssembly, containingType, attributes, declaredAccessibility, modifiers, name)
        {
            this.Type = type;
            _refKind = refKind;
            this.IsIndexer = isIndexer;
            this.Parameters = parametersOpt.NullToEmpty();
            this.ExplicitInterfaceImplementations = explicitInterfaceImplementations.NullToEmpty();
            this.GetMethod = getMethod;
            this.SetMethod = setMethod;
        }

        protected override CodeGenerationSymbol Clone()
        {
            var result = new CodeGenerationPropertySymbol(
                this.ContainingType, this.GetAttributes(), this.DeclaredAccessibility,
                this.Modifiers, this.Type, this.RefKind, this.ExplicitInterfaceImplementations,
                this.Name, this.IsIndexer, this.Parameters,
                this.GetMethod, this.SetMethod);
            CodeGenerationPropertyInfo.Attach(result,
                CodeGenerationPropertyInfo.GetIsNew(this),
                CodeGenerationPropertyInfo.GetIsUnsafe(this),
                CodeGenerationPropertyInfo.GetInitializer(this));

            return result;
        }

        public override SymbolKind Kind => SymbolKind.Property;

        public override void Accept(SymbolVisitor visitor)
            => visitor.VisitProperty(this);

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            => visitor.VisitProperty(this);

        public override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitProperty(this, argument);

        public bool IsReadOnly => this.GetMethod != null && this.SetMethod == null;

        public bool IsWriteOnly => this.GetMethod == null && this.SetMethod != null;

        public new IPropertySymbol OriginalDefinition => this;

        public RefKind RefKind => _refKind;

        public bool ReturnsByRef => _refKind == RefKind.Ref;

        public bool ReturnsByRefReadonly => _refKind == RefKind.RefReadOnly;

        public IPropertySymbol OverriddenProperty => null;

        public bool IsWithEvents => false;

        public ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public ImmutableArray<CustomModifier> TypeCustomModifiers => ImmutableArray<CustomModifier>.Empty;
    }
}
