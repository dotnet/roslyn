// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.UseConditionalExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseConditionalExpression;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseConditionalExpressionForReturn), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class CSharpUseConditionalExpressionForReturnCodeFixProvider()
    : AbstractUseConditionalExpressionForReturnCodeFixProvider<StatementSyntax, IfStatementSyntax, ExpressionSyntax, ConditionalExpressionSyntax>
{
    protected override ISyntaxFacts SyntaxFacts
        => CSharpSyntaxFacts.Instance;

    protected override AbstractFormattingRule GetMultiLineFormattingRule()
        => MultiLineConditionalExpressionFormattingRule.Instance;

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

    protected override SyntaxNode WrapIfStatementIfNecessary(IConditionalOperation operation)
    {
        if (operation.Syntax is IfStatementSyntax { Condition: CheckedExpressionSyntax exp })
            return exp;

        return base.WrapIfStatementIfNecessary(operation);
    }

    protected override ExpressionSyntax WrapReturnExpressionIfNecessary(ExpressionSyntax returnExpression, IOperation returnOperation)
    {
        if (returnOperation.Syntax is ReturnStatementSyntax { Expression: CheckedExpressionSyntax exp })
            return exp;

        return base.WrapReturnExpressionIfNecessary(returnExpression, returnOperation);
    }

    protected override ExpressionSyntax ConvertToExpression(IThrowOperation throwOperation)
        => CSharpUseConditionalExpressionHelpers.ConvertToExpression(throwOperation);

    protected override ISyntaxFormatting SyntaxFormatting
        => CSharpSyntaxFormatting.Instance;
}
