// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseNullPropagation;

namespace Microsoft.CodeAnalysis.CSharp.UseNullPropagation;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseNullPropagation), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpUseNullPropagationCodeFixProvider() : AbstractUseNullPropagationCodeFixProvider<
    CSharpUseNullPropagationDiagnosticAnalyzer,
    SyntaxKind,
    ExpressionSyntax,
    StatementSyntax,
    ConditionalExpressionSyntax,
    BinaryExpressionSyntax,
    InvocationExpressionSyntax,
    ConditionalAccessExpressionSyntax,
    ElementAccessExpressionSyntax,
    MemberAccessExpressionSyntax,
    ElementBindingExpressionSyntax,
    IfStatementSyntax,
    ExpressionStatementSyntax,
    BracketedArgumentListSyntax>
{
    protected override CSharpUseNullPropagationDiagnosticAnalyzer Analyzer
        => CSharpUseNullPropagationDiagnosticAnalyzer.Instance;

    private static BlockSyntax ReplaceBlockStatements(BlockSyntax block, StatementSyntax newInnerStatement)
        => block.WithStatements([newInnerStatement, .. block.Statements.Skip(1).Select(s => s.WithAdditionalAnnotations(Formatter.Annotation))]);

    protected override SyntaxNode PostProcessElseIf(
        IfStatementSyntax ifStatement, StatementSyntax newWhenTrueStatement)
    {
        if (ifStatement.Statement is BlockSyntax block)
            newWhenTrueStatement = ReplaceBlockStatements(block, newWhenTrueStatement);

        var elseClauseSyntax = (ElseClauseSyntax)ifStatement.Parent!;
        return elseClauseSyntax
            .WithElseKeyword(elseClauseSyntax.ElseKeyword.WithTrailingTrivia())
            .WithStatement(newWhenTrueStatement.WithPrependedLeadingTrivia(ifStatement.CloseParenToken.TrailingTrivia));
    }

    protected override ElementBindingExpressionSyntax ElementBindingExpression(BracketedArgumentListSyntax argumentList)
        => SyntaxFactory.ElementBindingExpression(argumentList);
}
