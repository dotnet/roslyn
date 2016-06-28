// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
        /// Report errors from source generator.
        /// </summary>
        [Fact]
        public void ReportErrors()
        {
            string text =
@"class C
{
}";
            using (var directory = new DisposableDirectory(Temp))
            {
                var file = directory.CreateFile("c.cs");
                file.WriteAllText(text);

                // Error only.
                var diagnostics = DiagnosticBag.GetInstance();
                RunCompiler(
                    directory.Path,
                    file.Path,
                    diagnostics,
                    ImmutableArray.Create<SourceGenerator>(
                        new SimpleSourceGenerator(
                            c =>
                            {
                                c.ReportDiagnostic(new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_IntDivByZero), Location.None));
                            })));
                diagnostics.Verify(
                    Diagnostic(ErrorCode.ERR_IntDivByZero).WithLocation(1, 1)); 
                diagnostics.Free();

                // Error and valid tree.
                diagnostics = DiagnosticBag.GetInstance();
                RunCompiler(
                    directory.Path,
                    file.Path,
                    diagnostics,
                    ImmutableArray.Create<SourceGenerator>(
                        new SimpleSourceGenerator(
                            c =>
                            {
                                c.AddCompilationUnit("S", CSharpSyntaxTree.ParseText(@"struct S { }"));
                                c.ReportDiagnostic(new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_IntDivByZero), Location.None));
                            })));
                diagnostics.Verify(
                    Diagnostic(ErrorCode.ERR_IntDivByZero).WithLocation(1, 1));
                diagnostics.Free();

                // Error and tree with parse error.
                diagnostics = DiagnosticBag.GetInstance();
                RunCompiler(
                    directory.Path,
                    file.Path,
                    diagnostics,
                    ImmutableArray.Create<SourceGenerator>(
                        new SimpleSourceGenerator(
                            c =>
                            {
                                c.AddCompilationUnit("__c", CSharpSyntaxTree.ParseText(@"class { }"));
                                c.ReportDiagnostic(new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_IntDivByZero), Location.None));
                            })));
                diagnostics.Verify(
                    Diagnostic(ErrorCode.ERR_IntDivByZero).WithLocation(1, 1),
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(1, 7));
                diagnostics.Free();
            }
        }

        /// <summary>
        /// Report exceptions as errors.
        /// </summary>
        [Fact]
        public void ReportExceptions()
        {
            string text =
@"class C
{
}";
            using (var directory = new DisposableDirectory(Temp))
            {
                var file = directory.CreateFile("c.cs");
                file.WriteAllText(text);
                var diagnostics = DiagnosticBag.GetInstance();
                RunCompiler(
                    directory.Path,
                    file.Path,
                    diagnostics,
                    ImmutableArray.Create<SourceGenerator>(
                        new SimpleSourceGenerator(c => { throw new InvalidCastException(); }),
                        new SimpleSourceGenerator(c => { throw new NullReferenceException(); })));
                diagnostics.Verify(
                    Diagnostic("SG0001").WithArguments("Microsoft.CodeAnalysis.Test.Utilities.SimpleSourceGenerator", "System.InvalidCastException", "Specified cast is not valid.").WithLocation(1, 1),
                    Diagnostic("SG0001").WithArguments("Microsoft.CodeAnalysis.Test.Utilities.SimpleSourceGenerator", "System.NullReferenceException", "Object reference not set to an instance of an object.").WithLocation(1, 1));
                diagnostics.Free();
            }
        }

        /// <summary>
        /// OperationCanceledException should not be caught.
        /// </summary>
        [Fact]
        public void PropagateCancellationException()
        {
            string text =
@"class C
{
}";
            using (var directory = new DisposableDirectory(Temp))
            {
                var file = directory.CreateFile("c.cs");
                file.WriteAllText(text);
                var diagnostics = DiagnosticBag.GetInstance();
                Assert.Throws<OperationCanceledException>(() =>
                    RunCompiler(
                        directory.Path,
                        file.Path,
                        diagnostics,
                        ImmutableArray.Create<SourceGenerator>(
                            new SimpleSourceGenerator(c => { throw new InvalidCastException(); }),
                            new SimpleSourceGenerator(c => { throw new OperationCanceledException(); }))));
                diagnostics.Verify();
                diagnostics.Free();
            }
        }

        private static void RunCompiler(string baseDirectory, string filePath, DiagnosticBag diagnostics, ImmutableArray<SourceGenerator> generators)
        {
            var compiler = new MyCompiler(
                baseDirectory: baseDirectory,
                args: new[] { "/nologo", "/preferreduilang:en", "/t:library", filePath },
                generators: generators);
            var errorLogger = new DiagnosticBagErrorLogger(diagnostics);
            var builder = new StringBuilder();
            using (var writer = new StringWriter(builder))
            {
                compiler.RunCore(writer, errorLogger, default(CancellationToken));
            }
        }

        private sealed class MyCompiler : CSharpCompiler
        {
            private readonly ImmutableArray<SourceGenerator> _generators;

            internal MyCompiler(
                string baseDirectory,
                string[] args,
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
                _generators = generators;
            }

            protected override void ResolveAnalyzersAndGeneratorsFromArguments(
                List<DiagnosticInfo> diagnostics,
                CommonMessageProvider messageProvider,
                out ImmutableArray<DiagnosticAnalyzer> analyzers,
                out ImmutableArray<SourceGenerator> generators)
            {
                analyzers = ImmutableArray<DiagnosticAnalyzer>.Empty;
                generators = _generators;
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
