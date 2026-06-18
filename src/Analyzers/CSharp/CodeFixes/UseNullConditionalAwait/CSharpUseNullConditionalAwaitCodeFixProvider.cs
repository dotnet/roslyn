// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseNullConditionalAwait;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.UseNullConditionalAwait;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseNullConditionalAwait), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpUseNullConditionalAwaitCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseNullConditionalAwait];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Use_null_conditional_await, nameof(CSharpAnalyzersResources.Use_null_conditional_await));
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in diagnostics)
        {
            var ifStatement = (IfStatementSyntax)diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);

            var statement = ifStatement.Statement is BlockSyntax { Statements: [var single] } ? single : ifStatement.Statement;
            if (statement is not ExpressionStatementSyntax { Expression: AwaitExpressionSyntax awaitExpression })
                continue;

            if (!UseNullConditionalAwaitHelpers.TryGetNotNullCheckOperand(ifStatement.Condition, out var conditionOperand))
                continue;

            var match = UseNullConditionalAwaitHelpers.GetReceiverMatch(
                semanticModel, conditionOperand, awaitExpression.Expression, cancellationToken);
            if (match is null)
                continue;

            // Bare receiver (`await a`) keeps its operand; otherwise splice a `?.` at the receiver.
            var newOperand = match == awaitExpression.Expression.WalkDownParentheses()
                ? awaitExpression.Expression
                : SpliceConditionalAccess(awaitExpression.Expression, match);

            var newAwait = CreateNullConditionalAwait(awaitExpression, newOperand);
            var newStatement = ExpressionStatement(newAwait).WithTriviaFrom(ifStatement);

            editor.ReplaceNode(ifStatement, newStatement);
        }
    }

    private static ExpressionSyntax SpliceConditionalAccess(ExpressionSyntax operand, ExpressionSyntax match)
    {
        var access = match.Parent;
        while (access is ParenthesizedExpressionSyntax)
            access = access.Parent;

        return access switch
        {
            MemberAccessExpressionSyntax memberAccess => operand.ReplaceNode(
                memberAccess, ConditionalAccessExpression(memberAccess.Expression, MemberBindingExpression(memberAccess.Name))),
            ElementAccessExpressionSyntax elementAccess => operand.ReplaceNode(
                elementAccess, ConditionalAccessExpression(elementAccess.Expression, ElementBindingExpression(elementAccess.ArgumentList))),
            _ => operand,
        };
    }

    private static AwaitExpressionSyntax CreateNullConditionalAwait(AwaitExpressionSyntax awaitExpression, ExpressionSyntax newOperand)
    {
        // Move the `await` keyword's trailing trivia to after the `?` so we produce `await? x`, not `await ? x`.
#pragma warning disable RSEXPERIMENTAL006 // Internal usage of the in-progress await? public API.
        return awaitExpression
            .WithAwaitKeyword(awaitExpression.AwaitKeyword.WithoutTrailingTrivia())
            .WithQuestionToken(Token(SyntaxKind.QuestionToken).WithTrailingTrivia(awaitExpression.AwaitKeyword.TrailingTrivia))
            .WithExpression(newOperand);
#pragma warning restore RSEXPERIMENTAL006
    }
}
