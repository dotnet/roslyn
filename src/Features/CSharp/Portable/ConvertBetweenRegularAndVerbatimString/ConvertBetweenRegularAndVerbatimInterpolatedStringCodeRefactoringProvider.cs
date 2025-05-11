// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.ConvertBetweenRegularAndVerbatimString;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertBetweenRegularAndVerbatimInterpolatedString), Shared]
[ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.ConvertToInterpolatedString)]
internal sealed class ConvertBetweenRegularAndVerbatimInterpolatedStringCodeRefactoringProvider
    : AbstractConvertBetweenRegularAndVerbatimStringCodeRefactoringProvider<InterpolatedStringExpressionSyntax>
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public ConvertBetweenRegularAndVerbatimInterpolatedStringCodeRefactoringProvider()
    {
    }

    protected override bool IsInterpolation { get; } = true;

    protected override bool IsAppropriateLiteralKind(InterpolatedStringExpressionSyntax literalExpression)
        => true;

    protected override void AddSubStringTokens(InterpolatedStringExpressionSyntax literalExpression, ArrayBuilder<SyntaxToken> subStringTokens)
    {
        foreach (var content in literalExpression.Contents)
        {
            if (content is InterpolatedStringTextSyntax textSyntax)
                subStringTokens.Add(textSyntax.TextToken);
        }
    }

    protected override bool IsVerbatim(InterpolatedStringExpressionSyntax literalExpression)
        => literalExpression.StringStartToken.Kind() == SyntaxKind.InterpolatedVerbatimStringStartToken;

    private static InterpolatedStringExpressionSyntax Convert(
        IVirtualCharService charService, StringBuilder sb, InterpolatedStringExpressionSyntax stringExpression,
        SyntaxKind newStartKind, Action<IVirtualCharService, StringBuilder, SyntaxToken> addStringText)
    {
        using var _ = ArrayBuilder<InterpolatedStringContentSyntax>.GetInstance(out var newContents);

        foreach (var content in stringExpression.Contents)
        {
            if (content is InterpolatedStringTextSyntax textSyntax)
            {
                // Ensure our temp builder is in a empty starting state.
                sb.Clear();

                addStringText(charService, sb, textSyntax.TextToken);
                newContents.Add(textSyntax.WithTextToken(CreateTextToken(textSyntax.TextToken, sb)));
            }
            else
            {
                // not text (i.e. it's an interpolation).  just add as is.
                newContents.Add(content);
            }
        }

        var startToken = stringExpression.StringStartToken;
        var newStartToken = SyntaxFactory.Token(
            leading: startToken.LeadingTrivia,
            kind: newStartKind,
            trailing: startToken.TrailingTrivia);

        return stringExpression.Update(
            newStartToken,
            [.. newContents],
            stringExpression.StringEndToken);
    }

    private static SyntaxToken CreateTextToken(SyntaxToken textToken, StringBuilder sb)
        => SyntaxFactory.Token(
            leading: textToken.LeadingTrivia,
            SyntaxKind.InterpolatedStringTextToken,
            sb.ToString(), valueText: "",
            trailing: textToken.TrailingTrivia);

    protected override InterpolatedStringExpressionSyntax CreateVerbatimStringExpression(IVirtualCharService charService, StringBuilder sb, InterpolatedStringExpressionSyntax stringExpression)
        => Convert(charService, sb, stringExpression,
            SyntaxKind.InterpolatedVerbatimStringStartToken, AddVerbatimStringText);

    protected override InterpolatedStringExpressionSyntax CreateRegularStringExpression(IVirtualCharService charService, StringBuilder sb, InterpolatedStringExpressionSyntax stringExpression)
        => Convert(charService, sb, stringExpression,
            SyntaxKind.InterpolatedStringStartToken, AddRegularStringText);
}
