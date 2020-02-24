// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Code fix tests for scenarios where multiple fixes are available.
    /// </summary>
    public class MultipleCodeFixesTests
    {
        [Fact]
        [WorkItem(171, "https://github.com/dotnet/roslyn-sdk/issues/171")]
        public async Task TestDefaultSelection()
        {
            var testCode = @"
class TestClass {
  int field = [|0|];
}
";
            var fixedCode = $@"
class TestClass {{
  int field = 1;
}}
";

            // A single CodeFixProvider provides three actions
            var codeFixes = ImmutableArray.Create(ImmutableArray.Create(1, 2, 3));
            await new CSharpTest(codeFixes)
            {
                TestCode = testCode,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestDefaultSelectionMultipleFixers()
        {
            var testCode = @"
class TestClass {
  int field = [|0|];
}
";
            var fixedCode = $@"
class TestClass {{
  int field = 1;
}}
";

            // Three CodeFixProviders provide three actions
            var codeFixes = ImmutableArray.Create(
                ImmutableArray.Create(1),
                ImmutableArray.Create(2),
                ImmutableArray.Create(3));
            await new CSharpTest(codeFixes)
            {
                TestCode = testCode,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [WorkItem(171, "https://github.com/dotnet/roslyn-sdk/issues/171")]
        public async Task TestSelectionByIndex(int index)
        {
            var testCode = @"
class TestClass {
  int field = [|0|];
}
";
            var fixedCode = $@"
class TestClass {{
  int field = {index + 1};
}}
";

            // A single CodeFixProvider provides three actions
            var codeFixes = ImmutableArray.Create(ImmutableArray.Create(1, 2, 3));
            await new CSharpTest(codeFixes)
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                CodeActionIndex = index,
            }.RunAsync();
        }

        [Theory]
        [InlineData(0, "ReplaceZeroFix_1")]
        [InlineData(1, "ReplaceZeroFix_2")]
        [InlineData(2, "ReplaceZeroFix_3")]
        [WorkItem(171, "https://github.com/dotnet/roslyn-sdk/issues/171")]
        public async Task TestSelectionByEquivalenceKey(int index, string equivalenceKey)
        {
            var testCode = @"
class TestClass {
  int field = [|0|];
}
";
            var fixedCode = $@"
class TestClass {{
  int field = {index + 1};
}}
";

            // A single CodeFixProvider provides three actions
            var codeFixes = ImmutableArray.Create(ImmutableArray.Create(1, 2, 3));
            await new CSharpTest(codeFixes)
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                CodeActionEquivalenceKey = equivalenceKey,
            }.RunAsync();
        }

        [Theory]
        [InlineData(0, "ReplaceZeroFix_1")]
        [InlineData(1, "ReplaceZeroFix_2")]
        [InlineData(2, "ReplaceZeroFix_3")]
        [WorkItem(171, "https://github.com/dotnet/roslyn-sdk/issues/171")]
        public async Task TestIndexAndEquivalenceKeyMatch(int index, string equivalenceKey)
        {
            var testCode = @"
class TestClass {
  int field = [|0|];
}
";
            var fixedCode = $@"
class TestClass {{
  int field = {index + 1};
}}
";

            // A single CodeFixProvider provides three actions
            var codeFixes = ImmutableArray.Create(ImmutableArray.Create(1, 2, 3));
            await new CSharpTest(codeFixes)
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                CodeActionIndex = index,
                CodeActionEquivalenceKey = equivalenceKey,
            }.RunAsync();
        }

        [Theory]
        [InlineData(0, "ReplaceZeroFix_1")]
        [InlineData(1, "ReplaceZeroFix_2")]
        [InlineData(2, "ReplaceZeroFix_3")]
        public async Task TestIndexAndEquivalenceKeyMatchMultipleFixers(int index, string equivalenceKey)
        {
            var testCode = @"
class TestClass {
  int field = [|0|];
}
";
            var fixedCode = $@"
class TestClass {{
  int field = {index + 1};
}}
";

            // Three CodeFixProviders provide three actions
            var codeFixes = ImmutableArray.Create(
                ImmutableArray.Create(1),
                ImmutableArray.Create(2),
                ImmutableArray.Create(3));
            await new CSharpTest(codeFixes)
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                CodeActionIndex = index,
                CodeActionEquivalenceKey = equivalenceKey,
            }.RunAsync();
        }

        [Fact]
        [WorkItem(171, "https://github.com/dotnet/roslyn-sdk/issues/171")]
        public async Task TestIndexAndEquivalenceKeyMismatch()
        {
            var testCode = @"
class TestClass {
  int field = [|0|];
}
";
            var fixedCode = $@"
class TestClass {{
  int field = 2;
}}
";

            // A single CodeFixProvider provides three actions
            var codeFixes = ImmutableArray.Create(ImmutableArray.Create(1, 2, 3));
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest(codeFixes)
                {
                    TestCode = testCode,
                    FixedCode = fixedCode,
                    CodeActionIndex = 1,
                    CodeActionEquivalenceKey = "ReplaceZeroFix_1",
                }.RunAsync();
            });

            Assert.Equal($"Context: Iterative code fix application{Environment.NewLine}The code action equivalence key and index must be consistent when both are specified.", exception.Message);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class LiteralZeroAnalyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor Descriptor =
                new DiagnosticDescriptor("LiteralZero", "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

                context.RegisterSyntaxNodeAction(HandleNumericLiteralExpression, SyntaxKind.NumericLiteralExpression);
            }

            private void HandleNumericLiteralExpression(SyntaxNodeAnalysisContext context)
            {
                var node = (LiteralExpressionSyntax)context.Node;
                if (node.Token.ValueText == "0")
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, node.Token.GetLocation()));
                }
            }
        }

        [ExportCodeFixProvider(LanguageNames.CSharp)]
        [PartNotDiscoverable]
        private class ReplaceZeroFix : CodeFixProvider
        {
            private readonly ImmutableArray<int> _replacements;

            public ReplaceZeroFix(ImmutableArray<int> replacements)
            {
                Debug.Assert(replacements.All(replacement => replacement >= 0), $"Assertion failed: {nameof(replacements)}.All(replacement => replacement >= 0)");
                _replacements = replacements;
            }

            public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(LiteralZeroAnalyzer.Descriptor.Id);

            public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

            public override Task RegisterCodeFixesAsync(CodeFixContext context)
            {
                foreach (var diagnostic in context.Diagnostics)
                {
                    foreach (var replacement in _replacements)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "ThisToBase",
                                cancellationToken => CreateChangedDocument(context.Document, diagnostic.Location.SourceSpan, replacement, cancellationToken),
                                $"{nameof(ReplaceZeroFix)}_{replacement}"),
                            diagnostic);
                    }
                }

                return Task.CompletedTask;
            }

            private async Task<Document> CreateChangedDocument(Document document, TextSpan sourceSpan, int replacement, CancellationToken cancellationToken)
            {
                var tree = await document.GetSyntaxTreeAsync(cancellationToken);
                var root = await tree.GetRootAsync(cancellationToken);
                var token = root.FindToken(sourceSpan.Start);
                var newToken = SyntaxFactory.Literal(token.LeadingTrivia, replacement.ToString(), replacement, token.TrailingTrivia);
                return document.WithSyntaxRoot(root.ReplaceToken(token, newToken));
            }
        }

        private class CSharpTest : CodeFixTest<DefaultVerifier>
        {
            private readonly ImmutableArray<ImmutableArray<int>> _replacementGroups;

            public CSharpTest(ImmutableArray<ImmutableArray<int>> replacementGroups)
            {
                _replacementGroups = replacementGroups;
            }

            public override string Language => LanguageNames.CSharp;

            public override Type SyntaxKindType => typeof(SyntaxKind);

            protected override string DefaultFileExt => "cs";

            protected override CompilationOptions CreateCompilationOptions()
            {
                return new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            }

            protected override ParseOptions CreateParseOptions()
            {
                return new CSharpParseOptions(LanguageVersion.Default, DocumentationMode.Diagnose);
            }

            protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
            {
                foreach (var replacementGroup in _replacementGroups)
                {
                    yield return new ReplaceZeroFix(replacementGroup);
                }
            }

            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            {
                yield return new LiteralZeroAnalyzer();
            }
        }
    }
}
