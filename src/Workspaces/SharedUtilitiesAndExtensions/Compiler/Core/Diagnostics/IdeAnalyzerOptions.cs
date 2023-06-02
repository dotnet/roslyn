// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// IDE specific options available to analyzers in a specific project (language).
/// </summary>
[DataContract]
internal sealed record class IdeAnalyzerOptions
{
    private static readonly CodeStyleOption2<bool> s_defaultPreferSystemHashCode =
        new(value: true, notification: NotificationOption2.Suggestion);

    public static readonly IdeAnalyzerOptions CommonDefault = new();

    [DataMember] public bool CrashOnAnalyzerException { get; init; } = false;
    [DataMember] public bool ReportInvalidPlaceholdersInStringDotFormatCalls { get; init; } = true;
    [DataMember] public bool ReportInvalidRegexPatterns { get; init; } = true;
    [DataMember] public bool ReportInvalidJsonPatterns { get; init; } = true;
    [DataMember] public bool DetectAndOfferEditorFeaturesForProbableJsonStrings { get; init; } = true;
    [DataMember] public CodeStyleOption2<bool> PreferSystemHashCode { get; init; } = s_defaultPreferSystemHashCode;

    /// <summary>
    /// Default values for <see cref="CleanCodeGenerationOptions"/>, or null if not available (the project language does not support these options).
    /// </summary>
    [DataMember] public CleanCodeGenerationOptions? CleanCodeGenerationOptions { get; init; } = null;

    /// <summary>
    /// Default values for <see cref="IdeCodeStyleOptions"/>, or null if not available (the project language does not support these options).
    /// </summary>
    [DataMember] public IdeCodeStyleOptions? CodeStyleOptions { get; init; } = null;

    public CodeCleanupOptions? CleanupOptions => CleanCodeGenerationOptions?.CleanupOptions;
    public CodeGenerationOptions? GenerationOptions => CleanCodeGenerationOptions?.GenerationOptions;

#if !CODE_STYLE
    public static IdeAnalyzerOptions GetDefault(LanguageServices languageServices)
        => new()
        {
            CleanCodeGenerationOptions = CodeGeneration.CleanCodeGenerationOptions.GetDefault(languageServices),
            CodeStyleOptions = IdeCodeStyleOptions.GetDefault(languageServices),
        };
#endif
}
