// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Options
{
    internal static partial class EditorConfigFileGenerator
    {
        public static string Generate(
            ImmutableArray<(string feature, ImmutableArray<IOption2> options)> groupedOptions,
            StructuredAnalyzerConfigOptions configOptions,
            string language)
        {
            var editorconfig = new StringBuilder();

            editorconfig.AppendLine($"# {WorkspacesResources.Remove_the_line_below_if_you_want_to_inherit_dot_editorconfig_settings_from_higher_directories}");
            editorconfig.AppendLine("root = true");
            editorconfig.AppendLine();

            if (language == LanguageNames.CSharp)
            {
                editorconfig.AppendLine($"# {WorkspacesResources.CSharp_files}");
                editorconfig.AppendLine("[*.cs]");
            }
            else if (language == LanguageNames.VisualBasic)
            {
                editorconfig.AppendLine($"# {WorkspacesResources.Visual_Basic_files}");
                editorconfig.AppendLine("[*.vb]");
            }

            editorconfig.AppendLine();

            foreach ((var feature, var options) in groupedOptions)
            {
                AppendOptionsToEditorConfig(configOptions, feature, options, editorconfig);
            }

            var namingStylePreferences = configOptions.GetNamingStylePreferences();
            AppendNamingStylePreferencesToEditorConfig(namingStylePreferences, language, editorconfig);

            return editorconfig.ToString();
        }

        private static void AppendOptionsToEditorConfig(StructuredAnalyzerConfigOptions configOptions, string feature, ImmutableArray<IOption2> options, StringBuilder editorconfig)
        {
            editorconfig.AppendLine($"#### {feature} ####");
            editorconfig.AppendLine();

            foreach (var optionGrouping in options
                                           .Where(o => o.StorageLocations.Any(static l => l is IEditorConfigStorageLocation2))
                                           .GroupBy(o => (o as IOptionWithGroup)?.Group ?? OptionGroup.Default)
                                           .OrderBy(g => g.Key.Priority))
            {
                editorconfig.AppendLine($"# {optionGrouping.Key.Description}");

                var uniqueEntries = new SortedSet<string>();
                foreach (var option in optionGrouping)
                {
                    var configName = option.OptionDefinition.ConfigName;
                    if (configOptions.TryGetValue(configName, out var configValue))
                    {
                        uniqueEntries.Add($"{configName} = {configValue}");
                    }
                }

                foreach (var entry in uniqueEntries)
                {
                    editorconfig.AppendLine(entry);
                }

                editorconfig.AppendLine();
            }
        }
    }
}
