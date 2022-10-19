// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static partial class AnalyzerOptionsExtensions
    {
        public static OptionSet GetAnalyzerOptionSet(this AnalyzerOptions analyzerOptions, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            var optionSetAsync = GetAnalyzerOptionSetAsync(analyzerOptions, syntaxTree, cancellationToken);
            if (optionSetAsync.IsCompleted)
                return optionSetAsync.Result;

            return optionSetAsync.AsTask().GetAwaiter().GetResult();
        }

        public static async ValueTask<OptionSet> GetAnalyzerOptionSetAsync(this AnalyzerOptions analyzerOptions, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            var configOptions = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
#pragma warning disable CS0612 // Type or member is obsolete
            var optionSet = await GetDocumentOptionSetAsync(analyzerOptions, syntaxTree, cancellationToken).ConfigureAwait(false);
#pragma warning restore CS0612 // Type or member is obsolete

            return new AnalyzerConfigOptionSet(configOptions, optionSet);
        }

        public static T GetOption<T>(this AnalyzerOptions analyzerOptions, ILanguageSpecificOption<T> option, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            var optionAsync = GetOptionAsync<T>(analyzerOptions, option, language: null, syntaxTree, cancellationToken);
            if (optionAsync.IsCompleted)
                return optionAsync.Result;

            return optionAsync.AsTask().GetAwaiter().GetResult();
        }

        public static T GetOption<T>(this AnalyzerOptions analyzerOptions, IPerLanguageOption<T> option, string? language, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            var optionAsync = GetOptionAsync<T>(analyzerOptions, option, language, syntaxTree, cancellationToken);
            if (optionAsync.IsCompleted)
                return optionAsync.Result;

            return optionAsync.AsTask().GetAwaiter().GetResult();
        }

        public static async ValueTask<T> GetOptionAsync<T>(this AnalyzerOptions analyzerOptions, IOption option, string? language, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            if (analyzerOptions.TryGetEditorConfigOption<T>(option, syntaxTree, out var value))
            {
                return value;
            }

#pragma warning disable CS0612 // Type or member is obsolete
            var optionSet = await analyzerOptions.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).ConfigureAwait(false);
#pragma warning restore CS0612 // Type or member is obsolete

            if (optionSet != null)
            {
                value = optionSet.GetOption<T>(new OptionKey(option, language));
            }

            return value ?? (T)option.DefaultValue!;
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", OftenCompletesSynchronously = true)]
        public static ValueTask<OptionSet?> GetDocumentOptionSetAsync(this AnalyzerOptions analyzerOptions, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            if (analyzerOptions is not WorkspaceAnalyzerOptions workspaceAnalyzerOptions)
            {
                return ValueTaskFactory.FromResult((OptionSet?)null);
            }

            return workspaceAnalyzerOptions.GetDocumentOptionSetAsync(syntaxTree, cancellationToken);
        }
    }
}
