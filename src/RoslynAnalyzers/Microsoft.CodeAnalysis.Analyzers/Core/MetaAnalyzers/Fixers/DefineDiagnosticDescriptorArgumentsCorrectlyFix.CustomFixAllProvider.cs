// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
{
    public sealed partial class DefineDiagnosticDescriptorArgumentsCorrectlyFix : CodeFixProvider
    {
        private sealed class CustomFixAllProvider : FixAllProvider
        {
            public static CustomFixAllProvider Instance = new();

            private CustomFixAllProvider()
            {
            }

            public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            {
                // FixAll for source document fixes are handled fine by the batch fixer.
                if (fixAllContext.CodeActionEquivalenceKey!.EndsWith(SourceDocumentEquivalenceKeySuffix, StringComparison.Ordinal))
                {
                    return await WellKnownFixAllProviders.BatchFixer.GetFixAsync(fixAllContext).ConfigureAwait(false);
                }

                // We need custom FixAll handling for additional document fixes.
                Debug.Assert(fixAllContext.CodeActionEquivalenceKey.EndsWith(AdditionalDocumentEquivalenceKeySuffix, StringComparison.Ordinal));

                var diagnosticsToFix = new List<KeyValuePair<Project, ImmutableArray<Diagnostic>>>();
                switch (fixAllContext.Scope)
                {
                    case FixAllScope.Document:
                        {
                            ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Document!).ConfigureAwait(false);
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

                protected override async Task<Solution?> GetChangedSolutionAsync(CancellationToken cancellationToken)
                {
                    // Group fixes by additional documents.
                    var fixInfoMap = new Dictionary<TextDocument, List<FixInfo>>();
                    foreach (var kvp in _diagnosticsToFix)
                    {
                        var project = kvp.Key;
                        var diagnostics = kvp.Value;

                        var additionalDocuments = project.AdditionalDocuments.ToImmutableArray();
                        foreach (var diagnostic in diagnostics)
                        {
                            if (TryGetFixValue(diagnostic, out var fixValue) &&
                                TryGetAdditionalDocumentFixInfo(diagnostic, fixValue, additionalDocuments, out var fixInfo))
                            {
                                var additionalDocument = fixInfo.Value.AdditionalDocumentToFix;
                                RoslynDebug.Assert(additionalDocument != null);
                                if (!fixInfoMap.TryGetValue(additionalDocument, out var fixInfos))
                                {
                                    fixInfos = [];
                                    fixInfoMap.Add(additionalDocument, fixInfos);
                                }

                                fixInfos.Add(fixInfo.Value);
                            }
                        }
                    }

                    var newSolution = _solution;
                    foreach (var kvp in fixInfoMap)
                    {
                        var additionalDocument = kvp.Key;
                        var fixInfos = kvp.Value;

                        var text = await additionalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        using var _1 = ArrayBuilder<TextChange>.GetInstance(fixInfos.Count, out var textChanges);
                        using var _2 = PooledHashSet<TextSpan>.GetInstance(out var seenInputSpansToFix);
                        foreach (var fixInfo in fixInfos)
                        {
                            var inputSpanToFix = fixInfo.AdditionalDocumentSpanToFix!.Value;
                            if (seenInputSpansToFix.Add(inputSpanToFix))
                            {
                                textChanges.Add(new TextChange(inputSpanToFix, fixInfo.FixValue));
                            }
                        }

                        var newText = text.WithChanges(textChanges);
                        newSolution = newSolution.WithAdditionalDocumentText(additionalDocument.Id, newText);
                    }

                    return newSolution;
                }
            }
        }
    }
}
