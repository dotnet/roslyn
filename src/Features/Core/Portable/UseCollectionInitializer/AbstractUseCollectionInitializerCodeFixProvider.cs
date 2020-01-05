// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer
{
    internal abstract class AbstractUseCollectionInitializerCodeFixProvider<
        TSyntaxKind,
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TInvocationExpressionSyntax,
        TExpressionStatementSyntax,
        TVariableDeclaratorSyntax>
        : SyntaxEditorBasedCodeFixProvider
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TExpressionStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseCollectionInitializerDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => !diagnostic.Descriptor.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            // Fix-All for this feature is somewhat complicated.  As Collection-Initializers 
            // could be arbitrarily nested, we have to make sure that any edits we make
            // to one Collection-Initializer are seen by any higher ones.  In order to do this
            // we actually process each object-creation-node, one at a time, rewriting
            // the tree for each node.  In order to do this effectively, we use the '.TrackNodes'
            // feature to keep track of all the object creation nodes as we make edits to
            // the tree.  If we didn't do this, then we wouldn't be able to find the 
            // second object-creation-node after we make the edit for the first one.
            var workspace = document.Project.Solution.Workspace;
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var originalRoot = editor.OriginalRoot;

            var originalObjectCreationNodes = new Stack<TObjectCreationExpressionSyntax>();
            foreach (var diagnostic in diagnostics)
            {
                var objectCreation = (TObjectCreationExpressionSyntax)originalRoot.FindNode(
                    diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
                originalObjectCreationNodes.Push(objectCreation);
            }

            // We're going to be continually editing this tree.  Track all the nodes we
            // care about so we can find them across each edit.
            document = document.WithSyntaxRoot(originalRoot.TrackNodes(originalObjectCreationNodes));
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var currentRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            while (originalObjectCreationNodes.Count > 0)
            {
                var originalObjectCreation = originalObjectCreationNodes.Pop();
                var objectCreation = currentRoot.GetCurrentNodes(originalObjectCreation).Single();

                var matches = ObjectCreationExpressionAnalyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TInvocationExpressionSyntax, TExpressionStatementSyntax, TVariableDeclaratorSyntax>.Analyze(
                    semanticModel, syntaxFacts, objectCreation, cancellationToken);

                if (matches == null || matches.Value.Length == 0)
                {
                    continue;
                }

                var statement = objectCreation.FirstAncestorOrSelf<TStatementSyntax>();
                var newStatement = GetNewStatement(statement, objectCreation, matches.Value)
                    .WithAdditionalAnnotations(Formatter.Annotation);

                var subEditor = new SyntaxEditor(currentRoot, workspace);

                subEditor.ReplaceNode(statement, newStatement);
                foreach (var match in matches)
                {
                    subEditor.RemoveNode(match, SyntaxRemoveOptions.KeepUnbalancedDirectives);
                }

                document = document.WithSyntaxRoot(subEditor.GetChangedRoot());
                semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                currentRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            }

            editor.ReplaceNode(originalRoot, currentRoot);
        }

        protected abstract TStatementSyntax GetNewStatement(
            TStatementSyntax statement, TObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<TExpressionStatementSyntax> matches);

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Collection_initialization_can_be_simplified, createChangedDocument)
            {
            }
        }
    }
}
