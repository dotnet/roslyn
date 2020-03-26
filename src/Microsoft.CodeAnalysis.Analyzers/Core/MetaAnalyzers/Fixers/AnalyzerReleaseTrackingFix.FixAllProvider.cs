// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
{
    public sealed partial class AnalyzerReleaseTrackingFix
    {
        private sealed class ReleaseTrackingFixAllProvider : FixAllProvider
        {
            public static readonly FixAllProvider Instance = new ReleaseTrackingFixAllProvider();
            public ReleaseTrackingFixAllProvider() { }
            public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            {
                var diagnosticsToFix = new List<KeyValuePair<Project, ImmutableArray<Diagnostic>>>();
                switch (fixAllContext.Scope)
                {
                    case FixAllScope.Document:
                        {
                            ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Document).ConfigureAwait(false);
                            diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(fixAllContext.Project, diagnostics));
                            break;
                        }

                    case FixAllScope.Project:
                        {
                            Project project = fixAllContext.Project;
                            ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                            diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(fixAllContext.Project, diagnostics));
                            break;
                        }

                    case FixAllScope.Solution:
                        {
                            foreach (Project project in fixAllContext.Solution.Projects)
                            {
                                ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                                diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(project, diagnostics));
                            }

                            break;
                        }

                    case FixAllScope.Custom:
                        return null;

                    default:
                        Debug.Fail($"Unknown FixAllScope '{fixAllContext.Scope}'");
                        return null;
                }

                return new FixAllAdditionalDocumentChangeAction(fixAllContext.Scope, fixAllContext.Solution, diagnosticsToFix, fixAllContext.CodeActionEquivalenceKey);
            }

            private sealed class FixAllAdditionalDocumentChangeAction : CodeAction
            {
                private readonly List<KeyValuePair<Project, ImmutableArray<Diagnostic>>> _diagnosticsToFix;
                private readonly Solution _solution;

                public FixAllAdditionalDocumentChangeAction(FixAllScope fixAllScope, Solution solution, List<KeyValuePair<Project, ImmutableArray<Diagnostic>>> diagnosticsToFix, string equivalenceKey)
                {
                    this.Title = fixAllScope.ToString();
                    _solution = solution;
                    _diagnosticsToFix = diagnosticsToFix;
                    this.EquivalenceKey = equivalenceKey;
                }

                public override string Title { get; }
                public override string EquivalenceKey { get; }

                protected override async Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
                {
                    var updatedUnshippedText = new List<(DocumentId, SourceText)>();

                    foreach (KeyValuePair<Project, ImmutableArray<Diagnostic>> pair in _diagnosticsToFix)
                    {
                        Project project = pair.Key;
                        ImmutableArray<Diagnostic> diagnostics = pair.Value;

                        TextDocument? unshippedDocument = project.AdditionalDocuments.FirstOrDefault(a => a.Name == DiagnosticDescriptorCreationAnalyzer.UnshippedFileName);
                        if (unshippedDocument == null || diagnostics.IsEmpty)
                        {
                            continue;
                        }

                        if (EquivalenceKey == CodeAnalysisDiagnosticsResources.AddEntryForDiagnosticIdInAnalyzerReleaseCodeFixTitle)
                        {
                            var newText = await AddEntriesToUnshippedFileForDiagnosticsAsync(unshippedDocument, diagnostics, cancellationToken).ConfigureAwait(false);
                            updatedUnshippedText.Add((unshippedDocument.Id, newText));
                        }
                        else if (EquivalenceKey == CodeAnalysisDiagnosticsResources.UpdateEntryForDiagnosticIdInAnalyzerReleaseCodeFixTitle)
                        {
                            var newText = await UpdateEntriesInUnshippedFileForDiagnosticsAsync(unshippedDocument, diagnostics, cancellationToken).ConfigureAwait(false);
                            updatedUnshippedText.Add((unshippedDocument.Id, newText));
                        }
                    }

                    Solution newSolution = _solution;
                    foreach (var (unshippedDocumentId, newText) in updatedUnshippedText)
                    {
                        newSolution = newSolution.WithAdditionalDocumentText(unshippedDocumentId, newText);
                    }

                    return newSolution;
                }

                private static async Task<SourceText> AddEntriesToUnshippedFileForDiagnosticsAsync(TextDocument unshippedDataDocument, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
                {
                    var entriesToAdd = new SortedSet<string>();
                    foreach (var diagnostic in diagnostics)
                    {
                        if (IsAddEntryToUnshippedFileDiagnostic(diagnostic, out var entryToAdd))
                        {
                            entriesToAdd.Add(entryToAdd);
                        }
                    }

                    return await AddEntriesToUnshippedFileAsync(unshippedDataDocument, entriesToAdd, cancellationToken).ConfigureAwait(false);
                }

                private static async Task<SourceText> UpdateEntriesInUnshippedFileForDiagnosticsAsync(TextDocument unshippedDataDocument, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
                {
                    var entriesToUpdate = new Dictionary<string, string>();
                    foreach (var diagnostic in diagnostics)
                    {
                        if (IsUpdateEntryToUnshippedFileDiagnostic(diagnostic, out var ruleId, out var entryToUpdate))
                        {
                            entriesToUpdate.Add(ruleId, entryToUpdate);
                        }
                    }

                    return await UpdateEntriesInUnshippedFileAsync(unshippedDataDocument, entriesToUpdate, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
