// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;
using static Roslyn.Test.Utilities.TestHelpers;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    using SimpleDiagnostic = Diagnostic.SimpleDiagnostic;

    public class CompilationWithAnalyzersTests : TestBase
    {
        private static CSharpCompilation CreateCSharpCompilation(
            string source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null)
            => CSharpCompilation.Create(
                Guid.NewGuid().ToString(),
                SpecializedCollections.SingletonEnumerable(CSharp.SyntaxFactory.ParseSyntaxTree(source)),
                references ?? SpecializedCollections.SingletonEnumerable(MscorlibRef),
                options);

        private static VisualBasicCompilation CreateBasicCompilation(
            string source,
            IEnumerable<MetadataReference> references = null,
            VisualBasicCompilationOptions options = null)
            => VisualBasicCompilation.Create(
                Guid.NewGuid().ToString(),
                SpecializedCollections.SingletonEnumerable(VisualBasic.SyntaxFactory.ParseSyntaxTree(source)),
                references ?? SpecializedCollections.SingletonEnumerable(MscorlibRef),
                options);

        [Fact]
        public void CSharpGetEffectiveDiagnosticsPragmaSuppressed()
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var c = CreateCSharpCompilation(@"
class C
{
#pragma warning disable CS0169
    int _f;
#pragma warning restore CS0169
}", options: options);
            CompilationWithAnalyzers.GetEffectiveDiagnostics(c.GetDiagnostics(), c).Verify();

            options = options.WithReportSuppressedDiagnostics(true);
            c = c.WithOptions(options);
            CompilationWithAnalyzers.GetEffectiveDiagnostics(c.GetDiagnostics(), c).Verify(
                // (5,9): warning CS0169: The field 'C._f' is never used
                //     int _f;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "_f").WithArguments("C._f").WithLocation(5, 9));
        }

        [Fact]
        public void CSharpGetEffectiveDiagnosticsSpecificSuppressed()
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var c = CreateCSharpCompilation("class C { int _f; }", options: options);
            CompilationWithAnalyzers.GetEffectiveDiagnostics(c.GetDiagnostics(), c).Verify(
                // (1,15): warning CS0169: The field 'C._f' is never used
                // class C { int _f; }
                Diagnostic(ErrorCode.WRN_UnreferencedField, "_f").WithArguments("C._f").WithLocation(1, 15));

            options = options.WithSpecificDiagnosticOptions(CreateImmutableDictionary(("CS0169", ReportDiagnostic.Suppress)));
            c = c.WithOptions(options);
            CompilationWithAnalyzers.GetEffectiveDiagnostics(c.GetDiagnostics(), c).Verify();

            options = options.WithReportSuppressedDiagnostics(true);
            c = c.WithOptions(options);
            CompilationWithAnalyzers.GetEffectiveDiagnostics(c.GetDiagnostics(), c).Verify(
                // (1,15): warning CS0169: The field 'C._f' is never used
                // class C { int _f; }
                Diagnostic(ErrorCode.WRN_UnreferencedField, "_f").WithArguments("C._f").WithLocation(1, 15));
        }

        [Fact]
        public void CSharpGetEffectiveDiagnosticsGeneralSuppressed()
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var c = CreateCSharpCompilation("class C { int _f; }", options: options);
            CompilationWithAnalyzers.GetEffectiveDiagnostics(c.GetDiagnostics(), c).Verify(
                // (1,15): warning CS0169: The field 'C._f' is never used
                // class C { int _f; }
                Diagnostic(ErrorCode.WRN_UnreferencedField, "_f").WithArguments("C._f").WithLocation(1, 15));

            options = options.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress);
            c = c.WithOptions(options);
            CompilationWithAnalyzers.GetEffectiveDiagnostics(c.GetDiagnostics(), c).Verify();

            options = options.WithReportSuppressedDiagnostics(true);
            c = c.WithOptions(options);
            CompilationWithAnalyzers.GetEffectiveDiagnostics(c.GetDiagnostics(), c).Verify(
                // (1,15): warning CS0169: The field 'C._f' is never used
                // class C { int _f; }
                Diagnostic(ErrorCode.WRN_UnreferencedField, "_f").WithArguments("C._f").WithLocation(1, 15));
        }

        [Fact]
        public void BasicGetEffectiveDiagnosticsPragmaSuppressed()
        {
            var options = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var c = CreateBasicCompilation(@"
Class C
    Sub M()
#Disable Warning BC42024
        Dim x as Integer
#Enable Warning BC42024
    End Sub
End Class", options: options);
            CompilationWithAnalyzers.GetEffectiveDiagnostics(c.GetDiagnostics(), c).Verify();

            options = options.WithReportSuppressedDiagnostics(true);
            c = c.WithOptions(options);
            CompilationWithAnalyzers.GetEffectiveDiagnostics(c.GetDiagnostics(), c).Verify(
                Diagnostic(ERRID.WRN_UnusedLocal, "x").WithArguments("x").WithLocation(5, 13));
        }

        [Fact]
        public void BasicGetEffectiveDiagnosticsSpecificSuppressed()
        {
            var options = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var c = CreateBasicCompilation(@"
Class C
    Sub M()
        Dim x as Integer
    End Sub
End Class", options: options);
            CompilationWithAnalyzers.GetEffectiveDiagnostics(c.GetDiagnostics(), c).Verify(
                Diagnostic(ERRID.WRN_UnusedLocal, "x").WithArguments("x").WithLocation(4, 13));

            options = options.WithSpecificDiagnosticOptions(CreateImmutableDictionary(("BC42024", ReportDiagnostic.Suppress)));
            c = c.WithOptions(options);
            CompilationWithAnalyzers.GetEffectiveDiagnostics(c.GetDiagnostics(), c).Verify();

            options = options.WithReportSuppressedDiagnostics(true);
            c = c.WithOptions(options);
            CompilationWithAnalyzers.GetEffectiveDiagnostics(c.GetDiagnostics(), c).Verify(
                Diagnostic(ERRID.WRN_UnusedLocal, "x").WithArguments("x").WithLocation(4, 13));
        }

        [Fact]
        public void BasicGetEffectiveDiagnosticsGeneralSuppressed()
        {
            var options = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var c = CreateBasicCompilation(@"
Class C
    Sub M()
        Dim x as Integer
    End Sub
End Class", options: options);
            CompilationWithAnalyzers.GetEffectiveDiagnostics(c.GetDiagnostics(), c).Verify(
                Diagnostic(ERRID.WRN_UnusedLocal, "x").WithArguments("x").WithLocation(4, 13));

            options = options.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress);
            c = c.WithOptions(options);
            CompilationWithAnalyzers.GetEffectiveDiagnostics(c.GetDiagnostics(), c).Verify();

            options = options.WithReportSuppressedDiagnostics(true);
            c = c.WithOptions(options);
            CompilationWithAnalyzers.GetEffectiveDiagnostics(c.GetDiagnostics(), c).Verify(
                Diagnostic(ERRID.WRN_UnusedLocal, "x").WithArguments("x").WithLocation(4, 13));
        }

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
                    new[] { KeyValuePair.Create($"CS{(int)ErrorCode.WRN_AlwaysNull:D4}", ReportDiagnostic.Suppress) }));

            var d1 = SimpleDiagnostic.Create(CSharp.MessageProvider.Instance, (int)ErrorCode.WRN_AlignmentMagnitude);
            var d2 = SimpleDiagnostic.Create(CSharp.MessageProvider.Instance, (int)ErrorCode.WRN_AlwaysNull);
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
