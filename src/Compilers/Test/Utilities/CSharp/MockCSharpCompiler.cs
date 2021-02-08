// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Caravela.Compiler;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    internal class MockCSharpCompiler : CSharpCompiler
    {
        private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;
        private readonly ImmutableArray<ISourceGenerator> _generators;
        // <Caravela>
        private readonly ImmutableArray<ISourceTransformer> _transformers;
        // </Caravela>
        internal Compilation Compilation;
        internal AnalyzerOptions AnalyzerOptions;

        // <Caravela>
        public MockCSharpCompiler(string responseFile, string workingDirectory, string[] args, ImmutableArray<DiagnosticAnalyzer> analyzers = default, ImmutableArray<ISourceGenerator> generators = default, ImmutableArray<ISourceTransformer> transformers = default, AnalyzerAssemblyLoader loader = null)
            : this(responseFile, CreateBuildPaths(workingDirectory), args, analyzers, generators, transformers, loader)
        // </Caravela>
        {
        }

        // <Caravela>
        public MockCSharpCompiler(string responseFile, BuildPaths buildPaths, string[] args, ImmutableArray<DiagnosticAnalyzer> analyzers = default, ImmutableArray<ISourceGenerator> generators = default, ImmutableArray<ISourceTransformer> transformers = default, AnalyzerAssemblyLoader loader = null)
            : base(CSharpCommandLineParser.Default, responseFile, args, buildPaths, Environment.GetEnvironmentVariable("LIB"), loader ?? new DefaultAnalyzerAssemblyLoader())
        // </Caravela>
        {
            _analyzers = analyzers.NullToEmpty();
            _generators = generators.NullToEmpty();
            // <Caravela>
            _transformers = transformers.NullToEmpty();
            // </Caravela>
        }

        private static BuildPaths CreateBuildPaths(string workingDirectory, string sdkDirectory = null) => RuntimeUtilities.CreateBuildPaths(workingDirectory, sdkDirectory);

        protected override void ResolveAnalyzersFromArguments(
            List<DiagnosticInfo> diagnostics,
            CommonMessageProvider messageProvider,
            bool skipAnalyzers,
            // <Caravela>
            ImmutableArray<string> transformerOrder,
            // </Caravela>
            out ImmutableArray<DiagnosticAnalyzer> analyzers,
            out ImmutableArray<ISourceGenerator> generators,
            // <Caravela>
            out ImmutableArray<ISourceTransformer> transformers,
            out ImmutableArray<object> plugins
            // </Caravela>
            )
        {
            // <Caravela>
            base.ResolveAnalyzersFromArguments(diagnostics, messageProvider, skipAnalyzers, transformerOrder, out analyzers, out generators, out transformers, out plugins);
            // </Caravela>
            if (!_analyzers.IsDefaultOrEmpty)
            {
                analyzers = analyzers.InsertRange(0, _analyzers);
            }
            if (!_generators.IsDefaultOrEmpty)
            {
                generators = generators.InsertRange(0, _generators);
            }
            // <Caravela>
            if (!_transformers.IsDefaultOrEmpty)
            {
                transformers = transformers.InsertRange(0, _transformers);
            }
            // </Caravela>
        }

        public Compilation CreateCompilation(
            TextWriter consoleOutput,
            TouchedFileLogger touchedFilesLogger,
            ErrorLogger errorLogger)
            => CreateCompilation(consoleOutput, touchedFilesLogger, errorLogger, syntaxDiagOptionsOpt: default, globalDiagnosticOptionsOpt: default);

        public override Compilation CreateCompilation(
            TextWriter consoleOutput,
            TouchedFileLogger touchedFilesLogger,
            ErrorLogger errorLogger,
            ImmutableArray<AnalyzerConfigOptionsResult> syntaxDiagOptionsOpt,
            AnalyzerConfigOptionsResult globalDiagnosticOptionsOpt)
        {
            Compilation = base.CreateCompilation(consoleOutput, touchedFilesLogger, errorLogger, syntaxDiagOptionsOpt, globalDiagnosticOptionsOpt);
            return Compilation;
        }

        protected override AnalyzerOptions CreateAnalyzerOptions(
            ImmutableArray<AdditionalText> additionalTextFiles,
            AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider)
        {
            AnalyzerOptions = base.CreateAnalyzerOptions(additionalTextFiles, analyzerConfigOptionsProvider);
            return AnalyzerOptions;
        }
    }
}
