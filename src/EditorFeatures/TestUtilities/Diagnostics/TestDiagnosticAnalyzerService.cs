// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        internal TestDiagnosticAnalyzerService(
            string language,
            DiagnosticAnalyzer analyzer,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
            : this(CreateHostAnalyzerManager(language, analyzer), hostDiagnosticUpdateSource, onAnalyzerException)
        {
        }

        internal TestDiagnosticAnalyzerService(
            string language,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null,
            IAsynchronousOperationListener listener = null)
            : this(CreateHostAnalyzerManager(language, analyzers), hostDiagnosticUpdateSource, onAnalyzerException, listener: listener)
        {
        }

        internal TestDiagnosticAnalyzerService(
            ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzersMap,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null,
            IDiagnosticUpdateSourceRegistrationService registrationService = null)
            : this(CreateHostAnalyzerManager(analyzersMap), hostDiagnosticUpdateSource, onAnalyzerException, registrationService)
        {
        }

        internal TestDiagnosticAnalyzerService(
            ImmutableArray<AnalyzerReference> hostAnalyzerReferences,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
            : this(CreateHostAnalyzerManager(hostAnalyzerReferences), hostDiagnosticUpdateSource, onAnalyzerException)
        {
        }

        private TestDiagnosticAnalyzerService(
            DiagnosticAnalyzerInfoCache analyzerInfoCache,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            IDiagnosticUpdateSourceRegistrationService registrationService = null,
            IAsynchronousOperationListener listener = null)
            : base(analyzerInfoCache, hostDiagnosticUpdateSource, registrationService ?? new MockDiagnosticUpdateSourceRegistrationService(), listener)
        {
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

        private static DiagnosticAnalyzerInfoCache CreateHostAnalyzerManager(string language, DiagnosticAnalyzer analyzer)
        {
            return CreateHostAnalyzerManager(language, ImmutableArray.Create(analyzer));
        }

        private static DiagnosticAnalyzerInfoCache CreateHostAnalyzerManager(string language, ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            var map = ImmutableDictionary.CreateRange(SpecializedCollections.SingletonEnumerable(KeyValuePairUtil.Create(language, analyzers)));
            return CreateHostAnalyzerManager(map);
        }

        private static DiagnosticAnalyzerInfoCache CreateHostAnalyzerManager(ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzersMap)
        {
            var analyzerReferences = ImmutableArray.Create<AnalyzerReference>(new TestAnalyzerReferenceByLanguage(analyzersMap));
            return CreateHostAnalyzerManager(analyzerReferences);
        }

        private static DiagnosticAnalyzerInfoCache CreateHostAnalyzerManager(ImmutableArray<AnalyzerReference> hostAnalyzerReferences)
        {
            return new DiagnosticAnalyzerInfoCache(hostAnalyzerReferences);
        }
    }
}
