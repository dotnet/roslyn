// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.Formatting;

namespace Roslyn.Diagnostics.Analyzers.CSharp.ApiDesign
{
    [ExportCodeFixProvider("CancellationAnalyzerCodeFixProvider", LanguageNames.CSharp), Shared]
    public class CancellationTokenMustBeLastCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(RoslynDiagnosticIds.CancellationTokenMustBeLastRuleId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the method declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            // TODO: When we have a public Change Signature API, use that
            // instead of introducing a bunch of build breaks :(

            context.RegisterFix(new MyCodeAction(context.Document, root, declaration), diagnostic);
        }

        private class MyCodeAction : CodeAction
        {
            private readonly MethodDeclarationSyntax declaration;
            private readonly Document document;
            private readonly SyntaxNode syntaxRoot;

            public MyCodeAction(Document document, SyntaxNode syntaxRoot, MethodDeclarationSyntax declaration)
            {
                this.document = document;
                this.syntaxRoot = syntaxRoot;
                this.declaration = declaration;
            }

            public override string Title
            {
                get
                {
                    return "Move CancellationToken to the end";
                }
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var methodSymbol = semanticModel.GetDeclaredSymbol(declaration);
                var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var cancellationTokenType = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

                var cancellationTokenParameters = new List<ParameterSyntax>();
                var nonCancellationTokenParameters = new List<ParameterSyntax>();
                foreach (var param in declaration.ParameterList.Parameters)
                {
                    var paramSymbol = semanticModel.GetDeclaredSymbol(param);
                    if (paramSymbol.Type.Equals(cancellationTokenType))
                    {
                        cancellationTokenParameters.Add(param);
                    }
                    else
                    {
                        nonCancellationTokenParameters.Add(param);
                    }
                }

                // TODO: This blows away trivia on the separators :(
                var newDeclaration = declaration.WithParameterList(
                    SyntaxFactory.ParameterList(
                        declaration.ParameterList.OpenParenToken,
                        SyntaxFactory.SeparatedList(nonCancellationTokenParameters.Concat(cancellationTokenParameters)),
                        declaration.ParameterList.CloseParenToken))
                    .WithAdditionalAnnotations(Formatter.Annotation);

                var newRoot = syntaxRoot.ReplaceNode(declaration, newDeclaration);
                return document.WithSyntaxRoot(newRoot);
            }
        }
    }
}
