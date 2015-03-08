// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    // put anything test related here
    internal partial class DiagnosticAnalyzerService
    {
        // Internal for testing purposes.
        internal DiagnosticAnalyzerService(string language, DiagnosticAnalyzer analyzer, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null)
            : this(language, ImmutableArray.Create(analyzer), hostDiagnosticUpdateSource)
        {
        }

        // Internal for testing purposes.
        internal DiagnosticAnalyzerService(string language, ImmutableArray<DiagnosticAnalyzer> analyzers, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null)
            : this(ImmutableDictionary.CreateRange(
                SpecializedCollections.SingletonEnumerable(KeyValuePair.Create(language, analyzers))), hostDiagnosticUpdateSource)
        {
        }

        // Internal for testing purposes.
        internal DiagnosticAnalyzerService(ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzersMap, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null)
            : this(ImmutableArray.Create<AnalyzerReference>(new TestAnalyzerReferenceByLanguage(analyzersMap)), hostDiagnosticUpdateSource)
        {
        }

        // Internal for testing purposes.
        internal DiagnosticAnalyzerService(AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null)
           : this(workspaceAnalyzerAssemblies: SpecializedCollections.EmptyEnumerable<string>(), hostDiagnosticUpdateSource: hostDiagnosticUpdateSource)
        {
        }

        private class TestAnalyzerReferenceByLanguage : AnalyzerReference
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
}
