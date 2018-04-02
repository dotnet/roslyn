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
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.UseConditionalExpression.UseConditionalExpressionHelpers;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal abstract class AbstractUseConditionalExpressionForReturnCodeFixProvider
        : SyntaxEditorBasedCodeFixProvider
    {
        protected abstract IFormattingRule GetMultiLineFormattingRule();

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseConditionalExpressionForReturnDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, 
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            return UseConditionalExpressionHelpers.FixAllAsync(
                document, diagnostics, editor, FixOneAsync,
                GetMultiLineFormattingRule(), cancellationToken);
        }

        private async Task<bool> FixOneAsync(
            Document document, Diagnostic diagnostic, 
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var ifStatement = diagnostic.AdditionalLocations[0].FindNode(cancellationToken);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var ifOperation = (IConditionalOperation)semanticModel.GetOperation(ifStatement);

            if (!UseConditionalExpressionForReturnHelpers.TryMatchPattern(ifOperation, 
                    out var trueReturn, out var falseReturn))
            {
                return false;
            }

            var generator = editor.Generator;
            var (conditionalExpression, isMultiLine) = await CreateConditionalExpressionAsync<SyntaxNode>(
                document, generator, ifOperation,
                trueReturn.ReturnedValue, falseReturn.ReturnedValue,
                cancellationToken).ConfigureAwait(false);

            var returnStatement = generator.ReturnStatement(conditionalExpression).WithTriviaFrom(ifStatement);

            editor.ReplaceNode(ifStatement, returnStatement);
            if (ifOperation.WhenFalse == null)
            {
                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                editor.RemoveNode(falseReturn.Syntax, GetRemoveOptions(syntaxFacts, falseReturn.Syntax));
            }

            return isMultiLine;
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
