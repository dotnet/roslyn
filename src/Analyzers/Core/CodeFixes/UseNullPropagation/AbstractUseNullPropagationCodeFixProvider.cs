// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseNullPropagation;

internal abstract class AbstractUseNullPropagationCodeFixProvider<
    TAnalyzer,
    TSyntaxKind,
    TExpressionSyntax,
    TStatementSyntax,
    TConditionalExpressionSyntax,
    TBinaryExpressionSyntax,
    TInvocationExpressionSyntax,
    TConditionalAccessExpressionSyntax,
    TElementAccessExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TElementBindingExpressionSyntax,
    TIfStatementSyntax,
    TExpressionStatementSyntax,
    TElementBindingArgumentListSyntax> : ForkingSyntaxEditorBasedCodeFixProvider<SyntaxNode>
    where TAnalyzer : AbstractUseNullPropagationDiagnosticAnalyzer<
        TSyntaxKind, TExpressionSyntax, TStatementSyntax,
        TConditionalExpressionSyntax, TBinaryExpressionSyntax, TInvocationExpressionSyntax,
        TConditionalAccessExpressionSyntax, TElementAccessExpressionSyntax, TMemberAccessExpressionSyntax,
        TIfStatementSyntax, TExpressionStatementSyntax>
    where TSyntaxKind : struct
    where TExpressionSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TConditionalExpressionSyntax : TExpressionSyntax
    where TBinaryExpressionSyntax : TExpressionSyntax
    where TInvocationExpressionSyntax : TExpressionSyntax
    where TConditionalAccessExpressionSyntax : TExpressionSyntax
    where TElementAccessExpressionSyntax : TExpressionSyntax
    where TMemberAccessExpressionSyntax : TExpressionSyntax
    where TElementBindingExpressionSyntax : TExpressionSyntax
    where TIfStatementSyntax : TStatementSyntax
    where TExpressionStatementSyntax : TStatementSyntax
    where TElementBindingArgumentListSyntax : SyntaxNode
{
    protected abstract SyntaxNode PostProcessElseIf(TIfStatementSyntax ifStatement, TStatementSyntax newWhenTrueStatement);
    protected abstract TElementBindingExpressionSyntax ElementBindingExpression(TElementBindingArgumentListSyntax argumentList);

    protected abstract TAnalyzer Analyzer { get; }

    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseNullPropagationDiagnosticId];

    protected override (string title, string equivalenceKey) GetTitleAndEquivalenceKey(CodeFixContext context)
    {
        var firstDiagnostic = context.Diagnostics.First();

        var title = IsTrivialNullableValueAccess(firstDiagnostic.Properties)
            ? AnalyzersResources.Simplify_conditional_expression
            : AnalyzersResources.Use_null_propagation;

        return (title, nameof(AnalyzersResources.Use_null_propagation));
    }

    private static bool IsTrivialNullableValueAccess(ImmutableDictionary<string, string?> properties)
        => properties.ContainsKey(UseNullPropagationHelpers.IsTrivialNullableValueAccess);

    protected override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        SyntaxNode conditionalExpressionOrIfStatement,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        if (conditionalExpressionOrIfStatement is TIfStatementSyntax ifStatement)
        {
            await FixIfStatementAsync(
                document, editor, ifStatement, properties, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await FixConditionalExpressionAsync(
                document, editor, (TConditionalExpressionSyntax)conditionalExpressionOrIfStatement, properties, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task FixConditionalExpressionAsync(
        Document document,
        SyntaxEditor editor,
        TConditionalExpressionSyntax conditionalExpression,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        var root = editor.OriginalRoot;
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var generator = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();

        var conditionalExpressionParts = this.GetPartsOfConditionalExpression(
            semanticModel, conditionalExpression, cancellationToken);
        if (conditionalExpressionParts is not var (conditionalPart, whenPart))
            return;

        syntaxFacts.GetPartsOfConditionalExpression(
            conditionalExpression, out _, out var whenTrue, out _);
        whenTrue = syntaxFacts.WalkDownParentheses(whenTrue);

        // `x == null ? x : x.Value` will be converted to just 'x'.
        if (IsTrivialNullableValueAccess(properties))
        {
            editor.ReplaceNode(
                conditionalExpression,
                conditionalPart.WithTriviaFrom(conditionalExpression));
            return;
        }

        var whenPartIsNullable = properties.ContainsKey(UseNullPropagationHelpers.WhenPartIsNullable);
        editor.ReplaceNode(
            conditionalExpression,
            (conditionalExpression, _) =>
            {
                syntaxFacts.GetPartsOfConditionalExpression(
                    conditionalExpression, out var currentCondition, out var currentWhenTrue, out var currentWhenFalse);

                var currentWhenPartToCheck = whenPart == whenTrue ? currentWhenTrue : currentWhenFalse;

                var unwrappedCurrentWhenPartToCheck = syntaxFacts.WalkDownParentheses(currentWhenPartToCheck);

                var match = this.Analyzer.GetWhenPartMatch(
                        syntaxFacts, semanticModel, conditionalPart, (TExpressionSyntax)unwrappedCurrentWhenPartToCheck, cancellationToken);
                if (match == null)
                {
                    return conditionalExpression;
                }

                var newNode = CreateConditionalAccessExpression(
                    syntaxFacts, generator, whenPartIsNullable, currentWhenPartToCheck, match) ?? conditionalExpression;

                newNode = newNode.WithTriviaFrom(conditionalExpression);
                return newNode;
            });
    }

    private async Task FixIfStatementAsync(
        Document document,
        SyntaxEditor editor,
        TIfStatementSyntax ifStatement,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        var root = editor.OriginalRoot;
        var semanticModel = await document.GetRequiredSemanticModelAsync(CancellationToken.None).ConfigureAwait(false);

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var generator = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();

        var ifStatementParts = this.GetPartsOfIfStatement(semanticModel, ifStatement, cancellationToken);
        if (ifStatementParts is not var (whenTrueStatement, match, nullAssignmentOpt))
            return;

        var whenPartIsNullable = properties.ContainsKey(UseNullPropagationHelpers.WhenPartIsNullable);

        SyntaxNode nodeToBeReplaced = ifStatement;

        // we have `if (x != null) x.Y();`.  Update `x.Y()` to be `x?.Y()`, then replace the entire
        // if-statement with that expression statement.
        var newWhenTrueStatement = CreateConditionalAccessExpression(
            syntaxFacts, generator, whenPartIsNullable, whenTrueStatement, match);
        Contract.ThrowIfNull(newWhenTrueStatement);

        if (syntaxFacts.IsElseClause(ifStatement.Parent))
        {
            // If we have code like:
            // ...
            // else if (v != null)
            // {
            //     v.M();
            // }
            // then we want to keep the result statement in a block:
            // else
            // {
            //     v?.M();
            // }
            // Applies only to C# since VB doesn't have a general-purpose block syntax
            editor.ReplaceNode(ifStatement.Parent, PostProcessElseIf(ifStatement, newWhenTrueStatement));
        }
        else
        {
            // If there's leading trivia on the original inner statement, then combine that with the leading
            // trivia on the if-statement.  We'll need to add a formatting annotation so that the leading comments
            // are put in the right location.
            if (newWhenTrueStatement.GetLeadingTrivia().Any(syntaxFacts.IsRegularComment))
            {
                newWhenTrueStatement = newWhenTrueStatement
                    .WithPrependedLeadingTrivia(ifStatement.GetLeadingTrivia())
                    .WithAdditionalAnnotations(Formatter.Annotation);
            }
            else
            {
                newWhenTrueStatement = newWhenTrueStatement.WithLeadingTrivia(ifStatement.GetLeadingTrivia());
            }

            // If there's trailing comments on the original inner statement, then preserve that.  Otherwise,
            // replace it with the trailing trivia of hte original if-statement.
            if (!newWhenTrueStatement.GetTrailingTrivia().Any(syntaxFacts.IsRegularComment))
                newWhenTrueStatement = newWhenTrueStatement.WithTrailingTrivia(ifStatement.GetTrailingTrivia());

            // If we don't have a `x = null;` statement, then we just replace the if-statement with the new expr?.Statement();
            // If we do have a `x = null;` statement, then insert `expr?.Statement();` and it after the if-statement, then
            // remove the if-statement.
            if (nullAssignmentOpt is null)
            {
                editor.ReplaceNode(nodeToBeReplaced, newWhenTrueStatement);
            }
            else
            {
                using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var replacementNodes);
                replacementNodes.Add(newWhenTrueStatement);

                replacementNodes.Add(nullAssignmentOpt.WithAdditionalAnnotations(Formatter.Annotation));

                editor.InsertAfter(nodeToBeReplaced, replacementNodes);
                editor.RemoveNode(nodeToBeReplaced);
            }
        }
    }

    private TContainer? CreateConditionalAccessExpression<TContainer>(
        ISyntaxFactsService syntaxFacts, SyntaxGeneratorInternal generator, bool whenPartIsNullable,
        TContainer container, SyntaxNode match) where TContainer : SyntaxNode
    {
        if (whenPartIsNullable)
        {
            if (syntaxFacts.IsSimpleMemberAccessExpression(match.Parent))
            {
                var memberAccess = match.Parent;
                var nameNode = syntaxFacts.GetNameOfMemberAccessExpression(memberAccess);
                syntaxFacts.GetNameAndArityOfSimpleName(nameNode, out var name, out var arity);
                var comparer = syntaxFacts.StringComparer;

                if (arity == 0 && comparer.Equals(name, nameof(Nullable<>.Value)))
                {
                    // They're calling ".Value" off of a nullable.  Because we're moving to ?.
                    // we want to remove the .Value as well.  i.e. we should generate:
                    //
                    //      goo?.Bar()  not   goo?.Value.Bar();
                    return CreateConditionalAccessExpression(
                        syntaxFacts, generator, container, match, memberAccess.GetRequiredParent());
                }
            }
        }

        return CreateConditionalAccessExpression(
            syntaxFacts, generator, container, match, match.GetRequiredParent());
    }

    private TContainer? CreateConditionalAccessExpression<TContainer>(
        ISyntaxFactsService syntaxFacts, SyntaxGeneratorInternal generator,
        TContainer whenPart, SyntaxNode match, SyntaxNode matchParent) where TContainer : SyntaxNode
    {
        if (syntaxFacts.IsSimpleMemberAccessExpression(matchParent))
        {
            var memberAccess = matchParent;
            return whenPart.ReplaceNode(memberAccess,
                generator.ConditionalAccessExpression(
                    match,
                    generator.MemberBindingExpression(
                        syntaxFacts.GetNameOfMemberAccessExpression(memberAccess))));
        }

        if (matchParent is TElementAccessExpressionSyntax elementAccess)
        {
            Debug.Assert(syntaxFacts.IsElementAccessExpression(elementAccess));
            var argumentList = (TElementBindingArgumentListSyntax)syntaxFacts.GetArgumentListOfElementAccessExpression(elementAccess)!;
            return whenPart.ReplaceNode(elementAccess,
                generator.ConditionalAccessExpression(
                    match, ElementBindingExpression(argumentList)));
        }

        return null;
    }

    private (TStatementSyntax whenTrueStatement, TExpressionSyntax whenPartMatch, TStatementSyntax? nullAssignmentOpt)? GetPartsOfIfStatement(
        SemanticModel semanticModel, TIfStatementSyntax ifStatement, CancellationToken cancellationToken)
    {
        var (_, referenceEqualsMethod) = this.Analyzer.GetAnalysisSymbols(semanticModel.Compilation);
        var analysisResult = this.Analyzer.AnalyzeIfStatement(
            semanticModel, referenceEqualsMethod, ifStatement, cancellationToken);
        if (analysisResult is null)
            return null;

        return (analysisResult.Value.TrueStatement, analysisResult.Value.WhenPartMatch, analysisResult.Value.NullAssignmentOpt);
    }

    private (TExpressionSyntax conditionalPart, SyntaxNode whenPart)? GetPartsOfConditionalExpression(
        SemanticModel semanticModel, TConditionalExpressionSyntax conditionalExpression, CancellationToken cancellationToken)
    {
        var (expressionType, referenceEqualsMethod) = this.Analyzer.GetAnalysisSymbols(semanticModel.Compilation);
        var analysisResult = this.Analyzer.AnalyzeTernaryConditionalExpression(
            semanticModel, expressionType, referenceEqualsMethod, conditionalExpression, cancellationToken);
        if (analysisResult is null)
            return null;

        return (analysisResult.Value.ConditionPartToCheck, analysisResult.Value.WhenPartToCheck);
    }
}
