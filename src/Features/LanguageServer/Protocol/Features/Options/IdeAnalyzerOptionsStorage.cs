// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class IdeAnalyzerOptionsStorage
{
    public static IdeAnalyzerOptions GetIdeAnalyzerOptions(this IGlobalOptionService globalOptions, Project project)
        => GetIdeAnalyzerOptions(globalOptions, project.Services);

    public static IdeAnalyzerOptions GetIdeAnalyzerOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
    {
        var language = languageServices.Language;

        // avoid throwing for languages other than C# and VB:
        var supportsLanguageSpecificOptions = languageServices.GetService<ISyntaxFormattingService>() != null;

        return new()
        {
            CrashOnAnalyzerException = globalOptions.GetOption(CrashOnAnalyzerException),
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
        "dotnet_crash_on_analyzer_exception", IdeAnalyzerOptions.CommonDefault.CrashOnAnalyzerException);

    public static PerLanguageOption2<bool> ReportInvalidPlaceholdersInStringDotFormatCalls = new(
        "dotnet_report_invalid_placeholders_in_string_dot_format_calls", IdeAnalyzerOptions.CommonDefault.ReportInvalidPlaceholdersInStringDotFormatCalls);

    public static PerLanguageOption2<bool> ReportInvalidRegexPatterns = new(
        "dotnet_report_invalid_regex_patterns", IdeAnalyzerOptions.CommonDefault.ReportInvalidRegexPatterns);

    public static PerLanguageOption2<bool> ReportInvalidJsonPatterns = new(
        "dotnet_report_invalid_json_patterns", IdeAnalyzerOptions.CommonDefault.ReportInvalidJsonPatterns);

    public static PerLanguageOption2<bool> DetectAndOfferEditorFeaturesForProbableJsonStrings = new(
        "dotnet_detect_and_offer_editor_features_for_probable_json_strings", IdeAnalyzerOptions.CommonDefault.DetectAndOfferEditorFeaturesForProbableJsonStrings);
}
