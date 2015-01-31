// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    internal static class MetadataTestHelpers
    {
        internal static NamedTypeSymbol GetCorLibType(this ModuleSymbol module, SpecialType typeId)
        {
            return module.ContainingAssembly.GetSpecialType(typeId);
        }

        internal static AssemblySymbol CorLibrary(this ModuleSymbol module)
        {
            return module.ContainingAssembly.CorLibrary;
        }

        internal static AssemblySymbol GetSymbolForReference(MetadataReference reference)
        {
            return GetSymbolsForReferences(mrefs: new[] { reference })[0];
        }

        internal static AssemblySymbol[] GetSymbolsForReferences(params MetadataReference[] mrefs)
        {
            return GetSymbolsForReferences(compilations: null, bytes: null, mrefs: mrefs, options: null);
        }

        internal static AssemblySymbol[] GetSymbolsForReferences(MetadataReference[] mrefs, Compilation[] compilations)
        {
            return GetSymbolsForReferences(
                mrefs: mrefs.Concat(compilations.Select(c => c.ToMetadataReference())).ToArray());
        }

        internal static AssemblySymbol[] GetSymbolsForReferences(
            CSharpCompilation[] compilations = null,
            byte[][] bytes = null,
            MetadataReference[] mrefs = null,
            CSharpCompilationOptions options = null)
        {
            var refs = new List<MetadataReference>();

            if (compilations != null)
            {
                foreach (var c in compilations)
                {
                    refs.Add(new CSharpCompilationReference(c));
                }
            }

            if (bytes != null)
            {
                foreach (var b in bytes)
                {
                    refs.Add(MetadataReference.CreateFromImage(b.AsImmutableOrNull()));
                }
            }

            if (mrefs != null)
            {
                refs.AddRange(mrefs);
            }

            var tc1 = CSharpCompilation.Create(assemblyName: "Dummy", options: options ?? TestOptions.ReleaseDll, syntaxTrees: new SyntaxTree[0], references: refs);

            return (from @ref in refs select tc1.GetReferencedAssemblySymbol(@ref)).ToArray();
        }
    }
}
