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

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    internal class MockCSharpCompiler : CSharpCompiler
    {
        private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;
        private readonly ImmutableArray<ISourceGenerator> _generators;
        // <Metalama>
        private readonly ImmutableArray<ISourceTransformer> _transformers;
        // </Metalama>
        private readonly ImmutableArray<MetadataReference> _additionalReferences;
        internal Compilation Compilation;
        internal AnalyzerOptions AnalyzerOptions;
        internal ImmutableArray<(DiagnosticDescriptor Descriptor, DiagnosticDescriptorErrorLoggerInfo Info)> DescriptorsWithInfo;
        internal double TotalAnalyzerExecutionTime;

        // <Metalama>
        public MockCSharpCompiler(string responseFile, string workingDirectory, string[] args, ImmutableArray<DiagnosticAnalyzer> analyzers = default, ImmutableArray<ISourceGenerator> generators = default, ImmutableArray<ISourceTransformer> transformers = default, AnalyzerAssemblyLoader loader = null, ImmutableArray<MetadataReference> additionalReferences = default)
            : this(responseFile, CreateBuildPaths(workingDirectory), args, analyzers, generators, transformers, loader, null, additionalReferences)
        // </Metalama>
        {
        }

        // <Metalama>
        public MockCSharpCompiler(string responseFile, BuildPaths buildPaths, string[] args, ImmutableArray<DiagnosticAnalyzer> analyzers = default, ImmutableArray<ISourceGenerator> generators = default, ImmutableArray<ISourceTransformer> transformers = default, AnalyzerAssemblyLoader loader = null, GeneratorDriverCache driverCache = null, ImmutableArray<MetadataReference> additionalReferences = default)
            : base(CSharpCommandLineParser.Default, responseFile, args, buildPaths, Environment.GetEnvironmentVariable("LIB"), loader ?? new AnalyzerAssemblyLoader(), driverCache)
        // </Metalama>
        {
            _analyzers = analyzers.NullToEmpty();
            _generators = generators.NullToEmpty();
            // <Metalama>
            _transformers = transformers.NullToEmpty();
            // </Metalama>
            _additionalReferences = additionalReferences.NullToEmpty();
        }

        private static BuildPaths CreateBuildPaths(string workingDirectory, string sdkDirectory = null) => RuntimeUtilities.CreateBuildPaths(workingDirectory, sdkDirectory);

        protected override void ResolveAnalyzersFromArguments(
            List<DiagnosticInfo> diagnostics,
            CommonMessageProvider messageProvider,
            CompilationOptions compilationOptions,
            bool skipAnalyzers,
            // <Metalama>
            ImmutableArray<string> transformerOrder,
            // </Metalama>
            out ImmutableArray<DiagnosticAnalyzer> analyzers,
            out ImmutableArray<ISourceGenerator> generators,
            // <Metalama>
            out ImmutableArray<ISourceTransformer> transformers
            // </Metalama>
            )
        {
            // <Metalama>
            base.ResolveAnalyzersFromArguments(diagnostics, messageProvider, compilationOptions, skipAnalyzers, transformerOrder, out analyzers, out generators, out transformers);
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

        // <Metalama>
        public void ResolveAnalyzersFromArguments(
            bool skipAnalyzers,
            out List<DiagnosticInfo> diagnostics,
            ImmutableArray<string> transformerOrder,
            out ImmutableArray<DiagnosticAnalyzer> analyzers,
            out ImmutableArray<ISourceGenerator> generators,
            out ImmutableArray<ISourceTransformer> transformers
            )
        {
            diagnostics = new List<DiagnosticInfo>();
            ResolveAnalyzersFromArguments(diagnostics, this.MessageProvider, TestOptions.DebugDll, skipAnalyzers, transformerOrder, out analyzers, out generators, out transformers);
        }

        public void ResolveAnalyzersFromArguments(
            bool skipAnalyzers,
            out List<DiagnosticInfo> diagnostics,
            out ImmutableArray<DiagnosticAnalyzer> analyzers,
            out ImmutableArray<ISourceGenerator> generators
        )
        {
            ResolveAnalyzersFromArguments(skipAnalyzers, out diagnostics, ImmutableArray<string>.Empty, out analyzers, out generators, out _);
        }
        // </Metalama>

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

            if (!_additionalReferences.IsEmpty)
            {
                Compilation = Compilation.AddReferences(_additionalReferences);
            }

            return Compilation;
        }

        protected override AnalyzerOptions CreateAnalyzerOptions(
            ImmutableArray<AdditionalText> additionalTextFiles,
            AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider)
        {
            AnalyzerOptions = base.CreateAnalyzerOptions(additionalTextFiles, analyzerConfigOptionsProvider);
            return AnalyzerOptions;
        }

        protected override void AddAnalyzerDescriptorsAndExecutionTime(ErrorLogger errorLogger, ImmutableArray<(DiagnosticDescriptor Descriptor, DiagnosticDescriptorErrorLoggerInfo Info)> descriptorsWithInfo, double totalAnalyzerExecutionTime)
        {
            DescriptorsWithInfo = descriptorsWithInfo;
            TotalAnalyzerExecutionTime = totalAnalyzerExecutionTime;

            base.AddAnalyzerDescriptorsAndExecutionTime(errorLogger, descriptorsWithInfo, totalAnalyzerExecutionTime);
        }

        public string GetAnalyzerExecutionTimeFormattedString()
            => ReportAnalyzerUtil.GetFormattedAnalyzerExecutionTime(TotalAnalyzerExecutionTime, Culture).Trim();
    }
}
