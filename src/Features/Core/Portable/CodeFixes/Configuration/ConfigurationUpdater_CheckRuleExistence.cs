// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeFixes.Configuration;

internal sealed partial class ConfigurationUpdater
{
    public enum ConfigKind
    {
        None,
        All,
        Category,
        Id,
    }

    // hack: mostly copied from CheckIfRuleExistsAndReplaceInFile
    public static async Task<ConfigKind> CheckIfConfigExistsAsync(Project project, string diagnosticId, string diagnosticCategory, CancellationToken cancellationToken)
    {
        var codeStyleOptionNames = GetCodeStyleOptionValuesForDiagnostic(diagnosticId, project).Select(t => t.optionName).ToImmutableHashSet();
        var bestFoundConfigKind = ConfigKind.None;

        foreach (var analyzerConfigDocument in project.AnalyzerConfigDocuments)
        {
            var text = await analyzerConfigDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            foreach (var curLine in text.Lines)
            {
                var curLineText = curLine.ToString();
                if (s_optionEntryPattern.IsMatch(curLineText))
                {
                    var groups = s_optionEntryPattern.Match(curLineText).Groups;

                    // Regex groups:
                    //  1. Option key
                    //  2. Option value
                    //  3. Optional severity suffix, i.e. ':severity' suffix
                    //  4. Optional comment suffix
                    var untrimmedKey = groups[1].Value.ToString();
                    var key = untrimmedKey.Trim();
                    var value = groups[2].Value.ToString();
                    var severitySuffixInValue = groups[3].Value.ToString();
                    var commentValue = groups[4].Value.ToString();

                    // We found the rule in the file -- replace it with updated option value/severity.
                    if (codeStyleOptionNames.Contains(key))
                    {
                        // We found an option configuration entry of form:
                        //      "%option_name% = %option_value%
                        //          OR
                        //      "%option_name% = %option_value%:%severity%
                        return ConfigKind.Id;
                    }

                    // We found a rule configuration entry of severity based form:
                    //      "dotnet_diagnostic.<%DiagnosticId%>.severity = %severity%
                    //              OR
                    //      "dotnet_analyzer_diagnostic.severity = %severity%
                    //              OR
                    //      "dotnet_analyzer_diagnostic.category-<%DiagnosticCategory%>.severity = %severity%
                    if (severitySuffixInValue.Length == 0 && key.EndsWith(SeveritySuffix))
                    {

                        if (key.StartsWith(DiagnosticOptionPrefix, StringComparison.Ordinal))
                        {
                            var diagIdLength = key.Length - (DiagnosticOptionPrefix.Length + SeveritySuffix.Length);
                            if (diagIdLength > 0)
                            {
                                var diagId = key.Substring(DiagnosticOptionPrefix.Length, diagIdLength);
                                if (string.Equals(diagId, diagnosticId, StringComparison.OrdinalIgnoreCase))
                                {
                                    return ConfigKind.Id;
                                }
                            }
                        }

                        if (key == BulkConfigureAllAnalyzerDiagnosticsOptionKey && bestFoundConfigKind < ConfigKind.All)
                            bestFoundConfigKind = ConfigKind.All;

                        if (key.StartsWith(BulkConfigureAnalyzerDiagnosticsByCategoryOptionPrefix, StringComparison.Ordinal))
                        {
                            var categoryLength = key.Length - (BulkConfigureAnalyzerDiagnosticsByCategoryOptionPrefix.Length + SeveritySuffix.Length);
                            var category = key.Substring(BulkConfigureAnalyzerDiagnosticsByCategoryOptionPrefix.Length, categoryLength);
                            if (string.Equals(category, diagnosticCategory, StringComparison.OrdinalIgnoreCase) && bestFoundConfigKind < ConfigKind.Category)
                                bestFoundConfigKind = ConfigKind.Category;
                        }
                    }
                }


            }
        }

        return bestFoundConfigKind;
    }
}

