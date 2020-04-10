// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class TestAnalyzerReferenceByLanguage : AnalyzerReference
    {
        private readonly ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> _analyzersMap;

        public TestAnalyzerReferenceByLanguage(ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzersMap)
            => _analyzersMap = analyzersMap;

        public override string FullPath
        {
            get
            {
                return null;
            }
        }

        public override string Display
        {
            get
            {
                return nameof(TestAnalyzerReferenceByLanguage);
            }
        }

        public override object Id
        {
            get
            {
                return Display;
            }
        }

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
    }
}
