// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression;

internal abstract partial class AbstractSuppressionCodeFixProvider : IConfigurationFixProvider
{
    internal abstract partial class RemoveSuppressionCodeAction
    {
        public static FixAllProvider GetBatchFixer(AbstractSuppressionCodeFixProvider suppressionFixProvider)
            => new RemoveSuppressionBatchFixAllProvider(suppressionFixProvider);

        /// <summary>
        /// Batch fixer for pragma suppression removal code action.
        /// </summary>
        private sealed class RemoveSuppressionBatchFixAllProvider(AbstractSuppressionCodeFixProvider suppressionFixProvider) : AbstractSuppressionBatchFixAllProvider
        {
            private readonly AbstractSuppressionCodeFixProvider _suppressionFixProvider = suppressionFixProvider;

            protected override async Task AddDocumentFixesAsync(
                Document document, ImmutableArray<Diagnostic> diagnostics,
                Action<(Diagnostic diagnostic, CodeAction action)> onItemFound,
                FixAllState fixAllState, CancellationToken cancellationToken)
            {
                // Batch all the pragma remove suppression fixes by executing them sequentially for the document.
                using var _1 = ArrayBuilder<IPragmaBasedCodeAction>.GetInstance(out var pragmaActionsBuilder);
                using var _2 = ArrayBuilder<Diagnostic>.GetInstance(out var pragmaDiagnosticsBuilder);

                foreach (var diagnostic in diagnostics.Where(d => d.Location.IsInSource && d.IsSuppressed))
                {
                    var span = diagnostic.Location.SourceSpan;
                    var removeSuppressionFixes = await _suppressionFixProvider.GetFixesAsync(
                        document, span, [diagnostic], cancellationToken).ConfigureAwait(false);
                    var removeSuppressionFix = removeSuppressionFixes.SingleOrDefault();
                    if (removeSuppressionFix != null)
                    {
                        if (removeSuppressionFix.Action is RemoveSuppressionCodeAction codeAction)
                        {
                            if (fixAllState.IsFixMultiple)
                            {
                                codeAction = codeAction.CloneForFixMultipleContext();
                            }

                            if (codeAction is PragmaRemoveAction pragmaRemoveAction)
                            {
                                pragmaActionsBuilder.Add(pragmaRemoveAction);
                                pragmaDiagnosticsBuilder.Add(diagnostic);
                            }
                            else
                            {
                                onItemFound((diagnostic, codeAction));
                            }
                        }
                    }
                }

                // Get the pragma batch fix.
                if (pragmaActionsBuilder.Count > 0)
                {
                    var pragmaBatchFix = PragmaBatchFixHelpers.CreateBatchPragmaFix(
                        _suppressionFixProvider, document,
                        pragmaActionsBuilder.ToImmutableAndClear(),
                        pragmaDiagnosticsBuilder.ToImmutableAndClear(),
                        fixAllState, cancellationToken);

                    onItemFound((diagnostic: null, pragmaBatchFix));
                }
            }

            protected override async Task AddProjectFixesAsync(
                Project project, ImmutableArray<Diagnostic> diagnostics,
                ConcurrentBag<(Diagnostic diagnostic, CodeAction action)> bag,
                FixAllState fixAllState, CancellationToken cancellationToken)
            {
                foreach (var diagnostic in diagnostics.Where(d => !d.Location.IsInSource && d.IsSuppressed))
                {
                    var removeSuppressionFixes = await _suppressionFixProvider.GetFixesAsync(
                        project, [diagnostic], cancellationToken).ConfigureAwait(false);
                    if (removeSuppressionFixes.SingleOrDefault()?.Action is RemoveSuppressionCodeAction removeSuppressionCodeAction)
                    {
                        if (fixAllState.IsFixMultiple)
                        {
                            removeSuppressionCodeAction = removeSuppressionCodeAction.CloneForFixMultipleContext();
                        }

                        bag.Add((diagnostic, removeSuppressionCodeAction));
                    }
                }
            }

            public override async Task<CodeAction> TryGetMergedFixAsync(
                ImmutableArray<(Diagnostic diagnostic, CodeAction action)> batchOfFixes,
                FixAllState fixAllState,
                IProgress<CodeAnalysisProgress> progressTracker,
                CancellationToken cancellationToken)
            {
                // Batch all the attribute removal fixes into a single fix.
                // Pragma removal fixes have already been batch for each document AddDocumentFixes method.
                // This ensures no merge conflicts in merging all fixes by our base implementation.

                var oldSolution = fixAllState.Project.Solution;
                var currentSolution = oldSolution;

                var attributeRemoveFixes = new List<AttributeRemoveAction>();
                var newBatchOfFixes = new List<(Diagnostic diagnostic, CodeAction action)>();
                foreach (var codeAction in batchOfFixes)
                {
                    if (codeAction.action is AttributeRemoveAction attributeRemoveFix)
                    {
                        attributeRemoveFixes.Add(attributeRemoveFix);
                    }
                    else
                    {
                        newBatchOfFixes.Add(codeAction);
                    }
                }

                if (attributeRemoveFixes.Count > 0)
                {
                    // Batch all of attribute removal fixes.
                    foreach (var removeSuppressionFixesForTree in attributeRemoveFixes.GroupBy(fix => fix.SyntaxTreeToModify))
                    {
                        var tree = removeSuppressionFixesForTree.Key;

                        var attributeRemoveFixesForTree = removeSuppressionFixesForTree.OfType<AttributeRemoveAction>().ToImmutableArray();
                        var attributesToRemove = await GetAttributeNodesToFixAsync(attributeRemoveFixesForTree, cancellationToken).ConfigureAwait(false);
                        var document = oldSolution.GetDocument(tree);
                        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                        var newRoot = root.RemoveNodes(attributesToRemove, SyntaxRemoveOptions.KeepLeadingTrivia | SyntaxRemoveOptions.AddElasticMarker);
                        currentSolution = currentSolution.WithDocumentSyntaxRoot(document.Id, newRoot);
                    }

                    var batchAttributeRemoveFix = CodeAction.Create(
                        attributeRemoveFixes.First().Title,
                        createChangedSolution: async ct => currentSolution,
                        equivalenceKey: fixAllState.CodeActionEquivalenceKey);

                    newBatchOfFixes.Insert(0, (diagnostic: null, batchAttributeRemoveFix));
                }

                return await base.TryGetMergedFixAsync(
                    [.. newBatchOfFixes], fixAllState, progressTracker, cancellationToken).ConfigureAwait(false);
            }

            private static async Task<ImmutableArray<SyntaxNode>> GetAttributeNodesToFixAsync(ImmutableArray<AttributeRemoveAction> attributeRemoveFixes, CancellationToken cancellationToken)
            {
                var builder = new FixedSizeArrayBuilder<SyntaxNode>(attributeRemoveFixes.Length);
                foreach (var attributeRemoveFix in attributeRemoveFixes)
                {
                    var attributeToRemove = await attributeRemoveFix.GetAttributeToRemoveAsync(cancellationToken).ConfigureAwait(false);
                    builder.Add(attributeToRemove);
                }

                return builder.MoveToImmutable();
            }
        }
    }
}
