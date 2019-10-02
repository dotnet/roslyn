// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationEventSymbol : CodeGenerationSymbol, IEventSymbol
    {
        public ITypeSymbol Type { get; }
        public NullableAnnotation NullableAnnotation => Type.GetNullability();

        public ImmutableArray<IEventSymbol> ExplicitInterfaceImplementations { get; }

        public IMethodSymbol AddMethod { get; }
        public IMethodSymbol RemoveMethod { get; }
        public IMethodSymbol RaiseMethod { get; }

        public CodeGenerationEventSymbol(
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol type,
            ImmutableArray<IEventSymbol> explicitInterfaceImplementations,
            string name,
            IMethodSymbol addMethod,
            IMethodSymbol removeMethod,
            IMethodSymbol raiseMethod)
            : base(containingType, attributes, declaredAccessibility, modifiers, name)
        {
            this.Type = type;
            this.ExplicitInterfaceImplementations = explicitInterfaceImplementations.NullToEmpty();
            this.AddMethod = addMethod;
            this.RemoveMethod = removeMethod;
            this.RaiseMethod = raiseMethod;
        }

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationEventSymbol(
                this.ContainingType, this.GetAttributes(), this.DeclaredAccessibility,
                this.Modifiers, this.Type, this.ExplicitInterfaceImplementations,
                this.Name, this.AddMethod, this.RemoveMethod, this.RaiseMethod);
        }

        public override SymbolKind Kind => SymbolKind.Event;

        public override void Accept(SymbolVisitor visitor)
            => visitor.VisitEvent(this);

        [return: MaybeNull]
        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return visitor.VisitEvent(this);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        public new IEventSymbol OriginalDefinition => this;

        public bool IsWindowsRuntimeEvent => false;

        public IEventSymbol? OverriddenEvent => null;

        public ImmutableArray<CustomModifier> TypeCustomModifiers => ImmutableArray.Create<CustomModifier>();
    }
}
