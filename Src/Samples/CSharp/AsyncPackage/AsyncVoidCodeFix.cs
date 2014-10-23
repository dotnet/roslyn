using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace AsyncPackage
{
    /// <summary>
    /// This codefix replaces the void return type with Task in any method declaration the AsyncVoidAnalyzer catches
    /// </summary>
    [ExportCodeFixProvider(AsyncVoidAnalyzer.AsyncVoidId, LanguageNames.CSharp), Shared]
    public class AsyncVoidCodeFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(AsyncVoidAnalyzer.AsyncVoidId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return null;
        }

        public sealed override async Task<IEnumerable<CodeAction>> GetFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnosticSpan = context.Diagnostics.First().Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var methodDeclaration = root.FindToken(diagnosticSpan.Start).Parent.FirstAncestorOrSelf<MethodDeclarationSyntax>();

            // Return a code action that will invoke the fix.
            return new[] { new AsyncVoidCodeAction("Async methods should not return void", c => VoidToTaskAsync(context.Document, methodDeclaration, c)) };
        }

        private async Task<Document> VoidToTaskAsync(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            // The Task object must be parsed from a string using the Syntax Factory
            var newType = SyntaxFactory.ParseTypeName("System.Threading.Tasks.Task").WithAdditionalAnnotations(Simplifier.Annotation).WithTrailingTrivia(methodDeclaration.ReturnType.GetTrailingTrivia());

            var newMethodDeclaration = methodDeclaration.WithReturnType(newType);

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.ReplaceNode(methodDeclaration, newMethodDeclaration);
            var newDocument = document.WithSyntaxRoot(newRoot);

            // Return document with transformed tree.
            return newDocument;
        }

        private class AsyncVoidCodeAction : CodeAction
        {
            private Func<CancellationToken, Task<Document>> createDocument;
            private string title;

            public AsyncVoidCodeAction(string title, Func<CancellationToken, Task<Document>> createDocument)
            {
                this.title = title;
                this.createDocument = createDocument;
            }

            public override string Title { get { return title; } }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return this.createDocument(cancellationToken);
            }
        }
    }
}