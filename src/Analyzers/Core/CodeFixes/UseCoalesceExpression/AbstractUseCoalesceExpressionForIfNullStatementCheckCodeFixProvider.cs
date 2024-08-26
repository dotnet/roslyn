// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseCoalesceExpression;

internal abstract class AbstractUseCoalesceExpressionForIfNullStatementCheckCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseCoalesceExpressionForIfNullCheckDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, AnalyzersResources.Use_coalesce_expression, nameof(AnalyzersResources.Use_coalesce_expression));
        return Task.CompletedTask;
    }

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
        }

        return;

        SyntaxNode TryAddExplicitCast(SyntaxNode expressionToCoalesce, SyntaxNode whenTrueStatement)
        {
            // This can be either SimpleAssignmentStatement or ThrowStatement
            // We only care about casting in the former case.
            if (!syntaxFacts.IsSimpleAssignmentStatement(whenTrueStatement))
                return expressionToCoalesce;

            syntaxFacts.GetPartsOfAssignmentStatement(whenTrueStatement, out var left, out var right);

            var castTo = TryGetExplicitCast(semanticModel, expressionToCoalesce, left, right, cancellationToken);
            if (castTo is null)
            {
                return expressionToCoalesce;
            }

            return generator.CastExpression(castTo, expressionToCoalesce);
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
