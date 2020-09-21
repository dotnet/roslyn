// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities.Lightup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        private static async Task<Document> ReplaceWithNullLiteralAsync(Document document, Location location, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntax = root.FindNode(location.SourceSpan, getInnermostNodeForTie: true);

            ExpressionSyntax newSyntax = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
            if (syntax is DefaultExpressionSyntax defaultExpression)
            {
                var type = defaultExpression.Type;
                if (!type.IsKind(SyntaxKind.NullableType) && !type.IsKind(SyntaxKind.PointerType))
                {
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var nullableContext = semanticModel.GetNullableContext(type.SpanStart);
                    if (nullableContext.AnnotationsEnabled())
                    {
                        type = SyntaxFactory.NullableType(type.WithoutTrivia()).WithTriviaFrom(type);
                    }
                }

                var castExpression = SyntaxFactory.CastExpression(type, newSyntax.WithTrailingTrivia(defaultExpression.Keyword.TrailingTrivia));
                castExpression = castExpression
                    .WithOpenParenToken(castExpression.OpenParenToken.WithTriviaFrom(defaultExpression.OpenParenToken))
                    .WithCloseParenToken(castExpression.CloseParenToken.WithLeadingTrivia(defaultExpression.CloseParenToken.LeadingTrivia));

                newSyntax = SyntaxFactory.ParenthesizedExpression(castExpression.WithAdditionalAnnotations(Simplifier.Annotation))
                    .WithAdditionalAnnotations(Simplifier.Annotation);
            }

            newSyntax = newSyntax.WithTriviaFrom(syntax);
            return document.WithSyntaxRoot(root.ReplaceNode(syntax, newSyntax));
        }
    }
}
