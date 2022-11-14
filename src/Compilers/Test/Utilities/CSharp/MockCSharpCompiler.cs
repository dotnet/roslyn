// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Metalama.Compiler;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Consumption;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    internal class MockCSharpCompiler : CSharpCompiler
    {
        private readonly bool _bypassLicensing;
        private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;
        private readonly ImmutableArray<ISourceGenerator> _generators;
        // <Metalama>
        private readonly ImmutableArray<ISourceTransformer> _transformers;
        // </Metalama>
        internal Compilation Compilation;
        internal AnalyzerOptions AnalyzerOptions;

        // <Metalama>
        public MockCSharpCompiler(string responseFile, string workingDirectory, string[] args, ImmutableArray<DiagnosticAnalyzer> analyzers = default, ImmutableArray<ISourceGenerator> generators = default, ImmutableArray<ISourceTransformer> transformers = default, AnalyzerAssemblyLoader loader = null, bool bypassLicensing = true)
            : this(responseFile, CreateBuildPaths(workingDirectory), args, analyzers, generators, transformers, loader, null, bypassLicensing)
        // </Metalama>
        {
        }

        // <Metalama>
        public MockCSharpCompiler(string responseFile, BuildPaths buildPaths, string[] args, ImmutableArray<DiagnosticAnalyzer> analyzers = default, ImmutableArray<ISourceGenerator> generators = default, ImmutableArray<ISourceTransformer> transformers = default, AnalyzerAssemblyLoader loader = null, GeneratorDriverCache driverCache = null, bool bypassLicensing = true)
            : base(CSharpCommandLineParser.Default, responseFile, args, buildPaths, Environment.GetEnvironmentVariable("LIB"), loader ?? new DefaultAnalyzerAssemblyLoader(), driverCache)
        // </Metalama>
        {
            _analyzers = analyzers.NullToEmpty();
            _generators = generators.NullToEmpty();
            // <Metalama>
            _bypassLicensing = bypassLicensing;
            _transformers = transformers.NullToEmpty();
            // </Metalama>
        }

        private static BuildPaths CreateBuildPaths(string workingDirectory, string sdkDirectory = null) => RuntimeUtilities.CreateBuildPaths(workingDirectory, sdkDirectory);

        protected override void ResolveAnalyzersFromArguments(
            List<DiagnosticInfo> diagnostics,
            CommonMessageProvider messageProvider,
            bool skipAnalyzers,
            // <Metalama>
            ImmutableArray<string> transformerOrder,
            // </Metalama>
            out ImmutableArray<DiagnosticAnalyzer> analyzers,
            out ImmutableArray<ISourceGenerator> generators,
            // <Metalama>
            out ImmutableArray<ISourceTransformer> transformers,
            out ImmutableArray<object> plugins
            // </Metalama>
            )
        {
            // <Metalama>
            base.ResolveAnalyzersFromArguments(diagnostics, messageProvider, skipAnalyzers, transformerOrder, out analyzers, out generators, out transformers, out plugins);
            // </Metalama>
            if (!_analyzers.IsDefaultOrEmpty)
            {
                analyzers = analyzers.InsertRange(0, _analyzers);
            }
            if (!_generators.IsDefaultOrEmpty)
            {
                generators = generators.InsertRange(0, _generators);
            }
            // <Metalama>
            if (!_transformers.IsDefaultOrEmpty)
            {
                transformers = transformers.InsertRange(0, _transformers);
            }
            // </Metalama>
        }

        public void ResolveAnalyzersFromArguments(
            bool skipAnalyzers,
            out List<DiagnosticInfo> diagnostics,
            out ImmutableArray<DiagnosticAnalyzer> analyzers,
            out ImmutableArray<ISourceGenerator> generators)
        {
            diagnostics = new List<DiagnosticInfo>();
            ResolveAnalyzersFromArguments(diagnostics, this.MessageProvider, skipAnalyzers, out analyzers, out generators);
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

        // <Metalama>

        protected override bool RequiresMetalamaSupportServices => false;
        protected override bool RequiresMetalamaLicenseEnforcement => !this._bypassLicensing;

        protected override bool RequiresMetalamaLicenseAudit => false;

        protected override bool IsLongRunningProcess => false;
        // </Metalama>
    }
}
