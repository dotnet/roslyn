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
            RefKind refKind = default,
            bool isParams = false,
            bool isDiscard = false,
            Optional<object> explicitDefaultValue = default,
            ImmutableArray<AttributeData> attributes = default)
        {
            return new ParameterSymbol(
                name,
                type,
                refKind,
                isParams,
                isDiscard,
                explicitDefaultValue,
                attributes);
        }

        public static IParameterSymbol With(
            this IParameterSymbol parameter,
            Optional<string> name = default,
            Optional<ITypeSymbol> type = default,
            Optional<RefKind> refKind = default,
            Optional<bool> isParams = default,
            Optional<bool> isDiscard = default,
            Optional<Optional<object>> explicitDefaultValue = default,
            Optional<ImmutableArray<AttributeData>> attributes = default)
        {
            return new ParameterSymbol(
                name.GetValueOr(parameter.Name),
                type.GetValueOr(parameter.Type),
                refKind.GetValueOr(parameter.RefKind),
                isParams.GetValueOr(parameter.IsParams),
                isDiscard.GetValueOr(parameter.IsDiscard),
                explicitDefaultValue.GetValueOr(GetExplicitDefaultValue(parameter)),
                attributes.GetValueOr(parameter.GetAttributes()));
        }

        private static Optional<object> GetExplicitDefaultValue(IParameterSymbol parameter)
            => parameter.HasExplicitDefaultValue ? new Optional<object>(parameter.ExplicitDefaultValue) : default;

        private class ParameterSymbol : Symbol, IParameterSymbol
        {
            private readonly ImmutableArray<AttributeData> _attributes;

            public ParameterSymbol(
                string name,
                ITypeSymbol type,
                RefKind refKind,
                bool isParams,
                bool isDiscard,
                Optional<object> explicitDefaultValue,
                ImmutableArray<AttributeData> attributes)
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
