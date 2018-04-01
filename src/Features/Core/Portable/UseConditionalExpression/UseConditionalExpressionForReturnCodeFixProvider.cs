// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    internal class UseConditionalExpressionForReturnCodeFixProvider
        : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseConditionalExpressionForReturnDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, 
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                await FixOneAsync(
                    document, diagnostic, editor, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task FixOneAsync(
            Document document, Diagnostic diagnostic, 
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var ifStatement = diagnostic.AdditionalLocations[0].FindNode(cancellationToken);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var ifOperation = (IConditionalOperation)semanticModel.GetOperation(ifStatement);

            if (!UseConditionalExpressionForReturnHelpers.TryMatchPattern(ifOperation, 
                    out var trueReturn, out var falseReturn))
            {
                return;
            }

            var generator = editor.Generator;

            var conditionalExpression = generator.ConditionalExpression(
                ifOperation.Condition.Syntax.WithoutTrivia(),
                generator.CastExpression(trueReturn.ReturnedValue.Type, trueReturn.ReturnedValue.Syntax.WithoutTrivia()),
                generator.CastExpression(falseReturn.ReturnedValue.Type, falseReturn.ReturnedValue.Syntax.WithoutTrivia()));

            conditionalExpression = conditionalExpression.WithAdditionalAnnotations(Simplifier.Annotation);
            var returnStatement = generator.ReturnStatement(conditionalExpression).WithTriviaFrom(ifStatement);

            editor.ReplaceNode(ifStatement, returnStatement);
            if (ifOperation.WhenFalse == null)
            {
                editor.RemoveNode(falseReturn.Syntax, SyntaxGenerator.DefaultRemoveOptions | SyntaxRemoveOptions.KeepExteriorTrivia);
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
