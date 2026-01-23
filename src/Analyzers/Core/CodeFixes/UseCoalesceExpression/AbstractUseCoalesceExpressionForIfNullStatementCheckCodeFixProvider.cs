// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseCoalesceExpression;

internal abstract class AbstractUseCoalesceExpressionForIfNullStatementCheckCodeFixProvider()
    : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseCoalesceExpressionForIfNullCheckDiagnosticId];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        => RegisterCodeFix(context, AnalyzersResources.Use_coalesce_expression, nameof(AnalyzersResources.Use_coalesce_expression));

    protected virtual ITypeSymbol? TryGetExplicitCast(
        SemanticModel semanticModel, SyntaxNode expressionToCoalesce,
        SyntaxNode leftAssignmentPart, SyntaxNode rightAssignmentPart,
        CancellationToken cancellationToken) => null;

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var generator = editor.Generator;

        foreach (var diagnostic in diagnostics)
        {
            var expressionToCoalesce = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
            var ifStatement = diagnostic.AdditionalLocations[1].FindNode(getInnermostNodeForTie: true, cancellationToken);
            var whenTrueStatement = diagnostic.AdditionalLocations[2].FindNode(getInnermostNodeForTie: true, cancellationToken);

            editor.RemoveNode(ifStatement);
            editor.ReplaceNode(
                expressionToCoalesce,
                generator.CoalesceExpression(
                    TryAddExplicitCast(expressionToCoalesce, whenTrueStatement).WithoutTrivia(),
                    GetWhenNullExpression(whenTrueStatement).WithoutTrailingTrivia()).WithTriviaFrom(expressionToCoalesce));

            var ifStatementLeadingTrivia = GetLeadingComments(ifStatement);
            var whenTrueStatementLeadingTrivia = GetLeadingComments(whenTrueStatement);
            var containingStatement = expressionToCoalesce.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsStatement);

            if ((ifStatementLeadingTrivia.Length >= 0 || whenTrueStatementLeadingTrivia.Length >= 0) &&
                containingStatement != null)
            {
                var finalTrivia = containingStatement
                    .GetLeadingTrivia()
                    .Concat(ifStatementLeadingTrivia)
                    .Concat(whenTrueStatementLeadingTrivia);
                editor.ReplaceNode(
                    containingStatement,
                    (current, _) => current.WithLeadingTrivia(finalTrivia).WithAdditionalAnnotations(Formatter.Annotation));
            }
        }

        return;

        ImmutableArray<SyntaxTrivia> GetLeadingComments(SyntaxNode node)
        {
            foreach (var trivia in node.GetLeadingTrivia())
            {
                if (syntaxFacts.IsSingleLineCommentTrivia(trivia) || syntaxFacts.IsMultiLineCommentTrivia(trivia))
                    return [.. node.GetLeadingTrivia().SkipWhile(syntaxFacts.IsWhitespaceOrEndOfLineTrivia)];
            }

            return [];
        }

        SyntaxNode TryAddExplicitCast(SyntaxNode expressionToCoalesce, SyntaxNode whenTrueStatement)
        {
            // This can be either SimpleAssignmentStatement or ThrowStatement
            // We only care about casting in the former case since the two
            // types being coalesce-d might not be the same and might result in broken
            // code without the cast.
            // In the latter case something like
            // _ = myParameter ?? throw new ArgumentNullException(nameof(myParameter));
            // will be always valid.
            if (!syntaxFacts.IsSimpleAssignmentStatement(whenTrueStatement))
                return expressionToCoalesce;

            syntaxFacts.GetPartsOfAssignmentStatement(whenTrueStatement, out var left, out var right);

            var castTo = TryGetExplicitCast(semanticModel, expressionToCoalesce, left, right, cancellationToken);
            return castTo is null
                ? expressionToCoalesce
                : generator.CastExpression(castTo, expressionToCoalesce);
        }

        SyntaxNode GetWhenNullExpression(SyntaxNode whenTrueStatement)
        {
            if (syntaxFacts.IsSimpleAssignmentStatement(whenTrueStatement))
            {
                syntaxFacts.GetPartsOfAssignmentStatement(whenTrueStatement, out _, out var right);
                return right;
            }
            else if (syntaxFacts.IsThrowStatement(whenTrueStatement))
            {
                var expression = syntaxFacts.GetExpressionOfThrowStatement(whenTrueStatement);
                Contract.ThrowIfNull(expression); // checked in analyzer.
                return generator.ThrowExpression(expression);
            }
            else
            {
                throw ExceptionUtilities.Unreachable();
            }
        }
    }
}
