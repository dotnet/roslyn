// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options;

internal static partial class EditorConfigFileGenerator
{
    public static string Generate(
        IEnumerable<(string feature, ImmutableArray<IOption2> options)> groupedOptions,
        IOptionsReader configOptions,
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
            AppendOptionsToEditorConfig(configOptions, feature, options, language, editorconfig);
        }

        if (configOptions.TryGetOption(new OptionKey2(NamingStyleOptions.NamingPreferences, language), out NamingStylePreferences namingStylePreferences))
        {
            AppendNamingStylePreferencesToEditorConfig(namingStylePreferences, language, editorconfig);
        }

        return editorconfig.ToString();
    }

    private static void AppendOptionsToEditorConfig(IOptionsReader configOptions, string feature, ImmutableArray<IOption2> options, string language, StringBuilder editorconfig)
    {
        editorconfig.AppendLine($"#### {feature} ####");
        editorconfig.AppendLine();

        foreach (var optionGrouping in options.GroupBy(o => o.Definition.Group).OrderBy(g => g.Key.Priority))
        {
            editorconfig.AppendLine($"# {optionGrouping.Key.Description}");

            var uniqueEntries = new SortedSet<string>();
            foreach (var option in optionGrouping)
            {
                var optionKey = new OptionKey2(option, option.IsPerLanguage ? language : null);
                if (configOptions.TryGetOption<object?>(optionKey, out var value))
                {
                    uniqueEntries.Add($"{option.Definition.ConfigName} = {option.Definition.Serializer.Serialize(value)}");
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
