// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;

#if CODE_STYLE
using TOption = Microsoft.CodeAnalysis.Options.IOption2;
#else
using TOption = Microsoft.CodeAnalysis.Options.IOption;
#endif

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal readonly record struct IdeAnalyzerOptions(
        bool FadeOutUnusedImports = true,
        bool FadeOutUnreachableCode = true,
        bool ReportInvalidPlaceholdersInStringDotFormatCalls = true,
        bool ReportInvalidRegexPatterns = true)
    {
        public IdeAnalyzerOptions()
            : this(FadeOutUnusedImports: true)
        {
        }

        public static readonly IdeAnalyzerOptions Default = new();

        public static readonly IdeAnalyzerOptions CodeStyleDefault = new(
            FadeOutUnusedImports: false,
            FadeOutUnreachableCode: false,
            ReportInvalidPlaceholdersInStringDotFormatCalls: true,
            ReportInvalidRegexPatterns: true);

#if !CODE_STYLE
        public static IdeAnalyzerOptions FromProject(Project project)
            => From(project.Solution.Options, project.Language);

        public static IdeAnalyzerOptions From(OptionSet options, string language)
            => new(
                FadeOutUnusedImports: options.GetOption(Microsoft.CodeAnalysis.Fading.FadingOptions.Metadata.FadeOutUnusedImports, language),
                FadeOutUnreachableCode: options.GetOption(Microsoft.CodeAnalysis.Fading.FadingOptions.Metadata.FadeOutUnreachableCode, language),
                ReportInvalidPlaceholdersInStringDotFormatCalls: options.GetOption(Microsoft.CodeAnalysis.ValidateFormatString.ValidateFormatStringOption.ReportInvalidPlaceholdersInStringDotFormatCalls, language),
                ReportInvalidRegexPatterns: options.GetOption(Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices.RegularExpressionsOptions.ReportInvalidRegexPatterns, language));
#endif
    }

    internal static partial class AnalyzerHelper
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

            return GetOption(analyzerOptions, option, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this SyntaxNodeAnalysisContext context, Option2<T> option)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Node.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            return GetOption(analyzerOptions, option, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this SyntaxTreeAnalysisContext context, Option2<T> option)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Tree;
            var cancellationToken = context.CancellationToken;

            return GetOption(analyzerOptions, option, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this OperationAnalysisContext context, Option2<T> option)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Operation.Syntax.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            return GetOption(analyzerOptions, option, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this SemanticModelAnalysisContext context, PerLanguageOption2<T> option, string? language)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.SemanticModel.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            return GetOption(analyzerOptions, option, language, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this SyntaxNodeAnalysisContext context, PerLanguageOption2<T> option, string? language)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Node.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            return GetOption(analyzerOptions, option, language, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this SyntaxTreeAnalysisContext context, PerLanguageOption2<T> option, string? language)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Tree;
            var cancellationToken = context.CancellationToken;

            return GetOption(analyzerOptions, option, language, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this OperationAnalysisContext context, PerLanguageOption2<T> option, string? language)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Operation.Syntax.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            return GetOption(analyzerOptions, option, language, syntaxTree, cancellationToken);
        }

        public static bool TryGetEditorConfigOption<T>(this AnalyzerOptions analyzerOptions, TOption option, SyntaxTree syntaxTree, [MaybeNullWhen(false)] out T value)
        {
            var configOptions = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
            return configOptions.TryGetEditorConfigOption(option, out value);
        }
    }
}
