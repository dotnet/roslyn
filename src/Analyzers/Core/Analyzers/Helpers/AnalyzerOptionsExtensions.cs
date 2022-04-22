// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

#if CODE_STYLE
using TOption = Microsoft.CodeAnalysis.Options.IOption2;
#else
using TOption = Microsoft.CodeAnalysis.Options.IOption;
#endif

#if CODE_STYLE
namespace Microsoft.CodeAnalysis.CodeCleanup
{
    internal readonly struct CodeCleanupOptions
    {
    }
}
#endif

namespace Microsoft.CodeAnalysis.Diagnostics
{
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

    internal static partial class AnalyzerOptionsExtensions
    {
        public static IdeAnalyzerOptions GetIdeOptions(this AnalyzerOptions options)
#if CODE_STYLE
            => IdeAnalyzerOptions.CodeStyleDefault;
#else
            => (options is WorkspaceAnalyzerOptions workspaceOptions) ? workspaceOptions.IdeOptions : IdeAnalyzerOptions.CodeStyleDefault;
#endif

        public static SyntaxFormattingOptions GetSyntaxFormattingOptions(this AnalyzerOptions options, SyntaxTree tree, ISyntaxFormatting formatting)
        {
#if CODE_STYLE
            var fallbackOptions = (SyntaxFormattingOptions?)null;
#else
            var fallbackOptions = options.GetIdeOptions().CleanupOptions?.FormattingOptions;
#endif
            return formatting.GetFormattingOptions(options.AnalyzerConfigOptionsProvider.GetOptions(tree), fallbackOptions);
        }

        public static bool TryGetEditorConfigOption<T>(this AnalyzerOptions analyzerOptions, TOption option, SyntaxTree syntaxTree, [MaybeNullWhen(false)] out T value)
        {
            var configOptions = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
            return configOptions.TryGetEditorConfigOption(option, out value);
        }
    }
}
