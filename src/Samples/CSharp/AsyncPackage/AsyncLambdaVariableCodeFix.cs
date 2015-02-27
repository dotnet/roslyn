// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
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

namespace AsyncPackage
{
    /// <summary>
    /// Codefix that changes the type of a variable to be Func of Task instead of a void-returning delegate type.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = AsyncLambdaAnalyzer.AsyncLambdaId1), Shared]
    public class AsyncLambdaVariableCodeFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AsyncLambdaAnalyzer.AsyncLambdaId1); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return null;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            Debug.Assert(root != null);
            var parent = root.FindToken(diagnosticSpan.Start).Parent;
            if (parent != null)
            {
                // Find the type declaration identified by the diagnostic.
                var variableDeclaration = parent.FirstAncestorOrSelf<VariableDeclarationSyntax>();

                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    new AsyncLambdaVariableCodeAction("Async lambdas should not be stored in void-returning delegates",
                                                      c => ChangeToFunc(context.Document, variableDeclaration, c)),
                    diagnostic);
            }
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
            private Func<CancellationToken, Task<Document>> _generateDocument;
            private string _title;

            public AsyncLambdaVariableCodeAction(string title, Func<CancellationToken, Task<Document>> generateDocument)
            {
                _title = title;
                _generateDocument = generateDocument;
            }

            public override string Title { get { return _title; } }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return _generateDocument(cancellationToken);
            }
        }
    }
}
