// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class AnalyzerOptionsUtilities
{
    /// <summary>
    /// Combines two <see cref="AnalyzerOptions"/> instances into one.   This allows us to package two sets
    /// of options alont (one that only uses editorconfig and one that uses editorconfig, but fallsback to 
    /// tools|options).  See <see cref="GetSpecificOptions"/> for more details.
    /// </summary>
    public static AnalyzerOptions Combine(
        AnalyzerOptions projectAnalyzerOptions,
        AnalyzerOptions hostAnalyzerOptions,
        Func<DiagnosticAnalyzer, AnalyzerOptions> pickAnalyzerOptions)
    {
        return new AnalyzerOptions(
            projectAnalyzerOptions.AdditionalFiles.AddRange(hostAnalyzerOptions.AdditionalFiles).Distinct(),
            new CombinedAnalyzerConfigOptionsProvider(
                projectAnalyzerOptions, hostAnalyzerOptions, pickAnalyzerOptions));
    }

    /// <summary>
    /// Given a generic <see cref="AnalyzerOptions"/> provided during a <see cref="DiagnosticAnalyzer"/> callback,
    /// returns the most specific options we can actually find given the particular <paramref name="diagnosticAnalyzer"/>
    /// we are executing.  This matters when executing a Roslyn-Features analyzer.  This matters if the project being
    /// analyzed references the Roslyn SDK or not.  If it does not, and this is a features-analyzer, then we want 
    /// options loaded from editorconfig files to be used, with a fallback to what's in tools-options if not present.
    /// If it does come from the sdk then we only want to use options from editorconfig, without any fallback to tools|options
    /// (as that's the experience that would happen on the command line).
    /// </summary>
    public static AnalyzerOptions GetSpecificOptions(
        AnalyzerOptions analyzerOptions,
        DiagnosticAnalyzer diagnosticAnalyzer)
    {
        return analyzerOptions.AnalyzerConfigOptionsProvider is CombinedAnalyzerConfigOptionsProvider combinedProvider
            ? combinedProvider.PickOptionsProvider(diagnosticAnalyzer)
            : analyzerOptions;
    }

    private sealed class CombinedAnalyzerConfigOptionsProvider(
        AnalyzerOptions projectAnalyzerOptions,
        AnalyzerOptions hostAnalyzerOptions,
        Func<DiagnosticAnalyzer, AnalyzerOptions> pickOptionsProvider) : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerOptions _projectAnalyzerOptions = projectAnalyzerOptions;
        private readonly AnalyzerOptions _hostAnalyzerOptions = hostAnalyzerOptions;
        public readonly Func<DiagnosticAnalyzer, AnalyzerOptions> PickOptionsProvider = pickOptionsProvider;

        public override AnalyzerConfigOptions GlobalOptions
            => new CombinedAnalyzerConfigOptions(
                _projectAnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions,
                _hostAnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
            => new CombinedAnalyzerConfigOptions(
                _projectAnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree),
                _hostAnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree));

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
            => new CombinedAnalyzerConfigOptions(
                _projectAnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(textFile),
                _hostAnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(textFile));

        private sealed class CombinedAnalyzerConfigOptions(
            AnalyzerConfigOptions projectOptions,
            AnalyzerConfigOptions hostOptions) : StructuredAnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            {
                // In debug builds, we want to make sure that we never accidentally call through this api without
                // calling GetSpecificOptions first.
#if DEBUG
                if (key is "generated_code")
                    return projectOptions.TryGetValue(key, out value) || hostOptions.TryGetValue(key, out value);

                throw new NotImplementedException();
#else
                return projectOptions.TryGetValue(key, out value) || hostOptions.TryGetValue(key, out value);
#endif
            }

            public override IEnumerable<string> Keys
            {
                get
                {
                    // In debug builds, we want to make sure that we never accidentally call through this api without
                    // calling GetSpecificOptions first.
#if DEBUG
                    throw new NotImplementedException();
#else
                    return projectOptions.Keys.Union(hostOptions.Keys);
#endif
                }
            }

            public override NamingStylePreferences GetNamingStylePreferences()
            {
                // In debug builds, we want to make sure that we never accidentally call through this api without
                // calling GetSpecificOptions first.
#if DEBUG
                throw new NotImplementedException();
#else
                var preferences = (projectOptions as StructuredAnalyzerConfigOptions)?.GetNamingStylePreferences();
                if (preferences is { IsEmpty: false })
                    return preferences;

                preferences = (hostOptions as StructuredAnalyzerConfigOptions)?.GetNamingStylePreferences();
                if (preferences is { IsEmpty: false })
                    return preferences;

                return NamingStylePreferences.Empty;
#endif
            }
        }
    }
}
