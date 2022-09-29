// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator
{
    internal readonly record struct GeneratorOptions(
        SymbolDescriptionOptions SymbolDescriptionOptions,
        BlockStructureOptions BlockStructureOptions)
    {
        public static readonly GeneratorOptions Default =
            new(SymbolDescriptionOptions.Default,
                BlockStructureOptions.Default);
    }
}
