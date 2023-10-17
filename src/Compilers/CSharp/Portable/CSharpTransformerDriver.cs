// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// <Metalama /> This code is used by Try.Metalama.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Metalama.Compiler
{
    public static class CSharpTransformerDriver
    {
        public static (Compilation, ImmutableArray<Diagnostic>) RunTransformers(
            Compilation input, ImmutableArray<ISourceTransformer> transformers,
            AnalyzerConfigOptionsProvider analyzerConfigProvider,
            ImmutableArray<ResourceDescription> manifestResources, IAnalyzerAssemblyLoader assemblyLoader,
            TransformerOptions? options = null)
        {
            // We pass null as the IServiceProvider because the main scenario where this code is called is Metalama.Try,
            // where services are passed to Metalama.Framework directly from Metalama.Try using the AsyncLocalConfiguration
            // mechanism.

            var diagnostics = DiagnosticBag.GetInstance();

            var results = CSharpCompiler.RunTransformers(
                input, transformers, null, analyzerConfigProvider, options, diagnostics, manifestResources,
                assemblyLoader, null, CancellationToken.None);
            return (results.TransformedCompilation, diagnostics.ToReadOnlyAndFree());
        }
    }
}
