// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = nameof(AnalyzerReleaseTrackingFix))]
    [Shared]
    public sealed partial class AnalyzerReleaseTrackingFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticIds.DeclareDiagnosticIdInAnalyzerReleaseRuleId, DiagnosticIds.UpdateDiagnosticIdInAnalyzerReleaseRuleId);

        public override FixAllProvider GetFixAllProvider()
            => new ReleaseTrackingFixAllProvider();

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                CodeAction? codeAction = null;
                switch (diagnostic.Id)
                {
                    case DiagnosticIds.DeclareDiagnosticIdInAnalyzerReleaseRuleId:
                        if (IsAddEntryToUnshippedFileDiagnostic(diagnostic, out var entryToAdd))
                        {
                            codeAction = CodeAction.Create(
                                CodeAnalysisDiagnosticsResources.AddEntryForDiagnosticIdInAnalyzerReleaseCodeFixTitle,
                                cancellationToken => AddEntryToUnshippedFileAsync(context.Document.Project, entryToAdd, cancellationToken),
                                equivalenceKey: CodeAnalysisDiagnosticsResources.AddEntryForDiagnosticIdInAnalyzerReleaseCodeFixTitle);
                        }

                        break;

                    case DiagnosticIds.UpdateDiagnosticIdInAnalyzerReleaseRuleId:
                        if (IsAddEntryToUnshippedFileDiagnostic(diagnostic, out entryToAdd))
                        {
                            codeAction = CodeAction.Create(
                                CodeAnalysisDiagnosticsResources.AddEntryForDiagnosticIdInAnalyzerReleaseCodeFixTitle,
                                cancellationToken => AddEntryToUnshippedFileAsync(context.Document.Project, entryToAdd, cancellationToken),
                                equivalenceKey: CodeAnalysisDiagnosticsResources.AddEntryForDiagnosticIdInAnalyzerReleaseCodeFixTitle);
                        }
                        else if (IsUpdateEntryToUnshippedFileDiagnostic(diagnostic, out var ruleId, out var entryToUpdate))
                        {
                            codeAction = CodeAction.Create(
                                CodeAnalysisDiagnosticsResources.UpdateEntryForDiagnosticIdInAnalyzerReleaseCodeFixTitle,
                                cancellationToken => UpdateEntryInUnshippedFileAsync(context.Document.Project, ruleId, entryToUpdate, cancellationToken),
                                equivalenceKey: CodeAnalysisDiagnosticsResources.UpdateEntryForDiagnosticIdInAnalyzerReleaseCodeFixTitle);
                        }
                        break;

                    default:
                        Debug.Fail($"Unsupported diagnostic ID {diagnostic.Id}");
                        continue;
                }

                if (codeAction != null)
                {
                    context.RegisterCodeFix(codeAction, diagnostic);
                }
            }

            return Task.CompletedTask;
        }

        private static bool IsAddEntryToUnshippedFileDiagnostic(Diagnostic diagnostic, [NotNullWhen(returnValue: true)] out string? entryToAdd)
        {
            if (diagnostic.Properties != null &&
                diagnostic.Properties.TryGetValue(DiagnosticDescriptorCreationAnalyzer.EntryToAddPropertyName, out entryToAdd) &&
                !string.IsNullOrEmpty(entryToAdd))
            {
                return true;
            }

            entryToAdd = null;
            return false;
        }

        private static bool IsUpdateEntryToUnshippedFileDiagnostic(
            Diagnostic diagnostic,
            [NotNullWhen(returnValue: true)] out string? ruleId,
            [NotNullWhen(returnValue: true)] out string? entryToUpdate)
        {
            if (diagnostic.Properties != null &&
                diagnostic.Properties.TryGetValue(DiagnosticDescriptorCreationAnalyzer.EntryToUpdatePropertyName, out entryToUpdate) &&
                !string.IsNullOrEmpty(entryToUpdate) &&
                TryGetRuleIdForEntry(entryToUpdate, out ruleId))
            {
                return true;
            }

            ruleId = null;
            entryToUpdate = null;
            return false;
        }

        private static bool TryGetRuleIdForEntry(string entry, [NotNullWhen(returnValue: true)] out string? ruleId)
        {
            var index = entry.IndexOf("|", StringComparison.Ordinal);
            if (index > 0)
            {
                ruleId = entry.Substring(0, index).Trim();
                if (ruleId.StartsWith(DiagnosticDescriptorCreationAnalyzer.RemovedPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    ruleId = ruleId.Substring(DiagnosticDescriptorCreationAnalyzer.RemovedPrefix.Length);
                }

                return true;
            }

            ruleId = null;
            return false;
        }

        private static async Task<Solution> AddEntryToUnshippedFileAsync(Project project, string entryToAdd, CancellationToken cancellationToken)
        {
            var unshippedDataDocument = project.AdditionalDocuments.FirstOrDefault(d => d.Name == DiagnosticDescriptorCreationAnalyzer.UnshippedFileName);
            if (unshippedDataDocument == null)
            {
                return project.Solution;
            }

            var newText = await AddEntriesToUnshippedFileAsync(unshippedDataDocument, new SortedSet<string>() { entryToAdd }, cancellationToken).ConfigureAwait(false);
            return project.Solution.WithAdditionalDocumentText(unshippedDataDocument.Id, newText);
        }

        private static Task<SourceText> AddEntriesToUnshippedFileAsync(
            TextDocument unshippedDataDocument,
            SortedSet<string> entriesToAdd,
            CancellationToken cancellationToken)
            => AddOrUpdateEntriesToUnshippedFileAsync(unshippedDataDocument, entriesToAdd, entriesToUpdate: null, cancellationToken);

        private static async Task<Solution> UpdateEntryInUnshippedFileAsync(Project project, string ruleId, string entryToUpdate, CancellationToken cancellationToken)
        {
            var unshippedDataDocument = project.AdditionalDocuments.FirstOrDefault(d => d.Name == DiagnosticDescriptorCreationAnalyzer.UnshippedFileName);
            if (unshippedDataDocument == null)
            {
                return project.Solution;
            }

            var newText = await UpdateEntriesInUnshippedFileAsync(unshippedDataDocument, new Dictionary<string, string>() { { ruleId, entryToUpdate } }, cancellationToken).ConfigureAwait(false);
            return project.Solution.WithAdditionalDocumentText(unshippedDataDocument.Id, newText);
        }

        private static Task<SourceText> UpdateEntriesInUnshippedFileAsync(
            TextDocument unshippedDataDocument,
            Dictionary<string, string> entriesToUpdate,
            CancellationToken cancellationToken)
            => AddOrUpdateEntriesToUnshippedFileAsync(unshippedDataDocument, entriesToAdd: null, entriesToUpdate, cancellationToken);

        private static async Task<SourceText> AddOrUpdateEntriesToUnshippedFileAsync(
            TextDocument unshippedDataDocument,
            SortedSet<string>? entriesToAdd,
            Dictionary<string, string>? entriesToUpdate,
            CancellationToken cancellationToken)
        {
            var unshippedText = await unshippedDataDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var builder = new StringBuilder();
            var needsHeader = true;
            var first = true;

            foreach (TextLine line in unshippedText.Lines)
            {
                if (!first)
                {
                    builder.AppendLine();
                }
                else
                {
                    first = false;
                }

                string originalLineText = line.ToString();
                var lineText = originalLineText.Trim();
                if (string.IsNullOrWhiteSpace(lineText) || lineText.StartsWith(";", StringComparison.Ordinal))
                {
                    // Skip blank and comment lines.
                    builder.Append(originalLineText);
                    continue;
                }

                if (needsHeader)
                {
                    // Add the the header, if not present
                    if (lineText.StartsWith(DiagnosticDescriptorCreationAnalyzer.ReleaseHeaderLine1, StringComparison.OrdinalIgnoreCase))
                    {
                        builder.Append(originalLineText);
                        continue;
                    }
                    else if (lineText.StartsWith(DiagnosticDescriptorCreationAnalyzer.ReleaseHeaderLine2, StringComparison.OrdinalIgnoreCase))
                    {
                        builder.Append(originalLineText);
                        needsHeader = false;
                    }
                    else
                    {
                        builder.AppendLine(DiagnosticDescriptorCreationAnalyzer.ReleaseHeaderLine1);
                        builder.Append(DiagnosticDescriptorCreationAnalyzer.ReleaseHeaderLine2);
                        needsHeader = false;
                    }

                    continue;
                }
                else if (lineText.StartsWith(DiagnosticDescriptorCreationAnalyzer.ReleaseHeaderLine1, StringComparison.OrdinalIgnoreCase) ||
                    lineText.StartsWith(DiagnosticDescriptorCreationAnalyzer.ReleaseHeaderLine2, StringComparison.OrdinalIgnoreCase))
                {
                    // Skip misplaced header lines
                    continue;
                }

                while (entriesToAdd?.Count > 0 &&
                    string.Compare(entriesToAdd.First(), lineText, StringComparison.OrdinalIgnoreCase) <= 0)
                {
                    builder.AppendLine(entriesToAdd.First());
                    entriesToAdd.Remove(entriesToAdd.First());
                }

                if (entriesToUpdate?.Count > 0 &&
                    TryGetRuleIdForEntry(lineText, out var ruleIdForLine) &&
                    entriesToUpdate.TryGetValue(ruleIdForLine, out var entryToUpdate))
                {
                    builder.Append(entryToUpdate);
                    entriesToUpdate.Remove(ruleIdForLine);
                    continue;
                }

                builder.Append(originalLineText);
            }

            if (needsHeader)
            {
                builder.AppendLine(DiagnosticDescriptorCreationAnalyzer.ReleaseHeaderLine1);
                builder.Append(DiagnosticDescriptorCreationAnalyzer.ReleaseHeaderLine2);
            }

            if (entriesToAdd != null)
            {
                foreach (var entryToAdd in entriesToAdd)
                {
                    builder.AppendLine();
                    builder.Append(entryToAdd);
                }
            }

            return unshippedText.Replace(new TextSpan(0, unshippedText.Length), builder.ToString());
        }
    }
}
