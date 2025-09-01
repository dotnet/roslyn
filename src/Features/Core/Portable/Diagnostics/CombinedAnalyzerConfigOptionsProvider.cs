// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class AnalyzerOptionsUtilities
{
    /// <summary>
    /// Combines two <see cref="AnalyzerOptions"/> instances into one.  The resulting instance will have the
    /// options merged from both.  Options defined in <paramref name="projectAnalyzerOptions"/> ("EditorConfig options")
    /// will take precedence over those in <paramref name="hostAnalyzerOptions"/> (VS UI options).
    /// </summary>
    public static AnalyzerOptions Combine(AnalyzerOptions projectAnalyzerOptions, AnalyzerOptions hostAnalyzerOptions)
    {
        return new AnalyzerOptions(
            projectAnalyzerOptions.AdditionalFiles.AddRange(hostAnalyzerOptions.AdditionalFiles).Distinct(),
            new CombinedAnalyzerConfigOptionsProvider(projectAnalyzerOptions, hostAnalyzerOptions));
    }

    private sealed class CombinedAnalyzerConfigOptionsProvider(
        AnalyzerOptions projectAnalyzerOptions,
        AnalyzerOptions hostAnalyzerOptions) : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerOptions _analyzerOptions = projectAnalyzerOptions;
        private readonly AnalyzerOptions _hostAnalyzerOptions = hostAnalyzerOptions;

        public override AnalyzerConfigOptions GlobalOptions
            => new CombinedAnalyzerConfigOptions(
                _analyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions,
                _hostAnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
            => new CombinedAnalyzerConfigOptions(
                _analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree),
                _hostAnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree));

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
            => new CombinedAnalyzerConfigOptions(
                _analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(textFile),
                _hostAnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(textFile));

        private sealed class CombinedAnalyzerConfigOptions(
            AnalyzerConfigOptions projectOptions,
            AnalyzerConfigOptions hostOptions) : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
                // Lookup in project options first.  Editor config should override the values from the host.
                => projectOptions.TryGetValue(key, out value) || hostOptions.TryGetValue(key, out value);

            public override IEnumerable<string> Keys
                => projectOptions.Keys.Concat(hostOptions.Keys).Distinct();
        }
    }
}
