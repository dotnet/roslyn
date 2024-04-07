// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal sealed partial class TopLevelSyntaxTreeIndex
{
    private readonly struct DeclarationInfo(ImmutableArray<DeclaredSymbolInfo> declaredSymbolInfos)
    {
        public ImmutableArray<DeclaredSymbolInfo> DeclaredSymbolInfos { get; } = declaredSymbolInfos;

        public void WriteTo(ObjectWriter writer)
            => writer.WriteArray(DeclaredSymbolInfos, static (w, d) => d.WriteTo(w));

        public static DeclarationInfo? TryReadFrom(StringTable stringTable, ObjectReader reader)
        {
            try
            {
                var infos = reader.ReadArray(static (r, stringTable) => DeclaredSymbolInfo.ReadFrom_ThrowsOnFailure(stringTable, r), stringTable);
                return new DeclarationInfo(infos);
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}
