// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static IDiscardSymbol Discard(ITypeSymbol type = null)
            => new DiscardSymbol(type);

        public static IDiscardSymbol WithType(this IDiscardSymbol discard, ITypeSymbol type)
            => With(discard, type: ToOptional(type));

        private static IDiscardSymbol With(
            this IDiscardSymbol discard,
            Optional<ITypeSymbol> type = default)
        {
            return new DiscardSymbol(
                type.GetValueOr(discard.Type));
        }

        private class DiscardSymbol : Symbol, IDiscardSymbol
        {
            public ITypeSymbol Type { get; }

            public DiscardSymbol(ITypeSymbol type = null)
            {
                Type = type;
            }

            public override SymbolKind Kind => SymbolKind.Discard;

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitDiscard(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitDiscard(this);

            #region default implementation

            public NullableAnnotation NullableAnnotation => throw new NotImplementedException();

            #endregion
        }
    }
}
