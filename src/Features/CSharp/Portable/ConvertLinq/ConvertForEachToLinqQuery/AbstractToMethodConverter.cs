// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

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

        // Trivia around "counter++;" or "list.Add(item);". Required to keep comments.
        private readonly SyntaxTrivia[] _trivia;

        public AbstractToMethodConverter(
            ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo,
            ExpressionSyntax selectExpression,
            ExpressionSyntax modifyingExpression,
            SyntaxTrivia[] trivia) : base(forEachInfo)
        {
            _selectExpression = selectExpression;
            _modifyingExpression = modifyingExpression;
            _trivia = trivia;
        }

        protected abstract string MethodName { get; }

        protected abstract bool CanReplaceInitialization(ExpressionSyntax expressionSyntax, SemanticModel semanticModel, CancellationToken cancellationToken);

        protected abstract StatementSyntax CreateDefaultStatement(QueryExpressionSyntax queryExpression, ExpressionSyntax expression);

        public override void Convert(SyntaxEditor editor, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var queryExpression = CreateQueryExpression(_forEachInfo, _selectExpression, Enumerable.Empty<SyntaxToken>(), Enumerable.Empty<SyntaxToken>());
            var previous = FindPreviousStatementInBlock(_forEachInfo.ForEachStatement);

            if (previous != null && !previous.ContainsDirectives)
            {
                switch (previous.Kind())
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
                SyntaxTrivia[] leadingTrivia;

                // Check if expressionAssigning is followed by a return statement.
                var expresisonSymbol = semanticModel.GetSymbolInfo(_modifyingExpression, cancellationToken).Symbol;
                if (expresisonSymbol is ILocalSymbol &&
                    FindNextStatementInBlock(_forEachInfo.ForEachStatement) is ReturnStatementSyntax returnStatement &&
                    !returnStatement.ContainsDirectives &&
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
                    leadingTrivia = GetTriviaFromNode(nodeToRemoveIfFollowedByReturn).Concat(Helpers.GetTrivia(replacingExpression)).ToArray();
                    editor.RemoveNode(nodeToRemoveIfFollowedByReturn);
                }
                else
                {
                    leadingTrivia = Helpers.GetTrivia(replacingExpression);
                }

                editor.ReplaceNode(replacingExpression, CreateInvocationExpression(queryExpression).WithComments(leadingTrivia, _trivia).KeepCommentsAndAddElasticMarkers());
                editor.RemoveNode(_forEachInfo.ForEachStatement);
            }

            SyntaxTrivia[] GetTriviaFromVariableDeclarator(VariableDeclaratorSyntax variableDeclarator)
                => Helpers.GetTrivia(variableDeclarator.Identifier, variableDeclarator.Initializer.EqualsToken, variableDeclarator.Initializer.Value);

            SyntaxTrivia[] GetTriviaFromNode(SyntaxNode node)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.LocalDeclarationStatement:

                        var localDeclaration = (LocalDeclarationStatementSyntax)node;
                        if (localDeclaration.Declaration.Variables.Count != 1)
                        {
                            throw ExceptionUtilities.Unreachable;
                        }

                        return new IEnumerable<SyntaxTrivia>[] {
                            Helpers.GetTrivia(localDeclaration.Declaration.Type),
                            GetTriviaFromVariableDeclarator(localDeclaration.Declaration.Variables[0]),
                            Helpers.GetTrivia(localDeclaration.SemicolonToken)}.SelectMany(x => x).ToArray();

                    case SyntaxKind.VariableDeclarator:
                        return GetTriviaFromVariableDeclarator((VariableDeclaratorSyntax)node);

                    case SyntaxKind.ExpressionStatement:
                        if (((ExpressionStatementSyntax)node).Expression is AssignmentExpressionSyntax assignmentExpression)
                        {
                            return Helpers.GetTrivia(assignmentExpression.Left, assignmentExpression.OperatorToken, assignmentExpression.Right);
                        }

                        break;
                }

                throw ExceptionUtilities.Unreachable;
            }
        }

        // query => query.Method()
        // like query.Count() or query.ToList()
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
