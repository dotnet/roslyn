// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static IParameterSymbol Parameter(
            string name,
            ITypeSymbol type,
            ImmutableArray<AttributeData> attributes = default,
            RefKind refKind = default,
            bool isParams = false,
            Optional<object> explicitDefaultValue = default)
        {
            return new ParameterSymbol(
                name,
                type,
                attributes,
                refKind,
                isParams,
                explicitDefaultValue,
                isDiscard: false);
        }

        public static IParameterSymbol DiscardParameter()
        {
            return new ParameterSymbol(
                name: null,
                type: null,
                attributes: default,
                refKind: default,
                isParams: default,
                explicitDefaultValue: default,
                isDiscard: true);
        }

        public static IParameterSymbol With(
            this IParameterSymbol parameter,
            Optional<string> name = default,
            Optional<ITypeSymbol> type = default,
            Optional<ImmutableArray<AttributeData>> attributes = default,
            Optional<RefKind> refKind = default,
            Optional<bool> isParams = default,
            Optional<Optional<object>> explicitDefaultValue = default)
        {
            return new ParameterSymbol(
                name.GetValueOr(parameter.Name),
                type.GetValueOr(parameter.Type),
                attributes.GetValueOr(parameter.GetAttributes()),
                refKind.GetValueOr(parameter.RefKind),
                isParams.GetValueOr(parameter.IsParams),
                explicitDefaultValue.GetValueOr(GetExplicitDefaultValue(parameter)),
                isDiscard: parameter.IsDiscard);
        }

        private static Optional<object> GetExplicitDefaultValue(IParameterSymbol parameter)
            => parameter.HasExplicitDefaultValue ? new Optional<object>(parameter.ExplicitDefaultValue) : default;

        private class ParameterSymbol : Symbol, IParameterSymbol
        {
            private readonly ImmutableArray<AttributeData> _attributes;

            public ParameterSymbol(
                string name,
                ITypeSymbol type,
                ImmutableArray<AttributeData> attributes,
                RefKind refKind,
                bool isParams,
                Optional<object> explicitDefaultValue,
                bool isDiscard)
            {
                Name = name;
                Type = type;
                RefKind = refKind;
                IsParams = isParams;
                IsDiscard = isDiscard;
                HasExplicitDefaultValue = explicitDefaultValue.HasValue;
                ExplicitDefaultValue = explicitDefaultValue.Value;
                _attributes = attributes;
            }

            public override SymbolKind Kind => SymbolKind.Parameter;
            public override string Name { get; }
            public RefKind RefKind { get; }
            public bool IsParams { get; }
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

            public bool IsOptional => throw new NotImplementedException();
            public bool IsThis => throw new NotImplementedException();
            public NullableAnnotation NullableAnnotation => throw new NotImplementedException();
            public ImmutableArray<CustomModifier> CustomModifiers => throw new NotImplementedException();
            public ImmutableArray<CustomModifier> RefCustomModifiers => throw new NotImplementedException();
            public int Ordinal => throw new NotImplementedException();
            IParameterSymbol IParameterSymbol.OriginalDefinition => throw new NotImplementedException();

            #endregion
        }
    }
}
