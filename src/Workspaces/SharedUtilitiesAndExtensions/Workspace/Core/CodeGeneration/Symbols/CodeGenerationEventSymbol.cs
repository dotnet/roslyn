// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationEventSymbol(
        INamedTypeSymbol? containingType,
        ImmutableArray<AttributeData> attributes,
        Accessibility declaredAccessibility,
        DeclarationModifiers modifiers,
        ITypeSymbol type,
        ImmutableArray<IEventSymbol> explicitInterfaceImplementations,
        string name,
        IMethodSymbol? addMethod,
        IMethodSymbol? removeMethod,
        IMethodSymbol? raiseMethod) : CodeGenerationSymbol(containingType?.ContainingAssembly, containingType, attributes, declaredAccessibility, modifiers, name), IEventSymbol
    {
        public ITypeSymbol Type { get; } = type;
        public NullableAnnotation NullableAnnotation => Type.NullableAnnotation;

        public ImmutableArray<IEventSymbol> ExplicitInterfaceImplementations { get; } = explicitInterfaceImplementations.NullToEmpty();

        public IMethodSymbol? AddMethod { get; } = addMethod;
        public IMethodSymbol? RemoveMethod { get; } = removeMethod;
        public IMethodSymbol? RaiseMethod { get; } = raiseMethod;

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
