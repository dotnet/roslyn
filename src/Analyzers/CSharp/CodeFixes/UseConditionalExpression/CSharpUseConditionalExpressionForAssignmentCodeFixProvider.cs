// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.UseConditionalExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseConditionalExpression;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseConditionalExpressionForAssignment), Shared]
internal partial class CSharpUseConditionalExpressionForAssignmentCodeFixProvider
    : AbstractUseConditionalExpressionForAssignmentCodeFixProvider<
        StatementSyntax, IfStatementSyntax, LocalDeclarationStatementSyntax, VariableDeclaratorSyntax, ExpressionSyntax, ConditionalExpressionSyntax>
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpUseConditionalExpressionForAssignmentCodeFixProvider()
    {
    }

    protected override ISyntaxFacts SyntaxFacts
        => CSharpSyntaxFacts.Instance;

    protected override AbstractFormattingRule GetMultiLineFormattingRule()
        => MultiLineConditionalExpressionFormattingRule.Instance;

    protected override VariableDeclaratorSyntax WithInitializer(VariableDeclaratorSyntax variable, ExpressionSyntax value)
        => variable.WithInitializer(SyntaxFactory.EqualsValueClause(value));

    protected override VariableDeclaratorSyntax GetDeclaratorSyntax(IVariableDeclaratorOperation declarator)
        => (VariableDeclaratorSyntax)declarator.Syntax;

    protected override LocalDeclarationStatementSyntax AddSimplificationToType(LocalDeclarationStatementSyntax statement)
        => statement.WithDeclaration(statement.Declaration.WithType(
            statement.Declaration.Type.WithAdditionalAnnotations(Simplifier.Annotation)));

    protected override StatementSyntax WrapWithBlockIfAppropriate(
        IfStatementSyntax ifStatement, StatementSyntax statement)
    {
        if (ifStatement.Parent is ElseClauseSyntax &&
            ifStatement.Statement is BlockSyntax block)
        {
            return block.WithStatements([statement])
                        .WithAdditionalAnnotations(Formatter.Annotation);
        }

        return statement;
    }

    protected override ExpressionSyntax ConvertToExpression(IThrowOperation throwOperation)
        => CSharpUseConditionalExpressionHelpers.ConvertToExpression(throwOperation);

    protected override ISyntaxFormatting GetSyntaxFormatting()
        => CSharpSyntaxFormatting.Instance;
}
