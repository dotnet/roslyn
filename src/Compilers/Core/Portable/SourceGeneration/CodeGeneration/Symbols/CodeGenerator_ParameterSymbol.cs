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
        public static IParameterSymbol Parameter(
            ITypeSymbol type,
            string name,
            ImmutableArray<AttributeData> attributes = default,
            SymbolModifiers modifiers = default,
            Optional<object> explicitDefaultValue = default)
        {
            return new ParameterSymbol(
                attributes,
                modifiers,
                type,
                name,
                explicitDefaultValue,
                isDiscard: false);
        }

        public static IParameterSymbol DiscardParameter()
        {
            return new ParameterSymbol(
                attributes: default,
                modifiers: default,
                type: null,
                name: null,
                explicitDefaultValue: default,
                isDiscard: true);
        }

        public static IParameterSymbol WithAttributes(this IParameterSymbol symbol, params AttributeData[] attributes)
            => WithAttributes(symbol, (IEnumerable<AttributeData>)attributes);

        public static IParameterSymbol WithAttributes(this IParameterSymbol symbol, IEnumerable<AttributeData> attributes)
            => WithAttributes(symbol, attributes.ToImmutableArray());

        public static IParameterSymbol WithAttributes(this IParameterSymbol symbol, ImmutableArray<AttributeData> attributes)
             => With(symbol, attributes: ToOptional(attributes));

        public static IParameterSymbol WithModifiers(this IParameterSymbol symbol, SymbolModifiers modifiers)
            => With(symbol, modifiers: ToOptional(modifiers));

        public static IParameterSymbol WithType(this IParameterSymbol symbol, ITypeSymbol type)
            => With(symbol, type: ToOptional(type));

        public static IParameterSymbol WithName(this IParameterSymbol symbol, string name)
            => With(symbol, name: ToOptional(name));

        public static IParameterSymbol WithExplicitDefaultValue(this IParameterSymbol symbol, Optional<object> explicitDefaultValue)
            => With(symbol, explicitDefaultValue: ToOptional(explicitDefaultValue));

        private static IParameterSymbol With(
            this IParameterSymbol parameter,
            Optional<ImmutableArray<AttributeData>> attributes = default,
            Optional<SymbolModifiers> modifiers = default,
            Optional<ITypeSymbol> type = default,
            Optional<string> name = default,
            Optional<Optional<object>> explicitDefaultValue = default)
        {
            return new ParameterSymbol(
                attributes.GetValueOr(parameter.GetAttributes()),
                modifiers.GetValueOr(parameter.GetModifiers()),
                type.GetValueOr(parameter.Type),
                name.GetValueOr(parameter.Name),
                explicitDefaultValue.GetValueOr(GetExplicitDefaultValue(parameter)),
                isDiscard: parameter.IsDiscard);
        }

        private static Optional<object> GetExplicitDefaultValue(IParameterSymbol parameter)
            => parameter.HasExplicitDefaultValue ? new Optional<object>(parameter.ExplicitDefaultValue) : default;

        private class ParameterSymbol : Symbol, IParameterSymbol
        {
            private readonly ImmutableArray<AttributeData> _attributes;

            public ParameterSymbol(
                ImmutableArray<AttributeData> attributes,
                SymbolModifiers modifiers,
                ITypeSymbol type,
                string name,
                Optional<object> explicitDefaultValue,
                bool isDiscard)
            {
                Name = name;
                Type = type;
                Modifiers = modifiers;
                IsDiscard = isDiscard;
                HasExplicitDefaultValue = explicitDefaultValue.HasValue;
                ExplicitDefaultValue = explicitDefaultValue.Value;
                _attributes = attributes.NullToEmpty();
            }

            public override SymbolKind Kind => SymbolKind.Parameter;
            public override SymbolModifiers Modifiers { get; }
            public override string Name { get; }

            public bool IsDiscard { get; }

            public ITypeSymbol Type { get; }

            public bool HasExplicitDefaultValue { get; }
            public object ExplicitDefaultValue { get; }

            public override ImmutableArray<AttributeData> GetAttributes()
                => _attributes;

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitParameter(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitParameter(this);

            #region default implementation

            IParameterSymbol IParameterSymbol.OriginalDefinition => throw new NotImplementedException();
            public bool IsOptional => throw new NotImplementedException();
            public bool IsParams => throw new NotImplementedException();
            public bool IsThis => throw new NotImplementedException();
            public ImmutableArray<CustomModifier> CustomModifiers => throw new NotImplementedException();
            public ImmutableArray<CustomModifier> RefCustomModifiers => throw new NotImplementedException();
            public int Ordinal => throw new NotImplementedException();
            public NullableAnnotation NullableAnnotation => throw new NotImplementedException();
            public RefKind RefKind => throw new NotImplementedException();

            #endregion
        }
    }
}
