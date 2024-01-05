﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Classification;

internal static class ClassificationOptionsStorage
{
    public static ClassificationOptions GetClassificationOptions(this IGlobalOptionService globalOptions, string language)
        => new()
        {
            ClassifyReassignedVariables = globalOptions.GetOption(ClassifyReassignedVariables, language),
            ColorizeRegexPatterns = globalOptions.GetOption(ColorizeRegexPatterns, language),
            ColorizeJsonPatterns = globalOptions.GetOption(ColorizeJsonPatterns, language),
            // ForceFrozenPartialSemanticsForCrossProcessOperations not stored in global options
        };

    public static PerLanguageOption2<bool> ClassifyReassignedVariables =
        new("dotnet_classify_reassigned_variables", ClassificationOptions.Default.ClassifyReassignedVariables);

    public static PerLanguageOption2<bool> ColorizeRegexPatterns =
        new("dotnet_colorize_regex_patterns", ClassificationOptions.Default.ColorizeRegexPatterns);

    public static PerLanguageOption2<bool> ColorizeJsonPatterns =
        new("dotnet_colorize_json_patterns", ClassificationOptions.Default.ColorizeJsonPatterns);
}
