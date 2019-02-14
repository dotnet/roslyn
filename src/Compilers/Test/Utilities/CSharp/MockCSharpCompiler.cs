// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    internal class MockCSharpCompiler : CSharpCompiler
    {
        private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;
        internal Compilation Compilation;

        public MockCSharpCompiler(string responseFile, string workingDirectory, string[] args, ImmutableArray<DiagnosticAnalyzer> analyzers = default, AnalyzerAssemblyLoader loader = null)
            : this(responseFile, CreateBuildPaths(workingDirectory), args, analyzers, loader)
        {
        }

        public MockCSharpCompiler(string responseFile, BuildPaths buildPaths, string[] args, ImmutableArray<DiagnosticAnalyzer> analyzers = default, AnalyzerAssemblyLoader loader = null)
            : base(CSharpCommandLineParser.Default, responseFile, args, buildPaths, Environment.GetEnvironmentVariable("LIB"), loader ?? RuntimeUtilities.CreateAnalyzerAssemblyLoader())
        {
            _analyzers = analyzers.NullToEmpty();
        }

        private static BuildPaths CreateBuildPaths(string workingDirectory, string sdkDirectory = null) => RuntimeUtilities.CreateBuildPaths(workingDirectory, sdkDirectory);

        protected override ImmutableArray<DiagnosticAnalyzer> ResolveAnalyzersFromArguments(
            List<DiagnosticInfo> diagnostics,
            CommonMessageProvider messageProvider)
        {
            var analyzers = base.ResolveAnalyzersFromArguments(diagnostics, messageProvider);
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
