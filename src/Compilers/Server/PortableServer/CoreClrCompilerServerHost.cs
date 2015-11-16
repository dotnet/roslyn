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
        public override IAnalyzerAssemblyLoader AnalyzerAssemblyLoader { get; }

        public override Func<string, MetadataReferenceProperties, PortableExecutableReference> AssemblyReferenceProvider { get; }

        internal CoreClrCompilerServerHost(string clientDirectory)
            :base(clientDirectory : clientDirectory, sdkDirectory: null)
        {
            AssemblyReferenceProvider = (path, properties) => new CachingMetadataReference(path, properties);
            AnalyzerAssemblyLoader = CoreClrAnalyzerAssemblyLoader.CreateAndSetDefault();
        }

        public override bool CheckAnalyzers(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzers)
        {
            // Analyzers not supported in the portable server yet.
            return analyzers.Length == 0;
        }
    }
}
