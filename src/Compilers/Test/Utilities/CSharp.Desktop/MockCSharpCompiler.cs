// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    internal class MockCSharpCompiler : CSharpCompiler
    {
        private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;
        internal Compilation Compilation;

        public MockCSharpCompiler(string responseFile, string baseDirectory, string[] args, DiagnosticAnalyzer analyzer = null)
            : this(responseFile, baseDirectory, args, analyzer == null ? ImmutableArray<DiagnosticAnalyzer>.Empty : ImmutableArray.Create(analyzer))
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

        protected override ImmutableArray<DiagnosticAnalyzer> ResolveAnalyzersFromArguments(List<DiagnosticInfo> diagnostics, CommonMessageProvider messageProvider, TouchedFileLogger touchedFiles)
        {
            var analyzers = base.ResolveAnalyzersFromArguments(diagnostics, messageProvider, touchedFiles);

            if (!_analyzers.IsDefaultOrEmpty)
            {
                analyzers = analyzers.InsertRange(0, _analyzers);
            }

            return analyzers;
        }

        public override Compilation CreateCompilation(TextWriter consoleOutput, TouchedFileLogger touchedFilesLogger, ErrorLogger errorLogger)
        {
            Compilation = base.CreateCompilation(consoleOutput, touchedFilesLogger, errorLogger);
            return Compilation;
        }
    }
}
