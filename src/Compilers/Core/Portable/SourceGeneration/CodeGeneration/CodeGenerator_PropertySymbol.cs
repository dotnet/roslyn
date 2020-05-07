// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static IPropertySymbol Property(
            string name,
            ITypeSymbol type,
            ImmutableArray<AttributeData> attributes = default,
            SymbolModifiers modifiers = default,
            ImmutableArray<IPropertySymbol> explicitInterfaceImplementations = default,
            ImmutableArray<IParameterSymbol> parameters = default,
            IMethodSymbol getMethod = null,
            IMethodSymbol setMethod = null)
        {
            return new PropertySymbol(
                attributes,
                modifiers,
                type,
                explicitInterfaceImplementations,
                name,
                parameters,
                getMethod,
                setMethod,
                isIndexer: false);
        }

        public static IPropertySymbol Indexer(
            ITypeSymbol type,
            ImmutableArray<IParameterSymbol> parameters,
            ImmutableArray<AttributeData> attributes = default,
            SymbolModifiers modifiers = default,
            ImmutableArray<IPropertySymbol> explicitInterfaceImplementations = default,
            IMethodSymbol getMethod = null,
            IMethodSymbol setMethod = null)
        {
            return new PropertySymbol(
                attributes,
                modifiers,
                type,
                explicitInterfaceImplementations,
                name: null,
                parameters,
                getMethod,
                setMethod,
                isIndexer: true);
        }

        public static IPropertySymbol With(
            this IPropertySymbol property,
            Optional<ImmutableArray<AttributeData>> attributes = default,
            Optional<SymbolModifiers> modifiers = default,
            Optional<ITypeSymbol> type = default,
            Optional<ImmutableArray<IPropertySymbol>> explicitInterfaceImplementations = default,
            Optional<string> name = default,
            Optional<ImmutableArray<IParameterSymbol>> parameters = default,
            Optional<IMethodSymbol> getMethod = default,
            Optional<IMethodSymbol> setMethod = default)
        {
            return new PropertySymbol(
                attributes.GetValueOr(property.GetAttributes()),
                modifiers.GetValueOr(property.GetModifiers()),
                type.GetValueOr(property.Type),
                explicitInterfaceImplementations.GetValueOr(property.ExplicitInterfaceImplementations),
                name.GetValueOr(property.Name),
                parameters.GetValueOr(property.Parameters),
                getMethod.GetValueOr(property.GetMethod),
                setMethod.GetValueOr(property.SetMethod),
                isIndexer: property.IsIndexer);
        }

        private class PropertySymbol : Symbol, IPropertySymbol
        {
            private readonly ImmutableArray<AttributeData> _attributes;

            public PropertySymbol(
                ImmutableArray<AttributeData> attributes,
                SymbolModifiers modifiers,
                ITypeSymbol type,
                ImmutableArray<IPropertySymbol> explicitInterfaceImplementations,
                string name,
                ImmutableArray<IParameterSymbol> parameters,
                IMethodSymbol getMethod,
                IMethodSymbol setMethod,
                bool isIndexer)
            {
                Name = name;
                Type = type;
                Modifiers = modifiers;
                IsIndexer = isIndexer;
                Parameters = parameters;
                GetMethod = getMethod;
                SetMethod = setMethod;
                ExplicitInterfaceImplementations = explicitInterfaceImplementations;
                _attributes = attributes;
            }

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
