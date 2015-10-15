// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationEventSymbol : CodeGenerationSymbol, IEventSymbol
    {
        public ITypeSymbol Type { get; }

        public bool IsWindowsRuntimeEvent
        {
            get
            {
                return false;
            }
        }

        public ImmutableArray<IEventSymbol> ExplicitInterfaceImplementations { get; }

        public IMethodSymbol AddMethod { get; }
        public IMethodSymbol RemoveMethod { get; }
        public IMethodSymbol RaiseMethod { get; }
        public IList<IParameterSymbol> ParameterList { get; }

        public CodeGenerationEventSymbol(
            INamedTypeSymbol containingType,
            IList<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol type,
            IEventSymbol explicitInterfaceSymbolOpt,
            string name,
            IMethodSymbol addMethod,
            IMethodSymbol removeMethod,
            IMethodSymbol raiseMethod,
            IList<IParameterSymbol> parameterList)
            : base(containingType, attributes, declaredAccessibility, modifiers, name)
        {
            this.Type = type;
            this.ExplicitInterfaceImplementations = explicitInterfaceSymbolOpt == null
                ? ImmutableArray.Create<IEventSymbol>()
                : ImmutableArray.Create(explicitInterfaceSymbolOpt);
            this.AddMethod = addMethod;
            this.RemoveMethod = removeMethod;
            this.RaiseMethod = raiseMethod;
            this.ParameterList = parameterList;
        }

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationEventSymbol(
                this.ContainingType, this.GetAttributes(), this.DeclaredAccessibility,
                this.Modifiers, this.Type, this.ExplicitInterfaceImplementations.FirstOrDefault(),
                this.Name, this.AddMethod, this.RemoveMethod, this.RaiseMethod, this.ParameterList);
        }

        public override SymbolKind Kind
        {
            get
            {
                return SymbolKind.Event;
            }
        }

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitEvent(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitEvent(this);
        }

        public new IEventSymbol OriginalDefinition
        {
            get
            {
                return this;
            }
        }

        public IEventSymbol OverriddenEvent
        {
            get
            {
                return null;
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
