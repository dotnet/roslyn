// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

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

            await CreateAnalyzer(semanticModel, context.CancellationToken)
                .ComputeRefactoringsAsync(context).ConfigureAwait(false);
        }

        protected abstract IAnalyzer CreateAnalyzer(SemanticModel semanticModel, CancellationToken cancellationToken);

        protected interface IAnalyzer
        {
            Task ComputeRefactoringsAsync(CodeRefactoringContext context);
        }

        protected abstract class AnalyzerBase<TRefactor, TSource, TDestination> : IAnalyzer
            where TRefactor : SyntaxNode
            where TSource : SyntaxNode
            where TDestination : SyntaxNode
        {
            protected readonly SemanticModel _semanticModel;
            protected readonly CancellationToken _cancellationToken;

            public AnalyzerBase(SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                _semanticModel = semanticModel;
                _cancellationToken = cancellationToken;
            }

            public async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
            {
                var document = context.Document;
                var cancellationToken = context.CancellationToken;
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var refactor = FindNodeToRefactor(root, context);
                if (refactor == null)
                {
                    return;
                }

                if (refactor.ContainsDiagnostics)
                {
                    return;
                }

                if (TryConvert(refactor, out var result))
                {
                    if (!result.IsValid())
                    {
                        return;
                    }

                    context.RegisterRefactoring(new MyCodeAction(Title, c => UpdateDocumentAsync(root, document, result)));
                }
            }

            protected abstract bool TryConvert(TRefactor refactor, out DocumentUpdate documentUpdate);

            protected virtual TRefactor FindNodeToRefactor(SyntaxNode root, CodeRefactoringContext context) =>
                root.FindNode(context.Span).FirstAncestorOrSelf<TRefactor>();

            protected abstract string Title { get; }

            private Task<Document> UpdateDocumentAsync(
                SyntaxNode root,
                Document document,
                DocumentUpdate documentUpdate)
            {
                root = documentUpdate.UpdateRoot(root);
                return Task.FromResult(document.WithSyntaxRoot(root));
            }
        }

        protected sealed class DocumentUpdate
        {
            private ImmutableArray<(SyntaxNode Source, ImmutableArray<SyntaxNode> Destinations)> _updates;

            public DocumentUpdate(SyntaxNode source, SyntaxNode destination)
                => _updates = ImmutableArray.Create((source, ImmutableArray.Create(destination)));

            public DocumentUpdate(SyntaxNode source, IEnumerable<SyntaxNode> destination)
                => _updates = ImmutableArray.Create((source, ImmutableArray.CreateRange(destination)));

            public bool IsValid()
            {
                return _updates.Any() && _updates.All(item => item.Source != null && item.Destinations != null);
            }

            public SyntaxNode UpdateRoot(SyntaxNode root)
            {
                foreach (var updateItem in _updates.OrderByDescending(update => update.Source.Span.End))
                {
                    if (updateItem.Destinations.Any())
                    {
                        // TODO do we need to find node?
                        // TODO consider using SyntaxEditor
                        root = root.ReplaceNode(root.FindNode(updateItem.Source.Span), updateItem.Destinations.Select(node => node.WithAdditionalAnnotations(Simplifier.Annotation)));
                    }
                    else
                    {
                        root = root.RemoveNode(updateItem.Source, SyntaxRemoveOptions.AddElasticMarker);
                    }
                }

                return root;
            }
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
