﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Roslyn.Utilities;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class TestDiagnosticAnalyzerService : DiagnosticAnalyzerService
    {
        private readonly Action<Exception, DiagnosticAnalyzer, Diagnostic> _onAnalyzerException;

        internal TestDiagnosticAnalyzerService(string language, DiagnosticAnalyzer analyzer, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
            : this(CreateHostAnalyzerManager(language, analyzer, hostDiagnosticUpdateSource), hostDiagnosticUpdateSource, onAnalyzerException)
        {
        }

        internal TestDiagnosticAnalyzerService(string language, ImmutableArray<DiagnosticAnalyzer> analyzers, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
            : this(CreateHostAnalyzerManager(language, analyzers, hostDiagnosticUpdateSource), hostDiagnosticUpdateSource, onAnalyzerException)
        {
        }

        internal TestDiagnosticAnalyzerService(ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzersMap, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
            : this(CreateHostAnalyzerManager(analyzersMap, hostDiagnosticUpdateSource), hostDiagnosticUpdateSource, onAnalyzerException)
        {
        }

        internal TestDiagnosticAnalyzerService(ImmutableArray<AnalyzerReference> hostAnalyzerReferences, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
            : this(CreateHostAnalyzerManager(hostAnalyzerReferences, hostDiagnosticUpdateSource), hostDiagnosticUpdateSource, onAnalyzerException)
        {
        }

        private TestDiagnosticAnalyzerService(HostAnalyzerManager hostAnalyzerManager, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException)
            : base(hostAnalyzerManager, hostDiagnosticUpdateSource)
        {
            _onAnalyzerException = onAnalyzerException;
        }

        internal TestDiagnosticAnalyzerService(AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
           : base(SpecializedCollections.EmptyEnumerable<HostDiagnosticAnalyzerPackage>(), hostDiagnosticUpdateSource)
        {
            _onAnalyzerException = onAnalyzerException;
        }

        private static HostAnalyzerManager CreateHostAnalyzerManager(string language, DiagnosticAnalyzer analyzer, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            return CreateHostAnalyzerManager(language, ImmutableArray.Create(analyzer), hostDiagnosticUpdateSource);
        }

        private static HostAnalyzerManager CreateHostAnalyzerManager(string language, ImmutableArray<DiagnosticAnalyzer> analyzers, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            var map = ImmutableDictionary.CreateRange(
                SpecializedCollections.SingletonEnumerable(KeyValuePair.Create(language, analyzers)));
            return CreateHostAnalyzerManager(map, hostDiagnosticUpdateSource);
        }

        private static HostAnalyzerManager CreateHostAnalyzerManager(ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzersMap, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            var analyzerReferences = ImmutableArray.Create<AnalyzerReference>(new TestAnalyzerReferenceByLanguage(analyzersMap));
            return CreateHostAnalyzerManager(analyzerReferences, hostDiagnosticUpdateSource);
        }

        private static HostAnalyzerManager CreateHostAnalyzerManager(ImmutableArray<AnalyzerReference> hostAnalyzerReferences, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            return new HostAnalyzerManager(hostAnalyzerReferences, hostDiagnosticUpdateSource);
        }

        internal override Action<Exception, DiagnosticAnalyzer, Diagnostic> GetOnAnalyzerException(ProjectId projectId, DiagnosticLogAggregator diagnosticLogAggregator)
        {
            return _onAnalyzerException ?? base.GetOnAnalyzerException(projectId, diagnosticLogAggregator);
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
}
