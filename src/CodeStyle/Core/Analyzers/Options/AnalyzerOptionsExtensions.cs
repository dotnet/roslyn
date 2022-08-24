// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static partial class AnalyzerOptionsExtensions
    {
#pragma warning disable IDE0060 // Remove unused parameter - Needed to share this method signature between CodeStyle and Features layer.
        public static T GetOption<T>(this AnalyzerOptions analyzerOptions, IOption2 option, string? language, SyntaxTree syntaxTree, CancellationToken cancellationToken)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            if (analyzerOptions.TryGetEditorConfigOption<T>(option, syntaxTree, out var value))
            {
                return value;
            }

            return (T)option.DefaultValue!;
        }

        public static T GetOption<T>(this AnalyzerOptions analyzerOptions, Option2<T> option, SyntaxTree syntaxTree, CancellationToken cancellationToken)
            => GetOption<T>(analyzerOptions, option, language: null, syntaxTree, cancellationToken);

        public static T GetOption<T>(this AnalyzerOptions analyzerOptions, PerLanguageOption2<T> option, string? language, SyntaxTree syntaxTree, CancellationToken cancellationToken)
            => GetOption<T>(analyzerOptions, (IOption2)option, language, syntaxTree, cancellationToken);

#pragma warning disable IDE0060 // Remove unused parameter - Needed to share this method signature between CodeStyle and Features layer.
        public static AnalyzerConfigOptions GetAnalyzerOptionSet(this AnalyzerOptions analyzerOptions, SyntaxTree syntaxTree, CancellationToken cancellationToken)
#pragma warning restore IDE0060 // Remove unused parameter
            => analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
    }
}
