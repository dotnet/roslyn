// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.UseConditionalExpression.UseConditionalExpressionHelpers;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
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
            => ImmutableArray.Create(IDEDiagnosticIds.UseConditionalExpressionForReturnDiagnosticId);

        protected abstract bool IsRef(IReturnOperation returnOperation);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixOneAsync(
            Document document, Diagnostic diagnostic,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ifStatement = (TIfStatementSyntax)diagnostic.AdditionalLocations[0].FindNode(cancellationToken);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var ifOperation = (IConditionalOperation)semanticModel.GetOperation(ifStatement);

            if (!UseConditionalExpressionForReturnHelpers.TryMatchPattern(
                    syntaxFacts, ifOperation,
                    out var trueReturn, out var falseReturn))
            {
                return;
            }

            var conditionalExpression = await CreateConditionalExpressionAsync(
                document, ifOperation, trueReturn, falseReturn,
                trueReturn.ReturnedValue, falseReturn.ReturnedValue,
                IsRef(trueReturn), cancellationToken).ConfigureAwait(false);

            var returnStatement = trueReturn.Kind == OperationKind.YieldReturn
                ? (TStatementSyntax)editor.Generator.YieldReturnStatement(conditionalExpression)
                : (TStatementSyntax)editor.Generator.ReturnStatement(conditionalExpression);

            returnStatement = returnStatement.WithTriviaFrom(ifStatement);

            editor.ReplaceNode(
                ifStatement,
                WrapWithBlockIfAppropriate(ifStatement, returnStatement));

            // if the if-statement had no 'else' clause, then we were using the following statement
            // as the 'false' statement.  If so, remove it explicitly.
            if (ifOperation.WhenFalse == null)
            {
                editor.RemoveNode(falseReturn.Syntax, GetRemoveOptions(syntaxFacts, falseReturn.Syntax));
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Convert_to_conditional_expression, createChangedDocument, IDEDiagnosticIds.UseConditionalExpressionForReturnDiagnosticId)
            {
            }
        }
    }
}
