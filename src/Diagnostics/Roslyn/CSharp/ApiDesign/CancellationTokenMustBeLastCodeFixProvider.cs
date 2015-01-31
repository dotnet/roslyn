// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "CancellationAnalyzerCodeFixProvider"), Shared]
    public class CancellationTokenMustBeLastCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(RoslynDiagnosticIds.CancellationTokenMustBeLastRuleId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the method declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            // TODO: When we have a public Change Signature API, use that
            // instead of introducing a bunch of build breaks :(

            context.RegisterCodeFix(new MyCodeAction(context.Document, root, declaration), diagnostic);
        }

        private class MyCodeAction : CodeAction
        {
            private readonly MethodDeclarationSyntax _declaration;
            private readonly Document _document;
            private readonly SyntaxNode _syntaxRoot;

            public MyCodeAction(Document document, SyntaxNode syntaxRoot, MethodDeclarationSyntax declaration)
            {
                _document = document;
                _syntaxRoot = syntaxRoot;
                _declaration = declaration;
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
                var semanticModel = await _document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var methodSymbol = semanticModel.GetDeclaredSymbol(_declaration, cancellationToken);
                var compilation = await _document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var cancellationTokenType = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

                var cancellationTokenParameters = new List<ParameterSyntax>();
                var nonCancellationTokenParameters = new List<ParameterSyntax>();
                foreach (var param in _declaration.ParameterList.Parameters)
                {
                    var paramSymbol = semanticModel.GetDeclaredSymbol(param, cancellationToken);
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
                var newDeclaration = _declaration.WithParameterList(
                    SyntaxFactory.ParameterList(
                        _declaration.ParameterList.OpenParenToken,
                        SyntaxFactory.SeparatedList(nonCancellationTokenParameters.Concat(cancellationTokenParameters)),
                        _declaration.ParameterList.CloseParenToken))
                    .WithAdditionalAnnotations(Formatter.Annotation);

                var newRoot = _syntaxRoot.ReplaceNode(_declaration, newDeclaration);
                return _document.WithSyntaxRoot(newRoot);
            }
        }
    }
}
