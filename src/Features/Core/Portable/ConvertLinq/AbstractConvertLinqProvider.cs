// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Formatting;
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

            await CreateAnalyzer(semanticModel, document, context.CancellationToken)
                .ComputeRefactoringsAsync(context).ConfigureAwait(false);
        }

        protected abstract IAnalyzer CreateAnalyzer(SemanticModel semanticModel, Document document, CancellationToken cancellationToken);

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
            protected readonly Document _document;

            public AnalyzerBase(SemanticModel semanticModel, Document document, CancellationToken cancellationToken)
            {
                _semanticModel = semanticModel;
                // TODO consider removing
                _document = document;
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
                    // TODO should not be checked inside tryconvert?
                    if (!result.IsValid())
                    {
                        return;
                    }

                    if (!Validate(result))
                    {
                        return;
                    }

                    context.RegisterRefactoring(new MyCodeAction(Title, c => UpdateDocumentAsync(root, document, result)));
                }
            }

            protected abstract bool TryConvert(TRefactor refactor, out DocumentUpdate documentUpdate);

            protected virtual TRefactor FindNodeToRefactor(SyntaxNode root, CodeRefactoringContext context) =>
                root.FindNode(context.Span).FirstAncestorOrSelf<TRefactor>();

            protected virtual bool Validate(DocumentUpdate documentUpdate) => true;

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
            private ImmutableArray<(SyntaxNode source, ImmutableArray<SyntaxNode> destinations)> _updates;

            public DocumentUpdate(SyntaxNode source, SyntaxNode destination)
                => _updates = ImmutableArray.Create((source, ImmutableArray.Create(destination)));

            public DocumentUpdate(SyntaxNode source, IEnumerable<SyntaxNode> destination)
            => _updates = ImmutableArray.Create((source, ImmutableArray.CreateRange(destination)));

            // TODO add a ctor for multiple sources
            public bool IsValid()
            {
                return _updates.Any() && _updates.All(item => item.source != null && item.destinations != null);
            }

            public SyntaxNode UpdateRoot(SyntaxNode root)
            {
                // SyntaxEditor
                // TODO do we need to sort nodes by span desc?
                foreach (var updateItem in _updates)
                {
                    if (updateItem.destinations.Any())
                    {
                        // TODO do we need to find node?
                        root = root.ReplaceNode(root.FindNode(updateItem.source.Span), updateItem.destinations.Select(node => node.WithAdditionalAnnotations(Simplifier.Annotation)));
                    }
                    else
                    {
                        root = root.RemoveNode(updateItem.source, SyntaxRemoveOptions.AddElasticMarker);
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
