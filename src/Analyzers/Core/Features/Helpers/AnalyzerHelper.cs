// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Options;
#else
using System.Threading;
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static partial class AnalyzerHelper
    {
        public static T GetOption<T>(this SemanticModelAnalysisContext context, Option<T> option)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.SemanticModel.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            return GetOption(analyzerOptions, option, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this SyntaxNodeAnalysisContext context, Option<T> option)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Node.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            return GetOption(analyzerOptions, option, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this SyntaxTreeAnalysisContext context, Option<T> option)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Tree;
            var cancellationToken = context.CancellationToken;

            return GetOption(analyzerOptions, option, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this OperationAnalysisContext context, Option<T> option)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Operation.Syntax.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            return GetOption(analyzerOptions, option, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this SemanticModelAnalysisContext context, PerLanguageOption<T> option, string? language)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.SemanticModel.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            return GetOption(analyzerOptions, option, language, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this SyntaxNodeAnalysisContext context, PerLanguageOption<T> option, string? language)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Node.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            return GetOption(analyzerOptions, option, language, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this SyntaxTreeAnalysisContext context, PerLanguageOption<T> option, string? language)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Tree;
            var cancellationToken = context.CancellationToken;

            return GetOption(analyzerOptions, option, language, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this OperationAnalysisContext context, PerLanguageOption<T> option, string? language)
        {
            var analyzerOptions = context.Options;
            var syntaxTree = context.Operation.Syntax.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            return GetOption(analyzerOptions, option, language, syntaxTree, cancellationToken);
        }

        public static bool TryGetEditorConfigOption<T>(this AnalyzerOptions analyzerOptions, IOption option, SyntaxTree syntaxTree, out T value)
        {
            var configOptions = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
            return configOptions.TryGetEditorConfigOption(option, out value);
        }
    }
}
