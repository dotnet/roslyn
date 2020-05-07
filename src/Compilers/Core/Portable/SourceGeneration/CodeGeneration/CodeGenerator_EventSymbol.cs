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
            string name,
            ITypeSymbol type,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility declaredAccessibility = Accessibility.NotApplicable,
            SymbolModifiers modifiers = SymbolModifiers.None,
            ImmutableArray<IEventSymbol> explicitInterfaceImplementations = default,
            IMethodSymbol addMethod = null,
            IMethodSymbol removeMethod = null,
            IMethodSymbol raiseMethod = null)
        {
            return new EventSymbol(
                name,
                type,
                attributes,
                declaredAccessibility,
                modifiers,
                explicitInterfaceImplementations,
                addMethod,
                removeMethod,
                raiseMethod);
        }

        public static IEventSymbol With(
            this IEventSymbol @event,
            Optional<string> name = default,
            Optional<ITypeSymbol> type = default,
            Optional<ImmutableArray<AttributeData>> attributes = default,
            Optional<Accessibility> declaredAccessibility = default,
            Optional<SymbolModifiers> modifiers = default,
            Optional<ImmutableArray<IEventSymbol>> explicitInterfaceImplementations = default,
            Optional<IMethodSymbol> addMethod = default,
            Optional<IMethodSymbol> removeMethod = default,
            Optional<IMethodSymbol> raiseMethod = default)
        {
            return new EventSymbol(
                name.GetValueOr(@event.Name),
                type.GetValueOr(@event.Type),
                attributes.GetValueOr(@event.GetAttributes()),
                declaredAccessibility.GetValueOr(@event.DeclaredAccessibility),
                modifiers.GetValueOr(@event.GetModifiers()),
                explicitInterfaceImplementations.GetValueOr(@event.ExplicitInterfaceImplementations),
                addMethod.GetValueOr(@event.AddMethod),
                removeMethod.GetValueOr(@event.RemoveMethod),
                raiseMethod.GetValueOr(@event.RaiseMethod));
        }

        private class EventSymbol : Symbol, IEventSymbol
        {
            private readonly ImmutableArray<AttributeData> _attributes;

            public EventSymbol(
                string name,
                ITypeSymbol type,
                ImmutableArray<AttributeData> attributes = default,
                Accessibility declaredAccessibility = default,
                SymbolModifiers modifiers = default,
                ImmutableArray<IEventSymbol> explicitInterfaceImplementations = default,
                IMethodSymbol addMethod = null,
                IMethodSymbol removeMethod = null,
                IMethodSymbol raiseMethod = null)
            {
                Name = name;
                Type = type;
                DeclaredAccessibility = declaredAccessibility;
                Modifiers = modifiers;
                _attributes = attributes;
                AddMethod = addMethod;
                RemoveMethod = removeMethod;
                RaiseMethod = raiseMethod;
                ExplicitInterfaceImplementations = explicitInterfaceImplementations;
            }

            public ITypeSymbol Type { get; }
            public IMethodSymbol AddMethod { get; }
            public IMethodSymbol RemoveMethod { get; }
            public IMethodSymbol RaiseMethod { get; }
            public ImmutableArray<IEventSymbol> ExplicitInterfaceImplementations { get; }

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

            public NullableAnnotation NullableAnnotation => throw new NotImplementedException();
            public bool IsWindowsRuntimeEvent => throw new NotImplementedException();
            public IEventSymbol OverriddenEvent => throw new NotImplementedException();
            IEventSymbol IEventSymbol.OriginalDefinition => throw new NotImplementedException();

            #endregion
        }
    }
}
