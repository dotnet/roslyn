// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal interface ICompilerServerHost
    {
        // BTODO: how many of these are needed anymore? 
        IAnalyzerAssemblyLoader AnalyzerAssemblyLoader { get; }
        Func<string, MetadataReferenceProperties, PortableExecutableReference> AssemblyReferenceProvider { get; }
        bool TryCreateCompiler(RunRequest request, out CommonCompiler compiler);
        bool CheckAnalyzers(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzers);
        void Log(string message);
    }
}
