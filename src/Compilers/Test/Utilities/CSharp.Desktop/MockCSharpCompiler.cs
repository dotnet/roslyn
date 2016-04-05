// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    internal class MockCSharpCompiler : CSharpCompiler
    {
        private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;
        internal Compilation Compilation;

        public MockCSharpCompiler(string responseFile, string baseDirectory, string[] args)
            : this(responseFile, baseDirectory, args, ImmutableArray<DiagnosticAnalyzer>.Empty)
        {
        }

        public MockCSharpCompiler(string responseFile, string baseDirectory, string[] args, ImmutableArray<DiagnosticAnalyzer> analyzers)
            : base(CSharpCommandLineParser.Default, responseFile, args, Path.GetDirectoryName(typeof(CSharpCompiler).Assembly.Location), baseDirectory, RuntimeEnvironment.GetRuntimeDirectory(), Environment.GetEnvironmentVariable("LIB"), new SimpleAnalyzerAssemblyLoader())
        {
            _analyzers = analyzers;
        }

        protected override void CompilerSpecificSqm(IVsSqmMulti sqm, uint sqmSession)
        {
            throw new NotImplementedException();
        }

        protected override uint GetSqmAppID()
        {
            throw new NotImplementedException();
        }

        protected override void ResolveAnalyzersAndGeneratorsFromArguments(
            List<DiagnosticInfo> diagnostics,
            CommonMessageProvider messageProvider,
            out ImmutableArray<DiagnosticAnalyzer> analyzers,
            out ImmutableArray<SourceGenerator> generators)
        {
            base.ResolveAnalyzersAndGeneratorsFromArguments(diagnostics, messageProvider, out analyzers, out generators);
            if (!_analyzers.IsDefaultOrEmpty)
            {
                analyzers = analyzers.InsertRange(0, _analyzers);
            }
        }

        public override Compilation CreateCompilation(TextWriter consoleOutput, TouchedFileLogger touchedFilesLogger, ErrorLogger errorLogger)
        {
            Compilation = base.CreateCompilation(consoleOutput, touchedFilesLogger, errorLogger);
            return Compilation;
        }
    }
}
