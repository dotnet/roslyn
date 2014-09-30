using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace AsyncPackage
{
    /// <summary>
    /// Codefix changes the synchronous operations to it's asynchronous equivalent. 
    /// </summary>
    [ExportCodeFixProvider(BlockingAsyncAnalyzer.BlockingAsyncId, LanguageNames.CSharp)]
    public class BlockingAsyncCodeFix : CodeFixProvider
    {
        public sealed override IEnumerable<string> GetFixableDiagnosticIds()
        {
            return new[] { BlockingAsyncAnalyzer.BlockingAsyncId };
        }

        public sealed override async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var diagnosticSpan = diagnostics.First().Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var invocation = root.FindToken(diagnosticSpan.Start).Parent.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            var invokemethod = root.FindToken(diagnosticSpan.Start).Parent.FirstAncestorOrSelf<MemberAccessExpressionSyntax>();

            var semanticmodel = await document.GetSemanticModelAsync().ConfigureAwait(false);

            var method = semanticmodel.GetEnclosingSymbol(invocation.SpanStart) as IMethodSymbol;

            if (method != null && method.IsAsync)
            {
                if (invokemethod != null && invokemethod.Name.Identifier.Text.Equals("Wait"))
                {
                    var name = invokemethod.Name.Identifier.Text;

                    // Return a code action that will invoke the fix.
                    return new[] { new CodeActionChangetoAwaitAsync("Change synchronous operation to asynchronous counterpart", c => ChangetoAwaitAsync(document, invocation, c, name)) };
                }

                if (invokemethod != null && invokemethod.Name.Identifier.Text.Equals("GetAwaiter"))
                {
                    // Return a code action that will invoke the fix.
                    return new[] { new CodeActionChangetoAwaitGetAwaiterAsync("Change synchronous operation to asynchronous counterpart", c => ChangetoAwaitGetAwaiterAsync(document, invocation, c)) };
                }

                if (invokemethod != null && invokemethod.Name.Identifier.Text.Equals("Result"))
                {
                    var name = invokemethod.Name.Identifier.Text;

                    // Return a code action that will invoke the fix.
                    return new[] { new CodeActionChangetoAwaitAsync("Change synchronous operation to asynchronous counterpart", c => ChangetoAwaitAsync(document, invocation, c, name)) };
                }

                if (invokemethod != null && invokemethod.Name.Identifier.Text.Equals("WaitAny"))
                {
                    var name = invokemethod.Name.Identifier.Text;

                    // Return a code action that will invoke the fix.
                    return new[] { new CodeActionToDelayWhenAnyWhenAllAsync("Change synchronous operation to asynchronous counterpart", c => ToDelayWhenAnyWhenAllAsync(document, invocation, c, name)) };
                }

                if (invokemethod != null && invokemethod.Name.Identifier.Text.Equals("WaitAll"))
                {
                    var name = invokemethod.Name.Identifier.Text;

                    // Return a code action that will invoke the fix.
                    return new[] { new CodeActionToDelayWhenAnyWhenAllAsync("Change synchronous operation to asynchronous counterpart", c => ToDelayWhenAnyWhenAllAsync(document, invocation, c, name)) };
                }

                if (invokemethod != null && invokemethod.Name.Identifier.Text.Equals("Sleep"))
                {
                    var name = invokemethod.Name.Identifier.Text;

                    // Return a code action that will invoke the fix.
                    return new[] { new CodeActionToDelayWhenAnyWhenAllAsync("Change synchronous operation to asynchronous counterpart", c => ToDelayWhenAnyWhenAllAsync(document, invocation, c, name)) };
                }
            }

            return ImmutableArray<CodeAction>.Empty;
        }

        private async Task<Document> ToDelayWhenAnyWhenAllAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken c, string name)
        {
            var simpleExpression = SyntaxFactory.ParseName("");
            if (name.Equals("WaitAny"))
            {
                simpleExpression = SyntaxFactory.ParseName("System.Threading.Tasks.Task.WhenAny").WithAdditionalAnnotations(Simplifier.Annotation);
            }
            else if (name.Equals("WaitAll"))
            {
                simpleExpression = SyntaxFactory.ParseName("System.Threading.Tasks.Task.WhenAll").WithAdditionalAnnotations(Simplifier.Annotation);
            }
            else if (name.Equals("Sleep"))
            {
                simpleExpression = SyntaxFactory.ParseName("System.Threading.Tasks.Task.Delay").WithAdditionalAnnotations(Simplifier.Annotation);
            }

            SyntaxNode oldExpression = invocation;
            var expression = invocation.WithExpression(simpleExpression).WithLeadingTrivia(invocation.GetLeadingTrivia()).WithTrailingTrivia(invocation.GetTrailingTrivia());

            var newExpression = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.AwaitExpression, expression.WithLeadingTrivia(SyntaxFactory.Space)).WithTrailingTrivia(invocation.GetTrailingTrivia()).WithLeadingTrivia(invocation.GetLeadingTrivia());

            var oldroot = await document.GetSyntaxRootAsync(c).ConfigureAwait(false);
            var newroot = oldroot.ReplaceNode(oldExpression, newExpression);

            var newDocument = document.WithSyntaxRoot(newroot);

            return newDocument;
        }

        private async Task<Document> ChangetoAwaitAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationTkn, string name)
        {
            SyntaxNode oldExpression = invocation;
            SyntaxNode newExpression = null;

            if (name.Equals("Wait"))
            {
                oldExpression = invocation.FirstAncestorOrSelf<InvocationExpressionSyntax>();
                var identifier = (invocation.Expression as MemberAccessExpressionSyntax).Expression as IdentifierNameSyntax;
                newExpression = SyntaxFactory.PrefixUnaryExpression(
                    SyntaxKind.AwaitExpression,
                    identifier).WithAdditionalAnnotations(Formatter.Annotation);
            }

            if (name.Equals("Result"))
            {
                oldExpression = invocation.Parent.FirstAncestorOrSelf<MemberAccessExpressionSyntax>();
                newExpression = SyntaxFactory.PrefixUnaryExpression(
                    SyntaxKind.AwaitExpression, 
                    invocation).WithAdditionalAnnotations(Formatter.Annotation);
            }

            var oldroot = await document.GetSyntaxRootAsync(cancellationTkn).ConfigureAwait(false);
            var newroot = oldroot.ReplaceNode(oldExpression, newExpression);

            var newDocument = document.WithSyntaxRoot(newroot);

            return newDocument;
        }

        private async Task<Document> ChangetoAwaitGetAwaiterAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationTkn)
        {
            SyntaxNode expression = invocation;
            while (!(expression is ExpressionStatementSyntax))
            {
                expression = expression.Parent;
            }

            var oldExpression = expression as ExpressionStatementSyntax;
            var awaitedInvocation = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.AwaitExpression, invocation.WithLeadingTrivia(SyntaxFactory.Space)).WithLeadingTrivia(invocation.GetLeadingTrivia());
            var newExpression = oldExpression.WithExpression(awaitedInvocation);

            var oldroot = await document.GetSyntaxRootAsync(cancellationTkn).ConfigureAwait(false);
            var newroot = oldroot.ReplaceNode(oldExpression, newExpression);
            var newDocument = document.WithSyntaxRoot(newroot);

            return newDocument;
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return null;
        }

        private class CodeActionToDelayWhenAnyWhenAllAsync : CodeAction
        {
            private Func<CancellationToken, Task<Document>> generateDocument;
            private string title;

            public CodeActionToDelayWhenAnyWhenAllAsync(string title, Func<CancellationToken, Task<Document>> generateDocument)
            {
                this.title = title;
                this.generateDocument = generateDocument;
            }

            public override string Title { get { return title; } }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return this.generateDocument(cancellationToken);
            }
        }

        private class CodeActionChangetoAwaitAsync : CodeAction
        {
            private Func<CancellationToken, Task<Document>> generateDocument;
            private string title;

            public CodeActionChangetoAwaitAsync(string title, Func<CancellationToken, Task<Document>> generateDocument)
            {
                this.title = title;
                this.generateDocument = generateDocument;
            }

            public override string Title { get { return title; } }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return this.generateDocument(cancellationToken);
            }
        }

        private class CodeActionChangetoAwaitGetAwaiterAsync : CodeAction
        {
            private Func<CancellationToken, Task<Document>> generateDocument;
            private string title;

            public CodeActionChangetoAwaitGetAwaiterAsync(string title, Func<CancellationToken, Task<Document>> generateDocument)
            {
                this.title = title;
                this.generateDocument = generateDocument;
            }

            public override string Title { get { return title; } }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return this.generateDocument(cancellationToken);
            }
        }
    }
}
