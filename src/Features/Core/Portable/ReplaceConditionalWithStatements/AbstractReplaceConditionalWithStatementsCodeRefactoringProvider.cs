// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ReplaceConditionalWithStatements;

internal abstract class AbstractReplaceConditionalWithStatementsCodeRefactoringProvider<
    TExpressionSyntax,
    TConditionalExpressionSyntax,
    TStatementSyntax,
    TThrowStatementSyntax,
    TYieldStatementSyntax,
    TReturnStatementSyntax,
    TExpressionStatementSyntax,
    TLocalDeclarationStatementSyntax,
    TArgumentSyntax,
    TArgumentListSyntax,
    TVariableSyntax,
    TVariableDeclaratorSyntax,
    TEqualsValueClauseSyntax>
    : CodeRefactoringProvider
    where TExpressionSyntax : SyntaxNode
    where TConditionalExpressionSyntax : TExpressionSyntax
    where TStatementSyntax : SyntaxNode
    where TThrowStatementSyntax : TStatementSyntax
    where TYieldStatementSyntax : TStatementSyntax
    where TReturnStatementSyntax : TStatementSyntax
    where TExpressionStatementSyntax : TStatementSyntax
    where TLocalDeclarationStatementSyntax : TStatementSyntax
    where TArgumentSyntax : SyntaxNode
    where TArgumentListSyntax : SyntaxNode
    where TVariableSyntax : SyntaxNode
    where TVariableDeclaratorSyntax : SyntaxNode
    where TEqualsValueClauseSyntax : SyntaxNode
{
    protected abstract bool HasSingleVariable(TLocalDeclarationStatementSyntax localDeclarationStatement, [NotNullWhen(true)] out TVariableSyntax? variable);
    protected abstract TLocalDeclarationStatementSyntax GetUpdatedLocalDeclarationStatement(SyntaxGenerator generator, TLocalDeclarationStatementSyntax localDeclarationStatement, ILocalSymbol symbol);

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var conditionalExpression = await context.TryGetRelevantNodeAsync<TConditionalExpressionSyntax>().ConfigureAwait(false);
        if (conditionalExpression is null)
            return;

        var (document, _, cancellationToken) = context;

        // Walk upwards from this conditional, through parent expressions and arguments until we find the owning construct.
        var topExpression = AbstractReplaceConditionalWithStatementsCodeRefactoringProvider<TExpressionSyntax, TConditionalExpressionSyntax, TStatementSyntax, TThrowStatementSyntax, TYieldStatementSyntax, TReturnStatementSyntax, TExpressionStatementSyntax, TLocalDeclarationStatementSyntax, TArgumentSyntax, TArgumentListSyntax, TVariableSyntax, TVariableDeclaratorSyntax, TEqualsValueClauseSyntax>.GetTopExpression(conditionalExpression);

        // Check if we're parented by one of the simple statement types.  In all these cases, we just convert the
        //
        //      ... a ? b : c ...;
        //
        // into
        //
        //      if (a)
        //          ... b ...;
        //      else
        //          ... c ...;
        //
        // (in other words, we duplicate the statement wholesale, just replacing the conditional with the
        // when-true/false portions.
        var parentStatement = topExpression.Parent as TStatementSyntax;
        if (parentStatement is TExpressionStatementSyntax or
                               TReturnStatementSyntax or
                               TThrowStatementSyntax or
                               TYieldStatementSyntax)
        {
            context.RegisterRefactoring(CodeAction.Create(
                FeaturesResources.Replace_conditional_expression_with_statements,
                c => ReplaceConditionalExpressionInSingleStatementAsync(document, conditionalExpression, parentStatement, c)));
            return;
        }

        // However, if we're parented by a local decl, e.g.:
        //
        //      object v = a ? b : c;
        //
        // Then we want to break this into two statements.  One for the declaration, and one for the if-statement:
        //
        //      object v;
        //      if (a)
        //          v = b;
        //      else
        //          v = c;

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

#if false
        var topExpression = (TExpressionSyntax)syntaxFacts.WalkUpParentheses(conditionalExpression);

        if (topExpression.GetAncestor<TStatementSyntax>() is { } assignmentStatement &&
            syntaxFacts.IsSimpleAssignmentStatement(assignmentStatement))
        {
            syntaxFacts.GetPartsOfAssignmentStatement(assignmentStatement, out _, out var right);
            if (right == topExpression)
            {
                context.RegisterRefactoring(CodeAction.Create(
                    FeaturesResources.Replace_conditional_expression_with_statements,
                    c => ReplaceConditionalExpressionInAssignmentStatementAsync(document, conditionalExpression, assignmentStatement, c)));
                return;
            }
        }

        if (topExpression.Parent is TEqualsValueClauseSyntax equalsValue &&
            equalsValue.Parent is TVariableDeclaratorSyntax variableDeclarator &&
            conditionalExpression.GetAncestor<TLocalDeclarationStatementSyntax>() is { } localDeclarationStatement &&
            HasSingleVariable(localDeclarationStatement, out var variable) &&
            syntaxFacts.IsDeclaratorOfLocalDeclarationStatement(variableDeclarator, localDeclarationStatement))
        {
            context.RegisterRefactoring(CodeAction.Create(
                FeaturesResources.Replace_conditional_expression_with_statements,
                c => ReplaceConditionalExpressionInLocalDeclarationStatementAsync(
                    document, conditionalExpression, variable, localDeclarationStatement, c)));
            return;
        }

        if (topExpression.Parent is TReturnStatementSyntax returnStatement)
        {
            context.RegisterRefactoring(CodeAction.Create(
                FeaturesResources.Replace_conditional_expression_with_statements,
                c => ReplaceConditionalExpressionInReturnStatementAsync(document, conditionalExpression, returnStatement, c)));
            return;
        }

        if (topExpression.Parent is TArgumentSyntax argument &&
            argument.Parent?.Parent is TInvocationExpressionSyntax invocationExpression &&
            invocationExpression.Parent is TExpressionStatementSyntax expressionStatement)
        {
            context.RegisterRefactoring(CodeAction.Create(
                FeaturesResources.Replace_conditional_expression_with_statements,
                c => ReplaceConditionalExpressionInInvocationExpressionAsync(
                    document, conditionalExpression, argument, invocationExpression, expressionStatement, c)));
            return;
        }
#endif
    }

    private static TExpressionSyntax GetTopExpression(TConditionalExpressionSyntax conditionalExpression)
    {
        TExpressionSyntax current = conditionalExpression;

outer:
        if (current.Parent is TExpressionSyntax parentExpression)
        {
            current = parentExpression;
            goto outer;
        }

        if (current.Parent is TArgumentSyntax { Parent: TArgumentListSyntax { Parent: TExpressionSyntax argumentParent } })
        {
            current = argumentParent;
            goto outer;
        }

        return current;
    }

    private static SyntaxNode TryCast(SyntaxGenerator generator, SyntaxNode whenTrue, ITypeSymbol? conditionalType)
        => conditionalType is null or IErrorTypeSymbol
            ? whenTrue
            : generator.CastExpression(conditionalType, whenTrue);

    private static TSyntaxNode WithElasticTrivia<TSyntaxNode>(SyntaxGenerator generator, TSyntaxNode node) where TSyntaxNode : SyntaxNode
        => node.WithLeadingTrivia(generator.ElasticMarker).WithTrailingTrivia(generator.ElasticMarker);

    private static TSyntaxNode WithAddedElasticTrivia<TSyntaxNode>(SyntaxGenerator generator, TSyntaxNode node) where TSyntaxNode : SyntaxNode
        => node.WithPrependedLeadingTrivia(generator.ElasticMarker).WithAppendedTrailingTrivia(generator.ElasticMarker);

    private static async Task<Document> ReplaceConditionalExpressionInSingleStatementAsync(
        Document document,
        TConditionalExpressionSyntax conditionalExpression,
        TStatementSyntax statement,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var generator = SyntaxGenerator.GetGenerator(document);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        // When we have `x ? y : z`, then the type of 'y' and 'z' can influence each other.  So when we break them into
        // pieces, ensure the type of each remains consistent.
        var conditionalType = semanticModel.GetTypeInfo(conditionalExpression, cancellationToken).Type;

        syntaxFacts.GetPartsOfConditionalExpression(conditionalExpression, out var condition, out var whenTrue, out var whenFalse);
        var ifStatement = generator.IfStatement(
            condition,
            new[] { Rewrite(whenTrue) },
            new[] { Rewrite(whenFalse) }).WithTriviaFrom(statement);

        var newRoot = root.ReplaceNode(statement, ifStatement);
        return document.WithSyntaxRoot(newRoot);

        TStatementSyntax Rewrite(SyntaxNode expression)
            => WithElasticTrivia(
                generator,
                statement.ReplaceNode(conditionalExpression,
                WithAddedElasticTrivia(
                    generator, TryCast(generator, expression, conditionalType))));
    }

    private static async Task<Document> ReplaceConditionalExpressionInAssignmentStatementAsync(
        Document document,
        TConditionalExpressionSyntax conditionalExpression,
        TStatementSyntax assignmentStatement,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var generator = SyntaxGenerator.GetGenerator(document);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        // When we have `a = x ? y : z`, then the type of 'y' and 'z' can influence each other.
        // If we convert this into:
        //
        //   if (x)
        //     a = y;
        //   else
        //     a = z;
        //
        // Then we need to preserve that meaning so that 'y' and 'z' have the same type/value, even after the
        // transformation.
        var conditionalType = semanticModel.GetTypeInfo(conditionalExpression, cancellationToken).Type;

        syntaxFacts.GetPartsOfAssignmentStatement(assignmentStatement, out var left, out _);
        syntaxFacts.GetPartsOfConditionalExpression(conditionalExpression, out var condition, out var whenTrue, out var whenFalse);
        var ifStatement = generator.IfStatement(
            condition,
            new[] { generator.AssignmentStatement(left, TryCast(generator, whenTrue, conditionalType)) },
            new[] { generator.AssignmentStatement(left, TryCast(generator, whenFalse, conditionalType)) }).WithTriviaFrom(assignmentStatement);

        var editor = new SyntaxEditor(root, generator);
        editor.ReplaceNode(
            assignmentStatement,
            ifStatement);

        return document.WithSyntaxRoot(editor.GetChangedRoot());
    }

    private async Task<Document> ReplaceConditionalExpressionInLocalDeclarationStatementAsync(
        Document document,
        TConditionalExpressionSyntax conditionalExpression,
        TVariableSyntax variable,
        TLocalDeclarationStatementSyntax localDeclarationStatement,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var generator = SyntaxGenerator.GetGenerator(document);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        var symbol = (ILocalSymbol)semanticModel.GetRequiredDeclaredSymbol(variable, cancellationToken);

        // When we have `object v = x ? y : z`, then the type of 'y' and 'z' can influence each other.
        // If we convert this into:
        //
        // object v;
        // if (x)
        //   v = y;
        // else
        //   v = z;
        //
        // Then we need to preserve that meaning so that 'y' and 'z' have the same type/value, even after the
        // transformation.
        //
        // Similarly, if we have 'var v', we need to give it a strong type at the declaration point.
        var conditionalType = semanticModel.GetTypeInfo(conditionalExpression, cancellationToken).Type;

        syntaxFacts.GetPartsOfConditionalExpression(conditionalExpression, out var condition, out var whenTrue, out var whenFalse);
        var identifier = generator.IdentifierName(symbol.Name);

        var updatedLocalDeclarationStatement = GetUpdatedLocalDeclarationStatement(generator, localDeclarationStatement, symbol);
        var ifStatement = generator.IfStatement(
            condition,
            new[] { generator.AssignmentStatement(identifier, TryCast(generator, whenTrue, conditionalType)) },
            new[] { generator.AssignmentStatement(identifier, TryCast(generator, whenFalse, conditionalType)) });

        var editor = new SyntaxEditor(root, generator);
        editor.ReplaceNode(
            localDeclarationStatement,
            (_, _) =>
            {
                return new[]
                {
                    updatedLocalDeclarationStatement,
                    ifStatement,
                };
            });

        return document.WithSyntaxRoot(editor.GetChangedRoot());
    }

    private static async Task<Document> ReplaceConditionalExpressionInReturnStatementAsync(
        Document document,
        TConditionalExpressionSyntax conditionalExpression,
        TReturnStatementSyntax returnStatement,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var generator = SyntaxGenerator.GetGenerator(document);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        // When we have `return x ? y : z`, then the type of 'y' and 'z' can influence each other.
        // If we convert this into:
        //
        // object M()
        // {
        //   if (x)
        //     return y;
        //   else
        //     return z;
        //
        // Then we need to preserve that meaning so that 'y' and 'z' have the same type/value, even after the
        // transformation.
        var conditionalType = semanticModel.GetTypeInfo(conditionalExpression, cancellationToken).Type;

        syntaxFacts.GetPartsOfConditionalExpression(conditionalExpression, out var condition, out var whenTrue, out var whenFalse);
        var ifStatement = generator.IfStatement(
            condition,
            new[] { generator.ReturnStatement(TryCast(generator, whenTrue, conditionalType)) },
            new[] { generator.ReturnStatement(TryCast(generator, whenFalse, conditionalType)) }).WithTriviaFrom(returnStatement);

        var editor = new SyntaxEditor(root, generator);
        editor.ReplaceNode(
            returnStatement,
            ifStatement);

        return document.WithSyntaxRoot(editor.GetChangedRoot());
    }

    //private async Task<Document> ReplaceConditionalExpressionInInvocationExpressionAsync(
    //    Document document,
    //    TConditionalExpressionSyntax conditionalExpression,
    //    TArgumentSyntax argument,
    //    TInvocationExpressionSyntax invocationExpression,
    //    TExpressionStatementSyntax expressionStatement,
    //    CancellationToken cancellationToken)
    //{
    //    var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
    //    var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

    //    var generator = SyntaxGenerator.GetGenerator(document);
    //    var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

    //    // When we have `Goo(x ? y : z)`, then the type of 'y' and 'z' can influence each other.
    //    // If we convert this into:
    //    //
    //    //  void Goo(object o);
    //    //  ...
    //    //   if (x)
    //    //     Goo(y);
    //    //   else
    //    //     Goo(z);
    //    //
    //    // Then we need to preserve that meaning so that 'y' and 'z' have the same type/value, even after the
    //    // transformation.
    //    var conditionalType = semanticModel.GetTypeInfo(conditionalExpression, cancellationToken).Type;

    //    syntaxFacts.GetPartsOfConditionalExpression(conditionalExpression, out var condition, out var whenTrue, out var whenFalse);
    //    var ifStatement = generator.IfStatement(
    //        condition,
    //        new[] { Rewrite(whenTrue) },
    //        new[] { Rewrite(whenFalse) }).WithTriviaFrom(expressionStatement);

    //    var editor = new SyntaxEditor(root, generator);
    //    editor.ReplaceNode(
    //        expressionStatement,
    //        ifStatement);

    //    return document.WithSyntaxRoot(editor.GetChangedRoot());

    //    TExpressionStatementSyntax Rewrite(SyntaxNode expression)
    //        => WithElasticTrivia(
    //            generator,
    //            expressionStatement.ReplaceNode(conditionalExpression,
    //            WithAddedElasticTrivia(
    //                generator, TryCast(generator, expression, conditionalType))));
    //}
}
