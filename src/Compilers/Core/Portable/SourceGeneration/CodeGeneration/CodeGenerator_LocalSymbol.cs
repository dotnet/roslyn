// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static ILocalSymbol Local(
            ITypeSymbol type,
            string name,
            SymbolModifiers modifiers = default,
            Optional<object> constantValue = default)
        {
            return new LocalSymbol(
                modifiers,
                type,
                name,
                constantValue);
        }

        public static ILocalSymbol With(
            this ILocalSymbol local,
            Optional<SymbolModifiers> modifiers = default,
            Optional<ITypeSymbol> type = default,
            Optional<string> name = default,
            Optional<Optional<object>> constantValue = default)
        {
            return new LocalSymbol(
                modifiers.GetValueOr(local.GetModifiers()),
                type.GetValueOr(local.Type),
                name.GetValueOr(local.Name),
                constantValue.GetValueOr(GetConstantValue(local)));
        }

        private static Optional<object> GetConstantValue(ILocalSymbol local)
            => local.HasConstantValue ? new Optional<object>(local.ConstantValue) : default;

        private class LocalSymbol : Symbol, ILocalSymbol
        {
            public LocalSymbol(
                SymbolModifiers modifiers,
                ITypeSymbol type,
                string name,
                Optional<object> constantValue)
            {
                Modifiers = modifiers;
                Type = type;
                Name = name;
                HasConstantValue = constantValue.HasValue;
                ConstantValue = constantValue.Value;
            }

            public override SymbolKind Kind => SymbolKind.Local;
            public override SymbolModifiers Modifiers { get; }
            public override string Name { get; }

            public ITypeSymbol Type { get; }

            public bool HasConstantValue { get; }
            public object ConstantValue { get; }

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitLocal(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitLocal(this);

            #region default implementation

            public bool IsConst => throw new NotImplementedException();
            public bool IsFixed => throw new NotImplementedException();
            public bool IsFunctionValue => throw new NotImplementedException();
            public bool IsRef => throw new NotImplementedException();
            public NullableAnnotation NullableAnnotation => throw new NotImplementedException();
            public RefKind RefKind => throw new NotImplementedException();

            #endregion
        }
    }
}
