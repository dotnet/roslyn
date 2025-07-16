// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

#if MICROSOFT_CODEANALYSIS_ANALYZERS
using Analyzer.Utilities.Extensions;
#endif

namespace Microsoft.CodeAnalysis.ReleaseTracking
{
    internal static class ReleaseTrackingHelper
    {
        internal const string ShippedFileName = "AnalyzerReleases.Shipped.md";
        internal const string UnshippedFileName = "AnalyzerReleases.Unshipped.md";
        internal const string DisabledText = "Disabled";
        internal const string UndetectedText = @"`<Undetected>`";
        internal const string ReleasePrefix = "## Release";
        internal const string TableTitleNewRules = "### New Rules";
        internal const string TableTitleRemovedRules = "### Removed Rules";
        internal const string TableTitleChangedRules = "### Changed Rules";
        internal const string TableHeaderNewOrRemovedRulesLine1 = @"Rule ID | Category | Severity | Notes";
        internal const string TableHeaderNewOrRemovedRulesLine2 = @"--------|----------|----------|-------";
        internal const string TableHeaderChangedRulesLine1 = @"Rule ID | New Category | New Severity | Old Category | Old Severity | Notes";
        internal const string TableHeaderChangedRulesLine2 = @"--------|--------------|--------------|--------------|--------------|-------";
        internal const string TableHeaderNewOrRemovedRulesLine1RegexPattern = @"^\|?\s*Rule ID\s*\|\s*Category\s*\|\s*\Severity\s*\|\s*Notes\s*\|?";
        internal const string TableHeaderChangedRulesLine1RegexPattern = @"^\|?\s*Rule ID\s*\|\s*New Category\s*\|\s*New Severity\s*\|\s*Old Category\s*\|\s*Old Severity\s*\|\s*Notes\s*\|?";
        internal const string TableHeaderNewOrRemovedRulesLine2RegexPattern = @"^\|?-{3,}\|-{3,}\|-{3,}\|-{3,}\|?";
        internal const string TableHeaderChangedRulesLine2RegexPattern = @"^\|?-{3,}\|-{3,}\|-{3,}\|-{3,}\|-{3,}\|-{3,}\|?";

        internal static Version UnshippedVersion { get; } = new Version(int.MaxValue, int.MaxValue);

        internal static ReleaseTrackingData ReadReleaseTrackingData(
            string path,
            SourceText sourceText,
            Action<string, Version, string, SourceText, TextLine> onDuplicateEntryInRelease,
            Action<TextLine, InvalidEntryKind, string, SourceText> onInvalidEntry,
            bool isShippedFile)
        {
            var releaseTrackingDataByRulesBuilder = new Dictionary<string, ReleaseTrackingDataForRuleBuilder>();
            var currentVersion = UnshippedVersion;
            ReleaseTrackingHeaderKind? expectedHeaderKind = isShippedFile ? ReleaseTrackingHeaderKind.ReleaseHeader : ReleaseTrackingHeaderKind.TableHeaderTitle;
            ReleaseTrackingRuleEntryKind? currentRuleEntryKind = null;
            using var _ = PooledHashSet<Version>.GetInstance(out var versionsBuilder);

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
                            string versionString = lineText[ReleasePrefix.Length..].Trim();
                            if (!Version.TryParse(versionString, out var version))
                            {
                                OnInvalidEntry(line, InvalidEntryKind.Header);
                                return ReleaseTrackingData.Default;
                            }
                            else
                            {
                                currentVersion = version;
                                versionsBuilder.Add(version);
                            }

                            continue;
                        }

                        OnInvalidEntry(line, InvalidEntryKind.Header);
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
                            OnInvalidEntry(line, InvalidEntryKind.Header);
                            return ReleaseTrackingData.Default;
                        }

                        continue;

                    case ReleaseTrackingHeaderKind.TableHeaderNewOrRemovedRulesLine1:
                        if (Regex.IsMatch(lineText, TableHeaderNewOrRemovedRulesLine1RegexPattern, RegexOptions.IgnoreCase))
                        {
                            expectedHeaderKind = ReleaseTrackingHeaderKind.TableHeaderNewOrRemovedRulesLine2;
                            continue;
                        }

                        OnInvalidEntry(line, InvalidEntryKind.Header);
                        return ReleaseTrackingData.Default;

                    case ReleaseTrackingHeaderKind.TableHeaderNewOrRemovedRulesLine2:
                        expectedHeaderKind = null;
                        if (Regex.IsMatch(lineText, TableHeaderNewOrRemovedRulesLine2RegexPattern, RegexOptions.IgnoreCase))
                        {
                            continue;
                        }

                        OnInvalidEntry(line, InvalidEntryKind.Header);
                        return ReleaseTrackingData.Default;

                    case ReleaseTrackingHeaderKind.TableHeaderChangedRulesLine1:
                        if (Regex.IsMatch(lineText, TableHeaderChangedRulesLine1RegexPattern, RegexOptions.IgnoreCase))
                        {
                            expectedHeaderKind = ReleaseTrackingHeaderKind.TableHeaderChangedRulesLine2;
                            continue;
                        }

                        OnInvalidEntry(line, InvalidEntryKind.Header);
                        return ReleaseTrackingData.Default;

                    case ReleaseTrackingHeaderKind.TableHeaderChangedRulesLine2:
                        expectedHeaderKind = null;
                        if (Regex.IsMatch(lineText, TableHeaderChangedRulesLine2RegexPattern, RegexOptions.IgnoreCase))
                        {
                            continue;
                        }

                        OnInvalidEntry(line, InvalidEntryKind.Header);
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

                var parts = lineText.Trim('|').Split('|').Select(s => s.Trim()).ToArray();
                if (IsInvalidEntry(parts, currentRuleEntryKind.Value))
                {
                    // Report invalid entry, but continue parsing remaining entries.
                    OnInvalidEntry(line, InvalidEntryKind.Other);
                    continue;
                }

                //  New or Removed rule entry:
                //      "Rule ID | Category | Severity | Notes"
                //      "   0    |     1    |    2     |        3           "
                //
                //  Changed rule entry:
                //      "Rule ID | New Category | New Severity | Old Category | Old Severity | Notes"
                //      "   0    |     1        |     2        |     3        |     4        |        5           "

                string ruleId = parts[0];

                InvalidEntryKind? invalidEntryKind = TryParseFields(parts, categoryIndex: 1, severityIndex: 2,
                    out var category, out var defaultSeverity, out var enabledByDefault);
                if (invalidEntryKind.HasValue)
                {
                    OnInvalidEntry(line, invalidEntryKind.Value);
                }

                ReleaseTrackingLine releaseTrackingLine;
                if (currentRuleEntryKind.Value == ReleaseTrackingRuleEntryKind.Changed)
                {
                    invalidEntryKind = TryParseFields(parts, categoryIndex: 3, severityIndex: 4,
                        out var oldCategory, out var oldDefaultSeverity, out var oldEnabledByDefault);
                    if (invalidEntryKind.HasValue)
                    {
                        OnInvalidEntry(line, invalidEntryKind.Value);
                    }

                    // Verify at least one field is changed for the entry:
                    if (string.Equals(category, oldCategory, StringComparison.OrdinalIgnoreCase) &&
                        defaultSeverity == oldDefaultSeverity &&
                        enabledByDefault == oldEnabledByDefault)
                    {
                        OnInvalidEntry(line, InvalidEntryKind.Other);
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
                if (hasExistingEntry)
                {
                    onDuplicateEntryInRelease(ruleId, currentVersion, path, sourceText, line);
                }
            }

            var builder = ImmutableSortedDictionary.CreateBuilder<string, ReleaseTrackingDataForRule>();
            foreach (var (ruleId, value) in releaseTrackingDataByRulesBuilder)
            {
                var releaseTrackingDataForRule = new ReleaseTrackingDataForRule(ruleId, value);
                builder.Add(ruleId, releaseTrackingDataForRule);
            }

            return new ReleaseTrackingData(builder.ToImmutable(), [.. versionsBuilder]);

            // Local functions
            void OnInvalidEntry(TextLine line, InvalidEntryKind invalidEntryKind)
                => onInvalidEntry(line, invalidEntryKind, path, sourceText);

            static bool IsInvalidEntry(string[] parts, ReleaseTrackingRuleEntryKind currentRuleEntryKind)
            {
                // Expected entry for New or Removed rules has 3 or 4 parts:
                //      "Rule ID | Category | Severity | Notes"
                //
                // Expected entry for Changed rules has 5 or 6 parts:
                //      "Rule ID | New Category | New Severity | Old Category | Old Severity | Notes"
                //
                // NOTE: Last field 'Helplink' is optional for both cases.

                if (parts.Length is < 3 or > 6)
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
    }

    internal enum InvalidEntryKind
    {
        Header,
        UndetectedField,
        Other
    }

#pragma warning disable CA1815 // Override equals and operator equals on value types
    internal sealed class ReleaseTrackingData
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public static readonly ReleaseTrackingData Default = new();
        public ImmutableSortedDictionary<string, ReleaseTrackingDataForRule> ReleaseTrackingDataByRuleIdMap { get; }
        public ImmutableHashSet<Version> Versions { get; }

        private ReleaseTrackingData()
            : this(ImmutableSortedDictionary<string, ReleaseTrackingDataForRule>.Empty, ImmutableHashSet<Version>.Empty)
        {
        }

        internal ReleaseTrackingData(ImmutableSortedDictionary<string, ReleaseTrackingDataForRule> releaseTrackingDataByRuleIdMap, ImmutableHashSet<Version> versions)
        {
            ReleaseTrackingDataByRuleIdMap = releaseTrackingDataByRuleIdMap;
            Versions = versions;
        }

        public bool TryGetLatestReleaseTrackingLine(
            string ruleId,
            [NotNullWhen(returnValue: true)] out Version? version,
            [NotNullWhen(returnValue: true)] out ReleaseTrackingLine? releaseTrackingLine)
        => TryGetLatestReleaseTrackingLineCore(ruleId, maxVersion: null, out version, out releaseTrackingLine);

        public bool TryGetLatestReleaseTrackingLine(
            string ruleId,
            Version maxVersion,
            [NotNullWhen(returnValue: true)] out Version? version,
            [NotNullWhen(returnValue: true)] out ReleaseTrackingLine? releaseTrackingLine)
        => TryGetLatestReleaseTrackingLineCore(ruleId, maxVersion, out version, out releaseTrackingLine);

        private bool TryGetLatestReleaseTrackingLineCore(
            string ruleId,
            Version? maxVersion,
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

            foreach (var (releaseVersion, releaseLine) in releaseTrackingDataForRule.ReleasesByVersionMap)
            {
                if (maxVersion == null || releaseVersion <= maxVersion)
                {
                    version = releaseVersion;
                    releaseTrackingLine = releaseLine;
                    return true;
                }
            }

            return false;
        }
    }

#pragma warning disable CA1815 // Override equals and operator equals on value types
    internal readonly struct ReleaseTrackingDataForRule
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

    internal sealed class ReleaseTrackingDataForRuleBuilder
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

            public int Compare([AllowNull] Version x, [AllowNull] Version y)
            {
                return (x?.CompareTo(y)).GetValueOrDefault() * -1;
            }
        }
    }

    internal abstract class ReleaseTrackingLine
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

    internal sealed class NewOrRemovedRuleReleaseTrackingLine : ReleaseTrackingLine
    {
        internal NewOrRemovedRuleReleaseTrackingLine(
            string ruleId, string category, bool? enabledByDefault,
            DiagnosticSeverity? defaultSeverity,
            TextSpan span, SourceText sourceText,
            string path, bool isShipped, ReleaseTrackingRuleEntryKind kind)
            : base(ruleId, category, enabledByDefault, defaultSeverity, span, sourceText, path, isShipped, kind)
        {
            Debug.Assert(kind is ReleaseTrackingRuleEntryKind.New or ReleaseTrackingRuleEntryKind.Removed);
        }
    }

    internal sealed class ChangedRuleReleaseTrackingLine : ReleaseTrackingLine
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

    internal enum ReleaseTrackingHeaderKind
    {
        ReleaseHeader,
        TableHeaderTitle,
        TableHeaderNewOrRemovedRulesLine1,
        TableHeaderNewOrRemovedRulesLine2,
        TableHeaderChangedRulesLine1,
        TableHeaderChangedRulesLine2,
    }

    internal enum ReleaseTrackingRuleEntryKind
    {
        New,
        Removed,
        Changed,
    }
}
