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

namespace AsyncPackage
{
    /// <summary>
    /// Codefix that changes the type of a variable to be Func of Task instead of a void-returning delegate type.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = CancellationAnalyzer.CancellationId), Shared]
    public class CancellationCodeFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CancellationAnalyzer.CancellationId); }
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
            var invocation = root.FindToken(diagnosticSpan.Start).Parent.FirstAncestorOrSelf<InvocationExpressionSyntax>();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                new CancellationCodeAction("Propagate CancellationTokens when possible",
                                           c => AddCancellationTokenAsync(context.Document, invocation, c)),
                diagnostic);
        }

        private async Task<Document> AddCancellationTokenAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);

            ITypeSymbol cancellationTokenType = semanticModel.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

            var invocationSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

            // Step up through the syntax tree to get the Method Declaration of the invocation
            var parent = invocation.Parent;
            parent = parent.FirstAncestorOrSelf<MethodDeclarationSyntax>();

            var containingMethod = semanticModel.GetDeclaredSymbol(parent) as IMethodSymbol;

            // Get the CancellationToken from the containing method
            var tokens = containingMethod.Parameters.Where(x => x.Type.Equals(cancellationTokenType));

            var firstToken = tokens.FirstOrDefault();

            // Get what slot to put it in
            var cancelSlots = invocationSymbol.Parameters.Where(x => x.Type.Equals(cancellationTokenType));

            if (cancelSlots.FirstOrDefault() == null)
            {
                return document;
            }

            var firstSlotIndex = invocationSymbol.Parameters.IndexOf(cancelSlots.FirstOrDefault());
            var newIdentifier = SyntaxFactory.IdentifierName(firstToken.Name.ToString());
            var newArgs = invocation.ArgumentList.Arguments;

            if (firstSlotIndex == 0)
            {
                newArgs = newArgs.Insert(firstSlotIndex, SyntaxFactory.Argument(newIdentifier).WithLeadingTrivia());
            }
            else
            {
                newArgs = invocation.ArgumentList.Arguments.Insert(firstSlotIndex, SyntaxFactory.Argument(newIdentifier).WithLeadingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.ElasticSpace)));
            }

            var newArgsList = SyntaxFactory.ArgumentList(newArgs);
            var newInvocation = invocation.WithArgumentList(newArgsList);

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.ReplaceNode(invocation, newInvocation);
            var newDocument = document.WithSyntaxRoot(newRoot);

            // Return document with transformed tree.
            return newDocument;
        }

        private class CancellationCodeAction : CodeAction
        {
            private Func<CancellationToken, Task<Document>> _createDocument;
            private string _title;

            public CancellationCodeAction(string title, Func<CancellationToken, Task<Document>> createDocument)
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
