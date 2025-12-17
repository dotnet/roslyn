// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.ReleaseTracking;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = nameof(AnalyzerReleaseTrackingFix))]
    [Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    public sealed partial class AnalyzerReleaseTrackingFix() : CodeFixProvider
    {
        private const string EntryFieldSeparator = "|";
        private static readonly string[] s_entryFieldSeparators = [EntryFieldSeparator];

        internal const string ShippedAnalyzerReleaseTrackingFileDefaultContent = @"; Shipped analyzer releases
" + CommonAnalyzerReleaseTrackingContent;
        internal const string UnshippedAnalyzerReleaseTrackingFileDefaultContent = @"; Unshipped analyzer release
" + CommonAnalyzerReleaseTrackingContent;
        private const string CommonAnalyzerReleaseTrackingContent = @"; https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

";

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(DiagnosticIds.DeclareDiagnosticIdInAnalyzerReleaseRuleId, DiagnosticIds.UpdateDiagnosticIdInAnalyzerReleaseRuleId, DiagnosticIds.EnableAnalyzerReleaseTrackingRuleId);

        public override FixAllProvider GetFixAllProvider()
            => new ReleaseTrackingFixAllProvider();

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
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

                    case DiagnosticIds.EnableAnalyzerReleaseTrackingRuleId:
                        codeAction = CodeAction.Create(
                            CodeAnalysisDiagnosticsResources.EnableAnalyzerReleaseTrackingRuleTitle,
                            cancellationToken => AddAnalyzerReleaseTrackingFilesAsync(context.Document.Project),
                            equivalenceKey: CodeAnalysisDiagnosticsResources.EnableAnalyzerReleaseTrackingRuleTitle);
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
        }

        private static async Task<Solution> AddAnalyzerReleaseTrackingFilesAsync(Project project)
        {
            project = AddAdditionalDocument(project, ReleaseTrackingHelper.ShippedFileName, ShippedAnalyzerReleaseTrackingFileDefaultContent);
            project = AddAdditionalDocument(project, ReleaseTrackingHelper.UnshippedFileName, UnshippedAnalyzerReleaseTrackingFileDefaultContent);
            return project.Solution;

            // Local functions.
            static Project AddAdditionalDocument(Project project, string name, string fileContent)
            {
                TextDocument? additionalDocument = project.AdditionalDocuments.FirstOrDefault(doc => string.Equals(doc.Name, name, StringComparison.OrdinalIgnoreCase));
                if (additionalDocument == null)
                {
                    project = project.AddAdditionalDocument(name, fileContent).Project;
                }

                return project;
            }
        }

        private static bool IsAddEntryToUnshippedFileDiagnostic(Diagnostic diagnostic, [NotNullWhen(returnValue: true)] out string? entryToAdd)
        {
            if (diagnostic.Properties != null &&
                diagnostic.Properties.TryGetValue(DiagnosticDescriptorCreationAnalyzer.EntryToAddPropertyName, out entryToAdd) &&
                !RoslynString.IsNullOrEmpty(entryToAdd))
            {
                RoslynDebug.Assert(entryToAdd != null);
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
                !RoslynString.IsNullOrEmpty(entryToUpdate) &&
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
            var index = entry.IndexOf(EntryFieldSeparator, StringComparison.Ordinal);
            if (index > 0)
            {
                ruleId = entry[..index].Trim();
                return true;
            }

            ruleId = null;
            return false;
        }

        private static async Task<Solution> AddEntryToUnshippedFileAsync(Project project, string entryToAdd, CancellationToken cancellationToken)
        {
            var unshippedDataDocument = project.AdditionalDocuments.FirstOrDefault(d => d.Name == ReleaseTrackingHelper.UnshippedFileName);
            if (unshippedDataDocument == null)
            {
                return project.Solution;
            }

            var newText = await AddEntriesToUnshippedFileAsync(unshippedDataDocument, [entryToAdd], cancellationToken).ConfigureAwait(false);
            return project.Solution.WithAdditionalDocumentText(unshippedDataDocument.Id, newText);
        }

        private static Task<SourceText> AddEntriesToUnshippedFileAsync(
            TextDocument unshippedDataDocument,
            SortedSet<string> entriesToAdd,
            CancellationToken cancellationToken)
            => AddOrUpdateEntriesToUnshippedFileAsync(unshippedDataDocument, entriesToAdd, entriesToUpdate: null, cancellationToken);

        private static async Task<Solution> UpdateEntryInUnshippedFileAsync(Project project, string ruleId, string entryToUpdate, CancellationToken cancellationToken)
        {
            var unshippedDataDocument = project.AdditionalDocuments.FirstOrDefault(d => d.Name == ReleaseTrackingHelper.UnshippedFileName);
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

        private static Task<SourceText> AddOrUpdateEntriesToUnshippedFileAsync(
            TextDocument unshippedDataDocument,
            SortedSet<string>? entriesToAdd,
            Dictionary<string, string>? entriesToUpdate,
            CancellationToken cancellationToken)
        {
            // Split entries to add into New rule entries and Changed rule entries as they should be added to separate tables.
            //  New rule entry:
            //      "Rule ID | Category | Severity | HelpLink (optional)"
            //      "   0    |     1    |    2     |        3           "
            //
            //  Changed rule entry:
            //      "Rule ID | New Category | New Severity | Old Category | Old Severity | HelpLink (optional)"
            //      "   0    |     1        |     2        |     3        |     4        |        5           "

            SortedSet<string>? newRuleEntriesToAdd = null;
            SortedSet<string>? changedRuleEntriesToAdd = null;
            if (entriesToAdd != null)
            {
                foreach (var entry in entriesToAdd)
                {
                    switch (entry.Split(s_entryFieldSeparators, StringSplitOptions.None).Length)
                    {
                        case 3:
                        case 4:
                            newRuleEntriesToAdd ??= [];
                            newRuleEntriesToAdd.Add(entry);
                            break;

                        case 5:
                        case 6:
                            changedRuleEntriesToAdd ??= [];
                            changedRuleEntriesToAdd.Add(entry);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }
            }

            return AddOrUpdateEntriesToUnshippedFileAsync(unshippedDataDocument, newRuleEntriesToAdd, changedRuleEntriesToAdd, entriesToUpdate, cancellationToken);
        }

        private static async Task<SourceText> AddOrUpdateEntriesToUnshippedFileAsync(
            TextDocument unshippedDataDocument,
            SortedSet<string>? newRuleEntriesToAdd,
            SortedSet<string>? changedRuleEntriesToAdd,
            Dictionary<string, string>? entriesToUpdate,
            CancellationToken cancellationToken)
        {
            var unshippedText = await unshippedDataDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var builder = new StringBuilder();
            RuleEntryTableKind? currentTableKind = null;
            var parsingHeaderLines = false;
            var first = true;
            bool sawNewLine = false;

            foreach (TextLine line in unshippedText.Lines)
            {
                sawNewLine = false;
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
                if (string.IsNullOrWhiteSpace(lineText))
                {
                    // Check if we were parsing New or Changed rules entries.
                    // If so, append all the new/changed rule entries to appropriate table.
                    if (!parsingHeaderLines)
                    {
                        if (currentTableKind == RuleEntryTableKind.New)
                        {
                            if (AddAllEntries(newRuleEntriesToAdd, builder, prependNewLine: false))
                            {
                                builder.AppendLine();
                            }
                        }
                        else if (currentTableKind == RuleEntryTableKind.Changed
                            && AddAllEntries(changedRuleEntriesToAdd, builder, prependNewLine: false))
                        {
                            builder.AppendLine();
                        }
                    }

                    // Append the blank line.
                    builder.Append(originalLineText);
                    sawNewLine = true;
                    continue;
                }

                if (lineText.StartsWith(";", StringComparison.Ordinal))
                {
                    builder.Append(originalLineText);
                    continue;
                }

                if (lineText.StartsWith("###", StringComparison.Ordinal))
                {
                    if (lineText.StartsWith(ReleaseTrackingHelper.TableTitleNewRules, StringComparison.OrdinalIgnoreCase))
                    {
                        currentTableKind = RuleEntryTableKind.New;
                    }
                    else
                    {
                        // Ensure that new rules table is always above the removed and changed rules.
                        if (newRuleEntriesToAdd?.Count > 0)
                        {
                            AddNewRulesTableHeader(builder, prependNewLine: false);
                            AddAllEntries(newRuleEntriesToAdd, builder, prependNewLine: true);
                            builder.AppendLine();
                            builder.AppendLine();
                        }

                        if (lineText.StartsWith(ReleaseTrackingHelper.TableTitleRemovedRules, StringComparison.OrdinalIgnoreCase))
                        {
                            currentTableKind = RuleEntryTableKind.Removed;
                        }
                        else if (lineText.StartsWith(ReleaseTrackingHelper.TableTitleChangedRules, StringComparison.OrdinalIgnoreCase))
                        {
                            currentTableKind = RuleEntryTableKind.Changed;
                        }
                        else
                        {
                            Debug.Fail($"Unexpected line {lineText}");
                            currentTableKind = null;
                        }
                    }

                    builder.Append(originalLineText);
                    parsingHeaderLines = true;
                    continue;
                }

                if (parsingHeaderLines)
                {
                    builder.Append(originalLineText);

                    if (Regex.IsMatch(lineText, ReleaseTrackingHelper.TableHeaderNewOrRemovedRulesLine1RegexPattern, RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(lineText, ReleaseTrackingHelper.TableHeaderChangedRulesLine1RegexPattern, RegexOptions.IgnoreCase))
                    {
                        continue;
                    }
                    else if (Regex.IsMatch(lineText, ReleaseTrackingHelper.TableHeaderNewOrRemovedRulesLine2RegexPattern, RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(lineText, ReleaseTrackingHelper.TableHeaderChangedRulesLine2RegexPattern, RegexOptions.IgnoreCase))
                    {
                        parsingHeaderLines = false;
                    }
                    else
                    {
                        Debug.Fail($"Unexpected line {lineText}");
                    }

                    continue;
                }

                RoslynDebug.Assert(currentTableKind.HasValue);
                if (currentTableKind.Value == RuleEntryTableKind.Removed)
                {
                    // Retain the entries in Removed rules table without changes.
                    builder.Append(lineText);
                    continue;
                }

                var entriesToAdd = currentTableKind.Value == RuleEntryTableKind.New ? newRuleEntriesToAdd : changedRuleEntriesToAdd;
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

            if (newRuleEntriesToAdd?.Count > 0 || changedRuleEntriesToAdd?.Count > 0)
            {
                switch (currentTableKind)
                {
                    case RuleEntryTableKind.New:
                        AddAllEntries(newRuleEntriesToAdd, builder, prependNewLine: !sawNewLine);
                        if (changedRuleEntriesToAdd?.Count > 0)
                        {
                            AddChangedRulesTableHeader(builder, prependNewLine: true);
                            AddAllEntries(changedRuleEntriesToAdd, builder, prependNewLine: true);
                        }

                        break;

                    case RuleEntryTableKind.Changed:
                        AddAllEntries(changedRuleEntriesToAdd, builder, prependNewLine: !sawNewLine);
                        Debug.Assert(newRuleEntriesToAdd == null || newRuleEntriesToAdd.Count == 0);
                        break;

                    default:
                        var hasNewRuleEntries = newRuleEntriesToAdd?.Count > 0;
                        if (hasNewRuleEntries)
                        {
                            AddNewRulesTableHeader(builder, prependNewLine: false);
                            AddAllEntries(newRuleEntriesToAdd, builder, prependNewLine: true);
                        }

                        if (changedRuleEntriesToAdd?.Count > 0)
                        {
                            AddChangedRulesTableHeader(builder, prependNewLine: hasNewRuleEntries);
                            AddAllEntries(changedRuleEntriesToAdd, builder, prependNewLine: true);
                        }

                        break;
                }
            }

            return unshippedText.Replace(new TextSpan(0, unshippedText.Length), builder.ToString());

            static void AddNewRulesTableHeader(StringBuilder builder, bool prependNewLine)
            {
                if (prependNewLine)
                {
                    builder.AppendLine();
                    builder.AppendLine();
                }

                builder.AppendLine(ReleaseTrackingHelper.TableTitleNewRules);
                builder.AppendLine();
                builder.AppendLine(ReleaseTrackingHelper.TableHeaderNewOrRemovedRulesLine1);
                builder.Append(ReleaseTrackingHelper.TableHeaderNewOrRemovedRulesLine2);
            }

            static void AddChangedRulesTableHeader(StringBuilder builder, bool prependNewLine)
            {
                if (prependNewLine)
                {
                    builder.AppendLine();
                    builder.AppendLine();
                }

                builder.AppendLine(ReleaseTrackingHelper.TableTitleChangedRules);
                builder.AppendLine();
                builder.AppendLine(ReleaseTrackingHelper.TableHeaderChangedRulesLine1);
                builder.Append(ReleaseTrackingHelper.TableHeaderChangedRulesLine2);
            }

            static bool AddAllEntries(SortedSet<string>? entriesToAdd, StringBuilder builder, bool prependNewLine)
            {
                if (entriesToAdd?.Count > 0)
                {
                    var first = true;
                    foreach (var entryToAdd in entriesToAdd)
                    {
                        if (!first || prependNewLine)
                        {
                            builder.AppendLine();
                        }

                        builder.Append(entryToAdd);
                        first = false;
                    }

                    entriesToAdd.Clear();
                    return true;
                }

                return false;
            }
        }

        private enum RuleEntryTableKind
        {
            New,
            Removed,
            Changed,
        }
    }
}
