// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed class CombinedAnalyzerConfigOptionsProvider(AnalyzerOptions analyzerOptions, AnalyzerOptions hostAnalyzerOptions) : AnalyzerConfigOptionsProvider
{
    private readonly AnalyzerOptions _analyzerOptions = analyzerOptions;
    private readonly AnalyzerOptions _hostAnalyzerOptions = hostAnalyzerOptions;

    public static AnalyzerOptions Combine(AnalyzerOptions analyzerOptions, AnalyzerOptions hostAnalyzerOptions)
    {
        return new AnalyzerOptions(
            analyzerOptions.AdditionalFiles.AddRange(hostAnalyzerOptions.AdditionalFiles).Distinct(),
            new CombinedAnalyzerConfigOptionsProvider(analyzerOptions, hostAnalyzerOptions));
    }

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
        AnalyzerConfigOptions globalOptions1,
        AnalyzerConfigOptions globalOptions2) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            => globalOptions1.TryGetValue(key, out value) || globalOptions2.TryGetValue(key, out value);
    }
}
