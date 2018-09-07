// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class MarkupTests
    {
        [Fact]
        [WorkItem(189, "https://github.com/dotnet/roslyn-sdk/issues/189")]
        public async Task TestCSharpMarkupBrace()
        {
            var testCode = @"
class TestClass {|Brace:{|}
  void TestMethod() {|Brace:{|} }
}
";

            await new CSharpTest(nestedDiagnostics: false) { TestCode = testCode }.RunAsync();
        }

        [Fact]
        [WorkItem(181, "https://github.com/dotnet/roslyn-sdk/issues/181")]
        public async Task TestCSharpMarkupSingleBracePosition()
        {
            var testCode = @"
class TestClass $${
}
";

            await new CSharpTest(nestedDiagnostics: false) { TestCode = testCode }.RunAsync();
        }

        [Fact]
        [WorkItem(181, "https://github.com/dotnet/roslyn-sdk/issues/181")]
        public async Task TestCSharpMarkupMultipleBracePositions()
        {
            var testCode = @"
class TestClass $${
  void TestMethod() $${ }
}
";

            await new CSharpTest(nestedDiagnostics: false) { TestCode = testCode }.RunAsync();
        }

        [Fact]
        [WorkItem(189, "https://github.com/dotnet/roslyn-sdk/issues/189")]
        public async Task TestCSharpNestedMarkupBrace()
        {
            var testCode = @"
class TestClass {|BraceOuter:{|Brace:{|}|}
  void TestMethod() {|BraceOuter:{|Brace:{|}|} }
}
";

            await new CSharpTest(nestedDiagnostics: true) { TestCode = testCode }.RunAsync();
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class HighlightBracesAnalyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor DescriptorOuter =
                new DiagnosticDescriptor("BraceOuter", "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            internal static readonly DiagnosticDescriptor Descriptor =
                new DiagnosticDescriptor("Brace", "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            private readonly bool _nestedDiagnostics;

            public HighlightBracesAnalyzer(bool nestedDiagnostics)
            {
                _nestedDiagnostics = nestedDiagnostics;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                => _nestedDiagnostics ? ImmutableArray.Create(Descriptor, DescriptorOuter) : ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

                context.RegisterSyntaxTreeAction(HandleSyntaxTree);
            }

            private void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
            {
                foreach (var token in context.Tree.GetRoot(context.CancellationToken).DescendantTokens())
                {
                    if (!token.IsKind(SyntaxKind.OpenBraceToken))
                    {
                        continue;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, token.GetLocation()));

                    if (_nestedDiagnostics)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(DescriptorOuter, token.GetLocation()));
                    }
                }
            }
        }

        private class CSharpTest : AnalyzerTest<DefaultVerifier>
        {
            private readonly bool _nestedDiagnostics;

            public CSharpTest(bool nestedDiagnostics)
            {
                _nestedDiagnostics = nestedDiagnostics;
            }

            public override string Language => LanguageNames.CSharp;

            protected override string DefaultFileExt => "cs";

            protected override CompilationOptions CreateCompilationOptions()
                => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            {
                yield return new HighlightBracesAnalyzer(_nestedDiagnostics);
            }
        }
    }
}
