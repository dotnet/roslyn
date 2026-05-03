// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
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
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    public class PreferNullLiteralCodeFixProvider() : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(PreferNullLiteral.Rule.Id);

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
