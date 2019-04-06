// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
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
    public class CodeFixValidationTests
    {
        private const string ReplaceThisWithBaseTestCode = @"
class TestClass {
  void TestMethod() { [|this|].Equals(null); }
}
";

        private const string ReplaceThisWithBaseFixedCode = @"
class TestClass {
  void TestMethod() { base.Equals(null); }
}
";

        [Fact]
        [WorkItem(149, "https://github.com/dotnet/roslyn-sdk/pull/149")]
        public async Task TestFullValidationFailsSemantics()
        {
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new ReplaceThisWithBaseTest<ReplaceThisWithBaseTokenFix>
                {
                    TestCode = ReplaceThisWithBaseTestCode,
                    FixedCode = ReplaceThisWithBaseFixedCode,
                    CodeFixValidationMode = CodeFixValidationMode.Full,
                }.RunAsync();
            });

            Assert.Equal($"Context: Iterative code fix application{Environment.NewLine}items not equal.  expected:'BaseExpression' actual:'ThisExpression'", failure.Message);
        }

        [Fact]
        [WorkItem(149, "https://github.com/dotnet/roslyn-sdk/pull/149")]
        public async Task TestFullValidationFailsWhitespace()
        {
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new ReplaceThisWithBaseTest<ReplaceThisWithBaseShiftWhitespaceFix>
                {
                    TestCode = ReplaceThisWithBaseTestCode,
                    FixedCode = ReplaceThisWithBaseFixedCode,
                    CodeFixValidationMode = CodeFixValidationMode.Full,
                }.RunAsync();
            });

            // This isn't the best message - it's reporting a mismatch in the number of SyntaxTrivia in the traling
            // trivia list of the open brace token preceding 'base'.
            Assert.Equal($"Context: Iterative code fix application{Environment.NewLine}items not equal.  expected:'1' actual:'0'", failure.Message);
        }

        [Fact]
        [WorkItem(149, "https://github.com/dotnet/roslyn-sdk/pull/149")]
        public async Task TestFullValidationPassesFull()
        {
            await new ReplaceThisWithBaseTest<ReplaceThisWithBaseNodeFix>
            {
                TestCode = ReplaceThisWithBaseTestCode,
                FixedCode = ReplaceThisWithBaseFixedCode,
                CodeFixValidationMode = CodeFixValidationMode.Full,
            }.RunAsync();
        }

        [Fact]
        [WorkItem(149, "https://github.com/dotnet/roslyn-sdk/pull/149")]
        public async Task TestSemanticValidationFailsSemantics()
        {
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new ReplaceThisWithBaseTest<ReplaceThisWithBaseTokenFix>
                {
                    TestCode = ReplaceThisWithBaseTestCode,
                    FixedCode = ReplaceThisWithBaseFixedCode,
                    CodeFixValidationMode = CodeFixValidationMode.SemanticStructure,
                }.RunAsync();
            });

            Assert.Equal($"Context: Iterative code fix application{Environment.NewLine}items not equal.  expected:'BaseExpression' actual:'ThisExpression'", failure.Message);
        }

        [Fact]
        [WorkItem(149, "https://github.com/dotnet/roslyn-sdk/pull/149")]
        public async Task TestSemanticValidationPassesWhitespace()
        {
            await new ReplaceThisWithBaseTest<ReplaceThisWithBaseShiftWhitespaceFix>
            {
                TestCode = ReplaceThisWithBaseTestCode,
                FixedCode = ReplaceThisWithBaseFixedCode,
                CodeFixValidationMode = CodeFixValidationMode.SemanticStructure,
            }.RunAsync();
        }

        [Fact]
        [WorkItem(149, "https://github.com/dotnet/roslyn-sdk/pull/149")]
        public async Task TestSemanticValidationPassesFull()
        {
            await new ReplaceThisWithBaseTest<ReplaceThisWithBaseNodeFix>
            {
                TestCode = ReplaceThisWithBaseTestCode,
                FixedCode = ReplaceThisWithBaseFixedCode,
                CodeFixValidationMode = CodeFixValidationMode.SemanticStructure,
            }.RunAsync();
        }

        [Fact]
        [WorkItem(149, "https://github.com/dotnet/roslyn-sdk/pull/149")]
        public async Task TestNoValidationPassesSemantics()
        {
            await new ReplaceThisWithBaseTest<ReplaceThisWithBaseTokenFix>
            {
                TestCode = ReplaceThisWithBaseTestCode,
                FixedCode = ReplaceThisWithBaseFixedCode,
                CodeFixValidationMode = CodeFixValidationMode.None,
            }.RunAsync();
        }

        [Fact]
        [WorkItem(149, "https://github.com/dotnet/roslyn-sdk/pull/149")]
        public async Task TestNoValidationPassesWhitespace()
        {
            await new ReplaceThisWithBaseTest<ReplaceThisWithBaseShiftWhitespaceFix>
            {
                TestCode = ReplaceThisWithBaseTestCode,
                FixedCode = ReplaceThisWithBaseFixedCode,
                CodeFixValidationMode = CodeFixValidationMode.None,
            }.RunAsync();
        }

        [Fact]
        [WorkItem(149, "https://github.com/dotnet/roslyn-sdk/pull/149")]
        public async Task TestNoValidationPassesFull()
        {
            await new ReplaceThisWithBaseTest<ReplaceThisWithBaseNodeFix>
            {
                TestCode = ReplaceThisWithBaseTestCode,
                FixedCode = ReplaceThisWithBaseFixedCode,
                CodeFixValidationMode = CodeFixValidationMode.None,
            }.RunAsync();
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class ReplaceThisWithBaseAnalyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor Descriptor =
                new DiagnosticDescriptor("ThisToBase", "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

                context.RegisterSyntaxNodeAction(HandleThisExpression, SyntaxKind.ThisExpression);
            }

            private void HandleThisExpression(SyntaxNodeAnalysisContext context)
            {
                var node = (ThisExpressionSyntax)context.Node;
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, node.Token.GetLocation()));
            }
        }

        [ExportCodeFixProvider(LanguageNames.CSharp)]
        [PartNotDiscoverable]
        private class ReplaceThisWithBaseTokenFix : CodeFixProvider
        {
            public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ReplaceThisWithBaseAnalyzer.Descriptor.Id);

            public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

            public override Task RegisterCodeFixesAsync(CodeFixContext context)
            {
                foreach (var diagnostic in context.Diagnostics)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "ThisToBase",
                            cancellationToken => CreateChangedDocument(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
                            nameof(ReplaceThisWithBaseTokenFix)),
                        diagnostic);
                }

                return Task.CompletedTask;
            }

            private async Task<Document> CreateChangedDocument(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
            {
                var tree = await document.GetSyntaxTreeAsync(cancellationToken);
                var root = await tree.GetRootAsync(cancellationToken);
                var token = root.FindToken(sourceSpan.Start);
                var newToken = SyntaxFactory.Token(token.LeadingTrivia, token.Kind(), "base", "base", token.TrailingTrivia);
                return document.WithSyntaxRoot(root.ReplaceToken(token, newToken));
            }
        }

        [ExportCodeFixProvider(LanguageNames.CSharp)]
        [PartNotDiscoverable]
        private class ReplaceThisWithBaseShiftWhitespaceFix : CodeFixProvider
        {
            public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ReplaceThisWithBaseAnalyzer.Descriptor.Id);

            public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

            public override Task RegisterCodeFixesAsync(CodeFixContext context)
            {
                foreach (var diagnostic in context.Diagnostics)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "ThisToBase",
                            cancellationToken => CreateChangedDocument(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
                            nameof(ReplaceThisWithBaseTokenFix)),
                        diagnostic);
                }

                return Task.CompletedTask;
            }

            private async Task<Document> CreateChangedDocument(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
            {
                var tree = await document.GetSyntaxTreeAsync(cancellationToken);
                var root = await tree.GetRootAsync(cancellationToken);
                var token = root.FindToken(sourceSpan.Start);
                var node = token.Parent;
                var newToken = SyntaxFactory.Token(token.LeadingTrivia, SyntaxKind.BaseKeyword, "base", "base", token.TrailingTrivia);

                // Intentionally relocate a whitespace trivia node so the text is the same but the tree shape changes
                newToken = newToken.WithLeadingTrivia(SyntaxFactory.Space);
                var newNode = SyntaxFactory.BaseExpression(newToken);
                var braceToken = token.GetPreviousToken();
                var newBraceToken = braceToken.WithTrailingTrivia(SyntaxTriviaList.Empty);

                return document.WithSyntaxRoot(root.ReplaceSyntax(
                    new[] { node },
                    (originalNode, rewrittenNode) => newNode,
                    new[] { braceToken },
                    (originalToken, rewrittenToken) => newBraceToken,
                    Array.Empty<SyntaxTrivia>(),
                    (originalTrivia, rewrittenTrivia) => rewrittenTrivia));
            }
        }

        [ExportCodeFixProvider(LanguageNames.CSharp)]
        [PartNotDiscoverable]
        private class ReplaceThisWithBaseNodeFix : CodeFixProvider
        {
            public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ReplaceThisWithBaseAnalyzer.Descriptor.Id);

            public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

            public override Task RegisterCodeFixesAsync(CodeFixContext context)
            {
                foreach (var diagnostic in context.Diagnostics)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "ThisToBase",
                            cancellationToken => CreateChangedDocument(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
                            nameof(ReplaceThisWithBaseTokenFix)),
                        diagnostic);
                }

                return Task.CompletedTask;
            }

            private async Task<Document> CreateChangedDocument(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
            {
                var tree = await document.GetSyntaxTreeAsync(cancellationToken);
                var root = await tree.GetRootAsync(cancellationToken);
                var token = root.FindToken(sourceSpan.Start);
                var node = token.Parent;
                var newToken = SyntaxFactory.Token(token.LeadingTrivia, SyntaxKind.BaseKeyword, "base", "base", token.TrailingTrivia);
                var newNode = SyntaxFactory.BaseExpression(newToken);
                return document.WithSyntaxRoot(root.ReplaceNode(node, newNode));
            }
        }

        private class ReplaceThisWithBaseTest<TCodeFix> : CodeFixTest<DefaultVerifier>
            where TCodeFix : CodeFixProvider, new()
        {
            public override string Language => LanguageNames.CSharp;

            public override Type SyntaxKindType => typeof(SyntaxKind);

            protected override string DefaultFileExt => "cs";

            protected override CompilationOptions CreateCompilationOptions()
            {
                return new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            }

            protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
            {
                yield return new TCodeFix();
            }

            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            {
                yield return new ReplaceThisWithBaseAnalyzer();
            }
        }
    }
}
