// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.UseExpressionBodyForLambda;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal static class UseExpressionBodyForLambdaCodeActionHelpers
{
    internal static LambdaExpressionSyntax Update(SemanticModel semanticModel, LambdaExpressionSyntax lambdaExpression, CancellationToken cancellationToken)
        => UpdateWorker(semanticModel, lambdaExpression, cancellationToken).WithAdditionalAnnotations(Formatter.Annotation);

    private static LambdaExpressionSyntax UpdateWorker(
        SemanticModel semanticModel, LambdaExpressionSyntax lambdaExpression, CancellationToken cancellationToken)
    {
        var expressionBody = UseExpressionBodyForLambdaHelpers.GetBodyAsExpression(lambdaExpression);
        return expressionBody == null
            ? WithExpressionBody(semanticModel, lambdaExpression, cancellationToken)
            : WithBlockBody(semanticModel, lambdaExpression, expressionBody);
    }

    private static LambdaExpressionSyntax WithExpressionBody(
        SemanticModel semanticModel, LambdaExpressionSyntax declaration, CancellationToken cancellationToken)
    {
        if (!UseExpressionBodyForLambdaHelpers.TryConvertToExpressionBody(
                semanticModel, declaration, declaration.GetLanguageVersion(), ExpressionBodyPreference.WhenPossible, cancellationToken, out var expressionBody))
        {
            return declaration;
        }

        var updatedDecl = declaration.WithBody(expressionBody);

        // If there will only be whitespace between the arrow and the body, then replace that
        // with a single space so that the lambda doesn't have superfluous newlines in it.
        if (declaration.ArrowToken.TrailingTrivia.All(t => t.IsWhitespaceOrEndOfLine()) &&
            expressionBody.GetLeadingTrivia().All(t => t.IsWhitespaceOrEndOfLine()))
        {
            updatedDecl = updatedDecl.WithArrowToken(updatedDecl.ArrowToken.WithTrailingTrivia(ElasticSpace));
        }

        return updatedDecl;
    }

    private static LambdaExpressionSyntax WithBlockBody(
        SemanticModel semanticModel, LambdaExpressionSyntax lambdaExpression, ExpressionSyntax expressionBody)
    {
        var createReturnStatementForExpression = CreateReturnStatementForExpression(
            semanticModel, lambdaExpression);

        if (!expressionBody.TryConvertToStatement(
                semicolonTokenOpt: null,
                createReturnStatementForExpression,
                out var statement))
        {
            return lambdaExpression;
        }

        // If the user is converting to a block, it's likely they intend to add multiple
        // statements to it.  So make a multi-line block so that things are formatted properly
        // for them to do so.
        return lambdaExpression.WithBody(Block(
            OpenBraceToken.WithAppendedTrailingTrivia(ElasticCarriageReturnLineFeed),
            [statement],
            CloseBraceToken));
    }

    private static bool CreateReturnStatementForExpression(
        SemanticModel semanticModel, LambdaExpressionSyntax declaration)
    {
        var lambdaType = (INamedTypeSymbol)semanticModel.GetTypeInfo(declaration).ConvertedType!;
        if (lambdaType.DelegateInvokeMethod!.ReturnsVoid)
        {
            return false;
        }

        // 'async Task' is effectively a void-returning lambda.  we do not want to create 
        // 'return statements' when converting.
        if (declaration.AsyncKeyword != default)
        {
            var returnType = lambdaType.DelegateInvokeMethod.ReturnType;
            if (returnType.IsErrorType())
            {
                // "async Goo" where 'Goo' failed to bind.  If 'Goo' is 'Task' then it's
                // reasonable to assume this is just a missing 'using' and that this is a true
                // "async Task" lambda.  If the name isn't 'Task', then this looks like a
                // real return type, and we should use return statements.
                return returnType.Name != nameof(Task);
            }

            var taskType = semanticModel.Compilation.GetTypeByMetadataName(typeof(Task).FullName!);
            if (returnType.Equals(taskType))
            {
                // 'async Task'.  definitely do not create a 'return' statement;
                return false;
            }
        }

        return true;
    }
}
