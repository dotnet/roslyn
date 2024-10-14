// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private sealed class SpecialTypeSymbolKey : AbstractSymbolKey<INamedTypeSymbol>
    {
        public static readonly SpecialTypeSymbolKey Instance = new();

        public override void Create(INamedTypeSymbol symbol, SymbolKeyWriter writer)
        {
            Contract.ThrowIfFalse(symbol.IsSpecialType());
            writer.WriteInteger((int)symbol.SpecialType);
        }

        protected override SymbolKeyResolution Resolve(SymbolKeyReader reader, INamedTypeSymbol? contextualSymbol, out string? failureReason)
        {
            var specialType = (SpecialType)reader.ReadInteger();
            failureReason = null;
            return new SymbolKeyResolution(reader.Compilation.GetSpecialType(specialType));
        }
    }
}
