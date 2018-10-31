// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ReplaceDefaultLiteral
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ReplaceDefaultLiteral), Shared]
    internal sealed class CSharpReplaceDefaultLiteralCodeFixProvider : CodeFixProvider
    {
        private const string CS8313 = nameof(CS8313); // A default literal 'default' is not valid as a case constant. Use another literal (e.g. '0' or 'null') as appropriate. If you intended to write the default label, use 'default:' without 'case'.
        private const string CS8363 = nameof(CS8363); // A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern 'var _'.

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(CS8313, CS8363);

        public override FixAllProvider GetFixAllProvider()
        {
            // This code fix addresses very specific compiler errors. It's unlikely there will be more than 1 of them at a time.
            return null;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var token = syntaxRoot.FindToken(context.Span.Start);

            if (token.Span == context.Span &&
                token.IsKind(SyntaxKind.DefaultKeyword) &&
                token.Parent.IsKind(SyntaxKind.DefaultLiteralExpression))
            {
                var defaultLiteral = (LiteralExpressionSyntax)token.Parent;
                var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

                var (newExpression, displayText) = GetReplacementExpressionAndText(
                    context.Document, defaultLiteral, semanticModel, context.CancellationToken);

                if (newExpression != null)
                {
                    context.RegisterCodeFix(
                        new MyCodeAction(
                            c => ReplaceAsync(context.Document, context.Span, newExpression, c),
                            displayText),
                        context.Diagnostics);
                }
            }
        }

        private static async Task<Document> ReplaceAsync(
            Document document, TextSpan span, SyntaxNode newExpression, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var defaultToken = syntaxRoot.FindToken(span.Start);
            var defaultLiteral = (LiteralExpressionSyntax)defaultToken.Parent;

            var newRoot = syntaxRoot.ReplaceNode(defaultLiteral, newExpression.WithTriviaFrom(defaultLiteral));
            return document.WithSyntaxRoot(newRoot);
        }

        private static (SyntaxNode newExpression, string displayText) GetReplacementExpressionAndText(
            Document document,
            LiteralExpressionSyntax defaultLiteral,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var constant = semanticModel.GetConstantValue(defaultLiteral, cancellationToken);
            if (constant.HasValue)
            {
                var newLiteral = SyntaxGenerator.GetGenerator(document).LiteralExpression(constant.Value);

                return (newLiteral, newLiteral.ToString());
            }

            var type = semanticModel.GetTypeInfo(defaultLiteral, cancellationToken).ConvertedType;
            if (type != null && type.TypeKind != TypeKind.Error && !type.ContainsAnonymousType())
            {
                var defaultExpression =
                    SyntaxFactory.DefaultExpression(
                        defaultLiteral.Token.WithoutTrivia(),
                        SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                        type.GenerateTypeSyntax(allowVar: false),
                        SyntaxFactory.Token(SyntaxKind.CloseParenToken));

                return (defaultExpression, $"default({type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)})");
            }

            return default;
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string literal)
                : base(string.Format(CSharpFeaturesResources.Use_0, literal), createChangedDocument, CSharpFeaturesResources.Use_0)
            {
            }
        }
    }
}
