// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class EditorConfigFileGenerator
    {
        public static string Generate(
            ImmutableArray<(string feature, ImmutableArray<IOption> options)> groupedOptions,
            OptionSet optionSet,
            string language)
        {
            var editorconfig = new StringBuilder();

            editorconfig.AppendLine($"# {WorkspacesResources.Comment_the_line_below_if_you_want_to_inherit_parent_dot_editorconfig_settings}");
            editorconfig.AppendLine("root = true");
            editorconfig.AppendLine();

            if (language == LanguageNames.CSharp)
            {
                editorconfig.AppendLine($"# {WorkspacesResources.CSharp_files}");
                editorconfig.AppendLine("[*.cs]");
            }
            else if (language == LanguageNames.VisualBasic)
            {
                editorconfig.AppendLine($"# {WorkspacesResources.VB_files}");
                editorconfig.AppendLine("[*.vb]");
            }
            editorconfig.AppendLine();

            foreach ((var feature, var options) in groupedOptions)
            {
                AppendOptionsToEditorConfig(optionSet, feature, options, language, editorconfig);
            }

            return editorconfig.ToString();
        }

        private static void AppendOptionsToEditorConfig(OptionSet optionSet, string feature, ImmutableArray<IOption> options, string language, StringBuilder editorconfig)
        {
            editorconfig.AppendLine($"#### {feature} ####");
            editorconfig.AppendLine();

            var processedKeys = new HashSet<string>();
            foreach (var optionGrouping in options
                                           .Where(o => o.StorageLocations.Any(l => l is IEditorConfigStorageLocationWithKey))
                                           .GroupBy(o => (o as IOptionWithOptionGroup)?.Group ?? OptionGroup.Default)
                                           .OrderBy(g => g.Key.Priority))
            {
                editorconfig.AppendLine($"# {optionGrouping.Key.Description}");

                var optionsAndEditorConfigLocations = optionGrouping.Select(o => (o, o.StorageLocations.OfType<IEditorConfigStorageLocationWithKey>().First()));
                foreach ((var option, var editorConfigLocation) in optionsAndEditorConfigLocations.OrderBy(e => e.Item2.KeyName))
                {
                    if (processedKeys.Add(editorConfigLocation.KeyName))
                    {
                        var editorConfigStringForValue = GetEditorConfigStringForValue(option, editorConfigLocation);
                        editorconfig.AppendLine($"{editorConfigLocation.KeyName} = {editorConfigStringForValue}");
                    }
                }

                editorconfig.AppendLine();
            }

            string GetEditorConfigStringForValue(IOption option, IEditorConfigStorageLocationWithKey editorConfigLocation)
            {
                var optionKey = new OptionKey(option, option.IsPerLanguage ? language : null);
                var value = optionSet.GetOption(optionKey);
                var editorConfigStringForValue = editorConfigLocation.GetEditorConfigStringForValue(value, optionSet);
                Debug.Assert(!string.IsNullOrEmpty(editorConfigStringForValue));
                return editorConfigStringForValue;
            }
        }
    }
}
