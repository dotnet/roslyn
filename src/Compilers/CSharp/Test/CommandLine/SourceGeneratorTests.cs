// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SourceGeneratorTests : CSharpTestBase
    {
        /// <summary>
        /// Command-line compiler should only run
        /// analyzers after generating source.
        /// </summary>
        [Fact]
        public void RunAnalyzersAfterGeneratingSource()
        {
            string text =
@"class C
{
}";
            using (var directory = new DisposableDirectory(Temp))
            {
                var file = directory.CreateFile("c.cs");
                file.WriteAllText(text);

                int analyzerCalls = 0;
                ImmutableArray<SyntaxTree> treesToAnalyze;
                var analyzer = new MyAnalyzer(c =>
                {
                    analyzerCalls++;
                    Assert.True(treesToAnalyze.IsDefault);
                    treesToAnalyze = ImmutableArray.CreateRange(c.Compilation.SyntaxTrees);
                });

                int generatorCalls = 0;
                var generator = new MyGenerator(c =>
                {
                    generatorCalls++;
                    c.AddCompilationUnit("__c", CSharpSyntaxTree.ParseText("class __C { }"));
                });

                var compiler = new MyCompiler(
                    baseDirectory: directory.Path,
                    args: new[] { "/nologo", "/preferreduilang:en", "/t:library", file.Path },
                    analyzers: ImmutableArray.Create<DiagnosticAnalyzer>(analyzer),
                    generators: ImmutableArray.Create<SourceGenerator>(generator));

                var builder = new StringBuilder();
                using (var writer = new StringWriter(builder))
                {
                    compiler.Run(writer);
                }
                var output = builder.ToString();
                // No errors from analyzer.
                Assert.Equal("", output);

                Assert.Equal(1, generatorCalls);
                Assert.Equal(1, analyzerCalls);
                Assert.Equal(2, treesToAnalyze.Length);
            }
        }

        private sealed class MyAnalyzer : DiagnosticAnalyzer
        {
            private static readonly DiagnosticDescriptor _descriptor = new DiagnosticDescriptor(
                id: "My01",
                title: "",
                messageFormat: "",
                category: "",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            private readonly Action<CompilationStartAnalysisContext> _startAction;

            internal MyAnalyzer(Action<CompilationStartAnalysisContext> startAction)
            {
                _startAction = startAction;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get { return ImmutableArray.Create(_descriptor); }
            }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationStartAction(_startAction);
            }
        }

        private sealed class MyGenerator : SourceGenerator
        {
            private readonly Action<SourceGeneratorContext> _execute;

            internal MyGenerator(Action<SourceGeneratorContext> execute)
            {
                _execute = execute;
            }

            public override void Execute(SourceGeneratorContext context)
            {
                _execute(context);
            }
        }

        private sealed class MyCompiler : CSharpCompiler
        {
            private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;
            private readonly ImmutableArray<SourceGenerator> _generators;

            internal MyCompiler(
                string baseDirectory,
                string[] args,
                ImmutableArray<DiagnosticAnalyzer> analyzers,
                ImmutableArray<SourceGenerator> generators) :
                base(
                    new CSharpCommandLineParser(),
                    responseFile: null,
                    args: args,
                    clientDirectory: null,
                    baseDirectory: baseDirectory,
                    sdkDirectoryOpt: RuntimeEnvironment.GetRuntimeDirectory(),
                    additionalReferenceDirectories: null,
                    assemblyLoader: null)
            {
                _analyzers = analyzers;
                _generators = generators;
            }

            protected override ImmutableArray<DiagnosticAnalyzer> ResolveAnalyzersFromArguments(List<DiagnosticInfo> diagnostics, CommonMessageProvider messageProvider, TouchedFileLogger touchedFiles)
            {
                return _analyzers;
            }

            protected override ImmutableArray<SourceGenerator> ResolveGeneratorsFromArguments(List<DiagnosticInfo> diagnostics, CommonMessageProvider messageProvider, TouchedFileLogger touchedFiles)
            {
                return _generators;
            }

            protected override void CompilerSpecificSqm(IVsSqmMulti sqm, uint sqmSession)
            {
                throw new NotImplementedException();
            }

            protected override uint GetSqmAppID()
            {
                throw new NotImplementedException();
            }
        }
    }
}
