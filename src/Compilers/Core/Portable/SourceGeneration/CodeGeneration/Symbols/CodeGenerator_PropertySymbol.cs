// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static IPropertySymbol Property(
            ITypeSymbol type,
            string name,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility accessibility = default,
            SymbolModifiers modifiers = default,
            ImmutableArray<IPropertySymbol> explicitInterfaceImplementations = default,
            ImmutableArray<IParameterSymbol> parameters = default,
            IMethodSymbol getMethod = null,
            IMethodSymbol setMethod = null,
            ISymbol containingSymbol = null)
        {
            return new PropertySymbol(
                attributes,
                accessibility,
                modifiers,
                type,
                explicitInterfaceImplementations,
                name,
                parameters,
                getMethod,
                setMethod,
                isIndexer: false,
                containingSymbol);
        }

        public static IPropertySymbol Indexer(
            ITypeSymbol type,
            ImmutableArray<IParameterSymbol> parameters,
            ImmutableArray<AttributeData> attributes = default,
            Accessibility accessibility = default,
            SymbolModifiers modifiers = default,
            ImmutableArray<IPropertySymbol> explicitInterfaceImplementations = default,
            IMethodSymbol getMethod = null,
            IMethodSymbol setMethod = null,
            ISymbol containingSymbol = null)
        {
            return new PropertySymbol(
                attributes,
                accessibility,
                modifiers,
                type,
                explicitInterfaceImplementations,
                name: null,
                parameters,
                getMethod,
                setMethod,
                isIndexer: true,
                containingSymbol);
        }

        public static IPropertySymbol WithAttributes(this IPropertySymbol symbol, params AttributeData[] attributes)
            => WithAttributes(symbol, (IEnumerable<AttributeData>)attributes);

        public static IPropertySymbol WithAttributes(this IPropertySymbol symbol, IEnumerable<AttributeData> attributes)
            => WithAttributes(symbol, attributes.ToImmutableArray());

        public static IPropertySymbol WithAttributes(this IPropertySymbol symbol, ImmutableArray<AttributeData> attributes)
            => With(symbol, attributes: ToOptional(attributes));

        public static IPropertySymbol WithDeclaredAccessibility(this IPropertySymbol symbol, Accessibility accessibility)
            => With(symbol, accessibility: ToOptional(accessibility));

        public static IPropertySymbol WithModifiers(this IPropertySymbol symbol, SymbolModifiers modifiers)
            => With(symbol, modifiers: ToOptional(modifiers));

        public static IPropertySymbol WithType(this IPropertySymbol symbol, ITypeSymbol type)
            => With(symbol, type: ToOptional(type));

        public static IPropertySymbol WithExplicitInterfaceImplementations(this IPropertySymbol symbol, params IPropertySymbol[] explicitInterfaceImplementations)
            => WithExplicitInterfaceImplementations(symbol, (IEnumerable<IPropertySymbol>)explicitInterfaceImplementations);

        public static IPropertySymbol WithExplicitInterfaceImplementations(this IPropertySymbol symbol, IEnumerable<IPropertySymbol> explicitInterfaceImplementations)
            => WithExplicitInterfaceImplementations(symbol, explicitInterfaceImplementations.ToImmutableArray());

        public static IPropertySymbol WithExplicitInterfaceImplementations(this IPropertySymbol symbol, ImmutableArray<IPropertySymbol> explicitInterfaceImplementations)
            => With(symbol, explicitInterfaceImplementations: ToOptional(explicitInterfaceImplementations));

        public static IPropertySymbol WithName(this IPropertySymbol symbol, string name)
            => With(symbol, name: ToOptional(name));

        public static IPropertySymbol WithParameters(this IPropertySymbol symbol, params IParameterSymbol[] parameters)
            => WithParameters(symbol, (IEnumerable<IParameterSymbol>)parameters);

        public static IPropertySymbol WithParameters(this IPropertySymbol symbol, IEnumerable<IParameterSymbol> parameters)
            => WithParameters(symbol, parameters.ToImmutableArray());

        public static IPropertySymbol WithParameters(this IPropertySymbol symbol, ImmutableArray<IParameterSymbol> parameters)
            => With(symbol, parameters: ToOptional(parameters));

        public static IPropertySymbol WithGetMethod(this IPropertySymbol symbol, IMethodSymbol getMethod)
            => With(symbol, getMethod: ToOptional(getMethod));

        public static IPropertySymbol WithSetMethod(this IPropertySymbol symbol, IMethodSymbol setMethod)
            => With(symbol, setMethod: ToOptional(setMethod));

        public static IPropertySymbol WithContainingSymbol(this IPropertySymbol symbol, ISymbol containingSymbol)
            => With(symbol, containingSymbol: ToOptional(containingSymbol));

        private static IPropertySymbol With(
            this IPropertySymbol property,
            Optional<ImmutableArray<AttributeData>> attributes = default,
            Optional<Accessibility> accessibility = default,
            Optional<SymbolModifiers> modifiers = default,
            Optional<ITypeSymbol> type = default,
            Optional<ImmutableArray<IPropertySymbol>> explicitInterfaceImplementations = default,
            Optional<string> name = default,
            Optional<ImmutableArray<IParameterSymbol>> parameters = default,
            Optional<IMethodSymbol> getMethod = default,
            Optional<IMethodSymbol> setMethod = default,
            Optional<ISymbol> containingSymbol = default)
        {
            return new PropertySymbol(
                attributes.GetValueOr(property.GetAttributes()),
                accessibility.GetValueOr(property.DeclaredAccessibility),
                modifiers.GetValueOr(property.GetModifiers()),
                type.GetValueOr(property.Type),
                explicitInterfaceImplementations.GetValueOr(property.ExplicitInterfaceImplementations),
                name.GetValueOr(property.Name),
                parameters.GetValueOr(property.Parameters),
                getMethod.GetValueOr(property.GetMethod),
                setMethod.GetValueOr(property.SetMethod),
                isIndexer: property.IsIndexer,
                containingSymbol.GetValueOr(property.ContainingSymbol));
        }

        private class PropertySymbol : Symbol, IPropertySymbol
        {
            private readonly ImmutableArray<AttributeData> _attributes;

            public PropertySymbol(
                ImmutableArray<AttributeData> attributes,
                Accessibility accessibility,
                SymbolModifiers modifiers,
                ITypeSymbol type,
                ImmutableArray<IPropertySymbol> explicitInterfaceImplementations,
                string name,
                ImmutableArray<IParameterSymbol> parameters,
                IMethodSymbol getMethod,
                IMethodSymbol setMethod,
                bool isIndexer,
                ISymbol containingSymbol)
            {
                Name = name;
                Type = type;
                DeclaredAccessibility = accessibility;
                Modifiers = modifiers;
                IsIndexer = isIndexer;
                Parameters = parameters;
                GetMethod = getMethod;
                SetMethod = setMethod;
                ExplicitInterfaceImplementations = explicitInterfaceImplementations.NullToEmpty();
                _attributes = attributes.NullToEmpty();
                ContainingSymbol = containingSymbol;
            }

            public override ISymbol ContainingSymbol { get; }
            public override Accessibility DeclaredAccessibility { get; }
            public override SymbolKind Kind => SymbolKind.Property;
            public override SymbolModifiers Modifiers { get; }
            public override string Name { get; }

            public bool IsIndexer { get; }
            public ITypeSymbol Type { get; }
            public ImmutableArray<IParameterSymbol> Parameters { get; }
            public IMethodSymbol GetMethod { get; }
            public IMethodSymbol SetMethod { get; }
            public ImmutableArray<IPropertySymbol> ExplicitInterfaceImplementations { get; }

            public override ImmutableArray<AttributeData> GetAttributes()
                => _attributes;

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitProperty(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitProperty(this);

            #region default implementation

            IPropertySymbol IPropertySymbol.OriginalDefinition => throw new NotImplementedException();
            public bool IsReadOnly => throw new NotImplementedException();
            public bool IsWithEvents => throw new NotImplementedException();
            public bool IsWriteOnly => throw new NotImplementedException();
            public bool ReturnsByRef => throw new NotImplementedException();
            public bool ReturnsByRefReadonly => throw new NotImplementedException();
            public ImmutableArray<CustomModifier> RefCustomModifiers => throw new NotImplementedException();
            public ImmutableArray<CustomModifier> TypeCustomModifiers => throw new NotImplementedException();
            public IPropertySymbol OverriddenProperty => throw new NotImplementedException();
            public NullableAnnotation NullableAnnotation => throw new NotImplementedException();
            public RefKind RefKind => throw new NotImplementedException();

            #endregion
        }
    }
}
