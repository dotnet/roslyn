// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static IEventSymbol Event(
            ITypeSymbol type,
            string name,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = Accessibility.NotApplicable,
            SymbolModifiers modifiers = SymbolModifiers.None,
            ImmutableArray<IEventSymbol> explicitInterfaceImplementations = default,
            IMethodSymbol addMethod = null,
            IMethodSymbol removeMethod = null,
            IMethodSymbol raiseMethod = null,
            ISymbol containingSymbol = null)
        {
            return new EventSymbol(
                attributes,
                declaredAccessibility,
                modifiers,
                type,
                explicitInterfaceImplementations,
                name,
                addMethod,
                removeMethod,
                raiseMethod,
                containingSymbol);
        }

        public static IEventSymbol WithAttributes(this IEventSymbol symbol, ImmutableArray<AttributeData> attributes)
            => With(symbol, attributes: ToOptional(attributes));

        public static IEventSymbol WithDeclaredAccessibility(this IEventSymbol symbol, Accessibility declaredAccessibility)
            => With(symbol, declaredAccessibility: ToOptional(declaredAccessibility));

        public static IEventSymbol WithModifiers(this IEventSymbol symbol, SymbolModifiers modifiers)
            => With(symbol, modifiers: ToOptional(modifiers));

        public static IEventSymbol WithType(this IEventSymbol symbol, ITypeSymbol type)
            => With(symbol, type: ToOptional(type));

        public static IEventSymbol WithExplicitInterfaceImplementations(this IEventSymbol symbol, ImmutableArray<IEventSymbol> explicitInterfaceImplementations)
            => With(symbol, explicitInterfaceImplementations: ToOptional(explicitInterfaceImplementations));

        public static IEventSymbol WithName(this IEventSymbol symbol, string name)
            => With(symbol, name: ToOptional(name));

        public static IEventSymbol WithAddMethod(this IEventSymbol symbol, IMethodSymbol addMethod)
            => With(symbol, addMethod: ToOptional(addMethod));

        public static IEventSymbol WithRemoveMethod(this IEventSymbol symbol, IMethodSymbol removeMethod)
            => With(symbol, removeMethod: ToOptional(removeMethod));

        public static IEventSymbol WithRaiseMethod(this IEventSymbol symbol, IMethodSymbol raiseMethod)
            => With(symbol, raiseMethod: ToOptional(raiseMethod));

        public static IEventSymbol WithContainingSymbol(this IEventSymbol symbol, ISymbol containingSymbol)
            => With(symbol, containingSymbol: ToOptional(containingSymbol));

        private static IEventSymbol With(
            this IEventSymbol @event,
            Optional<ImmutableArray<AttributeData>> attributes = default,
            Optional<Accessibility> declaredAccessibility = default,
            Optional<SymbolModifiers> modifiers = default,
            Optional<ITypeSymbol> type = default,
            Optional<ImmutableArray<IEventSymbol>> explicitInterfaceImplementations = default,
            Optional<string> name = default,
            Optional<IMethodSymbol> addMethod = default,
            Optional<IMethodSymbol> removeMethod = default,
            Optional<IMethodSymbol> raiseMethod = default,
            Optional<ISymbol> containingSymbol = default)
        {
            return new EventSymbol(
                attributes.GetValueOr(@event.GetAttributes()),
                declaredAccessibility.GetValueOr(@event.DeclaredAccessibility),
                modifiers.GetValueOr(@event.GetModifiers()),
                type.GetValueOr(@event.Type),
                explicitInterfaceImplementations.GetValueOr(@event.ExplicitInterfaceImplementations),
                name.GetValueOr(@event.Name),
                addMethod.GetValueOr(@event.AddMethod),
                removeMethod.GetValueOr(@event.RemoveMethod),
                raiseMethod.GetValueOr(@event.RaiseMethod),
                containingSymbol.GetValueOr(@event.ContainingSymbol));
        }

        private class EventSymbol : Symbol, IEventSymbol
        {
            private readonly ImmutableArray<AttributeData> _attributes;

            public EventSymbol(
                ImmutableArray<AttributeData> attributes,
                Accessibility declaredAccessibility,
                SymbolModifiers modifiers,
                ITypeSymbol type,
                ImmutableArray<IEventSymbol> explicitInterfaceImplementations,
                string name,
                IMethodSymbol addMethod,
                IMethodSymbol removeMethod,
                IMethodSymbol raiseMethod,
                ISymbol containingSymbol)
            {
                Name = name;
                Type = type;
                DeclaredAccessibility = declaredAccessibility;
                Modifiers = modifiers;
                _attributes = attributes.NullToEmpty();
                AddMethod = addMethod;
                RemoveMethod = removeMethod;
                RaiseMethod = raiseMethod;
                ExplicitInterfaceImplementations = explicitInterfaceImplementations.NullToEmpty();
                ContainingSymbol = containingSymbol;
            }

            public ITypeSymbol Type { get; }
            public IMethodSymbol AddMethod { get; }
            public IMethodSymbol RemoveMethod { get; }
            public IMethodSymbol RaiseMethod { get; }
            public ImmutableArray<IEventSymbol> ExplicitInterfaceImplementations { get; }

            public override ISymbol ContainingSymbol { get; }
            public override Accessibility DeclaredAccessibility { get; }
            public override SymbolKind Kind => SymbolKind.Event;
            public override SymbolModifiers Modifiers { get; }

            public override string Name { get; }

            public override ImmutableArray<AttributeData> GetAttributes()
                => _attributes;

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitEvent(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitEvent(this);

            #region default implementation

            IEventSymbol IEventSymbol.OriginalDefinition => throw new NotImplementedException();
            public bool IsWindowsRuntimeEvent => throw new NotImplementedException();
            public IEventSymbol OverriddenEvent => throw new NotImplementedException();
            public NullableAnnotation NullableAnnotation => throw new NotImplementedException();

            #endregion
        }
    }
}
