// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    using SimpleDiagnostic = Diagnostic.SimpleDiagnostic;

    public class CompilationWithAnalyzersTests : TestBase
    {
        private static readonly CSharpCompilationOptions s_dllWithMaxWarningLevel = new(OutputKind.DynamicallyLinkedLibrary, warningLevel: CodeAnalysis.Diagnostic.MaxWarningLevel);

        [Fact]
        public void GetEffectiveDiagnostics_Errors()
        {
            var c = CSharpCompilation.Create("c");
            var ds = new[] { (Diagnostic)null };

            Assert.Throws<ArgumentNullException>(() => CompilationWithAnalyzers.GetEffectiveDiagnostics(default(ImmutableArray<Diagnostic>), c));
            Assert.Throws<ArgumentNullException>(() => CompilationWithAnalyzers.GetEffectiveDiagnostics(null, c));
            Assert.Throws<ArgumentNullException>(() => CompilationWithAnalyzers.GetEffectiveDiagnostics(ds, null));
        }

        [Fact]
        public void GetEffectiveDiagnostics()
        {
            var c = CSharpCompilation.Create("c", options: s_dllWithMaxWarningLevel.
                WithSpecificDiagnosticOptions(
                    new[] { KeyValuePair.Create($"CS{(int)ErrorCode.WRN_AlwaysNull:D4}", ReportDiagnostic.Suppress) }));

            var d1 = SimpleDiagnostic.Create(MessageProvider.Instance, (int)ErrorCode.WRN_AlignmentMagnitude, "1", "2");
            var d2 = SimpleDiagnostic.Create(MessageProvider.Instance, (int)ErrorCode.WRN_AlwaysNull, "1");
            var ds = new[] { null, d1, d2 };

            var filtered = CompilationWithAnalyzers.GetEffectiveDiagnostics(ds, c);

            // overwrite the original value to test eagerness:
            ds[1] = null;

            AssertEx.Equal(new[] { d1 }, filtered);
        }

        [Fact]
        public void GetAnalyzerTelemetry()
        {
            var compilation = CSharpCompilation.Create("c", options: s_dllWithMaxWarningLevel);
            DiagnosticAnalyzer analyzer = new AnalyzerWithDisabledRules();
            var analyzers = ImmutableArray.Create(analyzer);
            var analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
            var compWithAnalyzers = new CompilationWithAnalyzers(compilation, analyzers, analyzerOptions);

            var analysisResult = compWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None).Result;
            Assert.Empty(analysisResult.CompilationDiagnostics);

            // Even though the analyzer registers a symbol action, it should never be invoked because all of its rules are disabled.
            var analyzerTelemetry = compWithAnalyzers.GetAnalyzerTelemetryInfoAsync(analyzer, CancellationToken.None).Result;
            Assert.Equal(0, analyzerTelemetry.SymbolActionsCount);
        }

        [Fact, Obsolete(message: "IsDiagnosticAnalyzerSuppressed is an obsolete public API")]
        public void TestIsDiagnosticAnalyzerSuppressedWithExceptionInSupportedDiagnostics()
        {
            // Verify IsDiagnosticAnalyzerSuppressed does not throw an exception when 'onAnalyzerException' is null.
            var analyzer = new AnalyzerThatThrowsInSupportedDiagnostics();
            _ = CompilationWithAnalyzers.IsDiagnosticAnalyzerSuppressed(analyzer, s_dllWithMaxWarningLevel, onAnalyzerException: null);
        }

        [Fact]
        public async Task AnalyzerWithInfoSeverityIsSkippedOnCommandLine()
        {
            // Verify that an analyzer with only Info severity diagnostics is skipped on command line (Hidden and Info filtered)
            var source = "class C { }";
            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create("test", new[] { tree }, options: s_dllWithMaxWarningLevel);

            var analyzer = new InfoSeverityAnalyzer();
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);
            var analyzerManager = new AnalyzerManager(analyzers);

            // Simulate command line build: filter out Hidden and Info diagnostics
            var severityFilter = SeverityFilter.Hidden | SeverityFilter.Info;
            var driver = AnalyzerDriver.CreateAndAttachToCompilation(
                compilation, analyzers, new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty), analyzerManager,
                onAnalyzerException: null, analyzerExceptionFilter: null, reportAnalyzer: false,
                severityFilter, trackSuppressedDiagnosticIds: false, out var newCompilation, CancellationToken.None);

            // Force complete compilation event queue and analyzer execution.
            _ = newCompilation.GetDiagnostics(CancellationToken.None);
            var diagnostics = await driver.GetDiagnosticsAsync(newCompilation, CancellationToken.None);

            // Verify analyzer was not invoked (no callbacks executed)
            Assert.Empty(analyzer.CallbackSymbols);

            // Verify no diagnostics were reported
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task AnalyzerWithHiddenSeverityIsSkippedOnCommandLine()
        {
            // Verify that an analyzer with only Hidden severity diagnostics is skipped on command line (Hidden and Info filtered)
            var source = "class C { }";
            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create("test", new[] { tree }, options: s_dllWithMaxWarningLevel);

            var analyzer = new HiddenSeverityAnalyzer();
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);
            var analyzerManager = new AnalyzerManager(analyzers);

            // Simulate command line build: filter out Hidden and Info diagnostics
            var severityFilter = SeverityFilter.Hidden | SeverityFilter.Info;
            var driver = AnalyzerDriver.CreateAndAttachToCompilation(
                compilation, analyzers, new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty), analyzerManager,
                onAnalyzerException: null, analyzerExceptionFilter: null, reportAnalyzer: false,
                severityFilter, trackSuppressedDiagnosticIds: false, out var newCompilation, CancellationToken.None);

            // Force complete compilation event queue and analyzer execution.
            _ = newCompilation.GetDiagnostics(CancellationToken.None);
            var diagnostics = await driver.GetDiagnosticsAsync(newCompilation, CancellationToken.None);

            // Verify analyzer was not invoked (no callbacks executed)
            Assert.Empty(analyzer.CallbackSymbols);

            // Verify no diagnostics were reported
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task AnalyzerWithInfoSeverityIsNotSkippedInIDE()
        {
            // Verify that an analyzer with Info severity diagnostics is NOT skipped in IDE (no severity filter)
            var source = "class C { }";
            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create("test", new[] { tree }, options: s_dllWithMaxWarningLevel);

            var analyzer = new InfoSeverityAnalyzer();
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);
            var analyzerManager = new AnalyzerManager(analyzers);

            // Simulate IDE scenario: no severity filter
            var severityFilter = SeverityFilter.None;
            var driver = AnalyzerDriver.CreateAndAttachToCompilation(
                compilation, analyzers, new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty), analyzerManager,
                onAnalyzerException: null, analyzerExceptionFilter: null, reportAnalyzer: false,
                severityFilter, trackSuppressedDiagnosticIds: false, out var newCompilation, CancellationToken.None);

            // Force complete compilation event queue and analyzer execution.
            _ = newCompilation.GetDiagnostics(CancellationToken.None);
            var diagnostics = await driver.GetDiagnosticsAsync(newCompilation, CancellationToken.None);

            // Verify analyzer was invoked (callbacks executed)
            Assert.NotEmpty(analyzer.CallbackSymbols);

            // Verify diagnostic was reported
            Assert.Single(diagnostics);
        }

        [Fact]
        public async Task AnalyzerWithInfoSeverityNotConfigurableIsNotSkippedOnCommandLine()
        {
            // Verify that an analyzer with Info severity diagnostics marked as NotConfigurable is NOT skipped on command line
            // This is the edge case where NotConfigurable diagnostics are always run, even if their severity would normally be filtered
            var source = "class C { }";
            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create("test", new[] { tree }, options: s_dllWithMaxWarningLevel);

            var analyzer = new InfoSeverityNotConfigurableAnalyzer();
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);
            var analyzerManager = new AnalyzerManager(analyzers);

            // Simulate command line build: filter out Hidden and Info diagnostics
            var severityFilter = SeverityFilter.Hidden | SeverityFilter.Info;
            var driver = AnalyzerDriver.CreateAndAttachToCompilation(
                compilation, analyzers, new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty), analyzerManager,
                onAnalyzerException: null, analyzerExceptionFilter: null, reportAnalyzer: false,
                severityFilter, trackSuppressedDiagnosticIds: false, out var newCompilation, CancellationToken.None);

            // Force complete compilation event queue and analyzer execution.
            _ = newCompilation.GetDiagnostics(CancellationToken.None);
            var diagnostics = await driver.GetDiagnosticsAsync(newCompilation, CancellationToken.None);

            // Verify analyzer WAS invoked (callbacks executed) because the diagnostic is NotConfigurable
            Assert.NotEmpty(analyzer.CallbackSymbols);

            // Verify diagnostic was NOT reported (it's filtered by severity filter even though analyzer ran)
            Assert.Empty(diagnostics);
        }
    }
}
