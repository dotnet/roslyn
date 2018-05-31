// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery
{
    internal abstract class AbstractConverter : IConverter
    {
        protected readonly ForEachInfo<ForEachStatementSyntax, StatementSyntax> _forEachInfo;

        public AbstractConverter(ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo)
        {
            _forEachInfo = forEachInfo;
        }

        // TODO comments?
        protected static QueryExpressionSyntax CreateQueryExpression(ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo, ExpressionSyntax selectExpression)
            => SyntaxFactory.QueryExpression(
                // TODO should add some trivia above!!!
                CreateFromClause(forEachInfo.ForEachStatement, forEachInfo.LeadingComments, Enumerable.Empty<SyntaxTrivia>()),
                SyntaxFactory.QueryBody(
                    SyntaxFactory.List(forEachInfo.ConvertingExtendedNodes.Select(node => CreateQueryClause(node).WithoutTrivia())), // TODO without trivia is removing comments???
                                                                                                                                     // The current coverage of foreach statements to support does not need to use query continuations.                                                                                                           
                    SyntaxFactory.SelectClause(selectExpression).WithoutTrivia(), continuation: null).WithoutTrivia()).WithAdditionalAnnotations(Formatter.Annotation);

        private static QueryClauseSyntax CreateQueryClause(ExtendedSyntaxNode node)
        {
            switch (node.Node.Kind())
            {
                case SyntaxKind.VariableDeclarator:
                    var variable = (VariableDeclaratorSyntax)node.Node;
                    return SyntaxFactory.LetClause(variable.Identifier, variable.Initializer.Value);

                case SyntaxKind.ForEachStatement:
                    return CreateFromClause((ForEachStatementSyntax)node.Node, node.ExtraLeadingComments, node.ExtraTrailingComments);

                case SyntaxKind.IfStatement:
                    var ifStatement = (IfStatementSyntax)node.Node;
                    return SyntaxFactory.WhereClause(ifStatement.Condition)
                        .AddBeforeLeadingTrivia(node.ExtraLeadingComments, ifStatement.IfKeyword, ifStatement.OpenParenToken)
                        .AddAfterTrailingTrivia(node.ExtraTrailingComments, ifStatement.CloseParenToken);
            }

            throw ExceptionUtilities.Unreachable;
        }

        private static FromClauseSyntax CreateFromClause(ForEachStatementSyntax forEachStatement, IEnumerable<SyntaxTrivia> extraLeadingTrivia, IEnumerable<SyntaxTrivia> extraTrailingTrivia)
            => SyntaxFactory.FromClause(
                forEachStatement.Type.IsVar ? null : forEachStatement.Type,
                forEachStatement.Identifier,
                forEachStatement.Expression)
            .AddBeforeLeadingTrivia(extraLeadingTrivia, forEachStatement.ForEachKeyword, forEachStatement.OpenParenToken, forEachStatement.InKeyword)
            .AddAfterTrailingTrivia(extraTrailingTrivia, forEachStatement.CloseParenToken);

        public abstract void Convert(SyntaxEditor editor, SemanticModel semanticModel, CancellationToken cancellationToken);

    }
}
