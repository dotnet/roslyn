// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : ISuppressionFixProvider
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

                public override async Task AddDocumentFixesAsync(Document document, ImmutableArray<Diagnostic> diagnostics, Action<CodeAction> addFix, FixAllContext fixAllContext)
                {
                    // Batch all the pragma remove suppression fixes by executing them sequentially for the document.
                    var pragmaActionsBuilder = ImmutableArray.CreateBuilder<IPragmaBasedCodeAction>();
                    var pragmaDiagnosticsBuilder = ImmutableArray.CreateBuilder<Diagnostic>();
                    foreach (var diagnostic in diagnostics.Where(d => d.Location.IsInSource && d.IsSuppressed))
                    {
                        var span = diagnostic.Location.SourceSpan;
                        var removeSuppressionFixes = await _suppressionFixProvider.GetSuppressionsAsync(document, span, SpecializedCollections.SingletonEnumerable(diagnostic), fixAllContext.CancellationToken).ConfigureAwait(false);
                        var removeSuppressionFix = removeSuppressionFixes.SingleOrDefault();
                        if (removeSuppressionFix != null)
                        {
                            var codeAction = removeSuppressionFix.Action as RemoveSuppressionCodeAction;
                            if (codeAction != null)
                            {
                                if (fixAllContext is FixMultipleContext)
                                {
                                    codeAction = codeAction.CloneForFixMultipleContext();
                                }

                                var pragmaRemoveAction = codeAction as PragmaRemoveAction;
                                if (pragmaRemoveAction != null)
                                {
                                    pragmaActionsBuilder.Add(pragmaRemoveAction);
                                    pragmaDiagnosticsBuilder.Add(diagnostic);
                                }
                                else
                                {
                                    addFix(codeAction);
                                }
                            }
                        }
                    }

                    // Get the pragma batch fix.
                    if (pragmaActionsBuilder.Count > 0)
                    {
                        var pragmaBatchFix = PragmaBatchFixHelpers.CreateBatchPragmaFix(_suppressionFixProvider, document,
                            pragmaActionsBuilder.ToImmutable(), pragmaDiagnosticsBuilder.ToImmutable(), fixAllContext);

                        addFix(pragmaBatchFix);
                    }
                }

                public async override Task AddProjectFixesAsync(Project project, ImmutableArray<Diagnostic> diagnostics, Action<CodeAction> addFix, FixAllContext fixAllContext)
                {
                    foreach (var diagnostic in diagnostics.Where(d => !d.Location.IsInSource && d.IsSuppressed))
                    {
                        var removeSuppressionFixes = await _suppressionFixProvider.GetSuppressionsAsync(project, SpecializedCollections.SingletonEnumerable(diagnostic), fixAllContext.CancellationToken).ConfigureAwait(false);
                        var removeSuppressionCodeAction = removeSuppressionFixes.SingleOrDefault()?.Action as RemoveSuppressionCodeAction;
                        if (removeSuppressionCodeAction != null)
                        {
                            if (fixAllContext is FixMultipleContext)
                            {
                                removeSuppressionCodeAction = removeSuppressionCodeAction.CloneForFixMultipleContext();
                            }

                            addFix(removeSuppressionCodeAction);
                        }
                    }
                }

                public override async Task<CodeAction> TryGetMergedFixAsync(IEnumerable<CodeAction> batchOfFixes, FixAllContext fixAllContext)
                {
                    // Batch all the attribute removal fixes into a single fix.
                    // Pragma removal fixes have already been batch for each document AddDocumentFixes method.
                    // This ensures no merge conflicts in merging all fixes by our base implementation.

                    var cancellationToken = fixAllContext.CancellationToken;
                    var oldSolution = fixAllContext.Project.Solution;
                    var currentSolution = oldSolution;

                    var attributeRemoveFixes = new List<AttributeRemoveAction>();
                    var newBatchOfFixes = new List<CodeAction>();
                    foreach (var codeAction in batchOfFixes)
                    {
                        var attributeRemoveFix = codeAction as AttributeRemoveAction;
                        if (attributeRemoveFix != null)
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
                            equivalenceKey: fixAllContext.CodeActionEquivalenceKey);
#pragma warning restore RS0005 // Do not use generic CodeAction.Create to create CodeAction

                        newBatchOfFixes.Insert(0, batchAttributeRemoveFix);
                    }

                    return await base.TryGetMergedFixAsync(newBatchOfFixes, fixAllContext).ConfigureAwait(false);
                }

                private static async Task<ImmutableArray<SyntaxNode>> GetAttributeNodesToFixAsync(ImmutableArray<AttributeRemoveAction> attributeRemoveFixes, CancellationToken cancellationToken)
                {
                    var builder = ImmutableArray.CreateBuilder<SyntaxNode>(attributeRemoveFixes.Length);
                    foreach (var attributeRemoveFix in attributeRemoveFixes)
                    {
                        var attributeToRemove = await attributeRemoveFix.GetAttributeToRemoveAsync(cancellationToken).ConfigureAwait(false);
                        builder.Add(attributeToRemove);
                    }

                    return builder.ToImmutable();
                }
            }
        }
    }
}