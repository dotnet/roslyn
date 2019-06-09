// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class TestDiagnosticAnalyzerService : DiagnosticAnalyzerService
    {
        private readonly Action<Exception, DiagnosticAnalyzer, Diagnostic> _onAnalyzerException;
        private readonly ImmutableDictionary<object, AnalyzerReference> _hostAnalyzerReferenceMap;

        internal TestDiagnosticAnalyzerService(
            string language,
            DiagnosticAnalyzer analyzer,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
            : this(CreateHostAnalyzerManager(language, analyzer, hostDiagnosticUpdateSource), hostDiagnosticUpdateSource, onAnalyzerException)
        {
        }

        internal TestDiagnosticAnalyzerService(
            string language,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null,
            IAsynchronousOperationListener listener = null)
            : this(CreateHostAnalyzerManager(language, analyzers, hostDiagnosticUpdateSource), hostDiagnosticUpdateSource, onAnalyzerException, listener: listener)
        {
        }

        internal TestDiagnosticAnalyzerService(
            ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzersMap,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null,
            IDiagnosticUpdateSourceRegistrationService registrationService = null)
            : this(CreateHostAnalyzerManager(analyzersMap, hostDiagnosticUpdateSource), hostDiagnosticUpdateSource, onAnalyzerException, registrationService)
        {
        }

        internal TestDiagnosticAnalyzerService(
            ImmutableArray<AnalyzerReference> hostAnalyzerReferences,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
            : this(CreateHostAnalyzerManager(hostAnalyzerReferences, hostDiagnosticUpdateSource), hostDiagnosticUpdateSource, onAnalyzerException)
        {
        }

        private TestDiagnosticAnalyzerService(
            HostAnalyzerManager hostAnalyzerManager,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            IDiagnosticUpdateSourceRegistrationService registrationService = null,
            IAsynchronousOperationListener listener = null)
            : base(hostAnalyzerManager, hostDiagnosticUpdateSource, registrationService ?? new MockDiagnosticUpdateSourceRegistrationService(), listener)
        {
            _hostAnalyzerReferenceMap = hostAnalyzerManager.CreateAnalyzerReferencesMap(projectOpt: null);
            _onAnalyzerException = onAnalyzerException;
        }

        internal TestDiagnosticAnalyzerService(
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            PrimaryWorkspace primaryWorkspace,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
           : base(new Lazy<ImmutableArray<HostDiagnosticAnalyzerPackage>>(() => ImmutableArray<HostDiagnosticAnalyzerPackage>.Empty),
                  hostAnalyzerAssemblyLoader: null, hostDiagnosticUpdateSource, primaryWorkspace, new MockDiagnosticUpdateSourceRegistrationService())
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
                SpecializedCollections.SingletonEnumerable(KeyValuePairUtil.Create(language, analyzers)));
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

        internal IEnumerable<AnalyzerReference> HostAnalyzerReferences => _hostAnalyzerReferenceMap.Values;
    }
}
