// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
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

                var constant = semanticModel.GetConstantValue(defaultLiteral, context.CancellationToken);
                if (!constant.HasValue)
                {
                    return;
                }

                var newLiteral = SyntaxGenerator.GetGenerator(context.Document).LiteralExpression(constant.Value);

                context.RegisterCodeFix(
                    new MyCodeAction(
                        c => FixAsync(context.Document, context.Span, newLiteral, c),
                        newLiteral.ToString()),
                    context.Diagnostics);
            }
        }

        private static async Task<Document> FixAsync(Document document, TextSpan span, SyntaxNode newLiteral, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var defaultToken = syntaxRoot.FindToken(span.Start);
            var defaultLiteral = (LiteralExpressionSyntax)defaultToken.Parent;

            var newRoot = syntaxRoot.ReplaceNode(defaultLiteral, newLiteral.WithTriviaFrom(defaultLiteral));
            return document.WithSyntaxRoot(newRoot);
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
