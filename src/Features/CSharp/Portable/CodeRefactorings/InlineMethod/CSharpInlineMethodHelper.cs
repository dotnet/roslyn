using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineMethod
{
    internal static class CSharpInlineMethodHelper
    {
        internal static bool CanStatementBeInlined(StatementSyntax statementSyntax)
        {
            if (statementSyntax.IsKind(SyntaxKind.ExpressionStatement))
            {
                return true;
            }

            if (statementSyntax is ReturnStatementSyntax returnStatementSyntax)
            {
                // In this case don't provide inline.
                // void Caller() { Callee(); }
                // void Callee() { return; }
                return returnStatementSyntax.Expression != null;
            }

            return false;
        }

        internal static bool TryGetInlineContent(MethodDeclarationSyntax methodDeclarationSyntax, out ExpressionSyntax? inlineExpressionSyntax)
        {
            inlineExpressionSyntax = null;
            var blockSyntaxNode = methodDeclarationSyntax.Body;
            // Check has been done before to make sure block statements only has one statement
            // or the declarationSyntax has an ExpressionBody.
            if (blockSyntaxNode != null)
            {
                // 1. If it is an ordinary method with block
                var blockStatements = blockSyntaxNode.Statements;
                if (blockStatements.Count == 1)
                {
                    inlineExpressionSyntax = GetExpressionFromStatementSyntaxNode(blockStatements[0]);
                    return true;
                }
            }
            else
            {
                // 2. If it is using Arrow Expression
                var arrowExpressionNode = methodDeclarationSyntax.ExpressionBody;
                if (arrowExpressionNode != null)
                {
                    inlineExpressionSyntax = arrowExpressionNode!.Expression;
                    return true;
                }
            }

            return false;
        }

        private static ExpressionSyntax GetExpressionFromStatementSyntaxNode(StatementSyntax statementSyntax)
            => statementSyntax switch
            {
                // Check has been done before to make sure the argument is ReturnStatementSyntax or ExpressionStatementSyntax
                // and their expression is not null
                ReturnStatementSyntax returnStatementSyntax => returnStatementSyntax.Expression!,
                ExpressionStatementSyntax expressionStatementSyntax => expressionStatementSyntax.Expression,
                _ => throw ExceptionUtilities.Unreachable
            };
    }
}
