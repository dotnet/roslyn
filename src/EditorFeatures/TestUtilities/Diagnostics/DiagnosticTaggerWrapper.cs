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

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    internal class DiagnosticTaggerWrapper<TProvider, TTag> : IDisposable
        where TProvider : AbstractDiagnosticsTaggerProvider<TTag>
        where TTag : ITag
    {
        private readonly TestWorkspace _workspace;
        public readonly DiagnosticAnalyzerService? AnalyzerService;
        private readonly ISolutionCrawlerRegistrationService _registrationService;
        private readonly ImmutableArray<IIncrementalAnalyzer> _incrementalAnalyzers;
        private readonly SolutionCrawlerRegistrationService? _solutionCrawlerService;
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

            if (updateSource == null)
            {
                updateSource = AnalyzerService = new MyDiagnosticAnalyzerService(_listenerProvider.GetListener(FeatureAttribute.DiagnosticService));
            }

            var analyzerReference = new TestAnalyzerReferenceByLanguage(analyzerMap ?? DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            _workspace = workspace;

            _registrationService = workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
            _registrationService.Register(workspace);

            DiagnosticService = (DiagnosticService)workspace.ExportProvider.GetExportedValue<IDiagnosticService>();
            DiagnosticService.Register(updateSource);

            if (createTaggerProvider)
            {
                _ = TaggerProvider;
            }

            if (AnalyzerService != null)
            {
                _incrementalAnalyzers = ImmutableArray.Create(AnalyzerService.CreateIncrementalAnalyzer(workspace));
                _solutionCrawlerService = workspace.Services.GetService<ISolutionCrawlerRegistrationService>() as SolutionCrawlerRegistrationService;
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
            if (_solutionCrawlerService != null)
            {
                _solutionCrawlerService.GetTestAccessor().WaitUntilCompletion(_workspace, _incrementalAnalyzers);
            }

            await _listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).ExpeditedWaitAsync();
            await _listenerProvider.GetWaiter(FeatureAttribute.ErrorSquiggles).ExpeditedWaitAsync();
            await _listenerProvider.GetWaiter(FeatureAttribute.Classification).ExpeditedWaitAsync();
        }

        private class MyDiagnosticAnalyzerService : DiagnosticAnalyzerService
        {
            internal MyDiagnosticAnalyzerService(IAsynchronousOperationListener listener)
                : base(new MockDiagnosticUpdateSourceRegistrationService(), listener)
            {
            }
        }
    }
}
