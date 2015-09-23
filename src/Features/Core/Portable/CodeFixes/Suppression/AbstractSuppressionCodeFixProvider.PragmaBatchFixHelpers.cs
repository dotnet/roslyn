// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal partial class AbstractSuppressionCodeFixProvider
    {
        /// <summary>
        /// Helper methods for pragma suppression add/remove batch fixers.
        /// </summary>
        private static class PragmaBatchFixHelpers
        {
            public static CodeAction CreateBatchPragmaFix(
                AbstractSuppressionCodeFixProvider suppressionFixProvider,
                Document document,
                ImmutableArray<IPragmaBasedCodeAction> pragmaActions,
                ImmutableArray<Diagnostic> pragmaDiagnostics,
                FixAllContext fixAllContext)
            {
                // This is a temporary generated code action, which doesn't need telemetry, hence suppressing RS0005.
#pragma warning disable RS0005 // Do not use generic CodeAction.Create to create CodeAction
                return CodeAction.Create(
                    ((CodeAction)pragmaActions[0]).Title,
                    createChangedDocument: ct =>
                        BatchPragmaFixesAsync(suppressionFixProvider, document, pragmaActions, pragmaDiagnostics, fixAllContext.CancellationToken),
                    equivalenceKey: fixAllContext.CodeActionEquivalenceKey);
#pragma warning restore RS0005 // Do not use generic CodeAction.Create to create CodeAction
            }

            private static async Task<Document> BatchPragmaFixesAsync(
                AbstractSuppressionCodeFixProvider suppressionFixProvider,
                Document document,
                ImmutableArray<IPragmaBasedCodeAction> pragmaActions,
                ImmutableArray<Diagnostic> diagnostics,
                CancellationToken cancellationToken)
            {
                // We apply all the pragma suppression fixes sequentially.
                // At every application, we track the updated locations for remaining diagnostics in the document.
                var currentDiagnosticSpans = new Dictionary<Diagnostic, TextSpan>();
                foreach (var diagnostic in diagnostics)
                {
                    currentDiagnosticSpans.Add(diagnostic, diagnostic.Location.SourceSpan);
                }

                var currentDocument = document;
                for (int i = 0; i < pragmaActions.Length; i++)
                {
                    var originalpragmaAction = pragmaActions[i];
                    var diagnostic = diagnostics[i];

                    // Get the diagnostic span for the diagnostic in latest document snapshot.
                    TextSpan currentDiagnosticSpan;
                    if (!currentDiagnosticSpans.TryGetValue(diagnostic, out currentDiagnosticSpan))
                    {
                        // Diagnostic whose location conflicts with a prior fix.
                        continue;
                    }

                    // Compute and apply pragma suppression fix.
                    var currentTree = await currentDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                    var currentLocation = Location.Create(currentTree, currentDiagnosticSpan);
                    diagnostic = Diagnostic.Create(
                        id: diagnostic.Id,
                        category: diagnostic.Descriptor.Category,
                        message: diagnostic.GetMessage(),
                        severity: diagnostic.Severity,
                        defaultSeverity: diagnostic.DefaultSeverity,
                        isEnabledByDefault: diagnostic.Descriptor.IsEnabledByDefault,
                        warningLevel: diagnostic.WarningLevel,
                        title: diagnostic.Descriptor.Title,
                        description: diagnostic.Descriptor.Description,
                        helpLink: diagnostic.Descriptor.HelpLinkUri,
                        location: currentLocation,
                        additionalLocations: diagnostic.AdditionalLocations,
                        customTags: diagnostic.Descriptor.CustomTags,
                        properties: diagnostic.Properties,
                        isSuppressed: diagnostic.IsSuppressed);

                    var newSuppressionFixes = await suppressionFixProvider.GetSuppressionsAsync(currentDocument, currentDiagnosticSpan, SpecializedCollections.SingletonEnumerable(diagnostic), cancellationToken).ConfigureAwait(false);
                    var newSuppressionFix = newSuppressionFixes.SingleOrDefault();
                    if (newSuppressionFix != null)
                    {
                        var newPragmaAction = newSuppressionFix.Action as IPragmaBasedCodeAction ??
                            newSuppressionFix.Action.GetCodeActions().OfType<IPragmaBasedCodeAction>().SingleOrDefault();
                        if (newPragmaAction != null)
                        {
                            // Get the changed document with pragma suppression add/removals.
                            // Note: We do it one token at a time to ensure we get single text change in the new document, otherwise UpdateDiagnosticSpans won't function as expected.
                            // Update the diagnostics spans based on the text changes.
                            var startTokenChanges = await GetChangedDocumentAsync(newPragmaAction, currentDocument, diagnostics, currentDiagnosticSpans,
                                includeStartTokenChange: true, includeEndTokenChange: false, cancellationToken: cancellationToken).ConfigureAwait(false);

                            var endTokenChanges = await GetChangedDocumentAsync(newPragmaAction, currentDocument, diagnostics, currentDiagnosticSpans,
                                includeStartTokenChange: false, includeEndTokenChange: true, cancellationToken: cancellationToken).ConfigureAwait(false);

                            var currentText = await currentDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                            var orderedChanges = startTokenChanges.Concat(endTokenChanges).OrderBy(change => change.Span).Distinct();
                            var newText = currentText.WithChanges(orderedChanges);
                            currentDocument = currentDocument.WithText(newText);
                        }
                    }
                }

                return currentDocument;
            }

            private static async Task<IEnumerable<TextChange>> GetChangedDocumentAsync(
                IPragmaBasedCodeAction pragmaAction,
                Document currentDocument,
                ImmutableArray<Diagnostic> diagnostics,
                Dictionary<Diagnostic, TextSpan> currentDiagnosticSpans,
                bool includeStartTokenChange,
                bool includeEndTokenChange,
                CancellationToken cancellationToken)
            {
                var newDocument = await pragmaAction.GetChangedDocumentAsync(includeStartTokenChange, includeEndTokenChange, cancellationToken).ConfigureAwait(false);
                
                // Update the diagnostics spans based on the text changes.
                var textChanges = await newDocument.GetTextChangesAsync(currentDocument, cancellationToken).ConfigureAwait(false);
                foreach (var textChange in textChanges)
                {
                    UpdateDiagnosticSpans(diagnostics, currentDiagnosticSpans, textChange);
                }

                return textChanges;
            }

            private static async Task UpdateDiagnosticSpansAsync(Document currentDocument, Document newDocument, ImmutableArray<Diagnostic> diagnostics, Dictionary<Diagnostic, TextSpan> currentDiagnosticSpans, CancellationToken cancellationToken)
            {
                // Update the diagnostics spans based on the text changes.
                var textChanges = await newDocument.GetTextChangesAsync(currentDocument, cancellationToken).ConfigureAwait(false);
                foreach (var textChange in textChanges)
                {
                    UpdateDiagnosticSpans(diagnostics, currentDiagnosticSpans, textChange);
                }
            }

            private static void UpdateDiagnosticSpans(ImmutableArray<Diagnostic> diagnostics, Dictionary<Diagnostic, TextSpan> currentDiagnosticSpans, TextChange textChange)
            {
                var isAdd = textChange.Span.Length == 0;
                Func<TextSpan, bool> isPriorSpan = span => span.End <= textChange.Span.Start;
                Func<TextSpan, bool> isFollowingSpan = span => span.Start >= textChange.Span.End;
                Func<TextSpan, bool> isEnclosingSpan = span => span.Contains(textChange.Span);

                foreach (var diagnostic in diagnostics)
                {
                    TextSpan currentSpan;
                    if (!currentDiagnosticSpans.TryGetValue(diagnostic, out currentSpan))
                    {
                        continue;
                    }

                    if (isPriorSpan(currentSpan))
                    {
                        // Prior span, needs no update.
                        continue;
                    }

                    var delta = textChange.NewText.Length - textChange.Span.Length;
                    if (delta != 0)
                    {
                        if (isFollowingSpan(currentSpan))
                        {
                            // Following span.
                            var newStart = currentSpan.Start + delta;
                            var newSpan = new TextSpan(newStart, currentSpan.Length);
                            currentDiagnosticSpans[diagnostic] = newSpan;
                        }
                        else if (isEnclosingSpan(currentSpan))
                        {
                            // Enclosing span.
                            var newLength = currentSpan.Length + delta;
                            var newSpan = new TextSpan(currentSpan.Start, newLength);
                            currentDiagnosticSpans[diagnostic] = newSpan;
                        }
                        else
                        {
                            // Overlapping span.
                            // Drop conflicting diagnostics.
                            currentDiagnosticSpans.Remove(diagnostic);
                        }
                    }
                }
            }
        }
    }
}
