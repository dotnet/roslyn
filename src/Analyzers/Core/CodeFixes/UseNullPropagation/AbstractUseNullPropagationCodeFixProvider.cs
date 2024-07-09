// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseNullPropagation;

internal abstract class AbstractUseNullPropagationCodeFixProvider<
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
    TElementBindingArgumentListSyntax> : SyntaxEditorBasedCodeFixProvider
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
    protected abstract bool TryGetBlock(SyntaxNode? node, [NotNullWhen(true)] out TStatementSyntax? block);
    protected abstract TStatementSyntax ReplaceBlockStatements(TStatementSyntax block, TStatementSyntax newInnerStatement);
    protected abstract SyntaxNode PostProcessElseIf(TIfStatementSyntax ifStatement, TStatementSyntax newWhenTrueStatement);
    protected abstract TElementBindingExpressionSyntax ElementBindingExpression(TElementBindingArgumentListSyntax argumentList);

    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseNullPropagationDiagnosticId];

    protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
        => !diagnostic.Descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.Unnecessary);

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, AnalyzersResources.Use_null_propagation, nameof(AnalyzersResources.Use_null_propagation));
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var root = editor.OriginalRoot;

        foreach (var diagnostic in diagnostics)
        {
            var conditionalExpressionOrIfStatement = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
            if (conditionalExpressionOrIfStatement is TIfStatementSyntax ifStatement)
            {
                FixIfStatement(document, editor, diagnostic, ifStatement);
            }
            else
            {
                FixConditionalExpression(document, editor, semanticModel, diagnostic, conditionalExpressionOrIfStatement, cancellationToken);
            }
        }
    }

    private void FixConditionalExpression(
        Document document,
        SyntaxEditor editor,
        SemanticModel semanticModel,
        Diagnostic diagnostic,
        SyntaxNode conditionalExpression,
        CancellationToken cancellationToken)
    {
        var root = editor.OriginalRoot;

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var generator = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();

        var conditionalPart = root.FindNode(diagnostic.AdditionalLocations[1].SourceSpan, getInnermostNodeForTie: true);
        var whenPart = root.FindNode(diagnostic.AdditionalLocations[2].SourceSpan, getInnermostNodeForTie: true);
        syntaxFacts.GetPartsOfConditionalExpression(
            conditionalExpression, out _, out var whenTrue, out _);
        whenTrue = syntaxFacts.WalkDownParentheses(whenTrue);

        var whenPartIsNullable = diagnostic.Properties.ContainsKey(UseNullPropagationConstants.WhenPartIsNullable);
        editor.ReplaceNode(
            conditionalExpression,
            (conditionalExpression, _) =>
            {
                syntaxFacts.GetPartsOfConditionalExpression(
                    conditionalExpression, out var currentCondition, out var currentWhenTrue, out var currentWhenFalse);

                var currentWhenPartToCheck = whenPart == whenTrue ? currentWhenTrue : currentWhenFalse;

                var unwrappedCurrentWhenPartToCheck = syntaxFacts.WalkDownParentheses(currentWhenPartToCheck);

                var match = AbstractUseNullPropagationDiagnosticAnalyzer<
                    TSyntaxKind, TExpressionSyntax, TStatementSyntax,
                    TConditionalExpressionSyntax, TBinaryExpressionSyntax, TInvocationExpressionSyntax,
                    TConditionalAccessExpressionSyntax, TElementAccessExpressionSyntax, TMemberAccessExpressionSyntax,
                    TIfStatementSyntax, TExpressionStatementSyntax>.GetWhenPartMatch(
                        syntaxFacts, semanticModel, (TExpressionSyntax)conditionalPart, (TExpressionSyntax)unwrappedCurrentWhenPartToCheck, cancellationToken);
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

    private void FixIfStatement(
        Document document,
        SyntaxEditor editor,
        Diagnostic diagnostic,
        TIfStatementSyntax ifStatement)
    {
        var root = editor.OriginalRoot;

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var generator = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();

        var whenTrueStatement = (TStatementSyntax)root.FindNode(diagnostic.AdditionalLocations[1].SourceSpan);
        var match = (TExpressionSyntax)root.FindNode(diagnostic.AdditionalLocations[2].SourceSpan, getInnermostNodeForTie: true);

        var whenPartIsNullable = diagnostic.Properties.ContainsKey(UseNullPropagationConstants.WhenPartIsNullable);

        SyntaxNode nodeToBeReplaced = ifStatement;
        SyntaxNode? replacementNode = null;

        // we have `if (x != null) x.Y();`.  Update `x.Y()` to be `x?.Y()`, then replace the entire
        // if-statement with that expression statement.
        var newWhenTrueStatement = CreateConditionalAccessExpression(
            syntaxFacts, generator, whenPartIsNullable, whenTrueStatement, match);
        Contract.ThrowIfNull(newWhenTrueStatement);

        var isElseIf = syntaxFacts.IsElseClause(ifStatement.Parent);

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
        if (isElseIf &&
            TryGetBlock(whenTrueStatement.Parent, out var block))
        {
            newWhenTrueStatement = ReplaceBlockStatements(block, newWhenTrueStatement);
        }

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
            if (isElseIf)
            {
                nodeToBeReplaced = ifStatement.Parent!;
                replacementNode = PostProcessElseIf(ifStatement, newWhenTrueStatement);
            }
            else
            {
                newWhenTrueStatement = newWhenTrueStatement.WithLeadingTrivia(ifStatement.GetLeadingTrivia());
            }
        }

        // If there's trailing comments on the original inner statement, then preserve that.  Otherwise,
        // replace it with the trailing trivia of hte original if-statement.
        if (!newWhenTrueStatement.GetTrailingTrivia().Any(syntaxFacts.IsRegularComment) && !isElseIf)
            newWhenTrueStatement = newWhenTrueStatement.WithTrailingTrivia(ifStatement.GetTrailingTrivia());

        editor.ReplaceNode(nodeToBeReplaced, replacementNode ?? newWhenTrueStatement);
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

                if (arity == 0 && comparer.Equals(name, nameof(Nullable<int>.Value)))
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
}
