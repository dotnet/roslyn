﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Diagnostics;

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

        public MockCSharpCompiler(string responseFile, string workingDirectory, string[] args, ImmutableArray<DiagnosticAnalyzer> analyzers)
            : base(CSharpCommandLineParser.Default, responseFile, args, CreateBuildPaths(workingDirectory), Environment.GetEnvironmentVariable("LIB"), new DesktopAnalyzerAssemblyLoader())
        {
            _analyzers = analyzers;
        }

        private static BuildPaths CreateBuildPaths(string workingDirectory)
        {
            return new BuildPaths(
                clientDir: Path.GetDirectoryName(typeof(CSharpCompiler).Assembly.Location),
                workingDir: workingDirectory,
                sdkDir: RuntimeEnvironment.GetRuntimeDirectory(),
                tempDir: Path.GetTempPath());
        }

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
