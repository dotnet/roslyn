// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// <Caravela /> This code is used by Try.Caravela.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Caravela.Compiler
{
    public static class CSharpTransformerDriver
    {
        public static (Compilation, ImmutableArray<Diagnostic>) RunTransformers(
            Compilation input, ImmutableArray<ISourceTransformer> transformers, ImmutableArray<object> plugins, AnalyzerConfigOptionsProvider analyzerConfigProvider,
            ImmutableArray<ResourceDescription> manifestResources, IAnalyzerAssemblyLoader assemblyLoader)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            var results = CSharpCompiler.RunTransformers(input, transformers, null, plugins, analyzerConfigProvider, diagnostics, manifestResources, assemblyLoader, CancellationToken.None);
            return (results.TransformedCompilation, diagnostics.ToReadOnlyAndFree());
        }
    }
}
