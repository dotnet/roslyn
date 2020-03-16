// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferNullLiteralCodeFixProvider))]
    [Shared]
    public class PreferNullLiteralCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(PreferNullLiteral.Rule.Id);

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        RoslynDiagnosticsAnalyzersResources.PreferNullLiteralCodeFix,
                        cancellationToken => ReplaceWithNullLiteralAsync(context.Document, diagnostic.Location, cancellationToken),
                        equivalenceKey: nameof(PreferNullLiteralCodeFixProvider)),
                    diagnostic);
            }

            return Task.CompletedTask;
        }

        private async Task<Document> ReplaceWithNullLiteralAsync(Document document, Location location, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntax = root.FindNode(location.SourceSpan, getInnermostNodeForTie: true);

            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            var newSyntax = syntaxGenerator.NullLiteralExpression();
            if (syntax is DefaultExpressionSyntax defaultExpression)
            {
                newSyntax = syntaxGenerator.CastExpression(defaultExpression.Type, newSyntax).WithAdditionalAnnotations(Simplifier.Annotation);
            }

            return document.WithSyntaxRoot(root.ReplaceNode(syntax, newSyntax));
        }
    }
}
