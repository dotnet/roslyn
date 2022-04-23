// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// IDE specific options available to analyzers in a specific project (language).
/// </summary>
/// <param name="CleanupOptions">Default values for <see cref="CodeCleanupOptions"/>, or null if not available (the project language does not support these options).</param>
[DataContract]
internal sealed record class IdeAnalyzerOptions(
    [property: DataMember(Order = 0)] bool CrashOnAnalyzerException = IdeAnalyzerOptions.DefaultCrashOnAnalyzerException,
    [property: DataMember(Order = 1)] bool FadeOutUnusedImports = IdeAnalyzerOptions.DefaultFadeOutUnusedImports,
    [property: DataMember(Order = 2)] bool FadeOutUnreachableCode = IdeAnalyzerOptions.DefaultFadeOutUnreachableCode,
    [property: DataMember(Order = 3)] bool FadeOutComplexObjectInitialization = IdeAnalyzerOptions.DefaultFadeOutComplexObjectInitialization,
    [property: DataMember(Order = 4)] bool FadeOutComplexCollectiontInitialization = IdeAnalyzerOptions.DefaultFadeOutComplexCollectionInitialization,
    [property: DataMember(Order = 5)] bool ReportInvalidPlaceholdersInStringDotFormatCalls = IdeAnalyzerOptions.DefaultReportInvalidPlaceholdersInStringDotFormatCalls,
    [property: DataMember(Order = 6)] bool ReportInvalidRegexPatterns = IdeAnalyzerOptions.DefaultReportInvalidRegexPatterns,
    [property: DataMember(Order = 7)] bool ReportInvalidJsonPatterns = IdeAnalyzerOptions.DefaultReportInvalidJsonPatterns,
    [property: DataMember(Order = 8)] bool DetectAndOfferEditorFeaturesForProbableJsonStrings = IdeAnalyzerOptions.DefaultDetectAndOfferEditorFeaturesForProbableJsonStrings,
    [property: DataMember(Order = 9)] CodeCleanupOptions? CleanupOptions = null,
    [property: DataMember(Order = 10)] IdeCodeStyleOptions? CodeStyleOptions = null)
{
    public const bool DefaultCrashOnAnalyzerException = false;
    public const bool DefaultFadeOutUnusedImports = true;
    public const bool DefaultFadeOutUnreachableCode = true;
    public const bool DefaultFadeOutComplexObjectInitialization = false;
    public const bool DefaultFadeOutComplexCollectionInitialization = false;
    public const bool DefaultReportInvalidPlaceholdersInStringDotFormatCalls = true;
    public const bool DefaultReportInvalidRegexPatterns = true;
    public const bool DefaultReportInvalidJsonPatterns = true;
    public const bool DefaultDetectAndOfferEditorFeaturesForProbableJsonStrings = true;

    public static readonly IdeAnalyzerOptions CodeStyleDefault = new(
        CrashOnAnalyzerException: false,
        FadeOutUnusedImports: false,
        FadeOutUnreachableCode: false);

#if !CODE_STYLE
    public static IdeAnalyzerOptions GetDefault(HostLanguageServices languageServices)
        => new(CleanupOptions: CodeCleanupOptions.GetDefault(languageServices));
#endif
}
