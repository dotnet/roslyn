// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.ConvertLinq
{
    internal abstract class AbstractConvertLinqProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);

            await CreateAnalyzer(syntaxFacts, semanticModel)
                .ComputeRefactoringsAsync(context).ConfigureAwait(false);
        }

        protected abstract IAnalyzer CreateAnalyzer(ISyntaxFactsService syntaxFacts, SemanticModel semanticModel);

        protected abstract class Analyzer<TSource, TDestination> : IAnalyzer
            where TSource : SyntaxNode
            where TDestination : SyntaxNode
        {
            protected readonly ISyntaxFactsService _syntaxFacts;
            protected readonly SemanticModel _semanticModel;

            public Analyzer(ISyntaxFactsService syntaxFacts, SemanticModel semanticModel)
            {
                _syntaxFacts = syntaxFacts;
                _semanticModel = semanticModel;
            }

            public async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
            {
                var document = context.Document;
                var cancellationToken = context.CancellationToken;
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var sourceNode = FindNodeToRefactor(root, context);
                if (sourceNode == null)
                {
                    return;
                }

                if (sourceNode.ContainsDiagnostics)
                {
                    return;
                }

                var destinationNode = Convert(sourceNode);
                if (destinationNode == null)
                {
                    return;
                }

                if (!Validate(sourceNode, destinationNode, context.CancellationToken))
                {
                    return;
                }

                context.RegisterRefactoring(new MyCodeAction(Title, c =>
                    UpdateDocumentAsync(root, document, sourceNode, destinationNode)));
            }

            protected abstract TSource FindNodeToRefactor(SyntaxNode root, CodeRefactoringContext context);

            protected abstract TDestination Convert(TSource source);

            protected abstract bool Validate(TSource source, TDestination destination, CancellationToken cancellationToken);

            protected abstract string Title { get;  }

            // TODO probably protected virtual
            private Task<Document> UpdateDocumentAsync(
                SyntaxNode root,
                Document document,
                TSource sourceNode,
                TDestination destinationNode)
            {
                root = root.ReplaceNode(root.FindNode(sourceNode.Span), destinationNode);
                return Task.FromResult(document.WithSyntaxRoot(root));
            }
        }

        protected interface IAnalyzer
        {
            Task ComputeRefactoringsAsync(CodeRefactoringContext context);
        }

        protected sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
