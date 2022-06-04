// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

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
    protected abstract bool IsAssignmentStatement(TStatementSyntax? statement);
    protected abstract bool HasSingleVariable(TLocalDeclarationStatementSyntax localDeclarationStatement, [NotNullWhen(true)] out TVariableSyntax? variable);
    protected abstract bool CanRewriteLocalDeclarationStatement(TLocalDeclarationStatementSyntax localDeclarationStatement);

    protected abstract TLocalDeclarationStatementSyntax GetUpdatedLocalDeclarationStatement(SyntaxGenerator generator, TLocalDeclarationStatementSyntax localDeclarationStatement, ILocalSymbol symbol);

    private bool IsSupportedSimpleStatement([NotNullWhen(true)] TStatementSyntax? statement)
        => IsAssignmentStatement(statement) ||
           statement is TExpressionStatementSyntax or
                        TReturnStatementSyntax or
                        TThrowStatementSyntax or
                        TYieldStatementSyntax;

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
        var statement = await context.TryGetRelevantNodeAsync<TStatementSyntax>().ConfigureAwait(false);
        if (IsSupportedSimpleStatement(statement))
        {
            TryHandleConditionalExpression(context, TryFindConditional(statement, cancellationToken));
            return;
        }

        if (statement is TLocalDeclarationStatementSyntax localDeclarationStatement)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
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
        return Recurse(top);

        TConditionalExpressionSyntax? Recurse(SyntaxNode? node)
        {
            if (node is not null)
            {
                foreach (var child in node.ChildNodesAndTokens())
                {
                    if (child.IsToken)
                        continue;

                    var result = CheckNode(child.AsNode());
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        TConditionalExpressionSyntax? CheckNode(SyntaxNode? node)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (node is TConditionalExpressionSyntax conditionalExpression)
                return conditionalExpression;

            if (node is TExpressionSyntax or TArgumentListSyntax or TArgumentSyntax)
                return Recurse(node);

            return null;
        }
    }

    private void TryHandleConditionalExpression(
        CodeRefactoringContext context,
        TConditionalExpressionSyntax? conditionalExpression)
    {
        if (conditionalExpression is null)
            return;

        var (document, _, cancellationToken) = context;
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
        if (IsSupportedSimpleStatement(parentStatement))
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
            equalsValue.Parent is TVariableDeclaratorSyntax variableDeclarator &&
            conditionalExpression.GetAncestor<TLocalDeclarationStatementSyntax>() is { } localDeclarationStatement &&
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

    private static SyntaxNode TryConvert(
        SyntaxGenerator generator,
        SyntaxNode expression,
        ITypeSymbol? conditionalType)
    {
        var syntaxFacts = generator.SyntaxFacts;
        if (syntaxFacts.IsRefExpression(expression))
            return syntaxFacts.GetExpressionOfRefExpression(expression);

        return conditionalType is null or IErrorTypeSymbol
            ? expression
            : generator.ConvertExpression(conditionalType, expression);
    }

    private abstract class AbstractConditionalExpressionReplacer<TContainingStatement>
        where TContainingStatement : TStatementSyntax
    {
        public Document Document { get; }
        public TContainingStatement ContainingStatement { get; }
        public TConditionalExpressionSyntax ConditionalExpression { get; }

        public SemanticModel SemanticModel { get; private set; } = null!;
        public SyntaxNode Root { get; private set; } = null!;
        public SyntaxGenerator Generator { get; private set; } = null!;
        public ISyntaxFacts SyntaxFacts => this.Generator.SyntaxFacts;

        protected AbstractConditionalExpressionReplacer(
            Document document,
            TContainingStatement containingStatement,
            TConditionalExpressionSyntax conditionalExpression)
        {
            Document = document;
            ContainingStatement = containingStatement;
            ConditionalExpression = conditionalExpression;
        }

        protected abstract ImmutableArray<TStatementSyntax> GetReplacementStatements(CancellationToken cancellationToken);

        public async Task<Document> ReplaceAsync(CancellationToken cancellationToken)
        {
            this.SemanticModel = await this.Document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            this.Root = await this.Document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            this.Generator = SyntaxGenerator.GetGenerator(this.Document);

            var replacementStatements = GetReplacementStatements(cancellationToken);

            var isGlobalStatement = this.SyntaxFacts.IsGlobalStatement(this.ContainingStatement.Parent);
            var newRoot = this.Root.ReplaceNode(
                isGlobalStatement ? this.ContainingStatement.GetRequiredParent() : this.ContainingStatement,
                replacementStatements.Select(WrapGlobal));

            return this.Document.WithSyntaxRoot(newRoot);

            SyntaxNode WrapGlobal(TStatementSyntax statement)
                => isGlobalStatement ? this.Generator.GlobalStatement(statement) : statement;
        }

        protected TStatementSyntax ConvertToIfStatement(
            SyntaxNode container,
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
            var conditionalType = this.SemanticModel.GetTypeInfo(this.ConditionalExpression, cancellationToken).Type;

            this.SyntaxFacts.GetPartsOfConditionalExpression(this.ConditionalExpression, out var condition, out var whenTrue, out var whenFalse);

            return (TStatementSyntax)this.Generator.IfStatement(
                condition.WithoutTrivia(),
                new[] { Rewrite((TExpressionSyntax)whenTrue) },
                new[] { Rewrite((TExpressionSyntax)whenFalse) });

            SyntaxNode Rewrite(TExpressionSyntax expression)
            {
                if (this.SyntaxFacts.IsThrowExpression(expression))
                    return this.Generator.ThrowStatement(this.SyntaxFacts.GetExpressionOfThrowExpression(expression));

                var containerWithConditionalReplaced = container.ReplaceNode(
                    this.ConditionalExpression,
                    TryConvert(this.Generator, expression, conditionalType).WithTriviaFrom(this.ConditionalExpression));
                Contract.ThrowIfNull(containerWithConditionalReplaced);
                return wrapConvertedSyntax(containerWithConditionalReplaced);
            }
        }
    }

    private class ConditionalExpressionInLocalDeclarationStatementReplacer :
        AbstractConditionalExpressionReplacer<TLocalDeclarationStatementSyntax>
    {
        public ConditionalExpressionInLocalDeclarationStatementReplacer(
            Document document, TLocalDeclarationStatementSyntax containingStatement, TConditionalExpressionSyntax conditionalExpression)
            : base(document, containingStatement, conditionalExpression)
        {
        }

        protected override ImmutableArray<TStatementSyntax> GetReplacementStatements(CancellationToken cancellationToken)
        {
            var declarator = this.SyntaxFacts.GetVariablesOfLocalDeclarationStatement(ContainingStatement).Single();
            var initializer = this.SyntaxFacts.GetInitializerOfVariableDeclarator(declarator);
            Contract.ThrowIfNull(initializer);
            var value = this.SyntaxFacts.GetValueOfEqualsValueClause(initializer);

            var symbol = (ILocalSymbol)this.SemanticModel.GetRequiredDeclaredSymbol(variable, cancellationToken);

            var identifier = this.Generator.IdentifierName(symbol.Name);

            var isGlobalStatement = this.SyntaxFacts.IsGlobalStatement(this.ContainingStatement.Parent);
            var updatedLocalDeclarationStatement = GetUpdatedLocalDeclarationStatement(
                this.Generator, this.ContainingStatement, symbol);
            var ifStatement = ConvertToIfStatement(
                container: value,
                convertedSyntax => this.Generator.AssignmentStatement(identifier, convertedSyntax),
                cancellationToken);

            return ImmutableArray.Create(updatedLocalDeclarationStatement, ifStatement);
        }
    }

    private class ConditionalExpressionInSingleStatementReplacer :
        AbstractConditionalExpressionReplacer<TStatementSyntax>
    {
        public ConditionalExpressionInSingleStatementReplacer(
            Document document, TStatementSyntax containingStatement, TConditionalExpressionSyntax conditionalExpression)
            : base(document, containingStatement, conditionalExpression)
        {
        }

        protected override ImmutableArray<TStatementSyntax> GetReplacementStatements(CancellationToken cancellationToken)
        {
            var ifStatement = ConvertToIfStatement(
                container: this.ContainingStatement,
                convertedSyntax => convertedSyntax,
                cancellationToken).WithTriviaFrom(this.ContainingStatement);

            return ImmutableArray.Create(ifStatement);
        }
    }
}
