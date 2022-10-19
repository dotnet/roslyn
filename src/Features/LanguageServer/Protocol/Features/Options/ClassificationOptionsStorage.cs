// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Classification
{
    internal static class ClassificationOptionsStorage
    {
        public static ClassificationOptions GetClassificationOptions(this IGlobalOptionService globalOptions, string language)
            => new(
                ClassifyReassignedVariables: globalOptions.GetOption(ClassifyReassignedVariables, language),
                ColorizeRegexPatterns: globalOptions.GetOption(ColorizeRegexPatterns, language),
                ColorizeJsonPatterns: globalOptions.GetOption(ColorizeJsonPatterns, language));

        public static PerLanguageOption2<bool> ClassifyReassignedVariables =
            new("ClassificationOptions", "ClassifyReassignedVariables", ClassificationOptions.Default.ClassifyReassignedVariables,
                storageLocation: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.ClassificationOptions.ClassifyReassignedVariables"));

        public static PerLanguageOption2<bool> ColorizeRegexPatterns =
            new("RegularExpressionsOptions", "ColorizeRegexPatterns", ClassificationOptions.Default.ColorizeRegexPatterns,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ColorizeRegexPatterns"));

        public static PerLanguageOption2<bool> ColorizeJsonPatterns =
            new("JsonFeatureOptions", "ColorizeJsonPatterns", ClassificationOptions.Default.ColorizeJsonPatterns,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ColorizeJsonPatterns"));
    }
}
