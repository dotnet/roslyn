// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ReplaceConditionalWithStatements;

internal abstract class AbstractReplaceConditionalWithStatementsCodeRefactoringProvider<
    TExpressionSyntax,
    TConditionalExpressionSyntax,
    TStatementSyntax,
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
    where TLocalDeclarationStatementSyntax : TStatementSyntax
    where TArgumentSyntax : SyntaxNode
    where TArgumentListSyntax : SyntaxNode
    where TVariableSyntax : SyntaxNode
    where TVariableDeclaratorSyntax : SyntaxNode
    where TEqualsValueClauseSyntax : SyntaxNode
{
    protected abstract bool HasSingleVariable(TLocalDeclarationStatementSyntax localDeclarationStatement, [NotNullWhen(true)] out TVariableSyntax? variable);
    protected abstract bool CanRewriteLocalDeclarationStatement(TLocalDeclarationStatementSyntax localDeclarationStatement);

    protected abstract TLocalDeclarationStatementSyntax GetUpdatedLocalDeclarationStatement(SyntaxGenerator generator, TLocalDeclarationStatementSyntax localDeclarationStatement, ILocalSymbol symbol);

    private static bool IsSupportedSimpleStatement(ISyntaxFacts syntaxFacts, [NotNullWhen(true)] TStatementSyntax? statement)
        => syntaxFacts.IsAnyAssignmentStatement(statement) ||
           syntaxFacts.IsExpressionStatement(statement) ||
           syntaxFacts.IsReturnStatement(statement) ||
           syntaxFacts.IsThrowStatement(statement) ||
           syntaxFacts.IsYieldReturnStatement(statement);

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, _, cancellationToken) = context;

        // First, see if we're inside a conditional.  If so, attempt to use that to offer fixes on.
        var conditionalExpression = await context.TryGetRelevantNodeAsync<TConditionalExpressionSyntax>().ConfigureAwait(false);
        if (conditionalExpression is not null)
        {
            TryHandleConditionalExpression(context, conditionalExpression);
            return;
        }

        // If not, see if we're on an supported statement.  If so, see if it has an applicable conditional within it
        // that we could support this on.
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var statement = await context.TryGetRelevantNodeAsync<TStatementSyntax>().ConfigureAwait(false);
        if (IsSupportedSimpleStatement(syntaxFacts, statement))
        {
            TryHandleConditionalExpression(context, TryFindConditional(statement, cancellationToken));
            return;
        }

        if (statement is TLocalDeclarationStatementSyntax localDeclarationStatement)
        {
            var variables = syntaxFacts.GetVariablesOfLocalDeclarationStatement(localDeclarationStatement);
            if (variables.Count == 1)
            {
                TryHandleConditionalExpression(
                    context, TryFindConditional(syntaxFacts.GetInitializerOfVariableDeclarator(variables[0]), cancellationToken));
                return;
            }
        }
    }

    private static TConditionalExpressionSyntax? TryFindConditional(SyntaxNode? top, CancellationToken cancellationToken)
    {
        if (top is null)
            return null;

        foreach (var node in top.DescendantNodesAndSelf(DescendIntoChildren))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (node is TConditionalExpressionSyntax conditionalExpression)
                return conditionalExpression;
        }

        return null;

        bool DescendIntoChildren(SyntaxNode node)
        {
            return node == top || node is TExpressionSyntax or TArgumentListSyntax or TArgumentSyntax;
        }
    }

    private void TryHandleConditionalExpression(
        CodeRefactoringContext context,
        TConditionalExpressionSyntax? conditionalExpression)
    {
        if (conditionalExpression is null)
            return;

        var document = context.Document;
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        // Walk upwards from this conditional, through parent expressions and arguments until we find the owning construct.
        var topExpression = GetTopExpression(conditionalExpression);

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
        if (IsSupportedSimpleStatement(syntaxFacts, parentStatement))
        {
            context.RegisterRefactoring(CodeAction.Create(
                FeaturesResources.Replace_conditional_expression_with_statements,
                c => ReplaceConditionalExpressionInSingleStatementAsync(document, conditionalExpression, parentStatement, c)),
                conditionalExpression.Span);
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

        if (topExpression.Parent is TEqualsValueClauseSyntax equalsValue &&
            equalsValue.Parent is TVariableDeclaratorSyntax variableDeclarator)
        {
            var localDeclarationStatement = conditionalExpression.GetAncestor<TLocalDeclarationStatementSyntax>();
            if (localDeclarationStatement != null &&
                HasSingleVariable(localDeclarationStatement, out var variable) &&
                syntaxFacts.IsDeclaratorOfLocalDeclarationStatement(variableDeclarator, localDeclarationStatement) &&
                CanRewriteLocalDeclarationStatement(localDeclarationStatement))
            {
                context.RegisterRefactoring(CodeAction.Create(
                    FeaturesResources.Replace_conditional_expression_with_statements,
                    c => ReplaceConditionalExpressionInLocalDeclarationStatementAsync(
                        document, conditionalExpression, variable, localDeclarationStatement, c)),
                    conditionalExpression.Span);
                return;
            }
        }
    }

    private static TExpressionSyntax GetTopExpression(TConditionalExpressionSyntax conditionalExpression)
    {
        TExpressionSyntax current = conditionalExpression;

        while (true)
        {
            if (current.Parent is TExpressionSyntax parentExpression)
            {
                current = parentExpression;
                continue;
            }

            if (current.Parent is TArgumentSyntax { Parent: TArgumentListSyntax { Parent: TExpressionSyntax argumentParent } })
            {
                current = argumentParent;
                continue;
            }

            return current;
        }
    }

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

        var ifStatement = ConvertToIfStatement(
            semanticModel,
            generator,
            container: statement,
            conditionalExpression,
            convertedSyntax => convertedSyntax,
            cancellationToken).WithTriviaFrom(statement);

        var newRoot = root.ReplaceNode(statement, ifStatement);
        return document.WithSyntaxRoot(newRoot);
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

        var declarator = syntaxFacts.GetVariablesOfLocalDeclarationStatement(localDeclarationStatement).Single();
        var initializer = syntaxFacts.GetInitializerOfVariableDeclarator(declarator);
        Contract.ThrowIfNull(initializer);
        var value = syntaxFacts.GetValueOfEqualsValueClause(initializer);

        var symbol = (ILocalSymbol)semanticModel.GetRequiredDeclaredSymbol(variable, cancellationToken);

        var identifier = generator.IdentifierName(symbol.Name);

        var isGlobalStatement = syntaxFacts.IsGlobalStatement(localDeclarationStatement.Parent);
        var updatedLocalDeclarationStatement = GetUpdatedLocalDeclarationStatement(generator, localDeclarationStatement, symbol);
        var ifStatement = ConvertToIfStatement(
            semanticModel,
            generator,
            container: value,
            conditionalExpression,
            convertedSyntax => generator.AssignmentStatement(identifier, convertedSyntax),
            cancellationToken);

        var newRoot = root.ReplaceNode(
            isGlobalStatement ? localDeclarationStatement.GetRequiredParent() : localDeclarationStatement,
            [
                WrapGlobal(updatedLocalDeclarationStatement),
                WrapGlobal(ifStatement),
            ]);

        return document.WithSyntaxRoot(newRoot);

        SyntaxNode WrapGlobal(TStatementSyntax statement)
            => isGlobalStatement ? generator.GlobalStatement(statement) : statement;
    }

    private static TStatementSyntax ConvertToIfStatement(
        SemanticModel semanticModel,
        SyntaxGenerator generator,
        SyntaxNode container,
        TConditionalExpressionSyntax conditionalExpression,
        Func<SyntaxNode, SyntaxNode> wrapConvertedSyntax,
        CancellationToken cancellationToken)
    {
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

        var syntaxFacts = generator.SyntaxFacts;
        syntaxFacts.GetPartsOfConditionalExpression(conditionalExpression, out var condition, out var whenTrue, out var whenFalse);

        return (TStatementSyntax)generator.IfStatement(
            condition.WithoutTrivia(),
            [Rewrite((TExpressionSyntax)whenTrue)],
            [Rewrite((TExpressionSyntax)whenFalse)]);

        SyntaxNode Rewrite(TExpressionSyntax expression)
        {
            if (syntaxFacts.IsThrowExpression(expression))
                return generator.ThrowStatement(syntaxFacts.GetExpressionOfThrowExpression(expression));

            var containerWithConditionalReplaced = container.ReplaceNode(conditionalExpression, TryConvert(expression, conditionalType).WithTriviaFrom(conditionalExpression));
            Contract.ThrowIfNull(containerWithConditionalReplaced);
            return wrapConvertedSyntax(containerWithConditionalReplaced);
        }

        SyntaxNode TryConvert(SyntaxNode expression, ITypeSymbol? conditionalType)
        {
            var syntaxFacts = generator.SyntaxFacts;
            if (syntaxFacts.IsRefExpression(expression))
                return syntaxFacts.GetExpressionOfRefExpression(expression);

            return conditionalType is null or IErrorTypeSymbol
                ? expression
                : generator.ConvertExpression(conditionalType, expression);
        }
    }
}
