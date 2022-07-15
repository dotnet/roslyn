// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CodeGeneration;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class IdeAnalyzerOptionsStorage
{
    public static IdeAnalyzerOptions GetIdeAnalyzerOptions(this IGlobalOptionService globalOptions, Project project)
        => GetIdeAnalyzerOptions(globalOptions, project.LanguageServices);

    public static IdeAnalyzerOptions GetIdeAnalyzerOptions(this IGlobalOptionService globalOptions, HostLanguageServices languageServices)
    {
        var language = languageServices.Language;
        var supportsLanguageSpecificOptions = languageServices.GetService<ISyntaxFormattingOptionsStorage>() != null;

        return new()
        {
            CrashOnAnalyzerException = globalOptions.GetOption(CrashOnAnalyzerException),
            FadeOutUnusedImports = globalOptions.GetOption(FadeOutUnusedImports, language),
            FadeOutUnreachableCode = globalOptions.GetOption(FadeOutUnreachableCode, language),
            FadeOutComplexObjectInitialization = globalOptions.GetOption(FadeOutComplexObjectInitialization, language),
            FadeOutComplexCollectionInitialization = globalOptions.GetOption(FadeOutComplexCollectionInitialization, language),
            ReportInvalidPlaceholdersInStringDotFormatCalls = globalOptions.GetOption(ReportInvalidPlaceholdersInStringDotFormatCalls, language),
            ReportInvalidRegexPatterns = globalOptions.GetOption(ReportInvalidRegexPatterns, language),
            ReportInvalidJsonPatterns = globalOptions.GetOption(ReportInvalidJsonPatterns, language),
            DetectAndOfferEditorFeaturesForProbableJsonStrings = globalOptions.GetOption(DetectAndOfferEditorFeaturesForProbableJsonStrings, language),
            PreferSystemHashCode = globalOptions.GetOption(CodeStyleOptions2.PreferSystemHashCode, language),
            CleanCodeGenerationOptions = supportsLanguageSpecificOptions ? globalOptions.GetCleanCodeGenerationOptions(languageServices) : null,
            CodeStyleOptions = supportsLanguageSpecificOptions ? globalOptions.GetCodeStyleOptions(languageServices) : null,
        };
    }

    public static readonly Option2<bool> CrashOnAnalyzerException = new(
        "InternalDiagnosticsOptions", "CrashOnAnalyzerException", IdeAnalyzerOptions.CommonDefault.CrashOnAnalyzerException,
        storageLocation: new LocalUserProfileStorageLocation(@"Roslyn\Internal\Diagnostics\CrashOnAnalyzerException"));

    public static readonly PerLanguageOption2<bool> FadeOutUnusedImports = new(
        "FadingOptions", "FadeOutUnusedImports", IdeAnalyzerOptions.CommonDefault.FadeOutUnusedImports,
        storageLocation: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.FadeOutUnusedImports"));

    public static readonly PerLanguageOption2<bool> FadeOutUnreachableCode = new(
        "FadingOptions", "FadeOutUnreachableCode", IdeAnalyzerOptions.CommonDefault.FadeOutUnreachableCode,
        storageLocation: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.FadeOutUnreachableCode"));

    public static readonly PerLanguageOption2<bool> FadeOutComplexObjectInitialization = new(
        "CodeStyleOptions", "PreferObjectInitializer_FadeOutCode", IdeAnalyzerOptions.CommonDefault.FadeOutComplexObjectInitialization,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferObjectInitializer_FadeOutCode"));

    internal static readonly PerLanguageOption2<bool> FadeOutComplexCollectionInitialization = new(
        "CodeStyleOptions", "PreferCollectionInitializer_FadeOutCode", IdeAnalyzerOptions.CommonDefault.FadeOutComplexCollectionInitialization,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferCollectionInitializer_FadeOutCode"));

    public static PerLanguageOption2<bool> ReportInvalidPlaceholdersInStringDotFormatCalls =
        new("ValidateFormatStringOption",
            "ReportInvalidPlaceholdersInStringDotFormatCalls",
            IdeAnalyzerOptions.CommonDefault.ReportInvalidPlaceholdersInStringDotFormatCalls,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.WarnOnInvalidStringDotFormatCalls"));

    public static PerLanguageOption2<bool> ReportInvalidRegexPatterns =
        new("RegularExpressionsOptions",
            "ReportInvalidRegexPatterns",
            IdeAnalyzerOptions.CommonDefault.ReportInvalidRegexPatterns,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ReportInvalidRegexPatterns"));

    public static PerLanguageOption2<bool> ReportInvalidJsonPatterns =
        new("JsonFeatureOptions",
            "ReportInvalidJsonPatterns",
            defaultValue: IdeAnalyzerOptions.CommonDefault.ReportInvalidJsonPatterns,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ReportInvalidJsonPatterns"));

    public static PerLanguageOption2<bool> DetectAndOfferEditorFeaturesForProbableJsonStrings =
        new("JsonFeatureOptions",
            "DetectAndOfferEditorFeaturesForProbableJsonStrings",
            defaultValue: IdeAnalyzerOptions.CommonDefault.DetectAndOfferEditorFeaturesForProbableJsonStrings,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.DetectAndOfferEditorFeaturesForProbableJsonStrings"));
}
