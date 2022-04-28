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
