// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class TestAnalyzerReferenceByLanguage : AnalyzerReference
    {
        private readonly IReadOnlyDictionary<string, ImmutableArray<DiagnosticAnalyzer>> _analyzersMap;

        public TestAnalyzerReferenceByLanguage(IReadOnlyDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzersMap, string? fullPath = null)
        {
            _analyzersMap = analyzersMap;
            FullPath = fullPath;
        }

        public override string? FullPath { get; }
        public override string Display => nameof(TestAnalyzerReferenceByLanguage);
        public override object Id => Display;

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
            => _analyzersMap.SelectMany(kvp => kvp.Value).ToImmutableArray();

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
        {
            if (_analyzersMap.TryGetValue(language, out var analyzers))
            {
                return analyzers;
            }

            return ImmutableArray<DiagnosticAnalyzer>.Empty;
        }

        public TestAnalyzerReferenceByLanguage WithAdditionalAnalyzers(string language, IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            var newAnalyzersMap = ImmutableDictionary.CreateRange(
                _analyzersMap.Select(kvp => new KeyValuePair<string, ImmutableArray<DiagnosticAnalyzer>>(
                    kvp.Key, kvp.Key == language ? kvp.Value.AddRange(analyzers) : kvp.Value)));
            return new(newAnalyzersMap);
        }
    }
}
