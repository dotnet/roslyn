// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery
{
    internal abstract class AbstractConverter : IConverter<ForEachStatementSyntax, StatementSyntax>
    {
        public ForEachInfo<ForEachStatementSyntax, StatementSyntax> ForEachInfo { get; }

        public AbstractConverter(ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo)
        {
            ForEachInfo = forEachInfo;
        }

        /// <summary>
        /// Creates a query expression.
        /// </summary>
        /// <param name="selectExpression">expression to be used into the last 'select ...' in the query expression</param>
        /// <param name="leadingTokenForSelect">extra leading tokens to be added to the select clause</param>
        /// <param name="trailingTokenForSelect">extra trailing tokens to be added to the select clause</param>
        /// <returns></returns>
        protected QueryExpressionSyntax CreateQueryExpression(
            ExpressionSyntax selectExpression,
            IEnumerable<SyntaxToken> leadingTokenForSelect,
            IEnumerable<SyntaxToken> trailingTokenForSelect)
            => SyntaxFactory.QueryExpression(
                CreateFromClause(ForEachInfo.ForEachStatement, Helpers.GetTrivia(ForEachInfo.LeadingTokens), Enumerable.Empty<SyntaxTrivia>()),
                SyntaxFactory.QueryBody(
                    SyntaxFactory.List(ForEachInfo.ConvertingExtendedNodes.Select(node => CreateQueryClause(node))),
                    SyntaxFactory.SelectClause(selectExpression)
                    .WithComments(leadingTokenForSelect, ForEachInfo.TrailingTokens.Concat(trailingTokenForSelect)),
                    continuation: null)) // The current coverage of foreach statements to support does not need to use query continuations.                                                                                                           
            .WithAdditionalAnnotations(Formatter.Annotation);

        private static QueryClauseSyntax CreateQueryClause(ExtendedSyntaxNode node)
        {
            switch (node.Node.Kind())
            {
                case SyntaxKind.VariableDeclarator:
                    var variable = (VariableDeclaratorSyntax)node.Node;
                    return SyntaxFactory.LetClause(SyntaxFactory.Token(SyntaxKind.LetKeyword), variable.Identifier, variable.Initializer.EqualsToken, variable.Initializer.Value)
                        .WithComments(node.ExtraLeadingComments, node.ExtraTrailingComments);

                case SyntaxKind.ForEachStatement:
                    return CreateFromClause((ForEachStatementSyntax)node.Node, node.ExtraLeadingComments, node.ExtraTrailingComments);

                case SyntaxKind.IfStatement:
                    var ifStatement = (IfStatementSyntax)node.Node;
                    return SyntaxFactory.WhereClause(
                                SyntaxFactory.Token(SyntaxKind.WhereKeyword).WithComments(ifStatement.IfKeyword.LeadingTrivia, ifStatement.IfKeyword.TrailingTrivia),
                                ifStatement.Condition.WithComments(new[] { ifStatement.OpenParenToken }, new[] { ifStatement.CloseParenToken }))
                                .WithComments(node.ExtraLeadingComments, node.ExtraTrailingComments);
            }

            throw ExceptionUtilities.Unreachable;
        }

        private static FromClauseSyntax CreateFromClause(ForEachStatementSyntax forEachStatement, IEnumerable<SyntaxTrivia> extraLeadingTrivia, IEnumerable<SyntaxTrivia> extraTrailingTrivia)
            => SyntaxFactory.FromClause(
                    fromKeyword: SyntaxFactory.Token(SyntaxKind.FromKeyword)
                        .WithComments(forEachStatement.ForEachKeyword.LeadingTrivia, forEachStatement.ForEachKeyword.TrailingTrivia, forEachStatement.OpenParenToken).KeepCommentsAndAddElasticMarkers(),
                    type: forEachStatement.Type.IsVar ? null : forEachStatement.Type,
                    identifier: forEachStatement.Type.IsVar ? forEachStatement.Identifier.WithCommentsBeforeLeadingTrivia(Helpers.GetTrivia(forEachStatement.Type.GetFirstToken())) : forEachStatement.Identifier,
                    inKeyword: forEachStatement.InKeyword.KeepCommentsAndAddElasticMarkers(),
                    expression: forEachStatement.Expression)
                .WithComments(extraLeadingTrivia, extraTrailingTrivia, forEachStatement.CloseParenToken);

        public abstract void Convert(SyntaxEditor editor, SemanticModel semanticModel, CancellationToken cancellationToken);
    }
}
