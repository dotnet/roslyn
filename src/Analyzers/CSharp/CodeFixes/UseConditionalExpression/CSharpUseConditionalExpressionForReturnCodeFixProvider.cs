// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.UseConditionalExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseConditionalExpression
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseConditionalExpressionForReturn), Shared]
    internal partial class CSharpUseConditionalExpressionForReturnCodeFixProvider
        : AbstractUseConditionalExpressionForReturnCodeFixProvider<StatementSyntax, IfStatementSyntax, ExpressionSyntax, ConditionalExpressionSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseConditionalExpressionForReturnCodeFixProvider()
        {
        }

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
                return block.WithStatements(SyntaxFactory.SingletonList(statement))
                            .WithAdditionalAnnotations(Formatter.Annotation);
            }

            return statement;
        }

        protected override ExpressionSyntax ConvertToExpression(IThrowOperation throwOperation)
            => CSharpUseConditionalExpressionHelpers.ConvertToExpression(throwOperation);

        protected override ISyntaxFormatting GetSyntaxFormatting()
            => CSharpSyntaxFormatting.Instance;
    }
}
