// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class DesktopCompilerServerHost : ICompilerServerHost
    {
        private static readonly IAnalyzerAssemblyLoader s_analyzerLoader = new ShadowCopyAnalyzerAssemblyLoader(Path.Combine(Path.GetTempPath(), "VBCSCompiler", "AnalyzerAssemblyLoader"));

        // Caches are used by C# and VB compilers, and shared here.
        private static readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> s_assemblyReferenceProvider =
            (path, properties) => new CachingMetadataReference(path, properties);

        public Func<string, MetadataReferenceProperties, PortableExecutableReference> AssemblyReferenceProvider => s_assemblyReferenceProvider;

        public bool CheckAnalyzers(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzers)
        {
            return AnalyzerConsistencyChecker.Check(baseDirectory, analyzers, s_analyzerLoader);
        }

        public void Log(string message)
        {
            CompilerServerLogger.Log(message);
        }
    }
}
