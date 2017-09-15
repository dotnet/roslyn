// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if NETCOREAPP2_0

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class CoreClrCompilerServerHost : CompilerServerHost
    {
        private static readonly IAnalyzerAssemblyLoader s_analyzerLoader = new CoreClrAnalyzerAssemblyLoader();

        // Caches are used by C# and VB compilers, and shared here.
        public static readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> SharedAssemblyReferenceProvider = (path, properties) => new CachingMetadataReference(path, properties);

        public override IAnalyzerAssemblyLoader AnalyzerAssemblyLoader => s_analyzerLoader;

        public override Func<string, MetadataReferenceProperties, PortableExecutableReference> AssemblyReferenceProvider => SharedAssemblyReferenceProvider;

        internal CoreClrCompilerServerHost(string clientDirectory, string sdkDirectory)
            : base(clientDirectory, sdkDirectory)
        {
        }

        public override bool CheckAnalyzers(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzers)
        {
            return AnalyzerConsistencyChecker.Check(baseDirectory, analyzers, s_analyzerLoader);
        }
    }
}

#endif
