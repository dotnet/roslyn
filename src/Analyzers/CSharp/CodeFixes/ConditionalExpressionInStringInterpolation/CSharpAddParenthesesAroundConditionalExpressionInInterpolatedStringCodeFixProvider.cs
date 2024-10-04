// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConditionalExpressionInStringInterpolation;

using static CSharpSyntaxTokens;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddParenthesesAroundConditionalExpressionInInterpolatedString), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpAddParenthesesAroundConditionalExpressionInInterpolatedStringCodeFixProvider() : CodeFixProvider
{
    private const string CS8361 = nameof(CS8361); //A conditional expression cannot be used directly in a string interpolation because the ':' ends the interpolation.Parenthesize the conditional expression.

    // CS8361 is a syntax error and it is unlikely that there is more than one CS8361 at a time.
    public override FixAllProvider? GetFixAllProvider() => null;

    public override ImmutableArray<string> FixableDiagnosticIds => [CS8361];

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var token = root.FindToken(diagnosticSpan.Start);
        var conditionalExpression = token.GetAncestor<ConditionalExpressionSyntax>();
        if (conditionalExpression != null)
        {
            var documentChangeAction = CodeAction.Create(
                CSharpCodeFixesResources.Add_parentheses_around_conditional_expression_in_interpolated_string,
                c => GetChangedDocumentAsync(context.Document, conditionalExpression.SpanStart, c),
                nameof(CSharpCodeFixesResources.Add_parentheses_around_conditional_expression_in_interpolated_string));
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
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var openParenthesisPosition = conditionalExpressionSyntaxStartPosition;
        var textWithOpenParenthesis = text.Replace(openParenthesisPosition, 0, "(");
        var documentWithOpenParenthesis = document.WithText(textWithOpenParenthesis);

        var syntaxRoot = await documentWithOpenParenthesis.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var nodeAtInsertPosition = syntaxRoot.FindNode(new TextSpan(openParenthesisPosition, 0));

        if (nodeAtInsertPosition is not ParenthesizedExpressionSyntax parenthesizedExpression ||
            !parenthesizedExpression.CloseParenToken.IsMissing)
        {
            return documentWithOpenParenthesis;
        }

        return await InsertCloseParenthesisAsync(
            documentWithOpenParenthesis, parenthesizedExpression, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Document> InsertCloseParenthesisAsync(
        Document document,
        ParenthesizedExpressionSyntax parenthesizedExpression,
        CancellationToken cancellationToken)
    {
        var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        if (parenthesizedExpression.Expression is ConditionalExpressionSyntax conditional &&
            parenthesizedExpression.GetAncestor<InterpolatedStringExpressionSyntax>()?.StringStartToken.Kind() == SyntaxKind.InterpolatedStringStartToken)
        {
            // If they have something like:
            //
            // var s3 = $""Text1 { true ? ""Text2""[|:|]
            // NextLineOfCode();
            //
            // We will update this initially to:
            //
            // var s3 = $""Text1 { (true ? ""Text2""[|:|]
            // NextLineOfCode();
            //
            // And we have to decide where the close paren should go.  Based on the parse tree, the
            // 'NextLineOfCode()' expression will be pulled into the WhenFalse portion of the conditional.
            // So placing the close paren after the conditional woudl result in: 'NextLineOfCode())'.
            //
            // However, the user intent is likely that NextLineOfCode is not part of the conditional
            // So instead find the colon and place the close paren after that, producing:
            //
            // var s3 = $""Text1 { (true ? ""Text2"":)
            // NextLineOfCode();

            var endToken = sourceText.AreOnSameLine(conditional.ColonToken, conditional.WhenFalse.GetFirstToken())
                ? conditional.WhenFalse.GetLastToken()
                : conditional.ColonToken;

            var closeParenPosition = endToken.Span.End;
            var textWithCloseParenthesis = sourceText.Replace(closeParenPosition, 0, ")");
            return document.WithText(textWithCloseParenthesis);
        }

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newCloseParen = CloseParenToken.WithTriviaFrom(parenthesizedExpression.CloseParenToken);
        var parenthesizedExpressionWithClosingParen = parenthesizedExpression.WithCloseParenToken(newCloseParen);
        var newRoot = root.ReplaceNode(parenthesizedExpression, parenthesizedExpressionWithClosingParen);
        return document.WithSyntaxRoot(newRoot);
    }
}
