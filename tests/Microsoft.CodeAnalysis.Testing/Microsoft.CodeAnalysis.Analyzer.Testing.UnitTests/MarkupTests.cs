// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
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
                    yield return token.Parent.ChildTokens().Single(t => t.IsKind(SyntaxKind.CloseBraceToken)).GetLocation();
                }
            }
        }

        private class CSharpTest : AnalyzerTest<DefaultVerifier>
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

            public override string Language => LanguageNames.CSharp;

            protected override string DefaultFileExt => "cs";

            protected override CompilationOptions CreateCompilationOptions()
                => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            {
                yield return new HighlightBracesAnalyzer(_nestedDiagnostics, _hiddenDescriptors, _reportAdditionalLocations);
            }
        }
    }
}
