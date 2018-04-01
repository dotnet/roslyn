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
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal abstract class AbstractUseConditionalExpressionForAssignmentCodeFixProvider<
        TLocalDeclarationStatementSyntax,
        TVariableDeclaratorSyntax,
        TExpressionSyntax>
        : SyntaxEditorBasedCodeFixProvider
        where TLocalDeclarationStatementSyntax : SyntaxNode
        where TVariableDeclaratorSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
    {
        protected abstract TVariableDeclaratorSyntax WithInitializer(TVariableDeclaratorSyntax variable, TExpressionSyntax value);
        protected abstract TVariableDeclaratorSyntax GetDeclaratorSyntax(IVariableDeclaratorOperation declarator);
        protected abstract TLocalDeclarationStatementSyntax AddSimplificationToType(TLocalDeclarationStatementSyntax updatedLocalDeclaration);

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseConditionalExpressionForAssignmentDiagnosticId);

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

            if (!UseConditionalExpressionForAssignmentHelpers.TryMatchPattern(ifOperation, 
                    out var localDeclarationOperation, 
                    out var trueAssignment, 
                    out var falseAssignment))
            {
                return;
            }

            var localDeclaration = localDeclarationOperation.Syntax;
            var declarator = localDeclarationOperation.Declarations[0].Declarators[0];
            var variable = GetDeclaratorSyntax(declarator);
            var generator = editor.Generator;

            var conditionalExpression = (TExpressionSyntax)generator.ConditionalExpression(
                ifOperation.Condition.Syntax,
                generator.CastExpression(declarator.Symbol.Type, trueAssignment.Value.Syntax),
                generator.CastExpression(declarator.Symbol.Type, falseAssignment.Value.Syntax));

            conditionalExpression = conditionalExpression.WithAdditionalAnnotations(Simplifier.Annotation);

            var updatedVariable = WithInitializer(variable, conditionalExpression);

            var updatedLocalDeclaration = localDeclaration.ReplaceNode(variable, updatedVariable);
            updatedLocalDeclaration = AddSimplificationToType(
                (TLocalDeclarationStatementSyntax)updatedLocalDeclaration);

            editor.ReplaceNode(localDeclaration, updatedLocalDeclaration);
            editor.RemoveNode(ifStatement, SyntaxGenerator.DefaultRemoveOptions | SyntaxRemoveOptions.KeepExteriorTrivia);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Assignment_can_be_simplified, createChangedDocument, FeaturesResources.Assignment_can_be_simplified)
            {
            }
        }
    }
}
