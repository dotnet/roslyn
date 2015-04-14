// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class TestDiagnosticAnalyzerService : DiagnosticAnalyzerService
    {
        private readonly Action<Exception, DiagnosticAnalyzer, Diagnostic> _onAnalyzerException;

        internal TestDiagnosticAnalyzerService(string language, DiagnosticAnalyzer analyzer, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
            : this(language, ImmutableArray.Create(analyzer), hostDiagnosticUpdateSource, onAnalyzerException)
        {
        }

        internal TestDiagnosticAnalyzerService(string language, ImmutableArray<DiagnosticAnalyzer> analyzers, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
            : this(ImmutableDictionary.CreateRange(
                SpecializedCollections.SingletonEnumerable(KeyValuePair.Create(language, analyzers))), hostDiagnosticUpdateSource, onAnalyzerException)
        {
        }

        internal TestDiagnosticAnalyzerService(ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzersMap, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
            : base(ImmutableArray.Create<AnalyzerReference>(new TestAnalyzerReferenceByLanguage(analyzersMap)), hostDiagnosticUpdateSource)
        {
            _onAnalyzerException = onAnalyzerException;
        }

        internal TestDiagnosticAnalyzerService(AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
           : base(SpecializedCollections.EmptyEnumerable<HostDiagnosticAnalyzerPackage>(), hostDiagnosticUpdateSource)
        {
            _onAnalyzerException = onAnalyzerException;
        }

        internal TestDiagnosticAnalyzerService(ImmutableArray<AnalyzerReference> workspaceAnalyzers, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
            : base(workspaceAnalyzers, hostDiagnosticUpdateSource)
        {
            _onAnalyzerException = onAnalyzerException;
        }

        internal override Action<Exception, DiagnosticAnalyzer, Diagnostic> GetOnAnalyzerException(ProjectId projectId, DiagnosticLogAggregator diagnosticLogAggregator)
        {
            return _onAnalyzerException ?? base.GetOnAnalyzerException(projectId, diagnosticLogAggregator);
        }

        internal override Action<Exception, DiagnosticAnalyzer, Diagnostic> GetOnAnalyzerException_NoTelemetryLogging(ProjectId projectId)
        {
            return _onAnalyzerException ?? base.GetOnAnalyzerException_NoTelemetryLogging(projectId);
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
