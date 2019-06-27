// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : IConfigurationFixProvider
    {
        internal abstract partial class RemoveSuppressionCodeAction
        {
            public static BatchFixAllProvider GetBatchFixer(AbstractSuppressionCodeFixProvider suppressionFixProvider)
            {
                return new BatchFixer(suppressionFixProvider);
            }

            /// <summary>
            /// Batch fixer for pragma suppression removal code action.
            /// </summary>
            private sealed class BatchFixer : BatchFixAllProvider
            {
                private readonly AbstractSuppressionCodeFixProvider _suppressionFixProvider;

                public BatchFixer(AbstractSuppressionCodeFixProvider suppressionFixProvider)
                {
                    _suppressionFixProvider = suppressionFixProvider;
                }

                protected override async Task AddDocumentFixesAsync(
                    Document document, ImmutableArray<Diagnostic> diagnostics,
                    ConcurrentBag<(Diagnostic diagnostic, CodeAction action)> fixes,
                    FixAllState fixAllState, CancellationToken cancellationToken)
                {
                    // Batch all the pragma remove suppression fixes by executing them sequentially for the document.
                    var pragmaActionsBuilder = ArrayBuilder<IPragmaBasedCodeAction>.GetInstance();
                    var pragmaDiagnosticsBuilder = ArrayBuilder<Diagnostic>.GetInstance();

                    foreach (var diagnostic in diagnostics.Where(d => d.Location.IsInSource && d.IsSuppressed))
                    {
                        var span = diagnostic.Location.SourceSpan;
                        var removeSuppressionFixes = await _suppressionFixProvider.GetFixesAsync(
                            document, span, SpecializedCollections.SingletonEnumerable(diagnostic), cancellationToken).ConfigureAwait(false);
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
                                    fixes.Add((diagnostic, codeAction));
                                }
                            }
                        }
                    }

                    // Get the pragma batch fix.
                    if (pragmaActionsBuilder.Count > 0)
                    {
                        var pragmaBatchFix = PragmaBatchFixHelpers.CreateBatchPragmaFix(
                            _suppressionFixProvider, document,
                            pragmaActionsBuilder.ToImmutableAndFree(),
                            pragmaDiagnosticsBuilder.ToImmutableAndFree(),
                            fixAllState, cancellationToken);

                        fixes.Add((diagnostic: null, pragmaBatchFix));
                    }
                }

                protected async override Task AddProjectFixesAsync(
                    Project project, ImmutableArray<Diagnostic> diagnostics,
                    ConcurrentBag<(Diagnostic diagnostic, CodeAction action)> bag,
                    FixAllState fixAllState, CancellationToken cancellationToken)
                {
                    foreach (var diagnostic in diagnostics.Where(d => !d.Location.IsInSource && d.IsSuppressed))
                    {
                        var removeSuppressionFixes = await _suppressionFixProvider.GetFixesAsync(
                            project, SpecializedCollections.SingletonEnumerable(diagnostic), cancellationToken).ConfigureAwait(false);
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

                        // This is a temporary generated code action, which doesn't need telemetry, hence suppressing RS0005.
#pragma warning disable RS0005 // Do not use generic CodeAction.Create to create CodeAction
                        var batchAttributeRemoveFix = Create(
                            attributeRemoveFixes.First().Title,
                            createChangedSolution: ct => Task.FromResult(currentSolution),
                            equivalenceKey: fixAllState.CodeActionEquivalenceKey);
#pragma warning restore RS0005 // Do not use generic CodeAction.Create to create CodeAction

                        newBatchOfFixes.Insert(0, (diagnostic: null, batchAttributeRemoveFix));
                    }

                    return await base.TryGetMergedFixAsync(
                        newBatchOfFixes.ToImmutableArray(), fixAllState, cancellationToken).ConfigureAwait(false);
                }

                private static async Task<ImmutableArray<SyntaxNode>> GetAttributeNodesToFixAsync(ImmutableArray<AttributeRemoveAction> attributeRemoveFixes, CancellationToken cancellationToken)
                {
                    var builder = ArrayBuilder<SyntaxNode>.GetInstance(attributeRemoveFixes.Length);
                    foreach (var attributeRemoveFix in attributeRemoveFixes)
                    {
                        var attributeToRemove = await attributeRemoveFix.GetAttributeToRemoveAsync(cancellationToken).ConfigureAwait(false);
                        builder.Add(attributeToRemove);
                    }

                    return builder.ToImmutableAndFree();
                }
            }
        }
    }
}
