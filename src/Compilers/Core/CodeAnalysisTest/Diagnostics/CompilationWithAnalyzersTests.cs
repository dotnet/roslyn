// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;
using KeyValuePairUtil = Roslyn.Utilities.KeyValuePairUtil;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    using SimpleDiagnostic = Diagnostic.SimpleDiagnostic;

    public class CompilationWithAnalyzersTests : TestBase
    {
        [Fact]
        public void GetEffectiveDiagnostics_Errors()
        {
            var c = CSharpCompilation.Create("c");
            var ds = new[] { default(Diagnostic) };

            Assert.Throws<ArgumentNullException>(() => CompilationWithAnalyzers.GetEffectiveDiagnostics(default(ImmutableArray<Diagnostic>), c));
            Assert.Throws<ArgumentNullException>(() => CompilationWithAnalyzers.GetEffectiveDiagnostics(default(IEnumerable<Diagnostic>), c));
            Assert.Throws<ArgumentNullException>(() => CompilationWithAnalyzers.GetEffectiveDiagnostics(ds, null));
        }

        [Fact]
        public void GetEffectiveDiagnostics()
        {
            var c = CSharpCompilation.Create("c", options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).
                WithSpecificDiagnosticOptions(
                    new[] { KeyValuePairUtil.Create($"CS{(int)ErrorCode.WRN_AlwaysNull:D4}", ReportDiagnostic.Suppress) }));

            var d1 = SimpleDiagnostic.Create(MessageProvider.Instance, (int)ErrorCode.WRN_AlignmentMagnitude);
            var d2 = SimpleDiagnostic.Create(MessageProvider.Instance, (int)ErrorCode.WRN_AlwaysNull);
            var ds = new[] { default(Diagnostic), d1, d2 };

            var filtered = CompilationWithAnalyzers.GetEffectiveDiagnostics(ds, c);

            // overwrite the original value to test eagerness:
            ds[1] = default(Diagnostic);

            AssertEx.Equal(new[] { d1 }, filtered);
        }

        [Fact]
        public void GetAnalyzerTelemetry()
        {
            var compilation = CSharpCompilation.Create("c", options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
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
    }
}
