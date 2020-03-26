// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    public sealed partial class DiagnosticDescriptorCreationAnalyzer
    {
        internal const string ShippedFileName = "AnalyzerReleases.Shipped.md";
        internal const string UnshippedFileName = "AnalyzerReleases.Unshipped.md";
        internal const string RemovedPrefix = "*REMOVED*";
        internal const string ReleasePrefix = "## Release";
        internal const string TableTitleNewRules = "### New Rules";
        internal const string TableTitleRemovedRules = "### Removed Rules";
        internal const string TableTitleChangedRules = "### Changed Rules";
        internal const string TableHeaderNewOrRemovedRulesLine1 = @"Rule ID | Category | Severity | HelpLink (optional)";
        internal const string TableHeaderNewOrRemovedRulesLine2 = @"--------|----------|----------|--------------------";
        internal const string TableHeaderChangedRulesLine1 = @"Rule ID | New Category | New Severity | Old Category | Old Severity | HelpLink (optional)";
        internal const string TableHeaderChangedRulesLine2 = @"--------|--------------|--------------|--------------|--------------|--------------------";
        private const string DisabledText = "Disabled";
        internal const string UndetectedText = @"<Undetected>";

        // Property names which are keys for diagnostic property bag passed to the code fixer.
        internal const string EntryToAddPropertyName = nameof(EntryToAddPropertyName);
        internal const string EntryToUpdatePropertyName = nameof(EntryToUpdatePropertyName);

        private static readonly Version s_unshippedVersion = new Version(int.MaxValue, int.MaxValue);

        internal static readonly DiagnosticDescriptor DeclareDiagnosticIdInAnalyzerReleaseRule = new DiagnosticDescriptor(
            id: DiagnosticIds.DeclareDiagnosticIdInAnalyzerReleaseRuleId,
            title: CodeAnalysisDiagnosticsResources.DeclareDiagnosticIdInAnalyzerReleaseTitle,
            messageFormat: CodeAnalysisDiagnosticsResources.DeclareDiagnosticIdInAnalyzerReleaseMessage,
            category: DiagnosticCategory.MicrosoftCodeAnalysisReleaseTracking,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CodeAnalysisDiagnosticsResources.DeclareDiagnosticIdInAnalyzerReleaseDescription,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor UpdateDiagnosticIdInAnalyzerReleaseRule = new DiagnosticDescriptor(
            id: DiagnosticIds.UpdateDiagnosticIdInAnalyzerReleaseRuleId,
            title: CodeAnalysisDiagnosticsResources.UpdateDiagnosticIdInAnalyzerReleaseTitle,
            messageFormat: CodeAnalysisDiagnosticsResources.UpdateDiagnosticIdInAnalyzerReleaseMessage,
            category: DiagnosticCategory.MicrosoftCodeAnalysisReleaseTracking,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CodeAnalysisDiagnosticsResources.UpdateDiagnosticIdInAnalyzerReleaseDescription,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor RemoveUnshippedDeletedDiagnosticIdRule = new DiagnosticDescriptor(
            id: DiagnosticIds.RemoveUnshippedDeletedDiagnosticIdRuleId,
            title: CodeAnalysisDiagnosticsResources.RemoveUnshippedDeletedDiagnosticIdTitle,
            messageFormat: CodeAnalysisDiagnosticsResources.RemoveUnshippedDeletedDiagnosticIdMessage,
            category: DiagnosticCategory.MicrosoftCodeAnalysisReleaseTracking,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CodeAnalysisDiagnosticsResources.RemoveUnshippedDeletedDiagnosticIdDescription,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor RemoveShippedDeletedDiagnosticIdRule = new DiagnosticDescriptor(
            id: DiagnosticIds.RemoveShippedDeletedDiagnosticIdRuleId,
            title: CodeAnalysisDiagnosticsResources.RemoveShippedDeletedDiagnosticIdTitle,
            messageFormat: CodeAnalysisDiagnosticsResources.RemoveShippedDeletedDiagnosticIdMessage,
            category: DiagnosticCategory.MicrosoftCodeAnalysisReleaseTracking,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CodeAnalysisDiagnosticsResources.RemoveShippedDeletedDiagnosticIdDescription,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor UnexpectedAnalyzerDiagnosticForRemovedDiagnosticIdRule = new DiagnosticDescriptor(
            id: DiagnosticIds.UnexpectedAnalyzerDiagnosticForRemovedDiagnosticIdRuleId,
            title: CodeAnalysisDiagnosticsResources.UnexpectedAnalyzerDiagnosticForRemovedDiagnosticIdTitle,
            messageFormat: CodeAnalysisDiagnosticsResources.UnexpectedAnalyzerDiagnosticForRemovedDiagnosticIdMessage,
            category: DiagnosticCategory.MicrosoftCodeAnalysisReleaseTracking,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CodeAnalysisDiagnosticsResources.UnexpectedAnalyzerDiagnosticForRemovedDiagnosticIdDescription,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor RemoveDuplicateEntriesForAnalyzerReleaseRule = new DiagnosticDescriptor(
            id: DiagnosticIds.RemoveDuplicateEntriesForAnalyzerReleaseRuleId,
            title: CodeAnalysisDiagnosticsResources.RemoveDuplicateEntriesForAnalyzerReleaseRuleTitle,
            messageFormat: CodeAnalysisDiagnosticsResources.RemoveDuplicateEntriesForAnalyzerReleaseRuleMessage,
            category: DiagnosticCategory.MicrosoftCodeAnalysisReleaseTracking,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CodeAnalysisDiagnosticsResources.RemoveDuplicateEntriesForAnalyzerReleaseRuleDescription,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor RemoveDuplicateEntriesBetweenAnalyzerReleasesRule = new DiagnosticDescriptor(
            id: DiagnosticIds.RemoveDuplicateEntriesBetweenAnalyzerReleasesRuleId,
            title: CodeAnalysisDiagnosticsResources.RemoveDuplicateEntriesBetweenAnalyzerReleasesRuleTitle,
            messageFormat: CodeAnalysisDiagnosticsResources.RemoveDuplicateEntriesBetweenAnalyzerReleasesRuleMessage,
            category: DiagnosticCategory.MicrosoftCodeAnalysisReleaseTracking,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CodeAnalysisDiagnosticsResources.RemoveDuplicateEntriesBetweenAnalyzerReleasesRuleDescription,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor InvalidEntryInAnalyzerReleasesFileRule = new DiagnosticDescriptor(
            id: DiagnosticIds.InvalidEntryInAnalyzerReleasesFileRuleId,
            title: CodeAnalysisDiagnosticsResources.InvalidEntryInAnalyzerReleasesFileRuleTitle,
            messageFormat: CodeAnalysisDiagnosticsResources.InvalidEntryInAnalyzerReleasesFileRuleMessage,
            category: DiagnosticCategory.MicrosoftCodeAnalysisReleaseTracking,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CodeAnalysisDiagnosticsResources.InvalidEntryInAnalyzerReleasesFileRuleDescription,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor InvalidHeaderInAnalyzerReleasesFileRule = new DiagnosticDescriptor(
            id: DiagnosticIds.InvalidEntryInAnalyzerReleasesFileRuleId,
            title: CodeAnalysisDiagnosticsResources.InvalidEntryInAnalyzerReleasesFileRuleTitle,
            messageFormat: CodeAnalysisDiagnosticsResources.InvalidHeaderInAnalyzerReleasesFileRuleMessage,
            category: DiagnosticCategory.MicrosoftCodeAnalysisReleaseTracking,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CodeAnalysisDiagnosticsResources.InvalidEntryInAnalyzerReleasesFileRuleDescription,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor InvalidUndetectedEntryInAnalyzerReleasesFileRule = new DiagnosticDescriptor(
            id: DiagnosticIds.InvalidEntryInAnalyzerReleasesFileRuleId,
            title: CodeAnalysisDiagnosticsResources.InvalidEntryInAnalyzerReleasesFileRuleTitle,
            messageFormat: CodeAnalysisDiagnosticsResources.InvalidUndetectedEntryInAnalyzerReleasesFileRuleMessage,
            category: DiagnosticCategory.MicrosoftCodeAnalysisReleaseTracking,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CodeAnalysisDiagnosticsResources.InvalidEntryInAnalyzerReleasesFileRuleDescription,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor InvalidRemovedOrChangedWithoutPriorNewEntryInAnalyzerReleasesFileRule = new DiagnosticDescriptor(
            id: DiagnosticIds.InvalidEntryInAnalyzerReleasesFileRuleId,
            title: CodeAnalysisDiagnosticsResources.InvalidEntryInAnalyzerReleasesFileRuleTitle,
            messageFormat: CodeAnalysisDiagnosticsResources.InvalidRemovedOrChangedWithoutPriorNewEntryInAnalyzerReleasesFileRuleMessageMessage,
            category: DiagnosticCategory.MicrosoftCodeAnalysisReleaseTracking,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CodeAnalysisDiagnosticsResources.InvalidEntryInAnalyzerReleasesFileRuleDescription,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        private static bool TryGetReleaseTrackingData(
            ImmutableArray<AdditionalText> additionalTexts,
            CancellationToken cancellationToken,
            [NotNullWhen(returnValue: true)] out ReleaseTrackingData? shippedData,
            [NotNullWhen(returnValue: true)] out ReleaseTrackingData? unshippedData,
            out List<Diagnostic>? invalidFileDiagnostics)
        {
            if (!TryGetReleaseTrackingFiles(additionalTexts, cancellationToken, out var shippedText, out var unshippedText))
            {
                // TODO: Report a diagnostic that both must be specified if either shippedText or unshippedText is non-null.
                shippedData = default;
                unshippedData = default;
                invalidFileDiagnostics = null;
                return false;
            }

            invalidFileDiagnostics = new List<Diagnostic>();
            shippedData = ReadReleaseTrackingData(shippedText.Path, shippedText.GetText(cancellationToken), invalidFileDiagnostics.Add, isShippedFile: true);
            unshippedData = ReadReleaseTrackingData(unshippedText.Path, unshippedText.GetText(cancellationToken), invalidFileDiagnostics.Add, isShippedFile: false);

            return invalidFileDiagnostics.Count == 0;
        }

        private static bool TryGetReleaseTrackingFiles(
            ImmutableArray<AdditionalText> additionalTexts,
            CancellationToken cancellationToken,
            [NotNullWhen(returnValue: true)] out AdditionalText? shippedText,
            [NotNullWhen(returnValue: true)] out AdditionalText? unshippedText)
        {
            shippedText = null;
            unshippedText = null;

            StringComparer comparer = StringComparer.Ordinal;
            foreach (AdditionalText text in additionalTexts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string fileName = Path.GetFileName(text.Path);
                if (comparer.Equals(fileName, ShippedFileName))
                {
                    shippedText = text;
                    continue;
                }

                if (comparer.Equals(fileName, UnshippedFileName))
                {
                    unshippedText = text;
                    continue;
                }
            }

            return shippedText != null && unshippedText != null;
        }

        private static ReleaseTrackingData ReadReleaseTrackingData(
            string path,
            SourceText sourceText,
            Action<Diagnostic> addInvalidFileDiagnostic,
            bool isShippedFile)
        {
            var releaseTrackingDataByRulesBuilder = new Dictionary<string, ReleaseTrackingDataForRuleBuilder>();
            var currentVersion = s_unshippedVersion;
            using var reportedInvalidLines = PooledHashSet<TextLine>.GetInstance();
            ReleaseTrackingHeaderKind? expectedHeaderKind = isShippedFile ? ReleaseTrackingHeaderKind.ReleaseHeader : ReleaseTrackingHeaderKind.TableHeaderTitle;
            ReleaseTrackingRuleEntryKind? currentRuleEntryKind = null;

            foreach (TextLine line in sourceText.Lines)
            {
                string lineText = line.ToString().Trim();
                if (string.IsNullOrWhiteSpace(lineText) || lineText.StartsWith(";", StringComparison.Ordinal))
                {
                    // Skip blank and comment lines.
                    continue;
                }

                // Parse release header if applicable.
                switch (expectedHeaderKind)
                {
                    case ReleaseTrackingHeaderKind.ReleaseHeader:
                        // Parse new release, if any.
                        if (lineText.StartsWith(ReleasePrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            // Expect new table after this line.
                            expectedHeaderKind = ReleaseTrackingHeaderKind.TableHeaderTitle;

                            // Parse the release version.
                            string versionString = lineText.Substring(ReleasePrefix.Length).Trim();
                            if (!Version.TryParse(versionString, out var version))
                            {
                                ReportInvalidEntryDiagnostic(line, InvalidEntryKind.Header);
                                return ReleaseTrackingData.Default;
                            }
                            else
                            {
                                currentVersion = version;
                            }

                            continue;
                        }

                        ReportInvalidEntryDiagnostic(line, InvalidEntryKind.Header);
                        return ReleaseTrackingData.Default;

                    case ReleaseTrackingHeaderKind.TableHeaderTitle:
                        if (lineText.StartsWith(TableTitleNewRules, StringComparison.OrdinalIgnoreCase))
                        {
                            expectedHeaderKind = ReleaseTrackingHeaderKind.TableHeaderNewOrRemovedRulesLine1;
                            currentRuleEntryKind = ReleaseTrackingRuleEntryKind.New;
                        }
                        else if (lineText.StartsWith(TableTitleRemovedRules, StringComparison.OrdinalIgnoreCase))
                        {
                            expectedHeaderKind = ReleaseTrackingHeaderKind.TableHeaderNewOrRemovedRulesLine1;
                            currentRuleEntryKind = ReleaseTrackingRuleEntryKind.Removed;
                        }
                        else if (lineText.StartsWith(TableTitleChangedRules, StringComparison.OrdinalIgnoreCase))
                        {
                            expectedHeaderKind = ReleaseTrackingHeaderKind.TableHeaderChangedRulesLine1;
                            currentRuleEntryKind = ReleaseTrackingRuleEntryKind.Changed;
                        }
                        else
                        {
                            ReportInvalidEntryDiagnostic(line, InvalidEntryKind.Header);
                            return ReleaseTrackingData.Default;
                        }

                        continue;

                    case ReleaseTrackingHeaderKind.TableHeaderNewOrRemovedRulesLine1:
                        if (lineText.StartsWith(TableHeaderNewOrRemovedRulesLine1, StringComparison.OrdinalIgnoreCase))
                        {
                            expectedHeaderKind = ReleaseTrackingHeaderKind.TableHeaderNewOrRemovedRulesLine2;
                            continue;
                        }

                        ReportInvalidEntryDiagnostic(line, InvalidEntryKind.Header);
                        return ReleaseTrackingData.Default;

                    case ReleaseTrackingHeaderKind.TableHeaderNewOrRemovedRulesLine2:
                        expectedHeaderKind = null;
                        if (lineText.StartsWith(TableHeaderNewOrRemovedRulesLine2, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        ReportInvalidEntryDiagnostic(line, InvalidEntryKind.Header);
                        return ReleaseTrackingData.Default;

                    case ReleaseTrackingHeaderKind.TableHeaderChangedRulesLine1:
                        if (lineText.StartsWith(TableHeaderChangedRulesLine1, StringComparison.OrdinalIgnoreCase))
                        {
                            expectedHeaderKind = ReleaseTrackingHeaderKind.TableHeaderChangedRulesLine2;
                            continue;
                        }

                        ReportInvalidEntryDiagnostic(line, InvalidEntryKind.Header);
                        return ReleaseTrackingData.Default;

                    case ReleaseTrackingHeaderKind.TableHeaderChangedRulesLine2:
                        expectedHeaderKind = null;
                        if (lineText.StartsWith(TableHeaderChangedRulesLine2, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        ReportInvalidEntryDiagnostic(line, InvalidEntryKind.Header);
                        return ReleaseTrackingData.Default;

                    default:
                        // We might be starting a new release or table.
                        if (lineText.StartsWith("## ", StringComparison.OrdinalIgnoreCase))
                        {
                            goto case ReleaseTrackingHeaderKind.ReleaseHeader;
                        }
                        else if (lineText.StartsWith("### ", StringComparison.OrdinalIgnoreCase))
                        {
                            goto case ReleaseTrackingHeaderKind.TableHeaderTitle;
                        }

                        break;
                }

                RoslynDebug.Assert(currentRuleEntryKind != null);

                var parts = lineText.Split('|').Select(s => s.Trim()).ToArray();
                if (IsInvalidEntry(parts, currentRuleEntryKind.Value))
                {
                    // Report invalid entry, but continue parsing remaining entries.
                    ReportInvalidEntryDiagnostic(line, InvalidEntryKind.Other);
                    continue;
                }

                //  New or Removed rule entry: 
                //      "Rule ID | Category | Severity | HelpLink (optional)"
                //      "   0    |     1    |    2     |        3           "
                //
                //  Changed rule entry:
                //      "Rule ID | New Category | New Severity | Old Category | Old Severity | HelpLink (optional)"
                //      "   0    |     1        |     2        |     3        |     4        |        5           "

                string ruleId = parts[0];

                InvalidEntryKind? invalidEntryKind = TryParseFields(parts, categoryIndex: 1, severityIndex: 2,
                    out var category, out var defaultSeverity, out var enabledByDefault);
                if (invalidEntryKind.HasValue)
                {
                    ReportInvalidEntryDiagnostic(line, invalidEntryKind.Value);
                }

                ReleaseTrackingLine releaseTrackingLine;
                if (currentRuleEntryKind.Value == ReleaseTrackingRuleEntryKind.Changed)
                {
                    invalidEntryKind = TryParseFields(parts, categoryIndex: 3, severityIndex: 4,
                        out var oldCategory, out var oldDefaultSeverity, out var oldEnabledByDefault);
                    if (invalidEntryKind.HasValue)
                    {
                        ReportInvalidEntryDiagnostic(line, invalidEntryKind.Value);
                    }

                    // Verify at least one field is changed for the entry:
                    if (string.Equals(category, oldCategory, StringComparison.OrdinalIgnoreCase) &&
                        defaultSeverity == oldDefaultSeverity &&
                        enabledByDefault == oldEnabledByDefault)
                    {
                        ReportInvalidEntryDiagnostic(line, InvalidEntryKind.Other);
                        return ReleaseTrackingData.Default;
                    }

                    releaseTrackingLine = new ChangedRuleReleaseTrackingLine(ruleId,
                        category, enabledByDefault, defaultSeverity,
                        oldCategory, oldEnabledByDefault, oldDefaultSeverity,
                        line.Span, sourceText, path, isShippedFile);
                }
                else
                {
                    releaseTrackingLine = new NewOrRemovedRuleReleaseTrackingLine(ruleId,
                        category, enabledByDefault, defaultSeverity, line.Span, sourceText,
                        path, isShippedFile, currentRuleEntryKind.Value);
                }

                if (!releaseTrackingDataByRulesBuilder.TryGetValue(ruleId, out var releaseTrackingDataForRuleBuilder))
                {
                    releaseTrackingDataForRuleBuilder = new ReleaseTrackingDataForRuleBuilder();
                    releaseTrackingDataByRulesBuilder.Add(ruleId, releaseTrackingDataForRuleBuilder);
                }

                releaseTrackingDataForRuleBuilder.AddEntry(currentVersion, releaseTrackingLine, out var hasExistingEntry);
                if (hasExistingEntry && reportedInvalidLines.Add(line))
                {
                    // Rule '{0}' has more then one entry for release '{1}' in analyzer release file '{2}'.
                    string arg1 = ruleId;
                    string arg2 = currentVersion == s_unshippedVersion ? "unshipped" : currentVersion.ToString();
                    string arg3 = Path.GetFileName(path);
                    LinePositionSpan linePositionSpan = sourceText.Lines.GetLinePositionSpan(line.Span);
                    Location location = Location.Create(path, line.Span, linePositionSpan);
                    var diagnostic = Diagnostic.Create(RemoveDuplicateEntriesForAnalyzerReleaseRule, location, arg1, arg2, arg3);
                    addInvalidFileDiagnostic(diagnostic);
                }
            }

            var builder = ImmutableSortedDictionary.CreateBuilder<string, ReleaseTrackingDataForRule>();
            foreach (var (ruleId, value) in releaseTrackingDataByRulesBuilder)
            {
                var releaseTrackingDataForRule = new ReleaseTrackingDataForRule(ruleId, value);
                builder.Add(ruleId, releaseTrackingDataForRule);
            }

            return new ReleaseTrackingData(builder.ToImmutable());

            // Local functions
            void ReportInvalidEntryDiagnostic(TextLine line, InvalidEntryKind invalidEntryKind)
            {
                if (!reportedInvalidLines.Add(line))
                {
                    // Already reported.
                    return;
                }

                var rule = invalidEntryKind switch
                {
                    // Analyzer release file '{0}' has a missing or invalid release header '{1}'.
                    InvalidEntryKind.Header => InvalidHeaderInAnalyzerReleasesFileRule,

                    // Analyzer release file '{0}' has an entry with one or more 'Undetected' fields that need to be manually filled in '{1}'.
                    InvalidEntryKind.UndetectedField => InvalidUndetectedEntryInAnalyzerReleasesFileRule,

                    // Analyzer release file '{0}' has an invalid entry '{1}'.
                    InvalidEntryKind.Other => InvalidEntryInAnalyzerReleasesFileRule,
                    _ => throw new NotImplementedException(),
                };

                string arg1 = Path.GetFileName(path);
                string arg2 = line.ToString();
                LinePositionSpan linePositionSpan = sourceText.Lines.GetLinePositionSpan(line.Span);
                Location location = Location.Create(path, line.Span, linePositionSpan);
                var diagnostic = Diagnostic.Create(rule, location, arg1, arg2);
                addInvalidFileDiagnostic(diagnostic);
            }

            static bool IsInvalidEntry(string[] parts, ReleaseTrackingRuleEntryKind currentRuleEntryKind)
            {
                // Expected entry for New or Removed rules has 3 or 4 parts:
                //      "Rule ID | Category | Severity | HelpLink (optional)"
                //
                // Expected entry for Changed rules has 5 or 6 parts:
                //      "Rule ID | New Category | New Severity | Old Category | Old Severity | HelpLink (optional)"
                //
                // NOTE: Last field 'Helplink' is optional for both cases.

                if (parts.Length < 3 || parts.Length > 6)
                {
                    return true;
                }

                return currentRuleEntryKind switch
                {
                    ReleaseTrackingRuleEntryKind.New => parts.Length > 4,
                    ReleaseTrackingRuleEntryKind.Removed => parts.Length > 4,
                    ReleaseTrackingRuleEntryKind.Changed => parts.Length <= 4,
                    _ => throw new NotImplementedException()
                };
            }

            static InvalidEntryKind? TryParseFields(
                string[] parts, int categoryIndex, int severityIndex,
                out string category,
                out DiagnosticSeverity? defaultSeverity,
                out bool? enabledByDefault)
            {
                InvalidEntryKind? invalidEntryKind = null;

                category = parts[categoryIndex];
                if (category.Equals(UndetectedText, StringComparison.OrdinalIgnoreCase))
                {
                    invalidEntryKind = InvalidEntryKind.UndetectedField;
                }

                var severityPart = parts[severityIndex];
                if (Enum.TryParse(severityPart, ignoreCase: true, out DiagnosticSeverity parsedSeverity))
                {
                    defaultSeverity = parsedSeverity;
                    enabledByDefault = true;
                }
                else
                {
                    defaultSeverity = null;
                    if (string.Equals(severityPart, DisabledText, StringComparison.OrdinalIgnoreCase))
                    {
                        enabledByDefault = false;
                    }
                    else if (severityPart.Equals(UndetectedText, StringComparison.OrdinalIgnoreCase))
                    {
                        enabledByDefault = null;
                        invalidEntryKind = InvalidEntryKind.UndetectedField;
                    }
                    else
                    {
                        enabledByDefault = null;
                        invalidEntryKind = InvalidEntryKind.Other;
                    }
                }

                return invalidEntryKind;
            }
        }

        private enum InvalidEntryKind
        {
            Header,
            UndetectedField,
            Other
        }

        private static void AnalyzeAnalyzerReleases(
            string ruleId,
            IArgumentOperation ruleIdArgument,
            string? category,
            string? helpLink,
            bool? isEnabledByDefault,
            DiagnosticSeverity? defaultSeverity,
            ReleaseTrackingData shippedData,
            ReleaseTrackingData unshippedData,
            Action<Diagnostic> addDiagnostic)
        {
            if (!TryGetLatestReleaseTrackingLine(ruleId, shippedData, unshippedData, out _, out var releaseTrackingLine) ||
                releaseTrackingLine.IsShipped && releaseTrackingLine.IsRemovedRule)
            {
                var properties = ImmutableDictionary<string, string?>.Empty.Add(
                    EntryToAddPropertyName, GetEntry(ruleId, category, helpLink, isEnabledByDefault, defaultSeverity));
                var diagnostic = ruleIdArgument.CreateDiagnostic(DeclareDiagnosticIdInAnalyzerReleaseRule, properties, ruleId);
                addDiagnostic(diagnostic);
                return;
            }

            if (releaseTrackingLine.IsRemovedRule)
            {
                var diagnostic = ruleIdArgument.CreateDiagnostic(UnexpectedAnalyzerDiagnosticForRemovedDiagnosticIdRule, ruleId);
                addDiagnostic(diagnostic);
                return;
            }

            if (category != null && !string.Equals(category, releaseTrackingLine.Category, StringComparison.OrdinalIgnoreCase) ||
                isEnabledByDefault != null && isEnabledByDefault != releaseTrackingLine.EnabledByDefault ||
                defaultSeverity != null && defaultSeverity != releaseTrackingLine.DefaultSeverity)
            {
                string propertyName;
                (string category, bool? isEnabledByDefault, DiagnosticSeverity? defaultSeverity)? oldRule = null;
                if (!releaseTrackingLine.IsShipped)
                {
                    // For existing entry in unshipped file, we can just update.
                    propertyName = EntryToUpdatePropertyName;
                    if (releaseTrackingLine is ChangedRuleReleaseTrackingLine changedLine)
                    {
                        oldRule = (changedLine.OldCategory, changedLine.OldEnabledByDefault, changedLine.OldDefaultSeverity);
                    }
                }
                else
                {
                    // Need to add a new changed rule entry in unshipped file.
                    propertyName = EntryToAddPropertyName;
                    oldRule = (releaseTrackingLine.Category, releaseTrackingLine.EnabledByDefault, releaseTrackingLine.DefaultSeverity);
                }

                var newEntry = GetEntry(ruleId, category, helpLink, isEnabledByDefault, defaultSeverity, oldRule);
                var properties = ImmutableDictionary<string, string?>.Empty.Add(propertyName, newEntry);
                var diagnostic = ruleIdArgument.CreateDiagnostic(UpdateDiagnosticIdInAnalyzerReleaseRule, properties, ruleId);
                addDiagnostic(diagnostic);
                return;
            }
        }

        private static string GetEntry(
            string ruleId,
            string? category,
            string? helpLink,
            bool? isEnabledByDefault,
            DiagnosticSeverity? defaultSeverity,
            (string category, bool? isEnabledByDefault, DiagnosticSeverity? defaultSeverity)? oldRule = null)
        {
            // Rule ID | Category | Severity | HelpLink (optional)
            //      OR
            // Rule ID | New Category | New Severity | Old Category | Old Severity | HelpLink (optional)
            var entry = $"{ruleId} | {GetCategoryText(category)} | {GetSeverityText(isEnabledByDefault, defaultSeverity)} |";

            if (oldRule.HasValue)
            {
                entry += $" {GetCategoryText(oldRule.Value.category)} | {GetSeverityText(oldRule.Value.isEnabledByDefault, oldRule.Value.defaultSeverity)} |";
            }

            helpLink ??= TryGetHelpLinkForCARule(ruleId);
            if (!string.IsNullOrEmpty(helpLink))
            {
                entry += $" [Documentation]({helpLink})";
            }

            return entry;

            static string GetCategoryText(string? category)
                => category ?? UndetectedText;

            static string GetSeverityText(bool? isEnabledByDefault, DiagnosticSeverity? defaultSeverity)
                => isEnabledByDefault == false ? DisabledText : (defaultSeverity?.ToString() ?? UndetectedText);

            static string? TryGetHelpLinkForCARule(string ruleId)
            {
                if (ruleId.StartsWith("CA", StringComparison.OrdinalIgnoreCase) &&
                    ruleId.Length > 2 &&
                    long.TryParse(ruleId.Substring(2), out _))
                {
#pragma warning disable CA1308 // Normalize strings to uppercase - use lower case ID in help link
                    return $"https://docs.microsoft.com/visualstudio/code-quality/{ruleId.ToLowerInvariant()}";
#pragma warning restore CA1308 // Normalize strings to uppercase
                }

                return null;
            }
        }

        private static bool TryGetLatestReleaseTrackingLine(
            string ruleId,
            ReleaseTrackingData shippedData,
            ReleaseTrackingData unshippedData,
            [NotNullWhen(returnValue: true)] out Version? version,
            [NotNullWhen(returnValue: true)] out ReleaseTrackingLine? releaseTrackingLine)
        {
            // Unshipped data is considered to always have a higher version then shipped data.
            return unshippedData.TryGetLatestReleaseTrackingLine(ruleId, out version, out releaseTrackingLine) ||
                shippedData.TryGetLatestReleaseTrackingLine(ruleId, out version, out releaseTrackingLine);
        }

        private static void ReportAnalyzerReleaseTrackingDiagnostics(
            List<Diagnostic>? invalidReleaseTrackingDiagnostics,
            ReleaseTrackingData shippedData,
            ReleaseTrackingData unshippedData,
            PooledConcurrentSet<string> seenRuleIds,
            CompilationAnalysisContext compilationEndContext)
        {
            // Report any invalid release tracking file diagnostics.
            if (invalidReleaseTrackingDiagnostics?.Count > 0)
            {
                foreach (var diagnostic in invalidReleaseTrackingDiagnostics)
                {
                    compilationEndContext.ReportDiagnostic(diagnostic);
                }

                // Do not report additional cascaded diagnostics.
                return;
            }

            // Map to track and report duplicate entries for same rule ID.
            using var lastEntriesByRuleMap = PooledDictionary<string, (Version version, ReleaseTrackingLine releaseTrackingLine)>.GetInstance();

            // Process each entry in unshipped file to flag rules which are not seen.
            foreach (var (ruleId, releaseTrackingDataForRule) in unshippedData.ReleaseTrackingDataByRuleIdMap)
            {
                var (unshippedVersion, releaseTrackingLine) = releaseTrackingDataForRule.ReleasesByVersionMap.First();
                lastEntriesByRuleMap[ruleId] = (unshippedVersion, releaseTrackingLine);
                if (seenRuleIds.Add(ruleId) && !releaseTrackingLine.IsRemovedRule)
                {
                    compilationEndContext.ReportNoLocationDiagnostic(RemoveUnshippedDeletedDiagnosticIdRule, ruleId);
                }
            }

            // Process each entry in shipped file to flag rules which are not seen.
            foreach (var (ruleId, releaseTrackingDataForRule) in shippedData.ReleaseTrackingDataByRuleIdMap)
            {
                foreach (var (version, releaseTrackingLine) in releaseTrackingDataForRule.ReleasesByVersionMap)
                {
                    if (seenRuleIds.Add(ruleId) && !releaseTrackingLine.IsRemovedRule)
                    {
                        compilationEndContext.ReportNoLocationDiagnostic(RemoveShippedDeletedDiagnosticIdRule, ruleId, version);
                    }

                    if (lastEntriesByRuleMap.TryGetValue(ruleId, out var lastEntry) &&
                        lastEntry.version != version &&
                        lastEntry.releaseTrackingLine.Category.Equals(releaseTrackingLine.Category, StringComparison.OrdinalIgnoreCase) &&
                        lastEntry.releaseTrackingLine.EnabledByDefault == releaseTrackingLine.EnabledByDefault &&
                        lastEntry.releaseTrackingLine.DefaultSeverity == releaseTrackingLine.DefaultSeverity &&
                        lastEntry.releaseTrackingLine.Kind == releaseTrackingLine.Kind)
                    {
                        // Rule '{0}' has duplicate entry between release '{1}' and release '{2}'.
                        string arg1 = ruleId;
                        string arg2 = lastEntry.version == s_unshippedVersion ? "unshipped" : lastEntry.version.ToString();
                        string arg3 = version.ToString();
                        LinePositionSpan linePositionSpan = lastEntry.releaseTrackingLine.SourceText.Lines.GetLinePositionSpan(lastEntry.releaseTrackingLine.Span);
                        Location location = Location.Create(lastEntry.releaseTrackingLine.Path, lastEntry.releaseTrackingLine.Span, linePositionSpan);
                        var diagnostic = Diagnostic.Create(RemoveDuplicateEntriesBetweenAnalyzerReleasesRule, location, arg1, arg2, arg3);
                        compilationEndContext.ReportDiagnostic(diagnostic);
                    }

                    lastEntriesByRuleMap[ruleId] = (version, releaseTrackingLine);
                }
            }

            // 'lastEntriesByRuleMap' should now have the first entry for each rule.
            // Flag each such first entry that is not marked as new rule - a removed/changed rule entry without a prior new entry is invalid.
            foreach (var (_, releaseTrackingLine) in lastEntriesByRuleMap.Values)
            {
                if (releaseTrackingLine.Kind != ReleaseTrackingRuleEntryKind.New)
                {
                    // Analyzer release file '{0}' has an invalid '{1}' entry without a prior shipped release for the rule '{2}'. Instead, add a separate '{1}' entry for the rule in unshipped release file.
                    string arg1 = Path.GetFileName(releaseTrackingLine.Path);
                    string arg2 = releaseTrackingLine.Kind.ToString();
                    string arg3 = releaseTrackingLine.RuleId;
                    LinePositionSpan linePositionSpan = releaseTrackingLine.SourceText.Lines.GetLinePositionSpan(releaseTrackingLine.Span);
                    Location location = Location.Create(releaseTrackingLine.Path, releaseTrackingLine.Span, linePositionSpan);
                    var diagnostic = Diagnostic.Create(InvalidRemovedOrChangedWithoutPriorNewEntryInAnalyzerReleasesFileRule, location, arg1, arg2, arg3);
                    compilationEndContext.ReportDiagnostic(diagnostic);
                }
            }
        }

#pragma warning disable CA1815 // Override equals and operator equals on value types
        private sealed class ReleaseTrackingData
#pragma warning restore CA1815 // Override equals and operator equals on value types
        {
            public static readonly ReleaseTrackingData Default = new ReleaseTrackingData();
            public ImmutableSortedDictionary<string, ReleaseTrackingDataForRule> ReleaseTrackingDataByRuleIdMap { get; }

            private ReleaseTrackingData()
                : this(ImmutableSortedDictionary<string, ReleaseTrackingDataForRule>.Empty)
            {
            }

            internal ReleaseTrackingData(ImmutableSortedDictionary<string, ReleaseTrackingDataForRule> releaseTrackingDataByRuleIdMap)
            {
                ReleaseTrackingDataByRuleIdMap = releaseTrackingDataByRuleIdMap;
            }

            public bool TryGetLatestReleaseTrackingLine(
                string ruleId,
                [NotNullWhen(returnValue: true)] out Version? version,
                [NotNullWhen(returnValue: true)] out ReleaseTrackingLine? releaseTrackingLine)
            {
                version = null;
                releaseTrackingLine = null;
                if (!ReleaseTrackingDataByRuleIdMap.TryGetValue(ruleId, out var releaseTrackingDataForRule) ||
                    releaseTrackingDataForRule.ReleasesByVersionMap.IsEmpty)
                {
                    return false;
                }

                (version, releaseTrackingLine) = releaseTrackingDataForRule.ReleasesByVersionMap.First();
                return true;
            }
        }

#pragma warning disable CA1815 // Override equals and operator equals on value types
        private readonly struct ReleaseTrackingDataForRule
#pragma warning restore CA1815 // Override equals and operator equals on value types
        {
            public string RuleId { get; }
            public ImmutableSortedDictionary<Version, ReleaseTrackingLine> ReleasesByVersionMap { get; }

            internal ReleaseTrackingDataForRule(string ruleId, ReleaseTrackingDataForRuleBuilder builder)
            {
                RuleId = ruleId;
                ReleasesByVersionMap = builder.ToImmutable();
            }
        }

        private sealed class ReleaseTrackingDataForRuleBuilder
        {
            private readonly ImmutableSortedDictionary<Version, ReleaseTrackingLine>.Builder _builder
                = ImmutableSortedDictionary.CreateBuilder<Version, ReleaseTrackingLine>(ReverseComparer.Instance);

            public void AddEntry(Version version, ReleaseTrackingLine releaseTrackingLine, out bool hasExistingEntry)
            {
                hasExistingEntry = _builder.ContainsKey(version);
                _builder[version] = releaseTrackingLine;
            }

            public ImmutableSortedDictionary<Version, ReleaseTrackingLine> ToImmutable()
                => _builder.ToImmutable();

            private sealed class ReverseComparer : IComparer<Version>
            {
                public static readonly IComparer<Version> Instance = new ReverseComparer();
                private ReverseComparer() { }

                public int Compare(Version x, Version y)
                {
                    return x.CompareTo(y) * -1;
                }
            }
        }

        private abstract class ReleaseTrackingLine
        {
            public string RuleId { get; }
            public string Category { get; }
            public bool? EnabledByDefault { get; }
            public DiagnosticSeverity? DefaultSeverity { get; }

            public TextSpan Span { get; }
            public SourceText SourceText { get; }
            public string Path { get; }
            public bool IsShipped { get; }
            public bool IsRemovedRule => Kind == ReleaseTrackingRuleEntryKind.Removed;
            public ReleaseTrackingRuleEntryKind Kind { get; }

            internal static ReleaseTrackingLine Create(
                string ruleId, string category, bool? enabledByDefault,
                DiagnosticSeverity? defaultSeverity,
                TextSpan span, SourceText sourceText,
                string path, bool isShipped, ReleaseTrackingRuleEntryKind kind)
            {
                return new NewOrRemovedRuleReleaseTrackingLine(ruleId, category,
                    enabledByDefault, defaultSeverity, span, sourceText, path, isShipped, kind);
            }

            protected ReleaseTrackingLine(string ruleId, string category, bool? enabledByDefault,
                DiagnosticSeverity? defaultSeverity,
                TextSpan span, SourceText sourceText,
                string path, bool isShipped, ReleaseTrackingRuleEntryKind kind)
            {
                RuleId = ruleId;
                Category = category;
                EnabledByDefault = enabledByDefault;
                DefaultSeverity = defaultSeverity;
                Span = span;
                SourceText = sourceText;
                Path = path;
                IsShipped = isShipped;
                Kind = kind;
            }
        }

        private sealed class NewOrRemovedRuleReleaseTrackingLine : ReleaseTrackingLine
        {
            internal NewOrRemovedRuleReleaseTrackingLine(
                string ruleId, string category, bool? enabledByDefault,
                DiagnosticSeverity? defaultSeverity,
                TextSpan span, SourceText sourceText,
                string path, bool isShipped, ReleaseTrackingRuleEntryKind kind)
                : base(ruleId, category, enabledByDefault, defaultSeverity, span, sourceText, path, isShipped, kind)
            {
                Debug.Assert(kind == ReleaseTrackingRuleEntryKind.New || kind == ReleaseTrackingRuleEntryKind.Removed);
            }
        }

        private sealed class ChangedRuleReleaseTrackingLine : ReleaseTrackingLine
        {
            public string OldCategory { get; }
            public bool? OldEnabledByDefault { get; }
            public DiagnosticSeverity? OldDefaultSeverity { get; }

            internal ChangedRuleReleaseTrackingLine(
                string ruleId, string category, bool? enabledByDefault,
                DiagnosticSeverity? defaultSeverity,
                string oldCategory, bool? oldEnabledByDefault,
                DiagnosticSeverity? oldDefaultSeverity,
                TextSpan span, SourceText sourceText,
                string path, bool isShipped)
                : base(ruleId, category, enabledByDefault, defaultSeverity, span, sourceText, path, isShipped, ReleaseTrackingRuleEntryKind.Changed)
            {
                OldCategory = oldCategory;
                OldEnabledByDefault = oldEnabledByDefault;
                OldDefaultSeverity = oldDefaultSeverity;
            }
        }

        private enum ReleaseTrackingHeaderKind
        {
            ReleaseHeader,
            TableHeaderTitle,
            TableHeaderNewOrRemovedRulesLine1,
            TableHeaderNewOrRemovedRulesLine2,
            TableHeaderChangedRulesLine1,
            TableHeaderChangedRulesLine2,
        }

        private enum ReleaseTrackingRuleEntryKind
        {
            New,
            Removed,
            Changed,
        }
    }
}
