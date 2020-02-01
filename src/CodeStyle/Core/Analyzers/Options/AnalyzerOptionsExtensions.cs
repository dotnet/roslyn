// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static partial class AnalyzerOptionsExtensions
    {
        public static AnalyzerConfigOptions GetOptions(this AnalyzerOptions analyzerOptions, SyntaxTree syntaxTree, CancellationToken _)
            => analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
    }
}
