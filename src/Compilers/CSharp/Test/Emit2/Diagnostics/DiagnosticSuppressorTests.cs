// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class DiagnosticSuppressorTests : CompilingTestBase
    {
        private static CSharpCompilation VerifyAnalyzerDiagnostics(
           CSharpCompilation c,
           DiagnosticAnalyzer[] analyzers,
           params DiagnosticDescription[] expected)
           => c.VerifyAnalyzerDiagnostics(analyzers, expected: expected);

        private static CSharpCompilation VerifySuppressedDiagnostics(
            CSharpCompilation c,
            DiagnosticAnalyzer[] analyzers,
            params DiagnosticDescription[] expected)
            => c.VerifySuppressedDiagnostics(analyzers, expected: expected);

        private static CSharpCompilation VerifySuppressedAndFilteredDiagnostics(
            CSharpCompilation c,
            DiagnosticAnalyzer[] analyzers)
            => c.VerifySuppressedAndFilteredDiagnostics(analyzers);

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestSuppression_CompilerSyntaxWarning()
        {
            // NOTE: Empty switch block warning is reported by the C# language parser
            string source = @"
class C
{
    void M(int i)
    {
        switch (i)
        {
        }
    }
}";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,9): warning CS1522: Empty switch block
                //         {
                Diagnostic(ErrorCode.WRN_EmptySwitch, "{", isSuppressed: false).WithLocation(7, 9));

            // Verify compiler syntax warning can be suppressed with a suppressor.
            var analyzers = new DiagnosticAnalyzer[] { new DiagnosticSuppressorForId("CS1522") };
            VerifySuppressedDiagnostics(compilation, analyzers,
                // (7,9): warning CS1522: Empty switch block
                //         {
                Diagnostic("CS1522", "{", isSuppressed: true).WithLocation(7, 9));

            VerifySuppressedAndFilteredDiagnostics(compilation, analyzers);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestSuppression_CompilerSemanticWarning()
        {
            string source = @"
class C
{
    // warning CS0169: The field 'C.f' is never used
    private readonly int f;
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (5,26): warning CS0169: The field 'C.f' is never used
                //     private readonly int f;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f", isSuppressed: false).WithArguments("C.f").WithLocation(5, 26));

            // Verify compiler semantic warning can be suppressed with a suppressor.
            var analyzers = new DiagnosticAnalyzer[] { new DiagnosticSuppressorForId("CS0169") };
            VerifySuppressedDiagnostics(compilation, analyzers,
                // (5,26): warning CS0169: The field 'C.f' is never used
                //     private readonly int f;
                Diagnostic("CS0169", "f", isSuppressed: true).WithArguments("C.f").WithLocation(5, 26));

            VerifySuppressedAndFilteredDiagnostics(compilation, analyzers);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestNoSuppression_CompilerSyntaxError()
        {
            string source = @"
class { }";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // (2,7): error CS1001: Identifier expected
                // class { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(2, 7));

            // Verify compiler syntax error cannot be suppressed.
            var analyzers = new DiagnosticAnalyzer[] { new DiagnosticSuppressorForId("CS1001") };
            VerifySuppressedDiagnostics(compilation, analyzers);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestNoSuppression_CompilerSemanticError()
        {
            string source = @"
class C
{
    void M(UndefinedType x) { }
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // (4,12): error CS0246: The type or namespace name 'UndefinedType' could not be found (are you missing a using directive or an assembly reference?)
                //     void M(UndefinedType x) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UndefinedType").WithArguments("UndefinedType").WithLocation(4, 12));

            // Verify compiler semantic error cannot be suppressed.
            var analyzers = new DiagnosticAnalyzer[] { new DiagnosticSuppressorForId("CS0246") };
            VerifySuppressedDiagnostics(compilation, analyzers);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestSuppression_MultipleDiagnostics()
        {
            string source1 = @"class C1 { }";
            string source2 = @"class C2 { }";
            var compilation = CreateCompilation(new[] { source1, source2 });
            compilation.VerifyDiagnostics();

            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            var expectedDiagnostics = new DiagnosticDescription[] {
                Diagnostic(analyzer.Descriptor.Id, source1, isSuppressed: false).WithLocation(1, 1),
                Diagnostic(analyzer.Descriptor.Id, source2, isSuppressed: false).WithLocation(1, 1),
            };
            VerifyAnalyzerDiagnostics(compilation, new DiagnosticAnalyzer[] { analyzer }, expectedDiagnostics);

            var analyzersAndSuppressors = new DiagnosticAnalyzer[] { analyzer, new DiagnosticSuppressorForId(analyzer.Descriptor.Id) };
            expectedDiagnostics = new DiagnosticDescription[] {
                Diagnostic(analyzer.Descriptor.Id, source1, isSuppressed: true).WithLocation(1, 1),
                Diagnostic(analyzer.Descriptor.Id, source2, isSuppressed: true).WithLocation(1, 1),
            };
            VerifySuppressedDiagnostics(compilation, analyzersAndSuppressors, expectedDiagnostics);
            VerifySuppressedAndFilteredDiagnostics(compilation, analyzersAndSuppressors);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestSuppression_MultipleSuppressors_SameDiagnostic()
        {
            string source = @"class C1 { }";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            var expectedDiagnostic = Diagnostic(analyzer.Descriptor.Id, source, isSuppressed: false).WithLocation(1, 1);
            VerifyAnalyzerDiagnostics(compilation, new DiagnosticAnalyzer[] { analyzer }, expectedDiagnostic);

            // Multiple suppressors with same suppression ID.
            expectedDiagnostic = Diagnostic(analyzer.Descriptor.Id, source, isSuppressed: true).WithLocation(1, 1);
            var analyzersAndSuppressors = new DiagnosticAnalyzer[] { analyzer, new DiagnosticSuppressorForId(analyzer.Descriptor.Id), new DiagnosticSuppressorForId(analyzer.Descriptor.Id) };
            VerifySuppressedDiagnostics(compilation, analyzersAndSuppressors, expectedDiagnostic);
            VerifySuppressedAndFilteredDiagnostics(compilation, analyzersAndSuppressors);

            // Multiple suppressors with different suppression ID.
            analyzersAndSuppressors = new DiagnosticAnalyzer[] { analyzer, new DiagnosticSuppressorForId(analyzer.Descriptor.Id, suppressionId: "SPR0001"), new DiagnosticSuppressorForId(analyzer.Descriptor.Id, suppressionId: "SPR0002") };
            VerifySuppressedDiagnostics(compilation, analyzersAndSuppressors, expectedDiagnostic);
            VerifySuppressedAndFilteredDiagnostics(compilation, analyzersAndSuppressors);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestSuppression_MultipleSuppressors_DifferentDiagnostic()
        {
            string source = @"class C1 { private readonly int f; }";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (1,33): warning CS0169: The field 'C1.f' is never used
                // class C1 { private readonly int f; }
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f").WithArguments("C1.f").WithLocation(1, 33));

            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            VerifyAnalyzerDiagnostics(compilation, new DiagnosticAnalyzer[] { analyzer },
                Diagnostic(analyzer.Descriptor.Id, source));

            var suppressor1 = new DiagnosticSuppressorForId("CS0169");
            var suppressor2 = new DiagnosticSuppressorForId(analyzer.Descriptor.Id);

            var analyzersAndSuppressors = new DiagnosticAnalyzer[] { analyzer, suppressor1, suppressor2 };
            VerifySuppressedDiagnostics(compilation, analyzersAndSuppressors,
                Diagnostic("CS0169", "f", isSuppressed: true).WithArguments("C1.f").WithLocation(1, 33),
                Diagnostic(analyzer.Descriptor.Id, source, isSuppressed: true));

            VerifySuppressedAndFilteredDiagnostics(compilation, analyzersAndSuppressors);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestNoSuppression_SpecificOptionsTurnsOffSuppressor()
        {
            string source = @"class C1 { }";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            var expectedDiagnostic = Diagnostic(analyzer.Descriptor.Id, source, isSuppressed: false);
            VerifyAnalyzerDiagnostics(compilation, new DiagnosticAnalyzer[] { analyzer }, expectedDiagnostic);

            const string suppressionId = "SPR1001";
            var analyzersAndSuppressors = new DiagnosticAnalyzer[] { analyzer, new DiagnosticSuppressorForId(analyzer.Descriptor.Id, suppressionId) };
            expectedDiagnostic = Diagnostic(analyzer.Descriptor.Id, source, isSuppressed: true);
            VerifySuppressedDiagnostics(compilation, analyzersAndSuppressors, expectedDiagnostic);

            var specificDiagnosticOptions = compilation.Options.SpecificDiagnosticOptions.Add(suppressionId, ReportDiagnostic.Suppress);
            compilation = compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(specificDiagnosticOptions));
            VerifySuppressedDiagnostics(compilation, analyzersAndSuppressors);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestSuppression_AnalyzerDiagnostics_SeveritiesAndConfigurableMatrix()
        {
            string source = @"
class C { }";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            var configurations = new[] { false, true };
            var severities = Enum.GetValues(typeof(DiagnosticSeverity));
            var originalSpecificDiagnosticOptions = compilation.Options.SpecificDiagnosticOptions;

            foreach (var configurable in configurations)
            {
                foreach (DiagnosticSeverity defaultSeverity in severities)
                {
                    foreach (DiagnosticSeverity effectiveSeverity in severities)
                    {
                        var diagnostic = Diagnostic("ID1000", "class C { }", isSuppressed: true)
                                            .WithLocation(2, 1)
                                            .WithDefaultSeverity(defaultSeverity)
                                            .WithEffectiveSeverity(configurable ? effectiveSeverity : defaultSeverity);

                        var diagnosticNoSuppressor = Diagnostic("ID1000", "class C { }", isSuppressed: false)
                            .WithLocation(2, 1)
                            .WithDefaultSeverity(defaultSeverity)
                            .WithEffectiveSeverity(configurable ? effectiveSeverity : defaultSeverity);

                        if (defaultSeverity == DiagnosticSeverity.Warning &&
                            effectiveSeverity == DiagnosticSeverity.Error &&
                            configurable)
                        {
                            diagnostic = diagnostic.WithWarningAsError(true);
                            diagnosticNoSuppressor = diagnosticNoSuppressor.WithWarningAsError(true);
                        }

                        var analyzer = new CompilationAnalyzerWithSeverity(defaultSeverity, configurable);
                        var suppressor = new DiagnosticSuppressorForId(analyzer.Descriptor.Id);
                        var analyzersWithoutSuppressor = new DiagnosticAnalyzer[] { analyzer };
                        var analyzersWithSuppressor = new DiagnosticAnalyzer[] { analyzer, suppressor };

                        var specificDiagnosticOptions = originalSpecificDiagnosticOptions.Add(
                            key: analyzer.Descriptor.Id,
                            value: DiagnosticDescriptor.MapSeverityToReport(effectiveSeverity));
                        compilation = compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(specificDiagnosticOptions));

                        // Verify analyzer diagnostic without suppressor, also verify no suppressions.
                        VerifyAnalyzerDiagnostics(compilation, analyzersWithoutSuppressor, diagnosticNoSuppressor);
                        VerifySuppressedDiagnostics(compilation, analyzersWithoutSuppressor);

                        // Verify suppressed analyzer diagnostic, except when default severity is Error or diagnostic is not-configurable.
                        if (defaultSeverity == DiagnosticSeverity.Error || !configurable)
                        {
                            VerifySuppressedDiagnostics(compilation, analyzersWithSuppressor);
                        }
                        else
                        {
                            VerifySuppressedDiagnostics(compilation, analyzersWithSuppressor, diagnostic);
                        }
                    }
                }
            }
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestExceptionFromSupportedSuppressions()
        {
            string source = "class C { }";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            var expectedException = new NotImplementedException();
            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            var suppressor = new DiagnosticSuppressorThrowsExceptionFromSupportedSuppressions(expectedException);
            var exceptions = new List<Exception>();
            EventHandler<FirstChanceExceptionEventArgs> firstChanceException =
                (sender, e) =>
                {
                    if (e.Exception == expectedException)
                    {
                        exceptions.Add(e.Exception);
                    }
                };

            try
            {
                AppDomain.CurrentDomain.FirstChanceException += firstChanceException;

                IFormattable context = $@"{new LazyToString(() => exceptions[0])}
-----";
                var analyzersAndSuppressors = new DiagnosticAnalyzer[] { analyzer, suppressor };
                VerifyAnalyzerDiagnostics(compilation, analyzersAndSuppressors,
                    Diagnostic("AD0001").WithArguments(suppressor.ToString(), typeof(NotImplementedException).FullName, new NotImplementedException().Message, context).WithLocation(1, 1),
                    Diagnostic("ID1000", "class C { }").WithLocation(1, 1));

                VerifySuppressedDiagnostics(compilation, analyzersAndSuppressors);
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= firstChanceException;
            }
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/41212")]
        [WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        [WorkItem(41212, "https://github.com/dotnet/roslyn/issues/41212")]
        public void TestExceptionFromSuppressor()
        {
            string source = "class C { }";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            var expectedException = new NotImplementedException();
            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            var suppressor = new DiagnosticSuppressorThrowsExceptionFromReportedSuppressions(analyzer.Descriptor.Id, expectedException);
            var exceptions = new List<Exception>();
            EventHandler<FirstChanceExceptionEventArgs> firstChanceException =
                (sender, e) =>
                {
                    if (e.Exception == expectedException)
                    {
                        exceptions.Add(e.Exception);
                    }
                };

            try
            {
                AppDomain.CurrentDomain.FirstChanceException += firstChanceException;

                IFormattable context = $@"{string.Format(CodeAnalysisResources.ExceptionContext, $@"Compilation: {compilation.AssemblyName}")}

{new LazyToString(() => exceptions[0])}
-----";
                var analyzersAndSuppressors = new DiagnosticAnalyzer[] { analyzer, suppressor };
                VerifyAnalyzerDiagnostics(compilation, analyzersAndSuppressors,
                    Diagnostic("AD0001").WithArguments(suppressor.ToString(), typeof(NotImplementedException).FullName, new NotImplementedException().Message, context).WithLocation(1, 1),
                    Diagnostic("ID1000", "class C { }").WithLocation(1, 1));

                VerifySuppressedDiagnostics(compilation, analyzersAndSuppressors);
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= firstChanceException;
            }
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/41212")]
        [WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        [WorkItem(41212, "https://github.com/dotnet/roslyn/issues/41212")]
        public void TestUnsupportedSuppressionReported()
        {
            string source = @"
class C { }";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            const string supportedSuppressionId = "supportedId";
            const string unsupportedSuppressionId = "unsupportedId";
            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            var suppressor = new DiagnosticSuppressor_UnsupportedSuppressionReported(analyzer.Descriptor.Id, supportedSuppressionId, unsupportedSuppressionId);
            var analyzersAndSuppressors = new DiagnosticAnalyzer[] { analyzer, suppressor };

            // "Reported suppression with ID '{0}' is not supported by the suppressor."
            var exceptionMessage = string.Format(CodeAnalysisResources.UnsupportedSuppressionReported, unsupportedSuppressionId);

            var exceptions = new List<Exception>();
            EventHandler<FirstChanceExceptionEventArgs> firstChanceException =
                (sender, e) =>
                {
                    if (e.Exception is ArgumentException
                        && e.Exception.Message == exceptionMessage)
                    {
                        exceptions.Add(e.Exception);
                    }
                };

            try
            {
                AppDomain.CurrentDomain.FirstChanceException += firstChanceException;

                IFormattable context = $@"{string.Format(CodeAnalysisResources.ExceptionContext, $@"Compilation: {compilation.AssemblyName}")}

{new LazyToString(() => exceptions[0])}
-----";
                VerifyAnalyzerDiagnostics(compilation, analyzersAndSuppressors,
                    Diagnostic("AD0001").WithArguments(suppressor.ToString(),
                                                       typeof(ArgumentException).FullName,
                                                       exceptionMessage,
                                                       context)
                                        .WithLocation(1, 1),
                    Diagnostic("ID1000", "class C { }").WithLocation(2, 1));

                VerifySuppressedDiagnostics(compilation, analyzersAndSuppressors);
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= firstChanceException;
            }
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/41212")]
        [WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        [WorkItem(41212, "https://github.com/dotnet/roslyn/issues/41212")]
        public void TestInvalidDiagnosticSuppressionReported()
        {
            string source = @"
class C { }";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            const string unsupportedSuppressedId = "UnsupportedId";
            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            var suppressor = new DiagnosticSuppressor_InvalidDiagnosticSuppressionReported(analyzer.Descriptor.Id, unsupportedSuppressedId);
            var analyzersAndSuppressors = new DiagnosticAnalyzer[] { analyzer, suppressor };

            // "Suppressed diagnostic ID '{0}' does not match suppressable ID '{1}' for the given suppression descriptor."
            var exceptionMessage = string.Format(CodeAnalysisResources.InvalidDiagnosticSuppressionReported, analyzer.Descriptor.Id, unsupportedSuppressedId);

            var exceptions = new List<Exception>();
            EventHandler<FirstChanceExceptionEventArgs> firstChanceException =
                (sender, e) =>
                {
                    if (e.Exception is ArgumentException
                        && e.Exception.Message == exceptionMessage)
                    {
                        exceptions.Add(e.Exception);
                    }
                };

            try
            {
                AppDomain.CurrentDomain.FirstChanceException += firstChanceException;

                IFormattable context = $@"{string.Format(CodeAnalysisResources.ExceptionContext, $@"Compilation: {compilation.AssemblyName}")}

{new LazyToString(() => exceptions[0])}
-----";
                VerifyAnalyzerDiagnostics(compilation, analyzersAndSuppressors,
                    Diagnostic("AD0001").WithArguments(suppressor.ToString(),
                                                       typeof(System.ArgumentException).FullName,
                                                       exceptionMessage,
                                                       context)
                                        .WithLocation(1, 1),
                    Diagnostic("ID1000", "class C { }").WithLocation(2, 1));

                VerifySuppressedDiagnostics(compilation, analyzersAndSuppressors);
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= firstChanceException;
            }
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/41212")]
        [WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        [WorkItem(41212, "https://github.com/dotnet/roslyn/issues/41212")]
        public void TestNonReportedDiagnosticCannotBeSuppressed()
        {
            string source = @"
class C { }";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            const string nonReportedDiagnosticId = "NonReportedId";
            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            var suppressor = new DiagnosticSuppressor_NonReportedDiagnosticCannotBeSuppressed(analyzer.Descriptor.Id, nonReportedDiagnosticId);
            var analyzersAndSuppressors = new DiagnosticAnalyzer[] { analyzer, suppressor };

            // "Non-reported diagnostic with ID '{0}' cannot be suppressed."
            var exceptionMessage = string.Format(CodeAnalysisResources.NonReportedDiagnosticCannotBeSuppressed, nonReportedDiagnosticId);

            var exceptions = new List<Exception>();
            EventHandler<FirstChanceExceptionEventArgs> firstChanceException =
                (sender, e) =>
                {
                    if (e.Exception is ArgumentException
                        && e.Exception.Message == exceptionMessage)
                    {
                        exceptions.Add(e.Exception);
                    }
                };

            try
            {
                AppDomain.CurrentDomain.FirstChanceException += firstChanceException;

                IFormattable context = $@"{string.Format(CodeAnalysisResources.ExceptionContext, $@"Compilation: {compilation.AssemblyName}")}

{new LazyToString(() => exceptions[0])}
-----";
                VerifyAnalyzerDiagnostics(compilation, analyzersAndSuppressors,
                    Diagnostic("AD0001").WithArguments(suppressor.ToString(),
                                                       typeof(System.ArgumentException).FullName,
                                                       exceptionMessage,
                                                       context)
                                        .WithLocation(1, 1),
                    Diagnostic("ID1000", "class C { }").WithLocation(2, 1));

                VerifySuppressedDiagnostics(compilation, analyzersAndSuppressors);
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= firstChanceException;
            }
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestProgrammaticSuppressionInfo_DiagnosticSuppressor()
        {
            string source = @"class C1 { }";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            var expectedDiagnostic = Diagnostic(analyzer.Descriptor.Id, source);
            VerifyAnalyzerDiagnostics(compilation, new DiagnosticAnalyzer[] { analyzer }, expectedDiagnostic);

            const string suppressionId = "SPR1001";
            var suppressor = new DiagnosticSuppressorForId(analyzer.Descriptor.Id, suppressionId);
            var analyzersAndSuppressors = new DiagnosticAnalyzer[] { analyzer, suppressor };
            var diagnostics = compilation.GetAnalyzerDiagnostics(analyzersAndSuppressors, reportSuppressedDiagnostics: true);
            Assert.Single(diagnostics);
            var suppressionInfo = diagnostics.Select(d => d.ProgrammaticSuppressionInfo).Single().Suppressions.Single();
            Assert.Equal(suppressionId, suppressionInfo.Id);
            Assert.Equal(suppressor.SuppressionDescriptor.Justification, suppressionInfo.Justification);

            const string suppressionId2 = "SPR1002";
            var suppressor2 = new DiagnosticSuppressorForId(analyzer.Descriptor.Id, suppressionId2);
            analyzersAndSuppressors = new DiagnosticAnalyzer[] { analyzer, suppressor, suppressor2 };
            diagnostics = compilation.GetAnalyzerDiagnostics(analyzersAndSuppressors, reportSuppressedDiagnostics: true);
            Assert.Single(diagnostics);
            var programmaticSuppression = diagnostics.Select(d => d.ProgrammaticSuppressionInfo).Single();
            Assert.Equal(2, programmaticSuppression.Suppressions.Count);
            var orderedSuppressions = programmaticSuppression.Suppressions.Order().ToImmutableArrayOrEmpty();
            Assert.Equal(suppressionId, orderedSuppressions[0].Id);
            Assert.Equal(suppressor.SuppressionDescriptor.Justification, orderedSuppressions[0].Justification);
            Assert.Equal(suppressionId2, orderedSuppressions[1].Id);
            Assert.Equal(suppressor2.SuppressionDescriptor.Justification, orderedSuppressions[1].Justification);
        }

        [CombinatorialData]
        [Theory, WorkItem(41713, "https://github.com/dotnet/roslyn/issues/41713")]
        public void TestCancellationDuringSuppressorExecution(bool concurrent)
        {
            string source = @"class C1 { }";
            var options = TestOptions.DebugDll.WithConcurrentBuild(concurrent);
            var compilation = CreateCompilation(source, options: options);
            compilation.VerifyDiagnostics();

            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            var expectedDiagnostic = Diagnostic(analyzer.Descriptor.Id, source);
            VerifyAnalyzerDiagnostics(compilation, new DiagnosticAnalyzer[] { analyzer }, expectedDiagnostic);

            var suppressor = new DiagnosticSuppressorForId_ThrowsOperationCancelledException(analyzer.Descriptor.Id);
            var cancellationToken = suppressor.CancellationTokenSource.Token;
            var analyzersAndSuppressors = new DiagnosticAnalyzer[] { analyzer, suppressor };
            Assert.Throws<OperationCanceledException>(() => compilation.GetAnalyzerDiagnostics(analyzersAndSuppressors, reportSuppressedDiagnostics: true, cancellationToken: cancellationToken));
        }
    }
}
