// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    internal class DiagnosticTaggerWrapper<TProvider, TTag> : IDisposable
        where TProvider : AbstractDiagnosticsTaggerProvider<TTag>
        where TTag : ITag
    {
        private readonly TestWorkspace _workspace;
        public readonly DiagnosticAnalyzerService? AnalyzerService;
        private readonly SolutionCrawlerRegistrationService _registrationService;
        public readonly DiagnosticService DiagnosticService;
        private readonly IThreadingContext _threadingContext;
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;

        private ITaggerProvider? _taggerProvider;

        public DiagnosticTaggerWrapper(
            TestWorkspace workspace,
            IReadOnlyDictionary<string, ImmutableArray<DiagnosticAnalyzer>>? analyzerMap = null,
            IDiagnosticUpdateSource? updateSource = null,
            bool createTaggerProvider = true)
        {
            _threadingContext = workspace.GetService<IThreadingContext>();
            _listenerProvider = workspace.GetService<IAsynchronousOperationListenerProvider>();

            var analyzerReference = new TestAnalyzerReferenceByLanguage(analyzerMap ?? DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            _workspace = workspace;

            _registrationService = (SolutionCrawlerRegistrationService)workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
            _registrationService.Register(workspace);

            if (!_registrationService.GetTestAccessor().TryGetWorkCoordinator(workspace, out var coordinator))
                throw new InvalidOperationException();

            AnalyzerService = (DiagnosticAnalyzerService?)_registrationService.GetTestAccessor().AnalyzerProviders.SelectMany(pair => pair.Value).SingleOrDefault(lazyProvider => lazyProvider.Metadata.Name == WellKnownSolutionCrawlerAnalyzers.Diagnostic && lazyProvider.Metadata.HighPriorityForActiveFile)?.Value;
            DiagnosticService = (DiagnosticService)workspace.ExportProvider.GetExportedValue<IDiagnosticService>();

            if (updateSource is object)
            {
                DiagnosticService.Register(updateSource);
            }

            if (createTaggerProvider)
            {
                _ = TaggerProvider;
            }
        }

        public ITaggerProvider TaggerProvider
        {
            get
            {
                if (_taggerProvider == null)
                {
                    WpfTestRunner.RequireWpfFact($"{nameof(DiagnosticTaggerWrapper<TProvider, TTag>)}.{nameof(TaggerProvider)} creates asynchronous taggers");

                    if (typeof(TProvider) == typeof(DiagnosticsSquiggleTaggerProvider)
                        || typeof(TProvider) == typeof(DiagnosticsSuggestionTaggerProvider)
                        || typeof(TProvider) == typeof(DiagnosticsClassificationTaggerProvider))
                    {
                        _taggerProvider = _workspace.ExportProvider.GetExportedValues<ITaggerProvider>()
                            .OfType<TProvider>()
                            .Single();
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }

                return _taggerProvider;
            }
        }

        public void Dispose()
            => _registrationService.Unregister(_workspace);

        public async Task WaitForTags()
        {
            await _listenerProvider.GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();
            await _listenerProvider.GetWaiter(FeatureAttribute.SolutionCrawler).ExpeditedWaitAsync();
            await _listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).ExpeditedWaitAsync();
            await _listenerProvider.GetWaiter(FeatureAttribute.ErrorSquiggles).ExpeditedWaitAsync();
            await _listenerProvider.GetWaiter(FeatureAttribute.Classification).ExpeditedWaitAsync();
        }
    }
}
