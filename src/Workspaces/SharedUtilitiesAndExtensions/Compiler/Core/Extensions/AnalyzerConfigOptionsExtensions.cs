// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis;

internal static class AnalyzerConfigOptionsExtensions
{
    public static T GetEditorConfigOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, IOption2 option, T defaultValue)
        => TryGetEditorConfigOption<T>(analyzerConfigOptions, option, out var value) ? value : defaultValue;

    public static T GetEditorConfigOptionValue<T>(this AnalyzerConfigOptions analyzerConfigOptions, IOption2 option, T defaultValue)
        => TryGetEditorConfigOption<CodeStyleOption2<T>>(analyzerConfigOptions, option, out var style) ? style.Value : defaultValue;

    public static bool TryGetEditorConfigOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, IOption2 option, out T value)
    {
        Contract.ThrowIfFalse(option.Definition.IsEditorConfigOption);

        if (option.Definition.Type == typeof(NamingStylePreferences))
        {
            if (StructuredAnalyzerConfigOptions.TryGetStructuredOptions(analyzerConfigOptions, out var structuredOptions))
            {
                var preferences = structuredOptions.GetNamingStylePreferences();
                value = (T)(object)preferences;
                return !preferences.IsEmpty;
            }
        }
        else
        {
            if (analyzerConfigOptions.TryGetValue(option.Definition.ConfigName, out var stringValue))
            {
                // Avoid boxing when reading typed value:
                if (typeof(T) != typeof(object))
                {
                    return ((OptionDefinition<T>)option.Definition).Serializer.TryParseValue(stringValue, out value!);
                }

                if (option.Definition.Serializer.TryParse(stringValue, out var objectValue))
                {
                    value = (T)objectValue!;
                    return true;
                }
            }
        }

        value = default!;
        return false;
    }

    public static bool IsCodeStyleSeverityEnabled(this AnalyzerConfigOptions analyzerConfigOptions)
    {
        const string EnableCodeStyleSeverityKey = "build_property.EnableCodeStyleSeverity";

        return analyzerConfigOptions.TryGetValue(EnableCodeStyleSeverityKey, out var value)
            && bool.TryParse(value, out var parsedValue)
            && parsedValue;
    }

    public static bool IsAnalysisLevelGreaterThanOrEquals(this AnalyzerConfigOptions analyzerConfigOptions, int minAnalysisLevel)
    {
        // See https://github.com/dotnet/roslyn/pull/70794 for details.
        const string AnalysisLevelKey = "build_property.EffectiveAnalysisLevelStyle";

        return analyzerConfigOptions.TryGetValue(AnalysisLevelKey, out var value)
            && double.TryParse(value, out var version)
            && version >= minAnalysisLevel;
    }
}
