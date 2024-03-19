// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private sealed class PointerTypeSymbolKey : AbstractSymbolKey<IPointerTypeSymbol>
    {
        public static readonly PointerTypeSymbolKey Instance = new();

        public sealed override void Create(IPointerTypeSymbol symbol, SymbolKeyWriter visitor)
            => visitor.WriteSymbolKey(symbol.PointedAtType);

        protected sealed override SymbolKeyResolution Resolve(
            SymbolKeyReader reader, IPointerTypeSymbol? contextualSymbol, out string? failureReason)
        {
            var pointedAtTypeResolution = reader.ReadSymbolKey(contextualSymbol?.PointedAtType, out var pointedAtTypeFailureReason);

            if (pointedAtTypeFailureReason != null)
            {
                failureReason = $"({nameof(PointerTypeSymbolKey)} {nameof(pointedAtTypeResolution)} failed -> {pointedAtTypeFailureReason})";
                return default;
            }

            if (reader.Compilation.Language == LanguageNames.VisualBasic)
            {
                failureReason = $"({nameof(PointerTypeSymbolKey)} is not supported in {LanguageNames.VisualBasic})";
                return default;
            }

            using var result = PooledArrayBuilder<IPointerTypeSymbol>.GetInstance(pointedAtTypeResolution.SymbolCount);
            foreach (var typeSymbol in pointedAtTypeResolution.OfType<ITypeSymbol>())
                result.AddIfNotNull(reader.Compilation.CreatePointerTypeSymbol(typeSymbol));

            return CreateResolution(result, $"({nameof(PointerTypeSymbolKey)} could not resolve)", out failureReason);
        }
    }
}
