// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationPropertySymbol : CodeGenerationSymbol, IPropertySymbol
    {
        public ITypeSymbol Type { get; }
        public bool IsIndexer { get; }

        public ImmutableArray<IParameterSymbol> Parameters { get; }
        public ImmutableArray<IPropertySymbol> ExplicitInterfaceImplementations { get; }

        public IMethodSymbol GetMethod { get; }
        public IMethodSymbol SetMethod { get; }

        public CodeGenerationPropertySymbol(
            INamedTypeSymbol containingType,
            IList<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol type,
            IPropertySymbol explicitInterfaceSymbolOpt,
            string name,
            bool isIndexer,
            IList<IParameterSymbol> parametersOpt,
            IMethodSymbol getMethod,
            IMethodSymbol setMethod)
            : base(containingType, attributes, declaredAccessibility, modifiers, name)
        {
            this.Type = type;
            this.IsIndexer = isIndexer;
            this.Parameters = parametersOpt.AsImmutableOrEmpty();
            this.ExplicitInterfaceImplementations = explicitInterfaceSymbolOpt == null
                ? ImmutableArray.Create<IPropertySymbol>()
                : ImmutableArray.Create(explicitInterfaceSymbolOpt);
            this.GetMethod = getMethod;
            this.SetMethod = setMethod;
        }

        protected override CodeGenerationSymbol Clone()
        {
            var result = new CodeGenerationPropertySymbol(
                this.ContainingType, this.GetAttributes(), this.DeclaredAccessibility,
                this.Modifiers, this.Type, this.ExplicitInterfaceImplementations.FirstOrDefault(),
                this.Name, this.IsIndexer, this.Parameters.IsDefault ? null : (IList<IParameterSymbol>)this.Parameters,
                this.GetMethod, this.SetMethod);
            CodeGenerationPropertyInfo.Attach(result,
                CodeGenerationPropertyInfo.GetIsNew(this),
                CodeGenerationPropertyInfo.GetIsUnsafe(this),
                CodeGenerationPropertyInfo.GetInitializer(this));

            return result;
        }

        public override SymbolKind Kind
        {
            get
            {
                return SymbolKind.Property;
            }
        }

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitProperty(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitProperty(this);
        }

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

        public bool ReturnsByRef
        {
            get
            {
                return this.GetMethod != null && this.GetMethod.ReturnsByRef;
            }
        }

        public new IPropertySymbol OriginalDefinition
        {
            get
            {
                return this;
            }
        }

        public IPropertySymbol OverriddenProperty
        {
            get
            {
                return null;
            }
        }

        public bool IsWithEvents
        {
            get
            {
                return false;
            }
        }

        public ImmutableArray<CustomModifier> TypeCustomModifiers
        {
            get
            {
                return ImmutableArray.Create<CustomModifier>();
            }
        }
    }
}
