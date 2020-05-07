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
            Accessibility declaredAccessibility = Accessibility.NotApplicable,
            SymbolModifiers modifiers = SymbolModifiers.None,
            IMethodSymbol addMethod = null,
            IMethodSymbol removeMethod = null,
            IMethodSymbol raiseMethod = null,
            ImmutableArray<IEventSymbol> explicitInterfaceImplementations = default,
            ImmutableArray<AttributeData> attributes = default)
            => new EventSymbol(
                name,
                type,
                declaredAccessibility,
                modifiers,
                addMethod,
                removeMethod,
                raiseMethod,
                explicitInterfaceImplementations,
                attributes);

        public static IEventSymbol With(
            this IEventSymbol @event,
            Optional<string> name = default,
            Optional<ITypeSymbol> type = default,
            Optional<Accessibility> declaredAccessibility = default,
            Optional<SymbolModifiers> modifiers = default,
            Optional<IMethodSymbol> addMethod = default,
            Optional<IMethodSymbol> removeMethod = default,
            Optional<IMethodSymbol> raiseMethod = default,
            Optional<ImmutableArray<IEventSymbol>> explicitInterfaceImplementations = default,
            Optional<ImmutableArray<AttributeData>> attributes = default)
        {
            return new EventSymbol(
                name.GetValueOr(@event.Name),
                type.GetValueOr(@event.Type),
                declaredAccessibility.GetValueOr(@event.DeclaredAccessibility),
                modifiers.GetValueOr(@event.GetModifiers()),
                addMethod.GetValueOr(@event.AddMethod),
                removeMethod.GetValueOr(@event.RemoveMethod),
                raiseMethod.GetValueOr(@event.RaiseMethod),
                explicitInterfaceImplementations.GetValueOr(@event.ExplicitInterfaceImplementations),
                attributes.GetValueOr(@event.GetAttributes()));
        }

        private class EventSymbol : Symbol, IEventSymbol
        {
            private readonly ImmutableArray<AttributeData> _attributes;

            public EventSymbol(
                string name,
                ITypeSymbol type,
                Accessibility declaredAccessibility = default,
                SymbolModifiers modifiers = default,
                IMethodSymbol addMethod = null,
                IMethodSymbol removeMethod = null,
                IMethodSymbol raiseMethod = null,
                ImmutableArray<IEventSymbol> explicitInterfaceImplementations = default,
                ImmutableArray<AttributeData> attributes = default)
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
