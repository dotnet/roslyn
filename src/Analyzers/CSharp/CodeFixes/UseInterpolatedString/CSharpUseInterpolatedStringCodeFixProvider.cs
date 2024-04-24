// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.UseInterpolatedString;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseInterpolatedString;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseInterpolatedString), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal partial class CSharpUseInterpolatedStringCodeFixProvider() : AbstractUseInterpolatedStringCodeFixProvider
{
    protected override async Task FixOneAsync(Document document, Diagnostic diagnostic, SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var span = diagnostic.Location.SourceSpan;
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var node = root.FindNode(span, getInnermostNodeForTie: true) as LiteralExpressionSyntax;
        if (node?.Span != span || !node.IsKind(SyntaxKind.StringLiteralExpression))
            return;

        var newNode = CreateInterpolatedString(node);
        editor.ReplaceNode(node, newNode);
    }

    private static InterpolatedStringExpressionSyntax CreateInterpolatedString(SyntaxNode stringLiteralExpressionNode)
    {
        var token = stringLiteralExpressionNode.GetFirstToken();
        var originalTokenKind = token.Kind();
        var isVerbatimString = token.IsVerbatimStringLiteral();
        var newTokenKind = GetKind(originalTokenKind);
        var startTokenKind = GetStartToken(originalTokenKind, isVerbatimString);
        var text = GetTextWithoutQuotes(token, isVerbatimString);
        var endTokenKind = GetEndToken(originalTokenKind);

        var newNode = SyntaxFactory.InterpolatedStringText(
            SyntaxFactory.Token(
                leading: SyntaxTriviaList.Empty,
                kind: newTokenKind,
                text: text,
                valueText: text,
                trailing: SyntaxTriviaList.Empty))
            .WithTriviaFrom(stringLiteralExpressionNode);

        return SyntaxFactory.InterpolatedStringExpression(
            stringStartToken: SyntaxFactory.Token(startTokenKind),
            contents: [newNode],
            stringEndToken: SyntaxFactory.Token(endTokenKind));
    }

    private static SyntaxKind GetKind(SyntaxKind kind) => kind switch
    {
        SyntaxKind.SingleLineRawStringLiteralToken => SyntaxKind.InterpolatedStringTextToken,
        SyntaxKind.MultiLineRawStringLiteralToken => SyntaxKind.InterpolatedStringTextToken,
        _ => SyntaxKind.InterpolatedStringTextToken,
    };

    private static SyntaxKind GetStartToken(SyntaxKind kind, bool isVerbatimString) => kind switch
    {
        SyntaxKind.SingleLineRawStringLiteralToken => SyntaxKind.InterpolatedSingleLineRawStringStartToken,
        SyntaxKind.MultiLineRawStringLiteralToken => SyntaxKind.InterpolatedMultiLineRawStringStartToken,
        _ => isVerbatimString ? SyntaxKind.InterpolatedVerbatimStringStartToken : SyntaxKind.InterpolatedStringStartToken,
    };

    private static string GetTextWithoutQuotes(SyntaxToken token, bool isVerbatimString)
    {
        var text = token.Text.Replace("{", "{{").Replace("}", "}}");
        var kind = token.Kind();
        var startIndex = GetStartIndex(kind, isVerbatimString);
        var endIndex = GetEndIndex(kind);
        return text[startIndex..^endIndex];
    }

    private static SyntaxKind GetEndToken(SyntaxKind kind) => kind switch
    {
        SyntaxKind.SingleLineRawStringLiteralToken => SyntaxKind.InterpolatedRawStringEndToken,
        SyntaxKind.MultiLineRawStringLiteralToken => SyntaxKind.InterpolatedRawStringEndToken,
        _ => SyntaxKind.InterpolatedStringEndToken,
    };

    private static int GetStartIndex(SyntaxKind kind, bool isVerbatimString) => (isVerbatimString ? 1 : 0)
        + kind switch
        {
            SyntaxKind.SingleLineRawStringLiteralToken => 3,
            SyntaxKind.MultiLineRawStringLiteralToken => 3,
            _ => 1,
        };

    private static int GetEndIndex(SyntaxKind kind) => kind switch
    {
        SyntaxKind.SingleLineRawStringLiteralToken => 3,
        SyntaxKind.MultiLineRawStringLiteralToken => 3,
        _ => 1,
    };
}
