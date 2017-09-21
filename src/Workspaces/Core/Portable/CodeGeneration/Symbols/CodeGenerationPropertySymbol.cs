// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationPropertySymbol : CodeGenerationSymbol, IPropertySymbol
    {
        public ITypeSymbol Type { get; }
        public bool ReturnsByRef { get; }
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
            bool returnsByRef,
            ImmutableArray<IPropertySymbol> explicitInterfaceImplementations,
            string name,
            bool isIndexer,
            ImmutableArray<IParameterSymbol> parametersOpt,
            IMethodSymbol getMethod,
            IMethodSymbol setMethod)
            : base(containingType, attributes, declaredAccessibility, modifiers, name)
        {
            this.Type = type;
            this.ReturnsByRef = returnsByRef;
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
                this.Modifiers, this.Type, this.ReturnsByRef, this.ExplicitInterfaceImplementations,
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

        public bool IsReadOnly
        {
            get
            {
                return this.GetMethod != null && this.SetMethod == null;
            }
        }

        public bool IsWriteOnly
        {
            get
            {
                return this.GetMethod == null && this.SetMethod != null;
            }
        }

        public new IPropertySymbol OriginalDefinition => this;

        public bool ReturnsByRefReadonly
        {
            get
            {
                return this.GetMethod != null && this.GetMethod.ReturnsByRefReadonly;
            }
        }

        public IPropertySymbol OverriddenProperty
        {
            get
            {
                return null;
            }
        }

        public bool IsWithEvents => false;

        public ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public ImmutableArray<CustomModifier> TypeCustomModifiers => ImmutableArray<CustomModifier>.Empty;
    }
}
