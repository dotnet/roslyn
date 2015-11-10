// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using System.IO.Pipes;
using System.Threading;
using System.Security.Principal;
using System.Security.AccessControl;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class DesktopCompilerServerHost : CompilerServerHost
    {
        private static readonly IAnalyzerAssemblyLoader s_analyzerLoader = new ShadowCopyAnalyzerAssemblyLoader(Path.Combine(Path.GetTempPath(), "VBCSCompiler", "AnalyzerAssemblyLoader"));

        // Caches are used by C# and VB compilers, and shared here.
        private static readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> s_assemblyReferenceProvider = (path, properties) => new CachingMetadataReference(path, properties);

        public override IAnalyzerAssemblyLoader AnalyzerAssemblyLoader => s_analyzerLoader;

        public override Func<string, MetadataReferenceProperties, PortableExecutableReference> AssemblyReferenceProvider => s_assemblyReferenceProvider;

        internal DesktopCompilerServerHost()
            : this(AppDomain.CurrentDomain.BaseDirectory, RuntimeEnvironment.GetRuntimeDirectory())
        {

        }

        internal DesktopCompilerServerHost(string clientDirectory, string sdkDirectory)
            : base(clientDirectory, sdkDirectory)
        {

        }

        public override bool CheckAnalyzers(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzers)
        {
            return AnalyzerConsistencyChecker.Check(baseDirectory, analyzers, s_analyzerLoader);
        }

        public override void Log(string message)
        {
            CompilerServerLogger.Log(message);
        }

    }
}
