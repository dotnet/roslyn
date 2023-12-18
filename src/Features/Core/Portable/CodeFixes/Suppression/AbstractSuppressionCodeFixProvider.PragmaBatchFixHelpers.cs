// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
                FixAllState fixAllState,
                CancellationToken cancellationToken)
            {
                return CodeAction.Create(
                    ((CodeAction)pragmaActions[0]).Title,
                    createChangedDocument: ct =>
                        BatchPragmaFixesAsync(suppressionFixProvider, document, pragmaActions, pragmaDiagnostics, fixAllState.CodeActionOptionsProvider, cancellationToken),
                    equivalenceKey: fixAllState.CodeActionEquivalenceKey);
            }

            private static async Task<Document> BatchPragmaFixesAsync(
                AbstractSuppressionCodeFixProvider suppressionFixProvider,
                Document document,
                ImmutableArray<IPragmaBasedCodeAction> pragmaActions,
                ImmutableArray<Diagnostic> diagnostics,
                CodeActionOptionsProvider fallbackOptions,
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
                for (var i = 0; i < pragmaActions.Length; i++)
                {
                    var originalpragmaAction = pragmaActions[i];
                    var diagnostic = diagnostics[i];
                    // Get the diagnostic span for the diagnostic in latest document snapshot.
                    if (!currentDiagnosticSpans.TryGetValue(diagnostic, out var currentDiagnosticSpan))
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

                    var newSuppressionFixes = await suppressionFixProvider.GetFixesAsync(currentDocument, currentDiagnosticSpan, SpecializedCollections.SingletonEnumerable(diagnostic), fallbackOptions, cancellationToken).ConfigureAwait(false);
                    var newSuppressionFix = newSuppressionFixes.SingleOrDefault();
                    if (newSuppressionFix != null)
                    {
                        var newPragmaAction = newSuppressionFix.Action as IPragmaBasedCodeAction ??
                            newSuppressionFix.Action.NestedCodeActions.OfType<IPragmaBasedCodeAction>().SingleOrDefault();
                        if (newPragmaAction != null)
                        {
                            // Get the text changes with pragma suppression add/removals.
                            // Note: We do it one token at a time to ensure we get single text change in the new document, otherwise UpdateDiagnosticSpans won't function as expected.
                            // Update the diagnostics spans based on the text changes.
                            var startTokenChanges = await GetTextChangesAsync(newPragmaAction, currentDocument,
                                includeStartTokenChange: true, includeEndTokenChange: false, cancellationToken: cancellationToken).ConfigureAwait(false);

                            var endTokenChanges = await GetTextChangesAsync(newPragmaAction, currentDocument,
                                includeStartTokenChange: false, includeEndTokenChange: true, cancellationToken: cancellationToken).ConfigureAwait(false);

                            var currentText = await currentDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                            var orderedChanges = startTokenChanges.Concat(endTokenChanges).OrderBy(change => change.Span).Distinct();
                            var newText = currentText.WithChanges(orderedChanges);
                            currentDocument = currentDocument.WithText(newText);

                            // Update the diagnostics spans based on the text changes.
                            UpdateDiagnosticSpans(diagnostics, currentDiagnosticSpans, orderedChanges);
                        }
                    }
                }

                return currentDocument;
            }

            private static async Task<IEnumerable<TextChange>> GetTextChangesAsync(
                IPragmaBasedCodeAction pragmaAction,
                Document currentDocument,
                bool includeStartTokenChange,
                bool includeEndTokenChange,
                CancellationToken cancellationToken)
            {
                var newDocument = await pragmaAction.GetChangedDocumentAsync(includeStartTokenChange, includeEndTokenChange, cancellationToken).ConfigureAwait(false);
                return await newDocument.GetTextChangesAsync(currentDocument, cancellationToken).ConfigureAwait(false);
            }

            private static void UpdateDiagnosticSpans(ImmutableArray<Diagnostic> diagnostics, Dictionary<Diagnostic, TextSpan> currentDiagnosticSpans, IEnumerable<TextChange> textChanges)
            {
                static bool IsPriorSpan(TextSpan span, TextChange textChange) => span.End <= textChange.Span.Start;
                static bool IsFollowingSpan(TextSpan span, TextChange textChange) => span.Start >= textChange.Span.End;
                static bool IsEnclosingSpan(TextSpan span, TextChange textChange) => span.Contains(textChange.Span);

                foreach (var diagnostic in diagnostics)
                {
                    // We use 'originalSpan' to identify if the diagnostic is prior/following/enclosing with respect to each text change.
                    // We use 'currentSpan' to track updates made to the originalSpan by each text change.
                    if (!currentDiagnosticSpans.TryGetValue(diagnostic, out var originalSpan))
                    {
                        continue;
                    }

                    var currentSpan = originalSpan;
                    foreach (var textChange in textChanges)
                    {
                        if (IsPriorSpan(originalSpan, textChange))
                        {
                            // Prior span, needs no update.
                            continue;
                        }

                        var delta = textChange.NewText.Length - textChange.Span.Length;
                        if (delta != 0)
                        {
                            if (IsFollowingSpan(originalSpan, textChange))
                            {
                                // Following span.
                                var newStart = currentSpan.Start + delta;
                                currentSpan = new TextSpan(newStart, currentSpan.Length);
                                currentDiagnosticSpans[diagnostic] = currentSpan;
                            }
                            else if (IsEnclosingSpan(originalSpan, textChange))
                            {
                                // Enclosing span.
                                var newLength = currentSpan.Length + delta;
                                currentSpan = new TextSpan(currentSpan.Start, newLength);
                                currentDiagnosticSpans[diagnostic] = currentSpan;
                            }
                            else
                            {
                                // Overlapping span.
                                // Drop conflicting diagnostics.
                                currentDiagnosticSpans.Remove(diagnostic);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
