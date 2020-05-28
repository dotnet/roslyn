// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.ReleaseTracking;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.ReleaseTracking.ReleaseTrackingHelper;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    public sealed partial class DiagnosticDescriptorCreationAnalyzer
    {
        // Property names which are keys for diagnostic property bag passed to the code fixer.
        internal const string EntryToAddPropertyName = nameof(EntryToAddPropertyName);
        internal const string EntryToUpdatePropertyName = nameof(EntryToUpdatePropertyName);

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

        internal static readonly DiagnosticDescriptor EnableAnalyzerReleaseTrackingRule = new DiagnosticDescriptor(
            id: DiagnosticIds.EnableAnalyzerReleaseTrackingRuleId,
            title: CodeAnalysisDiagnosticsResources.EnableAnalyzerReleaseTrackingRuleTitle,
            messageFormat: CodeAnalysisDiagnosticsResources.EnableAnalyzerReleaseTrackingRuleMessage,
            category: DiagnosticCategory.MicrosoftCodeAnalysisReleaseTracking,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CodeAnalysisDiagnosticsResources.EnableAnalyzerReleaseTrackingRuleDescription,
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
                shippedData = shippedText != null ? ReleaseTrackingData.Default : null;
                unshippedData = unshippedText != null ? ReleaseTrackingData.Default : null;
                invalidFileDiagnostics = null;
                return false;
            }

            var diagnostics = new List<Diagnostic>();
            using var reportedInvalidLines = PooledHashSet<TextLine>.GetInstance();
            shippedData = ReadReleaseTrackingData(shippedText.Path, shippedText.GetText(cancellationToken), OnDuplicateEntryInRelease, OnInvalidEntry, isShippedFile: true);
            unshippedData = ReadReleaseTrackingData(unshippedText.Path, unshippedText.GetText(cancellationToken), OnDuplicateEntryInRelease, OnInvalidEntry, isShippedFile: false);

            invalidFileDiagnostics = diagnostics;
            return invalidFileDiagnostics.Count == 0;

            // Local functions.
            void OnDuplicateEntryInRelease(string ruleId, Version currentVersion, string path, SourceText sourceText, TextLine line)
            {
                if (!reportedInvalidLines.Add(line))
                {
                    // Already reported.
                    return;
                }

                RoslynDebug.Assert(diagnostics != null);

                // Rule '{0}' has more then one entry for release '{1}' in analyzer release file '{2}'.
                string arg1 = ruleId;
                string arg2 = currentVersion == UnshippedVersion ? "unshipped" : currentVersion.ToString();
                string arg3 = Path.GetFileName(path);
                LinePositionSpan linePositionSpan = sourceText.Lines.GetLinePositionSpan(line.Span);
                Location location = Location.Create(path, line.Span, linePositionSpan);
                var diagnostic = Diagnostic.Create(RemoveDuplicateEntriesForAnalyzerReleaseRule, location, arg1, arg2, arg3);
                diagnostics.Add(diagnostic);
            }

            void OnInvalidEntry(TextLine line, InvalidEntryKind invalidEntryKind, string path, SourceText sourceText)
            {
                RoslynDebug.Assert(diagnostics != null);
                RoslynDebug.Assert(reportedInvalidLines != null);

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
                diagnostics.Add(diagnostic);
            }
        }

        private static bool TryGetReleaseTrackingFiles(
            ImmutableArray<AdditionalText> additionalTexts,
            CancellationToken cancellationToken,
            [NotNullWhen(returnValue: true)] out AdditionalText? shippedText,
            [NotNullWhen(returnValue: true)] out AdditionalText? unshippedText)
        {
            shippedText = null;
            unshippedText = null;

            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
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

        private static void AnalyzeAnalyzerReleases(
            string ruleId,
            IArgumentOperation ruleIdArgument,
            string? category,
            string analyzerName,
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
                    EntryToAddPropertyName, GetEntry(ruleId, category, analyzerName, helpLink, isEnabledByDefault, defaultSeverity));
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

                var newEntry = GetEntry(ruleId, category, analyzerName, helpLink, isEnabledByDefault, defaultSeverity, oldRule);
                var properties = ImmutableDictionary<string, string?>.Empty.Add(propertyName, newEntry);
                var diagnostic = ruleIdArgument.CreateDiagnostic(UpdateDiagnosticIdInAnalyzerReleaseRule, properties, ruleId);
                addDiagnostic(diagnostic);
                return;
            }
        }

        private static string GetEntry(
            string ruleId,
            string? category,
            string analyzerName,
            string? helpLink,
            bool? isEnabledByDefault,
            DiagnosticSeverity? defaultSeverity,
            (string category, bool? isEnabledByDefault, DiagnosticSeverity? defaultSeverity)? oldRule = null)
        {
            // Rule ID | Category | Severity | Notes
            //      OR
            // Rule ID | New Category | New Severity | Old Category | Old Severity | Notes
            var entry = $"{ruleId} | {GetCategoryText(category)} | {GetSeverityText(isEnabledByDefault, defaultSeverity)} |";

            if (oldRule.HasValue)
            {
                entry += $" {GetCategoryText(oldRule.Value.category)} | {GetSeverityText(oldRule.Value.isEnabledByDefault, oldRule.Value.defaultSeverity)} |";
            }

            entry += $" {analyzerName}";

            helpLink ??= TryGetHelpLinkForCARule(ruleId);
            if (!string.IsNullOrEmpty(helpLink))
            {
                entry += $", [Documentation]({helpLink})";
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
                        string arg2 = lastEntry.version == UnshippedVersion ? "unshipped" : lastEntry.version.ToString();
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
    }
}
