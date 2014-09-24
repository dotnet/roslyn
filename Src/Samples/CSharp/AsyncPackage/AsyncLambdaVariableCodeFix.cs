using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
    /// Codefix that changes the type of a variable to be Func of Task instead of a void-returning delegate type.
    /// </summary>
    [ExportCodeFixProvider(AsyncLambdaAnalyzer.AsyncLambdaId1, LanguageNames.CSharp)]
    public class AsyncLambdaVariableCodeFix : ICodeFixProvider
    {
        public IEnumerable<string> GetFixableDiagnosticIds()
        {
            return new[] { AsyncLambdaAnalyzer.AsyncLambdaId1 };
        }

        public FixAllProvider GetFixAllProvider()
        {
            return null;
        }

        public async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var diagnosticSpan = diagnostics.First().Location.SourceSpan;

            Debug.Assert(root != null);
            var parent = root.FindToken(diagnosticSpan.Start).Parent;
            if (parent != null)
            {
                // Find the type declaration identified by the diagnostic.
                var variableDeclaration = parent.FirstAncestorOrSelf<VariableDeclarationSyntax>();

                // Return a code action that will invoke the fix.
                return new[] { new AsyncLambdaVariableCodeAction("Async lambdas should not be stored in void-returning delegates", c => ChangeToFunc(document, variableDeclaration, c)) };
            }

            return ImmutableArray<CodeAction>.Empty;
        }

        private async Task<Document> ChangeToFunc(Document document, VariableDeclarationSyntax variableDeclaration, CancellationToken cancellationToken)
        {
            // Change the variable declaration
            var newDeclaration = variableDeclaration.WithType(SyntaxFactory.ParseTypeName("System.Func<System.Threading.Tasks.Task>").WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation)
                .WithLeadingTrivia(variableDeclaration.Type.GetLeadingTrivia()).WithTrailingTrivia(variableDeclaration.Type.GetTrailingTrivia()));

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.ReplaceNode(variableDeclaration, newDeclaration);
            var newDocument = document.WithSyntaxRoot(newRoot);

            // Return document with transformed tree.
            return newDocument;
        }

        private class AsyncLambdaVariableCodeAction : CodeAction
        {
            private Func<CancellationToken, Task<Document>> generateDocument;
            private string title;

            public AsyncLambdaVariableCodeAction(string title, Func<CancellationToken, Task<Document>> generateDocument)
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