// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class TestAnalyzerReferenceByLanguage : AnalyzerReference
    {
        private readonly ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> _analyzersMap;

        public TestAnalyzerReferenceByLanguage(ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzersMap)
        {
            _analyzersMap = analyzersMap;
        }

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
        {
            return _analyzersMap.SelectMany(kvp => kvp.Value).ToImmutableArray();
        }

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
        {
            ImmutableArray<DiagnosticAnalyzer> analyzers;
            if (_analyzersMap.TryGetValue(language, out analyzers))
            {
                return analyzers;
            }

            return ImmutableArray<DiagnosticAnalyzer>.Empty;
        }
    }
}
