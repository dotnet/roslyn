// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.Testing.TestFixes;
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
                    CodeActionValidationMode = CodeActionValidationMode.Full,
                }.RunAsync();
            });

            Assert.Equal(
                @"Context: Iterative code fix application
Actual and expected values differ. Expected shown in baseline of diff:
 Node(CompilationUnit):
   Node(ClassDeclaration):
     Token(ClassKeyword): class
       Leading(EndOfLineTrivia): \r\n
       Trailing(WhitespaceTrivia):  
     Token(IdentifierToken): TestClass
       Trailing(WhitespaceTrivia):  
     Token(OpenBraceToken): {
       Trailing(EndOfLineTrivia): \r\n
     Node(MethodDeclaration):
       Node(PredefinedType):
         Token(VoidKeyword): void
           Leading(WhitespaceTrivia):   
           Trailing(WhitespaceTrivia):  
       Token(IdentifierToken): TestMethod
       Node(ParameterList):
         Token(OpenParenToken): (
         Token(CloseParenToken): )
           Trailing(WhitespaceTrivia):  
       Node(Block):
         Token(OpenBraceToken): {
           Trailing(WhitespaceTrivia):  
         Node(ExpressionStatement):
           Node(InvocationExpression):
             Node(SimpleMemberAccessExpression):
-              Node(BaseExpression):
-                Token(BaseKeyword): base
+              Node(ThisExpression):
+                Token(ThisKeyword): base
               Token(DotToken): .
               Node(IdentifierName):
                 Token(IdentifierToken): Equals
             Node(ArgumentList):
               Token(OpenParenToken): (
               Node(Argument):
                 Node(NullLiteralExpression):
                   Token(NullKeyword): null
               Token(CloseParenToken): )
           Token(SemicolonToken): ;
             Trailing(WhitespaceTrivia):  
         Token(CloseBraceToken): }
           Trailing(EndOfLineTrivia): \r\n
     Token(CloseBraceToken): }
       Trailing(EndOfLineTrivia): \r\n
   Token(EndOfFileToken): 
 
",
                failure.Message);
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
                    CodeActionValidationMode = CodeActionValidationMode.Full,
                }.RunAsync();
            });

            Assert.Equal(
                @"Context: Iterative code fix application
Actual and expected values differ. Expected shown in baseline of diff:
 Node(CompilationUnit):
   Node(ClassDeclaration):
     Token(ClassKeyword): class
       Leading(EndOfLineTrivia): \r\n
       Trailing(WhitespaceTrivia):  
     Token(IdentifierToken): TestClass
       Trailing(WhitespaceTrivia):  
     Token(OpenBraceToken): {
       Trailing(EndOfLineTrivia): \r\n
     Node(MethodDeclaration):
       Node(PredefinedType):
         Token(VoidKeyword): void
           Leading(WhitespaceTrivia):   
           Trailing(WhitespaceTrivia):  
       Token(IdentifierToken): TestMethod
       Node(ParameterList):
         Token(OpenParenToken): (
         Token(CloseParenToken): )
           Trailing(WhitespaceTrivia):  
       Node(Block):
         Token(OpenBraceToken): {
-          Trailing(WhitespaceTrivia):  
         Node(ExpressionStatement):
           Node(InvocationExpression):
             Node(SimpleMemberAccessExpression):
               Node(BaseExpression):
                 Token(BaseKeyword): base
+                  Leading(WhitespaceTrivia):  
               Token(DotToken): .
               Node(IdentifierName):
                 Token(IdentifierToken): Equals
             Node(ArgumentList):
               Token(OpenParenToken): (
               Node(Argument):
                 Node(NullLiteralExpression):
                   Token(NullKeyword): null
               Token(CloseParenToken): )
           Token(SemicolonToken): ;
             Trailing(WhitespaceTrivia):  
         Token(CloseBraceToken): }
           Trailing(EndOfLineTrivia): \r\n
     Token(CloseBraceToken): }
       Trailing(EndOfLineTrivia): \r\n
   Token(EndOfFileToken): 
 
",
                failure.Message);
        }

        [Fact]
        [WorkItem(149, "https://github.com/dotnet/roslyn-sdk/pull/149")]
        public async Task TestFullValidationPassesFull()
        {
            await new ReplaceThisWithBaseTest<ReplaceThisWithBaseNodeFix>
            {
                TestCode = ReplaceThisWithBaseTestCode,
                FixedCode = ReplaceThisWithBaseFixedCode,
                CodeActionValidationMode = CodeActionValidationMode.Full,
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
                    CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
                }.RunAsync();
            });

            Assert.Equal(
                @"Context: Iterative code fix application
Actual and expected values differ. Expected shown in baseline of diff:
 Node(CompilationUnit):
   Node(ClassDeclaration):
     Token(ClassKeyword): class
     Token(IdentifierToken): TestClass
     Token(OpenBraceToken): {
     Node(MethodDeclaration):
       Node(PredefinedType):
         Token(VoidKeyword): void
       Token(IdentifierToken): TestMethod
       Node(ParameterList):
         Token(OpenParenToken): (
         Token(CloseParenToken): )
       Node(Block):
         Token(OpenBraceToken): {
         Node(ExpressionStatement):
           Node(InvocationExpression):
             Node(SimpleMemberAccessExpression):
-              Node(BaseExpression):
-                Token(BaseKeyword): base
+              Node(ThisExpression):
+                Token(ThisKeyword): base
               Token(DotToken): .
               Node(IdentifierName):
                 Token(IdentifierToken): Equals
             Node(ArgumentList):
               Token(OpenParenToken): (
               Node(Argument):
                 Node(NullLiteralExpression):
                   Token(NullKeyword): null
               Token(CloseParenToken): )
           Token(SemicolonToken): ;
         Token(CloseBraceToken): }
     Token(CloseBraceToken): }
   Token(EndOfFileToken): 
 
",
                failure.Message);
        }

        [Fact]
        [WorkItem(149, "https://github.com/dotnet/roslyn-sdk/pull/149")]
        public async Task TestSemanticValidationPassesWhitespace()
        {
            await new ReplaceThisWithBaseTest<ReplaceThisWithBaseShiftWhitespaceFix>
            {
                TestCode = ReplaceThisWithBaseTestCode,
                FixedCode = ReplaceThisWithBaseFixedCode,
                CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
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
                CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
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
                CodeActionValidationMode = CodeActionValidationMode.None,
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
                CodeActionValidationMode = CodeActionValidationMode.None,
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
                CodeActionValidationMode = CodeActionValidationMode.None,
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
                context.EnableConcurrentExecution();
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
                var root = await tree!.GetRootAsync(cancellationToken);
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
                var tree = (await document.GetSyntaxTreeAsync(cancellationToken))!;
                var root = await tree.GetRootAsync(cancellationToken);
                var token = root.FindToken(sourceSpan.Start);
                var node = token.Parent!;
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
                var tree = (await document.GetSyntaxTreeAsync(cancellationToken))!;
                var root = await tree.GetRootAsync(cancellationToken);
                var token = root.FindToken(sourceSpan.Start);
                var node = token.Parent!;
                var newToken = SyntaxFactory.Token(token.LeadingTrivia, SyntaxKind.BaseKeyword, "base", "base", token.TrailingTrivia);
                var newNode = SyntaxFactory.BaseExpression(newToken);
                return document.WithSyntaxRoot(root.ReplaceNode(node, newNode));
            }
        }

        private class ReplaceThisWithBaseTest<TCodeFix> : CSharpCodeFixTest<ReplaceThisWithBaseAnalyzer, TCodeFix>
            where TCodeFix : CodeFixProvider, new()
        {
        }
    }
}
