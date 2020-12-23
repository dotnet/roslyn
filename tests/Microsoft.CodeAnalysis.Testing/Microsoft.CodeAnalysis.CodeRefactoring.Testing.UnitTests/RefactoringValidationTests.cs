// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class RefactoringValidationTests
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
                @"Context: Code refactoring application
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
                @"Context: Code refactoring application
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
                @"Context: Code refactoring application
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

        [ExportCodeRefactoringProvider(LanguageNames.CSharp)]
        [PartNotDiscoverable]
        private class ReplaceThisWithBaseTokenFix : CodeRefactoringProvider
        {
            public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        "ThisToBase",
                        cancellationToken => CreateChangedDocument(context.Document, context.Span, cancellationToken),
                        nameof(ReplaceThisWithBaseTokenFix)));

                return Task.CompletedTask;
            }

            private async Task<Document> CreateChangedDocument(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
            {
                var tree = (await document.GetSyntaxTreeAsync(cancellationToken))!;
                var root = await tree.GetRootAsync(cancellationToken);
                var token = root.FindToken(sourceSpan.Start);
                var newToken = SyntaxFactory.Token(token.LeadingTrivia, token.Kind(), "base", "base", token.TrailingTrivia);
                return document.WithSyntaxRoot(root.ReplaceToken(token, newToken));
            }
        }

        [ExportCodeRefactoringProvider(LanguageNames.CSharp)]
        [PartNotDiscoverable]
        private class ReplaceThisWithBaseShiftWhitespaceFix : CodeRefactoringProvider
        {
            public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        "ThisToBase",
                        cancellationToken => CreateChangedDocument(context.Document, context.Span, cancellationToken),
                        nameof(ReplaceThisWithBaseTokenFix)));

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

        [ExportCodeRefactoringProvider(LanguageNames.CSharp)]
        [PartNotDiscoverable]
        private class ReplaceThisWithBaseNodeFix : CodeRefactoringProvider
        {
            public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        "ThisToBase",
                        cancellationToken => CreateChangedDocument(context.Document, context.Span, cancellationToken),
                        nameof(ReplaceThisWithBaseTokenFix)));

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

        private class ReplaceThisWithBaseTest<TCodeRefactoring> : CodeRefactoringTest<DefaultVerifier>
            where TCodeRefactoring : CodeRefactoringProvider, new()
        {
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

            protected override IEnumerable<CodeRefactoringProvider> GetCodeRefactoringProviders()
            {
                yield return new TCodeRefactoring();
            }
        }
    }
}
