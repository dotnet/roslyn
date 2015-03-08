// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = AsyncVoidAnalyzer.AsyncVoidId), Shared]
    public class AsyncVoidCodeFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AsyncVoidAnalyzer.AsyncVoidId); }
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

            // Find the type declaration identified by the diagnostic.
            var methodDeclaration = root.FindToken(diagnosticSpan.Start).Parent.FirstAncestorOrSelf<MethodDeclarationSyntax>();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                new AsyncVoidCodeAction("Async methods should not return void",
                                        c => VoidToTaskAsync(context.Document, methodDeclaration, c)),
                diagnostic);
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
            private Func<CancellationToken, Task<Document>> _createDocument;
            private string _title;

            public AsyncVoidCodeAction(string title, Func<CancellationToken, Task<Document>> createDocument)
            {
                _title = title;
                _createDocument = createDocument;
            }

            public override string Title { get { return _title; } }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return _createDocument(cancellationToken);
            }
        }
    }
}
