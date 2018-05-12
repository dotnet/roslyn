// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ConvertLinq
{
    internal abstract class AbstractConvertForEachToLinqQueryProvider<TForEachStatement> : CodeRefactoringProvider
        where TForEachStatement : SyntaxNode
    {
        protected abstract string Title { get; }

        protected abstract bool TryConvert(
            TForEachStatement forEachStatement,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out IConverter converter);

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

            if (TryConvert(forEachStatement, semanticModel, cancellationToken, out IConverter converter) &&
                !semanticModel.GetDiagnostics(forEachStatement.Span, cancellationToken).Any(diagnostic => diagnostic.DefaultSeverity == DiagnosticSeverity.Error))
            {
                context.RegisterRefactoring(new ForEachToLinqQueryCodeAction(Title, c =>
                {
                    var editor = new SyntaxEditor(semanticModel.SyntaxTree.GetRoot(c), document.Project.Solution.Workspace);
                    converter.Convert(editor, semanticModel, c);
                    return Task.FromResult(document.WithSyntaxRoot(editor.GetChangedRoot()));
                }));
            }
        }

        protected interface IConverter
        {
            void Convert(SyntaxEditor editor, SemanticModel semanticModel, CancellationToken cancellationToken);
        }

        private class ForEachToLinqQueryCodeAction : CodeAction.DocumentChangeAction
        {
            public ForEachToLinqQueryCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) : base(title, createChangedDocument) { }
        }
    }
}
