// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal partial class SymbolCache
{
    private static readonly ConditionalWeakTable<ISymbol, Entry> s_instance = new();

    public static SymbolData GetSymbolData(ISymbol symbol)
    {
        var entry = GetCacheEntry(symbol);
        entry.SymbolData ??= new SymbolData(symbol);

        return entry.SymbolData;
    }

    public static AssemblySymbolData GetAssemblySymbolData(IAssemblySymbol symbol)
    {
        var entry = GetCacheEntry(symbol);
        entry.AssemblySymbolData ??= new AssemblySymbolData(symbol);

        return entry.AssemblySymbolData;
    }

    public static NamedTypeSymbolData GetNamedTypeSymbolData(INamedTypeSymbol symbol)
    {
        var entry = GetCacheEntry(symbol);
        entry.NamedTypeSymbolData ??= new NamedTypeSymbolData(symbol);

        return entry.NamedTypeSymbolData;
    }

    private static Entry GetCacheEntry(ISymbol symbol)
        => s_instance.GetValue(symbol, static s => new Entry());
}
