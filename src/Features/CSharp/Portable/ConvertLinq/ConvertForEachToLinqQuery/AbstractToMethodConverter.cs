using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery
{
    internal abstract class AbstractToMethodConverter : AbstractConverter
    {
        // It is "item" for for "list.Add(item)"
        // It can be anything for "counter++". It will be ingored in the case.
        private readonly ExpressionSyntax _selectExpression;

        // It is "list" for "list.Add(item)"
        // It is "counter" for "counter++"
        private readonly ExpressionSyntax _modifyingExpression;
        public AbstractToMethodConverter(
            ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo,
            ExpressionSyntax selectExpression,
            ExpressionSyntax modifyingExpression) : base(forEachInfo)
        {
            _selectExpression = selectExpression;
            _modifyingExpression = modifyingExpression;
        }

        protected abstract string MethodName { get; }

        protected abstract bool CanReplaceInitialization(ExpressionSyntax expressionSyntax, SemanticModel semanticModel, CancellationToken cancellationToken);

        protected abstract StatementSyntax CreateDefaultStatement(QueryExpressionSyntax queryExpression, ExpressionSyntax expression);

        public override void Convert(SyntaxEditor editor, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var queryExpression = CreateQueryExpression(_forEachInfo, _selectExpression);
            var previous = FindPreviousStatementInBlock(_forEachInfo.ForEachStatement);

            if (!previous.ContainsDirectives)
            {
                switch (previous?.Kind())
                {
                    case SyntaxKind.LocalDeclarationStatement:
                        var variables = ((LocalDeclarationStatementSyntax)previous).Declaration.Variables;
                        var lastDeclaration = variables.Last();
                        // Check if 
                        // var ...., list = new List<T>(); or var ..., counter = 0;
                        // is just before the foreach.
                        // If so, join the declaration with the query.
                        if (_modifyingExpression is IdentifierNameSyntax identifierName &&
                            lastDeclaration.Identifier.ValueText.Equals(identifierName.Identifier.ValueText) &&
                            CanReplaceInitialization(lastDeclaration.Initializer.Value, semanticModel, cancellationToken))
                        {
                            Convert(lastDeclaration.Initializer.Value, variables.Count == 1 ? (SyntaxNode)previous : lastDeclaration);
                            return;
                        }

                        break;

                    case SyntaxKind.ExpressionStatement:
                        // Check if 
                        // list = new List<T>(); or counter = 0;
                        // is just before the foreach.
                        // If so, join the assignment with the query.
                        if (((ExpressionStatementSyntax)previous).Expression is AssignmentExpressionSyntax assignmentExpression &&
                            SymbolEquivalenceComparer.Instance.Equals(
                                semanticModel.GetSymbolInfo(assignmentExpression.Left, cancellationToken).Symbol,
                                semanticModel.GetSymbolInfo(_modifyingExpression, cancellationToken).Symbol) &&
                            CanReplaceInitialization(assignmentExpression.Right, semanticModel, cancellationToken))
                        {
                            Convert(assignmentExpression.Right, previous);
                            return;
                        }

                        break;
                }
            }

            // At least, we already can convert to 
            // list.AddRange(query) or counter += query.Count();
            editor.ReplaceNode(_forEachInfo.ForEachStatement, CreateDefaultStatement(queryExpression, _modifyingExpression).WithAdditionalAnnotations(Formatter.Annotation));

            void Convert(ExpressionSyntax replacingExpression, SyntaxNode nodeToRemoveIfFollowedByReturn)
            {
                // Check if expressionAssigning is followed by a return statement.
                var expresisonSymbol = semanticModel.GetSymbolInfo(_modifyingExpression, cancellationToken).Symbol;
                if (expresisonSymbol is ILocalSymbol &&
                    FindNextStatementInBlock(_forEachInfo.ForEachStatement) is ReturnStatementSyntax returnStatement &&
                    ! returnStatement.ContainsDirectives &&
                    SymbolEquivalenceComparer.Instance.Equals(expresisonSymbol, semanticModel.GetSymbolInfo(returnStatement.Expression, cancellationToken).Symbol))
                {
                    // Input:
                    // var list = new List<T>(); or var counter = 0;
                    // foreach(...)
                    // {
                    //     ...
                    //     ...
                    //     list.Add(item); or counter++;
                    //  }
                    //  return list; or return counter;
                    //
                    //  Output:
                    //  return queryGenerated.ToList(); or return queryGenerated.Count();
                    replacingExpression = returnStatement.Expression;
                    editor.RemoveNode(nodeToRemoveIfFollowedByReturn);
                }

                editor.ReplaceNode(replacingExpression, CreateInvocationExpression(queryExpression));
                editor.RemoveNode(_forEachInfo.ForEachStatement);
            }
        }

        // query => query.Method()
        // like query.Count() or query.ToList()
        // TODO comments?
        protected InvocationExpressionSyntax CreateInvocationExpression(QueryExpressionSyntax queryExpression)
            => SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ParenthesizedExpression(queryExpression),
                        SyntaxFactory.IdentifierName(MethodName))).WithAdditionalAnnotations(Formatter.Annotation);

        private static StatementSyntax FindPreviousStatementInBlock(StatementSyntax statement)
        {
            if (statement.Parent is BlockSyntax block)
            {
                var index = block.Statements.IndexOf(statement);
                if (index > 0)
                {
                    return block.Statements[index - 1];
                }
            }

            return null;
        }

        private static StatementSyntax FindNextStatementInBlock(StatementSyntax statement)
        {
            if (statement.Parent is BlockSyntax block)
            {
                var index = block.Statements.IndexOf(statement);
                if (index >= 0 && index < block.Statements.Count - 1)
                {
                    return block.Statements[index + 1];
                }
            }

            return null;
        }
    }
}
