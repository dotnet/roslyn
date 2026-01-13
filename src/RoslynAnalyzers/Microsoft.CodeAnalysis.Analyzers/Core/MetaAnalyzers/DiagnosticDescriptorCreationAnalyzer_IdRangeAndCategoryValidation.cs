// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    using static CodeAnalysisDiagnosticsResources;

    /// <summary>
    /// RS1018 <inheritdoc cref="DiagnosticIdMustBeInSpecifiedFormatTitle"/>
    /// RS1020 <inheritdoc cref="UseCategoriesFromSpecifiedRangeTitle"/>
    /// RS1021 <inheritdoc cref="AnalyzerCategoryAndIdRangeFileInvalidTitle"/>
    /// </summary>
    public sealed partial class DiagnosticDescriptorCreationAnalyzer
    {
        private const string DiagnosticCategoryAndIdRangeFile = "DiagnosticCategoryAndIdRanges.txt";
        private static readonly (string? prefix, int start, int end) s_defaultAllowedIdsInfo = (null, -1, -1);

        public static readonly DiagnosticDescriptor DiagnosticIdMustBeInSpecifiedFormatRule = new(
            DiagnosticIds.DiagnosticIdMustBeInSpecifiedFormatRuleId,
            CreateLocalizableResourceString(nameof(DiagnosticIdMustBeInSpecifiedFormatTitle)),
            CreateLocalizableResourceString(nameof(DiagnosticIdMustBeInSpecifiedFormatMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DiagnosticIdMustBeInSpecifiedFormatDescription)),
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor UseCategoriesFromSpecifiedRangeRule = new(
            DiagnosticIds.UseCategoriesFromSpecifiedRangeRuleId,
            CreateLocalizableResourceString(nameof(UseCategoriesFromSpecifiedRangeTitle)),
            CreateLocalizableResourceString(nameof(UseCategoriesFromSpecifiedRangeMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: CreateLocalizableResourceString(nameof(UseCategoriesFromSpecifiedRangeDescription)),
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor AnalyzerCategoryAndIdRangeFileInvalidRule = new(
            DiagnosticIds.AnalyzerCategoryAndIdRangeFileInvalidRuleId,
            CreateLocalizableResourceString(nameof(AnalyzerCategoryAndIdRangeFileInvalidTitle)),
            CreateLocalizableResourceString(nameof(AnalyzerCategoryAndIdRangeFileInvalidMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(AnalyzerCategoryAndIdRangeFileInvalidDescription)),
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        private static void AnalyzeAllowedIdsInfoList(
            string ruleId,
            IArgumentOperation argument,
            AdditionalText? additionalText,
            string? category,
            ImmutableArray<(string? prefix, int start, int end)> allowedIdsInfoList,
            Action<Diagnostic> addDiagnostic)
        {
            RoslynDebug.Assert(!allowedIdsInfoList.IsDefaultOrEmpty);
            RoslynDebug.Assert(category != null);
            RoslynDebug.Assert(additionalText != null);

            var foundMatch = false;
            static bool ShouldValidateRange((string? prefix, int start, int end) range)
                => range is { start: >= 0, end: >= 0 };

            // Check if ID matches any one of the required ranges.
            foreach (var allowedIds in allowedIdsInfoList)
            {
                RoslynDebug.Assert(allowedIds.prefix != null);

                if (ruleId.StartsWith(allowedIds.prefix, StringComparison.Ordinal))
                {
                    if (ShouldValidateRange(allowedIds))
                    {
                        var suffix = ruleId[allowedIds.prefix.Length..];
                        if (int.TryParse(suffix, out int ruleIdInt) &&
                            ruleIdInt >= allowedIds.start &&
                            ruleIdInt <= allowedIds.end)
                        {
                            foundMatch = true;
                            break;
                        }
                    }
                    else
                    {
                        foundMatch = true;
                        break;
                    }
                }
            }

            if (!foundMatch)
            {
                // Diagnostic Id '{0}' belonging to category '{1}' is not in the required range and/or format '{2}' specified in the file '{3}'.
                string arg1 = ruleId;
                string arg2 = category;
                var arg3 = new StringBuilder();
                foreach (var range in allowedIdsInfoList)
                {
                    if (arg3.Length != 0)
                    {
                        arg3.Append(", ");
                    }

                    if (ShouldValidateRange(range))
                    {
                        arg3.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}-{0}{2}", range.prefix, range.start, range.end);
                    }
                    else
                    {
                        arg3.AppendFormat(CultureInfo.InvariantCulture, "{0}XXXX", range.prefix);
                    }
                }

                string arg4 = Path.GetFileName(additionalText.Path);
                var diagnostic = argument.Value.CreateDiagnostic(DiagnosticIdMustBeInSpecifiedFormatRule, arg1, arg2, arg3.ToString(), arg4);
                addDiagnostic(diagnostic);
            }
        }

        private static bool TryAnalyzeCategory(
            OperationAnalysisContext operationAnalysisContext,
            ImmutableArray<IArgumentOperation> creationArguments,
            bool checkCategoryAndAllowedIds,
            AdditionalText? additionalText,
            ImmutableDictionary<string, ImmutableArray<(string? prefix, int start, int end)>>? categoryAndAllowedIdsInfoMap,
            [NotNullWhen(returnValue: true)] out string? category,
            out ImmutableArray<(string? prefix, int start, int end)> allowedIdsInfoList)
        {
            category = null;
            allowedIdsInfoList = default;
            foreach (var argument in creationArguments)
            {
                if (argument.Parameter?.Name.Equals(CategoryParameterName, StringComparison.Ordinal) == true)
                {
                    // Check if the category argument is a constant or refers to a string field.
                    if (argument.Value.ConstantValue.HasValue)
                    {
                        if (argument.Value.Type != null &&
                            argument.Value.Type.SpecialType == SpecialType.System_String &&
                            argument.Value.ConstantValue.Value is string value)
                        {
                            category = value;
                        }
                    }
                    else if (argument.Value is IFieldReferenceOperation fieldReference &&
                        fieldReference.Field.Type.SpecialType == SpecialType.System_String)
                    {
                        category = fieldReference.ConstantValue.HasValue && fieldReference.ConstantValue.Value is string value ? value : fieldReference.Field.Name;
                    }

                    if (!checkCategoryAndAllowedIds)
                    {
                        return category != null;
                    }

                    // Check if the category is one of the allowed values.
                    RoslynDebug.Assert(categoryAndAllowedIdsInfoMap != null);
                    RoslynDebug.Assert(additionalText != null);

                    if (category != null &&
                        categoryAndAllowedIdsInfoMap.TryGetValue(category, out allowedIdsInfoList))
                    {
                        return true;
                    }

                    // Category '{0}' is not from the allowed categories specified in the file '{1}'.
                    string arg1 = category ?? "<unknown>";
                    string arg2 = Path.GetFileName(additionalText.Path);
                    var diagnostic = argument.Value.CreateDiagnostic(UseCategoriesFromSpecifiedRangeRule, arg1, arg2);
                    operationAnalysisContext.ReportDiagnostic(diagnostic);
                    return false;
                }
            }

            return false;
        }

        private static bool TryGetCategoryAndAllowedIdsMap(
            ImmutableArray<AdditionalText> additionalFiles,
            CancellationToken cancellationToken,
            [NotNullWhen(returnValue: true)] out AdditionalText? additionalText,
            [NotNullWhen(returnValue: true)] out ImmutableDictionary<string, ImmutableArray<(string? prefix, int start, int end)>>? categoryAndAllowedIdsMap,
            out List<Diagnostic>? invalidFileDiagnostics)
        {
            invalidFileDiagnostics = null;
            categoryAndAllowedIdsMap = null;

            // Parse the additional file with allowed diagnostic categories and corresponding ID range.
            // Bail out if there is no such additional file or it contains at least one invalid entry.
            additionalText = TryGetCategoryAndAllowedIdsInfoFile(additionalFiles, cancellationToken);
            return additionalText != null &&
                TryParseCategoryAndAllowedIdsInfoFile(additionalText, cancellationToken, out categoryAndAllowedIdsMap, out invalidFileDiagnostics);
        }

        private static AdditionalText? TryGetCategoryAndAllowedIdsInfoFile(ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken)
        {
            StringComparer comparer = StringComparer.Ordinal;
            foreach (AdditionalText textFile in additionalFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string fileName = Path.GetFileName(textFile.Path);
                if (comparer.Equals(fileName, DiagnosticCategoryAndIdRangeFile))
                {
                    return textFile;
                }
            }

            return null;
        }

        private static bool TryParseCategoryAndAllowedIdsInfoFile(
            AdditionalText additionalText,
            CancellationToken cancellationToken,
            [NotNullWhen(returnValue: true)] out ImmutableDictionary<string, ImmutableArray<(string? prefix, int start, int end)>>? categoryAndAllowedIdsInfoMap,
            out List<Diagnostic>? invalidFileDiagnostics)
        {
            // Parse the additional file with allowed diagnostic categories and corresponding ID range.
            // FORMAT:
            // 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'

            categoryAndAllowedIdsInfoMap = null;
            invalidFileDiagnostics = null;

            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<(string? prefix, int start, int end)>>();
            var lines = additionalText.GetTextOrEmpty(cancellationToken).Lines;
            foreach (var line in lines)
            {
                var contents = line.ToString();
                if (contents.Length == 0 || contents.StartsWith("#", StringComparison.Ordinal))
                {
                    // Ignore empty lines and comments.
                    continue;
                }

                var parts = contents.Split(':');
                for (int i = 0; i < parts.Length; i++)
                {
                    parts[i] = parts[i].Trim();
                }

                var isInvalidLine = false;
                string category = parts[0];
                if (parts.Length > 2 ||                 // We allow only 0 or 1 ':' separator in the line.
                    category.Any(char.IsWhiteSpace) ||  // We do not allow white spaces in category name.
                    builder.ContainsKey(category))      // We do not allow multiple lines with same category.
                {
                    isInvalidLine = true;
                }
                else
                {
                    if (parts.Length == 1)
                    {
                        // No ':' symbol, so the entry just specifies the category.
                        builder.Add(category, default);
                        continue;
                    }

                    // Entry with the following possible formats:
                    // 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'
                    var ranges = parts[1].Split(',');

                    var infoList = ImmutableArray.CreateBuilder<(string? prefix, int start, int end)>(ranges.Length);
                    for (int i = 0; i < ranges.Length; i++)
                    {
                        (string? prefix, int start, int end) allowedIdsInfo = s_defaultAllowedIdsInfo;
                        string range = ranges[i].Trim();
                        if (!range.Contains('-'))
                        {
                            if (TryParseIdRangeEntry(range, out string prefix, out int start))
                            {
                                // Specific Id validation.
                                allowedIdsInfo.prefix = prefix;
                                allowedIdsInfo.start = start;
                                allowedIdsInfo.end = start;
                            }
                            else if (range.All(char.IsLetter))
                            {
                                // Only prefix validation.
                                allowedIdsInfo.prefix = range;
                            }
                            else
                            {
                                isInvalidLine = true;
                                break;
                            }
                        }
                        else
                        {
                            // Prefix and start-end range validation.
                            var rangeParts = range.Split('-');
                            if (TryParseIdRangeEntry(rangeParts[0], out string prefix1, out int start) &&
                                TryParseIdRangeEntry(rangeParts[1], out string prefix2, out int end) &&
                                prefix1.Equals(prefix2, StringComparison.Ordinal))
                            {
                                allowedIdsInfo.prefix = prefix1;
                                allowedIdsInfo.start = start;
                                allowedIdsInfo.end = end;
                            }
                            else
                            {
                                isInvalidLine = true;
                                break;
                            }
                        }

                        infoList.Add(allowedIdsInfo);
                    }

                    if (!isInvalidLine)
                    {
                        builder.Add(category, infoList.ToImmutable());
                    }
                }

                if (isInvalidLine)
                {
                    // Invalid entry '{0}' in analyzer category and diagnostic ID range specification file '{1}'.
                    string arg1 = contents;
                    string arg2 = Path.GetFileName(additionalText.Path);
                    LinePositionSpan linePositionSpan = lines.GetLinePositionSpan(line.Span);
                    Location location = Location.Create(additionalText.Path, line.Span, linePositionSpan);
                    invalidFileDiagnostics ??= [];
                    var diagnostic = Diagnostic.Create(AnalyzerCategoryAndIdRangeFileInvalidRule, location, arg1, arg2);
                    invalidFileDiagnostics.Add(diagnostic);
                }
            }

            categoryAndAllowedIdsInfoMap = builder.ToImmutable();
            return invalidFileDiagnostics == null;
        }

        private static bool TryParseIdRangeEntry(string entry, out string prefix, out int suffix)
        {
            // Parse an entry for diagnostic ID.
            // We require diagnostic ID to have an alphabetical prefix followed by a numerical suffix.
            var prefixBuilder = new StringBuilder();
            suffix = -1;
            var suffixStr = new StringBuilder();
            bool seenDigit = false;
            foreach (char ch in entry)
            {
                bool isDigit = char.IsDigit(ch);
                if (seenDigit && !isDigit)
                {
                    prefix = prefixBuilder.ToString();
                    return false;
                }

                if (isDigit)
                {
                    suffixStr.Append(ch);
                    seenDigit = true;
                }
                else if (!char.IsLetter(ch))
                {
                    prefix = prefixBuilder.ToString();
                    return false;
                }
                else
                {
                    prefixBuilder.Append(ch);
                }
            }

            prefix = prefixBuilder.ToString();
            return prefix.Length > 0 && int.TryParse(suffixStr.ToString(), out suffix);
        }
    }
}
