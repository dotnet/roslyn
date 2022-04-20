// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

#if CODE_STYLE
using TOption = Microsoft.CodeAnalysis.Options.IOption2;
#else
using TOption = Microsoft.CodeAnalysis.Options.IOption;
#endif

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [DataContract]
    internal readonly record struct IdeAnalyzerOptions(
        [property: DataMember(Order = 0)] bool CrashOnAnalyzerException = true,
        [property: DataMember(Order = 1)] bool FadeOutUnusedImports = true,
        [property: DataMember(Order = 2)] bool FadeOutUnreachableCode = true,
        [property: DataMember(Order = 3)] bool ReportInvalidPlaceholdersInStringDotFormatCalls = true,
        [property: DataMember(Order = 4)] bool ReportInvalidRegexPatterns = true,
        [property: DataMember(Order = 5)] bool ReportInvalidJsonPatterns = true,
        [property: DataMember(Order = 6)] bool DetectAndOfferEditorFeaturesForProbableJsonStrings = true,
        [property: DataMember(Order = 7)] SimplifierOptions? SimplifierOptions = null)
    {
        public IdeAnalyzerOptions()
            : this(CrashOnAnalyzerException: false)
        {
        }

        public static readonly IdeAnalyzerOptions Default = new();

        public static readonly IdeAnalyzerOptions CodeStyleDefault = new(
            CrashOnAnalyzerException: false,
            FadeOutUnusedImports: false,
            FadeOutUnreachableCode: false,
            ReportInvalidPlaceholdersInStringDotFormatCalls: true,
            ReportInvalidRegexPatterns: true,
            ReportInvalidJsonPatterns: true,
            DetectAndOfferEditorFeaturesForProbableJsonStrings: true);
    }

    internal static partial class AnalyzerOptionsExtensions
    {
        public static IdeAnalyzerOptions GetIdeOptions(this AnalyzerOptions options)
#if CODE_STYLE
            => IdeAnalyzerOptions.CodeStyleDefault;
#else
            => (options is WorkspaceAnalyzerOptions workspaceOptions) ? workspaceOptions.IdeOptions : IdeAnalyzerOptions.CodeStyleDefault;
#endif

        public static T GetOption<T>(this SemanticModelAnalysisContext context, Option2<T> option)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.SemanticModel.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            return analyzerOptions.GetOption(option, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this SyntaxNodeAnalysisContext context, Option2<T> option)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Node.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            return analyzerOptions.GetOption(option, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this SyntaxTreeAnalysisContext context, Option2<T> option)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Tree;
            var cancellationToken = context.CancellationToken;

            return analyzerOptions.GetOption(option, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this OperationAnalysisContext context, Option2<T> option)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Operation.Syntax.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            return analyzerOptions.GetOption(option, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this SemanticModelAnalysisContext context, PerLanguageOption2<T> option, string? language)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.SemanticModel.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            return analyzerOptions.GetOption(option, language, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this SyntaxNodeAnalysisContext context, PerLanguageOption2<T> option, string? language)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Node.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            return analyzerOptions.GetOption(option, language, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this SyntaxTreeAnalysisContext context, PerLanguageOption2<T> option, string? language)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Tree;
            var cancellationToken = context.CancellationToken;

            return analyzerOptions.GetOption(option, language, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this OperationAnalysisContext context, PerLanguageOption2<T> option, string? language)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Operation.Syntax.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            return analyzerOptions.GetOption(option, language, syntaxTree, cancellationToken);
        }

        public static bool TryGetEditorConfigOption<T>(this AnalyzerOptions analyzerOptions, TOption option, SyntaxTree syntaxTree, [MaybeNullWhen(false)] out T value)
        {
            var configOptions = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
            return configOptions.TryGetEditorConfigOption(option, out value);
        }
    }
}
