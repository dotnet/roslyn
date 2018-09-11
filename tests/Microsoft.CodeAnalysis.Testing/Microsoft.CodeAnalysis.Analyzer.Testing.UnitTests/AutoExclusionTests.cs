// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class AutoExclusionTests
    {
        private const string ReplaceThisWithBaseTestCode = @"
class TestClass {
  void TestMethod() { [|this|].Equals(null); }
}
";

        private const string ReplaceMyClassWithMyBaseTestCode = @"
Class TestClass
  Sub TestMethod()
    [|MyClass|].Equals(Nothing)
  End Sub
End Class
";

        [Fact]
        public async Task TestCSharpAnalyzerWithoutExclusionFails()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpReplaceThisWithBaseTest(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics)
                {
                    TestCode = ReplaceThisWithBaseTestCode,
                }.RunAsync();
            });

            var expected =
                "Mismatch between number of diagnostics returned, expected \"0\" actual \"1\"" + Environment.NewLine +
                Environment.NewLine +
                "Diagnostics:" + Environment.NewLine +
                "// Test0.cs(4,23): warning ThisToBase: message" + Environment.NewLine +
                "GetCSharpResultAt(4, 23, ReplaceThisWithBaseAnalyzer.ThisToBase)" + Environment.NewLine +
                Environment.NewLine;
            Assert.Equal(expected, exception.Message);
        }

        [Theory]
        [InlineData(GeneratedCodeAnalysisFlags.None)]
        [InlineData(GeneratedCodeAnalysisFlags.Analyze)]
        public async Task TestCSharpAnalyzerWithExclusionPasses(GeneratedCodeAnalysisFlags generatedCodeAnalysisFlags)
        {
            await new CSharpReplaceThisWithBaseTest(generatedCodeAnalysisFlags)
            {
                TestCode = ReplaceThisWithBaseTestCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestCSharpAnalyzerWithoutExclusionButAllowedPasses()
        {
            await new CSharpReplaceThisWithBaseTest(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics)
            {
                TestCode = ReplaceThisWithBaseTestCode,
                Exclusions = AnalysisExclusions.None,
            }.RunAsync();
        }

        [Fact]
        [WorkItem(159, "https://github.com/dotnet/roslyn-sdk/pull/159")]
        public async Task TestVisualBasicAnalyzerWithoutExclusionFails()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new VisualBasicReplaceThisWithBaseTest(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics)
                {
                    TestCode = ReplaceMyClassWithMyBaseTestCode,
                }.RunAsync();
            });

            var expected =
                "Mismatch between number of diagnostics returned, expected \"0\" actual \"1\"" + Environment.NewLine +
                Environment.NewLine +
                "Diagnostics:" + Environment.NewLine +
                "// Test0.vb(5,5): warning ThisToBase: message" + Environment.NewLine +
                "GetBasicResultAt(5, 5, ReplaceThisWithBaseAnalyzer.ThisToBase)" + Environment.NewLine +
                Environment.NewLine;
            Assert.Equal(expected, exception.Message);
        }

        [Theory]
        [InlineData(GeneratedCodeAnalysisFlags.None)]
        [InlineData(GeneratedCodeAnalysisFlags.Analyze)]
        [WorkItem(159, "https://github.com/dotnet/roslyn-sdk/pull/159")]
        public async Task TestVisualBasicAnalyzerWithExclusionPasses(GeneratedCodeAnalysisFlags generatedCodeAnalysisFlags)
        {
            await new VisualBasicReplaceThisWithBaseTest(generatedCodeAnalysisFlags)
            {
                TestCode = ReplaceMyClassWithMyBaseTestCode,
            }.RunAsync();
        }

        [Fact]
        [WorkItem(159, "https://github.com/dotnet/roslyn-sdk/pull/159")]
        public async Task TestVisualBasicAnalyzerWithoutExclusionButAllowedPasses()
        {
            await new VisualBasicReplaceThisWithBaseTest(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics)
            {
                TestCode = ReplaceMyClassWithMyBaseTestCode,
                Exclusions = AnalysisExclusions.None,
            }.RunAsync();
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        private class ReplaceThisWithBaseAnalyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor Descriptor =
                new DiagnosticDescriptor("ThisToBase", "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            private readonly GeneratedCodeAnalysisFlags _generatedCodeAnalysisFlags;

            public ReplaceThisWithBaseAnalyzer(GeneratedCodeAnalysisFlags generatedCodeAnalysisFlags)
            {
                _generatedCodeAnalysisFlags = generatedCodeAnalysisFlags;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.ConfigureGeneratedCodeAnalysis(_generatedCodeAnalysisFlags);

                context.RegisterSyntaxNodeAction(HandleThisExpression, CSharp.SyntaxKind.ThisExpression);
                context.RegisterSyntaxNodeAction(HandleMyClassExpression, VisualBasic.SyntaxKind.MyClassExpression);
            }

            private void HandleThisExpression(SyntaxNodeAnalysisContext context)
            {
                var node = (CSharp.Syntax.ThisExpressionSyntax)context.Node;
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, node.Token.GetLocation()));
            }

            private void HandleMyClassExpression(SyntaxNodeAnalysisContext context)
            {
                var node = (VisualBasic.Syntax.MyClassExpressionSyntax)context.Node;
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, node.Keyword.GetLocation()));
            }
        }

        private class CSharpReplaceThisWithBaseTest : AnalyzerTest<DefaultVerifier>
        {
            private readonly GeneratedCodeAnalysisFlags _generatedCodeAnalysisFlags;

            public CSharpReplaceThisWithBaseTest(GeneratedCodeAnalysisFlags generatedCodeAnalysisFlags)
            {
                _generatedCodeAnalysisFlags = generatedCodeAnalysisFlags;
            }

            public override string Language => LanguageNames.CSharp;

            protected override string DefaultFileExt => "cs";

            protected override CompilationOptions CreateCompilationOptions()
            {
                return new CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            }

            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            {
                yield return new ReplaceThisWithBaseAnalyzer(_generatedCodeAnalysisFlags);
            }
        }

        private class VisualBasicReplaceThisWithBaseTest : AnalyzerTest<DefaultVerifier>
        {
            private readonly GeneratedCodeAnalysisFlags _generatedCodeAnalysisFlags;

            public VisualBasicReplaceThisWithBaseTest(GeneratedCodeAnalysisFlags generatedCodeAnalysisFlags)
            {
                _generatedCodeAnalysisFlags = generatedCodeAnalysisFlags;
            }

            public override string Language => LanguageNames.VisualBasic;

            protected override string DefaultFileExt => "vb";

            protected override CompilationOptions CreateCompilationOptions()
            {
                return new VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            }

            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            {
                yield return new ReplaceThisWithBaseAnalyzer(_generatedCodeAnalysisFlags);
            }
        }
    }
}
