using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings
{
    [ExportLanguageService(typeof(IRefactoringHelpersService), LanguageNames.CSharp), Shared]
    internal class CSharpRefactoringHelpersService : AbstractRefactoringHelpersService
    {
        public override SyntaxNode DefaultNodeExtractor<TNode>(SyntaxNode node)
        {
            switch (node)
            {
                case LocalDeclarationStatementSyntax localDeclaration:
                    {
                        if (localDeclaration.Declaration.Variables.Count == 1 && localDeclaration.Declaration.Variables.First().Initializer != null)
                        {
                            var initializer = localDeclaration.Declaration.Variables.First().Initializer;
                            return initializer.Value as TNode;
                        }

                        break;
                    }

                case ExpressionStatementSyntax expressionStatement:
                    {
                        if (expressionStatement.Expression is AssignmentExpressionSyntax assignmentExpression)
                        {
                            return assignmentExpression.Right as TNode;
                        }

                        break;
                    }
            }

            return node;
        }
    }
}
