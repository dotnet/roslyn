// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery;

internal abstract class AbstractConverter(ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo) : IConverter<ForEachStatementSyntax, StatementSyntax>
{
    public ForEachInfo<ForEachStatementSyntax, StatementSyntax> ForEachInfo { get; } = forEachInfo;

    public abstract void Convert(SyntaxEditor editor, bool convertToQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a query expression or a linq invocation expression.
    /// </summary>
    /// <param name="selectExpression">expression to be used into the last Select in the query expression or linq invocation.</param>
    /// <param name="leadingTokensForSelect">extra leading tokens to be added to the select clause</param>
    /// <param name="trailingTokensForSelect">extra trailing tokens to be added to the select clause</param>
    /// <param name="convertToQuery">Flag indicating if a query expression should be generated</param>
    /// <returns></returns>
    protected ExpressionSyntax CreateQueryExpressionOrLinqInvocation(
        ExpressionSyntax selectExpression,
        IEnumerable<SyntaxToken> leadingTokensForSelect,
        IEnumerable<SyntaxToken> trailingTokensForSelect,
        bool convertToQuery)
    {
        return convertToQuery
            ? CreateQueryExpression(selectExpression, leadingTokensForSelect, trailingTokensForSelect)
            : CreateLinqInvocationOrSimpleExpression(selectExpression, leadingTokensForSelect, trailingTokensForSelect);
    }

    /// <summary>
    /// Creates a query expression.
    /// </summary>
    /// <param name="selectExpression">expression to be used into the last 'select ...' in the query expression</param>
    /// <param name="leadingTokensForSelect">extra leading tokens to be added to the select clause</param>
    /// <param name="trailingTokensForSelect">extra trailing tokens to be added to the select clause</param>
    /// <returns></returns>
    private QueryExpressionSyntax CreateQueryExpression(
        ExpressionSyntax selectExpression,
        IEnumerable<SyntaxToken> leadingTokensForSelect,
        IEnumerable<SyntaxToken> trailingTokensForSelect)
        => SyntaxFactory.QueryExpression(
            CreateFromClause(ForEachInfo.ForEachStatement, ForEachInfo.LeadingTokens.GetTrivia(), []),
            SyntaxFactory.QueryBody(
                [.. ForEachInfo.ConvertingExtendedNodes.Select(node => CreateQueryClause(node))],
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

        throw ExceptionUtilities.Unreachable();
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
                identifier: forEachStatement.Type.IsVar
                            ? forEachStatement.Identifier.WithPrependedLeadingTrivia(
                                SyntaxNodeOrTokenExtensions.GetTrivia(forEachStatement.Type.GetFirstToken())
                                .FilterComments(addElasticMarker: false))
                            : forEachStatement.Identifier,
                inKeyword: forEachStatement.InKeyword.KeepCommentsAndAddElasticMarkers(),
                expression: forEachStatement.Expression)
                    .WithCommentsFrom(extraLeadingTrivia, extraTrailingTrivia, forEachStatement.CloseParenToken);

    /// <summary>
    /// Creates a linq invocation expression.
    /// </summary>
    /// <param name="selectExpression">expression to be used in the last 'Select' invocation</param>
    /// <param name="leadingTokensForSelect">extra leading tokens to be added to the select clause</param>
    /// <param name="trailingTokensForSelect">extra trailing tokens to be added to the select clause</param>
    /// <returns></returns>
    private ExpressionSyntax CreateLinqInvocationOrSimpleExpression(
        ExpressionSyntax selectExpression,
        IEnumerable<SyntaxToken> leadingTokensForSelect,
        IEnumerable<SyntaxToken> trailingTokensForSelect)
    {
        var foreachStatement = ForEachInfo.ForEachStatement;
        selectExpression = selectExpression.WithCommentsFrom(leadingTokensForSelect, ForEachInfo.TrailingTokens.Concat(trailingTokensForSelect));
        var currentExtendedNodeIndex = 0;

        return CreateLinqInvocationOrSimpleExpression(
            foreachStatement,
            receiverForInvocation: foreachStatement.Expression,
            selectExpression: selectExpression,
            leadingCommentsTrivia: ForEachInfo.LeadingTokens.GetTrivia(),
            trailingCommentsTrivia: [],
            currentExtendedNodeIndex: ref currentExtendedNodeIndex)
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    private ExpressionSyntax CreateLinqInvocationOrSimpleExpression(
        ForEachStatementSyntax forEachStatement,
        ExpressionSyntax receiverForInvocation,
        IEnumerable<SyntaxTrivia> leadingCommentsTrivia,
        IEnumerable<SyntaxTrivia> trailingCommentsTrivia,
        ExpressionSyntax selectExpression,
        ref int currentExtendedNodeIndex)
    {
        leadingCommentsTrivia = forEachStatement.ForEachKeyword.GetAllTrivia().Concat(leadingCommentsTrivia);

        // Recursively create linq invocations, possibly updating the receiver (Where invocations), to get the inner expression for
        // the lambda body for the linq invocation to be created for this foreach statement. For example:
        //
        // INPUT:
        //   foreach (var n1 in c1)
        //      foreach (var n2 in c2)
        //          if (n1 > n2)
        //              yield return n1 + n2;
        //
        // OUTPUT:
        //   c1.SelectMany(n1 => c2.Where(n2 => n1 > n2).Select(n2 => n1 + n2))
        //
        var hasForEachChild = false;
        var lambdaBody = CreateLinqInvocationForExtendedNode(selectExpression, ref currentExtendedNodeIndex, ref receiverForInvocation, ref hasForEachChild);
        var lambda = SyntaxFactory.SimpleLambdaExpression(
            SyntaxFactory.Parameter(
                forEachStatement.Identifier.WithPrependedLeadingTrivia(
                SyntaxNodeOrTokenExtensions.GetTrivia(forEachStatement.Type.GetFirstToken())
                    .FilterComments(addElasticMarker: false))),
            lambdaBody)
            .WithCommentsFrom(leadingCommentsTrivia, trailingCommentsTrivia,
                forEachStatement.OpenParenToken, forEachStatement.InKeyword, forEachStatement.CloseParenToken);

        // Create Select or SelectMany linq invocation for this foreach statement. For example:
        //
        // INPUT:
        //   foreach (var n1 in c1)
        //      ...
        //
        // OUTPUT:
        //   c1.Select(n1 => ...
        //      OR
        //   c1.SelectMany(n1 => ...
        //

        var invokedMethodName = !hasForEachChild ? nameof(Enumerable.Select) : nameof(Enumerable.SelectMany);

        // Avoid `.Select(x => x)`
        if (invokedMethodName == nameof(Enumerable.Select) &&
            lambdaBody is IdentifierNameSyntax identifier &&
            identifier.Identifier.ValueText == forEachStatement.Identifier.ValueText)
        {
            // Because we're dropping the lambda, any comments associated with it need to be preserved.

            var droppedTrivia = new List<SyntaxTrivia>();
            foreach (var token in lambda.DescendantTokens())
            {
                droppedTrivia.AddRange(token.GetAllTrivia().Where(t => !t.IsWhitespace()));
            }

            return receiverForInvocation.WithAppendedTrailingTrivia(droppedTrivia);
        }

        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                receiverForInvocation.Parenthesize(),
                SyntaxFactory.IdentifierName(invokedMethodName)),
            SyntaxFactory.ArgumentList([SyntaxFactory.Argument(lambda)]));
    }

    /// <summary>
    /// Creates a linq invocation expression for the <see cref="ForEachInfo{ForEachStatementSyntax, StatementSyntax}.ConvertingExtendedNodes"/> node at the given index <paramref name="extendedNodeIndex"/>
    /// or returns the <paramref name="selectExpression"/> if all extended nodes have been processed.
    /// </summary>
    /// <param name="selectExpression">Innermost select expression</param>
    /// <param name="extendedNodeIndex">Index into <see cref="ForEachInfo{ForEachStatementSyntax, StatementSyntax}.ConvertingExtendedNodes"/> to be processed and updated.</param>
    /// <param name="receiver">Receiver for the generated linq invocation. Updated when processing an if statement.</param>
    /// <param name="hasForEachChild">Flag indicating if any of the processed <see cref="ForEachInfo{ForEachStatementSyntax, StatementSyntax}.ConvertingExtendedNodes"/> is a <see cref="ForEachStatementSyntax"/>.</param>
    private ExpressionSyntax CreateLinqInvocationForExtendedNode(
        ExpressionSyntax selectExpression,
        ref int extendedNodeIndex,
        ref ExpressionSyntax receiver,
        ref bool hasForEachChild)
    {
        // Check if we have converted all the descendant foreach/if statements.
        // If so, we return the select expression.
        if (extendedNodeIndex == ForEachInfo.ConvertingExtendedNodes.Length)
        {
            return selectExpression;
        }

        // Otherwise, convert the next foreach/if statement into a linq invocation.
        var node = ForEachInfo.ConvertingExtendedNodes[extendedNodeIndex];
        switch (node.Node.Kind())
        {
            // Nested ForEach statement is converted into a nested Select or SelectMany linq invocation. For example:
            //
            // INPUT:
            //   foreach (var n1 in c1)
            //      foreach (var n2 in c2)
            //          ...
            //
            // OUTPUT:
            //   c1.SelectMany(n1 => c2.Select(n2 => ...
            //
            case SyntaxKind.ForEachStatement:
                hasForEachChild = true;
                var foreachStatement = (ForEachStatementSyntax)node.Node;
                ++extendedNodeIndex;
                return CreateLinqInvocationOrSimpleExpression(
                    foreachStatement,
                    receiverForInvocation: foreachStatement.Expression,
                    selectExpression: selectExpression,
                    leadingCommentsTrivia: node.ExtraLeadingComments,
                    trailingCommentsTrivia: node.ExtraTrailingComments,
                    currentExtendedNodeIndex: ref extendedNodeIndex);

            // Nested If statement is converted into a Where method invocation on the current receiver. For example:
            //
            // INPUT:
            //   foreach (var n1 in c1)
            //      if (n1 > 0)
            //          ...
            //
            // OUTPUT:
            //   c1.Where(n1 => n1 > 0).Select(n1 => ...
            //
            case SyntaxKind.IfStatement:
                var ifStatement = (IfStatementSyntax)node.Node;
                var parentForEachStatement = ifStatement.GetAncestor<ForEachStatementSyntax>();
                var lambdaParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parentForEachStatement.Identifier.ValueText));
                var lambda = SyntaxFactory.SimpleLambdaExpression(
                    SyntaxFactory.Parameter(
                        SyntaxFactory.Identifier(parentForEachStatement.Identifier.ValueText)),
                    ifStatement.Condition.WithCommentsFrom(ifStatement.OpenParenToken, ifStatement.CloseParenToken))
                    .WithCommentsFrom(ifStatement.IfKeyword.GetAllTrivia().Concat(node.ExtraLeadingComments), node.ExtraTrailingComments);

                receiver = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        receiver.Parenthesize(),
                        SyntaxFactory.IdentifierName(nameof(Enumerable.Where))),
                    SyntaxFactory.ArgumentList([SyntaxFactory.Argument(lambda)]));

                ++extendedNodeIndex;
                return CreateLinqInvocationForExtendedNode(selectExpression, ref extendedNodeIndex, ref receiver, ref hasForEachChild);
        }

        throw ExceptionUtilities.Unreachable();
    }
}
