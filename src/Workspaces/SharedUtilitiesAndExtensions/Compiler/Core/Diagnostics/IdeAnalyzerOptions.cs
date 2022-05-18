// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// IDE specific options available to analyzers in a specific project (language).
/// </summary>
[DataContract]
internal sealed record class IdeAnalyzerOptions
{
    public static readonly CodeStyleOption2<bool> DefaultPreferSystemHashCode = new(value: true, notification: NotificationOption2.Suggestion);

    public static readonly IdeAnalyzerOptions CodeStyleDefault = new()
    {
        CrashOnAnalyzerException = false,
        FadeOutUnusedImports = false,
        FadeOutUnreachableCode = false
    };

    public static readonly IdeAnalyzerOptions CommonDefault = new();

    [DataMember(Order = 0)] public bool CrashOnAnalyzerException { get; init; } = false;
    [DataMember(Order = 1)] public bool FadeOutUnusedImports { get; init; } = true;
    [DataMember(Order = 2)] public bool FadeOutUnreachableCode { get; init; } = true;
    [DataMember(Order = 3)] public bool FadeOutComplexObjectInitialization { get; init; } = false;
    [DataMember(Order = 4)] public bool FadeOutComplexCollectionInitialization { get; init; } = false;
    [DataMember(Order = 5)] public bool ReportInvalidPlaceholdersInStringDotFormatCalls { get; init; } = true;
    [DataMember(Order = 6)] public bool ReportInvalidRegexPatterns { get; init; } = true;
    [DataMember(Order = 7)] public bool ReportInvalidJsonPatterns { get; init; } = true;
    [DataMember(Order = 8)] public bool DetectAndOfferEditorFeaturesForProbableJsonStrings { get; init; } = true;
    [DataMember(Order = 9)] public CodeStyleOption2<bool> PreferSystemHashCode { get; init; } = DefaultPreferSystemHashCode;

    /// <summary>
    /// Default values for <see cref="CleanCodeGenerationOptions"/>, or null if not available (the project language does not support these options).
    /// </summary>
    [DataMember(Order = 10)] public CleanCodeGenerationOptions? CleanCodeGenerationOptions { get; init; } = null;

    /// <summary>
    /// Default values for <see cref="IdeCodeStyleOptions"/>, or null if not available (the project language does not support these options).
    /// </summary>
    [DataMember(Order = 11)] public IdeCodeStyleOptions? CodeStyleOptions { get; init; } = null;

    public CodeCleanupOptions? CleanupOptions => CleanCodeGenerationOptions?.CleanupOptions;
    public CodeGenerationOptions? GenerationOptions => CleanCodeGenerationOptions?.GenerationOptions;

#if !CODE_STYLE
    public static IdeAnalyzerOptions GetDefault(HostLanguageServices languageServices)
        => new() { CleanCodeGenerationOptions = CodeGeneration.CleanCodeGenerationOptions.GetDefault(languageServices) };
#endif
}
