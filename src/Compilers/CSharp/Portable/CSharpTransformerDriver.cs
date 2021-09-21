// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Caravela.Compiler
{
    public static class CSharpTransformerDriver
    {
        public static (Compilation, ImmutableArray<Diagnostic>) RunTransformers(
            Compilation input, ImmutableArray<ISourceTransformer> transformers, ImmutableArray<object> plugins, AnalyzerConfigOptionsProvider analyzerConfigProvider,
            IList<ResourceDescription> manifestResources, IAnalyzerAssemblyLoader assemblyLoader)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            CSharpCompiler.RunTransformers(input, transformers, plugins, analyzerConfigProvider, diagnostics, manifestResources, assemblyLoader, out _, out var output, out _);
            return (output, diagnostics.ToReadOnlyAndFree());
        }
    }
}
