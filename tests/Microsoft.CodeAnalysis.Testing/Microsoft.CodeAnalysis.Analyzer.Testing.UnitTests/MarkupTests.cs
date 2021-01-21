// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.TestAnalyzers;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class MarkupTests
    {
        [Theory]
        [CombinatorialData]
        [WorkItem(189, "https://github.com/dotnet/roslyn-sdk/issues/189")]
        public async Task TestCSharpMarkupBrace(bool reportAdditionalLocations)
        {
            var testCode = @"
class TestClass {|Brace:{|}
  void TestMethod() {|Brace:{|} }
}
";

            await new CSharpTest(nestedDiagnostics: false, hiddenDescriptors: false, reportAdditionalLocations: reportAdditionalLocations) { TestCode = testCode }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(181, "https://github.com/dotnet/roslyn-sdk/issues/181")]
        public async Task TestCSharpMarkupSingleBracePosition(bool reportAdditionalLocations)
        {
            var testCode = @"
class TestClass $${
}
";

            await new CSharpTest(nestedDiagnostics: false, hiddenDescriptors: false, reportAdditionalLocations: reportAdditionalLocations) { TestCode = testCode }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(181, "https://github.com/dotnet/roslyn-sdk/issues/181")]
        public async Task TestCSharpMarkupMultipleBracePositions(bool reportAdditionalLocations)
        {
            var testCode = @"
class TestClass $${
  void TestMethod() $${ }
}
";

            await new CSharpTest(nestedDiagnostics: false, hiddenDescriptors: false, reportAdditionalLocations: reportAdditionalLocations) { TestCode = testCode }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(189, "https://github.com/dotnet/roslyn-sdk/issues/189")]
        public async Task TestCSharpNestedMarkupBrace(bool reportAdditionalLocations)
        {
            var testCode = @"
class TestClass {|BraceOuter:{|Brace:{|}|}
  void TestMethod() {|BraceOuter:{|Brace:{|}|} }
}
";

            await new CSharpTest(nestedDiagnostics: true, hiddenDescriptors: false, reportAdditionalLocations: reportAdditionalLocations) { TestCode = testCode }.RunAsync();
        }

        [Fact]
        [WorkItem(411, "https://github.com/dotnet/roslyn-sdk/issues/411")]
        public async Task TestCSharpNestedMarkupBraceWithCombinedSyntax()
        {
            var testCode = @"
class TestClass {|#0:{|}
  void TestMethod() {|#1:{|} }
}
";

            await new CSharpTest(nestedDiagnostics: true, hiddenDescriptors: false, reportAdditionalLocations: false)
            {
                TestCode = testCode,
                ExpectedDiagnostics =
                {
                    new DiagnosticResult(HighlightBracesAnalyzer.DescriptorOuter).WithLocation(0),
                    new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithLocation(0),
                    new DiagnosticResult(HighlightBracesAnalyzer.DescriptorOuter).WithLocation(1),
                    new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithLocation(1),
                },
            }.RunAsync();
        }

        [Fact]
        [WorkItem(411, "https://github.com/dotnet/roslyn-sdk/issues/411")]
        public async Task TestCSharpNestedMarkupBraceWithCombinedSyntaxInSecondFile()
        {
            var testCode1 = string.Empty;
            var testCode2 = @"
class TestClass {|#0:{|}
  void TestMethod() {|#1:{|} }
}
";

            await new CSharpTest(nestedDiagnostics: true, hiddenDescriptors: false, reportAdditionalLocations: false)
            {
                TestState =
                {
                    Sources = { testCode1, testCode2 },
                },
                ExpectedDiagnostics =
                {
                    new DiagnosticResult(HighlightBracesAnalyzer.DescriptorOuter).WithLocation(0),
                    new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithLocation(0),
                    new DiagnosticResult(HighlightBracesAnalyzer.DescriptorOuter).WithLocation(1),
                    new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithLocation(1),
                },
            }.RunAsync();
        }

        [Fact]
        [WorkItem(411, "https://github.com/dotnet/roslyn-sdk/issues/411")]
        public async Task TestCSharpNestedMarkupBraceWithCombinedSyntaxAndAdditionalLocations()
        {
            var testCode = @"
class TestClass {|#0:{|}
  void TestMethod() {|#1:{|} {|#2:}|}
{|#3:}|}
";

            await new CSharpTest(nestedDiagnostics: true, hiddenDescriptors: false, reportAdditionalLocations: true)
            {
                TestCode = testCode,
                ExpectedDiagnostics =
                {
                    new DiagnosticResult(HighlightBracesAnalyzer.DescriptorOuter).WithLocation(0).WithLocation(3),
                    new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithLocation(0).WithLocation(3),
                    new DiagnosticResult(HighlightBracesAnalyzer.DescriptorOuter).WithLocation(1).WithLocation(2),
                    new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithLocation(1).WithLocation(2),
                },
            }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task TestCSharpNestedMarkupBraceUnspecifiedIdWithoutDefault(bool reportAdditionalLocations)
        {
            var testCode = @"
class TestClass {|BraceOuter:{|Brace:{|}|}
  void TestMethod() {|BraceOuter:[|Brace:{|]|} }
}
";

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new CSharpTest(nestedDiagnostics: true, hiddenDescriptors: false, reportAdditionalLocations: reportAdditionalLocations) { TestCode = testCode }.RunAsync());

            var expected = "Markup syntax can only omit the diagnostic ID if the first analyzer only supports a single diagnostic. To customize the default value, override AnalyzerTest<TVerifier>.GetDefaultDiagnostic or specify MarkupOptions.UseFirstDescriptor.";
            Assert.Equal(expected, exception.Message);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(189, "https://github.com/dotnet/roslyn-sdk/issues/189")]
        public async Task TestCSharpNestedMarkupBraceMultipleWithoutDefault(bool reportAdditionalLocations)
        {
            var testCode = @"
class TestClass {|BraceOuter:{|Brace:{|}|}
  void TestMethod() {|BraceOuter:{|Brace:{|}|} }
}
";

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new CSharpTest(nestedDiagnostics: true, hiddenDescriptors: true, reportAdditionalLocations: reportAdditionalLocations) { TestCode = testCode }.RunAsync());

            var expected = "Multiple diagnostic descriptors with ID Brace were found. Use the explicitly diagnostic creation syntax or specify MarkupOptions.UseFirstDescriptor to use the first matching diagnostic.";
            Assert.Equal(expected, exception.Message);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(189, "https://github.com/dotnet/roslyn-sdk/issues/189")]
        public async Task TestCSharpNestedMarkupBraceWithDefault(bool reportAdditionalLocations)
        {
            var testCode = @"
class TestClass {|BraceOuter:{|Brace:{|}|}
  void TestMethod() {|BraceOuter:[|{|]|} }
}
";

            await new CSharpTest(nestedDiagnostics: true, hiddenDescriptors: false, reportAdditionalLocations: reportAdditionalLocations) { TestCode = testCode, MarkupOptions = MarkupOptions.UseFirstDescriptor }.RunAsync();
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class HighlightBracesAnalyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor DescriptorOuter =
                new DiagnosticDescriptor("BraceOuter", "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            internal static readonly DiagnosticDescriptor DescriptorOuterHidden =
                new DiagnosticDescriptor("BraceOuter", "title", "message", "category", DiagnosticSeverity.Hidden, isEnabledByDefault: true);

            internal static readonly DiagnosticDescriptor Descriptor =
                new DiagnosticDescriptor("Brace", "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            internal static readonly DiagnosticDescriptor DescriptorHidden =
                new DiagnosticDescriptor("Brace", "title", "message", "category", DiagnosticSeverity.Hidden, isEnabledByDefault: true);

            private readonly bool _nestedDiagnostics;
            private readonly bool _hiddenDescriptors;
            private readonly bool _reportAdditionalLocations;

            public HighlightBracesAnalyzer(bool nestedDiagnostics, bool hiddenDescriptors, bool reportAdditionalLocations)
            {
                _nestedDiagnostics = nestedDiagnostics;
                _hiddenDescriptors = hiddenDescriptors;
                _reportAdditionalLocations = reportAdditionalLocations;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    var builder = ImmutableArray.CreateBuilder<DiagnosticDescriptor>();
                    builder.Add(Descriptor);
                    if (_hiddenDescriptors)
                    {
                        builder.Add(DescriptorHidden);
                    }

                    if (_nestedDiagnostics)
                    {
                        builder.Add(DescriptorOuter);
                        if (_hiddenDescriptors)
                        {
                            builder.Add(DescriptorOuterHidden);
                        }
                    }

                    return builder.ToImmutable();
                }
            }

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
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

                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, token.GetLocation(), additionalLocations: GetAdditionalLocations(token)));

                    if (_nestedDiagnostics)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(DescriptorOuter, token.GetLocation(), additionalLocations: GetAdditionalLocations(token)));
                    }
                }
            }

            private IEnumerable<Location> GetAdditionalLocations(SyntaxToken token)
            {
                if (_reportAdditionalLocations)
                {
                    yield return token.Parent!.ChildTokens().Single(t => t.IsKind(SyntaxKind.CloseBraceToken)).GetLocation();
                }
            }
        }

        private class CSharpTest : CSharpAnalyzerTest<EmptyDiagnosticAnalyzer>
        {
            private readonly bool _nestedDiagnostics;
            private readonly bool _hiddenDescriptors;
            private readonly bool _reportAdditionalLocations;

            public CSharpTest(bool nestedDiagnostics, bool hiddenDescriptors, bool reportAdditionalLocations)
            {
                _nestedDiagnostics = nestedDiagnostics;
                _hiddenDescriptors = hiddenDescriptors;
                _reportAdditionalLocations = reportAdditionalLocations;
            }

            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            {
                yield return new HighlightBracesAnalyzer(_nestedDiagnostics, _hiddenDescriptors, _reportAdditionalLocations);
            }
        }
    }
}
