// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics;

// TODO: move to LSP layer
internal static class IdeAnalyzerOptionsStorage
{
    public static IdeAnalyzerOptions GetIdeAnalyzerOptions(this IGlobalOptionService globalOptions, string language)
        => new(
            CrashOnAnalyzerException: globalOptions.GetOption(CrashOnAnalyzerException),
            FadeOutUnusedImports: globalOptions.GetOption(FadeOutUnusedImports, language),
            FadeOutUnreachableCode: globalOptions.GetOption(FadeOutUnreachableCode, language),
            ReportInvalidPlaceholdersInStringDotFormatCalls: globalOptions.GetOption(ReportInvalidPlaceholdersInStringDotFormatCalls, language),
            ReportInvalidRegexPatterns: globalOptions.GetOption(ReportInvalidRegexPatterns, language),
            ReportInvalidJsonPatterns: globalOptions.GetOption(ReportInvalidJsonPatterns, language),
            DetectAndOfferEditorFeaturesForProbableJsonStrings: globalOptions.GetOption(DetectAndOfferEditorFeaturesForProbableJsonStrings, language));

    // for testing only
    internal static void SetIdeAnalyzerOptions(this IGlobalOptionService globalOptions, string language, IdeAnalyzerOptions options)
    {
        globalOptions.SetGlobalOption(new OptionKey((IOption)CrashOnAnalyzerException), options.CrashOnAnalyzerException);
        globalOptions.SetGlobalOption(new OptionKey((IOption)FadeOutUnusedImports, language), options.FadeOutUnusedImports);
        globalOptions.SetGlobalOption(new OptionKey((IOption)FadeOutUnreachableCode, language), options.FadeOutUnreachableCode);
        globalOptions.SetGlobalOption(new OptionKey((IOption)ReportInvalidPlaceholdersInStringDotFormatCalls, language), options.ReportInvalidPlaceholdersInStringDotFormatCalls);
        globalOptions.SetGlobalOption(new OptionKey((IOption)ReportInvalidRegexPatterns, language), options.ReportInvalidRegexPatterns);
        globalOptions.SetGlobalOption(new OptionKey((IOption)ReportInvalidJsonPatterns, language), options.ReportInvalidJsonPatterns);
        globalOptions.SetGlobalOption(new OptionKey((IOption)DetectAndOfferEditorFeaturesForProbableJsonStrings, language), options.DetectAndOfferEditorFeaturesForProbableJsonStrings);
    }

    public static readonly Option2<bool> CrashOnAnalyzerException = new(
        "InternalDiagnosticsOptions", "CrashOnAnalyzerException", IdeAnalyzerOptions.Default.CrashOnAnalyzerException,
        storageLocation: new LocalUserProfileStorageLocation(@"Roslyn\Internal\Diagnostics\CrashOnAnalyzerException"));

    public static readonly PerLanguageOption2<bool> FadeOutUnusedImports = new(
        "FadingOptions", "FadeOutUnusedImports", IdeAnalyzerOptions.Default.FadeOutUnusedImports,
        storageLocation: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.FadeOutUnusedImports"));

    public static readonly PerLanguageOption2<bool> FadeOutUnreachableCode = new(
        "FadingOptions", "FadeOutUnreachableCode", IdeAnalyzerOptions.Default.FadeOutUnreachableCode,
        storageLocation: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.FadeOutUnreachableCode"));

    public static PerLanguageOption2<bool> ReportInvalidPlaceholdersInStringDotFormatCalls =
        new("ValidateFormatStringOption",
            "ReportInvalidPlaceholdersInStringDotFormatCalls",
            IdeAnalyzerOptions.Default.ReportInvalidPlaceholdersInStringDotFormatCalls,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.WarnOnInvalidStringDotFormatCalls"));

    public static PerLanguageOption2<bool> ReportInvalidRegexPatterns =
        new("RegularExpressionsOptions",
            "ReportInvalidRegexPatterns",
            IdeAnalyzerOptions.Default.ReportInvalidRegexPatterns,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ReportInvalidRegexPatterns"));

    public static PerLanguageOption2<bool> ReportInvalidJsonPatterns =
        new("JsonFeatureOptions",
            "ReportInvalidJsonPatterns",
            defaultValue: IdeAnalyzerOptions.Default.ReportInvalidJsonPatterns,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ReportInvalidJsonPatterns"));

    public static PerLanguageOption2<bool> DetectAndOfferEditorFeaturesForProbableJsonStrings =
        new("JsonFeatureOptions",
            "DetectAndOfferEditorFeaturesForProbableJsonStrings",
            defaultValue: IdeAnalyzerOptions.Default.DetectAndOfferEditorFeaturesForProbableJsonStrings,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.DetectAndOfferEditorFeaturesForProbableJsonStrings"));
}
