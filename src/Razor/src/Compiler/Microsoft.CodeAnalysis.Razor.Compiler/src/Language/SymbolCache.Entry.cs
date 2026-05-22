// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal partial class SymbolCache
{
    private class Entry
    {
        public SymbolData? SymbolData { get; set; }
        public NamedTypeSymbolData? NamedTypeSymbolData { get; set; }
        public AssemblySymbolData? AssemblySymbolData { get; set; }
    }
}
