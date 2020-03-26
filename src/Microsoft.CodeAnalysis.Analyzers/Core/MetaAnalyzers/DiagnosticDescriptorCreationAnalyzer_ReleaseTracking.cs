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
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    public sealed partial class DiagnosticDescriptorCreationAnalyzer
    {
        internal const string ShippedFileName = "AnalyzerReleases.Shipped.md";
        internal const string UnshippedFileName = "AnalyzerReleases.Unshipped.md";
        internal const string RemovedPrefix = "*REMOVED*";
        internal const string ReleasePrefix = "## Release";
        internal const string ReleaseHeaderLine1 = @"Rule ID | Category | Severity | HelpLink (optional)";
        internal const string ReleaseHeaderLine2 = @"--------|----------|----------|--------------------";
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

        internal static readonly DiagnosticDescriptor InvalidRemovedWithoutShippedEntryInAnalyzerReleasesFileRule = new DiagnosticDescriptor(
            id: DiagnosticIds.InvalidEntryInAnalyzerReleasesFileRuleId,
            title: CodeAnalysisDiagnosticsResources.InvalidEntryInAnalyzerReleasesFileRuleTitle,
            messageFormat: CodeAnalysisDiagnosticsResources.InvalidRemovedWithoutShippedEntryInAnalyzerReleasesFileRuleMessage,
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
            int? expectedReleaseHeaderLine = isShippedFile ? 0 : 1;
            using var reportedInvalidLines = PooledHashSet<TextLine>.GetInstance();

            foreach (TextLine line in sourceText.Lines)
            {
                string lineText = line.ToString().Trim();
                if (string.IsNullOrWhiteSpace(lineText) || lineText.StartsWith(";", StringComparison.Ordinal))
                {
                    // Skip blank and comment lines.
                    continue;
                }

                // Parse release header if applicable.
                switch (expectedReleaseHeaderLine)
                {
                    case 0:
                    default:
                        // Parse new release, if any.
                        if (lineText.StartsWith(ReleasePrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!isShippedFile)
                            {
                                ReportInvalidEntryDiagnostic(line, InvalidEntryKind.Header);
                                return ReleaseTrackingData.Default;
                            }

                            // Expect release header line 1 after this line.
                            expectedReleaseHeaderLine = 1;

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
                        else if (expectedReleaseHeaderLine == 0)
                        {
                            ReportInvalidEntryDiagnostic(line, InvalidEntryKind.Header);
                            return ReleaseTrackingData.Default;
                        }
                        else
                        {
                            break;
                        }

                    case 1:
                        if (lineText.StartsWith(ReleaseHeaderLine1, StringComparison.OrdinalIgnoreCase))
                        {
                            expectedReleaseHeaderLine = 2;
                            continue;
                        }

                        ReportInvalidEntryDiagnostic(line, InvalidEntryKind.Header);
                        return ReleaseTrackingData.Default;

                    case 2:
                        expectedReleaseHeaderLine = null;
                        if (lineText.StartsWith(ReleaseHeaderLine2, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        ReportInvalidEntryDiagnostic(line, InvalidEntryKind.Header);
                        return ReleaseTrackingData.Default;
                }

                bool hasRemovedPrefix;
                if (lineText.StartsWith(RemovedPrefix, StringComparison.Ordinal))
                {
                    hasRemovedPrefix = true;
                    lineText = lineText.Substring(RemovedPrefix.Length);
                }
                else
                {
                    hasRemovedPrefix = false;
                }

                var parts = lineText.Split('|').Select(s => s.Trim()).ToArray();
                switch (parts.Length)
                {
                    // Last field 'Helplink' is optional
                    case 3:
                    case 4:
                        string ruleId = parts[0];

                        string category = parts[1];
                        if (category.Equals(UndetectedText, StringComparison.OrdinalIgnoreCase))
                        {
                            ReportInvalidEntryDiagnostic(line, InvalidEntryKind.UndetectedField);
                        }

                        DiagnosticSeverity? defaultSeverity;
                        bool? enabledByDefault;
                        var severityPart = parts[2];
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
                                ReportInvalidEntryDiagnostic(line, InvalidEntryKind.UndetectedField);
                            }
                            else
                            {
                                enabledByDefault = null;
                                ReportInvalidEntryDiagnostic(line, InvalidEntryKind.Other);
                            }
                        }

                        var releaseTrackingLine = new ReleaseTrackingLine(ruleId, category, enabledByDefault,
                            defaultSeverity, line.Span, sourceText, path, isShippedFile, hasRemovedPrefix);

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

                        break;

                    default:
                        ReportInvalidEntryDiagnostic(line, InvalidEntryKind.Other);
                        break;
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
            if (!TryGetLatestReleaseTrackingLine(ruleId, shippedData, unshippedData, out var version, out var releaseTrackingLine) ||
                releaseTrackingLine.IsShipped && releaseTrackingLine.HasRemovedPrefix)
            {
                var properties = ImmutableDictionary<string, string?>.Empty.Add(
                    EntryToAddPropertyName, GetEntry(ruleId, category, helpLink, isEnabledByDefault, defaultSeverity));
                var diagnostic = ruleIdArgument.CreateDiagnostic(DeclareDiagnosticIdInAnalyzerReleaseRule, properties, ruleId);
                addDiagnostic(diagnostic);
                return;
            }

            if (releaseTrackingLine.HasRemovedPrefix)
            {
                var diagnostic = ruleIdArgument.CreateDiagnostic(UnexpectedAnalyzerDiagnosticForRemovedDiagnosticIdRule, ruleId);
                addDiagnostic(diagnostic);
                return;
            }

            if (category != null && !string.Equals(category, releaseTrackingLine.Category, StringComparison.OrdinalIgnoreCase) ||
                isEnabledByDefault != null && isEnabledByDefault != releaseTrackingLine.EnabledByDefault ||
                defaultSeverity != null && defaultSeverity != releaseTrackingLine.DefaultSeverity)
            {
                var propertyName = version == s_unshippedVersion ?
                    EntryToUpdatePropertyName :
                    EntryToAddPropertyName;
                var newEntry = GetEntry(ruleId, category, helpLink, isEnabledByDefault, defaultSeverity);
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
            DiagnosticSeverity? defaultSeverity)
        {
            var categoryText = category ?? UndetectedText;
            var defaultSeverityText = isEnabledByDefault == false ? DisabledText : (defaultSeverity?.ToString() ?? UndetectedText);

            // Rule ID | Category | Severity | HelpLink (optional)
            var entry = $"{ruleId} | {categoryText} | {defaultSeverityText} |";

            helpLink ??= TryGetHelpLinkForCARule(ruleId);
            if (!string.IsNullOrEmpty(helpLink))
            {
                entry += $" [Documentation]({helpLink})";
            }

            return entry;

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
                var releaseTrackingLine = releaseTrackingDataForRule.ReleasesByVersionMap[s_unshippedVersion];
                lastEntriesByRuleMap[ruleId] = (s_unshippedVersion, releaseTrackingLine);
                if (seenRuleIds.Add(ruleId) && !releaseTrackingLine.HasRemovedPrefix)
                {
                    compilationEndContext.ReportNoLocationDiagnostic(RemoveUnshippedDeletedDiagnosticIdRule, ruleId);
                }
            }

            // Process each entry in shipped file to flag rules which are not seen.
            foreach (var (ruleId, releaseTrackingDataForRule) in shippedData.ReleaseTrackingDataByRuleIdMap)
            {
                foreach (var (version, releaseTrackingLine) in releaseTrackingDataForRule.ReleasesByVersionMap)
                {
                    if (seenRuleIds.Add(ruleId) && !releaseTrackingLine.HasRemovedPrefix)
                    {
                        compilationEndContext.ReportNoLocationDiagnostic(RemoveShippedDeletedDiagnosticIdRule, ruleId, version);
                    }

                    if (lastEntriesByRuleMap.TryGetValue(ruleId, out var lastEntry) &&
                        lastEntry.version != version &&
                        lastEntry.releaseTrackingLine.Category.Equals(releaseTrackingLine.Category, StringComparison.OrdinalIgnoreCase) &&
                        lastEntry.releaseTrackingLine.EnabledByDefault == releaseTrackingLine.EnabledByDefault &&
                        lastEntry.releaseTrackingLine.DefaultSeverity == releaseTrackingLine.DefaultSeverity &&
                        lastEntry.releaseTrackingLine.HasRemovedPrefix == releaseTrackingLine.HasRemovedPrefix)
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
            // Flag each such first entry that is marked as removed - a removed entry without a prior shipped entry is invalid.
            foreach (var (_, releaseTrackingLine) in lastEntriesByRuleMap.Values)
            {
                if (releaseTrackingLine.HasRemovedPrefix)
                {
                    // Analyzer release file '{0}' has an invalid removed entry without a prior shipped release for the rule '{1}'. Instead, add a separate removed entry for the rule in unshipped release file.
                    string arg1 = Path.GetFileName(releaseTrackingLine.Path);
                    string arg2 = releaseTrackingLine.RuleId;
                    LinePositionSpan linePositionSpan = releaseTrackingLine.SourceText.Lines.GetLinePositionSpan(releaseTrackingLine.Span);
                    Location location = Location.Create(releaseTrackingLine.Path, releaseTrackingLine.Span, linePositionSpan);
                    var diagnostic = Diagnostic.Create(InvalidRemovedWithoutShippedEntryInAnalyzerReleasesFileRule, location, arg1, arg2);
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

        private sealed class ReleaseTrackingLine
        {
            public string RuleId { get; }
            public string Category { get; }
            public bool? EnabledByDefault { get; }
            public DiagnosticSeverity? DefaultSeverity { get; }

            public TextSpan Span { get; }
            public SourceText SourceText { get; }
            public string Path { get; }
            public bool IsShipped { get; }
            public bool HasRemovedPrefix { get; }

            internal ReleaseTrackingLine(
                string ruleId, string category, bool? enabledByDefault,
                DiagnosticSeverity? defaultSeverity, TextSpan span, SourceText sourceText,
                string path, bool isShipped, bool hasRemovedPrefix)
            {
                RuleId = ruleId;
                Category = category;
                EnabledByDefault = enabledByDefault;
                DefaultSeverity = defaultSeverity;
                Span = span;
                SourceText = sourceText;
                Path = path;
                IsShipped = isShipped;
                HasRemovedPrefix = hasRemovedPrefix;
            }
        }
    }
}
