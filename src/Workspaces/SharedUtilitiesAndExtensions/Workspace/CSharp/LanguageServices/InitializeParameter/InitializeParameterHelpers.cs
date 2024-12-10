// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter;

using static CSharpSyntaxTokens;

internal static class InitializeParameterHelpers
{
    public static bool IsFunctionDeclaration(SyntaxNode node)
        => node is BaseMethodDeclarationSyntax
        or LocalFunctionStatementSyntax
        or AnonymousFunctionExpressionSyntax;

    public static SyntaxNode GetBody(SyntaxNode functionDeclaration)
        => functionDeclaration switch
        {
            BaseMethodDeclarationSyntax methodDeclaration => (SyntaxNode?)methodDeclaration.Body ?? methodDeclaration.ExpressionBody!,
            LocalFunctionStatementSyntax localFunction => (SyntaxNode?)localFunction.Body ?? localFunction.ExpressionBody!,
            AnonymousFunctionExpressionSyntax anonymousFunction => anonymousFunction.Body,
            _ => throw ExceptionUtilities.UnexpectedValue(functionDeclaration),
        };

    private static SyntaxToken? TryGetSemicolonToken(SyntaxNode functionDeclaration)
        => functionDeclaration switch
        {
            BaseMethodDeclarationSyntax methodDeclaration => methodDeclaration.SemicolonToken,
            LocalFunctionStatementSyntax localFunction => localFunction.SemicolonToken,
            AnonymousFunctionExpressionSyntax _ => null,
            _ => throw ExceptionUtilities.UnexpectedValue(functionDeclaration),
        };

    public static bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
        => compilation.ClassifyConversion(source: source, destination: destination).IsImplicit;

    public static SyntaxNode? TryGetLastStatement(IBlockOperation? blockStatement)
        => blockStatement?.Syntax is BlockSyntax block
            ? block.Statements.LastOrDefault()
            : blockStatement?.Syntax;

    public static void InsertStatement(
        SyntaxEditor editor,
        SyntaxNode functionDeclaration,
        bool returnsVoid,
        SyntaxNode? statementToAddAfterOpt,
        StatementSyntax statement)
    {
        var body = GetBody(functionDeclaration);

        if (IsExpressionBody(body))
        {
            var semicolonToken = TryGetSemicolonToken(functionDeclaration) ?? SemicolonToken;

            if (!TryConvertExpressionBodyToStatement(body, semicolonToken, !returnsVoid, out var convertedStatement))
            {
                return;
            }

            // Add the new statement as the first/last statement of the new block 
            // depending if we were asked to go after something or not.
            editor.SetStatements(functionDeclaration, statementToAddAfterOpt == null
                ? [statement, convertedStatement]
                : [convertedStatement, statement]);
        }
        else if (body is BlockSyntax block)
        {
            // Look for the statement we were asked to go after.
            var indexToAddAfter = block.Statements.IndexOf(s => s == statementToAddAfterOpt);
            if (indexToAddAfter >= 0)
            {
                // If we find it, then insert the new statement after it.
                editor.InsertAfter(block.Statements[indexToAddAfter], statement);
            }
            else if (block.Statements.Count > 0)
            {
                // Otherwise, if we have multiple statements already, then insert ourselves
                // before the first one.
                editor.InsertBefore(block.Statements[0], statement);
            }
            else
            {
                // Otherwise, we have no statements in this block.  Add the new statement
                // as the single statement the block will have.
                Debug.Assert(block.Statements.Count == 0);
                editor.ReplaceNode(block, (currentBlock, _) => ((BlockSyntax)currentBlock).AddStatements(statement));
            }

            // If the block was on a single line before, the format it so that the formatting
            // engine will update it to go over multiple lines. Otherwise, we can end up in
            // the strange state where the { and } tokens stay where they were originally,
            // which will look very strange like:
            //
            //          a => {
            //              if (...) {
            //              } };
            if (CSharpSyntaxFacts.Instance.IsOnSingleLine(block, fullSpan: false))
            {
                editor.ReplaceNode(
                    block,
                    (currentBlock, _) => currentBlock.WithAdditionalAnnotations(Formatter.Annotation));
            }
        }
        else
        {
            editor.SetStatements(functionDeclaration, ImmutableArray.Create(statement));
        }
    }

    // either from an expression lambda or expression bodied member
    public static bool IsExpressionBody(SyntaxNode body)
        => body is ExpressionSyntax or ArrowExpressionClauseSyntax;

    public static bool TryConvertExpressionBodyToStatement(
        SyntaxNode body,
        SyntaxToken semicolonToken,
        bool createReturnStatementForExpression,
        [NotNullWhen(true)] out StatementSyntax? statement)
    {
        Debug.Assert(IsExpressionBody(body));

        return body switch
        {
            // If this is a => method, then we'll have to convert the method to have a block body.
            ArrowExpressionClauseSyntax arrowClause => arrowClause.TryConvertToStatement(semicolonToken, createReturnStatementForExpression, out statement),
            // must be an expression lambda
            ExpressionSyntax expression => expression.TryConvertToStatement(semicolonToken, createReturnStatementForExpression, out statement),
            _ => throw ExceptionUtilities.UnexpectedValue(body),
        };
    }

    public static SyntaxNode? GetAccessorBody(IMethodSymbol accessor, CancellationToken cancellationToken)
    {
        var node = accessor.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
        if (node is AccessorDeclarationSyntax accessorDeclaration)
            return accessorDeclaration.ExpressionBody ?? (SyntaxNode?)accessorDeclaration.Body;

        // `int Age => ...;`
        if (node is ArrowExpressionClauseSyntax arrowExpression)
            return arrowExpression;

        return null;
    }

    public static SyntaxNode RemoveThrowNotImplemented(SyntaxNode node)
        => node is PropertyDeclarationSyntax propertyDeclaration ? RemoveThrowNotImplemented(propertyDeclaration) : node;

    public static PropertyDeclarationSyntax RemoveThrowNotImplemented(PropertyDeclarationSyntax propertyDeclaration)
    {
        if (propertyDeclaration.ExpressionBody != null)
        {
            var result = propertyDeclaration
                .WithExpressionBody(null)
                .WithSemicolonToken(default)
                .AddAccessorListAccessors(SyntaxFactory
                    .AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SemicolonToken))
                .WithTrailingTrivia(propertyDeclaration.SemicolonToken.TrailingTrivia)
                .WithAdditionalAnnotations(Formatter.Annotation);
            return result;
        }

        if (propertyDeclaration.AccessorList != null)
        {
            var accessors = propertyDeclaration.AccessorList.Accessors.Select(RemoveThrowNotImplemented);
            return propertyDeclaration.WithAccessorList(
                propertyDeclaration.AccessorList.WithAccessors([.. accessors]));
        }

        return propertyDeclaration;
    }

    private static AccessorDeclarationSyntax RemoveThrowNotImplemented(AccessorDeclarationSyntax accessorDeclaration)
    {
        var result = accessorDeclaration
            .WithExpressionBody(null)
            .WithBody(null)
            .WithSemicolonToken(SemicolonToken);

        return result.WithTrailingTrivia(accessorDeclaration.Body?.GetTrailingTrivia() ?? accessorDeclaration.SemicolonToken.TrailingTrivia);
    }

    public static bool IsThrowNotImplementedProperty(Compilation compilation, IPropertySymbol property, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var accessors);

        if (property.GetMethod != null)
            accessors.AddIfNotNull(GetAccessorBody(property.GetMethod, cancellationToken));

        if (property.SetMethod != null)
            accessors.AddIfNotNull(GetAccessorBody(property.SetMethod, cancellationToken));

        if (accessors.Count == 0)
            return false;

        foreach (var group in accessors.GroupBy(node => node.SyntaxTree))
        {
            var semanticModel = compilation.GetSemanticModel(group.Key);
            foreach (var accessorBody in accessors)
            {
                var operation = semanticModel.GetOperation(accessorBody, cancellationToken);
                if (operation is null)
                    return false;

                if (!operation.IsSingleThrowNotImplementedOperation())
                    return false;
            }
        }

        return true;
    }
}
