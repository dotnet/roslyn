// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Analyzers.UseCollectionInitializer;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.LanguageService;
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
        TForeachStatementSyntax,
        TIfStatementSyntax,
        TVariableDeclaratorSyntax,
        TAnalyzer>
        : SyntaxEditorBasedCodeFixProvider
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TExpressionStatementSyntax : TStatementSyntax
        where TForeachStatementSyntax : TStatementSyntax
        where TIfStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
        where TAnalyzer : AbstractUseCollectionInitializerAnalyzer<
            TExpressionSyntax,
            TStatementSyntax,
            TObjectCreationExpressionSyntax,
            TMemberAccessExpressionSyntax,
            TInvocationExpressionSyntax,
            TExpressionStatementSyntax,
            TForeachStatementSyntax,
            TIfStatementSyntax,
            TVariableDeclaratorSyntax,
            TAnalyzer>, new()
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseCollectionInitializerDiagnosticId);

        protected abstract TAnalyzer GetAnalyzer();

        protected abstract Task<TStatementSyntax> GetNewStatementAsync(
            Document document, CodeActionOptionsProvider fallbackOptions, TStatementSyntax statement, TObjectCreationExpressionSyntax objectCreation, bool useCollectionExpression, ImmutableArray<Match<TStatementSyntax>> matches, CancellationToken cancellationToken);

        protected sealed override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => !diagnostic.Descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.Unnecessary);

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, AnalyzersResources.Collection_initialization_can_be_simplified, nameof(AnalyzersResources.Collection_initialization_can_be_simplified));
            return Task.CompletedTask;
        }

        protected sealed override async Task FixAllAsync(
            Document document,
            ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor,
            CodeActionOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            // Fix-All for this feature is somewhat complicated.  As Collection-Initializers 
            // could be arbitrarily nested, we have to make sure that any edits we make
            // to one Collection-Initializer are seen by any higher ones.  In order to do this
            // we actually process each object-creation-node, one at a time, rewriting
            // the tree for each node.  In order to do this effectively, we use the '.TrackNodes'
            // feature to keep track of all the object creation nodes as we make edits to
            // the tree.  If we didn't do this, then we wouldn't be able to find the 
            // second object-creation-node after we make the edit for the first one.
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var originalRoot = editor.OriginalRoot;

            var originalObjectCreationNodes = new Stack<(TObjectCreationExpressionSyntax objectCreationExpression, bool useCollectionExpression)>();
            foreach (var diagnostic in diagnostics)
            {
                var objectCreation = (TObjectCreationExpressionSyntax)originalRoot.FindNode(
                    diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
                originalObjectCreationNodes.Push((objectCreation, diagnostic.Properties?.ContainsKey(UseCollectionInitializerHelpers.UseCollectionExpressionName) is true));
            }

            var solutionServices = document.Project.Solution.Services;

            // We're going to be continually editing this tree.  Track all the nodes we
            // care about so we can find them across each edit.
            var semanticDocument = await SemanticDocument.CreateAsync(
                document.WithSyntaxRoot(originalRoot.TrackNodes(originalObjectCreationNodes.Select(static t => t.objectCreationExpression))),
                cancellationToken).ConfigureAwait(false);

            using var analyzer = GetAnalyzer();

            while (originalObjectCreationNodes.Count > 0)
            {
                var (originalObjectCreation, useCollectionExpression) = originalObjectCreationNodes.Pop();
                var currentRoot = semanticDocument.Root;
                var objectCreation = currentRoot.GetCurrentNodes(originalObjectCreation).Single();

                var matches = analyzer.Analyze(
                    semanticDocument.SemanticModel, syntaxFacts, objectCreation, useCollectionExpression, cancellationToken);

                if (matches.IsDefault)
                    continue;

                var statement = objectCreation.FirstAncestorOrSelf<TStatementSyntax>();
                Contract.ThrowIfNull(statement);

                var newStatement = await GetNewStatementAsync(
                    semanticDocument.Document, fallbackOptions, statement, objectCreation, useCollectionExpression, matches, cancellationToken).ConfigureAwait(false);

                var subEditor = new SyntaxEditor(currentRoot, solutionServices);

                subEditor.ReplaceNode(statement, newStatement);
                foreach (var match in matches)
                    subEditor.RemoveNode(match.Statement, SyntaxRemoveOptions.KeepUnbalancedDirectives);

                semanticDocument = await semanticDocument.WithSyntaxRootAsync(
                    subEditor.GetChangedRoot(), cancellationToken).ConfigureAwait(false);
            }

            editor.ReplaceNode(originalRoot, semanticDocument.Root);
        }
    }
}
