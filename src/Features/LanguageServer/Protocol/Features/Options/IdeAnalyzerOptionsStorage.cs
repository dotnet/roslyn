// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class IdeAnalyzerOptionsStorage
{
    public static IdeAnalyzerOptions GetIdeAnalyzerOptions(this IGlobalOptionService globalOptions, Project project)
        => GetIdeAnalyzerOptions(globalOptions, project.LanguageServices);

    public static IdeAnalyzerOptions GetIdeAnalyzerOptions(this IGlobalOptionService globalOptions, HostLanguageServices languageServices)
    {
        var language = languageServices.Language;
        var supportsCleanupOptions = languageServices.GetService<ISyntaxFormattingOptionsStorage>() != null;

        return new(
            CrashOnAnalyzerException: globalOptions.GetOption(CrashOnAnalyzerException),
            FadeOutUnusedImports: globalOptions.GetOption(FadeOutUnusedImports, language),
            FadeOutUnreachableCode: globalOptions.GetOption(FadeOutUnreachableCode, language),
            ReportInvalidPlaceholdersInStringDotFormatCalls: globalOptions.GetOption(ReportInvalidPlaceholdersInStringDotFormatCalls, language),
            ReportInvalidRegexPatterns: globalOptions.GetOption(ReportInvalidRegexPatterns, language),
            ReportInvalidJsonPatterns: globalOptions.GetOption(ReportInvalidJsonPatterns, language),
            DetectAndOfferEditorFeaturesForProbableJsonStrings: globalOptions.GetOption(DetectAndOfferEditorFeaturesForProbableJsonStrings, language),
            CleanupOptions: supportsCleanupOptions ? globalOptions.GetCodeCleanupOptions(languageServices) : null);
    }

    public static readonly Option2<bool> CrashOnAnalyzerException = new(
        "InternalDiagnosticsOptions", "CrashOnAnalyzerException", IdeAnalyzerOptions.DefaultCrashOnAnalyzerException,
        storageLocation: new LocalUserProfileStorageLocation(@"Roslyn\Internal\Diagnostics\CrashOnAnalyzerException"));

    public static readonly PerLanguageOption2<bool> FadeOutUnusedImports = new(
        "FadingOptions", "FadeOutUnusedImports", IdeAnalyzerOptions.DefaultFadeOutUnusedImports,
        storageLocation: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.FadeOutUnusedImports"));

    public static readonly PerLanguageOption2<bool> FadeOutUnreachableCode = new(
        "FadingOptions", "FadeOutUnreachableCode", IdeAnalyzerOptions.DefaultFadeOutUnreachableCode,
        storageLocation: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.FadeOutUnreachableCode"));

    public static PerLanguageOption2<bool> ReportInvalidPlaceholdersInStringDotFormatCalls =
        new("ValidateFormatStringOption",
            "ReportInvalidPlaceholdersInStringDotFormatCalls",
            IdeAnalyzerOptions.DefaultReportInvalidPlaceholdersInStringDotFormatCalls,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.WarnOnInvalidStringDotFormatCalls"));

    public static PerLanguageOption2<bool> ReportInvalidRegexPatterns =
        new("RegularExpressionsOptions",
            "ReportInvalidRegexPatterns",
            IdeAnalyzerOptions.DefaultReportInvalidRegexPatterns,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ReportInvalidRegexPatterns"));

    public static PerLanguageOption2<bool> ReportInvalidJsonPatterns =
        new("JsonFeatureOptions",
            "ReportInvalidJsonPatterns",
            defaultValue: IdeAnalyzerOptions.DefaultReportInvalidJsonPatterns,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ReportInvalidJsonPatterns"));

    public static PerLanguageOption2<bool> DetectAndOfferEditorFeaturesForProbableJsonStrings =
        new("JsonFeatureOptions",
            "DetectAndOfferEditorFeaturesForProbableJsonStrings",
            defaultValue: IdeAnalyzerOptions.DefaultDetectAndOfferEditorFeaturesForProbableJsonStrings,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.DetectAndOfferEditorFeaturesForProbableJsonStrings"));
}
