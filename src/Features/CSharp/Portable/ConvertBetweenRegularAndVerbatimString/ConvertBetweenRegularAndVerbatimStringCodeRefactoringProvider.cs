// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.ConvertBetweenRegularAndVerbatimString;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertBetweenRegularAndVerbatimString), Shared]
[ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.ConvertToInterpolatedString)]
internal class ConvertBetweenRegularAndVerbatimStringCodeRefactoringProvider
    : AbstractConvertBetweenRegularAndVerbatimStringCodeRefactoringProvider<LiteralExpressionSyntax>
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public ConvertBetweenRegularAndVerbatimStringCodeRefactoringProvider()
    {
    }

    protected override bool IsInterpolation { get; } = false;

    protected override bool IsAppropriateLiteralKind(LiteralExpressionSyntax literalExpression)
        => literalExpression.Kind() == SyntaxKind.StringLiteralExpression;

    protected override void AddSubStringTokens(LiteralExpressionSyntax literalExpression, ArrayBuilder<SyntaxToken> subStringTokens)
        => subStringTokens.Add(literalExpression.Token);

    protected override bool IsVerbatim(LiteralExpressionSyntax literalExpression)
        => CSharpSyntaxFacts.Instance.IsVerbatimStringLiteral(literalExpression.Token);

    protected override LiteralExpressionSyntax CreateVerbatimStringExpression(IVirtualCharService charService, StringBuilder sb, LiteralExpressionSyntax stringExpression)
    {
        sb.Append('@');
        sb.Append(DoubleQuote);
        AddVerbatimStringText(charService, sb, stringExpression.Token);
        sb.Append(DoubleQuote);

        return stringExpression.WithToken(CreateStringToken(sb));
    }

    protected override LiteralExpressionSyntax CreateRegularStringExpression(IVirtualCharService charService, StringBuilder sb, LiteralExpressionSyntax stringExpression)
    {
        sb.Append(DoubleQuote);
        AddRegularStringText(charService, sb, stringExpression.Token);
        sb.Append(DoubleQuote);

        return stringExpression.WithToken(CreateStringToken(sb));
    }

    private static SyntaxToken CreateStringToken(StringBuilder sb)
        => SyntaxFactory.Token(
            leading: default,
            SyntaxKind.StringLiteralToken,
            sb.ToString(), valueText: "",
            trailing: default);
}
