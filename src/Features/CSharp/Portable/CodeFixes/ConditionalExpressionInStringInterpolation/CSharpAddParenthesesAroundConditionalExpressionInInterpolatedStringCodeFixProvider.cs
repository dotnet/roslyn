// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.ConditionalExpressionInStringInterpolation
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddParenthesesAroundConditionalExpressionInInterpolatedString), Shared]
    internal class CSharpAddParenthesesAroundConditionalExpressionInInterpolatedStringCodeFixProvider : CodeFixProvider
    {
        private const string CS8361 = nameof(CS8361); //A conditional expression cannot be used directly in a string interpolation because the ':' ends the interpolation.Parenthesize the conditional expression.

        [ImportingConstructor]
        public CSharpAddParenthesesAroundConditionalExpressionInInterpolatedStringCodeFixProvider()
        {
        }

        // CS8361 is a syntax error and it is unlikely that there is more than one CS8361 at a time.
        public override FixAllProvider GetFixAllProvider() => null;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS8361);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var token = root.FindToken(diagnosticSpan.Start);
            var conditionalExpression = token.GetAncestor<ConditionalExpressionSyntax>();
            if (conditionalExpression != null)
            {
                var documentChangeAction = new MyCodeAction(
                    c => GetChangedDocumentAsync(context.Document, conditionalExpression.SpanStart, c));
                context.RegisterCodeFix(documentChangeAction, diagnostic);
            }
        }

        private static async Task<Document> GetChangedDocumentAsync(Document document, int conditionalExpressionSyntaxStartPosition, CancellationToken cancellationToken)
        {
            // The usual SyntaxTree transformations are complicated if string literals are present in the false part as in
            // $"{ condition ? "Success": "Failure" }"
            // The colon starts a FormatClause and the double quote left to 'F' therefore ends the interpolated string.
            // The text starting with 'F' is parsed as code and the resulting syntax tree is impractical.
            // The same problem arises if a } is present in the false part.
            // To circumvent these problems this solution
            // 1. Inserts an opening parenthesis
            // 2. Re-parses the resulting document (now the colon isn't treated as starting a FormatClause anymore)
            // 3. Replaces the missing CloseParenToken with a new one
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var openParenthesisPosition = conditionalExpressionSyntaxStartPosition;
            var textWithOpenParenthesis = text.Replace(openParenthesisPosition, 0, "(");
            var documentWithOpenParenthesis = document.WithText(textWithOpenParenthesis);
            var syntaxRoot = await documentWithOpenParenthesis.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodeAtInsertPosition = syntaxRoot.FindNode(new TextSpan(openParenthesisPosition, 0));
            if (nodeAtInsertPosition is ParenthesizedExpressionSyntax parenthesizedExpression &&
                parenthesizedExpression.CloseParenToken.IsMissing)
            {
                var newCloseParen = SyntaxFactory.Token(SyntaxKind.CloseParenToken).WithTriviaFrom(parenthesizedExpression.CloseParenToken);
                var parenthesizedExpressionWithClosingParen = parenthesizedExpression.WithCloseParenToken(newCloseParen);
                syntaxRoot = syntaxRoot.ReplaceNode(parenthesizedExpression, parenthesizedExpressionWithClosingParen);
                return documentWithOpenParenthesis.WithSyntaxRoot(syntaxRoot);
            }

            return documentWithOpenParenthesis;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpFeaturesResources.Add_parentheses_around_conditional_expression_in_interpolated_string,
                       createChangedDocument,
                       CSharpFeaturesResources.Add_parentheses_around_conditional_expression_in_interpolated_string)
            {
            }
        }
    }
}
