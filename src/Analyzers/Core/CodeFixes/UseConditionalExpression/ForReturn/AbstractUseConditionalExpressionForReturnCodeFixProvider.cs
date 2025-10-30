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
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.UseConditionalExpression.UseConditionalExpressionCodeFixHelpers;

namespace Microsoft.CodeAnalysis.UseConditionalExpression;

internal abstract class AbstractUseConditionalExpressionForReturnCodeFixProvider<
    TStatementSyntax,
    TIfStatementSyntax,
    TExpressionSyntax,
    TConditionalExpressionSyntax>
    : AbstractUseConditionalExpressionCodeFixProvider<TStatementSyntax, TIfStatementSyntax, TExpressionSyntax, TConditionalExpressionSyntax>
    where TStatementSyntax : SyntaxNode
    where TIfStatementSyntax : TStatementSyntax
    where TExpressionSyntax : SyntaxNode
    where TConditionalExpressionSyntax : TExpressionSyntax
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseConditionalExpressionForReturnDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var (title, key) = context.Diagnostics.First().Properties.ContainsKey(UseConditionalExpressionHelpers.CanSimplifyName)
            ? (AnalyzersResources.Simplify_check, nameof(AnalyzersResources.Simplify_check))
            : (AnalyzersResources.Convert_to_conditional_expression, nameof(AnalyzersResources.Convert_to_conditional_expression));

        RegisterCodeFix(context, title, key);
        return Task.CompletedTask;
    }

    protected override async Task FixOneAsync(
        Document document,
        Diagnostic diagnostic,
        SyntaxEditor editor,
        SyntaxFormattingOptions formattingOptions,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var ifStatement = (TIfStatementSyntax)diagnostic.AdditionalLocations[0].FindNode(cancellationToken);

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var ifOperation = (IConditionalOperation)semanticModel.GetOperation(ifStatement, cancellationToken)!;
        var containingSymbol = semanticModel.GetRequiredEnclosingSymbol(ifStatement.SpanStart, cancellationToken);

        if (!UseConditionalExpressionForReturnHelpers.TryMatchPattern(
                syntaxFacts, ifOperation, containingSymbol, cancellationToken, out var isRef,
                out var trueStatement, out var falseStatement,
                out var trueReturn, out var falseReturn))
        {
            return;
        }

        var anyReturn = (trueReturn ?? falseReturn)!;
        var conditionalExpression = await CreateConditionalExpressionAsync(
            document, ifOperation,
            trueStatement, falseStatement,
            trueReturn?.ReturnedValue ?? trueStatement,
            falseReturn?.ReturnedValue ?? falseStatement,
            isRef,
            formattingOptions,
            cancellationToken).ConfigureAwait(false);

        var generatorInternal = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();
        var returnStatement = anyReturn.Kind == OperationKind.YieldReturn
            ? (TStatementSyntax)generatorInternal.YieldReturnStatement(conditionalExpression)
            : (TStatementSyntax)editor.Generator.ReturnStatement(conditionalExpression);

        returnStatement = returnStatement.WithTriviaFrom(ifStatement);

        editor.ReplaceNode(
            ifStatement,
            WrapWithBlockIfAppropriate(ifStatement, returnStatement));

        // if the if-statement had no 'else' clause, then we were using the following statement
        // as the 'false' statement.  If so, remove it explicitly.
        if (ifOperation.WhenFalse == null)
        {
            editor.RemoveNode(falseStatement.Syntax, GetRemoveOptions(syntaxFacts, falseStatement.Syntax));
        }
    }
}
