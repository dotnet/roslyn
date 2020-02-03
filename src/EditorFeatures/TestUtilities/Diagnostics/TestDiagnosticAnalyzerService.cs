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
        internal TestDiagnosticAnalyzerService(
            string language,
            DiagnosticAnalyzer analyzer,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null)
            : this(CreateHostAnalyzerManager(language, analyzer), hostDiagnosticUpdateSource)
        {
        }

        internal TestDiagnosticAnalyzerService(
            string language,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null,
            IAsynchronousOperationListener listener = null)
            : this(CreateHostAnalyzerManager(language, analyzers), hostDiagnosticUpdateSource, listener: listener)
        {
        }

        internal TestDiagnosticAnalyzerService(
            ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzersMap,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null,
            IDiagnosticUpdateSourceRegistrationService registrationService = null)
            : this(CreateHostAnalyzerManager(analyzersMap), hostDiagnosticUpdateSource, registrationService)
        {
        }

        internal TestDiagnosticAnalyzerService(
            ImmutableArray<AnalyzerReference> hostAnalyzerReferences,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null)
            : this(CreateHostAnalyzerManager(hostAnalyzerReferences), hostDiagnosticUpdateSource)
        {
        }

        private TestDiagnosticAnalyzerService(
            DiagnosticAnalyzerInfoCache analyzerInfoCache,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            IDiagnosticUpdateSourceRegistrationService registrationService = null,
            IAsynchronousOperationListener listener = null)
            : base(analyzerInfoCache, hostDiagnosticUpdateSource, registrationService ?? new MockDiagnosticUpdateSourceRegistrationService(), listener)
        {
        }

        internal TestDiagnosticAnalyzerService(
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            PrimaryWorkspace primaryWorkspace)
           : base(new Lazy<ImmutableArray<HostDiagnosticAnalyzerPackage>>(() => ImmutableArray<HostDiagnosticAnalyzerPackage>.Empty),
                  hostAnalyzerAssemblyLoader: null, hostDiagnosticUpdateSource, primaryWorkspace, new MockDiagnosticUpdateSourceRegistrationService())
        {
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
