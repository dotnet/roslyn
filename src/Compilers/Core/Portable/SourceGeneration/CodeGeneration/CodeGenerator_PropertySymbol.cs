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
            RefKind refKind = default,
            bool isIndexer = false,
            ImmutableArray<IParameterSymbol> parameters = default,
            IMethodSymbol getMethod = null,
            IMethodSymbol setMethod = null,
            ImmutableArray<IPropertySymbol> explicitInterfaceImplementations = default,
            ImmutableArray<AttributeData> attributes = default)
        {
            return new PropertySymbol(
                name,
                type,
                refKind,
                isIndexer,
                parameters,
                getMethod,
                setMethod,
                explicitInterfaceImplementations,
                attributes);
        }

        public static IPropertySymbol With(
            this IPropertySymbol property,
            Optional<string> name = default,
            Optional<ITypeSymbol> type = default,
            Optional<RefKind> refKind = default,
            Optional<bool> isIndexer = default,
            Optional<ImmutableArray<IParameterSymbol>> parameters = default,
            Optional<IMethodSymbol> getMethod = default,
            Optional<IMethodSymbol> setMethod = default,
            Optional<ImmutableArray<IPropertySymbol>> explicitInterfaceImplementations = default,
            Optional<ImmutableArray<AttributeData>> attributes = default)
        {
            return new PropertySymbol(
                name.GetValueOr(property.Name),
                type.GetValueOr(property.Type),
                refKind.GetValueOr(property.RefKind),
                isIndexer.GetValueOr(property.IsIndexer),
                parameters.GetValueOr(property.Parameters),
                getMethod.GetValueOr(property.GetMethod),
                setMethod.GetValueOr(property.SetMethod),
                explicitInterfaceImplementations.GetValueOr(property.ExplicitInterfaceImplementations),
                attributes.GetValueOr(property.GetAttributes()));
        }

        private class PropertySymbol : Symbol, IPropertySymbol
        {
            private readonly ImmutableArray<AttributeData> _attributes;

            public PropertySymbol(
                string name,
                ITypeSymbol type,
                RefKind refKind,
                bool isIndexer,
                ImmutableArray<IParameterSymbol> parameters,
                IMethodSymbol getMethod,
                IMethodSymbol setMethod,
                ImmutableArray<IPropertySymbol> explicitInterfaceImplementations,
                ImmutableArray<AttributeData> attributes)
            {
                Name = name;
                Type = type;
                RefKind = refKind;
                IsIndexer = isIndexer;
                Parameters = parameters;
                GetMethod = getMethod;
                SetMethod = setMethod;
                ExplicitInterfaceImplementations = explicitInterfaceImplementations;
                _attributes = attributes;
            }

            public override SymbolKind Kind => SymbolKind.Property;
            public override string Name { get; }

            public bool IsIndexer { get; }
            public RefKind RefKind { get; }
            public ITypeSymbol Type { get; }
            public ImmutableArray<IParameterSymbol> Parameters { get; }
            public IMethodSymbol GetMethod { get; }
            public IMethodSymbol SetMethod { get; }
            public ImmutableArray<IPropertySymbol> ExplicitInterfaceImplementations { get; }

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitProperty(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitProperty(this);

            #region default implementation

            public bool IsReadOnly => throw new NotImplementedException();
            public bool IsWriteOnly => throw new NotImplementedException();
            public bool IsWithEvents => throw new NotImplementedException();
            public bool ReturnsByRef => throw new NotImplementedException();
            public bool ReturnsByRefReadonly => throw new NotImplementedException();
            public NullableAnnotation NullableAnnotation => throw new NotImplementedException();
            public IPropertySymbol OverriddenProperty => throw new NotImplementedException();
            public ImmutableArray<CustomModifier> RefCustomModifiers => throw new NotImplementedException();
            public ImmutableArray<CustomModifier> TypeCustomModifiers => throw new NotImplementedException();
            IPropertySymbol IPropertySymbol.OriginalDefinition => throw new NotImplementedException();

            #endregion
        }
    }
}
