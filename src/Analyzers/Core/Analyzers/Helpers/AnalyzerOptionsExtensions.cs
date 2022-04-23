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

namespace Microsoft.CodeAnalysis.Diagnostics
{
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
