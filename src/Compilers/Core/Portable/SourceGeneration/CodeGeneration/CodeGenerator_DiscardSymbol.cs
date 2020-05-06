// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    internal static partial class CodeGenerator
    {
        public static IDiscardSymbol Discard()
            => new DiscardSymbol();

        public static IDiscardSymbol Discard(ITypeSymbol type)
            => new DiscardSymbol(type);

        public static IDiscardSymbol With(this IDiscardSymbol discard, Optional<ITypeSymbol> type = default)
        {
            var newType = type.HasValue ? type.Value : discard.Type;
            if (newType == discard.Type)
                return discard;

            return new DiscardSymbol(newType);
        }

        private class DiscardSymbol : Symbol, IDiscardSymbol
        {
            public ITypeSymbol Type { get; }

            public DiscardSymbol(ITypeSymbol type = null)
            {
                Type = type;
            }

            #region default implementation

            public NullableAnnotation NullableAnnotation => throw new NotImplementedException();

            #endregion
        }
    }
}
