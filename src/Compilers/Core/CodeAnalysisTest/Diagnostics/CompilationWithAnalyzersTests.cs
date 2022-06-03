// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;
using KeyValuePairUtil = Roslyn.Utilities.KeyValuePairUtil;

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
                    new[] { KeyValuePairUtil.Create($"CS{(int)ErrorCode.WRN_AlwaysNull:D4}", ReportDiagnostic.Suppress) }));

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
            var compWithAnalyzers = new CompilationWithAnalyzers(compilation, analyzers, analyzerOptions, CancellationToken.None);

            var analysisResult = compWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None).Result;
            Assert.Empty(analysisResult.CompilationDiagnostics);

            // Even though the analyzer registers a symbol action, it should never be invoked because all of its rules are disabled.
            var analyzerTelemetry = compWithAnalyzers.GetAnalyzerTelemetryInfoAsync(analyzer, CancellationToken.None).Result;
            Assert.Equal(0, analyzerTelemetry.SymbolActionsCount);
        }

        [Fact]
        public void TestIsDiagnosticAnalyzerSuppressedWithExceptionInSupportedDiagnostics()
        {
            // Verify IsDiagnosticAnalyzerSuppressed does not throw an exception when 'onAnalyzerException' is null.
            var analyzer = new AnalyzerThatThrowsInSupportedDiagnostics();
            _ = CompilationWithAnalyzers.IsDiagnosticAnalyzerSuppressed(analyzer, s_dllWithMaxWarningLevel, onAnalyzerException: null);
        }
    }
}
