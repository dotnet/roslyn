// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CommandLine;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class CoreClrCompilerServerHost : CompilerServerHost
    {
        private readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> _assemblyReferenceProvider = (path, properties) => new CachingMetadataReference(path, properties);
        private readonly IAnalyzerAssemblyLoader _analyzerAssemblyLoader = CoreClrAnalyzerAssemblyLoader.CreateAndSetDefault();

        public override IAnalyzerAssemblyLoader AnalyzerAssemblyLoader => _analyzerAssemblyLoader;

        public override Func<string, MetadataReferenceProperties, PortableExecutableReference> AssemblyReferenceProvider => _assemblyReferenceProvider;

        internal CoreClrCompilerServerHost(string clientDirectory)
            :base(clientDirectory : clientDirectory, sdkDirectory: null)
        {

        }

        public override bool CheckAnalyzers(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzers)
        {
            // Analyzers not supported in the portable server yet.
            return analyzers.Length == 0;
        }

        public override void Log(string message)
        {
            // BTODO: Do we need this anymore? 
        }
    }
}
