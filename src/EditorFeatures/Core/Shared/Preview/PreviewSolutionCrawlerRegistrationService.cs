// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Preview
{
    [ExportWorkspaceServiceFactory(typeof(ISolutionCrawlerRegistrationService), WorkspaceKind.Preview), Shared]
    internal class PreviewSolutionCrawlerRegistrationServiceFactory : IWorkspaceServiceFactory
    {
        private readonly DiagnosticAnalyzerService _analyzerService;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PreviewSolutionCrawlerRegistrationServiceFactory(IDiagnosticAnalyzerService analyzerService, IAsynchronousOperationListenerProvider listenerProvider)
        {
            // this service is directly tied to DiagnosticAnalyzerService and
            // depends on its implementation.
            _analyzerService = (DiagnosticAnalyzerService)analyzerService;

            _listener = listenerProvider.GetListener(FeatureAttribute.DiagnosticService);
        }

        public IWorkspaceService? CreateService(HostWorkspaceServices workspaceServices)
        {
            // to make life time management easier, just create new service per new workspace
            return new Service(this, workspaceServices.Workspace);
        }

        // internal for testing
        internal class Service : ISolutionCrawlerRegistrationService
        {
            private readonly PreviewSolutionCrawlerRegistrationServiceFactory _owner;
            private readonly Workspace _workspace;
            private readonly CancellationTokenSource _source;

            // since we now have one service for each one specific instance of workspace,
            // we can have states for this specific workspace.
            private Task? _analyzeTask;

            public Service(PreviewSolutionCrawlerRegistrationServiceFactory owner, Workspace workspace)
            {
                _owner = owner;
                _workspace = workspace;
                _source = new CancellationTokenSource();
            }

            public void Register(Workspace workspace)
            {
                // given workspace must be owner of this workspace service
                Contract.ThrowIfFalse(workspace == _workspace);

                // this can't be called twice
                Contract.ThrowIfFalse(_analyzeTask == null);

                var asyncToken = _owner._listener.BeginAsyncOperation(nameof(PreviewSolutionCrawlerRegistrationServiceFactory) + "." + nameof(Service) + "." + nameof(Register));
                _analyzeTask = AnalyzeAsync().CompletesAsyncOperation(asyncToken);
            }

            private async Task AnalyzeAsync()
            {
                var workerBackOffTimeSpan = SolutionCrawlerTimeSpan.PreviewBackOff;
                var incrementalAnalyzer = _owner._analyzerService.CreateIncrementalAnalyzer(_workspace);

                var solution = _workspace.CurrentSolution;
                var documentIds = _workspace.GetOpenDocumentIds().ToImmutableArray();

                try
                {
                    foreach (var documentId in documentIds)
                    {
                        var textDocument = solution.GetTextDocument(documentId);
                        if (textDocument == null)
                        {
                            continue;
                        }

                        // delay analyzing
                        await _owner._listener.Delay(workerBackOffTimeSpan, _source.Token).ConfigureAwait(false);

                        // do actual analysis
                        if (textDocument is Document document)
                        {
                            await incrementalAnalyzer.AnalyzeSyntaxAsync(document, InvocationReasons.Empty, _source.Token).ConfigureAwait(false);
                            await incrementalAnalyzer.AnalyzeDocumentAsync(document, bodyOpt: null, reasons: InvocationReasons.Empty, cancellationToken: _source.Token).ConfigureAwait(false);
                        }
                        else
                        {
                            await incrementalAnalyzer.AnalyzeNonSourceDocumentAsync(textDocument, InvocationReasons.Empty, _source.Token).ConfigureAwait(false);
                        }

                        // don't call project one.
                    }
                }
                catch (OperationCanceledException)
                {
                    // do nothing
                }
            }

            public void Unregister(Workspace workspace, bool blockingShutdown = false)
                => _ = UnregisterAsync(workspace);

            private async Task UnregisterAsync(Workspace workspace)
            {
                Contract.ThrowIfFalse(workspace == _workspace);
                Contract.ThrowIfNull(_analyzeTask);

                _source.Cancel();

                // wait for analyzer work to be finished
                await _analyzeTask.ConfigureAwait(false);

                // ask it to reset its stages for the given workspace
                _owner._analyzerService.ShutdownAnalyzerFrom(_workspace);
            }

            public void AddAnalyzerProvider(IIncrementalAnalyzerProvider provider, IncrementalAnalyzerProviderMetadata metadata)
            {
                // preview solution crawler doesn't support adding and removing analyzer dynamically
                throw new NotSupportedException();
            }
        }
    }
}
