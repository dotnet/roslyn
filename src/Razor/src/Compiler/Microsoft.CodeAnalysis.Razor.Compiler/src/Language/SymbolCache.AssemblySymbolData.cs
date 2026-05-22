// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal partial class SymbolCache
{
    public sealed partial class AssemblySymbolData(IAssemblySymbol symbol)
    {
        private readonly ConcurrentDictionary<int, TagHelperCollection> _tagHelpers = [];

        public bool TryGetTagHelpers(int key, [NotNullWhen(true)] out TagHelperCollection? value)
            => _tagHelpers.TryGetValue(key, out value);

        public TagHelperCollection AddTagHelpers(int key, TagHelperCollection value)
            => _tagHelpers.GetOrAdd(key, value);

        public bool MightContainTagHelpers { get; } = CalculateMightContainTagHelpers(symbol);

        private static bool CalculateMightContainTagHelpers(IAssemblySymbol assembly)
        {
            // In order to contain tag helpers, components, or anything else we might want to find,
            // the assembly must start with "Microsoft.AspNetCore." or reference an assembly that
            // starts with "Microsoft.AspNetCore."
            return assembly.Name.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal) ||
                    assembly.Modules.First().ReferencedAssemblies.Any(
                        a => a.Name.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal));
        }
    }
}
