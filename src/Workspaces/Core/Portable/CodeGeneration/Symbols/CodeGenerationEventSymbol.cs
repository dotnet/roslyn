// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationEventSymbol : CodeGenerationSymbol, IEventSymbol
    {
        public ITypeSymbol Type { get; }
        public NullableAnnotation NullableAnnotation => Type.NullableAnnotation;

        public ImmutableArray<IEventSymbol> ExplicitInterfaceImplementations { get; }

        public IMethodSymbol? AddMethod { get; }
        public IMethodSymbol? RemoveMethod { get; }
        public IMethodSymbol? RaiseMethod { get; }

        public CodeGenerationEventSymbol(
            INamedTypeSymbol? containingType,
            ImmutableArray<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol type,
            ImmutableArray<IEventSymbol> explicitInterfaceImplementations,
            string name,
            IMethodSymbol? addMethod,
            IMethodSymbol? removeMethod,
            IMethodSymbol? raiseMethod)
            : base(containingType?.ContainingAssembly, containingType, attributes, declaredAccessibility, modifiers, name)
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

        public override TResult? Accept<TResult>(SymbolVisitor<TResult> visitor)
            where TResult : default
            => visitor.VisitEvent(this);

        public override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitEvent(this, argument);

        public new IEventSymbol OriginalDefinition => this;

        public bool IsWindowsRuntimeEvent => false;

        public IEventSymbol? OverriddenEvent => null;

        public static ImmutableArray<CustomModifier> TypeCustomModifiers => ImmutableArray.Create<CustomModifier>();
    }
}
