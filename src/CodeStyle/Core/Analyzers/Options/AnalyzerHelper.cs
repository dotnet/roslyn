// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using Microsoft.CodeAnalysis.Internal.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static partial class AnalyzerHelper
    {
        public static T GetOption<T>(this AnalyzerOptions analyzerOptions, IOption option, string? language, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            if (analyzerOptions.TryGetEditorConfigOption<T>(option, syntaxTree, out var value))
            {
                return value;
            }

            return (T)option.DefaultValue!;
        }

        public static T GetOption<T>(this AnalyzerOptions analyzerOptions, Option<T> option, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            return GetOption<T>(analyzerOptions, option, language: null, syntaxTree, cancellationToken);
        }

        public static T GetOption<T>(this AnalyzerOptions analyzerOptions, PerLanguageOption<T> option, string? language, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            return GetOption<T>(analyzerOptions, (IOption)option, language, syntaxTree, cancellationToken);
        }
    }
}
