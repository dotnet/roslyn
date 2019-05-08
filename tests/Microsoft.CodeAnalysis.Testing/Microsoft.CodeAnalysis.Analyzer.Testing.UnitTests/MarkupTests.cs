// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

            await new CSharpTest(nestedDiagnostics: false, hiddenDescriptors: false) { TestCode = testCode }.RunAsync();
        }

        [Fact]
        [WorkItem(181, "https://github.com/dotnet/roslyn-sdk/issues/181")]
        public async Task TestCSharpMarkupSingleBracePosition()
        {
            var testCode = @"
class TestClass $${
}
";

            await new CSharpTest(nestedDiagnostics: false, hiddenDescriptors: false) { TestCode = testCode }.RunAsync();
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

            await new CSharpTest(nestedDiagnostics: false, hiddenDescriptors: false) { TestCode = testCode }.RunAsync();
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

            await new CSharpTest(nestedDiagnostics: true, hiddenDescriptors: false) { TestCode = testCode }.RunAsync();
        }

        [Fact]
        public async Task TestCSharpNestedMarkupBraceUnspecifiedIdWithoutDefault()
        {
            var testCode = @"
class TestClass {|BraceOuter:{|Brace:{|}|}
  void TestMethod() {|BraceOuter:[|Brace:{|]|} }
}
";

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new CSharpTest(nestedDiagnostics: true, hiddenDescriptors: false) { TestCode = testCode }.RunAsync());

            var expected = "Markup syntax can only omit the diagnostic ID if the first analyzer only supports a single diagnostic. To customize the default value, override AnalyzerTest<TVerifier>.GetDefaultDiagnostic or specify MarkupOptions.PreferFirstDescriptor.";
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        [WorkItem(189, "https://github.com/dotnet/roslyn-sdk/issues/189")]
        public async Task TestCSharpNestedMarkupBraceMultipleWithoutDefault()
        {
            var testCode = @"
class TestClass {|BraceOuter:{|Brace:{|}|}
  void TestMethod() {|BraceOuter:{|Brace:{|}|} }
}
";

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new CSharpTest(nestedDiagnostics: true, hiddenDescriptors: true) { TestCode = testCode }.RunAsync());

            var expected = "Multiple diagnostic descriptors with ID Brace were found. Use the explicitly diagnostic creation syntax or specify MarkupOptions.PreferFirstDescriptor to use the first matching diagnostic.";
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        [WorkItem(189, "https://github.com/dotnet/roslyn-sdk/issues/189")]
        public async Task TestCSharpNestedMarkupBraceWithDefault()
        {
            var testCode = @"
class TestClass {|BraceOuter:{|Brace:{|}|}
  void TestMethod() {|BraceOuter:[|{|]|} }
}
";

            await new CSharpTest(nestedDiagnostics: true, hiddenDescriptors: false) { TestCode = testCode, MarkupOptions = MarkupOptions.PreferFirstDescriptor }.RunAsync();
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

            public HighlightBracesAnalyzer(bool nestedDiagnostics, bool hiddenDescriptors)
            {
                _nestedDiagnostics = nestedDiagnostics;
                _hiddenDescriptors = hiddenDescriptors;
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
            private readonly bool _hiddenDescriptors;

            public CSharpTest(bool nestedDiagnostics, bool hiddenDescriptors)
            {
                _nestedDiagnostics = nestedDiagnostics;
                _hiddenDescriptors = hiddenDescriptors;
            }

            public override string Language => LanguageNames.CSharp;

            protected override string DefaultFileExt => "cs";

            protected override CompilationOptions CreateCompilationOptions()
                => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            {
                yield return new HighlightBracesAnalyzer(_nestedDiagnostics, _hiddenDescriptors);
            }
        }
    }
}
