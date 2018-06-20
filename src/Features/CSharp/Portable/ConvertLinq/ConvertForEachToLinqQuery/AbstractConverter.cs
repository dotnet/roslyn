// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using SyntaxNodeOrTokenExtensions = Microsoft.CodeAnalysis.Shared.Extensions.SyntaxNodeOrTokenExtensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery
{
    internal abstract class AbstractConverter : IConverter<ForEachStatementSyntax, StatementSyntax>
    {
        public ForEachInfo<ForEachStatementSyntax, StatementSyntax> ForEachInfo { get; }

        public AbstractConverter(ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo)
        {
            ForEachInfo = forEachInfo;
        }
        public abstract void Convert(SyntaxEditor editor, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a query expression.
        /// </summary>
        /// <param name="selectExpression">expression to be used into the last 'select ...' in the query expression</param>
        /// <param name="leadingTokensForSelect">extra leading tokens to be added to the select clause</param>
        /// <param name="trailingTokensForSelect">extra trailing tokens to be added to the select clause</param>
        /// <returns></returns>
        protected QueryExpressionSyntax CreateQueryExpression(
            ExpressionSyntax selectExpression,
            IEnumerable<SyntaxToken> leadingTokensForSelect,
            IEnumerable<SyntaxToken> trailingTokensForSelect)
            => SyntaxFactory.QueryExpression(
                CreateFromClause(ForEachInfo.ForEachStatement, ForEachInfo.LeadingTokens.GetTrivia(), Enumerable.Empty<SyntaxTrivia>()),
                SyntaxFactory.QueryBody(
                    SyntaxFactory.List(ForEachInfo.ConvertingExtendedNodes.Select(node => CreateQueryClause(node))),
                    SyntaxFactory.SelectClause(selectExpression)
                        .WithCommentsFrom(leadingTokensForSelect, ForEachInfo.TrailingTokens.Concat(trailingTokensForSelect)),
                    continuation: null)) // The current coverage of foreach statements to support does not need to use query continuations.                                                                                                           
            .WithAdditionalAnnotations(Formatter.Annotation);

        private static QueryClauseSyntax CreateQueryClause(ExtendedSyntaxNode node)
        {
            switch (node.Node.Kind())
            {
                case SyntaxKind.VariableDeclarator:
                    var variable = (VariableDeclaratorSyntax)node.Node;
                    return SyntaxFactory.LetClause(
                                SyntaxFactory.Token(SyntaxKind.LetKeyword),
                                variable.Identifier,
                                variable.Initializer.EqualsToken,
                                variable.Initializer.Value)
                            .WithCommentsFrom(node.ExtraLeadingComments, node.ExtraTrailingComments);

                case SyntaxKind.ForEachStatement:
                    return CreateFromClause((ForEachStatementSyntax)node.Node, node.ExtraLeadingComments, node.ExtraTrailingComments);

                case SyntaxKind.IfStatement:
                    var ifStatement = (IfStatementSyntax)node.Node;
                    return SyntaxFactory.WhereClause(
                                SyntaxFactory.Token(SyntaxKind.WhereKeyword)
                                    .WithCommentsFrom(ifStatement.IfKeyword.LeadingTrivia, ifStatement.IfKeyword.TrailingTrivia),
                                ifStatement.Condition.WithCommentsFrom(ifStatement.OpenParenToken, ifStatement.CloseParenToken))
                            .WithCommentsFrom(node.ExtraLeadingComments, node.ExtraTrailingComments);
            }

            throw ExceptionUtilities.Unreachable;
        }

        private static FromClauseSyntax CreateFromClause(
            ForEachStatementSyntax forEachStatement, 
            IEnumerable<SyntaxTrivia> extraLeadingTrivia, 
            IEnumerable<SyntaxTrivia> extraTrailingTrivia)
            => SyntaxFactory.FromClause(
                    fromKeyword: SyntaxFactory.Token(SyntaxKind.FromKeyword)
                                    .WithCommentsFrom(
                                        forEachStatement.ForEachKeyword.LeadingTrivia,
                                        forEachStatement.ForEachKeyword.TrailingTrivia,
                                        forEachStatement.OpenParenToken)
                                    .KeepCommentsAndAddElasticMarkers(),
                    type: forEachStatement.Type.IsVar ? null : forEachStatement.Type,
                    identifier: forEachStatement.Type.IsVar ?
                                forEachStatement.Identifier.WithPrependedLeadingTrivia(
                                    SyntaxNodeOrTokenExtensions.GetTrivia(forEachStatement.Type.GetFirstToken())
                                    .FilterComments(addElasticMarker: false)) :
                                forEachStatement.Identifier,
                    inKeyword: forEachStatement.InKeyword.KeepCommentsAndAddElasticMarkers(),
                    expression: forEachStatement.Expression)
                        .WithCommentsFrom(extraLeadingTrivia, extraTrailingTrivia, forEachStatement.CloseParenToken);

    }
}
