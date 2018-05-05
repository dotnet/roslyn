// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ConvertLinq
{
    internal abstract class AbstractConvertForEachToLinqQueryProvider<TForEachStatement, TQueryExpression> : CodeRefactoringProvider
        where TForEachStatement : SyntaxNode
        where TQueryExpression : SyntaxNode
    {
        protected abstract string Title { get; }

        protected abstract bool TryConvert(
            TForEachStatement forEachStatement,
            Workspace workspace,
            SemanticModel semanticModel,
            ISemanticFactsService semanticFacts,
            CancellationToken cancellationToken,
            out SyntaxEditor editor);

        protected abstract TForEachStatement FindNodeToRefactor(SyntaxNode root, TextSpan span);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var forEachStatement = FindNodeToRefactor(root, context.Span);
            if (forEachStatement == null)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

            if (TryConvert(forEachStatement,document.Project.Solution.Workspace, semanticModel, semanticFacts, cancellationToken, out SyntaxEditor editor) &&
                !semanticModel.GetDiagnostics(forEachStatement.Span, cancellationToken).Any(diagnostic => diagnostic.DefaultSeverity == DiagnosticSeverity.Error))
            {
                context.RegisterRefactoring(
                    new ForEachToLinqQueryCodeAction(
                        Title,
                        c => Task.FromResult(document.WithSyntaxRoot(editor.GetChangedRoot()))));
            }
        }

        private class ForEachToLinqQueryCodeAction : CodeAction.DocumentChangeAction
        {
            public ForEachToLinqQueryCodeAction(
                string title,
                Func<CancellationToken, Task<Document>> createChangedDocument) : base(title, createChangedDocument)
            {
            }
        }
    }
}
