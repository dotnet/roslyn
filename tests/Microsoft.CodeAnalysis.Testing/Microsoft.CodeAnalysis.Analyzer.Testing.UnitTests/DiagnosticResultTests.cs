// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.TestAnalyzers;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class DiagnosticResultTests
    {
        private static DiagnosticResult CompilerError
            => DiagnosticResult.CompilerError("CS1002").WithLocation(1, 1).WithMessage("; expected");

        [Fact]
        public void TestToString()
        {
            Assert.Equal(
                "?(1,1): error CS1002: ; expected",
                CompilerError.ToString());
        }

        [Fact]
        public void TestToStringWithLocation()
        {
            const string Expected = "?(1,2): error CS1002";

            // line/column are provided with 1-based indexes
            Assert.Equal(
                Expected,
                DiagnosticResult.CompilerError("CS1002").WithLocation(1, 2).ToString());

            // LinePosition is 0-based
            Assert.Equal(
                Expected,
                DiagnosticResult.CompilerError("CS1002").WithLocation(new LinePosition(0, 1)).ToString());
        }

        [Fact]
        public void TestToStringWithSpan()
        {
            const string Expected = "?(1,3,2,4): error CS1002";

            // line/column are provided with 1-based indexes
            Assert.Equal(
                Expected,
                DiagnosticResult.CompilerError("CS1002").WithSpan(1, 3, 2, 4).ToString());

            // LinePosition is 0-based
            Assert.Equal(
                Expected,
                DiagnosticResult.CompilerError("CS1002").WithSpan(new FileLinePositionSpan(string.Empty, new LinePosition(0, 2), new LinePosition(1, 3))).ToString());
        }

        [Fact]
        public void TestToStringSeverity()
        {
            Assert.Equal(
                "?(1,1): error CS1002: ; expected",
                CompilerError.WithSeverity(DiagnosticSeverity.Error).ToString());
            Assert.Equal(
                "?(1,1): warning CS1002: ; expected",
                CompilerError.WithSeverity(DiagnosticSeverity.Warning).ToString());
            Assert.Equal(
                "?(1,1): info CS1002: ; expected",
                CompilerError.WithSeverity(DiagnosticSeverity.Info).ToString());
            Assert.Equal(
                "?(1,1): hidden CS1002: ; expected",
                CompilerError.WithSeverity(DiagnosticSeverity.Hidden).ToString());
        }

        [Fact]
        public void TestToStringWithoutLocation()
        {
            Assert.Equal(
                "error CS1002: ; expected",
                DiagnosticResult.CompilerError("CS1002").WithMessage("; expected").ToString());
        }

        [Fact]
        public void TestToStringWithoutMessage()
        {
            Assert.Equal(
                "?(1,1): error CS1002",
                DiagnosticResult.CompilerError("CS1002").WithLocation(1, 1).ToString());
        }

        [Fact]
        public void TestToStringWithoutLocationOrMessage()
        {
            Assert.Equal(
                "error CS1002",
                DiagnosticResult.CompilerError("CS1002").ToString());
        }

        [Fact]
        public void TestToStringWithMessageFormat()
        {
            Assert.Equal(
                "error CS1002: {0} expected",
                DiagnosticResult.CompilerError("CS1002").WithMessageFormat("{0} expected").ToString());
            Assert.Equal(
                "error CS1002: ; expected",
                DiagnosticResult.CompilerError("CS1002").WithMessageFormat("{0} expected").WithArguments(";").ToString());
        }

        [Fact]
        [WorkItem(226, "https://github.com/dotnet/roslyn-sdk/issues/226")]
        public void TestConstructionThroughVerifierActionableError1()
        {
            var error = Assert.Throws<InvalidOperationException>(() => AnalyzerVerifier<TwoDescriptorAnalyzer, CSharpAnalyzerTest<TwoDescriptorAnalyzer>, DefaultVerifier>.Diagnostic());
            Assert.Equal("'Diagnostic()' can only be used when the analyzer has a single supported diagnostic. Use the 'Diagnostic(DiagnosticDescriptor)' overload to specify the descriptor from which to create the expected result.", error.Message);
        }

        [Fact]
        [WorkItem(226, "https://github.com/dotnet/roslyn-sdk/issues/226")]
        public void TestConstructionThroughVerifierActionableError2()
        {
            var error = Assert.Throws<InvalidOperationException>(() => AnalyzerVerifier<TwoDescriptorAnalyzer, CSharpAnalyzerTest<TwoDescriptorAnalyzer>, DefaultVerifier>.Diagnostic("ID"));
            Assert.Equal("'Diagnostic(string)' can only be used when the analyzer has a single supported diagnostic with the specified ID. Use the 'Diagnostic(DiagnosticDescriptor)' overload to specify the descriptor from which to create the expected result.", error.Message);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class TwoDescriptorAnalyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor Descriptor1 = new DiagnosticDescriptor("ID", "title", "first message format", "category", DiagnosticSeverity.Info, isEnabledByDefault: true);
            internal static readonly DiagnosticDescriptor Descriptor2 = new DiagnosticDescriptor("ID", "title", "second message format", "category", DiagnosticSeverity.Info, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
                = ImmutableArray.Create(Descriptor1, Descriptor2);

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            }
        }
    }
}
